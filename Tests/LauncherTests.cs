using MIRAGE.Shared.Cfg;
using MIRAGE.Shared.IO;
using MIRAGE_Launcher.Models;
using MIRAGE_Launcher.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MIRAGE.Tools.Tests;

internal static class LauncherTests
{
    [Test]
    private static void SettingsJsonRoundTrip()
    {
        using TempDir dir = new();
        string file = dir.File("Settings.json");
        LauncherSettingsStore store = new(file, dir.File("Settings.xml"), new ListLog());
        store.Load();
        Assert.True(File.Exists(file), "defaults are written");
        store.Settings.Launcher.ModVersion = "2.7.0";
        store.Settings.PEST_Control.Hashes["PWClient.exe"] = ["ABC"];
        Assert.True(store.Save());

        LauncherSettingsStore again = new(file, dir.File("Settings.xml"), new ListLog());
        again.Load();
        Assert.Equal("2.7.0", again.Settings.Launcher.ModVersion);
        Assert.Equal("ABC", again.Settings.PEST_Control.GetHashes("pwclient.EXE").Single(), "file names are not case sensitive");
        Assert.Equal(0, again.Settings.PEST_Control.GetHashes("PWServer.exe").Count);
        Assert.Null(again.LoadProblem);
    }

    [Test]
    private static void ShippedSettingsJsonLoads()
    {
        string? file = RepoPaths.Find("Launcher/Settings.json");
        Assert.NotNull(file, "Launcher/Settings.json");
        LauncherSettings settings = JsonFile.Load<LauncherSettings>(file!);
        Assert.Equal(1, settings.PEST_Control.GetHashes("Paraworld.exe").Count);
        Assert.Equal(1, settings.PEST_Control.GetHashes("PWClient.exe").Count);
        Assert.Equal(1, settings.PEST_Control.GetHashes("PWServer.exe").Count);
        Assert.True(settings.PEST_Control.Hashes.Values.SelectMany(static h => h).All(static h => h.Length == 64));
        Assert.Equal("BoosterPack3,MIRAGE", string.Join(',', settings.Addons.Enabled));
    }

    [Test]
    private static void BrokenSettingsJsonIsReplaced()
    {
        using TempDir dir = new();
        string file = dir.File("Settings.json", "{ \"Launcher\": { \"ModVersion\": ");
        LauncherSettingsStore store = new(file, dir.File("Settings.xml"), new ListLog());
        store.Load();
        Assert.NotNull(store.LoadProblem);
        Assert.True(File.Exists(file + ".broken"));
        Assert.Equal("BoosterPack3,MIRAGE", string.Join(',', store.Settings.Addons.Enabled));
        Assert.True(JsonFile.Load<LauncherSettings>(file) is not null, "valid again");
    }

    [Test]
    private static void LegacyXmlIsImportedOnce()
    {
        using TempDir dir = new();
        string json = dir.File("Settings.json");
        string xml = dir.File("Settings.xml",
            "<?xml version = \"1.0\"?>\n<Settings>\n\t<Launcher>\n\t\t<ModVersion>2.6.0</ModVersion>\n\t\t<bEnableDiscordStatus>false</bEnableDiscordStatus>\n" +
            "\t\t<bMusicOnStartup>false</bMusicOnStartup>\n\t\t<bSkipVideoCheck>true</bSkipVideoCheck>\n\t</Launcher>\n" +
            "\t<AddonMgr>\n\t\t<EnabledMods>BoosterPack3,MIRAGE,Wintermod</EnabledMods>\n\t</AddonMgr>\n</Settings>");
        LauncherSettingsStore store = new(json, xml, new ListLog());
        store.Load();
        Assert.False(store.Settings.Launcher.EnableDiscordStatus);
        Assert.False(store.Settings.Launcher.MusicOnStartup);
        Assert.True(store.Settings.Launcher.SkipVideoCheck);
        Assert.Equal("0.0.0", store.Settings.Launcher.ModVersion, "the version is not taken from the XML");
        Assert.Equal("BoosterPack3,MIRAGE,Wintermod", string.Join(',', store.Settings.Addons.Enabled));
        Assert.False(File.Exists(xml));
        Assert.True(File.Exists(xml + ".imported"));

        store.Settings.ResetUserOptions();
        Assert.True(store.Settings.Launcher.EnableDiscordStatus);
        Assert.Equal("BoosterPack3,MIRAGE", string.Join(',', store.Settings.Addons.Enabled));
    }

