using CommonAI.ZoneClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pomelo.connector;
using ZeusBattleClientBot.Bot;
using pomelo.area;
using CommonLang;
using ZeusCommon.ZoneClient;
using CommonAI.Zone;
using CommonLang.Reflection;

namespace ZeusBotTest.Runner
{
    public class ModuleDungeon : BotRunner.RunnerModule
    {
        private FubenListResponse LastFubenList;
        private FubenInfo LastSelectFuben;
        private EnterDungeonRequest LastEnterFuben;

        public ModuleDungeon(BotRunner r) : base(r) { }

        protected internal override void OnGateBindPlayer(BindPlayerResponse e)
        {
            base.OnGateBindPlayer(e);
            select_random_fuben();
        }
        protected override void OnEnableChanged(bool enable)
        {
            base.OnEnableChanged(enable);
            if (Enable)
            {
                try_enter_selected_fuben();
            }
        }
        protected internal override void OnBattleActorReady(CommonAI.ZoneClient.ZoneLayer layer, CommonAI.ZoneClient.ZoneActor actor)
        {
            layer.AddTimeDelayMS(Config.CheckIntervalMS, (t) =>
            {
                if (Enable)
                {
                    try_enter_selected_fuben();
                    get_fuben_list();
                }
            });
            layer.AddTimePeriodicMS(Config.CheckIntervalMS, (t) =>
            {
                if (Enable)
                {
                    on_do_1s(layer as HZZoneLayer);
                }
            });
            layer.MessageReceived += Layer_MessageReceived;
        }

        private void Layer_MessageReceived(ZoneLayer layer, CommonLang.Protocol.IMessage msg)
        {
            if (msg is GameOverEvent)
            {
                on_game_over();
            }
        }

        private void select_random_fuben()
        {
            client.GameSocket.fightLevelHandler.fubenListRequest(
                (int)Config.DType,
                (int)Config.DLevel,
                (err, rsp) =>
            {
                LastFubenList = rsp;
                LastSelectFuben = CUtils.GetRandomInArray<FubenInfo>(rsp.s2c_list, bot.Random);
               // LastSelectFuben = rsp.s2c_list[0];
                runner.log_response("fubenListRequest done", err, rsp);
            });
        }

        private void get_fuben_list()
        {
            client.GameSocket.fightLevelHandler.fubenListRequest(
                (int)Config.DType,
                (int)Config.DLevel,
                (err, rsp) =>
                {});
        }

        private void try_enter_selected_fuben()
        {
            if (LastSelectFuben != null)
            {
                //当前场景不是要进的副本//
                if (LastEnterFuben == null || LastSelectFuben.fubenId != LastEnterFuben.c2s_dungeonId)
                {
                    client.GameSocket.fightLevelHandler.enterDungeonRequest(LastSelectFuben.fubenId, (err, rsp) =>
                    {
                        runner.log_response("enterDungeonRequest done", err, rsp);
                        if (rsp != null)
                        {
                            LastEnterFuben = new EnterDungeonRequest();
                            LastEnterFuben.c2s_dungeonId = LastSelectFuben.fubenId;
                        }
                        else
                        {
                            LastEnterFuben = null;
                        }
                    });
                }
                else
                {
                    LastEnterFuben = null;
                }
            }
        }

        private void on_game_over()
        {
            LastSelectFuben = null;
            LastEnterFuben = null;
            select_random_fuben();
            client.GameSocket.fightLevelHandler.leaveDungeonRequest((err, rsp) =>
            {
                runner.log_response("leaveDungeonRequest done", err, rsp);
            });
        }

        private void on_do_1s(HZZoneLayer layer)
        {
            layer.Actor.SendUnitGuard(true);
            if (runner.CurrentMoveAgent == null || runner.CurrentMoveAgent.IsEnd)
            {
                runner.do_bs_actor_random_move_region(true, 0.01f);
            }

            //if(bot.CurrentSceneType == ZeusCommon.EditorData.ZeusConstConfig.SceneType.Dungeon)
            //{
            //    on_game_over();
            //}
        }

        [Desc("副本配置")]
        [Expandable]
        public class Config
        {
            [Desc("副本检测间隔")]
            public static int CheckIntervalMS = 5000;
            [Desc("副本难度")]
            public static BotClient.DungeonType DType = BotClient.DungeonType.单人副本;
            [Desc("副本等级")]
            public static BotClient.DungeonLevel DLevel = BotClient.DungeonLevel.普通;

            public override string ToString()
            {
                return "副本配置";
            }
        }
    }
}
