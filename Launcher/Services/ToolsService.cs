using MIRAGE.Shared;
using MIRAGE.Shared.Logging;
using MIRAGE.Shared.Processes;
using System.Diagnostics;
using System.IO;

namespace MIRAGE_Launcher.Services;

public enum PWKillerMode
{
    /// <summary>The window with the "End processes" button.</summary>
    User,

    /// <summary>Hidden; resets UseUslCPP when the game is closed.</summary>
    Watch,

    /// <summary>Hidden; ends the game and every PWKiller at once.</summary>
    KillAll,
}

/// <summary>The helper programs in Tools: PWKiller and the Discord status (ParaWorldStatus).</summary>
public sealed class ToolsService(GamePaths p_Paths, ShellService p_Shell, ILog p_Log)
{
    private readonly GamePaths m_paths = p_Paths;
    private readonly ShellService m_shell = p_Shell;
    private readonly ILog m_log = p_Log;

    public bool StartPWKiller(PWKillerMode p_Mode)
    {
        if (!File.Exists(m_paths.PWKillerExe))
        {
            m_log.Warn($"{m_paths.PWKillerExe} not found.");
            return false;
        }
        ProcessStartInfo info = new(m_paths.PWKillerExe)
        {
            WorkingDirectory = m_paths.ToolsDir,
            UseShellExecute = false,
        };
        switch (p_Mode)
        {
            case PWKillerMode.Watch:
                info.ArgumentList.Add("--watch");
                info.CreateNoWindow = true;
                break;
            case PWKillerMode.KillAll:
                info.ArgumentList.Add("--kill-all");
                info.CreateNoWindow = true;
                break;
            default:
                break;
        }
        return m_shell.Start(info);
    }

    /// <summary>Restarts ParaWorldStatus.exe (the Discord status), if it is installed.</summary>
    public void RestartDiscordStatus()
    {
        _ = GameProcesses.KillAll([GameProcesses.DiscordStatus], m_log);
        if (!File.Exists(m_paths.DiscordStatusExe))
        {
            return;
        }
        _ = m_shell.Start(new ProcessStartInfo(m_paths.DiscordStatusExe)
        {
            WorkingDirectory = Path.GetDirectoryName(m_paths.DiscordStatusExe),
            UseShellExecute = false,
        });
    }
}