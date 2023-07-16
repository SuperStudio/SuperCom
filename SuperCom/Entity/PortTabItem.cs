
using ICSharpCode.AvalonEdit;
using SuperCom.Config;
using SuperCom.Config.WindowConfig;
using SuperUtils.Common;
using SuperUtils.IO;
using SuperUtils.Time;
using SuperUtils.WPF.VieModel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static SuperCom.App;
using static SuperCom.Entity.HighLightRule;

namespace SuperCom.Entity
{
    public class PortTabItem : ViewModelBase
    {
        private const int MAX_READ_LENGTH = 10240;
        private const int READ_INTERVAL = 50;
        private const int NOT_RECV_HEX_SLEEP_INTERVAL = 2000;

        private AutoResetEvent ResetEvent { get; set; }

        public TextEditor TextEditor { get; set; }

        public double CurrentCharSize { get; set; }


        private bool _RunningCommands;
        public bool RunningCommands {
            get { return _RunningCommands; }
            set { _RunningCommands = value; RaisePropertyChanged(); }
        }

        public Queue<ResultCheck> ResultChecks { get; set; }
        public string SaveFileName { get; set; }

        public int FragCount { get; set; }

        private string _Name;
        public string Name {
            get { return _Name; }
            set { _Name = value; RaisePropertyChanged(); }
        }
        public bool _Connected;
        public bool Connected {
            get { return _Connected; }
            set {
                _Connected = value;
                RaisePropertyChanged();
            }
        }

        private bool _Selected;
        public bool Selected {
            get { return _Selected; }
            set { _Selected = value; RaisePropertyChanged(); }
        }


        private string _Data;
        public string Data {
            get { return _Data; }
            set { _Data = value; RaisePropertyChanged(); }
        }


        private PortSetting _Setting;
        public PortSetting Setting {
            get { return _Setting; }
            set {
                _Setting = value;
                RaisePropertyChanged();
            }
        }

        private SerialPortEx _SerialPort;
        public SerialPortEx SerialPort {
            get { return _SerialPort; }
            set { _SerialPort = value; RaisePropertyChanged(); }
        }

        private bool _AddNewLineWhenWrite = true;
        public bool AddNewLineWhenWrite {
            get { return _AddNewLineWhenWrite; }
            set {
                _AddNewLineWhenWrite = value;
                RaisePropertyChanged();
                RefreshSendHexValue(WriteData);
                Logger.Info($"set AddNewLineWhenWrite: {value}");
            }
        }

        private bool _SendHex;
        public bool SendHex {
            get { return _SendHex; }
            set {
                _SendHex = value;
                RaisePropertyChanged();
                if (value)
                    RefreshSendHexValue(WriteData);

                Logger.Info($"set SendHex: {value}");
            }
        }

        private bool _RecvShowHex;
        public bool RecvShowHex {
            get { return _RecvShowHex; }
            set {
                _RecvShowHex = value;
                RaisePropertyChanged();
                SetDataReceivedType();
                Logger.Info($"port: {Name}, RecvShowHex: {value}");
            }
        }
        private string _SendHexValue;
        public string SendHexValue {
            get { return _SendHexValue; }
            set { _SendHexValue = value; RaisePropertyChanged(); }
        }
        private string _WriteData = "";
        public string WriteData {
            get { return _WriteData; }
            set {
                _WriteData = value;
                RaisePropertyChanged();
                RefreshSendHexValue(value);
            }
        }



        private bool _AddTimeStamp = true;
        public bool AddTimeStamp {
            get { return _AddTimeStamp; }
            set {
                _AddTimeStamp = value;
                RaisePropertyChanged();
                Logger.Info($"port: {Name}, AddTimeStamp: {value}");
            }
        }


        private bool _EnabledMonitor = true;
        public bool EnabledMonitor {
            get { return _EnabledMonitor; }
            set {
                _EnabledMonitor = value;
                RaisePropertyChanged();
                //if (SerialPort != null && SerialPort.IsOpen)
                //{
                //    if (value)
                //        StartMonitorTask();
                //    else
                //        StopMonitorTask();
                //}
            }
        }


        private long _RX = 0L;
        public long RX {
            get { return _RX; }
            set { _RX = value; RaisePropertyChanged(); }
        }
        private long _TX = 0L;
        public long TX {
            get { return _TX; }
            set { _TX = value; RaisePropertyChanged(); }
        }


        private string _Remark = "";

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark {
            get { return _Remark; }
            set { _Remark = value; RaisePropertyChanged(); }
        }

