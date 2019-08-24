using DeepCore;
using DeepCore.IO;
using DeepMMO.Attributes;
using DeepMMO.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeepMMO.Server.Connect
{

    //-------------------------------------------------------------------

    /// <summary>
    /// 连接已断开通知（只是网络断开，服务状态不变）
    /// </summary>
    [ProtocolRoute("Connect", "Session -> Logic -> Area")]
    public class SessionDisconnectNotify : Notify
    {
        public string sessionName;
        public string socketID;
        public string roleID;
    }

    //-------------------------------------------------------------------

    /// <summary>
    /// 用户重新连接通知（只是网络重连，服务状态不变）
    /// </summary>
    [ProtocolRoute("Session", "Logic -> Area")]
    public class SessionReconnectNotify : Notify
    {
        public string sessionName;
        public string roleID;
        public HashMap<string, string> config;
    }
    //-------------------------------------------------------------------
    [ProtocolRoute("Session", "Logic -> Area")]
    public class SessionBeginLeaveRequest : Request
    {
        public string sessionName;
        public string roleID;
    }
    [ProtocolRoute("Area", "Session")]
    public class SessionBeginLeaveResponse : Response
    {
    }
    //-------------------------------------------------------------------
    /// <summary>
    ///用于广播的协议，发送给所有SessionService
    /// </summary>
    [ProtocolRoute("Connector", "* -> Connector")]
    public class ConnectorBroadcastNotify : Notify
    {
        public string serverGroupID
        {
            set
            {
                if (value == null)
                {
                    serverGroups = null;
                    return;
                }

                if (serverGroups == null)
                {
                    serverGroups = new ArrayList<string>();
                }
                serverGroups.Add(value);
            }
        }
        public string sessionID
        {
            set
            {
                if (value == null)
                {
                    sessions = null;
                    return;
                }

                if (sessions == null)
                {
                    sessions = new ArrayList<string>();
                }
                sessions.Add(value);
            }
        }
        /// <summary>
        /// 可接受广播的ServerGroupID。
        /// 如果为空，则广播到所有ServerGroup。
        /// </summary>
        public ArrayList<string> serverGroups;
        /// <summary>
        /// 可接受广播的所有客户端，一般指一个频道里的人。
        /// 如果为空，则广播到所有Session。
        /// </summary>
        public ArrayList<string> sessions;
        /// <summary>
        /// 真正广播出去的协议。
        /// </summary>
        public Notify notify;
    }

    [ProtocolRoute("*", "Session")]
    public class KickPlayerNotify : Notify
    {
        public string reason;
    }
}
