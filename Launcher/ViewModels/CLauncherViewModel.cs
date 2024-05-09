using MIRAGE_Launcher.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;

namespace MIRAGE_Launcher.ViewModel
{
    public class CLauncherViewModel : CViewModelBase
    {
        public static readonly MediaPlayer _mediaPlayer = new MediaPlayer();

        public static string _paraworldDir = AppDomain.CurrentDomain.BaseDirectory + "../../";
        public static string _paraworldBinDir = _paraworldDir + "/bin";
        public static string[] _pwProcesses = { "Paraworld", "PWClient", "PWServer" };
        public static string _toolsDir = _paraworldDir + "/Tools";
        public static string _currLang = "";

        public CLauncherViewModel()
        {
            #region Commands

            StartParaworldCmd = new CCommand(OnStartParaworldCmd, StartParaworldCmdEnabled);
            StartSDKCmd = new CCommand(OnStartSDKCmd, StartSDKCmdEnabled);
            StartServerCmd = new CCommand(OnStartServerCmd, StartServerCmdEnabled);
            SetLanguageCmd = new CCommand(OnSetLanguageCmd, SetLanguageCmdEnabled);
            ToggleMusicCmd = new CCommand(OnToggleMusicCmd, ToggleMusicCmdEnabled);
            PlayNextTrackCmd = new CCommand(OnPlayNextTrackCmd, PlayNextTrackCmdEnabled);
            OpenScrFolderCmd = new CCommand(OnOpenScrFoldereCmd, OpenScrFolderCmdEnabled);
            OpenSettingsFolderCmd = new CCommand(OnOpenSettingsFolderCmd, OpenSettingsFolderCmdEnabled);
            StartPWKillerCmd = new CCommand(OnStartPWKillerCmd, StartPWKillerCmdEnabled);
            TogglePWToolCmd = new CCommand(OnTogglePWToolCmd, TogglePWToolCmdEnabled);

            RestoreSettingsCmd = new CCommand(OnRestoreSettingsCmd, RestoreSettingsCmdEnabled);
            CreateSettingsBackupCmd = new CCommand(OnCreateSettingsBackupCmd, CreateSettingsBackupCmdEnabled);
            OpenCreditsCmd = new CCommand(OnOpenCreditsCmd, OpenCreditsCmdEnabled);

            UninstallCmd = new CCommand(OnUninstallCmd, UninstallCmdEnabled);
            ExitCmd = new CCommand(OnExitCmd, ExitCmdEnabled);
            HealthCheckCmd = new CCommand(OnHealthCheckCmd, HealthCheckCmdEnabled);

            OpenUpdatePageCmd = new CCommand(OnOpenUpdatePageCmd, OpenUpdatePageCmdEnabled);
            OpenModdbCmd = new CCommand(OnOpenModdbCmd, OpenModdbCmdEnabled);
            OpenDiscordCmd = new CCommand(OnOpenDiscordCmd, OpenDiscordCmdEnabled);
            OpenGitCmd = new CCommand(OnOpenGitCmd, OpenGitCmdEnabled);

            #endregion

            string settingsPath = null;
            if (!CLauncher.SettingsFound(ref settingsPath))
            {
                Application.Current.Shutdown();
                return;
            }

            Task taskGetMyPublicIp = new Task(CLauncher.GetMyPublicIp);
            taskGetMyPublicIp.Start();

            //Discord RichPresence support
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length == 3)
            {
                string PWSJoin = args[1];

                if (PWSJoin == "-PWSJoin")
                {
                    string ServerInfo = args[2];
                    if (!string.IsNullOrEmpty(ServerInfo))
                    {
                        if (ReadyToStart())
                        {
                            LoadLang();
                            LoadDB();
                            ReadMods();

                            //ServerInfo = -127.0.0.1:1111#BoosterPack1|MIRAGE
                            string server = ServerInfo.Split('#')[0].Trim('-'); //127.0.0.1:1111
                            string[] serverMods = ServerInfo.Split('#')[1].Split('|');
                            string[] clientMods = GetAllMods().ToArray();
                            string[] localMods = GetLocalMods().ToArray();
                            string missingMods = string.Join(", ", serverMods.Except(clientMods));

                            if (String.IsNullOrEmpty(missingMods))
                            {
                                string commandLine = " -enable " + string.Join(" -enable ", serverMods) + " -enable " + string.Join(" -enable ", localMods);
                                Process.Start($"{_paraworldBinDir}/{_pwProcesses[0]}.exe", $" -autoconnect {server}{commandLine}");
                                CLauncher.StartPWKiller(true,false);
                            }
                            else
                            {
                                MessageBox.Show($"Server requires these mods: {missingMods}");
                                CLauncher.StartPWKiller(true, false);
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show($"Unknown cmd line args: {string.Join(", ", args)}");
                }
                Application.Current.Shutdown();
                return;
            }

            if (Process.GetProcessesByName("MIRAGE Launcher").Length > 1)
            {
                MessageBox.Show(_launcherIsAlreadyRunning, _warning, MessageBoxButton.OK, MessageBoxImage.Warning);
                Application.Current.Shutdown();
                return;
            }

            LoadLang();
            LoadDB();
            LoadUI();
            ReadMods();
            SetS("EnabledMods", string.Join(",", GetEnabledMods()));
            ToggleMusic();
        }

        private void LoadLang()
        {
            LangCollection.Clear();
            LangCollection.Add(new LangInfo() { FullName = null, LangID = "uk" });

            _currLang = CCfgEditor.GetS("Root/Global/Language");
            if (String.IsNullOrEmpty(_currLang))
            {
                MessageBox.Show("Failed to get language from Settings.cfg. Language set to uk", null, MessageBoxButton.OK, MessageBoxImage.Warning);
                _currLang = "uk";
            }

            string[] langs = Directory.GetDirectories(_paraworldDir + "/Data/MIRAGE/Locale").Select(Path.GetFileName).ToArray();

            if (!langs.Contains(_currLang))
            {
                string error = _currLang.Length != 2 ? $"Root/Global/Language \"{_currLang}\" must be 2 characters long. " : $"Localization \"{_currLang}\" not supported or not found. ";
                MessageBox.Show(error + "Language set to uk. See Settings.cfg", null, MessageBoxButton.OK, MessageBoxImage.Warning);
                _currLang = "uk";
            }

            for (int i = 0; i < langs.Length; i++)
            {
                if (langs[i] == "uk")
                {
                    continue;
                }
                LangCollection.Add(new LangInfo() { FullName = langs[i].ToUpper(), LangID = langs[i] });
            }

            SelectedLang = LangCollection.FirstOrDefault(i => i.LangID == _currLang);
        }

        private void LoadUI()
        {
            Random random = new Random();
            int backgroundIndex = random.Next(1, 85);
            string musicDir = Path.GetFullPath(_paraworldDir + "/Data/Base/Audio/Music/");
            string backgroundDir = Path.GetFullPath(_toolsDir + "/MIRAGE Launcher/Backgrounds/");
            if (DateTime.Now.Month == 12 || DateTime.Now.Month == 01 || DateTime.Now.Month == 02)
            {
                MenuBackground = new BitmapImage(new Uri("pack://application:,,,/Resources/menu_bg_winter.png"));
            }
            else
            {
                MenuBackground = new BitmapImage(new Uri("pack://application:,,,/Resources/menu_bg.png"));
            }

            if (!Directory.Exists(backgroundDir) || !File.Exists(backgroundDir + "background_" + backgroundIndex + ".jpg"))
            {
                MessageBox.Show("Folder " + backgroundDir + " not found or empty.", null, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                LauncherBackground = new BitmapImage(new Uri(backgroundDir + "background_" + backgroundIndex + ".jpg"));
            }

            musicDir += CLauncher.GetMusicInfo(backgroundIndex) + ".mp3";

            if (!File.Exists(musicDir))
            {
                MessageBox.Show(musicDir + " not found.", null, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                _mediaPlayer.Volume = 0.25;
                _mediaPlayer.MediaEnded += (sender, eventArgs) => LoadUI();
                _mediaPlayer.Open(new Uri(musicDir, UriKind.Relative));
                _mediaPlayer.Play();
            }
        }

        public bool ReadyToStart()
        {
            foreach (string pwProcess in _pwProcesses)
            {
                if (Process.GetProcessesByName(pwProcess).Any())
                {
                    if (MessageBox.Show(_pwIsAlreadyRunning, null, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        CLauncher.StartPWKiller(false,false);
                    }
                    return false;
                }
                string path = null;
                if (!CLauncher.FileFound(_paraworldBinDir, $"{pwProcess}.exe", ref path))
                {
                    return false;
                }
            }

            string settingsPath = null;
            if (!CLauncher.SettingsFound(ref settingsPath))
            {
                return false;
            }

            if (!CLauncher.EnableSSS(true))
            {
                return false;
            }

            if (GetModsCmdLine() == null)
            {
                return false;
            }

            if (!CLauncher.CheckVideoLocale(_currLang))
            {
                MessageBox.Show($"You have selected a {_currLang} localization, but it does not have translated videos", null, MessageBoxButton.OK, MessageBoxImage.Error);
            }

            CLauncher.ClearCache();

            if (MusicPlaying)
            {
                ToggleMusic();
            }

            Directory.SetCurrentDirectory(_paraworldBinDir);

            return true;
        }
        private ObservableCollection<LangInfo> UpdatedLangCollection(string langID)
        {
            ObservableCollection<LangInfo> updatedLangCollection = new ObservableCollection<LangInfo>(LangCollection);

            for (int i = 0; i < updatedLangCollection.Count; i++)
            {
                updatedLangCollection[i].FullName = (updatedLangCollection[i].LangID == langID ? CurrentLangText : "") + updatedLangCollection[i].LangID.ToUpper();
            }

            return updatedLangCollection;
        }

        private ObservableCollection<LangInfo> _langCollection = new ObservableCollection<LangInfo>();
        public ObservableCollection<LangInfo> LangCollection
        {
            get => _langCollection;
            set
            {
                Set(ref _langCollection, value);
            }
        }
        public class LangInfo
        {
            public string FullName { get; set; }
            public string LangID { get; set; }

        }

        private LangInfo _selectedLang;
        public LangInfo SelectedLang
        {
            get => _selectedLang;
            set
            {
                if (_selectedLang == value)
                    return;

                if (CCfgEditor.SetS("Root/Global/Language " + value.LangID))
                {
                    _selectedLang = value;
                    LoadDB();
                    value.FullName = (value.FullName != null ? CurrentLangText : "Locale not found: ") + value.LangID.ToUpper();
                    LangCollection = UpdatedLangCollection(value.LangID);
                    _currLang = value.LangID;
                }
                else
                {
                    MessageBox.Show($"Failed to update locale in Settings.cfg", null, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private BitmapImage _launcherBackground;
        public BitmapImage LauncherBackground
        {
            get => _launcherBackground;
            set => Set(ref _launcherBackground, value);
        }

        private BitmapImage _menuBackground;
        public BitmapImage MenuBackground
        {
            get => _menuBackground;
            set => Set(ref _menuBackground, value);
        }

        private bool _showUpdateWindow;
        public bool ShowUpdateWindow
        {
            get => _showUpdateWindow;
            set => Set(ref _showUpdateWindow, value);
        }

        private string _updateLogText;
        public string UpdateLogText
        {
            get => _updateLogText;
            set => Set(ref _updateLogText, value);
        }

        #region Localization

        #region MenuButtons

        private string _updateTitleText = "New update available!";
        public string UpdateTitleText
        {
            get => _updateTitleText;
            set => Set(ref _updateTitleText, value);
        }

        private string _menuTitleText = "Launcher Menu";
        public string MenuTitleText
        {
            get => _menuTitleText;
            set => Set(ref _menuTitleText, value);
        }

        private string _startModText = "Start ParaWorld";
        public string StartModText
        {
            get => _startModText;
            set => Set(ref _startModText, value);
        }

        private string _startSDKText = "Start Mapeditor";
        public string StartSDKText
        {
            get => _startSDKText;
            set => Set(ref _startSDKText, value);
        }

        private string _startServerText = "Start Dedicated Server";
        public string StartServerText
        {
            get => _startServerText;
            set => Set(ref _startServerText, value);
        }

        private string _currentLangText = "Current lang: ";
        public string CurrentLangText
        {
            get => _currentLangText;
            set => Set(ref _currentLangText, value);
        }

        private string _toggleMusicText = "Turn Music Off";
        private string _turnMusicOnText = "Turn Music On";
        private string _turnMusicOffText = "Turn Music Off";
        public string ToggleMusicText
        {
            get => _toggleMusicText;
            set => Set(ref _toggleMusicText, value);
        }

        private string _openScrFolderText = "Screenshots folder";
        public string OpenScrFolderText
        {
            get => _openScrFolderText;
            set => Set(ref _openScrFolderText, value);
        }

        private string _openSettingsFolderText = "Settings Folder";
        public string OpenSettingsFolderText
        {
            get => _openSettingsFolderText;
            set => Set(ref _openSettingsFolderText, value);
        }

        private string _startPWKillerText = "Kill PW Processes";
        public string StartPWKillerText
        {
            get => _startPWKillerText;
            set => Set(ref _startPWKillerText, value);
        }

        private string _togglePWToolText = "Open PWTool";
        private string _openPWToolText = "Open PWTool";
        private string _closePWToolText = "Close PWTool";
        public string TogglePWToolText
        {
            get => _togglePWToolText;
            set => Set(ref _togglePWToolText, value);
        }

        private string _sssOnText = "Server Scripts On";
        public string SSSOnText
        {
            get => _sssOnText;
            set => Set(ref _sssOnText, value);
        }

        private string _sssOffText = "Server Scripts Off";
        public string SSSOffText
        {
            get => _sssOffText;
            set => Set(ref _sssOffText, value);
        }

        private string _restoreSettingsText = "Restore Game Settings";
        public string RestoreSettingsText
        {
            get => _restoreSettingsText;
            set => Set(ref _restoreSettingsText, value);
        }

        private string _createSettingsBackupText = "Create Settings Backup";
        public string CreateSettingsBackupText
        {
            get => _createSettingsBackupText;
            set => Set(ref _createSettingsBackupText, value);
        }

        private string _uninstallText = "Uninstall MIRAGE";
        public string UninstallText
        {
            get => _uninstallText;
            set => Set(ref _uninstallText, value);
        }

        private string _exitText = "Exit Launcher";
        public string ExitText
        {
            get => _exitText;
            set => Set(ref _exitText, value);
        }

        #endregion
        public static string _warning = "Warning";
        public static string _backupMissing = "Backup file not found";
        public static string _overwriteBackup = "Backup file already exists. Overwrite backup file?";
        public static string _pwIsAlreadyRunning = "ParaWorld is already running. Start PWKiller?";
        public static string _launcherIsAlreadyRunning = "This programm is already running. Please close the running version first!";
        public static string _backupCreated = "Created Settings_SSSS_backup.cfg from settings.cfg. This one will be used when settings.cfg becomes corrupt.";
        public static string _resetSettingsSuccess = "Replaced Settings.cfg with Settings_SSSS_backup.cfg. Some options might have been reset to an old state!";
        public static string _resetSettings = "This will reset your Settings.cfg file, and some saved data (like last IP addresses) will be lost. Do you really want to continue?";
        public static string _noCacheFound = "No cache files found.";
        public static string _cacheDeleted = "ParaWorld cache was successfully cleared.";
        public static string _settingsMissing = "Settings.cfg not found. If you have never run ParaWorld on this system before, you must run it first to create the necessary files.";
        public static string _cfgError = "Settings.cfg change failed.";
        public static string _askSettingsBackup = "Do you want to try to use the backup file?";

        #endregion

        #region Commands

        public ICommand StartParaworldCmd { get; }
        private bool StartParaworldCmdEnabled(object p) => true;
        private void OnStartParaworldCmd(object p)
        {
            if (ReadyToStart())
            {
                CLauncher.StartDiscordRPC();
                Process.Start($"{_paraworldBinDir}/{_pwProcesses[0]}.exe", GetModsCmdLine());
                CLauncher.StartPWKiller(true, false);
            }
        }

        public ICommand StartSDKCmd { get; }
        private bool StartSDKCmdEnabled(object p) => true;
        private void OnStartSDKCmd(object p)
        {
            if (ReadyToStart())
            {
                CLauncher.StartDiscordRPC();
                Process.Start($"{_paraworldBinDir}/{_pwProcesses[1]}.exe", " -leveled" + GetModsCmdLine());
                CLauncher.StartPWKiller(true, false);
            }
        }
        public ICommand StartServerCmd { get; }
        private bool StartServerCmdEnabled(object p) => true;
        private void OnStartServerCmd(object p)
        {
            if (ReadyToStart())
            {
                Process.Start($"{_paraworldBinDir}/{_pwProcesses[0]}.exe", " -dedicated" + GetModsCmdLine());
                CLauncher.StartPWKiller(true,false);
            }
        }
        public ICommand SetLanguageCmd { get; }
        private bool SetLanguageCmdEnabled(object p) => true;
        private void OnSetLanguageCmd(object p) { }

        public ICommand ToggleMusicCmd { get; }

        private bool _musicPlaying;
        public bool MusicPlaying
        {
            get => _musicPlaying;
            set => Set(ref _musicPlaying, value);
        }
        private bool ToggleMusicCmdEnabled(object p) => true;
        private void OnToggleMusicCmd(object p)
        {
            ToggleMusic();
        }
        private void ToggleMusic()
        {
            if (MusicPlaying)
            {
                ToggleMusicText = _turnMusicOnText;
                _mediaPlayer.Pause();
                MusicPlaying = false;
            }
            else
            {
                ToggleMusicText = _turnMusicOffText;
                _mediaPlayer.Play();
                MusicPlaying = true;
            }
        }

        public ICommand PlayNextTrackCmd { get; }
        private bool PlayNextTrackCmdEnabled(object p) => true;
        private void OnPlayNextTrackCmd(object p)
        {
            ToggleMusic();
            LoadUI();
            ToggleMusic();
        }

        public ICommand OpenScrFolderCmd { get; }
        private bool OpenScrFolderCmdEnabled(object p) => true;
        private void OnOpenScrFoldereCmd(object p)
        {
            string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\SpieleEntwicklungsKombinat\Paraworld\";
            if (!Directory.Exists(docPath))
            {
                MessageBox.Show("Folder " + docPath + " not found", null, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            Process.Start("explorer", Path.GetDirectoryName(docPath));
            //MessageBox.Show(CLauncher.ClearCache() ? _cacheDeleted : _noCacheFound, "ClearPWCache", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public ICommand OpenSettingsFolderCmd { get; }
        private bool OpenSettingsFolderCmdEnabled(object p) => true;
        private void OnOpenSettingsFolderCmd(object p)
        {
            string path = null;
            if (CLauncher.SettingsFound(ref path))
            {
                Process.Start("explorer", Path.GetDirectoryName(path));
            }
        }
        public ICommand StartPWKillerCmd { get; }
        private bool StartPWKillerCmdEnabled(object p) => true;
        private void OnStartPWKillerCmd(object p)
        {
            CLauncher.StartPWKiller(false, true);
        }
        public ICommand TogglePWToolCmd { get; }

        private bool _pwToolIsOpen;
        public bool PWToolIsOpen
        {
            get => _pwToolIsOpen;
            set
            {
                TogglePWToolText = value ? _closePWToolText : _openPWToolText;
                Set(ref _pwToolIsOpen, value);
            }
        }
        private bool TogglePWToolCmdEnabled(object p) => true;
        private void OnTogglePWToolCmd(object p)
        {
            TogglePWTool();
        }
        private void TogglePWTool()
        {
            PWToolIsOpen = !PWToolIsOpen;
        }


        public ICommand RestoreSettingsCmd { get; }
        private bool RestoreSettingsCmdEnabled(object p) => true;
        private void OnRestoreSettingsCmd(object p)
        {
            CLauncher.RestoreSettings();
        }

        public ICommand CreateSettingsBackupCmd { get; }
        private bool CreateSettingsBackupCmdEnabled(object p) => true;
        private void OnCreateSettingsBackupCmd(object p)
        {
            CLauncher.AskCreateSettingsBackup();
        }

        public ICommand OpenCreditsCmd { get; }
        private bool OpenCreditsCmdEnabled(object p) => true;
        private void OnOpenCreditsCmd(object p)
        {
            string path = null;
            if (CLauncher.FileFound(AppContext.BaseDirectory, "Credits.txt", ref path))
            {
                Process.Start("explorer", path);
            }
        }

        public ICommand UninstallCmd { get; }
        private bool UninstallCmdEnabled(object p) => true;
        private void OnUninstallCmd(object p)
        {
            string path = null;
            if (CLauncher.FileFound(_paraworldDir, "Uninstall MIRAGE.exe", ref path))
            {
                Process.Start(path);
                Application.Current.Shutdown();
            }
        }

        public ICommand ExitCmd { get; }
        private bool ExitCmdEnabled(object p) => true;
        private void OnExitCmd(object p)
        {
            Application.Current.Shutdown();
        }

        public ICommand HealthCheckCmd { get; }
        private bool HealthCheckCmdEnabled(object p) => true;
        private void OnHealthCheckCmd(object p)
        {
            Task taskHealthCheck = new Task(CLauncher.HealthCheck);
            taskHealthCheck.Start();
        }

        #region Social
        public ICommand OpenUpdatePageCmd { get; }
        private bool OpenUpdatePageCmdEnabled(object p) => true;
        private void OnOpenUpdatePageCmd(object p)
        {
            Process.Start("https://www.moddb.com/mods/paraworld-mirage");
        }

        public ICommand OpenModdbCmd { get; }
        private bool OpenModdbCmdEnabled(object p) => true;
        private void OnOpenModdbCmd(object p)
        {
            Process.Start("https://www.moddb.com/mods/paraworld-mirage");
        }

        public ICommand OpenDiscordCmd { get; }
        private bool OpenDiscordCmdEnabled(object p) => true;
        private void OnOpenDiscordCmd(object p)
        {
            Process.Start("https://discord.com/invite/uPT3T39Epc");
        }

        public ICommand OpenGitCmd { get; }
        private bool OpenGitCmdEnabled(object p) => true;
        private void OnOpenGitCmd(object p)
        {
            Process.Start("https://github.com/Tatsukio/MIRAGE");
        }

        #endregion

        #endregion


        static readonly XmlDocument _launcherDB = new XmlDocument();

        static string _launcherDBPath = null;
        public static string[] _supportedLangs = { "uk", "us", "de", "es", "fr", "hu", "it", "pl", "ru", "zh", "cz" };  //TODO get supportedLangs from the LauncherDB.xml
        static string _langCurrentOrUk = "uk";

        private void LoadDB()
        {
            if (CLauncher.FileFound(_toolsDir, "MIRAGE Launcher/LauncherDB.xml", ref _launcherDBPath))
            {
                _launcherDB.Load(_launcherDBPath);
                string ModVersion = GetS("ModVersion");
                string LatestVersionURL = GetS("VersionURL");
                if (_supportedLangs.Contains(SelectedLang.LangID))
                {
                    _langCurrentOrUk = SelectedLang.LangID;
                }

                Task taskVersionCheck = new Task(() => CheckMirageVersion(ModVersion, LatestVersionURL));
                taskVersionCheck.Start();
                
                UpdateTitleText = TranslateS("UpdateTitleText");
                MenuTitleText = GetS("MenuTitleText");
                StartModText = TranslateS("StartModText");
                StartSDKText = TranslateS("StartSDKText");
                StartServerText = TranslateS("StartServerText");
                CurrentLangText = TranslateS("CurrentLangText");
                _turnMusicOnText = TranslateS("TurnMusicOnText");
                _turnMusicOffText = TranslateS("TurnMusicOffText");
                OpenScrFolderText = TranslateS("OpenScrFolderText");
                OpenSettingsFolderText = TranslateS("OpenSettingsFolderText");
                StartPWKillerText = TranslateS("StartPWKillerText");
                _openPWToolText = TranslateS("OpenPWToolText");
                _closePWToolText = TranslateS("ClosePWToolText");
                RestoreSettingsText = TranslateS("RestoreSettingsText");
                CreateSettingsBackupText = TranslateS("CreateSettingsBackupText");
                UninstallText = TranslateS("UninstallText");
                ExitText = TranslateS("ExitText");
                ToggleMusicText = MusicPlaying ? _turnMusicOffText : _turnMusicOnText;
                TogglePWToolText = PWToolIsOpen ? _closePWToolText : _openPWToolText;

                _warning = TranslateS("Warning");
                
                string errorCode = "LogMessage#";

                _pwIsAlreadyRunning = errorCode + "00\n" + TranslateS("PWIsAlreadyRunning");
                _launcherIsAlreadyRunning = errorCode + "01\n" + TranslateS("LauncherIsAlreadyRunning");
                _backupMissing = errorCode + "02\n" + TranslateS("BackupMissing");
                _resetSettings = errorCode + "03\n" + TranslateS("ResetSettings");
                _resetSettingsSuccess = errorCode + "04\n" + TranslateS("ResetSettingsSuccess");
                _overwriteBackup = errorCode + "05\n" + TranslateS("OverwriteBackup");
                _backupCreated = errorCode + "06\n" + TranslateS("BackupCreated");
                _noCacheFound = errorCode + "07\n" + TranslateS("NoCacheFound");
                _cacheDeleted = errorCode + "08\n" + TranslateS("CacheDeleted");
                _settingsMissing = errorCode + "10\n" + TranslateS("SettingsMissing");
                _askSettingsBackup = errorCode + "12\n" + TranslateS("AskSettingsBackup");

                _enabledMods = GetS("EnabledMods");
            }
        }

        private static void SetS(string name, string value)
        {
            _launcherDB.DocumentElement.SelectSingleNode("/LauncherDB/Misc/" + name).InnerText = value;

            if (String.IsNullOrEmpty(_launcherDBPath))
            {
                MessageBox.Show($"SetS to LauncherDB.xml failed - no LauncherDB.xml found");
            }
            else
            {
                _launcherDB.Save(_launcherDBPath);
            }
        }

        private static string GetS(string text)
        {
            string value = _launcherDB.DocumentElement.SelectSingleNode("/LauncherDB/Misc/" + text).InnerText;
            if (value == null)
            {
                MessageBox.Show($"GetS from LauncherDB.xml failed - value /LauncherDB/Misc/{text} is null or empty");
                return String.Empty;
            }
            return value;
        }

        private static string TranslateS(string text)
        {
            return _launcherDB.DocumentElement.SelectSingleNode("/LauncherDB/Locale/" + _langCurrentOrUk + "/" + text).InnerText;
        }

        public void CheckMirageVersion(string version, string latestVersionURL)
        {
            if (!string.IsNullOrEmpty(version) && !string.IsNullOrEmpty(latestVersionURL))
            {
                Version mirageVersion = new Version(version);

                using (WebClient versionPage = new WebClient())
                {
                    versionPage.Proxy = new WebProxy();

                    //"versioncheck	MIRAGE 2.6.5	19	0	UI tweaks for fullhd resolution;4k monitors support;Multi-Defender maps;Epoch 6 (with Scorpion and Avatar);Phantom-mode;New animals and textures;And much, much more!"
                    string siteVersionFull = versionPage.DownloadString(latestVersionURL);
                    
                    string[] siteVersion = siteVersionFull.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);

                    if (siteVersion[0] == "versioncheck" && siteVersion != null)
                    {
                        string[] siteModVersion = siteVersion[1].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        
                        Version mirageSiteVersion = new Version(siteModVersion[1]);
                        switch (mirageVersion.CompareTo(mirageSiteVersion))
                        {
                            case 0:
                                //MirageVersion == MirageSiteVersion
                                break;
                            case 1:
                                //MirageVersion > MirageSiteVersion
                                break;
                            case -1:
                                UpdateLogText = "● " + siteVersion[4].Replace(";", "\n● ");
                                ShowUpdateWindow = true;
                                break;
                        }
                    }
                }
            }
        }

        public static string _enabledMods = "";
        public static ObservableCollection<ModInfo> ModCollection { get; } = new ObservableCollection<ModInfo>();

        public class ModInfo
        {
            private bool _modEnabled;

            public bool ModEnabled
            {
                get => _modEnabled;
                set
                {
                    _modEnabled = value;
                    SetS("EnabledMods", string.Join(",", GetEnabledMods()));
                }
            }

            public string ModName { get; set; }

            public string ModType { get; set; }

            public string ModVersion { get; set; }

            public List<string> ModRequires { get; set; }
        }

        private static void ReadMods()
        {
            string infoDir = _paraworldDir + "/Data/Info/";
            ModCollection.Clear();
            if (Directory.Exists(infoDir))
            {

                foreach (string infoName in Directory.EnumerateFiles(infoDir, "*.info").Select(Path.GetFileName).Where(s => s != "BaseLocale.info" && s != "LevelEd.info" && !s.Contains("Locale_")))
                {
                    using (StreamReader readInfo = new StreamReader(infoDir + infoName))
                    {
                        bool modEnabled = false;
                        string modName = "";
                        string modType = "";
                        string modVersion = "";
                        List<string> modRequires = new List<string>();

                        while (!readInfo.EndOfStream)
                        {
                            string line = readInfo.ReadLine();

                            if (line.StartsWith("#"))
                            {

                            }
                            else if (line.StartsWith("id"))
                            {
                                modName = line.Split().Skip(1).FirstOrDefault();
                            }
                            else if (line.StartsWith("type"))
                            {
                                modType = line.Split().Skip(1).FirstOrDefault();
                            }
                            else if (line.StartsWith("version"))
                            {
                                modVersion = line.Split().Skip(1).FirstOrDefault();
                            }
                            else if (line.StartsWith("requires"))
                            {
                                foreach (string temp in line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Skip(1))
                                {
                                    if (temp == "BaseData")
                                    {
                                        continue;
                                    }
                                    modRequires.Add(temp);
                                }
                            }
                        }
                        if (!string.IsNullOrEmpty(modName))
                        {
                            if (Array.Exists(_enabledMods.Split(','), s => s == modName))
                            {
                                modEnabled = true;
                            }

                            ModCollection.Add(new ModInfo() { ModEnabled = modEnabled, ModName = modName, ModType = modType, ModVersion = modVersion, ModRequires = modRequires });
                        }
                        else
                        {
                            MessageBox.Show($"Error while reading {infoName} file. Mod id can't be null.", null, MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }

            }
        }

        private static List<string> GetEnabledMods()
        {
            List<string> ModList = new List<string>();

            foreach (ModInfo mod in ModCollection)
            {
                if (mod.ModEnabled)
                {
                    ModList.Add(mod.ModName);
                }
            }
            return ModList;
        }

        private static List<string> GetAllMods()
        {
            List<string> ModList = new List<string>();

            foreach (ModInfo mod in ModCollection)
            {
                ModList.Add(mod.ModName);
            }
            return ModList;
        }

        private static List<string> GetLocalMods()
        {
            List<string> ModList = new List<string>();

            foreach (ModInfo mod in ModCollection)
            {
                if (mod.ModEnabled && mod.ModType == "locale")
                {
                    ModList.Add(mod.ModName);
                }
            }
            return ModList;
        }

        private static string GetModsCmdLine()
        {
            List<string> ModList = new List<string>();
            List<string> requiresList = new List<string>();

            foreach (ModInfo mod in ModCollection)
            {
                if (mod.ModEnabled)
                {
                    ModList.Add(mod.ModName);
                    requiresList = requiresList.Union(mod.ModRequires).ToList();
                }
            }

            if (ModList.Any())
            {
                string missingMods = string.Join(", ", requiresList.Except(ModList));

                if (string.IsNullOrEmpty(missingMods))
                {
                    string enabledMods = string.Join(",", ModList.Except(requiresList));
                    string commandLine = " -enable " + string.Join(" -enable ", enabledMods.Split(','));
                    return commandLine;
                }
                else
                {
                    MessageBox.Show($"Required mods ({missingMods}) not found or disabled.", null, MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
            }
            return string.Empty;
        }
    }
}
