using DeepCore;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepMMO.Attributes;
using DeepMMO.Data;
using DeepMMO.Protocol;
using System;
using System.Collections.Generic;

namespace DeepMMO.Server.AreaManager
{
    //     /// <summary>
    //     /// 寻找一个Area
    //     /// </summary>
    //     [ProtocolRoute("*", "AreaManager")]
    //     public class LookingForAreaRequest : Request
    //     {
    //         /// <summary>
    //         /// 预期的Area服务地址
    //         /// </summary>
    //         public string expectAreaName;
    //         /// <summary>
    //         /// 预期的Area服务地址
    //         /// </summary>
    //         public string expectAreaNode;
    //         /// <summary>
    //         /// 预期的场景
    //         /// </summary>
    //         public int expectSceneTemplateID;
    //         /// <summary>
    //         /// 预期的具体战斗场景
    //         /// </summary>
    //         public string expectZoneUUID;
    //     }
    // 
    //     [ProtocolRoute("AreaManager", "*")]
    //     public class LookingForAreaResponse : Response
    //     {
    //         /// <summary>
    //         /// 返回Area服务地址
    //         /// </summary>
    //         public string areaName;
    //         /// <summary>
    //         /// 返回Area服务地址
    //         /// </summary>
    //         public string areaNode;
    //         /// <summary>
    //         /// 返回场景UUID
    //         /// </summary>
    //         public string zoneUUID;
    //     }
    //---------------------------------------------------------------------------------

    [ProtocolRoute("Area", "AreaManager")]
    public class RegistAreaRequest : Request
    {
        public string areaName;
        public string areaNode;
    }

    [ProtocolRoute("AreaManager", "Area")]
    public class RegistAreaResponse : Response { }

    [ProtocolRoute("Area", "AreaManager")]
    public class AreaStateNotify : Notify
    {
        public string areaName;
        public string areaNode;
        public long memoryMB;
        public float cpuPercent;
        public int zoneCount;
        public int roleCount;

        public override string ToString()
        {
            return ("AreaStateNotify:" +
                        "\n    areaName=" + this.areaName +
                        "\n    areaNode=" + this.areaNode +
                        "\n   roleCount=" + this.roleCount +
                        "\n   zoneCount=" + this.zoneCount +
                        "\n  cpuPercent=" + this.cpuPercent +
                        "\n    memoryMB=" + this.memoryMB);
        }
    }
    //---------------------------------------------------------------------------------
    [ProtocolRoute("Logic", "AreaManager")]
    public class RoleEnterZoneRequest : Request
    {
        /// <summary>
        /// 公会UUID.如果赋值代表单位进公会场景.
        /// </summary>
        public string guildUUID;
        /// <summary>
        /// 服务器ID.
        /// </summary>
        public string serverID;
        /// <summary>
        /// 服务器组ID.
        /// </summary>
        public string servergroupID;
        /// <summary>
        /// 预期的Area服务地址
        /// </summary>
        public string expectAreaName;
        /// <summary>
        /// 预期的Area服务地址
        /// </summary>
        public string expectAreaNode;
        /// <summary>
        /// 预期的场景
        /// 如果为空，表示不知道在哪个场景，一般是第一次注册用户
        /// </summary>
        public int expectMapTemplateID;
        /// <summary>
        /// 预期的具体战斗场景
        /// 如果为空，表示没有确定的实体场景
        /// </summary>
        public string expectZoneUUID;
        /// <summary>
        /// 自定义玩家groupKey，groupkey相同进同一场景
        /// </summary>
        public string roomKey;
        /// <summary>
        /// 角色
        /// </summary>
        public string roleUUID;
        public string roleSessionName;
        public string roleSessionNode;
        public string roleLogicName;
        public string roleLogicNode;
        public int roleForce;
        public int roleUnitTemplateID;
        public string roleDisplayName;
        public ZonePosition roleScenePos;
        public ISerializable LastZoneSaveData;
        public ISerializable roleData;

        public string teamID;

        public string reason;
        /// <summary>
        /// 预期线.
        /// </summary>
        public int expectLineIndex;
        /// <summary>
        /// 上一次的公共场景.
        /// </summary>
        public int lastPublicMapID;
        /// <summary>
        /// 上一次的公共场景UUID.
        /// </summary>
        public string lastPublicMapUUID;
        /// <summary>
        /// 公共场景坐标.
        /// </summary>
        public ZonePosition lastPublicPos;
        /// <summary>
        /// 扩展数据.
        /// </summary>
        public HashMap<string, string> ext;
    }
    [ProtocolRoute("AreaManager", "Logic")]
    public class RoleEnterZoneResponse : Response
    {
        [MessageCode("重新进入")]
        public const int CODE_OK_REPLACE = CODE_OK + 1;
        [MessageCode("场景不存在")]
        public const int CODE_ZONE_NOT_EXIST = CODE_ERROR + 1;
        [MessageCode("角色已在场景中")]
        public const int CODE_ROLE_ALREADY_IN_ZONE = CODE_ERROR + 2;
        [MessageCode("场景未开放")]
        public const int CODE_ZONE_NOT_OPEN = CODE_ERROR + 3;
        [MessageCode("场景已关闭")]
        public const int CODE_ZONE_CLOSED = CODE_ERROR + 4;

