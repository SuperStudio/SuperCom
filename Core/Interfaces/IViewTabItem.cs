using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperCom.Core.Interfaces
{

    /// <summary>
    /// 用户可见的选项卡
    /// </summary>
    public interface IViewTabItem
    {
        void Close();
        void Open();
    }
}
