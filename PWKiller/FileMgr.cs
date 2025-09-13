using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;

namespace PWKiller
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
                return string.Empty;
            }
            return p_path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        }

        private static bool IsFilePathValid(Filepath p_filepath)
        {
            if (string.IsNullOrWhiteSpace(p_filepath.Path) || string.IsNullOrWhiteSpace(p_filepath.File))
            {
                return false;
            }

            if (p_filepath.Full.Length > 255)
            {
                return false;
            }

            if (!Path.IsPathRooted(p_filepath.Path))
            {
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
                return false;
            }
            return p_path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).All(component => !IsReservedName(component));
        }

        private static bool IsFileNameValid(string p_fileName)
        {
            var invalidChars = p_fileName.GetInvalidChars(m_invalidFileNameChars);
            if (invalidChars.Count > 0)
            {
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
                return false;
            }

            if (IsReservedName(fileNameWithoutExtension.ToString()))
            {
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
                    return false;
                }

                if ((attributes & FileAttributes.System) == FileAttributes.System)
                {
                    return false;
                }

                using FileStream stream = new(p_filepath.Full, FileMode.Open, FileAccess.Read, FileShare.None);
                stream.Close();
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (IOException)
            {
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }
    }
}
