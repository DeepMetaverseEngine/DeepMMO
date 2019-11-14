using DeepCore;
using DeepCore.Log;
using DeepCrystal.RPC;
using DeepMMO.Protocol;
using DeepMMO.Protocol.Client;
using DeepMMO.Server.Area;
using DeepMMO.Server.Connect;
using DeepMMO.Server.Logic.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DeepCore.GameEvent;
using DeepCore.GameEvent.Events;
using DeepCore.GameEvent.Message;
using DeepCrystal.ORM;
using System.Diagnostics;
using DeepMMO.Server.GameEvent;
using DeepCore.Xml;
using DeepCore.IO;
using DeepCrystal.Threading;
using DeepCrystal.Sql;
using DeepCore.Statistics;

namespace DeepMMO.Server.Logic
{
    public partial class LogicService : IService
    {

        public static bool ORM_TEST = false;
        public static int SAVE_EXPECT_TIME_LIMIT = 200;
        public static int LOAD_EXPECT_TIME_LIMIT = 200;
        public static TimeStatisticsRecoder Statistics { get; private set; } =
            new TimeStatisticsRecoder("LogicStatistics");

        /// <summary>
        /// 账号ID.
        /// </summary>
        public string accountID { get; private set; }
        /// <summary>
        /// 服务器ID.
        /// </summary>
        public string serverID { get; private set; }
        public string serverGroupID { get; private set; }
        public string sessionName { get; private set; }
        public string sessionNode { get; private set; }
        public string roleID { get; private set; }
        public string roleDigitID { get => roleModule.GetRoleData().digitID; }

        public ClientInfo clientInfo { get; private set; }

        public Logger log { get; private set; }
        public IRemoteService session { get; private set; }
        public LanguageManager Language { get => roleModule.Language; }
        public EventManager EventMgr { get; private set; }
        public IMappingAdapter DBAdapter { get; private set; }

        public event Action OnSessionReconnect;
        public event Action OnBeforeSaveData;
        public event Action OnClientEntered;
        private IDisposable mEventTimer;
        private IDisposable mSaveDataTimer;

        public bool IsClientEntered { get; private set; }

        public override ServiceProperties Properties
        {
            get
            {
                var ret = base.Properties;
                ret.IsConcurrent = false;
                return ret;
            }
        }

        public LogicService(ServiceStartInfo start) : base(start)
        {
            this.log = LoggerFactory.GetLogger(start.Address.ServiceName);
            this.sessionName = start.Config["sessionName"].ToString();
            this.sessionNode = start.Config["sessionNode"].ToString();
            this.accountID = start.Config["accountID"].ToString();
            this.serverID = start.Config["serverID"].ToString();
            this.roleID = start.Config["roleID"].ToString();
            this.serverGroupID = start.Config["serverGroupID"].ToString();
            this.clientInfo = new ClientInfo(start.Config);
            this.DBAdapter = ORMFactory.Instance.DefaultAdapter;
        }
        protected override async Task OnStartAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            this.session = await base.Provider.GetAsync(new RemoteAddress(sessionName, sessionNode));
            this.OnCreateModules();
            await this.OnModulesStartAsync();
            await this.OnModulesStartedAsync();
            this.EventMgr = EventManagerFactory.Instance.CreateEventManager("Player", roleID);
            if (EventMgr != null)
            {
                EventMgr.PutObject("Service", this);
                EventMgr.Start();
                mEventTimer = Provider.CreateTimer(OnEventManagerTick, this, TimeSpan.FromSeconds(0), TimeSpan.FromMilliseconds(300));
            }
            {
                //定期存数据.
                int interval = RPGServerManager.Instance.Config.timer_minute_SaveDataTimer;
                interval = Math.Max(5, interval);
                interval += (int)(interval * 0.25 * new Random().NextDouble());
                this.mSaveDataTimer = Provider.CreateTimer(OnFlushDataTick, this,
                    TimeSpan.FromMinutes(interval),
                    TimeSpan.FromMinutes(interval));
            }
            if (stopwatch.ElapsedMilliseconds > LOAD_EXPECT_TIME_LIMIT)
            {
                log.Warn("LogicService : OnStartAsync Use Time = " + stopwatch.Elapsed);
            }
            stopwatch.Stop();
        }


