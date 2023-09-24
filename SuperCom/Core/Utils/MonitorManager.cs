using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;

namespace SuperCom.Utils
{
    public class MonitorManager
    {

        private ConcurrentQueue<string> MonitorQueue { get; set; } = new ConcurrentQueue<string>();

        private bool StopMonitor { get; set; } = false;
        private bool MonitorRunning { get; set; } = false;
        private StringBuilder MonitorBuffer { get; set; } = new StringBuilder();
        public void MonitorLine(string value)
        {
            if (!string.IsNullOrEmpty(value)) {
                // 将字符转为一行
                int idx = value.IndexOf("\n");
                if (idx < 0)
                    MonitorBuffer.Append(value);
                else {
                    MonitorBuffer.Append(value.Substring(0, idx + 1));
                    MonitorQueue.Enqueue(MonitorBuffer.ToString());
                    MonitorBuffer.Clear();
                    MonitorLine(value.Substring(idx + 1));
                }
            }


        }

        private void RecordMonitorValue(string line)
        {
            //if (VarMonitors == null || VarMonitors.Count == 0)
            //    return;
            //foreach (VarMonitor monitor in VarMonitors.ToList())
            //{
            //    if (!monitor.Enabled || string.IsNullOrEmpty(monitor.RegexPattern))
            //        continue;
            //    Match match = Regex.Match(line, monitor.RegexPattern);
            //    if (match != null && match.Success)
            //    {
            //        // 写到文件中
            //        string toWrite = $"{{\"value\":\"{match.Value}\", " +
            //            $"\"line\":\"{line}\", " +
            //            $"\"pattern\":\"{monitor.RegexPattern}\"}}{Environment.NewLine}";
            //        Console.WriteLine($"成功捕获：{toWrite}");
            //        FileHelper.TryAppendToFile(monitor.DataFileName, toWrite);
            //        break;
            //    }
            //}
        }

        public void StartMonitorTask()
        {
            if (MonitorRunning)
                return;
            StopMonitor = false;
            MonitorRunning = true;
            Task.Run(async () => {
                while (true) {
                    if (!MonitorQueue.IsEmpty) {
                        bool success = MonitorQueue.TryDequeue(out string data);
                        if (!success) {
                            Console.WriteLine("取队列元素失败");
                            continue;
                        }
                        RecordMonitorValue(data);
                    } else {
                        await Task.Delay(100);
                        //Console.WriteLine("过滤中...");
                    }
                    if (StopMonitor)
                        break;
                }
                MonitorRunning = false;
            });
        }

        public void StopMonitorTask()
        {
            StopMonitor = true;
        }
    }
}
