using SuperCom.Config;
using SuperCom.Config.WindowConfig;
using SuperCom.Entity;
using SuperCom.Entity.Enums;
using SuperCom.ViewModel;
using SuperControls.Style;
using SuperControls.Style.Windows;
using SuperUtils.Common;
using SuperUtils.IO;
using SuperUtils.Time;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using static SuperCom.App;

namespace SuperCom
{
    /// <summary>
    /// Interaction logic for Window_AdvancedSend.xaml
    /// </summary>
    public partial class Window_AdvancedSend : BaseWindow
    {
        private const string DEFAULT_PROJECT_NAME = "我的项目";

        #region "属性"

        public VieModel_AdvancedSend vieModel { get; set; }
        public MainWindow Main { get; set; }

        public int SideSelectedIndex { get; set; }

        private List<SendCommand> CurrentSendCommands { get; set; }

        #endregion

        public Window_AdvancedSend()
        {
            InitializeComponent();
            vieModel = new VieModel_AdvancedSend();
            this.DataContext = vieModel;
            dataGrid.ItemsSource = vieModel.SendCommands;

            foreach (Window window in App.Current.Windows) {
                if (window.Name.Equals("mainWindow")) {
                    Main = (MainWindow)window;
                    break;
                }
            }

            vieModel.OnRunCommand += (running) => {
                if (running) {
                    this.Owner = Main;
                    if (ConfigManager.AdvancedSendSettings.WindowOpacity < AdvancedSendSettings.DEFAULT_WINDOW_OPACITY) {
                        ConfigManager.AdvancedSendSettings.WindowOpacity = AdvancedSendSettings.DEFAULT_WINDOW_OPACITY;
                        ConfigManager.AdvancedSendSettings.Save();
                    }
                    //this.Opacity = ConfigManager.AdvancedSendSettings.WindowOpacity;
                } else {
                    this.Owner = null;
                    //this.Opacity = 1;
                }
            };
        }


        private void BaseWindow_ContentRendered(object sender, EventArgs e)
        {

            if (Main != null && Main.vieModel != null && Main?.vieModel.HighlightingDefinitions?.Count > 0) {
                foreach (var item in Main?.vieModel.HighlightingDefinitions) {
                    if (item.Name.Equals("ComLog")) {
                        logTextBox.SyntaxHighlighting = item;
                        break;
                    }
                }



            }
            if (Main != null && Main.vieModel != null && vieModel.CurrentProjects?.Count > 0) {
                if (SideSelectedIndex < 0 || SideSelectedIndex > vieModel.CurrentProjects.Count)
                    SideSelectedIndex = 0;
                sideListBox.SelectedIndex = SideSelectedIndex;
            }
        }

        private void DeleteProject(object sender, RoutedEventArgs e)
        {
            FrameworkElement ele = sender as FrameworkElement;
            if (ele.Tag != null) {
                int.TryParse(ele.Tag.ToString(), out int projectID);
                DeleteProjectByID(projectID);

            }
        }

        private void DeleteProjectByID(int projectID)
        {
            if (!(bool)new MsgBox("确认删除？").ShowDialog(this))
                return;

            int idx = -1;
            for (int i = 0; i < vieModel.CurrentProjects.Count; i++) {
                if (vieModel.CurrentProjects[i].ProjectID == projectID) {
                    idx = i;
                    break;
                }
            }
            if (idx >= 0 && idx < vieModel.CurrentProjects.Count) {
                string projectName = vieModel.CurrentProjects[idx].ProjectName;
                vieModel.DeleteProject(vieModel.CurrentProjects[idx]);
                vieModel.CurrentProjects.RemoveAt(idx);
                vieModel.AllProjects.RemoveAll(arg => arg.ProjectID == projectID);
                DataChanged();
                Logger.Info($"delete project: {projectName}");

            }

        }

        private void DeleteProjectInMenuItem(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem.Tag != null) {
                int.TryParse(menuItem.Tag.ToString(), out int projectID);
                DeleteProjectByID(projectID);
            }
        }

        public void DataChanged()
        {
            vieModel.LoadAllProject();
            // 通知 mainWindow 更新
            MainWindow window = GetWindowByName("mainWindow") as MainWindow;
            window?.RefreshSendCommands();
            CurrentSendCommands = vieModel.SendCommands.OrderBy(arg => arg.Order).ToList();
            window?.SetComboboxStatus();
            Logger.Info("data changed");
        }



