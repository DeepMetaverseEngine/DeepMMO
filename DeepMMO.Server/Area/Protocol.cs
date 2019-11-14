using DeepCore.IO;
using DeepMMO.Attributes;
using DeepMMO.Protocol;
using System.Collections.Generic;
using DeepCore.Geometry;
using DeepMMO.Data;

namespace DeepMMO.Server.Area
{
    //---------------------------------------------------------------------------------
    [ProtocolRoute("Logic", "Area")]
    public class RoleDataChangedNotify : Notify
    {
        public string roleID;
        public ISerializable roleData;
    }
    //---------------------------------------------------------------------------------

    //     [ProtocolRoute("Area", "Logic")]
    //     public class AreaSaveRoleDataNotify : Notify
    //     {
    //         public string roleID;
    //         public ISerializable roleData;
    //     }
    // 
    //     [ProtocolRoute("Area", "Logic")]
    //     public class AreaSaveRoleInfoNotify : Notify
    //     {
    //     }

    /// <summary>
    /// 当玩家踩到传送点，或者场景内其他传送事件 
    /// Area通知逻辑需要传送操作，一般是踩到场景传送点
    /// </summary>
    [ProtocolRoute("Area", "Logic")]
    public class RoleNeedTransportNotify : Notify
    {
        public int nextMapID;
        public int nextZoneID;
        public string nextZoneFlagName;
        public string fromAreaName;
        public string fromAreaNode;
    }

    /// <summary>
    /// 角色所在场景Game Over后，推送给Logic
    /// 通常推送后，当前场景即将删除，
    /// Logic在收到后，将玩家传送到最后的公共场景或者主城
    /// </summary>
    [ProtocolRoute("Area", "Logic")]
    public class AreaGameOverNotify : Notify
    {
        public int mapTemplateID;
        public int zoneTemplateID;
        public string zoneUUID;
        public byte winForce;
        public string message;
        // TODO some award
        public ISerializable expandData;
    }
    //---------------------------------------------------------------------------------

    /// <summary>
    /// 通知Session
    /// </summary>
    [ProtocolRoute("Area", "Session")]
    public class SessionBindAreaNotify : Notify
    {
        public string areaName;
        public string areaNode;
    }
    /// <summary>
    /// 通知Session
    /// </summary>
    [ProtocolRoute("Area", "Session")]
    public class SessionUnbindAreaNotify : Notify
    {
        public string areaName;
        public string areaNode;
    }
    /// <summary>
    /// 战斗Action
    /// </summary>
    [ProtocolRoute("Session", "Area")]
    sealed public class SessionBattleAction : Notify
    {
        public string roleID;
        /// <summary>
        /// ClientBattleAction
        /// </summary>
        public byte[] clientBattleAction;
    }

    /// <summary>
    /// 单位奖励信息.
    /// </summary>
    [ProtocolRoute("Area", "Logic")]
    public class RoleBattleAwardNotify : Notify
    {
        public class AwardItem : ISerializable
        {
            public int ItemTemplateID;
            public int ItemCount;
        }

        public string RoleID;
        public int MonsterID;
        public List<AwardItem> Awards;
    }

    /// <summary>
    /// 角色穿越地图通知(无缝切图)
    /// </summary>
    [ProtocolRoute("Area", "Logic")]
    public class RoleCrossMapNotify : Notify
    {
        public int NextSceneID;
        public ZonePosition NextScenePos;
    }
    
    //---------------------------------------------------------------------------------
}
