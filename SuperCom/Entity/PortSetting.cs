using DynamicData.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SuperCom.Entity
{


    public enum FlowControls
    {
        None,
        Hardware,
        Software,
        Custom
    }

    public class PortSetting : INotifyPropertyChanged
    {

        public static int DEFAULT_BAUDRATE = 115200;
        public static int DEFAULT_DATABITS = 8;
        public static StopBits DEFAULT_STOPBITS = StopBits.One;
        public static Parity DEFAULT_PARITY = Parity.None;
        public static FlowControls DEFAULT_FLOWCONTROLS = FlowControls.None;

        private int _BaudRate;
        public int BaudRate
        {
            get { return _BaudRate; }
            set { _BaudRate = value; OnPropertyChanged(); }
        }
        private int _DataBits;
        public int DataBits
        {
            get { return _DataBits; }
            set { _DataBits = value; OnPropertyChanged(); }
        }
        private StopBits _Stopbits;
        public StopBits StopBits
        {
            get { return _Stopbits; }
            set { _Stopbits = value; OnPropertyChanged(); }
        }

        private Parity _Parity;
        public Parity Parity
        {
            get { return _Parity; }
            set { _Parity = value; OnPropertyChanged(); }
        }
        private FlowControls _FlowControls;
        public FlowControls FlowControls
        {
            get { return _FlowControls; }
            set { _FlowControls = value; OnPropertyChanged(); }
        }

        public static PortSetting GetDefaultSetting()
        {
            PortSetting portSetting = new PortSetting();
            portSetting.BaudRate = DEFAULT_BAUDRATE;
            portSetting.DataBits = DEFAULT_DATABITS;
            portSetting.StopBits = DEFAULT_STOPBITS;
            portSetting.Parity = DEFAULT_PARITY;
            portSetting.FlowControls = DEFAULT_FLOWCONTROLS;
            return portSetting;
        }


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
