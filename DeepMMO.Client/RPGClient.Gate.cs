using DeepCore;
using DeepCore.FuckPomeloClient;
using DeepCore.IO;
using DeepMMO.Protocol.Client;
using System;

namespace DeepMMO.Client
{
    public partial class RPGClient
    {
        public ClientEnterGateResponse last_EnterGateResponse { get; private set; }
        public ClientEnterGateRequest last_EnterGateRequest { get; private set; }

        public string AccountName
        {
            get { return last_EnterGateRequest != null ? last_EnterGateRequest.c2s_account : ""; }
        }
        protected virtual void Gate_Init()
        {
            gate_client.Listen<ClientEnterGateInQueueNotify>(Gate_OnClientEnterGateInQueueNotify);
        }
        protected virtual void Gate_OnClientEnterGateInQueueNotify(ClientEnterGateInQueueNotify notify)
        {
            event_OnGateQueueUpdated?.Invoke(notify);
            if (notify.IsEnetered)
            {
                this.last_EnterGateResponse.s2c_code = ClientEnterGateResponse.CODE_OK;
                this.last_EnterGateResponse.s2c_connectHost = notify.s2c_connectHost;
                this.last_EnterGateResponse.s2c_connectPort = notify.s2c_connectPort;
                this.last_EnterGateResponse.s2c_connectToken = notify.s2c_connectToken;
                this.last_EnterGateResponse.s2c_lastLoginToken = notify.s2c_lastLoginToken;
                this.event_OnGateEntered?.Invoke(last_EnterGateResponse);
                if (this.gate_client.IsConnected) gate_client.Disconnect();
            }
        }
        public virtual void Gate_Connect(string host, int port, string account, string token, string serverID, Action<ClientEnterGateResponse> callback)
        {
            this.last_EnterGateRequest = new ClientEnterGateRequest()
            {
                c2s_account = account,
                c2s_token = CUtils.ToBase64(token),
                c2s_clientInfo = clientInfo,
                c2s_serverID = serverID,
            };
            if (this.gate_client.IsConnected)
            {
                this.gate_client.Disconnect();
            }
            this.gate_client.Connect(host, port, this.ConnectTimeOut, last_EnterGateRequest, (response) =>
            {
                var rsp = response as ClientEnterGateResponse;
                this.last_EnterGateResponse = rsp;
                if (rsp.s2c_code == ClientEnterGateResponse.CODE_OK_IN_QUEUE)
                {
                    callback(rsp);
                }
                else
                {
                    callback(rsp);
                    event_OnGateEntered?.Invoke(rsp);
                    if (this.gate_client.IsConnected) gate_client.Disconnect();
                }
            });
        }
        protected virtual void Gate_Disposing()
        {
            gate_client.Dispose();
        }

        private Action<ClientEnterGateResponse> event_OnGateEntered;
        private Action<ClientEnterGateInQueueNotify> event_OnGateQueueUpdated;
        public event Action<ClientEnterGateResponse> OnGateEntered { add { event_OnGateEntered += value; } remove { event_OnGateEntered -= value; } }
        public event Action<ClientEnterGateInQueueNotify> OnGateQueueUpdated { add { event_OnGateQueueUpdated += value; } remove { event_OnGateQueueUpdated -= value; } }
    }
}
