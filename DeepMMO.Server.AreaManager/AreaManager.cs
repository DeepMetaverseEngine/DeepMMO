using DeepCore;
using DeepCore.GameData.Zone.ZoneEditor;
using DeepCore.GameEvent;
using DeepCore.IO;
using DeepCore.Log;
using DeepCrystal.RPC;
using DeepMMO.Data;
using DeepMMO.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DeepCore.GameEvent.Message;
using DeepCore.Net;
using DeepMMO.Protocol.Client;
using DeepMMO.Server.GameEvent;

namespace DeepMMO.Server.AreaManager
{
    public class AreaManager : IService
    {
        protected readonly Logger log;
        protected readonly Random random = new Random();
        private IDisposable mTimer;
        public EventManager EventMgr { get; private set; }

        public AreaManager(ServiceStartInfo start) : base(start)
        {
            this.log = LoggerFactory.GetLogger(GetType().Name);
            this.areas = new ValueSortedMap<string, ValueSortedMap<string, AreaInfo>>(AreaGroupComparison);
        }
        protected override void OnDisposed()
        {

        }
        protected override Task OnStartAsync()
        {
            EventMgr = EventManagerFactory.Instance.CreateEventManager(nameof(AreaManager), null);
            if (EventMgr != null)
            {
                EventMgr.PutObject("Service", this);
                EventMgr.Start();
                mTimer = Provider.CreateTimer(OnEventTick, this,
                    TimeSpan.FromSeconds(0),
                    TimeSpan.FromSeconds(TimerConfig.timer_sec_EventUpdateTime));
            }
            return Task.FromResult(0);
        }
        protected override Task OnStopAsync(ServiceStopInfo reason)
        {
            EventMgr?.Dispose();
            mTimer?.Dispose();
            return Task.FromResult(0);
        }
        private void OnEventTick(object state)
        {
            EventMgr?.Update();
        }

        public virtual void AreaSyncState(AreaStateNotify st)
        {
            if (areas.TryGetValue(st.areaNode, out var group))
            {
                areas.MarkSort();
                if (group.TryGetValue(st.areaName, out var area))
                {
                    group.MarkSort();
                    area.state = st;
                }
            }
            this.LogState();
        }
        public override bool GetState(TextWriter sb)
        {
            sb.WriteLine(CUtils.SequenceChar('-', 100));
            foreach (var group in areas.ToSortedArray())
            {
                foreach (var area in group.ToSortedArray())
                {
                    area.WriteState(sb);
                }
            }
            sb.WriteLine(CUtils.SequenceChar('-', 100));
            return true;
        }


        //------------------------------------------------------------------------------------------------------------------------------------
        #region RPC
        [RpcHandler(typeof(BatchCreateZoneLineRequest), typeof(BatchCreateZoneLineResponse))]
        public virtual Task<BatchCreateZoneLineResponse> logic_rpc_Handle(BatchCreateZoneLineRequest create)
        {
            //log.Info(create);
            return BatchCreateZoneLine(create);
        }

        [RpcHandler(typeof(CreateZoneNodeRequest), typeof(CreateZoneNodeResponse))]
        public virtual Task<CreateZoneNodeResponse> logic_rpc_Handle(CreateZoneNodeRequest create)
        {
            //log.Info(create);
            return CreateZone(create);
        }
        [RpcHandler(typeof(DestoryZoneNodeRequest), typeof(DestoryZoneNodeResponse))]
        public virtual Task<DestoryZoneNodeResponse> logic_rpc_Handle(DestoryZoneNodeRequest stop)
        {
            //log.Info(stop);
            return DestoryZone(stop);
        }
        [RpcHandler(typeof(RoleEnterZoneRequest), typeof(RoleEnterZoneResponse))]
        public virtual Task<RoleEnterZoneResponse> logic_rpc_Handle(RoleEnterZoneRequest req)
        {
            //log.Info(req);
            return RoleEnter(req);
        }
        [RpcHandler(typeof(RoleLeaveZoneRequest), typeof(RoleLeaveZoneResponse))]
        public virtual Task<RoleLeaveZoneResponse> logic_rpc_Handle(RoleLeaveZoneRequest req)
        {
            //log.Info(req);
            return RoleLeave(req);
        }

        [RpcHandler(typeof(ServerGameEventNotify))]
        public virtual void rpc_event_notify(ServerGameEventNotify ntf)
        {
            EventMgr?.OnReceiveMessage(EventMessage.FromBytes(ntf.EventMessageData));
        }

        //------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// 添加一个Area负载
        /// </summary>
        /// <returns></returns>
        [RpcHandler(typeof(RegistAreaRequest), typeof(RegistAreaResponse), ServerNames.AreaServiceType)]
        public virtual Task<RegistAreaResponse> area_rpc_RegistAreaAsync(RegistAreaRequest reg)
        {
            return AreaRegist(reg);
        }

        [RpcHandler(typeof(AreaStateNotify), ServerNames.AreaServiceType)]
        public virtual void area_rpc_AreaStateNotify(AreaStateNotify notify)
        {
            AreaSyncState(notify);
        }

