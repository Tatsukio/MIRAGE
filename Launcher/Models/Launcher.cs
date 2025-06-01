using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;
using MIRAGE_Launcher.ViewModels;
using System.Net.Http;

namespace MIRAGE_Launcher.Models
{
    public class Launcher
    {
        public static LauncherViewModel launcherVM;
        public static string[] pwProcesses = { "Paraworld", "PWClient", "PWServer" };
        public static bool kageBunshinNoJutsu = false;
        private static readonly HttpClient httpClient = new();
        public Launcher(LauncherViewModel p_launcherViewModel)
        {
            launcherVM = p_launcherViewModel;
            if (!Places.settingsFilePath.IsExist)
            {
                Application.Current.Shutdown();
                return;
            }

            Task taskGetMyPublicIp = new(() => GetMyPublicIpAsync());
            taskGetMyPublicIp.Start();

            string[] args = Environment.GetCommandLineArgs();
            if (args.Length == 3)
            {
                if (args[1] == "-PWSJoin" && !string.IsNullOrEmpty(args[2]))
                {
                    if (!ReadyToStart())
                    {
                        Application.Current.Shutdown();
                        return;
                    }
                    var serverInfo = args[2].Split('#');
                    var server = serverInfo[0].Trim('-');
                    var serverMods = serverInfo[1].Split('|');
                    var clientMods = AddonMgr.GetEnabledAddons(AddonMgr.Addons).Select(addon => addon.Id).ToArray();
                    var localMods = AddonMgr.GetHiddenAddons(AddonMgr.Addons).Select(addon => addon.Id).ToArray();
                    var missingMods = string.Join(", ", serverMods.Except(clientMods));
                    if (!string.IsNullOrEmpty(missingMods))
                    {
                        Log.Error($"{Locale.discordStatusMissingAddons} {missingMods}");
                        Application.Current.Shutdown();
                        return;
                    }
                    string commandLine = " -enable " + string.Join(" -enable ", serverMods) + " -enable " + string.Join(" -enable ", localMods);
                    Process.Start($"{Places.paraworldBinDir}/{pwProcesses[0]}.exe", $" -autoconnect {server}{commandLine}");
                    StartPWKiller(true, false);
                }
                else
                {
                    MessageBox.Show($"Unknown cmd line args: {string.Join(", ", args)}");
                    Application.Current.Shutdown();
                    return;
                }
            }

            if (Process.GetProcessesByName("MIRAGE Launcher").Length > 1)
            {
                MessageBox.Show(Locale.launcherIsAlreadyRunning, Locale.warning, MessageBoxButton.OK, MessageBoxImage.Warning);
                Application.Current.Shutdown();
                return;
            }

            string ModVersion = Settings.Get("Launcher", "ModVersion");
            string LatestVersionURL = Settings.Get("Launcher", "LatestVersionURL");
            Task taskVersionCheck = new Task(() => CheckMirageVersionAsync(ModVersion, LatestVersionURL));
            taskVersionCheck.Start();

            kageBunshinNoJutsu = Settings.GetB("Launcher", "bMultipleGameCopiesAllowed");
        }

        public static async Task CheckMirageVersionAsync(string version, string latestVersionURL)
        {
            if (!string.IsNullOrEmpty(version) && !string.IsNullOrEmpty(latestVersionURL))
            {
                Version mirageVersion = new Version(version);

                var handler = new HttpClientHandler
                {
                    Proxy = new WebProxy(),
                    UseProxy = true
                };

                using (HttpClient httpClient = new HttpClient(handler))
                {
                    string siteVersionFull = await httpClient.GetStringAsync(latestVersionURL);

                    string[] siteVersion = siteVersionFull.Split('\t', StringSplitOptions.RemoveEmptyEntries);

                    if (siteVersion.Length > 0 && siteVersion[0] == "versioncheck")
                    {
                        string[] siteModVersion = siteVersion[1].Split([' '], StringSplitOptions.RemoveEmptyEntries);

                        if (siteModVersion.Length > 1)
                        {
                            Version mirageSiteVersion = new Version(siteModVersion[1]);
                            switch (mirageVersion.CompareTo(mirageSiteVersion))
                            {
                                case 0:
                                    // MirageVersion == MirageSiteVersion
                                    break;
                                case 1:
                                    // MirageVersion > MirageSiteVersion
                                    break;
                                case -1:
                                    launcherVM.UpdateLogText = "● " + siteVersion[4].Replace(";", "\n● ");
                                    launcherVM.ShowUpdateWindow = true;
                                    break;
                            }
                        }
                    }
                }
            }
        }

