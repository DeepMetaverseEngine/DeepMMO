using DeepCore.GameData.Zone.ZoneEditor;
using DeepCore.GameEvent;
using DeepCore.IO;
using DeepCore.Log;
using DeepCrystal.RPC;
using DeepMMO.Server.AreaManager;
using System;
using System.Threading.Tasks;
using DeepCore.Game3D.Host.Instance;
using DeepCore.Game3D.Host.ZoneEditor;
using DeepCore.Game3D.Host.ZoneServer;
using DeepCore.Game3D.Host.ZoneServer.Interface;
using DeepCore.Geometry;
using DeepMMO.Data;
using DeepCore;
using System.Text;
using System.IO;

namespace DeepMMO.Server.Area
{
    public class AreaZoneNode : IZoneNodeServer
    {
        private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder(typeof(AreaZoneNode));
        protected readonly Logger log;
        protected readonly AreaService service;
        protected readonly CreateZoneNodeRequest create;
        protected readonly MapTemplateData map_temp;
        protected readonly string uuid;
        protected readonly ZoneNode node;

        public string ZoneUUID { get { return uuid; } }
        public int MapTemplateID { get { return create.mapTemplateID; } }
        public int ZoneTemplateID { get { return map_temp.zone_template_id; } }
        public string GuildUUID => create.guildUUID;
        public string RoomKey => create.roomKey;
        public string ServerID => create.serverID;
        public string ServerGroupID => create.serverGroupID;
        public ZoneNode ZoneNode { get { return node; } }
        public AreaZoneNode(AreaService svc, CreateZoneNodeRequest create, MapTemplateData map_temp)
        {
            Alloc.RecordConstructor(GetType().ToVisibleName() + ":" + map_temp.id);
            this.log = LoggerFactory.GetLogger(string.Format("{0}-{1}", GetType().Name, create.managerZoneUUID));
            this.service = svc;
            this.create = create;
            this.map_temp = map_temp;
            this.uuid = create.managerZoneUUID;
            this.node = CreateZoneNode(create);
        }
        ~AreaZoneNode()
        {
            Alloc.RecordDestructor(GetType().ToVisibleName() + ":" + map_temp.id);
        }
        public virtual void rpc_Handle(ISerializable msg, OnRpcReturn<ISerializable> cb)
        {
            zone_rpc_call_handler?.Invoke(msg, (rsp, err) => { cb(rsp as ISerializable, err); });
        }
        public virtual void rpc_Handle(ISerializable msg)
        {
            zone_rpc_invoke_handler?.Invoke(msg);
        }
        public virtual void rpc_Call(ISerializable msg, OnRpcReturn<ISerializable> cb)
        {
        }
        public virtual void rpc_Invoke(ISerializable msg)
        {
        }

        //--------------------------------------------------------------------------------------------------------------------------------
        #region 主动调用
        //--------------------------------------------------------------------------------------------------------------------------------
        public virtual async Task<EditorScene> DoStartAsync()
        {
            var sd = LoadSceneData(create);
            var zone = await this.node.StartAsync(sd) as EditorScene;
            zone.UUID = ZoneUUID;
            OnNodeStarted(zone as EditorScene);
            return zone;
        }

        public virtual Task<EditorScene> DoStopAsync()
        {
            Alloc.RecordDispose(GetType().ToVisibleName() + ":" + map_temp.id);
            node.OnZoneStop += (z) =>
            {
                OnNodeStopped(z as EditorScene);
            };
            return node.StopAsync().ContinueWith(t => t.Result as EditorScene);
        }

