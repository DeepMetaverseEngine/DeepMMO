using DeepCore;
using DeepCore.IO;
using DeepCore.ORM;
using DeepMMO.Data;
using System;
using System.Collections.Generic;

namespace DeepMMO.Server.Logic
{
    /// <summary>
    /// 角色完整数据
    /// </summary>
    [PersistType]
    public class ServerRoleData : ISerializable, IObjectMapping
    {
        //------------------------------------------------
        /// <summary>
        /// 服务器ID.
        /// </summary>
        [PersistField]
        public string server_id;
        //------------------------------------------------
        [PersistField]
        public string uuid;
        [PersistField]
        public string digitID;
        [PersistField]
        public string name;
        [PersistField]
        public string account_uuid;
        [PersistField]
        public int role_template_id;
        [PersistField]
        public int unit_template_id;
        //------------------------------------------------
        /// <summary> zh_CN, zh_TW, en_US </summary>
        [PersistField]
        public string local_code = "zh_CN";
        //------------------------------------------------
        [PersistField]
        public int Level;
        [PersistField]
        public DateTime create_time;
        [PersistField]
        public DateTime last_login_time;
        [PersistField]
        public DateTime last_logout_time;
        //------------------------------------------------
        /// <summary>
        /// 用户权限
        /// </summary>
        [PersistField]
        public RolePrivilege privilege = RolePrivilege.User_Player;
        //------------------------------------------------
        #region area

        /// <summary>
        /// 最后场景服务地址
        /// </summary>
        [PersistField]
        public string last_area_name;
        [PersistField]
        public string last_area_node;
        /// <summary>
        /// 最后存在场景UUID
        /// </summary>
        [PersistField]
        public string last_zone_uuid;
        /// <summary>
        /// 最后存在地图模板
        /// </summary>
        [PersistField]
        public int last_map_template_id;
        /// <summary>
        /// 最后存在场景坐标
        /// </summary>
        [PersistField]
        public ZonePosition last_zone_pos;
        /// <summary>
        /// 最后存在场景存储数据，用于跨场景存储一些状态，比如BUFF
        /// </summary>
        [PersistField]
        public ISerializable last_zone_saved;
        /// <summary>
        /// 最近一次公共场景实例ID.
        /// </summary>
        [PersistField]
        public string last_public_area_uuid;
        /// <summary>
        /// 上一次公共场景地图ID.
        /// </summary>
        [PersistField]
        public int last_public_mapID;
        /// <summary>
        /// 上一次公共地图所在坐标.
        /// </summary>
        [PersistField]
        public ZonePosition last_public_map_pos;
        /// <summary>
        /// 上一次公会场景ID.
        /// </summary>
        [PersistField]
        public int last_guild_mapID;
        #endregion
        //------------------------------------------------
        [PersistField]
        public int onlineState;

        public virtual RoleSnap ToSnap()
        {
            return new RoleSnap()
            {
                uuid = uuid,
                digitID = digitID,
                name = name,
                account_uuid = account_uuid,
                role_template_id = role_template_id,
                unit_template_id = unit_template_id,
                level = Level,
                create_time = create_time,
                last_login_time = last_login_time,
            };
        }

        public virtual ClientRoleData ToClientRoleData()
        {
            var ret = new ClientRoleData();
            ret.uuid = this.uuid;
            ret.digitID = this.digitID;
            ret.name = this.name;
            ret.account_uuid = this.account_uuid;
            ret.role_template_id = this.role_template_id;
            ret.unit_template_id = this.unit_template_id;
            ret.level = this.Level;
            ret.create_time = this.create_time;
            ret.last_login_time = this.last_login_time;
            ret.server_name = this.server_id;
            ret.privilege = this.privilege;
            ret.last_area_name = this.last_area_name;
            ret.last_area_node = this.last_area_node;
            ret.last_zone_uuid = this.last_zone_uuid;
            ret.last_map_template_id = this.last_map_template_id;
            ret.last_zone_pos = this.last_zone_pos;
            return ret;
        }
    }
}