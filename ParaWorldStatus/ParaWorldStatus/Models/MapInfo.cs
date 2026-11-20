using System.IO;

namespace ParaWorldStatus.Models;

/// <summary>The map of the current game. <see cref="FileName"/> is lower case and without ".ula".</summary>
public sealed record MapInfo(string FileName, string Name, string Setting)
{
    public static MapInfo FromState(GameState p_State) => new(Path.GetFileNameWithoutExtension(p_State.MapFile ?? string.Empty).ToLowerInvariant(), p_State.MapName, p_State.MapSetting);
}
