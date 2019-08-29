using DeepCore.Game3D.Slave;
using DeepCore.GameData;
using DeepCore.Reflection;
using DeepMMO.Client.BotTest.Runner;
using DeepMMO.Data;
using System;
using System.Collections.Generic;

namespace DeepMMO.Client.BotTest
{
    public abstract class BotFactory
    {
        public static BotFactory Instance { get; private set; }
        public BotFactory()
        {
            Instance = this;
            BotModule.InitRunnerModules();
        }

        public abstract ZoneSlaveFactory ZoneFactory { get; }
        public abstract ZoneDataFactory DataFactory { get; }
        public abstract RPGClientTemplateManager ClientTemplates { get; }
        public abstract RPGClientBattleManager BattleManager { get; }
        public abstract void Init(BotConfig config);

        public virtual AddBotConfig CreateAddBotConfig()
        {
            var ret = new AddBotConfig();
            return ret;
        }
        public virtual BotConfig CreateBotConfig()
        {
            return new BotConfig();
        }
        public virtual BotRunner CreateBotRunner(BotConfig cfg, AddBotConfig add, string account)
        {
            var c = new RPGClient(this.DataFactory.MessageCodec, new ClientInfo() { sdkName = "OneGame" });
            return new BotRunner(c, cfg, add, account);
        }
        public virtual List<Type> GetModuleTypes()
        {
            var ret = new List<Type>();
            foreach (var mt in ReflectionUtil.GetNoneVirtualSubTypes(typeof(BotModule), true))
            {
                ret.Add(mt);
            }
            return ret;
        }
    }
}
