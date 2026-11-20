using DiscordRPC;
using DiscordRPC.IO;
using MIRAGE.Shared.Logging;
using ParaWorldStatus.Models;
using System;
using System.Linq;

namespace ParaWorldStatus.Services;

/// <summary>The connection to Discord (DiscordRichPresence package). The only class that uses DiscordRPC types.</summary>
public sealed class DiscordService(ILog p_Log) : IDisposable
{
    /// <summary>The ParaWorld application in Discord.</summary>
    public const string ApplicationId = "723292670327390248";

    private static readonly DateTime s_startTime = DateTime.UtcNow;

    private readonly ILog m_log = p_Log;
    private DiscordRpcClient? m_client;

    /// <summary>Connected (true) or disconnected (false).</summary>
    public event Action<bool>? ConnectionChanged;

    /// <summary>A Discord user clicked "Join" or "Spectate"; the argument is the secret.</summary>
    public event Action<string>? JoinRequested;

    /// <summary>Connects; false if the client can't start (then nothing is shown in Discord).</summary>
    public bool Start(PresenceData p_First)
    {
        m_client = new DiscordRpcClient(ApplicationId, pipe: -1, logger: new DiscordLogAdapter(m_log), autoEvents: true, client: new ManagedNamedPipeClient())
        {
            SkipIdenticalPresence = true,
        };
        // Lets Discord start this program for "Join" (discord-<id>:// URI).
        _ = m_client.RegisterUriScheme();
        m_client.OnReady += (_, p_E) =>
        {
            m_log.Info($"Discord ready (RPC version {p_E.Version}).");
            ConnectionChanged?.Invoke(true);
        };
        m_client.OnClose += (_, p_E) =>
        {
            m_log.Info($"Discord connection closed: {p_E.Reason}");
            ConnectionChanged?.Invoke(false);
        };
        m_client.OnConnectionFailed += (_, p_E) => ConnectionChanged?.Invoke(false);
        m_client.OnError += (_, p_E) => m_log.Warn($"Discord error {p_E.Code}: {p_E.Message}");
        m_client.OnJoin += (_, p_E) => OnJoin(p_E.Secret);
        m_client.OnSpectate += (_, p_E) => OnJoin(p_E.Secret);
        m_client.SetSubscription(EventType.Join | EventType.Spectate);
        m_client.SetPresence(ToRichPresence(p_First));
        if (!m_client.Initialize())
        {
            m_log.Error("DiscordRpcClient.Initialize failed.");
            return false;
        }
        return true;
    }

    public void SetPresence(PresenceData p_Presence)
    {
        if (m_client is null || m_client.IsDisposed)
        {
            return;
        }
        try
        {
            m_client.SetPresence(ToRichPresence(p_Presence));
        }
        catch (Exception ex)
        {
            // A value Discord doesn't accept: log it, keep the program running.
            m_log.Error($"Discord rejected the presence: {ex.Message}");
        }
    }

    public void Dispose()
    {
        m_client?.Dispose();
        m_client = null;
    }

    private static RichPresence ToRichPresence(PresenceData p_Data)
    {
        RichPresence presence = new()
        {
            Details = NullIfEmpty(p_Data.Details),
            State = NullIfEmpty(p_Data.State),
            Assets = new Assets
            {
                LargeImageKey = p_Data.LargeImage.Key,
                LargeImageText = NullIfEmpty(p_Data.LargeImage.Text),
                SmallImageKey = p_Data.SmallImage?.Key,
                SmallImageText = NullIfEmpty(p_Data.SmallImage?.Text),
            },
            Timestamps = new Timestamps { Start = s_startTime },
        };
        if (p_Data.Party is not null)
        {
            presence.Party = new Party
            {
                ID = p_Data.Party.Id,
                Privacy = Party.PrivacySetting.Public,
                Size = p_Data.Party.Size,
                Max = p_Data.Party.Max,
            };
        }
        if ((p_Data.JoinSecret is not null) || (p_Data.SpectateSecret is not null))
        {
            presence.Secrets = new Secrets { JoinSecret = p_Data.JoinSecret, SpectateSecret = p_Data.SpectateSecret };
        }
        if (p_Data.Buttons.Count > 0)
        {
            presence.Buttons = [.. p_Data.Buttons.Select(static p_B => new Button { Label = p_B.Label, Url = p_B.Url })];
        }
        return presence;
    }

    private static string? NullIfEmpty(string? p_Text) => string.IsNullOrEmpty(p_Text) ? null : p_Text;

    private void OnJoin(string p_Secret)
    {
        m_log.Info($"Join / spectate: {p_Secret}");
        JoinRequested?.Invoke(p_Secret);
    }
}

/// <summary>DiscordRPC's log goes to the ParaWorldStatus log (warnings and errors only).</summary>
internal sealed class DiscordLogAdapter(ILog p_Log) : DiscordRPC.Logging.ILogger
{
    private readonly ILog m_log = p_Log;

    public DiscordRPC.Logging.LogLevel Level { get; set; } = DiscordRPC.Logging.LogLevel.Warning;

    public void Trace(string p_Message, params object[] p_Args)
    {
    }

    public void Info(string p_Message, params object[] p_Args)
    {
    }

    public void Warning(string p_Message, params object[] p_Args) => m_log.Warn("DiscordRPC: " + Format(p_Message, p_Args));

    public void Error(string p_Message, params object[] p_Args) => m_log.Error("DiscordRPC: " + Format(p_Message, p_Args));

    private static string Format(string p_Message, object[] p_Args)
    {
        try
        {
            return (p_Args.Length == 0) ? p_Message : string.Format(System.Globalization.CultureInfo.InvariantCulture, p_Message, p_Args);
        }
        catch (FormatException)
        {
            return p_Message;
        }
    }
}
