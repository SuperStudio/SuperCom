using GalaSoft.MvvmLight;
using SuperCom.Entity;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;

namespace SuperCom.ViewModel
{

    public class VieModel_Main : ViewModelBase
    {
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

        //public List<CustomSerialPort> SerialPorts { get; set; }
        private string _StatusText = "就绪";
        public string StatusText
        {
            get { return _StatusText; }
            set { _StatusText = value; RaisePropertyChanged(); }
        }

        public VieModel_Main()
        {
            Init();
        }

        public void Init()
        {
            PortTabItems = new ObservableCollection<PortTabItem>();
            //SerialPorts = new List<CustomSerialPort>();
            InitPortSampleData();
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