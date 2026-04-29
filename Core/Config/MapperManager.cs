using Newtonsoft.Json.Linq;
using SuperCom.Entity;
using SuperControls.Style;
using SuperUtils.Framework.ORM.Mapper;
using System;
using System.Collections.Generic;

namespace SuperCom.Config
{
    public static class MapperManager
    {

        public class MapperConfig
        {

            public string Name { get; set; }
            public Func<List<JObject>> GetAllData { get; set; }
            public Func<List<JObject>, bool> ImportAllData { get; set; }


            public MapperConfig(string name, Func<List<JObject>> getAllData, Func<List<JObject>, bool> importAllData)
            {
                Name = name;
                GetAllData = getAllData;
                ImportAllData = importAllData;
            }
        }

        public static Dictionary<string, MapperConfig> ALL_TABLE { get; set; }

        public static SqliteMapper<AdvancedSend> AdvancedSendMapper { get; set; }
        public static SqliteMapper<ComSettings> ComMapper { get; set; }
        public static SqliteMapper<ShortCutBinding> ShortCutMapper { get; set; }
        public static SqliteMapper<HighLightRule> RuleMapper { get; set; }
        public static SqliteMapper<VarMonitor> MonitorMapper { get; set; }

        static MapperManager()
        {
            AdvancedSendMapper = new SqliteMapper<AdvancedSend>(ConfigManager.SQLITE_DATA_PATH);
            ComMapper = new SqliteMapper<ComSettings>(ConfigManager.SQLITE_DATA_PATH);
            ShortCutMapper = new SqliteMapper<ShortCutBinding>(ConfigManager.SQLITE_DATA_PATH);
            RuleMapper = new SqliteMapper<HighLightRule>(ConfigManager.SQLITE_DATA_PATH);
            MonitorMapper = new SqliteMapper<VarMonitor>(ConfigManager.SQLITE_DATA_PATH);

            ALL_TABLE = new Dictionary<string, MapperConfig>()
            {
                {"advanced_send",new MapperConfig(LangManager.GetValueByKey("SendCommand"),AdvancedSendMapper.SelectAllAsJson ,AdvancedSendMapper.ImportAllJson) },
                {"com_settings",new MapperConfig(LangManager.GetValueByKey("PortSettings"),ComMapper.SelectAllAsJson ,ComMapper.ImportAllJson) },
                {"short_cut",new MapperConfig(LangManager.GetValueByKey("FastKey"),ShortCutMapper.SelectAllAsJson ,ShortCutMapper.ImportAllJson) },
                {"highlight_rule",new MapperConfig(LangManager.GetValueByKey("HighLighting"),RuleMapper.SelectAllAsJson ,RuleMapper.ImportAllJson) },
                //{"var_monitor",new MapperConfig<VarMonitor>(LangManager.GetValueByKey("Monitor"),MonitorMapper,MonitorMapper.GetType()) },
            };
        }
    }
}
