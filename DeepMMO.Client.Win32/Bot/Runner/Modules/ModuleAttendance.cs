using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pomelo.connector;
using CommonLang.Reflection;
using pomelo.area;

namespace CommonRPG.Client.BotTest.Runner
{
    public class ModuleAttendance : BotRunner.RunnerModule
    {
        public ModuleAttendance(BotRunner r) : base(r)
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
                    try_get_attendance_info();
                }
            });
        }
        private void try_get_attendance_info()
        {
            client.GameSocket.attendanceHandler.getAttendanceInfoRequest(
                (err, rsp) =>
                { });
        }
        [Desc("签到配置")]
        [Expandable]
        public class Config
        {
            [Desc("签到检测间隔")]
            public static int CheckIntervalMS = 5000;

            public override string ToString()
            {
                return "签到配置";
            }
        }
    }
}