        [RpcHandler(typeof(AreaZoneGameOverNotify), ServerNames.AreaServiceType)]
        public virtual void area_rpc_AreaGameOverHandle(AreaZoneGameOverNotify stop)
        {
            //log.Info("AreaMgr receive " + stop + " " + stop.zoneUUID);
            zones.SetZoneCloseFlag(stop.zoneUUID);
        }

        [RpcHandler(typeof(AreaZoneDestoryNotify), ServerNames.AreaServiceType)]
        public virtual void area_rpc_AreaGameOverHandle(AreaZoneDestoryNotify stop)
        {
            //TODO flag destory 
            //log.Info("AreaMgr recive " + stop + " " + stop.zoneUUID);
            DestoryZone(stop);
        }

        //--------------------------------------------------

        [RpcHandler(typeof(RoleNameChangedNotify))]
        public virtual void logic_rpc_Handle(RoleNameChangedNotify ntf)
        {
            var role = GetRole(ntf.roleId);
            if (role != null)
            {
                role.enter.roleDisplayName = ntf.newName;
            }
        }
        //------------------------------------------------------------------------------------------------------------------------------------

        [RpcHandler(typeof(GetAllRoleRequest), typeof(GetAllRoleResponse))]
        public virtual void logic_rpc_Handle(GetAllRoleRequest req, OnRpcReturn<GetAllRoleResponse> cb)
        {
            RoleInfo[] roleList = GetAllRoles();
            HashMap<string, OnlinePlayerData> uuidMap = new HashMap<string, OnlinePlayerData>();
            for (int i = 0; i < roleList.Length; ++i)
            {
                var rold = roleList[i];
                uuidMap.Add(rold.uuid, new OnlinePlayerData()
                {
                    name = rold.enter.roleDisplayName,
                    serverGroupId = rold.enter.servergroupID
                });
            }
            cb(new GetAllRoleResponse() { uuidMap = uuidMap });
        }

        [RpcHandler(typeof(QueryZoneAreaNameRequest), typeof(QueryZoneAreaNameResponse))]
        public Task<QueryZoneAreaNameResponse> logic_rpc_Handle(QueryZoneAreaNameRequest req)
        {
            var zone = GetZone(req.zoneUUID);
            return Task.FromResult(new QueryZoneAreaNameResponse
            {
                s2c_code = zone != null ? Response.CODE_OK : Response.CODE_ERROR,
                areaName = zone?.area.key
            });
        }


        [RpcHandler(typeof(GetZonesInfoRequest), typeof(GetZonesInfoResponse))]
        public virtual void logic_rpc_Handle(GetZonesInfoRequest req, OnRpcReturn<GetZonesInfoResponse> cb)
        {
            GetZonesInfoResponse rsp = new GetZonesInfoResponse();
            //指定场景的所有线.
            var lt = GetZoneList(req.servergroupID, req.mapID);
            if (lt != null)
            {
                List<ZoneInfoSnap> snaps = new List<ZoneInfoSnap>();
                rsp.snaps = snaps;

                ZoneInfoSnap snap = null;
                ZoneInfo info = null;
                for (int i = 0; i < lt.Count; i++)
                {
                    //获取所有线的信息.
                    info = lt[i];
                    if (info != null && info.close == false)
                    {
                        snap = new ZoneInfoSnap();
                        snap.curPlayerCount = info.currentRoleCount;
                        snap.playerMaxCount = info.map_data.max_players;
                        snap.lineIndex = info.lineIndex;
                        snap.playerFullCount = info.map_data.full_players;
                        snap.uuid = info.uuid;
                        snaps.Add(snap);
                    }
                }
            }

            cb(rsp);
        }

        /// <summary>
        /// 批量获取场景分线
        /// </summary>
        /// <param name="req"></param>
        /// <param name="cb"></param>

        [RpcHandler(typeof(GetBatchZonesInfoRequest), typeof(GetBatchZonesInfoResponse))]
        public virtual void logic_rpc_Handle(GetBatchZonesInfoRequest req, OnRpcReturn<GetBatchZonesInfoResponse> cb)
        {
            GetBatchZonesInfoResponse rsp = new GetBatchZonesInfoResponse();
            rsp.snapDic = new HashMap<int, List<ZoneInfoSnap>>();

            foreach (var item in req.mapIDList)
            {
                var lt = GetZoneList(req.servergroupID, item);

                if (lt != null)
                {
                    ZoneInfoSnap snap = null;
                    ZoneInfo info = null;

                    List<ZoneInfoSnap> snaps = new List<ZoneInfoSnap>();

                    for (int i = 0; i < lt.Count; i++)
                    {
                        //获取所有线的信息.
                        info = lt[i];
                        if (info != null && info.close == false)
                        {
                            snap = new ZoneInfoSnap();
                            snap.curPlayerCount = info.currentRoleCount;
                            snap.playerMaxCount = info.map_data.max_players;
                            snap.lineIndex = info.lineIndex;
                            snap.playerFullCount = info.map_data.full_players;
                            snap.uuid = info.uuid;
                            snaps.Add(snap);
                        }
                    }
                    rsp.snapDic.Add(item, snaps);
                }
            }
            cb(rsp);
        }



