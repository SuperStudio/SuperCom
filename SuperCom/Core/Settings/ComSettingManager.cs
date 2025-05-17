using SuperCom.Config;
using SuperCom.Core.Events;
using SuperCom.Entity;
using SuperUtils.Common;
using SuperUtils.WPF.VieModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static SuperCom.App;

namespace SuperCom.Core.Settings
{
    public class ComSettingManager : ViewModelBase
    {
        public const string DEFAULT_ADD_TEXT = "新增";

        private ComSettingManager()
        {

        }


        private ObservableCollection<string> _BaudRates = new ObservableCollection<string>();
        public ObservableCollection<string> BaudRates {
            get { return _BaudRates; }
            set {
                _BaudRates = value;
                RaisePropertyChanged();
            }
        }
        private List<ComSettings> ComSettingList;

        public List<ComSettings> GetComSettings()
        {
            if (ComSettingList == null)
                ComSettingList = MapperManager.ComMapper.SelectList();

            return ComSettingList;
        }

        public ComSettings GetComSetting(string name)
        {
            return GetComSettings().FirstOrDefault(x => x.PortName == name);
        }

        public ComSettingManager Instance = InstanceHolder._Instance;

        public static ComSettingManager CreateInstance()
        {
            return InstanceHolder._Instance;
        }

        private static class InstanceHolder
        {
            public static ComSettingManager _Instance = new ComSettingManager();
        }

        public void SaveBaudRate()
        {
            List<string> baudrates = BaudRates.Where(x => !x.Equals(DEFAULT_ADD_TEXT)).ToList();
            ConfigManager.Main.CustomBaudRates = JsonUtils.TrySerializeObject(baudrates);
            ConfigManager.Main.Save();
        }

        public void ReLoadBaudRates()
        {
            Logger.Info("ReLoadBaudRates");
            BaudRates.Clear();
            List<string> baudrates = PortSetting.GetAllBaudRates();
            foreach (var item in baudrates) {
                BaudRates.Add(item);
            }
            string value = ConfigManager.Main.CustomBaudRates;
            if (!string.IsNullOrEmpty(value)) {
                List<string> list = JsonUtils.TryDeserializeObject<List<string>>(value);
                if (list?.Count > 0) {
                    foreach (var item in list)
                        BaudRates.Add(item);
                }
            }
            ConfigManager.Main.CustomBaudRates = JsonUtils.TrySerializeObject(BaudRates);
            ConfigManager.Main.Save();
            BaudRates.Add(DEFAULT_ADD_TEXT);
            BasicEventManager.SendEvent(EventType.NotifyTabItemBaudRate, null);
        }

        public override void Init()
        {
            ReLoadBaudRates();
        }
    }
}
