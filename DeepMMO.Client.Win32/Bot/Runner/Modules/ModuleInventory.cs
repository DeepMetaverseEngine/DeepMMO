using CommonAI.ZoneClient;
using pomelo.area;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZeusBattleClientBot.Bot;
using pomelo.connector;
using CommonLang.Reflection;

namespace ZeusBotTest.Runner
{
    public class ModuleInventory : BotRunner.RunnerModule
    {
        public ModuleInventory(BotRunner r) : base(r)
        {
            client.GameSocket.listen<BagNewItemPush>(on_got_new_item);
            client.GameSocket.listen<BagNewEquipPush>(on_got_new_equip);
        }

        protected internal override void OnGateBindPlayer(BindPlayerResponse e)
        {
            base.OnGateBindPlayer(e);
            runner.do_gm_add_diamond(99999999);
            runner.do_gm_add_gold(99999999);
        }

        protected internal override void OnBattleActorReady(CommonAI.ZoneClient.ZoneLayer layer, CommonAI.ZoneClient.ZoneActor actor)
        {
            if (Enable)
            {
                if (bot.CurrentInventories != null)
                {
                    bot.CurrentInventories.Bag.OpenStorage(5,
                        (e, r) => { runner.log_response("open Bag done : ", e, r); });
                    bot.CurrentInventories.Warehouse.OpenStorage(5,
                        (e, r) => { runner.log_response("open Warehouse done : ", e, r); });
                }
            }
            layer.AddTimePeriodicMS(Config.CheckIntervalMS, (t) =>
            {
                if (Enable)
                {
                    on_do_1s(layer);
                }
            });
        }

        private void on_do_1s(ZoneLayer layer)
        {
            runner.do_start_pick_any_item();
        }

        private void on_got_new_item(BagNewItemPush e)
        {
            foreach (var i in e.s2c_data)
            {
                log.Info("got new item : " + i.name);
            }
        }
        private void on_got_new_equip(BagNewEquipPush e)
        {
            foreach (var i in e.s2c_data)
            {
                log.Info("got new equip : " + i);
                bot.gs_EquipItemByID(i, (err, rsp) =>
                {
                    runner.log_response("equip new item ", err, rsp);
                });
            }
        }

        [Desc("道具配置")]
        [Expandable]
        public class Config
        {
            [Desc("检取间隔")]
            public static int CheckIntervalMS = 1000;

            public override string ToString()
            {
                return "道具配置";
            }
        }
    }
}