        public void ShowProjectById(string projectID)
        {
            if (!string.IsNullOrEmpty(projectID)) {
                AdvancedSend advancedSend = vieModel.CurrentProjects.Where(arg => arg.ProjectID.ToString().Equals(projectID)).FirstOrDefault();
                ShowProjectBySend(advancedSend);
            }
        }

        public void ShowProjectBySend(AdvancedSend advancedSend)
        {
            if (advancedSend == null)
                return;

            vieModel.SendCommands = new System.Collections.ObjectModel.ObservableCollection<SendCommand>();
            List<SendCommand> sendCommands = new List<SendCommand>();
            if (!string.IsNullOrEmpty(advancedSend.Commands))
                sendCommands = JsonUtils.TryDeserializeObject<List<SendCommand>>(advancedSend.Commands);
            if (sendCommands != null && sendCommands.Count > 0) {
                foreach (var item in sendCommands.OrderBy(arg => arg.Order)) {
                    vieModel.SendCommands.Add(item);
                }

            }
            vieModel.CurrentProjectID = advancedSend.ProjectID;
            vieModel.ShowCurrentSendCommand = true;
            dataGrid.ItemsSource = null;
            dataGrid.ItemsSource = vieModel.SendCommands;

            Logger.Info($"show project: {advancedSend.ProjectName}");
        }

        private void AddNewSendCommand(object sender, RoutedEventArgs e)
        {
            if (vieModel.SendCommands == null)
                vieModel.SendCommands = new System.Collections.ObjectModel.ObservableCollection<SendCommand>();
            SendCommand send = new SendCommand();
            send.CommandID = SendCommand.GenerateID(vieModel.SendCommands.Select(arg => arg.CommandID).ToList());
            send.Delay = SendCommand.DEFAULT_DELAY;
            if (vieModel.SendCommands.Count > 0)
                send.Order = vieModel.SendCommands.Select(arg => arg.Order).Max() + 1;
            else
                send.Order = 1;
            send.Command = "";
            AdvancedSend advancedSend = vieModel.CurrentProjects.Where(arg => arg.ProjectID == vieModel.CurrentProjectID).FirstOrDefault();
            if (advancedSend != null) {
                if (advancedSend.CommandList == null)
                    advancedSend.CommandList = new List<SendCommand>();
                if (!string.IsNullOrEmpty(advancedSend.Commands)) {
                    advancedSend.CommandList = JsonUtils.TryDeserializeObject<List<SendCommand>>(advancedSend.Commands);
                }


                advancedSend.CommandList.Add(send);
                advancedSend.Commands = JsonUtils.TrySerializeObject(advancedSend.CommandList);
                vieModel.UpdateProject(advancedSend);
                vieModel.SendCommands.Add(send);
                DataChanged();

                Logger.Info($"add new cmd, order: {send.Order}");
            }

        }

        private void DeleteCommand(object sender, RoutedEventArgs e)
        {
            FrameworkElement ele = sender as FrameworkElement;
            if (ele != null && ele.Tag != null) {
                string commandID = ele.Tag.ToString();
                SendCommand sendCommand = vieModel.SendCommands.Where(arg => arg.CommandID.ToString().Equals(commandID)).FirstOrDefault();
                if (sendCommand != null) {
                    vieModel.SendCommands.Remove(sendCommand);
                    AdvancedSend advancedSend = vieModel.CurrentProjects.Where(arg => arg.ProjectID == vieModel.CurrentProjectID).FirstOrDefault();
                    if (advancedSend != null) {
                        if (!string.IsNullOrEmpty(advancedSend.Commands)) {
                            advancedSend.CommandList = JsonUtils.TryDeserializeObject<List<SendCommand>>(advancedSend.Commands);
                            advancedSend.CommandList.RemoveAll(arg => arg.CommandID == sendCommand.CommandID);
                            advancedSend.Commands = JsonUtils.TrySerializeObject(advancedSend.CommandList);
                            vieModel.UpdateProject(advancedSend);
                            DataChanged();

                            Logger.Info($"delete cmd, id: {commandID}");
                        }
                    }
                }
            }
        }

