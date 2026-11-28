using System;
using MIRAGE.Shared;

namespace CfgEditor;

internal static class Program
{
    private static int Main(string[] p_Args)
    {
        string defaultFile = new GamePaths(AppContext.BaseDirectory).SettingsCfg;
        try
        {
            return p_Args.Length == 0
                ? new InteractiveShell(Console.In, Console.Out, Console.Error, defaultFile).Run()
                : new CfgCommands(Console.Out, Console.Error, defaultFile).Run(p_Args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"CfgEditor error: {ex}");
            return CfgCommands.ExitError;
        }
    }
}