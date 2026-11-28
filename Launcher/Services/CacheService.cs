using MIRAGE.Shared;
using MIRAGE.Shared.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MIRAGE_Launcher.Services;

/// <summary>Deletes the game's cache files (*.bin, *.ubc, *.swd in %TEMP%\SpieleEntwicklungsKombinat\Paraworld).</summary>
public sealed class CacheService(GamePaths p_Paths, ILog p_Log)
{
    private static readonly string[] s_extensions = [".bin", ".ubc", ".swd"];

    private readonly GamePaths m_paths = p_Paths;
    private readonly ILog m_log = p_Log;

    /// <summary>Returns the files that could not be deleted (empty: all done).</summary>
    public List<string> Clear()
    {
        List<string> failed = [];
        if (!Directory.Exists(m_paths.CacheDir))
        {
            return failed;
        }
        List<string> files;
        try
        {
            files = [.. Directory.EnumerateFiles(m_paths.CacheDir).Where(static p_F => s_extensions.Contains(Path.GetExtension(p_F), StringComparer.OrdinalIgnoreCase))];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            m_log.Error($"Could not list {m_paths.CacheDir}: {ex.Message}");
            return failed;
        }
        foreach (string file in files)
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                m_log.Warn($"Could not delete cache file {file}: {ex.Message}");
                failed.Add(file);
            }
        }
        m_log.Debug($"Cache: {files.Count - failed.Count} files deleted, {failed.Count} failed.");
        return failed;
    }
}