        private bool _EnabledFilter;
        public bool EnabledFilter {
            get { return _EnabledFilter; }
            set {
                _EnabledFilter = value;
                RaisePropertyChanged();
                if (SerialPort != null && SerialPort.IsOpen) {
                    if (value)
                        StartFilterTask();
                    else
                        StopFilterTask();
                }
            }
        }

        private DateTime _ConnectTime;
        public DateTime ConnectTime {
            get { return _ConnectTime; }
            set {
                _ConnectTime = value;
                SaveFileName = GetSaveFileName();
            }
        }



        private bool _Pinned;
        public bool Pinned {
            get { return _Pinned; }
            set { _Pinned = value; RaisePropertyChanged(); }
        }

        private bool _FixedText;
        public bool FixedText {
            get { return _FixedText; }
            set {
                _FixedText = value;
                RaisePropertyChanged();
                if (TextEditor != null) {
                    if (value)
                        TextEditor.TextChanged -= TextBox_TextChanged;
                    else
                        TextEditor.TextChanged += TextBox_TextChanged;
                    Logger.Info($"fixed text: {value}");
                }
            }
        }


        public void TextBox_TextChanged(object sender, EventArgs e)
        {
            TextEditor textEditor = sender as TextEditor;
            textEditor?.ScrollToEnd();
        }


        public void ClearData()
        {
            FirstSaveData = true;
        }

        void ProcessLine(SerialPortEx serialPort, string line, string now)
        {
            if (string.IsNullOrEmpty(line))
                return;
            string portName = serialPort.PortName;
            try {
                App.Current.Dispatcher.Invoke(() => {
                    SaveData(line, now);
                });
            } catch (Exception ex) {
                App.Logger.Error(ex.Message);
            }
        }


        private string line = "";
        private string now = "";

        /// <summary>
        /// 处理收到的 COM 数据
        /// <para>参考：<see href="https://stackoverflow.com/a/13755084">stackoverflow</see></para>
        /// </summary>
        /// <param name="serialPort"></param>
        private void HandleStrReceived(SerialPortEx serialPort)
        {
            try {
                line = serialPort.ReadExisting();
                if (string.IsNullOrEmpty(line))
                    return;

                // 记录此刻的时间
                if (AddTimeStamp)
                    now = DateHelper.Now();

                ProcessLine(serialPort, line, now);
            } catch (Exception ex) {
                App.Logger.Error(ex.Message);
            }
        }

        private void OnReceiveStr(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPortEx serialPort = sender as SerialPortEx;
            HandleStrReceived(serialPort);
        }

        #region "HEX 收数据处理"


        private void OnReceiveHEX(object sender, SerialDataReceivedEventArgs e)
        {
            ResetEvent.Set();
        }


        private DateTime hexNow;


        /// <summary>
        /// 读串口的 16 进制数据
        /// <para>16 进制的读处理参考：<see href="https://github.com/chenxuuu/llcom">llcom</see></para>
        /// <para>参考1：<see href="https://stackoverflow.com/a/10882588">stackoverflow</see></para>
        /// <para>参考2：<see href="https://learn.microsoft.com/en-us/dotnet/api/system.io.ports.serialport.datareceived">microsoft</see></para>
        /// <para>参考3：<see href="https://stackoverflow.com/questions/46882774/how-to-deal-with-c-sharp-serialport-read-and-write-data-perfectly">deal-with-c-sharp-serialport-read</see></para>
        /// </summary>
        public void ReadHexTask()
        {
            ResetEvent.Reset();
            List<byte> allData = new List<byte>();
            while (true) {
                if (SerialPort == null || !SerialPort.IsOpen)
                    break;
                try {
                    int len = SerialPort.BytesToRead;
                    if (len == 0)
                        break;
                    byte[] buffer = new byte[len];
                    hexNow = DateTime.Now;
                    SerialPort.Read(buffer, 0, len);
                    if (buffer.Length == 0)
                        break;
                    allData.AddRange(buffer);
                } catch {
                    break;
                }

                if (allData.Count > MAX_READ_LENGTH)
                    break;

                Thread.Sleep(READ_INTERVAL); // 不能设置过小，也不能过大，否则一次读取的数据不完整

            }
            if (allData.Count > 0) {
                Application.Current.Dispatcher.Invoke(() => {
                    SaveHex(allData.ToArray(), hexNow.ToLocalDate());
                });
            }
        }
        #endregion



