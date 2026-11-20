using ParaWorldStatus.Models;
using System;
using System.Globalization;
using System.Text;

namespace ParaWorldStatus.Services;

/// <summary>Turns the game state into what Discord shows. No Discord types here, so it can be tested.</summary>
public sealed class PresenceBuilder(MapCatalog p_Maps, string p_PartyId)
{
    /// <summary>Discord's limit for texts, keys and secrets, in UTF-8 bytes (DiscordRPC throws above it).</summary>
    public const int MaxTextBytes = 128;

    public const string InGameAsSpectator = "In game as spectator";

    private static readonly PresenceButton[] s_buttons =
    [
        new("Visit ParaWelt", "https://para-welt.com/"),
        new("Download MIRAGE mod", "https://www.moddb.com/mods/paraworld-mirage"),
    ];

    private readonly MapCatalog m_maps = p_Maps;
    private readonly string m_partyId = p_PartyId;

    /// <summary>A new random party id (32 characters) for one run of the program.</summary>
    public static string NewPartyId() => Guid.NewGuid().ToString("N");

    /// <summary>The presence before the game has written a state.</summary>
    public static PresenceData Starting() => new()
    {
        Details = "Loading...",
        LargeImage = new PresenceImage("mod_basegame", "ParaWorld"),
        Buttons = s_buttons,
    };

    public PresenceData Build(GameState? p_State)
    {
        if (p_State is null)
        {
            return Starting();
        }
        if (p_State.StateType == GameState.SmallType)
        {
            return Starting() with
            {
                Details = Limit(p_State.Details),
                LargeImage = GetModImage(p_State.Mods, string.Empty),
            };
        }
        if (p_State.StateType != GameState.FullType)
        {
            return Starting();
        }

        var map = MapInfo.FromState(p_State);
        string details = (p_State.Details == InGameAsSpectator) ? p_State.Details : p_State.Details + GetTribeText(p_State.Tribe);
        PresenceData presence = Starting() with
        {
            Details = Limit(details),
            State = Limit((p_State.Additional == "Lobby") ? p_State.Additional : p_State.GameMode),
            LargeImage = new PresenceImage(Limit(m_maps.GetImageKey(map)), Limit(m_maps.GetDisplayName(map))),
            SmallImage = GetModImage(p_State.Mods, p_State.Additional),
            // Discord rejects a party with a size above its maximum.
            Party = (p_State.MaxPlayers > 0) ? new PresenceParty(m_partyId, Math.Clamp(p_State.NumPlayers, 1, p_State.MaxPlayers), p_State.MaxPlayers) : null,
        };

        // In a multiplayer lobby: "Join" (or "Spectate" when it is full) instead of the buttons. The secret is passed to
        // "MIRAGE Launcher.exe -PWSJoin -<secret>": address#addon|addon...
        if ((p_State.Details == "In multiplayer") && (p_State.Additional == "Lobby") && !string.IsNullOrEmpty(p_State.Server))
        {
            string secret = Limit(p_State.Server + "#" + string.Join('|', p_State.Mods.Split(' ', StringSplitOptions.RemoveEmptyEntries)));
            bool full = p_State.NumPlayers >= p_State.MaxPlayers;
            presence = presence with
            {
                Buttons = [],
                JoinSecret = full ? null : secret,
                SpectateSecret = full ? secret : null,
            };
        }
        return presence;
    }

    /// <summary>Cuts a text to <paramref name="p_MaxBytes"/> UTF-8 bytes without splitting a character.</summary>
    public static string Limit(string? p_Text, int p_MaxBytes = MaxTextBytes)
    {
        if (string.IsNullOrEmpty(p_Text))
        {
            return string.Empty;
        }
        if (Encoding.UTF8.GetByteCount(p_Text) <= p_MaxBytes)
        {
            return p_Text;
        }
        StringBuilder sb = new();
        int bytes = 0;
        TextElementEnumerator elements = StringInfo.GetTextElementEnumerator(p_Text);
        while (elements.MoveNext())
        {
            string element = elements.GetTextElement();
            int size = Encoding.UTF8.GetByteCount(element);
            if ((bytes + size) > (p_MaxBytes - 3))
            {
                break;
            }
            _ = sb.Append(element);
            bytes += size;
        }
        return sb.Append("...").ToString();
    }

    public static string GetTribeText(string p_Tribe) => p_Tribe switch
    {
        "Hu" => " as \U0001f417 tribe",
        "Aje" => " as \U0001f996 tribe",
        "Ninigi" => " as \U0001f432 tribe",
        "SEAS" => " as \U0001f9be tribe",
        "Random" => " as ❓ tribe",
        // MIRAGE tribe combinations
        "Aje_Ninigi_SEAS" => " as \U0001f996|\U0001f432|\U0001f9be tribe",
        "Ninigi_Hu_SEAS" => " as \U0001f432|\U0001f417|\U0001f9be tribe",
        "Aje_Hu_SEAS" => " as \U0001f996|\U0001f417|\U0001f9be tribe",
        "Aje_Ninigi_Hu" => " as \U0001f996|\U0001f432|\U0001f417 tribe",
        "Aje_Ninigi" => " as \U0001f996|\U0001f432 tribe",
        "Ninigi_Hu" => " as \U0001f432|\U0001f417 tribe",
        "Hu_SEAS" => " as \U0001f417|\U0001f9be tribe",
        "SEAS_Aje" => " as \U0001f9be|\U0001f996 tribe",
        "Aje_Hu" => " as \U0001f996|\U0001f417 tribe",
        "Ninigi_SEAS" => " as \U0001f432|\U0001f9be tribe",
        _ => string.Empty,
    };

    /// <summary>The add-on picture: map editor, MIRAGE or the base game; "game_paused" while the game is paused.</summary>
    public static PresenceImage GetModImage(string p_Mods, string p_Additional)
    {
        string mods = p_Mods ?? string.Empty;
        if (mods.Contains("LevelEd", StringComparison.Ordinal))
        {
            return new PresenceImage("state_editor", Limit(mods));
        }
        string key = mods.Contains("MIRAGE", StringComparison.Ordinal) ? "mod_mirage" : "mod_basegame";
        string text = mods;
        if (!string.IsNullOrEmpty(p_Additional) && (p_Additional != "Lobby"))
        {
            key = "game_paused";
            text = "Paused: " + p_Additional;
        }
        return new PresenceImage(key, Limit(text));
    }
}
