using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ParaWorldStatus.Models;

/// <summary>
/// The line the game writes to DiscordState\state_mirage.txt (GameStateReporter.usl):
/// <c>GSRVersion=1,StateType=StateFull,Details=...,Additional=...,Tribe=...,MapFile=...,MapName=...,MapSetting=...,GameMode=...,Mods=...,Server=...,NumPlayers=2,MaxPlayers=4</c>
/// or <c>GSRVersion=1,StateType=StateSmall,Details=...,Mods=...</c>.
/// </summary>
public sealed partial record GameState
{
    public const string SmallType = "StateSmall";
    public const string FullType = "StateFull";

    public string StateType { get; init; } = string.Empty;

    public string Details { get; init; } = string.Empty;

    /// <summary>"Lobby", or the reason the game is paused.</summary>
    public string Additional { get; init; } = string.Empty;

    public string Tribe { get; init; } = string.Empty;

    public string MapFile { get; init; } = string.Empty;

    public string MapName { get; init; } = string.Empty;

    public string MapSetting { get; init; } = string.Empty;

    public string GameMode { get; init; } = string.Empty;

    /// <summary>Add-on ids separated by spaces, e.g. "BoosterPack1 MIRAGE".</summary>
    public string Mods { get; init; } = string.Empty;

    /// <summary>Address of the server, e.g. "1.2.3.4:6570".</summary>
    public string Server { get; init; } = string.Empty;

    public int NumPlayers { get; init; }

    public int MaxPlayers { get; init; }

    /// <summary>
    /// Reads one state line. Values may contain commas (map names): a comma only starts the next field when a known key
    /// and '=' follow it. Returns null for an empty line or another GSRVersion than 1.
    /// </summary>
    public static GameState? Parse(string? p_Line)
    {
        if (string.IsNullOrWhiteSpace(p_Line))
        {
            return null;
        }
        Dictionary<string, string> fields = new(StringComparer.Ordinal);
        foreach (string part in FieldSeparator().Split(p_Line.Trim()))
        {
            int equals = part.IndexOf('=');
            if (equals > 0)
            {
                fields[part[..equals]] = part[(equals + 1)..];
            }
        }
        return !fields.TryGetValue("GSRVersion", out string? version) || (version != "1")
            ? null
            : new GameState
            {
                StateType = Get(fields, "StateType"),
                Details = Get(fields, "Details"),
                Additional = Get(fields, "Additional"),
                Tribe = Get(fields, "Tribe"),
                MapFile = Get(fields, "MapFile"),
                MapName = Get(fields, "MapName"),
                MapSetting = Get(fields, "MapSetting"),
                GameMode = Get(fields, "GameMode"),
                Mods = Get(fields, "Mods"),
                Server = Get(fields, "Server"),
                NumPlayers = GetInt(fields, "NumPlayers"),
                MaxPlayers = GetInt(fields, "MaxPlayers"),
            };
    }

    private static string Get(Dictionary<string, string> p_Fields, string p_Key) => p_Fields.TryGetValue(p_Key, out string? value) ? value.Trim() : string.Empty;

    private static int GetInt(Dictionary<string, string> p_Fields, string p_Key) => int.TryParse(Get(p_Fields, p_Key), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : 0;

    [GeneratedRegex(",(?=(?:GSRVersion|StateType|Details|Additional|Tribe|MapFile|MapName|MapSetting|GameMode|Mods|Server|NumPlayers|MaxPlayers)=)")]
    private static partial Regex FieldSeparator();
}
