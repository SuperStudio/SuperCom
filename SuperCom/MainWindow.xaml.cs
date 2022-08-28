using SuperControls.Style;
using SuperCom.CustomWindows;
using SuperCom.Entity;
using SuperCom.Utils;
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

namespace SuperCom
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : BaseWindow
    {
        public static List<string> OpeningWindows = new List<string>();
        public bool CloseToTaskBar;
        public static bool WindowsVisible = true;
        public static TimeSpan FadeInterval { get; set; }

        public List<SideComPort> SerialPorts { get; set; }


        public VieModel_Main vieModel { get; set; }




        public MainWindow()
        {
            InitializeComponent();
            FadeInterval = TimeSpan.FromMilliseconds(150);//淡入淡出时间
            vieModel = new VieModel_Main();
            this.DataContext = vieModel;
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
            vieModel.InitPortSampleData();
            for (int i = 0; i < vieModel.SideComPorts.Count; i++)
            {
                SideComPort sideComPort = sideComPorts.Where(arg => arg.Name.Equals(vieModel.SideComPorts[i].Name)).FirstOrDefault();
                if (sideComPort != null)
                {
                    vieModel.SideComPorts[i] = sideComPort;
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
            await Task.Delay(1000);
            portTabItem.TextBox = FindTextBoxByPortName(portName);
            sideComPort.PortTabItem = portTabItem;
            await Task.Run(() =>
            {
                try
                {
                    serialPort.Open();
                    portTabItem.ConnectTime = DateTime.Now;
                    SetPortConnectStatus(portName, true);
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
                serialPort.Close();
                serialPort.Dispose();
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

            foreach (var item in vieModel.PortTabItems)
            {
                if (item.Name.Equals(portName))
                {
                    item.Connected = status;
                    break;
                }
            }

            foreach (var item in vieModel.SideComPorts)
            {
                if (item.Name.Equals(portName))
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

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

            (sender as TextBox).ScrollToEnd();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

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

        private void BaudRateChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (e.AddedItems.Count == 0) return;
            //ComboBoxItem comboBox = (ComboBoxItem)e.AddedItems[0];
            //int value = PortSetting.DEFAULT_BAUDRATE;
            //int.TryParse(comboBox.Content.ToString(), out value);
            //int index = tabControl.SelectedIndex;
            //if (index < vieModel.SerialPorts?.Count)
            //    vieModel.SerialPorts[index].BaudRate = value;
        }

        private void DataBitsChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (e.AddedItems.Count == 0) return;
            //ComboBoxItem comboBox = (ComboBoxItem)e.AddedItems[0];
            //int value = PortSetting.DEFAULT_DATABITS;
            //int.TryParse(comboBox.Content.ToString(), out value);
            //int index = tabControl.SelectedIndex;
            //if (index < vieModel.SerialPorts?.Count)
            //    vieModel.SerialPorts[index].DataBits = value;
        }

        private void StopBitsChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (e.AddedItems.Count == 0) return;
            //ComboBoxItem comboBox = (ComboBoxItem)e.AddedItems[0];
            //StopBits value = PortSetting.DEFAULT_STOPBITS;
            //Enum.TryParse(comboBox.Content.ToString(), out value);
            //int index = tabControl.SelectedIndex;
            //if (value == StopBits.None) value = PortSetting.DEFAULT_STOPBITS;
            //if (index < vieModel.SerialPorts?.Count)
            //    vieModel.SerialPorts[index].StopBits = value;
        }

        private void ParityChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (e.AddedItems.Count == 0) return;
            //ComboBoxItem comboBox = (ComboBoxItem)e.AddedItems[0];
            //Parity value = PortSetting.DEFAULT_PARITY;
            //Enum.TryParse(comboBox.Content.ToString(), out value);
            //int index = tabControl.SelectedIndex;
            //if (index < vieModel.SerialPorts?.Count)
            //    vieModel.SerialPorts[index].Parity = value;
        }


        private static double MAX_FONTSIZE = 25;
        private static double MIN_FONTSIZE = 5;

        private void Border_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                Border border = sender as Border;
                TextBox textBox = border.Child as TextBox;
                double fontSize = textBox.FontSize;
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

                textBox.FontSize = fontSize;
                e.Handled = true;
            }

        }

        private void SetTextBoxScroll(object sender, RoutedEventArgs e)
        {
            ToggleButton toggleButton = sender as ToggleButton;
            bool fix = (bool)toggleButton.IsChecked;
            PortTabItem portTabItem = GetPortItem(sender as FrameworkElement);
            if (portTabItem != null)
            {
                if (fix)
                    portTabItem.TextBox.TextChanged -= TextBox_TextChanged;
                else
                    portTabItem.TextBox.TextChanged += TextBox_TextChanged;
            }
        }

        private TextBox FindTextBox(Grid rootGrid)
        {
            if (rootGrid == null) return null;
            Border border = rootGrid.Children.OfType<Border>().FirstOrDefault();
            if (border != null && border.Child is TextBox textBox)
            {
                return textBox;
            }
            return null;
        }

        private TextBox FindTextBoxByPortName(string portName)
        {
            if (string.IsNullOrEmpty(portName)) return null;
            for (int i = 0; i < itemsControl.Items.Count; i++)
            {
                ContentPresenter presenter = (ContentPresenter)itemsControl.ItemContainerGenerator.ContainerFromItem(itemsControl.Items[i]);
                if (presenter == null) continue;
                Grid grid = VisualHelper.FindElementByName<Grid>(presenter, "rootGrid");
                if (grid != null && grid.Tag != null && portName.Equals(grid.Tag.ToString())
                    && FindTextBox(grid) is TextBox textBox) return textBox;
            }
            return null;
        }

        private void GotoTop(object sender, MouseButtonEventArgs e)
        {
            StackPanel stackPanel = (sender as Border).Parent as StackPanel;
            if (stackPanel != null && stackPanel.Parent is Grid grid)
            {
                TextBox textBox = FindTextBox(grid);
                if (textBox != null)
                    textBox.ScrollToHome();
            }
        }

        private void GotoBottom(object sender, MouseButtonEventArgs e)
        {
            StackPanel stackPanel = (sender as Border).Parent as StackPanel;
            if (stackPanel != null && stackPanel.Parent is Grid grid)
            {
                TextBox textBox = FindTextBox(grid);
                if (textBox != null)
                    textBox.ScrollToEnd();
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
                FileHelper.TryOpenSelectPath(fileName);
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

        private void AddTimeStamp(object sender, RoutedEventArgs e)
        {
            PortTabItem portTabItem = GetPortItem(sender as FrameworkElement);
            portTabItem.AddTimeStamp = (bool)(sender as CheckBox).IsChecked;
        }

        private void SendCommand(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null && button.Tag != null)
            {
                string portName = button.Tag.ToString();
                if (string.IsNullOrEmpty(portName)) return;
                SideComPort serialComPort = vieModel.SideComPorts.Where(arg => arg.Name.Equals(portName)).FirstOrDefault();
                if (serialComPort == null || serialComPort.PortTabItem == null || serialComPort.PortTabItem.SerialPort == null)
                {
                    MessageCard.Error($"连接串口 {portName} 失败！");
                    return;
                }
                SerialPort port = serialComPort.PortTabItem.SerialPort;
                PortTabItem portTabItem = vieModel.PortTabItems.Where(arg => arg.Name.Equals(portName)).FirstOrDefault();
                if (port != null)
                {
                    string value = portTabItem.WriteData;
                    if (portTabItem.AddNewLineWhenWrite)
                    {
                        value += "\r\n";
                    }
                    portTabItem.SaveData($"【SEND】 {value}");
                    try
                    {
                        port.Write(value);
                    }
                    catch (Exception ex)
                    {
                        MessageCard.Error(ex.Message);
                    }

                    vieModel.StatusText = $"【发送命令】=>{portTabItem.WriteData}";
                }
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




        private void TextBlock_IsVisibleChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void PortTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            Console.WriteLine($"TextBox.Text = {(sender as TextBox)?.Text}");
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
    }
}
