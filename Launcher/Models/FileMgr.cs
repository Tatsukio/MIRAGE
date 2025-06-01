using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;

namespace MIRAGE_Launcher.Models
{
    public static class FileMgr
    {
        private static readonly ImmutableHashSet<string> m_reservedNames =
        [
            "CON", "PRN", "AUX", "NUL",
            "COM0", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "COM¹", "COM²", "COM³",
            "LPT0", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9", "LPT¹", "LPT²", "LPT³"
        ];

        private static readonly HashSet<char> m_invalidPathChars = new(Path.GetInvalidPathChars());
        private static readonly HashSet<char> m_invalidFileNameChars = new(Path.GetInvalidFileNameChars());

        public readonly struct Filepath
        {
            public readonly string Path { get; }
            public readonly string File { get; }
            public readonly string Name { get; }
            public readonly string Full { get; }
            public readonly bool IsValid { get; }
            public bool IsExist => IsValid && System.IO.File.Exists(Full);
            public bool IsWriteReady => IsExist && IsWriteReady(this);
            public Filepath(string p_path, string p_file)
            {
                Path = p_path.Trim().FixPath();
                File = p_file.Trim();
                Name = System.IO.Path.GetFileNameWithoutExtension(File);
                Full = System.IO.Path.Combine(Path, File);
                IsValid = IsFilePathValid(this);
            }

            public override string ToString() => Full;
        }

        private static bool IsFilePathValid(Filepath p_filePath)
        {
            if (string.IsNullOrWhiteSpace(p_filePath.Path) || string.IsNullOrWhiteSpace(p_filePath.File))
            {
                Log.Warn("Path or file name is null or empty");
                return false;
            }

            if (p_filePath.Full.Length > 255)
            {
                Log.Warn("FilePath is too long");
                return false;
            }

            if (!Path.IsPathRooted(p_filePath.Path))
            {
                Log.Warn("Path must be absolute");
                return false;
            }

            return IsPathValid(p_filePath.Path) && IsFileNameValid(p_filePath.File);
        }

        private static bool IsReservedName(string name)
        {
            return m_reservedNames.Contains(name, StringComparer.OrdinalIgnoreCase);
        }

        public static HashSet<char> GetInvalidChars(this string input, HashSet<char> invalidChars)
        {
            HashSet<char> output = new();
            foreach (char c in input)
            {
                if (invalidChars.Contains(c))
                {
                    output.Add(c);
                }
            }
            return output;
        }

        private static bool IsPathValid(string p_path)
        {
            var invalidChars = p_path.GetInvalidChars(m_invalidPathChars);
            if (invalidChars.Count > 0)
            {
                Log.Warn($"Filename '{p_path}' contains invalid characters: ({string.Join(", ", invalidChars)})");
                return false;
            }
            return p_path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).All(component => !IsReservedName(component));
        }

        private static bool IsFileNameValid(string p_fileName)
        {
            var invalidChars = p_fileName.GetInvalidChars(m_invalidFileNameChars);
            if (invalidChars.Count > 0)
            {
                Log.Warn($"Filename '{p_fileName}' contains invalid characters: ({string.Join(", ", invalidChars)})");
                return false;
            }
            int dotCount = 0;
            StringBuilder fileNameWithoutExtension = new();
            for (int i = 0; i < p_fileName.Length; i++)
            {
                char c = p_fileName[i];
                if (c == '.')
                {
                    dotCount++;
                }
                else if (dotCount == 0)
                {
                    fileNameWithoutExtension.Append(c);
                }
            }

            if (dotCount != 1)
            {
                Log.Warn($"There is more than one dot in '{p_fileName}'");
                return false;
            }

            if (IsReservedName(fileNameWithoutExtension.ToString()))
            {
                Log.Warn($"Using reserved name '{p_fileName}' as filename is forbidden");
                return false;
            }

            return true;
        }

        private static bool IsWriteReady(Filepath p_filePath)
        {
            try
            {
                FileAttributes attributes = File.GetAttributes(p_filePath.Full);
                if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    Log.Warn($"File '{p_filePath}' is read-only");
                    return false;
                }

                if ((attributes & FileAttributes.System) == FileAttributes.System)
                {
                    Log.Warn($"File '{p_filePath}' is a system file");
                    return false;
                }

                using (FileStream stream = new FileStream(p_filePath.Full, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (FileNotFoundException)
            {
                Log.Warn($"File '{p_filePath}' not found");
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                Log.Warn($"Access to file '{p_filePath}' is denied");
                return false;
            }
            catch (IOException)
            {
                Log.Warn($"File '{p_filePath}' is locked");
                return true;
            }
            catch (Exception ex)
            {
                Log.Warn($"An error occurred while accessing the file '{p_filePath}': {ex.Message}");
                return false;
            }

            return true;
        }

        public static string FixPath(this string p_path)
        {
            if (p_path == null)
            {
                Log.Warn($"Path is null");
                return string.Empty;
            }
            return p_path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        }
    }
}
