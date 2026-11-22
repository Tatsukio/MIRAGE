# MIRAGE Launcher, PWKiller and CfgEditor

The tools in `GameFiles/Basic/Tools`. All three are C# on .NET 10 and use no third-party libraries.

| Project | Output | What it is |
|---|---|---|
| `Launcher/MIRAGE Launcher.csproj` | `Tools/MIRAGE Launcher/MIRAGE Launcher.exe` | The launcher (WPF, MVVM) |
| `PWKiller/PWKiller.csproj` | `Tools/PWKiller.exe` | Ends a hanging game; resets `UseUslCPP` after the game (WinForms) |
| `CfgEditor/CfgEditor.csproj` | `Tools/CfgEditor.exe` | Command line editor for Settings.cfg and the other config files |
| `Shared/MIRAGE.Shared.csproj` | `MIRAGE.Shared.dll` next to each exe | Config file parser, paths, processes, log, JSON |
| `Tests/MIRAGE.Tools.Tests.csproj` | not shipped | Tests (`dotnet run -c Release` in `Tests`) |

Open `MIRAGE.sln` (repository root) for all of them. `Launcher/MIRAGE Launcher.sln` and
`PWKiller/PWKiller.sln` contain one tool and `Shared`.

## Building

- Visual Studio 2022 (17.14 or newer) or 2026 with the .NET 10 SDK, or `dotnet build -c Release` on Windows.
- Release builds write straight to `GameFiles/Basic/Tools` (launcher: `GameFiles/Basic/Tools/MIRAGE Launcher`).
- Players need the **.NET 10 Desktop Runtime** (x64). It covers all three tools.
- Code style: the `.editorconfig` in the repository root. The projects set `EnforceCodeStyleInBuild`, so its
  warnings show up in the build.

## Launcher

### What it does

| Button | |
|---|---|
| Start MIRAGE / Mapeditor / Dedicated Server | Checks, then starts `Paraworld.exe`, `PWClient.exe -leveled` or `Paraworld.exe -dedicated` with `-enable <add-on>` for each enabled add-on |
| Music (right click: next track) | Background music; each track has its own background picture |
| Language | Writes `Root/Global/Language` to Settings.cfg |
| Screenshots / Settings folder | Opens the folders |
| Kill PW Processes | `PWKiller.exe --kill-all` |
| PWTool | Add-on list, Settings.cfg backup and restore |
| Exit (right click: health check) | The health check writes `PWHealthCheck.txt` to the desktop |

Before every start the launcher checks, in this order:

1. no game is running (unless `MultipleGameCopiesAllowed`); otherwise it offers PWKiller;
2. `bin/Paraworld.exe`, `PWClient.exe`, `PWServer.exe` exist;
3. **PEST-Control check:** their SHA-256 hashes are in `Settings.json` → `PEST-Control.Hashes`; otherwise it lists the files and
   asks whether to start anyway;
4. the `requires` / `excludes` rules of the enabled add-ons;
5. translated videos for the language (a note, once);
6. it clears the cache (`*.bin`, `*.ubc`, `*.swd` in `%TEMP%\SpieleEntwicklungsKombinat\Paraworld`);
7. it sets `Root/Pest/Server/UseUslCPP = '0'` in Settings.cfg, so the server runs the MIRAGE `.usl` scripts.

Then it starts the game, the PWKiller window and a hidden PWKiller (`--watch`) that sets `UseUslCPP` back to
`'1'` when the game is closed. If the game can't be started, the launcher sets it back itself.

### Structure (MVVM)

| Folder | Contents | WPF? |
|---|---|---|
| `Models` | `LauncherSettings` (Settings.json), `Addon`, `UpdateInfo`, `GameMode`, `ExeCheckResult` | no |
| `Services` | One class per job: `GameStartService` (the checks above), `GameConfigService` (Settings.cfg), `AddonService`, `ExeIntegrityService` (PEST-Control check), `LauncherSettingsStore`, `Localization`, `UpdateService`, `CacheService`, `MediaCatalog`, `ToolsService` (PWKiller, Discord status), `ShellService`, `HealthCheckService`; `IPlatform.cs` has the interfaces for message boxes, music and shutdown | no |
| `ViewModels` | `MainViewModel` (all properties and commands of the window), `AddonViewModel`, `RelayCommand` / `AsyncRelayCommand`, `ViewModelBase` | no |
| `Views` | `MainWindow.xaml` with bindings only; the code-behind only drags the window | yes |
| `Wpf` | The WPF side of the interfaces: `WpfDialogService` (MessageBox), `WpfAudioPlayer` (MediaPlayer), `WpfAppLifetime` | yes |
| `Converters` | `BoolToVisibilityConverter` | yes |
| `App.xaml.cs` | Creates the services and the view model, handles `-PWSJoin`, allows one launcher | yes |

