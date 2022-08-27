using DynamicData.Annotations;
using SuperCom.Utils;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SuperCom.Entity
{
    public class PortTabItem : INotifyPropertyChanged
    {
        private string _ID;
        public string ID
        {
            get { return _ID; }
            set { _ID = value; OnPropertyChanged(); }
        }
        private string _Name;
        public string Name
        {
            get { return _Name; }
            set { _Name = value; OnPropertyChanged(); }
        }
        public bool _Connected;
        public bool Connected
        {
            get { return _Connected; }
            set { _Connected = value; OnPropertyChanged(); }
        }


        private bool _Selected;
        public bool Selected
        {
            get { return _Selected; }
            set { _Selected = value; OnPropertyChanged(); }
        }


        private string _Data;
        public string Data
        {
            get { return _Data; }
            set { _Data = value; OnPropertyChanged(); }
        }
        private PortSetting _Setting;
        public PortSetting Setting
        {
            get { return _Setting; }
            set
            {
                _Setting = value;
                OnPropertyChanged();
            }
        }
        private CustomSerialPort _SerialPort;
        public CustomSerialPort SerialPort
        {
            get { return _SerialPort; }
            set { _SerialPort = value; OnPropertyChanged(); }
        }
        private bool _ScrollToEnd;
        public bool ScrollToEnd
        {
            get { return _ScrollToEnd; }
            set { _ScrollToEnd = value; OnPropertyChanged(); }
        }
        private bool _AddNewLineWhenWrite = true;
        public bool AddNewLineWhenWrite
        {
            get { return _AddNewLineWhenWrite; }
            set { _AddNewLineWhenWrite = value; OnPropertyChanged(); }
        }
        private string _WriteData = "AT^VERSION?";
        public string WriteData
        {
            get { return _WriteData; }
            set { _WriteData = value; OnPropertyChanged(); }
        }

        private StringBuilder Builder { get; set; }
        public TextBox TextBox { get; set; }

        public bool AddTimeStamp { get; set; }


        public void RefreshSettings()
        {
            if (SerialPort != null)
            {
                SerialPort.BaudRate = Setting.BaudRate;
                SerialPort.StopBits = Setting.StopBits;
                SerialPort.Parity = Setting.Parity;
                SerialPort.DataBits = Setting.DataBits;
            }

        }

        public DateTime ConnectTime { get; set; }


        public void ClearData()
        {
            Builder.Clear();
        }

        public string GetSaveFileName()
        {
            return Path.Combine(GlobalVariable.LogDir, $"[{Name}]{ConnectTime.ToString("yyyy-MM-dd-HH-mm-ss-fff")}.log");
        }

        public void SaveData(string line)
        {
            //bool zero = line.IndexOf("\0") >= 0;
            string value = line.Replace("\0", "\\0");
            if (AddTimeStamp)
            {
                int idx = line.IndexOf("\n");
                if (idx >= 0)
                {
                    value = $"{value.Substring(0, idx)}【{DateHelper.Now()}】{value.Substring(idx + 1)}";
                }
            }
            if (value.IndexOf('\n') < 0 && value.IndexOf("\\0\\0") >= 0) value += "\n";
            if (TextBox != null)
            {
                TextBox.AppendText(value);
            }


            // 保存到本地
            string fileName = GetSaveFileName();
            try
            {
                File.AppendAllText(fileName, value, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }




        public PortTabItem(string name, bool connected, string id)
        {
            Name = name;
            Connected = connected;
            ID = id;
            Setting = new PortSetting();
            Builder = new StringBuilder();
            AddTimeStamp = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