        [RpcHandler(typeof(GetRolePositionRequest), typeof(GetRolePositionResponse))]
        public virtual async Task<GetRolePositionResponse> logic_rpc_Handle(GetRolePositionRequest req)
        {
            if (roles.ContainsKey(req.roleUUID) == false)
            {
                return new GetRolePositionResponse()
                {
                    s2c_code = GetRolePositionResponse.CODE_ROLE_NOT_EXIST
                };
            }

            var role = roles.Get(req.roleUUID);
            var zone = role.zone;
            if (zone == null)
            {
                return (new GetRolePositionResponse() { s2c_code = GetRolePositionResponse.CODE_ZONE_NOT_EXIST });
            }

            var resp = await zone.area.service.CallAsync<GetRolePositionResponse>(req);
            resp.line = zone.lineIndex;
            resp.zoneId = zone.map_data.id;
            resp.zoneUUID = zone.uuid;
            return resp;
        }

        #endregion
        //------------------------------------------------------------------------------------------------------------------------------------
        #region ZoneManager

        private readonly ValueSortedMap<string, ValueSortedMap<string, AreaInfo>> areas;
        private readonly HashMap<string, RoleInfo> roles = new HashMap<string, RoleInfo>();
        private readonly ZoneMap zones = new ZoneMap();

        public virtual async Task<RoleEnterZoneResponse> RoleEnter(RoleEnterZoneRequest req)
        {
            if (roles.ContainsKey(req.roleUUID))
            {
                return (new RoleEnterZoneResponse() { s2c_code = RoleEnterZoneResponse.CODE_ROLE_ALREADY_IN_ZONE });
            }
            var roleInfo = new RoleInfo(req.roleUUID, req);
            //先添加以防止二次进入//
            roles.Add(roleInfo.uuid, roleInfo);
            try
            {
                var zone = await this.LookingForExpectZone(req);
                if (zone == null || zone.close)
                {
                    roles.Remove(req.roleUUID);
                    return (new RoleEnterZoneResponse() { s2c_code = RoleEnterZoneResponse.CODE_ZONE_NOT_EXIST });
                }
                //--------------------------------------------------------------------------------
                //分配线.
                req.expectLineIndex = zone.lineIndex;
                req.expectZoneUUID = zone.uuid;
                zone.AddTeamID(req.teamID);
                //--------------------------------------------------------------------------------
                var rsp = await zone.area.service.CallAsync<RoleEnterZoneResponse>(req);
                if (Response.CheckSuccess(rsp))
                {
                    zone.currentRoleCount++;
                    zone.area.currentRoleCount++;
                    roleInfo.zone = zone;
                }
                else
                {
                    roles.Remove(req.roleUUID);
                    log.Error(rsp);
                }
                return rsp;
            }
            catch (Exception err)
            {
                roles.Remove(req.roleUUID);
                return (new RoleEnterZoneResponse() { s2c_code = RoleEnterZoneResponse.CODE_ERROR, s2c_msg = err.Message });
            }
        }
        public virtual async Task<RoleLeaveZoneResponse> RoleLeave(RoleLeaveZoneRequest req)
        {
            var role = roles.RemoveByKey(req.roleID);
            if (role == null)
            {
                return (new RoleLeaveZoneResponse() { s2c_code = RoleLeaveZoneResponse.CODE_ROLE_NOT_EXIST });
            }
            var zone = role.zone;
            if (zone == null)
            {
                return (new RoleLeaveZoneResponse() { s2c_code = RoleLeaveZoneResponse.CODE_ZONE_NOT_EXIST });
            }
            //最后才删除//
            zone.currentRoleCount--;
            zone.area.currentRoleCount--;
            req.zoneUUID = zone.uuid;
            var rsp = await zone.area.service.CallAsync<RoleLeaveZoneResponse>(req);
            if (!Response.CheckSuccess(rsp))
            {
                log.Error(rsp);
            }
            zone.RemoveTeamID(role.enter.teamID);
            return rsp;
        }

