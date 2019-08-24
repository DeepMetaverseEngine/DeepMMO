using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pomelo.connector;
using CommonLang.Reflection;
using pomelo.area;

namespace ZeusBotTest.Runner
{
    public class ModuleRank : BotRunner.RunnerModule
    {
        List<int> Kinds = new List<int>();

        public ModuleRank(BotRunner r) : base(r)
        {
            Kinds = new List<int>();

            Kinds.Add(101);
            Kinds.Add(102);
            Kinds.Add(103);
            Kinds.Add(104);
            Kinds.Add(105);
            Kinds.Add(106);
            Kinds.Add(200);
            Kinds.Add(300);
            Kinds.Add(400);
            Kinds.Add(500);
            Kinds.Add(600);
            Kinds.Add(700);
            Kinds.Add(10001);
            Kinds.Add(10002);
            Kinds.Add(10003);
            Kinds.Add(10004);
            Kinds.Add(10005);
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
                    try_get_rank_list();
                }
            });
        }
        private void try_get_rank_list()
        {
            int randValue = bot.Random.Next(0, Kinds.Count - 1);

            client.GameSocket.leaderBoardHandler.leaderBoardRequest(Kinds[randValue],
                    (err, rsp) =>
                    { });
        }
        [Desc("排行榜配置")]
        [Expandable]
        public class Config
        {
            [Desc("排行榜检测间隔")]
            public static int CheckIntervalMS = 5000;

            public override string ToString()
            {
                return "排行榜配置";
            }
        }
    }
}
