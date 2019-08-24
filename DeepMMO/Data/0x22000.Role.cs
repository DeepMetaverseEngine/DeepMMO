using DeepCore.IO;
using DeepCore.ORM;
using System;

namespace DeepMMO.Data
{
    /// <summary>
    /// 返回哪些指令是啥GM等级可以使用的
    /// </summary>
    public enum RolePrivilege
    {
        //----- 3个玩家级别 -----
        ///<summary>普通玩家                 [公共聊天室说话]</summary>
        User_Player,
        ///<summary>超级玩家，比如家人，老玩家 [目前没用]</summary>
        User_PowerPlayer,
        ///<summary>比如 版署，合作方, 员工  [可随时登入游戏]</summary>
        User_VIP,

        // ----- 2个开发组级别 -----
        ///<summary>项目组程序开发人员       [同上，但可使用一些策划指令，如改npc]</summary>
        Dev_Programer,
        ///<summary>项目组策划开发人员</summary>
        Dev_Disgner,

        //----- 3个客服组级别 -----
        ///<summary>客服                    [可使用简单的客服指令]</summary>
        Gm_Wizard,
        ///<summary>资深客服                [可使用对玩家部分处罚和影响的客服指令]</summary>
        Gm_PowerWizard,
        ///<summary>客服主管                [可使用对玩家所有处罚和影响的客服指令]</summary>
        Gm_SuperWizard,

        //----- 4个公司领导层级别 -----
        ///<summary>项目小队长              [可使用有一点rmb价值的指令]</summary>
        Admin_Leader,
        ///<summary>项目负责人              [可以使用较有rmb价值的指令]</summary>
        Admin_Manager,
        ///<summary>项目总负责人            [可以使用所有rmb价值的指令]</summary>
        Admin_SuperManager,
        ///<summary>总管理员                [所有]</summary>
        Admin_BigBoss,
    }


    /// <summary>
    /// 角色模板数据
    /// </summary>
    [MessageType(Constants.ROLE_START + 1)]
    public class RoleTemplateData : ISerializable
    {
        /// <summary>模板ID</summary>
        public int id;
        /// <summary>名称</summary>
        public string name;
        /// <summary>性别</summary>
        public byte gender;

        /// <summary>战斗模板</summary>
        public int unit_template_id;

        public override string ToString()
        {
            return name;
        }
    }

    [MessageType(Constants.ROLE_START + 2)]
    public class ClientRoleData : ISerializable
    {
        //------------------------------------------------
        public string uuid;
        public string digitID;
        public string name;
        public string account_uuid;
        public int role_template_id;
        public int unit_template_id;
        //------------------------------------------------
        public string local_code = "zh_CN";
        //------------------------------------------------
        public int level;
        public DateTime create_time;
        public DateTime last_login_time;
        /// <summary>
        /// 服务器ID.
        /// </summary>
        public string server_name;

        /// <summary>
        /// 用户权限
        /// </summary>
        public RolePrivilege privilege = RolePrivilege.User_Player;


        //------------------------------------------------
        #region area

        /// <summary>
        /// 最后场景服务地址
        /// </summary>
        public string last_area_name;
        public string last_area_node;
        /// <summary>
        /// 最后存在场景UUID
        /// </summary>
        public string last_zone_uuid;
        /// <summary>
        /// 最后存在场景模板
        /// </summary>
        public int last_map_template_id;
        /// <summary>
        /// 最后存在场景坐标
        /// </summary>
        public ZonePosition last_zone_pos;
        #endregion
        //------------------------------------------------
    }

    /// <summary>
    /// 角色快照数据
    /// </summary>
    [PersistType]
    [MessageType(Constants.ROLE_START + 3)]
    public class RoleSnap : ISerializable, IObjectMapping
    {
        //------------------------------------------------
        [PersistField]
        public string uuid;
        [PersistField]
        public string digitID;
        [PersistField]
        public string name;
        [PersistField]
        public string session_name;
        [PersistField]
        public string account_uuid;
        [PersistField]
        public int role_template_id;
        [PersistField]
        public int unit_template_id;
        //------------------------------------------------
        /// <summary>
        /// 服务器ID.
        /// </summary>
        [PersistField]
        public string server_id;
        //------------------------------------------------
        [PersistField]
        public int level;
        [PersistField]
        public int vip_level;
        [PersistField]
        public DateTime create_time;
        [PersistField]
        public DateTime last_login_time;
        [PersistField]
        public int onlineState;
        [PersistField]
        public RolePrivilege privilege = RolePrivilege.User_Player;
    }

    [MessageType(Constants.ROLE_START + 4)]
    public class PropertyStruct : ISerializable
    {
        public const int TYPE_NUMBER = 1;
        public const int TYPE_STRING = 2;

        public string key;
        public string value;
        public int type;

        public PropertyStruct(string key, string value, bool isNum)
        {
            this.key = key;
            this.value = value;
            this.type = isNum ? TYPE_NUMBER : TYPE_STRING;
        }

        public PropertyStruct()
        {
        }
    }

    /// <summary>
    /// 角色在线类型
    /// </summary>
    [MessageType(Constants.ROLE_START + 5)]
    public class RoleState : ISerializable
    {
        public const int STATE_ONLINE = 1;
        public const int STATE_OFFLINE = 2;
    }


    //角色数据状态快照
    [PersistType]
    [MessageType(Constants.ROLE_START + 7)]
    public class RoleDataStatusSnap : ISerializable, IObjectMapping
    {
        [PersistField]
        public string PlayerUUID;
        [PersistField]
        public string PlayerName;
        [PersistField]
        public DateTime SuspendDate;
        [PersistField]
        public string SuspendReason;
        [PersistField]
        public string OperatorID;
    }
}
