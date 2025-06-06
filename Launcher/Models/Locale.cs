using MIRAGE_Launcher.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace MIRAGE_Launcher.Models
{
    class Locale
    {
        public static string CurrentLang = "";
        public static string warning = "Warning";
        public static string backupMissing = "Backup file not found";
        public static string overwriteBackup = "Backup file already exists. Overwrite backup file?";
        public static string pwIsAlreadyRunning = "ParaWorld is already running. Start PWKiller?";
        public static string launcherIsAlreadyRunning = "This program is already running. Please close the running version first!";
        public static string backupCreated = "Created Settings_backup.cfg from settings.cfg. This one will be used when settings.cfg becomes corrupt.";
        public static string resetSettingsSuccess = "Replaced Settings.cfg with Settings_SSSS_backup.cfg. Some options might have been reset to an old state!";
        public static string resetSettings = "This will reset your Settings.cfg file, and some saved data (like last IP addresses) will be lost. Do you really want to continue?";
        public static string noCacheFound = "No cache files found.";
        public static string cacheDeleted = "ParaWorld cache was successfully cleared.";
        public static string settingsMissing = "Settings.cfg not found. If you have never run ParaWorld on this system before, you must run it first to create the necessary files.";
        public static string cfgError = "Settings.cfg change failed.";
        public static string askSettingsBackup = "Do you want to try to use the backup file?";
        public static string discordStatusMissingAddons = "The following addons must be enabled to enter the server:";
        public static string missingAddonsMain = "The following addons require other addons to be enabled:";
        public static string addon = "Addon:";
        public static string missingAddons = "Missing addons:";


        private static readonly DBMgr LauncherLocale;
        static Locale()
        {
            LauncherLocale = new(Places.launcherLocaleFilePath);
            string currentLang = CfgEditor.GetS("Root/Global/Language").ToUpper();
            if (string.IsNullOrEmpty(currentLang))
            {
                Log.Error("Failed to get locale");
                Environment.Exit(1);
            }
            CurrentLang = currentLang;
            Load();
        }

        public static void Load()
        {
            warning = Translate("Warning");
            string errorCode = "LogMessage#";
            pwIsAlreadyRunning = errorCode + "00\n" + Translate("PWIsAlreadyRunning");
            launcherIsAlreadyRunning = errorCode + "01\n" + Translate("LauncherIsAlreadyRunning");
            backupMissing = errorCode + "02\n" + Translate("BackupMissing");
            resetSettings = errorCode + "03\n" + Translate("ResetSettings");
            resetSettingsSuccess = errorCode + "04\n" + Translate("ResetSettingsSuccess");
            overwriteBackup = errorCode + "05\n" + Translate("OverwriteBackup");
            backupCreated = errorCode + "06\n" + Translate("BackupCreated");
            noCacheFound = errorCode + "07\n" + Translate("NoCacheFound");
            cacheDeleted = errorCode + "08\n" + Translate("CacheDeleted");
            settingsMissing = errorCode + "10\n" + Translate("SettingsMissing");
            askSettingsBackup = errorCode + "12\n" + Translate("AskSettingsBackup");
            discordStatusMissingAddons = errorCode + "13\n" + Translate("DiscordStatusMissingAddons");
            missingAddonsMain = errorCode + "14\n" + Translate("MissingAddonsMain");
            addon = Translate("Addon");
            missingAddons = Translate("MissingAddons");
        }

        public static List<string> GetAvailableLangs()
        {
            var supportedLangs = LauncherLocale.DB.Elements()
                .Select(e => e.Name.LocalName).ToList() ?? [];

            var existingLangs = Directory.Exists(Places.langsPath)
                ? Directory.GetDirectories(Places.langsPath).Select(Path.GetFileName).Select(name => name.ToUpper()).ToList()
                : [];

            return supportedLangs.Intersect(existingLangs).ToList();
        }

        public static string Translate(string p_text)
        {
            return LauncherLocale.Get(CurrentLang, p_text);
        }

        public static ObservableCollection<string> ToObservableCollection(List<string> p_list)
        {
            var collection = new ObservableCollection<string>();
            for (int i = 0; i < p_list.Count; i++)
            {
                collection.Add(p_list[i]);
            }
            return collection;
        }
    }
}
