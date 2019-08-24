using CommonRPG.Attributes;
using CommonRPG.Data;
using CommonRPG.Protocol;
using CommonRPG.Protocol.Client;
using System;
using System.Collections.Generic;

namespace CommonRPG.Server.Chat
{
    public enum CHANNEL_TYPE
    {
        /// <summary>
        /// 无效的
        /// </summary>
        INVALID = ClientChatRequest.CHANNEL_TYPE_INVALID,
        /// <summary>
        /// 世界
        /// </summary>
        WORLD = ClientChatRequest.CHANNEL_TYPE_WORLD,
        /// <summary>
        /// 交易
        /// </summary>
        TRADE= ClientChatRequest.CHANNEL_TYPE_TRADE,
        /// <summary>
        /// 工会
        /// </summary>
        GUILD=ClientChatRequest.CHANNEL_TYPE_GUILD,
        /// <summary>
        /// 队伍
        /// </summary>
        TEAM = ClientChatRequest.CHANNEL_TYPE_TEAM,
        /// <summary>
        /// 战场 同阵营的频道
        /// </summary>
        BATTLE = ClientChatRequest.CHANNEL_TYPE_BATTLE,
        /// <summary>
        /// 区域 比如敌我都在这个频道
        /// </summary>
        AREA = ClientChatRequest.CHANNEL_TYPE_AREA,
        /// <summary>
        /// 系统
        /// </summary>
        SYSTEM,
        /// <summary>
        /// 私聊
        /// </summary>
        PRIVATE,
    }

//     [ProtocolRoute("LogicService", "ChatService")]
//     public class ChatRequest : Request
//     {
//         public short channel_type;
//         public string from_uuid;
//         public string from_name;
//         public string from_icon;
//         public int from_vip;
//         public string to_uuid;
//         public string content;
//     }

    [ProtocolRoute("ChatService", "LogicService")]
    public class ChatResponse : Response
    {
        public int errcode;
        public string errmsg;
    }

    [ProtocolRoute("ChatService", "LogicService")]
    public class ChatNotify : Notify
    {
        public short channel_type;
        public string from_name;
        public string from_uuid;
        public string content;
        public string to_uuid;
    }

    [ProtocolRoute("*", "ChatService")]
    public class CreateChannelRequest : Request
    {
        public short channel_type;
        public string creator_uuid;
        public bool no_member_auto_destroy;
    }

    [ProtocolRoute("ChatService", "*")]
    public class CreateChannelResponse : Response
    {
        public int errcode;
        public string errmsg;
        public short channel_type;
        public string channel_uuid;
    }

    [ProtocolRoute("*", "ChatService")]
    public class AddChannelMemberRequest : Request
    {
        public string uuid;
    }

    [ProtocolRoute("ChatService", "*")]
    public class AddChannelMemberResponse : Response
    {
        public const int CODE_INVALID_UUID = CODE_ERROR + 1;
        public const int CODE_ALREADY_EXIST = CODE_ERROR + 2;
    }

    [ProtocolRoute("*", "ChatService")]
    public class RemoveChannelMemberRequest : Request
    {
        public string uuid;
    }

    [ProtocolRoute("ChatService", "*")]
    public class RemoveChannelMemberResponse : Response
    {
        public const int CODE_INVALID_UUID = CODE_ERROR + 1;
        public const int CODE_NOT_EXIST = CODE_ERROR + 2;
    }
}
