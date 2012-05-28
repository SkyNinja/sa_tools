﻿using System;
using System.Collections.Generic;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using SADXPCTools;
using SonicRetro.SAModel.Direct3D;

namespace SonicRetro.SAModel.SADXLVL2
{
    public class ObjectData
    {
        [IniName("codefile")]
        public string CodeFile;
        [IniName("codetype")]
        public string CodeType;
        public string Name;
        public string Model;
        public string Texture;
        public float? XPos, YPos, ZPos, XScl, YScl, ZScl;
        [IniName("XRot")]
        public string XRotString;
        [IniIgnore]
        public int? XRot { get { return XRotString == null ? null : (int?)int.Parse(XRotString, System.Globalization.NumberStyles.HexNumber); } set { XRotString = value.HasValue ? null : value.Value.ToString("X"); } }
        [IniName("YRot")]
        public string YRotString;
        [IniIgnore]
        public int? YRot { get { return YRotString == null ? null : (int?)int.Parse(YRotString, System.Globalization.NumberStyles.HexNumber); } set { YRotString = value.HasValue ? null : value.Value.ToString("X"); } }
        [IniName("ZRot")]
        public string ZRotString;
        [IniIgnore]
        public int? ZRot { get { return ZRotString == null ? null : (int?)int.Parse(ZRotString, System.Globalization.NumberStyles.HexNumber); } set { ZRotString = value.HasValue ? null : value.Value.ToString("X"); } }
        public Dictionary<string, string> CustomProperties;
    }
    
    public abstract class ObjectDefinition
    {
        public abstract void Init(ObjectData data, string name, Device dev);
        public abstract HitResult CheckHit(SETItem item, Vector3 Near, Vector3 Far, Viewport Viewport, Matrix Projection, Matrix View, MatrixStack transform);
        public abstract RenderInfo[] Render(SETItem item, Device dev, MatrixStack transform, bool selected);
        public abstract string Name { get; }

        public virtual Type ObjectType { get { return typeof(SETItem); } }
    }

    internal class DefaultObjectDefinition : ObjectDefinition
    {
        private string name = "Unknown";
        private Object model;
        private Microsoft.DirectX.Direct3D.Mesh[] meshes;
        private string texture;
        private float? xpos, ypos, zpos, xscl, yscl, zscl;
        private int? xrot, yrot, zrot;

        public override void Init(ObjectData data, string name, Device dev)
        {
            this.name = data.Name ?? name;
            if (!string.IsNullOrEmpty(data.Model))
            {
                model = ObjectHelper.LoadModel(data.Model);
                meshes = ObjectHelper.GetMeshes(model, dev);
            }
            texture = data.Texture;
            xpos = data.XPos;
            ypos = data.YPos;
            zpos = data.ZPos;
            xrot = data.XRot;
            yrot = data.YRot;
            zrot = data.ZRot;
            xscl = data.XScl;
            yscl = data.YScl;
            zscl = data.ZScl;
        }

        public override HitResult CheckHit(SETItem item, Vector3 Near, Vector3 Far, Viewport Viewport, Matrix Projection, Matrix View, MatrixStack transform)
        {
            transform.Push();
            transform.TranslateLocal(xpos ?? item.Position.X, ypos ?? item.Position.Y, zpos ?? item.Position.Z);
            transform.RotateXYZLocal(xrot ?? item.Rotation.X, yrot ?? item.Rotation.Y, zrot ?? item.Rotation.Z);
            HitResult result;
            if (model == null)
                result = ObjectHelper.CheckSpriteHit(Near, Far, Viewport, Projection, View, transform);
            else
            {
                transform.ScaleLocal(xscl ?? item.Scale.X, yscl ?? item.Scale.Y, zscl ?? item.Scale.Z);
                result = model.CheckHit(Near, Far, Viewport, Projection, View, transform, meshes);
            }
            transform.Pop();
            return result;
        }

        public override RenderInfo[] Render(SETItem item, Device dev, MatrixStack transform, bool selected)
        {
            List<RenderInfo> result = new List<RenderInfo>();
            transform.Push();
            transform.TranslateLocal(xpos ?? item.Position.X, ypos ?? item.Position.Y, zpos ?? item.Position.Z);
            transform.RotateXYZLocal(xrot ?? item.Rotation.X, yrot ?? item.Rotation.Y, zrot ?? item.Rotation.Z);
            if (model == null)
                result.AddRange(ObjectHelper.RenderSprite(dev, transform, null, item.Position.ToVector3(), selected));
            else
            {
                transform.ScaleLocal(xscl ?? item.Scale.X, yscl ?? item.Scale.Y, zscl ?? item.Scale.Z);
                result.AddRange(model.DrawModelTree(dev, transform, ObjectHelper.GetTextures(texture), meshes));
                if (selected)
                    result.AddRange(model.DrawModelTreeInvert(dev, transform, meshes));
            }
            transform.Pop();
            return result.ToArray();
        }

        public override string Name { get { return name; } }
    }

    public abstract class LevelDefinition
    {
        public abstract void Init(Dictionary<string, string> data, byte act, Device dev);
        public abstract void Render(Device dev, Camera cam);
    }
}