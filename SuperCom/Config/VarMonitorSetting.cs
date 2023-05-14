using SuperUtils.Framework.ORM.Config;
using System.Collections.Generic;
using System.Windows;

namespace SuperCom.Config.WindowConfig
{
    public class VarMonitorSetting : AbstractConfig
    {
        private VarMonitorSetting() : base(ConfigManager.SQLITE_DATA_PATH, $"WindowConfig.VarMonitorSetting")
        {

        }

        private static VarMonitorSetting _instance = null;

        public static VarMonitorSetting CreateInstance()
        {
            if (_instance == null) _instance = new VarMonitorSetting();

            return _instance;
        }
        public long SideIndex { get; set; }
    }
}
