using DynamicData.Annotations;
using ICSharpCode.AvalonEdit;
using SuperUtils.Time;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
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
        private string _WriteData = "";
        public string WriteData
        {
            get { return _WriteData; }
            set { _WriteData = value; OnPropertyChanged(); }
        }

        private StringBuilder Builder { get; set; }
        public TextEditor TextEditor { get; set; }

        private bool _AddTimeStamp = true;
        public bool AddTimeStamp
        {
            get { return _AddTimeStamp; }
            set { _AddTimeStamp = value; OnPropertyChanged(); }
        }
        private long _RX = 0L;
        public long RX
        {
            get { return _RX; }
            set { _RX = value; OnPropertyChanged(); }
        }
        private long _TX = 0L;
        public long TX
        {
            get { return _TX; }
            set { _TX = value; OnPropertyChanged(); }
        }

        // 备注

        private string _Remark = "";
        public string Remark
        {
            get { return _Remark; }
            set { _Remark = value; OnPropertyChanged(); }
        }

        public DateTime ConnectTime { get; set; }

        public Queue<ResultCheck> ResultChecks { get; set; }

        public void ClearData()
        {
            Builder.Clear();
        }

        public string GetSaveFileName()
        {
            return Path.Combine(GlobalVariable.LogDir, $"[{Name}]{ConnectTime.ToString("yyyy-MM-dd-HH-mm-ss-fff")}.log");
        }

        public void FilterLine(string value)
        {
            // 查找结果
            TextEditor?.AppendText(value);
            // 回调
            if (ResultChecks?.Count > 0)
            {
                HashSet<string> set = new HashSet<string>();
                foreach (ResultCheck item in ResultChecks)
                {
                    // 同一类型的命令仅添加一次 buffer
                    if (!set.Contains(item.Command))
                    {
                        item.Buffer.Append(value);
                        set.Add(item.Command);
                    }
                }
            }

        }

        public void SaveData(string line)
        {
            RX += line.Length;
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
                }
                value = builder.ToString().Replace("\r\n", "\n");
                if (string.IsNullOrEmpty(TextEditor.Text))
                    value = $"[{DateHelper.Now()}] " + value;
            }
            FilterLine(value);
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
            //AddTimeStamp = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ResultCheck
    {
        public string Command { get; set; }
        public StringBuilder Buffer { get; set; }
    }

}
