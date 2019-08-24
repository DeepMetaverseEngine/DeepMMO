using DeepCore;
using DeepCore.FuckPomeloClient;
using DeepCore.Log;
using DeepCore.Reflection;
using System;

namespace DeepMMO.Client.BotTest.Runner
{
    public partial class BotRunner
    {
        private HashMap<Type, BotModule> modules = new HashMap<Type, BotModule>();

        internal void SetStatus(string text)
        {
            this.status.Value = text;
        }
    }

    //--------------------------------------------------------------------------------------------------------
    public abstract class BotModuleConfig
    {
        public override string ToString()
        {
            var attr = PropertyUtil.GetAttribute<DescAttribute>(GetType());
            return attr != null ? attr.Desc : base.ToString();
        }
    }
    public abstract class BotModule
    {
        //------------------------------------------------------------------------
        private static HashMap<Type, bool> s_SubModules = new HashMap<Type, bool>();
        internal static void InitRunnerModules()
        {
            foreach (var mt in BotFactory.Instance.GetModuleTypes())
            {
                s_SubModules.Add(mt, true);
            }
        }
        public static void SetModuleEnable(Type type, bool value)
        {
            s_SubModules[type] = value;
        }
        public static bool GetModuleEnable(Type type)
        {
            bool enable = false;
            if (s_SubModules.TryGetValue(type, out enable))
            {
                return enable;
            }
            return false;
        }
        //------------------------------------------------------------------------
        private bool m_Enable;
        protected RPGClient Client { get; private set; }
        protected PomeloClient GateClient { get { return Client.GateClient; } }
        protected PomeloClient GameClient { get { return Client.GameClient; } }
        protected BotRunner Runner { get; private set; }
        protected Logger log { get; private set; }
        public bool IsEnable { get { return m_Enable; } }

        public BotModule(BotRunner r)
        {
            this.Runner = r;
            this.Client = r.Client;
            this.log = r.LogEvents;
            this.m_Enable = GetModuleEnable(GetType());
        }
        internal void InternalUpdate(int intervalMS)
        {
            bool enable = GetModuleEnable(GetType());
            if (m_Enable != enable)
            {
                m_Enable = enable;
                this.OnEnableChanged(enable);
            }
            this.OnUpdate(intervalMS);
        }
        
        protected virtual void OnUpdate(int intervalMS) { }
        protected virtual void OnEnableChanged(bool enable) { }


        protected void SetStatus(string text)
        {
            this.Runner.SetStatus(text);
        }
    }
}
