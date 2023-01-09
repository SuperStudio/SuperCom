using SuperUtils.Framework.ORM.Config;
using System.Collections.Generic;
using System.Windows;

namespace SuperCom.Config.WindowConfig
{
    public class AdvancedSendSettings : AbstractConfig
    {
        public const double DEFAULT_WINDOW_OPACITY = 0.5;
        public const double DEFAULT_LOG_OPACITY = 0.8;
        private AdvancedSendSettings() : base(ConfigManager.SQLITE_DATA_PATH, $"WindowConfig.AdvancedSendSettings")
        {
            Width = SystemParameters.WorkArea.Width * 0.7;
            Height = SystemParameters.WorkArea.Height * 0.7;
            FirstRun = true;
            LogOpacity = DEFAULT_LOG_OPACITY;
            WindowOpacity = DEFAULT_WINDOW_OPACITY;
        }

        private static AdvancedSendSettings _instance = null;

        public static AdvancedSendSettings CreateInstance()
        {
            if (_instance == null) _instance = new AdvancedSendSettings();

            return _instance;
        }
        public long SideIndex { get; set; }
        public long ComPortSelectedIndex { get; set; }

        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public long WindowState { get; set; }
        public bool FirstRun { get; set; }
        public double WindowOpacity { get; set; }
        public bool ShowLogGrid { get; set; }
        public string SelectedPortNamesJson { get; set; }
        public bool LogAutoWrap { get; set; }
        public double LogOpacity { get; set; }


    }
}
