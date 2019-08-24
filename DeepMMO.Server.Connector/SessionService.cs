using DeepCore;
using DeepCore.IO;
using DeepCore.Log;
using DeepCrystal;
using DeepCrystal.ORM.Generic;
using DeepCrystal.ORM.Query;
using DeepCrystal.RPC;
using DeepMMO.Data;
using DeepMMO.Protocol;
using DeepMMO.Protocol.Client;
using DeepMMO.Server.Area;
using DeepMMO.Server.SystemMessage;
using System;
using System.Threading.Tasks;

namespace DeepMMO.Server.Connect
{
    /// <summary>
    /// 单个链接服务
    /// </summary>
    public partial class SessionService : IService
    {
        public readonly Logger log;
        public readonly string accountID;
        private readonly TypeCodec client_battle_action_codec;
        private readonly TypeCodec session_battle_action_codec;

        protected ConnectServer.ViewSession session { get; private set; }
        protected ClientEnterServerRequest enter { get; private set; }
        protected IRemoteService remote_logic_service { get; private set; }
        protected IRemoteService remote_area_service { get; private set; }
        protected ClientEnterGameRequest enter_game { get; private set; }
        public string Channel { get; private set; }

        private IDisposable heartbeat_timer;
        private DateTime last_heartbeat = DateTime.Now;
        private string sessionToken;
        private MappingReference<AccountData> accountSave;
        private QueryMappingReference<RoleSnap> queryRoleSnap;
        private MappingReference<AccountRoleSnap> accountRoleSnapSave;
        private QueryMappingReference<RoleDataStatusSnap> queryRoleDataStatusSnap;
        private bool mDisconnected = true;

        public override ServiceProperties Properties
        {
            get
            {
                var ret = base.Properties;
                ret.IgnoreRequestError = true;
                ret.IgnoreResponseError = true;
                return ret;
            }
        }

        public SessionService(ServiceStartInfo start) : base(start)
        {
            this.log = LoggerFactory.GetLogger(start.Address.ServiceName);
            this.accountID = start.Config["accountID"].ToString();
            this.client_battle_action_codec = ConnectServer.ClientCodec.Factory.GetCodec(typeof(ClientBattleAction));
            this.session_battle_action_codec = base.ServerCodec.Factory.GetCodec(typeof(SessionBattleAction));
            this.Channel = start.Config["channel"]?.ToString();
        }
        protected override void OnDisposed()
        {
            this.accountSave.Dispose();
            this.accountRoleSnapSave.Dispose();
            this.queryRoleSnap.Dispose();
            this.queryRoleDataStatusSnap.Dispose();
            this.session = null;
            this.enter = null;
            this.remote_logic_service = null;
            this.remote_area_service = null;
            this.enter_game = null;
            this.heartbeat_timer = null;
            this.sessionToken = null;
            this.accountSave = null;
            this.queryRoleSnap = null;
            this.queryRoleDataStatusSnap = null;
            this.accountRoleSnapSave = null;
        }
        protected override async Task OnStartAsync()
        {
            this.accountSave = new MappingReference<AccountData>(RPGServerPersistenceManager.TYPE_ACCOUNT_DATA, accountID, this);
            this.queryRoleSnap = new QueryMappingReference<RoleSnap>(RPGServerPersistenceManager.TYPE_ROLE_SNAP_DATA, this);
            this.accountRoleSnapSave = new MappingReference<AccountRoleSnap>(RPGServerPersistenceManager.TYPE_ACCOUNT_ROLE_SNAP_DATA, accountID, this);
            this.queryRoleDataStatusSnap = new QueryMappingReference<RoleDataStatusSnap>(RPGServerPersistenceManager.TYPE_ROLE_DATA_STATUS_SNAP_DATA, this);

            this.heartbeat_timer = base.Provider.CreateTimer(CheckHeartbeat, this,
                TimeSpan.FromSeconds(RPGServerManager.Instance.Config.timer_sec_SessionKeepTimeout),
                TimeSpan.FromSeconds(RPGServerManager.Instance.Config.timer_sec_SessionKeepTimeout));

            var data = await this.accountSave.LoadDataAsync();
            var roleSnap = await this.accountRoleSnapSave.LoadDataAsync();
        }

