using MIRAGE.Shared.IO;
using MIRAGE_Launcher.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MIRAGE.Tools.Tests;

/// <summary>Checks the parts that no compiler checks: XAML bindings and the keys of Locale.json.</summary>
internal static partial class ConsistencyTests
{
    [Test]
    private static void XamlBindingsExist()
    {
        string? xamlFile = RepoPaths.Find("Launcher/Views/MainWindow.xaml");
        Assert.NotNull(xamlFile);
        string xaml = File.ReadAllText(xamlFile!);
        _ = XDocument.Parse(xaml);

        var main = typeof(MainViewModel).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(static p => p.Name).ToHashSet();
        var addon = typeof(AddonViewModel).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(static p => p.Name).ToHashSet();
        Dictionary<string, string> uk = LoadLocale()["UK"];
        List<string> problems = [];
        foreach (Match match in BindingRegex().Matches(xaml))
        {
            string path = match.Groups[1].Value;
            if (path.StartsWith("Text[", StringComparison.Ordinal))
            {
                string key = path[5..^1];
                if (!uk.ContainsKey(key))
                {
                    problems.Add($"Locale.json UK has no '{key}'");
                }
            }
            else if (!main.Contains(path) && !addon.Contains(path))
            {
                problems.Add($"no property '{path}'");
            }
        }
        Assert.Equal(string.Empty, string.Join("; ", problems));
    }

    [Test]
    private static void StatusXamlBindingsExist()
    {
        string? xamlFile = RepoPaths.Find("ParaWorldStatus/ParaWorldStatus/Views/MainWindow.xaml");
        Assert.NotNull(xamlFile);
        string xaml = File.ReadAllText(xamlFile!);
        _ = XDocument.Parse(xaml);
        var properties = typeof(ParaWorldStatus.ViewModels.MainViewModel).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(static p => p.Name).ToHashSet();
        var missing = BindingRegex().Matches(xaml).Select(static m => m.Groups[1].Value).Where(p => !properties.Contains(p)).ToList();
        Assert.Equal(string.Empty, string.Join(", ", missing));
    }

    [Test]
    private static void LocaleKeysUsedInCodeExist()
    {
        string? launcher = RepoPaths.Find("Launcher");
        Assert.NotNull(launcher);
        Dictionary<string, Dictionary<string, string>> locale = LoadLocale();
        HashSet<string> used = [];
        foreach (string file in Directory.EnumerateFiles(launcher!, "*.cs", SearchOption.AllDirectories))
        {
            string code = File.ReadAllText(file);
            foreach (Match m in KeyRegex().Matches(code))
            {
                _ = used.Add(m.Groups[1].Value);
            }
        }
        _ = used.Remove("OK");
        used.UnionWith(["PWKillerTitleText", "PWKillerButtonText"]);
        Assert.True(used.Count > 30, $"found {used.Count} keys");
        var missing = used.Where(k => !locale["UK"].ContainsKey(k)).Order().ToList();
        Assert.Equal(string.Empty, string.Join(", ", missing), "keys missing in UK");

        foreach ((string language, Dictionary<string, string> texts) in locale)
        {
            Assert.True(texts.Values.All(static t => t is not null), language);
            Assert.True(texts.Count >= 39, $"{language}: {texts.Count} texts");
        }
    }

    private static Dictionary<string, Dictionary<string, string>> LoadLocale()
    {
        string? file = RepoPaths.Find("Launcher/Locale.json");
        Assert.NotNull(file);
        return JsonFile.Load<Dictionary<string, Dictionary<string, string>>>(file!);
    }

    [GeneratedRegex(@"\{Binding\s+(?:Path=)?([A-Za-z]+(?:\[[A-Za-z]+\])?)")]
    private static partial Regex BindingRegex();

    /// <summary>Text["Key"], m_text["Key"], Message(n, "Key"), Text[cond ? "A" : "B"] (both).</summary>
    [GeneratedRegex(@"(?:[Tt]ext\[|Message\(\d+,\s*|\?\s*|:\s*)""([A-Z][A-Za-z]+)""")]
    private static partial Regex KeyRegex();
}