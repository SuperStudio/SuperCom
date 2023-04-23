
using Microsoft.Win32;
using SuperControls.Style;
using SuperUtils.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Text;
using static SuperCom.App;

namespace SuperCom.Entity
{
    public class CustomSerialPort : SerialPort, INotifyPropertyChanged
    {

        public const int CLOSE_TIME_OUT = 5;

        public static Encoding DEFAULT_ENCODING = System.Text.Encoding.UTF8;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private const int DEFAULT_WRITE_TIME_OUT = 1000;
        private const int DEFAULT_READ_TIME_OUT = 2000;
        private const int MIN_TIME_OUT = 0;
        private const int MAX_TIME_OUT = 60 * 60 * 1000;

        public bool HandledDataReceived { get; set; }


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

        public static double DEFAULT_FONTSIZE = 15;

        public CustomSerialPort(string portName)
        {
            this.PortName = portName;
            RefreshSetting();
        }

        /// <summary>
        /// 仅可以有 COM 加 数字
        /// </summary>
        /// <returns></returns>
        public static string[] GetAllPorts()
        {
            List<string> result = new List<string>();
            string[] ports = GetPortNames();
            foreach (var item in ports)
            {
                if (int.TryParse(item.ToUpper().Replace("COM", ""), out int portNumber))
                    result.Add(item);
            }
            return result.ToArray();
        }

        public static string[] GetUsbSerDevices()
        {
            // HKLM\SYSTEM\CurrentControlSet\services\usbser\Enum -> Device IDs of what is plugged in
            // HKLM\SYSTEM\CurrentControlSet\Enum\{Device_ID}\Device Parameters\PortName -> COM port name.

            List<string> ports = new List<string>();

            RegistryKey k1 = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\services\usbser\Enum");
            if (k1 == null)
            {
                Debug.Fail("Unable to open Enum key");
            }
            else
            {
                int count = (int)k1.GetValue("Count");
                for (int i = 0; i < count; i++)
                {
                    object deviceID = k1.GetValue(i.ToString("D", CultureInfo.InvariantCulture));
                    Debug.Assert(deviceID != null && deviceID is string);
                    RegistryKey k2 = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\" + (string)deviceID + @"\Device Parameters");
                    if (k2 == null)
                    {
                        continue;
                    }
                    object portName = k2.GetValue("PortName");
                    Debug.Assert(portName != null && portName is string);
                    ports.Add((string)portName);
                }
            }
            return ports.ToArray();
        }

        public string Remark = "";
        public bool Pinned = false;
        public bool Hide = false;

        public void SaveRemark(string remark)
        {
            this.Remark = remark;
            SettingJson = PortSettingToJson(); // 保存
        }
        public void SavePinned(bool pinned)
        {
            this.Pinned = pinned;
            SettingJson = PortSettingToJson(); // 保存
        }
        public void SaveHide(bool hide)
        {
            this.Hide = hide;
            SettingJson = PortSettingToJson(); // 保存
        }


        public void SaveProperties()
        {
            //this.BaudRate = Setting.BaudRate;
            //this.DataBits = Setting.DataBits;
            this.Encoding = GetEncoding();
            this.StopBits = GetStopBits();
            this.Parity = GetParity();
        }

        public void RefreshSetting()
        {
            try
            {
                SaveProperties();
                SettingJson = PortSettingToJson(); // 保存
            }
            catch (Exception ex)
            {
                MessageCard.Error(ex.Message);
            }
        }

        public string SettingJson { get; set; }

        public string PortSettingToJson()
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            dic.Add("BaudRate", this.BaudRate);
            dic.Add("DataBits", this.DataBits);
            dic.Add("Encoding", this.Encoding.HeaderName);
            dic.Add("StopBits", this.StopBitsString);
            dic.Add("Parity", this.ParityString);
            dic.Add("DTR", this.DtrEnable);
            dic.Add("Handshake", this.Handshake);
            if (this.Handshake == Handshake.RequestToSend || this.Handshake == Handshake.RequestToSendXOnXOff)
            {
                // Handshake 设置为 RequestToSend 或 RequestToSendXOnXOff，则无法访问 RtsEnable
            }
            else
            {
                dic.Add("RTS", this.RtsEnable);
            }
            //dic.Add("BreakState", this.BreakState);
            dic.Add("ReadTimeout", this.ReadTimeoutValue);
            dic.Add("WriteTimeout", this.WriteTimeoutValue);

            dic.Add("DiscardNull", this.DiscardNull);



            dic.Add("Remark", this.Remark);
            dic.Add("Pinned", this.Pinned);
            dic.Add("Hide", this.Hide);
            dic.Add("TextFontSize", this.TextFontSize);
            dic.Add("HighLightIndex", this.HighLightIndex);
            return JsonUtils.TrySerializeObject(dic);
        }

