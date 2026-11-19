using System;
using System.IO;

namespace MIRAGE.Shared;

/// <summary>Folders and files of the ParaWorld installation and of the user profile.</summary>
/// <param name="p_GameDir">The ParaWorld folder (the one with bin, Data and Tools).</param>
/// <param name="p_AppDataDir">Folder of Settings.cfg; the default is %APPDATA%\SpieleEntwicklungsKombinat\Paraworld.</param>
/// <param name="p_CacheDir">The default is %TEMP%\SpieleEntwicklungsKombinat\Paraworld.</param>
/// <param name="p_DocumentsDir">The default is Documents\SpieleEntwicklungsKombinat\Paraworld.</param>
public sealed class GamePaths(string p_GameDir, string? p_AppDataDir = null, string? p_CacheDir = null, string? p_DocumentsDir = null)
{
    public const string SettingsCfgName = "Settings.cfg";
    public const string SettingsCfgBackupName = "Settings_backup.cfg";
    public const string LauncherFolderName = "MIRAGE Launcher";

    public string GameDir { get; } = Path.GetFullPath(p_GameDir);

    public string AppDataDir { get; } = p_AppDataDir ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SpieleEntwicklungsKombinat", "Paraworld");

    public string CacheDir { get; } = p_CacheDir ?? Path.Combine(Path.GetTempPath(), "SpieleEntwicklungsKombinat", "Paraworld");

    /// <summary>Screenshots and saves.</summary>
    public string DocumentsDir { get; } = p_DocumentsDir ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SpieleEntwicklungsKombinat", "Paraworld");

    public string BinDir => Path.Combine(GameDir, "bin");

    public string ToolsDir => Path.Combine(GameDir, "Tools");

    public string InfoDir => Path.Combine(GameDir, "Data", "Info");

    public string MusicDir => Path.Combine(GameDir, "Data", "Base", "Audio", "Music");

    /// <summary>Data\Locale: the game's own languages (videos).</summary>
    public string GameLocaleDir => Path.Combine(GameDir, "Data", "Locale");

    /// <summary>Data\MIRAGE\Locale: the languages MIRAGE has.</summary>
    public string ModLocaleDir => Path.Combine(GameDir, "Data", "MIRAGE", "Locale");

    public string LauncherDir => Path.Combine(ToolsDir, LauncherFolderName);

    public string BackgroundsDir => Path.Combine(LauncherDir, "Backgrounds");

    public string LauncherSettingsFile => Path.Combine(LauncherDir, "Settings.json");

    public string LauncherLocaleFile => Path.Combine(LauncherDir, "Locale.json");

    /// <summary>The XML settings of launcher versions up to 2.6.8; read once to keep the user's choices.</summary>
    public string LegacyLauncherSettingsFile => Path.Combine(LauncherDir, "Settings.xml");

    public string CreditsFile => Path.Combine(LauncherDir, "Credits.txt");

    public string LauncherExe => Path.Combine(LauncherDir, "MIRAGE Launcher.exe");

    /// <summary>Where the game writes its state for the Discord status (GameStateReporter.usl).</summary>
    public string DiscordStateDir => Path.Combine(AppDataDir, "DiscordState");

    public string PWKillerExe => Path.Combine(ToolsDir, "PWKiller.exe");

    public string CfgEditorExe => Path.Combine(ToolsDir, "CfgEditor.exe");

    public string DiscordStatusExe => Path.Combine(ToolsDir, "ParaWorldStatus", "ParaWorldStatus.exe");

    public string UninstallExe => Path.Combine(GameDir, "Uninstall MIRAGE.exe");

    public string SettingsCfg => Path.Combine(AppDataDir, SettingsCfgName);

    public string SettingsCfgBackup => Path.Combine(AppDataDir, SettingsCfgBackupName);

    /// <summary>Paths for a program in Tools\MIRAGE Launcher.</summary>
    public static GamePaths FromLauncherDir(string p_LauncherDir) => new(Path.Combine(p_LauncherDir, "..", ".."));

    /// <summary>Paths for a program in a folder below Tools (Tools\ParaWorldStatus).</summary>
    public static GamePaths FromToolSubDir(string p_ToolSubDir) => new(Path.Combine(p_ToolSubDir, "..", ".."));

    /// <summary>Paths for a program in Tools (PWKiller, CfgEditor).</summary>
    public static GamePaths FromToolsDir(string p_ToolsDir) => new(Path.Combine(p_ToolsDir, ".."));

    /// <summary>bin\&lt;name&gt;.exe, e.g. <c>GetGameExe("PWClient")</c>.</summary>
    public string GetGameExe(string p_ProcessName) => Path.Combine(BinDir, p_ProcessName + ".exe");
}