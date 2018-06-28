﻿using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using SonicRetro.SAModel.SAEditorCommon.UI;

namespace ProjectManager
{
    public struct SplitData
    {
        // both of these are relative to the
        public string dataFile;
        public string iniFile;
    }

    public partial class NewProject : Form
    {
        public delegate void ProjectCreationHandler(SA_Tools.Game game, string projectName, string fullProjectPath);
        public event ProjectCreationHandler ProjectCreated;

        public Action CreationCanceled;

        bool sadxIsValid = false;
        bool sa2pcIsValid = false;

        SplitData[] sadxpcsplits = new SplitData[]
        {
            new SplitData() { dataFile="sonic.exe", iniFile = "splitSADX.ini" },
            new SplitData() { dataFile="system/ADV00MODELS.DLL", iniFile = "adv00models.ini" },
            new SplitData() { dataFile="system/ADV01CMODELS.DLL", iniFile = "adv01cmodels.ini" },
            new SplitData() { dataFile="system/ADV01MODELS.DLL", iniFile = "adv01models.ini" },
            new SplitData() { dataFile="system/ADV01MODELS.DLL", iniFile = "adv01models.ini" },
            new SplitData() { dataFile="system/ADV02MODELS.DLL", iniFile = "adv02models.ini" },
            new SplitData() { dataFile="system/ADV03MODELS.DLL", iniFile = "adv03models.ini" },
            new SplitData() { dataFile="system/BOSSCHAOS0MODELS.DLL", iniFile = "bosschaos0models.ini" },
            new SplitData() { dataFile="system/CHAOSTGGARDEN02MR_DAYTIME.DLL", iniFile = "chaostggarden02mr_daytime.ini" },
            new SplitData() { dataFile="system/CHAOSTGGARDEN02MR_EVENING.DLL", iniFile = "chaostggarden02mr_evening.ini" },
            new SplitData() { dataFile="system/CHAOSTGGARDEN02MR_NIGHT.DLL", iniFile = "chaostggarden02mr_night.ini" },
            new SplitData() { dataFile="system/CHAOSTGGARDEN02MR_NIGHT.DLL", iniFile = "chaostggarden02mr_night.ini" }
            // chrmodels and chrmodels_orig are special cases
        };

        SplitData[] sa2pcSplitsDll = new SplitData[]
        {
            new SplitData() { dataFile = "resource/gd_PC/DLL/Win32/Data_DLL.dll", iniFile = "data_dll.ini" },
            new SplitData() { dataFile = "resource/gd_PC/DLL/Win32/Data_DLL.dll", iniFile = "data_dll.ini" },
        };

        /*string[] sa2pcSplitsMDL = new string[]
        {
            "/be resource\\gd_PC\\amymdl.prs resource\\gd_PC\\plcommtn.prs resource\\gd_PC\\amymtn.prs",
            "/be resource\\gd_PC\\bknuckmdl.prs resource\\gd_PC\\plcommtn.prs resource\\gd_PC\\knuckmtn.prs",
            "/be resource\\gd_PC\\brougemdl.prs resource\\gd_PC\\plcommtn.prs resource\\gd_PC\\rougemtn.prs",
            "/be resource\\gd_PC\\chaos0mdl.prs resource\\gd_PC\\plcommtn.prs resource\\gd_PC\\chaos0mtn.prs",
            "/be resource\\gd_PC\\cwalkmdl.prs resource\\gd_PC\\plcommtn.prs resource\\gd_PC\\cwalkmtn.prs",
            "/be resource\\gd_PC\\dwalkmdl.prs resource\\gd_PC\\plcommtn.prs resource\\gd_PC\\dwalkmtn.prs",
            "/be resource\\gd_PC\\eggmdl.prs resource\\gd_PC\\plcommtn.prs resource\\gd_PC\\eggmtn.prs",
            "/be resource\\gd_PC\\ewalk1mdl.prs resource\\gd_PC\\plcommtn.prs resource\\gd_PC\\ewalkmtn.prs",
            "/be resource\\gd_PC\\ewalk2mdl.prs resource\\gd_PC\\plcommtn.prs resource\\gd_PC\\ewalkmtn.prs",
            "/be resource\\gd_PC\\ewalkmdl.prs resource\\gd_PC\\plcommtn.prs resource\\gd_PC\\ewalkmtn.prs",
            "/be resource\\gd_PC\\knuckmdl.prs resource\\gd_PC\\plcommtn.prs resource\\gd_PC\\knuckmtn.prs",
            "/be resource\\gd_PC\\metalsonicmdl.prs resource\\gd_PC\\plcommtn.prs resource\\gd_PC\\metalsonicmtn.prs",
            "/be resource\\gd_PC\\milesmdl.prs resource\\gd_PC\\plcommtn.prs resource\\gd_PC\\milesmtn.prs",
            "/be resource\\gd_PC\\rougemdl.prs resource\\gd_PC\\plcommtn.prs resource\\gd_PC\\rougemtn.prs",
            "/be resource\\gd_PC\\shadow1mdl.prs resource\\gd_PC\\plcommtn.prs resource\\gd_PC\\teriosmtn.prs",
            "/be resource\\gd_PC\\sonic1mdl.prs resource\\gd_PC\\plcommtn.prs resource\\gd_PC\\sonicmtn.prs",
            "/be resource\\gd_PC\\sonicmdl.prs resource\\gd_PC\\plcommtn.prs resource\\gd_PC\\sonicmtn.prs",
            "/be resource\\gd_PC\\sshadowmdl.prs resource\\gd_PC\\plcommtn.prs",
            "/be resource\\gd_PC\\ssonicmdl.prs resource\\gd_PC\\plcommtn.prs",
            "/be resource\\gd_PC\\teriosmdl.prs resource\\gd_PC\\plcommtn.prs resource\\gd_PC\\teriosmtn.prs",
            "/be resource\\gd_PC\\ticalmdl.prs resource\\gd_PC\\plcommtn.prs resource\\gd_PC\\ticalmtn.prs",
            "/be resource\\gd_PC\\twalk1mdl.prs resource\\gd_PC\\plcommtn.prs resource\\gd_PC\\twalkmtn.prs",
            "/be resource\\gd_PC\\twalkmdl.prs resource\\gd_PC\\plcommtn.prs resource\\gd_PC\\twalkmtn.prs"
        };*/

