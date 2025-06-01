#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace ParaWorldStatus.Model
{
    public static class TextFileReader
    {
        public static IDictionary<string, string> ReadSupportedMaps()
        {
            var supportedMaps = new Dictionary<string, string>();

            foreach (var line in ReadCommentedFile("supported_maps.txt"))
            {
                var maps = line.Split(':');
                foreach (var map in maps)
                {
                    supportedMaps[map] = maps[0];
                }
            }

            return supportedMaps;
        }

        public static IDictionary<string, string> ReadMapTranslations()
        {
            var translations = new Dictionary<string, string>();

            foreach (var line in ReadCommentedFile("map_translations.txt"))
            {
                var idxColon = line.IndexOf(':');
                if (idxColon != -1)
                {
                    translations[line.Substring(0, idxColon)] = line.Substring(idxColon + 1);
                }
            }

            return translations;
        }

        private static IEnumerable<string> ReadCommentedFile(string filename)
        {
            using (var stream = OpenResourceStream(Assembly.GetCallingAssembly(), filename))
            using (var reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine().Trim();
                    if (line.StartsWith(';') || line.Length == 0)
                    {
                        continue;
                    }
                    yield return line;
                }
            }
        }

        public static Stream OpenResourceStream(Assembly assembly, string resource)
        {
            var assemblyName = assembly.GetName().Name;
            var stream = assembly.GetManifestResourceStream($"{assemblyName}.{resource}");
            if (stream == null)
            {
                throw new FileNotFoundException($"{resource} was not found");
            }
            return stream;
        }
    }
}
