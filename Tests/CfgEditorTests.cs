using CfgEditor;
using System.IO;
using System.Text;

namespace MIRAGE.Tools.Tests;

internal static class CfgEditorTests
{
    [Test]
    private static void CommandLine()
    {
        using TempDir dir = new();
        string file = dir.File("My Settings.cfg");
        File.WriteAllBytes(file, Encoding.Latin1.GetBytes(CfgTests.SettingsText));
        StringWriter output = new();
        StringWriter errors = new();
        CfgCommands commands = new(output, errors, dir.File("default.cfg"));

        Assert.Equal(CfgCommands.ExitOk, commands.Run([file]), "parse");
        Assert.Equal(CfgCommands.ExitOk, commands.Run(["-s", "Root/Pest/Server/UseUslCPP", "0", file]));
        Assert.Equal(CfgCommands.ExitOk, commands.Run(["--get", "Root/Pest/Server/UseUslCPP", file]));
        Assert.True(output.ToString().EndsWith("0" + System.Environment.NewLine, System.StringComparison.Ordinal), output.ToString());
        Assert.Equal(CfgCommands.ExitNotFound, commands.Run(["-g", "Root/Nope", file]));
        Assert.Equal(CfgCommands.ExitNothingRemoved, commands.Run(["-r", "Root/Nope", file]));
        Assert.Equal(CfgCommands.ExitOk, commands.Run(["-r", "Root/Empty", file]));
        Assert.Equal(CfgCommands.ExitWrongArgumentCount, commands.Run(["-s", "Root/A", file, "extra", "more"]));
        Assert.Equal(CfgCommands.ExitWrongCommand, commands.Run(["-x"]));
        Assert.Equal(CfgCommands.ExitError, commands.Run(["-g", "Root/A"]), "default file missing");

        string bad = dir.File("bad.cfg", "Root {\n");
        Assert.Equal(CfgCommands.ExitParseError, commands.Run([bad]));
        Assert.Equal(CfgCommands.ExitParseError, commands.Run(["-s", "Root/A", "1", bad]));
        Assert.Equal("Root {\n", File.ReadAllText(bad), "a file that can't be parsed is never written");
    }

    [Test]
    private static void InteractiveShell()
    {
        using TempDir dir = new();
        string file = dir.File("Settings.cfg");
        File.WriteAllBytes(file, Encoding.Latin1.GetBytes(CfgTests.SettingsText));
        StringWriter output = new();
        InteractiveShell shell = new(new StringReader("Set(Root/Global/Language,uk)\nget Root/Global/Language\nset Root/Global/Title Some text\nRemove(Root/Empty)\nq\n"), output, output, file);
        Assert.Equal(CfgCommands.ExitOk, shell.Run());
        var doc = MIRAGE.Shared.Cfg.CfgDocument.Load(file);
        Assert.Equal("uk", doc.Get("Global/Language"));
        Assert.Equal("Some text", doc.Get("Global/Title"));
        Assert.Null(doc.Find("Empty"));
    }
}