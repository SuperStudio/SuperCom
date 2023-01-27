using SuperCom.Config;
using SuperUtils.Framework.ORM.Attributes;
using SuperUtils.Framework.ORM.Mapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperCom.Entity
{

    public enum VarDataType
    {
        整数,
        小数,
        字符串,
    }

    [Table(tableName: "var_monitor")]

    public class VarMonitor
    {

        public const string DATA_DIR = "monitor_data";

        [TableId(IdType.AUTO)]
        public long MonitorID { get; set; }
        public bool Enabled { get; set; }
        public int SortOrder { get; set; }
        public int VarType { get; set; }

        /// <summary>
        /// 变量名
        /// </summary>
        public string Name { get; set; }
        public string RegexPattern { get; set; }


        /// <summary>
        /// 监视的变量所保存的文件名
        /// </summary>
        public string DataFileName { get; set; }
        public string CreateDate { get; set; }
        public string UpdateDate { get; set; }


        // 必须要有无参构造器
        public VarMonitor()
        {
            Enabled = true;
            VarType = (int)VarDataType.整数;
        }

        public VarMonitor(int sortOrder) : this()
        {
            SortOrder = sortOrder;
            Name = $"变量_{sortOrder}";
        }

        // SQLite 中建表语句
        public static class SqliteTable
        {
            public static Dictionary<string, string> Table = new Dictionary<string, string>()
            {

                {
                    "var_monitor",

                    "create table if not exists var_monitor( " +
                    "MonitorID INTEGER PRIMARY KEY autoincrement, " +
                    "Enabled INT DEFAULT 1, " +
                    "SortOrder INT DEFAULT 0, " +
                    "VarType INT DEFAULT 0, " +
                    "Name VARCHAR(50), " +
                    "RegexPattern VARCHAR(200), " +
                    "DataFileName VARCHAR(200), " +
                    "CreateDate VARCHAR(30) DEFAULT(STRFTIME('%Y-%m-%d %H:%M:%S', 'NOW', 'localtime')), " +
                    "UpdateDate VARCHAR(30) DEFAULT(STRFTIME('%Y-%m-%d %H:%M:%S', 'NOW', 'localtime')), " +
                    "unique(Name) );" }
            };

        }


        public static void InitSqlite()
        {
            SqliteMapper<VarMonitor> mapper = new SqliteMapper<VarMonitor>(ConfigManager.SQLITE_DATA_PATH);
            foreach (var item in SqliteTable.Table.Keys)
            {
                if (!mapper.IsTableExists(item))
                {
                    mapper.CreateTable(item, SqliteTable.Table[item]);
                }
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj as VarMonitor == null)
                return false;
            VarMonitor monitor = obj as VarMonitor;
            System.Reflection.PropertyInfo[] propertyInfos = monitor.GetType().GetProperties();
            foreach (var item in propertyInfos)
            {
                if (item.GetValue(monitor) == null && item.GetValue(this) == null)
                {
                    continue;
                }

                if (item.GetValue(monitor) == null || !item.GetValue(monitor).Equals(item.GetValue(this)))
                    return false;

            }
            return true;
        }

        public override int GetHashCode()
        {
            int result = int.MinValue;
            System.Reflection.PropertyInfo[] propertyInfos = this.GetType().GetProperties();
            foreach (var item in propertyInfos)
            {
                result += item.GetHashCode();
            }
            return result;
        }
    }
}
