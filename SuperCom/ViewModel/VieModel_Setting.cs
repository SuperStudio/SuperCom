using GalaSoft.MvvmLight;
using SuperCom.Config;
using SuperCom.Entity;
using SuperCom.Log;
using SuperUtils.Common;
using SuperUtils.Framework.ORM.Mapper;
using SuperUtils.WPF.VisualTools;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Windows;

namespace SuperCom.ViewModel
{

    public class VieModel_Setting : ViewModelBase
    {

        private ObservableCollection<string> _BaudRates;
        public ObservableCollection<string> BaudRates
        {
            get { return _BaudRates; }
            set { _BaudRates = value; RaisePropertyChanged(); }
        }
        private bool _FixedOnSearch = ConfigManager.CommonSettings.FixedOnSearch;
        public bool FixedOnSearch
        {
            get { return _FixedOnSearch; }
            set { _FixedOnSearch = value; RaisePropertyChanged(); }
        }
        private bool _FixedOnSendCommand = ConfigManager.CommonSettings.FixedOnSendCommand;
        public bool FixedOnSendCommand
        {
            get { return _FixedOnSendCommand; }
            set { _FixedOnSendCommand = value; RaisePropertyChanged(); }
        }
        private bool _ScrollOnSearchClosed = ConfigManager.CommonSettings.ScrollOnSearchClosed;
        public bool ScrollOnSearchClosed
        {
            get { return _ScrollOnSearchClosed; }
            set { _ScrollOnSearchClosed = value; RaisePropertyChanged(); }
        }
        private string _LogNameFormat = ConfigManager.CommonSettings.LogNameFormat;
        public string LogNameFormat
        {
            get { return _LogNameFormat; }
            set { _LogNameFormat = value; RaisePropertyChanged(); }
        }
        private int _TabSelectedIndex = ConfigManager.CommonSettings.TabSelectedIndex;
        public int TabSelectedIndex
        {
            get { return _TabSelectedIndex; }
            set
            {
                _TabSelectedIndex = value;
                RaisePropertyChanged();
                ConfigManager.CommonSettings.TabSelectedIndex = value;
                ConfigManager.CommonSettings.Save();
            }
        }





        public VieModel_Setting()
        {
            Init();
        }

        public void Init()
        {
            LoadBaudRates();
        }

        public void LoadBaudRates()
        {
            BaudRates = new ObservableCollection<string>();
            List<string> baudrates = PortSetting.GetAllBaudRates();
            foreach (var item in baudrates)
            {
                BaudRates.Add(item);
            }
            string value = ConfigManager.Main.CustomBaudRates;
            if (!string.IsNullOrEmpty(value))
            {
                List<string> list = JsonUtils.TryDeserializeObject<List<string>>(value);
                if (list?.Count > 0)
                {
                    foreach (var item in list)
                        BaudRates.Add(item);
                }
            }
        }

        static VieModel_Setting()
        {

        }


    }
}