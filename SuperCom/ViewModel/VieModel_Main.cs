using ICSharpCode.AvalonEdit.Highlighting;
using SuperCom.Comparers;
using SuperCom.Config;
using SuperCom.Entity;
using SuperControls.Style;
using SuperUtils.Common;
using SuperUtils.Framework.ORM.Mapper;
using SuperUtils.Framework.ORM.Wrapper;
using SuperUtils.WPF.VieModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System;

namespace SuperCom.ViewModel
{

    public class VieModel_Main : ViewModelBase
    {

        public Action<List<PortTabItem>> OnBaudRatesChanged;
        private static SqliteMapper<AdvancedSend> mapper { get; set; }
        private static SqliteMapper<ComSettings> comMapper { get; set; }
        private static SqliteMapper<ShortCutBinding> shortCutMapper { get; set; }
        private static SqliteMapper<HighLightRule> ruleMapper { get; set; }
        private static SqliteMapper<VarMonitor> monitorMapper { get; set; }
        public HashSet<string> SendHistory { get; set; }
        public HashSet<ComSettings> ComSettingList { get; set; }
        public List<ShortCutBinding> ShortCutBindings { get; set; }


        private ObservableCollection<string> _BaudRates;
        public ObservableCollection<string> BaudRates
        {
            get { return _BaudRates; }
            set { _BaudRates = value; RaisePropertyChanged(); }
        }



