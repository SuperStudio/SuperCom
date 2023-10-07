using SuperControls.Style;
using SuperUtils.Windows.WindowCmd;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SuperCom.Entity
{
    public static class VirtualPortManager
    {
        public const string COM_0_COM_PROGRAM_NAME = "Null-modem emulator (com0com)";
        public const string COM_0_COM_PROGRAM_EXE_NAME = "setupc.exe";
        public const int CMD_RUN_TIME_OUT = 5000;

        private static string AppDir { get; set; }
        private static string AppPath { get; set; }

        public static void Init(string appPath)
        {
            AppPath = appPath;
            AppDir = Directory.GetParent(AppPath).FullName;
        }

        /// <summary>
        /// CNCA0 PortName=COM2,EmuBR=yes,EmuOverrun=yes
        /// CNCB0 PortName = COM4, EmuBR = yes, EmuOverrun = yes
        /// </summary>
        public static VirtualPort ParseVirtualPort(string data)
        {
            if (string.IsNullOrEmpty(data) || data.IndexOf(" ") < 0)
                return null;
            data = data.Trim();
            string name = data.Split(' ')[0];
            VirtualPort result = new VirtualPort();
            result.ID = name;
            List<string> list = data.Split(' ')[1].Split(',').ToList();
            if (list.Count > 0) {
                List<System.Reflection.PropertyInfo> propertyInfos = result.GetType().GetProperties().ToList();
                List<string> names = propertyInfos.Select(arg => arg.Name).ToList();
                foreach (var item in list) {
                    if (item.IndexOf("=") <= 0)
                        continue;
                    string key = item.Split('=')[0];
                    string value = item.Split('=')[1];
                    if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
                        continue;
                    key = key.Trim();
                    value = value.Trim();
                    if (names.Contains(key)) {
                        System.Reflection.PropertyInfo propertyInfo = propertyInfos.FirstOrDefault(arg => arg.Name.Equals(key));
                        if (propertyInfo.PropertyType == typeof(bool)) {
                            if (value.ToLower().Equals("yes"))
                                propertyInfo.SetValue(result, true);
                            else
                                propertyInfo.SetValue(result, false);
                        } else if (propertyInfo.PropertyType == typeof(long)) {
                            long.TryParse(value, out long longValue);
                            propertyInfo.SetValue(result, longValue);
                        } else if (propertyInfo.PropertyType == typeof(double)) {
                            double.TryParse(value, out double doubleValue);
                            propertyInfo.SetValue(result, doubleValue);
                        } else if (propertyInfo.PropertyType == typeof(string)) {
                            propertyInfo.SetValue(result, value);
                        }
                    }
                }
            }

            return result;
        }


        public static async Task<List<VirtualPort>> ListAllPort()
        {
            List<VirtualPort> result = new List<VirtualPort>();
            if (!File.Exists(AppPath))
                return result;
            string cmdParam = $"/C cd /d \"{AppDir}\" && setupc.exe list";
            bool completed = false;
            await Task.Run(() => {
                CmdHelper.Run($"cmd.exe", cmdParam, (output) => {
                    VirtualPort port = ParseVirtualPort(output);
                    if (port != null)
                        result.Add(port);
                }, null, (ex) => {
                    App.Logger.Error(ex.Message);
                }, () => {
                    completed = true;
                });
            });

            // 超时
            await Task.Run(async () => {
                int time = 0;
                while (!completed) {
                    await Task.Delay(100);
                    time += 100;
                    if (time > CMD_RUN_TIME_OUT)
                        return false;
                }
                return completed;
            });
            return result;
        }

        public static async Task<bool> InsertPort(VirtualPort portA, VirtualPort portB)
        {
            if (portA == null || portB == null || string.IsNullOrEmpty(portA.PortName) || string.IsNullOrEmpty(portB.PortName))
                return false;

            if (!File.Exists(AppPath))
                return false;
            string cmdParam = $"/C cd /d \"{AppDir}\" && setupc.exe install PortName={portA.PortName} PortName={portB.PortName}";
            bool completed = false;
            int count = 0;
            await Task.Run(() => {
                CmdHelper.Run($"cmd.exe", cmdParam, (output) => {
                    App.Logger.Info(output);
                    if (output.IndexOf("logged as \"in use\"") >= 0)
                        count++;
                    completed = count == 2;
                }, null, (ex) => {
                    App.Logger.Error(ex.Message);
                });
            });
            // 超时
            return await Task.Run(async () => {
                int time = 0;
                while (!completed) {
                    await Task.Delay(100);
                    time += 100;
                    if (time > CMD_RUN_TIME_OUT)
                        return false;
                }
                return completed;
            });
        }

        public static async Task<bool> DeletePort(int n)
        {
            if (n < 0)
                return false;
            if (!File.Exists(AppPath))
                return false;
            string cmdParam = $"/C cd /d \"{AppDir}\" && setupc.exe remove {n}";
            bool completed = false;
            int count = 0;
            await Task.Run(() => {
                CmdHelper.Run($"cmd.exe", cmdParam, (output) => {
                    App.Logger.Info(output);
                    if (output.IndexOf($"Removed CNCA{n}") >= 0 ||
                        output.IndexOf($"Removed CNCB{n}") >= 0)
                        count++;
                    completed = count == 2;
                }, null, (ex) => {
                    App.Logger.Error(ex.Message);
                    completed = true;
                });
            });

            // 超时
            return await Task.Run(async () => {
                int time = 0;
                while (!completed) {
                    await Task.Delay(100);
                    time += 100;
                    if (time > CMD_RUN_TIME_OUT)
                        return false;
                }
                return completed;
            });
        }

        public static async Task<bool> UpdatePorts(List<VirtualPort> ports)
        {
            if (!File.Exists(AppPath) || ports == null || ports.Count == 0)
                return false;
            bool completed = false;
            foreach (VirtualPort port in ports) {
                string cmdParam = $"/C cd /d \"{AppDir}\" && setupc.exe change {port.ID} {port.ToUpdateString()}";
                App.Logger.Info($"{LangManager.GetValueByKey("RunCommand")}：{cmdParam}");
                await Task.Run(() => {
                    CmdHelper.Run($"cmd.exe", cmdParam, (output) => {
                        App.Logger.Info(output);
                        if (output.IndexOf($"Restarted {port.ID}") >= 0)
                            completed = true;
                    });
                });

                // 超时
                bool success = await Task.Run(async () => {
                    int time = 0;
                    while (!completed) {
                        await Task.Delay(100);
                        time += 100;
                        if (time > CMD_RUN_TIME_OUT)
                            return false;
                    }
                    return completed;
                });
                if (!success)
                    return false;
            }
            return true;
        }
    }

    public class VirtualPort : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] string name = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private const double MAX_EMUNOISE = 0.99999999;
        private const double MAX_MS_VALUE = int.MaxValue;

        /*
         * 常用命令
          install - -
          install 5 * *
          remove 0
          install PortName=COM2 PortName=COM4
          install PortName=COM5,EmuBR=yes,EmuOverrun=yes -
          change CNCA0 EmuBR=yes,EmuOverrun=yes
          list
          uninstall
          busynames COM?*
         */

        public VirtualPort()
        {

        }

        public VirtualPort(string portName)
        {
            this.PortName = portName;
        }

        public static bool IsProperPortName(string portName)
        {
            if (string.IsNullOrEmpty(portName) || !portName.ToUpper().StartsWith("COM"))
                return false;
            bool success = int.TryParse(portName.ToUpper().Trim().Replace("COM", ""), out int portNumber);
            return success && portNumber > 0 && portNumber < int.MaxValue;

        }

        public static bool IsProperNumber(VirtualPort port)
        {
            if (port == null)
                return false;
            if (port.AddRITO < 0 || port.AddRITO > MAX_MS_VALUE)
                return false;
            if (port.AddRTTO < 0 || port.AddRTTO > MAX_MS_VALUE)
                return false;
            if (port.EmuNoise < 0 || port.EmuNoise > MAX_EMUNOISE)
                return false;
            return true;
        }

        public string ID { get; set; }
        public string PortName { get; set; }

        /// <summary>
        ///  enable/disable baud rate emulation in the direction to the paired port(disabled by default)
        /// </summary>
        public bool EmuBR { get; set; }

        /// <summary>
        /// enable/disable buffer overrun (disabled by default)
        /// </summary>
        public bool EmuOverrun { get; set; }


        /// <summary>
        /// EmuNoise=<n>
        /// probability in range 0-0.99999999 of error per character frame in the direction to the paired port (0 by default)
        /// </summary>
        public double EmuNoise { get; set; }

        /// <summary>
        /// AddRTTO=<n>      
        /// add <n> milliseconds to the total time-out period for read operations(0 by default)
        /// </summary>
        public long AddRTTO { get; set; }


        /// <summary>
        /// AddRITO=<n>             
        /// add <n> milliseconds to the maximum time allowed to elapse between the arrival of two characters for read operations(0 by default)
        /// </summary>
        public long AddRITO { get; set; }


        /// <summary>
        /// PlugInMode={yes|no}
        /// enable/disable plug-in mode, the plug-in mode port is hidden and can't be open if the paired port is not open(disabled by default)
        /// </summary>
        public bool PlugInMode { get; set; }


        /// <summary>
        ///  ExclusiveMode={yes|no}
        ///  enable/disable exclusive mode, the exclusive mode port is hidden if it is open(disabled by default)
        /// </summary>
        public bool ExclusiveMode { get; set; }


        /// <summary>
        /// HiddenMode={yes|no}
        /// enable/disable hidden mode, the hidden mode port is hidden as it is possible for port enumerators (disabled by default)
        /// </summary>
        public bool HiddenMode { get; set; }



        /*
            另外几个
          cts=[!]<p>              - wire CTS pin to <p> (rrts by default)
          dsr=[!]<p>              - wire DSR pin to <p> (rdtr by default)
          dcd=[!]<p>              - wire DCD pin to <p> (rdtr by default)
          ri=[!]<p>               - wire RI pin to <p> (!on by default)

        The possible values of <p> above can be rrts, lrts, rdtr, ldtr, rout1, lout1,
        rout2, lout2 (remote/local RTS/DTR/OUT1/OUT2), ropen, lopen (logical ON if
        remote/local port is open) or on (logical ON). The exclamation sign (!) can be
        used to invert the value.
        */


        public override bool Equals(object obj)
        {
            if (obj == null || obj as VirtualPort == null)
                return false;
            VirtualPort port = obj as VirtualPort;
            System.Reflection.PropertyInfo[] propertyInfos = port.GetType().GetProperties();
            foreach (var item in propertyInfos) {
                if (item.GetValue(port) == null && item.GetValue(this) == null) {
                    continue;
                }

                if (item.GetValue(port) == null || !item.GetValue(port).Equals(item.GetValue(this)))
                    return false;

            }
            return true;
        }

        public override int GetHashCode()
        {
            int result = int.MinValue;
            System.Reflection.PropertyInfo[] propertyInfos = this.GetType().GetProperties();
            foreach (var item in propertyInfos) {
                result += item.GetHashCode();
            }
            return result;
        }

        public string ToUpdateString()
        {
            StringBuilder builder = new StringBuilder();
            System.Reflection.PropertyInfo[] propertyInfos = this.GetType().GetProperties();
            foreach (var item in propertyInfos) {
                if (item.Name.Equals("ID"))
                    continue;
                object v = item.GetValue(this);
                if (v == null)
                    continue;
                string str = v.ToString();
                if (item.PropertyType == typeof(bool))
                    str = str.ToLower().Equals("true") ? "yes" : "no";
                builder.Append($"{item.Name}={str},");
            }
            if (builder.Length > 0)
                builder.Remove(builder.Length - 1, 1);
            return builder.ToString();
        }

    }
}
