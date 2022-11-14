using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace SuperCom
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            // UI线程未捕获异常处理事件
#if DEBUG

#else
            this.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(App_DispatcherUnhandledException);

            // Task线程内未捕获异常处理事件
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            // 非UI线程未捕获异常处理事件
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            
#endif
            base.OnStartup(e);
        }

        void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                StringBuilder builder = new StringBuilder();
                builder.Append("[DispatcherUnhandledException] SuperCom 出现了一些问题，将退出");
                MessageBox.Show(builder.ToString(), "SuperCom 异常");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                App.Current.Shutdown();
                Environment.Exit(0);
            }
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {

            try
            {
                StringBuilder builder = new StringBuilder();
                if (e.IsTerminating)
                {
                    builder.Append("[CurrentDomain_UnhandledException] SuperCom 出现了一些问题，将退出");
                    MessageBox.Show(builder.ToString(), "SuperCom 异常");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                App.Current.Shutdown();
                Environment.Exit(0);
            }
        }

        void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            // task线程内未处理捕获
            Console.WriteLine(e.Exception.StackTrace);
            Console.WriteLine(e.Exception.Message);
            e.SetObserved(); // 设置该异常已察觉（这样处理后就不会引起程序崩溃）
        }
    }
}
