using MIRAGE.Shared.IO;
using ParaWorldStatus.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace ParaWorldStatus.Services;

/// <summary>
/// The map pictures and names for Discord, from supported_maps.json and map_translations.json (embedded in the exe).
/// <list type="bullet">
/// <item>supported_maps.json: <c>{ "group": { "single_05": ["multi_single_05", "koth_holy_city"] } }</c>. The key is a map
/// that has the picture "map_&lt;key&gt;" in the Discord application; the list has other map files that use the same
/// picture.</item>
/// <item>map_translations.json: <c>{ "group": { "single_05": "Mission 5: The Holy City" } }</c>: the name shown instead of
/// the name in the map file.</item>
/// </list>
/// The groups only sort the files; map file names are lower case and without ".ula".
/// </summary>
public sealed class MapCatalog
{
    public const string SupportedMapsResource = "supported_maps.json";
    public const string TranslationsResource = "map_translations.json";

    /// <summary>Settings with a picture "mapsetting_&lt;setting&gt;"; cave1 / cave2 are shown as savanna / ash valley.</summary>
    private static readonly HashSet<string> s_settings = new(StringComparer.Ordinal) { "ashvalley", "savanna", "jungle", "northland", "cave3", "icewaste" };

    private readonly Dictionary<string, string> m_mapImages;
    private readonly Dictionary<string, string> m_names;

    public MapCatalog(Dictionary<string, Dictionary<string, List<string>>> p_SupportedMaps, Dictionary<string, Dictionary<string, string>> p_Translations)
    {
        m_mapImages = new(StringComparer.OrdinalIgnoreCase);
        foreach (Dictionary<string, List<string>> group in p_SupportedMaps.Values)
        {
            foreach ((string image, List<string> others) in group)
            {
                m_mapImages[image] = image;
                foreach (string map in others)
                {
                    m_mapImages[map] = image;
                }
            }
        }
        m_names = new(StringComparer.OrdinalIgnoreCase);
        foreach (Dictionary<string, string> group in p_Translations.Values)
        {
            foreach ((string map, string name) in group)
            {
                m_names[map] = name;
            }
        }
    }

    public const string UnknownImage = "mod_unknown";

    public int MapCount => m_mapImages.Count;

    public int NameCount => m_names.Count;

    /// <summary>Reads both files from the resources of this assembly.</summary>
    public static MapCatalog LoadEmbedded()
    {
        Assembly assembly = typeof(MapCatalog).Assembly;
        return new MapCatalog(
            Read<Dictionary<string, Dictionary<string, List<string>>>>(assembly, SupportedMapsResource),
            Read<Dictionary<string, Dictionary<string, string>>>(assembly, TranslationsResource));
    }

    public static MapCatalog Load(Stream p_SupportedMaps, Stream p_Translations) => new(Deserialize<Dictionary<string, Dictionary<string, List<string>>>>(p_SupportedMaps), Deserialize<Dictionary<string, Dictionary<string, string>>>(p_Translations));

    /// <summary>The picture key: "map_&lt;map&gt;", else "mapsetting_&lt;setting&gt;", else "mod_unknown".</summary>
    public string GetImageKey(MapInfo p_Map)
    {
        if (m_mapImages.TryGetValue(p_Map.FileName, out string? image))
        {
            return "map_" + image;
        }
        string setting = p_Map.Setting.ToLowerInvariant() switch
        {
            "cave1" => "savanna",
            "cave2" => "ashvalley",
            string other => other,
        };
        return s_settings.Contains(setting) ? "mapsetting_" + setting : UnknownImage;
    }

    /// <summary>The translated name, else the name from the map file.</summary>
    public string GetDisplayName(MapInfo p_Map) => m_names.TryGetValue(p_Map.FileName, out string? name) ? name : p_Map.Name;

    private static T Read<T>(Assembly p_Assembly, string p_Name)
    {
        string resource = $"{p_Assembly.GetName().Name}.{p_Name}";
        using Stream stream = p_Assembly.GetManifestResourceStream(resource) ?? throw new FileNotFoundException($"Resource {resource} not found.");
        return Deserialize<T>(stream);
    }

    private static T Deserialize<T>(Stream p_Stream) => JsonSerializer.Deserialize<T>(p_Stream, JsonFile.Options) ?? throw new JsonException("The file contains 'null'.");
}
