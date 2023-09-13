using SuperCom.Config;
using SuperUtils.Framework.ORM.Attributes;
using SuperUtils.Framework.ORM.Enums;
using SuperUtils.Framework.ORM.Mapper;
using SuperUtils.WPF.VieModel;
using System.Collections.Generic;

namespace SuperCom.Entity
{

    public enum RunningStatus
    {
        WaitingToRun,
        Running,
        AlreadySend,
        WaitingDelay,
        Success,
        Failed
    }

    public class SendCommand : ViewModelBase
    {
        public const int DEFAULT_DELAY = 200;
        public const int DEFAULT_TIMEOUT = 5000;

        private static readonly Dictionary<RunningStatus, string> RUN_STATUS_TABLE =
            new Dictionary<RunningStatus, string>() {
            { RunningStatus.WaitingToRun,"就绪" },
            { RunningStatus.WaitingDelay,"等待中" },
            { RunningStatus.Running,"运行中" },
            { RunningStatus.AlreadySend,"已发送" },
            { RunningStatus.Success,"成功" },
            { RunningStatus.Failed,"失败" },
        };


        public long CommandID { get; set; }
        public string Name { get; set; }

        private int _Order;
        public int Order {
            get { return _Order; }
            set { _Order = value; RaisePropertyChanged(); }
        }
        public string Command { get; set; }
        public int Delay { get; set; }
        public bool Running { get; set; }
        private RunningStatus _Status = RunningStatus.WaitingToRun;
        public RunningStatus Status {
            get { return _Status; }
            set {
                _Status = value;
                RaisePropertyChanged();
                StatusText = RUN_STATUS_TABLE[value];
            }
        }
        private string _StatusText = "就绪";
        public string StatusText {
            get { return _StatusText; }
            set { _StatusText = value; RaisePropertyChanged(); }
        }
        private string _RecvResult = "";
        public string RecvResult {
            get { return _RecvResult; }
            set { _RecvResult = value; RaisePropertyChanged(); }
        }
        private int _RecvTimeOut = DEFAULT_TIMEOUT;
        public int RecvTimeOut {
            get { return _RecvTimeOut; }
            set { _RecvTimeOut = value; RaisePropertyChanged(); }
        }
        private bool _IsResultCheck = false;
        public bool IsResultCheck {
            get { return _IsResultCheck; }
            set { _IsResultCheck = value; RaisePropertyChanged(); }
        }

        public static long GenerateID(List<long> id_list)
        {
            for (long i = 0; i <= id_list.Count; i++) {
                if (id_list.Contains(i))
                    continue;
                return i;
            }
            return 0;
        }

        public override void Init()
        {
            throw new System.NotImplementedException();
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
                {
                    "advanced_send",
                    "create table if not exists advanced_send( " +
                        "ProjectID INTEGER PRIMARY KEY autoincrement, " +
                        "ProjectName VARCHAR(200), " +
                        "Commands TEXT, " +
                        "CreateDate VARCHAR(30) DEFAULT(STRFTIME('%Y-%m-%d %H:%M:%S', 'NOW', 'localtime')), " +
                        "UpdateDate VARCHAR(30) DEFAULT(STRFTIME('%Y-%m-%d %H:%M:%S', 'NOW', 'localtime')) " +
                    ");"
                }
            };

        }

        public static void InitSqlite()
        {
            SqliteMapper<AdvancedSend> mapper = new SqliteMapper<AdvancedSend>(ConfigManager.SQLITE_DATA_PATH);
            foreach (var item in AdvancedSend.SqliteTable.Table.Keys) {
                if (!mapper.IsTableExists(item)) {
                    mapper.CreateTable(item, AdvancedSend.SqliteTable.Table[item]);
                }
            }
        }
    }



}
