using SuperCom.Config.WindowConfig;
using SuperUtils.Framework.ORM.Config;
using System;

namespace SuperCom.Config
{
    public static class ConfigManager
    {
        public static Main Main = Main.CreateInstance();

        public static void InitConfig()
        {
            System.Reflection.FieldInfo[] fieldInfos = typeof(ConfigManager).GetFields();
            foreach (var item in fieldInfos)
            {
                AbstractConfig config = item.GetValue(null) as AbstractConfig;
                if (config == null)
                    throw new Exception("无法识别的 AbstractConfig");
                config.Read();
            }
        }

    }
}
