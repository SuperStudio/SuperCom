using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperCom.Entity.Enums
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
}
