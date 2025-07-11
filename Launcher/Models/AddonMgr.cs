﻿using MIRAGE_Launcher.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

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
            List<string> requires = [];
            List<string> excludes = [];

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
                    case "excludes":
                        excludes.AddRange(lineData.Skip(1).Where(req => req != "BaseData"));
                        break;
                }
            }

            return string.IsNullOrEmpty(id) ? null : new Addon
            {
                Id = id,
                Type = type,
                Version = version,
                Requires = requires,
                Excludes = excludes
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

        public static Dictionary<string, List<string>> GetAddonsDependencies(bool p_checkForMissing)
        {
            var enabledAddonsSet = GetEnabledAddons(Addons).Select(a => a.Id).ToHashSet();
            var addonsDependencies = new Dictionary<string, List<string>>();
            foreach (var addon in Addons.Where(a => a.IsEnabled))
            {
                var dependenciesForAddon = new List<string>();
                CheckDependencies(addon, enabledAddonsSet, dependenciesForAddon, p_checkForMissing);
                if (dependenciesForAddon.Count > 0)
                {
                    addonsDependencies[addon.Id] = dependenciesForAddon;
                }
            }
            return addonsDependencies;
        }

        private static void CheckDependencies(Addon p_addon, HashSet<string> p_enabledAddonsSet, List<string> p_dependenciesForAddon, bool p_checkForMissing)
        {
            var requiredMods = p_checkForMissing ? p_addon.Requires : p_addon.Excludes;
            foreach (var requiredMod in requiredMods)
            {
                if (p_checkForMissing ? !p_enabledAddonsSet.Contains(requiredMod) : p_enabledAddonsSet.Contains(requiredMod))
                {
                    p_dependenciesForAddon.Add(requiredMod);
                    var relatedAddon = Addons.FirstOrDefault(a => a.Id == requiredMod);
                    if (relatedAddon != null)
                    {
                        CheckDependencies(relatedAddon, p_enabledAddonsSet, p_dependenciesForAddon, p_checkForMissing);
                    }
                }
            }
        }

        public static string GetMissingAddonsMsg(Dictionary<string, List<string>> p_missingAddons, bool p_missing)
        {
            StringBuilder msg = new();

            msg.AppendLine(p_missing ? Locale.missingAddonsMain : Locale.excludedAddonsMain);
            msg.AppendLine();
            foreach (var addon in p_missingAddons)
            {
                msg.AppendLine($"{Locale.addon} {addon.Key}");
                msg.AppendLine(p_missing ? Locale.missingAddons : Locale.excludedAddons);
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

        public static string GetCmdLine(string[] p_addons)
        {
            if (p_addons == null || p_addons.Length == 0)
            {
                return string.Empty;
            }
            return " -enable " + string.Join(" -enable ", p_addons);
        }
    }
}
