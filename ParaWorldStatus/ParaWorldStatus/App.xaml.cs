using MIRAGE.Shared;
using MIRAGE.Shared.Logging;
using ParaWorldStatus.Services;
using ParaWorldStatus.ViewModels;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace ParaWorldStatus;

/// <summary>
/// ParaWorldStatus.exe in Tools\ParaWorldStatus: shows the game state in Discord (rich presence) and starts the launcher
/// when someone clicks "Join". The game writes its state to %APPDATA%\SpieleEntwicklungsKombinat\Paraworld\DiscordState.
/// </summary>
public partial class App : Application
{
    private const string c_MutexName = @"Local\MIRAGE_ParaWorldStatus";

    private Mutex? m_instanceMutex;
    private AppLog? m_log;
    private DiscordService? m_discord;
    private GameStateWatcher? m_watcher;

    protected override void OnStartup(StartupEventArgs p_Args)
    {
        base.OnStartup(p_Args);

        m_instanceMutex = new Mutex(true, c_MutexName, out bool isFirst);
        if (!isFirst)
        {
            // Already running (the launcher restarts it before every game; a second one would fight over Discord).
            m_instanceMutex.Dispose();
            m_instanceMutex = null;
            Shutdown();
            return;
        }

        var paths = GamePaths.FromToolSubDir(AppContext.BaseDirectory);
        m_log = new AppLog("MIRAGE.ParaWorldStatus", Path.Combine(paths.AppDataDir, "MIRAGE_ParaWorldStatus.log"), 0);
        AppLog log = m_log;
        DispatcherUnhandledException += (_, p_E) =>
        {
            log.Error($"Unhandled: {p_E.Exception}");
            p_E.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, p_E) => log.Error($"Unhandled: {p_E.ExceptionObject}");

        MapCatalog maps;
        try
        {
            maps = MapCatalog.LoadEmbedded();
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            log.Error($"Map lists can't be read: {ex.Message}");
            maps = new MapCatalog([], []);
        }

        PresenceBuilder builder = new(maps, PresenceBuilder.NewPartyId());
        m_discord = new DiscordService(log);
        LauncherStarter launcher = new(paths.LauncherExe, log);
        m_watcher = new GameStateWatcher(paths.DiscordStateDir, log);
        GameStateWatcher watcher = m_watcher;

        MainViewModel viewModel = new(builder, m_discord.SetPresence, watcher.ReadNow, Shutdown);
        m_discord.ConnectionChanged += viewModel.OnConnectionChanged;
        m_discord.JoinRequested += p_Secret =>
        {
            string? error = launcher.Join(p_Secret);
            if (error is not null)
            {
                _ = Dispatcher.Invoke(() => MessageBox.Show(error, "ParaWorld Status", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        };
        watcher.StateChanged += viewModel.OnStateChanged;

        if (!m_discord.Start(PresenceBuilder.Starting()))
        {
            _ = MessageBox.Show("ParaWorldStatus could not start the Discord connection.", "ParaWorld Status", MessageBoxButton.OK, MessageBoxImage.Warning);
            Shutdown();
            return;
        }
        try
        {
            watcher.Start();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            log.Error($"Can't watch {paths.DiscordStateDir}: {ex.Message}");
        }

        Views.MainWindow window = new() { DataContext = viewModel };
        MainWindow = window;
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        window.Show();
    }

    protected override void OnExit(ExitEventArgs p_Args)
    {
        m_watcher?.Dispose();
        m_discord?.Dispose();
        if (m_instanceMutex is not null)
        {
            m_instanceMutex.ReleaseMutex();
            m_instanceMutex.Dispose();
        }
        m_log?.Dispose();
        base.OnExit(p_Args);
    }
}
