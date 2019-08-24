using CommonLang.Reflection;
using pomelo.connector;

namespace ZeusBotTest.Runner
{
    public class ModuleSkill : BotRunner.RunnerModule
    {
        public ModuleSkill(BotRunner r) : base(r)
        {
        }
        protected internal override void OnGateBindPlayer(BindPlayerResponse e)
        {
            base.OnGateBindPlayer(e);
        }
        protected internal override void OnBattleActorReady(CommonAI.ZoneClient.ZoneLayer layer, CommonAI.ZoneClient.ZoneActor actor)
        {
            layer.AddTimeDelayMS(Config.CheckIntervalMS, (t) =>
            {
                if (Enable)
                {
                    
                }
            });
            layer.AddTimePeriodicMS(Config.CheckIntervalMS, (t) =>
            {
                if (Enable)
                {
                    try_get_skill_list();
                }
            });
        }
        private void try_get_skill_list()
        {
            client.GameSocket.skillHandler.getAllSkillRequest(
                    (err, rsp) =>
                    { });
        }
        [Desc("技能配置")]
        [Expandable]
        public class Config
        {
            [Desc("技能检测间隔")]
            public static int CheckIntervalMS = 5000;

            public override string ToString()
            {
                return "技能配置";
            }
        }
    }
}
