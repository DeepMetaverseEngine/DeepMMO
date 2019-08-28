using DeepCore;
using DeepCore.Concurrent;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Net;
using DeepCore.Reflection;
using DeepMMO.Protocol;
using DeepMMO.Protocol.Client;
using System;
using System.Collections.Generic;

namespace DeepMMO.Client.BotTest.Runner
{
    public partial class BotRunner : Disposable
    {
        protected readonly ListLogger log;
        protected readonly RPGClient client;
        protected readonly AddBotConfig add;
        protected readonly BotConfig cfg;
        protected readonly string account;
        protected readonly AtomicReference<string> status = new AtomicReference<string>("");
        protected readonly AtomicReference<string> net_status = new AtomicReference<string>("");
        protected readonly Random random = new Random();

        public RPGClient Client { get { return client; } }
        public ListLogger LogEvents { get { return log; } }
        public string AccountName { get { return account; } }
        public string RoleName { get { return client.RoleName; } }
        public string RoleUUID { get { return client.RoleUUID; } }
        public string Status { get { return status.Value; } }
        public string NetState
        {
            get
            {
                if (client.GameClient.IsConnected)
                {
                    return string.Format("{0} Ping={1}/{2}",
                        client.GameClient.IsConnected ? "已连接" : (client.IsGameDisconnected ? "已掉线" : "未连接"),
                        client.CurrentPing,
                        client.CurrentBattlePing);
                }
                else if (client.GateClient.IsConnected)
                {
                    return net_status.Value;
                }
                else
                {
                    return string.Empty;
                }
            }
        }
        public string[] Columns
        {
            get
            {
                return new string[] { account, $"{RoleName}", $"{client.SceneData}", Status, NetState };
            }
        }


        public BotRunner(RPGClient c, BotConfig cfg, AddBotConfig add, string account)
        {
            this.log = new ListLogger("bot[" + account + "]-");
            this.cfg = cfg;
            this.add = add;
            this.account = account;
            this.client = c;
            this.client.OnGateEntered += on_gate_connect_callback;
            this.client.GateClient.Listen<ClientEnterGateInQueueNotify>(on_gate_in_queue);
            foreach (var type in BotFactory.Instance.GetModuleTypes())
            {
                var m = ReflectionUtil.CreateInterface<BotModule>(type, this);
                modules.Add(m.GetType(), m);
            }
            client.AddTimePeriodicMS(10000, (e) =>
            {
                if (client.CurrentZoneLayer != null && client.GameClient != null && client.GameClient.IsHandshake)
                {
                    client.GameClient.Request<ClientPong>(new ClientPing(), (s, a) => { });
                }
            });
        }
        protected override void Disposing()
        {
            lock (this)
            {
                client.Disconnect();
                client.Dispose();
                modules.Clear();
            }
        }
        public void Start()
        {
            this.net_status.Value = "";
            lock (this)
            {
                if (client.IsDisposed) return;
                var server = RPGClientTemplateManager.Instance.GetServer(add.serverID);
                if (server != null && IPUtil.TryParseHostPort(server.address, out var host, out var port))
                {
                    client.Gate_Connect(host, port, account, add.password, add.serverID, (rsp) =>
                    {
                        if (rsp.s2c_code == ClientEnterGateResponse.CODE_OK_IN_QUEUE)
                        {
                            this.net_status.Value = "排队中:";
                        }
                    });
                }
            }
        }
        public void Stop()
        {
            client.Disconnect();
            this.net_status.Value = string.Empty;
            this.status.Value = string.Empty;
        }
        public void Update(int intervalMS)
        {
            client.Update(intervalMS);
            foreach (var m in modules.Values)
            {
                m.InternalUpdate(intervalMS);
            }
        }
        public void Reconnect()
        {
            client.Connect_Connect(on_game_connect_callback);
        }
        public void Disconnect()
        {
            client.Disconnect();
        }

        public override string ToString()
        {
            return client.AccountName;
        }

        public void PopLogs(List<string> list)
        {
            log.PopLogs(list);
        }
        //--------------------------------------------------------------------------------------------------------


        protected virtual void on_error(Exception err)
        {
            log.Error(err.Message, err);
        }
        protected virtual void log_response(Response rsp, Exception err = null, string msg = null)
        {
            var suffix = (msg != null ? " - " + msg : string.Empty);
            var prefix = (rsp != null ? string.Format("{0}:{1}:{2} - ", rsp.GetType().Name, rsp.s2c_code, rsp.s2c_msg) : string.Format("{0}", rsp));
            if (err != null)
                log.Error(string.Format("{0}{1}{2}", prefix, err.Message, suffix), err);
            else if (rsp != null)
                log.Debug(string.Format("{0}{1}", prefix, suffix));
        }

        //--------------------------------------------------------------------------------------------------------

    }

}