        /// <summary>
        /// 两种接收模式：HEX 和 STR
        /// </summary>
        /// <param name="serialPort"></param>
        /// <param name="tabItem"></param>
        public void SetDataReceivedType()
        {
            if (SerialPort == null)
                return;
            if (RecvShowHex) {
                // HEX 模式
                SerialPort.DataReceived -= OnReceiveStr;
                SerialPort.DataReceived -= OnReceiveHEX;
                SerialPort.DataReceived += OnReceiveHEX;
            } else {
                // STR 模式
                SerialPort.DataReceived -= OnReceiveHEX;
                SerialPort.DataReceived -= OnReceiveStr;
                SerialPort.DataReceived += OnReceiveStr;
            }
        }


        private void RefreshSendHexValue(string value)
        {
            if (SendHex) {
                string data = value;
                if (AddNewLineWhenWrite)
                    data += "\r\n";

                byte[] bytes = TransformHelper.ParseHexString(data);
                string str = TransformHelper.FormatHexString(TransformHelper.ByteArrayToHexString(bytes), "", " ");
                SendHexValue = $"将发送：{str}";
            }
        }

        private string GetFileNameByFormat(string format)
        {
            //  "%C","%R","%Y","%M","%D","%H","%M","%S","%F"
            string result = format;
            foreach (string item in CommonSettings.SUPPORT_FORMAT) {
                switch (item) {
                    case "%C":
                        result = result.Replace(item, Name);
                        break;
                    case "%R":
                        if (!string.IsNullOrEmpty(Remark))
                            result = result.Replace(item, Remark);
                        else
                            result = result.Replace("%R", "");
                        break;
                    case "%Y":
                        result = result.Replace(item, ConnectTime.Year.ToString());
                        break;
                    case "%M":
                        result = result.Replace(item, ConnectTime.Month.ToString());
                        break;
                    case "%D":
                        result = result.Replace(item, ConnectTime.Day.ToString());
                        break;
                    case "%h":
                        result = result.Replace(item, ConnectTime.Hour.ToString());
                        break;
                    case "%m":
                        result = result.Replace(item, ConnectTime.Minute.ToString());
                        break;
                    case "%s":
                        result = result.Replace(item, ConnectTime.Second.ToString());
                        break;
                    case "%f":
                        result = result.Replace(item, ConnectTime.Millisecond.ToString());
                        break;
                    default:
                        break;
                }
            }
            // 删除特殊字符
            result = FileHelper.ToProperFileName(result);
            return result;
        }

        private string GetDirByFormat(string format)
        {
            //  "%C","%R","%Y","%M","%D","%H","%M","%S","%F"
            string result = format;
            foreach (string item in CommonSettings.SUPPORT_FORMAT) {
                switch (item) {
                    case "%C":
                        result = result.Replace(item, Name);
                        break;
                    case "%R":
                        if (!string.IsNullOrEmpty(Remark))
                            result = result.Replace(item, Remark);
                        else
                            result = result.Replace("%R", "");
                        break;
                    case "%Y":
                        result = result.Replace(item, ConnectTime.Year.ToString());
                        break;
                    case "%M":
                        result = result.Replace(item, ConnectTime.Month.ToString());
                        break;
                    case "%D":
                        result = result.Replace(item, ConnectTime.Day.ToString());
                        break;
                    case "%h":
                        result = result.Replace(item, ConnectTime.Hour.ToString());
                        break;
                    case "%m":
                        result = result.Replace(item, ConnectTime.Minute.ToString());
                        break;
                    case "%s":
                        result = result.Replace(item, ConnectTime.Second.ToString());
                        break;
                    case "%f":
                        result = result.Replace(item, ConnectTime.Millisecond.ToString());
                        break;
                    default:
                        break;
                }
            }
            return result;
        }


        public string GetSaveFileName()
        {
            string format = ConfigManager.CommonSettings.LogNameFormat;
            if (string.IsNullOrEmpty(format))
                format = CommonSettings.DEFAULT_LOG_NAME_FORMAT;
            string name = GetFileNameByFormat(format);
            if (string.IsNullOrEmpty(name))
                name = GetFileNameByFormat(CommonSettings.DEFAULT_LOG_NAME_FORMAT);

            string dirName = ConfigManager.CommonSettings.LogSaveDir;
            if (string.IsNullOrEmpty(dirName))
                dirName = CommonSettings.DEFAULT_LOG_SAVE_DIR;
            string logDir = GetDirByFormat(dirName);
            if (string.IsNullOrEmpty(logDir))
                logDir = CommonSettings.DEFAULT_LOG_SAVE_DIR;
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);
            return Path.Combine(logDir, name + ".log");
        }


