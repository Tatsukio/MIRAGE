using MIRAGE.Shared.Cfg;
using MIRAGE_Launcher.Services;
using MIRAGE_Launcher.ViewModels;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace MIRAGE.Tools.Tests;

internal static class ViewModelTests
{
    [Test]
    private static async Task MainViewModelFlow()
    {
        using FakeGame game = new();
        LauncherSettingsStore settings = new(game.Paths.LauncherSettingsFile, game.Paths.LegacyLauncherSettingsFile, game.Log);
        settings.Load();
        // No network in the test.
        settings.Settings.Launcher.LatestVersionUrl = string.Empty;
        settings.Settings.Launcher.PublicIpUrl = string.Empty;
        settings.Settings.Launcher.EnableDiscordStatus = false;

        Localization text = new(FakeGame.Locale);
        GameConfigService config = new(game.Paths, game.Log);
        text.SetLanguage(config.GetLanguage()!);
        FakeDialogs dialogs = new();
        FakeAudio audio = new();
        FakeLifetime lifetime = new();
        ShellService shell = new(game.Log);
        ToolsService tools = new(game.Paths, shell, game.Log);
        AddonService addons = new(game.Paths, settings, game.Log);
        addons.Load(); // as App.xaml.cs; the view model only shows the list
        ExeIntegrityService exeCheck = new(game.Paths, settings);
        GameStartService start = new(game.Paths, settings, text, config, addons, exeCheck, new CacheService(game.Paths, game.Log), tools, shell, dialogs, game.Log);
        using HttpClient http = new();
        MainViewModel vm = new(
            game.Paths, settings, text, config, addons, start, new MediaCatalog(game.Paths), new UpdateService(game.Paths, settings, game.Log, http),
            new HealthCheckService(game.Paths, exeCheck, game.Log), tools, shell, dialogs, audio, lifetime, game.Log);

        await vm.InitializeAsync();
        Assert.Equal("UK,DE", string.Join(',', vm.Languages), "FR has no Data\\MIRAGE\\Locale folder");
        Assert.Equal("DE", vm.SelectedLanguage);
        Assert.Equal("BoosterPack3,MIRAGE,Wintermod", string.Join(',', vm.Addons.Select(static a => a.Id)));
        Assert.NotNull(vm.BackgroundImage);
        Assert.NotNull(audio.File);
        Assert.True(audio.IsPlaying, "MusicOnStartup");
        Assert.False(vm.IsUpdateAvailable);

        vm.ToggleMusicCommand.Execute(null);
        Assert.False(vm.IsMusicPlaying);
        Assert.False(audio.IsPlaying);
        Assert.Equal("On", vm.ToggleMusicText);

        vm.SelectedLanguage = "UK";
        Assert.Equal("uk", CfgDocument.Load(game.Paths.SettingsCfg).Get(GameSettings.Language));
        Assert.Equal("MIRAGE 0.0.0 build ", vm.MenuTitle);

        vm.ToggleToolsPanelCommand.Execute(null);
        Assert.True(vm.IsToolsPanelOpen);
        Assert.Equal("Close", vm.ToggleToolsPanelText);

        vm.Addons.Single(static a => a.Id == "Wintermod").IsEnabled = true;
        Assert.True(settings.Settings.Addons.Enabled.Contains("Wintermod"));

        string? before = vm.BackgroundImage;
        audio.RaiseEnded();
        Assert.False(before == vm.BackgroundImage, "next background after the track");

        vm.RestoreSettingsCommand.Execute(null);
        Assert.True(dialogs.Messages.Last().StartsWith("ERROR LogMessage#02", System.StringComparison.Ordinal), "no backup yet");
        vm.CreateSettingsBackupCommand.Execute(null);
        Assert.True(dialogs.Messages.Last().StartsWith("INFO LogMessage#06", System.StringComparison.Ordinal));
        dialogs.Answer = true;
        vm.RestoreSettingsCommand.Execute(null);
        Assert.True(dialogs.Messages.Last().StartsWith("INFO LogMessage#04", System.StringComparison.Ordinal), dialogs.Messages.Last());
        Assert.False(settings.Settings.Addons.Enabled.Contains("Wintermod"), "user options reset");
        Assert.False(vm.Addons.Single(static a => a.Id == "Wintermod").IsEnabled, "list reloaded");

        vm.ExitCommand.Execute(null);
        Assert.True(lifetime.IsShutDown);
    }
}