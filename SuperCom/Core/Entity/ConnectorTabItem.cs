using SuperCom.Core.Entity;
using SuperCom.Core.Events;
using SuperControls.Style.UserControls.TabControlPro;
using SuperControls.Style.UserControls.TabControlPro.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace SuperCom.Entity
{
    public class ConnectorTabItem : TabItemPro
    {
        public ConnectorTabItem(string name, UIElement element, Geometry tabIcon = null, bool selected = false, bool pinned = false) : base(name, element, tabIcon, selected, pinned)
        {
        }

        public ConnectorTabItem(string name, bool connect, UIElement element, TabPosition position) : base(name, element)
        {
            Connected = connect;
            Position = position;
        }

        private string _Remark;
        public string Remark {
            get { return _Remark; }
            set { _Remark = value; RaisePropertyChanged(); }
        }
        private string _Detail;
        public string Detail {
            get { return _Detail; }
            set { _Detail = value; RaisePropertyChanged(); }
        }

        private bool _Connect;
        public bool Connected {
            get { return _Connect; }
            set { _Connect = value; RaisePropertyChanged(); }
        }
    }
}