        private Window GetWindowByName(string name)
        {
            foreach (Window item in App.Current.Windows) {
                if (item.Name.Equals(name))
                    return item;
            }
            return null;
        }

        private void SaveSendCommands(object sender, RoutedEventArgs e)
        {
            string commands = JsonUtils.TrySerializeObject(vieModel.SendCommands.ToList());
            AdvancedSend advancedSend = vieModel.CurrentProjects.Where(arg => arg.ProjectID == vieModel.CurrentProjectID).FirstOrDefault();
            if (advancedSend != null && !string.IsNullOrEmpty(commands)) {
                if (!commands.Equals(advancedSend.Commands)) {
                    advancedSend.Commands = commands;
                    vieModel.UpdateProject(advancedSend);
                    DataChanged();
                }
            }
        }

        private void RenameProject(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            ContextMenu contextMenu = menuItem.Parent as ContextMenu;
            Grid grid = contextMenu.PlacementTarget as Grid;
            TextBox textBox = grid.Children.OfType<TextBox>().FirstOrDefault();
            if (textBox != null) {
                textBox.Visibility = Visibility.Visible;
                textBox.SelectAll();
                textBox.Focus();
                DataChanged();


            }
        }

        private void RenameTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;

            textBox.Visibility = Visibility.Hidden;
            string newName = textBox.Text;



