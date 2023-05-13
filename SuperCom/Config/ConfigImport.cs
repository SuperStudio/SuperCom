using Newtonsoft.Json.Linq;
using SuperControls.Style;
using SuperUtils.Common;
using System.Collections.Generic;
using static SuperCom.App;
using static SuperCom.Config.MapperManager;

namespace SuperCom.Config
{

    /// <summary>
    /// 配置导入导出
    /// </summary>
    public class ConfigImport
    {

        public static List<DataBaseInfo> GetCurrentDataBaseInfo()
        {
            List<DataBaseInfo> result = new List<DataBaseInfo>();
            int num = 0;
            int count = ALL_TABLE.Count;
            foreach (var item in ALL_TABLE)
            {
                string tableName = item.Key;
                string name = item.Value.Name;
                DataBaseInfo info = new DataBaseInfo();
                info.TableName = tableName;
                info.Name = name;
                info.FilePath = ConfigManager.SQLITE_DATA_PATH;
                info.Enabled = true;
                result.Add(info);

                Logger.Info($"[{++num}/{count}] add table, tableName: {tableName}, name: {name}");
            }
            return result;
        }


        public static bool ImportDataBaseInfo(List<DataBaseInfo> dataBaseInfos, string data)
        {

            if (dataBaseInfos == null || dataBaseInfos.Count == 0)
                return false;

            Dictionary<string, List<JObject>> dict = JsonUtils.TryDeserializeObject<Dictionary<string, List<JObject>>>(data);
            if (dict == null || dict.Count == 0)
                return false;

            int num = 0;
            int count = dict.Count;

            bool result = true;

            foreach (var item in dataBaseInfos)
            {
                if (!item.Enabled || string.IsNullOrEmpty(item.TableName))
                    continue;

                if (!dict.ContainsKey(item.TableName))
                    continue;


                result &= ALL_TABLE[item.TableName].ImportAllData(dict[item.TableName]);

                Logger.Info($"[{++num}/{count}] import data, tableName: {item.TableName}, name: {item.Name}, result: {result}");
            }

            return result;
        }

        public static string ExportDataBaseInfo(List<DataBaseInfo> dataBaseInfo)
        {
            if (dataBaseInfo == null || dataBaseInfo.Count == 0)
                return "";

            Dictionary<string, object> dict = new Dictionary<string, object>();


            foreach (var item in dataBaseInfo)
            {

                if (!item.Enabled)
                    continue;

                string tableName = item.TableName;

                if (dict.ContainsKey(tableName))
                    continue;

                if (!ALL_TABLE.ContainsKey(tableName))
                    continue;

                MapperConfig mapperConfig = ALL_TABLE[tableName];
                List<JObject> jObjects = mapperConfig.GetAllData();
                if (jObjects == null)
                    continue;

                dict.Add(item.TableName, jObjects);
                Logger.Info($"import data, tableName: {item.TableName}, name: {item.Name}, enabled: {item.Enabled}");
            }

            return JsonUtils.TrySerializeObject(dict);
        }

        public static List<DataBaseInfo> ParseInfo(string data)
        {
            if (string.IsNullOrEmpty(data))
                return null;

            Dictionary<string, object> dict = JsonUtils.TryDeserializeObject<Dictionary<string, object>>(data);
            if (dict == null || dict.Count == 0)
                return null;

            List<DataBaseInfo> result = new List<DataBaseInfo>();

            foreach (var item in dict)
            {
                if (!ALL_TABLE.ContainsKey(item.Key))
                    continue;

                DataBaseInfo info = new DataBaseInfo();
                info.TableName = item.Key;
                info.Enabled = true;
                info.Name = ALL_TABLE[item.Key].Name;
                result.Add(info);
            }
            return result;
        }

    }
}
