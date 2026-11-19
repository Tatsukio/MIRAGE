using ParaWorldStatus.Models;
using ParaWorldStatus.Services;
using System;
using System.Windows.Input;

namespace ParaWorldStatus.ViewModels;

/// <summary>
/// The small ParaWorldStatus window: shows whether Discord is connected and what it shows. Game state changes come from
/// <see cref="GameStateWatcher"/>, the presence goes to Discord through <paramref name="p_Publish"/>.
/// </summary>
public sealed class MainViewModel(PresenceBuilder p_Builder, Action<PresenceData> p_Publish, Action p_Refresh, Action p_Exit) : ViewModelBase
{
    private readonly PresenceBuilder m_builder = p_Builder;
    private readonly Action<PresenceData> m_publish = p_Publish;

    public bool IsConnected
    {
        get;
        private set
        {
            if (Set(ref field, value))
            {
                OnPropertyChanged(nameof(ConnectionText));
            }
        }
    }

    public string ConnectionText => IsConnected ? "Discord: connected" : "Discord: not connected";

    /// <summary>The first line Discord shows, e.g. "In multiplayer as 🐗 tribe".</summary>
    public string PresenceText
    {
        get;
        private set => Set(ref field, value);
    } = PresenceBuilder.Starting().Details;

    public ICommand RefreshCommand { get; } = new RelayCommand(p_Refresh);

    public ICommand ExitCommand { get; } = new RelayCommand(p_Exit);

    /// <summary>A new state from the game (any thread).</summary>
    public void OnStateChanged(GameState p_State)
    {
        PresenceData presence = m_builder.Build(p_State);
        m_publish(presence);
        PresenceText = string.IsNullOrEmpty(presence.State) ? presence.Details : $"{presence.Details} - {presence.State}";
    }

    public void OnConnectionChanged(bool p_Connected) => IsConnected = p_Connected;
}
