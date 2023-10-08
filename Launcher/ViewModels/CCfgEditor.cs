using MIRAGE_Launcher.ViewModel;
using System;
using System.Diagnostics;
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
            string path = null;
            if (CLauncher.FileFound(CLauncherViewModel._toolsDir, "CfgEditor.exe", ref path))
            {
                Process modConf = new Process();
                modConf.StartInfo.FileName = path;
                modConf.StartInfo.Arguments = "-s " + value;
                modConf.StartInfo.CreateNoWindow = true;
                modConf.StartInfo.UseShellExecute = false;
                modConf.StartInfo.RedirectStandardOutput = true;
                modConf.StartInfo.RedirectStandardError = true;
                modConf.Start();
                modConf.WaitForExit();
                string errorText = CLauncherViewModel._cfgError + " CfgEditor.exe: ";
                string errorDescription = modConf.StandardError.ReadToEnd();

                if (!String.IsNullOrEmpty(errorDescription))
                {
                    errorText += errorDescription;
                }

                if (modConf.ExitCode != 0)
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
                Process modConf = new Process();
                modConf.StartInfo.FileName = path;
                modConf.StartInfo.Arguments = "-g " + value;
                modConf.StartInfo.CreateNoWindow = true;
                modConf.StartInfo.UseShellExecute = false;
                modConf.StartInfo.RedirectStandardOutput = true;
                modConf.StartInfo.RedirectStandardError = true;
                modConf.Start();
                modConf.WaitForExit();
                string errorText = CLauncherViewModel._cfgError + " CfgEditor.exe: ";
                string errorDescription = modConf.StandardError.ReadToEnd();
                string outputDescription = modConf.StandardOutput.ReadToEnd();

                if (!String.IsNullOrEmpty(errorDescription))
                {
                    errorText += errorDescription;
                }

                if (modConf.ExitCode != 0)
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
                    errorDescription = " |-- CfgEditor.exe parse failed: " + errorDescription;
                }

                if (!String.IsNullOrEmpty(outputDescription))
                {
                    outputDescription = " |-- CfgEditor.exe parse failed: " + outputDescription;
                }
                else
                {
                    outputDescription = " |-- CfgEditor.exe parse success";
                }

                return (outputDescription + "\n" + errorDescription).TrimEnd();
            }
            return "CfgEdior.exe was not found in Tools directory.";
        }
    }
}
