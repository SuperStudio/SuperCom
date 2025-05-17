using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Search;
using SuperCom.Config;
using SuperCom.Core.Entity;
using SuperCom.Core.Events;
using SuperCom.Core.Interfaces;
using SuperCom.Core.Settings;
using SuperCom.Entity;
using SuperCom.Entity.Enums;
using SuperCom.ViewModel;
using SuperControls.BlankWindow;
using SuperControls.Style;
using SuperControls.Style.Utils;
using SuperControls.Style.Windows;
using SuperUtils.Common;
using SuperUtils.Framework.ORM.Enums;
using SuperUtils.IO;
using SuperUtils.Time;
using SuperUtils.WPF.VisualTools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using static SuperCom.App;

namespace SuperCom.Controls.DataTemplate
{
    /// <summary>
    /// ComTemplate.xaml 的交互逻辑
    /// </summary>
    public partial class ComTemplate : UserControl, IConnectTemplate
    {

        private const int DEFAULT_PORT_OPEN_INTERVAL = 100;

        /// <summary>
        /// HEX 转换工具中最大长度
        /// </summary>
        private const int MAX_TRANSFORM_SIZE = 100000;

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged([CallerMemberName] string name = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }



        private AbstractConnector _Connector;
        public AbstractConnector Connector {
            get { return _Connector; }
            set {
                _Connector = value;
                RaisePropertyChanged();
            }
        }

        public Action<TabInfo> OnConnectStatusChanged { get; set; }

        /// <summary>
        /// 时间戳转换中最长时间戳长度
        /// </summary>
        private const int MAX_TIMESTAMP_LENGTH = 100;

        private ComTemplateVieModel _vieModel;
        public ComTemplateVieModel vieModel {
            get { return _vieModel; }
            set {
                _vieModel = value;
                RaisePropertyChanged();
            }
        }
        private SendCommand CurrentEditCommand { get; set; }

        public bool IsFirstCreate { get; set; } = true;

        public ComTemplate(string name)
        {
            InitializeComponent();

            Name = name;

            Init();
        }

        private void Init()
        {
            vieModel = new ComTemplateVieModel();
            SetComConfig();
            this.DataContext = Connector;
        }

        private async void SetComConfig()
        {
            string name = Name;
            ComConnector comConnector = new ComConnector(name, true);
            comConnector.Setting = PortSetting.GetDefaultSetting();

            if (comConnector.SerialPort == null)
                comConnector.SerialPort = new SerialPortEx(name);

            // 从配置里读取
            ComSettings comSettings = GlobalSettings.ComSetting.GetComSetting(name);
            if (comSettings != null) {
                comConnector.WriteData = comSettings.WriteData;
                comConnector.AddTimeStamp = comSettings.AddTimeStamp;
                comConnector.AddNewLineWhenWrite = comSettings.AddNewLineWhenWrite;
                comConnector.SendHex = comSettings.SendHex;
                comConnector.RecvShowHex = comSettings.RecvShowHex;
                comConnector.EnabledFilter = comSettings.EnabledFilter;
                comConnector.EnabledMonitor = comSettings.EnabledMonitor;
                comConnector.SerialPort.SetPortSettingByJson(comSettings.PortSetting);
                comConnector.Remark = comConnector.SerialPort.Remark;
                comConnector.Pinned = comConnector.SerialPort.Pinned;
            }
            comConnector.Selected = true;
            Connector = comConnector;

            await Task.Run(async () => {
                await Task.Delay(500);
                Dispatcher.Invoke(() => {
                    SetComboboxStatus();
                });
            });
            //ScrollIntoView(comConnector); // todo
        }

        public void SetComboboxStatus()
        {
            ComboBox comboBox = commandsComboBox;
            if (comboBox != null) {
                if (vieModel.CommandsSelectIndex < comboBox.Items.Count)
                    comboBox.SelectedIndex = vieModel.CommandsSelectIndex;
                else
                    comboBox.SelectedIndex = 0;
            }
        }


