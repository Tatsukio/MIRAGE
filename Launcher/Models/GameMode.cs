namespace MIRAGE_Launcher.Models;

public enum GameMode
{
    /// <summary>Paraworld.exe</summary>
    Game,

    /// <summary>PWClient.exe -leveled</summary>
    MapEditor,

    /// <summary>Paraworld.exe -dedicated</summary>
    DedicatedServer,
}