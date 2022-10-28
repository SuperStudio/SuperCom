using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperCom.Config
{
    public static class UrlManager
    {

        public static readonly string ReleaseUrl = "https://github.com/SuperStudio/SuperCom/releases";
        public static readonly string UpgradeSource = "https://superstudio.github.io";
        public static readonly string UpdateUrl = "https://superstudio.github.io/SuperCom-Upgrade/latest.json";
        public const string UpdateFileListUrl = "https://superstudio.github.io/SuperCom-Upgrade/list.json";
        public static string UpdateFilePathUrl = "https://superstudio.github.io/SuperCom-Upgrade/File/";
    }
}
