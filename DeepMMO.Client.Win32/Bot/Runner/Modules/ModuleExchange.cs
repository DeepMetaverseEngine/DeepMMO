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
    public class ModuleExchange : BotRunner.RunnerModule
    {
        List<label> LabelList;

        public ModuleExchange(BotRunner r) : base(r)
        {
        }
        protected internal override void OnGateBindPlayer(BindPlayerResponse e)
        {
            base.OnGateBindPlayer(e);
            try_get_exchange_label();
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
                    try_get_exchange_list();
                }
            });
        }
        private void try_get_exchange_list()
        {
            if (LabelList != null)
            {
                int randValue = bot.Random.Next(0, LabelList.Count - 1);

                client.GameSocket.exchangeHandler.getExchangeListRequest(
                "1000120501", LabelList[randValue].typeId, (err, rsp) =>
                { });
            }
        }
        private void try_get_exchange_label()
        {
            client.GameSocket.exchangeHandler.getExchangeLabelRequest(
            "1000120501", (err, rsp) =>
            {
                LabelList = rsp.s2c_labelList;
            });
        }
        [Desc("兑换配置")]
        [Expandable]
        public class Config
        {
            [Desc("兑换检测间隔")]
            public static int CheckIntervalMS = 5000;

            public override string ToString()
            {
                return "兑换配置";
            }
        }
    }
}
