using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace MIRAGE_Launcher.Models
{
    public static class AddonMgr
    {
        static AddonMgr()
        {
            Load();
        }

        public static void Load()
        {
            Addons = GetAddons();
        }

        public static List<Addon> Addons { get; set; }

        public static ObservableCollection<Addon> ToObservableCollection(List<Addon> p_addons)
        {
            var collection = new ObservableCollection<Addon>();
            for (int i = 0; i < p_addons.Count; i++)
            {
                collection.Add(p_addons[i]);
            }
            return collection;
        }

        public static List<Addon> ToList(ObservableCollection<Addon> p_addons)
        {
            var list = new List<Addon>();
            for (int i = 0; i < p_addons.Count; i++)
            {
                list.Add(p_addons[i]);
            }
            return list;
        }

        private static List<Addon> GetAddons()
        {
            string infoDir = Path.Combine(Places.paraworldDir, "Data", "Info");
            if (!Directory.Exists(infoDir)) return [];

            var errorMessages = new StringBuilder();
            var enabledAddons = Settings.Get("AddonMgr", "EnabledMods").Split([','], StringSplitOptions.RemoveEmptyEntries);
            var excludedInfos = new HashSet<string> { "BaseLocale.info", "LevelEd.info" };
            var addons = Directory.GetFiles(infoDir, "*.info")
                .Where(info => !excludedInfos.Contains(Path.GetFileName(info)) && !info.Contains("Locale_"))
                .Select(info =>
                {
                    var addon = ParseAddonInfo(info);
                    if (addon != null)
                    {
                        addon.IsEnabled = enabledAddons.Contains(addon.Id);
                        return addon;
                    }
                    errorMessages.AppendLine($"Error while reading {Path.GetFileName(info)} file. Addon id can't be null.");
                    return null;
                })
                .Where(addon => addon != null)
                .ToList();

            if (errorMessages.Length > 0)
            {
                Log.Error(errorMessages.ToString());
            }
            return addons;
        }

        private static Addon ParseAddonInfo(string p_filePath)
        {
            string id = "";
            string type = "";
            string version = "";
            var requires = new List<string>();

            using var readInfo = new StreamReader(p_filePath);
            while (!readInfo.EndOfStream)
            {
                string line = readInfo.ReadLine();
                if (line.StartsWith('#')) continue;

                var lineData = line.Split([' '], StringSplitOptions.RemoveEmptyEntries);
                switch (lineData[0])
                {
                    case "id":
                        id = lineData[1];
                        break;
                    case "type":
                        type = lineData[1];
                        break;
                    case "version":
                        version = lineData[1];
                        break;
                    case "requires":
                        requires.AddRange(lineData.Skip(1).Where(req => req != "BaseData"));
                        break;
                }
            }

            return string.IsNullOrEmpty(id) ? null : new Addon
            {
                Id = id,
                Type = type,
                Version = version,
                Requires = requires
            };
        }

        public static List<Addon> GetEnabledAddons(this List<Addon> p_addons)
        {
            if (p_addons == null) return [];
            return new(p_addons.Where(mod => mod.IsEnabled));
        }

        public static List<Addon> GetHiddenAddons(this List<Addon> p_addons)
        {
            if (p_addons == null) return [];
            return new(p_addons.Where(mod => mod.Type == "locale"));
        }

        public static Dictionary<string, List<string>> GetMissingAddons()
        {
            HashSet<string> enabledAddonsSet = GetEnabledAddons(Addons).Select(a => a.Id).ToHashSet();
            Dictionary<string, List<string>> missingAddons = new();

            foreach (var addon in Addons)
            {
                if (addon.IsEnabled)
                {
                    List<string> missingForAddon = new List<string>();
                    CheckMissingDependencies(addon, enabledAddonsSet, missingForAddon);

                    if (missingForAddon.Count > 0)
                    {
                        missingAddons[addon.Id] = missingForAddon;
                    }
                }
            }
            return missingAddons;
        }

        private static void CheckMissingDependencies(Addon addon, HashSet<string> enabledAddonsSet, List<string> missingForAddon)
        {
            foreach (var requiredMod in addon.Requires)
            {
                if (!enabledAddonsSet.Contains(requiredMod))
                {
                    missingForAddon.Add(requiredMod);

                    var missingAddon = Addons.FirstOrDefault(a => a.Id == requiredMod);
                    if (missingAddon != null)
                    {
                        CheckMissingDependencies(missingAddon, enabledAddonsSet, missingForAddon);
                    }
                }
            }
        }

        public static string GetMissingAddonsMsg(Dictionary<string, List<string>> p_missingAddons)
        {
            StringBuilder msg = new();
            msg.AppendLine(Locale.missingAddonsMain);
            msg.AppendLine();
            foreach (var addon in p_missingAddons)
            {
                msg.AppendLine($"{Locale.addon} {addon.Key}");
                msg.AppendLine(Locale.missingAddons);
                foreach (var missingAddon in addon.Value)
                {
                    msg.AppendLine($" - {missingAddon}");
                }
                msg.AppendLine();
            }
            return msg.ToString();
        }

        public static string GetCmdLine()
        {
            if (Addons.Count != 0)
            {
                string commandLine = " -enable " + string.Join(" -enable ", Addons.GetEnabledAddons().Select(addon => addon.Id));
                return commandLine;
            }
            return string.Empty;
        }

        public class Addon : INotifyPropertyChanged
        {
            private bool isEnabled;
            public bool IsEnabled
            {
                get => isEnabled;
                set
                {
                    if (isEnabled != value)
                    {
                        isEnabled = value;
                        OnPropertyChanged(nameof(IsEnabled));
                    }
                }
            }

            public string Id { get; set; }
            public string Type { get; set; }
            public string Version { get; set; }
            public List<string> Requires { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;
            protected virtual void OnPropertyChanged(string p_name)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p_name));
            }
        }
    }
}
