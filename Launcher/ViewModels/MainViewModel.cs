using MIRAGE.Shared;
using MIRAGE.Shared.Logging;
using MIRAGE_Launcher.Models;
using MIRAGE_Launcher.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MIRAGE_Launcher.ViewModels;

/// <summary>The launcher window. Has no WPF types; the window binds to it (Views\MainWindow.xaml).</summary>
public sealed class MainViewModel : ViewModelBase
{
    private const string c_ModDbUrl = "https://www.moddb.com/mods/paraworld-mirage";
    private const string c_DownloadsUrl = "https://www.moddb.com/mods/paraworld-mirage/downloads";
    private const string c_DiscordUrl = "https://discord.com/invite/uPT3T39Epc";
    private const string c_GitUrl = "https://github.com/Tatsukio/MIRAGE";

    private readonly GamePaths m_paths;
    private readonly LauncherSettingsStore m_settings;
    private readonly GameConfigService m_config;
    private readonly AddonService m_addons;
    private readonly GameStartService m_gameStart;
    private readonly MediaCatalog m_media;
    private readonly UpdateService m_updates;
    private readonly HealthCheckService m_healthCheck;
    private readonly ToolsService m_tools;
    private readonly ShellService m_shell;
    private readonly IDialogService m_dialogs;
    private readonly IAudioPlayer m_audio;
    private readonly IAppLifetime m_lifetime;
    private readonly ILog m_log;

    private int m_trackId;
    private bool m_isMusicPlaying;
    private string? m_selectedLanguage;

    public MainViewModel(
        GamePaths p_Paths,
        LauncherSettingsStore p_Settings,
        Localization p_Text,
        GameConfigService p_Config,
        AddonService p_Addons,
        GameStartService p_GameStart,
        MediaCatalog p_Media,
        UpdateService p_Updates,
        HealthCheckService p_HealthCheck,
        ToolsService p_Tools,
        ShellService p_Shell,
        IDialogService p_Dialogs,
        IAudioPlayer p_Audio,
        IAppLifetime p_Lifetime,
        ILog p_Log)
    {
        m_paths = p_Paths;
        m_settings = p_Settings;
        Text = p_Text;
        m_config = p_Config;
        m_addons = p_Addons;
        m_gameStart = p_GameStart;
        m_media = p_Media;
        m_updates = p_Updates;
        m_healthCheck = p_HealthCheck;
        m_tools = p_Tools;
        m_shell = p_Shell;
        m_dialogs = p_Dialogs;
        m_audio = p_Audio;
        m_lifetime = p_Lifetime;
        m_log = p_Log;

        MenuBackground = "pack://application:,,,/Resources/" + MediaCatalog.GetMenuBackground(DateTime.Now);
        m_isMusicPlaying = m_settings.Settings.Launcher.MusicOnStartup;
        Text.PropertyChanged += OnLanguageChanged;
        m_audio.Ended += (_, _) => NextTrack();

        StartGameCommand = new RelayCommand(() => StartGame(GameMode.Game));
        StartMapEditorCommand = new RelayCommand(() => StartGame(GameMode.MapEditor));
        StartServerCommand = new RelayCommand(() => StartGame(GameMode.DedicatedServer));
        ToggleMusicCommand = new RelayCommand(() => IsMusicPlaying = !IsMusicPlaying);
        NextTrackCommand = new RelayCommand(NextTrack);
        OpenScreenshotsCommand = new RelayCommand(() => m_shell.OpenFolder(m_paths.DocumentsDir));
        OpenSettingsFolderCommand = new RelayCommand(() => m_shell.OpenFolder(m_paths.AppDataDir));
        KillGameCommand = new RelayCommand(() => m_tools.StartPWKiller(PWKillerMode.KillAll));
        ToggleToolsPanelCommand = new RelayCommand(() => IsToolsPanelOpen = !IsToolsPanelOpen);
        RestoreSettingsCommand = new RelayCommand(RestoreSettings);
        CreateSettingsBackupCommand = new RelayCommand(CreateSettingsBackup);
        OpenCreditsCommand = new RelayCommand(() => m_shell.OpenFile(m_paths.CreditsFile));
        UninstallCommand = new RelayCommand(Uninstall);
        ExitCommand = new RelayCommand(m_lifetime.Shutdown);
        HealthCheckCommand = new AsyncRelayCommand(RunHealthCheckAsync, OnCommandError);
        OpenUpdatePageCommand = new RelayCommand(() => m_shell.OpenUrl(c_DownloadsUrl));
        OpenModDbCommand = new RelayCommand(() => m_shell.OpenUrl(c_ModDbUrl));
        OpenDiscordCommand = new RelayCommand(() => m_shell.OpenUrl(c_DiscordUrl));
        OpenGitCommand = new RelayCommand(() => m_shell.OpenUrl(c_GitUrl));

        // App.xaml.cs has read the add-ons already; fill the list before the window exists, as the old launcher did.
        ShowAddons(false);
    }

