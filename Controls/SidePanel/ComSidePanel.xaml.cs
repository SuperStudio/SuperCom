using SuperCom.Comparers;
using SuperCom.Config;
using SuperCom.Core.Entity;
using SuperCom.Core.Events;
using SuperCom.Entity;
using SuperCom.Entity.Enums;
using SuperControls.Style;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static SuperCom.App;

namespace SuperCom.Controls
{
    /// <summary>
    /// ComSidePanel.xaml 的交互逻辑
    /// </summary>
    public partial class ComSidePanel : UserControl
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged([CallerMemberName] string name = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        /// <summary>
        /// 最后使用的串口排序类型
        /// </summary>
        private ComPortSortType LastSortType { get; set; } = ComPortSortType.AddTime;
        /// <summary>
        /// 最后使用的串口排序方式
        /// </summary>
        private bool LastSortDesc { get; set; } = false;


        public ComSidePanel()
        {
            InitializeComponent();
            Init();
        }


        private ObservableCollection<SideComPort> _SideComPorts;
        public ObservableCollection<SideComPort> SideComPorts {
            get { return _SideComPorts; }
            set { _SideComPorts = value; RaisePropertyChanged(); }
        }

        public HashSet<ComSettings> ComSettingList { get; set; }

        private void Init()
        {
            this.DataContext = this;
            InitPortData();
            InitEvent();
        }

        private void InitEvent()
        {
            BasicEventManager.RegisterEvent(EventType.StatusChanged, OnStatusChanged);
        }

        private void OnStatusChanged(object data)
        {
            if (data is TabInfo tabInfo && tabInfo.ConnectType == Core.Entity.Enums.ConnectType.Com &&
                tabInfo.Data is string name) {
                SideComPort sideComPort = SideComPorts.Where(arg => arg.Name.Equals(name)).FirstOrDefault();
                if (sideComPort != null) {
                    sideComPort.Connected = tabInfo.IsConnected;
                }
            }
        }

        public void InitPortData(ComPortSortType sortType = ComPortSortType.AddTime, bool desc = false)
        {
            Dictionary<string, string> dict = SerialPortEx.GetAllPorts();
            string[] ports = dict.Keys.ToArray();
            List<string> portNames = new List<string>();
            switch (sortType) {
                case ComPortSortType.AddTime:
                    if (desc)
                        portNames = ports.Reverse().ToList();
                    else
                        portNames = ports.ToList();
                    break;
                case ComPortSortType.PortName:
                    if (desc)
                        portNames = ports.OrderByDescending(name => name, new ComPortComparer()).ToList();
                    else
                        portNames = ports.OrderBy(name => name, new ComPortComparer()).ToList();
                    break;
                default:
                    break;


            }

            sideListBox.ItemsSource = null;
            SideComPorts = new ObservableCollection<SideComPort>();
            foreach (string port in portNames) {
                SideComPort sideComPort = new SideComPort(port, false);
                sideComPort.Detail = dict[port];
                SideComPorts.Add(sideComPort);
            }
            sideListBox.ItemsSource = SideComPorts;
        }

        private void SetAllMenuItemSortable(MenuItem menuItem)
        {
            ContextMenu contextMenu = menuItem.Parent as ContextMenu;
            List<MenuItem> menuItems = contextMenu.Items.OfType<MenuItem>().ToList();
            foreach (var item in menuItems) {
                MenuItemExt.SetSortable(item, false);
            }
        }

        /// <summary>
        /// 恢复侧边栏串口的配置信息
        /// </summary>
        /// <param name="sideComPorts"></param>
        private void RetainSidePortValue(List<SideComPort> sideComPorts)
        {
            if (sideComPorts == null || SideComPorts == null)
                return;
            int count = SideComPorts.Count;
            for (int i = 0; i < count; i++) {
                string portName = SideComPorts[i].Name;
                if (string.IsNullOrEmpty(portName))
                    continue;
                SideComPort sideComPort = sideComPorts.FirstOrDefault(arg => portName.Equals(arg.Name));
                if (sideComPort == null)
                    continue;
                SideComPorts[i] = sideComPort;
                ComSettings comSettings = ComSettingList.FirstOrDefault(arg => portName.Equals(arg.PortName));
                if (comSettings != null && !string.IsNullOrEmpty(comSettings.PortSetting)) {
                    SideComPorts[i].Remark = SerialPortEx.GetRemark(comSettings.PortSetting);
                    SideComPorts[i].Hide = SerialPortEx.GetHide(comSettings.PortSetting);
                    Logger.Info($"[{i + 1}/{count}]retain side com port: {portName}, remark: {SideComPorts[i].Remark}, hide: {SideComPorts[i].Hide}");
                }
            }
        }


        private void SortSidePorts(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem != null && menuItem.Tag != null) {
                SetAllMenuItemSortable(menuItem);
                MenuItemExt.SetSortable(menuItem, true);
                LastSortDesc = MenuItemExt.GetDesc(menuItem);
                List<SideComPort> sideComPorts = SideComPorts.ToList();
                string value = menuItem.Tag.ToString();
                Enum.TryParse(value, out ComPortSortType sortType);
                LastSortType = sortType;
                InitPortData(LastSortType, LastSortDesc);

                Logger.Info($"sort port, type: {LastSortType}, desc: {LastSortDesc}");

                RetainSidePortValue(sideComPorts);
                MenuItemExt.SetDesc(menuItem, !LastSortDesc);
            }
        }

