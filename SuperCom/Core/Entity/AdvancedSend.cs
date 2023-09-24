using SuperCom.Config;
using SuperCom.Entity.Enums;
using SuperControls.Style;
using SuperUtils.Common;
using SuperUtils.Framework.ORM.Attributes;
using SuperUtils.Framework.ORM.Enums;
using SuperUtils.Framework.ORM.Mapper;
using SuperUtils.WPF.VieModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SuperCom.Entity
{
    public class SendCommand : ViewModelBase
    {
        public const int DEFAULT_DELAY = 200;
        public const int DEFAULT_TIMEOUT = 5000;

        #region "静态属性"

        private static readonly Dictionary<RunningStatus, string> RUN_STATUS_TABLE =
            new Dictionary<RunningStatus, string>() {
            { RunningStatus.WaitingToRun,"就绪" },
            { RunningStatus.WaitingDelay,"等待中" },
            { RunningStatus.Running,"运行中" },
            { RunningStatus.AlreadySend,"已发送" },
            { RunningStatus.Success,"成功" },
            { RunningStatus.Failed,"失败" },
        };

        #endregion

        #region "属性"


        public long CommandID { get; set; }

        private string _Name;
        public string Name {
            get { return _Name; }
            set { _Name = value; RaisePropertyChanged(); }
        }

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

        #endregion


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


        private async Task<bool> AsyncSendCommand(int idx, PortTabItem portTabItem, SendCommand command, AdvancedSend advancedSend)
        {
            bool success = false;
            await App.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)delegate {
                string value = command.Command;
                success = portTabItem.SendCommand(value, true);
                if (!success) {
                    return;
                }

                if (idx < advancedSend.CommandList.Count)
                    advancedSend.CommandList[idx].Status = RunningStatus.AlreadySend;
                success = true;
            });
            return success;
        }

        /// <summary>
        /// 发送命令的任务
        /// </summary>
        /// <param name="advancedSend"></param>
        /// <param name="portName"></param>
        /// <param name="button"></param>
        public void BeginSendCommands(AdvancedSend advancedSend, PortTabItem portTabItem, Action<bool> onSetRunningStatus)
        {
            if (advancedSend == null || string.IsNullOrEmpty(advancedSend.Commands) || portTabItem == null)
                return;
            string portName = portTabItem.Name;

            advancedSend.CommandList = JsonUtils.TryDeserializeObject<List<SendCommand>>(advancedSend.Commands);
            if (advancedSend.CommandList == null || advancedSend.CommandList.Count == 0)
                return;

            if (portTabItem == null || !portTabItem.Connected) {
                MessageNotify.Error($"端口 {portName} 未连接");
                return;
            }
            portTabItem.RunningCommands = true;

            onSetRunningStatus?.Invoke(true);
            Task.Run(async () => {
                int idx = 0;
                while (portTabItem.RunningCommands) {
                    SendCommand command = advancedSend.CommandList[idx];
                    if (idx < advancedSend.CommandList.Count)
                        advancedSend.CommandList[idx].Status = RunningStatus.Running;

                    bool success = await AsyncSendCommand(idx, portTabItem, command, advancedSend);
                    if (!success)
                        break;
                    advancedSend.CommandList[idx].Status = RunningStatus.WaitingDelay;
                    if (command.Delay > 0) {
                        int delay = 10;
                        for (int i = 1; i <= command.Delay; i += delay) {
                            if (!portTabItem.RunningCommands)
                                break;
                            await Task.Delay(delay);
                            advancedSend.CommandList[idx].StatusText = $"{command.Delay - i} ms";
                        }
                        advancedSend.CommandList[idx].StatusText = "0 ms";
                    }
                    advancedSend.CommandList[idx].Status = RunningStatus.WaitingToRun;
                    idx++;
                    if (idx >= advancedSend.CommandList.Count) {
                        idx = 0;
                        advancedSend.CommandList = advancedSend.CommandList.OrderBy(arg => arg.Order).ToList();
                    }
                }
                App.Current.Dispatcher.Invoke(() => {
                    onSetRunningStatus?.Invoke(false);
                });
            });
        }

    }



}
