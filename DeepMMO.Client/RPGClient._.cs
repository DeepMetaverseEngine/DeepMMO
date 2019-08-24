using DeepCore;
using DeepCore.FuckPomeloClient;
using DeepCore.IO;
using DeepCore.Log;
using DeepMMO.Data;
using DeepMMO.Protocol;
using DeepMMO.Protocol.Client;
using System;
using System.Collections.Generic;
using DeepCore.GameData.Helper;
using DeepCore.GameEvent;

namespace DeepMMO.Client
{
    public partial class RPGClient : Disposable
    {
        protected readonly Logger log;
        protected readonly IExternalizableFactory codec;
        protected readonly ClientInfo clientInfo;

        protected readonly PomeloClient gate_client;
        protected readonly PomeloClient game_client;

        public IExternalizableFactory NetCodec { get { return codec; } }
        public PomeloClient GateClient { get { return gate_client; } }
        public PomeloClient GameClient { get { return game_client; } }
        public int CurrentPing { get; private set; }
        public bool IsAutoUpdateBattle { get; set; }
        public int ConnectTimeOut { get; set; }

        /// <summary>
        /// 曾经链接到游戏后掉线
        /// </summary>
        public bool IsGameDisconnected
        {
            get { return (this.GameClient.IsConnected == false && this.last_EnterGameResponse != null); }
        }

        public RPGClient(IExternalizableFactory codec, ClientInfo client)
        {
            this.CurrentPing = 0;
            this.IsAutoUpdateBattle = false;
            this.ConnectTimeOut = 5000;

            this.log = LoggerFactory.GetLogger(GetType().Name);
            this.codec = codec;
            this.clientInfo = client;
            this.gate_client = new PomeloClient(codec);
            this.game_client = new PomeloClient(codec);

            this.game_client.NetHandleResponseImmediately += Game_client_AsyncHandleResponseImmediately;
            this.game_client.NetHandleBodyImmediately += Game_client_AsyncHandleBodyImmediately;
            this.game_client.NetError += Game_client_AsyncError;
            this.gate_client.NetError += Gate_client_AsyncError;
            this.ping_codec = codec.GetCodec(typeof(ClientPing));
            this.pong_codec = codec.GetCodec(typeof(ClientPong));
            this.Gate_Init();
            this.Connect_Init();
            this.Area_Init();
            this.OnCreateModules();
        }

        protected virtual void OnCreateModules()
        {
            AddModule(new EventModule(this));
        }

        public void Disconnect()
        {
            this.gate_client.Disconnect();
            this.game_client.Disconnect();
        }
        //----------------------------------------------------------------------------------------------------------
        private readonly TypeCodec pong_codec;
        private readonly TypeCodec ping_codec;
        private void Game_client_AsyncHandleResponseImmediately(DeepCore.FuckPomelo.IRecvMessage protocol)
        {
            if (protocol.MsgRoute == pong_codec.MessageID)
            {
                var pong = protocol.ReadBody() as ClientPong;
                this.CurrentPing = (int)(DateTime.Now - pong.time).TotalMilliseconds;
            }
        }
        private void Game_client_AsyncHandleBodyImmediately(object message)
        {
            if (message is Response response)
            {
                response.EndRead();
            }
        }

        //----------------------------------------------------------------------------------------------------------
        #region Modules

        private HashSet<RPGClientModule> mModules = new HashSet<RPGClientModule>();

        public void AddModule(RPGClientModule module)
        {
            mModules.Add(module);
        }

        public void RemoveModule(RPGClientModule module)
        {
            mModules.Remove(module);
            module.Dispose();
        }

        public T GetModel<T>(Predicate<T> predicate = null) where T : RPGClientModule
        {
            foreach (var m in mModules)
            {
                if (m is T model && (predicate == null || predicate.Invoke(model)))
                {
                    return model;
                }
            }

            return null;
        }

        protected virtual void OnGameClientConnected(object arg1, ISerializable arg2)
        {
        }

        protected virtual void OnGameClientEntered(ClientEnterGameResponse enter)
        {
            foreach (var module in mModules)
            {
                module.OnStart();
            }
        }
        protected virtual void OnGameClientDisconnected(object arg1, string arg2)
        {
            foreach (var module in mModules)
            {
                module.OnStop();
            }
        }
        protected override void Disposing()
        {
            this.gate_client.Disconnect();
            this.game_client.Disconnect();
            this.tasks.Clear();
            this.timer_tasks.Dispose();
            this.Area_Disposing();
            this.Connect_Disposing();
            this.Gate_Disposing();
            event_OnError = null;
            foreach (IDisposable module in mModules)
            {
                module.Dispose();
            }
            mModules.Clear();
        }

        #endregion
        //----------------------------------------------------------------------------------------------------------
        #region Update

        private readonly SyncMessageQueue<Action> tasks = new SyncMessageQueue<Action>(new SingleThreadCollectionPool());
        private readonly TimeTaskQueue timer_tasks = new TimeTaskQueue(new SingleThreadCollectionPool());
        public virtual void QueueTask(Action action)
        {
            tasks.Enqueue(action);
        }
        /// <summary>
        /// 【线程安全】增加时间任务
        /// </summary>
        /// <param name="intervalMS"></param>
        /// <param name="delayMS"></param>
        /// <param name="repeat"></param>
        /// <param name="handler"></param>
        public TimeTaskMS AddTimeTask(int intervalMS, int delayMS, int repeat, TickHandler handler)
        {
            return timer_tasks.AddTimeTask(intervalMS, delayMS, repeat, handler);
        }
        /// <summary>
        /// 【线程安全】增加延时回调方法
        /// </summary>
        /// <param name="delayMS"></param>
        /// <param name="handler"></param>
        public TimeTaskMS AddTimeDelayMS(int delayMS, TickHandler handler)
        {
            return timer_tasks.AddTimeDelayMS(delayMS, handler);
        }
        /// <summary>
        /// 【线程安全】增加定时回调方法
        /// </summary>
        /// <param name="intervalMS"></param>
        /// <param name="handler"></param>
        public TimeTaskMS AddTimePeriodicMS(int intervalMS, TickHandler handler)
        {
            return timer_tasks.AddTimePeriodicMS(intervalMS, handler);
        }


        public virtual void Update(int intervalMS)
        {
            if (IsAutoUpdateBattle && current_battle != null)
            {
                current_battle.BeginUpdate(intervalMS);
            }
            tasks.ProcessMessages((act) =>
            {
                try { act.Invoke(); } catch (Exception err) { DoError(err); }
            });
            timer_tasks.Update(intervalMS);
            if (gate_client != null)
            {
                gate_client.Update();
            }
            if (game_client != null)
            {
                game_client.Update();
            }
            if (IsAutoUpdateBattle && current_battle != null)
            {
                current_battle.Update();
            }
            foreach (var module in mModules)
            {
                module.Update(intervalMS);
            }
        }

        private void Game_client_AsyncError(Exception obj)
        {
            QueueTask(() => { DoError(obj); });
        }
        private void Gate_client_AsyncError(Exception obj)
        {
            QueueTask(() => { DoError(obj); });
        }
        protected virtual void DoError(Exception err)
        {
            log.Error(err.Message, err);
            if (event_OnError != null) event_OnError(err);
        }
        #endregion

        //----------------------------------------------------------------------------------------------------------
        #region Events

        private Action<Exception> event_OnError;
        public event Action<Exception> OnError { add { event_OnError += value; } remove { event_OnError -= value; } }

        #endregion
        //----------------------------------------------------------------------------------------------------------
    }
}
