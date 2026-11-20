using MIRAGE.Shared.Logging;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace MIRAGE_Launcher.Services;

/// <summary>Starts programs and opens folders and web pages. Never throws; failures are logged.</summary>
public sealed class ShellService(ILog p_Log)
{
    private readonly ILog m_log = p_Log;

    public bool OpenUrl(string p_Url) => Start(new ProcessStartInfo(p_Url) { UseShellExecute = true });

    public bool OpenFolder(string p_Folder)
    {
        if (!Directory.Exists(p_Folder))
        {
            m_log.Warn($"Folder {p_Folder} not found");
            return false;
        }
        return Start(new ProcessStartInfo("explorer.exe") { ArgumentList = { p_Folder }, UseShellExecute = false });
    }

    /// <summary>Opens a file with its program (e.g. a .txt file in the editor).</summary>
    public bool OpenFile(string p_File)
    {
        if (!File.Exists(p_File))
        {
            m_log.Warn($"File {p_File} not found");
            return false;
        }
        return Start(new ProcessStartInfo(p_File) { UseShellExecute = true });
    }

    /// <summary>Starts a process and returns it (the caller disposes it), or null.</summary>
    public Process? StartProcess(ProcessStartInfo p_StartInfo, out string p_Error)
    {
        try
        {
            p_Error = string.Empty;
            return Process.Start(p_StartInfo);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or IOException or PlatformNotSupportedException)
        {
            p_Error = ex.Message;
            m_log.Error($"Could not start {p_StartInfo.FileName} {p_StartInfo.Arguments}: {ex.Message}");
            return null;
        }
    }

    public bool Start(ProcessStartInfo p_StartInfo)
    {
        using Process? process = StartProcess(p_StartInfo, out _);
        return process is not null;
    }
}