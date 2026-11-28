using MIRAGE.Shared;
using MIRAGE.Shared.Localization;
using MIRAGE.Shared.Logging;
using MIRAGE_Launcher.Services;
using MIRAGE_Launcher.ViewModels;
using MIRAGE_Launcher.Wpf;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Localization = MIRAGE_Launcher.Services.Localization;

namespace MIRAGE_Launcher;

/// <summary>
/// Start of the launcher: creates the services, handles <c>-PWSJoin</c> (Discord status), allows one launcher
/// window, then shows the main window.
/// </summary>
public partial class App : Application
{
    private const string c_MutexName = @"Local\MIRAGE_Launcher";

    private AppLog? m_log;
    private Mutex? m_instanceMutex;
    private WpfAudioPlayer? m_audio;
    private HttpClient? m_http;

    protected override void OnStartup(StartupEventArgs p_Args)
    {
        base.OnStartup(p_Args);

        var paths = GamePaths.FromLauncherDir(AppContext.BaseDirectory);
        m_log = new AppLog("MIRAGE.Launcher", Path.Combine(paths.AppDataDir, "MIRAGE_Launcher.log"), 0);
        AppLog log = m_log;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, p_E) => log.Error($"Unhandled: {p_E.ExceptionObject}");
        TaskScheduler.UnobservedTaskException += (_, p_E) =>
        {
            log.Error($"Unobserved task: {p_E.Exception}");
            p_E.SetObserved();
        };

        LauncherSettingsStore settings = new(paths.LauncherSettingsFile, paths.LegacyLauncherSettingsFile, log);
        settings.Load();
        log.SetUdpPort(settings.Settings.Launcher.UdpLogPort);
        log.Info($"Started {AppContext.BaseDirectory}, args: {string.Join(' ', p_Args.Args)}");

        Localization text = new(LoadLocale(paths, log, out string? localeProblem));
        WpfDialogService dialogs = new(text, log);
        if (localeProblem is not null)
        {
            dialogs.ShowWarning(localeProblem);
        }
        if (settings.LoadProblem is not null)
        {
            dialogs.ShowWarning(text.Message(21, "LauncherSettingsBroken") + "\n" + settings.LoadProblem);
        }

        GameConfigService config = new(paths, log);
        if (!config.Exists)
        {
            dialogs.ShowError(text.Message(10, "SettingsMissing") + "\n" + config.FilePath);
            Shutdown(2);
            return;
        }
        text.SetLanguage(ReadLanguage(config, text, dialogs));

        ShellService shell = new(log);
        ToolsService tools = new(paths, shell, log);
        AddonService addons = new(paths, settings, log);
        addons.Load();
        ExeIntegrityService exeCheck = new(paths, settings);
        GameStartService gameStart = new(paths, settings, text, config, addons, exeCheck, new CacheService(paths, log), tools, shell, dialogs, log);

        // "MIRAGE Launcher.exe -PWSJoin <address>#<addons>" from the Discord status: start the game, no window.
        string[] args = p_Args.Args;
        if ((args.Length == 2) && string.Equals(args[0], "-PWSJoin", StringComparison.OrdinalIgnoreCase))
        {
            _ = gameStart.JoinServer(args[1]);
            Shutdown();
            return;
        }
        if (args.Length != 0)
        {
            dialogs.ShowError($"Invalid command line arguments: {string.Join(' ', args)}");
            Shutdown(1);
            return;
        }

        m_instanceMutex = new Mutex(true, c_MutexName, out bool isFirst);
        if (!isFirst)
        {
            dialogs.ShowWarning(text.Message(1, "LauncherIsAlreadyRunning"));
            m_instanceMutex.Dispose();
            m_instanceMutex = null;
            Shutdown();
            return;
        }

        m_http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        m_audio = new WpfAudioPlayer(log);
        MainViewModel viewModel = new(
            paths,
            settings,
            text,
            config,
            addons,
            gameStart,
            new MediaCatalog(paths),
            new UpdateService(paths, settings, log, m_http),
            new HealthCheckService(paths, exeCheck, log),
            tools,
            shell,
            dialogs,
            m_audio,
            new WpfAppLifetime(),
            log);

        // "Views.": inside App, MainWindow alone is the Application.MainWindow property.
        Views.MainWindow window = new() { DataContext = viewModel };
        MainWindow = window;
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        window.Show();
        _ = InitializeAsync(viewModel, log);
    }

    protected override void OnExit(ExitEventArgs p_Args)
    {
        m_audio?.Close();
        m_http?.Dispose();
        if (m_instanceMutex is not null)
        {
            m_instanceMutex.ReleaseMutex();
            m_instanceMutex.Dispose();
        }
        m_log?.Info("Exit.");
        m_log?.Dispose();
        base.OnExit(p_Args);
    }

    private static async Task InitializeAsync(MainViewModel p_ViewModel, ILog p_Log)
    {
        try
        {
            await p_ViewModel.InitializeAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            p_Log.Error($"Initialization failed: {ex}");
        }
    }

    private static LocaleTable LoadLocale(GamePaths p_Paths, ILog p_Log, out string? p_Problem)
    {
        p_Problem = null;
        try
        {
            return LocaleTable.Load(p_Paths.LauncherLocaleFile);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            p_Problem = $"Locale.json can't be read, the launcher shows text keys instead of texts.\n{p_Paths.LauncherLocaleFile}\n{ex.Message}";
            p_Log.Error(p_Problem);
            return LocaleTable.Empty;
        }
    }

    /// <summary>The language from Settings.cfg; offers to restore the backup if the file is damaged.</summary>
    private static string ReadLanguage(GameConfigService p_Config, Localization p_Text, WpfDialogService p_Dialogs)
    {
        string? language = p_Config.GetLanguage();
        if ((language is null) && (p_Config.LastError is not null))
        {
            string message = p_Text.Message(11, "CfgError") + "\n" + p_Config.LastError;
            if (p_Config.HasBackup && p_Dialogs.AskYesNo(message + "\n\n" + p_Text.Message(12, "AskSettingsBackup")))
            {
                if (p_Config.RestoreBackup())
                {
                    language = p_Config.GetLanguage();
                }
            }
            else if (!p_Config.HasBackup)
            {
                p_Dialogs.ShowError(message);
            }
        }
        return language ?? LocaleTable.FallbackLanguage;
    }

    private void OnDispatcherUnhandledException(object p_Sender, DispatcherUnhandledExceptionEventArgs p_Args)
    {
        m_log?.Error($"Unhandled: {p_Args.Exception}");
        _ = MessageBox.Show(
            $"{p_Args.Exception.GetType().Name}: {p_Args.Exception.Message}\n\nThe details are in MIRAGE_Launcher.log next to Settings.cfg.",
            "MIRAGE Launcher",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        p_Args.Handled = true;
    }
}