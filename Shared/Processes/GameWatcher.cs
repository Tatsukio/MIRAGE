using System;
using System.Threading;
using System.Threading.Tasks;

namespace MIRAGE.Shared.Processes;

/// <summary>Waits until the game is closed (used by PWKiller in the background).</summary>
public sealed class GameWatcher(Func<bool> p_IsGameRunning, Func<TimeSpan, CancellationToken, Task>? p_Delay = null)
{
    private readonly Func<bool> m_isGameRunning = p_IsGameRunning;
    private readonly Func<TimeSpan, CancellationToken, Task> m_delay = p_Delay ?? Task.Delay;

    /// <summary>How long to wait for the game to appear after the start (default 60 s).</summary>
    public TimeSpan StartTimeout { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>The game must be gone this long in a row, to bridge Paraworld.exe handing over to PWClient.exe (default 3 s).</summary>
    public TimeSpan ExitConfirmTime { get; init; } = TimeSpan.FromSeconds(3);

    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Returns when the game was running and is closed now: true. Returns false if the game never appeared within
    /// <see cref="StartTimeout"/>.
    /// </summary>
    public async Task<bool> WaitForExitAsync(CancellationToken p_Token = default)
    {
        TimeSpan waited = TimeSpan.Zero;
        while (!m_isGameRunning())
        {
            if (waited >= StartTimeout)
            {
                return false;
            }
            await m_delay(PollInterval, p_Token).ConfigureAwait(false);
            waited += PollInterval;
        }

        TimeSpan goneFor = TimeSpan.Zero;
        while (goneFor < ExitConfirmTime)
        {
            await m_delay(PollInterval, p_Token).ConfigureAwait(false);
            goneFor = m_isGameRunning() ? TimeSpan.Zero : goneFor + PollInterval;
        }
        return true;
    }
}