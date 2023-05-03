using SuperCom.Config;
using SuperCom.WatchDog;
using SuperControls.Style.Windows;
using SuperUtils.Framework.Logger;
using SuperUtils.IO;
using System;
using System.Windows;

namespace SuperCom
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    /// 

    public partial class App : Application
    {
        public static AbstractLogger Logger = Log.Logger.Instance;
        private static MemoryDog MemoryDog { get; set; }

        public static Action OnMemoryDog;
        public static Action<long> OnMemoryChanged;

        static App()
        {
            MemoryDog = new MemoryDog();
            MemoryDog.OnNotFeed += () =>
            {
                OnMemoryDog?.Invoke();
            };
            MemoryDog.OnMemoryChanged += (memory) =>
            {
                OnMemoryChanged?.Invoke(memory);
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

#if DEBUG

#else
            // UI线程未捕获异常处理事件
            this.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(Window_ErrorMsg.App_DispatcherUnhandledException);

            // Task线程内未捕获异常处理事件
            TaskScheduler.UnobservedTaskException += Window_ErrorMsg.TaskScheduler_UnobservedTaskException;

            // 非UI线程未捕获异常处理事件
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(Window_ErrorMsg.CurrentDomain_UnhandledException);
            Logger.Info("异常后处理启动");
#endif
            // 看门狗
            MemoryDog.Watch();
            base.OnStartup(e);
        }




    }
}
