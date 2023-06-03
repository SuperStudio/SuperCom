using SuperCom.Config;
using SuperCom.WatchDog;
using SuperControls.Style.Windows;
using SuperUtils.Framework.Logger;
using SuperUtils.IO;
using SuperUtils.Systems;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace SuperCom
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    /// 

    public partial class App : Application
    {
        public static AbstractLogger Logger { get; set; } = Log.Logger.Instance;
        private static MemoryDog MemoryDog { get; set; }

        public static Action OnMemoryDog;
        public static Action<long> OnMemoryChanged;
        public static Action<double> OnCpuUsageChanged;

        static App()
        {

            SuperUtils.Handler.LogHandler.Logger = Logger;
            SuperControls.Style.Handler.LogHandler.Logger = Logger;

            MemoryDog = new MemoryDog();
            MemoryDog.OnNotFeed += () =>
            {
                OnMemoryDog?.Invoke();
            };
            MemoryDog.OnMemoryChanged += (memory) =>
            {
                OnMemoryChanged?.Invoke(memory);
            };

            MemoryDog.OnCpuUsageChanged += (cpu) =>
            {
                OnCpuUsageChanged?.Invoke(cpu);
            };

            Window_ErrorMsg.OnFeedBack += () =>
            {
                FileHelper.TryOpenUrl(UrlManager.FeedbackUrl);
            };
            Window_ErrorMsg.OnLog += (str) =>
            {
                Logger.Error(str);
            };
            Logger.Info("app init");
        }

        protected override void OnStartup(StartupEventArgs e)
        {

            // UI线程未捕获异常处理事件
            this.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(Window_ErrorMsg.App_DispatcherUnhandledException);

            // Task线程内未捕获异常处理事件
            TaskScheduler.UnobservedTaskException += Window_ErrorMsg.TaskScheduler_UnobservedTaskException;

            // 非UI线程未捕获异常处理事件
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(Window_ErrorMsg.CurrentDomain_UnhandledException);
            Logger.Info("exception handler init");

            // 看门狗
            MemoryDog.Watch();
            base.OnStartup(e);
        }

        public App()
        {

        }




        protected override void OnExit(ExitEventArgs e)
        {
            Win32Helper.CancelPreventSleep(); // 取消系统休眠
            base.OnExit(e);
        }


    }
}
