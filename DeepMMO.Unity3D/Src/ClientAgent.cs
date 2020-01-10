using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DeepCore;
using DeepCore.FuckPomeloClient;
using DeepCore.Game3D.Slave.Layer;
using DeepCore.IO;
using DeepCore.Net;
using DeepMMO.Client;
using DeepMMO.Client.Battle;
using DeepMMO.Data;
using DeepMMO.Protocol;
using DeepMMO.Protocol.Client;

namespace DeepMMO.Unity3D
{
    public class ClientAgent : Disposable
    {
        public enum SocketType
        {
            GateSocket,
            GameSocket
        }

        public enum NetWorkState
        {
            Connected,
            DisConnected,
            Error
        }


        public class NetEventData
        {
            public SocketType Type { get; set; }
            public NetWorkState State { get; set; }
            public CloseReason Reason { get; set; }
            public string Error { get; set; }

            public NetEventData(SocketType type, NetWorkState state)
            {
                Type = type;
                State = state;
            }

            public NetEventData(SocketType type, NetWorkState state, CloseReason reason) : this(type, state)
            {
                Reason = reason;
            }
        }

        private Action<NetEventData> mOnNetEvent;

        public event Action<NetEventData> OnNetEvent
        {
            add => mOnNetEvent += value;
            remove => mOnNetEvent -= value;
        }

        private readonly RPGClient mNetClient;
        private readonly Queue<NetEventData> mNetStateQueue = new Queue<NetEventData>();

        public PomeloClient GameClient => mNetClient.GameClient;
        public PomeloClient GateClient => mNetClient.GateClient;

        public List<ServerInfo> LastAllServers => RPGClientTemplateManager.Instance.GetAllServers();
        public List<ServerInfo> LastRecommendServers => RPGClientTemplateManager.Instance.GetRecommendServers();

        public List<RoleIDSnap> LastRoleList => mNetClient.last_EnterGateResponse?.s2c_roleIDList;

        public ClientRoleData LastRoleData => mNetClient.last_EnterGameResponse?.s2c_role;

        public string Account { get; private set; }
        public string Token { get; private set; }


        public ClientAgent(ClientInfo info, IExternalizableFactory codec)
        {
            mNetClient = new RPGClient(codec, info);
            mNetClient.GateClient.OnConnected += (msg, token) => { mNetStateQueue.Enqueue(new NetEventData(SocketType.GateSocket, NetWorkState.Connected)); };
            mNetClient.GateClient.OnDisconnected += (reason, err) => { mNetStateQueue.Enqueue(new NetEventData(SocketType.GateSocket, NetWorkState.DisConnected, reason)); };
            mNetClient.GameClient.OnConnected += (msg, token) => { mNetStateQueue.Enqueue(new NetEventData(SocketType.GameSocket, NetWorkState.Connected)); };
            mNetClient.GameClient.OnDisconnected += (reason, err) =>
            {
                var netEvtData = new NetEventData(SocketType.GameSocket, NetWorkState.DisConnected, reason) {Error = err};
                mNetStateQueue.Enqueue(netEvtData);
            };
            mNetClient.GameClient.NetError += (err) => { mNetStateQueue.Enqueue(new NetEventData(SocketType.GameSocket, NetWorkState.Error)); };
            mNetClient.GateClient.OnRequestStart += OnRequestStart;
            mNetClient.GameClient.OnRequestStart += OnRequestStart;
            mNetClient.GameClient.OnRequestEnd += OnRequestEnd;
            mNetClient.GameClient.OnRequestEnd += OnRequestEnd;

            mNetClient.OnGameEntered += (client, response) => { };
            mNetClient.OnGameDisconnected += (client, reason) => { };

            mNetClient.OnZoneChanged += OnZoneChanged;
            mNetClient.OnZoneActorEntered += OnZoneActorEntered;
        }

        protected virtual void OnZoneActorEntered(LayerPlayer obj)
        {
        }

        protected virtual void OnZoneChanged(RPGBattleClient obj)
        {
        }

        protected virtual BattleDecorator CreateBattleDecorator(RPGBattleClient battle)
        {
            return new BattleDecorator(battle);
        }

