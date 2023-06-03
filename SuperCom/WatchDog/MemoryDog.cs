using SuperCom.Config;
using SuperUtils.IO;
using SuperUtils.Systems;
using System;
using System.Diagnostics;
using static SuperCom.App;

namespace SuperCom.WatchDog
{


    /// <summary>
    /// 内存监控
    /// </summary>
    public class MemoryDog : AbstractDog
    {

        public Action<long> OnMemoryChanged;
        public Action<double> OnCpuUsageChanged;

#if DEBUG
        private const int WATCH_INTERVAL = 10 * 1000;
#else
        private const int WATCH_INTERVAL = 10 * 1000;
#endif
        public MemoryDog() : base(WATCH_INTERVAL)
        {

        }

        public override bool Feed()
        {
            using (Process proc = Process.GetCurrentProcess())
            {
                double cpu = Win32Helper.GetCpuUsage(proc);
                OnCpuUsageChanged?.Invoke(cpu);
                Logger.Debug($"current cpu: {cpu}%");

                // 计算内存

                long currentMemory = proc.PrivateMemorySize64;
                Logger.Debug($"current memory: {currentMemory.ToProperFileSize()}");
                OnMemoryChanged?.Invoke(currentMemory);
                if ((double)currentMemory / 1024 / 1024 >= ConfigManager.Settings.MemoryLimit)
                    return false;
            }

            return true;
        }
    }
}
