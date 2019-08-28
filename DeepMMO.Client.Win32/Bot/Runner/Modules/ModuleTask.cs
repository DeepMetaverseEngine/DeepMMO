using CommonAI.Zone;
using CommonAI.Zone.Helper;
using CommonAI.Zone.ZoneEditor;
using CommonAI.ZoneClient;
using CommonAI.ZoneClient.Agent;
using CommonLang;
using CommonLang.Concurrent;
using CommonLang.Log;
using CommonLang.Reflection;
using CommonLang.Vector;
using pomelo.area;
using pomelo.connector;
using Pomelo.DotNetClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ZeusBattleClient.Client;
using ZeusBattleClientBot.Bot;

namespace ZeusBotTest.Runner
{
    public class ModuleTask : BotRunner.RunnerModule
    {
        private BotClient.QuestData.SeekInfo CurrentQuestSeeking;
        private int InProgressQuestTicking = 0;

        public ModuleTask(BotRunner r) : base(r)
        {
            bot.Client.GameSocket.listen<TaskUpdatePush>(on_task_update_push);
        }

        protected override void OnEnableChanged(bool enable)
        {
            if (!enable)
                do_clear_current_seeking();
        }
        protected internal override void OnGateBindPlayer(BindPlayerResponse e)
        {
            runner.do_gm_add_exp(999999999);
        }
        protected internal override void OnBattleActorDead(ZoneActor actor, UnitDeadEvent e)
        {
            do_clear_current_seeking();
        }
        protected internal override void OnGameEnterScene(EnterSceneResponse response)
        {
            do_clear_current_seeking();
        }
        protected internal override void OnBattleActorReady(CommonAI.ZoneClient.ZoneLayer layer, CommonAI.ZoneClient.ZoneActor actor)
        {
            layer.AddTimeDelayMS(bot.Random.Next(Config.CheckIntervalMS, Config.CheckIntervalMS * 2), (t) =>
              {
                  if (Enable) runner.do_seek_random_task(on_seek_action);
              });
            layer.AddTimePeriodicMS(Config.CheckIntervalMS, (t) =>
            {
                if (bot.CurrentZoneActor.CurrentState == UnitActionStatus.Dead)
                {
                    runner.do_relive(1);
                }
                if (Enable)
                    do_test_quest_seeking_1000();
                else
                    do_clear_current_seeking();
            });
        }

        private void on_task_update_push(TaskUpdatePush e)
        {
            if (Enable)
            {
                log.Log("on_task_update_push");
                if (CurrentQuestSeeking != null)
                {
                    if (CurrentQuestSeeking.quest.State != BotClient.QuestData.QuestStatus.DONE)
                    {
                        log.Log("continue do_seek_task : " + CurrentQuestSeeking.quest);
                        runner.do_seek_task(CurrentQuestSeeking.quest.TemplateID, on_seek_action);
                    }
                }
                else
                {
                    do_clear_current_seeking();
                    var q = runner.do_seek_random_task(on_seek_action);
                    if (q == null)
                    {
                        log.Log("no more task!!!");
                    }
                }
            }
        }

