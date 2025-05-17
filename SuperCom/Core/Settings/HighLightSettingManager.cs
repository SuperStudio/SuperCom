using ICSharpCode.AvalonEdit.Highlighting;
using SuperCom.Core.Events;
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
    public class HighLightSettingManager : ViewModelBase
    {
        private ObservableCollection<IHighlightingDefinition> _HighlightingDefinitions = new ObservableCollection<IHighlightingDefinition>();
        public ObservableCollection<IHighlightingDefinition> HighlightingDefinitions {
            get { return _HighlightingDefinitions; }
            set { _HighlightingDefinitions = value; RaisePropertyChanged(); }
        }

        private static class IntanceHolder
        {
            public static HighLightSettingManager Instance = new HighLightSettingManager();
        }

        public static HighLightSettingManager CreateInstance()
        {
            return IntanceHolder.Instance;
        }

        public override void Init()
        {
            LoadDefinitions();
        }

        public void LoadDefinitions()
        {
            HighlightingDefinitions.Clear();
            HighLightRule.AllName = new List<string>();
            foreach (var item in HighlightingManager.Instance.HighlightingDefinitions) {
                HighlightingDefinitions.Add(item);
                HighLightRule.AllName.Add(item.Name);
            }
            Logger.Info("load definitions success");
        }

        private HighLightSettingManager()
        {
        }

        public IHighlightingDefinition this[int index] {
            get {
                if (index < 0 || index >= HighlightingDefinitions.Count)
                    return null;
                return HighlightingDefinitions[index];
            }
        }

        public int this[IHighlightingDefinition definition] {
            get {
                return HighlightingDefinitions.IndexOf(definition);
            }
        }
    }
}
