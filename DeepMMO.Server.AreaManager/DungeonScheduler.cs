using DeepCore.Log;
using DeepCrystal.Schedule;
using System;
//using Quartz;

namespace DeepMMO.Server.AreaManager
{
    public class DungeonScheduler : IDisposable
    {
        private static Logger log = LoggerFactory.GetLogger("DungeonScheduler");
        private ISchedule scheduler;

        public DungeonScheduler()
        {
//             scheduler = ScheduleFactory.Instance.GetScheduler("dungeon", MissFirePolicy.FireOnceNow);
//             {
//                 var old = scheduler.GetJob("task-0");
//                 if (old != null)
//                     scheduler.RemoveJob(old.Name);
//             }
//             scheduler.GetOrCreateCornJob<RefreshDungeonJob>("task-0", "0/10 * * * * ?", new Dictionary<string, object>() { { "123", 123 }, { "str", "str" } });
//             scheduler.GetOrCreateCornJob<RefreshDungeonJob>("task-1", "0/5 * * * * ?", new Dictionary<string, object>() { { "123", 123 }, { "str", "str" } });
            //             IJobDetail job = JobBuilder.Create<RefreshDungeonJob>().WithIdentity("job1", "group1").Build();
            // 
            //             ITrigger trigger = TriggerBuilder.Create().WithIdentity("trigger1", "group1").StartNow().WithSimpleSchedule(x => x.WithIntervalInSeconds(10).RepeatForever()).Build();
            // 
            //             scheduler.ScheduleJob(job, trigger);


          //  scheduler.Start();
        }

        public void Dispose()
        {
       //     scheduler.Shutdown();
        }

        /// <summary>
        /// 副本是否开启
        /// </summary>
        /// <param name="mapID"></param>
        /// <returns></returns>
        public virtual bool IsMapOpen(int mapID)
        {
            return true;
        }
        
    }

    public class RefreshDungeonJob : IJob
    {
        public void Execute(IJobExeContext state)
        {
            state.Log.ErrorFormat("RefreshDungeonJob : {0} : FireTimeUtc={1} ScheduledFireTimeUtc={2} NextFireTimeUtc={3} PreviousFireTimeUtc={4}",
                state.Name,
                state.FireTimeUtc,
                state.ScheduledFireTimeUtc,
                state.NextFireTimeUtc,
                state.PreviousFireTimeUtc);
        }
    }
}

