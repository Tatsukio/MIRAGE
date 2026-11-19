using System;
using MIRAGE_Launcher.Models;

namespace MIRAGE_Launcher.ViewModels;

/// <summary>One line of the add-on list (check box, id, version).</summary>
public sealed class AddonViewModel(Addon p_Addon, Action<string, bool> p_OnEnabledChanged) : ViewModelBase
{
    private readonly Addon m_addon = p_Addon;
    private readonly Action<string, bool> m_onEnabledChanged = p_OnEnabledChanged;

    public string Id => m_addon.Id;

    public string Version => m_addon.Version;

    public bool IsEnabled
    {
        get => m_addon.IsEnabled;
        set
        {
            if (m_addon.IsEnabled == value)
            {
                return;
            }
            m_onEnabledChanged(m_addon.Id, value);
            OnPropertyChanged();
        }
    }
}