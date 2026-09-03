
using SuperCom.Core.Entity.Enums;
using SuperUtils.Common;
using SuperUtils.WPF.VieModel;
using static SuperCom.App;

namespace SuperCom.Entity
{

    public class SideComPort : ViewModelBase
    {

        private const string VIRTUAL_TAG = "virtual";
        private const string USB_TAG = "usb";
        private static string[] BLE_STRING = {
            "蓝牙",
            "ble",
            "bluetooth low energy" ,
            "bluetooth smart",
            "bluetooth le"
        };

        #region "属性"
        private string _Name;
        public string Name {
            get { return _Name; }
            set { _Name = value; RaisePropertyChanged(); }
        }

        private string _Detail;
        public string Detail {
            get { return _Detail; }
            set {
                _Detail = value;
                RaisePropertyChanged();
                PortType = GetPortType();
            }
        }

        private PortType _PortType;
        public PortType PortType {
            get { return _PortType; }
            set { _PortType = value; RaisePropertyChanged(); }
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

        private PortType GetPortType()
        {
            if (string.IsNullOrEmpty(Detail))
                return PortType.None;

            string detail = Detail.ToLower().Trim();
            if (detail.IndexOf(VIRTUAL_TAG) >= 0)
                return PortType.Virtual;
            if (detail.IndexOf(USB_TAG) >= 0)
                return PortType.USBSerial;
            if (detail.IndexOfAnyString(BLE_STRING) >= 0)
                return PortType.BLE;
            return PortType.None;
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
