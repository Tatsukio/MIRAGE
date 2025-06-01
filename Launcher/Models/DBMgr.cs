using System;
using System.Xml.Linq;
using System.Xml.XPath;

namespace MIRAGE_Launcher.Models
{
    public class DBMgr
    {
        public XElement DB;

        private DBMgr() { }
        public DBMgr(FileMgr.Filepath p_filePath)
        {
            DB = Load(p_filePath);
        }

        private static XElement Load(FileMgr.Filepath p_filePath)
        {
            if (!p_filePath.IsExist)
            {
                Log.Error($"{p_filePath.Full} not found");
                Environment.Exit(2);
            }

            try
            {
                return XElement.Load(p_filePath.Full);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to load {p_filePath.Full}: {ex}");
                Environment.Exit(2);
                return null;
            }
        }

        public void Set(string p_key, string p_value)
        {
            if (!Places.launcherSettingsFilePath.IsWriteReady) return;
            try
            {
                var element = DB?.XPathSelectElement(p_key);
                if (element != null)
                {
                    element.Value = p_value;
                    DB.Save(Places.launcherSettingsFilePath.Full);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to set Misc/{p_key} to {p_value}: {ex}");
                return;
            }
        }

        public string Get(string p_key, string p_value)
        {
            string path = $"{p_key}/{p_value}";
            var value = DB?.XPathSelectElement(path)?.Value;
            if (value == null)
            {
                Log.Error($"Get from LauncherDB.xml failed - value at path '{path}' is null");
                return "DUMMY";
            }
            return value;
        }

        public bool GetB(string p_key, string p_value)
        {
            string musicSetting = Get(p_key, p_value);
            if (bool.TryParse(musicSetting, out bool result))
            {
                return result;
            }
            Log.Error($"Failed to convert {p_key}/{p_value} to bool");
            return false;
        }
    }
}
