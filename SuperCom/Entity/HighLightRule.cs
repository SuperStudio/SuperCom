using SuperCom.Config;
using SuperUtils.Framework.ORM.Attributes;
using SuperUtils.Framework.ORM.Mapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicData.Annotations;
using ICSharpCode.AvalonEdit;
using SuperUtils.Time;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SuperCom.Entity
{
    [Table(tableName: "highlight_rule")]
    public class HighLightRule
    {
        [TableId(IdType.AUTO)]
        public long RuleID { get; set; }
        public string RuleName { get; set; }
        public string FileName { get; set; }

        public string RuleSetString { get; set; }


        [TableField(exist: false)]
        public List<RuleSet> RuleSetList { get; set; }



        // 必须要有无参构造器
        public HighLightRule()
        {

        }
        public HighLightRule(int RuleID, string RuleName, string FileName)
        {
            this.RuleID = RuleID;
            this.RuleName = RuleName;
            this.FileName = FileName;
        }

        public static class SqliteTable
        {
            public static Dictionary<string, string> Table = new Dictionary<string, string>()
            {
                {
                    "highlight_rule",
                    "create table if not exists highlight_rule( RuleID INTEGER PRIMARY KEY autoincrement, RuleName VARCHAR(200), FileName TEXT, RuleSetString TEXT, CreateDate VARCHAR(30) DEFAULT(STRFTIME('%Y-%m-%d %H:%M:%S', 'NOW', 'localtime')), UpdateDate VARCHAR(30) DEFAULT(STRFTIME('%Y-%m-%d %H:%M:%S', 'NOW', 'localtime')) );" }
            };

        }

        public static void InitSqlite()
        {
            SqliteMapper<HighLightRule> mapper = new SqliteMapper<HighLightRule>(ConfigManager.SQLITE_DATA_PATH);
            foreach (var item in SqliteTable.Table.Keys)
            {
                if (!mapper.IsTableExists(item))
                {
                    mapper.CreateTable(item, SqliteTable.Table[item]);
                }
            }
        }


        public enum RuleType
        {
            Regex,
            KeyWord
        }

        public class RuleSet : INotifyPropertyChanged
        {
            public long RuleSetID { get; set; }
            public RuleType RuleType { get; set; }
            public string Foreground { get; set; }
            public bool Bold { get; set; }
            public bool Italic { get; set; }
            public string RuleValue { get; set; }


            public static long GenerateID(List<long> id_list)
            {
                for (long i = 0; i <= id_list.Count; i++)
                {
                    if (id_list.Contains(i)) continue;
                    return i;
                }
                return 0;
            }

            public event PropertyChangedEventHandler PropertyChanged;

            [NotifyPropertyChangedInvocator]
            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
