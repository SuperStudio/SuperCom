using SuperUtils.Framework.ORM.Config;
using System.Collections.Generic;
using System.Windows;

namespace SuperCom.Config.WindowConfig
{
    public class AdvancedSendSettings : AbstractConfig
    {

        private AdvancedSendSettings() : base(ConfigManager.SQLITE_DATA_PATH, $"WindowConfig.AdvancedSendSettings")
        {

        }

        private static AdvancedSendSettings _instance = null;

        public static AdvancedSendSettings CreateInstance()
        {
            if (_instance == null) _instance = new AdvancedSendSettings();

            return _instance;
        }
        public int SideIndex { get; set; }
        public int ComPortSelectedIndex { get; set; }


    }
}