    [Test]
    private static void UpdateInfoParsing()
    {
        Assert.True(UpdateInfo.TryParse("versioncheck\tMIRAGE 2.6.7\t20\t0\tAlly control feature;COOP campaign lobby presets\n", out UpdateInfo? info));
        Assert.Equal(new System.Version(2, 6, 7), info!.Version);
        Assert.Equal("● Ally control feature\n● COOP campaign lobby presets", info.FormatChanges());
        Assert.True(UpdateInfo.TryParse("versioncheck\tMIRAGE 3.0", out info), "no change list");
        Assert.Equal(string.Empty, info!.FormatChanges());
        Assert.False(UpdateInfo.TryParse("<html>404</html>", out _));
        Assert.False(UpdateInfo.TryParse("versioncheck\tMIRAGE x.y", out _));
        Assert.False(UpdateInfo.TryParse(string.Empty, out _));
        string? real = RepoPaths.Find("updateinfo.txt");
        if (real is not null)
        {
            Assert.True(UpdateInfo.TryParse(File.ReadAllText(real), out _), "updateinfo.txt of the repository");
        }
    }

    [Test]
    private static void AddonInfoAndRules()
    {
        Addon? addon = AddonService.ParseInfo(["# comment", string.Empty, "id   MIRAGE", "requires BoosterPack3 BaseData", "excludes", "type add-on", "version 2.6.8"]);
        Assert.NotNull(addon);
        Assert.Equal("BoosterPack3", string.Join(',', addon!.Requires), "BaseData is ignored");
        Assert.Equal(0, addon.Excludes.Count, "an empty line or a key without value doesn't crash");
        Assert.Null(AddonService.ParseInfo(["type add-on", "id"]));
        Assert.Equal("-enable A -enable B", AddonService.BuildCommandLine(["A", "B"]));
        Assert.Equal(string.Empty, AddonService.BuildCommandLine([]));

        using FakeGame game = new();
        LauncherSettingsStore settings = new(game.Paths.LauncherSettingsFile, game.Paths.LegacyLauncherSettingsFile, game.Log);
        settings.Load();
        settings.Settings.Addons.Enabled = ["MIRAGE", "NotInstalled"];
        AddonService addons = new(game.Paths, settings, game.Log);
        addons.Load();
        Assert.Equal("BoosterPack3,MIRAGE,Wintermod", string.Join(',', addons.Addons.Select(static a => a.Id)), "Locale_ files are not listed");
        Assert.Equal("MIRAGE", string.Join(',', addons.EnabledIds));
        Assert.Equal("BoosterPack3", string.Join(',', addons.FindMissingRequirements()["MIRAGE"]));

        addons.SetEnabled("BoosterPack3", true);
        Assert.Equal(0, addons.FindMissingRequirements().Count);
        Assert.Equal("NotInstalled,BoosterPack3,MIRAGE", string.Join(',', settings.Settings.Addons.Enabled), "unknown ids are kept");
        Assert.Equal("NotInstalled,BoosterPack3,MIRAGE", string.Join(',', JsonFile.Load<LauncherSettings>(game.Paths.LauncherSettingsFile).Addons.Enabled), "saved");
    }

    [Test]
    private static void ServerInfoFromDiscord()
    {
        Assert.True(GameStartService.TryParseServerInfo("-192.168.1.2:6570#BoosterPack3|MIRAGE", out string address, out List<string> addons));
        Assert.Equal("192.168.1.2:6570", address);
        Assert.Equal("BoosterPack3,MIRAGE", string.Join(',', addons));
        Assert.True(GameStartService.TryParseServerInfo("host.example.org#MIRAGE", out _, out _));
        Assert.False(GameStartService.TryParseServerInfo("1.2.3.4", out _, out _), "no add-on part");
        Assert.False(GameStartService.TryParseServerInfo("1.2.3.4 -dedicated#MIRAGE", out _, out _), "no extra arguments");
        Assert.False(GameStartService.TryParseServerInfo("1.2.3.4#MIRAGE -enable X", out _, out _));
        Assert.False(GameStartService.TryParseServerInfo("\"x\"#MIRAGE", out _, out _));
        Assert.False(GameStartService.TryParseServerInfo("a#b#c", out _, out _));
    }

