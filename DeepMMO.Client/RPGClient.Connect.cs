using DeepCore.FuckPomeloClient;
using DeepCore.IO;
using DeepMMO.Data;
using DeepMMO.Protocol.Client;
using System;

namespace DeepMMO.Client
{
    public partial class RPGClient
    {
        public ClientEnterServerRequest last_EnterServerRequest { get; private set; }
        public ClientEnterServerResponse last_EnterServerResponse { get; private set; }
        public ClientEnterGameResponse last_EnterGameResponse { get; private set; }

        public ClientRoleData LastRoleData
        {
            get { return (last_EnterGameResponse != null) ? last_EnterGameResponse.s2c_role : null; }
        }
        public string RoleName
        {
            get { var data = LastRoleData; return data == null ? "" : data.name; }
        }
        public string RoleUUID
        {
            get { var data = LastRoleData; return data == null ? "" : data.uuid; }
        }
        public string SceneUUID
        {
            get { var battle = current_battle; return battle == null ? "" : battle.ZoneUUID; }
        }
        public DeepCore.GameData.Zone.ZoneEditor.SceneData SceneData
        {
            get { var battle = current_battle; return battle == null ? null : battle.Layer.Data; }
        }
        protected virtual void Connect_Init()
        {
            this.game_client.OnConnected += Connect_OnConnected;
            this.game_client.OnDisconnected += Connect_OnDisconnected;
            this.game_client.OnRequestEnd += Game_client_OnRequestEnd;
        }

        public virtual void Connect_Connect(Action<ClientEnterServerResponse> callback)
        {
            this.last_EnterServerRequest = new ClientEnterServerRequest()
            {
                c2s_account = last_EnterGateResponse.s2c_accountUUID,
                c2s_gate_token = last_EnterGateResponse.s2c_connectToken,
                c2s_login_token = last_EnterGateResponse.s2c_lastLoginToken,
                c2s_session_token = last_EnterServerResponse != null ? last_EnterServerResponse.s2c_session_token : null,
                c2s_time = DateTime.Now,
                c2s_clientInfo = clientInfo,
            };
            if (this.last_EnterServerRequest.c2s_clientInfo != null)
            {
                this.last_EnterServerRequest.c2s_clientInfo.platformAcount = last_EnterGateResponse.s2c_platformAccount;
            }
            if (this.game_client.IsConnected)
            {
                this.game_client.Disconnect();
            }
            var host = last_EnterGateResponse.s2c_connectHost;
            var port = last_EnterGateResponse.s2c_connectPort;
            this.game_client.Connect(host, port, this.ConnectTimeOut, last_EnterServerRequest, response =>
            {
                //   game_client.Request<ClientPong>(new ClientPing(), (s, a) => { });
                callback(response as ClientEnterServerResponse);
            });
        }
        private void Connect_OnConnected(object obj, ISerializable token)
        {
            this.last_EnterServerResponse = token as ClientEnterServerResponse;
            OnGameClientConnected(obj, token);
            if (event_OnGameConnected != null) event_OnGameConnected(game_client, last_EnterServerResponse);
        }
        private void Connect_OnDisconnected(CloseReason reason, string err)
        {
            OnGameClientDisconnected(reason, err);
            if (event_OnGameDisconnected != null) event_OnGameDisconnected(game_client, reason);
        }
        private void Game_client_OnRequestEnd(string route, PomeloException error, ISerializable response, object option)
        {
            if (response is ClientEnterGameResponse && ((ClientEnterGameResponse)response).IsSuccess)
            {
                last_EnterGameResponse = response as ClientEnterGameResponse;
                OnGameClientEntered(last_EnterGameResponse);
                if (event_OnGameEntered != null) event_OnGameEntered(game_client, last_EnterGameResponse);
            }
        }


        protected virtual void Connect_Disposing()
        {
            game_client.Dispose();
            event_OnGameConnected = null;
            event_OnGameDisconnected = null;
        }

        private Action<PomeloClient, ClientEnterServerResponse> event_OnGameConnected;
        private Action<PomeloClient, ClientEnterGameResponse> event_OnGameEntered;
        private Action<PomeloClient, CloseReason> event_OnGameDisconnected;
        public event Action<PomeloClient, ClientEnterServerResponse> OnGameConnected { add { event_OnGameConnected += value; } remove { event_OnGameConnected -= value; } }
        public event Action<PomeloClient, ClientEnterGameResponse> OnGameEntered { add { event_OnGameEntered += value; } remove { event_OnGameEntered -= value; } }
        public event Action<PomeloClient, CloseReason> OnGameDisconnected { add { event_OnGameDisconnected += value; } remove { event_OnGameDisconnected -= value; } }

    }
}
