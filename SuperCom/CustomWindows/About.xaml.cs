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
using SuperUtils.IO;

namespace SuperCom.CustomWindows
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : SuperControls.Style.BaseDialog
    {
        public About(Window owner) : base(owner, false)
        {
            InitializeComponent();
        }

        private void OpenUrl(object sender, RoutedEventArgs e)
        {
            Hyperlink hyperlink = sender as Hyperlink;
            FileHelper.TryOpenUrl(hyperlink.NavigateUri.ToString(), (err) =>
            {
                MessageCard.Error(err);
            });
        }
    }
}
