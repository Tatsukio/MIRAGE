using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;

namespace MIRAGE_Launcher.Helpers
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

        public class Filepath
        {
            public string Path { get; }
            public string File { get; }
            public string Name { get; }
            public string Full { get; }
            public bool IsValid { get; }
            public bool IsExist => IsExist(this);
            public bool IsWriteReady => IsWriteReady(this);
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

        public static string FixPath(this string p_path)
        {
            if (p_path == null)
            {
                Log.Warn($"Path is null");
                return string.Empty;
            }
            return p_path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        }

        private static bool IsFilePathValid(Filepath p_filepath)
        {
            if (string.IsNullOrWhiteSpace(p_filepath.Path) || string.IsNullOrWhiteSpace(p_filepath.File))
            {
                Log.Warn("Path or file name is null or empty");
                return false;
            }

            if (p_filepath.Full.Length > 255)
            {
                Log.Warn("FilePath is too long");
                return false;
            }

            if (!Path.IsPathRooted(p_filepath.Path))
            {
                Log.Warn("Path must be absolute");
                return false;
            }

            return IsPathValid(p_filepath.Path) && IsFileNameValid(p_filepath.File);
        }

        private static bool IsReservedName(string p_name)
        {
            return m_reservedNames.Contains(p_name, StringComparer.OrdinalIgnoreCase);
        }

        private static HashSet<char> GetInvalidChars(this string p_input, HashSet<char> p_invalidChars)
        {
            HashSet<char> output = new();
            foreach (char c in p_input)
            {
                if (p_invalidChars.Contains(c))
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

        private static bool IsExist(Filepath p_filepath)
        {
            return p_filepath.IsValid && File.Exists(p_filepath.Full);
        }

        private static bool IsWriteReady(Filepath p_filepath)
        {
            if (!p_filepath.IsExist) return false;
            try
            {
                FileAttributes attributes = File.GetAttributes(p_filepath.Full);
                if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    Log.Warn($"File '{p_filepath}' is read-only");
                    return false;
                }

                if ((attributes & FileAttributes.System) == FileAttributes.System)
                {
                    Log.Warn($"File '{p_filepath}' is a system file");
                    return false;
                }

                using FileStream stream = new(p_filepath.Full, FileMode.Open, FileAccess.Read, FileShare.None);
                stream.Close();
            }
            catch (UnauthorizedAccessException)
            {
                Log.Warn($"Access to file '{p_filepath}' is denied");
                return false;
            }
            catch (IOException)
            {
                Log.Warn($"File '{p_filepath}' is locked");
                return true;
            }
            catch (Exception ex)
            {
                Log.Warn($"An error occurred while accessing the file '{p_filepath}': {ex.Message}");
                return false;
            }
            return true;
        }
    }
}