    [Test]
    private static void ExeHashCheck()
    {
        using FakeGame game = new();
        LauncherSettingsStore settings = new(game.Paths.LauncherSettingsFile, game.Paths.LegacyLauncherSettingsFile, game.Log);
        settings.Load();
        settings.Settings.PEST_Control.Hashes = new()
        {
            ["Paraworld.exe"] = [FakeGame.Sha256("Paraworld exe").ToUpperInvariant()],
            ["PWClient.exe"] = ["0000"],
        };
        ExeIntegrityService check = new(game.Paths, settings);
        List<ExeCheckResult> results = check.Check();
        Assert.Equal(ExeCheckStatus.Ok, results.Single(static r => r.FileName == "Paraworld.exe").Status, "hash case doesn't matter");
        Assert.Equal(ExeCheckStatus.Mismatch, results.Single(static r => r.FileName == "PWClient.exe").Status);
        Assert.Equal(ExeCheckStatus.NotChecked, results.Single(static r => r.FileName == "PWServer.exe").Status);
        Assert.Null(results.Single(static r => r.FileName == "PWServer.exe").Hash);
        Assert.Equal(FakeGame.Sha256("PWServer exe"), check.Check(true).Single(static r => r.FileName == "PWServer.exe").Hash);

        File.Delete(game.Paths.GetGameExe("PWClient"));
        Assert.Equal(ExeCheckStatus.Missing, check.Check().Single(static r => r.FileName == "PWClient.exe").Status);
    }

    [Test]
    private static void CacheClear()
    {
        using FakeGame game = new();
        string[] cache = ["a.bin", "b.UBC", "c.swd"];
        string[] keep = ["paraworld_ip.txt", "robin", "x.binx"];
        foreach (string name in cache.Concat(keep))
        {
            _ = game.Dir.File("Temp/" + name, "x");
        }
        List<string> failed = new CacheService(game.Paths, game.Log).Clear();
        Assert.Equal(0, failed.Count);
        Assert.Equal(string.Join(',', keep.Order()), string.Join(',', Directory.GetFiles(game.Paths.CacheDir).Select(Path.GetFileName).Order()));
    }

    [Test]
    private static void MediaCatalog_()
    {
        using FakeGame game = new();
        MediaCatalog media = new(game.Paths);
        Assert.Equal("1,2", string.Join(',', media.BackgroundIds));
        Assert.Equal(2, media.PickRandom(1));
        Assert.Equal(1, media.PickRandom(2));
        Assert.NotNull(media.GetMusic(2), "13_plain_icewaste");
        Assert.Null(media.GetMusic(3), "11_plain_jungle_1 doesn't exist here");
        Assert.Equal("17_maintheme_hu", MediaCatalog.GetTrackName(85), "background 85 has music too");
        Assert.Equal("menu_bg_winter.png", MediaCatalog.GetMenuBackground(new System.DateTime(2026, 12, 24)));
        Assert.Equal("menu_bg.png", MediaCatalog.GetMenuBackground(new System.DateTime(2026, 6, 1)));
    }

    [Test]
    private static void LocalizationFallbackAndMessages()
    {
        Localization text = new(FakeGame.Locale);
        List<string> changed = [];
        text.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);
        text.SetLanguage("de");
        Assert.Equal("DE", text.Language);
        Assert.True(changed.Contains("Item[]"), "WPF indexer bindings update");
        Assert.Equal("MIRAGE-DE", text["ModNameText"]);
        Assert.Equal("build", text["ModBuildText"], "missing in DE: UK");
        Assert.Equal("NoSuchKey", text["NoSuchKey"]);
        Assert.Equal("LogMessage#07\nNoCacheFound", text.Message(7, "NoCacheFound"));
    }

    [Test]
    private static void GameConfig()
    {
        using FakeGame game = new();
        GameConfigService config = new(game.Paths, game.Log);
        Assert.Equal("DE", config.GetLanguage());
        Assert.True(config.SetLanguage("UK"));
        Assert.Equal("uk", CfgDocument.Load(game.Paths.SettingsCfg).Get(GameSettings.Language), "written in lower case");
        Assert.True(config.SetUseUslCpp(false));
        Assert.Equal("0", CfgDocument.Load(game.Paths.SettingsCfg).Get(GameSettings.UseUslCpp));
        Assert.False(config.HasBackup);
        Assert.True(config.CreateBackup());
        Assert.True(config.HasBackup);

        File.WriteAllText(game.Paths.SettingsCfg, "Root {\n");
        Assert.Null(config.GetLanguage());
        Assert.NotNull(config.LastError);
        Assert.False(config.SetUseUslCpp(true), "a damaged file is not written");
        Assert.True(config.RestoreBackup());
        Assert.Equal("UK", config.GetLanguage());
    }
}