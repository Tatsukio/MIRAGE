using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using MIRAGE.Shared.Localization;

namespace MIRAGE_Launcher.Services;

/// <summary>
/// The launcher texts in the current language. Bindable: <c>{Binding Text[StartModText]}</c> updates when the
/// language changes.
/// </summary>
public sealed class Localization(LocaleTable p_Table) : INotifyPropertyChanged
{
    private readonly LocaleTable m_table = p_Table;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Upper case, as in Locale.json (UK, DE ...).</summary>
    public string Language { get; private set; } = LocaleTable.FallbackLanguage;

    public IEnumerable<string> Languages => m_table.Languages;

    public string this[string p_Key] => m_table.Translate(Language, p_Key);

    public void SetLanguage(string p_Language)
    {
        string language = string.IsNullOrWhiteSpace(p_Language) ? LocaleTable.FallbackLanguage : p_Language.Trim().ToUpperInvariant();
        if (language == Language)
        {
            return;
        }
        Language = language;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
        // "Item[]" is how WPF names the indexer (Binding.IndexerName).
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }

    /// <summary>A message with its number, e.g. "LogMessage#07\nNo cache files found.", formatted with the arguments.</summary>
    public string Message(int p_Code, string p_Key, params object[] p_Args)
    {
        string text = this[p_Key];
        if (p_Args.Length > 0)
        {
            try
            {
                text = string.Format(CultureInfo.CurrentCulture, text, p_Args);
            }
            catch (System.FormatException)
            {
                text = $"{text} {string.Join(", ", p_Args)}";
            }
        }
        return $"LogMessage#{p_Code:00}\n{text}";
    }
}