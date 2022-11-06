using SuperControls.Style;
using SuperCom.CustomWindows;
using SuperCom.Entity;
using SuperCom.ViewModel;
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
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using SuperUtils.Time;
using SuperUtils.IO;
using SuperUtils.Common;
using SuperCom.Config;
using SuperCom.Windows;
using SuperUtils.Framework.ORM.Mapper;
using SuperUtils.WPF.VisualTools;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using System.Xml;
using ICSharpCode.AvalonEdit.Search;
using SuperControls.Style.XAML.CustomWindows;

namespace SuperCom
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : BaseWindow
    {
        Window_Setting window_Setting { get; set; }
        Window_AdvancedSend window_AdvancedSend { get; set; }




        public static List<string> OpeningWindows = new List<string>();
        public bool CloseToTaskBar;
        public static bool WindowsVisible = true;
        public static TimeSpan FadeInterval { get; set; }

        public List<SideComPort> SerialPorts { get; set; }


        public VieModel_Main vieModel { get; set; }




        public MainWindow()
        {
            InitializeComponent();
            Init();


            // 注册 SuperUtils 异常事件
            SuperUtils.Handler.ExceptionHandler.OnError += (e) =>
            {
                MessageCard.Error(e.Message);
            };

            SuperUtils.Handler.LogHandler.OnLog += (msg) =>
            {
                Console.WriteLine(msg);
            };


            FadeInterval = TimeSpan.FromMilliseconds(150);//淡入淡出时间
            vieModel = new VieModel_Main();
            this.DataContext = vieModel;
            // 读取设置列表
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
            CreateSqlTables();
            ConfigManager.InitConfig(); // 读取配置
            ReadXshdList();// 自定义语法高亮
        }

        private void ReadXshdList()
        {
            string[] xshd_list = new string[] { "ComLog" };
            foreach (var name in xshd_list)
            {
                try
                {
                    IHighlightingDefinition customHighlighting;
                    string xshdPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AvalonEdit", "Higlighting", $"{name}.xshd");
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
                    HighlightingManager.Instance.RegisterHighlighting(name, new string[] { ".cool" }, customHighlighting);
                }
                catch (Exception ex)
                {
                    MessageCard.Error(ex.Message);
                    continue;
                }

            }
        }


        private void CreateSqlTables()
        {

            ComSettings.InitSqlite();
            AdvancedSend.InitSqlite();
        }

        public override void CloseWindow(object sender, RoutedEventArgs e)
        {
            if (CloseToTaskBar && this.IsVisible == true)
            {
                SetWindowVisualStatus(false);
            }
            else
            {
                FadeOut();
                base.CloseWindow(sender, e);
            }
        }




        public void FadeOut()
        {
            //if (Properties.Settings.Default.EnableWindowFade)
            //{
            //    var anim = new DoubleAnimation(0, (Duration)FadeInterval);
            //    anim.Completed += (s, _) => this.Close();
            //    this.BeginAnimation(UIElement.OpacityProperty, anim);
            //}
            //else
            //{
            this.Close();
            //}
        }

        private void AnimateWindow(Window window)
        {
            window.Show();
            double opacity = 1;
            var anim = new DoubleAnimation(1, opacity, (Duration)FadeInterval, FillBehavior.Stop);
            anim.Completed += (s, _) => window.Opacity = opacity;
            window.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        private void SetWindowVisualStatus(bool visible, bool taskIconVisible = true)
        {

            if (visible)
            {
                foreach (Window window in App.Current.Windows)
                {
                    if (OpeningWindows.Contains(window.GetType().ToString()))
                    {
                        AnimateWindow(window);
                    }
                }

            }
            else
            {
                OpeningWindows.Clear();
                foreach (Window window in App.Current.Windows)
                {
                    window.Hide();
                    OpeningWindows.Add(window.GetType().ToString());
                }
            }
            WindowsVisible = visible;
        }

        public void MinWindow(object sender, RoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Minimized;

        }


        public void OnMaxWindow(object sender, RoutedEventArgs e)
        {
            this.MaxWindow(sender, e);

        }

        private void MoveWindow(object sender, MouseEventArgs e)
        {
            Border border = sender as Border;

            //移动窗口
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (baseWindowState == BaseWindowState.Maximized || (this.Width == SystemParameters.WorkArea.Width && this.Height == SystemParameters.WorkArea.Height))
                {
                    baseWindowState = 0;
                    double fracWidth = e.GetPosition(border).X / border.ActualWidth;
                    this.Width = WindowSize.Width;
                    this.Height = WindowSize.Height;
                    this.WindowState = System.Windows.WindowState.Normal;
                    this.Left = e.GetPosition(border).X - border.ActualWidth * fracWidth;
                    this.Top = e.GetPosition(border).Y - border.ActualHeight / 2;
                    this.OnLocationChanged(EventArgs.Empty);
                    MaxPath.Data = Geometry.Parse(PathData.MaxPath);
                    MaxMenuItem.Header = "最大化";
                }
                this.DragMove();
            }
        }

        private void Border_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Border border = (Border)sender;
            if (border == null) return;
            string portName = border.Tag.ToString();
            if (string.IsNullOrEmpty(portName) || vieModel.PortTabItems?.Count <= 0) return;

            for (int i = 0; i < vieModel.PortTabItems.Count; i++)
            {
                if (vieModel.PortTabItems[i].Name.Equals(portName))
                {
                    vieModel.PortTabItems[i].Selected = true;
                    //tabControl.SelectedIndex = i;
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
                {
                    grid.Visibility = Visibility.Visible;
                }
                else
                {
                    grid.Visibility = Visibility.Hidden;
                }
            }
        }

        private void scrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
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
            RemovePortTabItem(portName);
        }


        private void RemovePortTabItem(string portName)
        {
            SaveComSettings();
            int idx = -1;
            for (int i = 0; idx < vieModel.PortTabItems.Count; i++)
            {
                if (vieModel.PortTabItems[i].Name.Equals(portName))
                {
                    idx = i;
                    break;
                }
            }
            if (idx >= 0)
            {
                ClosePort(portName);
                vieModel.PortTabItems.RemoveAt(idx);
            }
        }

        private void RefreshPortsStatus(object sender, MouseButtonEventArgs e)
        {
            List<SideComPort> sideComPorts = vieModel.SideComPorts.ToList();
            vieModel.InitPortData();
            for (int i = 0; i < vieModel.SideComPorts.Count; i++)
            {
                string portName = vieModel.SideComPorts[i].Name;
                SideComPort sideComPort = sideComPorts.Where(arg => arg.Name.Equals(portName)).FirstOrDefault();

                if (sideComPort != null)
                {
                    vieModel.SideComPorts[i] = sideComPort;
                    ComSettings comSettings = vieModel.ComSettingList.Where(arg => arg.PortName.Equals(portName)).FirstOrDefault();
                    if (comSettings != null && !string.IsNullOrEmpty(comSettings.PortSetting))
                    {
                        vieModel.SideComPorts[i].Remark = CustomSerialPort.GetRemark(comSettings.PortSetting);
                    }
                }
            }
        }

        private async void ConnectPort(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == null || button.Tag == null) return;
            button.IsEnabled = false;
            string content = button.Content.ToString();
            string portName = button.Tag.ToString();
            SideComPort sideComPort = vieModel.SideComPorts.Where(arg => arg.Name.Equals(portName)).FirstOrDefault();
            if (sideComPort == null)
            {
                MessageCard.Error($"打开 {portName} 失败！");
                return;
            }


            if ("连接".Equals(content))
            {
                // 连接
                await OpenPort(sideComPort);
            }
            else
            {
                // 断开
                ClosePort(portName);
            }
            button.IsEnabled = true;
        }

        private async Task<bool> OpenPort(SideComPort sideComPort)
        {
            if (sideComPort == null) return false;
            string portName = sideComPort.Name;
            OpenPortTabItem(portName, true);
            PortTabItem portTabItem = vieModel.PortTabItems.Where(arg => arg.Name.Equals(portName)).FirstOrDefault();
            if (portTabItem == null)
            {
                MessageCard.Error($"打开 {portName} 失败！");
                return false;
            }

            await Task.Delay(1000);
            portTabItem.TextEditor = FindTextBoxByPortName(portName);
            // 搜索框
            SearchPanel.Install(portTabItem.TextEditor);

            //string xshdPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AvalonEdit", "Higlighting", "Default.xshd");
            //using (Stream s = File.OpenRead(xshdPath))
            //{
            //    using (System.Xml.XmlTextReader reader = new System.Xml.XmlTextReader(s))
            //    {
            //        portTabItem.TextEditor.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load
            //            (reader, ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance);
            //    }
            //}


            sideComPort.PortTabItem = portTabItem;
            await Task.Run(() =>
            {
                try
                {
                    CustomSerialPort serialPort = portTabItem.SerialPort;
                    if (!serialPort.IsOpen)
                    {
                        serialPort.Open();
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

        private void ClosePort(string portName)
        {
            PortTabItem portTabItem = vieModel.PortTabItems.Where(arg => arg.Name.Equals(portName)).FirstOrDefault();
            if (portTabItem == null) return;
            CustomSerialPort serialPort = portTabItem.SerialPort;
            if (serialPort != null && serialPort.IsOpen)
            {
                try
                {
                    portTabItem.RX = 0;
                    portTabItem.TX = 0;
                    serialPort.Close();
                    serialPort.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

            }
            SetPortConnectStatus(portName, false);
        }

        private void HandleDataReceived(CustomSerialPort serialPort)
        {
            string line = serialPort.ReadExisting();
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

        private void SetPortConnectStatus(string portName, bool status)
        {

            foreach (PortTabItem item in vieModel.PortTabItems)
            {
                if (item != null && item.Name.Equals(portName))
                {
                    item.Connected = status;
                    break;
                }
            }

            foreach (SideComPort item in vieModel.SideComPorts)
            {
                if (item != null && item.Name.Equals(portName))
                {
                    item.Connected = status;
                    break;
                }
            }
        }

        private void OpenPortTabItem(string portName, bool connect)
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
                    portTabItem.SerialPort.SetPortSettingByJson(comSettings.PortSetting);
                    portTabItem.Remark = portTabItem.SerialPort.Remark;
                }
                portTabItem.Selected = true;
                SetGridVisible(portName);
                vieModel.PortTabItems.Add(portTabItem);
            }


        }

        private void Grid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                Grid grid = sender as Grid;
                if (grid == null || grid.Tag == null) return;
                string portName = grid.Tag.ToString();
                OpenPortTabItem(portName, false);
            }
        }

        private void TextBox_TextChanged(object sender, EventArgs e)
        {

            (sender as TextEditor).ScrollToEnd();
        }

        private void ShowAbout(object sender, RoutedEventArgs e)
        {
            new About(this).ShowDialog();
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
                    portTabItem.TextEditor.TextChanged += TextBox_TextChanged;
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
            if (string.IsNullOrEmpty(portName)) return null;
            for (int i = 0; i < itemsControl.Items.Count; i++)
            {
                ContentPresenter presenter = (ContentPresenter)itemsControl.ItemContainerGenerator.ContainerFromItem(itemsControl.Items[i]);
                if (presenter == null) continue;
                Grid grid = VisualHelper.FindElementByName<Grid>(presenter, "rootGrid");
                if (grid != null && grid.Tag != null && portName.Equals(grid.Tag.ToString())
                    && FindTextBox(grid) is TextEditor textEditor) return textEditor;
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
                string fileName = portTabItem.GetSaveFileName();
                if (File.Exists(fileName))
                {
                    FileHelper.TryOpenSelectPath(fileName);
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

        //private void AddTimeStamp(object sender, RoutedEventArgs e)
        //{
        //    PortTabItem portTabItem = GetPortItem(sender as FrameworkElement);
        //    portTabItem.AddTimeStamp = (bool)(sender as CheckBox).IsChecked;
        //}

        private void SendCommand(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null && button.Tag != null)
            {
                string portName = button.Tag.ToString();
                if (string.IsNullOrEmpty(portName)) return;
                SendCommand(portName);
            }
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

        public bool SendCommand(SerialPort port, PortTabItem portTabItem, string value, bool saveToHistory = true)
        {
            if (portTabItem.AddNewLineWhenWrite)
            {
                value += "\r\n";
            }
            portTabItem.SaveData($"SEND >>>>>>>>>> {value}");
            try
            {
                port.Write(value);
                portTabItem.TX += value.Length;
                // 保存到发送历史
                if (saveToHistory)
                {
                    vieModel.SendHistory.Add(value.Trim());
                    vieModel.SaveSendHistory();
                }
                vieModel.StatusText = $"【发送命令】=>{portTabItem.WriteData}";
                return true;
            }
            catch (Exception ex)
            {
                MessageCard.Error(ex.Message);
                return false;
            }
        }

        private void MaxCurrentWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                MaxWindow(sender, new RoutedEventArgs());

            }
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
                    MessageCard.Success("成功存到新文件！");
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
            MessageCard.Info("开发中");
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
            CloseAllPort(null, null);

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
            if (string.IsNullOrEmpty(text)) return;
            if (text.Length > MAX_TRANSFORM_SIZE)
            {
                MessageCard.Warning($"超过了 {MAX_TRANSFORM_SIZE}");
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
            if (text.Length > MAX_TIMESTAMP_LENGTH)
            {
                MessageCard.Warning($"超过了 {MAX_TIMESTAMP_LENGTH}");
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
                LocalTimeTextBox.Text = DateHelper.UnixTimeStampToDateTime(timeStamp, TimeComboBox.SelectedIndex == 0).ToLocalDate();
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
                string fileName = portTabItem.GetSaveFileName();
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



        private void mainWindow_ContentRendered(object sender, EventArgs e)
        {
            AdjustWindow();
            if (ConfigManager.Main.FirstRun) ConfigManager.Main.FirstRun = false;
            OpenBeforePorts();
            //new Window_AdvancedSend().Show();
        }


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
                    OpenPortTabItem(portName, false);
                }

            }
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
            Grid grid = (textBox.Parent as Border).Parent as Grid;
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

        private void DeleteSendHistory(object sender, RoutedEventArgs e)
        {
            Popup popup = (sender as Button).Tag as Popup;
            string value = ((sender as Button).Parent as Border).Tag.ToString();
            vieModel.SendHistory.RemoveWhere(arg => arg.Equals(value));
            vieModel.SaveSendHistory();
            if (popup != null && popup.IsOpen)
            {
                Grid grid1 = popup.Child as Grid;
                ItemsControl itemsControl = grid1.FindName("itemsControl") as ItemsControl;
                if (itemsControl.ItemsSource != null)
                {
                    List<string> list = itemsControl.ItemsSource as List<string>;
                    list.RemoveAll(arg => arg.Equals(value));
                    itemsControl.ItemsSource = null;
                    itemsControl.ItemsSource = list;
                    if (list.Count == 0) popup.IsOpen = false;
                }

            }
        }
        #endregion

        private void OpenSendPanel(object sender, RoutedEventArgs e)
        {
            window_AdvancedSend?.Close();
            window_AdvancedSend = new Window_AdvancedSend();
            window_AdvancedSend.Show();
            window_AdvancedSend.Focus();
            window_AdvancedSend.BringIntoView();
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


            Grid grid = comboBox.Parent as Grid;
            ScrollViewer scrollViewer = grid.Children.OfType<ScrollViewer>().LastOrDefault();
            ItemsControl itemsControl = scrollViewer.FindName("sendButtons") as ItemsControl;
            if (itemsControl == null)
            {
                return;
            }
            itemsControl.ItemsSource = null;
            if (comboBox.SelectedValue == null) return;
            string id = comboBox.SelectedValue.ToString();
            if (string.IsNullOrEmpty(id)) return;
            AdvancedSend advancedSend = vieModel.SendCommandProjects.Where(arg => arg.ProjectID.ToString().Equals(id)).FirstOrDefault();
            if (!string.IsNullOrEmpty(advancedSend.Commands))
            {
                itemsControl.ItemsSource = JsonUtils.TryDeserializeObject<List<SendCommand>>(advancedSend.Commands);
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
                TextEditor textEditor = item.TextEditor;
                if (textEditor != null)
                {
                    ComboBox comboBox = FindCombobox(textEditor);
                    if (comboBox != null)
                    {
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
            ComboBox comboBox = grid1.Children.OfType<ComboBox>().FirstOrDefault();
            return comboBox;
        }

        private void ScrollViewer_PreviewMouseWheel_1(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scrollViewer = sender as ScrollViewer;
            scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - e.Delta);
            e.Handled = true;
        }

        private void SendCustomCommand(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            string command = "";
            if (button.ToolTip != null)
            {
                command = button.ToolTip.ToString();
            }
            Border border = button.FindParentOfType<Border>("sendBorder");
            if (border != null && border.Tag != null)
            {
                string portName = border.Tag.ToString();
                SideComPort sideComPort = vieModel.SideComPorts.Where(arg => arg.Name.Equals(portName)).FirstOrDefault();
                if (sideComPort != null && sideComPort.PortTabItem != null && sideComPort.PortTabItem.SerialPort != null)
                {
                    SendCommand(sideComPort.PortTabItem.SerialPort, sideComPort.PortTabItem, command, false);
                }
            }

        }

        private void StartSendCommands(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Grid grid = button.Parent as Grid;
            ComboBox comboBox = grid.Children.OfType<ComboBox>().FirstOrDefault();
            if (comboBox != null && comboBox.SelectedValue != null)
            {
                string projectID = comboBox.SelectedValue.ToString();
                // 开始执行队列
                MessageCard.Warning("开发中");
            }
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
                    InputWindow inputWindow = new InputWindow(this, defaultContent, true);
                    if (inputWindow.ShowDialog() == true)
                    {
                        string value = inputWindow.Text;
                        portTabItem.Remark = value;
                        Console.WriteLine(value);
                        portTabItem.SerialPort.SaveRemark(value);
                        sideComPort.Remark = value;

                    }
                }
                else if (sideComPort.PortTabItem == null)
                {
                    MessageCard.Info("打开串口后才能备注");
                }
            }
        }

        private void OpenLog(object sender, RoutedEventArgs e)
        {
            MessageCard.Info("开发中");
        }

        private void BuadRate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems == null || e.AddedItems.Count <= 0) return;

            // 记录原来的下标
            int index = 0;
            ComboBoxItem origin = e.OriginalSource as ComboBoxItem;
            if (origin != null)
            {
                string value = origin.Content.ToString();
                for (int i = 0; i < vieModel.BaudRates.Count; i++)
                {
                    if (vieModel.BaudRates[i].Equals(value))
                    {
                        index = i;
                        break;
                    }
                }
            }
            string text = e.AddedItems[0].ToString();
            if ("新增".Equals(text))
            {
                InputWindow inputWindow = new InputWindow(this, "");
                bool success = false;
                if ((bool)inputWindow.ShowDialog())
                {
                    string value = inputWindow.Text;
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
                    }
                }
                if (!success)
                {
                    (sender as ComboBox).SelectedIndex = index;

                }
            }
            vieModel.SaveBaudRate((sender as ComboBox).Tag.ToString(), text);
        }


    }
}