        private void onPreviewKeyDown(object sender, KeyEventArgs e)
        {

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
            FixedTextEditor(true);
        }

        private void FixedTextEditor(bool fixedText)
        {
            // 将文本固定
            if (!ConfigManager.Settings.FixedWhenFocus)
                return;
            if (Connector != null && Connector.FixedText != fixedText)
                Connector.FixedText = fixedText;
        }

        private void textBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextEditor textEditor = sender as TextEditor;
            if (textEditor != null && textEditor.Parent is Border border)
                border.BorderBrush = Brushes.Transparent;
        }

        private void textEditor_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (ConfigManager.Settings.PinOnMouseWheel &&
                sender is TextEditor textEditor &&
                textEditor.IsLoaded && textEditor.Parent is Border border) {
                if (e.Delta > 0) {
                    FixedTextEditor(true);
                } else {
                    // 鼠标滚动到底部
                    TextView textView = textEditor.TextArea.TextView;
                    bool isAtEnd = textView.VerticalOffset + textView.ActualHeight + 1 >= textView.DocumentHeight;
                    if (isAtEnd)
                        FixedTextEditor(false);
                }
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

        private void GotoBottom(object sender, RoutedEventArgs e)
        {
            FrameworkElement ele = sender as FrameworkElement;
            if (ele != null && ele.Parent is Grid grid) {
                TextEditor textEditor = FindTextBox(grid);
                if (textEditor != null) {
                    textEditor.ScrollToEnd();
                    if (ConfigManager.Settings.PinOnMouseWheel)
                        FixedTextEditor(false);
                }
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

        private async void ConnectPort(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button) || button.Tag == null || button.Content == null)
                return;
            button.IsEnabled = false;
            await ConnectPort(button.Tag.ToString(), button.Content.ToString().Equals(LangManager.GetValueByKey("Connect")));
            button.IsEnabled = true;
        }

        public async Task<bool> ConnectPort(object data, bool open)
        {
            if (data is string name) {
                if (open)
                    await Open();
                else
                    await Close();
            }
            return true;
        }

        private void SendStatusText(string text)
        {
            BasicEventManager.SendEvent(EventType.StatusText, text);
        }

        public async Task<bool> Open()
        {
            bool ret = false;
            await Task.Delay(DEFAULT_PORT_OPEN_INTERVAL);
            Connector.TextEditor = textEditor;
            if (IsFirstCreate) {
                SetTextEditorConfig();
                // 设置语法高亮
                ComConnector portTabItem = Connector as ComConnector;
                int idx = (int)portTabItem.SerialPort.HighLightIndex;
                Connector.TextEditor.SyntaxHighlighting = GlobalSettings.HighLightSetting[idx];
                Logger.Info($"set HighLightIndex = {idx}");
            }
            Connector.TextEditor.TextChanged += Connector.TextBox_TextChanged; // 默认自动滚动
            Connector.RX = 0;
            Connector.TX = 0;
            Connector.CurrentCharSize = 0;
            Connector.FragCount = 0;

            await Task.Run(() => {
                Dispatcher.Invoke(() => {
                    try {
                        ComConnector portTabItem = Connector as ComConnector;
                        SerialPortEx serialPort = portTabItem.SerialPort;
                        if (!serialPort.IsOpen) {
                            //serialPort.WriteTimeout = CustomSerialPort.WRITE_TIME_OUT;
                            //serialPort.ReadTimeout = CustomSerialPort.READ_TIME_OUT;
                            serialPort.PrintSetting();
                            serialPort.Open();
                            portTabItem.ConnectTime = DateTime.Now;
                            portTabItem.SaveFileName = portTabItem.GetDefaultFileName();
                            SetPortConnectStatus(true);
                            portTabItem.Open();
                        }
                        ret = true;
                        OnConnectStatusChanged?.Invoke(new TabInfo(Core.Entity.Enums.ConnectType.Com, true, Name));
                    } catch (Exception ex) {
                        string msg = $"{LangManager.GetValueByKey("OpenPortFailed")}: {Name} => {ex.Message}";
                        MessageCard.Error(msg);
                        SendStatusText(msg);
                        BasicEventManager.SendEvent(EventType.RemoveTabBar, Name);
                        SetPortConnectStatus(false);
                    }
                });
            });
            Logger.Info($"open port：{Name} ret: {ret}");
            return ret;
        }

        private void SetTextEditorConfig()
        {
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

        public async Task<bool> Close()
        {
            bool ret = false;
            do {
                if (Connector == null || !Connector.Connected) {
                    ret = true;
                    break;
                }
                ComConnector connector = Connector as ComConnector;
                SerialPortEx serialPort = connector.SerialPort;
                if (serialPort == null) {
                    ret = true;
                    break;
                }

                bool success = await AsyncClosePort(serialPort);
                Logger.Info($"close port：{Name} ret: {success}");
                if (!success) {
                    MessageNotify.Error($"{LangManager.GetValueByKey("ClosePortTimeout")}: {serialPort.PortName}");
                    ret = false;
                    break;
                }

                connector.Close();
                SetPortConnectStatus(false);
            } while (false);

            // 保存界面
            SaveComSetting();
            return ret;
        }


        private void SaveComSetting()
        {
            ComConnector comConnector = Connector as ComConnector;
            ComSettings comSettings = GlobalSettings.ComSetting.GetComSetting(comConnector.Name);
            if (comSettings == null)
                comSettings = new ComSettings();
            comSettings.PortName = comConnector.Name;
            comSettings.Connected = comConnector.Connected;
            comSettings.WriteData = comConnector.WriteData;
            comSettings.AddNewLineWhenWrite = comConnector.AddNewLineWhenWrite;
            comSettings.SendHex = comConnector.SendHex;
            comSettings.RecvShowHex = comConnector.RecvShowHex;
            comSettings.EnabledFilter = comConnector.EnabledFilter;
            comSettings.EnabledMonitor = comConnector.EnabledMonitor;
            comSettings.AddTimeStamp = comConnector.AddTimeStamp;
            comConnector.SerialPort.RefreshSetting();
            comSettings.PortSetting = comConnector.SerialPort?.SettingJson;

            MapperManager.ComMapper.Insert(comSettings, InsertMode.Replace);
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

        private void SetPortConnectStatus(bool status)
        {
            try {
                Connector.Connected = status;
                OnConnectStatusChanged?.Invoke(new TabInfo(Core.Entity.Enums.ConnectType.Com, status, Name));
            } catch (Exception ex) {
                MessageCard.Error(ex.Message);
            }
        }

        private void ClearData(object sender, RoutedEventArgs e)
        {
            StackPanel stackPanel = (sender as Button).Parent as StackPanel;
            if (stackPanel != null && stackPanel.Parent is Border border && border.Parent is Grid rootGrid) {
                if (rootGrid.Tag == null)
                    return;
                string portName = rootGrid.Tag.ToString();
                FindTextBox(rootGrid)?.Clear();
                if (Connector != null) {
                    Connector.ClearData();
                    Connector.RX = Connector.TX = 0;
                    Logger.Info($"clear data: {portName}");
                }
            }
        }

        private void pinToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            if (ConfigManager.Settings.HintWhenPin)
                MessageNotify.Info(LangManager.GetValueByKey("PinLogHint"));
        }

        private void OpenPath(object sender, RoutedEventArgs e)
        {
            if (Connector != null) {
                Logger.Info($"open log dir, portName: {Connector.Name}");
                string fileName = Connector.SaveFileName;
                if (File.Exists(fileName)) {
                    FileHelper.TryOpenSelectPath(fileName);
                    if (Connector.FragCount > 0)
                        MessageNotify.Info($"{LangManager.GetValueByKey("LogFragWithCount")} {Connector.FragCount}");
                } else {
                    MessageNotify.Warning($"{LangManager.GetValueByKey("CurrentNoLog")}");
                }

            }

        }

        private void OpenByDefaultApp(object sender, RoutedEventArgs e)
        {
            if (Connector != null) {
                Logger.Info($"open log file, portName: {Connector.Name}");
                string fileName = Connector.SaveFileName;
                if (File.Exists(fileName)) {
                    FileHelper.TryOpenByDefaultApp(fileName);
                } else {
                    MessageCard.Warning(LangManager.GetValueByKey("CurrentNoLog"));
                }

            }
        }

        private bool SetNewSaveFileName(string portName)
        {
            if (Connector == null)
                return false;

            Connector.ConnectTime = DateTime.Now;
            string defaultName = Connector.GetDefaultFileName();
            string originFileName = Path.GetFileNameWithoutExtension(defaultName);
            DialogInput dialogInput = new DialogInput(LangManager.GetValueByKey("PleaseEnterNewFileName"), originFileName);

            if (!(bool)dialogInput.ShowDialog())
                return false;

            if (dialogInput.Text is string newName &&
                !string.IsNullOrEmpty(newName) &&
                newName.ToProperFileName() is string name) {

                if (name.ToLower().Equals(originFileName.ToLower())) {
                    // 文件名未变化，使用默认方式
                    Connector.SaveFileName = defaultName;
                    return true;
                }

                string targetFileName = Connector.GetCustomFileName(newName);
                if (File.Exists(targetFileName)) {
                    if (!(bool)(new MsgBox(LangManager.GetValueByKey("FileExistAskForAppend")).ShowDialog())) {
                        return false;
                    }
                }
                // 保存为新文件名
                Connector.SaveFileName = targetFileName;
                return true;
            } else {
                MessageNotify.Error(LangManager.GetValueByKey("FileNameInvalid"));
                return false;
            }
        }

        private async void SaveToNewFile(object sender, RoutedEventArgs e)
        {
            FrameworkElement ele = sender as FrameworkElement;
            if (Connector == null || Connector == null)
                return;
            ele.IsEnabled = false;
            string portName = Connector.Name;
            if (!string.IsNullOrEmpty(portName)) {
                if (SetNewSaveFileName(portName)) {
                    MessageNotify.Success(LangManager.GetValueByKey("LogSaveAsOK"));
                    await Task.Delay(500); // 防止频繁点保存
                }
            }
            ele.IsEnabled = true;
        }

        private void PortSettingChanged(object sender)
        {
            FrameworkElement frameworkElement = sender as FrameworkElement;
            object tag = (frameworkElement.Parent as FrameworkElement).Tag;
            if (Connector != null && Connector is ComConnector portTabItem && portTabItem.SerialPort is SerialPortEx port)
                port.PrintSetting();
        }

        private void OnPortSettingChanged(object sender, RoutedEventArgs e)
        {
            PortSettingChanged(sender);
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

            if (!(bool)new MsgBox($"{LangManager.GetValueByKey("RestoreSpec")}: {name} ?").ShowDialog())
                return;
            if (Connector != null && Connector is ComConnector portTabItem && portTabItem.SerialPort is SerialPortEx port) {
                port.RestoreDefault();
                port.PrintSetting();
            }
        }

        public void OnBaudRateChange()
        {
            ComConnector comConnector = Connector as ComConnector;
            // 设置下标
            ComboBox comboBox = baudRateComboBox;
            int number = comConnector.SerialPort.BaudRate;
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

        public void OnHighLightChange(IHighlightingDefinition definition)
        {
            ComConnector comConnector = Connector as ComConnector;
            SerialPortEx serialPortEx = comConnector.SerialPort;
            if (serialPortEx != null) {
                // todo
                // 获取 idx
                //GlobalSettings.HighLightSetting.GetHashCode


                //serialPortEx.HighLightIndex = idx;
            }
        }

        private void AddNewBaudRate(ComboBox comboBox, string origin, string baudRateText)
        {
            Logger.Info("add new baudrate");
            // 记录原来的下标
            int index = 0;

            ObservableCollection<string> BaudRates = GlobalSettings.ComSetting.BaudRates;

            if (!string.IsNullOrEmpty(origin)) {
                for (int i = 0; i < BaudRates.Count; i++) {
                    if (BaudRates[i].Equals(origin)) {
                        index = i;
                        break;
                    }
                }
            }
            DialogInput dialogInput = new DialogInput(Window_Setting.INPUT_NOTICE_TEXT);
            bool success = false;
            if ((bool)dialogInput.ShowDialog()) {
                string value = dialogInput.Text;

                if (!string.IsNullOrEmpty(value) && int.TryParse(value, out int baudrate) &&
                    !BaudRates.Contains(baudrate.ToString())) {
                    Logger.Info($"new baudrate = {value}");
                    BaudRates.RemoveAt(BaudRates.Count - 1);
                    BaudRates.Add(baudrate.ToString());
                    BaudRates.Add(ComSettingManager.DEFAULT_ADD_TEXT);
                    success = true;
                    comboBox.SelectedIndex = BaudRates.Count - 2;
                    // 保存当前项目
                    SaveBaudRate(baudRateText);
                    GlobalSettings.ComSetting.SaveBaudRate();
                }

            }
            if (!success) {
                comboBox.SelectedIndex = index;
            }
        }

        private void BaudRate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            if (!comboBox.IsLoaded)
                return;

            if (e.AddedItems == null || e.AddedItems.Count <= 0)
                return;
            string baudRateText = e.AddedItems[0].ToString();
            if (ComSettingManager.DEFAULT_ADD_TEXT.Equals(baudRateText)) {
                string origin = e.RemovedItems[0].ToString();
                AddNewBaudRate(comboBox, origin, baudRateText);
            } else {
                Logger.Info($"set baudrate: {baudRateText}");

            }

        }

        public void SaveBaudRate(string baudRate)
        {
            List<ComSettings> list = GlobalSettings.ComSetting.GetComSettings();
            for (int i = 0; i < list.Count; i++) {
                ComSettings comSettings = list[i];
                if (!comSettings.PortName.Equals(Name))
                    continue;
                int.TryParse(baudRate, out int value);
                if (value <= 0)
                    return;
                ComConnector comConnector = Connector as ComConnector;
                comConnector.SerialPort.BaudRate = value;
                comSettings.PortSetting = comConnector.SerialPort.PortSettingToJson();
                MapperManager.ComMapper.UpdateFieldById("PortSetting", comSettings.PortSetting, comSettings.Id);
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

        private void highlightingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems == null || e.AddedItems.Count == 0)
                return;
            if (Connector is ComConnector portTabItem && portTabItem.TextEditor != null &&
                e.AddedItems[0] is IHighlightingDefinition syntaxHighlighting) {
                portTabItem.TextEditor.SyntaxHighlighting = syntaxHighlighting;
                Logger.Debug($"set SyntaxHighlighting: {portTabItem.TextEditor.SyntaxHighlighting}");
            } else {
                Logger.Error("set SyntaxHighlighting failed");
            }
        }


        private void ShowHighLightEdit(object sender, RoutedEventArgs e)
        {
            if (Connector is ComConnector comConnector &&
                comConnector.SerialPort is SerialPortEx serialPortEx && serialPortEx != null) {
                BasicEventManager.SendEvent(EventType.ShowHighLight, (int)serialPortEx.HighLightIndex);
            }
        }

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

        private void StartSendCommands(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == null || button.Tag == null)
                return;
            StackPanel stackPanel = button.Parent as StackPanel;
            ComboBox comboBox = stackPanel.Children.OfType<ComboBox>().LastOrDefault();
            string portName = button.Tag.ToString();
            if (comboBox != null && comboBox.SelectedValue != null &&
                GlobalSettings.SendCommand.Count > 0) {
                string projectID = comboBox.SelectedValue.ToString();

                // 开始执行队列
                AdvancedSend advancedSend = GlobalSettings.SendCommand[projectID];
                ComConnector portTabItem = Connector as ComConnector;
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

        public void SetRunningStatus(Button button, bool running)
        {
            button.IsEnabled = !running;
            StackPanel stackPanel = button.Parent as StackPanel;
            Button stopButton = stackPanel.Children.OfType<Button>().LastOrDefault();
            if (stopButton != null)
                stopButton.IsEnabled = running;
        }

        private void StopSendCommands(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == null || button.Tag == null)
                return;
            StackPanel stackPanel = button.Parent as StackPanel;
            ComboBox comboBox = stackPanel.Children.OfType<ComboBox>().LastOrDefault();
            string portName = button.Tag.ToString();
            ComConnector portTabItem = Connector as ComConnector;
            if (comboBox != null && comboBox.SelectedValue != null &&
                GlobalSettings.SendCommand.Count > 0 && portTabItem != null) {
                string projectID = comboBox.SelectedValue.ToString();
                // 执行队列
                AdvancedSend advancedSend = GlobalSettings.SendCommand[projectID];
                if (advancedSend == null)
                    return;

                Logger.Info($"stop run command: {advancedSend.ProjectName}");

                portTabItem.RunningCommands = false;
                if (advancedSend.CommandList?.Count > 0)
                    foreach (var item in advancedSend.CommandList)
                        item.Status = RunningStatus.WaitingToRun;

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
            AdvancedSend advancedSend = GlobalSettings.SendCommand[id];
            vieModel.CurrentAdvancedSend = advancedSend;
            Logger.Info($"set current run project: {advancedSend.ProjectName}");
            if (!string.IsNullOrEmpty(advancedSend.Commands)) {
                itemsControl.ItemsSource = JsonUtils.TryDeserializeObject<List<SendCommand>>(advancedSend.Commands).OrderBy(arg => arg.Order);
                vieModel.CommandsSelectIndex = comboBox.SelectedIndex;
                ConfigManager.Main.CommandsSelectIndex = comboBox.SelectedIndex;
                ConfigManager.Main.Save();
            }
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
            ComConnector portTabItem = Connector as ComConnector;
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
            }

            portTabItem.SendCustomCommand(command);
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

                        GlobalSettings.SendCommand.UpdateProject(advancedSend);
                        BasicEventManager.SendEvent(EventType.OnSendCommandModify, null);
                    }

                }
            }

        }

        public void SendCommand(string portName)
        {
            ComConnector portTabItem = Connector as ComConnector;
            if (portTabItem == null)
                return;

            string value = portTabItem.WriteData;
            portTabItem.SendCommand(value);
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

        /// <summary>
        /// CRC校验
        /// </summary>
        /// <param name="sender"></param>
        private void SaveDataCheck(object sender)
        {
            if (Connector is ComConnector portTabItem) {
                portTabItem.SerialPort.SaveDataCheck();
                ComSettings comSettings = GlobalSettings.ComSetting.GetComSetting(Name);
                if (comSettings != null) {
                    Dictionary<string, object> dict = JsonUtils.TryDeserializeObject<Dictionary<string, object>>(comSettings.PortSetting);
                    if (dict != null && dict.ContainsKey("DataCheck")) {
                        dict["DataCheck"] = portTabItem.SerialPort.DataCheck;
                        comSettings.PortSetting = JsonUtils.TrySerializeObject(dict);
                        Logger.Info($"set datacheck");
                        portTabItem.RefreshSendHexValue();
                        portTabItem.SerialPort.SaveDataCheck();
                    }
                }
            }
        }

        private void SaveDataCheck(object sender, RoutedEventArgs e) => SaveDataCheck(sender);

        private void SaveDataCheck(object sender, SelectionChangedEventArgs e) => SaveDataCheck(sender);

        private void SaveDataCheck(object sender, TextChangedEventArgs e) => SaveDataCheck(sender);


        private void ShowHexCheckSettings(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement ele && ele.Parent is Grid grid &&
                grid.Children.OfType<Popup>().FirstOrDefault() is Popup popup) {
                popup.IsOpen = true;
            }
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
                if (ConfigManager.CommonSettings.FixedOnSendCommand && Connector is ComConnector portTabItem)
                    portTabItem.FixedText = true;
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
                    if (Connector is ComConnector portTabItem) {
                        portTabItem.WriteData = value;
                        popup.IsOpen = false;
                        TextBox textBox = (popup.Parent as Grid).Children.OfType<Border>().FirstOrDefault().Child as TextBox;
                        textBox.CaretIndex = textBox.Text.Length;
                    }
                }
            }
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

        private void FormatJson(object sender, RoutedEventArgs e)
        {
            TextEditor textEditor = Connector.TextEditor;
            if (textEditor == null)
                return;
            string origin = textEditor.SelectedText;
            string format = FormatString(FormatType.JSON, origin);
            textEditor.SelectedText = format;
        }

        private void JoinLine(object sender, RoutedEventArgs e)
        {
            TextEditor textEditor = Connector.TextEditor;
            if (textEditor == null)
                return;
            string origin = textEditor.SelectedText;
            string format = FormatString(FormatType.JOINLINE, origin);
            textEditor.SelectedText = format;
        }

        private void HexToStr(object sender, RoutedEventArgs e)
        {
            StrTextBox.Text = TransformHelper.HexToStr(HexTextBox.Text);
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

        private void OpenHexTransform(object sender, RoutedEventArgs e)
        {
            if (Connector == null || Connector.TextEditor == null)
                return;
            string text = Connector.TextEditor.SelectedText;
            OpenHex(text);
        }

        private void OpenTimeTransform(object sender, RoutedEventArgs e)
        {
            if (Connector == null || Connector.TextEditor == null)
                return;
            string text = Connector.TextEditor.SelectedText;
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

        private void CopyCommand(object sender, RoutedEventArgs e)
        {
            string text = editTextBoxCommand.Text;
            if (!string.IsNullOrEmpty(text))
                ClipBoard.TrySetDataObject(text);

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
                GlobalSettings.SendCommand.UpdateProject(advancedSend);
                BasicEventManager.SendEvent(EventType.OnSendCommandModify, null);
            }
        }

        private void EditCommandCancel(object sender, RoutedEventArgs e)
        {
            editSendCommandPopup.IsOpen = false;
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

        private void ClearHexSpace(object sender, RoutedEventArgs e)
        {
            string text = HexTextBox.Text;
            text = text.Trim().Replace(" ", "");
            HexTextBox.Text = text;
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

        private void LocalTimeToTimeStamp(object sender, RoutedEventArgs e)
        {
            bool success = DateTime.TryParse(LocalTimeTextBox.Text, out DateTime dt);
            if (!success) {
                TimeStampTextBox.Text = LangManager.GetValueByKey("ParseFailed");
            } else {
                TimeStampTextBox.Text = DateHelper.DateTimeToUnixTimeStamp(dt, TimeComboBox.SelectedIndex == 0).ToString();
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

        private void StrToHex(object sender, RoutedEventArgs e)
        {
            string text = TransformHelper.StrToHex(StrTextBox.Text);
            if ((bool)HexToStrSwitch.IsChecked) {
                HexTextBox.Text = text;
            } else {
                HexTextBox.Text = text.ToLower();
            }

        }
    }
}
