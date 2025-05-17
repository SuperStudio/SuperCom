
using SuperCom.Config;
using SuperCom.Entity;
using SuperCom.Entity.Enums;
using SuperUtils.Common;
using SuperUtils.Framework.ORM.Mapper;
using SuperUtils.IO;
using SuperUtils.WPF.VieModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows.Media;
using static SuperCom.App;
using static SuperCom.Entity.HighLightRule;

namespace SuperCom.ViewModel
{

    public class VieModel_Setting : ViewModelBase
    {
        public VieModel_Setting()
        {
            Init();
        }

        #region "属性"

        private ObservableCollection<string> _BaudRates;
        public ObservableCollection<string> BaudRates {
            get { return _BaudRates; }
            set { _BaudRates = value; RaisePropertyChanged(); }
        }

        private bool _FixedOnSearch = ConfigManager.CommonSettings.FixedOnSearch;
        public bool FixedOnSearch {
            get { return _FixedOnSearch; }
            set { _FixedOnSearch = value; RaisePropertyChanged(); }
        }

        private bool _CloseToBar = ConfigManager.CommonSettings.CloseToBar;
        public bool CloseToBar {
            get { return _CloseToBar; }
            set { _CloseToBar = value; RaisePropertyChanged(); }
        }

        private bool _FixedOnSendCommand = ConfigManager.CommonSettings.FixedOnSendCommand;
        public bool FixedOnSendCommand {
            get { return _FixedOnSendCommand; }
            set { _FixedOnSendCommand = value; RaisePropertyChanged(); }
        }

        private bool _ScrollOnSearchClosed = ConfigManager.CommonSettings.ScrollOnSearchClosed;
        public bool ScrollOnSearchClosed {
            get { return _ScrollOnSearchClosed; }
            set { _ScrollOnSearchClosed = value; RaisePropertyChanged(); }
        }

        private string _LogNameFormat = ConfigManager.CommonSettings.LogNameFormat;
        public string LogNameFormat {
            get { return _LogNameFormat; }
            set { _LogNameFormat = value; RaisePropertyChanged(); }
        }

        private string _LogSaveDir = ConfigManager.CommonSettings.LogSaveDir;
        public string LogSaveDir {
            get { return _LogSaveDir; }
            set { _LogSaveDir = value; RaisePropertyChanged(); }
        }

        private int _TabSelectedIndex = (int)ConfigManager.CommonSettings.TabSelectedIndex;
        public int TabSelectedIndex {
            get { return _TabSelectedIndex; }
            set {
                _TabSelectedIndex = value;
                RaisePropertyChanged();
                SaveValue();
            }
        }

        private string _CurrentLanguage = ConfigManager.Settings.CurrentLanguage;
        public string CurrentLanguage {
            get { return _CurrentLanguage; }

            set {
                _CurrentLanguage = value;
                RaisePropertyChanged();
            }
        }

        private bool _AvoidScreenClose = ConfigManager.Settings.AvoidScreenClose;
        public bool AvoidScreenClose {
            get { return _AvoidScreenClose; }

            set {
                _AvoidScreenClose = value;
                RaisePropertyChanged();

            }
        }



        private bool _AutoBackup = ConfigManager.Settings.AutoBackup;

        public bool AutoBackup {
            get { return _AutoBackup; }

            set {
                _AutoBackup = value;
                RaisePropertyChanged();
            }
        }

        private int _AutoBackupPeriodIndex = (int)ConfigManager.Settings.AutoBackupPeriodIndex;

        public int AutoBackupPeriodIndex {
            get { return _AutoBackupPeriodIndex; }

            set {
                _AutoBackupPeriodIndex = value;
                RaisePropertyChanged();
            }
        }



        #endregion

        #region "语法高亮"


