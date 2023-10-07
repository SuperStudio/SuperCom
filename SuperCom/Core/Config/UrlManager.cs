using Newtonsoft.Json.Linq;
using SuperControls.Style;
using SuperControls.Style.Upgrade;
using SuperUtils.Common;
using SuperUtils.IO;
using System.Collections.Generic;
using System.Linq;

namespace SuperCom.Config
{
    public static class UrlManager
    {
        public static Dictionary<string, UpgradeSource> UpgradeSourceDict = new Dictionary<string, UpgradeSource>()
        {
            {"Github",new UpgradeSource("https://superstudio.github.io/","https://github.com/SuperStudio/SuperCom/releases","SuperCom-Upgrade") },
            {"StormGit Proxy",new UpgradeSource("https://venomplum-54uhej.stormkit.dev/","https://github.com/SuperStudio/SuperCom/releases","") },
            {"Github Proxy",new UpgradeSource("https://cdn.jsdelivr.net/gh/SuperStudio/","https://gitee.com/SuperStudio/SuperCom/releases","SuperCom-Upgrade") },
        };

        public static List<string> UpgradeSourceKeys = UpgradeSourceDict.Keys.ToList();

        private static int RemoteIndex = (int)ConfigManager.Settings.RemoteIndex; // 用户切换源的时候存储起来

        private static string DonateJsonBasePath = "SuperStudio-Donate";
        private static string PluginBasePath = "SuperPlugins";

        public static string FeedbackUrl = "https://github.com/SuperStudio/SuperCom/issues";
        public static string HelpUrl = "https://github.com/SuperStudio/SuperCom/wiki";
        public const string NOTICE_URL = "https://superstudio.github.io/SuperCom-Upgrade/notice.json";


        #region ABOUT
        public static string GITHUB_URL { get; set; }
        public static string WEB_URL { get; set; }
        public static string JOIN_GROUP_URL { get; set; }
        public static string AUTHOR { get; set; } = "chao, itldg 老大哥";
        public static string LICENSE { get; set; } = "GPL-3.0";
        #endregion


        public static List<ReferenceData> REFERENCE_DATAS = new List<ReferenceData>()
        {
            new ReferenceData("Serial and UART Tutorial",
                "https://docs.freebsd.org/en/articles/serial-uart/","FreeBSD","1996/01/13","-"),
            new ReferenceData("SerialPort Class",
                "https://learn.microsoft.com/en-us/dotnet/api/system.io.ports.serialport?view=netframework-4.7.2",
                "Microsoft","-","-"),
        };

        #region WIKI
        public const string WIKI_SETTING = "https://github.com/SuperStudio/SuperCom/wiki/03_Settings";
        public const string WIKI_HIGH_LIGHT = "https://github.com/SuperStudio/SuperCom/wiki/05_HighLight";
        public const string WIKI_FAQ = "https://github.com/SuperStudio/SuperCom/wiki/06_FAQ";
        public const string WIKI_DEVELOP = "https://github.com/SuperStudio/SuperCom/wiki/20_Developer";

        #endregion

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
            GITHUB_URL = FileHelper.TryReadConfigFromJson("GitHubUrl") as string;
            WEB_URL = FileHelper.TryReadConfigFromJson("WebUrl") as string;
            JOIN_GROUP_URL = FileHelper.TryReadConfigFromJson("JoinGroupUrl") as string;
            if (FileHelper.TryReadConfigFromJson("Author") is string author && !string.IsNullOrEmpty(author))
                AUTHOR = author;

            if (FileHelper.TryReadConfigFromJson("License") is string license && !string.IsNullOrEmpty(license))
                LICENSE = license;

            JArray array = FileHelper.TryReadConfigFromJson("UpgradeSources") as JArray;
            if (array == null || array.Count == 0)
                return;
            foreach (JObject obj in array) {
                if (obj == null)
                    continue;
                Dictionary<string, string> dict = obj.ToObject<Dictionary<string, string>>();
                if (dict == null)
                    continue;
                if (dict.ContainsKey("Name") && !string.IsNullOrEmpty(dict["Name"])) {
                    string value = dict["Name"];

                    PluginBasePath = dict.Get("PluginPath", PluginBasePath);
                    FeedbackUrl = dict.Get("FeedbackUrl", FeedbackUrl);
                    HelpUrl = dict.Get("HelpUrl", HelpUrl);
                    string UpgradeSource = dict.Get("UpgradeSource", "");
                    string UpdatePath = dict.Get("UpdatePath", "");
                    string ReleaseUrl = dict.Get("ReleaseUrl", "");
                    if (UpgradeSourceDict.ContainsKey(value)) {
                        UpgradeSource source = UpgradeSourceDict[value];
                        if (!string.IsNullOrEmpty(UpgradeSource))
                            source.BaseUrl = UpgradeSource;
                        if (!string.IsNullOrEmpty(UpdatePath))
                            source.RemotePath = UpdatePath;
                        if (!string.IsNullOrEmpty(ReleaseUrl))
                            source.ReleaseUrl = ReleaseUrl;
                        UpgradeSourceDict[value] = source;
                    } else {
                        UpgradeSource source = new UpgradeSource(UpgradeSource, ReleaseUrl, UpdatePath);
                        UpgradeSourceDict.Add(value, source);
                    }

                }


            }


        }

    }
}
