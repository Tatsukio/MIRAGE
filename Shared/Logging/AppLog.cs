using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace MIRAGE.Shared.Logging;

public enum LogLevel
{
    Debug,
    Info,
    Warn,
    Error,
}

public interface ILog
{
    void Write(LogLevel p_Level, string p_Message);
}

public static class LogExtensions
{
    public static void Debug(this ILog p_Log, string p_Message) => p_Log.Write(LogLevel.Debug, p_Message);

    public static void Info(this ILog p_Log, string p_Message) => p_Log.Write(LogLevel.Info, p_Message);

    public static void Warn(this ILog p_Log, string p_Message) => p_Log.Write(LogLevel.Warn, p_Message);

    public static void Error(this ILog p_Log, string p_Message) => p_Log.Write(LogLevel.Error, p_Message);
}

/// <summary>
/// Writes log lines to a file and, if a port is given, as UDP broadcast (the log viewer of the launcher listens on
/// that port). Never throws.
/// </summary>
public sealed class AppLog : ILog, IDisposable
{
    private const long c_MaxFileSize = 1024 * 1024;

    private readonly Lock m_lock = new();
    private readonly string m_source;
    private readonly string? m_filePath;
    private UdpClient? m_udp;
    private IPEndPoint? m_udpTarget;

    /// <param name="p_Source">Name in front of every line, e.g. "MIRAGE.Launcher".</param>
    /// <param name="p_FilePath">Log file; it is moved to *.old when it gets bigger than 1 MB. Null for none.</param>
    /// <param name="p_UdpPort">Port for UDP broadcast; 0 for none.</param>
    public AppLog(string p_Source, string? p_FilePath, int p_UdpPort)
    {
        m_source = p_Source;
        m_filePath = p_FilePath;
        SetUdpPort(p_UdpPort);
        RotateFile();
    }

    public void SetUdpPort(int p_Port)
    {
        lock (m_lock)
        {
            m_udp?.Dispose();
            m_udp = null;
            m_udpTarget = null;
            if (p_Port is <= 0 or > 65535)
            {
                return;
            }
            try
            {
                m_udp = new UdpClient { EnableBroadcast = true };
                m_udpTarget = new IPEndPoint(IPAddress.Broadcast, p_Port);
            }
            catch (SocketException)
            {
                m_udp = null;
            }
        }
    }

    public void Write(LogLevel p_Level, string p_Message)
    {
        string level = p_Level switch
        {
            LogLevel.Debug => "DBUG",
            LogLevel.Info => "INFO",
            LogLevel.Warn => "WARN",
            _ => "ERROR",
        };
        string line = $"{m_source} {level}: {p_Message}";
        lock (m_lock)
        {
            if (m_filePath is not null)
            {
                try
                {
                    File.AppendAllText(m_filePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {line}{Environment.NewLine}");
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // Logging must never break the program.
                }
            }
            if ((m_udp is not null) && (m_udpTarget is not null))
            {
                try
                {
                    byte[] data = Encoding.UTF8.GetBytes(line);
                    _ = m_udp.Send(data, data.Length, m_udpTarget);
                }
                catch (Exception ex) when (ex is SocketException or ObjectDisposedException)
                {
                    // No network: ignore.
                }
            }
        }
    }

    public void Dispose()
    {
        lock (m_lock)
        {
            m_udp?.Dispose();
            m_udp = null;
        }
    }

    private void RotateFile()
    {
        if (m_filePath is null)
        {
            return;
        }
        try
        {
            string? directory = Path.GetDirectoryName(m_filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                _ = Directory.CreateDirectory(directory);
            }
            FileInfo info = new(m_filePath);
            if (info.Exists && (info.Length > c_MaxFileSize))
            {
                File.Move(m_filePath, m_filePath + ".old", true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Keep appending to the big file.
        }
    }
}