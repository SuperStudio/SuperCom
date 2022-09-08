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
            string value = line.Replace("\0", "\\0");
            if (AddTimeStamp)
            {
                // 遍历字符串
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < value.Length; i++)
                {
                    char c = value[i];

                    if (c == '\r' && i < value.Length - 1 && value[i + 1] == '\n')
                    {
                        continue;
                    }
                    else if (c == '\r' || c == '\n')
                    {
                        builder.Append(c);
                        builder.Append($"[{DateHelper.Now()}] ");
                    }
                    else
                    {
                        builder.Append(c);
                    }
                    //if (c == '\r' || c == '\n')
                    //    builder.Append($"{c.ToString().Replace("\r", "\\r").Replace("\n", "\\n")}[{DateHelper.Now()}] ");
                }
                value = builder.ToString().Replace("\r\n", "\n");
                if (string.IsNullOrEmpty(TextBox.Text))
                    value = $"[{DateHelper.Now()}] " + value;
            }
            TextBox?.AppendText(value);
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




        public PortTabItem(string name, bool connected)
        {
            Name = name;
            Connected = connected;
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
