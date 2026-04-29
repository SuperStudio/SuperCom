using SuperCom.Core.Entity.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperCom.Core.Entity
{
    public class TabInfo
    {
        public ConnectType ConnectType { get; set; }

        public bool IsConnected { get; set; }

        public object Data { get; set; }

        public bool RemoveBar { get; set; }

        public TabInfo()
        {

        }

        public TabInfo(ConnectType connectType, bool isConnected, object data)
        {
            ConnectType = connectType;
            IsConnected = isConnected;
            Data = data;
        }
    }
}
