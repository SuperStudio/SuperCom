using SuperUtils.Framework.ORM.Config;
using System.Collections.Generic;
using System.Windows;

namespace SuperCom.Config.WindowConfig
{
    public class AdvancedSendSettings : AbstractConfig
    {

        private AdvancedSendSettings() : base(ConfigManager.SQLITE_DATA_PATH, $"WindowConfig.AdvancedSendSettings")
        {
            Width = SystemParameters.WorkArea.Width * 0.7;
            Height = SystemParameters.WorkArea.Height * 0.7;
            FirstRun = true;
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


    }
}
