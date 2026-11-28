namespace MIRAGE.Shared.Cfg;

/// <summary>The Settings.cfg entries the tools read or write.</summary>
public static class GameSettings
{
    /// <summary>Game language, e.g. 'uk', 'de'.</summary>
    public const string Language = "Root/Global/Language";

    /// <summary>
    /// '0': the server runs the .usl scripts from the Data folders (needed by MIRAGE). '1': it runs the compiled
    /// scripts (the original game).
    /// </summary>
    public const string UseUslCpp = "Root/Pest/Server/UseUslCPP";

    public const string AutoKillCache = "Root/Global/AutoKillCache";

    /// <summary>Loads the file, sets one value and saves the file if the value changed.</summary>
    public static bool SetValue(string p_CfgFile, string p_Path, string p_Value)
    {
        var document = CfgDocument.Load(p_CfgFile);
        if (!document.Set(p_Path, p_Value))
        {
            return false;
        }
        document.Save();
        return true;
    }
}