Everything without WPF is also compiled and tested by the `Tests` project.

Texts in XAML are bound by key: `Content="{Binding Text[StartModText]}"`. A test checks that every binding has a
property in the view model and every key exists in `Locale.json`.

### Files

| File | |
|---|---|
| `Tools/MIRAGE Launcher/Settings.json` | Launcher settings, see below. Replaced by the installer. |
| `Tools/MIRAGE Launcher/Locale.json` | Texts: `{ "UK": { "Key": "Text" }, "DE": { ... } }`. A missing key falls back to UK. |
| `%APPDATA%\SpieleEntwicklungsKombinat\Paraworld\MIRAGE_Launcher.log` | Log (also sent as UDP broadcast on `UdpLogPort`) |
| `...\Paraworld\MIRAGE_PWKiller.log` | PWKiller log |

`Settings.json`:

| Key | |
|---|---|
| `Launcher.ModVersion`, `ModBuild` | Shown in the title; `ModVersion` is compared with `LatestVersionUrl` |
| `Launcher.LatestVersionUrl` | `updateinfo.txt` (`versioncheck<TAB>MIRAGE 2.6.8<TAB>..<TAB>..<TAB>change;change`) |
| `Launcher.PublicIpUrl` | Its answer is saved to `%TEMP%\...\paraworld_ip.txt` for the scripts; empty: off |
| `Launcher.UdpLogPort` | 0: no UDP log |
| `Launcher.EnableDiscordStatus`, `MusicOnStartup`, `MultipleGameCopiesAllowed` | Options |
| `Launcher.SkipVideoCheck` | Set after the video note was shown once |
| `Addons.Enabled` | Enabled add-on ids |
| `PEST-Control.CheckHashes` | false: no PEST-Control check |
| `PEST-Control.Hashes` | `{ "PWClient.exe": ["<sha256>", ...], ... }`; a file without hashes isn't checked |

An old `Settings.xml` in the launcher folder is read once: the player's choices (add-ons, music, Discord, video
note) go into `Settings.json`, then it is renamed to `Settings.xml.imported`. "Restore Game Settings" copies
`Settings_backup.cfg` over Settings.cfg and puts these choices back to the defaults.

**New PEST-Control version:** add the hashes of its exes to `PEST-Control.Hashes` (PowerShell:
`Get-FileHash bin\PWClient.exe -Algorithm SHA256`). The health check also lists the current hashes.

### Command line

`MIRAGE Launcher.exe -PWSJoin <address>#<add-on>|<add-on>...` (from the Discord status): starts the game with
`-autoconnect <address>` without opening the launcher, if all the server's add-ons are enabled. The address and the
add-on ids are checked, so nothing else can get into the game's command line.

## PWKiller

| Arguments | |
|---|---|
| none | Window with "End processes": ends `Paraworld.exe`, `PWClient.exe`, `PWServer.exe`, `ParaWorldStatus.exe`, sets `UseUslCPP` to `'1'` |
| `--watch` (old: `-SSSOffAfterPWExit`) | No window. Waits up to a minute for the game, then until `Paraworld.exe` and `PWClient.exe` have been gone for 3 seconds. Then it ends what is left (`PWServer.exe`, Discord status), sets `UseUslCPP` to `'1'` and closes the PWKiller window. |
| `--kill-all` (old: `-KillAll`) | No window. Ends the game and every other PWKiller, sets `UseUslCPP` to `'1'`. |

One instance per mode (named mutex). The window texts come from the launcher's `Locale.json` in the game language.

## CfgEditor

See `CfgEditor/README.md`. The launcher and PWKiller don't start CfgEditor.exe any more; they use the same code
(`Shared/Cfg`) directly. CfgEditor.exe stays for modders and scripts.