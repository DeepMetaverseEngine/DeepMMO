using CommonLang.Reflection;
using pomelo.connector;

namespace ZeusBotTest.Runner
{
    public class ModuleMount : BotRunner.RunnerModule
    {
        public ModuleMount(BotRunner r) : base(r)
        {
        }

        protected internal override void OnGateBindPlayer(BindPlayerResponse e)
        {
            base.OnGateBindPlayer(e);
            runner.do_gm_open_func();
        }
        protected internal override void OnBattleActorReady(CommonAI.ZoneClient.ZoneLayer layer, CommonAI.ZoneClient.ZoneActor actor)
        {
            layer.AddTimeDelayMS(Config.CheckIntervalMS, (t) =>
            {
                if (Enable)
                {
                    try_ride_mount();
                }
            });
            layer.AddTimePeriodicMS(Config.CheckIntervalMS, (t) =>
            {
                if (Enable)
                {
                    try_get_mount_info();
                }
            });
        }
        private void try_get_mount_info()
        {
            log.Log("try_get_mount_info2 : ");
            client.GameSocket.mountHandler.getMountInfoRequest(
                (err, rsp) =>
                {
                    log.Log("try_get_mount_info : " + rsp);
                });
        }
        private void try_ride_mount()
        {
            log.Log("try_ride_mount : ");
            client.GameSocket.mountHandler.ridingMountRequest(1,
                (err, rsp) =>
                {
                });
        }
        [Desc("坐骑配置")]
        [Expandable]
        public class Config
        {
            [Desc("坐骑检测间隔")]
            public static int CheckIntervalMS = 5000;

            public override string ToString()
            {
                return "坐骑配置";
            }
        }
    }
}
