using DeepCore;
using DeepCore.IO;
using DeepMMO.Attributes;
using DeepMMO.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeepMMO.Server.Gate
{
    /// <summary>
    /// 链接服通知Gate服当前状态
    /// </summary>
    [ProtocolRoute("Connect", "Gate")]
    public class SyncConnectToGateNotify : Notify
    {
        /// <summary>
        /// 服务地址
        /// </summary>
        public string connectServiceAddress;
        /// <summary>
        /// 服务IP
        /// </summary>
        public string connectHost;
        /// <summary>
        /// 服务端口
        /// </summary>
        public int connectPort;
        /// <summary>
        /// 给客户端的Token
        /// </summary>
        public string connectToken;

        /// <summary>
        /// 已连接客户端数量
        /// </summary>
        public int clientNumber;

        /// <summary>
        /// 每个Group玩家数
        /// </summary>
        public HashMap<string, int> groupClientNumbers;
    }

    [ProtocolRoute("*", "*")]
    public class Ping : Request
    {
        public DateTime time = DateTime.Now;
        public int index;
    }
    [ProtocolRoute("*", "*")]
    public class Pong : Response
    {
        public DateTime time = DateTime.Now;
        public int index;
    }

    /// <summary>
    /// 通知Gate服务器开启.
    /// </summary>
    [ProtocolRoute("AdminServer", "Gate")]
    public class SyncGateServerOpen : Notify
    {
        public bool status;
    }

    /// <summary>
    /// 通知Gate服务器某个Group人数限制.
    /// </summary>
    [ProtocolRoute("AdminServer", "Gate")]
    public class SyncGateClientNumberLimit : Notify
    {
        public string serverGroupID;
        public int clientLimit;

        // H.Q.Cai 添加开始
        /// <summary>
        /// 连接数量软限制
        /// </summary>
        public int clientSoftLimit;
        /// <summary>
        /// 最大可排队人数
        /// </summary>
        public int queueMaxLimit;
        /// <summary>
        /// 每一名角色增加时间
        /// </summary>
        public int queueAddTime;
        // H.Q.Cai 添加结束
    }

    // H.Q.Cai 添加开始

    /// <summary>
    /// 
    /// </summary>
    [ProtocolRoute("LogicService", "Gate")]
    public class SyncGateClientAccountExpire : Notify
    {
        /// <summary>
        /// 
        /// </summary>
        public string serverGroupID;

        /// <summary>
        /// 
        /// </summary>
        public string accountUUid;

        /// <summary>
        /// 
        /// </summary>
        public DateTime ExpectTime;
    }

    // H.Q.Cai 添加结束
}
