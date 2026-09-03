using SuperUtils.Framework.ORM.Config;
using SuperUtils.WPF.VieModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SuperCom.Config.WindowConfig
{
    /// <summary>
    /// 扩展功能面板配置
    /// </summary>
    public class QuickCommandSettings : AbstractConfig
    {
        private const int DEFAULT_PANEL_WIDTH = 250;

        #region 属性

        private bool _IsPanelVisible = false;
        /// <summary>
        /// 面板是否展开
        /// </summary>
        public bool IsPanelVisible {
            get { return _IsPanelVisible; }
            set { _IsPanelVisible = value; RaisePropertyChanged(); }
        }

        private double _PanelWidth = DEFAULT_PANEL_WIDTH;
        /// <summary>
        /// 面板宽度
        /// </summary>
        public double PanelWidth {
            get { return _PanelWidth; }
            set { _PanelWidth = value; RaisePropertyChanged(); }
        }

        private string _CommandsJson = "[]";
        /// <summary>
        /// 指令列表 JSON（序列化保存）
        /// </summary>
        public string CommandsJson {
            get { return _CommandsJson; }
            set { _CommandsJson = value; RaisePropertyChanged(); }
        }

        private string _ColumnWidthsJson = "";
        /// <summary>
        /// 列宽度 JSON
        /// </summary>
        public string ColumnWidthsJson {
            get { return _ColumnWidthsJson; }
            set { _ColumnWidthsJson = value; RaisePropertyChanged(); }
        }

        #endregion

        #region 运行时数据（不保存）

        private ObservableCollection<QuickCommandItem> _Commands = new ObservableCollection<QuickCommandItem>();
        /// <summary>
        /// 指令列表（运行时）
        /// </summary>
        public ObservableCollection<QuickCommandItem> Commands {
            get { return _Commands; }
            set { _Commands = value; RaisePropertyChanged(); }
        }

        #endregion

        #region 列宽度配置

        private double _HexColumnWidth = 43;
        public double HexColumnWidth {
            get { return _HexColumnWidth; }
            set { _HexColumnWidth = value; RaisePropertyChanged(); }
        }

        private double _CommandColumnWidth = 176;
        public double CommandColumnWidth {
            get { return _CommandColumnWidth; }
            set { _CommandColumnWidth = value; RaisePropertyChanged(); }
        }

        private double _ExecuteColumnWidth = 172;
        public double ExecuteColumnWidth {
            get { return _ExecuteColumnWidth; }
            set { _ExecuteColumnWidth = value; RaisePropertyChanged(); }
        }

        private double _DelayColumnWidth = 39;
        public double DelayColumnWidth {
            get { return _DelayColumnWidth; }
            set { _DelayColumnWidth = value; RaisePropertyChanged(); }
        }

        #endregion

        private QuickCommandSettings() : base(ConfigManager.SQLITE_DATA_PATH, "WindowConfig.QuickCommandSettings")
        {
        }

        private static QuickCommandSettings _instance = null;

        public static QuickCommandSettings CreateInstance()
        {
            if (_instance == null)
                _instance = new QuickCommandSettings();
            return _instance;
        }

        public void Load()
        {
            // 加载配置
            Read();
            // 解析指令列表
            LoadCommands();
            // 解析列宽度
            LoadColumnWidths();
        }

        /// <summary>
        /// 保存所有配置
        /// </summary>
        public void SaveAll()
        {
            SaveCommands();
            Save();
        }

        #region 指令列表管理

        private void LoadCommands()
        {
            try {
                if (!string.IsNullOrEmpty(CommandsJson)) {
                    var list = Newtonsoft.Json.JsonConvert.DeserializeObject<List<QuickCommandItem>>(CommandsJson);
                    if (list != null) {
                        Commands = new ObservableCollection<QuickCommandItem>(list);
                    }
                }
            } catch {
                Commands = new ObservableCollection<QuickCommandItem>();
            }
        }

        private void SaveCommands()
        {
            try {
                CommandsJson = Newtonsoft.Json.JsonConvert.SerializeObject(Commands.ToList());
            } catch {
                CommandsJson = "[]";
            }
        }

        public void AddCommand(string command, int delay = 1000)
        {
            var item = new QuickCommandItem {
                ID = GenerateID(),
                Command = command,
                Delay = delay,
                IsHex = false,
                IsSelected = false,
                Memo = ""
            };
            Commands.Add(item);
            SaveAll();
        }

        public void RemoveCommand(long id)
        {
            var item = Commands.FirstOrDefault(c => c.ID == id);
            if (item != null) {
                Commands.Remove(item);
                SaveAll();
            }
        }

        public void UpdateCommand(QuickCommandItem item)
        {
            var existing = Commands.FirstOrDefault(c => c.ID == item.ID);
            if (existing != null) {
                existing.Command = item.Command;
                existing.Delay = item.Delay;
                existing.IsHex = item.IsHex;
                existing.IsSelected = item.IsSelected;
                existing.Memo = item.Memo;
                SaveAll();
            }
        }

        private long GenerateID()
        {
            var ids = Commands.Select(c => c.ID).ToList();
            for (long i = 0; i <= ids.Count; i++) {
                if (!ids.Contains(i))
                    return i;
            }
            return 0;
        }

        #endregion

        #region 列宽度管理

        private void LoadColumnWidths()
        {
            try {
                if (!string.IsNullOrEmpty(ColumnWidthsJson)) {
                    var widths = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, double>>(ColumnWidthsJson);
                    if (widths != null) {
                        if (widths.ContainsKey("Hex")) HexColumnWidth = widths["Hex"];
                        if (widths.ContainsKey("Command")) CommandColumnWidth = widths["Command"];
                        if (widths.ContainsKey("Execute")) ExecuteColumnWidth = widths["Execute"];
                        if (widths.ContainsKey("Delay")) DelayColumnWidth = widths["Delay"];
                    }
                }
            } catch {
                // 使用默认值
            }
        }

        private void SaveColumnWidths()
        {
            try {
                var widths = new Dictionary<string, double> {
                    { "Hex", HexColumnWidth },
                    { "Command", CommandColumnWidth },
                    { "Execute", ExecuteColumnWidth },
                    { "Delay", DelayColumnWidth }
                };
                ColumnWidthsJson = Newtonsoft.Json.JsonConvert.SerializeObject(widths);
            } catch {
                // 忽略错误
            }
        }

        #endregion

        public void TogglePanel()
        {
            IsPanelVisible = !IsPanelVisible;
            SaveAll();
        }
    }

    /// <summary>
    /// 快速命令项
    /// </summary>
    public class QuickCommandItem : ViewModelBase
    {
        private long _ID;
        public long ID {
            get { return _ID; }
            set { _ID = value; RaisePropertyChanged(); }
        }

        private string _Command = "";
        public string Command {
            get { return _Command; }
            set { _Command = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(ButtonText)); }
        }

        private string _Memo = "";
        public string Memo {
            get { return _Memo; }
            set { _Memo = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(ButtonText)); }
        }

        private bool _IsHex = false;
        public bool IsHex {
            get { return _IsHex; }
            set { _IsHex = value; RaisePropertyChanged(); }
        }

        private bool _IsSelected = false;
        public bool IsSelected {
            get { return _IsSelected; }
            set { _IsSelected = value; RaisePropertyChanged(); }
        }

        private int _Delay = 1000;
        public int Delay {
            get { return _Delay; }
            set { _Delay = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 按钮显示文字（备注或默认"执行"）
        /// </summary>
        public string ButtonText => string.IsNullOrEmpty(Memo) ? "执行" : Memo;

        public override void Init()
        {
        }
    }
}
