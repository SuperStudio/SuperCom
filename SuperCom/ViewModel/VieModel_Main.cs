using GalaSoft.MvvmLight;
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

        public HashSet<string> SendHistory { get; set; }
        public HashSet<ComSettings> ComSettingList { get; set; }





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


        public VieModel_Main()
        {
            Init();
        }

        public void Init()
        {
            PortTabItems = new ObservableCollection<PortTabItem>();
            InitPortSampleData();
            if (!string.IsNullOrEmpty(ConfigManager.Main.SendHistory))
            {
                SendHistory = JsonUtils.TryDeserializeObject<HashSet<string>>(ConfigManager.Main.SendHistory);
            }
            if (SendHistory == null) SendHistory = new HashSet<string>();
        }

        public void SaveSendHistory()
        {
            ConfigManager.Main.SendHistory = JsonUtils.TrySerializeObject(SendHistory);
            ConfigManager.Main.Save();
        }

        public void InitPortSampleData()
        {
            string[] ports = SerialPort.GetPortNames();
            SideComPorts = new ObservableCollection<SideComPort>();
            foreach (string port in ports)
            {
                SideComPorts.Add(new SideComPort(port, false));
            }
        }
    }
}