using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperCom
{
    public static class GlobalVariable
    {
        public static string LogDir = System.IO.Path.Combine(Environment.CurrentDirectory, "logs");

        static GlobalVariable()
        {
            if (!Directory.Exists(LogDir))
            {
                Directory.CreateDirectory(LogDir);
            }
        }
    }
}