        [DependOnProperty(nameof(IsSuccess))]
        public int mapTemplateID;
        [DependOnProperty(nameof(IsSuccess))]
        public string zoneUUID;
        [DependOnProperty(nameof(IsSuccess))]
        public int zoneTemplateID;
        [DependOnProperty(nameof(IsSuccess))]
        public int roleUnitTemplateID;
        [DependOnProperty(nameof(IsSuccess))]
        public string roleDisplayName;
        [DependOnProperty(nameof(IsSuccess))]
        public ISerializable roleBattleData;
        [DependOnProperty(nameof(IsSuccess))]
        public ZonePosition roleScenePos;
        [DependOnProperty(nameof(IsSuccess))]
        public string areaName;
        [DependOnProperty(nameof(IsSuccess))]
        public string areaNode;
        [DependOnProperty(nameof(IsSuccess))]
        public string guildUUID;
    }
    //---------------------------------------------------------------------------------
    [ProtocolRoute("Logic", "AreaManager")]
    public class RoleLeaveZoneRequest : Request
    {
        public string zoneUUID;
        public string roleID;
        public bool keepObject;
        public string reason;
    }
    [ProtocolRoute("AreaManager", "Logic")]
    public class RoleLeaveZoneResponse : Response
    {
        [MessageCode("场景不存在")]
        public const int CODE_ZONE_NOT_EXIST = CODE_ERROR + 1;
        [MessageCode("角色不存在")]
        public const int CODE_ROLE_NOT_EXIST = CODE_ERROR + 2;
        /// <summary>
        /// 最后存在场景坐标
        /// </summary>
        public ZonePosition lastScenePos;
        public ISerializable expandData;
        public ISerializable LeaveZoneSaveData;

        public int curHP;
        public int curMP;
    }

    //---------------------------------------------------------------------------------
    [ProtocolRoute("*", "AreaManager -> Area")]
    public class CreateZoneNodeRequest : Request
    {
        public string serverID;
        public string serverGroupID;
        public string expectAreaNode;

        public int mapTemplateID;
        public string managerZoneUUID;
        public string reason;

        public string createRoleID;
        public string teamID;

        public string guildUUID;

        /// <summary>
        /// 自定义玩家roomKey，roomKey相同进同一场景
        /// </summary>
        public string roomKey;
        /// <summary>
        /// 扩展数据.
        /// </summary>
        public ISerializable expandData;
    }
    [ProtocolRoute("Area -> AreaManager", "*")]
    public class CreateZoneNodeResponse : Response
    {
        [MessageCodeAttribute("地图尚未开放！")]
        public const int CODE_ERROR_MAP_NOT_OPEN = 501;

        public string zoneUUID;
        public string areaName;
        public string areaNode;
    }
    [ProtocolRoute("*", "AreaManager -> Area")]
    public class DestoryZoneNodeRequest : Request
    {
        public string zoneUUID;
        public string reason;
    }
    [ProtocolRoute("Area -> AreaManager", "*")]
    public class DestoryZoneNodeResponse : Response
    {

    }
    [ProtocolRoute("Area -> AreaManager", "*")]
    public class AreaZoneGameOverNotify : Notify
    {
        public string zoneUUID;
        public string reason;
    }

    [ProtocolRoute("Area -> AreaManager", "*")]
    public class AreaZoneDestoryNotify : Notify
    {
        public string zoneUUID;
        public string reason;
    }
    //---------------------------------------------------------------------------------

    //---------------------------------------------------------------------------------
    [ProtocolRoute("*", "AreaManager")]
    public class GetAllRoleRequest : Request
    {

    }
    [ProtocolRoute("AreaManager", "*")]
    public class GetAllRoleResponse : Response
    {
        public HashMap<string, OnlinePlayerData> uuidMap;
    }

    [ProtocolRoute("*", "AreaManager")]
    public class QueryZoneAreaNameRequest : Request
    {
        public string zoneUUID;
    }

    [ProtocolRoute("AreaManager", "*")]
    public class QueryZoneAreaNameResponse : Response
    {
        public string areaName;
    }

    /// <summary>
    /// 获得场景信息快照.
    /// </summary>
    [ProtocolRoute("logic", "AreaManager")]
    public class GetZonesInfoRequest : Request
    {
        public string servergroupID;
        public int mapID;
    }

    /// <summary>
    /// 获得场景信息快照.
    /// </summary>
    [ProtocolRoute("AreaManager", "logic")]
    public class GetZonesInfoResponse : Response
    {
        public List<ZoneInfoSnap> snaps;
    }
    
    [ProtocolRoute("Logic", "AreaManager")]
    public class GetRolePositionRequest : Request
    {
//        public string zoneUUID;
        public string roleUUID;
    }

    [ProtocolRoute("AreaManager", "Logic")]
    public class GetRolePositionResponse : Response
    {
        public int zoneId;
        public string zoneUUID;
        public float x;
        public float y;
        public float z;
        public int line;
        
        [MessageCode("场景不存在")]
        public const int CODE_ZONE_NOT_EXIST = CODE_ERROR + 1;
        [MessageCode("角色不存在")]
        public const int CODE_ROLE_NOT_EXIST = CODE_ERROR + 2;
    }

    [ProtocolRoute("Logic", "AreaManager")]
    public class RoleNameChangedNotify : Notify
    {
        public string roleId;
        public string newName;
    }
}
