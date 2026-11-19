using System;

namespace MIRAGE_Launcher.Services;

/// <summary>Message boxes. The WPF implementation is in Wpf\WpfDialogService.cs; it also logs every message.</summary>
public interface IDialogService
{
    void ShowInfo(string p_Message);

    void ShowWarning(string p_Message);

    void ShowError(string p_Message);

    bool AskYesNo(string p_Message);
}

/// <summary>Background music. The WPF implementation is in Wpf\WpfAudioPlayer.cs.</summary>
public interface IAudioPlayer
{
    /// <summary>The track has played to the end.</summary>
    event EventHandler? Ended;

    /// <summary>Opens a file; plays it at once if <paramref name="p_Play"/> is true.</summary>
    void Open(string p_FilePath, bool p_Play);

    void Play();

    void Pause();
}

/// <summary>Ends the application (WPF: Application.Current.Shutdown).</summary>
public interface IAppLifetime
{
    void Shutdown();
}