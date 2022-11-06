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