using MIRAGE.Shared.IO;
using MIRAGE.Shared.Logging;
using MIRAGE_Launcher.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace MIRAGE_Launcher.Services;

/// <summary>Loads and saves Settings.json. Takes over the player's choices from an old Settings.xml once.</summary>
public sealed class LauncherSettingsStore(string p_FilePath, string p_LegacyXmlPath, ILog p_Log)
{
    private readonly string m_legacyXmlPath = p_LegacyXmlPath;
    private readonly ILog m_log = p_Log;

    public LauncherSettings Settings { get; private set; } = new();

    public string FilePath { get; } = p_FilePath;

    /// <summary>Set by <see cref="Load"/> when the file was damaged and had to be replaced by defaults.</summary>
    public string? LoadProblem { get; private set; }

    public void Load()
    {
        LoadProblem = null;
        if (File.Exists(FilePath))
        {
            try
            {
                Settings = JsonFile.Load<LauncherSettings>(FilePath);
                Normalize();
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
            {
                LoadProblem = $"{FilePath}: {ex.Message}";
                m_log.Error($"Settings.json can't be read, using defaults: {ex.Message}");
                Settings = new LauncherSettings();
                Backup(".broken");
                _ = Save();
            }
        }
        else
        {
            m_log.Warn($"{FilePath} not found, using defaults.");
            Settings = new LauncherSettings();
            _ = Save();
        }

        if (File.Exists(m_legacyXmlPath))
        {
            ImportLegacyXml();
        }
    }

    /// <summary>Saves the file; false if that failed (logged).</summary>
    public bool Save()
    {
        try
        {
            JsonFile.Save(FilePath, Settings);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            m_log.Error($"Could not save {FilePath}: {ex.Message}");
            return false;
        }
    }

    private static bool? ReadBool(XElement p_Section, string p_Name)
    {
        string? text = p_Section.Element(p_Name)?.Value;
        return bool.TryParse(text?.Trim(), out bool value) ? value : null;
    }

    private void Normalize()
    {
        Settings.Launcher ??= new LauncherOptions();
        Settings.Addons ??= new AddonOptions();
        Settings.Addons.Enabled ??= [.. AddonOptions.DefaultEnabled];
        Settings.PEST_Control ??= new PEST_ControlOptions();
        Settings.PEST_Control.Hashes ??= [];
        Settings.Addons.Enabled = [.. Settings.Addons.Enabled
            .Where(static p_Id => !string.IsNullOrWhiteSpace(p_Id))
            .Select(static p_Id => p_Id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// The installer replaces Settings.json, but an old Settings.xml next to it still has the player's choices
    /// (enabled add-ons, music, Discord status). They are copied once, then the XML is renamed to
    /// Settings.xml.imported. Version, URLs and hashes always come from Settings.json.
    /// </summary>
    private void ImportLegacyXml()
    {
        try
        {
            var root = XElement.Load(m_legacyXmlPath);
            XElement? launcher = root.Element("Launcher");
            if (launcher is not null)
            {
                LauncherOptions options = Settings.Launcher;
                options.EnableDiscordStatus = ReadBool(launcher, "bEnableDiscordStatus") ?? options.EnableDiscordStatus;
                options.MusicOnStartup = ReadBool(launcher, "bMusicOnStartup") ?? options.MusicOnStartup;
                options.SkipVideoCheck = ReadBool(launcher, "bSkipVideoCheck") ?? options.SkipVideoCheck;
                options.MultipleGameCopiesAllowed = ReadBool(launcher, "bMultipleGameCopiesAllowed") ?? options.MultipleGameCopiesAllowed;
            }
            string? enabled = root.Element("AddonMgr")?.Element("EnabledMods")?.Value;
            if (enabled is not null)
            {
                List<string> ids = [.. enabled.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
                if (ids.Count > 0)
                {
                    Settings.Addons.Enabled = ids;
                }
            }
            _ = Save();
            File.Move(m_legacyXmlPath, m_legacyXmlPath + ".imported", true);
            m_log.Info("Imported the player's choices from Settings.xml.");
        }
        catch (Exception ex) when (ex is XmlException or IOException or UnauthorizedAccessException)
        {
            m_log.Warn($"Could not import {m_legacyXmlPath}: {ex.Message}");
        }
    }

    private void Backup(string p_Suffix)
    {
        try
        {
            File.Copy(FilePath, FilePath + p_Suffix, true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            m_log.Warn($"Could not back up {FilePath}: {ex.Message}");
        }
    }
}