    /// <summary>All texts: <c>{Binding Text[StartModText]}</c>.</summary>
    public Localization Text { get; }

    public string MenuTitle => $"{Text["ModNameText"]} {m_settings.Settings.Launcher.ModVersion} {Text["ModBuildText"]} {m_settings.Settings.Launcher.ModBuild}";

    /// <summary>Full path of the background image.</summary>
    public string? BackgroundImage
    {
        get;
        private set => Set(ref field, value);
    }

    public string MenuBackground { get; }

    public bool IsMusicPlaying
    {
        get => m_isMusicPlaying;
        set
        {
            if (!Set(ref m_isMusicPlaying, value))
            {
                return;
            }
            if (value)
            {
                m_audio.Play();
            }
            else
            {
                m_audio.Pause();
            }
            OnPropertyChanged(nameof(ToggleMusicText));
        }
    }

    public string ToggleMusicText => Text[IsMusicPlaying ? "TurnMusicOffText" : "TurnMusicOnText"];

    /// <summary>The "PWTool" panel: add-ons and the Settings.cfg backup buttons.</summary>
    public bool IsToolsPanelOpen
    {
        get;
        set
        {
            if (Set(ref field, value))
            {
                OnPropertyChanged(nameof(ToggleToolsPanelText));
            }
        }
    }

    public string ToggleToolsPanelText => Text[IsToolsPanelOpen ? "ClosePWToolText" : "OpenPWToolText"];

    public bool IsUpdateAvailable
    {
        get;
        private set => Set(ref field, value);
    }

    public string UpdateLog
    {
        get;
        private set => Set(ref field, value);
    } = string.Empty;

    public ObservableCollection<string> Languages { get; } = [];

    public string? SelectedLanguage
    {
        get => m_selectedLanguage;
        set
        {
            if (string.IsNullOrEmpty(value) || (value == m_selectedLanguage))
            {
                return;
            }
            if (!m_config.SetLanguage(value))
            {
                m_dialogs.ShowError(Text.Message(11, "CfgError") + "\n" + m_config.LastError);
                OnPropertyChanged();
                return;
            }
            m_selectedLanguage = value;
            Text.SetLanguage(value);
            OnPropertyChanged();
        }
    }

    public ObservableCollection<AddonViewModel> Addons { get; } = [];

    public ICommand StartGameCommand { get; }

    public ICommand StartMapEditorCommand { get; }

    public ICommand StartServerCommand { get; }

    public ICommand ToggleMusicCommand { get; }

    public ICommand NextTrackCommand { get; }

    public ICommand OpenScreenshotsCommand { get; }

    public ICommand OpenSettingsFolderCommand { get; }

    public ICommand KillGameCommand { get; }

    public ICommand ToggleToolsPanelCommand { get; }

    public ICommand RestoreSettingsCommand { get; }

    public ICommand CreateSettingsBackupCommand { get; }

    public ICommand OpenCreditsCommand { get; }

    public ICommand UninstallCommand { get; }

    public ICommand ExitCommand { get; }

    public ICommand HealthCheckCommand { get; }

    public ICommand OpenUpdatePageCommand { get; }

    public ICommand OpenModDbCommand { get; }

    public ICommand OpenDiscordCommand { get; }

    public ICommand OpenGitCommand { get; }

    /// <summary>Called once when the window is shown.</summary>
    public async Task InitializeAsync()
    {
        LoadLanguages();
        NextTrack();

        Task ip = m_updates.SavePublicIpAsync();
        UpdateInfo? update = await m_updates.CheckForUpdateAsync().ConfigureAwait(true);
        if (update is not null)
        {
            UpdateLog = update.FormatChanges();
            IsUpdateAvailable = true;
        }
        await ip.ConfigureAwait(true);
    }

