using ICSharpCode.AvalonEdit.Highlighting;
using ITLDG.DataCheck;
using SuperCom.Comparers;
using SuperCom.Config;
using SuperCom.Core.Settings;
using SuperCom.Entity;
using SuperCom.Entity.Enums;
using SuperControls.Style.UserControls.TabControlPro;
using SuperUtils.Common;
using SuperUtils.WPF.VieModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using static SuperCom.App;

namespace SuperCom.ViewModel
{

    public class VieModel_Main : ViewModelBase
    {
        #region "常量"

        private const string DEFAULT_STATUS_TEXT = "就绪";
        #endregion



        static VieModel_Main()
        {

        }

        public VieModel_Main()
        {
            Init();
        }

        #region "属性"

        public Action<List<ComConnector>> OnBaudRatesChanged { get; set; }

        public HashSet<string> SendHistory { get; set; }
        public List<ShortCutBinding> ShortCutBindings { get; set; }

        private ObservableCollection<Plugin> _DataCheckPlugins;
        public ObservableCollection<Plugin> DataCheckPlugins {
            get { return _DataCheckPlugins; }
            set { _DataCheckPlugins = value; RaisePropertyChanged(); }
        }



        private ObservableCollection<string> _DataBits;
        public ObservableCollection<string> DataBits {
            get { return _DataBits; }
            set { _DataBits = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<string> _Encodings;
        public ObservableCollection<string> Encodings {
            get { return _Encodings; }
            set { _Encodings = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<string> _Parities;
        public ObservableCollection<string> Parities {
            get { return _Parities; }
            set { _Parities = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<string> _HandShakes;
        public ObservableCollection<string> HandShakes {
            get { return _HandShakes; }
            set { _HandShakes = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<string> _StopBits;
        public ObservableCollection<string> StopBits {
            get { return _StopBits; }
            set { _StopBits = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<ComConnector> _PortTabItems;
        public ObservableCollection<ComConnector> PortTabItems {
            get { return _PortTabItems; }
            set { _PortTabItems = value; RaisePropertyChanged(); }
        }

        private string _StatusText = DEFAULT_STATUS_TEXT;
        public string StatusText {
            get { return _StatusText; }
            set { _StatusText = value; RaisePropertyChanged(); }
        }

        private double _SideGridWidth = ConfigManager.Main.SideGridWidth;

        public double SideGridWidth {
            get { return _SideGridWidth; }
            set {
                _SideGridWidth = value;
                RaisePropertyChanged();
            }
        }

        private int _SendHistorySelectedIndex;

        public int SendHistorySelectedIndex {
            get { return _SendHistorySelectedIndex; }
            set {
                _SendHistorySelectedIndex = value;
                RaisePropertyChanged();
            }
        }




        private string _TextFontName = ConfigManager.Main.TextFontName;

        public string TextFontName {
            get { return _TextFontName; }
            set {
                _TextFontName = value;
                RaisePropertyChanged();
            }
        }




        private bool _ShowDonate;
        public bool ShowDonate {
            get { return _ShowDonate; }
            set { _ShowDonate = value; RaisePropertyChanged(); }
        }

        private bool _DoingLongWork;
        public bool DoingLongWork {
            get { return _DoingLongWork; }
            set { _DoingLongWork = value; RaisePropertyChanged(); }
        }

        private string _DoingWorkMsg;
        public string DoingWorkMsg {
            get { return _DoingWorkMsg; }
            set { _DoingWorkMsg = value; RaisePropertyChanged(); }
        }

        private double _MemoryUsed;
        public double MemoryUsed {
            get { return _MemoryUsed; }
            set { _MemoryUsed = value; RaisePropertyChanged(); }
        }

        private bool _ShowSoft = true;
        public bool ShowSoft {
            get { return _ShowSoft; }
            set { _ShowSoft = value; RaisePropertyChanged(); }
        }


        #endregion



        public override void Init()
        {
            PortTabItems = new ObservableCollection<ComConnector>();
            PortTabItems.CollectionChanged += OnPortTabItemsCollectionChanged;

            InitSendHistory();
            GlobalSettings.Init();
            LoadDataCheck();
            LoadDataBits();
            LoadEncodings();
            LoadParities();
            LoadStopBits();
            LoadShortCut();
            LoadHandshake();
            LoadHighLightRule();
        }

        private void InitSendHistory()
        {
            if (!string.IsNullOrEmpty(ConfigManager.Main.SendHistory)) {
                SendHistory = JsonUtils.TryDeserializeObject<HashSet<string>>(ConfigManager.Main.SendHistory);
            }
            if (SendHistory == null)
                SendHistory = new HashSet<string>();

        }

        private void OnPortTabItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (PortTabItems != null && PortTabItems.Count > 0)
                ShowSoft = false;
            else
                ShowSoft = true;
        }

        public void LoadStopBits()
        {
            StopBits = new ObservableCollection<string>();
            foreach (var item in PortSetting.DEFAULT_STOPBIT_LIST) {
                StopBits.Add(item.ToString());
            }
        }
        public void LoadParities()
        {
            Parities = new ObservableCollection<string>();
            foreach (var item in PortSetting.DEFAULT_PARITIES) {
                Parities.Add(item.ToString());
            }
        }
        public void LoadEncodings()
        {
            Encodings = new ObservableCollection<string>();
            foreach (var item in PortSetting.DEFAULT_ENCODINGS) {
                Encodings.Add(item.ToString());
            }
        }
        public void LoadDataBits()
        {
            DataBits = new ObservableCollection<string>();
            foreach (var item in PortSetting.DEFAULT_DATABITS_LIST) {
                DataBits.Add(item.ToString());
            }
        }
        public void LoadHandshake()
        {
            HandShakes = new ObservableCollection<string>();
            foreach (var item in PortSetting.DEFAULT_HANDSHAKES) {
                HandShakes.Add(item.ToString());
            }
        }
        public void LoadDataCheck()
        {
            DataCheckPlugins = new ObservableCollection<Plugin>();
            List<Plugin> baudrates = Plugin.GePlugins();
            foreach (var item in baudrates) {
                DataCheckPlugins.Add(item);
            }
        }

        public void LoadShortCut()
        {
            ShortCutBindings = new List<ShortCutBinding>();
            List<ShortCutBinding> shortCutBindings = MapperManager.ShortCutMapper.SelectList();
            foreach (var item in shortCutBindings) {
                item.RefreshKeyList();
                ShortCutBindings.Add(item);
            }

        }
        public void LoadHighLightRule()
        {
            HighLightRule.AllRules = MapperManager.RuleMapper.SelectList();
        }
    }
}