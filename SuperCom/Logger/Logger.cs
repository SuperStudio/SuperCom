using SuperUtils.Framework.Logger;
using SuperUtils.IO;
using SuperUtils.Time;
using System;
using System.IO;

namespace SuperCom.Log
{
    public class Logger : AbstractLogger
    {
        public static string LOG_DIR = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_logs");
        private Logger()
        {
        }

        public static Logger Instance { get; }



        static Logger()
        {
            Instance = new Logger();
#if DEBUG
            Instance.LogLevel = Level.Debug;
#else
            Instance.LogLevel = Level.Info;
#endif
        }


        /// <summary>
        /// 防止递归
        /// </summary>
        private bool writing = false;

        public override void LogPrint(string str)
        {
            if (writing)
                return;
            writing = true;
            Console.Write(str);
            if (!Directory.Exists(LOG_DIR))
                DirHelper.TryCreateDirectory(LOG_DIR);
            string filePath = Path.Combine(LOG_DIR, DateHelper.NowDate() + ".log");
            FileHelper.TryAppendToFile(filePath, str);
            writing = false;
        }
    }
}
