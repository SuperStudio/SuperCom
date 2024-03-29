﻿using SuperCom.Config;
using SuperCom.Entity.Enums;
using SuperControls.Style;
using SuperUtils.Framework.ORM.Attributes;
using SuperUtils.Framework.ORM.Enums;
using SuperUtils.Framework.ORM.Mapper;
using SuperUtils.WPF.VieModel;
using System.Collections.Generic;

namespace SuperCom.Entity
{

    [Table(tableName: "var_monitor")]

    public class VarMonitor : ViewModelBase
    {

        public const string DATA_DIR = "monitor_data";

        [TableId(IdType.AUTO)]
        public long MonitorID { get; set; }

        /// <summary>
        /// 和串口号绑定的
        /// </summary>
        public string PortName { get; set; }
        public bool Enabled { get; set; }
        public int SortOrder { get; set; }
        private int _VarType;
        public int VarType {
            get { return _VarType; }
            set {
                _VarType = value;
                if (value <= 1)
                    CanDrawImage = true;
                else
                    CanDrawImage = false;
                RaisePropertyChanged();
            }
        }

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



        private bool _CanDrawImage = false;
        [TableField(exist: false)]
        public bool CanDrawImage {
            get { return _CanDrawImage; }
            set {
                _CanDrawImage = value;
                RaisePropertyChanged();
            }
        }


        // 必须要有无参构造器
        public VarMonitor()
        {
            Enabled = true;
            VarType = (int)VarDataType.整数;
        }

        public VarMonitor(int sortOrder, string portName) : this()
        {
            SortOrder = sortOrder;
            Name = $"{LangManager.GetValueByKey("Variable")}_{sortOrder}";
            PortName = portName;
        }

        // SQLite 中建表语句
        public static class SqliteTable
        {
            public static Dictionary<string, string> Table = new Dictionary<string, string>()
            {
                {
                    "var_monitor",

                    "BEGIN;" +
                        "create table if not exists var_monitor( " +
                            "MonitorID INTEGER PRIMARY KEY autoincrement, " +
                            "PortName VARCHAR(20), " +
                            "Enabled INT DEFAULT 1, " +
                            "SortOrder INT DEFAULT 0, " +
                            "VarType INT DEFAULT 0, " +
                            "Name VARCHAR(50), " +
                            "RegexPattern VARCHAR(200), " +
                            "DataFileName VARCHAR(200), " +
                            "CreateDate VARCHAR(30) DEFAULT(STRFTIME('%Y-%m-%d %H:%M:%S', 'NOW', 'localtime')), " +
                            "UpdateDate VARCHAR(30) DEFAULT(STRFTIME('%Y-%m-%d %H:%M:%S', 'NOW', 'localtime')), " +
                            "unique(PortName,Name)" +
                        ");" +
                    "CREATE INDEX var_monitor_idx_PortName ON var_monitor (PortName);" +
                    "CREATE INDEX var_monitor_idx_PortName_Name ON var_monitor (PortName,Name);" +
                    "COMMIT;"
                }
            };

        }


        public static void InitSqlite()
        {
            SqliteMapper<VarMonitor> mapper = new SqliteMapper<VarMonitor>(ConfigManager.SQLITE_DATA_PATH);
            foreach (var item in SqliteTable.Table.Keys) {
                if (!mapper.IsTableExists(item)) {
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
            foreach (var item in propertyInfos) {
                if (item.GetValue(monitor) == null && item.GetValue(this) == null) {
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
            foreach (var item in propertyInfos) {
                result += item.GetHashCode();
            }
            return result;
        }

        public override void Init()
        {
            throw new System.NotImplementedException();
        }
    }
}