        public static BitmapImage GetMenuBg()
        {
            string soul = "pack://application:,,,/Resources";
            string bgImage = (DateTime.Now.Month >= 12 || DateTime.Now.Month <= 2) ? "menu_bg_winter.png" : "menu_bg.png";
            return new BitmapImage(new Uri($"{soul}/{bgImage}"));
        }

        public static BitmapImage GetBgImage(int p_id)
        {
            FileMgr.Filepath bgImageFilePath = new(Places.backgroundsDir, $"background_{p_id}.jpg");
            if (!bgImageFilePath.IsExist)
            {
                Log.Error($"Background image {bgImageFilePath.Full} not found");
                return new BitmapImage();
            }
            return new BitmapImage(new Uri(bgImageFilePath.Full));
        }

        public static string GetMusic(int p_id)
        {
            FileMgr.Filepath musicFilePath = new(Places.musicDir, $"{GetMusicInfo(p_id)}.mp3");
            if (!musicFilePath.IsExist)
            {
                Log.Error($"Music {musicFilePath.Full} not found");
            }
            return musicFilePath.Full;
        }

        public static bool ReadyToStart()
        {
            if (!kageBunshinNoJutsu)
            {
                foreach (string pwProcess in pwProcesses)
                {
                    if (Process.GetProcessesByName(pwProcess).Length != 0)
                    {
                        if (MessageBox.Show(Locale.pwIsAlreadyRunning, null, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            StartPWKiller(false, false);
                        }
                        return false;
                    }

                    FileMgr.Filepath pwExeFilePath = new(Places.paraworldBinDir, $"{pwProcess}.exe");
                    if (!pwExeFilePath.IsExist)
                    {
                        return false;
                    }
                }
            }

            if (!EnableSSS(true))
            {
                return false;
            }

            var missingAddons = AddonMgr.GetMissingAddons();
            if (missingAddons.Count != 0)
            {
                Log.Error(AddonMgr.GetMissingAddonsMsg(missingAddons));
                return false;
            }

            if (!CheckVideoLocale(Locale.CurrentLang))
            {
                if (!Settings.GetB("Launcher", "bSkipVideoCheck"))
                {
                    Log.Warn($"You have selected the {Locale.CurrentLang} localization, but it does not have translated videos, so uk videos will be used.\n\nThis message will be displayed only once");
                    Settings.Set("Launcher/bSkipVideoCheck", "true");
                }
            }

            ClearCache();

            if (launcherVM.IsMusicPlaying)
            {
                launcherVM.OnToggleMusic();
            }

            Directory.SetCurrentDirectory(Places.paraworldBinDir);

            if (Settings.GetB("Launcher", "bEnableDiscordStatus"))
            {
                StartDiscordRPC();
            }

            return true;
        }

        public static void RestoreSettings()
        {
            if (!Places.launcherSettingsBackupFilePath.IsExist)
            {
                Log.Error(Locale.backupMissing);
                return;
            }

            if (MessageBox.Show(Locale.resetSettings, Locale.warning, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                FileInfo settingsBackup = new(Places.settingsBackupFilePath.Full);
                settingsBackup.CopyTo(Places.settingsFilePath.Full, true);

                if (Places.launcherSettingsBackupFilePath.IsExist)
                {
                    FileInfo launcherSettings = new(Places.launcherSettingsBackupFilePath.Full);
                    launcherSettings.CopyTo(Places.launcherSettingsFilePath.Full, true);
                }

                CfgEditor.Load();
                Settings.Load();
                AddonMgr.Load();
                Locale.Load();
                AddonMgrViewModel.Load();
                launcherVM.Load();
                Log.Info(Locale.resetSettingsSuccess);
            }
        }

        public static void AskCreateSettingsBackup()
        {
            if (!Places.settingsFilePath.IsExist) return;

            FileInfo settings = new(Places.settingsFilePath.Full);
            if (Places.settingsBackupFilePath.IsExist)
            {
                if (MessageBox.Show(Locale.overwriteBackup, Locale.warning, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    settings.CopyTo(Places.settingsBackupFilePath.Full, true);
                    Log.Info(Locale.backupCreated);
                }
                else
                {

                }
            }
            else
            {
                settings.CopyTo(Places.settingsBackupFilePath.Full, true);
                Log.Info(Locale.backupCreated);
            }
        }

        public static async Task GetMyPublicIpAsync()
        {
            try
            {
                string myIP = await httpClient.GetStringAsync("https://ipinfo.io/ip");
                myIP = myIP.Trim();
                string ipPath = Path.Combine(Places.cacheDir, "paraworld_ip.txt");

                using StreamWriter writeIP = new(ipPath, false, Encoding.Default);
                await writeIP.WriteAsync(myIP);
            }
            catch { }
        }

        public static bool ClearCache()
        {
            if (!Directory.Exists(Places.cacheDir)) return false;
            var cacheExts = new[] { "bin", "ubc", "swd" };
            var cacheFiles = Directory.EnumerateFiles(Places.cacheDir, "*.*").Where(file => cacheExts.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase)));
            foreach (var cacheFile in cacheFiles)
            {
                try
                {
                    File.Delete(cacheFile);
                }
                catch { }
            }
            return cacheFiles.Any();
        }

        public static void StartPWKiller(bool minimized, bool killAll)
        {
            if (Places.pwKillerFilePath.IsExist)
            {
                ProcessStartInfo pwKiller = new ProcessStartInfo(Places.pwKillerFilePath.Full);
                if (killAll)
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
            if (allDiscordRPC.Length != 0)
            {
                foreach (Process discordRPC in allDiscordRPC)
                {
                    discordRPC.Kill();
                }
            }
            string discordRPCPath = Path.Combine(Places.toolsDir, "ParaWorldStatus");
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
            return CfgEditor.SetS($"Root/Pest/Server/UseUslCPP {(enable ? 0 : 1)}");
        }

        public static string GetMusicInfo(int musicIndex)
        {
            return musicIndex switch
            {
                2 or 41 or 43 => "13_plain_icewaste",
                3 or 65 or 67 => "11_plain_jungle_1",
                4 or 48 => "23_location_arena",
                5 or 53 or 74 => "36_combat_aje_1",
                6 or 82 => "43_combat_ninigi_2",
                7 => "10_plain_heroes",
                8 or 45 or 54 or 83 => "35_combat_hu_1",
                9 => "04_plain_northland_1",
                10 or 60 => "41_combat_hu_2",
                11 => "16_darkzone_3",
                12 or 56 or 75 => "42_combat_aje_2",
                13 or 36 or 38 => "48_var_combat_aje_1",
                14 => "15_darkzone_2",
                15 or 73 or 85 => "17_maintheme_hu",
                16 or 50 or 81 => "47_var_combat_hu_2",
                17 or 61 or 71 => "50_var_combat_seas_1",
                18 or 66 or 78 => "12_plain_jungle_2",
                19 or 79 => "51_location_seas_temple",
                20 => "30_location_scientist_hut",
                21 or 35 or 55 or 57 => "39_combat_dinos",
                22 or 27 or 80 => "28_location_walhalla",
                23 or 24 => "32_location_holycity",
                25 => "26_location_water_temple",
                26 or 29 => "27_location_entry_to_walhalla",
                28 => "29_location_aeroplane",
                30 => "25_location_pirates",
                31 => "20_location_druids",
                32 => "06_plain_savannah_1",
                33 => "21_location_amazons",
                34 or 47 => "18_maintheme_aje",
                64 => "45_var_plain_savannah_1",
                37 => "05_plain_northland_2",
                52 or 76 => "07_plain_savannah_2",
                39 => "40_combat_heroes",
                40 => "08_plain_savannah_3",
                42 => "14_darkzone_1",
                44 => "34_location_prison_island",
                46 => "49_var_combat_ninigi_1",
                49 => "09_plain_savannah_4",
                51 => "37_combat_ninigi_1",
                58 or 70 or 72 => "38_combat_seas_1",
                59 => "19_maintheme_ninigi",
                62 or 63 => "44_var_plain_northland_1",
                68 => "33_location_holycity_walls",
                69 => "24_location_temple",
                77 => "46_var_plain_heroes",
                84 => "22_location_the_gate",
                _ => "01_maintheme",
            };
        }

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

        private static void HealthCheckDirectory(string targetDirectory)
        {
            string[] fileEntries = Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries)
            {
                HealthCheckFile(Path.GetFullPath(fileName));
            }

            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
            {
                HealthCheckDirectory(subdirectory);
            }
        }

        public static void HealthCheck()
        {
            try
            {
                string outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "PWHealthCheck.txt");
                Log.Info($"Wait fot {outputPath} to be created");

                using (StreamWriter outputFile = new StreamWriter(outputPath, false))
                {
                    outputFile.WriteLine("Launcher admin rights are" + (IsRunAsAdmin() ? "" : " not") + " granted");
                    outputFile.WriteLine("\nWin7fix check:");
                    outputFile.WriteLine("\nTages drivers check:");
                    outputFile.WriteLine(IsTagesInstalled());
                }
                HealthCheckDirectory(Places.paraworldDir);

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
