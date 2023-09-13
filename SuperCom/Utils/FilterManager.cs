using SuperCom.Entity;
using SuperCom.Entity.Enums;
using SuperUtils.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static SuperCom.Entity.HighLightRule;

namespace SuperCom.Utils
{

    /// <summary>
    /// 用于过滤抓起的日志（开发中）
    /// </summary>
    public class FilterManager
    {


        #region "静态属性"

        private static List<RuleSet> FilterRuleSet { get; set; }

        #endregion

        private ConcurrentQueue<string> FilterQueue { get; set; } = new ConcurrentQueue<string>();

        private bool StopFilter { get; set; } = false;
        private bool FilterRunning { get; set; } = false;

        private SerialPortEx SerialPort { get; set; }

        public Action<string> onFilter { get; set; }

        public void Filter()
        {
            if (FilterRunning)
                return;
            StopFilter = false;
            FilterRunning = true;
            Task.Run(async () => {
                while (true) {
                    if (!FilterQueue.IsEmpty) {
                        bool success = FilterQueue.TryDequeue(out string data);
                        if (!success) {
                            Console.WriteLine("取队列元素失败");
                            continue;
                        }

                        if (IsInFilterRule(data)) {
                            App.Current.Dispatcher.Invoke(() => {
                                onFilter?.Invoke(data);
                            });
                        } else {
                            Console.WriteLine($"过滤了：{data}");
                        }
                    } else {
                        await Task.Delay(100);
                        //Console.WriteLine("过滤中...");
                    }
                    if (StopFilter)
                        break;
                }
                FilterRunning = false;
            });
        }

        private static List<RuleSet> GetFilterRuleSet(int index)
        {
            if (HighLightRule.AllRules == null || HighLightRule.AllRules.Count == 0) {
                return HighLightRule.DefaultRuleSet;
            } else if (index < HighLightRule.AllName.Count && index >= 0) {
                string name = HighLightRule.AllName[index];
                if (name.Equals("ComLog"))
                    return HighLightRule.DefaultRuleSet;

                HighLightRule rule = HighLightRule.AllRules.FirstOrDefault(arg => arg.RuleName.Equals(name));
                if (rule == null)
                    return HighLightRule.DefaultRuleSet;
                List<RuleSet> result = new List<RuleSet>();
                if (!string.IsNullOrEmpty(rule.RuleSetString)) {
                    result = JsonUtils.TryDeserializeObject<List<RuleSet>>(rule.RuleSetString);
                }
                return result;
            }
            return null;
        }

        public bool IsInFilterRule(string line)
        {
            // 获取当前过滤器
            if (SerialPort == null || string.IsNullOrEmpty(line))
                return true;
            int index = (int)SerialPort.HighLightIndex;
            FilterRuleSet = GetFilterRuleSet(index);
            if (FilterRuleSet == null || FilterRuleSet.Count == 0)
                return true;
            foreach (var item in FilterRuleSet) {
                if (item.RuleType == RuleType.Regex) {
                    try {
                        if (!string.IsNullOrEmpty(item.RuleValue) &&
                            Regex.IsMatch(line, item.RuleValue))
                            return true;
                    } catch (Exception ex) {
                        App.Logger.Error(ex.Message);
                        continue;
                    }

                } else if (item.RuleType == RuleType.KeyWord && !string.IsNullOrEmpty(item.RuleValue)) {
                    if (line.IndexOf(item.RuleValue) >= 0)
                        return true;
                }
            }

            return false;
        }

        public void StopFilterTask()
        {
            StopFilter = true;
        }
    }
}
