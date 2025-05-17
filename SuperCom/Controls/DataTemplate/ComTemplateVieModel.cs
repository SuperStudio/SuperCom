using ICSharpCode.AvalonEdit.Highlighting;
using SuperCom.Config;
using SuperCom.Entity;
using SuperUtils.WPF.VieModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SuperCom.App;

namespace SuperCom.Controls.DataTemplate
{
    public class ComTemplateVieModel : ViewModelBase
    {

        private int _CommandsSelectIndex = (int)ConfigManager.Main.CommandsSelectIndex;

        public int CommandsSelectIndex {
            get { return _CommandsSelectIndex; }
            set {
                _CommandsSelectIndex = value;
                RaisePropertyChanged();
            }
        }


        private string _SendHistorySelectedValue = "";
        public string SendHistorySelectedValue {
            get { return _SendHistorySelectedValue; }
            set {
                _SendHistorySelectedValue = value;
                RaisePropertyChanged();
            }
        }

        public AdvancedSend CurrentAdvancedSend { get; set; }

        public ComTemplateVieModel()
        {
            Init();
        }

        public override void Init()
        {

        }
    }
}
