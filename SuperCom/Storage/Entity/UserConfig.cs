using SuperUtils.Framework.ORM.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperCom.Storage.Entity
{

    [Table(tableName: "userconfig")]
    public class UserConfig
    {
        // 自增 ID
        [TableId(IdType.AUTO)]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
    }
}
