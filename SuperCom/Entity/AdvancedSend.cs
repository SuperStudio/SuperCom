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
    public class SendCommand
    {
        public static int DEFAULT_DELAY = 1000;
        public long CommandID { get; set; }
        public int Order { get; set; }
        public string Command { get; set; }
        public int Delay { get; set; }

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


    [Table(tableName: "advanced_send")]
    public class AdvancedSend
    {
        [TableId(IdType.AUTO)]
        public int ProjectID { get; set; }
        public string ProjectName { get; set; }
        public string Commands { get; set; }


        [TableField(exist: false)]
        public List<SendCommand> CommandList { get; set; }





        // 必须要有无参构造器
        public AdvancedSend()
        {

        }
        public AdvancedSend(int projectID, string projectName)
        {
            this.ProjectID = projectID;
            this.ProjectName = projectName;
        }

        public static class SqliteTable
        {
            public static Dictionary<string, string> Table = new Dictionary<string, string>()
            {
                {"advanced_send","create table if not exists advanced_send( ProjectID INTEGER PRIMARY KEY autoincrement, ProjectName VARCHAR(200), Commands TEXT, CreateDate VARCHAR(30) DEFAULT(STRFTIME('%Y-%m-%d %H:%M:%S', 'NOW', 'localtime')), UpdateDate VARCHAR(30) DEFAULT(STRFTIME('%Y-%m-%d %H:%M:%S', 'NOW', 'localtime')) );" }
            };

        }

        public static void InitSqlite()
        {
            SqliteMapper<AdvancedSend> mapper = new SqliteMapper<AdvancedSend>(ConfigManager.SQLITE_DATA_PATH);
            foreach (var item in AdvancedSend.SqliteTable.Table.Keys)
            {
                if (!mapper.IsTableExists(item))
                {
                    mapper.CreateTable(item, AdvancedSend.SqliteTable.Table[item]);
                }
            }
        }
    }
}
