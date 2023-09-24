
using SuperCom.Comparers;
using SuperCom.Config;
using SuperCom.Entity;
using SuperUtils.Common;
using SuperUtils.WPF.VieModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using static SuperCom.App;

namespace SuperCom.ViewModel
{

    public class VieModel_AdvancedSend : ViewModelBase
    {

        public VieModel_AdvancedSend()
        {
            Init();
        }

        #region "属性"
        public MainWindow Main { get; set; }

        public Action<bool> OnRunCommand { get; set; }

        public List<AdvancedSend> AllProjects { get; set; }

        public Dictionary<SideComPort, bool> SideComPortSelected { get; set; }


        private ObservableCollection<AdvancedSend> _CurrentProjects;
        public ObservableCollection<AdvancedSend> CurrentProjects {
            get { return _CurrentProjects; }
            set { _CurrentProjects = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<SendCommand> _SendCommands;

        public ObservableCollection<SendCommand> SendCommands {
            get { return _SendCommands; }
            set {
                _SendCommands = value;
                RaisePropertyChanged();
            }
        }

        private long _CurrentProjectID;
        public long CurrentProjectID {
            get { return _CurrentProjectID; }
            set {
                _CurrentProjectID = value;
                RaisePropertyChanged();
            }
        }

        private bool _ShowCurrentSendCommand;
        public bool ShowCurrentSendCommand {
            get { return _ShowCurrentSendCommand; }
            set {
                _ShowCurrentSendCommand = value;
                RaisePropertyChanged();
            }
        }

        private bool _RunningCommands;
        public bool RunningCommands {
            get { return _RunningCommands; }
            set {
                _RunningCommands = value;
                RaisePropertyChanged();
                OnRunCommand?.Invoke(value);
            }
        }

        private double _WindowOpacity = ConfigManager.AdvancedSendSettings.WindowOpacity;
        public double WindowOpacity {
            get { return _WindowOpacity; }
            set {
                _WindowOpacity = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<SideComPort> _SideComPorts;
        public ObservableCollection<SideComPort> SideComPorts {
            get { return _SideComPorts; }
            set { _SideComPorts = value; RaisePropertyChanged(); }
        }

        private int _SideIndex = (int)ConfigManager.AdvancedSendSettings.SideIndex;
        public int SideIndex {
            get { return _SideIndex; }
            set {
                _SideIndex = value;
                RaisePropertyChanged();
                ConfigManager.AdvancedSendSettings.SideIndex = value;
                ConfigManager.AdvancedSendSettings.Save();
            }
        }

        private int _ComPortSelectedIndex = (int)ConfigManager.AdvancedSendSettings.ComPortSelectedIndex;
        public int ComPortSelectedIndex {
            get { return _ComPortSelectedIndex; }
            set {
                _ComPortSelectedIndex = value;
                RaisePropertyChanged();
                ConfigManager.AdvancedSendSettings.ComPortSelectedIndex = value;
                ConfigManager.AdvancedSendSettings.Save();
            }
        }

        private bool _ShowLogGrid = ConfigManager.AdvancedSendSettings.ShowLogGrid;
        public bool ShowLogGrid {
            get { return _ShowLogGrid; }
            set {
                _ShowLogGrid = value;
                RaisePropertyChanged();
                ConfigManager.AdvancedSendSettings.ShowLogGrid = value;
                ConfigManager.AdvancedSendSettings.Save();
            }
        }

        private bool _LogAutoWrap = ConfigManager.AdvancedSendSettings.LogAutoWrap;
        public bool LogAutoWrap {
            get { return _LogAutoWrap; }
            set {
                _LogAutoWrap = value;
                RaisePropertyChanged();
                ConfigManager.AdvancedSendSettings.LogAutoWrap = value;
                ConfigManager.AdvancedSendSettings.Save();
            }
        }

        private double _LogOpacity = ConfigManager.AdvancedSendSettings.LogOpacity;
        public double LogOpacity {
            get { return _LogOpacity; }
            set {
                _LogOpacity = value;
                RaisePropertyChanged();
                ConfigManager.AdvancedSendSettings.LogOpacity = value;
                ConfigManager.AdvancedSendSettings.Save();
            }
        }

        #endregion

        public override void Init()
        {
            CurrentProjects = new ObservableCollection<AdvancedSend>();
            SendCommands = new ObservableCollection<SendCommand>();
            AllProjects = new List<AdvancedSend>();
            // 从数据库中读取
            if (MapperManager.AdvancedSendMapper != null) {
                AllProjects = MapperManager.AdvancedSendMapper.SelectList();
                foreach (var item in AllProjects) {
                    CurrentProjects.Add(item);
                }
            }
            foreach (Window window in App.Current.Windows) {
                if (window.Name.Equals("mainWindow")) {
                    Main = (MainWindow)window;
                    break;
                }
            }
            LoadSideComports();
        }

        public void SearchProject(string name)
        {
            CurrentProjects = new ObservableCollection<AdvancedSend>();
            if (string.IsNullOrEmpty(name)) {
                AllProjects = MapperManager.AdvancedSendMapper.SelectList();
                foreach (var item in AllProjects)
                    CurrentProjects.Add(item);
            } else {
                foreach (var item in AllProjects.Where(arg => arg.ProjectName
                    .ToLower().IndexOf(name.ToLower()) >= 0)) {
                    CurrentProjects.Add(item);
                }
            }
        }

        public void LoadAllProject()
        {
            AllProjects = MapperManager.AdvancedSendMapper.SelectList();
        }

        private void LoadSideComports()
        {
            SideComPorts = new ObservableCollection<SideComPort>();
            SideComPortSelected = new Dictionary<SideComPort, bool>();
            Dictionary<string, bool> dict = JsonUtils.TryDeserializeObject<Dictionary<string, bool>>(ConfigManager.AdvancedSendSettings.SelectedPortNamesJson);
            foreach (var item in Main?.vieModel.SideComPorts.OrderBy(arg => arg.Name, new ComPortComparer())) {
                SideComPorts.Add(item);
                bool isChecked = false;
                // 设置选中
                if (dict != null && dict.ContainsKey(item.Name))
                    isChecked = dict[item.Name];
                SideComPortSelected.Add(item, isChecked);
            }
        }

        public void UpdateProject(AdvancedSend send)
        {
            int count = MapperManager.AdvancedSendMapper.UpdateById(send);
            if (count <= 0) {
                Logger.Error($"insert error: {send.ProjectName}");
            }
        }

        public void RenameProject(AdvancedSend send)
        {
            bool result = MapperManager.AdvancedSendMapper.UpdateFieldById("ProjectName", send.ProjectName, send.ProjectID);
            if (!result) {
                Logger.Error($"update error {send.ProjectName}");
            }
        }

        public void DeleteProject(AdvancedSend send)
        {
            int count = MapperManager.AdvancedSendMapper.DeleteById(send.ProjectID);
            if (count <= 0) {
                Logger.Error($"delete error {send.ProjectName}");
            } else {
                ShowCurrentSendCommand = false;
            }
        }

        public void AddProject(string projectName)
        {
            if (string.IsNullOrEmpty(projectName))
                return;
            AdvancedSend send = new AdvancedSend();
            send.ProjectName = projectName;
            bool success = MapperManager.AdvancedSendMapper.Insert(send);
            if (success) {
                CurrentProjects.Add(send);
                AllProjects.Add(send);
                Logger.Info($"new project: {projectName}");
            }
        }
    }
}