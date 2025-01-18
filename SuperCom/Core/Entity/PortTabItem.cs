
using ICSharpCode.AvalonEdit;
using ITLDG.DataCheck;
using Newtonsoft.Json.Linq;
using SuperCom.Config;
using SuperCom.Config.WindowConfig;
using SuperCom.Core.Entity.Enums;
using SuperCom.Core.Utils;
using SuperControls.Style;
using SuperUtils.Common;
using SuperUtils.IO;
using SuperUtils.Time;
using SuperUtils.WPF.VieModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using static SuperCom.App;

namespace SuperCom.Entity
{

    /// <summary>
    /// 打开串口后，显示的标签栏
    /// </summary>
    public class PortTabItem : ViewModelBase
    {
        private const int MAX_READ_LENGTH = 10240;

        /// <summary>
        /// 命令发送面板显示错误的数量上限，与 CurrentErrorCount 配合
        /// </summary>
        private const int MAX_ERROR_COUNT = 2;

        #region "属性"

        private static List<Plugin> DataCheckPlugins = Plugin.GePlugins();

        /// <summary>
        /// 避免连续发送命令时一直弹窗报错
        /// </summary>
        private int CurrentErrorCount { get; set; } = 0;


        /// <summary>
        /// 保存收到数据并处理
        /// </summary>
        private StringBuilder RecvBuffer { get; set; } = new StringBuilder();

        private DateTime HexRecvTime { get; set; }

        private AutoResetEvent ResetEvent { get; set; }

        public TextEditor TextEditor { get; set; }

        public double CurrentCharSize { get; set; }

        public Queue<ResultCheck> ResultChecks { get; set; }

        public string SaveFileName { get; set; }

        public int FragCount { get; set; }

        private bool _RunningCommands;
        public bool RunningCommands {
            get { return _RunningCommands; }
            set { _RunningCommands = value; RaisePropertyChanged(); }
        }

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
            set { _SerialPort = value; _SerialPort.DataReceived += OnReceive; RaisePropertyChanged(); }
        }

        private bool _AddNewLineWhenWrite = true;
        public bool AddNewLineWhenWrite {
            get { return _AddNewLineWhenWrite; }
            set {
                _AddNewLineWhenWrite = value;
                RaisePropertyChanged();
                RefreshSendHexValue();
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
                    RefreshSendHexValue();

                Logger.Info($"set SendHex: {value}");
            }
        }

