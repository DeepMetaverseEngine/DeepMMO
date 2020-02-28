using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeepCore;
using DeepCore.Reflection;

namespace DeepMMO.Client.BotTest
{

    public class BotConfig
    {
        //-------------------------------------------------------------------------------------

        //-------------------------------------------------------------------------------------

        //-------------------------------------------------------------------------------------

        [Desc("服务器入口", "服务器")]
        [OptionalValue(
            "http://office.1gamesh.com:31000/api/client/server_list",
            "http://office.1gamesh.com:31000/api/server/server_list",
            "http://192.168.0.147:7002/api/server/server_list,",
            "http://192.168.1.173:7002/api/server/server_list",
            "http://192.168.0.57:7002/api/server/server_list")]
        public string ServerListURL = "http://192.168.0.57:7002/api/server/server_list";

        [Desc("IP地址映射(外网转内网)", "客户端")]
        public HashMap<string, string> AddressMapping;
        //-------------------------------------------------------------------------------------
        [Desc("客户端FPS", "显示")]
        public int ClientFPS = 30;

        [Desc("添加机器人间隔", "显示")]
        public int AddBotIntervalMS = 1000;

        [Desc("维持机器人数量", "显示")]
        public int KeepBotCount = 0;

        [Desc("是否显示场景", "显示")]
        public bool NoBattleView = false;
        //-------------------------------------------------------------------------------------

    }

    public class BotConfigHistory
    {
        public HashMap<string, List<object>> List = new HashMap<string, List<object>>();
    }

}
