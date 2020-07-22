using DeepCore;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Reflection;
using DeepCrystal.FuckPomeloServer;
using DeepCrystal.ORM.Generic;
using DeepCrystal.RPC;
using DeepMMO.Data;
using DeepMMO.Protocol.Client;
using DeepMMO.Server.SystemMessage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DeepMMO.Server.Gate
{
    public class GateServer : IService
    {
        /// <summary>
        /// 控制账号是否可以登录，默认为false,特殊权限账号忽略该配置.
        /// </summary>
        private bool mServerOpenFlag = false;
        public bool ServerOpenFlag { get => mServerOpenFlag; set => mServerOpenFlag = value; }

        private static Random random = new Random();

        protected readonly Logger log;
        protected readonly IServer acceptor;
        protected readonly ConnectorNodeMap groupMap;
        protected IRemoteNodeInfo[] realmNodes;

        protected bool isReady = false;
        /// <summary>
        /// 最小连接数量，由GM系统控制。
        /// </summary>
        public static int MIN_CLIENT_NUMBER_SOFT_LIMIT = 50;

        /// <summary>
        /// 最大连接数量，由GM系统控制。
        /// </summary>
        public static int MAX_CLIENT_NUMBER_SOFT_LIMIT = 100;

        /// <summary>
        /// 最大连接数量，由GM系统控制。
        /// </summary>
        public static int CLIENT_NUMBER_HARD_LIMIT = 100;
        
        /// <summary>
        /// 最大连接数量软限制，由GM系统控制。
        /// </summary>
        public static int CLIENT_NUMBER_SOFT_LIMIT = 10;

        /// <summary>
        /// 排队的最大数量限制，由GM系统控制。
        /// </summary>
        public static int QUEUE_MAX_LIMIT = 1;

        /// <summary>
        /// 每一名角色增加时间，由GM系统控制。
        /// </summary>
        public static int QUEUE_ADD_TIME = 30;

        /// <summary>
        /// 每一名角色增加时间，由GM系统控制。
        /// </summary>
        public static int QUEUE_MAX_HARD_ADD_TIME = 300;

        /// <summary>
        /// 间隔数量
        /// </summary>
        public static int POST_SERVER_NUMBER_INTERVAL = 5;

        /// <summary>
        /// 软进入百分比
        /// </summary>
        public static float SOFT_ENTER_PERCENTAGE = 0.1f;

        /// <summary>
        /// 硬进入百分比
        /// </summary>
        public static float HARD_ENTER_PERCENTAGE = 0.1f;

        /// <summary>
        /// 硬进入最大百分比
        /// </summary>
        public static float MAX_HARD_ENTER_PERCENTAGE = 0.08f;

        //------------------------------------------------------------------------------------------
        //public override bool IsConcurrent => false;
        //------------------------------------------------------------------------------------------
        public GateServer(ServiceStartInfo start) : base(start)
        {
            var factory = ServerFactory.Instance;
            var codec = ReflectionUtil.CreateInterface<IExternalizableFactory>(start.Config["NetCodec"].ToString());
            this.log = LoggerFactory.GetLogger(start.Address.FullPath);
            if (!string.IsNullOrEmpty(DeepMMO.Server.GlobalConfig.ReplaceNetHost))
            {
                start.Config["Host"] = DeepMMO.Server.GlobalConfig.ReplaceNetHost;
            }
            this.acceptor = factory.CreateServer(new HashMap<string, string>(start.Config), codec);
            this.acceptor.OnSessionConnected += Acceptor_OnSessionConnected;
            this.acceptor.OnSessionDisconnected += Acceptor_OnSessionDisconnected;
            this.acceptor.OnServerError += Acceptor_OnServerError;
            this.groupMap = new ConnectorNodeMap(log);

            // H.Q.Cai 添加代码开始
            start.Config.TryGetAsInt("ClientNumberHardLimit", out CLIENT_NUMBER_HARD_LIMIT);
            start.Config.TryGetAsInt("ClientNumberSoftLimit", out CLIENT_NUMBER_SOFT_LIMIT);
            start.Config.TryGetAsInt("QueueMaxLimit", out QUEUE_MAX_LIMIT);
            start.Config.TryGetAsInt("QueueAddTime", out QUEUE_ADD_TIME);
            start.Config.TryGetAsInt("QueueMaxHardAddTime", out QUEUE_MAX_HARD_ADD_TIME);
            start.Config.TryGetAsInt("PostServerNumberInterval", out POST_SERVER_NUMBER_INTERVAL);
            start.Config.TryGetAsFloat("SoftEnterPercentage", out SOFT_ENTER_PERCENTAGE);
            start.Config.TryGetAsFloat("HardEnterPercentage", out HARD_ENTER_PERCENTAGE);
            start.Config.TryGetAsFloat("MaxHardEnterPercentage", out MAX_HARD_ENTER_PERCENTAGE);

            if(start.Config.TryGetAsInt("MinClientNumberSoftLimit", out var minClientNumberSoftLimit))
            {
                MIN_CLIENT_NUMBER_SOFT_LIMIT = minClientNumberSoftLimit;
            }

            if (start.Config.TryGetAsInt("MaxClientNumberSoftLimit", out var maxClientNumberSoftLimit))
            {
                MAX_CLIENT_NUMBER_SOFT_LIMIT = maxClientNumberSoftLimit;
            }
            // H.Q.Cai 添加代码结束
        }
        protected override void OnDisposed()
        {
            this.groupMap.Dispose();
        }
        protected override Task OnStartAsync()
        {
            foreach (var server in RPGServerTemplateManager.Instance.GetAllServers())
            {
                log.WarnFormat("Templates Server:{0} Group:{1} State:{2}", server.id, server.group, server.state);
            }
            var interval = TimeSpan.FromSeconds(TimerConfig.timer_sec_GateUpdateQueue);
            //var interval = TimeSpan.FromSeconds(3);
            this.Provider.CreateTimer(UpdateInQueue, this, interval, interval);
            return Task.CompletedTask;
        }
        protected override Task OnStopAsync(ServiceStopInfo reason)
        {
            this.acceptor.Dispose();
            return Task.FromResult(0);
        }
        public override bool GetState(TextWriter output)
        {
            this.groupMap.WriteStatus(output);
            return true;
        }
        //------------------------------------------------------------------------------------------

        [RpcHandler(typeof(SystemShutdownNotify))]
        public virtual void rpc_HandleSystem(SystemShutdownNotify shutdown)
        {
            this.acceptor.StopAsync(shutdown.reason);
        }
        [RpcHandler(typeof(SystemGateReloadServerList))]
        public virtual void rpc_HandleReloadServerList(SystemGateReloadServerList reload)
        {
            this.groupMap.ReloadServerGroups();
        }
        [RpcHandler(typeof(SystemStaticServicesStartedNotify))]
        public virtual void rpc_HandleSystem(SystemStaticServicesStartedNotify shutdown)
        {
            base.Execute(async () =>
            {
                using (var log = StringBuilderObjectPool.AllocAutoRelease())
                {
                    log.WriteLine(CUtils.SequenceChar('-', 100));
                    log.WriteLine("- 获取查询远端服务示例");
                    log.WriteLine(CUtils.SequenceChar('-', 100));
                    {
                        log.WriteLine("Provider.GetServicesWithAddressPatternAsync(\"\\w+@\\w+@ConnectServer\")");
                        var svcs = await base.Provider.GetServicesWithAddressPatternAsync(@"\w+@\w+@ConnectServer");
                        foreach (var svc in svcs) { log.WriteLine(svc); }
                    }
                    log.WriteLine(CUtils.SequenceChar('-', 100));
                    {
                        log.WriteLine("Provider.GetServicesWithInfoLinqAsync(\"Address.ServiceType =\\\"AreaService\\\"\", \"Address.ServiceName\")");
                        var svcs = await base.Provider.GetServicesWithInfoLinqAsync("Address.ServiceType=\"AreaService\"", "Address.ServiceName");
                        foreach (var svc in svcs) { log.WriteLine(svc); }
                    }
                    log.WriteLine(CUtils.SequenceChar('-', 100));
                    {
                        log.WriteLine("Provider.GetStaticServicesAsync()");
                        var svcs = await base.Provider.GetStaticServicesAsync();
                        foreach (var svc in svcs) { log.WriteLine(svc); }
                    }
                    log.WriteLine(CUtils.SequenceChar('-', 100));
                    {
                        log.WriteLine("Provider.FindStaticServiceWithTypeAsync(\"RankingService\")");
                        log.WriteLine("根据分组1，获取Ranking服务分片");
                        var group = 1;
                        var svc = await base.Provider.FindStaticServiceWithTypeAsync(ServerNames.RankingServiceType, (list) =>
                        {
                            if (list.Length == 1) return list[0];
                            if (list.Length > 0)
                            {
                                var rank_group = (group % list.Length).ToString();
                                return Array.Find(list, e =>
                                {
                                    return e.Config["IndexId"] == rank_group;
                                });
                            }
                            return null;
                        });
                        log.WriteLine(svc);
                    }
                    log.WriteLine(CUtils.SequenceChar('-', 100));
                    this.log.Info("\n" + log.ToString());
                }
            });
        }

        //------------------------------------------------------------------------------------------
        [RpcHandler(typeof(Ping), typeof(Pong))]
        public virtual Task<Pong> rpc_OnHandlePing(Ping msg)
        {
            log.Info("ping index = " + msg.index);
            //log.Info("on rpc_OnHandle All : " + msg);
            //await Task.Delay(random.Next()%1000);
            return Task.FromResult(new Pong() { time = msg.time, index = msg.index });
        }

        [RpcHandler(typeof(SyncConnectToGateNotify), ServerNames.ConnectServerType)]
        public virtual void rpc_OnHandleConnector(SyncConnectToGateNotify msg)
        {
            //  log.Info("on SyncConnectToGateNotify : " + msg.connectAddress);
            if (groupMap.SyncConnect(msg))
            {
                if (isReady == false)
                {
                    isReady = true;
                    this.acceptor.StartAsync();
                    log.Info($"Gate Service Is Ready ! Port={this.StartConfig["Port"]}");
                }
            }
            this.LogState();
        }

        //------------------------------------------------------------------------------------------
        [RpcHandler(typeof(SyncGateServerOpen))]
        public virtual void rpc_HandleSystem(SyncGateServerOpen notify)
        {
            this.ServerOpenFlag = notify.status;
        }

        [RpcHandler(typeof(SyncGateClientNumberLimit))]
        public virtual void rpc_HandleClientLimit(SyncGateClientNumberLimit notify)
        {
            this.groupMap.SetClientLimit(notify);
        }

        [RpcHandler(typeof(SyncGateClientAccountExpire), ServerNames.LogicServiceType)]
        public virtual void rpc_HandleClientLimit(SyncGateClientAccountExpire notify)
        {
            PushOtherAccountExpire(notify.accountUUid, notify.ExpectTime);
        }

        //------------------------------------------------------------------------------------------

        /// <summary>
        /// 选择最优服务器
        /// </summary>
        /// <returns></returns>
        protected virtual async Task<EnterToken> SelectConnectAsync(ClientEnterGateRequest login, ISession session)
        {
            //log.Log(login);
            try
            {
                //账号/密码/客户端信息为空//
                if (string.IsNullOrEmpty(login.c2s_account) || string.IsNullOrEmpty(login.c2s_token) || login.c2s_clientInfo == null)
                {
                    log.Warn("账号/密码/客户端信息为空");
                    return new EnterToken(login, new ClientEnterGateResponse()
                    {
                        s2c_code = ClientEnterGateResponse.CODE_ACCOUNT_OR_PASSWORD,
                    });
                }

                //ServerGroup是否存在//
                var serverGroupID = RPGServerTemplateManager.Instance.GetServerGroupID(login.c2s_serverID);
                if (serverGroupID == null)
                {
                    log.Warn("ServerGroup不存在");
                    return new EnterToken(login, new ClientEnterGateResponse()
                    {
                        s2c_code = ClientEnterGateResponse.CODE_SERVER_NOT_OPEN,
                    });
                }

                //第三方/一号通验证//
                var serverPassportResult = await RPGServerManager.Instance.Passport.VerifyAsync(login);

                //保存渠道Account.
                string platformAccount = login.c2s_account;

                if (serverPassportResult.Verified == false)
                {
                    log.Warn("一号通验证失败");
                    return new EnterToken(login, new ClientEnterGateResponse()
                    {
                        s2c_code = ClientEnterGateResponse.CODE_ACCOUNT_OR_PASSWORD,
                    });
                }
                //统一登陆用户名，解决多渠道名称冲突//
                var accountUUID = await RPGServerManager.Instance.Passport.FormatAccountAsync(login);
                if (accountUUID == null)
                {
                    log.Warn("AccountID不合法");
                    return new EnterToken(login, new ClientEnterGateResponse()
                    {
                        s2c_code = ClientEnterGateResponse.CODE_ACCOUNT_OR_PASSWORD,
                    });
                }

                using (var saveAcc = new MappingReference<AccountData>(RPGServerPersistenceManager.TYPE_ACCOUNT_DATA, accountUUID, this))
                {
                    var accountData = await RPGServerPersistenceManager.Instance.GetOrCreateAccountDataAsync(saveAcc, accountUUID, login.c2s_token);
                    if (accountData == null)
                    {
                        log.Warn("无法创建账号");
                        return new EnterToken(login, new ClientEnterGateResponse()
                        {
                            s2c_code = ClientEnterGateResponse.CODE_NO_ACCOUNT,
                        });
                    }

                    //特殊密码进入游戏时，修改权限.
                    if (accountData.lastLoginServerID == null && serverPassportResult.Privilege != 0)
                    {
                        accountData.privilege = (RolePrivilege)serverPassportResult.Privilege;
                        saveAcc.SetField(nameof(AccountData.privilege), accountData.privilege);
                    }

                    //var privilege = accountData.privilege;
                    //是否有白名单权限.//
                    bool hasPrivilege = RPGServerTemplateManager.Instance.IsValidOfPrivilege((int)accountData.privilege, "white_list");
                    //服务器状态是否开启.//
                    if (!hasPrivilege && !ServerOpenFlag)
                    {
                        log.Warn("服务器未开启");
                        return new EnterToken(login, new ClientEnterGateResponse()
                        {
                            s2c_code = ClientEnterGateResponse.CODE_SERVER_NOT_OPEN,
                        });
                    }

                    //获取Connector负载//
                    if (!groupMap.TryDispatchConnect(serverGroupID, accountData.lastLoginConnectAddress, out var group, out var connect, out var inQueue, out var queueCount, out var inMaxQueue))
                    {
                        log.Warn("没有可用服务器");
                        return new EnterToken(login, new ClientEnterGateResponse()
                        {
                            s2c_code = ClientEnterGateResponse.CODE_NO_CONNECT_SERVER,
                        });
                    }

                    var loginToken = CMD5.CalculateMD5(random.Next().ToString() + accountUUID);
                    saveAcc.SetField(nameof(AccountData.lastLoginRemoteAddress), session.RemoteAddress.ToString());
                    saveAcc.SetField(nameof(AccountData.lastLoginTime), DateTime.Now);
                    saveAcc.SetField(nameof(AccountData.lastLoginConnectAddress), connect.Sync.connectServiceAddress);
                    saveAcc.SetField(nameof(AccountData.lastLoginToken), loginToken);
                    saveAcc.SetField(nameof(AccountData.lastLoginServerID), login.c2s_serverID);
                    saveAcc.SetField(nameof(AccountData.lastLoginServerGroupID), serverGroupID);
                    await saveAcc.FlushAsync();

                    //角色列表//
                    List<RoleIDSnap> roleList = new List<RoleIDSnap>();
                    using (var accountRoleSnapSave = new MappingReference<AccountRoleSnap>(RPGServerPersistenceManager.TYPE_ACCOUNT_ROLE_SNAP_DATA, accountUUID, this))
                    {
                        var accountRoleSnap = await accountRoleSnapSave.LoadOrCreateDataAsync(() => new AccountRoleSnap());
                        foreach (var item in accountRoleSnap.roleIDMap)
                        {
                            roleList.Add(item.Value);
                        }
                    }

                    //软性连接数限制//
                    var isContains = ContainsAccount(session, accountUUID);

                    var s2c_code = ClientEnterGateResponse.CODE_OK;
                    if (inQueue && isContains == false)
                    {
                        s2c_code = ClientEnterGateResponse.CODE_OK_IN_QUEUE;
                    }

                    //if (inMaxQueue && isContains == false)
                    //{
                    //    s2c_code = ClientEnterGateResponse.CODE_SERVER_MAX_QUEUE;
                    //}

                    return new EnterToken(login, new ClientEnterGateResponse()
                    {
                        s2c_code = s2c_code,
                        s2c_accountUUID = accountUUID,
                        s2c_connectHost = connect.Sync.connectHost,
                        s2c_connectPort = connect.Sync.connectPort,
                        s2c_connectToken = connect.Sync.connectToken,
                        s2c_lastLoginToken = accountData.lastLoginToken,
                        s2c_lastLoginRoleID = accountData.lastLoginRoleID,
                        s2c_platformAccount = platformAccount,
                        s2c_roleIDList = roleList,
                        s2c_queueCount = queueCount,
                        s2c_queuetTime = TimeSpan.FromSeconds(10),
                    }, group, connect, saveAcc.Data);
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                throw;
            }
        }


        //----------------------------------------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------------------------------------
        public class ConnectorNodeMap : Disposable
        {
            protected Logger log;
            private ReaderWriterLockSlim connect_lock = new ReaderWriterLockSlim();
            // 强制ServerGroup进入指定Node
            private ValueSortedMap<string, GroupInfo> groupMap;
            // 用于记录老的Session重新连接后，匹配到之前进入的Node信息
            private ValueSortedMap<string, NodeInfo> nodeMap;
            // 所有Connector信息
            private ValueSortedMap<string, ConnectInfo> connectorMap;

            public ConnectorNodeMap(Logger log)
            {
                this.log = log;
                this.groupMap = new ValueSortedMap<string, GroupInfo>((a, b) => a.ClientNumber - b.ClientNumber);
                this.nodeMap = new ValueSortedMap<string, NodeInfo>((a, b) => a.ClientNumber - b.ClientNumber);
                this.connectorMap = new ValueSortedMap<string, ConnectInfo>((a, b) => a.ClientNumber - b.ClientNumber);
                this.ReloadServerGroups();
            }
            public void ReloadServerGroups()
            {
                log.Warn("ConnectorNodeMap: ReloadServerGroups");
                using (connect_lock.EnterWrite())
                {
                    foreach (var server in RPGServerTemplateManager.Instance.GetAllServers())
                    {
                        var group = groupMap.GetOrAdd(server.group, (g) => new GroupInfo(g));
                        if (server.nodes != null)
                        {
                            foreach (var node in server.nodes)
                            {
                                if (!string.IsNullOrEmpty(node))
                                {
                                    var nodeConnectors = nodeMap.GetOrAdd(node, (name) => new NodeInfo(name));
                                    group.BindNode(nodeConnectors);
                                }
                            }
                        }
                    }
                }
            }
            protected override void Disposing()
            {
                connect_lock.Dispose();
            }
            public void SetClientLimit(SyncGateClientNumberLimit notify)
            {
                using (connect_lock.EnterWrite())
                {
                    if (groupMap.TryGetValue(notify.serverGroupID, out var group))
                    {
                        // H.Q.Cai 修改开始
                        //group.SetClientLimit(notify.clientLimit);
                        group.SetClientLimit(notify.clientLimit, notify.clientSoftLimit, notify.queueMaxLimit, notify.queueAddTime);
                        // H.Q.Cai 修改结束
                    }
                }
            }
            public bool SyncConnect(SyncConnectToGateNotify msg)
            {
                var msgAddr = RemoteAddress.Parse(msg.connectServiceAddress);
                using (connect_lock.EnterWrite())
                {
                    bool ret = false;
                    //纯数据同步//
                    {
                        var exist = connectorMap.TryGetOrCreate(msg.connectServiceAddress, out var conn, (addr) => new ConnectInfo(msg));
                        if (exist) { conn.Sync = (msg); }
                        var nodeConnectors = nodeMap.GetOrAdd(msgAddr.ServiceNode, (name) => new NodeInfo(name));
                        nodeConnectors.SyncConnect(msgAddr, conn);
                        ret = !exist;
                    }
                    //刷新每个Group用户数量//
                    {
                        foreach (var g in groupMap.Values)
                        {
                            g.Sync(connectorMap);
                        }
                    }
                    //刷新排序//
                    {
                        connectorMap.MarkSort();
                        nodeMap.MarkSort();
                        groupMap.MarkSort();
                    }
                    return ret;
                }
            }

            public void UpdateInQueue(object state)
            {
                using (connect_lock.EnterWrite())
                {
                    //处理ServerGroup中等待队列//
                    foreach (var g in groupMap.Values)
                    {
                        try
                        {
                            g.UpdateInQueue(state);
                        }
                        catch (Exception err)
                        {
                            log.Error(err.Message, err);
                        }
                    }
                }
            }

            public bool TryDispatchConnect(string serverGroupID, string expectConnectAddress, out GroupInfo group, out ConnectInfo conn, out bool inQueue, out int queueCount, out bool inMaxQueue)
            {
                var expectAddr = RemoteAddress.Parse(expectConnectAddress);
                using (connect_lock.EnterRead())
                {
                    // 通过ServerGroupID匹配 //
                    if (groupMap.TryGetValue(serverGroupID, out group))
                    {
                        if (group.TryDispatchConnect(expectAddr, out conn))
                        {
                            inQueue = group.IsNeedQueue;
                            queueCount = group.QueueCount;
                            group.PushClientNumber();

                            inMaxQueue = group.IsMaxQueue;

                            return group.IsAllowHardEnter;
                            //return true;
                        }
                        else
                        {
                            // 如果预期进入场景 //
                            if (expectAddr.NotNull)
                            {
                                if (nodeMap.TryGetValue(expectAddr.ServiceNode, out var connectors))
                                {
                                    if (connectors.TryDispatchConnect(expectAddr, out conn))
                                    {
                                        inQueue = group.IsNeedQueue;
                                        queueCount = group.QueueCount;
                                        group.PushClientNumber();

                                        inMaxQueue = group.IsMaxQueue;

                                        return group.IsAllowHardEnter;
                                        //return true;
                                    }
                                }
                            }
                            // 兜底取所有Connect //
                            if (connectorMap.TryGetRandomFirst(random, out conn))
                            {
                                inQueue = group.IsNeedQueue;
                                queueCount = group.QueueCount;
                                group.PushClientNumber();

                                inMaxQueue = group.IsMaxQueue;

                                return group.IsAllowHardEnter;
                                //return true;
                            }
                        }
                    }
                }
                inQueue = false;
                queueCount = 0;
                conn = null;
                inMaxQueue = false;
                return false;
            }
            public void WriteStatus(TextWriter sb)
            {
                using (connect_lock.EnterRead())
                {
                    sb.WriteLine(CUtils.SequenceChar('-', 100));
                    sb.WriteLine("- Group Map -");
                    sb.WriteLine(CUtils.SequenceChar('-', 100));
                    foreach (var group in groupMap.ToSortedArray())
                    {
                        group.WriteStatus(sb, string.Empty);
                    }
                    sb.WriteLine(CUtils.SequenceChar('-', 100));
                    sb.WriteLine("- Node Map -");
                    sb.WriteLine(CUtils.SequenceChar('-', 100));
                    foreach (var conns in nodeMap.ToSortedArray())
                    {
                        conns.WriteStatus(sb, string.Empty);
                    }
                    sb.WriteLine(CUtils.SequenceChar('-', 100));
                    sb.WriteLine("- Connector Map -");
                    sb.WriteLine(CUtils.SequenceChar('-', 100));
                    foreach (var conn in connectorMap.ToSortedArray())
                    {
                        conn.WriteStatus(sb, string.Empty);
                    }
                    sb.WriteLine(CUtils.SequenceChar('-', 100));
                }
            }

            /// <summary>
            /// 链接负载分组
            /// </summary>
            public class GroupInfo
            {
                private readonly ValueSortedMap<string, NodeInfo> connectorMap;
                private readonly LinkedList<ViewSession> inQueueSessions = new LinkedList<ViewSession>();
                public int NodeCount { get => connectorMap.Count; }
                public string GroupID { get; private set; }
                public int QueueCount { get => inQueueSessions.Count; }
                /// <summary>
                /// 当前连接数量
                /// </summary>
                public int ClientNumber { get; private set; }

                #region H.Q.Cai 代码添加

                // H.Q.Cai 代码添加开始
                /// <summary>
                /// 是否需要排队
                /// </summary>
                //public bool IsNeedQueue { get => this.ClientNumberLimit > 0 && this.ClientNumber >= this.ClientNumberLimit; }
                public bool IsNeedQueue => IsAllowSoftEnter == false;
                //public bool IsNeedQueue => true;
                //public bool IsNeedQueue => false;

                private readonly HashSet<ViewSession> checkInQueueSessions = new HashSet<ViewSession>();

                /// <summary>
                /// 是否已满最大的排队系统
                /// </summary>
                public bool IsMaxQueue => QueueCount >= QUEUE_MAX_LIMIT;

                /// <summary>
                /// 第一个进入列队的时间
                /// </summary>
                public DateTime? FirstInQueueTime { get; set; }

                /// <summary>
                /// 是否允许软进入
                /// </summary>
                public bool IsAllowSoftEnter => CLIENT_NUMBER_SOFT_LIMIT <= 0 || ClientNumber < CLIENT_NUMBER_SOFT_LIMIT * SOFT_ENTER_PERCENTAGE;

                /// <summary>
                /// 是否允许硬进入
                /// </summary>
                public bool IsAllowHardEnter => CLIENT_NUMBER_HARD_LIMIT <= 0 || ClientNumber < CLIENT_NUMBER_HARD_LIMIT * HARD_ENTER_PERCENTAGE;

                /// <summary>
                /// 是否超过硬上线
                /// </summary>
                public bool IsExceedHardLine => CLIENT_NUMBER_HARD_LIMIT > 0 && ClientNumber > CLIENT_NUMBER_HARD_LIMIT * MAX_HARD_ENTER_PERCENTAGE;

                /// <summary>
                /// 设置客户端限制
                /// </summary>
                internal void SetClientLimit(int hardLimit, int softLimit, int queueMaxLimit, int queueAddTime)
                {
                    if (softLimit < MIN_CLIENT_NUMBER_SOFT_LIMIT) softLimit = MIN_CLIENT_NUMBER_SOFT_LIMIT;
                    if (softLimit > MAX_CLIENT_NUMBER_SOFT_LIMIT) softLimit = MAX_CLIENT_NUMBER_SOFT_LIMIT;

                    CLIENT_NUMBER_HARD_LIMIT = hardLimit;
                    CLIENT_NUMBER_SOFT_LIMIT = softLimit;
                    QUEUE_MAX_LIMIT = queueMaxLimit;
                    QUEUE_ADD_TIME = queueAddTime;
                }
                // H.Q.Cai 代码添加结束

                #endregion

                public GroupInfo(string group)
                {
                    this.GroupID = group;
                    this.connectorMap = new ValueSortedMap<string, NodeInfo>((a, b) =>
                    {
                        return a.ClientNumber - b.ClientNumber;
                    });
                }
                internal void BindNode(NodeInfo node)
                {
                    connectorMap.Put(node.NodeName, node);
                }
                internal void Sync(IDictionary<string, ConnectInfo> connectorMap)
                {
                    // H.Q.Cai 添加代码开始
                    var oldClientNumber = ClientNumber;
                    // H.Q.Cai 添加代码结束

                    this.ClientNumber = 0;
                    foreach (var c in connectorMap.Values)
                    {
                        foreach (var gn in c.Sync.groupClientNumbers)
                        {
                            if (gn.Key == this.GroupID)
                            {
                                this.ClientNumber += gn.Value;
                            }
                        }
                    }

                    // H.Q.Cai 添加代码开始
                    if (oldClientNumber != ClientNumber)
                    {
                        var subtraction = oldClientNumber - ClientNumber;
                        if (subtraction > POST_SERVER_NUMBER_INTERVAL || subtraction < -POST_SERVER_NUMBER_INTERVAL)
                            RPGServerTemplateManager.Instance.PostGroupServerNumber(GroupID, ClientNumber);
                    }
                    // H.Q.Cai 添加代码结束
                }
                internal bool TryDispatchConnect(RemoteAddress addr, out ConnectInfo conn)
                {
                    // 从当前分组里找到对应的Node链接负载 //
                    if (addr.NotNull && connectorMap.TryGetValue(addr.ServiceNode, out var node))
                    {
                        if (node.TryDispatchConnect(addr, out conn))
                        {
                            return true;
                        }
                    }
                    // 尝试从最小负载Node链接负载 //
                    if (connectorMap.TryGetRandomFirst(random, out node))
                    {
                        if (node.TryDispatchConnect(addr, out conn))
                        {
                            return true;
                        }
                    }
                    conn = null;
                    return false;
                }
                internal void WriteStatus(TextWriter sb, string prefix)
                {
                    sb.WriteLine($"{prefix}{GetType().Name} : {GroupID}");
                    sb.WriteLine($"{prefix}    ClientNumber={ClientNumber}");
                    sb.WriteLine($"{prefix}    ClientInQueue={inQueueSessions.Count}");
                    foreach (var conn in connectorMap.ToSortedArray())
                    {
                        conn.WriteStatus(sb, prefix + " - ");
                    }
                }
                //开始排队等待//
                public void PushInQueue(ViewSession session)
                {
                    inQueueSessions.AddLast(session);
                    checkInQueueSessions.Add(session);
                }
                internal void PushClientNumber()
                {
                    if (!IsNeedQueue)
                    {
                        this.ClientNumber += 1;
                    }
                }
                internal void UpdateInQueue(object state)
                {

                    // H.Q.Cai 添加开始

                    var gateServer = state as GateServer;
                    if (gateServer == null) return;

                    DateTime nowTime = DateTime.Now;
                    TimeSpan constExpectTime = TimeSpan.FromSeconds(QUEUE_ADD_TIME);
                    TimeSpan constExpectHardTime = TimeSpan.FromSeconds(QUEUE_MAX_HARD_ADD_TIME);

                    int queueIndexNew = 0;
                    for (var it = inQueueSessions.First; it != null;)
                    {
                        var subTimeSpan = FirstInQueueTime.HasValue ? nowTime - FirstInQueueTime.Value : TimeSpan.Zero;

                        bool isEnter;
                        if (IsExceedHardLine) 
                            isEnter = subTimeSpan >= constExpectHardTime;
                        else
                            isEnter = subTimeSpan >= constExpectTime;

                        if (gateServer.ContainsSession(it.Value.Session) == false)
                        {
                            var rm = it;
                            it = it.Next;
                            inQueueSessions.Remove(rm);
                            checkInQueueSessions.Remove(rm.Value);

                            if (it == null) FirstInQueueTime = null;
                            else FirstInQueueTime = nowTime;
                        }
                        else if (it.Value.IsConnected == false)
                        {
                            gateServer.RemoveSessionAccount(it.Value.Session);

                            var rm = it;
                            it = it.Next;
                            inQueueSessions.Remove(rm);
                            checkInQueueSessions.Remove(rm.Value);

                            if (it == null) FirstInQueueTime = null;
                            else FirstInQueueTime = nowTime;
                        }
                        else if (isEnter)
                        {
                            if (IsAllowHardEnter == false)
                            { 
                                continue;
                            }

                            if (gateServer.ContainsSession(it.Value.Session) == false)
                            {
                                continue;
                            }

                            gateServer.PushAccountExpire(it.Value.Session, it.Value.Enter.response.s2c_accountUUID, nowTime + constExpectTime);

                            it.Value.UpdateInQueue(queueIndexNew, true, TimeSpan.Zero);

                            ClientNumber += 1;
                            var rm = it;
                            it = it.Next;
                            inQueueSessions.Remove(rm);
                            checkInQueueSessions.Remove(rm.Value);


                            if (it == null) FirstInQueueTime = null;
                            else FirstInQueueTime = nowTime;
                        }
                        else
                        {
                            if (gateServer.ContainsSession(it.Value.Session) == false)
                            {
                                continue;
                            }

                            subTimeSpan = constExpectTime - subTimeSpan;
                            TimeSpan expectTime = TimeSpan.FromSeconds(constExpectTime.TotalSeconds * queueIndexNew) + subTimeSpan;

                            it.Value.UpdateInQueue(queueIndexNew, false, expectTime);

                            it = it.Next;
                            queueIndexNew++;
                        }

                        //log.Warn("queueIndexNew : " + queueIndexNew);
                    }

                    return;

                    // H.Q.Cai 添加结束
/*
                    int allowCount = CLIENT_NUMBER_HARD_LIMIT > 0 ? CLIENT_NUMBER_HARD_LIMIT - ClientNumber : int.MaxValue;
                    int queueIndex = 0;
                    for (var it = inQueueSessions.First; it != null;)
                    {
                        var isEnter = allowCount > 0;
                        if (it.Value.IsConnected == false)
                        {
                            var rm = it;
                            it = it.Next;
                            inQueueSessions.Remove(rm);
                        }
                        else if (isEnter)
                        {
                            it.Value.UpdateInQueue(queueIndex, isEnter);
                            allowCount--;
                            this.ClientNumber += 1;
                            var rm = it;
                            it = it.Next;
                            inQueueSessions.Remove(rm);
                        }
                        else
                        {
                            it.Value.UpdateInQueue(queueIndex, isEnter);
                            it = it.Next;
                            queueIndex++;
                        }
                    }
*/
                }
            }

            /// <summary>
            /// 链接负载
            /// </summary>
            public class NodeInfo
            {
                private readonly ValueSortedMap<string, ConnectInfo> connectorMap;
                public int ConnectorCount { get => connectorMap.Count; }
                public int ClientNumber { get; private set; }
                public string NodeName { get; private set; }

                public NodeInfo(string name)
                {
                    this.NodeName = name;
                    this.connectorMap = new ValueSortedMap<string, ConnectInfo>((a, b) =>
                    {
                        return a.ClientNumber - b.ClientNumber;
                    });
                }
                internal bool SyncConnect(RemoteAddress addr, ConnectInfo msg)
                {
                    msg.Node = this;
                    var ret = this.connectorMap.TryAddOrUpdate(addr.ServiceName, msg);
                    this.ClientNumber = connectorMap.Sum(e => e.Value.ClientNumber);
                    connectorMap.MarkSort();
                    return ret;
                }
                internal bool TryDispatchConnect(RemoteAddress addr, out ConnectInfo conn)
                {
                    //从指定的ServiceName，获得链接//
                    if (addr.NotNull && connectorMap.TryGetValue(addr.ServiceName, out conn))
                    {
                        return true;
                    }
                    // 尝试从最小负载获得链接 //
                    if (connectorMap.TryGetRandomFirst(random, out conn))
                    {
                        return true;
                    }
                    return false;
                }
                internal void WriteStatus(TextWriter sb, string prefix)
                {
                    sb.WriteLine($"{prefix}{GetType().Name} : {NodeName}");
                    sb.WriteLine($"{prefix}    ClientNumber={ClientNumber}");
                    foreach (var conn in connectorMap.ToSortedArray())
                    {
                        conn.WriteStatus(sb, prefix + " - ");
                    }
                }
            }

            /// <summary>
            /// 连接服信息
            /// </summary>
            public class ConnectInfo
            {
                public SyncConnectToGateNotify Sync
                {
                    get; internal set;
                }
                public NodeInfo Node
                {
                    get; internal set;
                }
                public int ClientNumber
                {
                    get => Sync.clientNumber;
                }
                public ConnectInfo(SyncConnectToGateNotify sync)
                {
                    this.Sync = sync;
                }
                internal void WriteStatus(TextWriter sb, string prefix)
                {
                    sb.WriteLine($"{prefix}{GetType().Name} : {Sync.connectServiceAddress} ClientNumber={ClientNumber}");
                }
            }
        }

        //----------------------------------------------------------------------------------------------------------------------------------------------
        
        /// <summary>
        /// 账号列队到期时间
        /// </summary>
        private readonly HashMap<string, (DateTime ExpectTime, bool IsEnetered, ISession Session)> inAccountExpire = new HashMap<string, (DateTime ExpectTime, bool IsEnetered, ISession Session)>();

        /// <summary>
        /// 账号列队到期时间(其它)
        /// </summary>
        private readonly HashMap<string, DateTime> inAccountExpireOther = new HashMap<string, DateTime>();

        /// <summary>
        /// 通过当前的Session获取账号Uuid
        /// </summary>
        private readonly HashMap<ISession, string> inSessionAccount = new HashMap<ISession, string>();

        /// <summary>
        /// 推入会议账号结束期
        /// </summary>
        private void InsertSessionAccountExpire(ISession session, string account, DateTime expire)
        {
            inSessionAccount.TryAddOrUpdate(session, account);
            inAccountExpire.TryAddOrUpdate(account, (expire, false, session));
        }

        /// <summary>
        /// 推入会议结束期
        /// </summary>
        private void PushAccountExpire(ISession session, string account, DateTime expire)
        {
            inAccountExpire.TryAddOrUpdate(account, (expire, true, session));
        }

        /// <summary>
        /// 推入会议结束期
        /// </summary>
        private void PushOtherAccountExpire(string account, DateTime expire)
        {
            if(expire == DateTime.MinValue)
                inAccountExpireOther.RemoveByKey(account);
            else 
                inAccountExpireOther.TryAddOrUpdate(account, expire);
        }

        /// <summary>
        /// 移除会议
        /// </summary>
        /// <param name="session"></param>
        private bool ContainsSession(ISession session)
        {
            return inSessionAccount.ContainsKey(session);
        }

        /// <summary>
        /// 移除账号
        /// </summary>
        /// <param name="session"></param>
        private void RemoveSessionAccount(ISession session)
        {
            if (inSessionAccount.ContainsKey(session))
            {
                var sessionAccount = inSessionAccount[session];
                inSessionAccount.RemoveByKey(session);

                if (inAccountExpire.ContainsKey(sessionAccount))
                {
                    var dateTime = inAccountExpire[sessionAccount];
                    if (dateTime.IsEnetered == false)
                        inAccountExpire.RemoveByKey(sessionAccount);
                }
            }
        }

        /// <summary>
        /// 包含账号
        /// </summary>
        /// <param name="session"></param>
        /// <param name="account"></param>
        private bool ContainsAccount(ISession session, string account)
        {
            if (inAccountExpire.ContainsKey(account))
            {
                var dateTime = inAccountExpire[account];
                var result = dateTime.IsEnetered && dateTime.ExpectTime >= DateTime.Now;
                return result;
            }

            if (inAccountExpireOther.ContainsKey(account))
                return true;

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        private void UpdateInQueue(object state)
        {
            DateTime nowTime = DateTime.Now;

            List<string> delAccountList = new List<string>();
            foreach (var dateTime in inAccountExpire)
            {
                var dateTimeValue = dateTime.Value;
                if (dateTimeValue.ExpectTime < nowTime)
                {
                    delAccountList.Add(dateTime.Key);
                }
            }

            foreach (var account in delAccountList)
            {
                inAccountExpire.Remove(account);
            }

            delAccountList.Clear();

            foreach (var dateTime in inAccountExpireOther)
            {
                if (dateTime.Value < nowTime)
                {
                    delAccountList.Add(dateTime.Key);
                }
            }

            foreach (var account in delAccountList)
            {
                inAccountExpireOther.Remove(account);
            }

            groupMap.UpdateInQueue(state);
        }

        //----------------------------------------------------------------------------------------------------------------------------------------------

        protected virtual void Acceptor_OnSessionConnected(ISession session)
        {
            log.Info("Acceptor_OnSessionConnected : " + session);
            Acceptor_CreateViewSession(session);
        }
        protected virtual void Acceptor_OnSessionDisconnected(ISession session)
        {
            log.Info("Acceptor_OnSessionDisconnected : " + session);

            // H.Q.Cai 添加开始

            RemoveSessionAccount(session);
            groupMap.UpdateInQueue(this);

            // H.Q.Cai 添加结束
        }
        protected virtual void Acceptor_OnServerError(IServer server, Exception err)
        {
            log.Error("Acceptor_OnServerError : " + err.Message, err);
        }
        protected virtual ViewSession Acceptor_CreateViewSession(ISession session)
        {
            return new ViewSession(this, session, log);
        }

        //------------------------------------------------------------------------------------------

        public class EnterToken
        {
            public readonly ClientEnterGateRequest request;
            public readonly ClientEnterGateResponse response;
            public readonly ConnectorNodeMap.GroupInfo group;
            public readonly ConnectorNodeMap.ConnectInfo connect;
            public readonly AccountData account;
            public EnterToken(ClientEnterGateRequest request, ClientEnterGateResponse response, ConnectorNodeMap.GroupInfo group = null, ConnectorNodeMap.ConnectInfo connect = null, AccountData account = null)
            {
                this.request = request;
                this.response = response;
                this.group = group;
                this.connect = connect;
                this.account = account;
            }
        }
        public class ViewSession
        {
            protected readonly Logger log;
            protected readonly GateServer server;
            protected readonly ISession session;
            protected readonly DateTime loginTime;
            protected EnterToken enter;

            public ISession Session => session;

            public EnterToken Enter => enter;

            public bool IsConnected
            {
                get => session.IsConnected;
            }
            public ViewSession(GateServer server, ISession session, Logger log)
            {
                this.log = log;
                this.server = server;
                this.session = session;
                this.session.OnValidateAsync += Session_OnValidateAsync;
                this.session.OnError += Session_OnError;
                this.loginTime = DateTime.Now;
            }
            protected virtual async Task<Tuple<bool, ISerializable>> Session_OnValidateAsync(ISession session, ISerializable user)
            {
                if (user is ClientEnterGateRequest)
                {
                    //TODO 尽量分配到之前登陆过的Connect
                    return await server.Provider.Execute(async () =>
                    {
                        this.enter = await server.SelectConnectAsync(user as ClientEnterGateRequest, session);

                        if (enter.response.s2c_code == ClientEnterGateResponse.CODE_OK_IN_QUEUE)
                        {

                            // H.Q.Cai 添加开始
                            var nowTime = DateTime.Now;
                            TimeSpan queueAddTime = TimeSpan.FromSeconds(QUEUE_ADD_TIME);
                            DateTime constExpectTime = nowTime + TimeSpan.FromMinutes(1.0f);

                            enter.group.PushInQueue(this);
                            server.InsertSessionAccountExpire(this.session, enter.response.s2c_accountUUID, constExpectTime);

                            if (enter.group.QueueCount == 1)
                            {
                                enter.group.FirstInQueueTime = nowTime;
                            }

                            Func<ConnectorNodeMap.GroupInfo, TimeSpan> getWaiteSecond = group =>
                            {
                                var subTimeSpan = group.FirstInQueueTime.HasValue ? nowTime - group.FirstInQueueTime.Value : TimeSpan.Zero;
                                var expectTime = TimeSpan.FromSeconds(queueAddTime.TotalSeconds * (enter.group.QueueCount - 1)) + subTimeSpan;

                                return expectTime;
                            };

                            ClientEnterGateInQueueNotify notify = new ClientEnterGateInQueueNotify()
                            {
                                IsEnetered = false,
                                QueueIndex = enter.group.QueueCount - 1,
                                ExpectTime = getWaiteSecond(enter.group)
                            };

                            //session.SendAsync(notify).NoWait();
                            session.Send(notify);

                            // H.Q.Cai 添加结束

                            var result = server.ServerCodec.CloneSerializable(enter.response);
                            result.s2c_connectHost = null;
                            result.s2c_connectPort = 0;
                            result.s2c_connectToken = null;
                            result.s2c_lastLoginToken = null;
                            return new Tuple<bool, ISerializable>(true, result);
                        }
                        else
                        {
                            return new Tuple<bool, ISerializable>(false, enter.response);
                        }
                    });
                }
                else
                {
                    return new Tuple<bool, ISerializable>(false, null);
                }
            }
            protected virtual void Session_OnError(ISession session, Exception err)
            {
                log.Error(err.Message, err);
            }
            public virtual void UpdateInQueue(int queueIndex, bool isEnter)
            {
                var notify = new ClientEnterGateInQueueNotify();
                notify.IsEnetered = isEnter;
                notify.QueueIndex = queueIndex;
                if (isEnter)
                {
                    notify.s2c_connectHost = enter.response.s2c_connectHost;
                    notify.s2c_connectPort = enter.response.s2c_connectPort;
                    notify.s2c_connectToken = enter.response.s2c_connectToken;
                    notify.s2c_lastLoginToken = enter.response.s2c_lastLoginToken;
                    session.Send(notify);
                    session.Disconnect("entered");
                }
                else
                {
                    session.Send(notify);
                }
            }

            // H.Q.Cai 添加开始
            /// <summary>
            /// 
            /// </summary>
            public virtual void UpdateInQueue(int queueIndex, bool isEnter, TimeSpan expectTime)
            {
                var notify = new ClientEnterGateInQueueNotify
                {
                    IsEnetered = isEnter,
                    QueueIndex = queueIndex,
                    ExpectTime = expectTime
                };

                if (isEnter)
                {
                    notify.s2c_connectHost = enter.response.s2c_connectHost;
                    notify.s2c_connectPort = enter.response.s2c_connectPort;
                    notify.s2c_connectToken = enter.response.s2c_connectToken;
                    notify.s2c_lastLoginToken = enter.response.s2c_lastLoginToken;
                    session.Send(notify);
                    session.Disconnect("entered");
                }
                else
                {
                    session.Send(notify);
                }
            }
            // H.Q.Cai 添加结束
        }

        //------------------------------------------------------------------------------------------
    }
}
