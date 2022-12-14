using SuperUtils.Framework.Logger;
using SuperUtils.IO;
using System;
using System.IO;

namespace SuperCom.Log
{
    public class Logger : AbstractLogger
    {
        public static string FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
        private Logger() { }

        public static Logger Instance { get; }

        static Logger()
        {
            Instance = new Logger();
        }

        public override void LogPrint(string str)
        {
            Console.WriteLine(str);
            FileHelper.TryAppendToFile(FilePath, str);
        }
    }
}
