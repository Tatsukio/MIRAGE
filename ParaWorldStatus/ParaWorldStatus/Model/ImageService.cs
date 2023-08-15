#nullable enable

using System.Collections.Generic;

namespace ParaWorldStatus.Model
{
    public static class ImageService
    {
        private static readonly IDictionary<string, string> _supportedMaps = TextFileReader.ReadSupportedMaps();

        public static string GetImage(MapInfo map)
        {
            var mapname = map.Filename;

            if (_supportedMaps.TryGetValue(mapname, out var discordMapName))
            {
                return "map_" + discordMapName;
            }

            var setting = map.Setting.ToLowerInvariant();
            switch (setting)
            {
                case "cave1": setting = "savanna"; break;
                case "cave2": setting = "ashvalley"; break;
            }

            if (IsSupportedSetting(setting))
            {
                return "mapsetting_" + setting;
            }

            return Unknown;
        }

        private static bool IsSupportedSetting(string setting)
        {
            // every map setting here needs its own image asset "mapsetting_xxx"
            switch (setting)
            {
                case "ashvalley":
                case "savanna":
                case "jungle":
                case "northland":
                case "cave1": // holy city
                case "cave2": // mission 16
                case "cave3": // Oasis in mirage
                case "icewaste": return true;
            }
            return false;
        }
        private static string Unknown { get; } = "mod_unknown";

    }
}