        private void OnEventManagerTick(object state)
        {
            EventMgr?.Update();
        }

        private void OnFlushDataTick(object state)
        {
            this.OnModulesSaveDataAsync().NoWait();
        }

        protected override async Task OnStopAsync(ServiceStopInfo reason)
        {
            IsClientEntered = false;
            mSaveDataTimer?.Dispose();
            await OnModulesStopAsync();
            EventMgr?.Dispose();
            if (reason.Event != ServiceStopInfo.ShutdownEvent.START_ERROR)
            {
                await OnModulesSaveDataAsync();
            }
            await OnModulesStopedAsync();
        }

        protected override void OnDisposed()
        {
            this.OnModulesDispose();
            OnSessionReconnect = null;
            OnBeforeSaveData = null;
            OnClientEntered = null;
            mEventTimer?.Dispose();
            EventMgr = null;
        }

        //---------------------------------------------------------------------------------------------------
        #region Modules

        public RoleModule roleModule { get; protected set; }
        public AreaModule areaModule { get; protected set; }
        private List<ILogicModule> modules = new List<ILogicModule>();

        protected virtual void OnCreateModules()
        {
            this.roleModule = RegistModule(new RoleModule(this));
            this.areaModule = RegistModule(new AreaModule(this));
        }
        protected T RegistModule<T>(T module) where T : ILogicModule
        {
            modules.Add(module);
            return module;
        }
        private async Task OnModulesStartAsync()
        {
            using (var list = CollectionObjectPool<ILogicModule>.AllocList(modules))
            {
                foreach (var module in list)
                {
                    try
                    {
                        await module.OnStartAsync();
                    }
                    catch (Exception err)
                    {
                        log.Error(ErrorBaseInfo() + err.Message, err);
                        throw;
                    }
                }
            }
        }
        private async Task OnModulesStartedAsync()
        {
            using (var list = CollectionObjectPool<ILogicModule>.AllocList(modules))
            {
                foreach (var module in list)
                {
                    try
                    {
                        await module.OnStartedAsync();
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                    }
                }
            }
        }
        private async Task OnModulesSaveDataAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            OnBeforeSaveData?.Invoke();
            if (ORM_TEST)
            {
                var watch_exe = CUtils.TickTimeMS;
                //var exe = new SyncTaskExecutor();
                var trans = DBAdapter.CreateExecutableObjectTransaction(this);
                using (var list = CollectionObjectPool<ILogicModule>.AllocList(modules))
                {
                    foreach (var module in list)
                    {
                        var watch = CUtils.TickTimeMS;
                        try
                        {
                            module.OnSaveData(trans);
                        }
                        catch (Exception err)
                        {
                            log.Error(err.Message, err);
                        }
                        finally
                        {
                            Statistics.LogTime($"{module.GetType().Name} : OnSaveData", CUtils.TickTimeMS - watch);
                        }
                    }
                }
                var test = new TestTransaction(CFiles.CurrentSubDir("/test_orm/"), this.roleID, trans, ORMFactory.Instance.DefaultAdapter, this);
                await trans.ExecuteAsync().ContinueWith(t =>
                {
                    Statistics.LogTime($"{GetType().Name} : OnModulesSaveDataAsync", CUtils.TickTimeMS - watch_exe);
                });
                await test.CheckAsync(true);
            }
            else
            {
                var watch_exe = CUtils.TickTimeMS;
                var trans = DBAdapter.CreateExecutableObjectTransaction(this);
                using (var list = CollectionObjectPool<ILogicModule>.AllocList(modules))
                {
                    foreach (var module in list)
                    {
                        var watch = CUtils.TickTimeMS;
                        try
                        {
                            module.OnSaveData(trans);
                        }
                        catch (Exception err)
                        {
                            log.Error(err.Message, err);
                        }
                        finally
                        {
                            Statistics.LogTime($"{module.GetType().Name} : OnSaveData", CUtils.TickTimeMS - watch);
                        }
                    }
                }
                await trans.ExecuteAsync().ContinueWith(t =>
                {
                    Statistics.LogTime($"{GetType().Name} : OnModulesSaveDataAsync", CUtils.TickTimeMS - watch_exe);
                });
            }
            if (stopwatch.ElapsedMilliseconds > SAVE_EXPECT_TIME_LIMIT)
            {
                log.Warn("LogicService : OnModulesSaveDataAsync Flush Time = " + stopwatch.Elapsed);
            }
            stopwatch.Stop();
        }

        private async Task OnModulesStopAsync()
        {
            using (var list = CollectionObjectPool<ILogicModule>.AllocList(modules))
            {
                list.Reverse();
                foreach (var module in list)
                {
                    try
                    {
                        await module.OnStopAsync();
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                    }
                }
            }
        }
        private async Task OnModulesStopedAsync()
        {
            using (var list = CollectionObjectPool<ILogicModule>.AllocList(modules))
            {
                list.Reverse();
                foreach (var module in list)
                {
                    try
                    {
                        await module.OnStopedAsync();
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                    }
                }
            }
        }
        private async Task NotifyModulesClientEnterGameAsync()
        {
            IsClientEntered = true;
            OnClientEntered?.Invoke();
            using (var list = CollectionObjectPool<ILogicModule>.AllocList(modules))
            {
                foreach (var module in list)
                {
                    try
                    {
                        await module.OnClientEnterGameAsync();
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                    }
                }
            }
        }

        private void OnModulesDispose()
        {
            using (var list = CollectionObjectPool<ILogicModule>.AllocList(modules))
            {
                list.Reverse();
                foreach (var module in list)
                {
                    try
                    {
                        module.Dispose();
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                    }
                }
            }
            modules.Clear();
        }
        private void OnModulesSessionReconnect()
        {
            using (var list = CollectionObjectPool<ILogicModule>.AllocList(modules))
            {
                foreach (var module in list)
                {
                    try
                    {
                        module.OnSessionReconnect();
                    }
                    catch (Exception err)
                    {
                        log.Error(err.Message, err);
                    }
                }
            }
        }

        private string ErrorBaseInfo()
        {
            return string.Format("ServerID:{0}|AccountID:{1}|RoleID:{2} ", serverID, accountID, roleID);
        }

        #endregion
        //---------------------------------------------------------------------------------------------------
        #region Area

        /// <summary>
        /// Area通知逻辑需要传送操作，一般是踩到场景传送点
        /// </summary>
        /// <param name="tp"></param>
        [RpcHandler(typeof(RoleNeedTransportNotify), ServerNames.AreaServiceType)]
        public void area_rpc_Handle(RoleNeedTransportNotify tp)
        {
            var task = areaModule.RequestTransportAsync(tp);
        }

        /// <summary>
        /// Area通知逻辑服无缝切场景
        /// </summary>
        /// <param name="tp"></param>
        [RpcHandler(typeof(RoleCrossMapNotify), ServerNames.AreaServiceType)]
        public void area_rpc_Handle(RoleCrossMapNotify notify)
        {
            areaModule.RequestCrossMapAsync(notify).NoWait();
        }
        
        [RpcHandler(typeof(AreaGameOverNotify), ServerNames.AreaServiceType)]
        public void area_rpc_Handle(AreaGameOverNotify notify)
        {
            areaModule.DoAreaGameOverNotify(notify);
        }

        #endregion
        //---------------------------------------------------------------------------------------------------
        #region Session

        /// <summary>
        /// 玩家断开连接
        /// </summary>
        /// <param name="disconnect"></param>
        [RpcHandler(typeof(SessionDisconnectNotify), ServerNames.SessionServiceType)]
        public virtual void session_rpc_Handle(SessionDisconnectNotify disconnect)
        {
            var area = areaModule.currentArea;
            if (area != null)
            {
                disconnect.roleID = this.roleID;
                area.Invoke(disconnect);
            }
        }
        /// <summary>
        /// 玩家重新连接
        /// </summary>
        /// <param name="disconnect"></param>
        [RpcHandler(typeof(SessionReconnectNotify), ServerNames.SessionServiceType)]
        public virtual void session_rpc_Handle(SessionReconnectNotify reconnect)
        {
            this.clientInfo = new ClientInfo(reconnect.config);
            var area = areaModule.currentArea;
            if (area != null)
            {
                reconnect.roleID = this.roleID;
                area.Invoke(reconnect);
            }

            this.OnModulesSessionReconnect();
            OnSessionReconnect?.Invoke();
        }

        [RpcHandler(typeof(SessionBeginLeaveRequest), typeof(SessionBeginLeaveResponse), ServerNames.SessionServiceType)]
        public virtual void session_rpc_Handle(SessionBeginLeaveRequest disconnect, OnRpcReturn<SessionBeginLeaveResponse> cb)
        {
            var area = areaModule.currentArea;
            if (area != null)
            {
                area.Call(disconnect, cb);
            }
            else
            {
                cb(new SessionBeginLeaveResponse() { s2c_code = Response.CODE_ERROR });
            }
        }



        [RpcHandler(typeof(ClientEnterGameRequest), typeof(ClientEnterGameResponse), ServerNames.SessionServiceType)]
        public void client_rpc_Handle(ClientEnterGameRequest enter, OnRpcReturn<ClientEnterGameResponse> cb)
        {
            cb(new ClientEnterGameResponse() { s2c_role = this.roleModule.ToClientRoleData() });
            Provider.Execute(NotifyModulesClientEnterGameAsync);
        }

        /// <summary>
        /// 测试客户端Ping，Pong
        /// </summary>
        [RpcHandler(typeof(ClientPing), typeof(ClientPong))]
        public virtual void client_rpc_Handle(ClientPing ping, OnRpcReturn<ClientPong> cb)
        {
#if TEST
                session.Invoke(new LogicTimeNotify() { index = 0, time = ping.time });
                session.Invoke(new LogicTimeNotify() { index = 1, time = ping.time });
                cb(new ClientPong()
                {
                    s2c_code = (ping.time.Millisecond % 2 == 0) ? Response.CODE_OK : Response.CODE_ERROR,
                    s2c_msg = DateTime.Now.ToString(),
                    time = ping.time
                });
                session.Invoke(new LogicTimeNotify() { index = 2, time = ping.time });
                session.Invoke(new LogicTimeNotify() { index = 3, time = ping.time });
#else
            cb(new ClientPong() { s2c_code = Response.CODE_OK, time = ping.time });
#endif
        }

        [RpcHandler(typeof(ServerGameEventNotify))]
        public virtual void rpc_event_notify(ServerGameEventNotify ntf)
        {
            if (EventMgr != null && (string.IsNullOrEmpty(ntf.ServerGroupID) || ntf.ServerGroupID == serverGroupID))
            {
                EventMgr.OnReceiveMessage(EventMessage.FromBytes(ntf.EventMessageData));
            }
        }

        [RpcHandler(typeof(ClientGameEventNotify))]
        public virtual void client_rpc_notify(ClientGameEventNotify ntf)
        {
            var msg = EventMessage.FromBytes(ntf.EventMessageData);
            if (msg is NamedEventMessage nameMsg)
            {
                nameMsg.From = EventMgr?.Address;
            }
            var address = EventManagerAddress.Parse(msg.From);
            address = new EventManagerAddress("Client", address.UUID);
            msg.From = address.Address;
            EventManager.MessageBroker.Publish(ntf.To, EventMgr, msg);
        }


        [RpcHandler(typeof(ClientGetZoneInfoSnapRequest), typeof(ClientGetZoneInfoSnapResponse))]
        public virtual Task<ClientGetZoneInfoSnapResponse> client_rpc_Handle(ClientGetZoneInfoSnapRequest req)
        {
            return areaModule.DoClientGetZoneInfoSnapRequest(req);
        }

        [RpcHandler(typeof(ClientChangeZoneLineRequest), typeof(ClientChangeZoneLineResponse))]
        public virtual Task<ClientChangeZoneLineResponse> client_rpc_Handle(ClientChangeZoneLineRequest req)
        {
            return areaModule.DoClientChangeZoneLineRequest(req);
        }
        #endregion

        public class ClientInfo
        {
            public string Os { get; private set; }
            public string Network { get; private set; }
            public string AppVersion { get; private set; }
            public string AppVersionCode { get; private set; }
            public string OsVersion { get; private set; }
            public string SdkVersion { get; private set; }
            public string DeviceBrand { get; private set; }
            public string DeviceModel { get; private set; }
            public string DeviceScreen { get; private set; }
            public string Mac { get; private set; }
            public string Imei { get; private set; }
            public string Uuid { get; private set; }
            public string PackageName { get; private set; }
            public string BuildNumber { get; private set; }
            public string Carrier { get; private set; }
            public string Iccid { get; private set; }
            public string Imsi { get; private set; }
            public string Idfa { get; private set; }
            public string ClientIp { get; private set; }
            public string DeviceID { get; private set; }
            public string Channel { get; private set; }
            public string SDKName { get; private set; }
            public string PlatformAccount { get; private set; }

            public ClientInfo(HashMap<string, string> config)
            {
                this.Os = config["deviceType"]?.ToString();
                this.Os = SQLFactory.CurrentFactory.EscapeString(Os);

                this.Network = config["network"]?.ToString();
                this.Network = SQLFactory.CurrentFactory.EscapeString(Network);

                this.AppVersion = config["clientVersion"]?.ToString();
                this.AppVersion = SQLFactory.CurrentFactory.EscapeString(AppVersion);

                this.AppVersionCode = null;
                this.OsVersion = null;
                this.SdkVersion = config["sdkVersion"]?.ToString();
                this.SdkVersion = SQLFactory.CurrentFactory.EscapeString(SdkVersion);

                this.DeviceBrand = null;
                this.DeviceModel = config["deviceModel"]?.ToString();
                this.DeviceModel = SQLFactory.CurrentFactory.EscapeString(DeviceModel);

                this.DeviceScreen = null;
                this.Mac = config["deviceId"]?.ToString();
                this.Mac = SQLFactory.CurrentFactory.EscapeString(Mac);
                this.Imei = null;
                this.Uuid = config["deviceId"]?.ToString();
                this.Uuid = SQLFactory.CurrentFactory.EscapeString(Uuid);
                this.PackageName = null;
                this.BuildNumber = null;
                this.Carrier = null;
                this.Iccid = null;
                this.Imsi = null;
                this.Idfa = config["deviceId"]?.ToString();
                this.Idfa = SQLFactory.CurrentFactory.EscapeString(Idfa);
                this.ClientIp = config["clientIp"]?.ToString();
                this.ClientIp = SQLFactory.CurrentFactory.EscapeString(ClientIp);
                this.DeviceID = config["deviceId"]?.ToString();
                this.DeviceID = SQLFactory.CurrentFactory.EscapeString(DeviceID);
                this.Channel = config["channel"]?.ToString();
                this.Channel = SQLFactory.CurrentFactory.EscapeString(Channel);
                if (string.IsNullOrEmpty(Channel))
                    this.Channel = "0";

                this.SDKName = config["sdkName"]?.ToString();
                this.SDKName = SQLFactory.CurrentFactory.EscapeString(SDKName);
                this.PlatformAccount = config["platformAccount"]?.ToString();
            }
        }
        //---------------------------------------------------------------------------------------------------
    }
}
