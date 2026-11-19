using MIRAGE.Shared.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace MIRAGE.Shared.Processes;

/// <summary>Finding and ending the game's processes. All methods catch the errors of single processes.</summary>
public static class GameProcesses
{
    public const string Launcher = "Paraworld";
    public const string Client = "PWClient";
    public const string Server = "PWServer";
    public const string DiscordStatus = "ParaWorldStatus";
    public const string PWKiller = "PWKiller";

    /// <summary>Paraworld.exe, PWClient.exe, PWServer.exe.</summary>
    public static IReadOnlyList<string> All { get; } = [Launcher, Client, Server];

    /// <summary>The processes a player has open while playing (PWServer can stay behind after a crash).</summary>
    public static IReadOnlyList<string> Clients { get; } = [Launcher, Client];

    public static bool IsAnyRunning(IEnumerable<string> p_Names)
    {
        foreach (string name in p_Names)
        {
            Process[] processes = GetProcesses(name);
            bool running = processes.Length > 0;
            DisposeAll(processes);
            if (running)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>Ends all processes with these names except this process. Returns how many were ended.</summary>
    public static int KillAll(IEnumerable<string> p_Names, ILog? p_Log)
    {
        int ownId = Environment.ProcessId;
        int count = 0;
        foreach (string name in p_Names)
        {
            Process[] processes = GetProcesses(name);
            foreach (Process process in processes)
            {
                if (process.Id == ownId)
                {
                    continue;
                }
                try
                {
                    process.Kill(true);
                    _ = process.WaitForExit(5000);
                    count++;
                    p_Log?.Info($"Ended {name}.exe (process {process.Id})");
                }
                catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or NotSupportedException)
                {
                    // Already gone, or no access (e.g. started by another user / with admin rights).
                    p_Log?.Warn($"Could not end {name}.exe (process {process.Id}): {ex.Message}");
                }
            }
            DisposeAll(processes);
        }
        return count;
    }

    private static Process[] GetProcesses(string p_Name)
    {
        try
        {
            return Process.GetProcessesByName(p_Name);
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or PlatformNotSupportedException)
        {
            return [];
        }
    }

    private static void DisposeAll(Process[] p_Processes)
    {
        foreach (Process process in p_Processes)
        {
            process.Dispose();
        }
    }
}