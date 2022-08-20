using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperCom.Entity
{
    public class CustomSerialPort : SerialPort
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
            this.BaudRate = Setting.BaudRate;
            this.Parity = Setting.Parity;
            this.DataBits = Setting.DataBits;
            this.StopBits = Setting.Stopbits;
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
