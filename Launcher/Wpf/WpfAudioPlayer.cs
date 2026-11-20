using MIRAGE.Shared.Logging;
using MIRAGE_Launcher.Services;
using System;
using System.Windows.Media;

namespace MIRAGE_Launcher.Wpf;

/// <summary>The background music with the WPF MediaPlayer. Use it on the UI thread.</summary>
public sealed class WpfAudioPlayer : IAudioPlayer
{
    private readonly MediaPlayer m_player = new() { Volume = 0.2 };
    private readonly ILog m_log;
    private bool m_isOpened;
    private bool m_playWhenOpened;

    public WpfAudioPlayer(ILog p_Log)
    {
        m_log = p_Log;
        m_player.MediaOpened += (_, _) =>
        {
            m_isOpened = true;
            if (m_playWhenOpened)
            {
                m_player.Play();
            }
        };
        m_player.MediaEnded += (_, _) => Ended?.Invoke(this, EventArgs.Empty);
        m_player.MediaFailed += (_, p_E) => m_log.Error($"Failed to play music: {p_E.ErrorException?.Message ?? "unknown error"}");
    }

    public event EventHandler? Ended;

    public void Open(string p_FilePath, bool p_Play)
    {
        m_player.Stop();
        m_player.Close();
        m_isOpened = false;
        m_playWhenOpened = p_Play;
        m_player.Open(new Uri(p_FilePath, UriKind.Absolute));
    }

    public void Play()
    {
        m_playWhenOpened = true;
        if (m_isOpened)
        {
            m_player.Play();
        }
    }

    public void Pause()
    {
        m_playWhenOpened = false;
        if (m_isOpened)
        {
            m_player.Pause();
        }
    }

    public void Close() => m_player.Close();
}