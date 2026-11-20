using MIRAGE.Shared;
using MIRAGE.Shared.Logging;
using MIRAGE.Shared.Processes;
using MIRAGE_Launcher.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MIRAGE_Launcher.Services;

/// <summary>Everything that happens when the player starts the game, the map editor or a server.</summary>
public sealed partial class GameStartService(
    GamePaths p_Paths,
    LauncherSettingsStore p_Settings,
    Localization p_Text,
    GameConfigService p_Config,
    AddonService p_Addons,
    ExeIntegrityService p_ExeCheck,
    CacheService p_Cache,
    ToolsService p_Tools,
    ShellService p_Shell,
    IDialogService p_Dialogs,
    ILog p_Log)
{
    /// <summary>Videos the game plays in the campaign (Data\Locale\&lt;language&gt;\Video\hs_&lt;name&gt;.bik).</summary>
    private static readonly string[] s_campaignVideos =
    [
        "1000", "1000a", "1010", "1090", "1120", "2005", "2020", "2045", "2046", "2090", "2560", "2580",
        "3010", "3040", "4010", "4015", "6020", "7020", "7026", "7060", "7060b", "8090_2",
    ];

    private readonly GamePaths m_paths = p_Paths;
    private readonly LauncherSettingsStore m_settings = p_Settings;
    private readonly Localization m_text = p_Text;
    private readonly GameConfigService m_config = p_Config;
    private readonly AddonService m_addons = p_Addons;
    private readonly ExeIntegrityService m_exeCheck = p_ExeCheck;
    private readonly CacheService m_cache = p_Cache;
    private readonly ToolsService m_tools = p_Tools;
    private readonly ShellService m_shell = p_Shell;
    private readonly IDialogService m_dialogs = p_Dialogs;
    private readonly ILog m_log = p_Log;

    /// <summary>Checks everything, prepares Settings.cfg and starts the game. False if it was not started.</summary>
    public bool Start(GameMode p_Mode)
    {
        if (!Prepare())
        {
            return false;
        }
        string addons = AddonService.BuildCommandLine(m_addons.EnabledIds);
        return p_Mode switch
        {
            GameMode.MapEditor => Launch(GameProcesses.Client, Join("-leveled", addons)),
            GameMode.DedicatedServer => Launch(GameProcesses.Launcher, Join("-dedicated", addons)),
            _ => Launch(GameProcesses.Launcher, addons),
        };
    }

    /// <summary>
    /// "Join" from the Discord status: <c>MIRAGE Launcher.exe -PWSJoin &lt;address&gt;#&lt;addon&gt;|&lt;addon&gt;...</c>.
    /// The server's add-ons must be enabled here.
    /// </summary>
    public bool JoinServer(string p_ServerInfo)
    {
        if (!TryParseServerInfo(p_ServerInfo, out string address, out List<string> serverAddons))
        {
            m_dialogs.ShowError(m_text.Message(20, "InvalidJoinInfo") + "\n" + p_ServerInfo);
            return false;
        }

        var enabled = m_addons.EnabledIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = serverAddons.Where(p_A => !enabled.Contains(p_A)).ToList();
        if (missing.Count > 0)
        {
            m_dialogs.ShowError($"{m_text.Message(13, "DiscordStatusMissingAddons")} {string.Join(", ", missing)}");
            return false;
        }
        if (!Prepare())
        {
            return false;
        }
        // Only ids of installed add-ons go to the command line.
        IEnumerable<string> ids = serverAddons
            .Select(p_A => m_addons.Addons.First(p_X => string.Equals(p_X.Id, p_A, StringComparison.OrdinalIgnoreCase)).Id)
            .Concat(m_addons.LocaleIds)
            .Distinct(StringComparer.OrdinalIgnoreCase);
        return Launch(GameProcesses.Launcher, Join($"-autoconnect {address}", AddonService.BuildCommandLine(ids)));
    }

    /// <summary>Splits "-1.2.3.4:5678#BoosterPack3|MIRAGE". The address may only have letters, digits and . : - _ [ ].</summary>
    public static bool TryParseServerInfo(string p_ServerInfo, out string p_Address, out List<string> p_Addons)
    {
        p_Address = string.Empty;
        p_Addons = [];
        string[] parts = (p_ServerInfo ?? string.Empty).Split('#');
        if (parts.Length != 2)
        {
            return false;
        }
        string address = parts[0].Trim().TrimStart('-');
        if (!AddressRegex().IsMatch(address))
        {
            return false;
        }
        List<string> addons = [.. parts[1].Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
        if (addons.Any(static p_A => !AddonIdRegex().IsMatch(p_A)))
        {
            return false;
        }
        p_Address = address;
        p_Addons = addons;
        return true;
    }

    public bool HasLocalizedVideos(string p_Language)
    {
        string folder = Path.Combine(m_paths.GameLocaleDir, p_Language, "Video");
        return Directory.Exists(folder) && s_campaignVideos.All(p_V => File.Exists(Path.Combine(folder, $"hs_{p_V}.bik")));
    }

    [GeneratedRegex(@"^[A-Za-z0-9._:\-\[\]]{1,253}$")]
    private static partial Regex AddressRegex();

    [GeneratedRegex(@"^[A-Za-z0-9_\-]{1,64}$")]
    private static partial Regex AddonIdRegex();

    private static string Join(string p_First, string p_Second) => string.IsNullOrEmpty(p_Second) ? p_First : $"{p_First} {p_Second}";

    private bool Prepare()
    {
        LauncherOptions options = m_settings.Settings.Launcher;

        // 1. Another game running?
        if (!options.MultipleGameCopiesAllowed && GameProcesses.IsAnyRunning(GameProcesses.All))
        {
            if (m_dialogs.AskYesNo(m_text.Message(0, "PWIsAlreadyRunning")))
            {
                _ = m_tools.StartPWKiller(PWKillerMode.User);
            }
            return false;
        }

        // 2. Game exes, and the WinFix check.
        List<ExeCheckResult> exes = m_exeCheck.IsEnabled ? m_exeCheck.Check() : [];
        var missing = ExeIntegrityService.FileNames.Where(p_F => !File.Exists(Path.Combine(m_paths.BinDir, p_F))).ToList();
        if (missing.Count > 0)
        {
            m_dialogs.ShowError(m_text.Message(15, "ExeMissing") + FormatList(missing) + "\n" + m_paths.BinDir);
            return false;
        }
        var mismatched = exes.Where(static p_R => p_R.Status == ExeCheckStatus.Mismatch).Select(static p_R => p_R.FileName).ToList();
        if (mismatched.Count > 0)
        {
            m_log.Warn($"WinFix check: {string.Join(", ", exes.Select(static p_R => $"{p_R.FileName} {p_R.Status} {p_R.Hash}"))}");
            if (!m_dialogs.AskYesNo(m_text.Message(16, "ExeHashMismatch") + FormatList(mismatched) + "\n" + m_text["ExeHashHint"]))
            {
                return false;
            }
        }

        // 3. Add-on rules.
        Dictionary<string, List<string>> missingAddons = m_addons.FindMissingRequirements();
        if (missingAddons.Count > 0)
        {
            m_dialogs.ShowError(FormatAddonProblems(missingAddons, true));
            return false;
        }
        Dictionary<string, List<string>> conflicts = m_addons.FindConflicts();
        if (conflicts.Count > 0)
        {
            m_dialogs.ShowError(FormatAddonProblems(conflicts, false));
            return false;
        }

        // 4. Videos of the language (a note, once).
        if (!options.SkipVideoCheck && !HasLocalizedVideos(m_text.Language))
        {
            m_dialogs.ShowWarning(m_text.Message(17, "VideoLocaleMissing", m_text.Language));
            options.SkipVideoCheck = true;
            _ = m_settings.Save();
        }

        // 5. Cache.
        List<string> locked = m_cache.Clear();
        if (locked.Count > 0)
        {
            m_dialogs.ShowError(m_text.Message(18, "CacheClearFailed") + FormatList(locked.Select(Path.GetFileName).OfType<string>()));
            return false;
        }

        // 6. The server runs the MIRAGE .usl scripts (PWKiller sets it back to 1 when the game is closed).
        if (!m_config.SetUseUslCpp(false))
        {
            ShowConfigError();
            return false;
        }

        if (options.EnableDiscordStatus)
        {
            m_tools.RestartDiscordStatus();
        }
        return true;
    }

    private bool Launch(string p_ProcessName, string p_Arguments)
    {
        ProcessStartInfo info = new(m_paths.GetGameExe(p_ProcessName), p_Arguments)
        {
            // PWClient -leveled doesn't work without it.
            WorkingDirectory = m_paths.BinDir,
            UseShellExecute = false,
        };
        m_log.Info($"Starting {p_ProcessName}.exe {p_Arguments}");
        using Process? process = m_shell.StartProcess(info, out string error);
        if (process is null)
        {
            m_dialogs.ShowError(m_text.Message(19, "StartFailed", p_ProcessName + ".exe") + "\n" + error);
            _ = m_config.SetUseUslCpp(true);
            return false;
        }
        _ = m_tools.StartPWKiller(PWKillerMode.User);
        _ = m_tools.StartPWKiller(PWKillerMode.Watch);
        return true;
    }

    private void ShowConfigError()
    {
        string message = m_text.Message(11, "CfgError") + "\n" + m_config.LastError;
        if (m_config.HasBackup && m_dialogs.AskYesNo(message + "\n\n" + m_text["AskSettingsBackup"]))
        {
            if (m_config.RestoreBackup())
            {
                m_dialogs.ShowInfo(m_text.Message(4, "ResetSettingsSuccess"));
            }
        }
        else if (!m_config.HasBackup)
        {
            m_dialogs.ShowError(message);
        }
    }

    private string FormatAddonProblems(Dictionary<string, List<string>> p_Problems, bool p_Missing)
    {
        StringBuilder sb = new();
        _ = sb.AppendLine(m_text.Message(14, p_Missing ? "MissingAddonsMain" : "ExcludedAddonsMain"));
        _ = sb.AppendLine();
        foreach ((string addon, List<string> others) in p_Problems)
        {
            _ = sb.AppendLine($"{m_text["Addon"]} {addon}");
            _ = sb.AppendLine(m_text[p_Missing ? "MissingAddons" : "ExcludedAddons"]);
            foreach (string other in others)
            {
                _ = sb.AppendLine($" - {other}");
            }
            _ = sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    private static string FormatList(IEnumerable<string> p_Items) => "\n" + string.Join("\n", p_Items.Select(static p_I => " - " + p_I));
}