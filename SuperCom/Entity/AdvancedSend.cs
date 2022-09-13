using SuperUtils.Framework.ORM.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperCom.Entity
{
    public class SendCommand
    {
        public int Order { get; set; }
        public string Command { get; set; }
        public int Delay { get; set; }
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
                {"advanced_send","create table if not exists advanced_send( Id INTEGER PRIMARY KEY autoincrement, PortName VARCHAR(50), Connected INT DEFAULT 0, AddTimeStamp INT DEFAULT 0, AddNewLineWhenWrite INT DEFAULT 0, PortSetting VARCHAR(1000), WriteData VARCHAR(5000), CreateDate VARCHAR(30) DEFAULT(STRFTIME('%Y-%m-%d %H:%M:%S', 'NOW', 'localtime')), UpdateDate VARCHAR(30) DEFAULT(STRFTIME('%Y-%m-%d %H:%M:%S', 'NOW', 'localtime')), unique(PortName) );" }
            };

        }
    }
}
