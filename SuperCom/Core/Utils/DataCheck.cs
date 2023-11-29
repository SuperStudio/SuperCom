using SuperUtils.Common;
using SuperUtils.WPF.VieModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperCom.Core.Utils
{
    public class DataCheck : ViewModelBase
    {
        public DataCheck()
        {

        }

        private bool _Enabled;
        public bool Enabled {
            get { return _Enabled; }
            set {
                _Enabled = value;
                RaisePropertyChanged();
            }
        }

        private int _SelectedIndex;
        public int SelectedIndex {
            get { return _SelectedIndex; }
            set {
                _SelectedIndex = value;
                RaisePropertyChanged();
            }
        }

        private bool _UseCustom;
        public bool UseCustom {
            get { return _UseCustom; }
            set {
                _UseCustom = value;
                RaisePropertyChanged();
            }
        }
        private int _CustomStart;
        public int CustomStart {
            get { return _CustomStart; }
            set {
                _CustomStart = value;
                RaisePropertyChanged();
            }
        }
        private int _CustomEnd;
        public int CustomEnd {
            get { return _CustomEnd; }
            set {
                _CustomEnd = value;
                RaisePropertyChanged();
            }
        }
        private int _CustomInsert;
        public int CustomInsert {
            get { return _CustomInsert; }
            set {
                _CustomInsert = value;
                RaisePropertyChanged();
            }
        }


        public static DataCheck FromJson(string json)
        {
            DataCheck dataCheck = new DataCheck();
            if (string.IsNullOrEmpty(json))
                return dataCheck;
            Dictionary<string, object> data = JsonUtils.TryDeserializeObject<Dictionary<string, object>>(json);
            if (data == null)
                return dataCheck;

            dataCheck.Enabled = data.GetBool("Enabled", false);
            dataCheck.SelectedIndex = data.GetInt("SelectedIndex", 0);
            dataCheck.UseCustom = data.GetBool("UseCustom", false);
            dataCheck.CustomStart = data.GetInt("CustomStart", 0);
            dataCheck.CustomEnd = data.GetInt("CustomEnd", 0);
            dataCheck.CustomInsert = data.GetInt("CustomInsert", 0);
            return dataCheck;
        }

        public override void Init()
        {
            throw new NotImplementedException();
        }
    }
}
