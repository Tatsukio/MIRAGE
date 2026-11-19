using System;
using System.IO;
using System.Text.Json;
using MIRAGE.Shared;
using MIRAGE.Shared.Cfg;
using MIRAGE.Shared.Localization;

namespace PWKiller.Core;

/// <summary>The two texts of the window, in the game language (Locale.json of the launcher), English if that fails.</summary>
public sealed record KillerTexts(string Title, string Button)
{
    public static KillerTexts English { get; } = new("If ParaWorld crashes or does not terminate, press this button.", "End processes");

    public static KillerTexts Load(GamePaths p_Paths)
    {
        LocaleTable table;
        try
        {
            if (!File.Exists(p_Paths.LauncherLocaleFile))
            {
                return English;
            }
            table = LocaleTable.Load(p_Paths.LauncherLocaleFile);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return English;
        }
        string language = ReadLanguage(p_Paths) ?? LocaleTable.FallbackLanguage;
        return new KillerTexts(table.Translate(language, "PWKillerTitleText"), table.Translate(language, "PWKillerButtonText"));
    }

    private static string? ReadLanguage(GamePaths p_Paths)
    {
        try
        {
            string? value = File.Exists(p_Paths.SettingsCfg) ? CfgDocument.Load(p_Paths.SettingsCfg).Get(GameSettings.Language) : null;
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CfgParseException)
        {
            return null;
        }
    }
}