        public virtual async Task<ZoneInfo> LookingForExpectZone(RoleEnterZoneRequest req)
        {
            ZoneInfo zone = null;
            #region 返回上一个场景.

            //返回上一次的场景，如果没有统一返回上一次的公共场景.
            if (!string.IsNullOrEmpty(req.expectZoneUUID))
            {
                //根据提供的UUID寻找场景//
                zone = GetZone(req.expectZoneUUID);
                if (zone != null && !zone.close)
                {
                    if (zone.currentRoleCount < zone.map_data.max_players)
                    {
                        return (zone);
                    }
                }

                return await LookingForPublicZone(req.lastPublicMapUUID, req.lastPublicMapID, req.lastPublicPos, req);
            }
            #endregion
            else
            {
                var temp = RPGServerTemplateManager.Instance.GetMapTemplate(req.expectMapTemplateID);
                if (temp == null)//场景不存在，往公共场景扔.
                {
                    return await LookingForPublicZone(req.lastPublicMapUUID, req.lastPublicMapID, new ZonePosition(), req);
                }
                //优先判断公会场景//
                if (!string.IsNullOrEmpty(req.guildUUID))
                {
                    ZoneInfo guildZone = GetGuildZone(req.guildUUID);
                    if (guildZone != null && !guildZone.close)
                    {
                        if (guildZone.currentRoleCount < guildZone.map_data.max_players)
                        {
                            return (guildZone);
                        }
                        else//公会场景人数大于人数时候，不再能进入场景.
                        {
                            return await LookingForPublicZone(req.lastPublicMapUUID, req.lastPublicMapID, req.lastPublicPos, req);
                        }
                    }
                    else //创建公会场景.
                    {
                        //新创建一个场景//
                        var rsp = await this.CreateZone(new CreateZoneNodeRequest()
                        {
                            serverID = req.serverID,
                            serverGroupID = req.servergroupID,
                            mapTemplateID = req.expectMapTemplateID,
                            createRoleID = req.roleUUID,
                            teamID = req.teamID,
                            guildUUID = req.guildUUID,
                            reason = "LookingForAreaRequest dispatch",
                            expandData = RPGServerTemplateManager.Instance.GetCreateZoneExpandData(req),
                            expectAreaNode = req.roleSessionNode,
                        });
                        if (Response.CheckSuccess(rsp))
                        {
                            return (GetZone(rsp.zoneUUID));
                        }
                        else
                        {
                            return await LookingForPublicZone(req.lastPublicMapUUID, req.lastPublicMapID, req.lastPublicPos, req);
                        }
                    }
                }

                //ROOMKEY.
                if (!string.IsNullOrEmpty(req.roomKey))
                {
                    zone = GetRoomZone(req.roomKey);
                    if (zone != null && !zone.close)
                    {
                        return (zone);
                    }
                }

                //分配至队伍场景/
                if (req.teamID != null)
                {
                    zone = LookingForExpectServerGroupZone(req.servergroupID, z =>
                    {
                        return z.currentRoleCount < temp.max_players &&
                               z.map_data.zone_template_id == req.expectMapTemplateID &&
                               z.ContainTeamID(req.teamID);
                    });
                    if (zone != null && !zone.close)
                    {
                        return (zone);
                    }
                }

                //根据EXPECT MAP来.
                if (RPGServerTemplateManager.Instance.IsPublicMap(temp))
                {
                    return await LookingForPublicZone(null, req.expectMapTemplateID, req.roleScenePos, req);
                }
                else
                {
                    //新创建一个场景//
                    var rsp = await this.CreateZone(new CreateZoneNodeRequest()
                    {
                        serverID = req.serverID,
                        serverGroupID = req.servergroupID,
                        mapTemplateID = temp.id,
                        createRoleID = req.roleUUID,
                        teamID = req.teamID,
                        reason = "LookingForAreaRequest dispatch",
                        expandData = RPGServerTemplateManager.Instance.GetCreateZoneExpandData(req),
                        roomKey = req.roomKey,
                        expectAreaNode = req.roleSessionNode,
                    });
                    if (Response.CheckSuccess(rsp))
                    {
                        return (GetZone(rsp.zoneUUID));
                    }
                    else
                    {
                        return await LookingForPublicZone(req.lastPublicMapUUID, req.lastPublicMapID, req.lastPublicPos, req);
                    }
                }
            }
        }
        protected virtual async Task<ZoneInfo> LookingForPublicZone(string publicmapUUID, int publicmapID, ZonePosition pos, RoleEnterZoneRequest req)
        {
            //没有统一返回上一次的公共场景.
            req.roleScenePos = pos;
            ZoneInfo zone = GetZone(publicmapUUID);
            if (zone != null && !zone.close)
            {
                if (zone.currentRoleCount < zone.map_data.max_players)
                {
                    return (zone);
                }
            }
            //找当前存在的公共场景.
            var temp = RPGServerTemplateManager.Instance.GetMapTemplate(publicmapID);
            if (temp == null)
            {
                temp = RPGServerTemplateManager.Instance.GetDefaultMapData(req);
                req.lastPublicPos = new ZonePosition();
            }
            zone = LookingForExpectServerGroupZone(req.servergroupID, z =>
            {
                return z.currentRoleCount < temp.full_players && z.map_data.zone_template_id == temp.id;
            }, (a, b) =>
            {
                if (req.roleSessionNode == a.nodeName) return -1;
                if (req.roleSessionNode == b.nodeName) return 1;
                return 0;
            });
            if (zone != null && !zone.close)
            {
                return (zone);
            }
            //创建一个公共场景.
            var rsp = await this.CreateZone(new CreateZoneNodeRequest()
            {
                serverGroupID = req.servergroupID,
                serverID = req.serverID,
                mapTemplateID = temp.id,
                reason = "LookingForAreaRequest dispatch",
                expandData = RPGServerTemplateManager.Instance.GetCreateZoneExpandData(req),
                roomKey = req.roomKey,
                expectAreaNode = req.roleSessionNode,
            });
            if (Response.CheckSuccess(rsp))
            {
                return (GetZone(rsp.zoneUUID));
            }
            else
            {
                return (null);
            }
        }
        /// <summary>
        /// 根据地图ID，和Area名字选择合适Zone
        /// </summary>
        /// <param name="serverGroupID"></param>
        /// <param name="condition">必要条件</param>
        /// <param name="expect">可选条件</param>
        /// <returns></returns>
        protected ZoneInfo LookingForExpectServerGroupZone(string serverGroupID, Predicate<ZoneInfo> condition, Comparison<ZoneInfo> expect = null)
        {
            Dictionary<string, ZoneInfo> map = zones.GetZoneMap(serverGroupID);
            if (map != null)
            {
                var zones = new List<ZoneInfo>(map.Values);
                return LookingForExpectZone(zones, condition, expect);
            }
            return null;
        }
        /// <summary>
        /// 选取预期场景
        /// </summary>
        /// <param name="zones"></param>
        /// <param name="condition">必要条件</param>
        /// <param name="expect">可选条件</param>
        /// <returns></returns>
        protected virtual ZoneInfo LookingForExpectZone(List<ZoneInfo> zones, Predicate<ZoneInfo> condition, Comparison<ZoneInfo> expect = null)
        {
            for (int i = zones.Count - 1; i >= 0; --i)
            {
                var z = zones[i];
                if (z.close || !condition(z))
                {
                    zones.RemoveAt(i);
                }
            }
            if (zones.Count > 0)
            {
                if (expect != null)
                {
                    zones.Sort(expect);
                }
                return zones[0];
            }
            return null;
        }

