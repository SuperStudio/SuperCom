using SuperUtils.Framework.ORM.Attributes;
using SuperUtils.Framework.ORM.Enums;
using SuperUtils.WPF.VieModel;
using System;

namespace SuperCom.Entity
{
    /// <summary>
    /// CMD 指令工具 - 单条指令实体
    /// </summary>
    public class CmdCommand : ViewModelBase
    {
        private const int DEFAULT_DELAY = 1000;

        [TableId(IdType.AUTO)]
        public long CommandID { get; set; }

        private string _Command;
        public string Command {
            get { return _Command; }
            set { _Command = value; RaisePropertyChanged(); }
        }

        private string _Memo = "";
        /// <summary>
        /// 备注（显示在执行按钮上）
        /// </summary>
        public string Memo {
            get { return _Memo; }
            set { _Memo = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(ButtonText)); }
        }

        private bool _IsHex = false;
        /// <summary>
        /// 是否以 HEX 发送
        /// </summary>
        public bool IsHex {
            get { return _IsHex; }
            set { _IsHex = value; RaisePropertyChanged(); }
        }

        private int _Delay = DEFAULT_DELAY;
        /// <summary>
        /// 延迟（毫秒）
        /// </summary>
        public int Delay {
            get { return _Delay; }
            set { _Delay = value; RaisePropertyChanged(); }
        }

        private bool _IsSelected = true;
        /// <summary>
        /// 是否勾选（参与批量执行）
        /// </summary>
        public bool IsSelected {
            get { return _IsSelected; }
            set { _IsSelected = value; RaisePropertyChanged(); }
        }

        private int _Order;
        /// <summary>
        /// 排序顺序
        /// </summary>
        public int Order {
            get { return _Order; }
            set { _Order = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 按钮显示文字（备注或默认）
        /// </summary>
        public string ButtonText => string.IsNullOrEmpty(Memo) ? "执行" : Memo;

        public static int GetDefaultDelay() => DEFAULT_DELAY;

        public static long GenerateID(System.Collections.Generic.List<long> idList)
        {
            for (long i = 0; i <= idList.Count; i++) {
                if (!idList.Contains(i))
                    return i;
            }
            return 0;
        }

        public override void Init()
        {
            // 不需要实现
        }
    }
}
