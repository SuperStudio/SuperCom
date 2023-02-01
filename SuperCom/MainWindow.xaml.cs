
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Search;
using SuperCom.Config;
using SuperCom.Config.WindowConfig;
using SuperCom.CustomWindows;
using SuperCom.Entity;
using SuperCom.Upgrade;
using SuperCom.ViewModel;
using SuperCom.Windows;
using SuperControls.Style;
using SuperControls.Style.Plugin;
using SuperControls.Style.Windows;
using SuperControls.Style.XAML.CustomWindows;
using SuperUtils.Common;
using SuperUtils.Framework.ORM.Mapper;
using SuperUtils.IO;
using SuperUtils.Time;
using SuperUtils.WPF.VisualTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Xml;

namespace SuperCom
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : BaseWindow
    {
        private const double DEFAULT_SEND_PANEL_HEIGHT = 186;
        private const int DEFAULT_PORT_OPEN_INTERVAL = 100;

        Window_Setting window_Setting { get; set; }
        public VieModel_Main vieModel { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            Init();
        }

        public void Init()
        {
            this.MaximumToNormal += (s, e) =>
            {
                MaxPath.Data = Geometry.Parse(PathData.MaxPath);
                MaxMenuItem.Header = "最大化";
            };

            this.NormalToMaximum += (s, e) =>
            {
                MaxPath.Data = Geometry.Parse(PathData.MaxToNormalPath);
                MaxMenuItem.Header = "窗口化";
            };
            InitSqlite();
            ConfigManager.InitConfig(); // 读取配置
            // 注册 SuperUtils 异常事件
            SuperUtils.Handler.ExceptionHandler.OnError += (e) =>
            {
                // MessageCard.Error(e.Message); 
            };
            SuperUtils.Handler.LogHandler.OnLog += (msg) =>
            {
                Console.WriteLine(msg);
            };
            vieModel = new VieModel_Main();
            this.DataContext = vieModel;
            SetLang();      // 设置语言
            ReadConfig();   // 读取设置列表
            PathManager.Init();
        }

        public void ReadConfig()
        {
            SqliteMapper<ComSettings> mapper = new SqliteMapper<ComSettings>(ConfigManager.SQLITE_DATA_PATH);
            vieModel.ComSettingList = mapper.SelectList().ToHashSet();

            // 设置配置
            foreach (var item in vieModel.SideComPorts)
            {
                ComSettings comSettings = vieModel.ComSettingList.Where(arg => arg.PortName.Equals(item.Name)).FirstOrDefault();
                if (comSettings != null && !string.IsNullOrEmpty(comSettings.PortSetting))
                {
                    item.Remark = CustomSerialPort.GetRemark(comSettings.PortSetting);
                }
            }
            //textWrapMenuItem.IsChecked = vieModel.AutoTextWrap;
        }

        private void SetLang()
        {
            // 设置语言
            if (!string.IsNullOrEmpty(ConfigManager.Settings.CurrentLanguage)
                && SuperControls.Style.LangManager.SupportLanguages.Contains(ConfigManager.Settings.CurrentLanguage))
            {
                SuperControls.Style.LangManager.SetLang(ConfigManager.Settings.CurrentLanguage);
                SuperCom.Lang.LangManager.SetLang(ConfigManager.Settings.CurrentLanguage);
            }
        }

        public void RefreshSetting()
        {
            this.CloseToTaskBar = ConfigManager.CommonSettings.CloseToBar;
        }

        public void ReadXshdList()
        {
            // 记录先前选定的
            Dictionary<string, long> selectDict = new Dictionary<string, long>();
            if (vieModel.PortTabItems?.Count > 0)
            {
                foreach (PortTabItem item in vieModel.PortTabItems)
                {
                    if (item.SerialPort == null) continue;
                    selectDict.Add(item.Name, item.SerialPort.HighLightIndex);

                }
            }

            HighlightingManager.Instance.Clear();
            string[] xshd_list = FileHelper.TryGetAllFiles(HighLightRule.GetDirName(), "*.xshd");
            foreach (var xshdPath in xshd_list)
            {
                try
                {
                    IHighlightingDefinition customHighlighting;
                    using (Stream s = File.OpenRead(xshdPath))
                    {
                        if (s == null)
                            throw new InvalidOperationException("Could not find embedded resource");
                        using (XmlReader reader = new XmlTextReader(s))
                        {
                            customHighlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.
                                HighlightingLoader.Load(reader, HighlightingManager.Instance);
                        }
                    }
                    // and register it in the HighlightingManager
                    HighlightingManager.Instance.RegisterHighlighting(customHighlighting.Name, null, customHighlighting);
                }
                catch (Exception ex)
                {
                    MessageCard.Error(ex.Message);
                    continue;
                }

            }

            vieModel.LoadHighlightingDefinitions();


            // 恢复选中项
            if (vieModel.PortTabItems?.Count > 0)
            {
                foreach (PortTabItem item in vieModel.PortTabItems)
                {
                    if (item.SerialPort == null || !selectDict.ContainsKey(item.Name)) continue;
                    long idx = selectDict[item.Name];
                    if (idx >= vieModel.HighlightingDefinitions.Count)
                        idx = 0;
                    item.SerialPort.HighLightIndex = idx;
                }
            }
        }


        private void InitSqlite()
        {
            ComSettings.InitSqlite();
            AdvancedSend.InitSqlite();
            HighLightRule.InitSqlite();
            ShortCutBinding.InitSqlite();
            VarMonitor.InitSqlite();
        }


        public new void MinWindow(object sender, RoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Minimized;
        }


        public void OnMaxWindow(object sender, RoutedEventArgs e)
        {
            this.MaxWindow(sender, e);
        }

        private void OnMoveWindow(object sender, MouseEventArgs e)
        {
            base.MoveWindow(sender, e);
            //Border border = sender as Border;

            ////移动窗口
            //if (e.LeftButton == MouseButtonState.Pressed)
            //{
            //    if (baseWindowState == BaseWindowState.Maximized || (this.Width == SystemParameters.WorkArea.Width && this.Height == SystemParameters.WorkArea.Height))
            //    {
            //        baseWindowState = 0;
            //        double fracWidth = e.GetPosition(border).X / border.ActualWidth;
            //        this.Width = WindowSize.Width;
            //        this.Height = WindowSize.Height;
            //        this.WindowState = System.Windows.WindowState.Normal;
            //        this.Left = e.GetPosition(border).X - border.ActualWidth * fracWidth;
            //        this.Top = e.GetPosition(border).Y - border.ActualHeight / 2;
            //        this.OnLocationChanged(EventArgs.Empty);
            //        MaxPath.Data = Geometry.Parse(PathData.MaxPath);
            //        MaxMenuItem.Header = "最大化";
            //    }
            //    this.DragMove();
            //}
        }

        private void SetPortSelected(object sender, MouseButtonEventArgs e)
        {
            Border border = (Border)sender;
            if (border == null || border.Tag == null) return;
            string portName = border.Tag.ToString();
            if (string.IsNullOrEmpty(portName) || vieModel.PortTabItems == null ||
                vieModel.PortTabItems.Count <= 0) return;
            SetPortTabSelected(portName);
        }

        public void SetPortTabSelected(string portName)
        {
            if (vieModel.PortTabItems == null) return;
            for (int i = 0; i < vieModel.PortTabItems.Count; i++)
            {
                if (vieModel.PortTabItems[i].Name.Equals(portName))
                {
                    vieModel.PortTabItems[i].Selected = true;
                    SetGridVisible(portName);
                }
                else
                {
                    vieModel.PortTabItems[i].Selected = false;
                }
            }
        }

        private void SetGridVisible(string portName)
        {
            if (string.IsNullOrEmpty(portName)) return;
            for (int i = 0; i < itemsControl.Items.Count; i++)
            {
                ContentPresenter presenter = (ContentPresenter)itemsControl.ItemContainerGenerator.ContainerFromItem(itemsControl.Items[i]);
                if (presenter == null) continue;
                Grid grid = VisualHelper.FindElementByName<Grid>(presenter, "baseGrid");
                if (grid == null || grid.Tag == null) continue;
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


        private void CloseTabItem(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Grid grid = (button.Parent as Border).Parent as Grid;
            Border border = grid.Parent as Border;
            string portName = border.Tag.ToString();
            if (string.IsNullOrEmpty(portName) || vieModel.PortTabItems?.Count <= 0) return;
            RemovePortTabItem(portName, button);
            // 默认选中 0
            if (vieModel.PortTabItems.Count > 0)
                SetPortTabSelected(vieModel.PortTabItems[0].Name);
        }

        private async void RemovePortTabItem(string portName, Button button = null)
        {
            if (vieModel.PortTabItems == null || string.IsNullOrEmpty(portName)) return;
            SaveComSettings();
            int idx = -1;
            try
            {
                for (int i = 0; idx < vieModel.PortTabItems.Count; i++)
                {
                    if (portName.Equals(vieModel.PortTabItems[i].Name))
                    {
                        idx = i;
                        break;
                    }
                }
                if (idx >= 0 && idx < vieModel.PortTabItems.Count)
                {
                    if (button != null) button.IsEnabled = false;
                    bool success = await ClosePort(portName);
                    if (success)
                        vieModel.PortTabItems.RemoveAt(idx);
                    if (button != null) button.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageNotify.Error(ex.Message);
            }

        }

        private void RefreshPortsStatus(object sender, MouseButtonEventArgs e)
        {
            List<SideComPort> sideComPorts = vieModel.SideComPorts.ToList();
            vieModel.InitPortData();
            RetainSidePortValue(sideComPorts);
        }

        /// <summary>
        /// 恢复侧边栏串口的配置信息
        /// </summary>
        /// <param name="sideComPorts"></param>
        private void RetainSidePortValue(List<SideComPort> sideComPorts)
        {
            if (sideComPorts == null || vieModel.SideComPorts == null) return;
            for (int i = 0; i < vieModel.SideComPorts.Count; i++)
            {
                string portName = vieModel.SideComPorts[i].Name;
                if (string.IsNullOrEmpty(portName)) continue;
                SideComPort sideComPort = sideComPorts.FirstOrDefault(arg => portName.Equals(arg.Name));
                if (sideComPort == null) continue;
                vieModel.SideComPorts[i] = sideComPort;
                ComSettings comSettings = vieModel.ComSettingList.FirstOrDefault(arg => portName.Equals(arg.PortName));
                if (comSettings != null && !string.IsNullOrEmpty(comSettings.PortSetting))
                    vieModel.SideComPorts[i].Remark = CustomSerialPort.GetRemark(comSettings.PortSetting);
            }
        }

        private async void ConnectPort(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button) || button.Tag == null || button.Content == null) return;
            button.IsEnabled = false;
            string content = button.Content.ToString();
            string portName = button.Tag.ToString();
            SideComPort sideComPort = vieModel.SideComPorts.FirstOrDefault(arg => arg.Name.Equals(portName));
            if (sideComPort == null)
            {
                MessageNotify.Error($"打开 {portName} 失败！");
                return;
            }

            if ("连接".Equals(content))
                await OpenPort(sideComPort);
            else
                await ClosePort(portName);
            button.IsEnabled = true;
        }

        private async Task<bool> OpenPort(SideComPort sideComPort)
        {
            if (sideComPort == null || string.IsNullOrEmpty(sideComPort.Name))
                return false;
            string portName = sideComPort.Name;
            await OpenPortTabItem(portName, true);
            if (vieModel.PortTabItems == null)
                return false;
            PortTabItem portTabItem = vieModel.PortTabItems.FirstOrDefault(arg => arg.Name.Equals(portName));
            if (portTabItem == null)
            {
                MessageCard.Error($"打开 {portName} 失败！");
                return false;
            }

            // 加载监视器
            portTabItem.VarMonitors = new System.Collections.ObjectModel.ObservableCollection<VarMonitor>();
            foreach (var item in vieModel.GetVarMonitorByPortName(portName))
            {
                portTabItem.VarMonitors.Add(item);
            }


            await Task.Delay(DEFAULT_PORT_OPEN_INTERVAL);
            TextEditor textEditor = FindTextBoxByPortName(portName);
            // 编辑器设置
            TextEditorOptions textEditorOptions = new TextEditorOptions();
            textEditorOptions.HighlightCurrentLine = ConfigManager.Main.HighlightCurrentLine;
            textEditorOptions.ShowEndOfLine = ConfigManager.Main.ShowEndOfLine;
            textEditorOptions.ShowSpaces = ConfigManager.Main.ShowSpaces;
            textEditorOptions.ShowTabs = ConfigManager.Main.ShowTabs;
            textEditor.Options = textEditorOptions;
            textEditor.ShowLineNumbers = ConfigManager.Main.ShowLineNumbers;
            textEditor.Language = XmlLanguage.GetLanguage(VisualHelper.ZH_CN);
            if (!string.IsNullOrEmpty(ConfigManager.Main.TextFontName))
                textEditor.FontFamily = new FontFamily(ConfigManager.Main.TextFontName);
            if (!string.IsNullOrEmpty(ConfigManager.Main.TextForeground))
            {
                Brush brush = SuperUtils.WPF.VisualTools.VisualHelper.HexStringToBrush(ConfigManager.Main.TextForeground);
                textEditor.Foreground = brush;
            }



            portTabItem.TextEditor = textEditor;
            // 搜索框
            SearchPanel searchPanel = SearchPanel.Install(portTabItem.TextEditor);

            Grid rootGrid = (portTabItem.TextEditor.Parent as Border).Parent as Grid;
            ToggleButton toggleButton = rootGrid.FindName("pinToggleButton") as ToggleButton;


            searchPanel.OnSearching += (e) =>
            {
                if (ConfigManager.CommonSettings.FixedOnSearch)
                {
                    // 将文本固定
                    portTabItem.TextEditor.TextChanged -= TextBox_TextChanged;
                    toggleButton.IsEnabled = false;
                    toggleButton.IsChecked = true;
                }
            };
            searchPanel.OnClosed += () =>
            {
                if (ConfigManager.CommonSettings.ScrollOnSearchClosed)
                {
                    toggleButton.IsChecked = false;
                    portTabItem.TextEditor.TextChanged -= TextBox_TextChanged;
                    portTabItem.TextEditor.TextChanged += TextBox_TextChanged;
                }
                toggleButton.IsEnabled = true;
            };

            sideComPort.PortTabItem = portTabItem;
            sideComPort.PortTabItem.RX = 0;
            sideComPort.PortTabItem.TX = 0;
            sideComPort.PortTabItem.CurrentCharSize = 0;
            sideComPort.PortTabItem.FragCount = 0;
            await Task.Run(() =>
            {
                try
                {
                    CustomSerialPort serialPort = portTabItem.SerialPort;
                    if (!serialPort.IsOpen)
                    {
                        serialPort.WriteTimeout = CustomSerialPort.WRITE_TIME_OUT;
                        serialPort.ReadTimeout = CustomSerialPort.READ_TIME_OUT;
                        serialPort.Open();
                        portTabItem.FirstSaveData = true;
                        // 打开后启动对应的过滤器线程
                        portTabItem.StartFilterTask();
                        portTabItem.StartMonitorTask();
                        portTabItem.ConnectTime = DateTime.Now;
                        SetPortConnectStatus(portName, true);
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        string msg = $"打开串口 {portName} 失败：{ex.Message}";
                        MessageCard.Error(msg);
                        vieModel.StatusText = msg;
                        RemovePortTabItem(portName);
                    });
                    SetPortConnectStatus(portName, false);
                }
            });

            return true;
        }

        private async Task<bool> ClosePort(string portName)
        {
            if (vieModel.PortTabItems == null || string.IsNullOrEmpty(portName)) return false;
            PortTabItem portTabItem = vieModel.PortTabItems.FirstOrDefault(arg => arg.Name.Equals(portName));
            if (portTabItem == null) return false;
            CustomSerialPort serialPort = portTabItem.SerialPort;
            if (serialPort != null && serialPort.IsOpen)
            {
                bool success = await AsynClosePort(serialPort);
                if (success)
                {
                    portTabItem.StopFilterTask();
                    portTabItem.StopMonitorTask();
                    return SetPortConnectStatus(portName, false);
                }
                else
                {
                    MessageNotify.Error($"{serialPort.PortName} 关闭串口超时");
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        public async Task<bool> AsynClosePort(CustomSerialPort serialPort)
        {
            try
            {
                return await Task.Run(() =>
              {
                  serialPort.Close();
                  serialPort.Dispose();
                  return true;
              }).TimeoutAfter(TimeSpan.FromSeconds(CustomSerialPort.CLOSE_TIME_OUT));
            }
            catch (TimeoutException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                MessageNotify.Error(ex.Message);
            }
            return false;
        }


        // todo 检视重构
        private void HandleDataReceived(CustomSerialPort serialPort)
        {
            string line = "";
            try
            {
                line = serialPort.ReadExisting();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            string portName = serialPort.PortName;
            PortTabItem portTabItem = vieModel.PortTabItems.Where(arg => arg.Name.Equals(portName)).FirstOrDefault();
            if (portTabItem != null)
            {
                try
                {
                    // 异步存储
                    Dispatcher.Invoke(() =>
                    {
                        portTabItem.SaveData(line);
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

        }

        private bool SetPortConnectStatus(string portName, bool status)
        {
            try
            {
                if (vieModel.PortTabItems != null && vieModel.PortTabItems.Count > 0)
                {
                    foreach (PortTabItem item in vieModel.PortTabItems)
                    {
                        if (item != null && item.Name.Equals(portName))
                        {
                            item.Connected = status;
                            break;
                        }
                    }
                }

                if (vieModel.SideComPorts != null && vieModel.SideComPorts.Count > 0)
                {
                    foreach (SideComPort item in vieModel.SideComPorts)
                    {
                        if (item != null && item.Name.Equals(portName))
                        {
                            item.Connected = status;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
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
            for (int i = 0; i < vieModel.PortTabItems.Count; i++)
            {
                if (vieModel.PortTabItems[i].Name.Equals(portName))
                {
                    vieModel.PortTabItems[i].Selected = true;
                    SetGridVisible(portName);
                    existed = true;
                }
                else
                {
                    vieModel.PortTabItems[i].Selected = false;
                }
            }
            if (!existed)
            {
                PortTabItem portTabItem = new PortTabItem(portName, connect);
                portTabItem.Setting = PortSetting.GetDefaultSetting();

                CustomSerialPort serialPort;
                if (portTabItem.SerialPort == null)
                {
                    serialPort = new CustomSerialPort(portName);
                    serialPort.DataReceived += new SerialDataReceivedEventHandler((a, b) =>
                    {
                        HandleDataReceived(serialPort);
                    });
                    portTabItem.SerialPort = serialPort;
                }
                else
                {
                    serialPort = portTabItem.SerialPort;
                }

                // 从配置里读取
                ComSettings comSettings = vieModel.ComSettingList.Where(arg => arg.PortName.Equals(portName)).FirstOrDefault();
                if (comSettings != null)
                {
                    portTabItem.WriteData = comSettings.WriteData;
                    portTabItem.AddTimeStamp = comSettings.AddTimeStamp;
                    portTabItem.AddNewLineWhenWrite = comSettings.AddNewLineWhenWrite;
                    portTabItem.EnabledFilter = comSettings.EnabledFilter;
                    portTabItem.EnabledMonitor = comSettings.EnabledMonitor;
                    portTabItem.SerialPort.SetPortSettingByJson(comSettings.PortSetting);
                    portTabItem.Remark = portTabItem.SerialPort.Remark;
                }
                portTabItem.Selected = true;
                vieModel.PortTabItems.Add(portTabItem);
                await Task.Run(async () =>
                {
                    await Task.Delay(500);
                    Dispatcher.Invoke(() =>
                    {
                        SetComboboxStatus();
                    });
                });
                SetPortTabSelected(portName);
            }
            return true;
        }

        private async void Grid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                Grid grid = sender as Grid;
                if (grid == null || grid.Tag == null) return;
                string portName = grid.Tag.ToString();
                await OpenPortTabItem(portName, false);
            }
        }

        private void TextBox_TextChanged(object sender, EventArgs e)
        {

            (sender as TextEditor).ScrollToEnd();
        }

        private void ShowAbout(object sender, RoutedEventArgs e)
        {
            Dialog_About about = new Dialog_About();
            string local = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            local = local.Substring(0, local.Length - ".0.0".Length);
            about.AppName = "SuperCom";
            about.AppSubName = "超级串口工具";
            about.Version = local;
            about.ReleaseDate = ConfigManager.RELEASE_DATE;
            about.Author = "Chao";
            about.License = "GPL-3.0";
            about.GithubUrl = "https://github.com/SuperStudio/SuperCom";
            about.WebUrl = "https://github.com/SuperStudio/SuperCom";
            about.JoinGroupUrl = "https://github.com/SuperStudio/SuperCom";
            about.Image = SuperUtils.Media.ImageHelper.ImageFromUri("pack://application:,,,/SuperCom;Component/Resources/Ico/ICON_256.ico");
            about.ShowDialog();
        }

        private void OpenContextMenu(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == null || button.ContextMenu == null)
                return;
            button.ContextMenu.IsOpen = true;
        }
        private static double MAX_FONTSIZE = 25;
        private static double MIN_FONTSIZE = 5;

        private void Border_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                Border border = sender as Border;
                TextEditor textEditor = border.Child as TextEditor;
                double fontSize = textEditor.FontSize;
                if (e.Delta > 0)
                {
                    fontSize++;
                }
                else
                {
                    fontSize--;
                }
                if (fontSize > MAX_FONTSIZE) fontSize = MAX_FONTSIZE;
                if (fontSize < MIN_FONTSIZE) fontSize = MIN_FONTSIZE;

                textEditor.FontSize = fontSize;
                e.Handled = true;
            }

        }

        private void SetTextBoxScroll(object sender, RoutedEventArgs e)
        {
            ToggleButton toggleButton = sender as ToggleButton;
            bool fix = (bool)toggleButton.IsChecked;
            PortTabItem portTabItem = GetPortItem(sender as FrameworkElement);
            if (portTabItem != null && portTabItem.TextEditor != null)
            {
                if (fix)
                    portTabItem.TextEditor.TextChanged -= TextBox_TextChanged;
                else
                {
                    portTabItem.TextEditor.TextChanged -= TextBox_TextChanged;
                    portTabItem.TextEditor.TextChanged += TextBox_TextChanged;
                }
            }
        }

        private TextEditor FindTextBox(Grid rootGrid)
        {
            if (rootGrid == null) return null;
            Border border = rootGrid.Children.OfType<Border>().FirstOrDefault();
            if (border != null && border.Child is TextEditor textEditor)
            {
                return textEditor;
            }
            return null;
        }

        private TextEditor FindTextBoxByPortName(string portName)
        {
            if (string.IsNullOrEmpty(portName))
                return null;
            for (int i = 0; i < itemsControl.Items.Count; i++)
            {
                ContentPresenter presenter = (ContentPresenter)itemsControl.ItemContainerGenerator.ContainerFromItem(itemsControl.Items[i]);
                if (presenter == null) continue;
                Grid grid = VisualHelper.FindElementByName<Grid>(presenter, "rootGrid");
                if (grid != null && grid.Tag != null && portName.Equals(grid.Tag.ToString())
                    && grid.FindName("textBox") is TextEditor textEditor) return textEditor;
            }
            return null;
        }

        private void GotoTop(object sender, MouseButtonEventArgs e)
        {
            StackPanel stackPanel = (sender as Border).Parent as StackPanel;
            if (stackPanel != null && stackPanel.Parent is Grid grid)
            {
                TextEditor textEditor = FindTextBox(grid);
                if (textEditor != null)
                    textEditor.ScrollToHome();
            }
        }

        private void GotoBottom(object sender, MouseButtonEventArgs e)
        {
            StackPanel stackPanel = (sender as Border).Parent as StackPanel;
            if (stackPanel != null && stackPanel.Parent is Grid grid)
            {
                TextEditor textEditor = FindTextBox(grid);
                if (textEditor != null)
                    textEditor.ScrollToEnd();
            }
        }

        private void ClearData(object sender, RoutedEventArgs e)
        {
            StackPanel stackPanel = (sender as Button).Parent as StackPanel;
            if (stackPanel != null && stackPanel.Parent is Border border && border.Parent is Grid rootGrid)
            {
                if (rootGrid.Tag == null) return;
                string portName = rootGrid.Tag.ToString();
                FindTextBox(rootGrid)?.Clear();
                PortTabItem portTabItem = vieModel.PortTabItems.Where(arg => arg.Name.Equals(portName)).FirstOrDefault();
                if (portTabItem != null)
                {
                    portTabItem.ClearData();

                }
            }
        }






        private void OpenPath(object sender, RoutedEventArgs e)
        {
            PortTabItem portTabItem = GetPortItem(sender as FrameworkElement);
            if (portTabItem != null)
            {
                string fileName = portTabItem.SaveFileName;
                if (File.Exists(fileName))
                {
                    FileHelper.TryOpenSelectPathEx(fileName);
                    if (portTabItem.FragCount > 0)
                        MessageNotify.Info($"当前日志已分 {portTabItem.FragCount} 片");
                }
                else
                {
                    MessageCard.Warning($"当前无日志");
                }

            }

        }

        private PortTabItem GetPortItem(FrameworkElement element)
        {
            StackPanel stackPanel = element.Parent as StackPanel;
            if (stackPanel != null && stackPanel.Parent is Border border && border.Parent is Grid rootGrid)
            {
                if (rootGrid.Tag == null) return null;
                string portName = rootGrid.Tag.ToString();
                return vieModel.PortTabItems.Where(arg => arg.Name.Equals(portName)).FirstOrDefault();
            }
            return null;
        }
        private Grid GetRootGrid(FrameworkElement element)
        {
            StackPanel stackPanel = element.Parent as StackPanel;
            if (stackPanel != null && stackPanel.Parent is Grid grid && grid.Parent is Grid rootGrid)
            {
                return rootGrid;
            }
            return null;
        }

        private void SendCommand(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null && button.Tag != null)
            {
                string portName = button.Tag.ToString();
                if (string.IsNullOrEmpty(portName)) return;
                SendCommand(portName);
                if (ConfigManager.CommonSettings.FixedOnSendCommand)
                {
                    Grid grid = ((button.Parent as Grid).Parent as Grid).Parent as Grid;
                    Grid baseGrid = grid.Parent as Grid;
                    (ToggleButton toggleButton, TextEditor textEditor) = FindToggleButtonByBaseGrid(baseGrid);
                    toggleButton.IsChecked = true;
                    textEditor.TextChanged -= TextBox_TextChanged;
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
            SideComPort serialComPort = vieModel.SideComPorts.Where(arg => arg.Name.Equals(portName)).FirstOrDefault();
            if (serialComPort == null || serialComPort.PortTabItem == null || serialComPort.PortTabItem.SerialPort == null)
            {
                MessageCard.Error($"连接串口 {portName} 失败！");
                return;
            }
            SerialPort port = serialComPort.PortTabItem.SerialPort;
            PortTabItem portTabItem = vieModel.PortTabItems.Where(arg => arg.Name.Equals(portName)).FirstOrDefault();
            string value = portTabItem.WriteData;
            if (port != null)
            {
                SendCommand(port, portTabItem, value);
            }
        }

        private int CurrentErrorCount = 0;
        private const int MAX_ERROR_COUNT = 2;


        /// <summary>
        /// 异步超时发送
        /// </summary>
        /// <param name="port"></param>
        /// <param name="portTabItem"></param>
        /// <param name="value"></param>
        /// <param name="saveToHistory"></param>
        /// <returns></returns>
        public bool SendCommand(SerialPort port, PortTabItem portTabItem, string value, bool saveToHistory = true)
        {
            if (portTabItem == null) return false;
            if (portTabItem.AddNewLineWhenWrite)
            {
                value += "\r\n";
            }
            portTabItem.SaveData($"SEND >>>>>>>>>> {value}");
            try
            {
                //Console.WriteLine($"before port write, value = {value}");
                port.Write(value);
                //Console.WriteLine($"after port write");
                portTabItem.TX += value.Length;
                // todo 保存到发送历史
                //if (saveToHistory)
                //{
                //    vieModel.SendHistory.Add(value.Trim());
                //    vieModel.SaveSendHistory();
                //}
                vieModel.StatusText = $"【发送命令】=>{portTabItem.WriteData}";
                CurrentErrorCount = 0;
                return true;
            }
            catch (Exception ex)
            {
                CurrentErrorCount++;
                if (CurrentErrorCount <= MAX_ERROR_COUNT)
                    MessageCard.Error(ex.Message);
                return false;
            }
        }

        private void OnMaxCurrentWindow(object sender, MouseButtonEventArgs e)
        {
            //if (e.ClickCount > 1)
            //{
            //    MaxWindow(sender, new RoutedEventArgs());
            //}
            base.DragMoveWindow(sender, e);
        }


        private string GetPortName(FrameworkElement element)
        {
            if (element == null) return null;
            StackPanel stackPanel = element.Parent as StackPanel;
            if (stackPanel != null && stackPanel.Parent is Border border)
            {
                if (border.Tag != null)
                {
                    return border.Tag.ToString();
                }
            }
            return null;
        }

        private async void SaveToNewFile(object sender, RoutedEventArgs e)
        {
            (sender as FrameworkElement).IsEnabled = false;
            string portName = GetPortName(sender as FrameworkElement);
            if (!string.IsNullOrEmpty(portName))
            {
                PortTabItem portTabItem = vieModel.PortTabItems.Where(arg => arg.Name.Equals(portName)).FirstOrDefault();
                if (portTabItem != null)
                {
                    portTabItem.ConnectTime = DateTime.Now;
                    await Task.Delay(500);
                    MessageNotify.Success("成功存到新文件！");
                }
            }
            (sender as FrameworkElement).IsEnabled = true;
        }



        private string GetPortName(ComboBox comboBox)
        {
            if (comboBox != null && comboBox.Tag != null)
            {
                return comboBox.Tag.ToString();
            }
            return null;
        }




        private void ShowSettingsPopup(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                Border border = sender as Border;
                ContextMenu contextMenu = border.ContextMenu;
                contextMenu.PlacementTarget = border;
                contextMenu.Placement = PlacementMode.Top;
                contextMenu.IsOpen = true;
            }
            e.Handled = true;
        }

        private void ShowSplitPopup(object sender, RoutedEventArgs e)
        {
            panelSplitPopup.IsOpen = true;
        }

        private void ShowContextMenu(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
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
            foreach (var item in vieModel.SideComPorts)
            {
                ClosePort(item.Name);
            }
        }

        private void OpenAllPort(object sender, RoutedEventArgs e)
        {
            foreach (SideComPort item in vieModel.SideComPorts)
            {
                OpenPort(item);
            }
        }

        private void SplitPanel(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == null) return;
            if (button.Parent is Grid grid)
            {
                SplitPanel(SplitPanelType.Left | SplitPanelType.Right);
            }
            else if (button.Parent is StackPanel panel)
            {
                int idx = panel.Children.IndexOf(button);
                if (idx == 0)
                {
                    SplitPanel(SplitPanelType.Top | SplitPanelType.Bottom);
                }
                else if (idx == 1)
                {
                    SplitPanel(SplitPanelType.Top | SplitPanelType.Bottom | SplitPanelType.Left | SplitPanelType.Right);
                }
                else if (idx == 2)
                {
                    SplitPanel(SplitPanelType.Bottom | SplitPanelType.Left | SplitPanelType.Right);
                }
                else if (idx == 3)
                {
                    SplitPanel(SplitPanelType.Top | SplitPanelType.Left | SplitPanelType.Right);
                }
                else if (idx == 4)
                {
                    SplitPanel(SplitPanelType.None);
                }
            }
            panelSplitPopup.IsOpen = false;
            MessageNotify.Info("开发中");
        }

        private void SplitPanel(SplitPanelType type)
        {
            if (type == SplitPanelType.None)
            {
                Console.WriteLine(SplitPanelType.None);
            }
            if ((type & SplitPanelType.Left) != 0)
            {
                Console.WriteLine(SplitPanelType.Left);
            }
            if ((type & SplitPanelType.Right) != 0)
            {
                Console.WriteLine(SplitPanelType.Right);
            }
            if ((type & SplitPanelType.Top) != 0)
            {
                Console.WriteLine(SplitPanelType.Top);
            }
            if ((type & SplitPanelType.Bottom) != 0)
            {
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
            try
            {
                CloseAllPort(null, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }



        /// <summary>
        /// 保存串口的配置文件
        /// </summary>
        private void SaveComSettings()
        {
            foreach (var portTabItem in vieModel.PortTabItems)
            {

                ComSettings comSettings = vieModel.ComSettingList.Where(arg => arg.PortName.Equals(portTabItem.Name)).FirstOrDefault();
                if (comSettings == null) comSettings = new ComSettings();
                comSettings.PortName = portTabItem.Name;
                comSettings.Connected = portTabItem.Connected;
                // PortTabItem portTabItem = item.PortTabItem;

                comSettings.WriteData = portTabItem.WriteData;
                comSettings.AddNewLineWhenWrite = portTabItem.AddNewLineWhenWrite;
                comSettings.EnabledFilter = portTabItem.EnabledFilter;
                comSettings.EnabledMonitor = portTabItem.EnabledMonitor;
                comSettings.AddTimeStamp = portTabItem.AddTimeStamp;
                portTabItem.SerialPort.RefreshSetting();
                comSettings.PortSetting = portTabItem.SerialPort?.SettingJson;

                SqliteMapper<ComSettings> mapper = new SqliteMapper<ComSettings>(ConfigManager.SQLITE_DATA_PATH);
                mapper.Insert(comSettings, SuperUtils.Framework.ORM.Attributes.InsertMode.Replace);
            }
        }

        private void SaveCustomSetting()
        {

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
            ConfigManager.Main.WindowState = (long)baseWindowState;
            ConfigManager.Main.SideGridWidth = SideGridColumn.ActualWidth;
            ConfigManager.Main.Save();
        }


        private const int MAX_TRANSFORM_SIZE = 100000;

        private void OpenHexTransform(object sender, RoutedEventArgs e)
        {
            string text = GetCurrentText(sender as FrameworkElement);
            OpenHex(text);
        }

        private void OpenHex(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (text.Length > MAX_TRANSFORM_SIZE)
            {
                MessageNotify.Warning($"超过了 {MAX_TRANSFORM_SIZE}");
                return;
            }
            hexTransPopup.IsOpen = true;
            HexTextBox.Text = text;
            HexToStr(null, null);
        }



        private void CopyText(object sender, RoutedEventArgs e)
        {
            string text = GetCurrentText(sender as FrameworkElement);
            ClipBoard.TrySetDataObject(text);
        }


        private string GetCurrentText(FrameworkElement element)
        {
            MenuItem menuItem = element as MenuItem;
            if (menuItem != null && menuItem.Parent is ContextMenu contextMenu)
            {
                if (contextMenu.PlacementTarget is TextEditor textEditor)
                {
                    return textEditor.SelectedText;
                }
            }
            return null;
        }


        private const int MAX_TIMESTAMP_LENGTH = 100;
        private void OpenTimeTransform(object sender, RoutedEventArgs e)
        {
            string text = GetCurrentText(sender as FrameworkElement);
            OpenTime(text);
        }

        private void OpenTime(string text)
        {
            if (text.Length > MAX_TIMESTAMP_LENGTH)
            {
                MessageNotify.Warning($"超过了 {MAX_TIMESTAMP_LENGTH}");
                return;
            }
            if (string.IsNullOrEmpty(text)) return;
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
            if ((bool)HexToStrSwitch.IsChecked)
            {
                HexTextBox.Text = text;
            }
            else
            {
                HexTextBox.Text = text.ToLower();
            }

        }

        private void Switch_Click(object sender, RoutedEventArgs e)
        {
            Switch obj = sender as Switch;
            if ((bool)obj.IsChecked)
            {
                HexTextBox.Text = HexTextBox.Text.ToUpper();
            }
            else
            {
                HexTextBox.Text = HexTextBox.Text.ToLower();
            }
        }


        private void TimeStampToLocalTime(object sender, RoutedEventArgs e)
        {
            bool success = long.TryParse(TimeStampTextBox.Text, out long timeStamp);
            if (!success)
            {
                LocalTimeTextBox.Text = "解析失败";
                return;
            }
            try
            {
                DateTime dateTime = DateHelper.UnixTimeStampToDateTime(timeStamp, TimeComboBox.SelectedIndex == 0);
                LocalTimeTextBox.Text = dateTime.ToLocalDate();
            }
            catch (Exception ex)
            {
                LocalTimeTextBox.Text = ex.Message;
            }
        }

        private void LocalTimeToTimeStamp(object sender, RoutedEventArgs e)
        {
            bool success = DateTime.TryParse(LocalTimeTextBox.Text, out DateTime dt);
            if (!success)
            {
                TimeStampTextBox.Text = "解析失败";
            }
            else
            {
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
            StackPanel panel = (sender as Button).Parent as StackPanel;
            (panel.Parent as Grid).Visibility = Visibility.Hidden;
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
            if (portTabItem != null)
            {
                string fileName = portTabItem.SaveFileName;
                if (File.Exists(fileName))
                {
                    FileHelper.TryOpenByDefaultApp(fileName);
                }
                else
                {
                    MessageCard.Warning($"当前无日志");
                }

            }
        }



        private async void mainWindow_ContentRendered(object sender, EventArgs e)
        {
            AdjustWindow();
            if (ConfigManager.Main.FirstRun) ConfigManager.Main.FirstRun = false;
            InitThemeSelector();
            RefreshSetting();
            ReadXshdList();// 自定义语法高亮
            LoadDonateConfig();
            await BackupData(); // 备份文件
            //new Window_AdvancedSend().Show();
            //Window_Setting setting = new Window_Setting();
            //setting.Owner = this;
            //setting.ShowDialog();
            //OpenShortCut(null, null);
            LoadFontFamily();
            InitUpgrade();
            CommonSettings.InitLogDir();
            OpenBeforePorts();

        }



        private void LoadFontFamily()
        {
            foreach (string fontName in VisualHelper.SYSTEM_FONT_FAMILIES.Keys)
            {
                MenuItem menuItem = new MenuItem();
                menuItem.Header = fontName;
                menuItem.FontFamily = VisualHelper.SYSTEM_FONT_FAMILIES[fontName];
                menuItem.IsCheckable = true;
                menuItem.IsChecked = fontName.Equals(ConfigManager.Main.TextFontName);
                menuItem.Checked += (s, e) =>
                {
                    string name = (s as MenuItem).Header.ToString();
                    SetFontFamily(name);


                };
                FontMenuItem.Items.Add(menuItem);
            }
        }

        public void SetFontFamily(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            foreach (MenuItem item in FontMenuItem.Items)
            {
                if (name.Equals(item.Header.ToString()))
                    continue;
                item.IsChecked = false;
            }

            foreach (PortTabItem item in vieModel.PortTabItems)
            {
                TextEditor textEditor = FindTextBoxByPortName(item.Name);
                if (textEditor != null)
                    textEditor.FontFamily = VisualHelper.SYSTEM_FONT_FAMILIES[name];
            }
            ConfigManager.Main.TextFontName = name;
            ConfigManager.Main.Save();
        }

        public void InitUpgrade()
        {
            UpgradeHelper.Init(this);
            CheckUpgrade();
        }

        private void LoadDonateConfig()
        {
            vieModel.ShowDonate = true;
            string json_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_config.json");
            if (File.Exists(json_path))
            {
                string v = FileHelper.TryReadFile(json_path);
                if (!string.IsNullOrEmpty(v))
                {
                    Dictionary<string, object> dict = JsonUtils.TryDeserializeObject<Dictionary<string, object>>(v);
                    if (dict != null && dict.ContainsKey("ShowDonate"))
                    {
                        string showDonate = dict["ShowDonate"].ToString();
                        if (!string.IsNullOrEmpty(showDonate))
                        {
                            vieModel.ShowDonate = showDonate.ToLower().Equals("false") ? false : true;
                        }
                    }
                }
            }
        }

        public void InitThemeSelector()
        {
            themeSelector.AddTransParentColor("TabItem.Background");
            themeSelector.AddTransParentColor("Window.Title.Background");
            themeSelector.AddTransParentColor("ListBoxItem.Background");
            themeSelector.SetThemeConfig(ConfigManager.Settings.ThemeIdx, ConfigManager.Settings.ThemeID);
            themeSelector.onThemeChanged += (ThemeIdx, ThemeID) =>
            {
                ConfigManager.Settings.ThemeIdx = ThemeIdx;
                ConfigManager.Settings.ThemeID = ThemeID;
                ConfigManager.Settings.Save();
            };
            themeSelector.onBackGroundImageChanged += (image) =>
            {
                BgImage.Source = image;
            };
            themeSelector.onSetBgColorTransparent += () =>
           {
               if (itemsControl == null || itemsControl.ItemsSource == null) return;
               TitleBorder.Background = Brushes.Transparent;
           };

            themeSelector.onReSetBgColorBinding += () =>
            {
                if (itemsControl == null || itemsControl.ItemsSource == null) return;
                TitleBorder.SetResourceReference(Control.BackgroundProperty, "Window.Title.Background");
            };

            themeSelector.InitThemes();
        }




        public string longText = "";


        private async void OpenBeforePorts()
        {
            if (string.IsNullOrEmpty(ConfigManager.Main.OpeningPorts)) return;
            List<string> list = JsonUtils.TryDeserializeObject<List<string>>(ConfigManager.Main.OpeningPorts);
            foreach (string portName in list)
            {
                ComSettings comSettings = vieModel.ComSettingList.Where(arg => arg.PortName.Equals(portName)).FirstOrDefault();
                SideComPort sideComPort = vieModel.SideComPorts.Where(arg => arg.Name.Equals(portName)).FirstOrDefault();
                if (comSettings != null && sideComPort != null && comSettings.Connected)
                {
                    await OpenPort(sideComPort);
                }
                else
                {
                    await OpenPortTabItem(portName, false);
                }
            }
            SetFontFamily(ConfigManager.Main.TextFontName);
        }

        public void AdjustWindow()
        {

            if (ConfigManager.Main.FirstRun)
            {
                this.Width = SystemParameters.WorkArea.Width * 0.8;
                this.Height = SystemParameters.WorkArea.Height * 0.8;
                this.Left = SystemParameters.WorkArea.Width * 0.1;
                this.Top = SystemParameters.WorkArea.Height * 0.1;
            }
            else
            {
                if (ConfigManager.Main.Height == SystemParameters.WorkArea.Height && ConfigManager.Main.Width < SystemParameters.WorkArea.Width)
                {
                    baseWindowState = 0;
                    this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    this.CanResize = true;
                }
                else
                {
                    this.Left = ConfigManager.Main.X;
                    this.Top = ConfigManager.Main.Y;
                    this.Width = ConfigManager.Main.Width;
                    this.Height = ConfigManager.Main.Height;
                }


                baseWindowState = (BaseWindowState)ConfigManager.Main.WindowState;
                if (baseWindowState == BaseWindowState.FullScreen)
                {
                    this.WindowState = System.Windows.WindowState.Maximized;
                }
                else if (baseWindowState == BaseWindowState.None)
                {
                    baseWindowState = 0;
                    this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }
                if (this.Width == SystemParameters.WorkArea.Width
                    && this.Height == SystemParameters.WorkArea.Height) baseWindowState = BaseWindowState.Maximized;

                if (baseWindowState == BaseWindowState.Maximized || baseWindowState == BaseWindowState.FullScreen)
                {
                    MaxPath.Data = Geometry.Parse(PathData.MaxToNormalPath);
                    MaxMenuItem.Header = "窗口化";
                }


            }
        }



        private void Border_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (SideGridColumn.ActualWidth <= 100)
            {
                SideGridColumn.Width = new GridLength(0);
                sideGridMenuItem.IsChecked = false;
            }
        }

        private void OpenSetting(object sender, RoutedEventArgs e)
        {
            window_Setting?.Close();
            window_Setting = new Window_Setting();
            window_Setting.Show();
            window_Setting.Focus();
            window_Setting.BringIntoView();
        }

        #region "历史记录弹窗"
        private void SendTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            string text = textBox.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(text)) return;
            List<string> list = vieModel.SendHistory.Where(arg => arg.ToLower().IndexOf(text) >= 0).ToList();
            if (list.Count > 0)
            {
                Grid grid = (textBox.Parent as Border).Parent as Grid;
                Popup popup = grid.Children.OfType<Popup>().FirstOrDefault();
                if (popup != null)
                {
                    popup.IsOpen = true;
                    Grid g = popup.Child as Grid;
                    ItemsControl itemsControl = g.FindName("itemsControl") as ItemsControl;
                    if (itemsControl != null)
                    {
                        itemsControl.ItemsSource = list;
                        vieModel.SendHistorySelectedIndex = 0;
                    }
                }
            }
        }

        private void SetSendHistory(object sender, MouseButtonEventArgs e)
        {
            Border border = sender as Border;
            Grid grid = border.Child as Grid;
            TextBlock textBlock = grid.Children.OfType<TextBlock>().FirstOrDefault();
            if (textBlock != null)
            {
                string value = textBlock.Text;
                vieModel.SendHistorySelectedValue = value;
                Popup popup = textBlock.Tag as Popup;
                if (popup != null && popup.Tag != null)
                {
                    string portName = popup.Tag.ToString();
                    PortTabItem portTabItem = vieModel.PortTabItems.Where(arg => arg.Name.Equals(portName)).FirstOrDefault();
                    if (portTabItem != null)
                    {
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


            TextBox textBox = sender as TextBox;
            Grid grid = ((textBox.Parent as Grid).Parent as Border).Parent as Grid;
            string text = textBox.Text.Trim();
            List<string> list = vieModel.SendHistory.Where(arg => arg.ToLower().IndexOf(text.ToLower()) >= 0).ToList();
            Popup popup = grid.Children.OfType<Popup>().FirstOrDefault();
            if (string.IsNullOrEmpty(text) || list.Count <= 0)
            {
                if (popup != null)
                    popup.IsOpen = false;
                if (e.Key == Key.Enter && grid.Tag != null)
                {
                    string portName = grid.Tag.ToString();
                    SendCommand(portName);
                }
                return;
            }
            if (e.Key == Key.Up || e.Key == Key.Down)
            {
                if (list.Count > 0 && popup != null && popup.IsOpen)
                {
                    popup.Focus();
                    int idx = vieModel.SendHistorySelectedIndex;
                    if (e.Key == Key.Up) idx--;
                    else idx++;
                    if (idx >= list.Count) idx = 0;
                    else if (idx < 0) idx = list.Count - 1;
                    Console.WriteLine("text=" + text);
                    Console.WriteLine(idx);
                    vieModel.SendHistorySelectedIndex = idx;
                    vieModel.SendHistorySelectedValue = list[idx];
                    // 设置当前选中状态
                    Grid grid1 = popup.Child as Grid;
                    ItemsControl itemsControl = grid1.FindName("itemsControl") as ItemsControl;
                    SetSelectedStatus(itemsControl);

                }
            }
            else if (e.Key == Key.Enter || e.Key == Key.Tab)
            {
                if (grid.Tag != null)
                {
                    string portName = grid.Tag.ToString();
                    if (popup != null && popup.IsOpen && !string.IsNullOrEmpty(vieModel.SendHistorySelectedValue))
                    {

                        PortTabItem portTabItem = vieModel.PortTabItems.Where(arg => arg.Name.Equals(portName)).FirstOrDefault();
                        if (portTabItem != null)
                        {
                            portTabItem.WriteData = vieModel.SendHistorySelectedValue;
                            textBox.CaretIndex = textBox.Text.Length;
                            popup.IsOpen = false;
                        }
                    }
                    else if (e.Key == Key.Enter)
                    {
                        SendCommand(portName);
                    }
                }
            }
            else if (e.Key == Key.Escape)
            {
                if (popup != null)
                    popup.IsOpen = false;
            }
        }

        private void SetSelectedStatus(ItemsControl itemsControl)
        {
            if (itemsControl == null || itemsControl.ItemsSource == null) return;
            for (int i = 0; i < itemsControl.Items.Count; i++)
            {
                ContentPresenter presenter = (ContentPresenter)itemsControl.ItemContainerGenerator.ContainerFromItem(itemsControl.Items[i]);
                if (presenter == null) continue;
                Border border = VisualHelper.FindElementByName<Border>(presenter, "baseBorder");
                if (border == null) continue;
                if (i == vieModel.SendHistorySelectedIndex)
                {
                    border.Background = (Brush)FindResource("ListBoxItem.Selected.Active.Background");
                    border.BorderBrush = (Brush)FindResource("ListBoxItem.Selected.Active.BorderBrush");
                }
                else
                {
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
            //if (window_AdvancedSend == null || window_AdvancedSend.IsClosed)
            //{

            Button button = sender as Button;
            int index = 0;
            if (button != null && button.Tag != null)
                int.TryParse(button.Tag.ToString(), out index);

            Window_AdvancedSend window = new Window_AdvancedSend();
            window.SideSelectedIndex = index;
            window.Show();
            window.Focus();
            window.BringIntoView();
            //}
            //else
            //{
            //    window_AdvancedSend.Focus();
            //    window_AdvancedSend.BringIntoView();
            //}
        }


        FoldingManager foldingManager;
        object foldingStrategy;


        private void HideSide(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem.IsChecked)
            {
                SideGridColumn.Width = new GridLength(200);
            }
            else
            {
                SideGridColumn.Width = new GridLength(0);
                Border_SizeChanged(null, null);
            }

        }
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            Grid grid = (comboBox.Parent as StackPanel).Parent as Grid;
            ItemsControl itemsControl = grid.Children.OfType<ItemsControl>().LastOrDefault();
            if (itemsControl == null)
            {
                return;
            }
            itemsControl.ItemsSource = null;
            if (comboBox.SelectedValue == null) return;
            string id = comboBox.SelectedValue.ToString();
            if (string.IsNullOrEmpty(id)) return;
            AdvancedSend advancedSend = vieModel.SendCommandProjects.Where(arg => arg.ProjectID.ToString().Equals(id)).FirstOrDefault();
            vieModel.CurrentAdvancedSend = advancedSend;
            if (!string.IsNullOrEmpty(advancedSend.Commands))
            {
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
            foreach (PortTabItem item in vieModel.PortTabItems)
            {
                TextEditor textEditor = FindTextBoxByPortName(item.Name);
                if (textEditor != null)
                {
                    ComboBox comboBox = FindCombobox(textEditor);
                    if (comboBox != null)
                    {
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
            PortTabItem portTabItem = vieModel.PortTabItems.Where(arg => arg.Name.Equals(item.Name)).FirstOrDefault();
            if (portTabItem.ResultChecks == null) portTabItem.ResultChecks = new Queue<ResultCheck>();
            ResultCheck resultCheck = new ResultCheck();
            resultCheck.Command = command;
            resultCheck.Buffer = new StringBuilder();
            portTabItem.ResultChecks.Enqueue(resultCheck);
            int time = 0;
            bool find = false;
            while (!find && time <= timeOut)
            {
                ResultCheck check = portTabItem.ResultChecks.Where(arg => arg.Command.Equals(command)).FirstOrDefault();
                if (check != null)
                {
                    string[] buffers = check.Buffer.ToString().Split(Environment.NewLine.ToCharArray());
                    foreach (string line in buffers)
                    {
                        if (line.IndexOf(recvResult) >= 0 && line.IndexOf($"SEND >>>>>>>>>> {command}") < 0)
                        {
                            find = true;

                            break;
                        }
                    }
                    if (find) break;


                    await Task.Delay(100);
                    time += 100;
                    Console.WriteLine("查找中...");
                }
                else
                {
                    break;
                }
            }

            if (find)
            {
                MessageCard.Info($"找到：\n{resultCheck.Buffer}");
            }
            else
            {
                MessageNotify.Info($"查找超时！");
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
            if (border != null && border.Tag != null)
            {
                string portName = border.Tag.ToString();
                SideComPort sideComPort = vieModel.SideComPorts.Where(arg => arg.Name.Equals(portName)).FirstOrDefault();
                if (sideComPort != null && sideComPort.PortTabItem != null && sideComPort.PortTabItem.SerialPort != null)
                {

                    AdvancedSend send = vieModel.CurrentAdvancedSend;
                    if (send != null && !string.IsNullOrEmpty(send.Commands))
                    {
                        send.CommandList = JsonUtils.TryDeserializeObject<List<SendCommand>>(send.Commands);
                        SendCommand sendCommand = send.CommandList.Where(arg => arg.CommandID == commandID).FirstOrDefault();
                        if (sendCommand != null && sendCommand.IsResultCheck)
                        {
                            // 过滤找到需要的字符串
                            string recvResult = sendCommand.RecvResult;
                            int timeOut = sendCommand.RecvTimeOut;
                            SendToFindResultTask(sideComPort.PortTabItem, recvResult, timeOut, command);
                        }
                    }
                    SendCommand(sideComPort.PortTabItem.SerialPort, sideComPort.PortTabItem, command, false);
                    // 设置固定滚屏
                    if (ConfigManager.CommonSettings.FixedOnSendCommand)
                    {
                        Grid baseGrid = (border.Parent as Grid).Parent as Grid;
                        (ToggleButton toggleButton, TextEditor textEditor) = FindToggleButtonByBaseGrid(baseGrid);
                        toggleButton.IsChecked = true;
                        textEditor.TextChanged -= TextBox_TextChanged;
                    }
                }
            }

        }

        private void StartSendCommands(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == null || button.Tag == null) return;
            StackPanel stackPanel = button.Parent as StackPanel;
            ComboBox comboBox = stackPanel.Children.OfType<ComboBox>().LastOrDefault();
            string portName = button.Tag.ToString();
            if (comboBox != null && comboBox.SelectedValue != null &&
                vieModel.SendCommandProjects?.Count > 0)
            {
                string projectID = comboBox.SelectedValue.ToString();
                // 开始执行队列
                AdvancedSend advancedSend = vieModel.SendCommandProjects.Where(arg => arg.ProjectID.ToString().Equals(projectID)).FirstOrDefault();
                if (advancedSend != null)
                    BeginSendCommands(advancedSend, portName, button);
            }
        }

        private void StopSendCommands(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == null || button.Tag == null) return;
            StackPanel stackPanel = button.Parent as StackPanel;
            ComboBox comboBox = stackPanel.Children.OfType<ComboBox>().LastOrDefault();
            string portName = button.Tag.ToString();
            PortTabItem portTabItem = vieModel.PortTabItems.Where(arg => arg.Name.Equals(portName)).FirstOrDefault();
            if (comboBox != null && comboBox.SelectedValue != null &&
                vieModel.SendCommandProjects?.Count > 0 && portTabItem != null)
            {
                string projectID = comboBox.SelectedValue.ToString();
                // 开始执行队列
                AdvancedSend advancedSend = vieModel.SendCommandProjects.Where(arg => arg.ProjectID.ToString().Equals(projectID)).FirstOrDefault();
                if (advancedSend == null) return;
                portTabItem.RunningCommands = false;
                if (advancedSend.CommandList?.Count > 0)
                    foreach (var item in advancedSend.CommandList)
                        item.Status = RunningStatus.WaitingToRun;

            }


        }


        public void BeginSendCommands(AdvancedSend advancedSend, string portName, Button button)
        {
            if (advancedSend == null || string.IsNullOrEmpty(advancedSend.Commands)) return;
            advancedSend.CommandList = JsonUtils.TryDeserializeObject<List<SendCommand>>(advancedSend.Commands);
            if (advancedSend.CommandList == null || advancedSend.CommandList.Count == 0) return;
            PortTabItem portTabItem = vieModel.PortTabItems.Where(arg => arg.Name.Equals(portName)).FirstOrDefault();
            if (portTabItem == null) return;
            portTabItem.RunningCommands = true;
            SetRunningStatus(button, true);
            Task.Run(async () =>
            {
                int idx = 0;
                while (portTabItem.RunningCommands)
                {

                    SendCommand command = advancedSend.CommandList[idx];
                    if (idx < advancedSend.CommandList.Count)
                        advancedSend.CommandList[idx].Status = RunningStatus.Running;

                    bool success = await AsyncSendCommand(idx, portName, command, advancedSend);
                    advancedSend.CommandList[idx].Status = RunningStatus.WaitingDelay;
                    if (command.Delay > 0)
                    {
                        int delay = 10;
                        for (int i = 1; i <= command.Delay; i += delay)
                        {
                            if (!portTabItem.RunningCommands)
                                break;
                            await Task.Delay(delay);
                            advancedSend.CommandList[idx].StatusText = $"{command.Delay - i} ms";
                        }
                        advancedSend.CommandList[idx].StatusText = "0 ms";
                    }
                    advancedSend.CommandList[idx].Status = RunningStatus.WaitingToRun;
                    idx++;
                    if (idx >= advancedSend.CommandList.Count)
                    {
                        idx = 0;
                        advancedSend.CommandList = advancedSend.CommandList.OrderBy(arg => arg.Order).ToList();
                    }
                }
                Dispatcher.Invoke(() =>
                {
                    SetRunningStatus(button, false);
                });
            });
        }


        public void SetRunningStatus(Button button, bool running)
        {
            button.IsEnabled = !running;
            StackPanel stackPanel = button.Parent as StackPanel;
            Button stopButton = stackPanel.Children.OfType<Button>().LastOrDefault();
            if (stopButton != null)
                stopButton.IsEnabled = running;
        }


        public async Task<bool> AsyncSendCommand(int idx, string portName, SendCommand command, AdvancedSend advancedSend)
        {
            bool success = false;
            await Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)delegate
            {
                SideComPort serialComPort = vieModel.SideComPorts.Where(arg => arg.Name.Equals(portName)).FirstOrDefault();
                if (serialComPort == null || serialComPort.PortTabItem == null || serialComPort.PortTabItem.SerialPort == null)
                {
                    success = false;
                    return;
                }
                SerialPort port = serialComPort.PortTabItem.SerialPort;
                PortTabItem portTabItem = vieModel.PortTabItems.Where(arg => arg.Name.Equals(portName)).FirstOrDefault();
                string value = command.Command;
                if (port != null)
                {
                    success = SendCommand(port, portTabItem, value);
                    if (!success)
                    {
                        success = false;
                        return;
                    }
                }
                if (idx < advancedSend.CommandList.Count)
                    advancedSend.CommandList[idx].Status = RunningStatus.AlreadySend;
                success = true;
            });
            return success;
        }



        private void Remark(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            ContextMenu contextMenu = menuItem.Parent as ContextMenu;
            FrameworkElement frameworkElement = contextMenu.PlacementTarget as FrameworkElement;
            if (frameworkElement != null && frameworkElement.Tag != null)
            {
                string portName = frameworkElement.Tag.ToString();
                SideComPort sideComPort = vieModel.SideComPorts.Where(arg => arg.Name.Equals(portName)).FirstOrDefault();
                if (sideComPort != null && sideComPort.PortTabItem is PortTabItem portTabItem)
                {
                    string defaultContent = "请输入备注";
                    if (!string.IsNullOrEmpty(portTabItem.Remark))
                        defaultContent = portTabItem.Remark;
                    DialogInput dialogInput = new DialogInput(this, defaultContent);
                    if (dialogInput.ShowDialog() == true)
                    {
                        string value = dialogInput.Text;
                        portTabItem.Remark = value;
                        Console.WriteLine(value);
                        portTabItem.SerialPort.SaveRemark(value);
                        sideComPort.Remark = value;
                        ComSettings comSettings = vieModel.ComSettingList.Where(arg => arg.PortName.Equals(portName)).FirstOrDefault();
                        if (comSettings != null)
                        {
                            Dictionary<string, object> dict = JsonUtils.TryDeserializeObject<Dictionary<string, object>>(comSettings.PortSetting);
                            if (dict != null && dict.ContainsKey("Remark"))
                            {
                                dict["Remark"] = value;
                                comSettings.PortSetting = JsonUtils.TrySerializeObject(dict);
                            }
                        }

                    }
                }
                else if (sideComPort.PortTabItem == null)
                {
                    MessageNotify.Info("打开串口后才能备注");
                }
            }
        }

        private void OpenLog(object sender, RoutedEventArgs e)
        {
            MessageNotify.Info("开发中");
        }

        private void BuadRate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems == null || e.AddedItems.Count <= 0) return;


            string text = e.AddedItems[0].ToString();
            if ("新增".Equals(text))
            {
                // 记录原来的下标
                int index = 0;
                string origin = e.RemovedItems[0].ToString();
                if (!string.IsNullOrEmpty(origin))
                {
                    for (int i = 0; i < vieModel.BaudRates.Count; i++)
                    {
                        if (vieModel.BaudRates[i].Equals(origin))
                        {
                            index = i;
                            break;
                        }
                    }
                }
                DialogInput dialogInput = new DialogInput(this, "请输入波特率");
                bool success = false;
                if ((bool)dialogInput.ShowDialog())
                {
                    string value = dialogInput.Text;
                    if (!string.IsNullOrEmpty(value) && int.TryParse(value, out int baudrate) &&
                        !vieModel.BaudRates.Contains(baudrate.ToString()))
                    {

                        vieModel.BaudRates.RemoveAt(vieModel.BaudRates.Count - 1);
                        vieModel.BaudRates.Add(baudrate.ToString());
                        vieModel.BaudRates.Add("新增");
                        success = true;
                        (sender as ComboBox).SelectedIndex = vieModel.BaudRates.Count - 2;
                        // 保存当前项目
                        vieModel.SaveBaudRate();
                        vieModel.SaveBaudRate((sender as ComboBox).Tag.ToString(), text);
                    }

                }
                if (!success)
                {
                    (sender as ComboBox).SelectedIndex = index;

                }
            }

        }
        private void SortSidePorts(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem != null && menuItem.Tag != null)
            {
                List<SideComPort> sideComPorts = vieModel.SideComPorts.ToList();
                string value = menuItem.Tag.ToString();
                Enum.TryParse(value, out ComPortSortType sortType);
                vieModel.InitPortData(sortType);
                RetainSidePortValue(sideComPorts);
            }
        }



        private void ShowPluginWindow(object sender, RoutedEventArgs e)
        {
            Window_Plugin window_Plugin = new Window_Plugin();

            PluginConfig config = new PluginConfig();
            config.PluginBaseDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");
            config.RemoteUrl = UrlManager.GetPluginUrl();
            // 读取本地配置
            window_Plugin.OnEnabledChange += (enabled) =>
            {
                return true;
            };

            window_Plugin.OnDelete += (data) =>
            {
                return true;
            };

            window_Plugin.OnBeginDownload += (data) =>
            {
                return true;
            };

            window_Plugin.SetConfig(config);
            window_Plugin.Show();
        }

        private async void CheckUpgrade()
        {
            // 启动后检查更新
            try
            {
                await Task.Delay(UpgradeHelper.AUTO_CHECK_UPGRADE_DELAY);
                (string LatestVersion, string ReleaseDate, string ReleaseNote) result = await UpgradeHelper.GetUpgardeInfo();
                string remote = result.LatestVersion;
                string ReleaseDate = result.ReleaseDate;
                if (!string.IsNullOrEmpty(remote))
                {
                    string local = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                    local = local.Substring(0, local.Length - ".0.0".Length);
                    if (local.CompareTo(remote) < 0)
                    {
                        bool opened = (bool)new MsgBox(this,
                            $"存在新版本\n版本：{remote}\n日期：{ReleaseDate}").ShowDialog();
                        if (opened)
                            UpgradeHelper.OpenWindow();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
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
            if (ConfigManager.Settings.AutoBackup)
            {
                int period = Config.WindowConfig.Settings.BackUpPeriods[(int)ConfigManager.Settings.AutoBackupPeriodIndex];
                bool backup = false;
                string BackupPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backup");
                string[] arr = DirHelper.TryGetDirList(BackupPath);
                if (arr != null && arr.Length > 0)
                {
                    string dirname = arr[arr.Length - 1];
                    if (Directory.Exists(dirname))
                    {
                        string dirName = Path.GetFileName(dirname);
                        DateTime before = DateTime.Now.AddDays(1);
                        DateTime now = DateTime.Now;
                        DateTime.TryParse(dirName, out before);
                        if (now.CompareTo(before) < 0 || (now - before).TotalDays > period)
                        {
                            backup = true;
                        }
                    }
                }
                else
                {
                    backup = true;
                }

                if (backup)
                {
                    string dir = Path.Combine(BackupPath, DateHelper.NowDate());
                    bool created = DirHelper.TryCreateDirectory(dir);
                    if (!created) return false;
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


        private SendCommand CurrentEditCommand;

        private void EditSendCommand(object sender, RoutedEventArgs e)
        {
            editTextBoxOrder.Text = "";
            editTextBoxName.Text = "";
            editTextBoxDelay.Text = "";
            editTextBoxCommand.Text = "";


            MenuItem menuItem = sender as MenuItem;
            Button button = (menuItem.Parent as ContextMenu).PlacementTarget as Button;
            if (button != null && button.Tag != null && vieModel.CurrentAdvancedSend != null)
            {
                string commandID = button.Tag.ToString();
                List<SendCommand> sendCommands = JsonUtils.TryDeserializeObject<List<SendCommand>>(vieModel.CurrentAdvancedSend.Commands).OrderBy(arg => arg.Order).ToList();
                SendCommand sendCommand = sendCommands.Where(arg => arg.CommandID.ToString().Equals(commandID)).FirstOrDefault();
                if (sendCommand != null)
                {
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
            if (button != null && button.Tag != null && vieModel.CurrentAdvancedSend != null)
            {
                string commandID = button.Tag.ToString();
                List<SendCommand> sendCommands = JsonUtils.TryDeserializeObject<List<SendCommand>>(vieModel.CurrentAdvancedSend.Commands).OrderBy(arg => arg.Order).ToList();
                SendCommand sendCommand = sendCommands.Where(arg => arg.CommandID.ToString().Equals(commandID)).FirstOrDefault();
                if (sendCommand != null)
                {
                    sendCommands.Remove(sendCommand);
                    AdvancedSend advancedSend = vieModel.CurrentAdvancedSend;
                    if (!string.IsNullOrEmpty(advancedSend.Commands))
                    {
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
            if (!string.IsNullOrEmpty(advancedSend.Commands) && CurrentEditCommand != null)
            {
                advancedSend.CommandList = JsonUtils.TryDeserializeObject<List<SendCommand>>(advancedSend.Commands);

                for (int i = 0; i < advancedSend.CommandList.Count; i++)
                {
                    if (advancedSend.CommandList[i].CommandID.Equals(CurrentEditCommand.CommandID))
                    {
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



        private Window_ShortCut window_ShortCut;
        private void OpenShortCut(object sender, RoutedEventArgs e)
        {
            if (window_ShortCut != null) window_ShortCut.Close();
            window_ShortCut = null;
            window_ShortCut = new Window_ShortCut();
            window_ShortCut.ShowDialog();
            window_ShortCut.BringIntoView();
            window_ShortCut.Activate();
        }

        private Grid GetBaseGridByPortName(string portName)
        {
            for (int i = 0; i < itemsControl.Items.Count; i++)
            {
                ContentPresenter presenter = (ContentPresenter)itemsControl.ItemContainerGenerator.ContainerFromItem(itemsControl.Items[i]);
                if (presenter == null) continue;
                Grid grid = VisualHelper.FindElementByName<Grid>(presenter, "baseGrid");
                if (grid == null || grid.Tag == null) continue;
                string name = grid.Tag.ToString();
                if (portName.Equals(name))
                {
                    return grid;
                }
            }
            return null;
        }

        private async void baseGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            string portName = "";
            if (vieModel.PortTabItems?.Count > 0)
            {
                foreach (var portTabItem in vieModel.PortTabItems)
                {
                    if (portTabItem.Selected)
                    {
                        portName = portTabItem.Name;
                        break;
                    }
                }
            }

            Console.WriteLine($"key = {e.Key}");

            // 快捷键检测
            ShortCutBinding shortCutBinding = null;
            int max = 0;
            foreach (var item in vieModel.ShortCutBindings)
            {
                // 贪婪匹配：最多的按键按下
                if (KeyBoardHelper.IsAllKeyDown(item.KeyList) && item.KeyList.Count > max)
                {
                    shortCutBinding = item;
                    max = item.KeyList.Count;
                }
            }
            if (shortCutBinding == null) return;

            switch (shortCutBinding.KeyID)
            {
                case 1:
                    SideComPort sideComPort = vieModel.SideComPorts.Where(arg => arg.Name.Equals(portName)).FirstOrDefault();
                    if (sideComPort == null)
                    {
                        MessageCard.Error($"打开 {portName} 失败！");
                        return;
                    }

                    if (sideComPort.Connected)
                    {
                        await ClosePort(portName);
                    }
                    else
                    {
                        // 连接
                        await OpenPort(sideComPort);
                    }

                    break;
                case 2:
                    {
                        // 收起展开发送栏
                        Grid baseGrid = sender as Grid;
                        if (baseGrid != null)
                        {
                            double height = baseGrid.RowDefinitions[2].ActualHeight;
                            if (height <= 10)
                                baseGrid.RowDefinitions[2].Height = new GridLength(DEFAULT_SEND_PANEL_HEIGHT, GridUnitType.Pixel);
                            else
                                baseGrid.RowDefinitions[2].Height = new GridLength(0, GridUnitType.Pixel);
                        }

                    }
                    break;
                case 3:
                    // 全屏
                    this.MaxWindow(null, null);

                    break;
                case 4:
                    {
                        // 固定滚屏
                        Grid baseGrid = sender as Grid;
                        (ToggleButton toggleButton, TextEditor textEditor) = FindToggleButtonByBaseGrid(baseGrid);
                        if (!toggleButton.IsEnabled) return;
                        toggleButton.IsChecked = !toggleButton.IsChecked;
                        if ((bool)toggleButton.IsChecked)
                            textEditor.TextChanged -= TextBox_TextChanged;
                        else
                        {
                            textEditor.TextChanged -= TextBox_TextChanged;
                            textEditor.TextChanged += TextBox_TextChanged;
                        }
                    }
                    break;
                case 5:
                    {

                        // hex 转换
                        Grid baseGrid = sender as Grid;
                        (ToggleButton toggleButton, TextEditor textEditor) = FindToggleButtonByBaseGrid(baseGrid);
                        if (textEditor != null)
                            OpenHex(textEditor.SelectedText);
                    }
                    break;
                case 6:

                    {
                        // 时间戳
                        Grid baseGrid = sender as Grid;
                        (ToggleButton toggleButton, TextEditor textEditor) = FindToggleButtonByBaseGrid(baseGrid);
                        if (textEditor != null)
                            OpenTime(textEditor.SelectedText);
                    }
                    break;
                case 7:
                    {
                        // 格式化为 JSON
                        Grid baseGrid = sender as Grid;
                        (ToggleButton toggleButton, TextEditor textEditor) = FindToggleButtonByBaseGrid(baseGrid);
                        if (textEditor != null)
                        {
                            string origin = textEditor.SelectedText;
                            string format = FormatString(FormatType.JSON, origin);
                            textEditor.SelectedText = format;
                        }
                    }
                    break;
                case 8:
                    {
                        // 合并为一行
                        Grid baseGrid = sender as Grid;
                        (ToggleButton toggleButton, TextEditor textEditor) = FindToggleButtonByBaseGrid(baseGrid);
                        if (textEditor != null)
                        {
                            string origin = textEditor.SelectedText;
                            string format = FormatString(FormatType.JOINLINE, origin);
                            textEditor.SelectedText = format;
                        }
                    }

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
            if (depth == 0)
            {
                if (menuItem != null && menuItem.Parent is ContextMenu contextMenu)
                {
                    if (contextMenu.PlacementTarget is TextEditor textEditor)
                    {
                        return textEditor;
                    }
                }
            }
            else if (depth == 1)
            {
                if (menuItem != null && (menuItem.Parent as MenuItem).Parent is ContextMenu contextMenu)
                {
                    if (contextMenu.PlacementTarget is TextEditor textEditor)
                    {
                        return textEditor;
                    }
                }
            }
            return null;
        }

        private void FormatJson(object sender, RoutedEventArgs e)
        {
            TextEditor textEditor = GetTextEditorFromMenuItem((MenuItem)sender, 1);
            if (textEditor == null) return;
            string origin = textEditor.SelectedText;
            string format = FormatString(FormatType.JSON, origin);
            textEditor.SelectedText = format;
        }

        private string FormatString(FormatType formatType, string origin)
        {
            if (string.IsNullOrEmpty(origin)) return "";
            switch (formatType)
            {
                case FormatType.JSON:
                    Dictionary<string, object> dictionary = JsonUtils.TryDeserializeObject<Dictionary<string, object>>(origin);
                    if (dictionary != null)
                    {
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
            if (textEditor == null) return;
            string origin = textEditor.SelectedText;
            string format = FormatString(FormatType.JOINLINE, origin);
            textEditor.SelectedText = format;
        }

        private void ShowUpgradeWindow(object sender, RoutedEventArgs e)
        {
            UpgradeHelper.OpenWindow();
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

        private void SetTextWrap(object sender, RoutedEventArgs e)
        {
            //bool status = GetMenuItemCheckedStatus(sender);
            //foreach (PortTabItem item in vieModel.PortTabItems)
            //{
            //    TextEditor textEditor = FindTextBoxByPortName(item.Name);
            //    if (textEditor != null)
            //        textEditor.WordWrap = status;
            //}
            //vieModel.AutoTextWrap = status;
        }

        private void SetTextEditOption(string optionName, object status)
        {
            foreach (PortTabItem item in vieModel.PortTabItems)
            {
                TextEditor textEditor = FindTextBoxByPortName(item.Name);
                if (textEditor != null)
                {
                    TextEditorOptions options = textEditor.Options;
                    if (options == null) continue;
                    System.Reflection.PropertyInfo propertyInfo = options.GetType().GetProperty(optionName);
                    if (propertyInfo == null) continue;
                    propertyInfo.SetValue(options, status);
                }
            }
        }
        private void SetTextPropOption(string propName, object status)
        {
            foreach (PortTabItem item in vieModel.PortTabItems)
            {
                TextEditor textEditor = FindTextBoxByPortName(item.Name);
                if (textEditor != null)
                {
                    System.Reflection.PropertyInfo propertyInfo = textEditor.GetType().GetProperty(propName);
                    if (propertyInfo == null) continue;
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

        private void SetTextForeground(object sender, RoutedEventArgs e)
        {
            StackPanel stackPanel = (sender as Button).Parent as StackPanel;
            ColorPicker colorPicker = stackPanel.Children.OfType<ColorPicker>().FirstOrDefault();
            SolidColorBrush brush = new SolidColorBrush(colorPicker.SelectedColor);
            SetTextPropOption("Foreground", brush);
            ConfigManager.Main.TextForeground = brush.ToString();
            if (stackPanel.Tag != null && stackPanel.Tag is ContextMenu contextMenu)
                contextMenu.IsOpen = false;
        }

        private void textBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ((sender as TextEditor).Parent as Border).BorderBrush = (SolidColorBrush)Application.Current.Resources["Button.Selected.BorderBrush"];
        }

        private void textBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ((sender as TextEditor).Parent as Border).BorderBrush = Brushes.Transparent;
        }
        Window_VirtualPort virtualPort;
        private async void ShowVirtualPort(object sender, RoutedEventArgs e)
        {
            if (virtualPort == null)

            {
                virtualPort = new Window_VirtualPort();
                virtualPort.Show();
            }
            else
            {
                if (virtualPort.IsClosed)
                {
                    virtualPort = new Window_VirtualPort();
                    virtualPort.Show();
                }
                virtualPort.BringIntoView();
                virtualPort.Focus();
            }
        }





        private void MenuItem_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem.Items != null && menuItem.Items.Count > 0 && menuItem.Items[0] is MenuItem item)
            {
                Task.Run(async () =>
                {
                    await Task.Delay(200);  // 等待一会，因为 template 未渲染出来
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        ControlTemplate template = item.Template;
                        object v = template.FindName("colorPicker", item);
                        if (v != null && v is ColorPicker colorPicker)
                        {

                            string colorText = ConfigManager.Main.TextForeground;
                            if (!string.IsNullOrEmpty(colorText))
                            {

                                Brush brush = SuperUtils.WPF.VisualTools.VisualHelper.HexStringToBrush(colorText);
                                if (brush != null)
                                {
                                    SolidColorBrush solidColorBrush = (SolidColorBrush)brush;
                                    colorPicker.SetCurrentColor(solidColorBrush.Color);
                                }

                            }
                        }
                    });


                });
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

        private void AddNewVarMonitor(object sender, RoutedEventArgs e)
        {
            // 新增监视变量
            if (sender is Button button && button.Tag != null)
            {
                string name = button.Tag.ToString();
                PortTabItem portTabItem = vieModel.PortTabItems.FirstOrDefault(arg => arg.Name.Equals(name));
                if (portTabItem != null)
                {
                    vieModel.NewVarMonitor(portTabItem, name);
                }

            }


        }

        private void DeleteVarMonitory(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag != null &&
                (element.Parent as FrameworkElement).Tag != null &&
                (element.Parent as FrameworkElement).Tag is System.Windows.Controls.DataGrid dataGrid &&
                dataGrid.Tag != null)
            {
                string name = dataGrid.Tag.ToString();
                PortTabItem portTabItem = vieModel.PortTabItems.FirstOrDefault(arg => arg.Name.Equals(name));
                if (portTabItem != null && long.TryParse(element.Tag.ToString(), out long id))
                    vieModel.DeleteVarMonitor(portTabItem, id);
            }
        }

        private void OpenVarMonitorDataPath(object sender, RoutedEventArgs e)
        {

        }

        private void SetMonitorTabVisible(object sender, RoutedEventArgs e)
        {
            ToggleButton toggleButton = sender as ToggleButton;


            if (toggleButton.Parent is StackPanel panel &&
                panel.Parent is Grid grid && grid.Parent is Grid rootGrid)
            {
                Grid monitorGrid = rootGrid.FindName("monitorGrid") as Grid;

                if (monitorGrid != null)
                {
                    monitorGrid.Visibility = (bool)toggleButton.IsChecked ? Visibility.Visible : Visibility.Collapsed;

                }
            }
        }

        private void RefreshVarMonitor(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                string name = button.Tag.ToString();
                PortTabItem portTabItem = vieModel.PortTabItems.FirstOrDefault(arg => arg.Name.Equals(name));
                if (portTabItem != null)
                {
                    portTabItem.VarMonitors = new System.Collections.ObjectModel.ObservableCollection<VarMonitor>();
                    foreach (var item in vieModel.GetVarMonitorByPortName(name))
                    {
                        portTabItem.VarMonitors.Add(item);
                    }
                }
            }

        }

        private void DrawMonitorPicture(object sender, RoutedEventArgs e)
        {

        }

        private void SaveVarMonitor(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                string name = button.Tag.ToString();
                PortTabItem portTabItem = vieModel.PortTabItems.FirstOrDefault(arg => arg.Name.Equals(name));
                if (portTabItem != null)
                {
                    vieModel.SaveMonitor(portTabItem);
                }
            }
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleButton toggleButton = sender as ToggleButton;
            Grid grid = toggleButton.Parent as Grid;
            TextBox textBox = grid.Children.OfType<TextBox>().FirstOrDefault();
            if ((bool)toggleButton.IsChecked)
            {
                textBox.TextWrapping = TextWrapping.Wrap;

            }
            else
            {
                textBox.TextWrapping = TextWrapping.NoWrap;
            }
        }
    }
}
