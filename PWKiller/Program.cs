using MIRAGE.Shared;
using MIRAGE.Shared.Logging;
using PWKiller.Core;
using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace PWKiller;

/// <summary>
/// PWKiller.exe in Tools. Modes:
/// <list type="bullet">
/// <item>no arguments: a window with a button that ends the game (for the player, when the game hangs);</item>
/// <item><c>--watch</c>: started hidden by the launcher with the game; waits until the game is closed, then
/// resets Pest/Server/UseUslCPP to 1 and closes the window instance;</item>
/// <item><c>--kill-all</c>: ends the game and every PWKiller at once (the launcher's "Kill PW Processes").</item>
/// </list>
/// The old arguments <c>-SSSOffAfterPWExit</c> and <c>-KillAll</c> still work. One instance per mode.
/// </summary>
internal static class Program
{
    [STAThread]
    private static int Main(string[] p_Args)
    {
        KillerMode mode = KillerModeParser.Parse(p_Args);
        if (mode == KillerMode.Invalid)
        {
            _ = MessageBox.Show("Usage: PWKiller.exe [--watch | --kill-all]", "PWKiller", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return 2;
        }

        using Mutex mutex = new(true, KillerModeParser.MutexName(mode), out bool isFirst);
        if (!isFirst)
        {
            // Already running in this mode.
            return 0;
        }

        var paths = GamePaths.FromToolsDir(AppContext.BaseDirectory);
        using AppLog log = new("MIRAGE.PWKiller", Path.Combine(paths.AppDataDir, "MIRAGE_PWKiller.log"), 0);
        KillerActions actions = new(paths, log);
        AppDomain.CurrentDomain.UnhandledException += (_, p_E) => log.Error($"Unhandled: {p_E.ExceptionObject}");

        try
        {
            switch (mode)
            {
                case KillerMode.Watch:
                    log.Info("Watching the game.");
                    actions.WatchAsync().GetAwaiter().GetResult();
                    return 0;
                case KillerMode.KillAll:
                    actions.KillAll();
                    return 0;
                default:
                    RunWindow(actions, KillerTexts.Load(paths), log);
                    return 0;
            }
        }
        catch (Exception ex)
        {
            log.Error(ex.ToString());
            if (mode == KillerMode.User)
            {
                _ = MessageBox.Show(ex.Message, "PWKiller", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return 1;
        }
    }

    private static void RunWindow(KillerActions p_Actions, KillerTexts p_Texts, AppLog p_Log)
    {
        _ = Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, p_E) =>
        {
            p_Log.Error(p_E.Exception.ToString());
            _ = MessageBox.Show(p_E.Exception.Message, "PWKiller", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };
        Application.Run(new PWKillerMain(p_Actions, p_Texts));
    }
}