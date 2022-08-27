using DynamicData.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SuperCom.Entity
{
    public class SideComPort : INotifyPropertyChanged
    {
        private string _Name;
        public string Name
        {
            get { return _Name; }
            set { _Name = value; OnPropertyChanged(); }
        }
        private bool _Connected;
        public bool Connected
        {
            get { return _Connected; }
            set { _Connected = value; OnPropertyChanged(); }
        }
        private PortTabItem _PortTabItem;
        public PortTabItem PortTabItem
        {
            get { return _PortTabItem; }
            set { _PortTabItem = value; OnPropertyChanged(); }
        }

        public SideComPort(string name, bool connected)
        {
            Name = name;
            Connected = connected;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
