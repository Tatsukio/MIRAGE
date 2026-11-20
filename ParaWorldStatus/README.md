# ParaWorldStatus

`Tools/ParaWorldStatus/ParaWorldStatus.exe` (optional package `GameFiles/Optional/DiscordRPC`): shows what you do in
ParaWorld as your Discord status (rich presence), and lets friends join your multiplayer lobby from Discord.
.NET 10 WPF, MVVM like the launcher. Packages: only `DiscordRichPresence` and its `Newtonsoft.Json`.

## How it works

1. The game writes one line to `%APPDATA%\SpieleEntwicklungsKombinat\Paraworld\DiscordState\state_mirage.txt` on every
   change (`GameStateReporter.usl`), e.g.
   `GSRVersion=1,StateType=StateFull,Details=In multiplayer,Additional=Lobby,Tribe=Hu,MapFile=single_05.ula,...`.
2. `GameStateWatcher` notices the change, waits a quarter second (the game deletes and rewrites the file), reads it
   and parses it (`GameState.Parse`).
3. `PresenceBuilder` turns it into what Discord shows: text, map picture (`map_<map>` or `mapsetting_<setting>`), add-on
   picture, party size, and in a multiplayer lobby the "Join" (or "Spectate" when full) secret
   `<server>#<addon>|<addon>`.
4. `DiscordService` sends it to Discord (application id 723292670327390248).
5. "Join" / "Spectate" in Discord starts `MIRAGE Launcher.exe -PWSJoin -<secret>`; the launcher checks the add-ons and
   starts the game with `-autoconnect`.

The launcher starts ParaWorldStatus before every game when "EnableDiscordStatus" is on; PWKiller ends it after the
game.

## Structure

| Folder / file | |
|---|---|
| `Models/` | `GameState` (the state line), `MapInfo`, `PresenceData` (what Discord shows, without Discord types) |
| `Services/` | `GameStateWatcher`, `PresenceBuilder`, `MapCatalog` (the two JSON files), `LauncherStarter`, `DiscordService` (the only class with DiscordRPC types) |
| `ViewModels/` | `MainViewModel`: connection and status text, "Update status" / "Exit" |
| `Views/MainWindow.xaml` | The small window (starts minimized): the icon, the connection and the current status; right click for the menu |
| `App.xaml.cs` | Creates everything; one instance (named mutex) |
| `supported_maps.json`, `map_translations.json` | Embedded in the exe |

## The map files

Both are grouped only for reading ("Singleplayer maps", "SEK maps", "Custom maps"). Map file names are lower case,
without `.ula`.

`supported_maps.json`: maps that have a picture in the Discord application. The key needs the picture `map_<key>`; the
list has other map files that show the same picture.

```json
{ "Singleplayer maps": { "single_05": [ "multi_single_05", "koth_holy_city", "multi_4_sav_govhc" ] } }
```

`map_translations.json`: the name shown instead of the name in the map file.

```json
{ "Singleplayer maps": { "single_05": "Mission 5: The Holy City" } }
```

Maps without a picture show their setting (`mapsetting_savanna` ... ; `cave1` / `cave2` as savanna / ash valley), else
`mod_unknown`.

Log: `%APPDATA%\SpieleEntwicklungsKombinat\Paraworld\MIRAGE_ParaWorldStatus.log`.
