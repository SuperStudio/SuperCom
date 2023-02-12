using SuperCom.Config.WindowConfig;
using SuperUtils.Framework.ORM.Config;
using System;

namespace SuperCom.Config
{
    public static class ConfigManager
    {
        public const string SQLITE_DATA_PATH = "user_data.sqlite";

        public const string RELEASE_DATE = "2023-02-12";
        public static Main Main { get; set; }
        public static CommonSettings CommonSettings { get; set; }
        public static Settings Settings { get; set; }
        public static AdvancedSendSettings AdvancedSendSettings { get; set; }
        public static VirtualPortSettings VirtualPortSettings { get; set; }


        static ConfigManager()
        {
            Main = Main.CreateInstance();
            CommonSettings = CommonSettings.CreateInstance();
            AdvancedSendSettings = AdvancedSendSettings.CreateInstance();
            VirtualPortSettings = VirtualPortSettings.CreateInstance();
            Settings = Settings.CreateInstance();
        }

        public static void InitConfig()
        {
            System.Reflection.PropertyInfo[] propertyInfos = typeof(ConfigManager).GetProperties();
            foreach (var item in propertyInfos)
            {
                AbstractConfig config = item.GetValue(null) as AbstractConfig;
                if (config == null)
                    throw new Exception("无法识别的 AbstractConfig");
                config.Read();
            }
        }

    }
}