        private void do_test_quest_seeking_1000()
        {
            if (CurrentQuestSeeking != null)
            {
                if (CurrentQuestSeeking.quest.State == BotClient.QuestData.QuestStatus.DONE)
                {
                    do_clear_current_seeking();
                    return;
                }
                else
                {
                    if (CurrentQuestSeeking.quest.Kind != BotClient.QuestData.QuestType.TRUNK)
                    {
                        var q = runner.do_seek_random_task((quest, info) => { });
                        if (q != null && q.Kind == BotClient.QuestData.QuestType.TRUNK)
                        {
                            do_clear_current_seeking();
                            return;
                        }
                    }
                    if (CurrentQuestSeeking.MoveTo != null)
                    {
                        var obj = bot.CurrentZoneActor;
                        if (MathVector.getDistance(CurrentQuestSeeking.MoveTo.x, CurrentQuestSeeking.MoveTo.y, obj.X, obj.Y) < Config.MinSeekRange)
                        {
                            on_seek_quest_move_done(CurrentQuestSeeking);
                        }
                        else if (bot.CurrentZoneLayer.FindPath(bot.CurrentZoneActor.X, bot.CurrentZoneActor.Y, CurrentQuestSeeking.MoveTo.X, CurrentQuestSeeking.MoveTo.Y) != null)
                        {
                            if (bot.CurrentSceneType == ZeusCommon.EditorData.ZeusConstConfig.SceneType.Dungeon)
                            {
                                bot.CurrentZoneActor.SendUnitGuard(true);
                                runner.do_bs_actor_random_move_region(true, Config.MinSeekRange);
                            }
                            else
                            {
                                var agent = runner.do_start_move_agent(CurrentQuestSeeking.MoveTo.X, CurrentQuestSeeking.MoveTo.Y, Config.MinSeekRange);
                                if (agent.TryStep() == false)
                                {
                                    bot.CurrentZoneActor.SendUnitGuard(true);
                                    runner.do_bs_actor_random_move_region(true, Config.MinSeekRange);
                                }
                            }
                        }
                        else
                        {
                            runner.do_bs_actor_random_move_region(true, Config.MinSeekRange);
                        }
                    }
                    else if (CurrentQuestSeeking.NoWay)
                    {
                        if (bot.CurrentSceneType == ZeusCommon.EditorData.ZeusConstConfig.SceneType.Dungeon)
                        {
                            runner.do_bs_actor_random_move_transport(true, Config.MinSeekRange);

                            var quest = runner.do_seek_random_task(on_seek_action, true);
                            if (quest != null && quest.TemplateID == 1015)
                            {
                                runner.do_gm_finish_task(quest.TemplateID);
                                bot.CurrentRegionManager.CheckTrans(1, true);
                            }
                        }
                        else
                        {
                            runner.do_bs_actor_random_move_transport(false, Config.MinSeekRange);
                        }
                    }
                    else
                    {
                        runner.do_bs_actor_random_move_region(true, Config.MinSeekRange);
                    }
                }
            }
            else
            {
                var q = runner.do_seek_random_task(on_seek_action, true);
                if (q == null)
                {
                    runner.do_bs_actor_random_move_transport(true, Config.MinSeekRange);
                    if (runner.CurrentRandomMoveTarget != null && bot.CurrentZoneLayer.IsLoaded)
                    {
                        SetStatus(runner.CurrentRandomMoveTarget.Name + "@" + bot.CurrentZoneLayer.Data);
                    }
                }
            }
        }

        private void do_clear_current_seeking()
        {
            if (runner.CurrentMoveAgent != null)
            {
                runner.CurrentMoveAgent.Stop();
            }
            InProgressQuestTicking = 0;
            CurrentQuestSeeking = null;
        }

        private void on_seek_action(BotClient.QuestData quest, BotClient.QuestData.SeekInfo seek)
        {
            SetStatus(quest.Name + "(" + quest.State + ")");
            log.Log("on_seek_action : " + quest);

            if (quest != null && quest.TemplateID == 1015)
            {
                runner.do_gm_finish_task(quest.TemplateID);
                bot.CurrentRegionManager.CheckTrans(1, true);
            }

            if (CurrentQuestSeeking == null || CurrentQuestSeeking.quest.TemplateID != quest.TemplateID)
            {
                InProgressQuestTicking = 0;
            }
            CurrentQuestSeeking = seek;
            if (seek.MoveTo != null)
            {
                var agent = runner.do_start_move_agent(CurrentQuestSeeking.MoveTo.X, CurrentQuestSeeking.MoveTo.Y, Config.MinSeekRange);
                if (agent.IsEnd || agent.Result == CommonAI.RTS.Manhattan.AstarManhattan.FindPathResult.Destination)
                {
                    bot.CurrentZoneActor.SendUnitGuard(true);
                    if (CurrentQuestSeeking.areaId != bot.CurrentZoneLayer.Data.TemplateID)
                    {
                        bot.CurrentRegionManager.CheckTrans(1, false);
                    }
                }
                else
                {
                    bot.CurrentZoneActor.SendUnitGuard(false);
                }
            }
            else if (seek.NoWay)
            {
                runner.do_bs_actor_random_move_transport(true, Config.MinSeekRange);
            }
            else
            {
                switch (seek.quest.State)
                {
                    case BotClient.QuestData.QuestStatus.IN_PROGRESS:
                        runner.do_gm_finish_task(quest.TemplateID);
                        break;
                    case BotClient.QuestData.QuestStatus.CAN_FINISH:
                        runner.do_submit_task(quest, (err) => { do_clear_current_seeking(); });
                        break;
                    case BotClient.QuestData.QuestStatus.NEW:
                        runner.do_accept_quest(quest, (err) => { do_clear_current_seeking(); });
                        break;
                }
                bot.CurrentZoneLayer.AddTimeDelayMS(bot.Random.Next(Config.CheckIntervalMS, Config.CheckIntervalMS * 2), (t) =>
                {
                    runner.do_seek_random_task(on_seek_action, true);
                });
            }
        }