        private ObservableCollection<string> _DataBits;
        public ObservableCollection<string> DataBits
        {
            get { return _DataBits; }
            set { _DataBits = value; RaisePropertyChanged(); }
        }
        private ObservableCollection<string> _Encodings;
        public ObservableCollection<string> Encodings
        {
            get { return _Encodings; }
            set { _Encodings = value; RaisePropertyChanged(); }
        }
        private ObservableCollection<string> _Paritys;
        public ObservableCollection<string> Paritys
        {
            get { return _Paritys; }
            set { _Paritys = value; RaisePropertyChanged(); }
        }
        private ObservableCollection<string> _StopBits;
        public ObservableCollection<string> StopBits
        {
            get { return _StopBits; }
            set { _StopBits = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<PortTabItem> _PortTabItems;
        public ObservableCollection<PortTabItem> PortTabItems
        {
            get { return _PortTabItems; }
            set { _PortTabItems = value; RaisePropertyChanged(); }
        }


        private ObservableCollection<SideComPort> _SideComPorts;
        public ObservableCollection<SideComPort> SideComPorts
        {
            get { return _SideComPorts; }
            set { _SideComPorts = value; RaisePropertyChanged(); }
        }
        private string _StatusText = "就绪";
        public string StatusText
        {
            get { return _StatusText; }
            set { _StatusText = value; RaisePropertyChanged(); }
        }
        private double _SideGridWidth = ConfigManager.Main.SideGridWidth;

        public double SideGridWidth
        {
            get { return _SideGridWidth; }
            set
            {
                _SideGridWidth = value;
                RaisePropertyChanged();
            }
        }



        private int _SendHistorySelectedIndex = 0;

        public int SendHistorySelectedIndex
        {
            get { return _SendHistorySelectedIndex; }
            set
            {
                _SendHistorySelectedIndex = value;
                RaisePropertyChanged();
            }
        }
        private string _SendHistorySelectedValue = "";

        public string SendHistorySelectedValue
        {
            get { return _SendHistorySelectedValue; }
            set
            {
                _SendHistorySelectedValue = value;
                RaisePropertyChanged();
            }
        }
        private int _CommandsSelectIndex = (int)ConfigManager.Main.CommandsSelectIndex;

        public int CommandsSelectIndex
        {
            get { return _CommandsSelectIndex; }
            set
            {
                _CommandsSelectIndex = value;
                RaisePropertyChanged();
            }
        }



        private string _TextFontName = ConfigManager.Main.TextFontName;

        public string TextFontName
        {
            get { return _TextFontName; }
            set
            {
                _TextFontName = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<AdvancedSend> _SendCommandProjects;
        public ObservableCollection<AdvancedSend> SendCommandProjects
        {
            get { return _SendCommandProjects; }
            set { _SendCommandProjects = value; RaisePropertyChanged(); }
        }



        private ObservableCollection<IHighlightingDefinition> _HighlightingDefinitions;
        public ObservableCollection<IHighlightingDefinition> HighlightingDefinitions
        {
            get { return _HighlightingDefinitions; }
            set { _HighlightingDefinitions = value; RaisePropertyChanged(); }
        }

        private bool _ShowDonate;
        public bool ShowDonate
        {
            get { return _ShowDonate; }
            set { _ShowDonate = value; RaisePropertyChanged(); }
        }
        private bool _DoingLongWork;
        public bool DoingLongWork
        {
            get { return _DoingLongWork; }
            set { _DoingLongWork = value; RaisePropertyChanged(); }
        }
        private string _DoingWorkMsg;
        public string DoingWorkMsg
        {
            get { return _DoingWorkMsg; }
            set { _DoingWorkMsg = value; RaisePropertyChanged(); }
        }
        private double _MemoryUsed;
        public double MemoryUsed
        {
            get { return _MemoryUsed; }
            set { _MemoryUsed = value; RaisePropertyChanged(); }
        }
        private bool _ShowSoft = true;
        public bool ShowSoft
        {
            get { return _ShowSoft; }
            set { _ShowSoft = value; RaisePropertyChanged(); }
        }

        public AdvancedSend CurrentAdvancedSend { get; set; }


        static VieModel_Main()
        {

        }

        public VieModel_Main()
        {
            Init();
        }

        public void Init()
        {
            PortTabItems = new ObservableCollection<PortTabItem>();
            PortTabItems.CollectionChanged += (s, e) =>
            {
                if (PortTabItems != null && PortTabItems.Count > 0)
                    ShowSoft = false;
                else
                    ShowSoft = true;
            };
            InitPortData();
            if (!string.IsNullOrEmpty(ConfigManager.Main.SendHistory))
            {
                SendHistory = JsonUtils.TryDeserializeObject<HashSet<string>>(ConfigManager.Main.SendHistory);
            }
            if (SendHistory == null) SendHistory = new HashSet<string>();

            LoadSendCommands();

            LoadBaudRates();
            LoadDataBits();
            LoadEncodings();
            LoadParitys();
            LoadStopBits();
            LoadShortCut();
            LoadShortCut();
            LoadHighLightRule();
            comMapper = new SqliteMapper<ComSettings>(ConfigManager.SQLITE_DATA_PATH);


        }

        public void LoadStopBits()
        {
            StopBits = new ObservableCollection<string>();
            foreach (var item in PortSetting.DEFAULT_STOPBIT_LIST)
            {
                StopBits.Add(item.ToString());
            }
        }
        public void LoadParitys()
        {
            Paritys = new ObservableCollection<string>();
            foreach (var item in PortSetting.DEFAULT_PARITYS)
            {
                Paritys.Add(item.ToString());
            }
        }
        public void LoadEncodings()
        {
            Encodings = new ObservableCollection<string>();
            foreach (var item in PortSetting.DEFAULT_ENCODINGS)
            {
                Encodings.Add(item.ToString());
            }
        }
        public void LoadDataBits()
        {
            DataBits = new ObservableCollection<string>();
            foreach (var item in PortSetting.DEFAULT_DATABITS_LIST)
            {
                DataBits.Add(item.ToString());
            }
        }
        public void LoadBaudRates()
        {
            List<PortTabItem> beforePortTabItems = new List<PortTabItem>();
            foreach (var item in PortTabItems)
            {
                PortTabItem portTabItem = new PortTabItem(item.Name, item.Connected);
                portTabItem.SerialPort = new CustomSerialPort(item.Name, item.SerialPort.BaudRate, item.SerialPort.Parity, item.SerialPort.DataBits, item.SerialPort.StopBits);
                beforePortTabItems.Add(portTabItem);
            }


            BaudRates = new ObservableCollection<string>();
            List<string> baudrates = PortSetting.GetAllBaudRates();
            foreach (var item in baudrates)
            {
                BaudRates.Add(item);
            }
            string value = ConfigManager.Main.CustomBaudRates;
            if (!string.IsNullOrEmpty(value))
            {
                List<string> list = JsonUtils.TryDeserializeObject<List<string>>(value);
                if (list?.Count > 0)
                {
                    foreach (var item in list)
                        BaudRates.Add(item);
                }
            }
            ConfigManager.Main.CustomBaudRates = JsonUtils.TrySerializeObject(BaudRates);
            ConfigManager.Main.Save();
            BaudRates.Add("新增");
            OnBaudRatesChanged?.Invoke(beforePortTabItems);
        }

        public void LoadSendCommands()
        {
            SendCommandProjects = new ObservableCollection<AdvancedSend>();
            if (mapper == null) mapper = new SqliteMapper<AdvancedSend>(ConfigManager.SQLITE_DATA_PATH);
            List<AdvancedSend> advancedSends = mapper.SelectList();
            foreach (var item in advancedSends)
            {
                SendCommandProjects.Add(item);
            }

        }

        public void UpdateProject(AdvancedSend send)
        {
            int count = mapper.UpdateById(send);
            if (count <= 0)
            {
                Console.WriteLine($"插入 {send.ProjectName} 失败");
            }
        }

        public void LoadHighlightingDefinitions()
        {
            HighlightingDefinitions = new ObservableCollection<IHighlightingDefinition>();
            HighLightRule.AllName = new List<string>();
            foreach (var item in HighlightingManager.Instance.HighlightingDefinitions)
            {
                HighlightingDefinitions.Add(item);
                HighLightRule.AllName.Add(item.Name);
            }

        }




        public void SaveSendHistory()
        {
            //ConfigManager.Main.SendHistory = JsonUtils.TrySerializeObject(SendHistory);
            //ConfigManager.Main.Save();
        }

        public void InitPortData(ComPortSortType sortType = ComPortSortType.AddTime, bool desc = true)
        {
            string[] ports = CustomSerialPort.GetAllPorts();

            List<string> portNames = new List<string>();
            switch (sortType)
            {
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
            foreach (string port in portNames)
            {
                SideComPorts.Add(new SideComPort(port, false));
            }

        }

        public void SaveBaudRate()
        {
            List<string> baudrates = new List<string>();
            for (int i = 0; i < BaudRates.Count; i++)
            {
                if (!"新增".Equals(BaudRates[i]))
                {
                    baudrates.Add(BaudRates[i]);
                }
            }
            ConfigManager.Main.CustomBaudRates = JsonUtils.TrySerializeObject(baudrates);
            ConfigManager.Main.Save();
        }

        public void SaveBaudRate(string portName, string baudRate)
        {
            List<ComSettings> list = ComSettingList.ToList();
            for (int i = 0; i < list.Count; i++)
            {
                ComSettings comSettings = list[i];
                if (!comSettings.PortName.Equals(portName)) continue;
                PortTabItem portTabItem = PortTabItems.Where(arg => arg.Name.Equals(comSettings.PortName)).FirstOrDefault();
                int.TryParse(baudRate, out int value);
                if (value <= 0) return;
                portTabItem.SerialPort.BaudRate = value;
                comSettings.PortSetting = portTabItem.SerialPort.PortSettingToJson();
                comMapper.UpdateFieldById("PortSetting", comSettings.PortSetting, comSettings.Id);
            }
        }

        public void LoadShortCut()
        {
            if (shortCutMapper == null)
                shortCutMapper = new SqliteMapper<ShortCutBinding>(ConfigManager.SQLITE_DATA_PATH);
            ShortCutBindings = new List<ShortCutBinding>();
            List<ShortCutBinding> shortCutBindings = shortCutMapper.SelectList();
            foreach (var item in shortCutBindings)
            {
                item.RefreshKeyList();
                ShortCutBindings.Add(item);
            }

        }
        public void LoadHighLightRule()
        {
            if (ruleMapper == null)
                ruleMapper = new SqliteMapper<HighLightRule>(ConfigManager.SQLITE_DATA_PATH);
            HighLightRule.AllRules = ruleMapper.SelectList();
        }


        #region "变量监视器"
        public List<VarMonitor> GetVarMonitorByPortName(string portName)
        {
            if (monitorMapper == null)
                monitorMapper = new SqliteMapper<VarMonitor>(ConfigManager.SQLITE_DATA_PATH);
            SelectWrapper<VarMonitor> wrapper = new SelectWrapper<VarMonitor>();
            wrapper.Eq("PortName", portName);
            List<Dictionary<string, object>> data = monitorMapper.Select(wrapper);
            List<VarMonitor> list = monitorMapper.ToEntity<VarMonitor>(data, typeof(VarMonitor).GetProperties());
            return list.OrderBy(arg => arg.SortOrder).ToList();
        }

        public void NewVarMonitor(PortTabItem portTabItem, string portName)
        {
            if (portTabItem.VarMonitors == null)
                portTabItem.VarMonitors = new ObservableCollection<VarMonitor>();
            int maxOrder = 0;
            if (portTabItem.VarMonitors.Count > 0)
                maxOrder = portTabItem.VarMonitors.Max(arg => arg.SortOrder);
            if (maxOrder <= 0)
                maxOrder = 1;
            else
                maxOrder++;
            VarMonitor varMonitor = new VarMonitor(maxOrder, portName);
            monitorMapper.InsertAndGetID(varMonitor);
            portTabItem.VarMonitors.Add(varMonitor);
        }
        public void DeleteVarMonitor(PortTabItem portTabItem, long id)
        {
            if (portTabItem.VarMonitors == null || portTabItem.VarMonitors.Count == 0 || id <= 0)
                return;
            monitorMapper.DeleteById(id);

            int idx = -1;

            for (int i = 0; i < portTabItem.VarMonitors.Count; i++)
            {
                if (portTabItem.VarMonitors[i].MonitorID == id)
                {
                    idx = i;
                    break;
                }
            }
            if (idx >= 0 && idx < portTabItem.VarMonitors.Count)
                portTabItem.VarMonitors.RemoveAt(idx);
        }

        public void SaveMonitor(PortTabItem portTabItem)
        {
            if (portTabItem.VarMonitors == null || portTabItem.VarMonitors.Count == 0)
                return;

            List<VarMonitor> toUpdate = new List<VarMonitor>();

            List<VarMonitor> allData = monitorMapper.SelectList();
            foreach (var item in portTabItem.VarMonitors)
            {
                VarMonitor varMonitor = allData.FirstOrDefault(arg => arg.MonitorID == item.MonitorID);
                if (varMonitor == null || !varMonitor.Equals(item))
                    toUpdate.Add(item);
            }


            if (toUpdate.Count == 0)
            {
                MessageNotify.Info("无改变，无需保存");
                return;
            }
            else
            {
                foreach (var item in toUpdate)
                    monitorMapper.UpdateById(item);

                MessageNotify.Success("保存成功");
                portTabItem.VarMonitors = new ObservableCollection<VarMonitor>();

                foreach (var item in GetVarMonitorByPortName(portTabItem.Name))
                    portTabItem.VarMonitors.Add(item);

            }

        }

        #endregion
    }
}