        SplitData sonic2AppSplit = new SplitData() { dataFile = "sonic2app.exe", iniFile = "splitsonic2app.ini" };

        public NewProject()
        {
            InitializeComponent();

            string sadxInvalidReason = "";
            sadxIsValid = Program.CheckSADXPCValid(out sadxInvalidReason);

            string sa2pcInvalidReason = "";
            sa2pcIsValid = Program.CheckSADXPCValid(out sa2pcInvalidReason);

            backgroundWorker1.RunWorkerCompleted += BackgroundWorker1_RunWorkerCompleted;

            SetControls();
        }

        private void BackgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if(ProjectCreated != null)
            {
                ProjectCreationHandler dispatch = ProjectCreated;

                dispatch(GetGameForRadioButtons(), ProjectNameBox.Text, GetOutputFolder());
            }
        }

        void SetControls()
        {
            SADXPCButton.Checked = (sadxIsValid);
            SADXPCButton.Enabled = (sadxIsValid);

            SA2RadioButton.Checked = (false);
            SA2RadioButton.Enabled = (sa2pcIsValid);

            NextButton.Enabled = (ProjectNameBox.Text.Length > 0);
        }

        private void NextButton_Click(object sender, EventArgs e)
        {
            // do any validation
            ProjectNameBox.Enabled = false;
            SA2RadioButton.Enabled = false;
            SADXPCButton.Enabled = false;
            BackButton.Enabled = false;
            NextButton.Enabled = false;
            ControlBox = false;

#if !DEBUG
            backgroundWorker1.RunWorkerAsync();
#endif
#if DEBUG
            backgroundWorker1_DoWork(null, null);
            BackgroundWorker1_RunWorkerCompleted(null, null);
#endif
        }

        private void ProjectNameBox_TextChanged(object sender, EventArgs e)
        {
            SetControls();
        }

        private SA_Tools.Game GetGameForRadioButtons()
        {
            if (SADXPCButton.Checked) return SA_Tools.Game.SADX;
            else if (SADXPCButton.Checked) return SA_Tools.Game.SA2B;
            else return SA_Tools.Game.SA1;
        }            

        private string GetIniFolderForGame(SA_Tools.Game game)
        {
            switch (game)
            {
                case SA_Tools.Game.SA1:
                    return "SA1";

                case SA_Tools.Game.SADX:
                    return "SADXPC";

                case SA_Tools.Game.SA2:
                    return "SA2";

                case SA_Tools.Game.SA2B:
                    return "SA2PC";
                default:
                    break;
            }

            return "";
        }