        protected virtual void OnRequestStart(string route, ISerializable request, object option)
        {
        }

        protected virtual void OnRequestEnd(string route, PomeloException exp, ISerializable response, object option)
        {
        }


        public void RequestEnterServer(ServerInfo server, string account, string token, Action<List<RoleIDSnap>, string> callback)
        {
            Account = account;
            Token = token;
            mNetClient.Disconnect();
            IPUtil.TryParseHostPort(server.address, out var ip, out var port);
            mNetClient.Gate_Connect(ip, port, account, token, server.id, (rsp) =>
            {
                if (rsp.IsSuccess)
                {
                    mNetClient.Connect_Connect((rsp2) =>
                    {
                        if (rsp2.IsSuccess)
                        {
                            callback?.Invoke(rsp.s2c_roleIDList, null);
                        }
                        else
                        {
                            mNetClient.Disconnect();
                            callback?.Invoke(null, MessageCodeManager.Instance.GetCodeMessage(rsp2));
                        }
                    });
                }
                else
                {
                    mNetClient.Disconnect();
                    callback?.Invoke(null, MessageCodeManager.Instance.GetCodeMessage(rsp));
                }
            });
        }

        private string GetError(PomeloException exp, Response rsp)
        {
            var msg = exp?.Message;
            if (msg == null && !rsp.IsSuccess)
            {
                msg = MessageCodeManager.Instance.GetCodeMessage(rsp);
            }

            return msg;
        }

        private Exception GetException(PomeloException exp, Response rsp)
        {
            if (exp != null)
            {
                return exp;
            }

            if (!rsp.IsSuccess)
            {
                return new Exception(GetError(null, rsp));
            }

            return null;
        }

        public void RequestEnterGame(string roleId, Action<ClientRoleData, string> callback)
        {
            var request = new ClientEnterGameRequest {c2s_roleUUID = roleId};
            GameClient.Request<ClientEnterGameResponse>(request, (exp, rsp) => { callback.Invoke(rsp?.s2c_role, GetError(exp, rsp)); });
        }

        public async Task<ClientRoleData> RequestEnterGame(string roleId)
        {
            var request = new ClientEnterGameRequest {c2s_roleUUID = roleId};
            var rsp = await RequestGame<ClientEnterGameResponse>(request);
            return rsp.s2c_role;
        }


        public void RequestBinary(BinaryMessage request, Action<PomeloException, BinaryMessage> action)
        {
            GameClient.RequestBinary(request, action);
        }

        protected override void Disposing()
        {
            mNetClient.Dispose();
        }

        public void Update(int ms)
        {
            mNetClient.Update(ms);
            while (mNetStateQueue.Count > 0)
            {
                var nv = mNetStateQueue.Dequeue();
                mOnNetEvent?.Invoke(nv);
            }
        }

        #region async await

        public Task<BinaryMessage> RequestBinary(BinaryMessage request)
        {
            var ts = new TaskCompletionSource<BinaryMessage>();
            GameClient.RequestBinary(request, (exp, rsp) =>
            {
                if (exp != null)
                {
                    ts.SetException(exp);
                }
                else
                {
                    ts.SetResult(rsp);
                }
            });
            return ts.Task;
        }

        public Task<T> RequestGame<T>(Request request, bool allowErrorCode = false) where T : Response
        {
            var tc = new TaskCompletionSource<T>();
            GameClient.Request<T>(request, (exp, rsp) =>
            {
                var taskExp = !allowErrorCode ? GetException(exp, rsp) : exp;
                if (taskExp != null)
                {
                    tc.SetException(taskExp);
                }
                else
                {
                    tc.SetResult(rsp);
                }
            });
            return tc.Task;
        }

        public Task<List<RoleIDSnap>> RequestEnterServer(ServerInfo server, string account, string token)
        {
            var tc = new TaskCompletionSource<List<RoleIDSnap>>();
            RequestEnterServer(server, account, token, (list, error) =>
            {
                if (error != null)
                { 
                    tc.SetException(new Exception(error));
                }
                else
                {
                    tc.SetResult(list);
                }
            });
            return tc.Task;
        }

        #endregion
    }
}