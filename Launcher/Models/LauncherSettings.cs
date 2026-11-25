using System;
using System.Collections.Generic;
using System.Linq;

namespace MIRAGE_Launcher.Models;

/// <summary>Tools\MIRAGE Launcher\Settings.json.</summary>
public sealed class LauncherSettings
{
    public LauncherOptions Launcher { get; set; } = new();

    public AddonOptions Addons { get; set; } = new();

    public PEST_ControlOptions PEST_Control { get; set; } = new();

    /// <summary>Puts the options a player can change back to the defaults; keeps the release data (version, URLs, hashes).</summary>
    public void ResetUserOptions()
    {
        LauncherOptions defaults = new();
        Launcher.EnableDiscordStatus = defaults.EnableDiscordStatus;
        Launcher.MusicOnStartup = defaults.MusicOnStartup;
        Launcher.SkipVideoCheck = defaults.SkipVideoCheck;
        Launcher.MultipleGameCopiesAllowed = defaults.MultipleGameCopiesAllowed;
        Addons.Enabled = [.. AddonOptions.DefaultEnabled];
    }
}

public sealed class LauncherOptions
{
    public string ModVersion { get; set; } = "0.0.0";

    public string ModBuild { get; set; } = string.Empty;

    /// <summary>The updateinfo.txt that tells the newest version.</summary>
    public string LatestVersionUrl { get; set; } = "https://raw.githubusercontent.com/Tatsukio/MIRAGE/master/updateinfo.txt";

    /// <summary>Returns the public IP as text; it is saved for the game in %TEMP%\...\paraworld_ip.txt. Empty: off.</summary>
    public string PublicIpUrl { get; set; } = "https://ipinfo.io/ip";

    /// <summary>Port of the UDP log broadcast; 0: off.</summary>
    public int UdpLogPort { get; set; } = 11023;

    public bool EnableDiscordStatus { get; set; } = true;

    public bool MusicOnStartup { get; set; } = true;

    /// <summary>Set after the "no translated videos" message was shown once.</summary>
    public bool SkipVideoCheck { get; set; }

    public bool MultipleGameCopiesAllowed { get; set; }
}

public sealed class AddonOptions
{
    public static IReadOnlyList<string> DefaultEnabled { get; } = ["BoosterPack3", "MIRAGE"];

    /// <summary>Ids of the enabled add-ons (the "id" line of Data\Info\*.info).</summary>
    public List<string> Enabled { get; set; } = [.. DefaultEnabled];
}

public sealed class PEST_ControlOptions
{
    public string RequiredVersion { get; set; } = string.Empty;

    /// <summary>Check the game exes before every start.</summary>
    public bool CheckHashes { get; set; } = true;

    /// <summary>
    /// File name in bin (Paraworld.exe, PWClient.exe, PWServer.exe) -> accepted SHA-256 hashes. A file without
    /// hashes isn't checked.
    /// </summary>
    public Dictionary<string, List<string>> Hashes { get; set; } = [];

    public IReadOnlyList<string> GetHashes(string p_FileName)
    {
        foreach ((string file, List<string> hashes) in Hashes)
        {
            if (string.Equals(file, p_FileName, StringComparison.OrdinalIgnoreCase))
            {
                return [.. hashes.Where(static p_H => !string.IsNullOrWhiteSpace(p_H))];
            }
        }
        return [];
    }
}