using Microsoft.Win32;
using MIRAGE_Launcher.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Windows;

namespace MIRAGE_Launcher.ViewModels
{
    internal class CLauncher
    {
        static readonly string _cacheDir = Path.GetFullPath(Path.GetTempPath() + "/SpieleEntwicklungsKombinat/Paraworld");
        static readonly string _appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        static readonly string _settingsDir = Path.GetFullPath(_appDataDir + "/SpieleEntwicklungsKombinat/Paraworld");

        public static void RestoreSettings()
        {
            string backupPath = null;
            if (FileFound(_settingsDir, "Settings_SSSS_backup.cfg", ref backupPath))
            {
                FileInfo settingsBackup = new FileInfo(backupPath);
                if (MessageBox.Show(CLauncherViewModel._resetSettings, CLauncherViewModel._warning, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    settingsBackup.CopyTo(Path.Combine(_settingsDir, "Settings.cfg"), true);
                    //CLauncherViewModel.LoadLang();
                    MessageBox.Show(CLauncherViewModel._resetSettingsSuccess, CLauncherViewModel._warning, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show(CLauncherViewModel._backupMissing, null, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        public static void AskCreateSettingsBackup()
        {
            string path = null;
            if (FileFound(_settingsDir, "Settings.cfg", ref path))
            {
                FileInfo settings = new FileInfo(path);
                string backupPath = Path.Combine(_settingsDir, "Settings_SSSS_backup.cfg");
                if (File.Exists(backupPath))
                {
                    if (MessageBox.Show(CLauncherViewModel._overwriteBackup, CLauncherViewModel._warning, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        settings.CopyTo(backupPath, true);
                        MessageBox.Show(CLauncherViewModel._backupCreated, CLauncherViewModel._warning, MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {

                    }
                }
                else
                {
                    settings.CopyTo(backupPath, true);
                    MessageBox.Show(CLauncherViewModel._backupCreated, CLauncherViewModel._warning, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        public static void CreateSettingsBackup()
        {
            string path = null;
            if (FileFound(_settingsDir, "Settings.cfg", ref path))
            {
                string backupPath = Path.Combine(_settingsDir, "Settings_SSSS_backup.cfg");
                FileInfo settings = new FileInfo(path);
                settings.CopyTo(backupPath, true);
            }
        }

        public static void GetMyPublicIp()
        {
            using (WebClient getIP = new WebClient())
            {
                string myIP = getIP.DownloadString(new Uri("https://ipinfo.io/ip")).Trim();
                string ipPath = _cacheDir + "/paraworld_ip.txt";
                using (StreamWriter writeIP = new StreamWriter(ipPath, false, Encoding.Default))
                {
                    writeIP.Write(myIP);
                }
            }
        }

        public static bool ClearCache()
        {
            string[] cacheExts = { "bin", "ubc", "swd" };
            if (Directory.Exists(_cacheDir))
            {
                IEnumerable<string> CacheFiles = Directory.EnumerateFiles(_cacheDir, "*.*").Where(file => cacheExts.Any(x => file.EndsWith(x, StringComparison.OrdinalIgnoreCase)));
                if (CacheFiles.Any())
                {
                    foreach (string cacheFile in CacheFiles)
                    {
                        try
                        {
                            File.Delete(cacheFile);
                        }
                        catch (IOException)
                        {

                        }
                    }
                    return true;
                }
            }
            return false;
        }

        public static bool FileFound(string path, string filename, ref string outpath)
        {
            if (String.IsNullOrEmpty(path) || String.IsNullOrEmpty(filename))
            {
                MessageBox.Show("Null path or filename", null, MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show("File " + Path.GetFullPath(filename) + " not found.", null, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            else
            {
                MessageBox.Show("Folder " + Path.GetFullPath(path) + " not found.", null, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public static bool SettingsFound(ref string outpath)
        {
            string path = _settingsDir;
            string filename = "Settings.cfg";

            if (String.IsNullOrEmpty(path) || String.IsNullOrEmpty(filename))
            {
                MessageBox.Show("Null Settings.cfg path", null, MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(CLauncherViewModel._settingsMissing, null, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            else
            {
                MessageBox.Show(CLauncherViewModel._settingsMissing, null, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public static void StartPWKiller(bool minimized, bool killAll)
        {
            string path = null;
            if (FileFound(CLauncherViewModel._toolsDir, "PWKiller.exe", ref path))
            {
                ProcessStartInfo pwKiller = new ProcessStartInfo(path);
                if(killAll)
                {
                    pwKiller.Arguments = "-KillAll";
                }
                Process.Start(pwKiller);
                if (minimized)
                {
                    pwKiller.Arguments = "-SSSOffAfterPWExit";
                    pwKiller.WindowStyle = ProcessWindowStyle.Hidden;
                    Process.Start(pwKiller);
                }
            }
        }

        public static void StartDiscordRPC()
        {
            Process[] allDiscordRPC = Process.GetProcessesByName("ParaWorldStatus");
            if (allDiscordRPC.Any())
            {
                foreach (Process discordRPC in allDiscordRPC)
                {
                    discordRPC.Kill();
                }
            }
            string discordRPCPath = Path.Combine(CLauncherViewModel._toolsDir, "ParaWorldStatus");
            if (Directory.Exists(discordRPCPath))
            {
                string discordRPCFile = Path.Combine(discordRPCPath, "ParaWorldStatus.exe");
                if (File.Exists(discordRPCFile))
                {
                    Process.Start(discordRPCFile);
                }
            }
        }

        public static bool EnableSSS(bool enable)
        {
            string path = null;
            if (FileFound(_settingsDir, "Settings.cfg", ref path))
            {
                return CCfgEditor.SetS($"Root/Pest/Server/UseUslCPP {(enable ? 0 : 1)}");
            }
            return false;
        }

        public static string GetMusicInfo(int musicIndex)
        {
            switch (musicIndex)
            {
                default:
                case 1:
                    return "01_maintheme";
                case 2:
                case 41:
                case 43:
                    return "13_plain_icewaste";
                case 3:
                case 65:
                case 67:
                    return "11_plain_jungle_1";
                case 4:
                case 48:
                    return "23_location_arena";
                case 5:
                case 53:
                case 74:
                    return "36_combat_aje_1";
                case 6:
                case 82:
                    return "43_combat_ninigi_2";
                case 7:
                    return "10_plain_heroes";
                case 8:
                case 45:
                case 54:
                case 83:
                    return "35_combat_hu_1";
                case 9:
                    return "04_plain_northland_1";
                case 10:
                case 60:
                    return "41_combat_hu_2";
                case 11:
                    return "16_darkzone_3";
                case 12:
                case 56:
                case 75:
                    return "42_combat_aje_2";
                case 13:
                case 36:
                case 38:
                    return "48_var_combat_aje_1";
                case 14:
                    return "15_darkzone_2";
                case 15:
                case 73:
                case 85:
                    return "17_maintheme_hu";
                case 16:
                case 50:
                case 81:
                    return "47_var_combat_hu_2";
                case 17:
                case 61:
                case 71:
                    return "50_var_combat_seas_1";
                case 18:
                case 66:
                case 78:
                    return "12_plain_jungle_2";
                case 19:
                case 79:
                    return "51_location_seas_temple";
                case 20:
                    return "30_location_scientist_hut";
                case 21:
                case 35:
                case 55:
                case 57:
                    return "39_combat_dinos";
                case 22:
                case 27:
                case 80:
                    return "28_location_walhalla";
                case 23:
                case 24:
                    return "32_location_holycity";
                case 25:
                    return "26_location_water_temple";
                case 26:
                case 29:
                    return "27_location_entry_to_walhalla";
                case 28:
                    return "29_location_aeroplane";
                case 30:
                    return "25_location_pirates";
                case 31:
                    return "20_location_druids";
                case 32:
                    return "06_plain_savannah_1";
                case 33:
                    return "21_location_amazons";
                case 34:
                case 47:
                    return "18_maintheme_aje";
                case 64:
                    return "45_var_plain_savannah_1";
                case 37:
                    return "05_plain_northland_2";
                case 52:
                case 76:
                    return "07_plain_savannah_2";
                case 39:
                    return "40_combat_heroes";
                case 40:
                    return "08_plain_savannah_3";
                case 42:
                    return "14_darkzone_1";
                case 44:
                    return "34_location_prison_island";
                case 46:
                    return "49_var_combat_ninigi_1";
                case 49:
                    return "09_plain_savannah_4";
                case 51:
                    return "37_combat_ninigi_1";
                case 58:
                case 70:
                case 72:
                    return "38_combat_seas_1";
                case 59:
                    return "19_maintheme_ninigi";
                case 62:
                case 63:
                    return "44_var_plain_northland_1";
                case 68:
                    return "33_location_holycity_walls";
                case 69:
                    return "24_location_temple";
                case 77:
                    return "46_var_plain_heroes";
                case 84:
                    return "22_location_the_gate";
            }
        }

        private static bool IsRunAsAdmin()
        {
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    return (new WindowsPrincipal(identity)).IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch
            {
                return false;
            }
        }

        private static string IsTagesInstalled()
        {
            string result = "";

            //Add 32 bit support?
            var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            var key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\SharedDLLs");

            try
            {
                string path = @"C:\Windows\system32\DRIVERS\";
                string[] drivers = { "lirsgt.sys", "atksgt.sys" };

                foreach (string driver in drivers)
                {
                    result += driver + " " + (key.GetValue(path + driver) != null ? " is installed\n" : " is not installed\n");
                }
                return result;
            }
            catch
            {
                return "Tages check failed";
            }
        }

        private static string IsWinFixInstalled()
        {
            string result = "Paraworld.exe not found";

            string path = null;
            if (FileFound(CLauncherViewModel._paraworldBinDir, $"Paraworld.exe", ref path))
            {
                long length = new FileInfo(path).Length;

                result = "Win7fix is" + (length < 3000000 ? "" : " not") + " installed";
            }
            return result;
        }

        private static void ProcessFile(string path)
        {
            try
            {
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "PWHealthCheck.txt"), true))
                {
                    string result = "";
                    string ext = Path.GetExtension(path);
                    if (ext == ".txt" || ext == ".cfg" || ext == ".ttree")
                    {
                        result = CCfgEditor.Parse(path);
                        int indexOfSteam = result.IndexOf(Environment.NewLine);

                        if (indexOfSteam >= 0)
                        {
                            result = result.Remove(indexOfSteam);
                        }
                    }
                    outputFile.WriteLine(path + result);
                }
            }
            catch
            {

            }
        }

        private static void ProcessDirectory(string targetDirectory)
        {
            string[] fileEntries = Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries)
            {
                ProcessFile(Path.GetFullPath(fileName));
            }
            
            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
            {
                ProcessDirectory(subdirectory);
            }
        }

        public static void HealthCheck()
        {
            try
            {
                string outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "PWHealthCheck.txt");
                MessageBox.Show($"Wait fot {outputPath} to be created", "PWHealthCheck", MessageBoxButton.OK, MessageBoxImage.Information);

                using (StreamWriter outputFile = new StreamWriter(outputPath, false))
                {
                    outputFile.WriteLine("Launcher admin rights are" + (IsRunAsAdmin() ? "" : " not") + " granted");
                    outputFile.WriteLine("\nWin7fix check:");
                    outputFile.WriteLine(IsWinFixInstalled());
                    outputFile.WriteLine("\nTages drivers check:");
                    outputFile.WriteLine(IsTagesInstalled());
                }
                ProcessDirectory(CLauncherViewModel._paraworldDir);

                MessageBox.Show(outputPath + " created", "PWHealthCheck", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch
            {
                MessageBox.Show($"PWHealthCheck failed", null, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
