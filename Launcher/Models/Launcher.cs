using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;
using MIRAGE_Launcher.ViewModels;
using System.Net.Http;
using MIRAGE_Launcher.Helpers;

namespace MIRAGE_Launcher.Models
{
    public class Launcher
    {
        private static LauncherViewModel launcherVM;
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

            if (!ProcessCommandLineArgs())
            {
                Application.Current.Shutdown();
            }

            if (Process.GetProcessesByName("MIRAGE Launcher").Length > 1)
            {
                Log.Warn(Locale.launcherIsAlreadyRunning);
                Application.Current.Shutdown();
                return;
            }

            Task taskVersionCheck = new(() => CheckMirageVersionAsync());
            taskVersionCheck.Start();
        }

        public static bool ProcessCommandLineArgs()
        {
            try
            {
                var cmd = Environment.GetCommandLineArgs();
                if (cmd.Length == 1)
                {
                    return true;
                }
                else if (cmd.Length == 3 && cmd[1] == "-PWSJoin" && !string.IsNullOrEmpty(cmd[2]))
                {
                    StartViaDiscordRPC(cmd[2]);
                    return true;
                }
                Log.Error($"Invalid command line arguments: {string.Join(", ", cmd)}");
                return false;
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurred while processing command line arguments: {ex.Message}");
                return false;
            }
        }

