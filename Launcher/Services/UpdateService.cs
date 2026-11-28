using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MIRAGE.Shared;
using MIRAGE.Shared.IO;
using MIRAGE.Shared.Logging;
using MIRAGE_Launcher.Models;

namespace MIRAGE_Launcher.Services;

/// <summary>The online version check and the public IP for the game. Network errors are only logged.</summary>
public sealed class UpdateService(GamePaths p_Paths, LauncherSettingsStore p_Settings, ILog p_Log, HttpClient p_Http)
{
    private readonly GamePaths m_paths = p_Paths;
    private readonly LauncherSettingsStore m_settings = p_Settings;
    private readonly ILog m_log = p_Log;
    private readonly HttpClient m_http = p_Http;

    /// <summary>The newer version, or null if there is none or the check failed.</summary>
    public async Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken p_Token = default)
    {
        LauncherOptions options = m_settings.Settings.Launcher;
        if (string.IsNullOrWhiteSpace(options.LatestVersionUrl) || !Version.TryParse(options.ModVersion, out Version? current))
        {
            m_log.Debug($"Update check skipped (URL '{options.LatestVersionUrl}', version '{options.ModVersion}').");
            return null;
        }
        string? text = await GetStringAsync(options.LatestVersionUrl, p_Token).ConfigureAwait(false);
        if (text is null)
        {
            return null;
        }
        if (!UpdateInfo.TryParse(text, out UpdateInfo? info))
        {
            m_log.Warn($"Unknown format of {options.LatestVersionUrl}.");
            return null;
        }
        return (info!.Version > current) ? info : null;
    }

    /// <summary>Saves the public IP to %TEMP%\SpieleEntwicklungsKombinat\Paraworld\paraworld_ip.txt (read by the MIRAGE scripts).</summary>
    public async Task SavePublicIpAsync(CancellationToken p_Token = default)
    {
        string url = m_settings.Settings.Launcher.PublicIpUrl;
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }
        string? text = await GetStringAsync(url, p_Token).ConfigureAwait(false);
        if ((text is null) || !IPAddress.TryParse(text.Trim(), out IPAddress? address))
        {
            return;
        }
        try
        {
            AtomicFile.WriteAllBytes(Path.Combine(m_paths.CacheDir, "paraworld_ip.txt"), Encoding.ASCII.GetBytes(address.ToString()));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            m_log.Warn($"Could not save the public IP: {ex.Message}");
        }
    }

    private async Task<string?> GetStringAsync(string p_Url, CancellationToken p_Token)
    {
        try
        {
            return await m_http.GetStringAsync(p_Url, p_Token).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException or UriFormatException)
        {
            m_log.Debug($"Request to {p_Url} failed: {ex.Message}");
            return null;
        }
    }
}