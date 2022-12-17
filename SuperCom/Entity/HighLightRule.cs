using SuperCom.Config;
using SuperUtils.Framework.ORM.Attributes;
using SuperUtils.Framework.ORM.Mapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit;
using SuperUtils.Time;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Controls;
using System.Windows.Threading;
using SuperUtils.IO;
using SuperUtils.Common;
using SuperUtils.WPF.VieModel;

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

        public string PreviewText { get; set; }

        public string Extra { get; set; }


        [TableField(exist: false)]
        public List<RuleSet> RuleSetList { get; set; }



        // 必须要有无参构造器
        public HighLightRule()
        {

        }

        public void SetFileName()
        {
            FileName = $"{RuleID}_{RuleName.ToProperFileName()}.xshd";
        }

        public string GetFullFileName()
        {
            SetFileName();
            return Path.Combine(GetDirName(), FileName);
        }


        public static string GetDirName()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "AvalonEdit", "Higlighting");
        }

        public void WriteToXshd()
        {
            string outputFileName = GetFullFileName();
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
            builder.AppendLine($"<SyntaxDefinition name=\"{RuleName}\" xmlns=\"http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008\">");
            builder.AppendLine("    <RuleSet >");

            if (!string.IsNullOrEmpty(RuleSetString))
            {
                RuleSetList = JsonUtils.TryDeserializeObject<List<RuleSet>>(RuleSetString);
                if (RuleSetList != null && RuleSetList.Count > 0)
                {
                    foreach (RuleSet rule in RuleSetList)
                    {
                        string ruleString = GetRuleString(rule);
                        if (!string.IsNullOrEmpty(ruleString))
                            builder.AppendLine(ruleString);
                    }
                }
            }
            builder.AppendLine("    </RuleSet>");
            builder.AppendLine("</SyntaxDefinition>");


            FileHelper.TryWriteToFile(outputFileName, builder.ToString());
        }

        private string GetRuleString(RuleSet rule)
        {
            StringBuilder builder = new StringBuilder();
            if (rule == null || string.IsNullOrEmpty(rule.RuleValue)) return null;

            string fontWeight = rule.Bold ? " fontWeight=\"bold\"" : "";
            string fontStyle = rule.Italic ? " fontStyle=\"italic\"" : "";


            if (rule.RuleType == RuleType.KeyWord)
            {
                builder.AppendLine($"       <Keywords{fontWeight}{fontStyle} foreground=\"{rule.Foreground}\">");
                builder.AppendLine($"           <Word>{rule.RuleValue}</Word>");
                builder.AppendLine("        </Keywords>");
            }
            else if (rule.RuleType == RuleType.Regex)
            {
                builder.Append($"       <Rule{fontWeight}{fontStyle} foreground=\"{rule.Foreground}\">{rule.RuleValue}</Rule>");
            }
            return builder.ToString();
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
                    "create table if not exists highlight_rule( RuleID INTEGER PRIMARY KEY autoincrement, RuleName VARCHAR(200), FileName TEXT, RuleSetString TEXT,PreviewText TEXT,Extra TEXT, CreateDate VARCHAR(30) DEFAULT(STRFTIME('%Y-%m-%d %H:%M:%S', 'NOW', 'localtime')), UpdateDate VARCHAR(30) DEFAULT(STRFTIME('%Y-%m-%d %H:%M:%S', 'NOW', 'localtime')) );" }
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

        public class RuleSet
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
        }
    }
}
