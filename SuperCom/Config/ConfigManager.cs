using SuperCom.Config.WindowConfig;
using SuperUtils.Framework.ORM.Config;
using System;

namespace SuperCom.Config
{
    public static class ConfigManager
    {
        public const string SQLITE_DATA_PATH = "user_data.sqlite";

        public static Main Main { get; set; }


        static ConfigManager()
        {
            Main = Main.CreateInstance();
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
