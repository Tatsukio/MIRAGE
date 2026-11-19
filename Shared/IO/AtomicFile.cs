using System;
using System.IO;
using System.Threading;

namespace MIRAGE.Shared.IO;

/// <summary>Writes files through a temporary file in the same folder, so the target is never half written.</summary>
public static class AtomicFile
{
    private const int c_Attempts = 10;
    private static readonly TimeSpan s_retryDelay = TimeSpan.FromMilliseconds(200);

    public static void WriteAllBytes(string p_FilePath, byte[] p_Bytes)
    {
        string fullPath = Path.GetFullPath(p_FilePath);
        string directory = Path.GetDirectoryName(fullPath) ?? throw new IOException($"Invalid path '{p_FilePath}'.");
        _ = Directory.CreateDirectory(directory);
        string tempPath = Path.Combine(directory, $"{Path.GetFileName(fullPath)}.{Environment.ProcessId}.tmp");
        try
        {
            File.WriteAllBytes(tempPath, p_Bytes);
            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    File.Move(tempPath, fullPath, true);
                    return;
                }
                catch (IOException) when (attempt < c_Attempts)
                {
                    // Locked by another program for a moment, e.g. the game writing Settings.cfg.
                    Thread.Sleep(s_retryDelay);
                }
            }
        }
        finally
        {
            TryDelete(tempPath);
        }
    }

    private static void TryDelete(string p_FilePath)
    {
        try
        {
            if (File.Exists(p_FilePath))
            {
                File.Delete(p_FilePath);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Only a leftover temporary file.
        }
    }
}