        public virtual async Task<RoleEnterZoneResponse> DoPlayerEnterAsync(AreaZonePlayer player, RoleEnterZoneRequest enter)
        {
            if (await player.OnEnterAsync() == false)
            {
                log.Error("Can Not Find Session Or Logic : " + enter.roleSessionName + " : " + enter.roleUUID);
            }
            try
            {
                player.SessionReconnect();
                var tcs = new System.Threading.Tasks.TaskCompletionSource<RoleEnterZoneResponse>();
                this.node.PlayerEnter(player, ToAddUnit(enter), client =>
                {
                    if (HasAddPlayer == false)
                    {
                        HasAddPlayer = true;
                    }
                    client.Actor.SetAttribute(nameof(AreaZonePlayer.RoleSessionName), player.RoleSessionName);
                    tcs.TrySetResult(new RoleEnterZoneResponse()
                    {
                        s2c_code = RoleEnterZoneResponse.CODE_OK,
                        mapTemplateID = this.MapTemplateID,
                        zoneUUID = this.ZoneUUID,
                        zoneTemplateID = this.ZoneTemplateID,
                        roleBattleData = enter.roleData,
                        roleDisplayName = enter.roleDisplayName,
                        roleUnitTemplateID = enter.roleUnitTemplateID,
                        roleScenePos = new ZonePosition()
                        {
                            x = client.Actor.X,
                            y = client.Actor.Y,
                            z = client.Actor.Z
                        },
                        areaName = service.SelfAddress.ServiceName,
                        areaNode = service.SelfAddress.ServiceNode,
                        guildUUID = enter.guildUUID,
                    });
                }, err =>
                {
                    tcs.TrySetResult(new RoleEnterZoneResponse()
                    {
                        s2c_code = RoleEnterZoneResponse.CODE_ERROR,
                        s2c_msg = err.Message,
                    });
                });
                return await tcs.Task;
            }
            catch (Exception err)
            {
                log.Error(err);
                return (new RoleEnterZoneResponse()
                {
                    s2c_code = RoleEnterZoneResponse.CODE_ERROR,
                    s2c_msg = err.Message,
                });
            }
        }
        public virtual Task<RoleLeaveZoneResponse> DoPlayerLeaveAsync(AreaZonePlayer player, RoleLeaveZoneRequest leave)
        {
            var tcs = new System.Threading.Tasks.TaskCompletionSource<RoleLeaveZoneResponse>();
            this.node.PlayerLeave(player,
                (c) =>
                {
                    tcs.TrySetResult(new RoleLeaveZoneResponse()
                    {
                        lastScenePos = new Data.ZonePosition()
                        {
                            x = c.Actor.X,
                            y = c.Actor.Y,
                            z = c.Actor.Z,
                        },
                        curHP = c.Actor.CurrentHP,
                        curMP = c.Actor.CurrentMP,
                        LeaveZoneSaveData = c.LastZoneSaveData,
                    });
                },
                (e) =>
                {
                    tcs.TrySetResult(new RoleLeaveZoneResponse()
                    {
                        s2c_code = RoleLeaveZoneResponse.CODE_ERROR,
                        s2c_msg = e.Message,
                    });
                });
            return tcs.Task;
        }

        public Task<GetRolePositionResponse> DoGetPlayerPosition(AreaZonePlayer player, GetRolePositionRequest req)
        {
            return node.QueuePlayerTaskAsync<GetRolePositionResponse>(player.RoleUUID, (instancePlayer) =>
            {
                return (new GetRolePositionResponse()
                {
                    x = instancePlayer.X,
                    y = instancePlayer.Y,
                    z = instancePlayer.Z,
                    s2c_code = GetRolePositionResponse.CODE_OK
                });
            });

        }

        public virtual void DoPlayerDisconnect(AreaZonePlayer player)
        {
            player.SessionDisconnect();
            this.node.PlayerDisconnect(player, (client) => { }, (err) => { });
        }
        public virtual void DoPlayerReconnect(AreaZonePlayer player)
        {
            player.SessionReconnect();
            this.node.PlayerReconnect(player, (client) => { }, (err) => { });
        }
        public virtual Task DoPlayerBeginLeaveAsync(AreaZonePlayer player)
        {
            player.SessionBeginLeave();
            return this.node.QueueSceneTaskAsync(z => { return 1; });
        }

