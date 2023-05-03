
using SuperCom.Config;
using SuperControls.Style.Upgrade;
using SuperUtils.NetWork;
using SuperUtils.NetWork.Crawler;
using SuperUtils.WPF.VieModel;
using SuperUtils.WPF.VisualTools;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace SuperCom.Upgrade
{
    public static class UpgradeHelper
    {
        public static int AUTO_CHECK_UPGRADE_DELAY = 60 * 1000;

        private const string DEFAULT_LANG = "zh-CN";
        private const string DEFAULT_UPDATEFILEDIR = "TEMP";
        private const string DEFAULT_APP_NAME = "SuperCom.exe";
        private const int DEFAULT_BEFORE_UPDATE_DELAY = 5;
        private const int DEFAULT_AFTER_UPDATE_DELAY = 1;
        public static Action OnBeforeCopyFile { get; set; }
        private static bool WindowClosed { get; set; }

        private static SuperUpgrader Upgrader { get; set; }
        private static Dialog_Upgrade Dialog_Upgrade { get; set; }

        public static void Init()
        {
            Upgrader = new SuperUpgrader();
            Upgrader.UpgradeSourceDict = UrlManager.UpgradeSourceDict;
            Upgrader.UpgradeSourceIndex = UrlManager.GetRemoteIndex();
            Upgrader.Language = DEFAULT_LANG;
            Upgrader.Header = new CrawlerHeader(SuperWebProxy.SystemWebProxy).Default;
            Upgrader.Logger = null; // todo
            Upgrader.BeforeUpdateDelay = DEFAULT_BEFORE_UPDATE_DELAY;
            Upgrader.AfterUpdateDelay = DEFAULT_AFTER_UPDATE_DELAY;
            Upgrader.UpDateFileDir = DEFAULT_UPDATEFILEDIR;
            Upgrader.AppName = DEFAULT_APP_NAME;
            CreateDialog_Upgrade();
        }

        public static void CreateDialog_Upgrade()
        {
            Dialog_Upgrade = new Dialog_Upgrade(Upgrader);
            Dialog_Upgrade.LocalVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Dialog_Upgrade.OnSourceChanged += (s, e) =>
            {
                // 保存当前选择的地址
                int index = e.NewValue;
                UrlManager.SetRemoteIndex(index);
                ConfigManager.Settings.RemoteIndex = index;
                ConfigManager.Settings.Save();
            };
            Dialog_Upgrade.Closed += (s, e) =>
            {
                WindowClosed = true;
            };
            Dialog_Upgrade.OnExitApp += () =>
            {
                OnBeforeCopyFile?.Invoke();
            };
            WindowClosed = false;
        }



        public static void OpenWindow(Window window = null)
        {
            if (WindowClosed)
                CreateDialog_Upgrade();
            Dialog_Upgrade?.ShowDialog(window);

        }

        public static async Task<(string LatestVersion, string ReleaseDate, string ReleaseNote)> GetUpgardeInfo()
        {
            if (Upgrader == null) return (null, null, null);
            return await Upgrader.GetUpgardeInfo();
        }


    }
}
