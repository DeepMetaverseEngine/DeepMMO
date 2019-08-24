using System.Collections.Generic;
using DeepCore.FuckPomeloClient;
using DeepCore.GameEvent;
using DeepCore.GameEvent.Message;
using DeepCore.Log;
using DeepMMO.Protocol.Client;

namespace DeepMMO.Client
{
    public class EventModule : RPGClientModule
    {
        private readonly PushHandler mPushHandler;



        public const string ClientManagerName = "Client";

        protected Logger Logger;

        public EventManager GameEventManager { get; private set; }
        private readonly Queue<ClientGameEventNotify> mPreQueue = new Queue<ClientGameEventNotify>();

        private EventManager mCreateedManager;
        private static EventManager CreateTempEventManager(string name, string uuid)
        {
            return new EventManager(name, uuid) {RemoteAction = EventManager.RemoteActionType.Success};
        }

        public EventModule(RPGClient client) : base(client)
        {
            mPushHandler = game_client.Listen<ClientGameEventNotify>(OnRemoteGameEventNotify);
            Logger = LoggerFactory.GetLogger(GetType().Name);
        }

        ~EventModule()
        {
            Logger.Debug("~EventModule");
        }
        
        protected override void Disposing()
        {
            mPushHandler?.Clear();
            if (mCreateedManager != null)
            {
                mCreateedManager?.Dispose();
                mCreateedManager = null;
                GameEventManager = null;
            }
            EventManager.MessageBroker.Unsubscribe(MessageBroker.UnknownChannel, UnknownChannel);
        }

        private bool mStarted ;
        public override void OnStart()
        {
            mStarted = true;
            if (!EventManagerFactory.Instance.ContainsName(ClientManagerName))
            {
                EventManagerFactory.Instance.RegisterName(ClientManagerName, CreateTempEventManager);
            }

            GameEventManager = EventManagerFactory.Instance.GetEventManager(EventManager.GetAddress("Client", null));
            if (GameEventManager == null)
            {
                GameEventManager = EventManagerFactory.Instance.CreateEventManager(ClientManagerName, null);
                mCreateedManager = GameEventManager;
                GameEventManager.Start();
            }


            if (GameEventManager != null)
            {
                EventManager.MessageBroker.Subscribe(MessageBroker.UnknownChannel, UnknownChannel);
                while (mPreQueue.Count > 0)
                {
                    var ntf = mPreQueue.Dequeue();
                    ProcessEventNotify(ntf);
                }
            }
        }
        
        public override void OnStop()
        {
            mStarted = false;
        }


        private void ProcessEventNotify(ClientGameEventNotify ntf)
        {
            var msg = EventMessage.FromBytes(ntf.EventMessageData);
            GameEventManager?.OnReceiveMessage(msg);
        }
        
        
        private void UnknownChannel(IMessagePayload messagePayload)
        {
            if (!mStarted)
            {
                Logger.Error("event module not started");
                return;
            }
            var notifyMsg = messagePayload.WhatObject as EventMessage;
            var mgr = messagePayload.Who as EventManager;
            if (mgr == null || notifyMsg == null)
            {
                Logger.Error("mgr == null || notifyMsg == nul");
            }
            else
            {
                if (!notifyMsg.From.StartsWith(ClientManagerName))
                {
                    return;
                }
                var ntf = new ClientGameEventNotify
                {
                    From = notifyMsg.From,
                    To = notifyMsg.To,
                    EventMessageData = notifyMsg.ToBytes()
                };
                client.GameClient.Notify(ntf);
            }
        }

 
        public override void Update(int intervalMS)
        {
            base.Update(intervalMS);
            mCreateedManager?.Update();
        }

        private void OnRemoteGameEventNotify(ClientGameEventNotify ntf)
        {
            if (GameEventManager == null)
            {
                mPreQueue.Enqueue(ntf);
            }
            else
            {
                ProcessEventNotify(ntf);
            }
        }
    }
}