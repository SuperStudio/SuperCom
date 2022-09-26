using SuperCom.Entity;
using SuperCom.ViewModel;
using SuperControls.Style;
using SuperUtils.Common;
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


            }
            // 通知 mainWindow 更新
            MainWindow window = GetWindowByName("mainWindow") as MainWindow;
            window?.RefreshSendCommands();
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
                    // 通知 mainWindow 更新
                    MainWindow window = GetWindowByName("mainWindow") as MainWindow;
                    window?.RefreshSendCommands();

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
            }
        }

        private void RenameTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            textBox.Visibility = Visibility.Hidden;
        }
    }
}
