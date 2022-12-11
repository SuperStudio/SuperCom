using SuperUtils.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SuperCom.Config
{
    public static class UrlManager
    {

        public static string ReleaseUrl = "https://github.com/SuperStudio/SuperCom/releases";
        public static string UpgradeSource = "https://superstudio.github.io";
        public static string UpdateUrl = "https://superstudio.github.io/SuperCom-Upgrade/latest.json";
        public static string UpdateFileListUrl = "https://superstudio.github.io/SuperCom-Upgrade/list.json";
        public static string UpdateFilePathUrl = "https://superstudio.github.io/SuperCom-Upgrade/File/";
        public static string PluginUrl = "https://superstudio.github.io/SuperPlugins/";

        static UrlManager()
        {
            Type t = typeof(UrlManager);
            FieldInfo[] fields = t.GetFields(BindingFlags.Static | BindingFlags.Public);

            foreach (FieldInfo fi in fields)
            {
                string name = fi.Name;
                string value = FileHelper.TryReadConfigFromJson(name);
                if (!string.IsNullOrEmpty(value))
                {
                    fi.SetValue(null, value);
                }
            }
        }

    }
}
