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

namespace SuperCom.Core.Settings
{

    public class SendCommandManager : ViewModelBase
    {
        private ObservableCollection<AdvancedSend> _SendCommandProjects = new ObservableCollection<AdvancedSend>();
        public ObservableCollection<AdvancedSend> SendCommandProjects {
            get { return _SendCommandProjects; }
            set { _SendCommandProjects = value; RaisePropertyChanged(); }
        }

        private static class IntanceHolder
        {
            public static SendCommandManager Instance = new SendCommandManager();
        }

        public static SendCommandManager CreateInstance()
        {
            return IntanceHolder.Instance;
        }

        public override void Init()
        {
            LoadSendCommands();
        }

        public void LoadSendCommands()
        {
            SendCommandProjects.Clear();
            List<AdvancedSend> advancedSends = MapperManager.AdvancedSendMapper.SelectList();
            foreach (var item in advancedSends) {
                SendCommandProjects.Add(item);
            }
        }

        private SendCommandManager()
        {
        }

        public AdvancedSend this[int index] {
            get {
                if (index < 0 || index >= SendCommandProjects.Count)
                    return null;
                return SendCommandProjects[index];
            }
        }


        public AdvancedSend this[long projectId] {
            get {
                return SendCommandProjects.FirstOrDefault(x => x.ProjectID.Equals(projectId));
            }
        }


        public AdvancedSend this[string projectId] {
            get {
                return SendCommandProjects.FirstOrDefault(x => x.ProjectID.ToString().Equals(projectId));
            }
        }

        public int GetIndex(AdvancedSend send)
        {
            return SendCommandProjects.IndexOf(send);
        }

        public int Count => SendCommandProjects.Count;

        public void UpdateProject(AdvancedSend send)
        {
            int count = MapperManager.AdvancedSendMapper.UpdateById(send);
            if (count <= 0) {
                Logger.Error($"insert error: {send.ProjectName}");
            }
        }

    }
}
