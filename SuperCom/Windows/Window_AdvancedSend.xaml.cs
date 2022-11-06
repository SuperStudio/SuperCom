using SuperCom.Entity;
using SuperCom.ViewModel;
using SuperControls.Style;
using SuperUtils.Common;
using System;
using System.Collections.Generic;
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
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SuperCom.Windows
{
    /// <summary>
    /// Interaction logic for Window_AdvancedSend.xaml
    /// </summary>
    public partial class Window_AdvancedSend : BaseWindow
    {
        public VieModel_AdvancedSend vieModel { get; set; }

        public Window_AdvancedSend()
        {
            InitializeComponent();
            vieModel = new VieModel_AdvancedSend();
            this.DataContext = vieModel;
            dataGrid.ItemsSource = vieModel.SendCommands;
        }




        private void BaseWindow_ContentRendered(object sender, EventArgs e)
        {


        }

        private void AddNewProject(object sender, MouseButtonEventArgs e)
        {
            // 保存
            vieModel.AddProject("我的项目");
            vieModel.SaveProjects();
            DataChanged();
        }

        private void DeleteProject(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button.Tag != null)
            {
                int.TryParse(button.Tag.ToString(), out int projectID);
                DeleteProjectByID(projectID);

            }
        }

        private void DeleteProjectByID(int projectID)
        {
            int idx = -1;
            for (int i = 0; i < vieModel.Projects.Count; i++)
            {
                if (vieModel.Projects[i].ProjectID == projectID)
                {
                    idx = i;
                    break;
                }
            }
            if (idx >= 0 && idx < vieModel.Projects.Count)
            {
                vieModel.DeleteProject(vieModel.Projects[idx]);
                vieModel.Projects.RemoveAt(idx);
                DataChanged();
            }

        }

        private void DeleteProjectInMenuItem(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem.Tag != null)
            {
                int.TryParse(menuItem.Tag.ToString(), out int projectID);
                DeleteProjectByID(projectID);
            }
        }

        public void DataChanged()
        {
            // 通知 mainWindow 更新
            MainWindow window = GetWindowByName("mainWindow") as MainWindow;
            window?.RefreshSendCommands();
            CurrentSendCommands = vieModel.SendCommands.ToList();
        }

        private void OnProjectClick(object sender, MouseButtonEventArgs e)
        {
            Grid grid = sender as Grid;
            if (grid != null && grid.Tag != null)
            {
                string projectID = grid.Tag.ToString();
                if (!string.IsNullOrEmpty(projectID))
                {
                    AdvancedSend advancedSend = vieModel.Projects.Where(arg => arg.ProjectID.ToString().Equals(projectID)).FirstOrDefault();
                    if (advancedSend != null)
                    {
                        vieModel.SendCommands = new System.Collections.ObjectModel.ObservableCollection<SendCommand>();
                        List<SendCommand> sendCommands = new List<SendCommand>();
                        if (!string.IsNullOrEmpty(advancedSend.Commands))
                            sendCommands = JsonUtils.TryDeserializeObject<List<SendCommand>>(advancedSend.Commands);
                        if (sendCommands.Count > 0)
                        {
                            foreach (var item in sendCommands)
                            {
                                vieModel.SendCommands.Add(item);
                            }

                        }
                        vieModel.CurrentProjectID = advancedSend.ProjectID;
                        vieModel.ShowCurrentSendCommand = true;
                        dataGrid.ItemsSource = null;
                        dataGrid.ItemsSource = vieModel.SendCommands;
                    }
                }
            }
        }

        private void AddNewSendCommand(object sender, RoutedEventArgs e)
        {
            if (vieModel.SendCommands == null) vieModel.SendCommands = new System.Collections.ObjectModel.ObservableCollection<SendCommand>();
            SendCommand send = new SendCommand();
            send.CommandID = SendCommand.GenerateID(vieModel.SendCommands.Select(arg => arg.CommandID).ToList());
            send.Delay = SendCommand.DEFAULT_DELAY;
            if (vieModel.SendCommands.Count > 0)
                send.Order = vieModel.SendCommands.Select(arg => arg.Order).Max() + 1;
            else
                send.Order = 1;
            send.Command = "";
            AdvancedSend advancedSend = vieModel.Projects.Where(arg => arg.ProjectID == vieModel.CurrentProjectID).FirstOrDefault();
            if (advancedSend != null)
            {
                if (advancedSend.CommandList == null)
                    advancedSend.CommandList = new List<SendCommand>();
                if (!string.IsNullOrEmpty(advancedSend.Commands))
                {
                    advancedSend.CommandList = JsonUtils.TryDeserializeObject<List<SendCommand>>(advancedSend.Commands);
                }


                advancedSend.CommandList.Add(send);
                advancedSend.Commands = JsonUtils.TrySerializeObject(advancedSend.CommandList);
                vieModel.UpdateProject(advancedSend);
                vieModel.SendCommands.Add(send);
                DataChanged();

            }

        }

        private void DeleteCommand(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null && button.Tag != null)
            {
                string commandID = button.Tag.ToString();
                SendCommand sendCommand = vieModel.SendCommands.Where(arg => arg.CommandID.ToString().Equals(commandID)).FirstOrDefault();
                if (sendCommand != null)
                {
                    vieModel.SendCommands.Remove(sendCommand);
                    AdvancedSend advancedSend = vieModel.Projects.Where(arg => arg.ProjectID == vieModel.CurrentProjectID).FirstOrDefault();
                    if (advancedSend != null)
                    {
                        if (!string.IsNullOrEmpty(advancedSend.Commands))
                        {
                            advancedSend.CommandList = JsonUtils.TryDeserializeObject<List<SendCommand>>(advancedSend.Commands);
                            advancedSend.CommandList.RemoveAll(arg => arg.CommandID == sendCommand.CommandID);
                            advancedSend.Commands = JsonUtils.TrySerializeObject(advancedSend.CommandList);
                            vieModel.UpdateProject(advancedSend);
                            DataChanged();
                        }
                    }
                }
            }
        }

        private Window GetWindowByName(string name)
        {
            foreach (Window item in App.Current.Windows)
            {
                if (item.Name.Equals(name)) return item;
            }
            return null;
        }

        private void SaveSendCommands(object sender, RoutedEventArgs e)
        {
            string commands = JsonUtils.TrySerializeObject(vieModel.SendCommands.ToList());
            AdvancedSend advancedSend = vieModel.Projects.Where(arg => arg.ProjectID == vieModel.CurrentProjectID).FirstOrDefault();
            if (advancedSend != null && !string.IsNullOrEmpty(commands))
            {
                if (!commands.Equals(advancedSend.Commands))
                {
                    advancedSend.Commands = commands;
                    vieModel.UpdateProject(advancedSend);
                    Console.WriteLine("保存项目");
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
            if (textBox != null)
            {
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
            if (textBox.Tag != null)
            {
                string projectID = textBox.Tag.ToString();
                if (!string.IsNullOrEmpty(projectID))
                {
                    AdvancedSend advancedSend = vieModel.Projects.Where(arg => arg.ProjectID.ToString().Equals(projectID)).FirstOrDefault();
                    if (string.IsNullOrEmpty(newName))
                    {
                        textBox.Text = advancedSend.ProjectName;
                        return;
                    }

                    if (advancedSend != null && !advancedSend.ProjectName.Equals(newName))
                    {
                        advancedSend.ProjectName = newName;
                        vieModel.RenameProject(advancedSend);
                        DataChanged();
                    }
                }
            }



        }

        private void TextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Escape)
            {
                RenameTextBoxLostFocus(sender, e);
            }
        }


        private List<SendCommand> CurrentSendCommands { get; set; }

        private void StartCommands(object sender, RoutedEventArgs e)
        {
            foreach (var item in vieModel.SendCommands)
            {
                item.Status = RunningStatus.Waiting;
            }
            CurrentSendCommands = vieModel.SendCommands.ToList();
            string portName = comboBox.Text;
            vieModel.RunningCommands = true;
            Task.Run(async () =>
            {
                int idx = 0;
                while (vieModel.RunningCommands)
                {
                    if (idx >= CurrentSendCommands.Count)
                    {
                        idx = 0;
                        CurrentSendCommands = vieModel.SendCommands.ToList();
                        await Task.Delay(500);
                        continue;
                    }
                    SendCommand command = CurrentSendCommands[idx];
                    if (idx < vieModel.SendCommands.Count)
                        vieModel.SendCommands[idx].Status = RunningStatus.Running;
                    Console.WriteLine($"暂停 {command.Delay} ms");
                    await Task.Delay(command.Delay);
                    await Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)delegate
                  {
                      SideComPort serialComPort = vieModel.Main.vieModel.SideComPorts.Where(arg => arg.Name.Equals(portName)).FirstOrDefault();
                      if (serialComPort == null || serialComPort.PortTabItem == null || serialComPort.PortTabItem.SerialPort == null)
                      {
                          MessageCard.Error($"连接串口 {portName} 失败！");
                          vieModel.RunningCommands = false;
                          return;
                      }
                      SerialPort port = serialComPort.PortTabItem.SerialPort;
                      PortTabItem portTabItem = vieModel.Main.vieModel.PortTabItems.Where(arg => arg.Name.Equals(portName)).FirstOrDefault();
                      string value = command.Command;
                      if (port != null)
                      {
                          bool success = vieModel.Main.SendCommand(port, portTabItem, value);
                          if (!success)
                          {
                              vieModel.RunningCommands = false;
                              return;
                          }
                      }
                      if (idx < vieModel.SendCommands.Count)
                          vieModel.SendCommands[idx].Status = RunningStatus.Success;
                      idx++;
                      if (idx >= CurrentSendCommands.Count) idx = 0;
                  });


                }
            });
        }

        private void StopCommands(object sender, RoutedEventArgs e)
        {
            vieModel.RunningCommands = false;
            foreach (var item in vieModel.SendCommands)
            {
                item.Status = RunningStatus.Waiting;
            }
        }

        private void ShowEditPopup(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Grid grid = button.Parent as Grid;
            Popup popup = grid.Children.OfType<Popup>().FirstOrDefault();
            if (popup != null)
            {
                popup.IsOpen = true;
            }
        }

        private void BaseWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            vieModel.RunningCommands = false;
        }
    }
}
