using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperCom.Core.Events
{
    public enum EventType
    {
        None,
        OpenAll,
        CloseAll,
        ProcOne,
        OpenTab,
        Remark,
        StatusChanged,
        StatusText,
        RemoveTabBar,
        NotifyTabItemBaudRate,
        ShowHighLight,
        OnSendCommandModify,
    }
}
