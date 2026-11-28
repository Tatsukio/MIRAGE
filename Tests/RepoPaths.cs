using System;
using System.IO;

namespace MIRAGE.Tools.Tests;

/// <summary>Finds files of the repository (the folder with "MIRAGE.sln") from the test output folder.</summary>
internal static class RepoPaths
{
    public static string? Root
    {
        get
        {
            DirectoryInfo? dir = new(AppContext.BaseDirectory);
            while (dir is not null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "MIRAGE.sln")))
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            return null;
        }
    }

    /// <summary>A file or folder below the repository root, or null if it doesn't exist.</summary>
    public static string? Find(string p_RelativePath)
    {
        if (Root is not string root)
        {
            return null;
        }
        string path = Path.Combine(root, p_RelativePath);
        return (File.Exists(path) || Directory.Exists(path)) ? path : null;
    }
}