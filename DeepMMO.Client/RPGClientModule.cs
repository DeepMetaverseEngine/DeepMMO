using DeepCore.FuckPomeloClient;
using System;
using DeepCore;

namespace DeepMMO.Client
{
    public abstract class RPGClientModule : Disposable
    {
        public RPGClient client { get; private set; }
        public PomeloClient game_client { get; private set; }

        protected RPGClientModule(RPGClient client)
        {
            this.client = client;
            this.game_client = client.GameClient;
        }

        public abstract void OnStart();
        public abstract void OnStop();

        public virtual void Update(int intervalMS) { }
    }
}
