
using SuperCom.Config;
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
        public static List<int> DEFAULT_BAUDRATES = new List<int>()
        {
            9600,19200,115200,1500000
        };

        public static List<int> DEFAULT_DATABITS_LIST = new List<int>()
        {
            5,6,7,8
        };

        public static List<string> DEFAULT_ENCODINGS = new List<string>()
        {
            "UTF8","US-ASCII","GB2312","ISO-8859-1"
        };

        public static List<string> DEFAULT_PARITYS = new List<string>()
        {
            "None","Odd","Even","Mark","Space"
        };

        public static List<string> DEFAULT_STOPBIT_LIST = new List<string>()
        {
            "One","Two","OnePointFive"
        };

        public static List<string> DEFAULT_HANDSHAKES = Enum.GetNames(typeof(System.IO.Ports.Handshake)).ToList();

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



        public static List<string> GetAllBaudRates()
        {
            List<string> list = new List<string>();
            if (ConfigManager.Main.UseDefaultBaudRate)
            {
                foreach (var item in PortSetting.DEFAULT_BAUDRATES)
                {
                    list.Add(item.ToString());
                }
                ConfigManager.Main.UseDefaultBaudRate = false;
                ConfigManager.Main.Save();
            }

            return list;
        }


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
