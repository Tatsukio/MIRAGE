using System;
using System.IO;

namespace PWKiller
{
    public static class Places
    {
        public static readonly string[] pwProcesses = ["Paraworld", "PWClient", "PWServer"];
        public static readonly string[] cacheExt = ["bin", "ubc", "swd"];

        public static readonly string cacheDir = Path.Combine(Path.GetTempPath(), "SpieleEntwicklungsKombinat", "Paraworld");
        public static readonly string appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SpieleEntwicklungsKombinat", "Paraworld");
        public static readonly string docDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SpieleEntwicklungsKombinat", "Paraworld");

        public static readonly string paraworldDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".."));
        public static readonly string paraworldBinDir = Path.Combine(paraworldDir, "bin");
        public static readonly string toolsDir = Path.Combine(paraworldDir, "Tools");
        public static readonly string backgroundsDir = Path.Combine(toolsDir, "MIRAGE Launcher", "Backgrounds");
        public static readonly string musicDir = Path.Combine(paraworldDir, "Data", "Base", "Audio", "Music");
        public static readonly string langsPath = Path.Combine(paraworldDir, "Data", "MIRAGE", "Locale");

        public static readonly FileMgr.Filepath settingsFilePath = new(appDataDir, "Settings.cfg");
        public static readonly FileMgr.Filepath settingsBackupFilePath = new(appDataDir, "Settings_backup.cfg");
        public static readonly FileMgr.Filepath cfgEditorFilePath = new(toolsDir, "CfgEditor.exe");
        public static readonly FileMgr.Filepath pwKillerFilePath = new(toolsDir, "PWKiller.exe");
        public static readonly FileMgr.Filepath launcherSettingsFilePath = new(Path.Combine(toolsDir, "MIRAGE Launcher"), "Settings.xml");
        public static readonly FileMgr.Filepath launcherSettingsBackupFilePath = new(Path.Combine(toolsDir, "MIRAGE Launcher"), "Settings_backup.xml");
        public static readonly FileMgr.Filepath launcherLocaleFilePath = new(Path.Combine(toolsDir, "MIRAGE Launcher"), "Locale.xml");
        public static readonly FileMgr.Filepath discordRPCFilePath = new(Path.Combine(toolsDir, "ParaWorldStatus"), "ParaWorldStatus.exe");
    }
}