            if (textBox.Tag != null) {
                string projectID = textBox.Tag.ToString();
                if (!string.IsNullOrEmpty(projectID)) {
                    AdvancedSend advancedSend = vieModel.CurrentProjects.Where(arg => arg.ProjectID.ToString().Equals(projectID)).FirstOrDefault();
                    if (advancedSend == null)
                        return;
                    string oldName = advancedSend.ProjectName;
                    if (string.IsNullOrEmpty(newName)) {
                        textBox.Text = oldName;
                        return;
                    }

                    if (advancedSend != null && !oldName.Equals(newName)) {
                        advancedSend.ProjectName = newName;
                        vieModel.RenameProject(advancedSend);
                        TextBlock textBlock = (textBox.Parent as Grid).Children.OfType<TextBlock>().FirstOrDefault();
                        textBlock.Text = newName;
                        DataChanged();
                        if (!string.IsNullOrEmpty(projectSearchBox.Text)) {
                            if (newName.ToLower().IndexOf(projectSearchBox.Text) < 0)
                                RemoveProjectById(projectID);
                        }

                        Logger.Info($"rename project, before: {oldName}, after: {newName}");
                    }
                }
            }
        }

        private void RemoveProjectById(string id)
        {
            int idx = -1;
            for (int i = 0; i < vieModel.CurrentProjects.Count; i++) {
                if (vieModel.CurrentProjects[i].ProjectID.ToString().Equals(id)) {
                    idx = i;
                    break;
                }
            }

            if (idx >= 0 && idx < vieModel.CurrentProjects.Count)
                vieModel.CurrentProjects.RemoveAt(idx);
        }

        private void TextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Escape) {
                RenameTextBoxLostFocus(sender, e);
            }
        }

        private void StartCommands(object sender, RoutedEventArgs e)
        {
            if (vieModel.SideComPortSelected?.Count == 0)
                return;
            foreach (var item in vieModel.SendCommands) {
                item.Status = RunningStatus.WaitingToRun;
            }
            CurrentSendCommands = vieModel.SendCommands.OrderBy(arg => arg.Order).ToList();
            List<string> portNames = new List<string>();
            foreach (SideComPort key in vieModel.SideComPortSelected.Keys) {
                if (vieModel.SideComPortSelected[key])
                    portNames.Add(key.Name);
            }
            if (portNames.Count == 0) {
                LogToTextBox("未选择任何串口");
                return;
            }


            Logger.Info("start send cmds");

            vieModel.RunningCommands = true;
            Task.Run(async () => {
                int idx = 0;
                while (vieModel.RunningCommands) {

                    SendCommand command = CurrentSendCommands[idx];
                    if (idx < vieModel.SendCommands.Count)
                        vieModel.SendCommands[idx].Status = RunningStatus.Running;

                    foreach (var portName in portNames) {
                        bool success = await AsyncSendCommand(idx, portName, command);

                        Logger.Debug($"send cmd, port: {portName}, " +
                            $"name: {command.Name}, " +
                            $"order: {command.Order}, " +
                            $"delay: {command.Delay}, " +
                            $"cmd: {command.Command}");

                        if (!success)
                            continue;
                    }

                    vieModel.SendCommands[idx].Status = RunningStatus.WaitingDelay;
                    LogToTextBox($"等待 {command.Delay} ms");
                    int delay = 10;
                    for (int i = 1; i <= command.Delay; i += delay) {
                        if (!vieModel.RunningCommands)
                            break;
                        await Task.Delay(delay);
                        vieModel.SendCommands[idx].StatusText = $"{command.Delay - i} ms";
                    }
                    vieModel.SendCommands[idx].StatusText = $"0 ms";
                    vieModel.SendCommands[idx].Status = RunningStatus.WaitingToRun;
                    idx++;
                    if (idx >= CurrentSendCommands.Count) {
                        idx = 0;
                        CurrentSendCommands = vieModel.SendCommands.OrderBy(arg => arg.Order).ToList();
                        // 更新选择的串口
                        portNames.Clear();
                        foreach (SideComPort key in vieModel.SideComPortSelected.Keys) {
                            if (vieModel.SideComPortSelected[key])
                                portNames.Add(key.Name);
                        }
                        if (portNames.Count == 0) {
                            LogToTextBox("未选择任何串口");
                            vieModel.RunningCommands = false;
                            return;
                        }
                    }
                }
            });
        }

        public async Task<bool> AsyncSendCommand(int idx, string portName, SendCommand command)
        {
            bool success = false;
            await Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)delegate {
                SideComPort serialComPort = vieModel.Main.vieModel.SideComPorts.Where(arg => arg.Name.Equals(portName)).FirstOrDefault();
                if (serialComPort == null || serialComPort.PortTabItem == null || serialComPort.PortTabItem.SerialPort == null) {
                    LogToTextBox($"[E] 连接串口 {portName} 失败！");
                    success = false;
                    return;
                }
                SerialPort port = serialComPort.PortTabItem.SerialPort;
                PortTabItem portTabItem = vieModel.Main.vieModel.PortTabItems.Where(arg => arg.Name.Equals(portName)).FirstOrDefault();
                string value = command.Command;
                if (port != null) {
                    success = portTabItem.SendCommand(value);
                    if (!success) {
                        LogToTextBox($"[E] 向串口 {portName} 发送命令失败  {value} ");
                        success = false;
                        return;
                    }
                }
                if (idx < vieModel.SendCommands.Count)
                    vieModel.SendCommands[idx].Status = RunningStatus.AlreadySend;
                LogToTextBox($"[I] 向串口 {portName} 发送命令成功  {value} ");
                success = true;
            });
            return success;
        }


        public void LogToTextBox(string text)
        {
            Dispatcher.Invoke(() => {
                logTextBox.AppendText($"[{DateHelper.Now()}] {text}{Environment.NewLine}");
                // 保存到文件？
                logTextBox.ScrollToEnd();
            });
        }

        private void StopCommands(object sender, RoutedEventArgs e)
        {
            Logger.Info("stop all cmd");
            vieModel.RunningCommands = false;
            foreach (var item in vieModel.SendCommands) {
                item.Status = RunningStatus.WaitingToRun;
            }
        }

        private void ShowEditPopup(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Grid grid = button.Parent as Grid;
            Popup popup = grid.Children.OfType<Popup>().FirstOrDefault();
            if (popup != null) {
                popup.IsOpen = true;
            }
        }

        private void BaseWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            vieModel.RunningCommands = false;
            SaveConfigValue();
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems == null || e.AddedItems.Count == 0)
                return;
            AdvancedSend advancedSend = e.AddedItems[0] as AdvancedSend;
            ShowProjectBySend(advancedSend);
        }


        public void AdjustWindow()
        {

            if (ConfigManager.AdvancedSendSettings.FirstRun) {
                this.Width = SystemParameters.WorkArea.Width * 0.7;
                this.Height = SystemParameters.WorkArea.Height * 0.7;
                this.Left = SystemParameters.WorkArea.Width * 0.1;
                this.Top = SystemParameters.WorkArea.Height * 0.1;
                ConfigManager.AdvancedSendSettings.FirstRun = false;
            } else {
                if (ConfigManager.AdvancedSendSettings.Height == SystemParameters.WorkArea.Height && ConfigManager.AdvancedSendSettings.Width < SystemParameters.WorkArea.Width) {
                    //baseWindowState = 0;
                    this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    this.CanResize = true;
                } else {
                    this.Left = ConfigManager.AdvancedSendSettings.X;
                    this.Top = ConfigManager.AdvancedSendSettings.Y;
                    this.Width = ConfigManager.AdvancedSendSettings.Width;
                    this.Height = ConfigManager.AdvancedSendSettings.Height;
                }
            }
        }

        private void SaveConfigValue()
        {
            ConfigManager.AdvancedSendSettings.X = this.Left;
            ConfigManager.AdvancedSendSettings.Y = this.Top;
            ConfigManager.AdvancedSendSettings.Width = this.Width;
            ConfigManager.AdvancedSendSettings.Height = this.Height;
            //ConfigManager.AdvancedSendSettings.WindowState = (long)baseWindowState;
            ConfigManager.AdvancedSendSettings.Save();
        }


        private void CommandTextChanged(object sender, TextChangedEventArgs e)
        {
            //textChanged = true;
        }

        private void BaseWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //AdjustWindow();
            Logger.Info("advanced send loaded");
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //this.Opacity = (double)e.NewValue;
            //ConfigManager.AdvancedSendSettings.WindowOpacity = this.Opacity;
            //ConfigManager.AdvancedSendSettings.Save();
        }



        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (vieModel.SideComPortSelected?.Count == 0)
                return;
            CheckBox checkBox = sender as CheckBox;
            string portName = checkBox.Content.ToString();
            SetCheckedStatus(portName, true);
        }

        private void SetCheckedStatus(string portName, bool status)
        {
            if (!string.IsNullOrEmpty(portName)) {
                foreach (SideComPort key in vieModel.SideComPortSelected.Keys) {
                    if (key.Name.Equals(portName)) {
                        vieModel.SideComPortSelected[key] = status;
                        break;
                    }
                }
            }
            // 保存状态
            Dictionary<string, bool> dict = new Dictionary<string, bool>();
            foreach (SideComPort key in vieModel.SideComPortSelected.Keys) {
                dict.Add(key.Name, vieModel.SideComPortSelected[key]);
            }
            ConfigManager.AdvancedSendSettings.SelectedPortNamesJson = JsonUtils.TrySerializeObject(dict);
            ConfigManager.AdvancedSendSettings.Save();
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (vieModel.SideComPortSelected?.Count == 0)
                return;
            CheckBox checkBox = sender as CheckBox;
            string portName = checkBox.Content.ToString();
            SetCheckedStatus(portName, false);
        }

        private void HideLogGrid(object sender, RoutedEventArgs e)
        {
            showLogCheckBox.IsChecked = false;
        }

        private void ClearLogGrid(object sender, RoutedEventArgs e)
        {
            logTextBox.Clear();
        }

        private void CopyCommand(object sender, RoutedEventArgs e)
        {
            FrameworkElement ele = sender as FrameworkElement;
            Border border = (ele.Parent as Grid).Children.OfType<Border>().LastOrDefault();
            SearchBox searchBox = border.Child as SearchBox;
            if (!string.IsNullOrEmpty(searchBox.Text)) {
                bool v = ClipBoard.TrySetDataObject(searchBox.Text);
                if (v)
                    MessageNotify.Success("已复制");
            }

        }

        private void dataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void SearchProjectByName(object sender, RoutedEventArgs e)
        {
            SearchBox searchBox = sender as SearchBox;
            vieModel.SearchProject(searchBox.Text);

        }

        private void SaveChanges(object sender, RoutedEventArgs e)
        {

        }

        private void Save(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Apply(object sender, RoutedEventArgs e)
        {
            MessageNotify.Success("保存成功");
        }

        private void AddNewProject(object sender, RoutedEventArgs e)
        {
            // 保存
            vieModel.AddProject(DEFAULT_PROJECT_NAME);
            DataChanged();
        }
    }
}
