using System.Linq;
using pomelo.connector;
using CommonLang.Reflection;

namespace ZeusBotTest.Runner
{
    public class ModuleGuild : BotRunner.RunnerModule
    {
        string nameList = "abcdefghijklmnopqrstuvwsyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";

        public ModuleGuild(BotRunner r) : base(r)
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
                    try_get_guild_list();
                    try_get_guild_member_list();
                    try_get_guild_depot_list();
                }
            });
            layer.AddTimePeriodicMS(Config.CheckIntervalMS, (t) =>
            {
                if (Enable)
                {
                    
                }
            });
        }
        private void try_get_guild_list()
        {
            client.GameSocket.guildHandler.getGuildListRequest("",
                    (err, rsp) =>
                    {

                        if (30 > rsp.s2c_guildList.Count)
                        {
                            client.GameSocket.guildHandler.createGuildRequest("111", get_rand_name(), "1",
                            (err1, rsp1) => { });
                        }
                        
                    });
        }
        
        private string get_rand_name()
        {
            string name = "";

            for (int j = 0; j < 6; j++)
            {
                int randChar = bot.Random.Next(0, nameList.Count() - 1);

                name += nameList[randChar];
            }
            return name;
        }

        private void try_get_guild_member_list()
        {
            client.GameSocket.guildHandler.getMyGuildMembersRequest(
                    (err, rsp) =>
                    { });
        }
        private void try_get_guild_depot_list()
        {
            client.GameSocket.guildManagerHandler.getDepotInfoRequest(
                    (err, rsp) =>
                    { });
        }
        [Desc("公会配置")]
        [Expandable]
        public class Config
        {
            [Desc("公会检测间隔")]
            public static int CheckIntervalMS = 5000;

            public override string ToString()
            {
                return "公会配置";
            }
        }
    }
}