        private void DoSADXSplit(ProgressDialog progress, string gameFolder, string iniFolder, string outputFolder)
        {
            progress.StepProgress();
            progress.SetStep("Splitting EXE & DLLs");

            foreach (SplitData splitData in sadxpcsplits)
            {
                string datafilename=Path.Combine(gameFolder, splitData.dataFile);
                string inifilename=Path.Combine(iniFolder, splitData.iniFile);
                string projectFolderName = outputFolder;

                progress.StepProgress();
                progress.SetStep("Splitting: " + splitData.dataFile);

#region Validating Inputs
                if (!File.Exists(datafilename))
                {
                    Console.WriteLine("No source file supplied. Aborting.");
                    Console.WriteLine("Press any key to exit.");
                    Console.ReadLine();

                    throw new Exception(ERRORVALUE.NoSourceFile.ToString());
                    //return (int)ERRORVALUE.NoSourceFile;
                }

                if (!File.Exists(inifilename))
                {
                    Console.WriteLine("ini data mapping not found. Aborting.");
                    Console.WriteLine("Press any key to exit.");
                    Console.ReadLine();

                    throw new Exception(ERRORVALUE.NoDataMapping.ToString());
                    //return (int)ERRORVALUE.NoDataMapping;
                }

                if (!Directory.Exists(projectFolderName))
                {
                    // try creating the directory
                    bool created = true;

                    try
                    {
                        // check to see if trailing charcter closes 
                        Directory.CreateDirectory(projectFolderName);
                    }
                    catch
                    {
                        created = false;
                    }

                    if (!created)
                    {
                        // couldn't create directory.
                        Console.WriteLine("Output folder did not exist and couldn't be created.");
                        Console.WriteLine("Press any key to exit.");
                        Console.ReadLine();

                        throw new Exception(ERRORVALUE.InvalidProject.ToString());
                        //return (int)ERRORVALUE.InvalidProject;
                    }
                }
#endregion

                // switch on file extension - if dll, use dll splitter
                System.IO.FileInfo fileInfo = new System.IO.FileInfo(datafilename);

                int result = (fileInfo.Extension.ToLower().Contains("dll")) ? SplitDLL.SplitDLL.SplitDLLFile(datafilename, inifilename, projectFolderName) :
                    Split.Split.SplitFile(datafilename, inifilename, projectFolderName);
            }
        }

