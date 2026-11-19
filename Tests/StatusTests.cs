using ParaWorldStatus.Models;
using ParaWorldStatus.Services;
using ParaWorldStatus.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MIRAGE.Tools.Tests;

internal static class StatusTests
{
    private const string c_Lobby =
        "GSRVersion=1,StateType=StateFull,Details=In multiplayer,Additional=Lobby,Tribe=Hu,MapFile=single_05.ula," +
        "MapName=Holy City, the,MapSetting=cave1,GameMode=Deathmatch,Mods=BoosterPack1 MIRAGE,Server=1.2.3.4:6570,NumPlayers=2,MaxPlayers=4";

    [Test]
    private static void ParseStateLine()
    {
        var state = GameState.Parse(c_Lobby);
        Assert.NotNull(state);
        Assert.Equal(GameState.FullType, state!.StateType);
        Assert.Equal("Holy City, the", state.MapName, "a comma inside a value");
        Assert.Equal("BoosterPack1 MIRAGE", state.Mods);
        Assert.Equal(2, state.NumPlayers);
        Assert.Equal(4, state.MaxPlayers);

        var small = GameState.Parse("GSRVersion=1,StateType=StateSmall,Details=In main menu,Mods=MIRAGE");
        Assert.Equal("In main menu", small!.Details);
        Assert.Equal(string.Empty, small.Server);

        Assert.Null(GameState.Parse(string.Empty));
        Assert.Null(GameState.Parse("GSRVersion=2,StateType=StateSmall"), "unknown version");
        Assert.Equal(0, GameState.Parse("GSRVersion=1,StateType=StateFull,NumPlayers=x")!.NumPlayers, "bad number doesn't crash");
    }

    [Test]
    private static void MapCatalogFromJson()
    {
        MapCatalog maps = LoadMaps();
        Assert.Equal(121, maps.MapCount, "all map files of supported_maps.txt");
        Assert.Equal(114, maps.NameCount, "all names of map_translations.txt");
        Assert.Equal("map_single_05", maps.GetImageKey(new MapInfo("koth_holy_city", "x", "savanna")));
        Assert.Equal("map_multi_4_sav_901", maps.GetImageKey(new MapInfo("single_03 mp", "x", "")));
        Assert.Equal("mapsetting_savanna", maps.GetImageKey(new MapInfo("my_map", "x", "Cave1")));
        Assert.Equal("mapsetting_cave3", maps.GetImageKey(new MapInfo("my_map", "x", "cave3")));
        Assert.Equal(MapCatalog.UnknownImage, maps.GetImageKey(new MapInfo("my_map", "x", "moon")));
        Assert.Equal("Mission 0: Tutorial", maps.GetDisplayName(new MapInfo("single_00", "x", "")));
        Assert.Equal("My Map", maps.GetDisplayName(new MapInfo("my_map", "My Map", "")));
    }

    [Test]
    private static void PresenceFromStates()
    {
        PresenceBuilder builder = new(LoadMaps(), "party");
        PresenceData lobby = builder.Build(GameState.Parse(c_Lobby));
        Assert.Equal("In multiplayer as \U0001f417 tribe", lobby.Details);
        Assert.Equal("Lobby", lobby.State);
        Assert.Equal("map_single_05", lobby.LargeImage.Key);
        Assert.Equal("Mission 5: The Holy City", lobby.LargeImage.Text);
        Assert.Equal("mod_mirage", lobby.SmallImage!.Key);
        Assert.Equal("1.2.3.4:6570#BoosterPack1|MIRAGE", lobby.JoinSecret);
        Assert.Null(lobby.SpectateSecret);
        Assert.Equal(0, lobby.Buttons.Count, "no buttons with Join");
        Assert.Equal(new PresenceParty("party", 2, 4), lobby.Party);

        PresenceData full = builder.Build(GameState.Parse(c_Lobby.Replace("NumPlayers=2", "NumPlayers=4")));
        Assert.Null(full.JoinSecret);
        Assert.Equal("1.2.3.4:6570#BoosterPack1|MIRAGE", full.SpectateSecret);

        PresenceData paused = builder.Build(GameState.Parse(c_Lobby.Replace("Additional=Lobby", "Additional=Player left").Replace("Details=In multiplayer", "Details=In game as spectator")));
        Assert.Equal("In game as spectator", paused.Details, "no tribe for spectators");
        Assert.Equal("Deathmatch", paused.State);
        Assert.Equal(new PresenceImage("game_paused", "Paused: Player left"), paused.SmallImage);
        Assert.Equal(2, paused.Buttons.Count);

        PresenceData editor = builder.Build(GameState.Parse("GSRVersion=1,StateType=StateSmall,Details=In the map editor,Mods=LevelEd MIRAGE"));
        Assert.Equal("state_editor", editor.LargeImage.Key);

        PresenceData noParty = builder.Build(GameState.Parse("GSRVersion=1,StateType=StateFull,Details=In singleplayer,MapFile=x.ula,MaxPlayers=0"));
        Assert.Null(noParty.Party, "Discord rejects a party without a maximum");
        Assert.Equal("Loading...", builder.Build(null).Details);
    }

