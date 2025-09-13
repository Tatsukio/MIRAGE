using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace PWKiller
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
            if (process.ExitCode != 0)
            {
                string errorText = "CfgEditor error: " + process.StandardError.ReadToEnd();
                MessageBox.Show(errorText);
                return false;
            }
            return true;
        }

        public static bool SetS(string value)
        {
            var process = Start($"-s {value} {Places.settingsFilePath.Full}");
            return process != null && HandleProcessError(process);
        }

        public static string GetS(string value)
        {
            var process = Start($"-g {value} {Places.settingsFilePath.Full}");
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
