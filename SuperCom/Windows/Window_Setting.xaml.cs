using SuperCom.Config;
using SuperCom.Config.WindowConfig;
using SuperCom.Entity;
using SuperCom.ViewModel;
using SuperControls.Style;
using SuperControls.Style.Windows;
using SuperControls.Style.XAML.CustomWindows;
using SuperUtils.Common;
using SuperUtils.IO;
using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Interaction logic for Window_Setting.xaml
    /// </summary>
    public partial class Window_Setting : BaseWindow
    {

        private MainWindow Main { get; set; }
        public VieModel_Setting vieModel { get; set; }
        public Window_Setting()
        {
            InitializeComponent();
            Init();
        }

        public void Init()
        {
            vieModel = new VieModel_Setting();
            DataContext = vieModel;
            foreach (Window item in App.Current.Windows)
            {
                if (item.Name.Equals("mainWindow"))
                {
                    Main = (MainWindow)item;
                    break;
                }
            }
        }

        private void AddNewBaudRate(object sender, MouseButtonEventArgs e)
        {
            InputWindow inputWindow = new InputWindow(this);
            if ((bool)inputWindow.ShowDialog())
            {
                string text = inputWindow.Text;
                bool success = int.TryParse(text, out int baudRate);
                if (success && baudRate > 0 && !vieModel.BaudRates.Contains(baudRate.ToString()))
                {
                    vieModel.BaudRates.Add(baudRate.ToString());
                    ConfigManager.Main.CustomBaudRates = JsonUtils.TrySerializeObject(vieModel.BaudRates);
                    ConfigManager.Main.Save();
                    Main.vieModel.LoadBaudRates();
                }
            }
        }

        private bool IsPortRunning()
        {
            if (Main != null && Main.vieModel.SideComPorts?.Count > 0)
            {
                foreach (var item in Main.vieModel.SideComPorts)
                {
                    if (item.Connected) return true;
                }
            }
            return false;
        }

        private void DeleteBaudRate(object sender, RoutedEventArgs e)
        {
            if (IsPortRunning())
            {
                MessageCard.Error("请关闭所有串口后再试");
                return;
            }
            if (vieModel.BaudRates?.Count <= 1)
            {
                MessageCard.Error("至少保留一个");
                return;
            }
            Button button = sender as Button;
            if (button != null && button.Tag != null)
            {
                string value = button.Tag.ToString();
                int idx = -1;
                for (int i = 0; i < vieModel.BaudRates.Count; i++)
                {
                    if (value.Equals(vieModel.BaudRates[i].ToString()))
                    {
                        idx = i;
                        break;
                    }
                }
                if (idx >= 0 && idx < vieModel.BaudRates.Count)
                {
                    vieModel.BaudRates.RemoveAt(idx);
                    ConfigManager.Main.CustomBaudRates = JsonUtils.TrySerializeObject(vieModel.BaudRates);
                    ConfigManager.Main.Save();
                    Main.vieModel.LoadBaudRates();
                }
            }
        }

        private void SaveSettings(object sender, RoutedEventArgs e)
        {
            ConfigManager.CommonSettings.FixedOnSearch = vieModel.FixedOnSearch;
            ConfigManager.CommonSettings.FixedOnSendCommand = vieModel.FixedOnSendCommand;
            ConfigManager.CommonSettings.ScrollOnSearchClosed = vieModel.ScrollOnSearchClosed;
            ConfigManager.CommonSettings.LogNameFormat = vieModel.LogNameFormat;
            ConfigManager.CommonSettings.Save();
            MessageCard.Success("保存成功");
        }

        private void RestoreSettings(object sender, RoutedEventArgs e)
        {
            if (IsPortRunning())
            {
                MessageCard.Error("请关闭所有串口后再试");
                return;
            }

            if (new MsgBox(this, "将删除所有自定义串口设置，是否继续？").ShowDialog() == false)
            {
                return;
            }

            vieModel.BaudRates = new System.Collections.ObjectModel.ObservableCollection<string>();
            foreach (var item in PortSetting.DEFAULT_BAUDRATES)
            {
                vieModel.BaudRates.Add(item.ToString());
            }
            ConfigManager.Main.CustomBaudRates = JsonUtils.TrySerializeObject(vieModel.BaudRates);
            ConfigManager.Main.Save();
            Main.vieModel.LoadBaudRates();

            vieModel.LogNameFormat = CommonSettings.DEFAULT_LOGNAMEFORMAT;
            vieModel.FixedOnSearch = true;
            vieModel.FixedOnSendCommand = false;
            vieModel.ScrollOnSearchClosed = true;
            MessageCard.Success("已恢复默认值");
        }


        private void searchBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            List<char> list = System.IO.Path.GetInvalidFileNameChars().ToList();
            foreach (var item in e.Text.ToCharArray())
            {
                if (list.Contains(item))
                {
                    MessageCard.Error("非法文件名：\\ / : * ? \" < > | ");
                    e.Handled = true;
                    break;
                }
            }
        }
    }
}
