﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using puyo_tools;
using SonicRetro.SAModel.Direct3D;
using VrSharp.PvrTexture;

namespace SonicRetro.SAModel.SAMDL
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
        }

        void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            File.WriteAllText("SAMDL.log", e.Exception.ToString());
            if (MessageBox.Show("Unhandled " + e.Exception.GetType().Name + "\nLog file has been saved.\n\nDo you want to try to continue running?", "SAMDL Fatal Error", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == System.Windows.Forms.DialogResult.No)
                Close();
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            File.WriteAllText("SAMDL.log", e.ExceptionObject.ToString());
            MessageBox.Show("Unhandled Exception: " + e.ExceptionObject.GetType().Name + "\nLog file has been saved.", "SAMDL Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        internal Device d3ddevice;
        Camera cam = new Camera();
        bool loaded;
        int interval = 1;
        FillMode rendermode;
        Cull cullmode = Cull.None;
        Object model;
        Animation[] animations;
        Animation animation;
        ModelFile modelFile;
        int animnum = -1;
        int animframe = 0;
        Microsoft.DirectX.Direct3D.Mesh[] meshes;
        string[] TextureNames;
        Bitmap[] TextureBmps;
        Texture[] Textures;

        private void MainForm_Load(object sender, EventArgs e)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.Opaque, true);
            d3ddevice = new Device(0, DeviceType.Hardware, panel1.Handle, CreateFlags.SoftwareVertexProcessing, new PresentParameters[] { new PresentParameters() { Windowed = true, SwapEffect = SwapEffect.Discard, EnableAutoDepthStencil = true, AutoDepthStencilFormat = DepthFormat.D24X8 } });
            d3ddevice.Lights[0].Type = LightType.Directional;
            d3ddevice.Lights[0].Diffuse = Color.White;
            d3ddevice.Lights[0].Ambient = Color.White;
            d3ddevice.Lights[0].Specular = Color.White;
            d3ddevice.Lights[0].Range = 100000;
            d3ddevice.Lights[0].Direction = Vector3.Normalize(new Vector3(0, -1, 0));
            d3ddevice.Lights[0].Enabled = true;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (loaded)
                switch (MessageBox.Show(this, "Do you want to save?", "SAMDL", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question))
                {
                    case DialogResult.Yes:
                        saveToolStripMenuItem_Click(this, EventArgs.Empty);
                        break;
                    case DialogResult.Cancel:
                        return;
                }
            OpenFileDialog a = new OpenFileDialog()
            {
                DefaultExt = "sa1mdl",
                Filter = "Model Files|*.sa1mdl;*.sa2mdl;*.exe;*.dll;*.bin|All Files|*.*"
            };
            if (a.ShowDialog(this) == DialogResult.OK)
            {
                loaded = false;
                Environment.CurrentDirectory = Path.GetDirectoryName(a.FileName);
                timer1.Stop();
                modelFile = null;
                animation = null;
                animations = null;
                animnum = -1;
                animframe = 0;
                if (ModelFile.CheckModelFile(a.FileName))
                {
                    modelFile = new ModelFile(a.FileName);
                    model = modelFile.Model;
                    animations = new Animation[modelFile.Animations.Count];
                    modelFile.Animations.CopyTo(animations, 0);
                }
                else
                {
                    using (FileTypeDialog ftd = new FileTypeDialog())
                    {
                        if (ftd.ShowDialog(this) != System.Windows.Forms.DialogResult.OK)
                            return;
                        byte[] file = File.ReadAllBytes(a.FileName);
                        if (ftd.typBinary.Checked)
                        {
                            using (ModelFileDialog dlg = new ModelFileDialog())
                            {
                                dlg.ShowDialog(this);
                                if (dlg.checkBox1.Checked)
                                    animations = new Animation[] { Animation.ReadHeader(file, (int)dlg.numericUpDown3.Value, (uint)dlg.numericUpDown2.Value, (ModelFormat)dlg.comboBox2.SelectedIndex) };
                                model = new Object(file, (int)dlg.NumericUpDown1.Value, (uint)dlg.numericUpDown2.Value, (ModelFormat)dlg.comboBox2.SelectedIndex);
                            }
                        }
                        else if (ftd.typSA2MDL.Checked | ftd.typSA2BMDL.Checked)
                        {
                            ModelFormat fmt = ModelFormat.SA2;
                            ByteConverter.BigEndian = false;
                            if (ftd.typSA2BMDL.Checked)
                            {
                                fmt = ModelFormat.SA2B;
                                ByteConverter.BigEndian = true;
                            }
                            using (SA2MDLDialog dlg = new SA2MDLDialog())
                            {
                                int address = 0;
                                SortedDictionary<int, Object> sa2models = new SortedDictionary<int, Object>();
                                int i = ByteConverter.ToInt32(file, address);
                                while (i != -1)
                                {
                                    sa2models.Add(i, new Object(file, ByteConverter.ToInt32(file, address + 4), 0, fmt));
                                    address += 8;
                                    i = ByteConverter.ToInt32(file, address);
                                }
                                foreach (KeyValuePair<int, Object> item in sa2models)
                                    dlg.modelChoice.Items.Add(item.Key + ": " + item.Value.Name);
                                dlg.ShowDialog(this);
                                i = 0;
                                foreach (KeyValuePair<int, Object> item in sa2models)
                                {
                                    if (i == dlg.modelChoice.SelectedIndex)
                                    {
                                        model = item.Value;
                                        break;
                                    }
                                    i++;
                                }
                                if (dlg.checkBox1.Checked)
                                {
                                    using (OpenFileDialog anidlg = new OpenFileDialog()
                                        {
                                            DefaultExt = "bin",
                                            Filter = "Motion Files|*MTN.BIN|All Files|*.*"
                                        })
                                    {
                                        if (anidlg.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                                        {
                                            byte[] anifile = File.ReadAllBytes(anidlg.FileName);
                                            address = 0;
                                            SortedDictionary<int, Animation> anis = new SortedDictionary<int, Animation>();
                                            i = ByteConverter.ToInt32(file, address);
                                            while (i != -1)
                                            {
                                                anis.Add(i, new Animation(file, ByteConverter.ToInt32(file, address + 4), 0, fmt, model.CountAnimated()));
                                                address += 8;
                                                i = ByteConverter.ToInt32(file, address);
                                            }
                                            animations = new List<Animation>(anis.Values).ToArray();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                Object[] models = model.GetObjects();
                meshes = new Microsoft.DirectX.Direct3D.Mesh[models.Length];
                for (int i = 0; i < models.Length; i++)
                    if (models[i].Attach != null)
                        try { meshes[i] = models[i].Attach.CreateD3DMesh(d3ddevice); }
                        catch { }
                loaded = true;
                DrawLevel();
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (loaded)
                switch (MessageBox.Show(this, "Do you want to save?", "SAMDL", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question))
                {
                    case DialogResult.Yes:
                        saveToolStripMenuItem_Click(this, EventArgs.Empty);
                        break;
                    case DialogResult.Cancel:
                        e.Cancel = true;
                        break;
                }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog a = new SaveFileDialog()
            {
                DefaultExt = "sa1mdl",
                Filter = "Model Files|*.sa1mdl|All Files|*.*"
            };
            if (a.ShowDialog(this) == DialogResult.OK)
            {
                if (modelFile != null)
                    modelFile.SaveToFile(a.FileName, ModelFormat.SA1);
                else
                    ModelFile.CreateFile(a.FileName, model, ModelFormat.SA1);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        internal void DrawLevel()
        {
            if (!loaded) return;
            d3ddevice.SetTransform(TransformType.Projection, Matrix.PerspectiveFovRH((float)(Math.PI / 4), panel1.Width / (float)panel1.Height, 1, 10000));
            d3ddevice.SetTransform(TransformType.View, cam.ToMatrix());
            Text = "X=" + cam.Position.X + " Y=" + cam.Position.Y + " Z=" + cam.Position.Z + " Pitch=" + cam.Pitch.ToString("X") + " Yaw=" + cam.Yaw.ToString("X") + " Interval=" + interval + (cam.mode == 1 ? " Distance=" + cam.Distance : "") + (animation != null ? " Animation=" + animation.Name + " Frame=" + animframe : "");
            d3ddevice.SetRenderState(RenderStates.FillMode, (int)rendermode);
            d3ddevice.SetRenderState(RenderStates.CullMode, (int)cullmode);
            d3ddevice.Material = new Microsoft.DirectX.Direct3D.Material { Ambient = Color.White };
            d3ddevice.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black.ToArgb(), 1, 0);
            d3ddevice.RenderState.ZBufferEnable = true;
            d3ddevice.BeginScene();
            //all drawings after this line
            d3ddevice.SetSamplerState(0, SamplerStageStates.MinFilter, (int)TextureFilter.Anisotropic);
            d3ddevice.SetSamplerState(0, SamplerStageStates.MagFilter, (int)TextureFilter.Anisotropic);
            d3ddevice.SetSamplerState(0, SamplerStageStates.MipFilter, (int)TextureFilter.Anisotropic);
            d3ddevice.SetRenderState(RenderStates.Lighting, true);
            d3ddevice.SetRenderState(RenderStates.SpecularEnable, false);
            d3ddevice.SetRenderState(RenderStates.Ambient, Color.White.ToArgb());
            d3ddevice.SetRenderState(RenderStates.AlphaBlendEnable, true);
            d3ddevice.SetRenderState(RenderStates.BlendOperation, (int)BlendOperation.Add);
            d3ddevice.SetRenderState(RenderStates.DestinationBlend, (int)Blend.InvSourceAlpha);
            d3ddevice.SetRenderState(RenderStates.SourceBlend, (int)Blend.SourceAlpha);
            d3ddevice.SetRenderState(RenderStates.AlphaTestEnable, true);
            d3ddevice.SetRenderState(RenderStates.AlphaFunction, (int)Compare.Greater);
            d3ddevice.SetRenderState(RenderStates.AmbientMaterialSource, (int)ColorSource.Material);
            d3ddevice.SetRenderState(RenderStates.DiffuseMaterialSource, (int)ColorSource.Material);
            d3ddevice.SetRenderState(RenderStates.SpecularMaterialSource, (int)ColorSource.Material);
            d3ddevice.SetTextureStageState(0, TextureStageStates.AlphaOperation, (int)TextureOperation.BlendDiffuseAlpha);
            d3ddevice.SetRenderState(RenderStates.ColorVertex, true);
            MatrixStack transform = new MatrixStack();
            if (animation != null)
                RenderInfo.Draw(model.DrawModelTreeAnimated(d3ddevice, transform, Textures, meshes, animation, animframe), d3ddevice, cam);
            else
                RenderInfo.Draw(model.DrawModelTree(d3ddevice, transform, Textures, meshes), d3ddevice, cam);
            d3ddevice.EndScene(); //all drawings before this line
            d3ddevice.Present();
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            DrawLevel();
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (!loaded) return;
            if (cam.mode == 0)
            {
                if (e.KeyCode == Keys.Down)
                    if (e.Shift)
                        cam.Position += cam.Up * -interval;
                    else
                        cam.Position += cam.Look * interval;
                if (e.KeyCode == Keys.Up)
                    if (e.Shift)
                        cam.Position += cam.Up * interval;
                    else
                        cam.Position += cam.Look * -interval;
                if (e.KeyCode == Keys.Left)
                    cam.Position += cam.Right * -interval;
                if (e.KeyCode == Keys.Right)
                    cam.Position += cam.Right * interval;
                if (e.KeyCode == Keys.K)
                    cam.Yaw = unchecked((ushort)(cam.Yaw - 0x100));
                if (e.KeyCode == Keys.J)
                    cam.Yaw = unchecked((ushort)(cam.Yaw + 0x100));
                if (e.KeyCode == Keys.H)
                    cam.Yaw = unchecked((ushort)(cam.Yaw + 0x4000));
                if (e.KeyCode == Keys.L)
                    cam.Yaw = unchecked((ushort)(cam.Yaw - 0x4000));
                if (e.KeyCode == Keys.M)
                    cam.Pitch = unchecked((ushort)(cam.Pitch - 0x100));
                if (e.KeyCode == Keys.I)
                    cam.Pitch = unchecked((ushort)(cam.Pitch + 0x100));
                if (e.KeyCode == Keys.E)
                    cam.Position = new Vector3();
                if (e.KeyCode == Keys.R)
                {
                    cam.Pitch = 0;
                    cam.Yaw = 0;
                }
            }
            else
            {
                if (e.KeyCode == Keys.Down)
                    if (e.Shift)
                        cam.Pitch = unchecked((ushort)(cam.Pitch - 0x100));
                    else
                        cam.Distance += interval;
                if (e.KeyCode == Keys.Up)
                    if (e.Shift)
                        cam.Pitch = unchecked((ushort)(cam.Pitch + 0x100));
                    else
                    {
                        cam.Distance -= interval;
                        cam.Distance = Math.Max(cam.Distance, interval);
                    }
                if (e.KeyCode == Keys.Left)
                    cam.Yaw = unchecked((ushort)(cam.Yaw + 0x100));
                if (e.KeyCode == Keys.Right)
                    cam.Yaw = unchecked((ushort)(cam.Yaw - 0x100));
                if (e.KeyCode == Keys.K)
                    cam.Yaw = unchecked((ushort)(cam.Yaw - 0x100));
                if (e.KeyCode == Keys.J)
                    cam.Yaw = unchecked((ushort)(cam.Yaw + 0x100));
                if (e.KeyCode == Keys.H)
                    cam.Yaw = unchecked((ushort)(cam.Yaw + 0x4000));
                if (e.KeyCode == Keys.L)
                    cam.Yaw = unchecked((ushort)(cam.Yaw - 0x4000));
                if (e.KeyCode == Keys.M)
                    cam.Pitch = unchecked((ushort)(cam.Pitch - 0x100));
                if (e.KeyCode == Keys.I)
                    cam.Pitch = unchecked((ushort)(cam.Pitch + 0x100));
                if (e.KeyCode == Keys.R)
                {
                    cam.Pitch = 0;
                    cam.Yaw = 0;
                }
            }
            if (e.KeyCode == Keys.X)
                cam.mode = (cam.mode + 1) % 2;
            if (e.KeyCode == Keys.Q)
                interval += 1;
            if (e.KeyCode == Keys.W)
                interval -= 1;
            if (e.KeyCode == Keys.OemQuotes & animations != null)
            {
                animnum++;
                animframe = 0;
                if (animnum == animations.Length) animnum = -1;
                if (animnum > -1)
                    animation = animations[animnum];
                else
                    animation = null;
            }
            if (e.KeyCode == Keys.OemSemicolon & animations != null)
            {
                animnum--;
                animframe = 0;
                if (animnum == -2) animnum = animations.Length - 1;
                if (animnum > -1)
                    animation = animations[animnum];
                else
                    animation = null;
            }
            if (e.KeyCode == Keys.OemOpenBrackets & animation != null)
            {
                animframe--;
                if (animframe < 0) animframe = animation.Frames - 1;
            }
            if (e.KeyCode == Keys.OemCloseBrackets & animation != null)
            {
                animframe++;
                if (animframe == animation.Frames) animframe = 0;
            }
            if (e.KeyCode == Keys.P & animation != null)
                timer1.Enabled = !timer1.Enabled;
            if (e.KeyCode == Keys.N)
                if (rendermode == FillMode.Solid)
                    rendermode = FillMode.Point;
                else
                    rendermode += 1;
            DrawLevel();
        }

        Point lastmouse;
        private void Panel1_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (!loaded) return;
            Point evloc = e.Location;
            if (lastmouse == Point.Empty)
            {
                lastmouse = evloc;
                return;
            }
            Point chg = evloc - (Size)lastmouse;
            if (e.Button == System.Windows.Forms.MouseButtons.Middle)
            {
                cam.Yaw = unchecked((ushort)(cam.Yaw - chg.X * 0x10));
                cam.Pitch = unchecked((ushort)(cam.Pitch - chg.Y * 0x10));
            }
            lastmouse = evloc;
        }

        public static Dictionary<string, Bitmap> GetTextures(string filename)
        {
            Dictionary<string, Bitmap> functionReturnValue = new Dictionary<string, Bitmap>();
            bool gvm = false;
            ArchiveModule pvmfile = null;
            switch (Path.GetExtension(filename).ToLowerInvariant())
            {
                case ".pvm":
                    pvmfile = new PVM();
                    break;
                case ".gvm":
                    pvmfile = new GVM();
                    gvm = true;
                    break;
            }
            VrSharp.VpClut pvp = null;
            Stream pvmdata = new MemoryStream(File.ReadAllBytes(filename));
            pvmdata = pvmfile.TranslateData(ref pvmdata);
            ArchiveFileList pvmentries = pvmfile.GetFileList(ref pvmdata);
            foreach (ArchiveFileList.Entry file in pvmentries.Entries)
            {
                byte[] data = new byte[file.Length];
                pvmdata.Seek(file.Offset, SeekOrigin.Begin);
                pvmdata.Read(data, 0, (int)file.Length);
                VrSharp.VrTexture vrfile = gvm ? (VrSharp.VrTexture)new VrSharp.GvrTexture.GvrTexture(data) : (VrSharp.VrTexture)new VrSharp.PvrTexture.PvrTexture(data);
                if (vrfile.NeedsExternalClut())
                {
                    using (OpenFileDialog a = new OpenFileDialog
                    {
                        DefaultExt = gvm ? "gvp" : "pvp",
                        Filter = gvm ? "GVP Files|*.gvp" : "PVP Files|*.pvp",
                        InitialDirectory = System.IO.Path.GetDirectoryName(filename),
                        Title = "External palette file"
                    })
                    {
                        if (pvp == null)
                            if (a.ShowDialog() == DialogResult.OK)
                                pvp = gvm ? (VrSharp.VpClut)new VrSharp.GvrTexture.GvpClut(a.FileName) : (VrSharp.VpClut)new PvpClut(a.FileName);
                            else
                                return new Dictionary<string, Bitmap>();
                    }
                    vrfile.SetClut(pvp);
                }
                try
                {
                    functionReturnValue.Add(Path.GetFileNameWithoutExtension(file.Filename), vrfile.GetTextureAsBitmap());
                }
                catch
                {
                    functionReturnValue.Add(Path.GetFileNameWithoutExtension(file.Filename), new Bitmap(1, 1));
                }
            }
            return functionReturnValue;
        }

        private void loadTexturesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog a = new OpenFileDialog() { DefaultExt = "pvm", Filter = "PVM Files|*.pvm;*.gvm" })
            {
                if (a.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    List<string> texnames = new List<string>();
                    List<Bitmap> bmps = new List<Bitmap>();
                    foreach (KeyValuePair<string, Bitmap> item in GetTextures(a.FileName))
                    {
                        texnames.Add(item.Key);
                        bmps.Add(item.Value);
                    }
                    TextureNames = texnames.ToArray();
                    TextureBmps = bmps.ToArray();
                    Textures = new Texture[TextureBmps.Length];
                    for (int j = 0; j < TextureBmps.Length; j++)
                        Textures[j] = new Texture(d3ddevice, TextureBmps[j], Usage.SoftwareProcessing, Pool.Managed);
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (animation == null) return;
            animframe++;
            if (animframe == animation.Frames) animframe = 0;
            DrawLevel();
        }

        private void colladaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sd = new SaveFileDialog() { DefaultExt = "dae", Filter = "DAE Files|*.dae" })
            {
                if (sd.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                {
                    model.ToCollada(TextureNames).Save(sd.FileName);
                    string p = Path.GetDirectoryName(sd.FileName);
                    if (TextureNames != null)
                        for (int i = 0; i < TextureNames.Length; i++)
                            TextureBmps[i].Save(Path.Combine(p, TextureNames[i] + ".png"));
                }
            }
        }

        private void modelTreeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new ModelTreeForm(model).Show(this);
        }
    }
}