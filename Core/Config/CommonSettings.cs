using SuperUtils.Framework.ORM.Config;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace SuperCom.Config.WindowConfig
{
    public class CommonSettings : AbstractConfig
    {

        public const string DEFAULT_LOG_NAME_FORMAT = "[%C] %Y-%MM-%DD %hh-%mm-%ss.%fff";

        public static string DEFAULT_LOG_SAVE_DIR { get; set; } =
            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "%Y-%MM-%DD");
        public static List<string> SUPPORT_FORMAT { get; set; } = new List<string>()
        {
            "%MM","%DD","%hh","%mm","%ss","%fff","%C","%R","%Y","%M","%D","%h","%m","%s","%f"
        };

        private CommonSettings() : base(ConfigManager.SQLITE_DATA_PATH, $"WindowConfig.CommonSettings")
        {
            FixedOnSearch = true;
            ScrollOnSearchClosed = true;
            FixedOnSendCommand = false;
            LogNameFormat = DEFAULT_LOG_NAME_FORMAT;
            LogSaveDir = DEFAULT_LOG_SAVE_DIR;
            WriteLogToFile = true;
            // 数据库迁移：确保新增的 LogEditorPath 字段存在
            MigrateLogEditorPathColumn();
        }

        /// <summary>
        /// 迁移：给 WindowConfig.CommonSettings 表添加 LogEditorPath 列（如不存在）
        /// </summary>
        private static void MigrateLogEditorPathColumn()
        {
            try
            {
                string tableName = $"WindowConfig.CommonSettings";
                using (var conn = new System.Data.SQLite.SQLiteConnection(
                    $"Data Source={ConfigManager.SQLITE_DATA_PATH}"))
                {
                    conn.Open();
                    // 检查列是否存在
                    using (var cmd = new System.Data.SQLite.SQLiteCommand(
                        $"PRAGMA table_info({tableName})", conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            bool exists = false;
                            while (reader.Read())
                            {
                                if (reader["name"].ToString() == "LogEditorPath")
                                {
                                    exists = true;
                                    break;
                                }
                            }
                            if (!exists)
                            {
                                using (var alter = new System.Data.SQLite.SQLiteCommand(
                                    $"ALTER TABLE {tableName} ADD COLUMN LogEditorPath TEXT DEFAULT ''", conn))
                                {
                                    alter.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 迁移失败不阻塞启动，下次打开设置时会重试
                System.Diagnostics.Debug.WriteLine($"MigrateLogEditorPathColumn failed: {ex.Message}");
            }
        }

        private static CommonSettings _instance = null;

        public static CommonSettings CreateInstance()
        {
            if (_instance == null)
                _instance = new CommonSettings();

            return _instance;
        }
        public bool FixedOnSearch { get; set; }
        public bool CloseToBar { get; set; }
        public bool ScrollOnSearchClosed { get; set; }
        public bool FixedOnSendCommand { get; set; }
        public string LogNameFormat { get; set; }
        public string LogSaveDir { get; set; }
        public long TabSelectedIndex { get; set; }
        public long HighLightSideIndex { get; set; }
        public bool WriteLogToFile { get; set; }
        public long AsciiSelectedIndex { get; set; }
        public long RefSelectedIndex { get; set; }

        /// <summary>
        /// 日志文件打开程序路径（留空则使用系统默认应用）
        /// </summary>
        public string LogEditorPath { get; set; } = "";

    }
}
