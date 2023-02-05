using SuperUtils.Framework.Logger;
using SuperUtils.IO;
using SuperUtils.Time;
using System;
using System.IO;

namespace SuperCom.Log
{
    public class Logger : AbstractLogger
    {
        private static string LogDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_logs");
        private static string LogFilePath = Path.Combine(LogDir, "app.log");
        private Logger() { }

        public static Logger Instance { get; }

        static Logger()
        {
            Instance = new Logger();
            Instance.LogLevel = AbstractLogger.Level.Info;
            if (!Directory.Exists(LogDir))
                DirHelper.TryCreateDirectory(LogDir);
            LogFilePath = Path.Combine(LogDir, $"{DateHelper.NowDate()}.log");
        }

        public override void LogPrint(string str)
        {
            Console.WriteLine(str);
            FileHelper.TryAppendToFile(LogFilePath, str, System.Text.Encoding.UTF8);
        }
    }
}
