using DeepCore.IO;
using DeepCore.IO;
using DeepMMO.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DeepCore.Reflection;
using DeepMMO.Attributes;

namespace DeepMMO.Protocol.Client
{
    public interface IGateProtocol { }
    //--------------------------------------------------------------------------------
    /// <summary>
    /// 链接Gate
    /// </summary>
    [MessageType(Constants.GATE_START + 1)]
    public class ClientEnterGateRequest : Request, IGateProtocol
    {
        /// <summary>
        /// 帐号
        /// </summary>
        public string c2s_account;

        /// <summary>
        /// 密钥
        /// </summary>
        public string c2s_token;
        /// <summary>
        /// 客户端信息
        /// </summary>
        public ClientInfo c2s_clientInfo;
        /// <summary>
        /// 服务器ID.
        /// </summary>
        public string c2s_serverID;
    }

    [MessageType(Constants.GATE_START + 2)]
    public class ClientEnterGateResponse : Response, IGateProtocol
    {
        [MessageCode("排队等待中！")]
        public const int CODE_OK_IN_QUEUE = 201;
        [MessageCode("没有连接服务器！")]
        public const int CODE_NO_CONNECT_SERVER = 404;
        [MessageCode("账户或密码错误！")]
        public const int CODE_ACCOUNT_OR_PASSWORD = 501;
        [MessageCode("账户不存在！")]
        public const int CODE_NO_ACCOUNT = 502;
        [MessageCode("服务器未开启！")]
        public const int CODE_SERVER_NOT_OPEN = 503;

        public bool IsInQueue { get => s2c_code == CODE_OK_IN_QUEUE; }

        [DependOnProperty(nameof(IsSuccess))] public string s2c_accountUUID;
        [DependOnProperty(nameof(IsSuccess))] public string s2c_connectHost;
        [DependOnProperty(nameof(IsSuccess))] public int s2c_connectPort;
        [DependOnProperty(nameof(IsSuccess))] public string s2c_connectToken;
        [DependOnProperty(nameof(IsSuccess))] public int s2c_logicServerID;
        [DependOnProperty(nameof(IsSuccess))] public string s2c_lastLoginRoleID;
        [DependOnProperty(nameof(IsSuccess))] public string s2c_lastLoginToken;
        [DependOnProperty(nameof(IsSuccess))] public List<RoleIDSnap> s2c_roleIDList;
        [DependOnProperty(nameof(IsSuccess))] public string s2c_platformAccount;
        [DependOnProperty(nameof(IsInQueue))] public int s2c_queueCount;
        [DependOnProperty(nameof(IsInQueue))] public TimeSpan s2c_queuetTime;

    }

    [MessageType(Constants.GATE_START + 3)]
    public class ClientEnterGateInQueueNotify : Notify, IGateProtocol
    {
        /// <summary>
        /// 可进入
        /// </summary>
        public bool IsEnetered;
        /// <summary>
        /// 等待人数
        /// </summary>
        public int QueueIndex;
        /// <summary>
        /// 预计时间
        /// </summary>
        public TimeSpan ExpectTime;
        /// <summary>
        /// 登陆Connector之必要信息
        /// </summary>       
        [DependOnProperty(nameof(IsEnetered))] public string s2c_connectHost;
        [DependOnProperty(nameof(IsEnetered))] public int s2c_connectPort;
        [DependOnProperty(nameof(IsEnetered))] public string s2c_connectToken;
        [DependOnProperty(nameof(IsEnetered))] public string s2c_lastLoginToken;

    }

}
