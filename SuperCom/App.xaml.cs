using SuperCom.Log;
using SuperCom.WatchDog;
using SuperControls.Style.Windows;
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
    /// 

    public partial class App : Application
    {
        private static bool Error { get; set; }

        public static Logger Logger = Logger.Instance;
        private static MemoryDog memoryDog { get; set; }

        public static Action OnMemoryDog;
        public static Action<long> OnMemoryChanged;

        static App()
        {
            memoryDog = new MemoryDog();
            memoryDog.OnNotFeed += () =>
            {
                OnMemoryDog?.Invoke();
            };
            memoryDog.OnMemoryChanged += (memory) =>
            {
                OnMemoryChanged?.Invoke(memory);
            };
        }

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
            // 看门狗
            memoryDog.Watch();
            base.OnStartup(e);
        }

        void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                if (Error)
                    return;
                Error = true;
                Window_ErrorMsg window_ErrorMsg = new Window_ErrorMsg();
                string error = e.Exception.ToString();
                Logger.Error(error);
                window_ErrorMsg.SetError(error);
                window_ErrorMsg.ShowDialog();
                
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
                if (e.IsTerminating && !Error)
                {
                    Error = true;
                    Window_ErrorMsg window_ErrorMsg = new Window_ErrorMsg();
                    string error = e.ExceptionObject.ToString();
                    Logger.Error(error);
                    window_ErrorMsg.SetError(error);
                    window_ErrorMsg.ShowDialog();
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
            Logger.Error(e.Exception.StackTrace);
            Logger.Error(e.Exception.Message);
            e.SetObserved(); // 设置该异常已察觉（这样处理后就不会引起程序崩溃）
        }
    }
}
