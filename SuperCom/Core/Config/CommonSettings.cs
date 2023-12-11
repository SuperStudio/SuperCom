using SuperUtils.Framework.ORM.Config;
using System;
using System.Collections.Generic;

namespace SuperCom.Config.WindowConfig
{
    public class CommonSettings : AbstractConfig
    {

        public const string DEFAULT_LOG_NAME_FORMAT = "[%C] %Y-%MM-%DD %hh-%mm-%ss.%fff";

        public static string DEFAULT_LOG_SAVE_DIR { get; set; } =
            System.IO.Path.Combine(Environment.CurrentDirectory, "logs", "%Y-%MM-%DD");
        public static List<string> SUPPORT_FORMAT { get; set; } = new List<string>()
        {
            "%MM","%DD","%hh","%mm","%ss","%fff","%C","%R","%Y","%M","%D","%h","%m","%s","%f"
        };

        private CommonSettings() : base(ConfigManager.SQLITE_DATA_PATH, $"WindowConfig.CommonSettings")
        {
            FixedOnSearch = true;
            ScrollOnSearchClosed = true;
            FixedOnSendCommand = false;
            LogNameFormat = DEFAULT_LOG_NAME_FORMAT;
            LogSaveDir = DEFAULT_LOG_SAVE_DIR;
            WriteLogToFile = true;
        }

        private static CommonSettings _instance = null;

        public static CommonSettings CreateInstance()
        {
            if (_instance == null)
                _instance = new CommonSettings();

            return _instance;
        }
        public bool FixedOnSearch { get; set; }
        public bool CloseToBar { get; set; }
        public bool ScrollOnSearchClosed { get; set; }
        public bool FixedOnSendCommand { get; set; }
        public string LogNameFormat { get; set; }
        public string LogSaveDir { get; set; }
        public long TabSelectedIndex { get; set; }
        public long HighLightSideIndex { get; set; }
        public bool WriteLogToFile { get; set; }
        public long AsciiSelectedIndex { get; set; }
        public long RefSelectedIndex { get; set; }

    }
}
