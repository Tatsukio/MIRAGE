using MIRAGE.Shared.Cfg;
using MIRAGE.Shared.Processes;
using PWKiller.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MIRAGE.Tools.Tests;

internal static class KillerTests
{
    [Test]
    private static void Modes()
    {
        Assert.Equal(KillerMode.User, KillerModeParser.Parse([]));
        Assert.Equal(KillerMode.Watch, KillerModeParser.Parse(["--watch"]));
        Assert.Equal(KillerMode.Watch, KillerModeParser.Parse(["-SSSOffAfterPWExit"]), "old argument");
        Assert.Equal(KillerMode.KillAll, KillerModeParser.Parse(["-KillAll"]), "old argument");
        Assert.Equal(KillerMode.KillAll, KillerModeParser.Parse(["--kill-all"]));
        Assert.Equal(KillerMode.Invalid, KillerModeParser.Parse(["--watch", "x"]));
        Assert.Equal(KillerMode.Invalid, KillerModeParser.Parse(["/x"]));
        Assert.True(KillerModeParser.MutexName(KillerMode.User) != KillerModeParser.MutexName(KillerMode.Watch));
    }

    /// <summary>The game appears late, Paraworld.exe hands over to PWClient.exe with a 1 s gap, then it is closed.</summary>
    [Test]
    private static async Task WatcherWaitsForTheRealExit()
    {
        Queue<bool> running = new([false, false, true, true, false, true, true, false, false, false, false]);
        int polls = 0;
        GameWatcher watcher = new(() =>
        {
            polls++;
            return running.Count > 1 ? running.Dequeue() : running.Peek();
        }, static (_, _) => Task.CompletedTask);
        Assert.True(await watcher.WaitForExitAsync());
        Assert.Equal(0, running.Count - 1, "returned only after 3 polls without the game");
        Assert.Equal(10, polls);
    }

    [Test]
    private static async Task WatcherGivesUpIfTheGameNeverStarts()
    {
        int polls = 0;
        GameWatcher watcher = new(() =>
        {
            polls++;
            return false;
        }, static (_, _) => Task.CompletedTask)
        { StartTimeout = TimeSpan.FromSeconds(5) };
        Assert.False(await watcher.WaitForExitAsync());
        Assert.Equal(6, polls);

        using CancellationTokenSource cts = new();
        cts.Cancel();
        GameWatcher cancelled = new(static () => true);
        bool threw = false;
        try
        {
            _ = await cancelled.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            threw = true;
        }
        Assert.True(threw);
    }

    [Test]
    private static void ResetServerScriptsAndTexts()
    {
        using FakeGame game = new();
        KillerActions actions = new(game.Paths, game.Log);
        _ = GameSettings.SetValue(game.Paths.SettingsCfg, GameSettings.UseUslCpp, "0");
        Assert.True(actions.ResetServerScripts());
        Assert.Equal("1", CfgDocument.Load(game.Paths.SettingsCfg).Get(GameSettings.UseUslCpp));

        File.WriteAllText(game.Paths.SettingsCfg, "Root {");
        Assert.False(actions.ResetServerScripts(), "a damaged Settings.cfg is reported, not overwritten");
        Assert.Equal("Root {", File.ReadAllText(game.Paths.SettingsCfg));

        Assert.Equal(KillerTexts.English, KillerTexts.Load(game.Paths), "no Locale.json");
        _ = game.Dir.File("Paraworld/Tools/MIRAGE Launcher/Locale.json", "{ \"UK\": { \"PWKillerTitleText\": \"T\", \"PWKillerButtonText\": \"B\" } }");
        Assert.Equal(new KillerTexts("T", "B"), KillerTexts.Load(game.Paths));
    }
}