        public virtual Task<RoleEnterZoneResponse> DoPlayerEnterReplace(AreaZonePlayer player, RoleEnterZoneRequest enter)
        {
            return node.QueuePlayerTaskAsync<RoleEnterZoneResponse>(player.RoleUUID, (instancePlayer) =>
             {
                 return (new RoleEnterZoneResponse()
                 {
                     mapTemplateID = this.MapTemplateID,
                     zoneUUID = this.ZoneUUID,
                     zoneTemplateID = this.ZoneTemplateID,
                     roleBattleData = enter.roleData,
                     roleDisplayName = enter.roleDisplayName,
                     roleUnitTemplateID = enter.roleUnitTemplateID,
                     roleScenePos = new ZonePosition()
                     {
                         x = instancePlayer.X,
                         y = instancePlayer.Y,
                         z = instancePlayer.Z
                     },

                     areaName = service.SelfAddress.ServiceName,
                     areaNode = service.SelfAddress.ServiceNode,
                     guildUUID = enter.guildUUID,
                     s2c_code = RoleEnterZoneResponse.CODE_OK_REPLACE
                 });
             });


            // var client = this.node.GetPlayerClient(player.RoleUUID);
            /*
            float px = 0, py = 0, pz = 0;
            if (client != null)
            {
                px = client.Actor.X;
                py = client.Actor.Y;
                pz = client.Actor.Z;
            }

            return (new RoleEnterZoneResponse()
            {
                mapTemplateID = this.MapTemplateID,
                zoneUUID = this.ZoneUUID,
                zoneTemplateID = this.ZoneTemplateID,
                roleBattleData = enter.roleData,
                roleDisplayName = enter.roleDisplayName,
                roleUnitTemplateID = enter.roleUnitTemplateID,
                roleScenePos = new ZonePosition() { x = px, y = py, z = pz},
                areaName = service.SelfAddress.ServiceName,
                areaNode = service.SelfAddress.ServiceNode,
                guildUUID = enter.guildUUID,
                s2c_code = RoleEnterZoneResponse.CODE_OK_REPLACE;
            });
            */
        }
        #endregion
        //--------------------------------------------------------------------------------------------------------------------------------
        #region 构造对象
        //--------------------------------------------------------------------------------------------------------------------------------
        protected virtual AddUnit ToAddUnit(RoleEnterZoneRequest enter)
        {
            var unitInfo = RPGServerBattleManager.DataRoot.Templates.GetUnit(enter.roleUnitTemplateID);
            if (unitInfo == null)
            {
                unitInfo = service.GetDefaultUnitTemplate();
            }
            DeepCore.Vector.Vector3 pos = null;
            if (enter.roleScenePos != null)
            {
                if (enter.roleScenePos.HasFlag)
                {
                    var sceneObj = node.FindSceneObjData(enter.roleScenePos.flagName);
                    if (sceneObj != null)
                    {
                        pos = new DeepCore.Vector.Vector3(sceneObj.X, sceneObj.Y, sceneObj.Z);
                    }
                }
                else if (enter.roleScenePos.HasPos)
                {
                    if (enter.roleScenePos.x == enter.roleScenePos.y && enter.roleScenePos.x == -1)
                    {
                        var map = node.SceneData.GetStartRegions();
                        var ret = map.Get(2);
                        if (ret != null)
                            pos = new DeepCore.Vector.Vector3(ret.X, ret.Y, ret.Z);
                        else
                            pos = new DeepCore.Vector.Vector3(enter.roleScenePos.x, enter.roleScenePos.y, enter.roleScenePos.z);

                    }
                    else
                        pos = new DeepCore.Vector.Vector3(enter.roleScenePos.x, enter.roleScenePos.y, enter.roleScenePos.z);
                }
            }
            var add = new AddUnit()
            {
                info = unitInfo,
                player_uuid = enter.roleUUID,
                pos = pos,
                arg = enter,
                //中立单位0，怪物1，玩家2.
                force = enter.roleForce > 0 ? (byte)enter.roleForce : (byte)2,
                editor_name = enter.roleDisplayName,
                last_zone_save_data = enter.LastZoneSaveData,
            };
            return add;
        }
        protected virtual SceneData LoadSceneData(CreateZoneNodeRequest input)
        {
            return RPGServerBattleManager.Instance.GetSceneAsCache(this.ZoneTemplateID);
        }
        protected virtual ZoneNode CreateZoneNode(CreateZoneNodeRequest input)
        {
            return RPGServerBattleManager.ZoneFactory.CreateServerZoneNode(this, RPGServerBattleManager.DataRoot, RPGServerBattleManager.NodeConfig);
        }

        #endregion
        //--------------------------------------------------------------------------------------------------------------------------------
        #region 回调事件
        //--------------------------------------------------------------------------------------------------------------------------------
        public EventManager EventMgr { get; private set; }

