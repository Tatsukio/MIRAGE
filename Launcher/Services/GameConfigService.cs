using MIRAGE.Shared;
using MIRAGE.Shared.Cfg;
using MIRAGE.Shared.Logging;
using System;
using System.IO;

namespace MIRAGE_Launcher.Services;

/// <summary>Settings.cfg of the game (in %APPDATA%). Methods return false / null on failure and set <see cref="LastError"/>.</summary>
public sealed class GameConfigService(GamePaths p_Paths, ILog p_Log)
{
    private readonly GamePaths m_paths = p_Paths;
    private readonly ILog m_log = p_Log;

    public string? LastError { get; private set; }

    public bool Exists => File.Exists(m_paths.SettingsCfg);

    public bool HasBackup => File.Exists(m_paths.SettingsCfgBackup);

    public string FilePath => m_paths.SettingsCfg;

    /// <summary>The game language in upper case (UK, DE ...), or null if Settings.cfg can't be read.</summary>
    public string? GetLanguage()
    {
        LastError = null;
        try
        {
            string? language = CfgDocument.Load(m_paths.SettingsCfg).Get(GameSettings.Language);
            return string.IsNullOrWhiteSpace(language) ? null : language.Trim().ToUpperInvariant();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CfgParseException)
        {
            Fail($"Could not read the language from {m_paths.SettingsCfg}: {ex.Message}");
            return null;
        }
    }

    /// <summary>Writes the language in lower case, as the game does.</summary>
    public bool SetLanguage(string p_Language) => SetValue(GameSettings.Language, p_Language.Trim().ToLowerInvariant());

    /// <summary>false: the server runs the .usl files (MIRAGE); true: the compiled scripts (original game).</summary>
    public bool SetUseUslCpp(bool p_UseCompiled) => SetValue(GameSettings.UseUslCpp, p_UseCompiled ? "1" : "0");

    public bool SetAutoKillCache(bool p_AutoKill) => SetValue(GameSettings.AutoKillCache, p_AutoKill ? "1" : "0");

    public bool CreateBackup() => Copy(m_paths.SettingsCfg, m_paths.SettingsCfgBackup);

    public bool RestoreBackup() => Copy(m_paths.SettingsCfgBackup, m_paths.SettingsCfg);

    private bool SetValue(string p_Path, string p_Value)
    {
        LastError = null;
        try
        {
            if (GameSettings.SetValue(m_paths.SettingsCfg, p_Path, p_Value))
            {
                m_log.Debug($"Settings.cfg: {p_Path} = '{p_Value}'");
            }
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CfgParseException or ArgumentException)
        {
            Fail($"Could not set {p_Path} in {m_paths.SettingsCfg}: {ex.Message}");
            return false;
        }
    }

    private bool Copy(string p_From, string p_To)
    {
        LastError = null;
        try
        {
            File.Copy(p_From, p_To, true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Fail($"Could not copy {p_From} to {p_To}: {ex.Message}");
            return false;
        }
    }

    private void Fail(string p_Message)
    {
        LastError = p_Message;
        m_log.Error(p_Message);
    }
}