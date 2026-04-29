using SuperUtils.Framework.ORM.Config;
using System;
using System.IO;
using System.Text;

namespace SuperCom.Config.WindowConfig
{
    public class CmdSettings : AbstractConfig
    {
        private const string DEFAULT_OUTPUT_PATH = "";

        public CmdSettings() : base(ConfigManager.SQLITE_DATA_PATH, $"WindowConfig.CmdSettings")
        {
            FirstRun = true;
            OutputPath = DEFAULT_OUTPUT_PATH;
            DefaultDelay = 1000;
            DefaultHex = false;
        }

        private static CmdSettings _instance = null;
        public static CmdSettings CreateInstance()
        {
            if (_instance == null)
                _instance = new CmdSettings();
            return _instance;
        }

        public static CmdSettings Reset()
        {
            _instance = null;
            return CreateInstance();
        }

        private bool _FirstRun { get; set; }
        public bool FirstRun {
            get { return _FirstRun; }
            set { _FirstRun = value; RaisePropertyChanged(); }
        }

        private bool _IsProcessRunning { get; set; }
        public bool IsProcessRunning {
            get { return _IsProcessRunning; }
            set { _IsProcessRunning = value; RaisePropertyChanged(); }
        }

        private string _OutputPath { get; set; }
        /// <summary>
        /// 输出保存路径
        /// </summary>
        public string OutputPath {
            get { return _OutputPath; }
            set { _OutputPath = value; RaisePropertyChanged(); }
        }

        private int _DefaultDelay { get; set; }
        /// <summary>
        /// 新指令默认延迟（毫秒）
        /// </summary>
        public int DefaultDelay {
            get { return _DefaultDelay; }
            set { _DefaultDelay = value; RaisePropertyChanged(); }
        }

        private bool _DefaultHex { get; set; }
        /// <summary>
        /// 新指令默认是否 HEX
        /// </summary>
        public bool DefaultHex {
            get { return _DefaultHex; }
            set { _DefaultHex = value; RaisePropertyChanged(); }
        }

        private string _CommandsJson { get; set; } = "";
        /// <summary>
        /// 指令列表 JSON
        /// </summary>
        public string CommandsJson {
            get { return _CommandsJson; }
            set { _CommandsJson = value; RaisePropertyChanged(); }
        }

        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }
}
