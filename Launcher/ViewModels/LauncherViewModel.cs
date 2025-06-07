using MIRAGE_Launcher.Helpers;
using MIRAGE_Launcher.Models;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MIRAGE_Launcher.ViewModels
{
    public class LauncherViewModel : ViewModelBase
    {
        public readonly MediaPlayer mediaPlayer = new();
        public AddonMgrViewModel AddonMgrVM { get; set; }
        public LauncherViewModel()
        {
            Load();
        }

        public void Load()
        {
            MenuBackground = Launcher.GetMenuBg();

            mediaPlayer.Volume = 0.25;
            mediaPlayer.MediaEnded += (sender, eventArgs) => GenerateNewUI();
            IsMusicPlaying = Settings.GetB("Launcher", "bMusicOnStartup");
            GenerateNewUI();

            LangsDropdownMenu = Locale.ToObservableCollection(Locale.GetAvailableLangs());
            LangsDropdownMenuSelectedItem = LangsDropdownMenu.FirstOrDefault(e => e == Locale.CurrentLang);

            StartParaworldCmd = new Cmd(OnStartParaworld);
            StartSDKCmd = new Cmd(OnStartSDK);
            StartServerCmd = new Cmd(OnStartServer);
            SetLanguageCmd = new Cmd(OnSetLanguage);
            ToggleMusicCmd = new Cmd(OnToggleMusic);
            PlayNextTrackCmd = new Cmd(OnPlayNextTrack);
            OpenScrFolderCmd = new Cmd(OnOpenScrFoldere);
            OpenSettingsFolderCmd = new Cmd(OnOpenSettingsFolder);
            StartPWKillerCmd = new Cmd(OnStartPWKiller);
            TogglePWToolCmd = new Cmd(OnTogglePWTool);

            RestoreSettingsCmd = new Cmd(OnRestoreSettings);
            CreateSettingsBackupCmd = new Cmd(OnCreateSettingsBackup);
            OpenCreditsCmd = new Cmd(OnOpenCredits);

            UninstallCmd = new Cmd(OnUninstall);
            ExitCmd = new Cmd(OnExit);
            HealthCheckCmd = new Cmd(OnHealthCheck);

            OpenUpdatePageCmd = new Cmd(OnOpenUpdatePage);
            OpenModdbCmd = new Cmd(OnOpenModdb);
            OpenDiscordCmd = new Cmd(OnOpenDiscord);
            OpenGitCmd = new Cmd(OnOpenGit);

            Launcher launcher = new(this);
            AddonMgrVM = new AddonMgrViewModel();
        }

        private void GenerateNewUI()
        {
            int rndId = new Random().Next(1, 85);

            LauncherBackground = Launcher.GetBgImage(rndId);

            mediaPlayer.Open(new Uri(Launcher.GetMusic(rndId), UriKind.Relative));
            if (IsMusicPlaying)
            {
                mediaPlayer.Play();
            }
        }

        private void LoadLocale()
        {
            Locale.Load();

            string modNameText = Locale.Translate("ModNameText");
            string modBuildText = Locale.Translate("ModBuildText");
            string modVersion = Settings.Get("Launcher", "ModVersion");
            string modBuild = Settings.Get("Launcher", "ModBuild");
            MenuTitleText = $"{modNameText} {modVersion} {modBuildText} {modBuild}";

            UpdateTitleText = Locale.Translate("UpdateTitleText");
            StartModText = Locale.Translate("StartModText");
            StartSDKText = Locale.Translate("StartSDKText");
            StartServerText = Locale.Translate("StartServerText");
            CurrentLangText = Locale.Translate("CurrentLangText");
            TurnMusicOnText = Locale.Translate("TurnMusicOnText");
            TurnMusicOffText = Locale.Translate("TurnMusicOffText");
            OpenScrFolderText = Locale.Translate("OpenScrFolderText");
            OpenSettingsFolderText = Locale.Translate("OpenSettingsFolderText");
            StartPWKillerText = Locale.Translate("StartPWKillerText");
            OpenPWToolText = Locale.Translate("OpenPWToolText");
            ClosePWToolText = Locale.Translate("ClosePWToolText");
            RestoreSettingsText = Locale.Translate("RestoreSettingsText");
            CreateSettingsBackupText = Locale.Translate("CreateSettingsBackupText");
            UninstallText = Locale.Translate("UninstallText");
            ExitText = Locale.Translate("ExitText");
            ToggleMusicText = IsMusicPlaying ? TurnMusicOffText : TurnMusicOnText;
            TogglePWToolText = IsPwToolOpen ? ClosePWToolText : OpenPWToolText;
        }

        public ICommand StartParaworldCmd { get; set; }
        public ICommand StartSDKCmd { get; set; }
        public ICommand StartServerCmd { get; set; }
        public ICommand SetLanguageCmd { get; set; }
        public ICommand ToggleMusicCmd { get; set; }
        public ICommand PlayNextTrackCmd { get; set; }
        public ICommand OpenScrFolderCmd { get; set; }
        public ICommand OpenSettingsFolderCmd { get; set; }
        public ICommand StartPWKillerCmd { get; set; }
        public ICommand TogglePWToolCmd { get; set; }
        public ICommand RestoreSettingsCmd { get; set; }
        public ICommand CreateSettingsBackupCmd { get; set; }
        public ICommand OpenCreditsCmd { get; set; }
        public ICommand UninstallCmd { get; set; }
        public ICommand ExitCmd { get; set; }
        public ICommand HealthCheckCmd { get; set; }
        public ICommand OpenUpdatePageCmd { get; set; }
        public ICommand OpenModdbCmd { get; set; }
        public ICommand OpenDiscordCmd { get; set; }
        public ICommand OpenGitCmd { get; set; }


        private string currentLangText = "Current lang:";
        public string CurrentLangText
        {
            get => currentLangText;
            set => Set(ref currentLangText, value);
        }

        private ObservableCollection<string> langsDropdownMenu = [];
        public ObservableCollection<string> LangsDropdownMenu
        {
            get => langsDropdownMenu;
            set => Set(ref langsDropdownMenu, value);
        }

        private string langsDropdownMenuSelectedItem;
        public string LangsDropdownMenuSelectedItem
        {
            get => langsDropdownMenuSelectedItem;
            set
            {
                if (langsDropdownMenuSelectedItem == value) return;
                if (CfgEditor.SetS("Root/Global/Language " + value.ToLower()))
                {
                    Set(ref langsDropdownMenuSelectedItem, value);
                    Locale.CurrentLang = value;
                    LoadLocale();
                }
                else
                {
                    Log.Error($"Failed to set locale in Settings.cfg to '{value}'");
                }
            }
        }

        private BitmapImage launcherBackground;
        public BitmapImage LauncherBackground
        {
            get => launcherBackground;
            set => Set(ref launcherBackground, value);
        }

        private BitmapImage menuBackground;
        public BitmapImage MenuBackground
        {
            get => menuBackground;
            set => Set(ref menuBackground, value);
        }

        private bool showUpdateWindow;
        public bool ShowUpdateWindow
        {
            get => showUpdateWindow;
            set => Set(ref showUpdateWindow, value);
        }

        private string updateLogText;
        public string UpdateLogText
        {
            get => updateLogText;
            set => Set(ref updateLogText, value);
        }

        private string updateTitleText = "New update available!";
        public string UpdateTitleText
        {
            get => updateTitleText;
            set => Set(ref updateTitleText, value);
        }

        private string menuTitleText = "Launcher Menu";
        public string MenuTitleText
        {
            get => menuTitleText;
            set => Set(ref menuTitleText, value);
        }

        private string startModText = "Start ParaWorld";
        public string StartModText
        {
            get => startModText;
            set => Set(ref startModText, value);
        }

        private string startSDKText = "Start Mapeditor";
        public string StartSDKText
        {
            get => startSDKText;
            set => Set(ref startSDKText, value);
        }

        private string startServerText = "Start Dedicated Server";
        public string StartServerText
        {
            get => startServerText;
            set => Set(ref startServerText, value);
        }

        private string turnMusicOnText = "Turn Music On";
        public string TurnMusicOnText
        {
            get => turnMusicOnText;
            set => Set(ref turnMusicOnText, value);
        }

        private string turnMusicOffText = "Turn Music Off";
        public string TurnMusicOffText
        {
            get => turnMusicOffText;
            set => Set(ref turnMusicOffText, value);
        }

        private string toggleMusicText = "Turn Music Off";
        public string ToggleMusicText
        {
            get => toggleMusicText;
            set => Set(ref toggleMusicText, value);
        }

        private string openScrFolderText = "Screenshots folder";
        public string OpenScrFolderText
        {
            get => openScrFolderText;
            set => Set(ref openScrFolderText, value);
        }

        private string openSettingsFolderText = "Settings Folder";
        public string OpenSettingsFolderText
        {
            get => openSettingsFolderText;
            set => Set(ref openSettingsFolderText, value);
        }

        private string startPWKillerText = "Kill PW Processes";
        public string StartPWKillerText
        {
            get => startPWKillerText;
            set => Set(ref startPWKillerText, value);
        }

        private string openPWToolText = "Open PWTool";
        public string OpenPWToolText
        {
            get => openPWToolText;
            set => Set(ref openPWToolText, value);
        }

        private string closePWToolText = "Close PWTool";
        public string ClosePWToolText
        {
            get => closePWToolText;
            set => Set(ref closePWToolText, value);
        }

        private string togglePWToolText = "Open PWTool";
        public string TogglePWToolText
        {
            get => togglePWToolText;
            set => Set(ref togglePWToolText, value);
        }

        private string sssOnText = "Server Scripts On";
        public string SSSOnText
        {
            get => sssOnText;
            set => Set(ref sssOnText, value);
        }

        private string sssOffText = "Server Scripts Off";
        public string SSSOffText
        {
            get => sssOffText;
            set => Set(ref sssOffText, value);
        }

        private string restoreSettingsText = "Restore Game Settings";
        public string RestoreSettingsText
        {
            get => restoreSettingsText;
            set => Set(ref restoreSettingsText, value);
        }

        private string createSettingsBackupText = "Create Settings Backup";
        public string CreateSettingsBackupText
        {
            get => createSettingsBackupText;
            set => Set(ref createSettingsBackupText, value);
        }

        private string uninstallText = "Uninstall MIRAGE";
        public string UninstallText
        {
            get => uninstallText;
            set => Set(ref uninstallText, value);
        }

        private string exitText = "Exit Launcher";
        public string ExitText
        {
            get => exitText;
            set => Set(ref exitText, value);
        }

        private void OnStartParaworld()
        {
            if (!Launcher.ReadyToStart()) return;
            Process.Start($"{Places.paraworldBinDir}/{Places.pwProcesses[0]}.exe", AddonMgr.GetCmdLine());
            Launcher.StartPWKiller(true, false);
        }


        private void OnStartSDK()
        {
            if (!Launcher.ReadyToStart()) return;
            Process.Start($"{Places.paraworldBinDir}/{Places.pwProcesses[1]}.exe", " -leveled" + AddonMgr.GetCmdLine());
            Launcher.StartPWKiller(true, false);
        }

        private void OnStartServer()
        {
            if (!Launcher.ReadyToStart()) return;
            Process.Start($"{Places.paraworldBinDir}/{Places.pwProcesses[0]}.exe", " -dedicated" + AddonMgr.GetCmdLine());
            Launcher.StartPWKiller(true, false);
        }
        private void OnSetLanguage() { }


        private bool isMusicPlaying;
        public bool IsMusicPlaying
        {
            get => isMusicPlaying;
            set
            {
                if (IsMusicPlaying)
                {
                    ToggleMusicText = turnMusicOnText;
                    mediaPlayer.Pause();
                }
                else
                {
                    ToggleMusicText = turnMusicOffText;
                    mediaPlayer.Play();
                }
                Set(ref isMusicPlaying, value);
            }
        }
        public void OnToggleMusic()
        {
            IsMusicPlaying = !IsMusicPlaying;
        }

        private void OnPlayNextTrack()
        {
            GenerateNewUI();
        }

        private void OnOpenScrFoldere()
        {
            if (!Directory.Exists(Places.docDir))
            {
                Log.Error($"Folder {Places.docDir} not found");
                return;
            }
            Process.Start("explorer", Places.docDir);
        }

        private void OnOpenSettingsFolder()
        {
            if (!Directory.Exists(Places.appDataDir))
            {
                Log.Error($"Folder {Places.appDataDir} not found");
                return;
            }
            Process.Start("explorer", Places.appDataDir);
        }
        private void OnStartPWKiller()
        {
            Launcher.StartPWKiller(false, true);
        }

        private bool isPwToolOpen;
        public bool IsPwToolOpen
        {
            get => isPwToolOpen;
            set
            {
                TogglePWToolText = value ? closePWToolText : openPWToolText;
                Set(ref isPwToolOpen, value);
            }
        }
        private void OnTogglePWTool()
        {
            TogglePWTool();
        }
        private void TogglePWTool()
        {
            IsPwToolOpen = !IsPwToolOpen;
        }

        private void OnRestoreSettings()
        {
            Launcher.RestoreSettings();
        }

        private void OnCreateSettingsBackup()
        {
            Launcher.AskCreateSettingsBackup();
        }

        private void OnOpenCredits()
        {
            FileMgr.Filepath creditsFilePath = new(AppContext.BaseDirectory, "Credits.txt");
            if (creditsFilePath.IsExist)
            {
                Process.Start("explorer", creditsFilePath.Full);
            }
        }

        private void OnUninstall()
        {
            FileMgr.Filepath uninstallFilePath = new(Places.paraworldDir, "Uninstall MIRAGE.exe");
            if (uninstallFilePath.IsExist)
            {
                Process.Start(uninstallFilePath.Full);
                Application.Current.Shutdown();
            }
        }

        private void OnExit()
        {
            Log.Close();
            Application.Current.Shutdown();
        }

        private void OnHealthCheck()
        {
            Task taskHealthCheck = new(HealthCheck.Check);
            taskHealthCheck.Start();
        }

        private void OnOpenUpdatePage()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://www.moddb.com/mods/paraworld-mirage/downloads",
                UseShellExecute = true
            });
        }

        private void OnOpenModdb()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://www.moddb.com/mods/paraworld-mirage",
                UseShellExecute = true
            });
        }

        private void OnOpenDiscord()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://discord.com/invite/uPT3T39Epc",
                UseShellExecute = true
            });
        }

        private void OnOpenGit()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/Tatsukio/MIRAGE",
                UseShellExecute = true
            });
        }
    }
}
