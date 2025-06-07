using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Security.Principal;

namespace MIRAGE_Launcher.Helpers
{
    class HealthCheck
    {
        private static bool IsRunAsAdmin()
        {
            try
            {
                return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private static string IsTagesInstalled()
        {
            var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            var key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\SharedDLLs");
            var drivers = new[] { "lirsgt.sys", "atksgt.sys" };

            try
            {
                return string.Join("\n", drivers.Select(driver => $"{driver} {(key.GetValue(@"C:\Windows\system32\DRIVERS\" + driver) != null ? "is installed" : "is not installed")}"));
            }
            catch
            {
                return "Tages check failed";
            }
        }

        private static void HealthCheckFile(string path)
        {
            try
            {
                var outputFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "PWHealthCheck.txt");
                var ext = Path.GetExtension(path);
                if (ext is ".txt" or ".cfg" or ".ini" or ".srf" or ".ttree")
                {
                    var peekRoot = File.ReadLines(path).FirstOrDefault();
                    string result = peekRoot.Contains("Root") ? CfgEditor.Parse(path).Split(Environment.NewLine)[0] : string.Empty;

                    using var outputFile = new StreamWriter(outputFilePath, true);
                    outputFile.WriteLine(path + result);
                }
            }
            catch { }
        }

        private static void CheckDirectory(string targetDirectory)
        {
            string[] fileEntries = Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries)
            {
                HealthCheckFile(Path.GetFullPath(fileName));
            }

            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
            {
                CheckDirectory(subdirectory);
            }
        }

        public static void Check()
        {
            try
            {
                string outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "PWHealthCheck.txt");
                Log.Info($"Wait fot {outputPath} to be created");

                using (StreamWriter outputFile = new(outputPath, false))
                {
                    outputFile.WriteLine("Launcher admin rights are" + (IsRunAsAdmin() ? "" : " not") + " granted");
                    outputFile.WriteLine("\nWin7fix check:");
                    outputFile.WriteLine("\nTages drivers check:");
                    outputFile.WriteLine(IsTagesInstalled());
                }
                CheckDirectory(Places.paraworldDir);

                Log.Info(outputPath + " created");
            }
            catch
            {
                Log.Error($"PWHealthCheck failed");
            }
        }

        public static bool CheckVideoLocale(string currLang)
        {
            string currLangVideoFolder = Path.Combine(Places.paraworldDir, "Data", "Locale", currLang, "Video");
            if (!Directory.Exists(currLangVideoFolder)) return false;

            var baseGameVideo = new[] {
                "1000",
                "1000a",
                "1010",
                "1090",
                "1120",
                "2005",
                "2020",
                "2045",
                "2046",
                "2090",
                "2560",
                "2580",
                "3010",
                "3040",
                "4010",
                "4015",
                "6020",
                "7020",
                "7026",
                "7060",
                "7060b",
                "8090_2" };

            return baseGameVideo.All(videoName => File.Exists(Path.Combine(currLangVideoFolder, "hs_" + videoName + ".bik")));
        }
    }
}
