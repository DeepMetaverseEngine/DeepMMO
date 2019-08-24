using DeepCore;
using DeepCore.FuckPomeloClient;
using DeepCore.GameData.Zone;
using DeepCore.Log;
using DeepCore.Protocol;
using DeepMMO.Protocol.Client;
using System;
using DeepCore.Game3D.Slave.Runtime;
using DeepCore.Game3D.ZoneServer;

namespace DeepMMO.Client.Battle
{
    public class RPGBattleClient : AbstractBattle
    {
        protected readonly Logger log;
        protected readonly RPGClient client;
        protected readonly PomeloClient game_client;
        protected readonly ClientEnterZoneNotify enter;
        private long sent_package = 0;
        private long recv_package = 0;
        private TimeInterval<int> ping_interval = new TimeInterval<int>(3000);
        private TimeInterval<int> post_interval;
        private PackAction post_queue = new PackAction();

        public override KickedByServerNotifyB2C KickMessage { get { return null; } }
        public override bool IsNet { get { return true; } }
        public override long RecvPackages { get { return recv_package; } }
        public override long SendPackages { get { return sent_package; } }
        public int PingIntervalMS
        {
            get { return ping_interval.IntervalTimeMS; }
            set { ping_interval = new TimeInterval<int>(Math.Max(1000, value)); }
        }
        public string ZoneUUID { get => enter.s2c_ZoneUUID; }
        public ClientEnterZoneNotify Enter { get { return enter; } }

        public RPGBattleClient(RPGClient client, ClientEnterZoneNotify sd) : base(RPGClientBattleManager.DataRoot)
        {
            this.log = LoggerFactory.GetLogger(GetType().Name);
            this.client = client;
            this.game_client = client.GameClient;
            this.enter = sd;
            this.post_interval = new TimeInterval<int>(sd.s2c_ZoneUpdateIntervalMS);
            this.Layer.ActorSyncMode = SyncMode.MoveByClient_PreSkillByClient;
        }
        public override string ToString()
        {
            return string.Format("{1}({0})", enter.s2c_ZoneUUID, Layer.Data);
        }
        protected internal virtual void OnReceived(ClientBattleEvent notify)
        {
            try
            {
                recv_package++;
                object evt;
                if (RPGClientBattleManager.BattleCodec.doDecode(new ArraySegment<byte>(notify.s2c_battleEvent), out evt))
                {
                    Layer.ProcessMessage(evt as IMessage);
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
        }
        protected virtual void SendToServer(object message)
        {
            try
            {
                ArraySegment<byte> bin;
                if (RPGClientBattleManager.BattleCodec.doEncode(message, out bin))
                {
                    game_client.Notify(new ClientBattleAction() { c2s_battleAction = bin.Array });
                    sent_package++;
                }
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
        }
        public override void SendAction(DeepCore.GameData.Zone.Action action)
        {
            post_queue.actions.Add(action);
        }
        public override void BeginUpdate(int intervalMS)
        {
            base.BeginUpdate(intervalMS);
            if (post_interval.Update(intervalMS))
            {
                if (post_queue.actions.Count > 0)
                {
                    try
                    {
                        //post_queue.object_id = Actor.ObjectID;
                        SendToServer(post_queue);
                    }
                    finally
                    {
                        post_queue.actions.Clear();
                    }
                }
            }
        }
        public override void Update()
        {
            base.Update();
            if (ping_interval.Update(Layer.CurrentIntervalMS))
            {
                SendToServer(new Ping());
            }
        }
    }
}
