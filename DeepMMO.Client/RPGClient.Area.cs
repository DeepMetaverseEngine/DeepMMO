using DeepMMO.Client.Battle;
using DeepMMO.Protocol.Client;
using System;
using DeepCore.Game3D.Slave.Layer;

namespace DeepMMO.Client
{
    public partial class RPGClient
    {
        protected RPGBattleClient current_battle;
        protected RPGBattleClient next_battle;

        public RPGBattleClient CurrentBattle
        {
            get { return current_battle; }
        }

        public RPGBattleClient NextBattle
        {
            get { return next_battle; }
        }
        public int CurrentBattlePing
        {
            get { return current_battle != null ? current_battle.CurrentPing : 0; }
        }
        public LayerZone CurrentZoneLayer
        {
            get { return current_battle != null ? current_battle.Layer : null; }
        }
        public LayerPlayer CurrentZoneActor
        {
            get { return current_battle != null ? current_battle.Actor : null; }
        }

        public bool IsDelayReleaseBattleClient { get; set; }
        protected virtual void Area_Init()
        {
            this.game_client.Listen<ClientEnterZoneNotify>(Area_OnClientEnterZoneNotify);
            this.game_client.Listen<ClientLeaveZoneNotify>(Area_OnClientLeaveZoneNotify);
            this.game_client.Listen<ClientBattleEvent>(Area_OnClientBattleEvent);
        }

        protected virtual void Area_OnClientBattleEvent(ClientBattleEvent notify)
        {
            if (next_battle != null)
            {
                next_battle.OnReceived(notify);
            }
            else if (current_battle != null)
            {
                current_battle.OnReceived(notify);
            }
            else
            {
                throw new Exception("Battle Not Init !!!");
            }
        }
        protected virtual void Area_OnClientEnterZoneNotify(ClientEnterZoneNotify notify)
        {
            if (!IsDelayReleaseBattleClient && current_battle != null)
            {
                current_battle.Dispose();
                current_battle = null;
            }
            log.Info("ClientEnterZoneNotify : " + notify);
            var battle = CreateBattle(notify);
            battle.Layer.ActorAdded += Layer_ActorAdded;
            
            if (current_battle == null || !IsDelayReleaseBattleClient)
            {
                current_battle = battle;
            }
            else
            {
                next_battle = battle;
            }
			if (event_OnZoneChanged != null) event_OnZoneChanged(battle);
        }
        protected virtual void Area_OnClientLeaveZoneNotify(ClientLeaveZoneNotify notify)
        {
            log.Info("ClientLeaveZoneNotify : " + notify);
            if (event_OnZoneLeaved != null) event_OnZoneLeaved(current_battle);
            if (current_battle != null) { current_battle.Dispose(); }
        }
        protected virtual void Layer_ActorAdded(LayerZone layer, LayerPlayer actor)
        {
            if (next_battle != null)
            {
                if (current_battle != null)
                {
                    current_battle.Dispose();
                }
                current_battle = next_battle;
                next_battle = null;
            }
            if (event_OnZoneActorEntered != null)
                event_OnZoneActorEntered(actor);
        }

        protected virtual RPGBattleClient CreateBattle(ClientEnterZoneNotify sd)
        {
            return new RPGBattleClient(this, sd);
        }

        protected virtual void Area_Disposing()
        {
            event_OnZoneChanged = null;
            event_OnZoneLeaved = null;
            event_OnZoneActorEntered = null;
        }

        private Action<RPGBattleClient> event_OnZoneChanged;
        private Action<RPGBattleClient> event_OnZoneLeaved;
        private Action<LayerPlayer> event_OnZoneActorEntered;

        public event Action<RPGBattleClient> OnZoneChanged { add { event_OnZoneChanged += value; } remove { event_OnZoneChanged -= value; } }
        public event Action<RPGBattleClient> OnZoneLeaved { add { event_OnZoneLeaved += value; } remove { event_OnZoneLeaved -= value; } }
        public event Action<LayerPlayer> OnZoneActorEntered { add { event_OnZoneActorEntered += value; } remove { event_OnZoneActorEntered -= value; } }

    }
}
