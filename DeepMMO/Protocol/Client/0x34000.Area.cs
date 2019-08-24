using DeepCore;
using DeepCore.IO;
using DeepMMO.Attributes;
using DeepMMO.Data;
using System.Collections.Generic;

namespace DeepMMO.Protocol.Client
{
    public interface IAreaProtocol { }
    //---------------------------------------------------------------------------------------
    /// <summary>
    /// (客户端)进入场景请求
    /// </summary>
    [MessageType(Constants.AREA_START + 1)]
    public class ClientEnterZoneRequest : Request, ILogicProtocol
    {
        public string c2s_action;
    }
    /// <summary>
    /// (客户端)进入场景回馈
    /// </summary>
    [MessageType(Constants.AREA_START + 2)]
    public class ClientEnterZoneResponse : Response, ILogicProtocol
    {
        [MessageCode("已排入队列")]
        public const int CODE_ENQUEUE = 202;
    }
    /// <summary>
    /// (客户端)进入场景通知
    /// </summary>
    [MessageType(Constants.AREA_START + 3)]
    public class ClientEnterZoneNotify : Notify, ILogicProtocol, INetProtocolS2C
    {
        public string s2c_ZoneUUID;
        public int s2c_MapTemplateID;
        public int s2c_ZoneTemplateID;
        public int s2c_RoleUnitTemplateID;
        public string s2c_RoleDisplayName;
        public ISerializable s2c_RoleData;
        public int s2c_SceneLineIndex;
        public string s2c_GuildUUID;
        public int s2c_ZoneUpdateIntervalMS;
        public HashMap<string, string> s2c_Ext;
    }
    /// <summary>
    /// (客户端)离开场景通知
    /// </summary>
    [MessageType(Constants.AREA_START + 4)]
    public class ClientLeaveZoneNotify : Notify, ILogicProtocol, INetProtocolS2C
    {
        public string s2c_ZoneUUID;
    }
    /// <summary>
    /// (客户端)进入场景排队
    /// </summary>
    [MessageType(Constants.AREA_START + 5)]
    public class ClientEnterZoneQueueUpdateNotify : Notify, ILogicProtocol, INetProtocolS2C
    {
        public int s2c_queueSize;
        public int s2c_expectTimeSec;
    }

    //---------------------------------------------------------------------------------------
    /// <summary>
    /// (客户端)战斗Action
    /// </summary>
    [MessageType(Constants.AREA_START + 6)]
    sealed public class ClientBattleAction : Notify, IAreaProtocol, IRpcNoneSerializable, INetProtocolC2S
    {
        public byte[] c2s_battleAction;
    }
    /// <summary>
    /// (客户端)战斗Event
    /// </summary>
    [MessageType(Constants.AREA_START + 7)]
    public class ClientBattleEvent : Notify, IAreaProtocol, IRpcNoneSerializable, INetProtocolS2C
    {
        public byte[] s2c_battleEvent;
    }
    //---------------------------------------------------------------------------------------

    /// <summary>
    // 客户端获取分线信息.
    /// </summary>
    [MessageType(Constants.AREA_START + 8)]
    public class ClientGetZoneInfoSnapRequest : Request, ILogicProtocol
    {
    }

    /// <summary>
    /// 客户端获取分线信息.
    /// </summary>
    [MessageType(Constants.AREA_START + 9)]
    public class ClientGetZoneInfoSnapResponse : Response, ILogicProtocol
    {
        public string s2c_curZoneUUID;
        public List<ZoneInfoSnap> s2c_snaps;
    }

    /// <summary>
    /// 场景换线.
    /// </summary>
    [MessageType(Constants.AREA_START + 10)]
    public class ClientChangeZoneLineRequest : Request, ILogicProtocol
    {
        public string c2s_zoneuuid;
    }

    /// <summary>
    /// 场景换线
    /// </summary>
    [MessageType(Constants.AREA_START + 11)]
    public class ClientChangeZoneLineResponse : Response, ILogicProtocol
    {
        [MessageCodeAttribute("目标场景繁忙")]
        public const int CODE_LINE_BUSY = 501;

        [MessageCodeAttribute("目标场景不存在")]
        public const int CODE_NOT_EXIST = 502;
    }
}
