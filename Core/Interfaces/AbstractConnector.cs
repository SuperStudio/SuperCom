using ICSharpCode.AvalonEdit;
using SuperUtils.WPF.VieModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SuperCom.App;

namespace SuperCom.Core.Interfaces
{
    /// <summary>
    /// 抽象连接处理类
    /// </summary>
    public abstract class AbstractConnector : ViewModelBase
    {
        private string _Name;
        public string Name {
            get { return _Name; }
            set { _Name = value; RaisePropertyChanged(); }
        }
        public bool _Connected;
        public bool Connected {
            get { return _Connected; }
            set {
                _Connected = value;
                RaisePropertyChanged();
            }
        }

        private bool _Selected;
        public bool Selected {
            get { return _Selected; }
            set { _Selected = value; RaisePropertyChanged(); }
        }

        private long _RX = 0L;
        public long RX {
            get { return _RX; }
            set { _RX = value; RaisePropertyChanged(); }
        }

        private long _TX = 0L;
        public long TX {
            get { return _TX; }
            set { _TX = value; RaisePropertyChanged(); }
        }

        private string _Remark = "";

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark {
            get { return _Remark; }
            set { _Remark = value; RaisePropertyChanged(); }
        }

        private DateTime _ConnectTime;
        public DateTime ConnectTime {
            get { return _ConnectTime; }
            set {
                _ConnectTime = value;
            }
        }


        private bool _Pinned;
        public bool Pinned {
            get { return _Pinned; }
            set {
                _Pinned = value;
                RaisePropertyChanged();
            }
        }

        private bool _FixedText;
        public bool FixedText {
            get { return _FixedText; }
            set {
                _FixedText = value;
                RaisePropertyChanged();
                if (TextEditor != null) {
                    if (value)
                        TextEditor.TextChanged -= TextBox_TextChanged;
                    else
                        TextEditor.TextChanged += TextBox_TextChanged;
                    Logger.Info($"fixed text: {value}");
                }
            }
        }

        public TextEditor TextEditor { get; set; }

        public double CurrentCharSize { get; set; }

        public int FragCount { get; set; }

        public string SaveFileName { get; set; }

        public void ClearData()
        {

        }

        public void TextBox_TextChanged(object sender, EventArgs e)
        {
            TextEditor textEditor = sender as TextEditor;
            textEditor?.ScrollToEnd();
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public void Open()
        {
            throw new NotImplementedException();
        }

        public string GetDefaultFileName()
        {
            throw new NotImplementedException();
        }

        public string GetCustomFileName(string name)
        {
            throw new NotImplementedException();
        }

        public override bool Equals(object obj)
        {
            if (obj is AbstractConnector other) {
                return Name.Equals(other.Name);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}
