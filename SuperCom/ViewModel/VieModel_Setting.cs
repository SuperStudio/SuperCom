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
using System.Windows.Media;
using static SuperCom.Entity.HighLightRule;

namespace SuperCom.ViewModel
{

    public class VieModel_Setting : ViewModelBase
    {
        SqliteMapper<HighLightRule> ruleMapper { get; set; }


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
        private int _TabSelectedIndex = (int)ConfigManager.CommonSettings.TabSelectedIndex;
        public int TabSelectedIndex
        {
            get { return _TabSelectedIndex; }
            set
            {
                _TabSelectedIndex = value;
                RaisePropertyChanged();
                SaveValue();
            }
        }

        public void SaveValue()
        {
            ConfigManager.CommonSettings.TabSelectedIndex = TabSelectedIndex;
            ConfigManager.CommonSettings.HighLightSideIndex = HighLightSideIndex;
            ConfigManager.CommonSettings.Save();
        }


        #region "日志高亮"


        private ObservableCollection<HighLightRule> _HighLightRules;
        public ObservableCollection<HighLightRule> HighLightRules
        {
            get { return _HighLightRules; }
            set
            {
                _HighLightRules = value;
                RaisePropertyChanged(); SaveValue();
            }
        }
        private int _HighLightSideIndex = (int)ConfigManager.CommonSettings.HighLightSideIndex;
        public int HighLightSideIndex
        {
            get { return _HighLightSideIndex; }
            set { _HighLightSideIndex = value; RaisePropertyChanged(); }
        }
        private bool _ShowCurrentRule = false;
        public bool ShowCurrentRule
        {
            get { return _ShowCurrentRule; }
            set { _ShowCurrentRule = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<RuleSet> _RuleSets;

        public ObservableCollection<RuleSet> RuleSets
        {
            get { return _RuleSets; }
            set
            {
                _RuleSets = value;
                RaisePropertyChanged();
            }
        }
        private long _CurrentRuleID;

        public long CurrentRuleID
        {
            get { return _CurrentRuleID; }
            set
            {
                _CurrentRuleID = value;
                RaisePropertyChanged();
            }
        }

        public void NewRule()
        {
            HighLightRule rule = new HighLightRule();
            rule.RuleName = "自定义规则";
            rule.FileName = "";
            ruleMapper.InsertAndGetID(rule);
            HighLightRules.Add(rule);
        }


        public bool DeleteRule(long id)
        {
            int count = ruleMapper.DeleteById(id);
            if (count <= 0)
            {
                return false;
            }
            int idx = -1;
            for (int i = 0; i < HighLightRules.Count; i++)
            {
                if (HighLightRules[i].RuleID == id)
                {
                    idx = i;
                    break;
                }
            }
            if (idx >= 0 && idx < HighLightRules.Count)
                HighLightRules.RemoveAt(idx);

            return true;
        }


        public void NewRuleSet()
        {
            if (RuleSets == null) RuleSets = new System.Collections.ObjectModel.ObservableCollection<RuleSet>();

            RuleSet ruleSet = new RuleSet();
            ruleSet.RuleSetID = RuleSet.GenerateID(RuleSets.Select(arg => arg.RuleSetID).ToList());
            ruleSet.Bold = false;
            ruleSet.Italic = false;
            ruleSet.RuleType = RuleType.Regex;
            ruleSet.RuleValue = "";
            ruleSet.Foreground = "#FFFFFF";


            HighLightRule rule = HighLightRules.Where(arg => arg.RuleID == CurrentRuleID).FirstOrDefault();
            if (rule != null)
            {
                if (rule.RuleSetList == null)
                    rule.RuleSetList = new List<RuleSet>();
                if (!string.IsNullOrEmpty(rule.RuleSetString))
                {
                    rule.RuleSetList = JsonUtils.TryDeserializeObject<List<RuleSet>>(rule.RuleSetString);
                }


                rule.RuleSetList.Add(ruleSet);
                rule.RuleSetString = JsonUtils.TrySerializeObject(rule.RuleSetList);
                UpdateRule(rule);
                RuleSets.Add(ruleSet);
            }

        }

        public void SetRuleSetColor(Color color, long id)
        {
            for (int i = 0; i < RuleSets.Count; i++)
            {
                if (RuleSets[i].RuleSetID == id)
                {
                    RuleSets[i].Foreground = color.ToString();
                    break;
                }
            }
        }


        public void UpdateRule(HighLightRule rule)
        {

            int count = ruleMapper.UpdateById(rule);
            if (count <= 0)
            {
                System.Console.WriteLine($"插入 {rule.RuleName} 失败");
            }

        }



        #endregion




        public VieModel_Setting()
        {
            Init();
        }

        public void Init()
        {
            ruleMapper = new SqliteMapper<HighLightRule>(ConfigManager.SQLITE_DATA_PATH);
            LoadBaudRates();
            LoadHighLightRules();
            RuleSets = new ObservableCollection<RuleSet>();
        }

        public void LoadHighLightRules()
        {
            List<HighLightRule> highLightRules = ruleMapper.SelectList();
            HighLightRules = new ObservableCollection<HighLightRule>();
            foreach (var item in highLightRules)
            {
                HighLightRules.Add(item);
            }
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