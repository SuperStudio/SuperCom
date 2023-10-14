using ICSharpCode.AvalonEdit;
using SuperCom.Entity;
using SuperControls.Style;
using SuperUtils.IO;
using SuperUtils.NetWork;
using SuperUtils.Time;
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

namespace SuperCom.Core.Telnet
{
    /// <summary>
    /// Window_TelnetServer.xaml 的交互逻辑
    /// </summary>
    public partial class Window_TelnetServer : BaseWindow
    {
        public Window_TelnetServer()
        {
            InitializeComponent();
            TelnetServerManager.SetLogFunc(Log);
        }


        private void Border_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) {
                Border border = sender as Border;
                TextEditor textEditor = border.Child as TextEditor;
                double fontSize = textEditor.FontSize;
                if (e.Delta > 0) {
                    fontSize++;
                } else {
                    fontSize--;
                }
                if (fontSize > PortSetting.MAX_FONTSIZE)
                    fontSize = PortSetting.MAX_FONTSIZE;
                if (fontSize < PortSetting.MIN_FONTSIZE)
                    fontSize = PortSetting.MIN_FONTSIZE;

                textEditor.FontSize = fontSize;
                e.Handled = true;
            }
        }

        private void textBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextEditor textEditor = sender as TextEditor;
            if (textEditor == null || textEditor.Parent == null)
                return;
            Border border = textEditor.Parent as Border;
            if (border == null)
                return;
            border.BorderBrush = (SolidColorBrush)Application.Current.Resources["Button.Selected.BorderBrush"];
        }

        private void textBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextEditor textEditor = sender as TextEditor;
            if (textEditor != null && textEditor.Parent is Border border)
                border.BorderBrush = Brushes.Transparent;
        }

        private void StopTelnet(object sender, RoutedEventArgs e)
        {
            TelnetServerManager.Stop();
        }

        private void SartTelnet(object sender, RoutedEventArgs e)
        {
            TelnetServerManager.Start(NetUtils.GetLocalIPAddress());
        }

        private void Log(string data)
        {
            if (string.IsNullOrEmpty(data))
                data = "";
            string value = $"[{DateHelper.Now()}] {data} {Environment.NewLine}";
            Dispatcher.Invoke(() => {
                textEditor.AppendText(value);
                textEditor.ScrollToEnd();
            });
        }

        private void SaveTelnetLog(object sender, RoutedEventArgs e)
        {
            string text = textEditor.Text;
            if (string.IsNullOrEmpty(text))
                return;

            string fileName = FileHelper.SaveFile(this, filter: "*.log|*.log");
            if (!string.IsNullOrEmpty(fileName)) {
                FileHelper.TryWriteToFile(fileName, text, Encoding.UTF8);
                FileHelper.TryOpenSelectPath(fileName);
            }
        }
    }
}