        private void on_seek_quest_move_done(BotClient.QuestData.SeekInfo seek_info)
        {
            if (runner.CurrentMoveAgent != null)
            {
                runner.CurrentMoveAgent.Stop();
            }
            log.Log("on_seek_quest_move_done : " + seek_info.quest);
            if (seek_info.areaId == bot.CurrentZoneLayer.SceneID)
            {
                switch (seek_info.quest.State)
                {
                    case BotClient.QuestData.QuestStatus.NEW:
                        bot.CurrentZoneActor.SendUnitGuard(false);
                        runner.do_accept_quest(seek_info.quest, (err) => { do_clear_current_seeking(); });
                        break;
                    case BotClient.QuestData.QuestStatus.IN_PROGRESS:
                        log.Error(seek_info.quest.SubType + " : " + seek_info.quest + " : " + seek_info.quest.State + " T=" + InProgressQuestTicking);
                        switch (seek_info.quest.SubType)
                        {
                            case BotClient.QuestData.EventType.InterActiveItem:
                                bot.CurrentZoneActor.SendUnitGuard(false);
                                Vector2 move_to;
                                var item = runner.do_try_pick_nearest_item(out move_to);
                                if (item == null && move_to != null)
                                {
                                    CurrentQuestSeeking.MoveTo = move_to;
                                }
                                break;
                            case BotClient.QuestData.EventType.KillCollect:
                                bot.CurrentZoneActor.SendUnitGuard(true);
                                runner.do_start_pick_any_item();
                                break;
                            case BotClient.QuestData.EventType.FuckFB:
                            case BotClient.QuestData.EventType.KillMonster:
                                bot.CurrentZoneActor.SendUnitGuard(true);
                                break;
                            case BotClient.QuestData.EventType.InterActiveNpc:
                                runner.do_update_quest_status(seek_info.quest);
                                break;
                        }
                        break;
                    case BotClient.QuestData.QuestStatus.CAN_FINISH:
                        runner.do_submit_task(seek_info.quest, (err) =>
                        {
                            do_clear_current_seeking();
                            runner.do_gm_finish_task(seek_info.quest.TemplateID);
                            runner.do_seek_random_task(on_seek_action, true);
                        });
                        break;
                    case BotClient.QuestData.QuestStatus.DONE:
                        log.Log("DONE : " + seek_info.quest + " : " + seek_info.quest.State);
                        CurrentQuestSeeking = null;
                        bot.CurrentZoneActor.SendUnitGuard(false);
                        runner.do_seek_random_task(on_seek_action);
                        break;
                }


            }
            else
            {
                if (bot.CurrentRegionManager.CheckTrans(1, false) == false)
                {
                    bot.CurrentZoneActor.SendUnitGuard(true);
                }
                log.Log("continue do_seek_task : " + seek_info.quest);
                runner.do_seek_task(seek_info.quest.TemplateID, on_seek_action);
            }
            if (seek_info.quest.State == BotClient.QuestData.QuestStatus.IN_PROGRESS)
            {
                InProgressQuestTicking++;
                if (InProgressQuestTicking >= Config.QuestTickingMaxTime)
                {
                    InProgressQuestTicking = 0;
                    runner.do_gm_finish_task(CurrentQuestSeeking.quest.TemplateID);
                    bot.CurrentZoneLayer.AddTimeDelayMS(bot.Random.Next(Config.CheckIntervalMS, Config.CheckIntervalMS * 2), (t) =>
                    {
                        runner.do_seek_random_task(on_seek_action, true);
                    });
                }
            }
        }

        [Desc("任务配置")]
        [Expandable]
        public class Config
        {
            [Desc("任务检测间隔")]
            public static int CheckIntervalMS = 5000;

            [Desc("任务进度最大尝试次数")]
            public static int QuestTickingMaxTime = 20;

            [Desc("寻路到达目的地距离")]
            public static float MinSeekRange = 0.01f;

            public override string ToString()
            {
                return "任务配置";
            }
        }
    }

}
