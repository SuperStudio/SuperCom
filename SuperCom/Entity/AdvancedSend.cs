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

    public enum RunningStatus
    {
        Waiting,
        Running,
        Success,
        Failed
    }
    public class SendCommand : INotifyPropertyChanged
    {
        public static int DEFAULT_DELAY = 200;
        public static int DEFAULT_TIMEOUT = 5000;
        public long CommandID { get; set; }
        public string Name { get; set; }

        private int _Order;
        public int Order
        {
            get { return _Order; }
            set { _Order = value; OnPropertyChanged(); }
        }
        public string Command { get; set; }
        public int Delay { get; set; }
        public bool Running { get; set; }
        private RunningStatus _Status = RunningStatus.Waiting;
        public RunningStatus Status
        {
            get { return _Status; }
            set { _Status = value; OnPropertyChanged(); }
        }
        private string _RecvResult = "";
        public string RecvResult
        {
            get { return _RecvResult; }
            set { _RecvResult = value; OnPropertyChanged(); }
        }
        private int _RecvTimeOut = DEFAULT_TIMEOUT;
        public int RecvTimeOut
        {
            get { return _RecvTimeOut; }
            set { _RecvTimeOut = value; OnPropertyChanged(); }
        }
        private bool _IsResultCheck = false;
        public bool IsResultCheck
        {
            get { return _IsResultCheck; }
            set { _IsResultCheck = value; OnPropertyChanged(); }
        }

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


    [Table(tableName: "advanced_send")]
    public class AdvancedSend
    {
        [TableId(IdType.AUTO)]
        public long ProjectID { get; set; }
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