        private bool _RecvShowHex;
        public bool RecvShowHex {
            get { return _RecvShowHex; }
            set {
                _RecvShowHex = value;
                RaisePropertyChanged();
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
                RefreshSendHexValue();
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


        private string _Detail = "";

        /// <summary>
        /// 串口详情
        /// </summary>
        public string Detail {
            get { return _Detail; }
            set { _Detail = value; RaisePropertyChanged(); }
        }

        private PortType _PortType;

        /// <summary>
        /// 串口详情
        /// </summary>
        public PortType PortType {
            get { return _PortType; }
            set { _PortType = value; RaisePropertyChanged(); }
        }

        private bool _EnabledFilter;
        public bool EnabledFilter {
            get { return _EnabledFilter; }
            set {
                _EnabledFilter = value;
                RaisePropertyChanged();
            }
        }

        private DateTime _ConnectTime;
        public DateTime ConnectTime {
            get { return _ConnectTime; }
            set {
                _ConnectTime = value;
            }
        }



        private bool _Pinned;
        public bool Pinned {
            get { return _Pinned; }
            set {
                _Pinned = value;
                RaisePropertyChanged();
            }
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

        private bool _IsClose = false;

        #endregion

        public void TextBox_TextChanged(object sender, EventArgs e)
        {
            TextEditor textEditor = sender as TextEditor;
            textEditor?.ScrollToEnd();
        }

        public void ClearData()
        {

        }

        #region "收数据处理"


        private void OnReceive(object sender, SerialDataReceivedEventArgs e)
        {
            ResetEvent.Set();
        }


        /// <summary>
        /// 读串口的 16 进制数据
        /// <para>16 进制的读处理参考：<see href="https://github.com/chenxuuu/llcom">llcom</see></para>
        /// <para>参考1：<see href="https://stackoverflow.com/a/10882588">stackoverflow</see></para>
        /// <para>参考2：<see href="https://learn.microsoft.com/en-us/dotnet/api/system.io.ports.serialport.datareceived">microsoft</see></para>
        /// <para>参考3：<see href="https://stackoverflow.com/questions/46882774/how-to-deal-with-c-sharp-serialport-read-and-write-data-perfectly">deal-with-c-sharp-serialport-read</see></para>
        /// </summary>
        public void ReadTask()
        {
            ResetEvent.Reset();
            while (true) {
                ResetEvent.WaitOne();
                if (_IsClose)
                    break;
                List<byte> allData = new List<byte>();
                while (true) {
                    if (SerialPort == null || !SerialPort.IsOpen)
                        break;
                    try {
                        int len = SerialPort.BytesToRead;
                        if (len == 0)
                            break;
                        byte[] buffer = new byte[len];
                        HexRecvTime = DateTime.Now;
                        SerialPort.Read(buffer, 0, len);
                        if (buffer.Length == 0)
                            break;
                        allData.AddRange(buffer);
                    } catch {
                        break;
                    }

                    if (allData.Count > MAX_READ_LENGTH)
                        break;

                    Thread.Sleep(SerialPort.SubcontractingTimeoutValue); // 不能设置过小，也不能过大，否则一次读取的数据不完整

                }
                if (allData.Count > 0) {
                    App.GetDispatcher()?.Invoke(() => {
                        RX += allData.Count;
                        if (RecvShowHex) {
                            // HEX 模式
                            SaveHex(allData.ToArray(), HexRecvTime.ToLocalDate());
                        } else {
                            // STR 模式
                            SaveData(SerialPort.Encoding.GetString(allData.ToArray()), HexRecvTime.ToLocalDate());
                        }

                    });
                }
            }
        }
        #endregion

        public byte[] CalcHexValue(string data)
        {
            if (string.IsNullOrEmpty(data))
                return null;
            byte[] bytes = TransformHelper.ParseHexString(data);
            // 添加校验位
            if (SerialPort != null &&
                SerialPort.DataCheck is DataCheck dataCheck &&
                dataCheck.Enabled &&
                DataCheckPlugins.Count > 0 &&
                dataCheck.SelectedIndex is int index &&
                index >= 0 && index < DataCheckPlugins.Count - 1) {
                Plugin plugin = DataCheckPlugins[index];

                int len = bytes.Length;

                int start = 0;
                int end = len - 1;
                int insert = len;
                if (dataCheck.UseCustom) {
                    start = dataCheck.CustomStart;
                    end = dataCheck.CustomEnd;
                    insert = dataCheck.CustomInsert;
                }

                // start 小于 0
                if (start < 0) {
                    start += len;
                }

                // end 小于 0
                if (end < 0) {
                    end += len;
                }

                // insert 小于 0
                if (insert < 0) {
                    insert += len + 1;
                }

                if (start < 0 || end < 0 || insert < 0 ||
                    start >= len || end >= len || insert > len ||
                    (start != 0 && end != 0 && start >= end)) {
                    return bytes;
                }

                byte[] dataToCalc = new ArraySegment<byte>(bytes, start, end - start + 1).ToArray();
                byte[] checkByte = null;
                try {
                    checkByte = plugin.CheckData(dataToCalc);
                } catch (Exception e) {
                    Logger.Error(e);
                }
                if (checkByte != null) {
                    byte[] newData = new byte[bytes.Length + checkByte.Length];
                    if (insert == 0) {
                        checkByte.CopyTo(newData, 0);
                        bytes.CopyTo(newData, checkByte.Length);
                    } else {
                        // 插入中间
                        List<byte> list = bytes.ToList();
                        list.InsertRange(insert, checkByte);
                        newData = list.ToArray();
                    }
                    return newData;
                }
            }
            return bytes;
        }

        public void RefreshSendHexValue()
        {
            if (SendHex) {
                string data = WriteData;
                if (AddNewLineWhenWrite)
                    data += ConfigManager.Settings.NewLineText;
                byte[] bytes = CalcHexValue(data);
                string str = TransformHelper.FormatHexString(TransformHelper.ByteArrayToHexString(bytes), "", " ");
                SendHexValue = $"{LangManager.GetValueByKey("WillSend")}：{str}";
            }
        }

        private string GetFileNameByFormat(string format)
        {
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
                    case "%MM":
                        result = result.Replace(item, ConnectTime.Month.ToString().PadLeft(2, '0'));
                        break;
                    case "%DD":
                        result = result.Replace(item, ConnectTime.Day.ToString().PadLeft(2, '0'));
                        break;
                    case "%hh":
                        result = result.Replace(item, ConnectTime.Hour.ToString().PadLeft(2, '0'));
                        break;
                    case "%mm":
                        result = result.Replace(item, ConnectTime.Minute.ToString().PadLeft(2, '0'));
                        break;
                    case "%ss":
                        result = result.Replace(item, ConnectTime.Second.ToString().PadLeft(2, '0'));
                        break;
                    case "%fff":
                        result = result.Replace(item, ConnectTime.Millisecond.ToString().PadLeft(3, '0'));
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


        public string GetLogDir()
        {
            string dirName = ConfigManager.CommonSettings.LogSaveDir;
            if (string.IsNullOrEmpty(dirName))
                dirName = CommonSettings.DEFAULT_LOG_SAVE_DIR;
            string logDir = GetDirByFormat(dirName);
            if (string.IsNullOrEmpty(logDir))
                logDir = CommonSettings.DEFAULT_LOG_SAVE_DIR;
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            return logDir;
        }

        public string GetCustomFileName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return GetDefaultFileName();
            string newName = name;
            if (!newName.EndsWith(SuperCom.Log.Logger.DEFAULT_LOG_EXTENSION))
                newName += SuperCom.Log.Logger.DEFAULT_LOG_EXTENSION;


            return Path.Combine(GetLogDir(), newName);
        }


        /// <summary>
        /// 根据时间戳和自定义文件名组合方式保存的目标文件名
        /// </summary>
        /// <returns></returns>
        public string GetDefaultFileName()
        {
            string format = ConfigManager.CommonSettings.LogNameFormat;
            if (string.IsNullOrEmpty(format))
                format = CommonSettings.DEFAULT_LOG_NAME_FORMAT;
            string name = GetFileNameByFormat(format);
            if (string.IsNullOrEmpty(name))
                name = GetFileNameByFormat(CommonSettings.DEFAULT_LOG_NAME_FORMAT);
            string logDir = GetLogDir();
            return Path.Combine(logDir, name + SuperCom.Log.Logger.DEFAULT_LOG_EXTENSION);
        }


        /// <summary>
        /// 过滤器
        /// </summary>
        /// <param name="value"></param>
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

        /// <summary>
        /// 日志分片
        /// </summary>
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
                    SaveFileName = GetDefaultFileName();
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


        public void SaveData(string inputData, string now)
        {
            string value = inputData.Replace("\0", "\\0"); // 业务侧会打印很多 \0，需要转成 \\0 才能在文本框上显示
            int valueLen = value.Length;
            if (AddTimeStamp) {
                // 遍历字符串
                RecvBuffer.Clear();
                RecvBuffer.Append($"[{now}] ");
                // 一次遍历效率最高，使用 indexof 还额外多遍历几次
                char c;
                for (int i = 0; i < valueLen; i++) {
                    c = value[i];
                    if (c == '\r' && i < valueLen - 1 && value[i + 1] == '\n') {
                        RecvBuffer.Append($"\r\n[{now}] ");
                        i++;//跳过 \n
                        continue;
                    } else if (c == '\r' || c == '\n') {
                        RecvBuffer.Append($"\r\n[{now}] ");
                        continue;
                    } else {
                        RecvBuffer.Append(c);
                    }
                }
                value = RecvBuffer.ToString();
                value += Environment.NewLine;
            }
            CurrentCharSize += Encoding.UTF8.GetByteCount(value);
            //MonitorLine(value);
            FilterLine(value);
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
            App.Logger.Debug($"{LangManager.GetValueByKey("SaveData")}：{bytes.Length} B");
            string value =
                TransformHelper.FormatHexString(TransformHelper.ByteArrayToHexString(bytes), "", " ");
            SaveData(value, now);
        }

        public override void Init()
        {
            throw new NotImplementedException();
        }

        public PortTabItem(string name, bool connected)
        {
            Name = name;
            Connected = connected;
            Setting = new PortSetting();
            ResetEvent = new AutoResetEvent(false);
            //Task.Run(() => {
            //    while (true) {
            //        ReadTask();
            //    }
            //});
        }

        /// <summary>
        /// 异步超时发送
        /// </summary>
        /// <param name="port"></param>
        /// <param name="portTabItem"></param>
        /// <param name="value"></param>
        /// <param name="saveToHistory"></param>
        /// <returns></returns>
        public bool SendCommand(string value, bool saveToHistory = true)
        {
            if (SerialPort == null)
                return false;
            if (AddNewLineWhenWrite) {
                value += ConfigManager.Settings.NewLineText;
            }

            SerialPort port = SerialPort;

            try {
                if (SendHex) {
                    int len = SendHexData(value);
                    TX += len;
                } else {
                    port.Write(value);
                    if (ConfigManager.Settings.EnabledSendPrefix)
                    {
                        SaveData($"{ConfigManager.Settings.SendPrefix}{value}", DateHelper.Now());
                    }
                    else { 
                        SaveData($"{value}", DateHelper.Now());
                    }

                    TX += Encoding.UTF8.GetByteCount(value);
                    Logger.Info($"send data, port name: {Name}, hex: {SendHex}, TX: {TX}, value: {value}");

                }

                // todo 保存到发送历史
                //if (saveToHistory)
                //{
                //    vieModel.SendHistory.Add(value.Trim());
                //    vieModel.SaveSendHistory();
                //}
                //vieModel.StatusText = $"【发送命令】=>{WriteData}";
                CurrentErrorCount = 0;
                return true;
            } catch (Exception ex) {
                CurrentErrorCount++;
                if (CurrentErrorCount <= MAX_ERROR_COUNT)
                    MessageCard.Error(ex.Message);
                return false;
            }
        }



        public int SendHexData(string value)
        {
            if (string.IsNullOrEmpty(value) || SerialPort == null)
                return 0;
            byte[] bytes = CalcHexValue(value);
            if (bytes == null || bytes.Length == 0)
                return 0;
            string str = TransformHelper.FormatHexString(TransformHelper.ByteArrayToHexString(bytes), "", " ");
            if (ConfigManager.Settings.EnabledSendPrefix)
                SaveData($"{ConfigManager.Settings.SendPrefix}{str}", DateHelper.Now());
            SerialPort.Write(bytes, 0, bytes.Length);
            Logger.Info($"send data, port name: {Name}, hex: {SendHex}, TX: {TX + bytes.Length}, value: {str}");
            return bytes.Length;
        }

        public void SendCustomCommand(string value)
        {
            // 设置固定滚屏
            if (ConfigManager.CommonSettings.FixedOnSendCommand) {
                FixedText = true;
            }
            CurrentErrorCount = 0;
            SendCommand(value, false);
        }
        /// <summary>
        /// 窗口关闭
        /// </summary>
        public void Close()
        {
            _IsClose = true;
            ResetEvent.Set();
        }
        /// <summary>
        /// 串口打开
        /// </summary>
        public void Open()
        {
            _IsClose = false;
            new Thread(ReadTask).Start();
        }
    }

    public class ResultCheck
    {
        public string Command { get; set; }
        public StringBuilder Buffer { get; set; }
    }

}
