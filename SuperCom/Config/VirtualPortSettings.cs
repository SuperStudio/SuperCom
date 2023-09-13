using SuperUtils.Framework.ORM.Config;

namespace SuperCom.Config.WindowConfig
{
    public class VirtualPortSettings : AbstractConfig
    {

        private VirtualPortSettings() : base(ConfigManager.SQLITE_DATA_PATH, $"WindowConfig.VirtualPortSettings")
        {
            Com0ConInstalledPath = "";
        }

        private static VirtualPortSettings _instance = null;

        public static VirtualPortSettings CreateInstance()
        {
            if (_instance == null)
                _instance = new VirtualPortSettings();

            return _instance;
        }
        public string Com0ConInstalledPath { get; set; }


    }
}