        /// <summary>
        /// 负载均衡排序
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public virtual int AreaComparison(AreaInfo a, AreaInfo b)
        {
            if (a.state == null) return -1;
            if (b.state == null) return 1;
            return a.state.roleCount - b.state.roleCount;
        }
        public virtual int AreaGroupComparison(ValueSortedMap<string, AreaInfo> a, ValueSortedMap<string, AreaInfo> b)
        {
            var ac = a.Values.Sum(area => area.currentRoleCount);
            var bc = b.Values.Sum(area => area.currentRoleCount);
            return ac - bc;
        }
        public virtual async Task<RegistAreaResponse> AreaRegist(RegistAreaRequest reg)
        {
            IRemoteService svc;
            try
            {
                svc = await base.Provider.GetAsync(new RemoteAddress(reg.areaName, reg.areaNode));
                if (svc != null)
                {
                    var node = areas.GetOrAdd(svc.Address.ServiceNode, (n) => new ValueSortedMap<string, AreaInfo>(AreaComparison));
                    node.Add(svc.Address.ServiceName, new AreaInfo(svc));
                    return new RegistAreaResponse();
                }
                else
                {
                    return new RegistAreaResponse() { s2c_code = RegistAreaResponse.CODE_ERROR };
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                throw;
            }
        }
        /// <summary>
        /// 分配一个空闲的Area
        /// </summary>
        /// <param name="expectAreaName"></param>
        /// <returns></returns>
        public virtual AreaInfo AreaDispatch(string expectNodeName)
        {
            // TODO 场景最大人数负载
            if (areas.Count > 0)
            {
                if (expectNodeName != null && areas.TryGetValue(expectNodeName, out var group))
                {
                    if (group.Count > 0)
                    {
                        return group.First;
                    }
                }
                group = areas.First;
                if (group.Count > 0)
                {
                    return group.First;
                }
            }
            throw new Exception("No Area !!!");
        }
        /// <summary>
        /// 创建场景
        /// </summary>
        /// <param name="create"></param>
        /// <param name="cb"></param>
        public virtual async Task<CreateZoneNodeResponse> CreateZone(CreateZoneNodeRequest create)
        {
            //if (dungeon_scheduler.IsMapOpen(create.mapTemplateID))
            {
                var area = AreaDispatch(create.expectAreaNode);
                if (area != null)
                {
                    create.managerZoneUUID = (Guid.NewGuid().ToString());
                    //先添加以防止二次进入//
                    var map_data = RPGServerTemplateManager.Instance.GetMapTemplate(create.mapTemplateID);
                    var scene_data = RPGServerBattleManager.Instance.GetSceneAsCache(map_data.zone_template_id);

                    ZoneInfo info = new ZoneInfo(create.managerZoneUUID, area, map_data, scene_data, create.serverID, create.serverGroupID, create.guildUUID, create.expectAreaNode)
                    {
                        teamID = create.teamID,
                        createrRoleID = create.createRoleID,
                        roomKey = create.roomKey
                    };
                    zones.AddZone(info);
                    area.currentZoneCount++;
                    var rsp = await area.service.CallAsync<CreateZoneNodeResponse>(create);
                    if (!Response.CheckSuccess(rsp))
                    {
                        zones.RemoveZone(create.managerZoneUUID);
                        area.currentZoneCount--;
                    }
                    else
                    {
                        log.InfoFormat("CreateZone: {0} : TotalZoneCount={1}", scene_data, zones.Count);
                    }
                    return rsp;
                }
                else
                {
                    return (new CreateZoneNodeResponse() { s2c_code = CreateZoneNodeResponse.CODE_ERROR, });
                }
            }
        }

        /// <summary>
        /// 销毁场景
        /// </summary>
        /// <param name="stop"></param>
        /// <param name="cb"></param>
        public virtual async Task<DestoryZoneNodeResponse> DestoryZone(DestoryZoneNodeRequest stop)
        {
            ZoneInfo zone = zones.RemoveZone(stop.zoneUUID);
            if (zone != null)
            {
                //log.Log("DestoryZone: " + stop.zoneUUID + " " + stop);
                zones.RemoveZone(stop.zoneUUID);
                zone.area.currentZoneCount--;
                return await zone.area.service.CallAsync<DestoryZoneNodeResponse>(stop);
            }
            else
            {
                return (new DestoryZoneNodeResponse() { s2c_code = Response.CODE_ERROR, });
            }
        }

        /// <summary>
        /// 批量创建场景分线
        /// </summary>
        /// <param name="create"></param>
        /// <returns></returns>
        public virtual async Task<BatchCreateZoneLineResponse> BatchCreateZoneLine(BatchCreateZoneLineRequest create)
        {
            BatchCreateZoneLineResponse response = new BatchCreateZoneLineResponse();

            response.zoneList = new List<ZoneInfoSnap>();

            foreach (var item in create.zoneList)
            {
                var result = await CreateZone(item);
                ZoneInfoSnap zone = new ZoneInfoSnap();
                zone.lineIndex = GetZone(result.zoneUUID).lineIndex;
                zone.TemplateID = result.TemplateID;
                zone.uuid = result.zoneUUID;
                response.zoneList.Add(zone);
            }

            return response;
        }

        public virtual void DestoryZone(AreaZoneDestoryNotify stop)
        {
            try
            {
                ZoneInfo zone = zones.RemoveZone(stop.zoneUUID);
                if (zone != null)
                {
                    //log.Log("DestoryZone: " + stop.zoneUUID + " " + stop);
                    zone.area.currentZoneCount--;
                    zone.area.service.Call<DestoryZoneNodeResponse>(new DestoryZoneNodeRequest()
                    {
                        reason = stop.reason,
                        zoneUUID = stop.zoneUUID
                    }, (rsp, err) =>
                    {
                        //log.Log("DestoryZone: " + stop.zoneUUID + " " + rsp);
                    });
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
                throw err;
            }
        }
        public virtual RoleInfo GetRole(string roleID)
        {
            RoleInfo ret;
            if (roleID != null && roles.TryGetValue(roleID, out ret))
            {
                return ret;
            }
            return null;
        }
        public virtual ZoneInfo GetZone(string zoneUUID)
        {
            return zones.GetZone(zoneUUID);
        }
        public ZoneInfo GetGuildZone(string guildUUID)
        {
            return zones.GetGuildZone(guildUUID);
        }
        public ZoneInfo GetRoomZone(string roomKey)
        {
            return zones.GetRoomZone(roomKey);
        }
        public List<ZoneInfo> GetZoneList(string serverGroupID, int mapID)
        {
            return zones.GetZoneList(serverGroupID, mapID);
        }
        public RoleInfo[] GetAllRoles()
        {
            var ret = new List<RoleInfo>(roles.Values);
            return ret.ToArray();
        }
        public List<ZoneInfo> GetAllZones()
        {
            return zones.GetAllZones();
        }

        //------------------------------------------------------------------------------------------------------------------------------------
        public class AreaInfo
        {
            public readonly IRemoteService service;
            public readonly string key;
            public int currentRoleCount { get; internal set; }
            public int currentZoneCount { get; internal set; }
            public AreaStateNotify state { get; internal set; }
            public AreaInfo(IRemoteService svc)
            {
                this.service = svc;
                this.key = svc.Address.ServiceName;
                this.currentRoleCount = 0;
                this.currentZoneCount = 0;
            }
            public void WriteState(TextWriter sb)
            {
                sb.WriteLine("AreaState : " + service.Address.FullPath);
                sb.WriteLine("     role = " + currentRoleCount);
                sb.WriteLine("     zone = " + currentZoneCount);
                if (state != null)
                {
                    sb.WriteLine("      cpu = " + state.cpuPercent);
                    sb.WriteLine("   memory = " + state.memoryMB + "(MB)");
                }
            }
        }
        public class ZoneInfo
        {
            public readonly string uuid;
            public readonly AreaInfo area;
            public readonly MapTemplateData map_data;
            public readonly SceneData scene_data;

            public int currentRoleCount { get; internal set; }
            public string createrRoleID { get; internal set; }
            public string teamID { get; internal set; }
            public string serverID { get; internal set; }
            public string serverGroupID { get; internal set; }
            public int lineIndex { get; set; }
            public string guildUUID { get; set; }
            public string roomKey { get; internal set; }
            public bool close { get; internal set; }
            public string nodeName { get; internal set; }
            private HashMap<string, int> TeamIDData = new HashMap<string, int>();

            public ZoneInfo(string uuid, AreaInfo parent, MapTemplateData map_data, SceneData sdata, string serverID, string serverGroupID, string guildUUID, string nodeName)
            {
                this.uuid = uuid;
                this.area = parent;
                this.map_data = map_data;
                this.scene_data = sdata;
                this.currentRoleCount = 0;
                this.serverID = serverID;
                this.serverGroupID = serverGroupID;
                this.guildUUID = guildUUID;
                this.lineIndex = 1;
                this.roomKey = roomKey;
                this.nodeName = nodeName;
            }

            public void AddTeamID(string teamID)
            {
                if (teamID == null) return;
                TeamIDData.TryGetValue(teamID, out var count);
                count++;
                TeamIDData.Put(teamID, count);
            }

            public bool ContainTeamID(string teamID)
            {
                if (teamID == null) return false;
                return TeamIDData.ContainsKey(teamID);
            }

            public void RemoveTeamID(string teamID)
            {
                if (teamID == null) return;
                TeamIDData.TryGetValue(teamID, out var count);
                count--;
                TeamIDData.Put(teamID, count);
                if (count <= 0)
                    TeamIDData.Remove(teamID);
            }
        }
        public class RoleInfo
        {
            public readonly string uuid;
            public readonly RoleEnterZoneRequest enter;
            public ZoneInfo zone { get; internal set; }
            public RoleInfo(string uuid, RoleEnterZoneRequest req)
            {
                this.uuid = uuid;
                this.enter = req;
            }
        }
        public class ZoneMap
        {
            /// <summary>
            /// 场景UUID，场景INFO.
            /// </summary>
            private readonly Dictionary<string, ZoneInfo> zones = new Dictionary<string, ZoneInfo>();
            /// <summary>
            /// ServerGroupID,(场景UUID，场景INFO).
            /// </summary>
            private readonly Dictionary<string, Dictionary<string, ZoneInfo>> zonesMap = new Dictionary<string, Dictionary<string, ZoneInfo>>();
            /// <summary>
            /// ServerGroupID,(MAPID，场景分线(场景UUID)).
            /// </summary>
            private readonly Dictionary<string, Dictionary<int, List<ZoneInfo>>> zonesLineMap = new Dictionary<string, Dictionary<int, List<ZoneInfo>>>();

            /// <summary>
            /// 公会UUID，场景UUID(用于存放全区内所有公会场景).
            /// </summary>
            private readonly Dictionary<string, Dictionary<string, ZoneInfo>> guildZones = new Dictionary<string, Dictionary<string, ZoneInfo>>();
            /// <summary>
            /// 房间所有这生成房间ID，用于创建特殊场景（匹配房间等待）
            /// </summary>
            private readonly Dictionary<string, ZoneInfo> roomZones = new Dictionary<string, ZoneInfo>();

            public int Count { get => zones.Count; }

            public void AddZone(ZoneInfo zone)
            {
                zones.Add(zone.uuid, zone);
                //---------------------------------------------------------------------------------------------
                Dictionary<string, ZoneInfo> map = null;
                if (!zonesMap.TryGetValue(zone.serverGroupID, out map))
                {
                    map = new Dictionary<string, ZoneInfo>();
                    zonesMap.Add(zone.serverGroupID, map);
                }

                map.Add(zone.uuid, zone);
                //---------------------------------------------------------------------------------------------

                if (RPGServerTemplateManager.Instance.IsPublicMap(zone.map_data))
                {
                    Dictionary<int, List<ZoneInfo>> lineMap = null;
                    List<ZoneInfo> lt = null;
                    if (!zonesLineMap.TryGetValue(zone.serverGroupID, out lineMap))
                    {
                        lineMap = new Dictionary<int, List<ZoneInfo>>();
                        zonesLineMap.Add(zone.serverGroupID, lineMap);
                    }

                    if (!lineMap.TryGetValue(zone.map_data.id, out lt))
                    {
                        lt = new List<ZoneInfo>();
                        lineMap.Add(zone.map_data.id, lt);
                    }

                    int line = AddLine(zone, lt);
                    zone.lineIndex = line;
                }
                //---------------------------------------------------------------------------------------------
                if (zone.guildUUID != null)
                {
                    AddGuildZone(zone.guildUUID, zone);
                }

                if (!string.IsNullOrEmpty(zone.roomKey))
                {
                    roomZones.Add(zone.roomKey, zone);
                }
            }

            public ZoneInfo GetRoomZone(string roomKey)
            {
                if (string.IsNullOrEmpty(roomKey))
                {
                    return null;
                }

                if (roomZones.TryGetValue(roomKey, out var ret))
                {
                    //if (ret.close != true)
                        return ret;
                }

                return null;
            }
            public ZoneInfo RemoveZone(string uuid)
            {
                if (string.IsNullOrEmpty(uuid))
                    return null;

                ZoneInfo info;
                string guildUUID = null;
                if (zones.TryGetValue(uuid, out info))
                {
                    guildUUID = info.guildUUID;

                    if (zonesMap.TryGetValue(info.serverGroupID, out var map))
                    {
                        map.Remove(uuid);
                    }
                    zones.Remove(uuid);
                    //---------------------------------------------------------------------------------------------
                    //分线表删除.
                    if (zonesLineMap.TryGetValue(info.serverGroupID, out var lineMap))
                    {
                        lineMap.TryGetValue(info.map_data.id, out var lt);
                        RemoveLine(info.uuid, lt);
                    }
                    //---------------------------------------------------------------------------------------------
                    //公会场景表删除.
                    RemoveGuildZone(uuid, guildUUID);
                    if (!string.IsNullOrEmpty(info.roomKey))
                    {
                        roomZones.Remove(info.roomKey);
                    }
                }
                return info;
            }
            public void Clear()
            {
                zones.Clear();
                zonesMap.Clear();
                zonesLineMap.Clear();
                roomZones.Clear();
            }
            //             public Dictionary<string, ZoneInfo> Values()
            //             {
            //                 return zones;
            //             }
            public ZoneInfo GetZone(string uuid)
            {
                ZoneInfo ret = null;

                if (!string.IsNullOrEmpty(uuid) && zones.TryGetValue(uuid, out ret))
                {
                    //if (ret.close != true)
                    return ret;
                }

                return null;
            }
            /// <summary>
            /// 获取当前Group内，指定UUID的场景.
            /// </summary>
            /// <param name="serverGroupID"></param>
            /// <param name="uuid"></param>
            /// <returns></returns>
            public ZoneInfo GetZone(string serverGroupID, string uuid)
            {
                Dictionary<string, ZoneInfo> map = null;
                if (string.IsNullOrEmpty(uuid) || string.IsNullOrEmpty(serverGroupID))
                    return null;

                ZoneInfo ret = null;
                if (zonesMap.TryGetValue(serverGroupID, out map) && map.TryGetValue(uuid, out ret))
                {
                    //if (ret.close != true)
                    return ret;
                }

                return null;
            }
            public bool TryGetValue(string uuid, out ZoneInfo zoneinfo)
            {
                return zones.TryGetValue(uuid, out zoneinfo);
            }
            public void SetZoneCloseFlag(string uuid)
            {
                var zone = GetZone(uuid);
                if (zone != null)
                    zone.close = true;
            }
            private int AddLine(ZoneInfo zone, List<ZoneInfo> lt)
            {
                if (lt == null)
                {
                    lt = new List<ZoneInfo>();
                    lt.Add(zone);
                    return lt.Count;
                }
                else
                {
                    bool insert = false;
                    int line = 1;
                    for (int i = 0; i < lt.Count; i++)
                    {
                        if (lt[i] == null)
                        {
                            insert = true;
                            lt[i] = zone;
                            line = i + 1;
                            break;
                        }
                    }

                    if (insert == false)
                    {
                        lt.Add(zone);
                        line = lt.Count;
                    }

                    return line;
                }
            }
            private void RemoveLine(string uuid, List<ZoneInfo> lt)
            {
                if (lt == null || lt.Count == 0) return;

                for (int i = 0; i < lt.Count; i++)
                {
                    if (lt[i] != null && lt[i].uuid == uuid)
                    {
                        lt[i] = null;
                        break;
                    }
                }
            }
            private void AddGuildZone(string guildUUID, ZoneInfo zone)
            {
                if (!guildZones.TryGetValue(guildUUID, out var submap))
                {
                    submap = new Dictionary<string, ZoneInfo>();
                    guildZones.Add(guildUUID, submap);
                }

                submap.Add(zone.uuid, zone);
            }
            private void RemoveGuildZone(string uuid, string guilduuid)
            {
                if (string.IsNullOrEmpty(guilduuid))
                    return;

                if (string.IsNullOrEmpty(guilduuid) == false)
                {
                    guildZones.TryGetValue(guilduuid, out var submap);
                    if (submap != null)
                    {
                        submap.Remove(uuid);
                        if (submap.Count == 0)
                            guildZones.Remove(guilduuid);
                    }
                }
            }
            public ZoneInfo GetGuildZone(string uuid)
            {
                if (string.IsNullOrEmpty(uuid))
                    return null;

                ZoneInfo ret;
                if (guildZones.TryGetValue(uuid, out var submap))
                {
                    foreach (var item in submap)
                    {
                        ret = item.Value;
                        //if (ret.close != true)
                        return ret;
                    }

                }
                return null;
            }
            /// <summary>
            /// 获取当前Group内，指定场景的分线信息.
            /// </summary>
            /// <param name="serverGroupID"></param>
            /// <param name="mapID"></param>
            /// <returns></returns>
            public List<ZoneInfo> GetZoneList(string serverGroupID, int mapID)
            {
                Dictionary<int, List<ZoneInfo>> lineMap = null;
                if (zonesLineMap.TryGetValue(serverGroupID, out lineMap))
                {
                    List<ZoneInfo> lt;
                    List<ZoneInfo> ret;
                    if (lineMap.TryGetValue(mapID, out lt))
                    {
                        ret = new List<ZoneInfo>();

                        for (int i = 0; i < lt.Count; i++)
                        {
                            if (lt[i] != null /*&& lt[i].close != true*/)
                            {
                                ret.Add(lt[i]);
                            }
                        }
                        return ret;
                    }
                }
                return null;
            }
            public Dictionary<string, ZoneInfo> GetZoneMap(string serverGroupID)
            {
                if (string.IsNullOrEmpty(serverGroupID)) return null;
                zonesMap.TryGetValue(serverGroupID, out var ret);
                return new Dictionary<string, ZoneInfo>(ret);
            }
            public List<ZoneInfo> GetZones(int templateID)
            {
                var all = new List<ZoneInfo>(this.zones.Values);
                var ret = new List<ZoneInfo>();
                foreach (var zoneInfo in all)
                {
                    if (zoneInfo.map_data.id == templateID /*&& zoneInfo.close != true*/)
                    {
                        ret.Add(zoneInfo);
                    }
                }
                return ret;
            }
            public List<ZoneInfo> GetAllZones()
            {
                var all = new List<ZoneInfo>(this.zones.Values);
                var ret = new List<ZoneInfo>();
                foreach (var zoneInfo in all)
                {
                    // if (zoneInfo.close != true)
                    {
                        ret.Add(zoneInfo);
                    }
                }
                return ret;
            }
        }

        //------------------------------------------------------------------------------------------------------------------------------------
        #endregion
        //------------------------------------------------------------------------------------------------------------------------------------
    }
}
