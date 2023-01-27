using SuperCom.Entity;
using SuperUtils.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperCom.Config
{
    public static class PathManager
    {

        public static string[] BASE_DIR = { VarMonitor.DATA_DIR, "logs" };


        public static void Init()
        {
            foreach (string dir in BASE_DIR)
            {
                string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dir);
                if (!Directory.Exists(path))
                {
                    DirHelper.TryCreateDirectory(path);
                }
            }
        }
    }
}
