using GalaSoft.MvvmLight;
using SuperCom.Comparers;
using SuperCom.Config;
using SuperCom.Entity;
using SuperUtils.Common;
using SuperUtils.Framework.ORM.Mapper;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;

namespace SuperCom.ViewModel
{

    public class VieModel_Main : ViewModelBase
    {




        private static SqliteMapper<AdvancedSend> mapper { get; set; }
        private static SqliteMapper<ComSettings> comMapper { get; set; }
        public HashSet<string> SendHistory { get; set; }
        public HashSet<ComSettings> ComSettingList { get; set; }


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



        /* 已经打开的选项卡 */
        private ObservableCollection<PortTabItem> _PortTabItems;
        public ObservableCollection<PortTabItem> PortTabItems
        {
            get { return _PortTabItems; }
            set { _PortTabItems = value; RaisePropertyChanged(); }
        }

        /* 侧边栏串口选项 */
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

        private ObservableCollection<AdvancedSend> _SendCommandProjects;
        public ObservableCollection<AdvancedSend> SendCommandProjects
        {
            get { return _SendCommandProjects; }
            set { _SendCommandProjects = value; RaisePropertyChanged(); }
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

        public void SaveSendHistory()
        {
            ConfigManager.Main.SendHistory = JsonUtils.TrySerializeObject(SendHistory);
            ConfigManager.Main.Save();
        }

        public void InitPortData(ComPortSortType sortType = ComPortSortType.AddTime)
        {
            string[] ports = SerialPort.GetPortNames();
            List<string> portNames = new List<string>();
            switch (sortType)
            {
                case ComPortSortType.AddTime:
                    portNames = ports.ToList();
                    break;
                case ComPortSortType.PortName:
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
                portTabItem.SerialPort.BaudRate = value;
                comSettings.PortSetting = portTabItem.SerialPort.PortSettingToJson();
                comMapper.UpdateFieldById("PortSetting", comSettings.PortSetting, comSettings.Id);
            }
        }
    }
}