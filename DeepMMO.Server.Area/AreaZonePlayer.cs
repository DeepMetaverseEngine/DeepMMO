using DeepCore;
using DeepCore.Concurrent;
using DeepCore.GameData.Zone;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Protocol;
using DeepCrystal.RPC;
using DeepMMO.Protocol.Client;
using DeepMMO.Server.AreaManager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DeepCore.Game3D.Host.Instance;
using DeepCore.Game3D.Host.ZoneServer;
using DeepCore.Game3D.Host.ZoneServer.Interface;

namespace DeepMMO.Server.Area
{
    public class AreaZonePlayer : Disposable, IZoneNodePlayer
    {
        private readonly static TypeAllocRecorder Alloc = new TypeAllocRecorder(typeof(AreaZonePlayer));
        protected readonly Logger log;
        protected readonly AreaService service;
        protected readonly AreaZoneNode node;
        protected readonly int client_event_route;
        protected IRemoteService remote_session;
        protected IRemoteService remote_logic;
        public readonly RoleEnterZoneRequest enter;
        private HashMap<string, object> mAttributes = new HashMap<string, object>();
        private bool pause_client = false;
        private bool pause_logic = false;

        public AreaZoneNode ZoneNode { get { return node; } }
        public string RoleUUID { get { return enter.roleUUID; } }
        public string RoleSessionName { get { return enter.roleSessionName; } }
        public string ZoneUUID { get { return node.ZoneUUID; } }
        public InstancePlayer Actor { get { return mBinding.Value.Actor; } }

        public AreaZonePlayer(AreaService svc, AreaZoneNode node, RoleEnterZoneRequest enter)
        {
            Alloc.RecordConstructor(this.GetType());
            this.log = LoggerFactory.GetLogger(GetType().Name);
            this.service = svc;
            this.node = node;
            this.enter = enter;
            this.client_event_route = TypeCodec.GetAttributeRoute(typeof(ClientBattleEvent));
        }
        ~AreaZonePlayer()
        {
            Alloc.RecordDestructor(this.GetType());
        }
        protected override void Disposing()
        {
            Alloc.RecordDispose(this.GetType());
        }
        internal async Task<bool> OnEnterAsync()
        {
            this.remote_session = await service.Provider.GetAsync(new RemoteAddress(enter.roleSessionName, enter.roleSessionNode));
            this.remote_logic = await service.Provider.GetAsync(ServerNames.GetLogicServiceAddress(enter.roleUUID, enter.roleLogicNode));
            return remote_session != null && remote_logic != null;
        }
        public virtual void SessionDisconnect()
        {
            this.remote_session.WormholeTransport(new ClientLeaveZoneNotify()
            {
                s2c_ZoneUUID = node.ZoneUUID,
            });
            this.remote_session.Invoke(new SessionUnbindAreaNotify()
            {
                areaName = service.SelfAddress.ServiceName,
                areaNode = service.SelfAddress.ServiceNode
            });
            this.pause_client = true;
        }
        public virtual void SessionBeginLeave()
        {
            this.pause_logic = true;
        }

        public virtual void SessionReconnect()
        {
            this.pause_client = false;
            this.pause_logic = false;
            this.remote_session?.WormholeTransport(new ClientEnterZoneNotify()
            {
                s2c_ZoneUUID = node.ZoneUUID,
                s2c_MapTemplateID = node.MapTemplateID,
                s2c_ZoneTemplateID = node.ZoneTemplateID,
                s2c_RoleDisplayName = enter.roleDisplayName,
                s2c_RoleUnitTemplateID = enter.roleUnitTemplateID,
                s2c_SceneLineIndex = enter.expectLineIndex,
                s2c_GuildUUID = enter.guildUUID,
                s2c_ZoneUpdateIntervalMS = ZoneNode.ZoneNode.FixedUpdateIntervalMS,
                s2c_Ext = enter.ext,
            });
            this.remote_session?.Invoke(new SessionBindAreaNotify()
            {
                areaName = service.SelfAddress.ServiceName,
                areaNode = service.SelfAddress.ServiceNode
            });
        }

