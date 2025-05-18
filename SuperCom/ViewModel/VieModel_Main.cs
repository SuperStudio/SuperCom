using ICSharpCode.AvalonEdit.Highlighting;
using ITLDG.DataCheck;
using SuperCom.Comparers;
using SuperCom.Config;
using SuperCom.Entity;
using SuperCom.Entity.Enums;
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
        public const string DEFAULT_ADD_TEXT = "新增";

        private const string DEFAULT_STATUS_TEXT = "就绪";
        private const int MAX_SEND_HISTORY_COUNT = 100;
        #endregion



        static VieModel_Main()
        {

        }

        public VieModel_Main()
        {
            Init();
        }

        #region "属性"

        public AdvancedSend CurrentAdvancedSend { get; set; }

        public Action<List<PortTabItem>> OnBaudRatesChanged { get; set; }

        private ObservableCollection<string> SendHistory { get; set; }

        public int CurrentSendHistoryIndex { get; set; }

        public ObservableCollection<string> CurrentSendHistory { get; set; } = new ObservableCollection<string>();

        public HashSet<ComSettings> ComSettingList { get; set; }

        public List<ShortCutBinding> ShortCutBindings { get; set; }


        private ObservableCollection<string> _BaudRates;
        public ObservableCollection<string> BaudRates {
            get { return _BaudRates; }
            set { _BaudRates = value; RaisePropertyChanged(); }
        }

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

        private ObservableCollection<PortTabItem> _PortTabItems;
        public ObservableCollection<PortTabItem> PortTabItems {
            get { return _PortTabItems; }
            set { _PortTabItems = value; RaisePropertyChanged(); }
        }


        private ObservableCollection<SideComPort> _SideComPorts;
        public ObservableCollection<SideComPort> SideComPorts {
            get { return _SideComPorts; }
            set { _SideComPorts = value; RaisePropertyChanged(); }
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

        private string _SendHistorySelectedValue = "";
        public string SendHistorySelectedValue {
            get { return _SendHistorySelectedValue; }
            set {
                _SendHistorySelectedValue = value;
                RaisePropertyChanged();
            }
        }

        private int _CommandsSelectIndex = (int)ConfigManager.Main.CommandsSelectIndex;

        public int CommandsSelectIndex {
            get { return _CommandsSelectIndex; }
            set {
                _CommandsSelectIndex = value;
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

        private ObservableCollection<AdvancedSend> _SendCommandProjects;
        public ObservableCollection<AdvancedSend> SendCommandProjects {
            get { return _SendCommandProjects; }
            set { _SendCommandProjects = value; RaisePropertyChanged(); }
        }


        private ObservableCollection<IHighlightingDefinition> _HighlightingDefinitions;
        public ObservableCollection<IHighlightingDefinition> HighlightingDefinitions {
            get { return _HighlightingDefinitions; }
            set { _HighlightingDefinitions = value; RaisePropertyChanged(); }
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
            PortTabItems = new ObservableCollection<PortTabItem>();
            PortTabItems.CollectionChanged += OnPortTabItemsCollectionChanged;

            InitPortData();
            InitSendHistory();
            LoadSendCommands();
            LoadBaudRates();
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
                SendHistory = JsonUtils.TryDeserializeObject<ObservableCollection<string>>(ConfigManager.Main.SendHistory);
            }
            if (SendHistory == null)
                SendHistory = new ObservableCollection<string>();

            SendHistory.CollectionChanged += (s, e) => {
                CurrentSendHistoryIndex = s as ObservableCollection<string> == null ? 0 : (s as ObservableCollection<string>).Count - 1;
            };
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

        public void LoadBaudRates()
        {
            List<PortTabItem> beforePortTabItems = new List<PortTabItem>();
            foreach (var item in PortTabItems) {
                PortTabItem portTabItem = new PortTabItem(item.Name, item.Connected);
                portTabItem.SerialPort = new SerialPortEx(item.Name, item.SerialPort.BaudRate, item.SerialPort.Parity, item.SerialPort.DataBits, item.SerialPort.StopBits);
                beforePortTabItems.Add(portTabItem);
            }


            BaudRates = new ObservableCollection<string>();
            List<string> baudrates = PortSetting.GetAllBaudRates();
            foreach (var item in baudrates) {
                BaudRates.Add(item);
            }
            string value = ConfigManager.Main.CustomBaudRates;
            if (!string.IsNullOrEmpty(value)) {
                List<string> list = JsonUtils.TryDeserializeObject<List<string>>(value);
                if (list?.Count > 0) {
                    foreach (var item in list)
                        BaudRates.Add(item);
                }
            }
            ConfigManager.Main.CustomBaudRates = JsonUtils.TrySerializeObject(BaudRates);
            ConfigManager.Main.Save();
            BaudRates.Add(DEFAULT_ADD_TEXT);
            OnBaudRatesChanged?.Invoke(beforePortTabItems);
        }

        public void LoadSendCommands()
        {
            SendCommandProjects = new ObservableCollection<AdvancedSend>();
            List<AdvancedSend> advancedSends = MapperManager.AdvancedSendMapper.SelectList();
            foreach (var item in advancedSends) {
                SendCommandProjects.Add(item);
            }

        }

        public void UpdateProject(AdvancedSend send)
        {
            int count = MapperManager.AdvancedSendMapper.UpdateById(send);
            if (count <= 0) {
                Logger.Error($"insert error: {send.ProjectName}");
            }
        }

        public void LoadHighlightingDefinitions()
        {
            HighlightingDefinitions = new ObservableCollection<IHighlightingDefinition>();
            HighLightRule.AllName = new List<string>();
            foreach (var item in HighlightingManager.Instance.HighlightingDefinitions) {
                HighlightingDefinitions.Add(item);
                HighLightRule.AllName.Add(item.Name);
            }

        }


        public void InitPortData(ComPortSortType sortType = ComPortSortType.AddTime, bool desc = false)
        {
            Dictionary<string, string> dict = SerialPortEx.GetAllPorts();
            string[] ports = dict.Keys.ToArray();
            List<string> portNames = new List<string>();
            switch (sortType) {
                case ComPortSortType.AddTime:
                    if (desc)
                        portNames = ports.Reverse().ToList();
                    else
                        portNames = ports.ToList();
                    break;
                case ComPortSortType.PortName:
                    if (desc)
                        portNames = ports.OrderByDescending(name => name, new ComPortComparer()).ToList();
                    else
                        portNames = ports.OrderBy(name => name, new ComPortComparer()).ToList();
                    break;
                default:
                    break;


            }

            SideComPorts = new ObservableCollection<SideComPort>();
            foreach (string port in portNames) {
                SideComPort sideComPort = new SideComPort(port, false);
                sideComPort.Detail = dict[port];
                SideComPorts.Add(sideComPort);
            }

        }

        public void SaveBaudRate()
        {
            List<string> baudrates = new List<string>();
            for (int i = 0; i < BaudRates.Count; i++) {
                if (!DEFAULT_ADD_TEXT.Equals(BaudRates[i])) {
                    baudrates.Add(BaudRates[i]);
                }
            }
            ConfigManager.Main.CustomBaudRates = JsonUtils.TrySerializeObject(baudrates);
            ConfigManager.Main.Save();
        }

        public void SaveBaudRate(string portName, string baudRate)
        {
            List<ComSettings> list = ComSettingList.ToList();
            for (int i = 0; i < list.Count; i++) {
                ComSettings comSettings = list[i];
                if (!comSettings.PortName.Equals(portName))
                    continue;
                PortTabItem portTabItem = PortTabItems.Where(arg => arg.Name.Equals(comSettings.PortName)).FirstOrDefault();
                int.TryParse(baudRate, out int value);
                if (value <= 0)
                    return;
                portTabItem.SerialPort.BaudRate = value;
                comSettings.PortSetting = portTabItem.SerialPort.PortSettingToJson();
                MapperManager.ComMapper.UpdateFieldById("PortSetting", comSettings.PortSetting, comSettings.Id);
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

        public void SaveToHistory(string value)
        {
            if (string.IsNullOrEmpty(value))
                return;
            string val = value.Trim();
            if (SendHistory.Count > MAX_SEND_HISTORY_COUNT)
                SendHistory.RemoveAt(0);
            SendHistory.Add(val);
            // 保存
            ConfigManager.Main.SendHistory = JsonUtils.TrySerializeObject(SendHistory.ToList());
            ConfigManager.Main.Save();
        }

        public string GetSelectSendHistory(bool up)
        {
            if (SendHistory == null || SendHistory.Count == 0)
                return string.Empty;
            if (up) {
                CurrentSendHistoryIndex--;
            } else {
                CurrentSendHistoryIndex++;
            }

            if (CurrentSendHistoryIndex >= SendHistory.Count)
                CurrentSendHistoryIndex = SendHistory.Count - 1;
            else if (CurrentSendHistoryIndex < 0)
                CurrentSendHistoryIndex = 0;
            return SendHistory[CurrentSendHistoryIndex];
        }
    }
}