        protected virtual void OnNodeStarted(EditorScene z)
        {
            z.OnUnitDead += Z_OnUnitDead;
            z.OnUnitGotInstanceItem += Z_OnUnitGotInstanceItem;
            z.OnGameOver += Z_OnGameOver;

            if (TimerConfig.timer_sec_ZoneKeepPlayerTimeout > 0)//场景无人后清理时间.程序控制.
            {
                keepPlayerExpire = TimeSpan.FromSeconds(TimerConfig.timer_sec_ZoneKeepPlayerTimeout);
                z.AddTimePeriodicMS((int)(keepPlayerExpire.TotalMilliseconds / 2), (t) =>
                  {
                      CheckZoneDispose(z, t);
                  });
            }
            this.EventMgr = EventManagerFactory.Instance.CreateEventManager("Zone", z.UUID);
            if (this.EventMgr != null)
            {
                this.EventMgr.PutObject("Zone", z);
                this.EventMgr.PutObject("Service", this.service);
                this.EventMgr.PutObject("Node", this);
                this.EventMgr.Start();
                node.OnZoneBeginUpdate += zone =>
                {
                    this.EventMgr.EnterUpdate();
                };
                node.OnZoneEndUpdate += zone =>
                {
                    this.EventMgr.Update();
                };
            }
        }

        protected virtual void OnNodeStopped(EditorScene z)
        {
            this.zone_rpc_call_handler = null;
            this.zone_rpc_invoke_handler = null;
            z.OnUnitDead -= Z_OnUnitDead;
            z.OnUnitGotInstanceItem -= Z_OnUnitGotInstanceItem;
            z.OnGameOver -= Z_OnGameOver;
            this.EventMgr?.Dispose();
            this.EventMgr = null;
        }
        //--------------------------------------------------------------------------------------------------------------------------------
        public virtual void GetStatus(TextWriter sb)
        {
            sb.WriteLine("         UUID = " + this.uuid);
            sb.WriteLine("    SceneData = " + this.node.SceneData);
            sb.WriteLine("  PlayerCount = " + this.node.PlayerCount);
            //sb.WriteLine("   ExpireTime = " + this.mExpireTimeUTC.ToLocalTime());
            sb.WriteLine("   IsGameOver = " + zoneGameOver);
            sb.WriteLine(" HasAddPlayer = " + HasAddPlayer);
            sb.WriteLine("     PassTime = " + node.ZonePassTime);
        }
        private DateTime keepPlayerLastTick = DateTime.Now;
        private TimeSpan keepPlayerExpire;
        private bool zoneGameOver = false;
        //private DateTime mExpireTimeUTC = DateTime.UtcNow;
        private bool HasAddPlayer = false;// player already in zone
//         public void SetSceneExpireTime(DateTime time)
//         {
//             mExpireTimeUTC = time.ToUniversalTime();
//         }
//         private bool IsExpire()
//         {
//             return (DateTime.UtcNow - mExpireTimeUTC).TotalSeconds > 0;
//         }
        /// <summary>
        /// 检测是否需要维持场景，否则，则销毁场景
        /// </summary>
        /// <returns></returns>
        protected virtual bool CheckNeedKeepPlayer(EditorScene z)
        {
            if (node.PlayerCount > 0) return true;
            if (zoneGameOver) return false;
            if (HasAddPlayer == false) return true;
           // if (!IsExpire()) return true;
            return false;
        }
        protected virtual void CheckZoneDispose(EditorScene z, TimeTaskMS t)
        {
            if (CheckNeedKeepPlayer(z))
            {
                zoneGameOver = false;
                keepPlayerLastTick = DateTime.Now;
            }
            else if ((DateTime.Now - keepPlayerLastTick) > keepPlayerExpire)
            {
                if (!zoneGameOver)
                {
                    zoneGameOver = true;
                    //notify areamanager close zone.
                    service.area_manager.Invoke(new AreaZoneGameOverNotify()
                    {
                        zoneUUID = this.ZoneUUID,
                        reason = "KeepPlayerTimeOver",
                    });
                }
                t.Dispose();
                //start delay desotry.
                var delayDestoryTime = TimeSpan.FromSeconds(TimerConfig.timer_sec_DelayDestoryTime);
                // if (delayDestoryTime > TimeSpan.Zero)
                {
                    //log.Info("ZoneNode Start Delay Destory : " + delayDestoryTime + " " + this.ZoneUUID);
                    var evt = new DeepCore.GameData.Zone.GameOverEvent() { WinForce = 0, message = "KeepPlayerTimeOver" };
                    service.Provider.Delay((st) =>
                    {
                        if (node.IsDisposed) { return; }
                        //log.Info("ZoneNode Send AreaZoneDestoryNotify ZoneUUID : " + this.ZoneUUID);
                        NotifyAllLogicsGameOver(evt);
                        service.area_manager.Invoke(new AreaZoneDestoryNotify()
                        {
                            zoneUUID = this.ZoneUUID,
                            reason = evt.message,
                        });
                    }, create, delayDestoryTime);
                }
            }
        }
        //--------------------------------------------------------------------------------------------------------------------------------


