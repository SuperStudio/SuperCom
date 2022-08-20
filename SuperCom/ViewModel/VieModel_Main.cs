using GalaSoft.MvvmLight;
using SuperCom.Entity;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;

namespace SuperCom.ViewModel
{

    public class VieModel_Main : ViewModelBase
    {
        private ObservableCollection<PortTabItem> _PortTabItems;
        public ObservableCollection<PortTabItem> PortTabItems
        {
            get { return _PortTabItems; }
            set { _PortTabItems = value; RaisePropertyChanged(); }
        }
        private ObservableCollection<SerialComPort> _SerialComPorts;
        public ObservableCollection<SerialComPort> SerialComPorts
        {
            get { return _SerialComPorts; }
            set { _SerialComPorts = value; RaisePropertyChanged(); }
        }

        public List<CustomSerialPort> SerialPorts { get; set; }
        private string _StatusText = "¾ÍÐ÷";
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
            SerialPorts = new List<CustomSerialPort>();
            InitPortSampleData();
        }

        public void InitPortSampleData()
        {
            string[] ports = SerialPort.GetPortNames();
            SerialComPorts = new ObservableCollection<SerialComPort>();
            foreach (string port in ports)
            {
                SerialComPorts.Add(new SerialComPort(port, false));
            }
        }
    }
}