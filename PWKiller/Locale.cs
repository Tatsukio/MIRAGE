using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Forms;

namespace PWKiller
{
    class Locale
    {
        private static readonly DBMgr LauncherLocale;
        public static string CurrentLang = "";
        public static string PWKillerTitleText = "";
        public static string PWKillerButtonText = "";
        static Locale()
        {
            LauncherLocale = new(Places.launcherLocaleFilePath);
            string currentLang = CfgEditor.GetS("Root/Global/Language").ToUpper();
            if (string.IsNullOrEmpty(currentLang))
            {
                Environment.Exit(1);
            }
            CurrentLang = currentLang;
        }

        public static bool Load()
        {
            PWKillerTitleText = Translate("PWKillerTitleText");
            PWKillerButtonText = Translate("PWKillerButtonText");
            if (string.IsNullOrEmpty(PWKillerTitleText) || string.IsNullOrEmpty(PWKillerButtonText)) return false;
            return true;
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