    [Test]
    private static void TextLimits()
    {
        string longText = string.Concat(Enumerable.Repeat("\U0001f417ab", 60));
        string cut = PresenceBuilder.Limit(longText);
        Assert.True(System.Text.Encoding.UTF8.GetByteCount(cut) <= PresenceBuilder.MaxTextBytes, cut);
        Assert.True(cut.EndsWith("...", StringComparison.Ordinal));
        Assert.False(char.IsHighSurrogate(cut[^4]), "no half character");
        Assert.Equal("short", PresenceBuilder.Limit("short"));
        Assert.Equal(string.Empty, PresenceBuilder.Limit(null));
    }

    [Test]
    private static void WatcherReadsTheStateFile()
    {
        using TempDir dir = new();
        string folder = Path.Combine(dir.Path, "DiscordState");
        using GameStateWatcher watcher = new(folder, new ListLog());
        List<GameState> states = [];
        watcher.StateChanged += states.Add;
        watcher.Start();
        Assert.True(Directory.Exists(folder), "the folder is created");
        Assert.Equal(0, states.Count, "no file yet");

        File.WriteAllText(watcher.StateFile, c_Lobby + "\r\n");
        watcher.ReadNow();
        watcher.ReadNow();
        Assert.Equal(1, states.Count, "the same line only once");
        File.Delete(watcher.StateFile);
        watcher.ReadNow();
        Assert.Equal(1, states.Count, "a deleted file is ignored");
        File.WriteAllText(watcher.StateFile, "GSRVersion=1,StateType=StateSmall,Details=In main menu,Mods=MIRAGE");
        watcher.ReadNow();
        Assert.Equal("In main menu", states[^1].Details);
    }

    [Test]
    private static void JoinStartsTheLauncher()
    {
        System.Diagnostics.ProcessStartInfo info = LauncherStarter.CreateStartInfo(@"C:\Game\Tools\MIRAGE Launcher\MIRAGE Launcher.exe", "1.2.3.4:6570#MIRAGE");
        Assert.Equal("-PWSJoin|-1.2.3.4:6570#MIRAGE", string.Join('|', info.ArgumentList));
        Assert.True(MIRAGE_Launcher.Services.GameStartService.TryParseServerInfo(info.ArgumentList[1], out string address, out List<string> addons), "the launcher accepts it");
        Assert.Equal("1.2.3.4:6570", address);
        Assert.Equal("MIRAGE", addons.Single());
        Assert.Equal("missing.exe not found.", Path.GetFileName(new LauncherStarter("missing.exe", new ListLog()).Join("x") ?? string.Empty));
    }

    [Test]
    private static void StatusViewModel()
    {
        List<PresenceData> published = [];
        bool exited = false;
        int refreshed = 0;
        MainViewModel vm = new(new PresenceBuilder(LoadMaps(), "p"), published.Add, () => refreshed++, () => exited = true);
        Assert.Equal("Discord: not connected", vm.ConnectionText);
        vm.OnConnectionChanged(true);
        Assert.Equal("Discord: connected", vm.ConnectionText);
        vm.OnStateChanged(GameState.Parse(c_Lobby)!);
        Assert.Equal(1, published.Count);
        Assert.Equal("In multiplayer as \U0001f417 tribe - Lobby", vm.PresenceText);
        vm.RefreshCommand.Execute(null);
        vm.ExitCommand.Execute(null);
        Assert.Equal(1, refreshed);
        Assert.True(exited);
    }

    private static MapCatalog LoadMaps()
    {
        string? folder = RepoPaths.Find("ParaWorldStatus/ParaWorldStatus");
        Assert.NotNull(folder);
        using FileStream maps = File.OpenRead(Path.Combine(folder!, MapCatalog.SupportedMapsResource));
        using FileStream names = File.OpenRead(Path.Combine(folder!, MapCatalog.TranslationsResource));
        return MapCatalog.Load(maps, names);
    }
}
