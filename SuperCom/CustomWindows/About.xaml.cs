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
using SuperUtils.Common;
using SuperUtils.NetWork;

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

            // 读取本地配置
            string configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "publishconfig");
            string value = FileHelper.TryReadFile(configPath);
            Dictionary<string, string> dict = JsonUtils.TryDeserializeObject<Dictionary<string, string>>(value);
            if (dict != null)
            {
                if (dict.ContainsKey("author") && dict["author"] is string author)
                    authorText.Text = $"By {author}";
                if (dict.ContainsKey("url") && dict["url"] is string url)
                    hyperLink.NavigateUri = NetUtils.TryGetUri(url);
                if (dict.ContainsKey("urltext") && dict["urltext"] is string urltext)
                {
                    runText.Text = urltext;
                    if (urltext.ToLower().IndexOf("github") < 0)
                        githubImage.Visibility = Visibility.Collapsed;
                }
            }
            string local = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            local = local.Substring(0, local.Length - ".0.0".Length);
            VersionTextBlock.Text = $"版本：{local}";
        }

        private void OpenUrl(object sender, RoutedEventArgs e)
        {
            Hyperlink hyperlink = sender as Hyperlink;
            FileHelper.TryOpenUrl(hyperlink.NavigateUri?.ToString(), (err) =>
            {
                MessageCard.Error(err);
            });
        }
    }
}
