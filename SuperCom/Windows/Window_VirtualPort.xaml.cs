using SuperCom.Config;
using SuperCom.Entity;
using SuperControls.Style;
using SuperControls.Style.Windows;
using SuperUtils.IO;
using SuperUtils.Windows.WindowRegistry;
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
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SuperCom.Windows
{
    /// <summary>
    /// Interaction logic for Window_VirtualPort.xaml
    /// </summary>
    public partial class Window_VirtualPort : BaseWindow, INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] string name = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public Window_VirtualPort()
        {
            InitializeComponent();
            this.DataContext = this;
        }



        private bool _IsCom0ConInstalled = false;
        public bool IsCom0ConInstalled
        {
            get { return _IsCom0ConInstalled; }
            set
            {
                _IsCom0ConInstalled = value;
                RaisePropertyChanged();
            }
        }
        private bool _IsCom0ConExeExists = false;
        public bool IsCom0ConExeExists
        {
            get { return _IsCom0ConExeExists; }
            set
            {
                _IsCom0ConExeExists = value;
                RaisePropertyChanged();
            }
        }
        private bool _Saving = false;
        public bool Saving
        {
            get { return _Saving; }
            set
            {
                _Saving = value;
                RaisePropertyChanged();
            }
        }
        private bool _AddingPort = false;
        public bool AddingPort
        {
            get { return _AddingPort; }
            set
            {
                _AddingPort = value;
                RaisePropertyChanged();
            }
        }
        private bool _DeletingPort = false;
        public bool DeletingPort
        {
            get { return _DeletingPort; }
            set
            {
                _DeletingPort = value;
                RaisePropertyChanged();
            }
        }
        private bool _ListingPort = false;
        public bool ListingPort
        {
            get { return _ListingPort; }
            set
            {
                _ListingPort = value;
                RaisePropertyChanged();
            }
        }
        private string _Com0ConInstalledPath = ConfigManager.VirtualPortSettings.Com0ConInstalledPath;
        public string Com0ConInstalledPath
        {
            get { return _Com0ConInstalledPath; }
            set
            {
                _Com0ConInstalledPath = value;
                RaisePropertyChanged();
                ConfigManager.VirtualPortSettings.Com0ConInstalledPath = value;
                ConfigManager.VirtualPortSettings.Save();
            }
        }
        private ObservableCollection<VirtualPort> _CurrentVirtualPorts;
        public ObservableCollection<VirtualPort> CurrentVirtualPorts
        {
            get { return _CurrentVirtualPorts; }
            set
            {
                _CurrentVirtualPorts = value;
                RaisePropertyChanged();
            }
        }

        public async void Init()
        {
            CurrentVirtualPorts = new ObservableCollection<VirtualPort>();
            InstalledApp app = RegistryHelper.GetInstalledApp(VirtualPortManager.COM_0_COM_PROGRAM_NAME);
            if (app != null)
            {
                IsCom0ConInstalled = true;
                string path = System.IO.Path.Combine(app.InstallLocation,
                    VirtualPortManager.COM_0_COM_PROGRAM_EXE_NAME);
                if (!File.Exists(Com0ConInstalledPath) && File.Exists(path))
                    Com0ConInstalledPath = path;
                ListingPort = true;
                IsCom0ConExeExists = File.Exists(Com0ConInstalledPath);
                VirtualPortManager.Init(Com0ConInstalledPath);
                List<VirtualPort> virtualPorts = await VirtualPortManager.ListAllPort();
                foreach (var item in virtualPorts)
                {
                    CurrentVirtualPorts.Add(item);
                }
                await Task.Delay(100);
                ListingPort = false;
            }
        }



        private static string COM_0_COM_INSTALLED_PATH =
            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Installer", "setup.exe");
        private void InstallCom0Com(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(COM_0_COM_INSTALLED_PATH))
            {
                MessageCard.Error($"不存在：{COM_0_COM_INSTALLED_PATH}");
                return;
            }
            FileHelper.TryOpenFile(COM_0_COM_INSTALLED_PATH);
            bool success = (bool)new MsgBox(this, "安装完成后重新打开虚拟串口").ShowDialog();
            this.Close();
        }

        private void BaseWindow_ContentRendered(object sender, EventArgs e)
        {
            Init();
        }


        private void SelectPath(object sender, RoutedEventArgs e)
        {
            string filePath = FileHelper.SelectFile("", "setupc.exe|*.exe");
            if (File.Exists(filePath) && filePath.EndsWith("setupc.exe"))
            {
                Com0ConInstalledPath = filePath;
                MessageNotify.Success("设置成功");
                Init();
            }
            else
                MessageNotify.Error("必须是 setupc.exe");

        }

        private async void DeletePort(object sender, RoutedEventArgs e)
        {
            if (!(bool)new MsgBox(this, "确定删除该串口对？").ShowDialog())
                return;

            bool deleted = false;
            if (sender is Button button && button.Tag != null)
            {
                string id = button.Tag.ToString();
                if (string.IsNullOrEmpty(id))
                    return;
                id = id.Replace("CNCA", "").Replace("CNCB", "");
                int.TryParse(id, out int n);
                if (n >= 0)
                {
                    DeletingPort = true;
                    deleted = await VirtualPortManager.DeletePort(n);
                }
            }


            if (deleted && CurrentVirtualPorts != null && CurrentVirtualPorts.Count > 1)
            {
                int selectedIndex = dataGrid.SelectedIndex;
                int small = -1, large = -1;
                if (selectedIndex % 2 == 0)
                {
                    large = selectedIndex + 1;
                    small = selectedIndex;
                }
                else
                {
                    small = selectedIndex - 1;
                    large = selectedIndex;
                }
                if (small >= 0 && large < CurrentVirtualPorts.Count)
                {
                    CurrentVirtualPorts.RemoveAt(large);
                    CurrentVirtualPorts.RemoveAt(small);
                }
            }
            DeletingPort = false;
        }

        private void RefreshVirtualPort(object sender, RoutedEventArgs e)
        {
            Init();
        }



        private async void SaveChanges(object sender, RoutedEventArgs e)
        {
            // 检查是否输入

            foreach (var item in CurrentVirtualPorts)
            {
                if (string.IsNullOrEmpty(item.PortName))
                {
                    MessageNotify.Error("存在未填写的串口号");
                    return;
                }
                if (!VirtualPort.IsProperPortName(item.PortName))
                {
                    MessageNotify.Error("串口号填写错误");
                    return;
                }
                if (!VirtualPort.IsProperNumber(item))
                {
                    MessageNotify.Error("数值填写有误");
                    return;
                }
                item.PortName = item.PortName.ToUpper();
            }

            long count = CurrentVirtualPorts.Select(arg => arg.PortName).ToHashSet().Count();
            if (count != CurrentVirtualPorts.Count)
            {
                MessageNotify.Error("存在重复串口号");
                return;
            }

            List<VirtualPort> AllPorts = await VirtualPortManager.ListAllPort();
            List<VirtualPort> CurrentPorts = CurrentVirtualPorts.ToList();
            Saving = true;
            // 更新
            List<VirtualPort> toChange = new List<VirtualPort>();
            foreach (var item in CurrentPorts)
            {
                VirtualPort virtualPort = AllPorts.FirstOrDefault(arg => arg.PortName.Equals(item.PortName));
                if (!item.Equals(virtualPort))
                    toChange.Add(item);
            }
            if (toChange.Count == 0)
            {
                Saving = false;
                MessageNotify.Info("无改动项");
                return;
            }
            bool success = await VirtualPortManager.UpdatePorts(toChange);
            Saving = false;
            if (success)
            {
                MessageNotify.Success("成功");
                Init();
            }
            else
                MessageNotify.Error("失败");

        }

        private async void AddNewVirtualPort(object sender, RoutedEventArgs e)
        {
            string nameA = portNameA.Text;
            string nameB = portNameB.Text;

            if (!VirtualPort.IsProperPortName(nameA) ||
                !VirtualPort.IsProperPortName(nameB))
            {
                MessageNotify.Error("串口号填写错误");
                return;
            }
            nameA = nameA.ToUpper().Trim();
            nameB = nameB.ToUpper().Trim();

            // 检查是否存在相同的
            List<VirtualPort> virtualPorts = await VirtualPortManager.ListAllPort();
            List<string> list = virtualPorts.Select(arg => arg.PortName).ToList();
            if (list.Contains(nameA) || list.Contains(nameB))
            {
                MessageNotify.Error("已存在串口");
                return;
            }

            // 执行 cmd 命令
            VirtualPort portA = new VirtualPort(nameA);
            VirtualPort portB = new VirtualPort(nameB);
            AddingPort = true;
            bool success = await VirtualPortManager.InsertPort(portA, portB);
            if (!success)
            {
                MessageNotify.Error("添加失败");
                AddingPort = false;
                return;
            }
            Init();
            newVirtualPortGrid.Visibility = Visibility.Collapsed;
            AddingPort = false;
            MessageNotify.Success("添加成功！");
        }

        private void CloseNewVirtualPortGrid(object sender, RoutedEventArgs e)
        {
            newVirtualPortGrid.Visibility = Visibility.Collapsed;

        }

        private async void ShowNewVirtualPortGrid(object sender, RoutedEventArgs e)
        {
            newVirtualPortGrid.Visibility = Visibility.Visible;


            portNameA.Text = "COM";
            portNameB.Text = "COM";
            await Task.Delay(100);
            portNameA.SetFocus();
        }

        private void portNameA_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                portNameB.SetFocus();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                AddNewVirtualPort(null, null);
            }


        }

        private void portNameB_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                portNameA.SetFocus();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                AddNewVirtualPort(null, null);
            }


        }
    }
}
