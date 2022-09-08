using DynamicData.Annotations;
using SuperControls.Style;
using System;
using System.ComponentModel;
using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Text;

namespace SuperCom.Entity
{
    public class CustomSerialPort : SerialPort, INotifyPropertyChanged
    {

        public PortSetting _Setting = PortSetting.GetDefaultSetting();
        public PortSetting Setting
        {
            get { return _Setting; }
            set { _Setting = value; }
        }
        public CustomSerialPort(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
            : base(portName, baudRate, parity, dataBits, stopBits)
        {
        }

        public CustomSerialPort(string portName)
        {
            this.PortName = portName;
            RefreshSetting();
        }


        public void RefreshSetting()
        {
            try
            {
                this.BaudRate = Setting.BaudRate;
                this.DataBits = Setting.DataBits;
                this.Encoding = GetEncoding();
                this.StopBits = GetStopBits();
                this.Parity = GetParity();
            }
            catch (Exception ex)
            {
                MessageCard.Error(ex.Message);
            }
        }

        public Encoding GetEncoding()
        {
            try
            {
                if (PortEncoding.Equals("UTF8")) return System.Text.Encoding.UTF8;
                return System.Text.Encoding.GetEncoding(PortEncoding);
            }
            catch (Exception ex)
            {
                MessageCard.Error(ex.Message);
                return System.Text.Encoding.UTF8;
            }
        }

        public StopBits GetStopBits()
        {
            Enum.TryParse<StopBits>(StopBitsString, out StopBits result);
            return result;
        }
        public Parity GetParity()
        {
            Enum.TryParse<Parity>(ParityString, out Parity result);
            return result;
        }

        private string _PortEncoding = "UTF8";
        public string PortEncoding
        {
            get { return _PortEncoding; }
            set { _PortEncoding = value; OnPropertyChanged(); }
        }
        private string _StopBitsString = "One";
        public string StopBitsString
        {
            get { return _StopBitsString; }
            set { _StopBitsString = value; OnPropertyChanged(); }
        }
        private string _ParityString = "One";
        public string ParityString
        {
            get { return _ParityString; }
            set { _ParityString = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            RefreshSetting();
        }


        public override bool Equals(object obj)
        {
            if (obj != null && obj is CustomSerialPort serialPort)
            {
                return serialPort.PortName.Equals(PortName);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return PortName.GetHashCode();
        }
    }
}
