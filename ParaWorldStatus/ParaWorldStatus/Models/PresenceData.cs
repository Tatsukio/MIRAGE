using System.Collections.Generic;

namespace ParaWorldStatus.Models;

public sealed record PresenceImage(string Key, string Text);

public sealed record PresenceParty(string Id, int Size, int Max);

public sealed record PresenceButton(string Label, string Url);

/// <summary>What Discord shows; DiscordService turns it into a DiscordRPC RichPresence. Texts are already cut to Discord's limits.</summary>
public sealed record PresenceData
{
    public string Details { get; init; } = string.Empty;

    public string State { get; init; } = string.Empty;

    public required PresenceImage LargeImage { get; init; }

    public PresenceImage? SmallImage { get; init; }

    public PresenceParty? Party { get; init; }

    public string? JoinSecret { get; init; }

    public string? SpectateSecret { get; init; }

    public IReadOnlyList<PresenceButton> Buttons { get; init; } = [];
}
