using MIRAGE.Shared.Cfg;
using MIRAGE_Launcher.Models;
using MIRAGE_Launcher.Services;
using System.IO;
using System.Linq;

namespace MIRAGE.Tools.Tests;

internal static class GameStartTests
{
    [Test]
    private static void MissingExeStopsTheStart()
    {
        using Setup setup = new();
        File.Delete(setup.Game.Paths.GetGameExe("PWServer"));
        Assert.False(setup.Start.Start(GameMode.Game));
        Assert.True(setup.Dialogs.Messages.Single().StartsWith("ERROR LogMessage#15", System.StringComparison.Ordinal), setup.Dialogs.Messages.Single());
        Assert.True(setup.Dialogs.Messages.Single().Contains("PWServer.exe", System.StringComparison.Ordinal));
        Assert.Equal("1", setup.UseUslCpp, "Settings.cfg not touched");
    }

    [Test]
    private static void WinFixMismatchAsks()
    {
        using Setup setup = new();
        setup.Settings.Settings.WinFix.Hashes["PWClient.exe"] = ["0000"];
        setup.Dialogs.Answer = false;
        Assert.False(setup.Start.Start(GameMode.Game));
        Assert.True(setup.Dialogs.Messages.Single().StartsWith("ASK LogMessage#16", System.StringComparison.Ordinal));
        Assert.Equal("1", setup.UseUslCpp);
    }

    [Test]
    private static void MissingAddonStopsTheStart()
    {
        using Setup setup = new();
        setup.Addons.SetEnabled("BoosterPack3", false);
        Assert.False(setup.Start.Start(GameMode.Game));
        string message = setup.Dialogs.Messages.Single();
        Assert.True(message.StartsWith("ERROR LogMessage#14", System.StringComparison.Ordinal) && message.Contains(" - BoosterPack3", System.StringComparison.Ordinal), message);
    }

    /// <summary>All checks pass; starting the fake exe fails, so UseUslCPP must be set back to 1.</summary>
    [Test]
    private static void FailedStartResetsUseUslCpp()
    {
        using Setup setup = new();
        setup.Settings.Settings.WinFix.Hashes["Paraworld.exe"] = [FakeGame.Sha256("Paraworld exe")];
        _ = setup.Game.Dir.File("Temp/old.bin", "x");
        Assert.False(setup.Start.Start(GameMode.MapEditor));
        Assert.True(setup.Dialogs.Messages[0].StartsWith("WARN LogMessage#17", System.StringComparison.Ordinal), "no translated videos: shown once");
        Assert.True(setup.Settings.Settings.Launcher.SkipVideoCheck);
        Assert.True(setup.Dialogs.Messages[1].StartsWith("ERROR LogMessage#19", System.StringComparison.Ordinal), setup.Dialogs.Messages[1]);
        Assert.False(File.Exists(Path.Combine(setup.Game.Paths.CacheDir, "old.bin")), "cache cleared");
        Assert.Equal("1", setup.UseUslCpp);
        Assert.True(setup.Game.Log.Lines.Any(static l => l.Contains("Starting PWClient.exe -leveled -enable BoosterPack3 -enable MIRAGE", System.StringComparison.Ordinal)), string.Join('\n', setup.Game.Log.Lines));

        setup.Dialogs.Messages.Clear();
        _ = setup.Start.Start(GameMode.Game);
        Assert.False(setup.Dialogs.Messages.Any(static m => m.StartsWith("WARN LogMessage#17", System.StringComparison.Ordinal)), "only once");
    }

    [Test]
    private static void JoinNeedsTheServerAddons()
    {
        using Setup setup = new();
        Assert.False(setup.Start.JoinServer("1.2.3.4#MIRAGE|Wintermod"));
        Assert.True(setup.Dialogs.Messages.Single().Contains("Wintermod", System.StringComparison.Ordinal));
        setup.Dialogs.Messages.Clear();
        Assert.False(setup.Start.JoinServer("1.2.3.4 -x#MIRAGE"));
        Assert.True(setup.Dialogs.Messages.Single().StartsWith("ERROR LogMessage#20", System.StringComparison.Ordinal));
    }

    private sealed class Setup : System.IDisposable
    {
        public Setup()
        {
            Settings = new(Game.Paths.LauncherSettingsFile, Game.Paths.LegacyLauncherSettingsFile, Game.Log);
            Settings.Load();
            Settings.Settings.Launcher.EnableDiscordStatus = false;
            Settings.Settings.Addons.Enabled = ["BoosterPack3", "MIRAGE"];
            Addons = new(Game.Paths, Settings, Game.Log);
            Addons.Load();
            Localization text = new(FakeGame.Locale);
            ShellService shell = new(Game.Log);
            Start = new GameStartService(
                Game.Paths, Settings, text, new GameConfigService(Game.Paths, Game.Log), Addons, new ExeIntegrityService(Game.Paths, Settings),
                new CacheService(Game.Paths, Game.Log), new ToolsService(Game.Paths, shell, Game.Log), shell, Dialogs, Game.Log);
        }

        public FakeGame Game { get; } = new();

        public FakeDialogs Dialogs { get; } = new();

        public LauncherSettingsStore Settings { get; }

        public AddonService Addons { get; }

        public GameStartService Start { get; }

        public string? UseUslCpp => CfgDocument.Load(Game.Paths.SettingsCfg).Get(GameSettings.UseUslCpp);

        public void Dispose() => Game.Dispose();
    }
}