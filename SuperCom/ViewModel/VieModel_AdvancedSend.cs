
using SuperCom.Comparers;
using SuperCom.Config;
using SuperCom.Entity;
using SuperCom.Log;
using SuperUtils.Common;
using SuperUtils.Framework.ORM.Mapper;
using SuperUtils.WPF.VieModel;
using SuperUtils.WPF.VisualTools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Windows;

namespace SuperCom.ViewModel
{

    public class VieModel_AdvancedSend : ViewModelBase
    {

        public Action<bool> OnRunCommand { get; set; }
        private static SqliteMapper<AdvancedSend> mapper { get; set; }

        private ObservableCollection<AdvancedSend> _Projects;
        public ObservableCollection<AdvancedSend> Projects
        {
            get { return _Projects; }
            set { _Projects = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<SendCommand> _SendCommands;

        public ObservableCollection<SendCommand> SendCommands
        {
            get { return _SendCommands; }
            set
            {
                _SendCommands = value;
                RaisePropertyChanged();
            }
        }
        private long _CurrentProjectID;

        public long CurrentProjectID
        {
            get { return _CurrentProjectID; }
            set
            {
                _CurrentProjectID = value;
                RaisePropertyChanged();
            }
        }





        private bool _ShowCurrentSendCommand;

        public bool ShowCurrentSendCommand
        {
            get { return _ShowCurrentSendCommand; }
            set
            {
                _ShowCurrentSendCommand = value;
                RaisePropertyChanged();
            }
        }

        private bool _RunningCommands;
        public bool RunningCommands
        {
            get { return _RunningCommands; }
            set
            {
                _RunningCommands = value;
                RaisePropertyChanged();
                OnRunCommand?.Invoke(value);
            }
        }
        private double _WindowOpacity = ConfigManager.AdvancedSendSettings.WindowOpacity;
        public double WindowOpacity
        {
            get { return _WindowOpacity; }
            set
            {
                _WindowOpacity = value;
                RaisePropertyChanged();
            }
        }

        public Dictionary<SideComPort, bool> SideComPortSelected { get; set; }

        private ObservableCollection<SideComPort> _SideComPorts;
        public ObservableCollection<SideComPort> SideComPorts
        {
            get { return _SideComPorts; }
            set { _SideComPorts = value; RaisePropertyChanged(); }
        }
        private int _SideIndex = (int)ConfigManager.AdvancedSendSettings.SideIndex;
        public int SideIndex
        {
            get { return _SideIndex; }
            set
            {
                _SideIndex = value;
                RaisePropertyChanged();
                ConfigManager.AdvancedSendSettings.SideIndex = value;
                ConfigManager.AdvancedSendSettings.Save();
            }
        }
        private int _ComPortSelectedIndex = (int)ConfigManager.AdvancedSendSettings.ComPortSelectedIndex;
        public int ComPortSelectedIndex
        {
            get { return _ComPortSelectedIndex; }
            set
            {
                _ComPortSelectedIndex = value;
                RaisePropertyChanged();
                ConfigManager.AdvancedSendSettings.ComPortSelectedIndex = value;
                ConfigManager.AdvancedSendSettings.Save();
            }
        }
        private bool _ShowLogGrid = ConfigManager.AdvancedSendSettings.ShowLogGrid;
        public bool ShowLogGrid
        {
            get { return _ShowLogGrid; }
            set
            {
                _ShowLogGrid = value;
                RaisePropertyChanged();
                ConfigManager.AdvancedSendSettings.ShowLogGrid = value;
                ConfigManager.AdvancedSendSettings.Save();
            }
        }
        private bool _LogAutoWrap = ConfigManager.AdvancedSendSettings.LogAutoWrap;
        public bool LogAutoWrap
        {
            get { return _LogAutoWrap; }
            set
            {
                _LogAutoWrap = value;
                RaisePropertyChanged();
                ConfigManager.AdvancedSendSettings.LogAutoWrap = value;
                ConfigManager.AdvancedSendSettings.Save();
            }
        }
        private double _LogOpacity = ConfigManager.AdvancedSendSettings.LogOpacity;
        public double LogOpacity
        {
            get { return _LogOpacity; }
            set
            {
                _LogOpacity = value;
                RaisePropertyChanged();
                ConfigManager.AdvancedSendSettings.LogOpacity = value;
                ConfigManager.AdvancedSendSettings.Save();
            }
        }

        public MainWindow Main { get; set; }

        public VieModel_AdvancedSend()
        {
            Init();
        }

        static VieModel_AdvancedSend()
        {
            mapper = new SqliteMapper<AdvancedSend>(ConfigManager.SQLITE_DATA_PATH);
        }


        private void Init()
        {
            Projects = new ObservableCollection<AdvancedSend>();
            SendCommands = new ObservableCollection<SendCommand>();
            // 从数据库中读取
            if (mapper != null)
            {
                List<AdvancedSend> advancedSends = mapper.SelectList();
                foreach (var item in advancedSends)
                {
                    Projects.Add(item);
                }
            }
            foreach (Window window in App.Current.Windows)
            {
                if (window.Name.Equals("mainWindow"))
                {
                    Main = (MainWindow)window;
                    break;
                }
            }
            LoadSideComports();
        }




        private void LoadSideComports()
        {
            SideComPorts = new ObservableCollection<SideComPort>();
            SideComPortSelected = new Dictionary<SideComPort, bool>();
            Dictionary<string, bool> dict = JsonUtils.TryDeserializeObject<Dictionary<string, bool>>(ConfigManager.AdvancedSendSettings.SelectedPortNamesJson);
            foreach (var item in Main?.vieModel.SideComPorts.OrderBy(arg => arg.Name, new ComPortComparer()))
            {
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
            int count = mapper.UpdateById(send);
            if (count <= 0)
            {
                System.Console.WriteLine($"插入 {send.ProjectName} 失败");
            }
        }

        public void RenameProject(AdvancedSend send)
        {
            bool result = mapper.UpdateFieldById("ProjectName", send.ProjectName, send.ProjectID);
            if (!result)
            {
                System.Console.WriteLine($"更新 {send.ProjectName} 失败");
            }
        }

        public void DeleteProject(AdvancedSend send)
        {
            int count = mapper.DeleteById(send.ProjectID);
            if (count <= 0)
            {
                System.Console.WriteLine($"删除 {send.ProjectName} 失败");
            }
            else
            {
                ShowCurrentSendCommand = false;
            }
        }

        public void AddProject(string projectName)
        {
            if (string.IsNullOrEmpty(projectName)) return;
            AdvancedSend send = new AdvancedSend();
            send.ProjectName = projectName;
            bool success = mapper.Insert(send);
            if (success)
                Projects.Add(send);
        }

        public void SetCurrentSendCommands()
        {

        }

    }
}