        private ObservableCollection<HighLightRule> _HighLightRules;
        public ObservableCollection<HighLightRule> HighLightRules {
            get { return _HighLightRules; }
            set {
                _HighLightRules = value;
                RaisePropertyChanged();
                SaveValue();
            }
        }
        private int _HighLightSideIndex = (int)ConfigManager.CommonSettings.HighLightSideIndex;
        public int HighLightSideIndex {
            get { return _HighLightSideIndex; }
            set { _HighLightSideIndex = value; RaisePropertyChanged(); }
        }
        private bool _ShowCurrentRule = false;
        public bool ShowCurrentRule {
            get { return _ShowCurrentRule; }
            set { _ShowCurrentRule = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<RuleSet> _RuleSets;

        public ObservableCollection<RuleSet> RuleSets {
            get { return _RuleSets; }
            set {
                _RuleSets = value;
                RaisePropertyChanged();
            }
        }
        private long _CurrentRuleID;

        public long CurrentRuleID {
            get { return _CurrentRuleID; }
            set {
                _CurrentRuleID = value;
                RaisePropertyChanged();
            }
        }

        public void NewRule(string ruleName = "我的规则")
        {
            HighLightRule rule = new HighLightRule();
            rule.RuleName = ruleName;
            rule.FileName = "";
            MapperManager.RuleMapper.InsertAndGetID(rule);
            HighLightRules.Add(rule);

            Logger.Info($"new rule, name: {ruleName}");
        }


        public bool DeleteRule(long id)
        {

            Logger.Info($"delete rule, id: {id}");

            int count = MapperManager.RuleMapper.DeleteById(id);
            if (count <= 0) {
                return false;
            }
            int idx = -1;
            for (int i = 0; i < HighLightRules.Count; i++) {
                if (HighLightRules[i].RuleID == id) {
                    idx = i;
                    break;
                }
            }
            if (idx >= 0 && idx < HighLightRules.Count) {
                FileHelper.TryMoveToRecycleBin(HighLightRules[idx].GetFullFileName());
                HighLightRules.RemoveAt(idx);
            }
            if (HighLightRules.Count == 0)
                ShowCurrentRule = false;
            else
                HighLightSideIndex = 0;



            return true;
        }


        public void NewRuleSet()
        {
            if (RuleSets == null)
                RuleSets = new System.Collections.ObjectModel.ObservableCollection<RuleSet>();

            RuleSet ruleSet = new RuleSet();
            ruleSet.RuleSetID = RuleSet.GenerateID(RuleSets.Select(arg => arg.RuleSetID).ToList());
            ruleSet.Bold = false;
            ruleSet.Italic = false;
            ruleSet.RuleType = RuleType.Regex;
            ruleSet.RuleValue = "";
            ruleSet.Foreground = "#FFFFFF";


            HighLightRule rule = HighLightRules.Where(arg => arg.RuleID == CurrentRuleID).FirstOrDefault();
            if (rule != null) {
                if (rule.RuleSetList == null)
                    rule.RuleSetList = new List<RuleSet>();
                if (!string.IsNullOrEmpty(rule.RuleSetString)) {
                    rule.RuleSetList = JsonUtils.TryDeserializeObject<List<RuleSet>>(rule.RuleSetString);
                }


                rule.RuleSetList.Add(ruleSet);
                rule.RuleSetString = JsonUtils.TrySerializeObject(rule.RuleSetList);
                UpdateRule(rule);
                RuleSets.Add(ruleSet);

                Logger.Info("add new rule set");
            }

        }

        public void SetRuleSetColor(Color color, long id)
        {
            for (int i = 0; i < RuleSets.Count; i++) {
                if (RuleSets[i].RuleSetID == id) {
                    RuleSets[i].Foreground = color.ToString();
                    break;
                }
            }
        }


        public void UpdateRule(HighLightRule rule)
        {

            int count = MapperManager.RuleMapper.UpdateById(rule);
            if (count <= 0) {
                Console.WriteLine($"重命名 {rule.RuleName} 失败");
            }

        }

        public void SaveAllRule()
        {
            HighLightRule.AllRules = new List<HighLightRule>();
            if (HighLightRules.Count > 0) {
                foreach (var item in HighLightRules) {
                    item.SetFileName();
                    item.WriteToXshd();
                    MapperManager.RuleMapper.UpdateById(item);
                }
                AllRules = MapperManager.RuleMapper.SelectList();
            }
        }

        public void RenameRule(HighLightRule rule)
        {
            bool result = MapperManager.RuleMapper.UpdateFieldById("RuleName", rule.RuleName, rule.RuleID);
            if (!result) {
                Console.WriteLine($"重命名 {rule.RuleName} 失败");
            }

        }



        #endregion

        public override void Init()
        {
            MapperManager.RuleMapper = new SqliteMapper<HighLightRule>(ConfigManager.SQLITE_DATA_PATH);
            LoadBaudRates();
            LoadHighLightRules();
            RuleSets = new ObservableCollection<RuleSet>();
        }


        public void SaveValue()
        {
            ConfigManager.CommonSettings.TabSelectedIndex = TabSelectedIndex;
            ConfigManager.CommonSettings.HighLightSideIndex = HighLightSideIndex;
            ConfigManager.CommonSettings.Save();
        }


        public void LoadHighLightRules()
        {
            List<HighLightRule> highLightRules = MapperManager.RuleMapper.SelectList();
            HighLightRules = new ObservableCollection<HighLightRule>();
            foreach (var item in highLightRules) {
                HighLightRules.Add(item);
            }
            Logger.Info("load highlight rules");
        }

        public void LoadBaudRates()
        {
            BaudRates = new ObservableCollection<string>();
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
        }
    }
}