        public virtual void DoGameOver(DeepCore.GameData.Zone.GameOverEvent evt)
        {
            this.remote_logic?.Invoke(new AreaGameOverNotify()
            {
                zoneUUID = node.ZoneUUID,
                mapTemplateID = node.MapTemplateID,
                zoneTemplateID = node.ZoneTemplateID,
                winForce = evt.WinForce,
                message = evt.message,
            });
        }

        public virtual void SendToSession(ISerializable msg)
        {
            this.remote_session?.Invoke(msg);
        }

        //---------------------------------------------------------------------------------------------
        #region ZoneEvents

        protected virtual void Actor_OnTransportScene(InstancePlayer player, InstanceFlag flag, int nextSceneID, string nextScenePosition)
        {
            this.remote_logic?.Invoke(new RoleNeedTransportNotify()
            {
                fromAreaName = service.SelfAddress.ServiceName,
                fromAreaNode = service.SelfAddress.ServiceNode,
                nextZoneID = nextSceneID,
                nextMapID = nextSceneID,
                nextZoneFlagName = nextScenePosition,
            });
        }

        #endregion
        //---------------------------------------------------------------------------------------------
        #region Attributes
        public bool IsAttribute(string key)
        {
            return mAttributes.ContainsKey(key);
        }
        public void SetAttribute(string key, object value)
        {
            mAttributes.Put(key, value);
        }
        public object RemoveAttribute(string key)
        {
            return mAttributes.RemoveByKey(key);
        }
        public object GetAttribute(string key)
        {
            return mAttributes.Get(key);
        }
        public T GetAttributeAs<T>(string key)
        {
            object obj = mAttributes.Get(key);
            if (obj != null)
            {
                return (T)obj;
            }
            return default(T);
        }
        #endregion
        //---------------------------------------------------------------------------------------------
        #region RPC

