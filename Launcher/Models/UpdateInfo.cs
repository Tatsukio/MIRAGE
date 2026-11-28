using System;

namespace MIRAGE_Launcher.Models;

/// <summary>
/// The online updateinfo.txt, one line with tabs:
/// <c>versioncheck	MIRAGE 2.6.7	20	0	change 1;change 2;...</c>
/// </summary>
public sealed record UpdateInfo(Version Version, string Changes)
{
    public static bool TryParse(string p_Text, out UpdateInfo? p_Info)
    {
        p_Info = null;
        if (string.IsNullOrWhiteSpace(p_Text))
        {
            return false;
        }
        string[] fields = p_Text.Trim().Split('\t');
        if ((fields.Length < 2) || (fields[0].Trim() != "versioncheck"))
        {
            return false;
        }
        string[] name = fields[1].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if ((name.Length == 0) || !Version.TryParse(name[^1], out Version? version))
        {
            return false;
        }
        p_Info = new UpdateInfo(version, (fields.Length > 4) ? fields[4].Trim() : string.Empty);
        return true;
    }

    /// <summary>The changes as a list with one "●" line each.</summary>
    public string FormatChanges()
    {
        string[] items = Changes.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return (items.Length == 0) ? string.Empty : "● " + string.Join("\n● ", items);
    }
}