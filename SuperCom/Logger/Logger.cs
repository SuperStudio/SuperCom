using SuperUtils.Framework.Logger;
using SuperUtils.IO;
using SuperUtils.Time;
using System;
using System.IO;

namespace SuperCom.Log
{
    public class Logger : AbstractLogger
    {
        public static string LOG_DIR { get; set; } =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_logs");

        public static Logger Instance { get; private set; }


        /// <summary>
        /// 防止递归
        /// </summary>
        private bool Writing { get; set; } = false;

        private Logger()
        {
        }

        static Logger()
        {
            Instance = new Logger();
#if DEBUG
            Instance.LogLevel = Level.Debug;
#else
            Instance.LogLevel = Level.Info;
#endif
        }


        public override void LogPrint(string str)
        {
            if (Writing)
                return;
            Writing = true;
            Console.Write(str);
            if (!Directory.Exists(LOG_DIR))
                DirHelper.TryCreateDirectory(LOG_DIR);
            string filePath = Path.Combine(LOG_DIR, DateHelper.NowDate() + ".log");
            FileHelper.TryAppendToFile(filePath, str);
            Writing = false;
        }
    }
}
