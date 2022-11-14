using DynamicData.Annotations;
using SuperControls.Style;
using SuperUtils.Common;
using System;
using System.Collections.Generic;
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

        public static double DEFAULT_FONTSIZE = 15;

        public CustomSerialPort(string portName)
        {
            this.PortName = portName;
            RefreshSetting();
        }

        public string Remark = "";

        public void SaveRemark(string remark)
        {
            this.Remark = remark;
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
            dic.Add("Remark", this.Remark);
            dic.Add("TextFontSize", this.TextFontSize);
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


                this.PortEncoding = dict["Encoding"].ToString();
                this.ParityString = dict["Parity"].ToString();
                this.StopBitsString = dict["StopBits"].ToString();
                if (dict.ContainsKey("Remark"))
                    this.Remark = dict["Remark"].ToString();
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
        private double _TextFontSize = DEFAULT_FONTSIZE;
        public double TextFontSize
        {
            get { return _TextFontSize; }
            set { _TextFontSize = value; OnPropertyChanged(); }
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
