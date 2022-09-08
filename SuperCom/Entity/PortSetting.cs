using DynamicData.Annotations;
using SuperControls.Style;
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

    public class PortSetting
    {

        public static int DEFAULT_BAUDRATE = 115200;
        public static int DEFAULT_DATABITS = 8;
        public static StopBits DEFAULT_STOPBITS = StopBits.One;
        public static Parity DEFAULT_PARITY = Parity.None;
        public static FlowControls DEFAULT_FLOWCONTROLS = FlowControls.None;
        public static string DEFAULT_ENCODING = "UTF8";

        public int BaudRate { get; set; }
        public int DataBits { get; set; }
        public StopBits StopBits { get; set; }

        public Parity Parity { get; set; }
        public FlowControls FlowControls { get; set; }
        public string Encoding { get; set; }




        public static PortSetting GetDefaultSetting()
        {
            PortSetting portSetting = new PortSetting();
            portSetting.BaudRate = DEFAULT_BAUDRATE;
            portSetting.DataBits = DEFAULT_DATABITS;
            portSetting.StopBits = DEFAULT_STOPBITS;
            portSetting.Parity = DEFAULT_PARITY;
            portSetting.FlowControls = DEFAULT_FLOWCONTROLS;
            portSetting.Encoding = DEFAULT_ENCODING;
            return portSetting;
        }
    }
}
