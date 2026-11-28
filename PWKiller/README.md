# PWKiller

`Tools/PWKiller.exe`, .NET 10 (WinForms), started by the MIRAGE launcher. Uses `Shared` (`MIRAGE.Shared.dll`).

| Arguments | |
|---|---|
| none | Window with "End processes": ends `Paraworld.exe`, `PWClient.exe`, `PWServer.exe` and `ParaWorldStatus.exe`, sets `Root/Pest/Server/UseUslCPP` to `'1'` |
| `--watch` (old: `-SSSOffAfterPWExit`) | Background, no window. Waits up to a minute for the game, then until `Paraworld.exe` and `PWClient.exe` have been gone for 3 seconds in a row. Then it ends what is left (`PWServer.exe`, the Discord status), sets `UseUslCPP` to `'1'` and closes the PWKiller window. |
| `--kill-all` (old: `-KillAll`) | No window. Ends the game and every other PWKiller, sets `UseUslCPP` to `'1'`. |

- The launcher starts the window and `--watch` together with the game.
- One instance per mode (named mutex); a second start in the same mode does nothing.
- A process that can't be ended (already gone, no access) is logged and skipped; it doesn't crash PWKiller.
- Log: `%APPDATA%\SpieleEntwicklungsKombinat\Paraworld\MIRAGE_PWKiller.log`.
- Texts: `PWKillerTitleText` / `PWKillerButtonText` from `Tools/MIRAGE Launcher/Locale.json` in the game language;
  English if the file is missing.
- `Core/` has the logic without WinForms (also compiled by the `Tests` project).