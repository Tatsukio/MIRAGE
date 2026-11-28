using MIRAGE.Shared.IO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MIRAGE.Shared.Localization;

/// <summary>
/// The texts of Locale.json: <c>{ "UK": { "Key": "Text", ... }, "DE": { ... } }</c>. A key missing in a language
/// falls back to UK, then to the key itself.
/// </summary>
public sealed class LocaleTable
{
    public const string FallbackLanguage = "UK";

    private readonly Dictionary<string, Dictionary<string, string>> m_languages;

    public LocaleTable(Dictionary<string, Dictionary<string, string>> p_Languages)
    {
        m_languages = new(StringComparer.OrdinalIgnoreCase);
        foreach ((string language, Dictionary<string, string> texts) in p_Languages)
        {
            m_languages[language] = new Dictionary<string, string>(texts, StringComparer.Ordinal);
        }
    }

    public static LocaleTable Empty { get; } = new([]);

    public IEnumerable<string> Languages => m_languages.Keys.Select(static p_L => p_L.ToUpperInvariant());

    public static LocaleTable Load(string p_FilePath) => new(JsonFile.Load<Dictionary<string, Dictionary<string, string>>>(p_FilePath));

    public bool HasLanguage(string p_Language) => m_languages.ContainsKey(p_Language);

    public string Translate(string p_Language, string p_Key)
    {
        return m_languages.TryGetValue(p_Language, out Dictionary<string, string>? texts) && texts.TryGetValue(p_Key, out string? text)
            ? text
            : m_languages.TryGetValue(FallbackLanguage, out texts) && texts.TryGetValue(p_Key, out text) ? text : p_Key;
    }
}