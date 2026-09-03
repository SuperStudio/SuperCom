using SuperCom.Entity;
using SuperUtils.IO;
using System;
using System.IO;

namespace SuperCom.Config
{
    public static class PathManager
    {

        public static string[] BASE_DIR = { VarMonitor.DATA_DIR, "logs" };


        public static void Init()
        {
            foreach (string dir in BASE_DIR) {
                string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dir);
                if (!Directory.Exists(path)) {
                    DirHelper.TryCreateDirectory(path);
                }
            }
        }
    }
}
