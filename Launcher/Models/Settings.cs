using MIRAGE_Launcher.Helpers;

namespace MIRAGE_Launcher.Models
{
    public static class Settings
    {
        private static DBMgr LauncherSettings;
        static Settings()
        {
            Load();
        }

        public static void Load()
        {
            LauncherSettings = new(Places.launcherSettingsFilePath);
        }

        public static void Set(string p_key, string p_value)
        {
            LauncherSettings.Set(p_key, p_value);
        }

        public static string Get(string p_key, string p_value)
        {
            return LauncherSettings.Get(p_key, p_value);
        }

        public static bool GetB(string p_key, string p_value)
        {
            return LauncherSettings.GetB(p_key, p_value);
        }

        public static int GetI(string p_key, string p_value)
        {
            return LauncherSettings.GetI(p_key, p_value);
        }
    }
}