        public void SetPortSettingByJson(string json)
        {
            Dictionary<string, object> dict = JsonUtils.TryDeserializeObject<Dictionary<string, object>>(json);
            if (dict != null)
            {
                int baudRate = PortSetting.DEFAULT_BAUDRATE;
                int.TryParse(dict["BaudRate"].ToString(), out baudRate);
                this.BaudRate = baudRate;

                int dataBits = PortSetting.DEFAULT_DATABITS;
                int.TryParse(dict["DataBits"].ToString(), out dataBits);
                this.DataBits = dataBits;

                if (dict.ContainsKey("TextFontSize"))
                {
                    double fontSize = CustomSerialPort.DEFAULT_FONTSIZE;
                    double.TryParse(dict["TextFontSize"].ToString(), out fontSize);
                    this.TextFontSize = fontSize;
                }
                if (dict.ContainsKey("HighLightIndex"))
                {
                    long.TryParse(dict["HighLightIndex"].ToString(), out long index);
                    this.HighLightIndex = index;
                }


                this.PortEncoding = dict["Encoding"].ToString();
                this.ParityString = dict["Parity"].ToString();
                this.StopBitsString = dict["StopBits"].ToString();


                if (dict.ContainsKey("DTR") && dict["DTR"] is bool dtr)
                    this.DtrEnable = dtr;
                if (dict.ContainsKey("RTS") && dict["RTS"] is bool rts)
                    this.RtsEnable = rts;
                //if (dict.ContainsKey("BreakState") && dict["BreakState"] is bool BreakState)
                //    this.BreakState = BreakState;
                if (dict.ContainsKey("DiscardNull") && dict["DiscardNull"] is bool DiscardNull)
                    this.DiscardNull = DiscardNull;

                if (dict.ContainsKey("ReadTimeout") && int.TryParse(dict["ReadTimeout"].ToString(), out int readTimeOut))
                    this.ReadTimeoutValue = readTimeOut;
                if (dict.ContainsKey("WriteTimeout") && int.TryParse(dict["WriteTimeout"].ToString(), out int writeTimeOut))
                    this.WriteTimeoutValue = writeTimeOut;
                if (dict.ContainsKey("Handshake") && Enum.TryParse(dict["Handshake"].ToString(), out Handshake Handshake))
                    this.Handshake = Handshake;

                if (dict.ContainsKey("Remark"))
                    this.Remark = dict["Remark"].ToString();
                if (dict.ContainsKey("Pinned"))
                    this.Pinned = dict["Pinned"].ToString().ToLower().Equals("true") ? true : false;
                if (dict.ContainsKey("Hide"))
                    this.Hide = dict["Hide"].ToString().ToLower().Equals("true") ? true : false;

            }
        }

        public static string GetRemark(string json)
        {
            Dictionary<string, object> dict = JsonUtils.TryDeserializeObject<Dictionary<string, object>>(json);
            if (dict != null)
            {
                if (dict.ContainsKey("Remark"))
                    return dict["Remark"].ToString();
            }
            return "";
        }
        public static bool GetHide(string json)
        {
            Dictionary<string, object> dict = JsonUtils.TryDeserializeObject<Dictionary<string, object>>(json);
            string status = "";
            if (dict != null)
            {
                if (dict.ContainsKey("Hide"))
                    status = dict["Hide"].ToString();
            }
            return status.ToLower().Equals("true") ? true : false;
        }
        public static bool GetPinned(string json)
        {
            Dictionary<string, object> dict = JsonUtils.TryDeserializeObject<Dictionary<string, object>>(json);
            string status = "";
            if (dict != null)
            {
                if (dict.ContainsKey("Pinned"))
                    status = dict["Pinned"].ToString();
            }
            return status.ToLower().Equals("true") ? true : false;
        }



        public Encoding GetEncoding()
        {
            try
            {
                if (PortEncoding.ToUpper().Equals("UTF8")) return System.Text.Encoding.UTF8;
                Encoding encoding = System.Text.Encoding.GetEncoding(PortEncoding);
                return encoding;
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
            set
            {
                _PortEncoding = value;
                RaisePropertyChanged();
                RefreshSetting();
            }
        }
        private string _StopBitsString = "One";
        public string StopBitsString
        {
            get { return _StopBitsString; }
            set
            {
                _StopBitsString = value;
                RaisePropertyChanged();
                RefreshSetting();
            }
        }
        private string _ParityString = "One";
        public string ParityString
        {
            get { return _ParityString; }
            set
            {
                _ParityString = value;
                RaisePropertyChanged();
                RefreshSetting();
            }
        }
        private double _TextFontSize = DEFAULT_FONTSIZE;
        public double TextFontSize
        {
            get { return _TextFontSize; }
            set { _TextFontSize = value; RaisePropertyChanged(); }
        }
        private long _HighLightIndex = 0;
        public long HighLightIndex
        {
            get { return _HighLightIndex; }
            set { _HighLightIndex = value; RaisePropertyChanged(); }
        }
        private long _FilterSelectedIndex = 0;
        public long FilterSelectedIndex
        {
            get { return _FilterSelectedIndex; }
            set { _FilterSelectedIndex = value; RaisePropertyChanged(); }
        }
        private long _ReadTimeoutValue = DEFAULT_READ_TIME_OUT;
        public long ReadTimeoutValue
        {
            get { return _ReadTimeoutValue; }
            set
            {
                _ReadTimeoutValue = value;
                RaisePropertyChanged();
                if (value >= MIN_TIME_OUT && value <= MAX_TIME_OUT)
                    this.ReadTimeout = (int)value;

            }
        }
        private long _WriteTimeoutValue = DEFAULT_WRITE_TIME_OUT;
        public long WriteTimeoutValue
        {
            get { return _WriteTimeoutValue; }
            set
            {
                _WriteTimeoutValue = value;
                RaisePropertyChanged();
                if (value >= MIN_TIME_OUT && value <= MAX_TIME_OUT)
                    this.WriteTimeout = (int)value;
            }
        }

        public void PrintSetting()
        {
            string data = PortSettingToJson();
            Logger.Info(data);
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

        public void RestoreDefault()
        {
            this.DtrEnable = false;
            this.RtsEnable = false;
            this.DiscardNull = false;
            this.ReadTimeout = DEFAULT_READ_TIME_OUT;
            this.WriteTimeout = DEFAULT_WRITE_TIME_OUT;
            this.Handshake = Handshake.None;
            this.StopBits = StopBits.One;
            this.Parity = Parity.None;
            this.Encoding = DEFAULT_ENCODING;
        }
    }
}
