using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MIRAGE.Shared;

namespace MIRAGE_Launcher.Services;

/// <summary>The launcher backgrounds (Backgrounds\background_&lt;n&gt;.jpg) and the game music that goes with each.</summary>
public sealed class MediaCatalog(GamePaths p_Paths)
{
    private readonly GamePaths m_paths = p_Paths;
    private List<int>? m_ids;

    /// <summary>The numbers of the background files that exist.</summary>
    public IReadOnlyList<int> BackgroundIds => m_ids ??= FindBackgrounds();

    /// <summary>A random background other than <paramref name="p_Previous"/>; 0 if there are none.</summary>
    public int PickRandom(int p_Previous)
    {
        IReadOnlyList<int> ids = BackgroundIds;
        if (ids.Count == 0)
        {
            return 0;
        }
        if (ids.Count == 1)
        {
            return ids[0];
        }
        int id;
        do
        {
            id = ids[Random.Shared.Next(ids.Count)];
        }
        while (id == p_Previous);
        return id;
    }

    public string? GetBackground(int p_Id)
    {
        string path = Path.Combine(m_paths.BackgroundsDir, $"background_{p_Id}.jpg");
        return File.Exists(path) ? path : null;
    }

    public string? GetMusic(int p_Id)
    {
        string path = Path.Combine(m_paths.MusicDir, GetTrackName(p_Id) + ".mp3");
        return File.Exists(path) ? path : null;
    }

    /// <summary>The menu background image in Resources: the winter one from December to February.</summary>
    public static string GetMenuBackground(DateTime p_Date) => (p_Date.Month is 12 or 1 or 2) ? "menu_bg_winter.png" : "menu_bg.png";

    public static string GetTrackName(int p_Id) => p_Id switch
    {
        2 or 41 or 43 => "13_plain_icewaste",
        3 or 65 or 67 => "11_plain_jungle_1",
        4 or 48 => "23_location_arena",
        5 or 53 or 74 => "36_combat_aje_1",
        6 or 82 => "43_combat_ninigi_2",
        7 => "10_plain_heroes",
        8 or 45 or 54 or 83 => "35_combat_hu_1",
        9 => "04_plain_northland_1",
        10 or 60 => "41_combat_hu_2",
        11 => "16_darkzone_3",
        12 or 56 or 75 => "42_combat_aje_2",
        13 or 36 or 38 => "48_var_combat_aje_1",
        14 => "15_darkzone_2",
        15 or 73 or 85 => "17_maintheme_hu",
        16 or 50 or 81 => "47_var_combat_hu_2",
        17 or 61 or 71 => "50_var_combat_seas_1",
        18 or 66 or 78 => "12_plain_jungle_2",
        19 or 79 => "51_location_seas_temple",
        20 => "30_location_scientist_hut",
        21 or 35 or 55 or 57 => "39_combat_dinos",
        22 or 27 or 80 => "28_location_walhalla",
        23 or 24 => "32_location_holycity",
        25 => "26_location_water_temple",
        26 or 29 => "27_location_entry_to_walhalla",
        28 => "29_location_aeroplane",
        30 => "25_location_pirates",
        31 => "20_location_druids",
        32 => "06_plain_savannah_1",
        33 => "21_location_amazons",
        34 or 47 => "18_maintheme_aje",
        64 => "45_var_plain_savannah_1",
        37 => "05_plain_northland_2",
        52 or 76 => "07_plain_savannah_2",
        39 => "40_combat_heroes",
        40 => "08_plain_savannah_3",
        42 => "14_darkzone_1",
        44 => "34_location_prison_island",
        46 => "49_var_combat_ninigi_1",
        49 => "09_plain_savannah_4",
        51 => "37_combat_ninigi_1",
        58 or 70 or 72 => "38_combat_seas_1",
        59 => "19_maintheme_ninigi",
        62 or 63 => "44_var_plain_northland_1",
        68 => "33_location_holycity_walls",
        69 => "24_location_temple",
        77 => "46_var_plain_heroes",
        84 => "22_location_the_gate",
        _ => "01_maintheme",
    };

    private List<int> FindBackgrounds()
    {
        if (!Directory.Exists(m_paths.BackgroundsDir))
        {
            return [];
        }
        List<int> ids = [];
        foreach (string file in Directory.EnumerateFiles(m_paths.BackgroundsDir, "background_*.jpg"))
        {
            string name = Path.GetFileNameWithoutExtension(file);
            if (int.TryParse(name.AsSpan("background_".Length), out int id))
            {
                ids.Add(id);
            }
        }
        ids.Sort();
        return ids;
    }
}