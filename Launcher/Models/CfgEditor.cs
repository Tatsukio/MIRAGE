using System;
using System.Diagnostics;
using System.Windows;

namespace MIRAGE_Launcher.Models
{
    static class CfgEditor
    {
        static CfgEditor()
        {
            Load();
        }

        public static void Load()
        {
            if (!Places.settingsFilePath.IsExist)
            {
                Log.Error(Locale.settingsMissing);
                Environment.Exit(2);
            }
        }

        private static Process Start(string arguments)
        {
            if (!Places.cfgEditorFilePath.IsExist || !Places.settingsFilePath.IsExist) return null;

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Places.cfgEditorFilePath.Full,
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            process.Start();
            process.WaitForExit();
            return process;
        }

        private static bool HandleProcessError(Process process)
        {
            string errorText = Locale.cfgError + " CfgEditor.exe: " + process.StandardError.ReadToEnd();
            if (process.ExitCode != 0)
            {
                if (MessageBox.Show(errorText + "\n" + Locale.askSettingsBackup, null, MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                {
                    Launcher.RestoreSettings();
                }
                return false;
            }
            return true;
        }

        public static bool SetS(string value)
        {
            var process = Start($"-s {value}");
            return process != null && HandleProcessError(process);
        }

        public static string GetS(string value)
        {
            var process = Start($"-g {value}");
            if (process == null) return "";

            if (!HandleProcessError(process)) return null;
            return process.StandardOutput.ReadToEnd().Trim();
        }

        public static string Parse(string filepath)
        {
            var process = Start($"\"{filepath}\"");
            if (process == null) return "";

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            return $"{(string.IsNullOrEmpty(output) ? " | CfgEditor.exe parse failed" : " | CfgEditor.exe parse success")}\n{error}".TrimEnd();
        }
    }
}
