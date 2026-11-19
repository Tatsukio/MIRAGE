using MIRAGE.Shared.Cfg;
using MIRAGE.Shared.IO;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace MIRAGE.Tools.Tests;

internal static class CfgTests
{
    /// <summary>Like Settings.cfg: tabs, CRLF, a value with a non-ASCII character (cp1252 0xE4).</summary>
    public const string SettingsText =
        "Root {\r\n" +
        "\tGlobal {\r\n" +
        "\t\tLanguage = 'de'\r\n" +
        "\t\tPlayerName = 'Jäger'\r\n" +
        "\t}\r\n" +
        "\tPest {\r\n" +
        "\t\tServer {\r\n" +
        "\t\t\tUseUslCPP = '1'\r\n" +
        "\t\t}\r\n" +
        "\t}\r\n" +
        "\tEmpty\r\n" +
        "}\r\n";

    [Test]
    private static void ParseAndGet()
    {
        var doc = CfgDocument.Parse(SettingsText);
        Assert.Equal("Root", doc.Root.Name);
        Assert.Equal("de", doc.Get("Root/Global/Language"));
        Assert.Equal("de", doc.Get("Global/Language"), "root name may be left out");
        Assert.Equal("Jäger", doc.Get("Root/Global/PlayerName"));
        Assert.Equal("1", doc.Get(GameSettings.UseUslCpp));
        Assert.Equal(string.Empty, doc.Get("Root/Empty"), "node without value");
        Assert.Null(doc.Get("Root/Nope"));
        Assert.Null(doc.Get("Root/Global/Language/Deeper"));
        Assert.Equal(3, doc.Root.Children.Count);
        Assert.Equal("Root/Pest/Server", doc.Find("Pest/Server")!.Path);
    }

    [Test]
    private static void RoundTripIsByteIdentical()
    {
        byte[] bytes = Encoding.Latin1.GetBytes(SettingsText);
        Assert.True(CfgDocument.FromBytes(bytes).ToBytes().SequenceEqual(bytes));

        byte[] bom = [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("Root {\n\tName = 'Жä'\n}\n")];
        var doc = CfgDocument.FromBytes(bom);
        Assert.True(doc.HasBom);
        Assert.Equal("Жä", doc.Get("Root/Name"), "UTF-8 value with BOM");
        Assert.True(doc.ToBytes().SequenceEqual(bom));
    }

    [Test]
    private static void SetExistingValueChangesOnlyTheValue()
    {
        var doc = CfgDocument.Parse(SettingsText);
        Assert.True(doc.Set(GameSettings.UseUslCpp, "0"));
        Assert.Equal(SettingsText.Replace("UseUslCPP = '1'", "UseUslCPP = '0'"), doc.ToString());
        Assert.True(doc.IsModified);
        Assert.False(doc.Set(GameSettings.UseUslCpp, "0"), "same value: no change");
    }

    [Test]
    private static void SetCreatesMissingNodes()
    {
        var doc = CfgDocument.Parse(SettingsText);
        _ = doc.Set("Root/Game/Sequence/DisableSkip", "1");
        _ = doc.Set("Root/Global/New", "x");
        string expected = SettingsText
            .Replace("\t\tPlayerName = 'Jäger'\r\n", "\t\tPlayerName = 'Jäger'\r\n\t\tNew = 'x'\r\n")
            .Replace("\tEmpty\r\n}\r\n", "\tEmpty\r\n\tGame {\r\n\t\tSequence {\r\n\t\t\tDisableSkip = '1'\r\n\t\t}\r\n\t}\r\n}\r\n");
        Assert.Equal(expected, doc.ToString());
        Assert.Equal("1", CfgDocument.Parse(doc.ToString()).Get("Game/Sequence/DisableSkip"));
    }

    [Test]
    private static void SetOnNodeWithoutValueOrBlock()
    {
        var doc = CfgDocument.Parse(SettingsText);
        _ = doc.Set("Root/Empty", "v");
        Assert.True(doc.ToString().Contains("\tEmpty = 'v'\r\n", StringComparison.Ordinal));
        _ = doc.Set("Root/Empty/Child", "c");
        Assert.True(doc.ToString().Contains("\tEmpty = 'v' {\r\n\t\tChild = 'c'\r\n\t}\r\n", StringComparison.Ordinal), doc.ToString());
        Assert.Equal("v", doc.Get("Root/Empty"));
        Assert.Equal("c", doc.Get("Root/Empty/Child"));
    }

