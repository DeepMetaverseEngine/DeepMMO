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
    }
}
