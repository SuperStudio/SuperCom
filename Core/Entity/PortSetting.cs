
using SuperCom.Config;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;

namespace SuperCom.Entity
{
    public enum FlowControls
    {
        None,
        Hardware,
        Software,
        Custom
    }


    /// <summary>
    /// 串口设置
    /// </summary>
    public class PortSetting
    {
        #region "常量"
        public const int CLOSE_TIME_OUT = 5;
        public const double DEFAULT_FONTSIZE = 15;
        public const int DEFAULT_WRITE_TIME_OUT = 1000;
        public const int DEFAULT_SUBCONTRACTING_TIME_OUT = 10;
        public const int DEFAULT_READ_TIME_OUT = 2000;
        public const int MIN_TIME_OUT = 0;
        public const int MAX_TIME_OUT = 60 * 60 * 1000;
        public const bool DEFAULT_DTR = false;
        public const bool DEFAULT_RTS = false;
        public const bool DEFAULT_DISCARD_NULL = false;

        public const double MAX_FONTSIZE = 25;
        public const double MIN_FONTSIZE = 5;


        public const int DEFAULT_BAUDRATE = 115200;
        public const int DEFAULT_DATABITS = 8;
        public const string DEFAULT_ENCODING_STRING = "UTF8";

        #endregion

        #region "静态属性"
        public static Encoding DEFAULT_ENCODING = System.Text.Encoding.UTF8;

        public static StopBits DEFAULT_STOPBITS = StopBits.One;
        public static Parity DEFAULT_PARITY = Parity.None;
        public static FlowControls DEFAULT_FLOW_CONTROLS = FlowControls.None;
        public static Handshake DEFAULT_HANDSHAKE = Handshake.None;
        public static List<string> DEFAULT_HANDSHAKES = Enum.GetNames(typeof(System.IO.Ports.Handshake)).ToList();

        public static List<int> DEFAULT_BAUDRATES = new List<int>()
        {
            9600,19200,115200,1500000
        };

        public static List<int> DEFAULT_DATABITS_LIST = new List<int>()
        {
            5,6,7,8
        };

        /// <summary>
        /// 更多编码支持参考：
        /// <see href="https://learn.microsoft.com/en-us/dotnet/api/system.text.encoding.getencodings">microsoft</see>
        /// </summary>
        public static List<string> DEFAULT_ENCODINGS = System.Text.Encoding.GetEncodings().Select(item => item.Name).OrderBy(item => item).ToList();

        public static List<string> DEFAULT_PARITIES = new List<string>()
        {
            "None","Odd","Even","Mark","Space"
        };

        public static List<string> DEFAULT_STOPBIT_LIST = new List<string>()
        {
            "One","Two","OnePointFive"
        };


        #endregion

        #region "属性"

        public int BaudRate { get; set; }
        public int DataBits { get; set; }
        public StopBits StopBits { get; set; }

        public Parity Parity { get; set; }
        public FlowControls FlowControls { get; set; }
        public string Encoding { get; set; }

        #endregion


        public static List<string> GetAllBaudRates()
        {
            List<string> list = new List<string>();
            if (ConfigManager.Main.UseDefaultBaudRate) {
                foreach (var item in DEFAULT_BAUDRATES) {
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
            portSetting.FlowControls = DEFAULT_FLOW_CONTROLS;
            portSetting.Encoding = DEFAULT_ENCODING_STRING;
            return portSetting;
        }
    }
}
