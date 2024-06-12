using MIRAGE_Launcher.ViewModel;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace MIRAGE_Launcher.ViewModels
{
    internal class CCfgEditor
    {
        public static bool SetS(string value)
        {
            string settingsPath = null;
            if (!CLauncher.SettingsFound(ref settingsPath))
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
            if (CLauncher.FileFound(CLauncherViewModel._toolsDir, "CfgEditor.exe", ref path))
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
                string errorText = CLauncherViewModel._cfgError + " CfgEditor.exe: ";
                string errorDescription = cfgEditor.StandardError.ReadToEnd();

                if (!String.IsNullOrEmpty(errorDescription))
                {
                    errorText += errorDescription;
                }

                if (cfgEditor.ExitCode != 0)
                {
                    if (MessageBox.Show(errorText + "\n" + CLauncherViewModel._askSettingsBackup, null, MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                    {
                        CLauncher.RestoreSettings();
                    }
                    return false;
                }
                return true;
            }
            return false;
        }

        public static string GetS(string value)
        {
            string settingsPath = null;
            if (!CLauncher.SettingsFound(ref settingsPath))
            {
                return null;
            }
            string path = null;
            if (CLauncher.FileFound(CLauncherViewModel._toolsDir, "CfgEditor.exe", ref path))
            {
                Process cfgEditor = new Process();
                cfgEditor.StartInfo.FileName = path;
                cfgEditor.StartInfo.Arguments = "-g " + value;
                cfgEditor.StartInfo.CreateNoWindow = true;
                cfgEditor.StartInfo.UseShellExecute = false;
                cfgEditor.StartInfo.RedirectStandardOutput = true;
                cfgEditor.StartInfo.RedirectStandardError = true;
                cfgEditor.Start();
                cfgEditor.WaitForExit();
                string errorText = CLauncherViewModel._cfgError + " CfgEditor.exe: ";
                string errorDescription = cfgEditor.StandardError.ReadToEnd();
                string outputDescription = cfgEditor.StandardOutput.ReadToEnd();

                if (!String.IsNullOrEmpty(errorDescription))
                {
                    errorText += errorDescription;
                }

                if (cfgEditor.ExitCode != 0)
                {
                    if (MessageBox.Show(errorText + "\n" + CLauncherViewModel._askSettingsBackup, null, MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                    {
                        CLauncher.RestoreSettings();
                    }
                    return null;
                }
                return outputDescription.Trim();
            }
            return null;
        }

        public static string Parse(string filepath)
        {
            string path = null;
            if (CLauncher.FileFound(CLauncherViewModel._toolsDir, "CfgEditor.exe", ref path))
            {
                Process cfgEditor = new Process();
                cfgEditor.StartInfo.FileName = path;
                cfgEditor.StartInfo.Arguments = "\"" + filepath + "\"";
                cfgEditor.StartInfo.CreateNoWindow = true;
                cfgEditor.StartInfo.UseShellExecute = false;
                cfgEditor.StartInfo.RedirectStandardOutput = true;
                cfgEditor.StartInfo.RedirectStandardError = true;
                cfgEditor.Start();
                cfgEditor.WaitForExit();
                string errorDescription = cfgEditor.StandardError.ReadToEnd();
                string outputDescription = cfgEditor.StandardOutput.ReadToEnd();

                if (!String.IsNullOrEmpty(errorDescription))
                {
                    errorDescription = " | CfgEditor.exe parse failed: \n" + errorDescription;
                }

                if (!String.IsNullOrEmpty(outputDescription))
                {
                    outputDescription = " | CfgEditor.exe parse failed: \n" + outputDescription;
                }
                else
                {
                    outputDescription = " | CfgEditor.exe parse success";
                }

                return (outputDescription + "\n" + errorDescription).TrimEnd();
            }
            return null;
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
    }
}
