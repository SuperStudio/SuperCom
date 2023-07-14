using SuperCom.Config;
using SuperUtils.Common;
using SuperUtils.Framework.ORM.Attributes;
using SuperUtils.Framework.ORM.Enums;
using SuperUtils.Framework.ORM.Mapper;
using SuperUtils.WPF.VieModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace SuperCom.Entity
{

    [Table(tableName: "short_cut")]
    public class ShortCutBinding : ViewModelBase
    {
        [TableId(IdType.AUTO)]
        public long _ID { get; set; }
        public long KeyID { get; set; }
        public string KeyName { get; set; }
        public string Keys { get; set; }


        /// <summary>
        /// 保存的 KeyCode
        /// </summary>

        [TableField(exist: false)]
        public List<int> KeyCodeList { get; set; }
        [TableField(exist: false)]
        public List<Key> KeyList { get; set; }


        /// <summary>
        /// 保存 Key 对应的可读名称
        /// </summary>
        [TableField(exist: false)]
        public ObservableCollection<string> KeyStringList { get; set; }




        public static List<ShortCutBinding> SHORT_CUT_BINDINGS = new List<ShortCutBinding>()
        {
            new ShortCutBinding(1,"关闭/打开当前串口",new List<Key>(){ Key.LeftCtrl,Key.Q }),
            new ShortCutBinding(2,"收起/展开发送栏",new List<Key>(){ Key.LeftCtrl,Key.T }),
            new ShortCutBinding(3,"全屏",new List<Key>(){ Key.LeftAlt,Key.Q }),
            new ShortCutBinding(4,"固定/滚屏",new List<Key>(){ Key.LeftAlt,Key.W }),
            new ShortCutBinding(5,"HEX转换",new List<Key>(){ Key.LeftAlt,Key.E }),
            new ShortCutBinding(6,"时间戳转换",new List<Key>(){ Key.LeftAlt,Key.D }),
            new ShortCutBinding(7,"格式化为JSON",new List<Key>(){ Key.F2 }),
            new ShortCutBinding(8,"合并为一行",new List<Key>(){ Key.F3 }),
        };


        public void RefreshKeyList()
        {
            KeyCodeList = new List<int>();
            KeyStringList = new ObservableCollection<string>();
            if (!string.IsNullOrEmpty(this.Keys)) {
                foreach (var item in this.Keys.Split(',')) {
                    bool s = int.TryParse(item, out int key);
                    if (s) {
                        KeyCodeList.Add(key);
                        Key k = (Key)key;
                        KeyStringList.Add(KeyBoardHelper.KeyToString(k).RemoveKeyDiff());
                    }
                }
            }
            if (KeyCodeList.Count > 0)
                KeyList = KeyCodeList.Select(arg => (Key)arg).ToList();
            else
                KeyList = new List<Key>();
        }


        // 必须要有无参构造器
        public ShortCutBinding()
        {

        }
        public ShortCutBinding(long id, string name, IEnumerable<Key> keyList)
        {
            this.KeyID = id;
            this.KeyName = name;
            this.KeyList = new List<Key>();
            if (keyList != null) {
                this.KeyCodeList = new List<int>();
                this.KeyStringList = new ObservableCollection<string>();
                foreach (var key in keyList) {
                    this.KeyCodeList.Add((int)key);
                    this.KeyStringList.Add(KeyBoardHelper.KeyToString(key).RemoveKeyDiff());
                }
                this.Keys = string.Join(",", KeyCodeList);
                if (KeyCodeList.Count > 0)
                    this.KeyList = KeyCodeList.Select(arg => (Key)arg).ToList();
            }
        }


        public static class SqliteTable
        {
            public static Dictionary<string, string> Table = new Dictionary<string, string>()
            {
                {
                    "short_cut",
                    "create table if not exists short_cut( " +
                        "_ID INTEGER PRIMARY KEY autoincrement, " +
                        "KeyID INTEGER, " +
                        "KeyName VARCHAR(200), " +
                        "Keys TEXT, " +
                        "CreateDate VARCHAR(30) DEFAULT(STRFTIME('%Y-%m-%d %H:%M:%S', 'NOW', 'localtime')), " +
                        "UpdateDate VARCHAR(30) DEFAULT(STRFTIME('%Y-%m-%d %H:%M:%S', 'NOW', 'localtime'))," +
                        "unique(KeyID)" +
                    ");"
                }
            };

        }

        public static void InitSqlite()
        {
            SqliteMapper<ShortCutBinding> mapper = new SqliteMapper<ShortCutBinding>(ConfigManager.SQLITE_DATA_PATH);
            foreach (var item in SqliteTable.Table.Keys) {
                if (!mapper.IsTableExists(item)) {
                    mapper.CreateTable(item, SqliteTable.Table[item]);
                }
            }
            // 插入数据
            List<ShortCutBinding> shotCutBindings = mapper.SelectList();
            List<long> list = shotCutBindings.Select(arg => arg.KeyID).ToList();
            foreach (var item in SHORT_CUT_BINDINGS) {
                if (!list.Contains(item.KeyID)) {
                    mapper.Insert(item);
                }
            }
        }

        public override bool Equals(object obj)
        {
            if (obj != null && obj is ShortCutBinding) {
                return this.Equals((obj as ShortCutBinding).KeyID);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return this.KeyID.GetHashCode();
        }
    }


}
