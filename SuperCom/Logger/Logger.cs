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
            Instance.LogLevel = Level.Debug;
        }

        public override void LogPrint(string str)
        {
            Console.Write(str);
            if (!Directory.Exists(LOG_DIR))
                DirHelper.TryCreateDirectory(LOG_DIR);
            string filePath = Path.Combine(LOG_DIR, DateHelper.NowDate() + ".log");
            FileHelper.TryAppendToFile(filePath, str);
        }
    }
}
