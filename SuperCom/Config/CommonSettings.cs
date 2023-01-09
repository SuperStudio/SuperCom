using SuperControls.Style;
using SuperUtils.Framework.ORM.Config;
using SuperUtils.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace SuperCom.Config.WindowConfig
{
    public class CommonSettings : AbstractConfig
    {

        public static string DEFAULT_LOGNAMEFORMAT = "[%C][%R] %Y-%M-%D %h-%m-%s.%f";
        public static string DEFAULT_LOG_SAVE_DIR = System.IO.Path.Combine(Environment.CurrentDirectory, "logs");
        public static List<string> SUPPORT_FORMAT = new List<string>()
        {
            "%C","%R","%Y","%M","%D","%h","%m","%s","%f"
        };
        public static string LogDir { get; set; }
        public static string InitLogDir()
        {
            if (!string.IsNullOrEmpty(ConfigManager.CommonSettings.LogSaveDir))
                LogDir = ConfigManager.CommonSettings.LogSaveDir;
            else
                LogDir = DEFAULT_LOG_SAVE_DIR;

            if (!Directory.Exists(LogDir))
            {
                bool success = DirHelper.TryCreateDirectory(LogDir);
                if (!success)
                {
                    MessageCard.Error($"创建目录失败：{LogDir}，使用默认目录");
                    LogDir = DEFAULT_LOG_SAVE_DIR;
                    Directory.CreateDirectory(LogDir);
                }
            }
            return LogDir;
        }
        private CommonSettings() : base(ConfigManager.SQLITE_DATA_PATH, $"WindowConfig.CommonSettings")
        {
            FixedOnSearch = true;
            ScrollOnSearchClosed = true;
            FixedOnSendCommand = false;
            LogNameFormat = DEFAULT_LOGNAMEFORMAT;
            LogSaveDir = DEFAULT_LOG_SAVE_DIR;
        }

        private static CommonSettings _instance = null;

        public static CommonSettings CreateInstance()
        {
            if (_instance == null) _instance = new CommonSettings();

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

    }
}
