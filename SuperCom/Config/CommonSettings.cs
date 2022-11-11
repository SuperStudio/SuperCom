using SuperUtils.Framework.ORM.Config;
using System.Windows;

namespace SuperCom.Config.WindowConfig
{
    public class CommonSettings : AbstractConfig
    {
        private CommonSettings() : base(ConfigManager.SQLITE_DATA_PATH, $"WindowConfig.CommonSettings")
        {
            FixedOnSearch = true;
            ScrollOnSearchClosed = true;
            FixedOnSendCommand = false;
        }

        private static CommonSettings _instance = null;

        public static CommonSettings CreateInstance()
        {
            if (_instance == null) _instance = new CommonSettings();

            return _instance;
        }
        public bool FixedOnSearch { get; set; }
        public bool ScrollOnSearchClosed { get; set; }
        public bool FixedOnSendCommand { get; set; }

    }
}
