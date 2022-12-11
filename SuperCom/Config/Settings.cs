using SuperUtils.Framework.ORM.Config;
using System.Collections.Generic;
using System.Windows;

namespace SuperCom.Config.WindowConfig
{
    public class Settings : AbstractConfig
    {
        private Settings() : base(ConfigManager.SQLITE_DATA_PATH, $"WindowConfig.Settings")
        {

        }

        private static Settings _instance = null;

        public static Settings CreateInstance()
        {
            if (_instance == null) _instance = new Settings();

            return _instance;
        }

        public string CurrentLanguage { get; set; }
        public long ThemeIdx { get; set; }
        public string ThemeID { get; set; }

    }
}