        protected virtual void Z_OnUnitDead(InstanceZone zone, InstanceUnit obj, InstanceUnit attacker)
        {
            if (attacker != null && obj != null)
            {
                if (obj.Info.UType == DeepCore.GameData.Zone.UnitInfo.UnitType.TYPE_MONSTER && attacker is InstancePlayer)
                {
                    //玩家.
                    var player = attacker as InstancePlayer;
                    //生成奖励信息.
                    var notify = GetRoleBattleAward(zone, obj, player);
                    if (notify != null)
                    {
                        //通知游戏服.
                        NotifyLogicRoleAward(player.PlayerUUID, notify);
                    }
                }
            }
        }
        //交给子类去实现
        protected virtual void Z_OnUnitGotInstanceItem(InstanceZone zone, InstanceUnit unit, InstanceItem item)
        {
            //TODO
        }
        protected virtual void Z_OnGameOver(InstanceZone zone, DeepCore.GameData.Zone.GameOverEvent evt)
        {
            zoneGameOver = true;
            //log.Info("ZoneNode GameOver : " + evt.message + " " + this.ZoneUUID);
            //notify logic.
            NotifyAllLogicsGameOver(evt);
            //notify areamanager close zone.
            service.area_manager.Invoke(new AreaZoneGameOverNotify()
            {
                zoneUUID = this.ZoneUUID,
                reason = evt.message,
            });
        }

        /// <summary>
        /// 通知逻辑服，场景结束
        /// </summary>
        /// <param name="evt"></param>
        protected virtual void NotifyAllLogicsGameOver(DeepCore.GameData.Zone.GameOverEvent evt)
        {
            node.ForEachPlayers((p) =>
            {
                var player = p.Client as AreaZonePlayer;
                player.DoGameOver(evt);
            });
        }
        /// <summary>
        /// 通知客户端，场景即将结束
        /// </summary>
        protected virtual void NotifyAllSessionsGameOver(TimeSpan delayTime)
        {
            //TODO 客户端显示倒计时
            //node.ForEachPlayers((p) =>
            //{
            //    var player = p.Client as AreaZonePlayer;
            //    player.SendToSession(null);
            //});
        }

        /// <summary>
        /// 向游戏服logic发送奖励信息.
        /// </summary>
        /// <param name="uuid"></param>
        /// <param name="notify"></param>
        protected virtual void NotifyLogicRoleAward(string uuid, RoleBattleAwardNotify notify)
        {
            var logic = service.GetPlayer(uuid);
            if (logic != null)
            {
                logic.logic_rpc_Invoke(notify);
            }
        }

        /// <summary>
        /// 生成奖励.
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="obj"></param>
        /// <param name="attacker"></param>
        /// <returns></returns>
        protected virtual RoleBattleAwardNotify GetRoleBattleAward(InstanceZone zone, InstanceUnit obj, InstancePlayer attacker)
        {
            RoleBattleAwardNotify ret = new RoleBattleAwardNotify();
            ret.MonsterID = obj.Info.ID;
            ret.RoleID = attacker.PlayerUUID;
            return ret;
        }

        #endregion
        //--------------------------------------------------------------------------------------------------------------------------------
        #region IServer

        private Action<object, Action<object, Exception>> zone_rpc_call_handler;
        private Action<object> zone_rpc_invoke_handler;

        void IZoneNodeServer.GameServerRpcInvoke(object msg)
        {
            this.rpc_Invoke(msg as ISerializable);
        }
        void IZoneNodeServer.GameServerRpcCall(object msg, Action<object, Exception> callback)
        {
            this.rpc_Call(msg as ISerializable, (rsp, err) => { callback(rsp, err); });
        }
        void IZoneNodeServer.ListenGameServerRpcInvoke(Action<object> handler)
        {
            this.zone_rpc_invoke_handler = handler;
        }
        void IZoneNodeServer.ListenGameServerRpcCall(Action<object, Action<object, Exception>> handler)
        {
            this.zone_rpc_call_handler = handler;
        }
        #endregion
    }
}

