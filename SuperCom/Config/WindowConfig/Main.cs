using SuperUtils.Framework.ORM.Config;
using System.Windows;

namespace SuperCom.Config.WindowConfig
{
    public class Main : AbstractConfig
    {
        private Main() : base("user_data.sqlite", $"WindowConfig.Main")
        {
            Width = SystemParameters.WorkArea.Width * 0.8;
            Height = SystemParameters.WorkArea.Height * 0.8;
            SideGridWidth = 200;
            FirstRun = true;
            SendHistory = "";
        }

        private static Main _instance = null;

        public static Main CreateInstance()
        {
            if (_instance == null) _instance = new Main();

            return _instance;
        }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public long WindowState { get; set; }

        public double SideGridWidth { get; set; }

        public bool FirstRun { get; set; }
        public string SendHistory { get; set; }

    }
}