        protected override async Task OnStopAsync(ServiceStopInfo reason)
        {
            this.heartbeat_timer.Dispose();
            if (session != null) { this.session.socket.Disconnect(reason.Reason); }
            await ShutdownLogicServiceAsync("session destroy");
            await this.accountSave.FlushAsync();
        }
        protected void CheckHeartbeat(object state)
        {
            if (DateTime.Now - last_heartbeat > TimeSpan.FromSeconds(RPGServerManager.Instance.Config.timer_sec_SessionKeepTimeout))
            {
                if (session == null || session.socket.IsConnected == false)
                {
                    this.ShutdownSelf("timeout");
                }
            }
        }

        [RpcHandler(typeof(SystemShutdownNotify))]
        public virtual void system_rpc_Handle(SystemShutdownNotify shutdown)
        {
            var logic = remote_logic_service;
            if (logic != null)
            {
                logic.Invoke(new SessionDisconnectNotify() { sessionName = SelfAddress.ServiceName, });
            }
            this.ShutdownSelf(shutdown.reason);
        }

        [RpcHandler(typeof(KickPlayerNotify))]
        public virtual void rpc_Handle(KickPlayerNotify notify)
        {
            if (session != null)
            {
                this.ShutdownSelf(notify.reason);
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 首次连接或者重新连接
        /// </summary>
        /// <param name="bind"></param>
        /// <param name="cb"></param>
        [RpcHandler(typeof(LocalBindSessionRequest), typeof(LocalBindSessionResponse), ServerNames.ConnectServerType)]
        public virtual async Task<LocalBindSessionResponse> connect_rpc_Handle(LocalBindSessionRequest bind)
        {
            if (!string.IsNullOrEmpty(bind.enter.c2s_session_token) && !string.IsNullOrEmpty(this.sessionToken) && bind.enter.c2s_session_token != this.sessionToken)
            {
                this.sessionToken = null;
                return (new LocalBindSessionResponse() { s2c_code = Response.CODE_ERROR });
            }
            var savedLoginToken = await accountSave.LoadFieldAsync<string>(nameof(AccountData.lastLoginToken));
            var savedServerGroup = await accountSave.LoadFieldAsync<string>(nameof(AccountData.lastLoginServerGroupID));
            if (savedLoginToken != bind.enter.c2s_login_token)
            {
                this.sessionToken = null;
                return (new LocalBindSessionResponse() { s2c_code = Response.CODE_ERROR });
            }
            var old_session = this.session;
            if (old_session != null)
            {
                var disconnect = new SessionDisconnectNotify()
                {
                    socketID = old_session.socket.ID,
                    sessionName = SelfAddress.ServiceName,
                };
                if (enter_game != null)
                {
                    disconnect.roleID = enter_game.c2s_roleUUID;
                }
                //老Session暂停发包//
                var logic = this.remote_logic_service;
                if (logic != null)
                {
                    logic.Invoke(disconnect);
                }
                //log.Log("Reconnect");
                //老Session踢下线//
                old_session.socket.Disconnect("New Session Reconnect");
            }
            else
            {
                //log.Log("Connect");
            }
            this.session = bind.session;
            this.enter = bind.enter;
            //登录成功后，产生新的Token用于断线重连//
            this.sessionToken = Guid.NewGuid().ToString();
            this.last_heartbeat = DateTime.Now;

            return (new LocalBindSessionResponse()
            {
                session = this,
                sessionToken = sessionToken,
                serverGroupID = savedServerGroup,
            });
        }
        /// <summary>
        /// 用户断线
        /// </summary>
        /// <param name="disconnect"></param>
        [RpcHandler(typeof(SessionDisconnectNotify), ServerNames.ConnectServerType)]
        public virtual void connect_rpc_Handle(SessionDisconnectNotify disconnect)
        {


            //log.Log("Disconnect");
            last_heartbeat = DateTime.Now;
            disconnect.sessionName = SelfAddress.ServiceName;
            if (enter_game != null)
            {
                disconnect.roleID = enter_game.c2s_roleUUID;
            }
            //             var area = remote_area_service;
            //             if (area != null)
            //             {
            //                 area.Invoke(disconnect);
            //             }
            //排除老Session踢下线导致的Disconnect//
            if (this.session == null || disconnect.socketID == this.session.socket.ID)
            {
                this.session = null;
                var logic = this.remote_logic_service;
                if (logic != null)
                {
                    mDisconnected = true;
                    logic.Invoke(disconnect);
                }
            }
        }

        /// <summary>
        /// 从网络线程接受协议
        /// </summary>
        /// <param name="session"></param>
        /// <param name="route_codec"></param>
        /// <param name="binary"></param>
        /// <param name="cb"></param>
        public void connect_OnReceivedBinaryImmediately(TypeCodec route_codec, BinaryMessage binary, OnRpcBinaryReturn cb = null)
        {
            last_heartbeat = DateTime.Now;
            if (client_battle_action_codec.MessageID == route_codec.MessageID)
            {
                SendToArea(route_codec, binary);
            }
            else
            {
                this.Provider.Execute(new Action(do_async_OnReceivedBinaryImmediately));
                void do_async_OnReceivedBinaryImmediately()
                {
                    SendToLogic(route_codec, binary, cb);
                }
            }
        }

        /// <summary>
        /// 玩家进入游戏
        /// </summary>
        /// <param name="enter"></param>
        /// <param name="cb"></param>
        [RpcHandler(typeof(ClientEnterGameRequest), typeof(ClientEnterGameResponse), ServerNames.ConnectServerType)]
        public virtual async Task<ClientEnterGameResponse> client_rpc_Handle(ClientEnterGameRequest enter)
        {
            //log.Log("ClientEnterGameRequest");
            #region 账号封停验证.
            var statusSnap = await queryRoleDataStatusSnap.LoadDataAsync(enter.c2s_roleUUID);
            if (statusSnap != null)
            {
                //没过期代表已封停.
                if (!((DateTime.UtcNow - statusSnap.SuspendDate).TotalMilliseconds > 0))
                {
                    return (new ClientEnterGameResponse() { s2c_code = ClientEnterGameResponse.CODE_ROLE_SUSPEND, s2c_suspendTime = statusSnap.SuspendDate });
                }
            }


            #endregion
            last_heartbeat = DateTime.Now;
            this.enter_game = enter;
            bool reconnect = false;
            var rec = new SessionReconnectNotify();

            rec.sessionName = SelfAddress.ServiceName;
            if (enter_game != null)
            {
                rec.roleID = enter_game.c2s_roleUUID;
            }
            var logic = remote_logic_service;
            if (logic != null)
            {
                var oldRoleID = logic.Config["roleID"].ToString();
                if (oldRoleID != enter.c2s_roleUUID)
                {
                    log.WarnFormat(string.Format("Role Already Login : Acc={0} : Role={1} -> {2}", accountID, oldRoleID, enter.c2s_roleUUID));
                    //cb(new ClientEnterGameResponse() { s2c_code = ClientEnterGameResponse.CODE_LOGIC_ALREADY_LOGIN });
                    await logic.ShutdownAsync("switch role");
                    logic = await this.CreateLogicServiceAsync(enter);
                    //return;
                }
                else
                {
                    log.InfoFormat(string.Format("Role Reconnect : Acc={0} : Role={1}", accountID, enter.c2s_roleUUID));
                    reconnect = true;
                    rec.config = await InitConfig();
                }
            }
            else
            {
                log.InfoFormat(string.Format("Role Connect : Acc={0} : Role={1}", accountID, enter.c2s_roleUUID));
                logic = await this.CreateLogicServiceAsync(enter);
            }
            if (logic != null)
            {
                accountSave.SetField(nameof(AccountData.lastLoginRoleID), enter.c2s_roleUUID);
                await accountSave.FlushAsync();
                try
                {
                    mDisconnected = false;
                    var ret = await logic.CallAsync<ClientEnterGameResponse>(enter);
                    //log.Log("ClientEnterGameResponse: " + ret.IsSuccess);
                    return ret;
                }
                finally
                {
                    if (reconnect)
                    {
                        logic.Invoke(rec);
                    }
                }
            }
            else
            {
                return (new ClientEnterGameResponse() { s2c_code = ClientEnterGameResponse.CODE_LOGIC_NOT_FOUND, });
            }
        }
        /// <summary>
        /// 玩家离开游戏
        /// </summary>
        /// <param name="enter"></param>
        /// <param name="cb"></param>
        [RpcHandler(typeof(ClientExitGameRequest), typeof(ClientExitGameResponse), ServerNames.ConnectServerType)]
        public virtual async Task<ClientExitGameResponse> client_rpc_Handle(ClientExitGameRequest exit)
        {
            //log.Log("ClientExitGameRequest");
            last_heartbeat = DateTime.Now;
            await ShutdownLogicServiceAsync("player exit");
            this.remote_area_service = null;
            return new ClientExitGameResponse();
            //return Task.FromResult(new ClientExitGameResponse());
        }

        //--------------------------------------------------------------------------------------------------------------------------------------------

        [RpcHandler(typeof(SessionBindAreaNotify), ServerNames.AreaServiceType)]
        public virtual void area_rpc_Handle(SessionBindAreaNotify bind)
        {
            this.Provider.GetAsync(new RemoteAddress(bind.areaName, bind.areaNode)).ContinueWith(t =>
            {
                remote_area_service = t.GetResultAs();
            });
        }
        [RpcHandler(typeof(SessionUnbindAreaNotify), ServerNames.AreaServiceType)]
        public virtual void area_rpc_Handle(SessionUnbindAreaNotify msg)
        {
            remote_area_service = null;
        }

        // from service: TODO
        [RpcHandler(true)]
        public virtual void rpc_Handle(BinaryMessage msg)
        {
            if (mDisconnected) return;

            var session = this.session;
            if (session != null)
            {
                session.SocketSend(msg);
            }
        }
        public override void OnWormholeTransported(RemoteAddress from, object message)
        {
            var session = this.session;
            if (session != null)
            {
                if (message is BinaryMessage bin)
                {
                    session.socket.Send(bin);
                }
                else if (message is ISerializable ser)
                {
                    session.socket.Send(ser);
                }
            }

        }

        //--------------------------------------------------------------------------------------------------------------------------------------------

        //--------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 战斗协议 ClientBattleAction 直接发往AreaService
        /// </summary>
        /// <param name="action"></param>
        public virtual void SendToArea(TypeCodec route_codec, BinaryMessage action)
        {
            try
            {
                var area = remote_area_service;
                var enter = enter_game;
                if (area != null && enter != null)
                {
                    using (var output = IOStreamObjectPool.AllocOutputAutoRelease(ConnectServer.ClientCodec.Factory))
                    {
                        output.PutUTF(enter.c2s_roleUUID);
                        output.PutBytes(action.Buffer, action.BufferOffset, action.BufferLength);
                        var to_area = BinaryMessage.FromBuffer(session_battle_action_codec.MessageID, output.Buffer);
                        area.WormholeTransport(to_area);
                    }
                }
            }
            catch (Exception err)
            {
                log.Error(err);
            }

        }
        /// <summary>
        /// 逻辑协议发往LogicService
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="callback"></param>
        public virtual void SendToLogic(TypeCodec route_codec, BinaryMessage msg, OnRpcBinaryReturn callback = null)
        {
            var logic = remote_logic_service;
            if (logic != null)
            {
                if (callback != null)
                    logic.Call(msg, callback);
                else
                    logic.Invoke(msg);
            }
            else
            {
                log.Warn("SendToLogic Error : Logic Service Not Init : " + route_codec);
                if (callback != null) callback(BinaryMessage.NULL);
            }
        }

        protected virtual async Task<IRemoteService> CreateLogicServiceAsync(ClientEnterGameRequest enter_game)
        {
            var cfg = await InitConfig();
            var ret = await this.Provider.CreateAsync(ServerNames.GetLogicServiceAddress(enter_game.c2s_roleUUID, this.SelfAddress.ServiceNode), cfg);
            this.remote_logic_service = ret;
            return ret;
        }
        protected virtual async Task ShutdownLogicServiceAsync(string reason)
        {
            var logic = remote_logic_service;
            remote_logic_service = null;
            if (logic != null)
            {
                try
                {
                    await logic.CallAsync<SessionBeginLeaveResponse>(new SessionBeginLeaveRequest()
                    {
                        sessionName = SelfAddress.ServiceName,
                        roleID = enter_game.c2s_roleUUID,
                    });
                }
                catch (Exception err)
                {
                    log.Error(err.Message, err);
                }
                try
                {
                    var result = await logic.ShutdownAsync(reason);
                    log.Info("ShutdownAsync Complete : " + result);
                }
                catch (Exception err)
                {
                    log.Error("ShutdownAsync Error : " + err.Message, err);
                }
            }
        }

        private async Task<HashMap<string, string>> InitConfig()
        {
            HashMap<string, string> cfg = new HashMap<string, string>();

            var serverID = await accountSave.LoadFieldAsync<string>(nameof(AccountData.lastLoginServerID));
            var serverGroupID = await accountSave.LoadFieldAsync<string>(nameof(AccountData.lastLoginServerGroupID));
            //var privilege = await accountSave.LoadFieldAsync<RolePrivilege>(nameof(AccountData.privilege));
            cfg["sessionNode"] = this.SelfAddress.ServiceNode;
            cfg["sessionName"] = this.SelfAddress.ServiceName;
            cfg["accountID"] = enter.c2s_account;
            //cfg["privilege"] = privilege.ToString();
            cfg["roleID"] = enter_game.c2s_roleUUID;
            cfg["serverID"] = serverID;
            cfg["serverGroupID"] = serverGroupID;
            cfg["channel"] = enter.c2s_clientInfo.channel;
            cfg["passport"] = enter.c2s_clientInfo.sdkName;

            cfg["clientVersion"] = enter.c2s_clientInfo.clientVersion;
            cfg["deviceId"] = enter.c2s_clientInfo.deviceId;
            cfg["deviceModel"] = enter.c2s_clientInfo.deviceModel;
            cfg["deviceType"] = enter.c2s_clientInfo.deviceType;
            cfg["network"] = enter.c2s_clientInfo.network;
            cfg["region"] = enter.c2s_clientInfo.region;
            cfg["sdkName"] = enter.c2s_clientInfo.sdkName;
            cfg["sdkVersion"] = enter.c2s_clientInfo.sdkVersion;
            cfg["subChannel"] = enter.c2s_clientInfo.subChannel;
            cfg["userAgent"] = enter.c2s_clientInfo.userAgent;
            cfg["userSource1"] = enter.c2s_clientInfo.userSource1;
            cfg["userSource2"] = enter.c2s_clientInfo.userSource2;
            var ip = (this.session.socket.RemoteAddress as System.Net.IPEndPoint)?.Address?.ToString();
            cfg["clientIp"] = ip;
            cfg["platformAccount"] = enter.c2s_clientInfo.platformAcount;


            return cfg;
        }
    }



    /// <summary>
    /// Connect 进程内，通知SessionService绑定ViewSession
    /// </summary>
    public class LocalBindSessionRequest : Request, IRpcNoneSerializable
    {
        public ConnectServer.ViewSession session;
        public ClientEnterServerRequest enter;
    }
    public class LocalBindSessionResponse : Response, IRpcNoneSerializable
    {
        public SessionService session;
        public string sessionToken;
        public string serverGroupID;
    }

}
