using System;
using System.Xml.Linq;
using System.Xml.XPath;

namespace PWKiller
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
                return null;
            }

            try
            {
                return XElement.Load(p_filePath.Full);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public string Get(string p_key, string p_value)
        {
            string path = $"{p_key}/{p_value}";
            var value = DB?.XPathSelectElement(path)?.Value;
            if (value == null)
            {
                return string.Empty;
            }
            return value;
        }
    }
}
