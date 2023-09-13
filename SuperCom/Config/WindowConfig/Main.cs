using SuperUtils.Framework.ORM.Config;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace SuperCom.Config.WindowConfig
{
    public class Main : AbstractConfig, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] string name = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private Main() : base(ConfigManager.SQLITE_DATA_PATH, $"WindowConfig.Main")
        {
            Width = SystemParameters.WorkArea.Width * 0.8;
            Height = SystemParameters.WorkArea.Height * 0.8;
            SideGridWidth = 200;
            FirstRun = true;
            SendHistory = "";
            UseDefaultBaudRate = true;
        }

        private static Main _instance = null;

        public static Main CreateInstance()
        {
            if (_instance == null)
                _instance = new Main();

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
        public string OpeningPorts { get; set; }

        public string CustomBaudRates { get; set; }
        public string LatestNotice { get; set; }
        public bool UseDefaultBaudRate { get; set; }
        public long CommandsSelectIndex { get; set; }

        private bool _ShowRightPanel = true;
        public bool ShowRightPanel {
            get { return _ShowRightPanel; }
            set {
                _ShowRightPanel = value;
                RaisePropertyChanged();
            }
        }


        #region "AvalonText 相关配置"


        private bool _AutoWrap = false;
        public bool AutoWrap {
            get { return _AutoWrap; }
            set {
                _AutoWrap = value;
                RaisePropertyChanged();
            }
        }
        private bool _ShowEndOfLine = false;
        public bool ShowEndOfLine {
            get { return _ShowEndOfLine; }
            set {
                _ShowEndOfLine = value;
                RaisePropertyChanged();
            }
        }

        private bool _ShowSpaces = false;
        public bool ShowSpaces {
            get { return _ShowSpaces; }
            set {
                _ShowSpaces = value;
                RaisePropertyChanged();
            }
        }


        private bool _ShowTabs = false;
        public bool ShowTabs {
            get { return _ShowTabs; }
            set {
                _ShowTabs = value;
                RaisePropertyChanged();
            }
        }


        private bool _HighlightCurrentLine = true;
        public bool HighlightCurrentLine {
            get { return _HighlightCurrentLine; }
            set {
                _HighlightCurrentLine = value;
                RaisePropertyChanged();
            }
        }


        private bool _ShowLineNumbers = true;
        public bool ShowLineNumbers {
            get { return _ShowLineNumbers; }
            set {
                _ShowLineNumbers = value;
                RaisePropertyChanged();
            }
        }


        private string _TextFontName = "微软雅黑";
        public string TextFontName {
            get { return _TextFontName; }
            set {
                _TextFontName = value;
                RaisePropertyChanged();
            }
        }
        private string _TextForeground = "";
        public string TextForeground {
            get { return _TextForeground; }
            set {
                _TextForeground = value;
                RaisePropertyChanged();
            }
        }

        #endregion

    }
}