        public void FilterLine(string value)
        {
            // if (!EnabledFilter)
            TextEditor?.AppendText(value);
            // else
            // {
            //     if (!string.IsNullOrEmpty(value))
            //     {
            //         // 将字符转为一行
            //         int idx = value.IndexOf("\n");
            //         if (idx < 0)
            //             Buffer.Append(value);
            //         else
            //         {
            //             Buffer.Append(value.Substring(0, idx + 1));
            //             FilterQueue.Enqueue(Buffer.ToString());
            //             Buffer.Clear();
            //             FilterLine(value.Substring(idx + 1));
            //         }
            //     }

            // }
        }



        private readonly ConcurrentQueue<string> FilterQueue = new ConcurrentQueue<string>();

        private bool StopFilter = false;
        private bool FilterRunning = false;

        public void StartFilterTask()
        {
            return;
            if (!EnabledFilter || FilterRunning)
                return;
            StopFilter = false;
            FilterRunning = true;
            Task.Run(async () => {
                while (true) {
                    if (!FilterQueue.IsEmpty) {
                        bool success = FilterQueue.TryDequeue(out string data);
                        if (!success) {
                            Console.WriteLine("取队列元素失败");
                            continue;
                        }

                        if (IsInFilterRule(data)) {
                            App.Current.Dispatcher.Invoke(() => {
                                TextEditor?.AppendText(data);
                            });
                        } else {
                            Console.WriteLine($"过滤了：{data}");
                        }
                    } else {
                        await Task.Delay(100);
                        //Console.WriteLine("过滤中...");
                    }
                    if (StopFilter)
                        break;
                }
                FilterRunning = false;
            });
        }

        private static List<RuleSet> FilterRuleSet;
        private static List<RuleSet> GetFilterRuleSet(int index)
        {
            if (HighLightRule.AllRules == null || HighLightRule.AllRules.Count == 0) {
                return HighLightRule.DefaultRuleSet;
            } else if (index < HighLightRule.AllName.Count && index >= 0) {
                string name = HighLightRule.AllName[index];
                if (name.Equals("ComLog"))
                    return HighLightRule.DefaultRuleSet;

                HighLightRule rule = HighLightRule.AllRules.FirstOrDefault(arg => arg.RuleName.Equals(name));
                if (rule == null)
                    return HighLightRule.DefaultRuleSet;
                List<RuleSet> result = new List<RuleSet>();
                if (!string.IsNullOrEmpty(rule.RuleSetString)) {
                    result = JsonUtils.TryDeserializeObject<List<RuleSet>>(rule.RuleSetString);
                }
                return result;
            }
            return null;
        }

        public bool IsInFilterRule(string line)
        {
            // 获取当前过滤器
            if (SerialPort == null || string.IsNullOrEmpty(line))
                return true;
            int index = (int)SerialPort.HighLightIndex;
            FilterRuleSet = GetFilterRuleSet(index);
            if (FilterRuleSet == null || FilterRuleSet.Count == 0)
                return true;
            foreach (var item in FilterRuleSet) {
                if (item.RuleType == RuleType.Regex) {
                    try {
                        if (!string.IsNullOrEmpty(item.RuleValue) &&
                            Regex.IsMatch(line, item.RuleValue))
                            return true;
                    } catch (Exception ex) {
                        App.Logger.Error(ex.Message);
                        continue;
                    }

                } else if (item.RuleType == RuleType.KeyWord && !string.IsNullOrEmpty(item.RuleValue)) {
                    if (line.IndexOf(item.RuleValue) >= 0)
                        return true;
                }
            }

            return false;
        }

        public void StopFilterTask()
        {
            StopFilter = true;
        }

        private ConcurrentQueue<string> MonitorQueue = new ConcurrentQueue<string>();

        private bool StopMonitor = false;
        private bool MonitorRunning = false;
        private StringBuilder MonitorBuffer = new StringBuilder();
        public void MonitorLine(string value)
        {
            if (EnabledMonitor) {
                if (!string.IsNullOrEmpty(value)) {
                    // 将字符转为一行
                    int idx = value.IndexOf("\n");
                    if (idx < 0)
                        MonitorBuffer.Append(value);
                    else {
                        MonitorBuffer.Append(value.Substring(0, idx + 1));
                        MonitorQueue.Enqueue(MonitorBuffer.ToString());
                        MonitorBuffer.Clear();
                        MonitorLine(value.Substring(idx + 1));
                    }
                }

            }
        }

