using SuperUtils.Framework.ORM.Config;
using System.Collections.Generic;
using System.Windows;

namespace SuperCom.Config.WindowConfig
{
    public class CommonSettings : AbstractConfig
    {

        public static string DEFAULT_LOGNAMEFORMAT = "[%C][%R] %Y-%M-%D %h-%m-%s.%f";
        public static List<string> SUPPORT_FORMAT = new List<string>()
        {
            "%C","%R","%Y","%M","%D","%h","%m","%s","%f"
        };
        private CommonSettings() : base(ConfigManager.SQLITE_DATA_PATH, $"WindowConfig.CommonSettings")
        {
            FixedOnSearch = true;
            ScrollOnSearchClosed = true;
            FixedOnSendCommand = false;
            LogNameFormat = DEFAULT_LOGNAMEFORMAT;
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
        public string LogNameFormat { get; set; }

    }
}
