using SuperUtils.Framework.ORM.Config;
using System.Collections.Generic;
using System.Windows;

namespace SuperCom.Config.WindowConfig
{
    public class Settings : AbstractConfig
    {

        private const int DEFAULT_LOG_FRAG_SIZE = 40;  // MB
        private const int DEFAULT_MEMORY_LIMIT = 1024; // MB
        private Settings() : base(ConfigManager.SQLITE_DATA_PATH, $"WindowConfig.Settings")
        {
            Width = SystemParameters.WorkArea.Width * 0.7;
            Height = SystemParameters.WorkArea.Height * 0.7;
            FirstRun = true;
            AutoBackupPeriodIndex = 0;
            AutoBackup = true;
            EnabledLogFrag = true;
            LogFragSize = DEFAULT_LOG_FRAG_SIZE;
            MemoryLimit = DEFAULT_MEMORY_LIMIT;
            HintWhenPin = true;
            FixedWhenFocus = true;
        }

        public static List<int> BackUpPeriods = new List<int> { 1, 3, 7, 15, 30 };

        private static Settings _instance = null;

        public static Settings CreateInstance()
        {
            if (_instance == null)
                _instance = new Settings();

            return _instance;
        }

        public string CurrentLanguage { get; set; }
        public long ThemeIdx { get; set; }
        public string ThemeID { get; set; }

        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public long WindowState { get; set; }
        public bool FirstRun { get; set; }

        public bool AutoBackup { get; set; }

        public long AutoBackupPeriodIndex { get; set; }
        public long RemoteIndex { get; set; }
        public bool EnabledLogFrag { get; set; }
        public long LogFragSize { get; set; }
        public long MemoryLimit { get; set; }
        public bool HintWhenPin { get; set; }
        public bool FixedWhenFocus { get; set; }
        public bool AvoidScreenClose { get; set; }

    }
}