        public void RefreshPortsStatus(object sender, RoutedEventArgs e)
        {
            List<SideComPort> sideComPorts = SideComPorts.ToList();
            InitPortData(LastSortType, LastSortDesc);
            RetainSidePortValue(sideComPorts);
        }

        private void OpenAllPort(object sender, RoutedEventArgs e)
        {
            List<string> nameList = SideComPorts.Select(arg => arg.Name).ToList();

            TabInfo tabInfo = new TabInfo() {
                Data = nameList,
                ConnectType = Core.Entity.Enums.ConnectType.Com,
                IsConnected = true,
            };

            BasicEventManager.SendEvent(EventType.OpenAll, tabInfo);
        }

        private void CloseAllPort(object sender, RoutedEventArgs e)
        {
            List<string> nameList = SideComPorts.Select(arg => arg.Name).ToList();
            TabInfo tabInfo = new TabInfo() {
                Data = nameList,
                ConnectType = Core.Entity.Enums.ConnectType.Com,
                IsConnected = false,
            };
            BasicEventManager.SendEvent(EventType.CloseAll, tabInfo);
        }


        private void HidePort(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            ContextMenu contextMenu = menuItem.Parent as ContextMenu;
            FrameworkElement frameworkElement = contextMenu.PlacementTarget as FrameworkElement;
            if (frameworkElement != null && frameworkElement.Tag != null) {
                string portName = frameworkElement.Tag.ToString();
                SideComPort sideComPort = SideComPorts.FirstOrDefault(arg => arg.Name.Equals(portName));
                if (sideComPort != null) {
                    sideComPort.Hide = true;
                }
            }
        }

        private void ShowAllHidePort(object sender, RoutedEventArgs e)
        {
            Logger.Info("show all hide port");
            foreach (SideComPort item in SideComPorts) {
                item.Hide = false;
            }

        }

        private void Grid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) {
                Grid grid = sender as Grid;
                if (grid == null || grid.Tag == null)
                    return;
                string portName = grid.Tag.ToString();

                TabInfo tabInfo = new TabInfo() {
                    Data = new List<string>() { portName },
                    ConnectType = Core.Entity.Enums.ConnectType.Com,
                    IsConnected = false
                };

                BasicEventManager.SendEvent(EventType.OpenTab, tabInfo);
            }
        }

        private void Remark(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            ContextMenu contextMenu = menuItem.Parent as ContextMenu;
            FrameworkElement frameworkElement = contextMenu.PlacementTarget as FrameworkElement;
            if (frameworkElement != null && frameworkElement.Tag != null) {
                string name = frameworkElement.Tag.ToString();
                BasicEventManager.SendEvent(EventType.Remark, name);
            }
        }

        private void ConnectPort(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button) || button.Tag == null || button.Content == null)
                return;
            string portName = button.Tag.ToString();
            TabInfo tabInfo = new TabInfo {
                Data = new List<string>() { portName },
                IsConnected = button.Content.ToString().Equals(LangManager.GetValueByKey("Connect")),
                ConnectType = Core.Entity.Enums.ConnectType.Com
            };
            BasicEventManager.SendEvent(EventType.ProcOne, tabInfo);
        }

        public void Update(int hash, string propertyName, object value)
        {
            if (SideComPorts == null)
                return;
            foreach (SideComPort sideComPort in SideComPorts) {
                if (sideComPort.GetHashCode() != hash)
                    continue;
                PropertyInfo propertyInfo = typeof(SideComPort).GetProperty(propertyName);
                if (propertyInfo == null) {
                    Logger.Error($"get property failed: {propertyName}");
                    return;
                }
                propertyInfo.SetValue(sideComPort, value);
            }
        }

        public object Get(int hash, PropertyInfo propertyInfo)
        {
            if (SideComPorts == null)
                return null;
            foreach (SideComPort sideComPort in SideComPorts) {
                if (sideComPort.GetHashCode() != hash)
                    continue;
                return propertyInfo.GetValue(sideComPort);
            }
            return null;
        }

        public bool Exists(int hash)
        {
            if (SideComPorts == null)
                return false;
            foreach (SideComPort sideComPort in SideComPorts) {
                if (sideComPort.GetHashCode() != hash)
                    continue;
                return true;
            }
            return false;
        }

        public void UpdateComSettingList(HashSet<ComSettings> comSettingList)
        {
            ComSettingList = comSettingList;
            // 设置配置
            foreach (var item in SideComPorts) {
                ComSettings comSettings = ComSettingList.FirstOrDefault(arg => arg.PortName.Equals(item.Name));
                if (comSettings != null && !string.IsNullOrEmpty(comSettings.PortSetting)) {
                    try {
                        item.Remark = SerialPortEx.GetRemark(comSettings.PortSetting);
                        item.Hide = SerialPortEx.GetHide(comSettings.PortSetting);
                    } catch (Exception ex) {
                        Logger.Error(ex);
                        continue;
                    }

                }
            }
        }
    }
}