        public static void StartViaDiscordRPC(string p_serverInfo)
        {
            if (!ReadyToStart())
            {
                Application.Current.Shutdown();
                return;
            }

            var serverInfo = p_serverInfo.Split('#');

            var serverAddress = serverInfo[0].Trim('-');
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

            Process.Start($"{Places.paraworldBinDir}/{Places.pwProcesses[0]}.exe", $" -autoconnect {serverAddress}{AddonMgr.GetCmdLine(serverMods.Concat(localMods).ToArray())}");
            StartPWKiller(true, false);
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
            if (!Settings.GetB("Launcher", "bMultipleGameCopiesAllowed"))
            {
                foreach (string pwProcess in Places.pwProcesses)
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

            if (!CfgEditor.SetS($"Root/Pest/Server/UseUslCPP 0")) return false;

            var missingAddons = AddonMgr.GetAddonsDependencies(true);
            if (missingAddons.Count != 0)
            {
                Log.Error(AddonMgr.GetMissingAddonsMsg(missingAddons, true));
                return false;
            }

            var excludedAddons = AddonMgr.GetAddonsDependencies(false);
            if (excludedAddons.Count != 0)
            {
                Log.Error(AddonMgr.GetMissingAddonsMsg(excludedAddons, false));
                return false;
            }

            if (!HealthCheck.CheckVideoLocale(Locale.CurrentLang))
            {
                if (!Settings.GetB("Launcher", "bSkipVideoCheck"))
                {
                    Log.Warn($"You have selected the {Locale.CurrentLang} localization, but it does not have translated videos, so uk videos will be used.\n\nThis message will be displayed only once");
                    Settings.Set("Launcher/bSkipVideoCheck", "true");
                }
            }

            if (!ClearCache()) return false;

            if (launcherVM.IsMusicPlaying)
            {
                launcherVM.OnToggleMusic();
            }

            if (Settings.GetB("Launcher", "bEnableDiscordStatus"))
            {
                StartDiscordRPC();
            }

            Directory.SetCurrentDirectory(Places.paraworldBinDir);
            return true;
        }

        public static void RestoreSettings()
        {
            if (!Places.launcherSettingsBackupFilePath.IsExist || !Places.launcherSettingsBackupFilePath.IsExist)
            {
                Log.Error(Locale.backupMissing);
                return;
            }

            if (MessageBox.Show(Locale.resetSettings, Locale.warning, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                FileInfo settingsBackup = new(Places.settingsBackupFilePath.Full);
                settingsBackup.CopyTo(Places.settingsFilePath.Full, true);

                FileInfo launcherSettings = new(Places.launcherSettingsBackupFilePath.Full);
                launcherSettings.CopyTo(Places.launcherSettingsFilePath.Full, true);

                CfgEditor.Load();
                Settings.Load();
                AddonMgr.Load();
                AddonMgrViewModel.Load();
                launcherVM.Load();
                Log.Info(Locale.resetSettingsSuccess);
            }
        }

        public static void AskCreateSettingsBackup()
        {
            if (!Places.settingsFilePath.IsExist) return;

            FileInfo settings = new(Places.settingsFilePath.Full);
            if (Places.settingsBackupFilePath.IsExist && MessageBox.Show(Locale.overwriteBackup, Locale.warning, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                settings.CopyTo(Places.settingsBackupFilePath.Full, true);
                Log.Info(Locale.backupCreated);
            }
        }

        public static async Task CheckMirageVersionAsync()
        {
            string modVersion = Settings.Get("Launcher", "ModVersion");
            string modLatestVersionURL = Settings.Get("Launcher", "LatestVersionURL");
            if (string.IsNullOrEmpty(modVersion) || string.IsNullOrEmpty(modLatestVersionURL)) return;

            string siteVersionFull = "";
            try
            {
                siteVersionFull = await httpClient.GetStringAsync(modLatestVersionURL);
            }
            catch (HttpRequestException ex)
            {
                Log.Dbug($"Failed to check updates: {ex} StatusCode = {ex.StatusCode}");
                return;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to check updates: {ex}");
                return;
            }

            string[] siteVersion = siteVersionFull.Split('\t', StringSplitOptions.RemoveEmptyEntries);
            if (siteVersion.Length <= 0 || siteVersion[0] != "versioncheck") return;

            string[] siteModVersion = siteVersion[1].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (siteModVersion.Length < 1) return;

            Version mirageVersion = new(modVersion);
            Version mirageSiteVersion = new(siteModVersion[1]);
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

        public static async Task GetMyPublicIpAsync()
        {
            try
            {
                string myIP = await httpClient.GetStringAsync("https://ipinfo.io/ip");
                myIP = myIP.Trim();
                string ipPath = Path.Combine(Places.cacheDir, "paraworld_ip.txt");
                Directory.CreateDirectory(Places.cacheDir);
                using StreamWriter writeIP = new(ipPath, false, Encoding.Default);
                await writeIP.WriteAsync(myIP);
            }
            catch (HttpRequestException ex)
            {
                Log.Dbug($"Failed to get own public ip: {ex} StatusCode = {ex.StatusCode}");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to get own public ip: {ex}");
            }
        }

        public static bool ClearCache()
        {
            if (!Directory.Exists(Places.cacheDir)) return true;

            var cacheFiles = Directory.EnumerateFiles(Places.cacheDir, "*.*").Where(file => Places.cacheExt.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase)));
            foreach (var cacheFile in cacheFiles)
            {
                try
                {
                    File.Delete(cacheFile);
                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to clear cache file {cacheFile}: {ex.Message}");
                    break;
                }
            }

            return !cacheFiles.Any();
        }

        public static void StartPWKiller(bool minimized, bool killAll)
        {
            if (Places.pwKillerFilePath.IsExist)
            {
                ProcessStartInfo pwKiller = new(Places.pwKillerFilePath.Full);
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

        static bool KillProcesses(string p_name)
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(p_name);
                foreach (Process process in processes)
                {
                    process.Kill();
                    process.WaitForExit();
                    Log.Info($"Process {process.ProcessName} with ID {process.Id} was killed");
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to kill {p_name} process: {ex.Message}");
            }
            return false;
        }


        public static void StartDiscordRPC()
        {
            if (!KillProcesses("ParaWorldStatus")) return;
            if (!Places.discordRPCFilePath.IsExist) return;
            Process.Start(Places.discordRPCFilePath.Full);
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
    }
}
