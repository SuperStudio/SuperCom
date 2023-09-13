
using SuperUtils.WPF.VieModel;
using static SuperCom.App;

namespace SuperCom.Entity
{

    public class SideComPort : ViewModelBase
    {

        #region "属性"
        private string _Name;
        public string Name {
            get { return _Name; }
            set { _Name = value; RaisePropertyChanged(); }
        }

        private bool _Connected;
        public bool Connected {
            get { return _Connected; }
            set { _Connected = value; RaisePropertyChanged(); }
        }

        private PortTabItem _PortTabItem;
        public PortTabItem PortTabItem {
            get { return _PortTabItem; }
            set { _PortTabItem = value; RaisePropertyChanged(); }
        }

        private string _Remark;
        public string Remark {
            get { return _Remark; }
            set { _Remark = value; RaisePropertyChanged(); }
        }

        private bool _Hide;
        public bool Hide {
            get { return _Hide; }
            set {
                _Hide = value;
                RaisePropertyChanged();
                Logger.Info($"set port[{Name}] hide: {value}");
            }
        }

        #endregion

        public SideComPort(string name, bool connected)
        {
            Name = name;
            Connected = connected;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SideComPort))
                return false;
            return this.Name.Equals((obj as SideComPort).Name);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override void Init()
        {
            throw new System.NotImplementedException();
        }
    }
}
