using DeepCore.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeepMMO.Server.Logic
{
    /// <summary>
    /// 道具基类
    /// </summary>
    public class ItemTemplate : ISerializable
    {
        public int id;
    }

    /// <summary>
    /// 实体类型道具，每个道具拥有UUID
    /// </summary>
    public class EntityItem : ISerializable
    {
        public string uuid;
        public string owner_uuid;
        public int template_id;
        public DateTime create_time;
        public List<EntityItemProperty> properties;
    }
    /// <summary>
    /// 实体类型道具，特殊属性
    /// </summary>
    public class EntityItemProperty : ISerializable
    {

    }


}
