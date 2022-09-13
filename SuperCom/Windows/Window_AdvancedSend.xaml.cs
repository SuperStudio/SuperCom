using SuperCom.Entity;
using SuperCom.ViewModel;
using SuperControls.Style;
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
            vieModel.ProjectNames.Add(new AdvancedSend(vieModel.ProjectNames.Count, $"我的项目"));
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
            for (int i = 0; i < vieModel.ProjectNames.Count; i++)
            {
                if (vieModel.ProjectNames[i].ProjectID == projectID)
                {
                    idx = i;
                    break;
                }
            }
            vieModel.ProjectNames.RemoveAt(idx);
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
    }
}
