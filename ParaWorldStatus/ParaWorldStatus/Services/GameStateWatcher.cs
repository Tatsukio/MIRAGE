using MIRAGE.Shared.IO;
using MIRAGE.Shared.Logging;
using ParaWorldStatus.Models;
using System;
using System.IO;
using System.Threading;

namespace ParaWorldStatus.Services;

/// <summary>
/// Watches DiscordState\state_mirage.txt. The game deletes the file and writes it again for every change, so the
/// watcher waits a moment after the last event, reads the file with retries and ignores a missing file. A changed line
/// raises <see cref="StateChanged"/> (on a thread-pool thread).
/// </summary>
public sealed class GameStateWatcher : IDisposable
{
    public const string StateFileName = "state_mirage.txt";

    private static readonly TimeSpan s_settleTime = TimeSpan.FromMilliseconds(250);

    private readonly string m_folder;
    private readonly ILog m_log;
    private readonly Lock m_lock = new();
    private readonly Timer m_timer;
    private FileSystemWatcher? m_watcher;
    private string m_lastLine = string.Empty;

    public GameStateWatcher(string p_Folder, ILog p_Log)
    {
        m_folder = p_Folder;
        m_log = p_Log;
        m_timer = new Timer(_ => ReadNow(), null, Timeout.Infinite, Timeout.Infinite);
    }

    public event Action<GameState>? StateChanged;

    public string StateFile => Path.Combine(m_folder, StateFileName);

    /// <summary>Starts watching (the folder is created if the game hasn't made it yet) and reads the current file.</summary>
    public void Start()
    {
        _ = Directory.CreateDirectory(m_folder);
        m_watcher = new FileSystemWatcher(m_folder, StateFileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
        };
        m_watcher.Changed += OnFileEvent;
        m_watcher.Created += OnFileEvent;
        m_watcher.Renamed += OnFileEvent;
        m_watcher.Error += (_, p_E) => m_log.Warn($"File watcher error: {p_E.GetException().Message}");
        m_watcher.EnableRaisingEvents = true;
        ReadNow();
    }

    /// <summary>Reads the file now and raises <see cref="StateChanged"/> if the line is new.</summary>
    public void ReadNow()
    {
        string? line = TryReadLine(StateFile);
        if (line is null)
        {
            return;
        }
        lock (m_lock)
        {
            if (line == m_lastLine)
            {
                return;
            }
            m_lastLine = line;
        }
        var state = GameState.Parse(line);
        if (state is null)
        {
            m_log.Warn($"Unknown state line: {line}");
            return;
        }
        StateChanged?.Invoke(state);
    }

    /// <summary>The first line of the file, or null if it doesn't exist or can't be read right now.</summary>
    public static string? TryReadLine(string p_File)
    {
        for (int attempt = 1; attempt <= 5; attempt++)
        {
            try
            {
                if (!File.Exists(p_File))
                {
                    return null;
                }
                using FileStream stream = new(p_File, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using StreamReader reader = new(stream, TextEncodings.Ansi);
                return reader.ReadLine() ?? string.Empty;
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (DirectoryNotFoundException)
            {
                return null;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // The game is writing it.
                Thread.Sleep(50 * attempt);
            }
        }
        return null;
    }

    public void Dispose()
    {
        m_watcher?.Dispose();
        m_timer.Dispose();
    }

    private void OnFileEvent(object p_Sender, FileSystemEventArgs p_Args) => m_timer.Change(s_settleTime, Timeout.InfiniteTimeSpan);
}
