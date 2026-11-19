using MIRAGE.Shared;
using MIRAGE.Shared.Logging;
using MIRAGE_Launcher.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MIRAGE_Launcher.Services;

/// <summary>The add-ons in Data\Info, which ones are enabled, and their requires / excludes rules.</summary>
public sealed class AddonService(GamePaths p_Paths, LauncherSettingsStore p_Settings, ILog p_Log)
{
    private static readonly HashSet<string> s_ignoredInfos = new(StringComparer.OrdinalIgnoreCase) { "BaseLocale.info", "LevelEd.info" };

    private readonly GamePaths m_paths = p_Paths;
    private readonly LauncherSettingsStore m_settings = p_Settings;
    private readonly ILog m_log = p_Log;
    private List<Addon> m_addons = [];

    public IReadOnlyList<Addon> Addons => m_addons;

    public IEnumerable<string> EnabledIds => m_addons.Where(static p_A => p_A.IsEnabled).Select(static p_A => p_A.Id);

    public IEnumerable<string> LocaleIds => m_addons.Where(static p_A => p_A.IsLocale).Select(static p_A => p_A.Id);

    /// <summary>The "-enable" arguments for the game.</summary>
    public static string BuildCommandLine(IEnumerable<string> p_Ids) => string.Join(' ', p_Ids.Select(static p_Id => $"-enable {p_Id}"));

    /// <summary>Reads a .info file; null if it has no id.</summary>
    public static Addon? ParseInfo(IEnumerable<string> p_Lines)
    {
        string id = string.Empty;
        string type = string.Empty;
        string version = string.Empty;
        List<string> requires = [];
        List<string> excludes = [];
        foreach (string line in p_Lines)
        {
            string[] words = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
            if ((words.Length < 2) || words[0].StartsWith('#'))
            {
                continue;
            }
            switch (words[0])
            {
                case "id":
                    id = words[1];
                    break;
                case "type":
                    type = words[1];
                    break;
                case "version":
                    version = words[1];
                    break;
                case "requires":
                    requires.AddRange(words.Skip(1).Where(static p_W => p_W != "BaseData"));
                    break;
                case "excludes":
                    excludes.AddRange(words.Skip(1).Where(static p_W => p_W != "BaseData"));
                    break;
                default:
                    break;
            }
        }
        return string.IsNullOrEmpty(id) ? null : new Addon { Id = id, Type = type, Version = version, Requires = requires, Excludes = excludes };
    }

    public void Load()
    {
        List<Addon> addons = [];
        if (Directory.Exists(m_paths.InfoDir))
        {
            HashSet<string> enabled = new(m_settings.Settings.Addons.Enabled, StringComparer.OrdinalIgnoreCase);
            StringBuilder errors = new();
            foreach (string file in Directory.GetFiles(m_paths.InfoDir, "*.info").Order(StringComparer.OrdinalIgnoreCase))
            {
                string name = Path.GetFileName(file);
                if (s_ignoredInfos.Contains(name) || name.Contains("Locale_", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                try
                {
                    Addon? addon = ParseInfo(File.ReadLines(file));
                    if (addon is null)
                    {
                        _ = errors.AppendLine($"{name}: no 'id' line.");
                        continue;
                    }
                    addon.IsEnabled = enabled.Contains(addon.Id);
                    addons.Add(addon);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    _ = errors.AppendLine($"{name}: {ex.Message}");
                }
                m_log.Info($"Add-on info {name} loaded");
            }
            if (errors.Length > 0)
            {
                m_log.Warn("Add-on info files with errors:\n" + errors);
            }
        }
        else
        {
            m_log.Warn($"{m_paths.InfoDir} not found.");
        }
        m_addons = addons;
    }

    public void SetEnabled(string p_Id, bool p_Enabled)
    {
        Addon? addon = m_addons.FirstOrDefault(p_A => p_A.Id == p_Id);
        if ((addon is null) || (addon.IsEnabled == p_Enabled))
        {
            return;
        }
        addon.IsEnabled = p_Enabled;

        // Keep enabled ids that have no .info file here (e.g. an add-on installed later) in the list.
        var ids = m_settings.Settings.Addons.Enabled
            .Where(p_Id => !m_addons.Any(p_A => string.Equals(p_A.Id, p_Id, StringComparison.OrdinalIgnoreCase)))
            .Concat(EnabledIds)
            .ToList();
        m_settings.Settings.Addons.Enabled = ids;
        _ = m_settings.Save();
    }

    /// <summary>Enabled add-on -> required add-ons that are not enabled (also those required by the required ones).</summary>
    public Dictionary<string, List<string>> FindMissingRequirements() => FindProblems(true);

    /// <summary>Enabled add-on -> enabled add-ons it excludes.</summary>
    public Dictionary<string, List<string>> FindConflicts() => FindProblems(false);

    private Dictionary<string, List<string>> FindProblems(bool p_Missing)
    {
        var enabled = EnabledIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, List<string>> result = [];
        foreach (Addon addon in m_addons.Where(static p_A => p_A.IsEnabled))
        {
            List<string> found = [];
            Collect(addon, enabled, found, p_Missing, []);
            if (found.Count > 0)
            {
                result[addon.Id] = found;
            }
        }
        return result;
    }

    private void Collect(Addon p_Addon, HashSet<string> p_Enabled, List<string> p_Found, bool p_Missing, HashSet<string> p_Visited)
    {
        if (!p_Visited.Add(p_Addon.Id))
        {
            // Requirement cycle.
            return;
        }
        foreach (string other in p_Missing ? p_Addon.Requires : p_Addon.Excludes)
        {
            bool problem = p_Missing ? !p_Enabled.Contains(other) : p_Enabled.Contains(other);
            if (!problem || p_Found.Contains(other, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }
            p_Found.Add(other);
            Addon? related = m_addons.FirstOrDefault(p_A => string.Equals(p_A.Id, other, StringComparison.OrdinalIgnoreCase));
            if (related is not null)
            {
                Collect(related, p_Enabled, p_Found, p_Missing, p_Visited);
            }
        }
    }
}