        private void DoSA2PCSplit(ProgressDialog progress, string gameFolder, string iniFolder, string outputFolder)
        {
            // split data dlls
#region Split Data DLLs

            progress.StepProgress();
            progress.SetStep("Splitting Data DLLs");

            foreach (SplitData splitData in sa2pcSplitsDll)
            {
                string datafilename = Path.Combine(gameFolder, splitData.dataFile);
                string inifilename = Path.Combine(iniFolder, splitData.iniFile);
                string projectFolderName = outputFolder;

                progress.StepProgress();
                progress.SetStep("Splitting: " + splitData.dataFile);

#region Validating Inputs
                if (!File.Exists(datafilename))
                {
                    Console.WriteLine("No source file supplied. Aborting.");
                    Console.WriteLine("Press any key to exit.");
                    Console.ReadLine();

                    throw new Exception(ERRORVALUE.NoSourceFile.ToString());
                    //return (int)ERRORVALUE.NoSourceFile;
                }

                if (!File.Exists(inifilename))
                {
                    Console.WriteLine("ini data mapping not found. Aborting.");
                    Console.WriteLine("Press any key to exit.");
                    Console.ReadLine();

                    throw new Exception(ERRORVALUE.NoDataMapping.ToString());
                    //return (int)ERRORVALUE.NoDataMapping;
                }

                if (!Directory.Exists(projectFolderName))
                {
                    // try creating the directory
                    bool created = true;

                    try
                    {
                        // check to see if trailing charcter closes 
                        Directory.CreateDirectory(projectFolderName);
                    }
                    catch
                    {
                        created = false;
                    }

                    if (!created)
                    {
                        // couldn't create directory.
                        Console.WriteLine("Output folder did not exist and couldn't be created.");
                        Console.WriteLine("Press any key to exit.");
                        Console.ReadLine();

                        throw new Exception(ERRORVALUE.InvalidProject.ToString());
                        //return (int)ERRORVALUE.InvalidProject;
                    }
                }
#endregion

                // switch on file extension - if dll, use dll splitter
                System.IO.FileInfo fileInfo = new System.IO.FileInfo(datafilename);

                int result = (fileInfo.Extension.ToLower().Contains("dll")) ? SplitDLL.SplitDLL.SplitDLLFile(datafilename, inifilename, projectFolderName) :
                    Split.Split.SplitFile(datafilename, inifilename, projectFolderName);
            }
#endregion

            // run split mdl commands
            // todo: add this

            // split sonic2app
#region Split sonic2app
            {
                progress.StepProgress();
                progress.SetStep("Splitting Sonic2App");

                SplitData splitData = sonic2AppSplit;

                string datafilename = Path.Combine(gameFolder, splitData.dataFile);
                string inifilename = Path.Combine(iniFolder, splitData.iniFile);
                string projectFolderName = outputFolder;

                progress.StepProgress();
                progress.SetStep("Splitting: " + splitData.dataFile);

#region Validating Inputs
                if (!File.Exists(datafilename))
                {
                    Console.WriteLine("No source file supplied. Aborting.");
                    Console.WriteLine("Press any key to exit.");
                    Console.ReadLine();

                    throw new Exception(ERRORVALUE.NoSourceFile.ToString());
                    //return (int)ERRORVALUE.NoSourceFile;
                }

                if (!File.Exists(inifilename))
                {
                    Console.WriteLine("ini data mapping not found. Aborting.");
                    Console.WriteLine("Press any key to exit.");
                    Console.ReadLine();

                    throw new Exception(ERRORVALUE.NoDataMapping.ToString());
                    //return (int)ERRORVALUE.NoDataMapping;
                }

                if (!Directory.Exists(projectFolderName))
                {
                    // try creating the directory
                    bool created = true;

                    try
                    {
                        // check to see if trailing charcter closes 
                        Directory.CreateDirectory(projectFolderName);
                    }
                    catch
                    {
                        created = false;
                    }

                    if (!created)
                    {
                        // couldn't create directory.
                        Console.WriteLine("Output folder did not exist and couldn't be created.");
                        Console.WriteLine("Press any key to exit.");
                        Console.ReadLine();

                        throw new Exception(ERRORVALUE.InvalidProject.ToString());
                        //return (int)ERRORVALUE.InvalidProject;
                    }
                }
#endregion

                // switch on file extension - if dll, use dll splitter
                System.IO.FileInfo fileInfo = new System.IO.FileInfo(datafilename);

                int result = (fileInfo.Extension.ToLower().Contains("dll")) ? SplitDLL.SplitDLL.SplitDLLFile(datafilename, inifilename, projectFolderName) :
                    Split.Split.SplitFile(datafilename, inifilename, projectFolderName);
            }
#endregion
        }

        private string GetOutputFolder()
        {
            return Path.Combine(GetGameFolder(), string.Format("Projects/{0}/", ProjectNameBox.Text));
        }

        private string GetGameFolder()
        {
            return ((SADXPCButton.Checked) ? Program.Settings.SADXPCPath : Program.Settings.SA2PCPath);
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            // we should disable all form controls
            SA_Tools.Game game = GetGameForRadioButtons();

            using (ProgressDialog progress = new ProgressDialog("Creating project"))
            {
                Invoke((Action)progress.Show);

                // create the folder
                progress.StepProgress();
                progress.SetStep("Creating Folder");

                string gameFolder = GetGameFolder();

                string outputFolder = GetOutputFolder();

                Directory.CreateDirectory(outputFolder);

                // get our ini files to split
                string iniFolder = "";
#if DEBUG
                iniFolder = Path.GetDirectoryName(Application.ExecutablePath) + "../../../../Configuration/" + GetIniFolderForGame(game);
#endif

#if !DEBUG
                iniFolder = Path.GetDirectoryName(Application.ExecutablePath) + "../" + GetFolderForGame(game);
#endif

                // we need to run split
                if (game == SA_Tools.Game.SADX) DoSADXSplit(progress, gameFolder, iniFolder, outputFolder);
                else if (game == SA_Tools.Game.SA2B) DoSA2PCSplit(progress, gameFolder, iniFolder, outputFolder);

                Invoke((Action)progress.Close);
            }
        }

        private void NewProject_Shown(object sender, EventArgs e)
        {
            ProjectNameBox.Enabled = true;
            SA2RadioButton.Enabled = sa2pcIsValid;
            SADXPCButton.Enabled = sadxIsValid;
            BackButton.Enabled = true;
            NextButton.Enabled = ProjectNameBox.Text.Length > 0;
            ControlBox = true;

            SetControls();
        }

        private void NavBack()
        {
            CreationCanceled.Invoke();
        }

        private void NewProject_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            NavBack();
            Hide();
        }

        private void BackButton_Click(object sender, EventArgs e)
        {
            NavBack();
            Hide();
        }
    }
}