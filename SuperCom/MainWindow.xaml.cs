
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Search;
using SuperCom.Config;
using SuperCom.Core.Telnet;
using SuperCom.Core.Utils;
using SuperCom.Entity;
using SuperCom.Entity.Enums;
using SuperCom.Upgrade;
using SuperCom.ViewModel;
using SuperCom.Windows;
using SuperControls.Style;
using SuperControls.Style.Plugin;
using SuperControls.Style.Utils;
using SuperControls.Style.Windows;
using SuperUtils.Common;
using SuperUtils.Framework.ORM.Enums;
using SuperUtils.IO;
using SuperUtils.NetWork;
using SuperUtils.Systems;
using SuperUtils.Time;
using SuperUtils.Values;
using SuperUtils.WPF.VisualTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using static SuperCom.App;

namespace SuperCom
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : BaseWindow
    {
        private const double DEFAULT_SEND_PANEL_HEIGHT = 186;
        private const int DEFAULT_PORT_OPEN_INTERVAL = 100;

        /// <summary>
        /// HEX 转换工具中最大长度
        /// </summary>
        private const int MAX_TRANSFORM_SIZE = 100000;


        /// <summary>
        /// 时间戳转换中最长时间戳长度
        /// </summary>
        private const int MAX_TIMESTAMP_LENGTH = 100;


        #region "属性"

        private SendCommand CurrentEditCommand { get; set; }

        private Window_ShortCut window_ShortCut { get; set; }
        private Window_Setting window_Setting { get; set; }
        private Window_Monitor window_Monitor { get; set; }
        private Window_TelnetServer Window_TelnetServer { get; set; }
        private Window_VirtualPort virtualPort { get; set; }
        public VieModel_Main vieModel { get; set; }

        /// <summary>
        /// 最后使用的串口排序类型
        /// </summary>
        ComPortSortType LastSortType { get; set; } = ComPortSortType.AddTime;
        /// <summary>
        /// 最后使用的串口排序方式
        /// </summary>
        bool LastSortDesc { get; set; } = false;


        /// <summary>
        /// 支持标签栏拖拽
        /// </summary>
        private FrameworkElement CurrentDragElement { get; set; }

        #endregion

        public MainWindow()
        {
            InitializeComponent();
            Init();
        }

        public void Init()
        {
            InitSqlite();
            ConfigManager.InitConfig();
            vieModel = new VieModel_Main();
            this.DataContext = vieModel;
            SetLang();
            ReadConfig();
            PathManager.Init();

            // 看门狗
            App.OnMemoryChanged += (memory) => {
                vieModel.MemoryUsed = Math.Ceiling((double)memory / 1024 / 1024);
            };

            App.OnMemoryDog += OnMemoryDog;

            colorPicker.SelectedColorChanged += (s, e) => {
                Logger.Info($"color picker set color: {colorPicker.SelectedColor}");
            };

            Logger.Info("main window init");
        }

        private async void mainWindow_ContentRendered(object sender, EventArgs e)
        {
            this.TopMenu = TopMenus;
            //AdjustWindow();
            if (ConfigManager.Main.FirstRun)
                ConfigManager.Main.FirstRun = false;
            InitThemeSelector();
            RefreshSetting();
            ReadXshdList();
            LoadDonateConfig();
            await BackupData();
            LoadFontFamily();
            InitUpgrade();
            OpenBeforePorts();
            SetBaudRateAction();
            InitNotice();
            ApplyScreenStatus();
        }

        private void OnMemoryDog()
        {
            App.GetDispatcher()?.Invoke(() => {
                if (vieModel != null && vieModel.PortTabItems != null && vieModel.PortTabItems.Count > 0) {
                    PortTabItem port = null;
                    long maxLength = 0;
                    foreach (var item in vieModel.PortTabItems) {
                        if (item.TextEditor == null || !item.Connected)
                            continue;
                        if (item.TextEditor.Text.Length > maxLength) {
                            maxLength = item.TextEditor.Text.Length;
                            port = item;
                        }
                    }
                    if (port != null && maxLength > 0) {
                        TextEditor oldTextEditor = port.TextEditor;
                        Border border = oldTextEditor.Parent as Border;
                        TextEditor newTextEditor = new TextEditor();
                        SetTextEditorConfig(ref newTextEditor, true);

                        IHighlightingDefinition syntaxHighlighting = oldTextEditor.SyntaxHighlighting;
                        double FontSize = oldTextEditor.FontSize;

                        newTextEditor.SyntaxHighlighting = syntaxHighlighting;
                        newTextEditor.FontSize = FontSize;
                        newTextEditor.Options = oldTextEditor.Options;
                        newTextEditor.ShowLineNumbers = oldTextEditor.ShowLineNumbers;
                        newTextEditor.Language = oldTextEditor.Language;
                        newTextEditor.FontFamily = oldTextEditor.FontFamily;
                        newTextEditor.Foreground = oldTextEditor.Foreground;


                        if (port.FixedText)
                            newTextEditor.TextChanged -= port.TextBox_TextChanged;
                        else
                            newTextEditor.TextChanged += port.TextBox_TextChanged;
                        port.TextEditor = newTextEditor;
                        border.Child = newTextEditor;
                        MessageCard.Warning($"{LangManager.GetValueByKey("MemLimitClearLog")}: {port.Name}");
                    }
                }
            });
        }


        public void ReadConfig()
        {
            vieModel.ComSettingList = MapperManager.ComMapper.SelectList().ToHashSet();
            if (vieModel.ComSettingList == null ||
                vieModel.ComSettingList.Count == 0 ||
                vieModel.SideComPorts == null ||
                vieModel.SideComPorts.Count == 0)
                return;

            // 设置配置
            foreach (var item in vieModel.SideComPorts) {
                ComSettings comSettings = vieModel.ComSettingList.FirstOrDefault(arg => arg.PortName.Equals(item.Name))
                    ;
                if (comSettings != null && !string.IsNullOrEmpty(comSettings.PortSetting)) {
                    try {
                        // 不知为啥，这里会弹出 System.NullReferenceException: 未将对象引用设置到对象的实例
                        item.Remark = SerialPortEx.GetRemark(comSettings.PortSetting);
                        item.Hide = SerialPortEx.GetHide(comSettings.PortSetting);
                    } catch (Exception ex) {
                        Logger.Error(ex);
                        continue;
                    }

                }
            }
            //textWrapMenuItem.IsChecked = vieModel.AutoTextWrap;
        }

        private void SetLang()
        {
            // 设置语言
            string lang = ConfigManager.Settings.CurrentLanguage;
            if (!string.IsNullOrEmpty(lang)
                && SuperControls.Style.LangManager.SupportLanguages.Contains(lang)) {
                SuperControls.Style.LangManager.SetLang(lang);
                SuperCom.Lang.LangManager.SetLang(lang);
                Logger.Debug($"{LangManager.GetValueByKey("SetLang")}：{lang}");
            }
        }

        public void RefreshSetting()
        {
            this.CloseToTaskBar = ConfigManager.CommonSettings.CloseToBar;
        }

        /// <summary>
        /// 读取自定义语法高亮
        /// </summary>
        public void ReadXshdList()
        {
            // 记录先前选定的
            Dictionary<string, long> selectDict = new Dictionary<string, long>();
            if (vieModel.PortTabItems?.Count > 0) {
                foreach (PortTabItem item in vieModel.PortTabItems) {
                    if (item.SerialPort == null)
                        continue;
                    selectDict.Add(item.Name, item.SerialPort.HighLightIndex);
                }
            }

            HighlightingManager.Instance.Clear();
            string[] xshd_list = FileHelper.TryGetAllFiles(HighLightRule.GetDirName(), "*.xshd");
            foreach (var xshdPath in xshd_list) {
                try {
                    IHighlightingDefinition customHighlighting;
                    using (Stream s = File.OpenRead(xshdPath)) {
                        if (s == null)
                            throw new InvalidOperationException("Could not find embedded resource");
                        using (XmlReader reader = new XmlTextReader(s)) {
                            customHighlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.
                                HighlightingLoader.Load(reader, HighlightingManager.Instance);
                        }
                    }
                    // 检查是否在数据库中存在
                    string name = customHighlighting.Name;
                    if (HighLightRule.DEFAULT_RULES.Contains(name) || HighLightRule.AllRules.FirstOrDefault(arg => arg.RuleName.Equals(name)) != null)
                        HighlightingManager.Instance.RegisterHighlighting(name, null, customHighlighting);
                } catch (Exception ex) {
                    MessageCard.Error(ex.Message);
                    continue;
                }

            }

            vieModel.LoadHighlightingDefinitions();


            // 恢复选中项
            if (vieModel.PortTabItems?.Count > 0) {
                foreach (PortTabItem item in vieModel.PortTabItems) {
                    if (item.SerialPort == null || !selectDict.ContainsKey(item.Name))
                        continue;
                    long idx = selectDict[item.Name];
                    if (idx >= vieModel.HighlightingDefinitions.Count)
                        idx = 0;
                    item.SerialPort.HighLightIndex = idx;
                }
            }
            Logger.Info("read high light xshd success");
        }


        private void InitSqlite()
        {
            ComSettings.InitSqlite();
            AdvancedSend.InitSqlite();
            HighLightRule.InitSqlite();
            ShortCutBinding.InitSqlite();
            VarMonitor.InitSqlite();
        }


        private void SetPortSelected(object sender, MouseButtonEventArgs e)
        {
            CanDragTabItem = false;
            if (CurrentDragElement != null)
                Mouse.Capture(CurrentDragElement, CaptureMode.None);
        }

        private void BeginDragTabItem(object sender, MouseButtonEventArgs e)
        {
            CanDragTabItem = true;
            CurrentDragElement = sender as FrameworkElement;
            Mouse.Capture(CurrentDragElement, CaptureMode.Element);

            Border border = (Border)sender;
            if (border == null || border.Tag == null)
                return;
            string portName = border.Tag.ToString();
            if (string.IsNullOrEmpty(portName) || vieModel.PortTabItems == null ||
                vieModel.PortTabItems.Count <= 0)
                return;
            SetPortTabSelected(portName);
        }

        private void Border_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!CanDragTabItem)
                return;
        }

        public void SetPortTabSelected(string portName)
        {
            if (vieModel.PortTabItems == null)
                return;
            for (int i = 0; i < vieModel.PortTabItems.Count; i++) {
                if (vieModel.PortTabItems[i].Name.Equals(portName)) {
                    vieModel.PortTabItems[i].Selected = true;
                    SetGridVisible(portName);
                } else {
                    vieModel.PortTabItems[i].Selected = false;
                }
            }
        }


        /// <summary>
        /// 设置下一个选中
        /// </summary>
        /// <param name="forward"></param>
        public void SetPortTabSelected(bool forward)
        {
            if (vieModel.PortTabItems == null || vieModel.PortTabItems.Count <= 1)
                return;
            int idx = 0;
            for (int i = 0; i < vieModel.PortTabItems.Count; i++) {
                if (vieModel.PortTabItems[i].Selected) {
                    idx = i;
                    break;
                }
            }
            if (forward)
                idx++;
            else
                idx--;
            if (idx < 0)
                idx = vieModel.PortTabItems.Count - 1;
            if (idx >= vieModel.PortTabItems.Count)
                idx = 0;
            SetPortTabSelected(vieModel.PortTabItems[idx].Name);
        }

        private void SetGridVisible(string portName)
        {
            Logger.Debug(portName);
            if (string.IsNullOrEmpty(portName))
                return;
            for (int i = 0; i < itemsControl.Items.Count; i++) {
                ContentPresenter presenter = (ContentPresenter)itemsControl.ItemContainerGenerator.ContainerFromItem(itemsControl.Items[i]);
                if (presenter == null) {
                    Logger.Debug($"presenter[{i}] is null");
                    continue;
                }
                Grid grid = VisualHelper.FindElementByName<Grid>(presenter, "baseGrid");
                if (grid == null || grid.Tag == null) {
                    Logger.Debug($"presenter[{i}] baseGrid is null");
                    continue;
                }

                string name = grid.Tag.ToString();
                if (portName.Equals(name))
                    grid.Visibility = Visibility.Visible;
                else
                    grid.Visibility = Visibility.Hidden;
            }
        }

        private void PortTab_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scrollViewer = sender as ScrollViewer;
            scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - e.Delta);
            e.Handled = true;
        }

        private void PinTabItem(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement ele = sender as FrameworkElement;
            Grid grid = (ele.Parent as FrameworkElement).Parent as Grid;
            Border baseBorder = grid.Parent as Border;
            string portName = baseBorder.Tag.ToString();
            PortTabItem portTabItem = vieModel.PortTabItems.FirstOrDefault(arg => arg.Name.Equals(portName));
            PinPort(portTabItem);
        }

        private void CloseTabItem(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement ele = sender as FrameworkElement;
            Grid grid = (ele.Parent as FrameworkElement).Parent as Grid;
            Border baseBorder = grid.Parent as Border;
            string portName = baseBorder.Tag.ToString();
            ClosePortTabItemByName(portName, null);
        }

        private async void ClosePortTabItemByName(string portName, Button button = null)
        {
            if (string.IsNullOrEmpty(portName) || vieModel.PortTabItems?.Count <= 0)
                return;
            await RemovePortTabItem(portName, button);
            // 默认选中 0
            if (vieModel.PortTabItems.Count > 0)
                SetPortTabSelected(vieModel.PortTabItems[0].Name);
        }

        private async Task<bool> RemovePortTabItem(string portName, Button button = null)
        {
            if (vieModel.PortTabItems == null || string.IsNullOrEmpty(portName))
                return false;

            int idx = -1;
            try {
                for (int i = 0; idx < vieModel.PortTabItems.Count; i++) {
                    if (portName.Equals(vieModel.PortTabItems[i].Name)) {
                        idx = i;
                        break;
                    }
                }

                if (idx >= 0 && idx < vieModel.PortTabItems.Count) {
                    if (button != null)
                        button.IsEnabled = false;
                    bool success = await ClosePort(portName);
                    if (success) {
                        vieModel.PortTabItems[idx].Pinned = false;
                        SavePinnedByName(portName, false);
                        SaveComSettings();
                        vieModel.PortTabItems.RemoveAt(idx);
                    }
                    if (button != null)
                        button.IsEnabled = true;
                }
                return true;
            } catch (Exception ex) {
                MessageNotify.Error(ex.Message);
                Logger?.Error(ex);
                return false;
            }

        }

        private async Task<bool> RemovePortsByName(List<string> portNames)
        {
            if (portNames == null || portNames.Count == 0)
                return true;
            SaveComSettings();
            List<string> toRemoved = new List<string>();
            bool success = false;
            foreach (var name in portNames) {
                vieModel.DoingWorkMsg = $"{LangManager.GetValueByKey("ClosePort")}: {name}";
                success = await ClosePort(name);
                if (success)
                    toRemoved.Add(name);
            }
            // 移除 item
            foreach (var item in toRemoved) {
                if (vieModel.PortTabItems.FirstOrDefault(arg => arg.Name.Equals(item))
                    is PortTabItem portTabItem)
                    vieModel.PortTabItems.Remove(portTabItem);
            }
            return true;
        }

        /// <summary>
        /// 恢复侧边栏串口的配置信息
        /// </summary>
        /// <param name="sideComPorts"></param>
        private void RetainSidePortValue(List<SideComPort> sideComPorts)
        {
            if (sideComPorts == null || vieModel.SideComPorts == null)
                return;
            int count = vieModel.SideComPorts.Count;
            for (int i = 0; i < count; i++) {
                string portName = vieModel.SideComPorts[i].Name;
                if (string.IsNullOrEmpty(portName))
                    continue;
                SideComPort sideComPort = sideComPorts.FirstOrDefault(arg => portName.Equals(arg.Name));
                if (sideComPort == null)
                    continue;
                vieModel.SideComPorts[i] = sideComPort;
                ComSettings comSettings = vieModel.ComSettingList.FirstOrDefault(arg => portName.Equals(arg.PortName));
                if (comSettings != null && !string.IsNullOrEmpty(comSettings.PortSetting)) {
                    vieModel.SideComPorts[i].Remark = SerialPortEx.GetRemark(comSettings.PortSetting);
                    vieModel.SideComPorts[i].Hide = SerialPortEx.GetHide(comSettings.PortSetting);
                    Logger.Info($"[{i + 1}/{count}]retain side com port: {portName}, remark: {vieModel.SideComPorts[i].Remark}, hide: {vieModel.SideComPorts[i].Hide}");
                }
            }
        }

        private async void ConnectPort(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button) || button.Tag == null || button.Content == null)
                return;
            button.IsEnabled = false;
            string content = button.Content.ToString();
            string portName = button.Tag.ToString();
            SideComPort sideComPort = vieModel.SideComPorts.FirstOrDefault(arg => arg.Name.Equals(portName));
            if (sideComPort == null) {
                MessageNotify.Error($"{LangManager.GetValueByKey("OpenPortFailed")}: {portName}");
                return;
            }

            if (LangManager.GetValueByKey("Connect").Equals(content))
                await OpenPort(sideComPort);
            else
                await ClosePort(portName);
            button.IsEnabled = true;
        }

        private async Task<bool> OpenPort(SideComPort sideComPort, bool connect = true)
        {
            if (sideComPort == null || string.IsNullOrEmpty(sideComPort.Name))
                return false;
            string portName = sideComPort.Name;
            await OpenPortTabItem(portName, connect);
            if (vieModel.PortTabItems == null)
                return false;
            PortTabItem portTabItem = vieModel.PortTabItems.FirstOrDefault(arg => arg.Name.Equals(portName));
            if (portTabItem == null) {
                MessageCard.Error($"{LangManager.GetValueByKey("OpenPortFailed")}: {portName}");
                return false;
            }

            await Task.Delay(DEFAULT_PORT_OPEN_INTERVAL);
            TextEditor textEditor = portTabItem.TextEditor;
            if (textEditor == null) {
                textEditor = FindTextBoxByPortName(portName);
                SetTextEditorConfig(ref textEditor);

                //textEditor.TextArea.TextView.LineTransformers.Add(new AnsiColorizer());
                //textEditor.TextArea.TextView.LineTransformers.Add(new AnsiOctalColorizer());

                textEditor.TextChanged += portTabItem.TextBox_TextChanged;
                portTabItem.TextEditor = textEditor;
                // 设置语法高亮
                int idx = (int)portTabItem.SerialPort.HighLightIndex;
                if (vieModel.HighlightingDefinitions != null && idx < vieModel.HighlightingDefinitions.Count && idx >= 0)
                    portTabItem.TextEditor.SyntaxHighlighting = vieModel.HighlightingDefinitions[idx];
                Logger.Debug($"set HighLightIndex = {idx}");
            }
            // 加载监视器
            //portTabItem.VarMonitors = new System.Collections.ObjectModel.ObservableCollection<VarMonitor>();
            //foreach (var item in vieModel.GetVarMonitorByPortName(portName))
            //{
            //    Logger.Debug($"add  var monitor: {item.Name}");
            //    portTabItem.VarMonitors.Add(item);
            //}


            // 搜索框
            sideComPort.PortTabItem = portTabItem;
            sideComPort.PortTabItem.RX = 0;
            sideComPort.PortTabItem.TX = 0;
            sideComPort.PortTabItem.CurrentCharSize = 0;
            sideComPort.PortTabItem.FragCount = 0;


            if (!connect)
                return true;

            await Task.Run(() => {
                try {
                    SerialPortEx serialPort = portTabItem.SerialPort;
                    if (!serialPort.IsOpen) {
                        //serialPort.WriteTimeout = CustomSerialPort.WRITE_TIME_OUT;
                        //serialPort.ReadTimeout = CustomSerialPort.READ_TIME_OUT;
                        serialPort.PrintSetting();
                        serialPort.Open();
                        // 打开后启动对应的过滤器线程
                        //portTabItem.StartFilterTask();
                        //portTabItem.StartMonitorTask();
                        portTabItem.ConnectTime = DateTime.Now;
                        portTabItem.SaveFileName = portTabItem.GetDefaultFileName();
                        SetPortConnectStatus(portName, true);
                        portTabItem.Open();
                    }
                } catch (Exception ex) {
                    Dispatcher.Invoke(() => {
                        string msg = $"{LangManager.GetValueByKey("OpenPortFailed")}: {portName} => {ex.Message}";
                        MessageCard.Error(msg);
                        vieModel.StatusText = msg;
                        RemovePortTabItem(portName);
                    });
                    SetPortConnectStatus(portName, false);
                }
            });
            Logger.Info($"success open port：{portName}");
            return true;
        }



        private void SetTextEditorConfig(ref TextEditor textEditor, bool createInCode = false)
        {
            if (createInCode) {
                // 恢复绑定
                textEditor.Background = Brushes.Transparent;
                textEditor.BorderThickness = new Thickness(0);

                Binding binding1 = new Binding();
                binding1.Source = vieModel.PortTabItems;
                binding1.Path = new PropertyPath("SerialPort.TextFontSize");
                binding1.Mode = BindingMode.TwoWay;
                binding1.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                BindingOperations.SetBinding(textEditor, TextEditor.FontSizeProperty, binding1);


                Binding binding2 = new Binding();
                binding2.Source = ConfigManager.Main;
                binding2.Path = new PropertyPath("AutoWrap");
                binding2.Mode = BindingMode.OneWay;
                BindingOperations.SetBinding(textEditor, TextEditor.WordWrapProperty, binding2);


                textEditor.SetResourceReference(TextEditor.ForegroundProperty, "Window.Foreground");
                textEditor.IsReadOnly = true;
                textEditor.GotFocus += textBox_GotFocus;
                textEditor.LostFocus += textBox_LostFocus;
            }

            textEditor.Name = "textEditor";
            textEditor.ContextMenu = this.Resources["TextEditorContextMenu"] as ContextMenu;
            TextEditorOptions textEditorOptions = new TextEditorOptions();
            textEditorOptions.HighlightCurrentLine = ConfigManager.Main.HighlightCurrentLine;
            textEditorOptions.ShowEndOfLine = ConfigManager.Main.ShowEndOfLine;
            textEditorOptions.ShowSpaces = ConfigManager.Main.ShowSpaces;
            textEditorOptions.ShowTabs = ConfigManager.Main.ShowTabs;
            textEditor.Options = textEditorOptions;
            textEditor.ShowLineNumbers = ConfigManager.Main.ShowLineNumbers;
            textEditor.Language = XmlLanguage.GetLanguage(VisualHelper.ZH_CN);
            // 字体
            textEditor.FontFamily = new FontFamily(ConfigManager.Main.TextFontName);
            // 颜色
            if (!string.IsNullOrEmpty(ConfigManager.Main.TextForeground)) {
                RGB rGB = ColorHelper.HexToRgb(new HEX(ConfigManager.Main.TextForeground));
                textEditor.Foreground = new SolidColorBrush(Color.FromRgb(rGB.R, rGB.G, rGB.B));
            }

            SearchPanel.Install(textEditor);
        }

        private async Task<bool> ClosePort(string portName)
        {
            if (vieModel.PortTabItems == null || string.IsNullOrEmpty(portName))
                return false;
            PortTabItem portTabItem = vieModel.PortTabItems.FirstOrDefault(arg => arg.Name.Equals(portName));
            if (portTabItem == null)
                return false;
            portTabItem.Close();
            SerialPortEx serialPort = portTabItem.SerialPort;
            if (serialPort != null) {
                bool success = await AsyncClosePort(serialPort);
                Logger.Info($"close port：{portName} ret: {success}");
                if (success) {
                    //portTabItem.StopFilterTask();
                    //portTabItem.StopMonitorTask();
                    return SetPortConnectStatus(portName, false);
                } else {
                    MessageNotify.Error($"{LangManager.GetValueByKey("ClosePortTimeout")}: {serialPort.PortName}");
                    return false;
                }
            } else {
                return true;
            }
        }

        public async Task<bool> AsyncClosePort(SerialPortEx serialPort)
        {
            try {
                return await Task.Run(() => {
                    serialPort.Close();
                    serialPort.Dispose();
                    return true;
                }).TimeoutAfter(TimeSpan.FromSeconds(PortSetting.CLOSE_TIME_OUT));
            } catch (TimeoutException ex) {
                App.Logger.Error(ex.Message);
            } catch (Exception ex) {
                MessageNotify.Error(ex.Message);
            }
            return false;
        }

        private bool SetPortConnectStatus(string portName, bool status)
        {
            try {
                if (vieModel.PortTabItems != null && vieModel.PortTabItems.Count > 0) {
                    foreach (PortTabItem item in vieModel.PortTabItems) {
                        if (item != null && item.Name.Equals(portName)) {
                            item.Connected = status;
                            break;
                        }
                    }
                }

                if (vieModel.SideComPorts != null && vieModel.SideComPorts.Count > 0) {
                    foreach (SideComPort item in vieModel.SideComPorts) {
                        if (item != null && item.Name.Equals(portName)) {
                            item.Connected = status;
                            break;
                        }
                    }
                }
            } catch (Exception ex) {
                MessageCard.Error(ex.Message);
            }

            return true;
        }




        private async Task<bool> OpenPortTabItem(string portName, bool connect)
        {
            // 打开窗口
            if (vieModel.PortTabItems == null)
                vieModel.PortTabItems = new System.Collections.ObjectModel.ObservableCollection<PortTabItem>();

            bool existed = false;
            PortTabItem portTabItem = null;
            for (int i = 0; i < vieModel.PortTabItems.Count; i++) {
                if (vieModel.PortTabItems[i].Name.Equals(portName)) {
                    vieModel.PortTabItems[i].Selected = true;
                    SetGridVisible(portName);
                    existed = true;
                    portTabItem = vieModel.PortTabItems[i];
                } else {
                    vieModel.PortTabItems[i].Selected = false;
                }
            }

            if (!existed) {
                portTabItem = new PortTabItem(portName, connect);
                if (vieModel.SideComPorts != null &&
                    vieModel.SideComPorts.FirstOrDefault(arg => arg.Name.Equals(portName)) is SideComPort p &&
                    p.Detail is string detail) {
                    portTabItem.Detail = detail;
                }
                portTabItem.Setting = PortSetting.GetDefaultSetting();

                if (portTabItem.SerialPort == null)
                    portTabItem.SerialPort = new SerialPortEx(portName);

                // 从配置里读取
                ComSettings comSettings = vieModel.ComSettingList.FirstOrDefault(arg => arg.PortName.Equals(portName));
                if (comSettings != null) {
                    portTabItem.WriteData = comSettings.WriteData;
                    portTabItem.AddTimeStamp = comSettings.AddTimeStamp;
                    portTabItem.AddNewLineWhenWrite = comSettings.AddNewLineWhenWrite;
                    portTabItem.SendHex = comSettings.SendHex;
                    portTabItem.RecvShowHex = comSettings.RecvShowHex;
                    portTabItem.EnabledFilter = comSettings.EnabledFilter;
                    portTabItem.EnabledMonitor = comSettings.EnabledMonitor;
                    portTabItem.SerialPort.SetPortSettingByJson(comSettings.PortSetting);
                    portTabItem.Remark = portTabItem.SerialPort.Remark;
                    portTabItem.Pinned = portTabItem.SerialPort.Pinned;
                }
                portTabItem.Selected = true;
                vieModel.PortTabItems.Add(portTabItem);

                await Task.Run(async () => {
                    await Task.Delay(500);
                    Dispatcher.Invoke(() => {
                        SetComboboxStatus();
                    });
                });
                SetPortTabSelected(portName);
            }
            ScrollIntoView(portTabItem);
            return true;
        }

        public void ScrollIntoView(PortTabItem portTabItem)
        {
            if (portTabItem == null)
                return;
            var container = tabItemsControl.ItemContainerGenerator.ContainerFromItem(portTabItem) as FrameworkElement;
            if (container != null)
                container.BringIntoView();
        }

        private async void Grid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) {
                Grid grid = sender as Grid;
                if (grid == null || grid.Tag == null)
                    return;
                string portName = grid.Tag.ToString();
                await OpenPortTabItem(portName, false);
            }
        }

        private void ShowAbout(object sender, RoutedEventArgs e)
        {
            Dialog_About about = new Dialog_About();
            string local = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            local = local.Substring(0, local.Length - ".0.0".Length);
            about.AppName = ConfigManager.APP_NAME;
            about.AppSubName = ConfigManager.APP_SUB_NAME;
            about.Version = local;
            about.ReleaseDate = ConfigManager.RELEASE_DATE;
            about.Author = UrlManager.AUTHOR;
            about.License = UrlManager.LICENSE;
            about.GithubUrl = UrlManager.GITHUB_URL;
            about.WebUrl = UrlManager.WEB_URL;
            about.JoinGroupUrl = UrlManager.JOIN_GROUP_URL;
            about.Image = SuperUtils.Media.ImageHelper
                .ImageFromUri("pack://application:,,,/SuperCom;Component/Resources/Ico/ICON_256.png");
            about.ShowDialog();
        }

        private void OpenContextMenu(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == null || button.ContextMenu == null)
                return;
            button.ContextMenu.IsOpen = true;
        }


        private void Border_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) {
                Border border = sender as Border;
                TextEditor textEditor = border.Child as TextEditor;
                double fontSize = textEditor.FontSize;
                if (e.Delta > 0) {
                    fontSize++;
                } else {
                    fontSize--;
                }
                if (fontSize > PortSetting.MAX_FONTSIZE)
                    fontSize = PortSetting.MAX_FONTSIZE;
                if (fontSize < PortSetting.MIN_FONTSIZE)
                    fontSize = PortSetting.MIN_FONTSIZE;

                textEditor.FontSize = fontSize;
                e.Handled = true;
            }
        }

        private TextEditor FindTextBox(Grid rootGrid)
        {
            if (rootGrid == null)
                return null;
            Border border = rootGrid.Children.OfType<Border>().FirstOrDefault();
            if (border != null && border.Child is TextEditor textEditor) {
                return textEditor;
            }
            return null;
        }

        private TextEditor FindTextBoxByPortName(string portName)
        {
            if (string.IsNullOrEmpty(portName))
                return null;
            for (int i = 0; i < itemsControl.Items.Count; i++) {
                ContentPresenter presenter = (ContentPresenter)itemsControl.ItemContainerGenerator.ContainerFromItem(itemsControl.Items[i]);
                if (presenter == null)
                    continue;
                Grid grid = VisualHelper.FindElementByName<Grid>(presenter, "rootGrid");
                if (grid != null && grid.Tag != null && portName.Equals(grid.Tag.ToString())
                    && grid.FindName("textEditor") is TextEditor textEditor)
                    return textEditor;
            }
            return null;
        }

        private void ClearData(object sender, RoutedEventArgs e)
        {
            StackPanel stackPanel = (sender as Button).Parent as StackPanel;
            if (stackPanel != null && stackPanel.Parent is Border border && border.Parent is Grid rootGrid) {
                if (rootGrid.Tag == null)
                    return;
                string portName = rootGrid.Tag.ToString();
                FindTextBox(rootGrid)?.Clear();
                PortTabItem portTabItem = vieModel.PortTabItems.FirstOrDefault(arg => arg.Name.Equals(portName));
                if (portTabItem != null) {
                    portTabItem.ClearData();
                    portTabItem.RX = portTabItem.TX = 0;
                    Logger.Info($"clear data: {portName}");
                }
            }
        }

        private void OpenPath(object sender, RoutedEventArgs e)
        {

            PortTabItem portTabItem = GetPortItem(sender as FrameworkElement);
            if (portTabItem != null) {
                Logger.Info($"open log dir, portName: {portTabItem.Name}");
                string fileName = portTabItem.SaveFileName;
                if (File.Exists(fileName)) {
                    FileHelper.TryOpenSelectPath(fileName);
                    if (portTabItem.FragCount > 0)
                        MessageNotify.Info($"{LangManager.GetValueByKey("LogFragWithCount")} {portTabItem.FragCount}");
                } else {
                    MessageNotify.Warning($"{LangManager.GetValueByKey("CurrentNoLog")}");
                }

            }

        }

        private PortTabItem GetPortItem(FrameworkElement element)
        {
            StackPanel stackPanel = element.Parent as StackPanel;
            if (stackPanel != null && stackPanel.Parent is Border border && border.Parent is Grid rootGrid) {
                if (rootGrid.Tag == null)
                    return null;
                string portName = rootGrid.Tag.ToString();
                return vieModel.PortTabItems.FirstOrDefault(arg => arg.Name.Equals(portName));
            }
            return null;
        }

        private void SendCommand(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null && button.Tag != null) {
                string portName = button.Tag.ToString();
                if (string.IsNullOrEmpty(portName))
                    return;

                Logger.Info($"click send command, port name: {portName}");

                SendCommand(portName);
                if (ConfigManager.CommonSettings.FixedOnSendCommand) {
                    PortTabItem portTabItem = vieModel.PortTabItems.FirstOrDefault(arg => arg.Name.Equals(portName));
                    if (portTabItem != null)
                        portTabItem.FixedText = true;
                }
            }
        }


        private (ToggleButton, TextEditor) FindToggleButtonByBaseGrid(Grid baseGrid)
        {
            Grid rooGrid = baseGrid.Children.OfType<Grid>().FirstOrDefault();
            Border firstBorder = rooGrid.Children.OfType<Border>().FirstOrDefault();
            Border lastBorder = rooGrid.Children.OfType<Border>().LastOrDefault();
            ToggleButton toggleButton = (lastBorder.Child as StackPanel).Children.OfType<ToggleButton>().FirstOrDefault();
            return (toggleButton, firstBorder.Child as TextEditor);
        }

        public void SendCommand(string portName)
        {
            SideComPort serialComPort = vieModel.SideComPorts.FirstOrDefault(arg => arg.Name.Equals(portName));
            if (serialComPort == null || serialComPort.PortTabItem == null || serialComPort.PortTabItem.SerialPort == null) {
                MessageCard.Error($"{LangManager.GetValueByKey("OpenPortFailed")}: {portName}");
                return;
            }
            PortTabItem portTabItem = vieModel.PortTabItems.FirstOrDefault(arg => arg.Name.Equals(portName));
            if (portTabItem == null)
                return;

            string value = portTabItem.WriteData;
            portTabItem.SendCommand(value);
        }


        private string GetPortName(FrameworkElement element)
        {
            if (element == null)
                return null;
            StackPanel stackPanel = element.Parent as StackPanel;
            if (stackPanel != null && stackPanel.Parent is Border border) {
                if (border.Tag != null) {
                    return border.Tag.ToString();
                }
            }
            return null;
        }

        private bool SetNewSaveFileName(string portName)
        {
            PortTabItem portTabItem = vieModel.PortTabItems.FirstOrDefault(arg => arg.Name.Equals(portName));
            if (portTabItem == null)
                return false;

            portTabItem.ConnectTime = DateTime.Now;
            string defaultName = portTabItem.GetDefaultFileName();
            string originFileName = Path.GetFileNameWithoutExtension(defaultName);
            DialogInput dialogInput = new DialogInput(LangManager.GetValueByKey("PleaseEnterNewFileName"), originFileName);

            if (!(bool)dialogInput.ShowDialog(this))
                return false;

            if (dialogInput.Text is string newName &&
                !string.IsNullOrEmpty(newName) &&
                newName.ToProperFileName() is string name) {

                if (name.ToLower().Equals(originFileName.ToLower())) {
                    // 文件名未变化，使用默认方式
                    portTabItem.SaveFileName = defaultName;
                    return true;
                }

                string targetFileName = portTabItem.GetCustomFileName(newName);
                if (File.Exists(targetFileName)) {
                    if (!(bool)(new MsgBox(LangManager.GetValueByKey("FileExistAskForAppend")).ShowDialog())) {
                        return false;
                    }
                }
                // 保存为新文件名
                portTabItem.SaveFileName = targetFileName;
                return true;
            } else {
                MessageNotify.Error(LangManager.GetValueByKey("FileNameInvalid"));
                return false;
            }
        }


        private async void SaveToNewFile(object sender, RoutedEventArgs e)
        {
            FrameworkElement ele = sender as FrameworkElement;
            if (ele == null)
                return;
            ele.IsEnabled = false;
            string portName = GetPortName(sender as FrameworkElement);
            if (!string.IsNullOrEmpty(portName)) {
                if (SetNewSaveFileName(portName)) {
                    MessageNotify.Success(LangManager.GetValueByKey("LogSaveAsOK"));
                    await Task.Delay(500); // 防止频繁点保存
                }
            }
            ele.IsEnabled = true;
        }

        private void ShowSettingsPopup(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) {
                Border border = sender as Border;
                ContextMenu contextMenu = border.ContextMenu;
                contextMenu.PlacementTarget = border;
                contextMenu.Placement = PlacementMode.Top;
                contextMenu.IsOpen = true;
            }
            e.Handled = true;
        }

        private void ShowContextMenu(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) {
                Border border = sender as Border;
                ContextMenu contextMenu = border.ContextMenu;
                contextMenu.PlacementTarget = border;
                contextMenu.Placement = PlacementMode.Bottom;
                contextMenu.IsOpen = true;
            }
            e.Handled = true;
        }

        private void CloseAllPort(object sender, RoutedEventArgs e)
        {
            Logger.Info("close all port");
            foreach (var item in vieModel.SideComPorts) {
                if (item.Hide)
                    continue;
                ClosePort(item.Name);
            }
        }

        private void OpenAllPort(object sender, RoutedEventArgs e)
        {
            Logger.Info("open all port");
            foreach (SideComPort item in vieModel.SideComPorts) {
                if (item.Hide)
                    continue;
                OpenPort(item);
            }
        }

        private void SplitPanel(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == null)
                return;
            if (button.Parent is Grid grid) {
                SplitPanel(SplitPanelType.Left | SplitPanelType.Right);
            } else if (button.Parent is StackPanel panel) {
                int idx = panel.Children.IndexOf(button);
                if (idx == 0) {
                    SplitPanel(SplitPanelType.Top | SplitPanelType.Bottom);
                } else if (idx == 1) {
                    SplitPanel(SplitPanelType.Top | SplitPanelType.Bottom | SplitPanelType.Left | SplitPanelType.Right);
                } else if (idx == 2) {
                    SplitPanel(SplitPanelType.Bottom | SplitPanelType.Left | SplitPanelType.Right);
                } else if (idx == 3) {
                    SplitPanel(SplitPanelType.Top | SplitPanelType.Left | SplitPanelType.Right);
                } else if (idx == 4) {
                    SplitPanel(SplitPanelType.None);
                }
            }
        }

        private void SplitPanel(SplitPanelType type)
        {
            if (type == SplitPanelType.None) {
                Console.WriteLine(SplitPanelType.None);
            }
            if ((type & SplitPanelType.Left) != 0) {
                Console.WriteLine(SplitPanelType.Left);
            }
            if ((type & SplitPanelType.Right) != 0) {
                Console.WriteLine(SplitPanelType.Right);
            }
            if ((type & SplitPanelType.Top) != 0) {
                Console.WriteLine(SplitPanelType.Top);
            }
            if ((type & SplitPanelType.Bottom) != 0) {
                Console.WriteLine(SplitPanelType.Bottom);
            }
        }

        private void mainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 保存配置
            SaveOpeningPorts();
            SaveComSettings();
            SaveConfigValue();
            vieModel.SaveBaudRate();
            try {
                CloseAllPort(null, null);
            } catch (Exception ex) {
                App.Logger.Error(ex.Message);
            }

            // 注意，只有启用防止系统休眠，关闭 App 后才取消该休眠
            if (ConfigManager.Settings.AvoidScreenClose)
                Win32Helper.CancelPreventSleep();
        }



        /// <summary>
        /// 保存串口的配置文件
        /// </summary>
        private void SaveComSettings()
        {
            foreach (var portTabItem in vieModel.PortTabItems) {

                ComSettings comSettings = vieModel.ComSettingList.FirstOrDefault(arg => arg.PortName.Equals(portTabItem.Name));
                if (comSettings == null)
                    comSettings = new ComSettings();
                comSettings.PortName = portTabItem.Name;
                comSettings.Connected = portTabItem.Connected;
                // PortTabItem portTabItem = item.PortTabItem;

                comSettings.WriteData = portTabItem.WriteData;
                comSettings.AddNewLineWhenWrite = portTabItem.AddNewLineWhenWrite;
                comSettings.SendHex = portTabItem.SendHex;
                comSettings.RecvShowHex = portTabItem.RecvShowHex;
                comSettings.EnabledFilter = portTabItem.EnabledFilter;
                comSettings.EnabledMonitor = portTabItem.EnabledMonitor;
                comSettings.AddTimeStamp = portTabItem.AddTimeStamp;
                portTabItem.SerialPort.RefreshSetting();
                comSettings.PortSetting = portTabItem.SerialPort?.SettingJson;

                MapperManager.ComMapper.Insert(comSettings, InsertMode.Replace);
            }
        }

        private void SaveOpeningPorts()
        {
            ConfigManager.Main.OpeningPorts = JsonUtils.TrySerializeObject(vieModel.PortTabItems.Select(arg => arg.Name).ToList());
            ConfigManager.Main.Save();
        }


        private void SaveConfigValue()
        {
            ConfigManager.Main.X = this.Left;
            ConfigManager.Main.Y = this.Top;
            ConfigManager.Main.Width = this.Width;
            ConfigManager.Main.Height = this.Height;
            //ConfigManager.Main.WindowState = (long)baseWindowState;
            ConfigManager.Main.SideGridWidth = SideGridColumn.ActualWidth;
            ConfigManager.Main.Save();
        }


        private void OpenHexTransform(object sender, RoutedEventArgs e)
        {
            string text = GetCurrentText(sender as FrameworkElement);
            OpenHex(text);
        }

        private void OpenHex(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;
            if (text.Length > MAX_TRANSFORM_SIZE) {
                MessageNotify.Warning($"{LangManager.GetValueByKey("Over")}: {MAX_TRANSFORM_SIZE}");
                return;
            }
            hexTransPopup.IsOpen = true;
            HexTextBox.Text = text;
            HexToStr(null, null);
        }


        private string GetCurrentText(FrameworkElement element)
        {
            MenuItem menuItem = element as MenuItem;
            if (menuItem != null && menuItem.Parent is ContextMenu contextMenu) {
                if (contextMenu.PlacementTarget is TextEditor textEditor) {
                    return textEditor.SelectedText;
                }
            }
            return null;
        }

        private void OpenTimeTransform(object sender, RoutedEventArgs e)
        {
            string text = GetCurrentText(sender as FrameworkElement);
            OpenTime(text);
        }

        private void OpenTime(string text)
        {
            if (text.Length > MAX_TIMESTAMP_LENGTH) {
                MessageNotify.Warning($"{LangManager.GetValueByKey("Over")}: {MAX_TIMESTAMP_LENGTH}");
                return;
            }
            if (string.IsNullOrEmpty(text))
                return;
            timeTransPopup.IsOpen = true;
            TimeStampTextBox.Text = text;
            TimeStampToLocalTime(null, null);
        }

        private void HexToStr(object sender, RoutedEventArgs e)
        {
            StrTextBox.Text = TransformHelper.HexToStr(HexTextBox.Text);
        }

        private void StrToHex(object sender, RoutedEventArgs e)
        {
            string text = TransformHelper.StrToHex(StrTextBox.Text);
            if ((bool)HexToStrSwitch.IsChecked) {
                HexTextBox.Text = text;
            } else {
                HexTextBox.Text = text.ToLower();
            }

        }

        private void Switch_Click(object sender, RoutedEventArgs e)
        {
            Switch obj = sender as Switch;
            if ((bool)obj.IsChecked) {
                HexTextBox.Text = HexTextBox.Text.ToUpper();
            } else {
                HexTextBox.Text = HexTextBox.Text.ToLower();
            }
        }


        private void TimeStampToLocalTime(object sender, RoutedEventArgs e)
        {
            bool success = long.TryParse(TimeStampTextBox.Text, out long timeStamp);
            if (!success) {
                LocalTimeTextBox.Text = LangManager.GetValueByKey("ParseFailed");
                return;
            }
            try {
                DateTime dateTime = DateHelper.UnixTimeStampToDateTime(timeStamp, TimeComboBox.SelectedIndex == 0);
                LocalTimeTextBox.Text = dateTime.ToLocalDate();
            } catch (Exception ex) {
                LocalTimeTextBox.Text = ex.Message;
            }
        }

        private void LocalTimeToTimeStamp(object sender, RoutedEventArgs e)
        {
            bool success = DateTime.TryParse(LocalTimeTextBox.Text, out DateTime dt);
            if (!success) {
                TimeStampTextBox.Text = LangManager.GetValueByKey("ParseFailed");
            } else {
                TimeStampTextBox.Text = DateHelper.DateTimeToUnixTimeStamp(dt, TimeComboBox.SelectedIndex == 0).ToString();
            }

        }

        private void ShowAdvancedOptions(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            WrapPanel wrapPanel = button.Parent as WrapPanel;
            Grid grid = (wrapPanel.Parent as Border).Parent as Grid;
            Grid rootGrid = grid.Parent as Grid;
            Grid advancedGrid = VisualHelper.FindChild(rootGrid, "advancedGrid") as Grid;
            if (advancedGrid != null)
                advancedGrid.Visibility = Visibility.Visible;
        }

        private void HideAdvancedGrid(object sender, RoutedEventArgs e)
        {
            StackPanel stackPanel = (sender as Button).Parent as StackPanel;
            Grid grid = stackPanel.Parent as Grid;
            (grid.Parent as Grid).Visibility = Visibility.Hidden;
        }

        private void RestorePortSetting(object sender, RoutedEventArgs e)
        {
            StackPanel stackPanel = (sender as Button).Parent as StackPanel;
            if (stackPanel.Tag == null || !(stackPanel.Tag.ToString() is string name))
                return;

            if (!(bool)new MsgBox($"{LangManager.GetValueByKey("RestoreSpec")}: {name} ?").ShowDialog(this))
                return;
            if (vieModel.PortTabItems != null &&
                vieModel.PortTabItems.FirstOrDefault(arg => arg.Name.Equals(name)) is PortTabItem tabItem &&
                tabItem.SerialPort is SerialPortEx port) {
                port.RestoreDefault();
                port.PrintSetting();
            }
        }

        private void SetStayOpenStatus(object sender, RoutedEventArgs e)
        {
            ToggleButton toggleButton = sender as ToggleButton;
            bool isChecked = (bool)toggleButton.IsChecked;
            Grid grid = (toggleButton.Parent as Grid).Parent as Grid;
            Popup popup = grid.Parent as Popup;
            if (popup != null)
                popup.StaysOpen = isChecked;
        }

        private void OpenByDefaultApp(object sender, RoutedEventArgs e)
        {
            PortTabItem portTabItem = GetPortItem(sender as FrameworkElement);
            if (portTabItem != null) {
                Logger.Info($"open log file, portName: {portTabItem.Name}");
                string fileName = portTabItem.SaveFileName;
                if (File.Exists(fileName)) {
                    FileHelper.TryOpenByDefaultApp(fileName);
                } else {
                    MessageCard.Warning(LangManager.GetValueByKey("CurrentNoLog"));
                }

            }
        }


        public void ApplyScreenStatus()
        {
            if (ConfigManager.Settings.AvoidScreenClose)
                Win32Helper.PreventSleep();
        }

        public void InitNotice()
        {
            noticeViewer.SetConfig(UrlManager.NOTICE_URL, ConfigManager.Main.LatestNotice);
            noticeViewer.onError += (error) => {
                App.Logger?.Error(error);
            };

            noticeViewer.onShowMarkdown += (markdown) => {
                //MessageCard.Info(markdown);
            };
            noticeViewer.onNewNotice += (newNotice) => {
                ConfigManager.Main.LatestNotice = newNotice;
                ConfigManager.Main.Save();
            };

            noticeViewer.BeginCheckNotice();
        }

        public void SetBaudRateAction()
        {
            vieModel.OnBaudRatesChanged += (beforePorts) => {
                if (vieModel.PortTabItems == null)
                    return;
                if (itemsControl == null || itemsControl.ItemsSource == null)
                    return;
                for (int i = 0; i < itemsControl.Items.Count; i++) {
                    ContentPresenter presenter = (ContentPresenter)itemsControl.ItemContainerGenerator.ContainerFromItem(itemsControl.Items[i]);
                    if (presenter == null)
                        continue;
                    ComboBox comboBox = VisualHelper.FindElementByName<ComboBox>(presenter, "baudRateComboBox");
                    if (comboBox == null || comboBox.Tag == null)
                        continue;
                    string portName = comboBox.Tag.ToString();
                    PortTabItem portTabItem = beforePorts.FirstOrDefault(arg => arg.Name.Equals(portName));
                    if (portTabItem == null || portTabItem.SerialPort == null)
                        continue;
                    int number = portTabItem.SerialPort.BaudRate;
                    bool found = false;
                    for (int j = 0; j < comboBox.Items.Count; j++) {
                        if (comboBox.Items[j].ToString().Equals(number.ToString())) {
                            comboBox.SelectedIndex = j;
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        comboBox.SelectedIndex = 0;
                }
            };
        }

        private void LoadFontFamily()
        {
            int idx = 1;
            int count = VisualHelper.SYSTEM_FONT_FAMILIES.Keys.Count;
            foreach (string fontName in VisualHelper.SYSTEM_FONT_FAMILIES.Keys) {
                MenuItem menuItem = new MenuItem();
                menuItem.Header = fontName;
                menuItem.FontFamily = VisualHelper.SYSTEM_FONT_FAMILIES[fontName];
                menuItem.IsCheckable = true;
                menuItem.IsChecked = fontName.Equals(ConfigManager.Main.TextFontName);
                menuItem.Checked += (s, e) => {
                    string name = (s as MenuItem).Header.ToString();
                    SetFontFamily(name);


                };
                FontMenuItem.Items.Add(menuItem);
                //Logger.Info($"[{idx++}/{count}]load font: {fontName}");
            }
        }

        public void SetFontFamily(string name)
        {
            if (string.IsNullOrEmpty(name))
                return;
            foreach (MenuItem item in FontMenuItem.Items) {
                if (name.Equals(item.Header.ToString()))
                    continue;
                item.IsChecked = false;
            }

            foreach (PortTabItem item in vieModel.PortTabItems) {
                TextEditor textEditor = item.TextEditor;
                if (textEditor != null)
                    textEditor.FontFamily = VisualHelper.SYSTEM_FONT_FAMILIES[name];
            }
            ConfigManager.Main.TextFontName = name;
            ConfigManager.Main.Save();
            Logger.Info($"set font: {name}");
        }

        public void InitUpgrade()
        {
            UpgradeHelper.Init();
            UpgradeHelper.OnBeforeCopyFile += () => {
                CloseAllPorts(null, null);
                this.CloseToTaskBar = false;
                this.Close();
            };
            CheckUpgrade();
        }

        private void LoadDonateConfig()
        {
            vieModel.ShowDonate = true;
            string json_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_config.json");
            if (File.Exists(json_path)) {
                string v = FileHelper.TryReadFile(json_path);
                if (!string.IsNullOrEmpty(v)) {
                    Dictionary<string, object> dict = JsonUtils.TryDeserializeObject<Dictionary<string, object>>(v);
                    if (dict != null && dict.ContainsKey("ShowDonate")) {
                        string showDonate = dict["ShowDonate"].ToString();
                        if (!string.IsNullOrEmpty(showDonate)) {
                            vieModel.ShowDonate = showDonate.ToLower().Equals("false") ? false : true;
                        }
                    }
                }
            }
        }

        public void InitThemeSelector()
        {
            ThemeSelectorDefault.AddTransParentColor("TabItem.Background");
            ThemeSelectorDefault.AddTransParentColor("Window.Title.Background");
            ThemeSelectorDefault.AddTransParentColor("Window.Side.Background");
            ThemeSelectorDefault.AddTransParentColor("Window.Side.Hover.Background");
            ThemeSelectorDefault.AddTransParentColor("ListBoxItem.Background");
            ThemeSelectorDefault.SetThemeConfig(ConfigManager.Settings.ThemeIdx, ConfigManager.Settings.ThemeID);
            ThemeSelectorDefault.onThemeChanged += (ThemeIdx, ThemeID) => {
                ConfigManager.Settings.ThemeIdx = ThemeIdx;
                ConfigManager.Settings.ThemeID = ThemeID;
                ConfigManager.Settings.Save();
            };
            ThemeSelectorDefault.onBackGroundImageChanged += (image) => {
                ImageBackground.Source = image;
            };
            ThemeSelectorDefault.onSetBgColorTransparent += () => {
                BorderTitle.Background = Brushes.Transparent;
            };

            ThemeSelectorDefault.onReSetBgColorBinding += () => {
                BorderTitle.SetResourceReference(Control.BackgroundProperty, "Window.Title.Background");
            };

            ThemeSelectorDefault.InitThemes();
        }

        private async void OpenBeforePorts()
        {
            if (string.IsNullOrEmpty(ConfigManager.Main.OpeningPorts))
                return;
            List<string> list = JsonUtils.TryDeserializeObject<List<string>>(ConfigManager.Main.OpeningPorts);
            foreach (string portName in list) {
                ComSettings comSettings = vieModel.ComSettingList.FirstOrDefault(arg => arg.PortName.Equals(portName));
                SideComPort sideComPort = vieModel.SideComPorts.FirstOrDefault(arg => arg.Name.Equals(portName));
                if (comSettings != null && sideComPort != null && comSettings.Connected) {
                    // 这里不需要等待
                    OpenPort(sideComPort);
                } else {
                    // 这里不需要等待
                    //OpenPortTabItem(portName, false);
                    OpenPort(sideComPort, false);
                }
            }
            SetFontFamily(ConfigManager.Main.TextFontName);
        }

        public void AdjustWindow()
        {

            if (ConfigManager.Main.FirstRun) {
                this.Width = SystemParameters.WorkArea.Width * 0.8;
                this.Height = SystemParameters.WorkArea.Height * 0.8;
                this.Left = SystemParameters.WorkArea.Width * 0.1;
                this.Top = SystemParameters.WorkArea.Height * 0.1;
            } else {
                //if (ConfigManager.Main.Height == SystemParameters.WorkArea.Height && ConfigManager.Main.Width < SystemParameters.WorkArea.Width)
                //{
                //    //baseWindowState = 0;
                //    this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                //    this.CanResize = true;
                //}
                //else
                //{
                //    this.Left = ConfigManager.Main.X;
                //    this.Top = ConfigManager.Main.Y;
                //    this.Width = ConfigManager.Main.Width;
                //    this.Height = ConfigManager.Main.Height;
                //}


                //baseWindowState = (BaseWindowState)ConfigManager.Main.WindowState;
                //if (baseWindowState == BaseWindowState.FullScreen)
                //{
                //    this.WindowState = System.Windows.WindowState.Maximized;
                //}
                //else if (baseWindowState == BaseWindowState.None)
                //{
                //    baseWindowState = 0;
                //    this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                ////}
                //if (this.Width == SystemParameters.WorkArea.Width
                //    && this.Height == SystemParameters.WorkArea.Height) baseWindowState = BaseWindowState.Maximized;

                //if (baseWindowState == BaseWindowState.Maximized || baseWindowState == BaseWindowState.FullScreen)
                //{
                //    MaxPath.Data = Geometry.Parse(PathData.MaxToNormalPath);
                //    MaxMenuItem.Header = "窗口化";
                //}


            }
        }



        private void Border_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (SideGridColumn.ActualWidth <= 100) {
                SideGridColumn.Width = new GridLength(0);
                sideGridMenuItem.IsChecked = false;
            }
        }

        private void OpenSetting(object sender, RoutedEventArgs e)
        {
            window_Setting?.Close();
            window_Setting = new Window_Setting();
            window_Setting.Owner = this;
            window_Setting.Show();
            window_Setting.Focus();
            window_Setting.BringIntoView();
        }

        #region "历史记录弹窗"
        private void SendTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            //TextBox textBox = sender as TextBox;
            //string text = textBox.Text.Trim().ToLower();
            //if (string.IsNullOrEmpty(text)) return;
            //List<string> list = vieModel.SendHistory.FirstOrDefault(arg => arg.ToLower().IndexOf(text) >= 0).ToList();
            //if (list.Count > 0)
            //{
            //    Grid grid = (textBox.Parent as Border).Parent as Grid;
            //    Popup popup = grid.Children.OfType<Popup>().FirstOrDefault();
            //    if (popup != null)
            //    {
            //        popup.IsOpen = true;
            //        Grid g = popup.Child as Grid;
            //        ItemsControl itemsControl = g.FindName("itemsControl") as ItemsControl;
            //        if (itemsControl != null)
            //        {
            //            itemsControl.ItemsSource = list;
            //            vieModel.SendHistorySelectedIndex = 0;
            //        }
            //    }
            //}
        }

        private void SetSendHistory(object sender, MouseButtonEventArgs e)
        {
            Border border = sender as Border;
            Grid grid = border.Child as Grid;
            TextBlock textBlock = grid.Children.OfType<TextBlock>().FirstOrDefault();
            if (textBlock != null) {
                string value = textBlock.Text;
                vieModel.SendHistorySelectedValue = value;
                Popup popup = textBlock.Tag as Popup;
                if (popup != null && popup.Tag != null) {
                    string portName = popup.Tag.ToString();
                    PortTabItem portTabItem = vieModel.PortTabItems.FirstOrDefault(arg => arg.Name.Equals(portName));
                    if (portTabItem != null) {
                        portTabItem.WriteData = value;
                        popup.IsOpen = false;
                        TextBox textBox = (popup.Parent as Grid).Children.OfType<Border>().FirstOrDefault().Child as TextBox;
                        textBox.CaretIndex = textBox.Text.Length;
                    }
                }
            }
        }

        private void SendTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;
            TextBox textBox = sender as TextBox;
            if (textBox == null || textBox.Tag == null)
                return;
            string portName = textBox.Tag.ToString();
            if (string.IsNullOrEmpty(portName))
                return;
            string text = textBox.Text.Trim();
            if (!string.IsNullOrEmpty(text))
                SendCommand(portName);
        }

        private void SetSelectedStatus(ItemsControl itemsControl)
        {
            if (itemsControl == null || itemsControl.ItemsSource == null)
                return;
            for (int i = 0; i < itemsControl.Items.Count; i++) {
                ContentPresenter presenter = (ContentPresenter)itemsControl.ItemContainerGenerator.ContainerFromItem(itemsControl.Items[i]);
                if (presenter == null)
                    continue;
                Border border = VisualHelper.FindElementByName<Border>(presenter, "baseBorder");
                if (border == null)
                    continue;
                if (i == vieModel.SendHistorySelectedIndex) {
                    border.Background = (Brush)FindResource("ListBoxItem.Selected.Active.Background");
                    border.BorderBrush = (Brush)FindResource("ListBoxItem.Selected.Active.BorderBrush");
                } else {
                    border.SetResourceReference(Control.BackgroundProperty, "Background");
                    border.SetResourceReference(Control.BorderBrushProperty, "BorderBrush");
                }
                // 滚动当前视图
                double offset = vieModel.SendHistorySelectedIndex * border.ActualHeight;
                ScrollViewer scrollViewer = itemsControl.Parent as ScrollViewer;
                scrollViewer.ScrollToVerticalOffset(offset);
            }

        }

        private void HistoryMouseEnter(object sender, MouseEventArgs e)
        {
            Border border = sender as Border;
            if (border.Background == null || !border.Background.Equals((Brush)FindResource("ListBoxItem.Selected.Active.Background")))
                border.Background = (Brush)FindResource("ListBoxItem.Hover.Background");
        }

        private void HistoryMouseLeave(object sender, MouseEventArgs e)
        {
            Border border = sender as Border;
            if (border.Background == null || !border.Background.Equals((Brush)FindResource("ListBoxItem.Selected.Active.Background")))
                border.Background = Brushes.Transparent;
        }


        // todo
        private void DeleteSendHistory(object sender, RoutedEventArgs e)
        {
            //Popup popup = (sender as Button).Tag as Popup;
            //string value = ((sender as Button).Parent as Border).Tag.ToString();
            //vieModel.SendHistory.RemoveWhere(arg => arg.Equals(value));
            //vieModel.SaveSendHistory();
            //if (popup != null && popup.IsOpen)
            //{
            //    Grid grid1 = popup.Child as Grid;
            //    ItemsControl itemsControl = grid1.FindName("itemsControl") as ItemsControl;
            //    if (itemsControl.ItemsSource != null)
            //    {
            //        List<string> list = itemsControl.ItemsSource as List<string>;
            //        list.RemoveAll(arg => arg.Equals(value));
            //        itemsControl.ItemsSource = null;
            //        itemsControl.ItemsSource = list;
            //        if (list.Count == 0) popup.IsOpen = false;
            //    }

            //}
        }
        #endregion

        private void OpenSendPanel(object sender, RoutedEventArgs e)
        {

            Button button = sender as Button;
            int index = 0;
            if (button != null && button.Tag != null)
                int.TryParse(button.Tag.ToString(), out index);

            Window_AdvancedSend window = new Window_AdvancedSend();
            window.SideSelectedIndex = index;
            window.Show();
            window.Focus();
            window.BringIntoView();
            Logger.Info("open send window");

        }

        private void HideSide(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem.IsChecked) {
                SideGridColumn.Width = new GridLength(200);
            } else {
                SideGridColumn.Width = new GridLength(0);
                Border_SizeChanged(null, null);
            }

        }
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            Grid grid = (comboBox.Parent as StackPanel).Parent as Grid;
            ItemsControl itemsControl = grid.Children.OfType<ItemsControl>().LastOrDefault();
            if (itemsControl == null) {
                return;
            }
            itemsControl.ItemsSource = null;
            if (comboBox.SelectedValue == null)
                return;
            string id = comboBox.SelectedValue.ToString();
            if (string.IsNullOrEmpty(id))
                return;
            AdvancedSend advancedSend = vieModel.SendCommandProjects.FirstOrDefault(arg => arg.ProjectID.ToString().Equals(id));
            vieModel.CurrentAdvancedSend = advancedSend;
            Logger.Info($"set current run project: {advancedSend.ProjectName}");
            if (!string.IsNullOrEmpty(advancedSend.Commands)) {
                itemsControl.ItemsSource = JsonUtils.TryDeserializeObject<List<SendCommand>>(advancedSend.Commands).OrderBy(arg => arg.Order);
                vieModel.CommandsSelectIndex = comboBox.SelectedIndex;
                ConfigManager.Main.CommandsSelectIndex = comboBox.SelectedIndex;
                ConfigManager.Main.Save();
            }
        }



        public void RefreshSendCommands()
        {
            vieModel.LoadSendCommands();
            SetComboboxStatus();
        }

        public void SetComboboxStatus()
        {
            foreach (PortTabItem item in vieModel.PortTabItems) {
                TextEditor textEditor = item.TextEditor;
                if (textEditor == null)
                    textEditor = FindTextBoxByPortName(item.Name);
                if (textEditor != null) {
                    ComboBox comboBox = FindCombobox(textEditor);
                    if (comboBox != null) {
                        if (vieModel.CommandsSelectIndex < comboBox.Items.Count)
                            comboBox.SelectedIndex = vieModel.CommandsSelectIndex;
                        else
                            comboBox.SelectedIndex = 0;
                    }
                }
            }
        }

        private ComboBox FindCombobox(TextEditor textEditor)
        {
            Grid grid = (textEditor.Parent as Border).Parent as Grid;
            Grid rootGrid = grid.Parent as Grid;
            Grid borderGrid = rootGrid.Children.OfType<Grid>().LastOrDefault();
            Border border = borderGrid.Children.OfType<Border>().Last();
            Grid grid1 = border.Child as Grid;
            StackPanel stackPanel = grid1.Children.OfType<StackPanel>().FirstOrDefault();
            if (stackPanel != null)
                return stackPanel.Children.OfType<ComboBox>().LastOrDefault();

            return null;
        }

        private void ScrollViewer_PreviewMouseWheel_1(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scrollViewer = sender as ScrollViewer;
            scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - e.Delta);
            e.Handled = true;
        }


        // todo 多命令同时发送
        private async void SendToFindResultTask(PortTabItem item, string recvResult, int timeOut, string command)
        {
            PortTabItem portTabItem = vieModel.PortTabItems.FirstOrDefault(arg => arg.Name.Equals(item.Name));
            if (portTabItem.ResultChecks == null)
                portTabItem.ResultChecks = new Queue<ResultCheck>();
            ResultCheck resultCheck = new ResultCheck();
            resultCheck.Command = command;
            resultCheck.Buffer = new StringBuilder();
            portTabItem.ResultChecks.Enqueue(resultCheck);
            int time = 0;
            bool find = false;
            while (!find && time <= timeOut) {
                ResultCheck check = portTabItem.ResultChecks.FirstOrDefault(arg => arg.Command.Equals(command));
                if (check != null) {
                    string[] buffers = check.Buffer.ToString().Split(Environment.NewLine.ToCharArray());
                    foreach (string line in buffers) {
                        if (line.IndexOf(recvResult) >= 0 && line.IndexOf($"SEND >>>>>>>>>> {command}") < 0) {
                            find = true;

                            break;
                        }
                    }
                    if (find)
                        break;


                    await Task.Delay(100);
                    time += 100;
                    //Console.WriteLine("查找中...");
                } else {
                    break;
                }
            }

            if (find) {
                MessageCard.Info($"{LangManager.GetValueByKey("Found")} \n{resultCheck.Buffer}");
            } else {
                MessageNotify.Info(LangManager.GetValueByKey("FoundTimeOut"));
            }
            portTabItem.ResultChecks.Dequeue();
        }


        private void SendCustomCommand(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            string command = "";
            int commandID = -1;
            if (button.ToolTip != null)
                command = button.ToolTip.ToString();
            if (button.Tag != null)
                int.TryParse(button.Tag.ToString(), out commandID);
            Border border = button.FindParentOfType<Border>("sendBorder");
            if (border == null || border.Tag == null)
                return;

            string portName = border.Tag.ToString();
            SideComPort sideComPort = vieModel.SideComPorts.FirstOrDefault(arg => arg.Name.Equals(portName));
            if (sideComPort == null || sideComPort.PortTabItem == null || sideComPort.PortTabItem.SerialPort == null)
                return;

            PortTabItem portTabItem = vieModel.PortTabItems.FirstOrDefault(arg => arg.Name.Equals(portName));
            if (portTabItem == null)
                return;

            AdvancedSend send = vieModel.CurrentAdvancedSend;
            if (send == null || string.IsNullOrEmpty(send.Commands))
                return;

            Logger.Info($"click send btn, name: {send.ProjectName}, port name: {portName}");

            send.CommandList = JsonUtils.TryDeserializeObject<List<SendCommand>>(send.Commands);
            SendCommand sendCommand = send.CommandList.FirstOrDefault(arg => arg.CommandID == commandID);
            if (sendCommand != null && sendCommand.IsResultCheck) {
                // 过滤找到需要的字符串
                string recvResult = sendCommand.RecvResult;
                int timeOut = sendCommand.RecvTimeOut;
                SendToFindResultTask(sideComPort.PortTabItem, recvResult, timeOut, command);
            }

            portTabItem.SendCustomCommand(command);
        }

        private void StartSendCommands(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == null || button.Tag == null)
                return;
            StackPanel stackPanel = button.Parent as StackPanel;
            ComboBox comboBox = stackPanel.Children.OfType<ComboBox>().LastOrDefault();
            string portName = button.Tag.ToString();
            if (comboBox != null && comboBox.SelectedValue != null &&
                vieModel.SendCommandProjects?.Count > 0) {
                string projectID = comboBox.SelectedValue.ToString();

                // 开始执行队列
                AdvancedSend advancedSend = vieModel.SendCommandProjects.FirstOrDefault(arg => arg.ProjectID.ToString().Equals(projectID));
                PortTabItem portTabItem = vieModel.PortTabItems.FirstOrDefault(arg => arg.Name.Equals(portName));
                if (advancedSend != null) {
                    Logger.Info($"start run command: {advancedSend.ProjectName}");
                    try {
                        advancedSend.BeginSendCommands(advancedSend, portTabItem, (status) => {
                            SetRunningStatus(button, status);
                        });
                    } catch (Exception ex) {
                        Logger.Error(ex);
                        MessageCard.Error(ex.Message);
                        SetRunningStatus(button, false);
                        portTabItem.RunningCommands = false;
                    }
                }
            }
        }


        private void StopSendCommands(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == null || button.Tag == null)
                return;
            StackPanel stackPanel = button.Parent as StackPanel;
            ComboBox comboBox = stackPanel.Children.OfType<ComboBox>().LastOrDefault();
            string portName = button.Tag.ToString();
            PortTabItem portTabItem = vieModel.PortTabItems.FirstOrDefault(arg => arg.Name.Equals(portName));
            if (comboBox != null && comboBox.SelectedValue != null &&
                vieModel.SendCommandProjects?.Count > 0 && portTabItem != null) {
                string projectID = comboBox.SelectedValue.ToString();
                // 执行队列
                AdvancedSend advancedSend = vieModel.SendCommandProjects.FirstOrDefault(arg => arg.ProjectID.ToString().Equals(projectID));
                if (advancedSend == null)
                    return;

                Logger.Info($"stop run command: {advancedSend.ProjectName}");

                portTabItem.RunningCommands = false;
                if (advancedSend.CommandList?.Count > 0)
                    foreach (var item in advancedSend.CommandList)
                        item.Status = RunningStatus.WaitingToRun;

            }


        }

        public void SetRunningStatus(Button button, bool running)
        {
            button.IsEnabled = !running;
            StackPanel stackPanel = button.Parent as StackPanel;
            Button stopButton = stackPanel.Children.OfType<Button>().LastOrDefault();
            if (stopButton != null)
                stopButton.IsEnabled = running;
        }

        private void Remark(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            ContextMenu contextMenu = menuItem.Parent as ContextMenu;
            FrameworkElement frameworkElement = contextMenu.PlacementTarget as FrameworkElement;
            if (frameworkElement != null && frameworkElement.Tag != null) {
                string portName = frameworkElement.Tag.ToString();
                SideComPort sideComPort = vieModel.SideComPorts.FirstOrDefault(arg => arg.Name.Equals(portName));
                if (sideComPort != null && sideComPort.PortTabItem is PortTabItem portTabItem) {
                    Logger.Info("click remark btn");
                    DialogInput dialogInput = new DialogInput(LangManager.GetValueByKey("PleaseEnterRemark"), portTabItem.Remark);
                    if (dialogInput.ShowDialog(this) == true) {
                        string value = dialogInput.Text;
                        portTabItem.Remark = value;
                        portTabItem.SerialPort.SaveRemark(value);
                        sideComPort.Remark = value;
                        ComSettings comSettings = vieModel.ComSettingList.FirstOrDefault(arg => arg.PortName.Equals(portName));
                        if (comSettings != null) {
                            Dictionary<string, object> dict = JsonUtils.TryDeserializeObject<Dictionary<string, object>>(comSettings.PortSetting);
                            if (dict != null && dict.ContainsKey("Remark")) {
                                dict["Remark"] = value;
                                comSettings.PortSetting = JsonUtils.TrySerializeObject(dict);
                                Logger.Info($"set remark: {value}");
                            }
                        }
                    }
                } else if (sideComPort.PortTabItem == null) {
                    MessageNotify.Info(LangManager.GetValueByKey("RemarkAfterOpenPort"));
                }
            }
        }

        private void HidePort(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            ContextMenu contextMenu = menuItem.Parent as ContextMenu;
            FrameworkElement frameworkElement = contextMenu.PlacementTarget as FrameworkElement;
            if (frameworkElement != null && frameworkElement.Tag != null) {
                string portName = frameworkElement.Tag.ToString();
                SideComPort sideComPort = vieModel.SideComPorts.FirstOrDefault(arg => arg.Name.Equals(portName));
                if (sideComPort != null) {
                    sideComPort.Hide = true;
                }
            }
        }

        private void ShowAllHidePort(object sender, RoutedEventArgs e)
        {
            Logger.Info("show all hide port");
            foreach (SideComPort item in vieModel.SideComPorts) {
                item.Hide = false;
            }

        }

        private void OpenLog(object sender, RoutedEventArgs e)
        {
            MessageNotify.Info(LangManager.GetValueByKey("Developing"));
        }

        private void BaudRate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            if (!comboBox.IsLoaded)
                return;

            if (e.AddedItems == null || e.AddedItems.Count <= 0)
                return;
            string text = e.AddedItems[0].ToString();
            if (VieModel_Main.DEFAULT_ADD_TEXT.Equals(text)) {
                Logger.Info("add new baudrate");
                // 记录原来的下标
                int index = 0;
                string origin = e.RemovedItems[0].ToString();
                if (!string.IsNullOrEmpty(origin)) {
                    for (int i = 0; i < vieModel.BaudRates.Count; i++) {
                        if (vieModel.BaudRates[i].Equals(origin)) {
                            index = i;
                            break;
                        }
                    }
                }
                DialogInput dialogInput = new DialogInput(Window_Setting.INPUT_NOTICE_TEXT);
                bool success = false;
                if ((bool)dialogInput.ShowDialog(this)) {
                    string value = dialogInput.Text;

                    if (!string.IsNullOrEmpty(value) && int.TryParse(value, out int baudrate) &&
                        !vieModel.BaudRates.Contains(baudrate.ToString())) {
                        Logger.Info($"new baudrate = {value}");
                        vieModel.BaudRates.RemoveAt(vieModel.BaudRates.Count - 1);
                        vieModel.BaudRates.Add(baudrate.ToString());
                        vieModel.BaudRates.Add(VieModel_Main.DEFAULT_ADD_TEXT);
                        success = true;
                        (sender as ComboBox).SelectedIndex = vieModel.BaudRates.Count - 2;
                        // 保存当前项目
                        vieModel.SaveBaudRate();
                        vieModel.SaveBaudRate((sender as ComboBox).Tag.ToString(), text);
                    }

                }
                if (!success) {
                    (sender as ComboBox).SelectedIndex = index;

                }
            } else {
                Logger.Info($"set baudrate: {text}");
            }

        }

        private void SortSidePorts(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem != null && menuItem.Tag != null) {
                SetAllMenuItemSortable(menuItem);
                MenuItemExt.SetSortable(menuItem, true);
                LastSortDesc = MenuItemExt.GetDesc(menuItem);
                List<SideComPort> sideComPorts = vieModel.SideComPorts.ToList();
                string value = menuItem.Tag.ToString();
                Enum.TryParse(value, out ComPortSortType sortType);
                LastSortType = sortType;
                vieModel.InitPortData(LastSortType, LastSortDesc);

                Logger.Info($"sort port, type: {LastSortType}, desc: {LastSortDesc}");

                RetainSidePortValue(sideComPorts);
                MenuItemExt.SetDesc(menuItem, !LastSortDesc);
            }
        }

        private void SetAllMenuItemSortable(MenuItem menuItem)
        {
            ContextMenu contextMenu = menuItem.Parent as ContextMenu;
            List<MenuItem> menuItems = contextMenu.Items.OfType<MenuItem>().ToList();
            foreach (var item in menuItems) {
                MenuItemExt.SetSortable(item, false);
            }
        }


        private void ShowPluginWindow(object sender, RoutedEventArgs e)
        {
            Window_Plugin window_Plugin = new Window_Plugin();

            PluginConfig config = new PluginConfig();
            config.PluginBaseDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");
            config.RemoteUrl = UrlManager.GetPluginUrl();
            // 读取本地配置
            window_Plugin.OnEnabledChange += (data, enabled) => {
                return true;
            };

            window_Plugin.OnBeginDelete += (data) => {
                return true;
            };

            window_Plugin.OnBeginDownload += (data) => {
                return true;
            };

            window_Plugin.SetConfig(config);
            window_Plugin.Icon = this.Icon;
            window_Plugin.Show();
        }

        private async void CheckUpgrade()
        {
            // 启动后检查更新
            try {
                await Task.Delay(UpgradeHelper.AUTO_CHECK_UPGRADE_DELAY);
                (string LatestVersion, string ReleaseDate, string ReleaseNote) result = await UpgradeHelper.GetUpgradeInfo();
                string remote = result.LatestVersion;
                string ReleaseDate = result.ReleaseDate;
                if (!string.IsNullOrEmpty(remote)) {
                    string local = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                    local = local.Substring(0, local.Length - ".0.0".Length);
                    if (local.CompareTo(remote) < 0) {
                        MessageCard.Info($"{LangManager.GetValueByKey("ExistNewVersion")}\n{LangManager.GetValueByKey("Version")}：{remote}\n{LangManager.GetValueByKey("Date")}：{ReleaseDate}", () => {
                            UpgradeHelper.OpenWindow(this);
                        }, targetWindow: this);
                    }
                }
            } catch (Exception ex) {
                App.Logger.Error(ex.Message);
            }
        }

        private void ClearHexSpace(object sender, RoutedEventArgs e)
        {
            string text = HexTextBox.Text;
            text = text.Trim().Replace(" ", "");
            HexTextBox.Text = text;
        }


        private async Task<bool> BackupData()
        {
            if (ConfigManager.Settings.AutoBackup) {
                int period = Config.WindowConfig.Settings.BackUpPeriods[(int)ConfigManager.Settings.AutoBackupPeriodIndex];
                bool backup = false;
                string BackupPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backup");
                string[] arr = DirHelper.TryGetDirList(BackupPath);
                if (arr != null && arr.Length > 0) {
                    string dirname = arr[arr.Length - 1];
                    if (Directory.Exists(dirname)) {
                        string dirName = Path.GetFileName(dirname);
                        DateTime before = DateTime.Now.AddDays(1);
                        DateTime now = DateTime.Now;
                        DateTime.TryParse(dirName, out before);
                        if (now.CompareTo(before) < 0 || (now - before).TotalDays > period) {
                            backup = true;
                        }
                    }
                } else {
                    backup = true;
                }

                if (backup) {
                    string dir = Path.Combine(BackupPath, DateHelper.NowDate());
                    bool created = DirHelper.TryCreateDirectory(dir);
                    if (!created)
                        return false;
                    string target_file = Path.Combine(dir, ConfigManager.SQLITE_DATA_PATH);
                    string origin_file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigManager.SQLITE_DATA_PATH);
                    FileHelper.TryCopyFile(origin_file, target_file);
                    // 复制语法高亮
                    string highLightDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AvalonEdit");
                    DirHelper.TryCopy(highLightDir, Path.Combine(dir, "AvalonEdit"));
                }
            }

            await Task.Delay(1);
            return false;
        }

        private void Button_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            Button button = sender as Button;
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.IsOpen = true;
        }

        private void EditSendCommand(object sender, RoutedEventArgs e)
        {
            editTextBoxOrder.Text = "";
            editTextBoxName.Text = "";
            editTextBoxDelay.Text = "";
            editTextBoxCommand.Text = "";


            MenuItem menuItem = sender as MenuItem;
            Button button = (menuItem.Parent as ContextMenu).PlacementTarget as Button;
            if (button != null && button.Tag != null && vieModel.CurrentAdvancedSend != null) {
                string commandID = button.Tag.ToString();
                List<SendCommand> sendCommands = JsonUtils.TryDeserializeObject<List<SendCommand>>(vieModel.CurrentAdvancedSend.Commands).OrderBy(arg => arg.Order).ToList();
                SendCommand sendCommand = sendCommands.FirstOrDefault(arg => arg.CommandID.ToString().Equals(commandID));
                if (sendCommand != null) {
                    CurrentEditCommand = sendCommand;
                    editTextBoxOrder.Text = sendCommand.Order.ToString();
                    editTextBoxName.Text = sendCommand.Name;
                    editTextBoxDelay.Text = sendCommand.Delay.ToString();
                    editTextBoxCommand.Text = sendCommand.Command;

                    editSendCommandPopup.IsOpen = true;
                }
            }
        }


        private void DeleteSendCommand(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            Button button = (menuItem.Parent as ContextMenu).PlacementTarget as Button;
            if (button != null && button.Tag != null && vieModel.CurrentAdvancedSend != null) {
                string commandID = button.Tag.ToString();
                List<SendCommand> sendCommands = JsonUtils.TryDeserializeObject<List<SendCommand>>(vieModel.CurrentAdvancedSend.Commands).OrderBy(arg => arg.Order).ToList();
                SendCommand sendCommand = sendCommands.FirstOrDefault(arg => arg.CommandID.ToString().Equals(commandID));
                if (sendCommand != null) {
                    sendCommands.Remove(sendCommand);
                    AdvancedSend advancedSend = vieModel.CurrentAdvancedSend;
                    if (!string.IsNullOrEmpty(advancedSend.Commands)) {
                        advancedSend.CommandList = JsonUtils.TryDeserializeObject<List<SendCommand>>(advancedSend.Commands);
                        advancedSend.CommandList.RemoveAll(arg => arg.CommandID == sendCommand.CommandID);
                        advancedSend.Commands = JsonUtils.TrySerializeObject(advancedSend.CommandList);
                        vieModel.UpdateProject(advancedSend);
                        RefreshSendCommands();
                        SetComboboxStatus();
                    }

                }
            }

        }

        private void EditCommandConfirm(object sender, RoutedEventArgs e)
        {
            editSendCommandPopup.IsOpen = false;
            AdvancedSend advancedSend = vieModel.CurrentAdvancedSend;
            if (!string.IsNullOrEmpty(advancedSend.Commands) && CurrentEditCommand != null) {
                advancedSend.CommandList = JsonUtils.TryDeserializeObject<List<SendCommand>>(advancedSend.Commands);

                for (int i = 0; i < advancedSend.CommandList.Count; i++) {
                    if (advancedSend.CommandList[i].CommandID.Equals(CurrentEditCommand.CommandID)) {
                        advancedSend.CommandList[i].Name = editTextBoxName.Text;
                        advancedSend.CommandList[i].Command = editTextBoxCommand.Text;
                        int.TryParse(editTextBoxDelay.Text, out int delay);
                        int.TryParse(editTextBoxOrder.Text, out int order);
                        advancedSend.CommandList[i].Delay = delay;
                        advancedSend.CommandList[i].Order = order;
                        break;
                    }
                }

                advancedSend.Commands = JsonUtils.TrySerializeObject(advancedSend.CommandList);
                vieModel.UpdateProject(advancedSend);
                RefreshSendCommands();
                SetComboboxStatus();
            }
        }

        private void EditCommandCancel(object sender, RoutedEventArgs e)
        {
            editSendCommandPopup.IsOpen = false;
        }




        private void OpenShortCut(object sender, RoutedEventArgs e)
        {
            if (window_ShortCut != null)
                window_ShortCut.Close();
            window_ShortCut = null;
            window_ShortCut = new Window_ShortCut();
            window_ShortCut.ShowDialog();
            window_ShortCut.BringIntoView();
            window_ShortCut.Activate();
        }

        #region "快捷键处理"

        private async void OpenCloseCurrentPort(string portName)
        {
            SideComPort sideComPort = vieModel.SideComPorts.FirstOrDefault(arg => arg.Name.Equals(portName));
            if (sideComPort == null) {
                MessageCard.Error($"{LangManager.GetValueByKey("OpenPortFailed")}: {portName}");
                return;
            }

            if (sideComPort.Connected) {
                await ClosePort(portName);
            } else {
                // 连接
                await OpenPort(sideComPort);
            }
        }

        /// <summary>
        /// 收起展开发送栏
        /// </summary>
        /// <param name="sender"></param>
        private void ExpandSendPanel(object sender)
        {
            Grid baseGrid = sender as Grid;
            if (baseGrid != null) {
                double height = baseGrid.RowDefinitions[2].ActualHeight;
                if (height <= 10)
                    baseGrid.RowDefinitions[2].Height = new GridLength(DEFAULT_SEND_PANEL_HEIGHT, GridUnitType.Pixel);
                else
                    baseGrid.RowDefinitions[2].Height = new GridLength(0, GridUnitType.Pixel);
            }
        }

        #endregion

        private bool KeyDownInTab(object sender)
        {
            return sender is Grid;
        }

        private void onPreviewKeyDown(object sender, KeyEventArgs e)
        {
            string portName = "";
            if (vieModel.PortTabItems?.Count > 0) {
                foreach (var portTabItem in vieModel.PortTabItems) {
                    if (portTabItem.Selected) {
                        portName = portTabItem.Name;
                        break;
                    }
                }
            }

            App.Logger.Debug($"{LangManager.GetValueByKey("PressKey")}：{e.Key}");

            // 快捷键检测
            ShortCutBinding shortCutBinding = null;
            int max = 0;
            foreach (var item in vieModel.ShortCutBindings) {
                // 贪婪匹配：最多的按键按下
                if (KeyBoardHelper.IsAllKeyDown(item.KeyList) && item.KeyList.Count > max) {
                    shortCutBinding = item;
                    max = item.KeyList.Count;
                }
            }
            if (shortCutBinding == null)
                return;
            ShortCutType type = (ShortCutType)shortCutBinding.KeyID;
            switch (type) {
                case ShortCutType.OpenCloseCurrentPort:
                    if (KeyDownInTab(sender))
                        OpenCloseCurrentPort(portName);
                    break;
                case ShortCutType.ExpandSendingBar:
                    if (KeyDownInTab(sender))
                        ExpandSendPanel(sender);
                    break;
                case ShortCutType.FullScreen:
                    // 全屏
                    if (this.WindowState == WindowState.Maximized) {
                        this.WindowState = WindowState.Normal;
                    } else if (this.WindowState == WindowState.Normal) {
                        this.WindowState = WindowState.Maximized;
                    }
                    e.Handled = true;
                    break;
                case ShortCutType.PinOrScroll: {
                        if (!KeyDownInTab(sender))
                            return;
                        // 固定滚屏
                        PortTabItem portTabItem = vieModel.PortTabItems.FirstOrDefault(arg => arg.Name.Equals(portName));
                        if (portTabItem != null)
                            portTabItem.FixedText = !portTabItem.FixedText;
                    }
                    break;
                case ShortCutType.HexTransform: {
                        if (!KeyDownInTab(sender))
                            return;
                        // hex 转换
                        Grid baseGrid = sender as Grid;
                        (ToggleButton toggleButton, TextEditor textEditor) = FindToggleButtonByBaseGrid(baseGrid);
                        if (textEditor != null)
                            OpenHex(textEditor.SelectedText);
                    }
                    break;
                case ShortCutType.TimeStampTransform: {
                        if (!KeyDownInTab(sender))
                            return;
                        // 时间戳
                        Grid baseGrid = sender as Grid;
                        (ToggleButton toggleButton, TextEditor textEditor) = FindToggleButtonByBaseGrid(baseGrid);
                        if (textEditor != null)
                            OpenTime(textEditor.SelectedText);
                    }
                    break;
                case ShortCutType.FormatToJSON: {
                        if (!KeyDownInTab(sender))
                            return;
                        // 格式化为 JSON
                        Grid baseGrid = sender as Grid;
                        (ToggleButton toggleButton, TextEditor textEditor) = FindToggleButtonByBaseGrid(baseGrid);
                        if (textEditor != null) {
                            string origin = textEditor.SelectedText;
                            string format = FormatString(FormatType.JSON, origin);
                            textEditor.SelectedText = format;
                        }
                    }
                    break;
                case ShortCutType.JoinLine: {
                        if (!KeyDownInTab(sender))
                            return;
                        // 合并为一行
                        Grid baseGrid = sender as Grid;
                        (ToggleButton toggleButton, TextEditor textEditor) = FindToggleButtonByBaseGrid(baseGrid);
                        if (textEditor != null) {
                            string origin = textEditor.SelectedText;
                            string format = FormatString(FormatType.JOINLINE, origin);
                            textEditor.SelectedText = format;
                        }
                    }

                    break;
                case ShortCutType.SaveLogAs:
                    SaveLog(null, null);// 另存为
                    e.Handled = true;
                    break;
                case ShortCutType.Close:
                    ClosePortTabItemByName(portName);
                    e.Handled = true;
                    break;
                case ShortCutType.PinnedTab:
                    PinPort(vieModel.PortTabItems.FirstOrDefault(arg => arg.Name.Equals(portName)));
                    e.Handled = true;
                    break;
                default:


                    break;

            }
        }

        public void OnShortCutChanged()
        {
            vieModel.LoadShortCut();
        }

        private void OpenFeedBack(object sender, RoutedEventArgs e)
        {
            FileHelper.TryOpenUrl(UrlManager.FeedbackUrl);
        }

        private void OpenHelp(object sender, RoutedEventArgs e)
        {
            FileHelper.TryOpenUrl(UrlManager.HelpUrl);
        }

        private void OpenDonate(object sender, RoutedEventArgs e)
        {
            Window_Donate window_Donate = new Window_Donate();
            window_Donate.SetUrl(UrlManager.GetDonateJsonUrl());
            window_Donate.ShowDialog();
        }

        private TextEditor GetTextEditorFromMenuItem(MenuItem menuItem, int depth = 0)
        {
            if (depth == 0) {
                if (menuItem != null && menuItem.Parent is ContextMenu contextMenu) {
                    if (contextMenu.PlacementTarget is TextEditor textEditor) {
                        return textEditor;
                    }
                }
            } else if (depth == 1) {
                if (menuItem != null && (menuItem.Parent as MenuItem).Parent is ContextMenu contextMenu) {
                    if (contextMenu.PlacementTarget is TextEditor textEditor) {
                        return textEditor;
                    }
                }
            }
            return null;
        }

        private void FormatJson(object sender, RoutedEventArgs e)
        {
            TextEditor textEditor = GetTextEditorFromMenuItem((MenuItem)sender, 1);
            if (textEditor == null)
                return;
            string origin = textEditor.SelectedText;
            string format = FormatString(FormatType.JSON, origin);
            textEditor.SelectedText = format;
        }

        private string FormatString(FormatType formatType, string origin)
        {
            if (string.IsNullOrEmpty(origin))
                return "";
            switch (formatType) {
                case FormatType.JSON:
                    Dictionary<string, object> dictionary = JsonUtils.TryDeserializeObject<Dictionary<string, object>>(origin);
                    if (dictionary != null) {
                        string json_text = JsonUtils.TrySerializeObject(dictionary, Newtonsoft.Json.Formatting.Indented);
                        if (!string.IsNullOrEmpty(json_text))
                            return $"{Environment.NewLine}{json_text}";
                    }
                    break;
                case FormatType.JOINLINE:
                    return origin.Replace("\n", "").Replace("\r", "");

                default:
                    break;
            }
            return origin;
        }

        private void JoinLine(object sender, RoutedEventArgs e)
        {
            TextEditor textEditor = GetTextEditorFromMenuItem((MenuItem)sender, 1);
            if (textEditor == null)
                return;
            string origin = textEditor.SelectedText;
            string format = FormatString(FormatType.JOINLINE, origin);
            textEditor.SelectedText = format;
        }

        private void ShowUpgradeWindow(object sender, RoutedEventArgs e)
        {
            UpgradeHelper.OpenWindow(this);
        }

        private void CopyCommand(object sender, RoutedEventArgs e)
        {
            string text = editTextBoxCommand.Text;
            if (!string.IsNullOrEmpty(text))
                ClipBoard.TrySetDataObject(text);

        }



        private bool GetMenuItemCheckedStatus(object sender)
        {
            return (sender as MenuItem).IsChecked;
        }


        private void SetTextEditOption(string optionName, object status)
        {
            Logger.Info($"set {optionName}: {status}");
            foreach (PortTabItem item in vieModel.PortTabItems) {
                TextEditor textEditor = item.TextEditor;
                if (textEditor != null) {
                    TextEditorOptions options = textEditor.Options;
                    if (options == null)
                        continue;
                    System.Reflection.PropertyInfo propertyInfo = options.GetType().GetProperty(optionName);
                    if (propertyInfo == null)
                        continue;
                    propertyInfo.SetValue(options, status);
                }
            }
        }
        private void SetTextPropOption(string propName, object status)
        {
            foreach (PortTabItem item in vieModel.PortTabItems) {
                TextEditor textEditor = item.TextEditor;
                if (textEditor != null) {
                    System.Reflection.PropertyInfo propertyInfo = textEditor.GetType().GetProperty(propName);
                    if (propertyInfo == null)
                        continue;
                    propertyInfo.SetValue(textEditor, status);
                }
            }

        }

        private void SetTextViewReturn(object sender, RoutedEventArgs e)
        {
            bool status = GetMenuItemCheckedStatus(sender);
            SetTextEditOption("ShowEndOfLine", status);
        }

        private void SetTextViewSpace(object sender, RoutedEventArgs e)
        {
            bool status = GetMenuItemCheckedStatus(sender);
            SetTextEditOption("ShowSpaces", status);
        }

        private void SetTextViewTab(object sender, RoutedEventArgs e)
        {
            bool status = GetMenuItemCheckedStatus(sender);
            SetTextEditOption("ShowTabs", status);
        }

        private void SetTextHighLightCurrent(object sender, RoutedEventArgs e)
        {
            bool status = GetMenuItemCheckedStatus(sender);
            SetTextEditOption("HighlightCurrentLine", status);
        }

        private void SetTextViewLineNumber(object sender, RoutedEventArgs e)
        {
            bool value = GetMenuItemCheckedStatus(sender);
            SetTextPropOption("ShowLineNumbers", value);

        }

        private void colorPicker_SelectedColorChanged(object sender, EventArgs e)
        {
            ColorPicker colorPicker = sender as ColorPicker;
            SolidColorBrush brush = new SolidColorBrush(colorPicker.SelectedColor);
            SetTextPropOption("Foreground", brush);
            ConfigManager.Main.TextForeground = brush.ToString();

        }

        private void textBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextEditor textEditor = sender as TextEditor;
            if (textEditor == null || textEditor.Parent == null)
                return;
            Border border = textEditor.Parent as Border;
            if (border == null)
                return;
            border.BorderBrush = (SolidColorBrush)Application.Current.Resources["Button.Selected.BorderBrush"];
            // 设置不滚动
            FixedTextEditor(border);
        }

        private void FixedTextEditor(Border border)
        {
            // 将文本固定
            if (ConfigManager.Settings.FixedWhenFocus) {
                Grid rootGrid = border.Parent as Grid;
                ToggleButton toggleButton = rootGrid.FindName("pinToggleButton") as ToggleButton;
                string portName = rootGrid.Tag.ToString();
                SideComPort sideComPort = vieModel.SideComPorts.FirstOrDefault(arg => arg.Name.Equals(portName));
                if (sideComPort != null && sideComPort.PortTabItem is PortTabItem portTabItem && !(bool)toggleButton.IsChecked) {
                    //portTabItem.TextEditor.TextChanged -= TextBox_TextChanged;
                    //toggleButton.IsChecked = true;
                    portTabItem.FixedText = true;
                }
            }
        }

        private void textBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextEditor textEditor = sender as TextEditor;
            if (textEditor != null && textEditor.Parent is Border border)
                border.BorderBrush = Brushes.Transparent;
        }

        private void ShowVirtualPort(object sender, RoutedEventArgs e)
        {
            if (virtualPort == null) {
                virtualPort = new Window_VirtualPort();
                virtualPort.Show();
            } else {
                if (virtualPort.IsClosed) {
                    virtualPort = new Window_VirtualPort();
                    virtualPort.Show();
                }
                virtualPort.BringIntoView();
                virtualPort.Focus();
            }
        }


        private void CloseForegroundSelected(object sender, RoutedEventArgs e)
        {
            ForegroundPopup.IsOpen = false;
        }

        private void OpenForegroundSelected(object sender, RoutedEventArgs e)
        {
            ForegroundPopup.IsOpen = true;
            string colorText = ConfigManager.Main.TextForeground;
            if (!string.IsNullOrEmpty(colorText)) {

                Brush brush = VisualHelper.HexStringToBrush(colorText);
                if (brush != null) {
                    SolidColorBrush solidColorBrush = (SolidColorBrush)brush;
                    colorPicker.SetCurrentColor(solidColorBrush.Color);

                }
            }
        }



        private void OnShowRightPanel(object sender, RoutedEventArgs e)
        {
            ConfigManager.Main.ShowRightPanel = false;
            StackPanel panel = (sender as Button).Parent as StackPanel;
            Grid grid = panel.Parent as Grid;
            Grid rootGrid = grid.Parent as Grid;
            Grid monitorGrid = rootGrid.FindName("monitorGrid") as Grid;
            monitorGrid.Visibility = Visibility.Collapsed;
            ToggleButton toggleButton = panel.Children.OfType<ToggleButton>().FirstOrDefault();
            toggleButton.IsChecked = false;
        }







        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleButton toggleButton = sender as ToggleButton;
            Grid grid = toggleButton.Parent as Grid;
            TextBox textBox = grid.Children.OfType<TextBox>().FirstOrDefault();
            if ((bool)toggleButton.IsChecked) {
                textBox.TextWrapping = TextWrapping.Wrap;

            } else {
                textBox.TextWrapping = TextWrapping.NoWrap;
            }
        }

        private bool CanDragTabItem = false;


        private string GetPortNameByMenuItem(object sender)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem == null)
                return "";
            ContextMenu contextMenu = menuItem.Parent as ContextMenu;
            if (contextMenu != null && contextMenu.PlacementTarget is Border border) {
                if (border.Tag != null)
                    return border.Tag.ToString();
            }
            return "";
        }



        private void MoveToFirst(object sender, RoutedEventArgs e)
        {
            string portName = GetPortNameByMenuItem(sender);
            PortTabItem portTabItem = vieModel.PortTabItems.FirstOrDefault(arg => arg.Name.Equals(portName));
            if (portTabItem.Pinned) {
                int oldIndex = -1;
                for (int i = 0; i < vieModel.PortTabItems.Count; i++) {
                    if (vieModel.PortTabItems[i].Name.Equals(portName)) {
                        oldIndex = i;
                        break;
                    }
                }
                if (oldIndex < 0)
                    return;
                vieModel.PortTabItems.Move(oldIndex, 0);
            } else {
                bool hasPinned = false;
                int targetIndex = -1;
                int oldIndex = -1;
                for (int i = 0; i < vieModel.PortTabItems.Count; i++) {
                    if (vieModel.PortTabItems[i].Pinned) {
                        hasPinned = true;
                        targetIndex = i;
                    }

                    if (vieModel.PortTabItems[i].Name.Equals(portName))
                        oldIndex = i;
                }
                if (oldIndex < 0)
                    return;
                if (targetIndex < 0 || targetIndex + 1 >= vieModel.PortTabItems.Count)
                    targetIndex = 0;
                if (hasPinned && targetIndex + 1 < vieModel.PortTabItems.Count)
                    vieModel.PortTabItems.Move(oldIndex, targetIndex + 1);
                else
                    vieModel.PortTabItems.Move(oldIndex, 0);
            }
        }

        private void MoveToLast(object sender, RoutedEventArgs e)
        {
            string portName = GetPortNameByMenuItem(sender);
            PortTabItem portTabItem = vieModel.PortTabItems.FirstOrDefault(arg => arg.Name.Equals(portName));
            if (portTabItem.Pinned) {
                int targetIndex = -1;
                int oldIndex = -1;
                for (int i = 0; i < vieModel.PortTabItems.Count; i++) {
                    if (vieModel.PortTabItems[i].Pinned)
                        targetIndex = i;
                    if (vieModel.PortTabItems[i].Name.Equals(portName))
                        oldIndex = i;
                }
                if (oldIndex < 0 || targetIndex < 0)
                    return;
                vieModel.PortTabItems.Move(oldIndex, targetIndex);
            } else {
                int oldIndex = -1;
                for (int i = 0; i < vieModel.PortTabItems.Count; i++) {
                    if (vieModel.PortTabItems[i].Name.Equals(portName)) {
                        oldIndex = i;
                        break;
                    }
                }
                if (oldIndex < 0)
                    return;
                if (vieModel.PortTabItems.Count - 1 >= 0)
                    vieModel.PortTabItems.Move(oldIndex, vieModel.PortTabItems.Count - 1);
            }
        }

        private async void CloseAllLeftPort(object sender, RoutedEventArgs e)
        {
            string portName = GetPortNameByMenuItem(sender);
            if (vieModel == null || vieModel.PortTabItems == null || vieModel.PortTabItems.Count == 0)
                return;
            List<PortTabItem> list = vieModel.PortTabItems.ToList();
            int idx = -1;
            for (int i = 0; i < list.Count; i++) {
                if (list[i].Name.Equals(portName)) {
                    idx = i;
                    break;
                }
            }
            if (idx <= 0 || idx >= list.Count)
                return;
            List<string> names = new List<string>();
            for (int i = 0; i < idx; i++) {
                if (list[i].Pinned)
                    continue;
                names.Add(list[i].Name);
            }
            vieModel.DoingLongWork = true;
            await RemovePortsByName(names);
            vieModel.DoingLongWork = false;
        }

        private async void CloseAllRightPort(object sender, RoutedEventArgs e)
        {
            string portName = GetPortNameByMenuItem(sender);
            if (vieModel == null || vieModel.PortTabItems == null || vieModel.PortTabItems.Count == 0)
                return;
            List<PortTabItem> list = vieModel.PortTabItems.ToList();
            int idx = -1;
            for (int i = 0; i < list.Count; i++) {
                if (list[i].Name.Equals(portName)) {
                    idx = i;
                    break;
                }
            }
            if (idx >= list.Count - 1)
                return;
            List<string> names = new List<string>();
            for (int i = idx + 1; i < list.Count; i++) {
                if (list[i].Pinned)
                    continue;
                names.Add(list[i].Name);
            }
            vieModel.DoingLongWork = true;
            await RemovePortsByName(names);
            vieModel.DoingLongWork = false;
        }

        private void PinPort(object sender, RoutedEventArgs e)
        {
            string portName = GetPortNameByMenuItem(sender);
            PortTabItem portTabItem = vieModel.PortTabItems.FirstOrDefault(arg => arg.Name.Equals(portName));
            PinPort(portTabItem);
        }


        private void PinPort(PortTabItem portTabItem)
        {
            string portName = portTabItem.Name;
            if (portTabItem.Pinned) {
                // 取消固定
                // todo 所有的都无固定
                int targetIndex = vieModel.PortTabItems.Count;
                int oldIndex = -1;
                for (int i = vieModel.PortTabItems.Count - 1; i >= 0; i--) {
                    if (targetIndex == vieModel.PortTabItems.Count && vieModel.PortTabItems[i].Pinned)
                        targetIndex = i;
                    if (vieModel.PortTabItems[i].Name.Equals(portName))
                        oldIndex = i;
                    if (targetIndex < vieModel.PortTabItems.Count && oldIndex >= 0)
                        break;
                }
                if (oldIndex < 0)
                    return;
                if (targetIndex == vieModel.PortTabItems.Count)
                    targetIndex = 0;
                portTabItem.Pinned = false;
                // 移动到前面
                vieModel.PortTabItems.Move(oldIndex, targetIndex);
            } else {
                // 固定
                // todo 所有的都固定
                int targetIndex = -1;
                int oldIndex = -1;
                for (int i = 0; i < vieModel.PortTabItems.Count; i++) {
                    if (targetIndex < 0 && !vieModel.PortTabItems[i].Pinned)
                        targetIndex = i;
                    if (vieModel.PortTabItems[i].Name.Equals(portName))
                        oldIndex = i;
                    if (targetIndex >= 0 && oldIndex >= 0)
                        break;
                }
                if (targetIndex < 0 || oldIndex < 0)
                    return;
                portTabItem.Pinned = true;
                // 移动到前面
                vieModel.PortTabItems.Move(oldIndex, targetIndex);
            }

            SavePinnedByName(portName, portTabItem.Pinned);

        }

        private void SavePinnedByName(string portName, bool pinned)
        {
            SideComPort sideComPort = vieModel.SideComPorts.FirstOrDefault(arg => arg.Name.Equals(portName));
            if (sideComPort != null && sideComPort.PortTabItem is PortTabItem tabItem) {
                tabItem.SerialPort.SavePinned(pinned);
                ComSettings comSettings = vieModel.ComSettingList.FirstOrDefault(arg => arg.PortName.Equals(portName));
                if (comSettings != null) {
                    Dictionary<string, object> dict = JsonUtils.TryDeserializeObject<Dictionary<string, object>>(comSettings.PortSetting);
                    if (dict != null && dict.ContainsKey("Pinned")) {
                        dict["Pinned"] = pinned.ToString();
                        comSettings.PortSetting = JsonUtils.TrySerializeObject(dict);
                    }
                }
            }
        }


        private void CloseCurrentPort(object sender, RoutedEventArgs e)
        {
            string portName = GetPortNameByMenuItem(sender);
            ClosePortTabItemByName(portName);
        }

        private async void CloseOtherPort(object sender, RoutedEventArgs e)
        {
            string portName = GetPortNameByMenuItem(sender);
            if (vieModel == null || vieModel.PortTabItems == null || vieModel.PortTabItems.Count == 0)
                return;
            List<PortTabItem> list = vieModel.PortTabItems.ToList();
            int idx = -1;
            for (int i = 0; i < list.Count; i++) {
                if (list[i].Name.Equals(portName)) {
                    idx = i;
                    break;
                }
            }
            List<string> names = new List<string>();
            for (int i = idx + 1; i < list.Count; i++) {
                if (list[i].Pinned)
                    continue;
                names.Add(list[i].Name);
            }
            for (int i = 0; i < idx; i++) {
                if (list[i].Pinned)
                    continue;
                names.Add(list[i].Name);
            }
            vieModel.DoingLongWork = true;
            await RemovePortsByName(names);
            vieModel.DoingLongWork = false;
        }

        private async void CloseAllPorts(object sender, RoutedEventArgs e)
        {
            if (vieModel == null || vieModel.PortTabItems == null || vieModel.PortTabItems.Count == 0)
                return;
            List<string> names = vieModel.PortTabItems.Where(arg => !arg.Pinned).Select(arg => arg.Name).ToList();
            vieModel.DoingLongWork = true;
            await RemovePortsByName(names);
            vieModel.DoingLongWork = false;
        }

        private void highlightingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems == null || e.AddedItems.Count == 0)
                return;
            ComboBox comboBox = sender as ComboBox;
            string portName = comboBox.Tag.ToString();
            PortTabItem portTabItem = vieModel.PortTabItems.FirstOrDefault(arg => arg.Name.Equals(portName));
            if (portTabItem != null && portTabItem.TextEditor != null) {
                portTabItem.TextEditor.SyntaxHighlighting = e.AddedItems[0] as IHighlightingDefinition;
                Logger.Debug($"set SyntaxHighlighting: {portTabItem.TextEditor.SyntaxHighlighting}");
            }
        }

        private void ShowHighLightEdit(object sender, RoutedEventArgs e)
        {
            OpenSetting(null, null);
            window_Setting.vieModel.TabSelectedIndex = Window_Setting.HIGH_LIGHT_TAB_INDEX;
        }

        private void CloseAllConnectPort(object sender, RoutedEventArgs e)
        {
            foreach (var item in vieModel.PortTabItems) {
                if (item.Connected)
                    ClosePort(item.Name);
            }
        }

        private void SaveLog(object sender, RoutedEventArgs e)
        {
            PortTabItem portTabItem = null;
            foreach (var item in vieModel.PortTabItems) {
                if (item.Selected) {
                    portTabItem = item;
                    break;
                }
            }


            if (portTabItem != null) {
                string fileName = portTabItem.SaveFileName;
                if (File.Exists(fileName)) {
                    string target = FileHelper.SaveFile(null, null, "Normal text file|*.txt|All types|*.*");
                    if (string.IsNullOrEmpty(target)) {
                        return;
                    }
                    if (!FileHelper.IsProperDirName(target)) {
                        MessageNotify.Error(LangManager.GetValueByKey("FileNameInvalid"));
                        return;
                    } else {
                        // 复制到该目录
                        FileHelper.TryCopyFile(fileName, target, true);
                        FileHelper.TryOpenSelectPath(target);
                        Logger.Info($"save log to {target}");
                    }
                } else {
                    MessageNotify.Warning(LangManager.GetValueByKey("CurrentNoLog"));
                }
            }



        }

        private void pinToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            if (ConfigManager.Settings.HintWhenPin)
                MessageNotify.Info(LangManager.GetValueByKey("PinLogHint"));
        }

        private void OpenAppDir(object sender, RoutedEventArgs e)
        {
            FileHelper.TryOpenPath(AppDomain.CurrentDomain.BaseDirectory);
        }

        private void ShowAscii(object sender, RoutedEventArgs e)
        {
            Window_Ascii window_Ascii = new Window_Ascii((int)ConfigManager.CommonSettings.AsciiSelectedIndex);
            window_Ascii.OnSelectedChanged += (index) => {
                ConfigManager.CommonSettings.AsciiSelectedIndex = index;
                ConfigManager.CommonSettings.Save();
            };
            window_Ascii.Icon = this.Icon;
            window_Ascii.Show();
        }

        private void ShowReferences(object sender, RoutedEventArgs e)
        {
            Window_References reference = new Window_References(UrlManager.REFERENCE_DATAS,
                (int)ConfigManager.CommonSettings.RefSelectedIndex);
            reference.OnSelectedChanged += (index) => {
                ConfigManager.CommonSettings.RefSelectedIndex = index;
                ConfigManager.CommonSettings.Save();
            };
            reference.Icon = this.Icon;
            reference.Show();
        }

        private void OnPortSettingChanged(object sender, RoutedEventArgs e)
        {
            PortSettingChanged(sender);
        }

        private void PortSettingChanged(object sender)
        {
            FrameworkElement frameworkElement = sender as FrameworkElement;
            object tag = (frameworkElement.Parent as FrameworkElement).Tag;
            if (tag != null && tag.ToString() is string portName &&
                !string.IsNullOrEmpty(portName) &&
                vieModel.PortTabItems != null &&
                vieModel.PortTabItems.FirstOrDefault(arg => arg.Name.Equals(portName)) is PortTabItem portTabItem &&
                portTabItem.SerialPort is SerialPortEx port) {
                port.PrintSetting();
            };
        }

        private void OnPortSettingChanged(object sender, TextChangedEventArgs e)
        {
            PortSettingChanged(sender);
        }

        private void OnPortSettingChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            if (!comboBox.IsLoaded)
                return;
            // 等待数据更新后在打印
            Task.Run(async () => {
                await Task.Delay(100);
                await App.GetDispatcher()?.BeginInvoke(DispatcherPriority.Normal, (Action)delegate {
                    PortSettingChanged(sender);
                });
            });

        }

        private void OpenHFAQ(object sender, RoutedEventArgs e)
        {
            FileHelper.TryOpenUrl(UrlManager.WIKI_FAQ);
        }

        private void OpenDevelop(object sender, RoutedEventArgs e)
        {
            FileHelper.TryOpenUrl(UrlManager.WIKI_DEVELOP);
        }

        private void mainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Logger.Info("main window loaded");
        }

        private void ExportAllCmd(object sender, RoutedEventArgs e)
        {
            List<DataBaseInfo> dataBaseInfos = ConfigImport.GetCurrentDataBaseInfo();
            if (dataBaseInfos == null || dataBaseInfos.Count == 0)
                return;

            Window_Import import = new Window_Import(dataBaseInfos, export: true);
            if ((bool)import.ShowDialog(this)) {
                if (import.CurrentBaseInfos == null)
                    return;

                string json = ConfigImport.ExportDataBaseInfo(import.CurrentBaseInfos.ToList());
                if (string.IsNullOrEmpty(json)) {
                    MessageCard.Error(LangManager.GetValueByKey("ExportFailed"));
                    return;
                }

                string fileName = FileHelper.SaveFile(this, filter: ConstValues.FILTER_JSON);

                FileHelper.TryWriteToFile(fileName, json, Encoding.UTF8);
                MessageNotify.Success(LangManager.GetValueByKey("ExportSuccess"));
                FileHelper.TryOpenSelectPath(fileName);
                Logger.Info($"export config to {fileName}");


            }
        }

        private void ImportAllCmd(object sender, RoutedEventArgs e)
        {
            string filePath = FileHelper.SelectFile(this, filter: ConstValues.FILTER_JSON);
            if (!File.Exists(filePath))
                return;

            string content = FileHelper.TryReadFile(filePath);
            if (string.IsNullOrEmpty(content))
                return;


            List<DataBaseInfo> dataBaseInfos = ConfigImport.ParseInfo(content);

            if (dataBaseInfos == null || dataBaseInfos.Count == 0) {
                MessageCard.Error(LangManager.GetValueByKey("ParseFailed"));
                return;
            }
            Window_Import window_Import = new Window_Import(dataBaseInfos, export: false);
            if ((bool)window_Import.ShowDialog(this))
                if (ConfigImport.ImportDataBaseInfo(dataBaseInfos, content)) {
                    RefreshSendCommands();
                    SetComboboxStatus();
                    MessageNotify.Success(LangManager.GetValueByKey("ImportAllSuccess"));
                    MessageCard.Info(LangManager.GetValueByKey("ImportHighLightHint"));
                }
        }

        private void ShowVarMonitor(object sender, RoutedEventArgs e)
        {
            window_Monitor?.Close();
            window_Monitor = new Window_Monitor();
            window_Monitor.Show();
            window_Monitor.Focus();
            window_Monitor.BringIntoView();
        }

        private void RefreshPortsStatus(object sender, RoutedEventArgs e)
        {
            List<SideComPort> sideComPorts = vieModel.SideComPorts.ToList();
            vieModel.InitPortData(LastSortType, LastSortDesc);
            RetainSidePortValue(sideComPorts);
        }

        private void GotoBottom(object sender, RoutedEventArgs e)
        {
            FrameworkElement ele = sender as FrameworkElement;
            if (ele != null && ele.Parent is Grid grid) {
                TextEditor textEditor = FindTextBox(grid);
                if (textEditor != null)
                    textEditor.ScrollToEnd();
            }
        }

        private void GotoTop(object sender, RoutedEventArgs e)
        {
            FrameworkElement ele = sender as FrameworkElement;
            if (ele != null && ele.Parent is Grid grid) {
                TextEditor textEditor = FindTextBox(grid);
                if (textEditor != null)
                    textEditor.ScrollToHome();
            }
        }

        private void PinTabItem(object sender, RoutedEventArgs e)
        {
            FrameworkElement ele = sender as FrameworkElement;
            Grid grid = (ele.Parent as FrameworkElement).Parent as Grid;
            Border baseBorder = grid.Parent as Border;
            string portName = baseBorder.Tag.ToString();
            PortTabItem portTabItem = vieModel.PortTabItems.FirstOrDefault(arg => arg.Name.Equals(portName));
            PinPort(portTabItem);
        }

        private void CloseTabItem(object sender, RoutedEventArgs e)
        {
            FrameworkElement ele = sender as FrameworkElement;
            Grid grid = (ele.Parent as FrameworkElement).Parent as Grid;
            Border baseBorder = grid.Parent as Border;
            string portName = baseBorder.Tag.ToString();
            ClosePortTabItemByName(portName, null);
        }

        private void StartTelnetServer(object sender, RoutedEventArgs e)
        {
            if (Window_TelnetServer == null) {
                Window_TelnetServer = new Window_TelnetServer();
                Window_TelnetServer.Show();
            } else {
                if (Window_TelnetServer.IsClosed) {
                    Window_TelnetServer = new Window_TelnetServer();
                    Window_TelnetServer.Show();
                }
                Window_TelnetServer.BringIntoView();
                Window_TelnetServer.Focus();
            }
        }

        private void onPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.XButton1 == MouseButtonState.Pressed) {
                SetPortTabSelected(true);
            } else if (e.XButton2 == MouseButtonState.Pressed) {
                SetPortTabSelected(false);
            }
        }

        private void ShowHexCheckSettings(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement ele && ele.Parent is Grid grid &&
                grid.Children.OfType<Popup>().FirstOrDefault() is Popup popup) {
                popup.IsOpen = true;
            }
        }

        private PortTabItem GetCurrentPort(object sender)
        {
            if (sender is FrameworkElement ele &&
                ele.Tag != null && ele.Tag.ToString() is string portName &&
                vieModel.PortTabItems.FirstOrDefault(arg => arg.Name.Equals(portName)) is PortTabItem portTabItem) {
                return portTabItem;
            }
            return null;
        }

        private void SaveDataCheck(object sender)
        {
            if (GetCurrentPort(sender) is PortTabItem portTabItem) {
                portTabItem.SerialPort.SaveDataCheck();
                ComSettings comSettings = vieModel.ComSettingList.FirstOrDefault(arg => arg.PortName.Equals(portTabItem.Name));
                if (comSettings != null) {
                    Dictionary<string, object> dict = JsonUtils.TryDeserializeObject<Dictionary<string, object>>(comSettings.PortSetting);
                    if (dict != null && dict.ContainsKey("DataCheck")) {
                        dict["DataCheck"] = portTabItem.SerialPort.DataCheck;
                        comSettings.PortSetting = JsonUtils.TrySerializeObject(dict);
                        Logger.Info($"set datacheck");
                        portTabItem.RefreshSendHexValue();
                    }
                }
            }
        }

        private void SaveDataCheck(object sender, RoutedEventArgs e) => SaveDataCheck(sender);

        private void SaveDataCheck(object sender, SelectionChangedEventArgs e) => SaveDataCheck(sender);

        private void SaveDataCheck(object sender, TextChangedEventArgs e) => SaveDataCheck(sender);
    }
}