    private void LoadLanguages()
    {
        Languages.Clear();
        string[] installed = Directory.Exists(m_paths.ModLocaleDir)
            ? [.. Directory.GetDirectories(m_paths.ModLocaleDir).Select(static p_D => Path.GetFileName(p_D).ToUpperInvariant())]
            : [];
        foreach (string language in Text.Languages.Where(p_L => installed.Contains(p_L)))
        {
            Languages.Add(language);
        }
        // Set the field: the language is already in Settings.cfg, nothing to write.
        m_selectedLanguage = Languages.Contains(Text.Language) ? Text.Language : null;
        OnPropertyChanged(nameof(SelectedLanguage));
    }

    /// <param name="p_Reload">true: read Data\Info again (after "Restore Game Settings").</param>
    private void ShowAddons(bool p_Reload)
    {
        if (p_Reload)
        {
            m_addons.Load();
        }
        Addons.Clear();
        foreach (Addon addon in m_addons.Addons)
        {
            Addons.Add(new AddonViewModel(addon, m_addons.SetEnabled));
        }
        m_log.Info($"Add-on list: {Addons.Count} entries ({string.Join(", ", Addons.Select(static p_A => p_A.Id))}).");
    }

    private void StartGame(GameMode p_Mode)
    {
        if (m_gameStart.Start(p_Mode))
        {
            IsMusicPlaying = false;
        }
    }

    private void NextTrack()
    {
        m_trackId = m_media.PickRandom(m_trackId);
        BackgroundImage = m_media.GetBackground(m_trackId);
        string? music = m_media.GetMusic(m_trackId);
        if (music is null)
        {
            m_log.Warn($"Music for background {m_trackId} ({MediaCatalog.GetTrackName(m_trackId)}.mp3) not found.");
            return;
        }
        m_audio.Open(music, IsMusicPlaying);
    }

    private void RestoreSettings()
    {
        if (!m_config.HasBackup)
        {
            m_dialogs.ShowError(Text.Message(2, "BackupMissing"));
            return;
        }
        if (!m_dialogs.AskYesNo(Text.Message(3, "ResetSettings")))
        {
            return;
        }
        if (!m_config.RestoreBackup())
        {
            m_dialogs.ShowError(Text.Message(11, "CfgError") + "\n" + m_config.LastError);
            return;
        }
        m_settings.Settings.ResetUserOptions();
        _ = m_settings.Save();
        Text.SetLanguage(m_config.GetLanguage() ?? Text.Language);
        LoadLanguages();
        ShowAddons(true);
        m_dialogs.ShowInfo(Text.Message(4, "ResetSettingsSuccess"));
    }

    private void CreateSettingsBackup()
    {
        if (!m_config.Exists)
        {
            m_dialogs.ShowError(Text.Message(10, "SettingsMissing"));
            return;
        }
        if (m_config.HasBackup && !m_dialogs.AskYesNo(Text.Message(5, "OverwriteBackup")))
        {
            return;
        }
        if (m_config.CreateBackup())
        {
            m_dialogs.ShowInfo(Text.Message(6, "BackupCreated"));
        }
        else
        {
            m_dialogs.ShowError(Text.Message(11, "CfgError") + "\n" + m_config.LastError);
        }
    }

    private void Uninstall()
    {
        if (m_shell.Start(new System.Diagnostics.ProcessStartInfo(m_paths.UninstallExe) { UseShellExecute = true }))
        {
            m_lifetime.Shutdown();
        }
    }

    private async Task RunHealthCheckAsync()
    {
        string path = HealthCheckService.ReportPath;
        m_dialogs.ShowInfo(Text.Message(22, "HealthCheckRunning", path));
        bool ok = await Task.Run(m_healthCheck.WriteReport).ConfigureAwait(true);
        if (ok)
        {
            m_dialogs.ShowInfo(Text.Message(23, "HealthCheckDone", path));
        }
        else
        {
            m_dialogs.ShowError("PWHealthCheck failed");
        }
    }

    private void OnCommandError(Exception p_Exception)
    {
        m_log.Error(p_Exception.ToString());
        m_dialogs.ShowError(p_Exception.Message);
    }

    private void OnLanguageChanged(object? p_Sender, PropertyChangedEventArgs p_Args)
    {
        OnPropertyChanged(nameof(MenuTitle));
        OnPropertyChanged(nameof(ToggleMusicText));
        OnPropertyChanged(nameof(ToggleToolsPanelText));
    }
}