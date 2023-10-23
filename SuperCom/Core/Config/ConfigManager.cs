using SuperCom.Config.WindowConfig;
using SuperUtils.Framework.ORM.Config;
using System;
using System.IO;
using System.Reflection;

namespace SuperCom.Config
{
    public static class ConfigManager
    {
        public static string RELEASE_DATE = 
            new FileInfo(Assembly.GetExecutingAssembly().Location).LastWriteTime.ToString("yyyy-MM-dd");

        public const string SQLITE_DATA_PATH = "user_data.sqlite";

        public const string APP_NAME = "SuperCom";
        public const string APP_SUB_NAME = "超级串口工具";
        public static Main Main { get; set; }
        public static CommonSettings CommonSettings { get; set; }
        public static Settings Settings { get; set; }
        public static AdvancedSendSettings AdvancedSendSettings { get; set; }
        public static VarMonitorSetting VarMonitorSetting { get; set; }
        public static VirtualPortSettings VirtualPortSettings { get; set; }



        static ConfigManager()
        {
            Main = Main.CreateInstance();
            CommonSettings = CommonSettings.CreateInstance();
            AdvancedSendSettings = AdvancedSendSettings.CreateInstance();
            VarMonitorSetting = VarMonitorSetting.CreateInstance();
            VirtualPortSettings = VirtualPortSettings.CreateInstance();
            Settings = Settings.CreateInstance();
        }

        public static void InitConfig()
        {
            System.Reflection.PropertyInfo[] propertyInfos = typeof(ConfigManager).GetProperties();
            foreach (var item in propertyInfos) {
                AbstractConfig config = item.GetValue(null) as AbstractConfig;
                if (config == null)
                    throw new Exception("无法识别的 AbstractConfig");
                config.Read();
            }
        }

    }
}
