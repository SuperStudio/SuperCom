using Newtonsoft.Json.Linq;
using SuperControls.Style.Upgrade;
using SuperUtils.Common;
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
        public static Dictionary<string, UpgradeSource> UpgradeSourceDict = new Dictionary<string, UpgradeSource>()
        {
            {"Github",new UpgradeSource("https://superstudio.github.io/","https://github.com/SuperStudio/SuperCom/releases","SuperCom-Upgrade") },
            {"StormGit加速",new UpgradeSource("https://venomplum-54uhej.stormkit.dev/","https://github.com/SuperStudio/SuperCom/releases","") },
            {"Github加速",new UpgradeSource("https://cdn.jsdelivr.net/gh/SuperStudio/","https://gitee.com/SuperStudio/SuperCom/releases","SuperCom-Upgrade") },
        };

        public static List<string> UpgradeSourceKeys = UpgradeSourceDict.Keys.ToList();

        private static int RemoteIndex = (int)ConfigManager.Settings.RemoteIndex; // 用户切换源的时候存储起来

        private static string DonateJsonBasePath = "SuperSudio-Donate";
        private static string PluginBasePath = "SuperPlugins";
        public static string FeedbackUrl = "https://github.com/SuperStudio/SuperCom/issues";
        public static string HelpUrl = "https://github.com/SuperStudio/SuperCom/wiki";

        public static int GetRemoteIndex()
        {
            return RemoteIndex;
        }
        public static void SetRemoteIndex(int idx)
        {
            RemoteIndex = idx;
        }
        public static string GetRemoteBasePath()
        {
            if (RemoteIndex < 0 || RemoteIndex >= UpgradeSourceKeys.Count)
                RemoteIndex = 0;
            return UpgradeSourceDict[UpgradeSourceKeys[RemoteIndex]].BaseUrl;
        }

        public static string GetDonateJsonUrl()
        {
            return $"{GetRemoteBasePath()}{DonateJsonBasePath}/config.json";
        }
        public static string GetPluginUrl()
        {
            return $"{GetRemoteBasePath()}{PluginBasePath}/";
        }

        static UrlManager()
        {
            Type t = typeof(UrlManager);
            JArray array = FileHelper.TryReadConfigFromJson("UpgradeSources") as JArray;
            if (array == null || array.Count == 0) return;
            foreach (JObject obj in array)
            {
                if (obj == null) continue;
                Dictionary<string, string> dict = obj.ToObject<Dictionary<string, string>>();
                if (dict == null) continue;
                if (dict.ContainsKey("Name") && !string.IsNullOrEmpty(dict["Name"]))
                {
                    string value = dict["Name"];

                    PluginBasePath = dict.Get("PluginPath", PluginBasePath);
                    FeedbackUrl = dict.Get("FeedbackUrl", FeedbackUrl);
                    HelpUrl = dict.Get("HelpUrl", HelpUrl);
                    string UpgradeSource = dict.Get("UpgradeSource", "");
                    string UpdatePath = dict.Get("UpdatePath", "");
                    string ReleaseUrl = dict.Get("ReleaseUrl", "");
                    if (UpgradeSourceDict.ContainsKey(value))
                    {
                        UpgradeSource source = UpgradeSourceDict[value];
                        if (!string.IsNullOrEmpty(UpgradeSource))
                            source.BaseUrl = UpgradeSource;
                        if (!string.IsNullOrEmpty(UpdatePath))
                            source.RemotePath = UpdatePath;
                        if (!string.IsNullOrEmpty(ReleaseUrl))
                            source.ReleaseUrl = ReleaseUrl;
                        UpgradeSourceDict[value] = source;
                    }
                    else
                    {
                        UpgradeSource source = new UpgradeSource(UpgradeSource, ReleaseUrl, UpdatePath);
                        UpgradeSourceDict.Add(value, source);
                    }

                }


            }


        }

    }
}
