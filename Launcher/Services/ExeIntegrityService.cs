using MIRAGE.Shared;
using MIRAGE.Shared.Processes;
using MIRAGE_Launcher.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace MIRAGE_Launcher.Services;

/// <summary>
/// The WinFix check: compares the SHA-256 of Paraworld.exe, PWClient.exe and PWServer.exe with the hashes in
/// Settings.json (WinFix.Hashes).
/// </summary>
public sealed class ExeIntegrityService(GamePaths p_Paths, LauncherSettingsStore p_Settings)
{
    private readonly GamePaths m_paths = p_Paths;
    private readonly LauncherSettingsStore m_settings = p_Settings;

    public static IReadOnlyList<string> FileNames { get; } = [.. GameProcesses.All.Select(static p_P => p_P + ".exe")];

    public bool IsEnabled => m_settings.Settings.WinFix.CheckHashes;

    public static string ComputeHash(string p_FilePath)
    {
        using FileStream stream = new(p_FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }

    /// <summary>Checks all three files. <paramref name="p_HashAll"/>: also hash files that have no expected hashes (health check).</summary>
    public List<ExeCheckResult> Check(bool p_HashAll = false)
    {
        List<ExeCheckResult> results = [];
        foreach (string fileName in FileNames)
        {
            results.Add(CheckFile(fileName, p_HashAll));
        }
        return results;
    }

    private ExeCheckResult CheckFile(string p_FileName, bool p_HashAll)
    {
        string path = Path.Combine(m_paths.BinDir, p_FileName);
        if (!File.Exists(path))
        {
            return new ExeCheckResult(p_FileName, ExeCheckStatus.Missing, null);
        }
        IReadOnlyList<string> expected = m_settings.Settings.WinFix.GetHashes(p_FileName);
        if ((expected.Count == 0) && !p_HashAll)
        {
            return new ExeCheckResult(p_FileName, ExeCheckStatus.NotChecked, null);
        }
        string hash;
        try
        {
            hash = ComputeHash(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new ExeCheckResult(p_FileName, ExeCheckStatus.Missing, null);
        }
        if (expected.Count == 0)
        {
            return new ExeCheckResult(p_FileName, ExeCheckStatus.NotChecked, hash);
        }
        bool ok = expected.Any(p_H => string.Equals(p_H.Trim(), hash, StringComparison.OrdinalIgnoreCase));
        return new ExeCheckResult(p_FileName, ok ? ExeCheckStatus.Ok : ExeCheckStatus.Mismatch, hash);
    }
}