using MIRAGE.Shared.Logging;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace ParaWorldStatus.Services;

/// <summary>Starts "MIRAGE Launcher.exe -PWSJoin -&lt;secret&gt;" for Discord's Join / Spectate. The launcher checks the secret.</summary>
public sealed class LauncherStarter(string p_LauncherExe, ILog p_Log)
{
    private readonly string m_launcherExe = p_LauncherExe;
    private readonly ILog m_log = p_Log;

    public static ProcessStartInfo CreateStartInfo(string p_LauncherExe, string p_Secret)
    {
        ProcessStartInfo info = new(p_LauncherExe)
        {
            WorkingDirectory = Path.GetDirectoryName(p_LauncherExe) ?? string.Empty,
            UseShellExecute = false,
        };
        info.ArgumentList.Add("-PWSJoin");
        info.ArgumentList.Add("-" + p_Secret);
        return info;
    }

    /// <summary>Returns an error message, or null when the launcher was started.</summary>
    public string? Join(string p_Secret)
    {
        if (!File.Exists(m_launcherExe))
        {
            return $"{Path.GetFullPath(m_launcherExe)} not found.";
        }
        try
        {
            using var process = Process.Start(CreateStartInfo(m_launcherExe, p_Secret));
            return null;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            m_log.Error($"Could not start the launcher: {ex.Message}");
            return ex.Message;
        }
    }
}
