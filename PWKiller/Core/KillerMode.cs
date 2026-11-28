using System;

namespace PWKiller.Core;

public enum KillerMode
{
    /// <summary>No arguments: the window with the button.</summary>
    User,

    /// <summary><c>--watch</c> (old: <c>-SSSOffAfterPWExit</c>): no window, waits for the game to close.</summary>
    Watch,

    /// <summary><c>--kill-all</c> (old: <c>-KillAll</c>): no window, ends everything at once.</summary>
    KillAll,

    Invalid,
}

public static class KillerModeParser
{
    public static KillerMode Parse(string[] p_Args)
    {
        return p_Args.Length == 0
            ? KillerMode.User
            : p_Args.Length > 1
            ? KillerMode.Invalid
            : p_Args[0].ToLowerInvariant() switch
            {
                "--watch" or "-sssoffafterpwexit" => KillerMode.Watch,
                "--kill-all" or "-killall" => KillerMode.KillAll,
                _ => KillerMode.Invalid,
            };
    }

    /// <summary>Name of the mutex that allows one instance per mode.</summary>
    public static string MutexName(KillerMode p_Mode) => p_Mode switch
    {
        KillerMode.User => @"Local\MIRAGE_PWKiller_User",
        KillerMode.Watch => @"Local\MIRAGE_PWKiller_Watch",
        KillerMode.KillAll => @"Local\MIRAGE_PWKiller_KillAll",
        _ => throw new ArgumentOutOfRangeException(nameof(p_Mode)),
    };
}