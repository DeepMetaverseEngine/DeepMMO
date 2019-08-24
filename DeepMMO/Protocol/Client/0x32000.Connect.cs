using DeepCore.IO;
using DeepMMO.Data;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DeepCore.Reflection;

namespace DeepMMO.Protocol.Client
{
    public interface IConnectProtocol { }
    //--------------------------------------------------------------------------------
    /// <summary>
    /// 链接Connect
    /// </summary>
    [MessageType(Constants.CONNECT_START + 1)]
    public class ClientEnterServerRequest : Request, IConnectProtocol, INetProtocolBotIgnore
    {
        public string c2s_account;
        public string c2s_gate_token;
        public string c2s_login_token;
        public string c2s_session_token;
        public DateTime c2s_time;
        public ClientInfo c2s_clientInfo;
    }
    [MessageType(Constants.CONNECT_START + 2)]
    public class ClientEnterServerResponse : Response, IConnectProtocol, INetProtocolBotIgnore
    {
        [DependOnProperty(nameof(IsSuccess))]
        public string s2c_session_token;
    }

    //--------------------------------------------------------------------------------
    /// <summary>
    /// 重新连接Connect
    /// </summary>
    [MessageType(Constants.CONNECT_START + 3)]
    public class ClientReconnectServerRequest : Request, IConnectProtocol
    {
        public string c2s_account;
        public string c2s_token;
        public int c2s_logicServerID;
    }
    [MessageType(Constants.CONNECT_START + 4)]
    public class ClientReconnectServerResponse : Response, IConnectProtocol
    {
    }
    //--------------------------------------------------------------------------------
}