    [Test]
    private static void SetInSingleLineBlock()
    {
        var doc = CfgDocument.Parse("Root { A = '1' }");
        _ = doc.Set("Root/B/C", "2");
        var again = CfgDocument.Parse(doc.ToString());
        Assert.Equal("1", again.Get("A"));
        Assert.Equal("2", again.Get("B/C"));
    }

    [Test]
    private static void LfFileGetsLfLines()
    {
        var doc = CfgDocument.Parse(SettingsText.Replace("\r\n", "\n"));
        _ = doc.Set("Root/X", "1");
        Assert.False(doc.ToString().Contains('\r'), "no CR in an LF file");
    }

    [Test]
    private static void Remove()
    {
        var doc = CfgDocument.Parse(SettingsText);
        Assert.True(doc.Remove("Root/Global/PlayerName"));
        Assert.Equal(SettingsText.Replace("\t\tPlayerName = 'Jäger'\r\n", string.Empty), doc.ToString());
        Assert.True(doc.Remove("Root/Pest"));
        Assert.Null(doc.Get(GameSettings.UseUslCpp));
        Assert.False(doc.Remove("Root/Nope"));
        _ = Assert.Throws<InvalidOperationException>(() => doc.Remove("Root"));
        Assert.Equal("Root {\r\n\tGlobal {\r\n\t\tLanguage = 'de'\r\n\t}\r\n\tEmpty\r\n}\r\n", doc.ToString());
    }

    [Test]
    private static void InvalidValuesAndNames()
    {
        var doc = CfgDocument.Parse(SettingsText);
        _ = Assert.Throws<ArgumentException>(() => doc.Set("Root/A", "it's"));
        _ = Assert.Throws<ArgumentException>(() => doc.Set("Root/A", "a\nb"));
        _ = Assert.Throws<ArgumentException>(() => doc.Set("Root/Bad Name", "1"));
        Assert.Equal(SettingsText, doc.ToString(), "nothing changed");
    }

    [Test]
    private static void SystemCodePageIsUsed()
    {
        // A Russian Windows: Settings.cfg in 1251. The tests use 1252 otherwise (Program.Main).
        Encoding saved = TextEncodings.Ansi;
        try
        {
            TextEncodings.Ansi = TextEncodings.GetCodePage(1251);
            byte[] bytes = TextEncodings.Ansi.GetBytes("Root {\r\n\tPlayerName = '\u0416\u0443\u043A'\r\n}\r\n");
            var doc = CfgDocument.FromBytes(bytes);
            Assert.Equal("\u0416\u0443\u043A", doc.Get("Root/PlayerName"));
            _ = doc.Set("Root/PlayerName", "\u0416\u0443\u043A2");
            Assert.Equal("Root {\r\n\tPlayerName = '\u0416\u0443\u043A2'\r\n}\r\n", TextEncodings.Ansi.GetString(doc.ToBytes()));
        }
        finally
        {
            TextEncodings.Ansi = saved;
        }
    }

    [Test]
    private static void ParseErrorsHaveLineAndColumn()
    {
        CfgParseException ex = Assert.Throws<CfgParseException>(() => CfgDocument.Parse("Root {\n\tA = '1'\n\tB {\n\t\tC = '2'\n}\n"));
        Assert.Equal(1, ex.Line, "the block that isn't closed: " + ex.Message);
        Assert.True(ex.Message.Contains("'Root'", StringComparison.Ordinal), ex.Message);
        ex = Assert.Throws<CfgParseException>(() => CfgDocument.Parse("Root {\n\tA = '1\n}\n"));
        Assert.Equal(2, ex.Line, ex.Message);
        _ = Assert.Throws<CfgParseException>(() => CfgDocument.Parse(string.Empty));
        _ = Assert.Throws<CfgParseException>(() => CfgDocument.Parse("Root {\n}\n}\n"));
    }