        private void RecordMonitorValue(string line)
        {
            //if (VarMonitors == null || VarMonitors.Count == 0)
            //    return;
            //foreach (VarMonitor monitor in VarMonitors.ToList())
            //{
            //    if (!monitor.Enabled || string.IsNullOrEmpty(monitor.RegexPattern))
            //        continue;
            //    Match match = Regex.Match(line, monitor.RegexPattern);
            //    if (match != null && match.Success)
            //    {
            //        // 写到文件中
            //        string toWrite = $"{{\"value\":\"{match.Value}\", " +
            //            $"\"line\":\"{line}\", " +
            //            $"\"pattern\":\"{monitor.RegexPattern}\"}}{Environment.NewLine}";
            //        Console.WriteLine($"成功捕获：{toWrite}");
            //        FileHelper.TryAppendToFile(monitor.DataFileName, toWrite);
            //        break;
            //    }
            //}
        }

        public void StartMonitorTask()
        {
            if (!EnabledMonitor || MonitorRunning)
                return;
            StopMonitor = false;
            MonitorRunning = true;
            Task.Run(async () => {
                while (true) {
                    if (!MonitorQueue.IsEmpty) {
                        bool success = MonitorQueue.TryDequeue(out string data);
                        if (!success) {
                            Console.WriteLine("取队列元素失败");
                            continue;
                        }
                        RecordMonitorValue(data);
                    } else {
                        await Task.Delay(100);
                        //Console.WriteLine("过滤中...");
                    }
                    if (StopMonitor)
                        break;
                }
                MonitorRunning = false;
            });
        }

        public void StopMonitorTask()
        {
            StopMonitor = true;
        }


        public void SepFile()
        {
            if (ConfigManager.Settings.EnabledLogFrag) {
                //if (CurrentCharSize >= 4096)
#if DEBUG
                if (CurrentCharSize / 1024 / 1024 >= (UInt64)ConfigManager.Settings.LogFragSize)
#else
                if (CurrentCharSize / 1024 / 1024 >= (UInt64)ConfigManager.Settings.LogFragSize)
#endif
                {
                    CurrentCharSize = 0;
                    ConnectTime = DateTime.Now;
                    FragCount++;
                }
                // 回调
                //if (ResultChecks?.Count > 0)
                //{
                //    HashSet<string> set = new HashSet<string>();
                //    foreach (ResultCheck item in ResultChecks)
                //    {
                //        // 同一类型的命令仅添加一次 buffer
                //        if (!set.Contains(item.Command))
                //        {
                //            item.Buffer.Append(value);
                //            set.Add(item.Command);
                //        }
                //    }
                //}
            }
        }

        public bool FirstSaveData;
        private StringBuilder Builder = new StringBuilder();
        public void SaveData(string inputData, string now)
        {
            RX += Encoding.UTF8.GetByteCount(inputData);     // todo
            string value = inputData.Replace("\0", "\\0");
            int valueLen = value.Length;
            if (AddTimeStamp) {
                // 遍历字符串
                Builder.Clear();
                // 一次遍历效率最高，使用 indexof 还额外多遍历几次
                char c;
                for (int i = 0; i < valueLen; i++) {
                    c = value[i];
                    if (c == '\r' && i < valueLen - 1 && value[i + 1] == '\n') {
                        continue;
                    } else if (c == '\r' || c == '\n') {
                        Builder.Append(c);
                        Builder.Append($"[{now}] ");
                    } else {
                        Builder.Append(c);
                    }
                }
                if (FirstSaveData) {
                    Builder.Insert(0, $"[{now}] ");
                    FirstSaveData = false;
                }
                value = Builder.ToString();
            }
            CurrentCharSize += Encoding.UTF8.GetByteCount(value);
            //MonitorLine(value);
            FilterLine(value);  // 过滤器
            SepFile();
            // 保存到本地
            try {
                if (ConfigManager.CommonSettings.WriteLogToFile)
                    File.AppendAllText(SaveFileName, value, Encoding.UTF8);
            } catch (Exception ex) {
                App.Logger.Error(ex.Message);
            }
        }


        public void SaveHex(byte[] bytes, string now)
        {
            if (bytes == null || bytes.Length == 0)
                return;
            App.Logger.Debug($"存数据：{bytes.Length} B");
            string value =
                TransformHelper.FormatHexString(TransformHelper.ByteArrayToHexString(bytes), "", " ");
            SaveData(value + Environment.NewLine, now);
        }



        public PortTabItem(string name, bool connected)
        {
            Name = name;
            Connected = connected;
            Setting = new PortSetting();
            ResetEvent = new AutoResetEvent(false);
            Task.Run(() => {
                while (true) {
                    if (RecvShowHex)
                        ReadHexTask();
                    else
                        Thread.Sleep(NOT_RECV_HEX_SLEEP_INTERVAL);
                }
            });
        }
    }

    public class ResultCheck
    {
        public string Command { get; set; }
        public StringBuilder Buffer { get; set; }
    }

}
