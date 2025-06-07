#nullable enable

using System;
using System.IO;

namespace ParaWorldStatus.Model
{
    public class PWState
    {
        public event Action<bool>? StateChanged;

        public string? StateType { get; set; }
        public string? Details { get; set; }
        public string? Additional { get; set; }
        public string? Tribe { get; set; }
        public string? MapFile { get; set; }
        public string? MapName { get; set; }
        public string? MapSetting { get; set; }
        public string? Mods { get; set; }
        public string? GameMode { get; set; }
        public string? Server { get; set; }
        public int NumPlayers { get; set; }
        public int MaxPlayers { get; set; }

        static readonly string StateFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SpieleEntwicklungsKombinat/Paraworld/DiscordState/");
        static readonly string StateFile = "state_mirage.txt";
        static string oldState = "";
        public void PWStateFileWatcher()
        {
            FileSystemWatcher watcher = new()
            {
                Path = StateFolder,
                Filter = StateFile,
                NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
            };

            watcher.Changed += new FileSystemEventHandler(TryReadPWStateFile);
            watcher.Created += new FileSystemEventHandler(TryReadPWStateFile);
            watcher.Deleted += new FileSystemEventHandler(TryReadPWStateFile);
            watcher.EnableRaisingEvents = true;
        }
        private void TryReadPWStateFile(object source, FileSystemEventArgs e)
        {
            var fs = new FileStream(Path.Combine(StateFolder,StateFile), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sr = new StreamReader(fs);
            string state = sr.ReadLine();
            if (state != null && state != oldState)
            {
                oldState = state;

                var stateKeyValuePair = state.Split(',');

                string stateValue = stateKeyValuePair[0].Split('=')[1];
                if (stateValue == "1")
                {
                    for (int i = 1; i < stateKeyValuePair.Length; i++)
                    {
                        string[] stateKeyValue = stateKeyValuePair[i].Split('=');

                        switch (stateKeyValue[0])
                        {
                            case "StateType":
                                StateType = stateKeyValue[1];
                                break;
                            case "Details":
                                Details = stateKeyValue[1];
                                break;
                            case "Additional":
                                Additional = stateKeyValue[1];
                                break;
                            case "Tribe":
                                Tribe = stateKeyValue[1];
                                break;
                            case "MapFile":
                                MapFile = stateKeyValue[1];
                                break;
                            case "MapName":
                                MapName = stateKeyValue[1];
                                break;
                            case "MapSetting":
                                MapSetting = stateKeyValue[1];
                                break;
                            case "Mods":
                                Mods = stateKeyValue[1];
                                break;
                            case "GameMode":
                                GameMode = stateKeyValue[1];
                                break;
                            case "Server":
                                Server = stateKeyValue[1];
                                break;
                            case "NumPlayers":
                                NumPlayers = int.Parse(stateKeyValue[1]);
                                break;
                            case "MaxPlayers":
                                MaxPlayers = int.Parse(stateKeyValue[1]);
                                break;
                        }
                    }
                    StateChanged?.Invoke(true);
                }
            }
        }
    }

    public class ImageData
    {
        public string Key { get; set; } = "";
        public string? Text { get; set; }
    }

    public class MapInfo
    {
        public string Filename { get; }
        public string Name { get; }
        public string Setting { get; }

        public MapInfo(string filename, string name, string setting)
        {
            var idxDot = filename.LastIndexOf('.');
            if (idxDot != -1)
            {
                // remove .ula
                filename = filename.Substring(0, idxDot);
            }

            Filename = filename.ToLowerInvariant();
            Name = name;
            Setting = setting;
        }
    }
}
