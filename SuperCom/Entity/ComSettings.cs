using SuperCom.Config;
using SuperUtils.Framework.ORM.Attributes;
using SuperUtils.Framework.ORM.Enums;
using SuperUtils.Framework.ORM.Mapper;
using System;
using System.Collections.Generic;

namespace SuperCom.Entity
{

    [Table(tableName: "com_settings")]
    public class ComSettings
    {
        [TableId(IdType.AUTO)]
        public int Id { get; set; }
        public string PortName { get; set; }
        public bool Connected { get; set; }
        public bool AddTimeStamp { get; set; }
        public bool AddNewLineWhenWrite { get; set; }
        public bool SendHex { get; set; }
        public bool RecvShowHex { get; set; }
        public bool EnabledFilter { get; set; }
        public bool EnabledMonitor { get; set; }
        public string PortSetting { get; set; } // json 格式
        public string WriteData { get; set; }

        public string CreateDate { get; set; }
        public string UpdateDate { get; set; }


        // 必须要有无参构造器
        public ComSettings()
        {

        }

        public override bool Equals(object obj)
        {
            if (obj is ComSettings com) {
                if (PortName == null && com.PortName == null)
                    return true;
                else if (PortName != null)
                    return PortName.Equals(com.PortName);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return this.PortName.GetHashCode();
        }

        // SQLite 中建表语句
        public static class SqliteTable
        {
            public static Dictionary<string, string> Table = new Dictionary<string, string>()
            {
                {
                    "com_settings",
                    "create table if not exists com_settings( " +
                        "Id INTEGER PRIMARY KEY autoincrement, " +
                        "PortName VARCHAR(50), " +
                        "Connected INT DEFAULT 0, " +
                        "AddTimeStamp INT DEFAULT 0, " +
                        "AddNewLineWhenWrite INT DEFAULT 0, " +
                        "PortSetting VARCHAR(1000), " +
                        "WriteData VARCHAR(5000), " +
                        "CreateDate VARCHAR(30) DEFAULT(STRFTIME('%Y-%m-%d %H:%M:%S', 'NOW', 'localtime')), " +
                        "UpdateDate VARCHAR(30) DEFAULT(STRFTIME('%Y-%m-%d %H:%M:%S', 'NOW', 'localtime')), " +
                        "unique(PortName) " +
                    ");"
                }
            };

        }

        public static string[] SQL_EXTRA_CMDS = {
             "ALTER TABLE com_settings ADD COLUMN EnabledFilter INT DEFAULT 0;",
             "ALTER TABLE com_settings ADD COLUMN EnabledMonitor INT DEFAULT 0;",
             "ALTER TABLE com_settings ADD COLUMN SendHex INT DEFAULT 0;",
             "ALTER TABLE com_settings ADD COLUMN RecvShowHex INT DEFAULT 0;",
        };

        public static void InitSqlite()
        {
            SqliteMapper<ComSettings> mapper = new SqliteMapper<ComSettings>(ConfigManager.SQLITE_DATA_PATH);
            foreach (var item in ComSettings.SqliteTable.Table.Keys) {
                if (!mapper.IsTableExists(item)) {
                    mapper.CreateTable(item, ComSettings.SqliteTable.Table[item]);
                }
            }

            // 新增列
            foreach (string sql in SQL_EXTRA_CMDS) {
                try {
                    mapper.ExecuteNonQuery(sql);
                } catch (Exception ex) {
                    App.Logger.Error(ex.Message);
                }
            }
        }
    }
}