    [Test]
    private static void ScriptFileFormats()
    {
        // Values without quotes, commas between nodes, a value and a block on the same node.
        var doc = CfgDocument.Parse("Root {\n\tWin {\n\t\tpos_x = 100\n\t\tstyle = border|caption\n\t},\n\tT = 'x' {\n\t\tU = 'y'\n\t}\n}\n");
        Assert.Equal("100", doc.Get("Win/pos_x"));
        Assert.Equal("border|caption", doc.Get("Win/style"));
        Assert.Equal("x", doc.Get("T"));
        Assert.Equal("y", doc.Get("T/U"));
        _ = doc.Set("Win/pos_x", "5");
        Assert.True(doc.ToString().Contains("pos_x = '5'", StringComparison.Ordinal));

        // No '=' before the value; a ' inside a value.
        doc = CfgDocument.Parse("Root {\n\tA {\n\t\ttype 'CFrameWindow'\n\t\ttooltip = 'Player who's TT'\n\t\tname = 'O'Brien' {\n\t\t}\n\t}\n}\n");
        Assert.Equal("CFrameWindow", doc.Get("A/type"));
        Assert.Equal("Player who's TT", doc.Get("A/tooltip"));
        Assert.Equal("O'Brien", doc.Get("A/name"));
        _ = doc.Set("A/type", "X");
        Assert.True(doc.ToString().Contains("\t\ttype 'X'\n", StringComparison.Ordinal), doc.ToString());
    }

    [Test]
    private static void SaveAndLoad()
    {
        using TempDir dir = new();
        string file = dir.File("Settings.cfg");
        File.WriteAllBytes(file, Encoding.Latin1.GetBytes(SettingsText));
        Assert.True(GameSettings.SetValue(file, GameSettings.UseUslCpp, "0"));
        Assert.False(GameSettings.SetValue(file, GameSettings.UseUslCpp, "0"), "no change, no write");
        Assert.Equal("0", CfgDocument.Load(file).Get(GameSettings.UseUslCpp));
        Assert.Equal(1, Directory.GetFiles(dir.Path).Length, "no temp file left");
        Assert.Equal("Jäger", CfgDocument.Load(file).Get("Global/PlayerName"), "other values kept");
    }

    [Test]
    private static void TryLoadReportsErrors()
    {
        using TempDir dir = new();
        Assert.False(CfgDocument.TryLoad(dir.File("missing.cfg"), out _, out string error));
        Assert.True(error.Length > 0);
        string bad = dir.File("bad.cfg", "Root {\n");
        Assert.False(CfgDocument.TryLoad(bad, out _, out error));
        Assert.True(error.Contains("line", StringComparison.Ordinal), error);
    }

    /// <summary>Every config file of the repository that starts with "Root": parsed and written back unchanged.</summary>
    [Test]
    private static void RepositoryFilesRoundTrip()
    {
        string? gameFiles = RepoPaths.Find("GameFiles/Basic/Data");
        if (gameFiles is null)
        {
            Console.WriteLine("  (GameFiles/Basic/Data not found, skipped)");
            return;
        }
        string[] extensions = [".txt", ".cfg", ".ini", ".srf", ".ttree"];
        int ok = 0;
        int unreadable = 0;
        foreach (string file in Directory.EnumerateFiles(gameFiles, "*", SearchOption.AllDirectories))
        {
            if (!extensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }
            byte[] bytes = File.ReadAllBytes(file);
            string head = Encoding.Latin1.GetString(bytes, 0, Math.Min(64, bytes.Length)).TrimStart('\u00EF', '\u00BB', '\u00BF', ' ', '\t', '\r', '\n');
            if (!head.StartsWith("Root", StringComparison.Ordinal))
            {
                continue;
            }
            CfgDocument doc;
            try
            {
                doc = CfgDocument.FromBytes(bytes);
            }
            catch (CfgParseException)
            {
                // Three editor .srf files have real errors (a // comment, an extra }); the health check lists them.
                unreadable++;
                continue;
            }
            Assert.True(doc.ToBytes().SequenceEqual(bytes), file);
            ok++;
        }
        Console.WriteLine($"  ({ok} files unchanged, {unreadable} with syntax errors)");
        Assert.True(ok > 100, "files found");
    }
}