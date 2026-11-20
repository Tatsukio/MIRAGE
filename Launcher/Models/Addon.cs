using System.Collections.Generic;

namespace MIRAGE_Launcher.Models;

/// <summary>An add-on from Data\Info\&lt;name&gt;.info.</summary>
public sealed class Addon
{
    public required string Id { get; init; }

    public string Type { get; init; } = string.Empty;

    public string Version { get; init; } = string.Empty;

    public IReadOnlyList<string> Requires { get; init; } = [];

    public IReadOnlyList<string> Excludes { get; init; } = [];

    public bool IsEnabled { get; set; }

    /// <summary>Locale add-ons are passed to the game when joining a server through the Discord status.</summary>
    public bool IsLocale => Type == "locale";
}