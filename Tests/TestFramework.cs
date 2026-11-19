using MIRAGE.Shared.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MIRAGE.Tools.Tests;

/// <summary>Marks a static method as a test. It may return void or Task.</summary>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class TestAttribute : Attribute;

internal sealed class AssertException(string p_Message) : Exception(p_Message);

internal static class Assert
{
    public static void True(bool p_Condition, string p_What = "")
    {
        if (!p_Condition)
        {
            throw new AssertException($"Expected true. {p_What}");
        }
    }

    public static void False(bool p_Condition, string p_What = "") => True(!p_Condition, p_What);

    public static void Equal<T>(T p_Expected, T p_Actual, string p_What = "")
    {
        if (!EqualityComparer<T>.Default.Equals(p_Expected, p_Actual))
        {
            throw new AssertException($"{p_What}\n  expected: {Show(p_Expected)}\n  actual:   {Show(p_Actual)}");
        }
    }

    public static void Null(object? p_Value, string p_What = "") => True(p_Value is null, $"Expected null, got {p_Value}. {p_What}");

    public static void NotNull(object? p_Value, string p_What = "") => True(p_Value is not null, $"Expected a value. {p_What}");

    public static T Throws<T>(Action p_Action)
        where T : Exception
    {
        try
        {
            p_Action();
        }
        catch (T ex)
        {
            return ex;
        }
        catch (Exception ex)
        {
            throw new AssertException($"Expected {typeof(T).Name}, got {ex.GetType().Name}: {ex.Message}");
        }
        throw new AssertException($"Expected {typeof(T).Name}, nothing was thrown.");
    }

    private static string Show<T>(T p_Value) => p_Value is string s ? "\"" + s.Replace("\r", "\\r").Replace("\n", "\\n") + "\"" : $"{p_Value}";
}

/// <summary>A temporary folder that is deleted at the end.</summary>
internal sealed class TempDir : IDisposable
{
    public TempDir()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mirage_tests_" + Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string File(string p_RelativePath, string? p_Content = null)
    {
        string full = System.IO.Path.Combine(Path, p_RelativePath);
        _ = Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
        if (p_Content is not null)
        {
            System.IO.File.WriteAllText(full, p_Content);
        }
        return full;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, true);
        }
        catch (IOException)
        {
            // Left in %TEMP%.
        }
    }
}

internal static class Program
{
    private static int Main(string[] p_Args)
    {
        // Settings.cfg is in the system's ANSI code page (Cyrillic 1251 on a Russian Windows ...). The test texts are
        // Western European, so the tests use 1252 on every system.
        TextEncodings.Ansi = TextEncodings.GetCodePage(1252);

        var tests = typeof(Program).Assembly.GetTypes()
            .SelectMany(static t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(static m => m.GetCustomAttribute<TestAttribute>() is not null)
            .Where(m => (p_Args.Length == 0) || p_Args.Any(a => $"{m.DeclaringType!.Name}.{m.Name}".Contains(a, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(static m => m.DeclaringType!.Name).ThenBy(static m => m.Name)
            .ToList();

        int failed = 0;
        var total = Stopwatch.StartNew();
        foreach (MethodInfo test in tests)
        {
            string name = $"{test.DeclaringType!.Name}.{test.Name}";
            try
            {
                object? result = test.Invoke(null, null);
                if (result is Task task)
                {
                    task.GetAwaiter().GetResult();
                }
                Console.WriteLine($"PASS {name}");
            }
            catch (Exception ex)
            {
                failed++;
                Exception inner = (ex is TargetInvocationException tie) ? tie.InnerException ?? ex : ex;
                Console.WriteLine($"FAIL {name}: {inner.GetType().Name}: {inner.Message}");
                if (inner is not AssertException)
                {
                    Console.WriteLine(inner.StackTrace);
                }
            }
        }
        Console.WriteLine($"{tests.Count - failed} passed, {failed} failed ({total.ElapsedMilliseconds} ms)");
        return (failed == 0) ? 0 : 1;
    }
}