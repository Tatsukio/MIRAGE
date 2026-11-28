using Microsoft.Win32;
using MIRAGE.Shared;
using MIRAGE.Shared.Cfg;
using MIRAGE.Shared.Logging;
using MIRAGE_Launcher.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;

namespace MIRAGE_Launcher.Services;

/// <summary>
/// Writes PWHealthCheck.txt to the desktop (right click on "Exit Launcher"): admin rights, the PEST-Control check with
/// the hashes, the Tages drivers, Settings.cfg, and every config file of the game that can't be parsed.
/// </summary>
public sealed class HealthCheckService(GamePaths p_Paths, ExeIntegrityService p_ExeCheck, ILog p_Log)
{
    private static readonly string[] s_configExtensions = [".txt", ".cfg", ".ini", ".srf", ".ttree"];

    private readonly GamePaths m_paths = p_Paths;
    private readonly ExeIntegrityService m_exeCheck = p_ExeCheck;
    private readonly ILog m_log = p_Log;

    public static string ReportPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "PWHealthCheck.txt");

    /// <summary>Builds the report text (slow: reads all config files of the game).</summary>
    public string BuildReport()
    {
        StringBuilder sb = new();
        _ = sb.AppendLine($"MIRAGE health check, {DateTime.Now:yyyy-MM-dd HH:mm}");
        _ = sb.AppendLine($"Game folder: {m_paths.GameDir}");
        _ = sb.AppendLine($"Launcher admin rights are{(IsRunAsAdmin() ? string.Empty : " not")} granted");

        _ = sb.AppendLine();
        _ = sb.AppendLine("PEST-Control check (SHA-256 of bin\\*.exe):");
        foreach (ExeCheckResult result in m_exeCheck.Check(true))
        {
            _ = sb.AppendLine($"  {result.FileName}: {result.Status} {result.Hash}");
        }

        _ = sb.AppendLine();
        _ = sb.AppendLine("Tages drivers check:");
        _ = sb.AppendLine(CheckTages());

        _ = sb.AppendLine();
        _ = sb.AppendLine("Settings.cfg:");
        _ = sb.AppendLine("  " + CheckConfig(m_paths.SettingsCfg));

        _ = sb.AppendLine();
        _ = sb.AppendLine("Config files that can't be parsed:");
        int checkedCount = 0;
        List<string> errors = [];
        foreach (string file in EnumerateConfigFiles(m_paths.GameDir))
        {
            checkedCount++;
            string result = CheckConfig(file);
            if (result != "OK")
            {
                errors.Add($"  {Path.GetRelativePath(m_paths.GameDir, file)}: {result}");
            }
        }
        _ = sb.AppendLine(errors.Count == 0 ? "  none" : string.Join(Environment.NewLine, errors));
        _ = sb.AppendLine($"({checkedCount} files checked)");
        return sb.ToString();
    }

    /// <summary>Writes the report to <see cref="ReportPath"/>; false if that failed.</summary>
    public bool WriteReport()
    {
        try
        {
            File.WriteAllText(ReportPath, BuildReport());
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            m_log.Error($"PWHealthCheck failed: {ex.Message}");
            return false;
        }
    }

    private static string CheckConfig(string p_File) => !File.Exists(p_File) ? "not found" : CfgDocument.TryLoad(p_File, out _, out string error) ? "OK" : error;

    /// <summary>Text files of the game that start with "Root" (the config tree format).</summary>
    private static IEnumerable<string> EnumerateConfigFiles(string p_Folder)
    {
        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(p_Folder, "*", new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            yield break;
        }
        foreach (string file in files)
        {
            if (s_configExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase) && StartsWithRoot(file))
            {
                yield return file;
            }
        }
    }

    private static bool StartsWithRoot(string p_File)
    {
        try
        {
            using FileStream stream = new(p_File, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            byte[] buffer = new byte[64];
            int read = stream.Read(buffer, 0, buffer.Length);
            string head = Encoding.Latin1.GetString(buffer, 0, read).TrimStart('\u00EF', '\u00BB', '\u00BF', ' ', '\t', '\r', '\n');
            return head.StartsWith("Root", StringComparison.Ordinal);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool IsRunAsAdmin()
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch (System.Security.SecurityException)
        {
            return false;
        }
    }

    private static string CheckTages()
    {
        if (!OperatingSystem.IsWindows())
        {
            return "  not Windows";
        }
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using RegistryKey? key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\SharedDLLs");
            List<string> lines = [];
            foreach (string driver in (string[])["lirsgt.sys", "atksgt.sys"])
            {
                bool installed = key?.GetValue(@"C:\Windows\system32\DRIVERS\" + driver) is not null;
                lines.Add($"  {driver} is{(installed ? string.Empty : " not")} installed");
            }
            return string.Join(Environment.NewLine, lines);
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            return $"  Tages check failed: {ex.Message}";
        }
    }
}