        /// <summary>
        /// ClientBattleAction
        /// </summary>
        /// <param name="msg"></param>
        public virtual void client_rpc_Handle(Stream clientBattleAction)
        {
            try
            {
                object action;
                // Drop 4 for head message id //
                clientBattleAction.Position += 4;
                if (node.ZoneNode.Codec.doDecode(clientBattleAction, out action))
                {
                    OnHandleClientMessage(action as IMessage);
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
        }
        public virtual void client_rpc_Handle(ArraySegment<byte> clientBattleAction)
        {
            try
            {
                object action;
                // Drop 4 for head message id //
                //clientBattleAction.Position += 4;
                if (node.ZoneNode.Codec.doDecode(new ArraySegment<byte>(clientBattleAction.Array, clientBattleAction.Offset + 4, clientBattleAction.Count - 4), out action))
                {
                    OnHandleClientMessage(action as IMessage);
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
        }
        public virtual void client_rpc_Handle(byte[] clientBattleAction)
        {
            try
            {
                // Drop Head 4 bytes //
                if (node.ZoneNode.Codec.doDecode(new ArraySegment<byte>(clientBattleAction, 4, clientBattleAction.Length - 4), out var action))
                {
                    OnHandleClientMessage(action as IMessage);
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
        }
        public virtual void client_rpc_Handle(ClientBattleAction msg)
        {
            try
            {
                if (node.ZoneNode.Codec.doDecode(new ArraySegment<byte>(msg.c2s_battleAction), out var action))
                {
                    OnHandleClientMessage(action as IMessage);
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
        }

        public virtual void logic_rpc_Handle(ISerializable msg)
        {
            if (mRpcHandleInvoke != null)
                mRpcHandleInvoke(msg);
        }
        public virtual void logic_rpc_Handle(ISerializable msg, OnRpcReturn<ISerializable> cb)
        {
            if (mRpcHandleCall != null)
                mRpcHandleCall(msg, (rsp, err) => { cb(rsp as ISerializable, err); });
        }
        public virtual void logic_rpc_Invoke(ISerializable msg)
        {
            if (pause_logic) return;
            lock (mLogicSendingQueue) { mLogicSendingQueue.Add(msg); }
        }
        public virtual void logic_rpc_Call(ISerializable msg, OnRpcReturn<ISerializable> cb)
        {
            this.remote_logic?.Call<ISerializable>(msg, cb);
        }

        #endregion
        //---------------------------------------------------------------------------------------------
        #region  IPlayer
        //---------------------------------------------------------------------------------------------
        private void OnHandleClientMessage(IMessage action)
        {
            if (mHandleClientMessage != null)
            {
                mHandleClientMessage.Invoke(action);
            }
            else
            {
                node.ZoneNode.QueueSceneTask((z, err) =>
                {
                    if (err != null)
                    {
                        log.Error(err.Message, err);
                    }
                    else if (mHandleClientMessage != null)
                    {
                        mHandleClientMessage.Invoke(action);
                    }
                });
            }
        }
        private AtomicReference<ZoneNode.PlayerClient> mBinding = new AtomicReference<ZoneNode.PlayerClient>(null);
        private Action<IMessage> mHandleClientMessage;
        private Action<object, Action<object, Exception>> mRpcHandleCall;
        private Action<object> mRpcHandleInvoke;
        private PackEvent mSendingQueue = new PackEvent();
        private List<ISerializable> mLogicSendingQueue = new List<ISerializable>();


        string IZoneNodePlayer.PlayerUUID { get { return enter.roleUUID; } }
        string IZoneNodePlayer.DisplayName { get { return enter.roleDisplayName; } }
        ZoneNode.PlayerClient IZoneNodePlayer.BindingPlayer
        {
            get { return mBinding.Value; }
            set { mBinding.Value = value; }
        }
        void IZoneNodePlayer.ListenClient(Action<IMessage> handler)
        {
            mHandleClientMessage = handler;
        }
        void IZoneNodePlayer.ClientSend(PlayerMessageEntry msg)
        {
            if (pause_client) return;
            if (msg.buffer != null)
            {
                mSendingQueue.events.Add(msg.buffer);
            }
            else
            {
                mSendingQueue.events.Add(msg.message);
            }
        }
        void IZoneNodePlayer.ClientFlush()
        {
            try
            {
                if (pause_logic == false)
                {
                    lock (mLogicSendingQueue)
                    {
                        if (mLogicSendingQueue.Count > 0)
                        {
                            remote_logic?.BatchInvoke(mLogicSendingQueue);
                            mLogicSendingQueue.Clear();
                        }
                    }
                }
                if (pause_client == false)
                {
                    if (mSendingQueue.events.Count > 0)
                    {
                        mSendingQueue.sequenceNo = node.ZoneNode.ZoneTick;
                        using (var buffer = MemoryStreamObjectPool.AllocAutoRelease())
                        {
                            try
                            {
                                if (node.ZoneNode.Codec.doEncodeWithHead(mSendingQueue, buffer))
                                {
                                    var notify = BinaryMessage.FromBuffer(client_event_route, buffer);
                                    remote_session?.WormholeTransport(notify);
                                }
                            }
                            catch (Exception err)
                            {
                                log.Error(err.Message, err);
                            }
                            finally
                            {
                                mSendingQueue.events.Clear();
                            }
                        }

                    }
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
        }
        void IZoneNodePlayer.OnPlayerConnected(ZoneNode.PlayerClient binding)
        {
            pause_client = false;
            mBinding.Value = binding;
            binding.Actor.OnTransportScene += Actor_OnTransportScene;
        }

        void IZoneNodePlayer.OnPlayerDisconnect(ZoneNode.PlayerClient binding)
        {
            mBinding.Value = null;
        }
        void IZoneNodePlayer.OnPlayerDisposed()
        {
            this.pause_client = true;
            this.pause_logic = true;
        }


        void IZoneNodePlayer.GameServerRpcInvoke(object msg)
        {
            this.logic_rpc_Invoke(msg as ISerializable);
        }
        void IZoneNodePlayer.GameServerRpcCall(object msg, Action<object, Exception> callback)
        {
            this.logic_rpc_Call(msg as ISerializable, (rsp, err) => { callback(rsp, err); });
        }
        void IZoneNodePlayer.ListenGameServerRpcInvoke(Action<object> handler)
        {
            mRpcHandleInvoke = handler;
        }
        void IZoneNodePlayer.ListenGameServerRpcCall(Action<object, Action<object, Exception>> handler)
        {
            mRpcHandleCall = handler;
        }
        #endregion
        //---------------------------------------------------------------------------------------------

        //---------------------------------------------------------------------------------------------
    }

}
