using MIRAGE.Shared;
using MIRAGE.Shared.Localization;
using MIRAGE.Shared.Logging;
using MIRAGE_Launcher.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MIRAGE.Tools.Tests;

internal sealed class ListLog : ILog
{
    public List<string> Lines { get; } = [];

    public void Write(LogLevel p_Level, string p_Message) => Lines.Add($"{p_Level}: {p_Message}");
}

internal sealed class FakeDialogs : IDialogService
{
    public List<string> Messages { get; } = [];

    public bool Answer { get; set; }

    public void ShowInfo(string p_Message) => Messages.Add("INFO " + p_Message);

    public void ShowWarning(string p_Message) => Messages.Add("WARN " + p_Message);

    public void ShowError(string p_Message) => Messages.Add("ERROR " + p_Message);

    public bool AskYesNo(string p_Message)
    {
        Messages.Add("ASK " + p_Message);
        return Answer;
    }
}

internal sealed class FakeAudio : IAudioPlayer
{
    public event EventHandler? Ended;

    public string? File { get; private set; }

    public bool IsPlaying { get; private set; }

    public void Open(string p_FilePath, bool p_Play)
    {
        File = p_FilePath;
        IsPlaying = p_Play;
    }

    public void Play() => IsPlaying = true;

    public void Pause() => IsPlaying = false;

    public void RaiseEnded() => Ended?.Invoke(this, EventArgs.Empty);
}

internal sealed class FakeLifetime : IAppLifetime
{
    public bool IsShutDown { get; private set; }

    public void Shutdown() => IsShutDown = true;
}

/// <summary>A small fake game installation in a temp folder.</summary>
internal sealed class FakeGame : IDisposable
{
    public FakeGame()
    {
        Paths = new GamePaths(Path.Combine(Dir.Path, "Paraworld"), Path.Combine(Dir.Path, "AppData"), Path.Combine(Dir.Path, "Temp"), Path.Combine(Dir.Path, "Docs"));
        _ = Dir.File("AppData/Settings.cfg", CfgTests.SettingsText);
        _ = Dir.File("Paraworld/Data/Info/MIRAGE.info", "# Add on info file\nid MIRAGE\nrequires BoosterPack3\ntype add-on\nversion 2.6.8\n");
        _ = Dir.File("Paraworld/Data/Info/BoosterPack3.info", "id BoosterPack3\n\nversion 1.0\nrequires BaseData\n");
        _ = Dir.File("Paraworld/Data/Info/Wintermod.info", "id Wintermod\nversion 1\nexcludes Summermod\n");
        _ = Dir.File("Paraworld/Data/Info/Locale_DE.info", "id Locale_DE\ntype locale\n");
        _ = Dir.File("Paraworld/Data/MIRAGE/Locale/UK/x.txt", string.Empty);
        _ = Dir.File("Paraworld/Data/MIRAGE/Locale/DE/x.txt", string.Empty);
        _ = Dir.File("Paraworld/Tools/MIRAGE Launcher/Backgrounds/background_1.jpg", "jpg");
        _ = Dir.File("Paraworld/Tools/MIRAGE Launcher/Backgrounds/background_2.jpg", "jpg");
        _ = Dir.File("Paraworld/Data/Base/Audio/Music/01_maintheme.mp3", "mp3");
        _ = Dir.File("Paraworld/Data/Base/Audio/Music/13_plain_icewaste.mp3", "mp3");
        foreach (string exe in (string[])["Paraworld", "PWClient", "PWServer"])
        {
            _ = Dir.File($"Paraworld/bin/{exe}.exe", exe + " exe");
        }
    }

    public TempDir Dir { get; } = new();

    public GamePaths Paths { get; }

    public ListLog Log { get; } = new();

    public static LocaleTable Locale => new(new Dictionary<string, Dictionary<string, string>>
    {
        ["UK"] = new() { ["ModNameText"] = "MIRAGE", ["ModBuildText"] = "build", ["TurnMusicOnText"] = "On", ["TurnMusicOffText"] = "Off", ["OpenPWToolText"] = "Open", ["ClosePWToolText"] = "Close" },
        ["DE"] = new() { ["ModNameText"] = "MIRAGE-DE" },
        ["FR"] = new() { ["ModNameText"] = "MIRAGE-FR" },
    });

    public static string Sha256(string p_Text) => Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(p_Text)));

    public void Dispose() => Dir.Dispose();
}