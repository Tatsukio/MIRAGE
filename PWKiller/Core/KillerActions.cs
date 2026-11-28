using MIRAGE.Shared;
using MIRAGE.Shared.Cfg;
using MIRAGE.Shared.Logging;
using MIRAGE.Shared.Processes;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PWKiller.Core;

/// <summary>What PWKiller does, without the window (the window is only the button of the user mode).</summary>
public sealed class KillerActions(GamePaths p_Paths, ILog p_Log)
{
    private readonly GamePaths m_paths = p_Paths;
    private readonly ILog m_log = p_Log;

    /// <summary>Ends Paraworld.exe, PWClient.exe, PWServer.exe and the Discord status, then resets UseUslCPP.</summary>
    public void KillGame()
    {
        _ = GameProcesses.KillAll([.. GameProcesses.All, GameProcesses.DiscordStatus], m_log);
        _ = ResetServerScripts();
    }

    /// <summary>
    /// Background mode: waits until the game is closed, then ends what is left (PWServer.exe, the Discord status),
    /// resets UseUslCPP and closes the PWKiller window.
    /// </summary>
    public async Task WatchAsync(CancellationToken p_Token = default)
    {
        GameWatcher watcher = new(static () => GameProcesses.IsAnyRunning(GameProcesses.Clients));
        bool ran = await watcher.WaitForExitAsync(p_Token).ConfigureAwait(false);
        m_log.Info(ran ? "The game was closed." : "The game didn't start within a minute.");
        _ = GameProcesses.KillAll([GameProcesses.Server, GameProcesses.DiscordStatus], m_log);
        _ = ResetServerScripts();
        CloseOtherKillers();
    }

    /// <summary>The "Kill PW processes" button of the launcher: everything at once, and the PWKiller window too.</summary>
    public void KillAll()
    {
        KillGame();
        CloseOtherKillers();
    }

    /// <summary>Sets Pest/Server/UseUslCPP back to '1' (the compiled scripts of the original game).</summary>
    public bool ResetServerScripts()
    {
        if (!File.Exists(m_paths.SettingsCfg))
        {
            m_log.Warn($"{m_paths.SettingsCfg} not found, UseUslCPP not reset.");
            return false;
        }
        try
        {
            if (GameSettings.SetValue(m_paths.SettingsCfg, GameSettings.UseUslCpp, "1"))
            {
                m_log.Info("UseUslCPP set to 1.");
            }
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CfgParseException)
        {
            m_log.Error($"Could not reset UseUslCPP in {m_paths.SettingsCfg}: {ex.Message}");
            return false;
        }
    }

    private void CloseOtherKillers() => GameProcesses.KillAll([GameProcesses.PWKiller], m_log);
}