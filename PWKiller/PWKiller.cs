using System;
using System.IO;
using System.Xml;
using System.Linq;
using System.Diagnostics;
using System.Windows.Forms;

namespace PWKiller
{
    public partial class PWKillerMain : Form
    {
        static readonly string _appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        static readonly string _settingsDir = Path.GetFullPath(_appDataDir + "/SpieleEntwicklungsKombinat/Paraworld");
        static string _currLang = "";
        public PWKillerMain(string[] args)
        {
            InitializeComponent();
            if (args.Contains("-SSSOffAfterPWExit"))
            {
                CheckPW();
            }
            if (args.Contains("-KillAll"))
            {
                ParaWorldExited();
            }
            else
            {
                _currLang = GetS("Root/Global/Language");
                if (LoadLocale())
                {
                    PWKillerTitleText.Text = Translate("PWKillerTitleText");
                    PWKillerButtonText.Text = Translate("PWKillerButtonText");
                }
            }
        }

        public static string GetS(string value)
        {
            string settingsPath = null;
            if (!FileFound(_settingsDir, "Settings.cfg", ref settingsPath))
            {
                return null;
            }
            string path = null;
            if (FileFound(_toolsDir, "CfgEditor.exe", ref path))
            {
                Process modConf = new Process();
                modConf.StartInfo.FileName = path;
                modConf.StartInfo.Arguments = "-g " + value;
                modConf.StartInfo.CreateNoWindow = true;
                modConf.StartInfo.UseShellExecute = false;
                modConf.StartInfo.RedirectStandardOutput = true;
                modConf.StartInfo.RedirectStandardError = true;
                modConf.Start();
                modConf.WaitForExit();
                string outputDescription = modConf.StandardOutput.ReadToEnd();
                if (modConf.ExitCode != 0)
                {
                    return null;
                }
                return outputDescription.Trim();
            }
            return null;
        }

        public static bool SetS(string value)
        {
            string settingsPath = null;
            if (!FileFound(_settingsDir, "Settings.cfg", ref settingsPath))
            {
                return false;
            }

            if (FileLocked(settingsPath))
            {
                System.Threading.Thread.Sleep(2000);
                if (FileLocked(settingsPath))
                {
                    return false;
                }
            }

            string path = null;
            if (FileFound(_toolsDir, "CfgEditor.exe", ref path))
            {
                Process cfgEditor = new Process();
                cfgEditor.StartInfo.FileName = path;
                cfgEditor.StartInfo.Arguments = "-s " + value;
                cfgEditor.StartInfo.CreateNoWindow = true;
                cfgEditor.StartInfo.UseShellExecute = false;
                cfgEditor.StartInfo.RedirectStandardOutput = true;
                cfgEditor.StartInfo.RedirectStandardError = true;
                cfgEditor.Start();
                cfgEditor.WaitForExit();

                if (cfgEditor.ExitCode != 0)
                {
                    return false;
                }
                return true;
            }
            return false;
        }

        public static bool FileFound(string path, string filename, ref string outpath)
        {
            if (String.IsNullOrEmpty(path) || String.IsNullOrEmpty(filename))
            {
                return false;
            }
            if (Directory.Exists(path))
            {
                string file = Path.Combine(path, filename);
                if (File.Exists(file))
                {
                    outpath = file;
                    return true;
                }
                return false;
            }
            else
            {
                return false;
            }
        }

        protected static bool FileLocked(string path)
        {
            var file = new FileInfo(path);
            try
            {
                using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }

            //file is not locked
            return false;
        }

        static readonly XmlDocument _locale = new XmlDocument();

        static readonly string _toolsDir = AppDomain.CurrentDomain.BaseDirectory;
        static readonly string[] _processes = { "Paraworld", "PWClient", "PWServer" };

        void MainButton_Click(object sender, EventArgs e)
        {
            KillPW();
            Process me = Process.GetCurrentProcess();
            me.Kill();
        }

        public static bool LoadLocale()
        {
            if (String.IsNullOrEmpty(_currLang))
            {
                return false;
            }
            string path = null;
            if (!FileFound(_toolsDir + "/MIRAGE Launcher/", "LauncherDB.xml", ref path))
            {
                return false;
            }
            _locale.Load(path);
            return true;
        }
        public static string Translate(string text)
        {
            string textPath = "/LauncherDB/Locale/" + _currLang + "/";
            return _locale.DocumentElement.SelectSingleNode(textPath + text).InnerText;
        }
        public static void CheckPW()
        {
            System.Threading.Thread.Sleep(1000);
            while (true)
            {
                if (!Process.GetProcessesByName(_processes[0]).Any())
                {
                    if (!Process.GetProcessesByName(_processes[1]).Any())
                    {
                        break;
                    }
                }
                System.Threading.Thread.Sleep(1000);
            }
            ParaWorldExited();
        }
        public static void ParaWorldExited()
        {
            if (File.Exists(_toolsDir + "/CfgEditor.exe"))
            {
                SetS("Root/Pest/Server/UseUslCPP 1");
            }
            KillPW();
            KillPWKiller();
        }
        public static void KillPW()
        {
            for (int i = 0; i < 3; i++)
            {
                Process[] paraworldProcesses = Process.GetProcessesByName(_processes[i]);
                foreach (Process paraworldProcess in paraworldProcesses)
                {
                    paraworldProcess.Kill();
                }
            }
            if (Process.GetProcessesByName("ParaWorldStatus").Any())
            {
                Process PWS = Process.GetProcessesByName("ParaWorldStatus").First();
                PWS.Kill();
            }
        }
        public static void KillPWKiller()
        {
            Process me = Process.GetCurrentProcess();
            Process[] AllPWKiller = Process.GetProcessesByName("PWKiller");
            foreach (Process PWKiller in AllPWKiller)
            {
                if (PWKiller.Id != me.Id)
                {
                    PWKiller.Kill();
                }
            }
            me.Kill();
        }
    }
}
