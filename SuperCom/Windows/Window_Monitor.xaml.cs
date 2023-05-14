using Newtonsoft.Json.Linq;
using SuperCom.Config;
using SuperCom.Entity;
using SuperControls.Style;
using SuperUtils.Common;
using SuperUtils.Framework.ORM.Mapper;
using SuperUtils.Framework.ORM.Wrapper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using static SuperCom.Config.MapperManager;

namespace SuperCom
{
    /// <summary>
    /// Interaction logic for Window_Monitor.xaml
    /// </summary>
    public partial class Window_Monitor : BaseWindow
    {

        public string CurrentPortName { get; set; }

        private ObservableCollection<string> _CurrentPortNames;
        public ObservableCollection<string> CurrentPortNames
        {
            get { return _CurrentPortNames; }
            set
            {
                _CurrentPortNames = value;
                RaisePropertyChanged();
            }
        }
        private int _SideIndex = (int)ConfigManager.VarMonitorSetting.SideIndex;
        public int SideIndex
        {
            get { return _SideIndex; }
            set
            {
                _SideIndex = value;
                RaisePropertyChanged();
                SetCurrentPortName();
            }
        }

        private ObservableCollection<VarMonitor> _CurrentVarMonitors;
        public ObservableCollection<VarMonitor> CurrentVarMonitors
        {
            get { return _CurrentVarMonitors; }
            set
            {
                _CurrentVarMonitors = value;
                RaisePropertyChanged();
            }
        }

        private MainWindow MainWindow { get; set; }

        public Window_Monitor()
        {
            InitializeComponent();
            this.DataContext = this;
            Init();
        }

        public void Init()
        {
            CurrentPortNames = new ObservableCollection<string>();
            string[] ports = SerialPortEx.GetAllPorts();

            if (ports != null && ports.Length > 0)
            {
                foreach (var item in ports)
                    CurrentPortNames.Add(item);
            }


        }

        public void SetCurrentPortName()
        {
            if (CurrentPortNames != null && SideIndex >= 0 && SideIndex < CurrentPortNames.Count)
                CurrentPortName = CurrentPortNames[SideIndex];
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox listBox = sender as ListBox;
            if (!listBox.IsLoaded)
                return;
            ConfigManager.VarMonitorSetting.SideIndex = SideIndex;
            ConfigManager.VarMonitorSetting.Save();
            LoadByName(CurrentPortName);
        }

        public void LoadByName(string portName)
        {
            CurrentVarMonitors = new ObservableCollection<VarMonitor>();

            if (string.IsNullOrEmpty(portName))
                return;
            SelectWrapper<VarMonitor> wrapper = new SelectWrapper<VarMonitor>();
            wrapper.Eq("PortName", portName);
            List<VarMonitor> varMonitors = MonitorMapper.SelectList(wrapper);
            if (varMonitors == null || varMonitors.Count == 0)
                return;

            foreach (var item in varMonitors)
                CurrentVarMonitors.Add(item);
        }

        private void AddNewVarMonitor(object sender, RoutedEventArgs e)
        {
            // 新增监视变量
            NewVarMonitor(CurrentPortName);
        }



        private void SaveVarMonitor(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                string name = button.Tag.ToString();
                SaveMonitor(name);
            }
        }

        private void DeleteVarMonitory(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag != null &&
                (element.Parent as FrameworkElement).Tag != null &&
                (element.Parent as FrameworkElement).Tag is System.Windows.Controls.DataGrid dataGrid &&
                dataGrid.Tag != null)
            {
                if (long.TryParse(element.Tag.ToString(), out long id))
                    DeleteVarMonitor(id);
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
                //string name = button.Tag.ToString();
                //PortTabItem portTabItem = vieModel.PortTabItems.FirstOrDefault(arg => arg.Name.Equals(name));
                //if (portTabItem != null)
                //{
                //    portTabItem.VarMonitors = new System.Collections.ObjectModel.ObservableCollection<VarMonitor>();
                //    foreach (var item in vieModel.GetVarMonitorByPortName(name))
                //    {
                //        portTabItem.VarMonitors.Add(item);
                //    }
                //}
            }

        }

        private void DrawMonitorPicture(object sender, RoutedEventArgs e)
        {

        }


        #region "变量监视器"
        public List<VarMonitor> GetVarMonitorByPortName(string portName)
        {
            if (MonitorMapper == null)
                MonitorMapper = new SqliteMapper<VarMonitor>(ConfigManager.SQLITE_DATA_PATH);
            SelectWrapper<VarMonitor> wrapper = new SelectWrapper<VarMonitor>();
            wrapper.Eq("PortName", portName);
            List<VarMonitor> varMonitors = MonitorMapper.SelectList(wrapper);
            return varMonitors.OrderBy(arg => arg.SortOrder).ToList();
        }

        public void NewVarMonitor(string portName)
        {
            if (string.IsNullOrEmpty(portName))
                return;

            if (CurrentVarMonitors == null)
                CurrentVarMonitors = new ObservableCollection<VarMonitor>();

            int maxOrder = 0;
            if (CurrentVarMonitors.Count > 0)
                maxOrder = CurrentVarMonitors.Max(arg => arg.SortOrder);
            if (maxOrder <= 0)
                maxOrder = 1;
            else
                maxOrder++;
            VarMonitor varMonitor = new VarMonitor(maxOrder, portName);
            MonitorMapper.InsertAndGetID(varMonitor);
            CurrentVarMonitors.Add(varMonitor);
        }
        public void DeleteVarMonitor(long id)
        {
            if (CurrentVarMonitors == null || CurrentVarMonitors.Count == 0 || id <= 0)
                return;
            MonitorMapper.DeleteById(id);

            int idx = -1;

            for (int i = 0; i < CurrentVarMonitors.Count; i++)
            {
                if (CurrentVarMonitors[i].MonitorID == id)
                {
                    idx = i;
                    break;
                }
            }
            if (idx >= 0 && idx < CurrentVarMonitors.Count)
                CurrentVarMonitors.RemoveAt(idx);
        }

        public void SaveMonitor(string portName)
        {
            if (string.IsNullOrEmpty(portName))
                return;

            List<VarMonitor> toUpdate = new List<VarMonitor>();

            List<VarMonitor> allData = MonitorMapper.SelectList();
            foreach (var item in CurrentVarMonitors)
            {
                VarMonitor varMonitor = allData.FirstOrDefault(arg => arg.MonitorID == item.MonitorID);
                if (varMonitor == null || !varMonitor.Equals(item))
                    toUpdate.Add(item);
            }


            if (toUpdate.Count == 0)
            {
                MessageNotify.Info("无改变，无需保存");
                return;
            }
            else
            {
                foreach (var item in toUpdate)
                    MonitorMapper.UpdateById(item);

                MessageNotify.Success("保存成功");
                CurrentVarMonitors = new ObservableCollection<VarMonitor>();

                foreach (var item in GetVarMonitorByPortName(CurrentPortName))
                    CurrentVarMonitors.Add(item);

            }

        }

        #endregion

        private void BaseWindow_ContentRendered(object sender, EventArgs e)
        {
            SetCurrentPortName();
            LoadByName(CurrentPortName);
        }

        private void Save(object sender, RoutedEventArgs e)
        {
            Apply(null, null);
            this.Close();
        }

        private void Apply(object sender, RoutedEventArgs e)
        {
            SaveMonitor(CurrentPortName);
        }
    }
}