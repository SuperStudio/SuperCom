
using SuperCom.Config;
using SuperControls.Style.Upgrade;
using SuperUtils.NetWork;
using SuperUtils.NetWork.Crawler;
using SuperUtils.WPF.VieModel;
using System.Windows;

namespace SuperCom.Commands
{
    public class OpenWindow
    {
        public static RelayCommand<Window> Upgrade { get; set; }


        static OpenWindow()
        {
            Upgrader = new SuperUpgrader();
            Upgrader.InfoUrl = UrlManager.UpdateUrl;
            Upgrader.FileListUrl = UrlManager.UpdateFileListUrl;
            Upgrader.FilePathUrl = UrlManager.UpdateFilePathUrl;
            Upgrader.ReleaseUrl = UrlManager.ReleaseUrl;
            Upgrader.UpgradeSource = UrlManager.UpgradeSource;
            Upgrader.Language = "zh-CN";
            Upgrader.Header = new CrawlerHeader(SuperWebProxy.SystemWebProxy).Default;
            Upgrader.Logger = null;//todo
            Upgrader.BeforeUpdateDelay = 5;
            Upgrader.AfterUpdateDelay = 1;
            Upgrader.UpDateFileDir = "TEMP";
            Upgrader.AppName = "SuperCom.exe";

            Upgrade = new RelayCommand<Window>(parent =>
            {

                Dialog_Upgrade dialog_Upgrade = new Dialog_Upgrade(parent, Upgrader);
                dialog_Upgrade.LocalVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                dialog_Upgrade.ShowDialog();
            });
        }

        public static SuperUpgrader Upgrader { get; set; }


    }
}
