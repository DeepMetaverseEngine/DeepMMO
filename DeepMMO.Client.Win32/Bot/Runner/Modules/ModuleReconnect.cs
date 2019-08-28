using CommonAI.ZoneClient;
using CommonLang.Reflection;

namespace ZeusBotTest.Runner
{
    public class ModuleReconnect : BotRunner.RunnerModule
    {
        public ModuleReconnect(BotRunner r) : base(r)
        {
        }

        protected internal override void OnBattleActorReady(ZoneLayer layer, ZoneActor actor)
        {
            layer.AddTimePeriodicMS(bot.Random.Next(Config.IntervalMS_Min, Config.IntervalMS_Max), (t) =>
            {
                if (Enable)
                {
                    runner.reconnect();
                }
            });
        }

        [Desc("重连配置")]
        [Expandable]
        public class Config
        {
            [Desc("重连间隔")]
            public static int IntervalMS_Min = 10000;
            [Desc("重连间隔")]
            public static int IntervalMS_Max = 20000;


            public override string ToString()
            {
                return "重连配置";
            }
        }
    }
}
