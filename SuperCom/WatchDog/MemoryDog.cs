using SuperCom.Config;
using SuperUtils.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperCom.WatchDog
{


    /// <summary>
    /// 内存监控
    /// </summary>
    public class MemoryDog : AbstractDog
    {

        public Action<long> OnMemoryChanged;

#if DEBUG
        private const int WATCH_INTERVAL = 5 * 1000;
#else
        private const int WATCH_INTERVAL = 60 * 1000;
#endif
        public MemoryDog() : base(WATCH_INTERVAL)
        {

        }

        public override bool Feed()
        {
            using (Process proc = Process.GetCurrentProcess())
            {
                long currentMemory = proc.PrivateMemorySize64;
                Console.WriteLine($"memory = {currentMemory.ToProperFileSize()}");
                OnMemoryChanged?.Invoke(currentMemory);
                if ((double)currentMemory / 1024 / 1024 >= ConfigManager.Settings.MemoryLimit)
                    return false;
            }
            return true;
        }
    }
}
