using DeepCrystal.RPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeepMMO.Server
{
    public static class ServerNames
    {

        public const string GateServerType /*    */ = "GateServer";
        public const string ConnectServerType /* */ = "ConnectServer";
        public const string AreaManagerType /*   */ = "AreaManager";
        public const string MailServiceType /*   */ = "MailService";
        public const string SocialServiceType/*  */ = "SocialService";
        public const string ShopServiceType /*   */ = "ShopService";
        public const string GuildServiceType /*  */ = "GuildService";
        public const string RankingServiceType /*  */ = "RankingService";
        public const string PaServerType /*  */ = "PayServer";
        public const string ChatServiceType /*   */ = "ChatService";
        public const string ArenaServiceType /*   */ = "ArenaService";
        public const string ChannelServiceType /**/ = "ChannelService";
        public const string SessionServiceType /**/ = "SessionService";
        public const string LogicServiceType /*  */ = "LogicService";
        public const string AreaServiceType /*   */ = "AreaService";
        public const string TeamServiceType /*   */ = "TeamService";
        public const string AuctionServiceType/*  */ = "AuctionService";
        public const string MasterRaceServiceType/*  */ = "MasterRaceService";
        public const string GlobalServiceType/*  */ = "GlobalService";


        public static RemoteAddress GateServer = new RemoteAddress("GateServer", null, GateServerType);
        public static RemoteAddress ConnectServer = new RemoteAddress("Connect:<Number>", null, ConnectServerType);
        public static RemoteAddress AreaManager = new RemoteAddress("AreaManager", null, AreaManagerType);
        public static RemoteAddress SocialService = new RemoteAddress("SocialService", null, SocialServiceType);
        public static RemoteAddress GuildService = new RemoteAddress("GuildService", null, GuildServiceType);
        public static RemoteAddress RankingService = new RemoteAddress("RankingService", null, RankingServiceType);
        public static RemoteAddress PayServer = new RemoteAddress("PayServer", null, PaServerType);
        public static RemoteAddress ChatService = new RemoteAddress("ChatService", null, ChatServiceType);
        public static RemoteAddress ArenaService = new RemoteAddress("ArenaService", null, ArenaServiceType);
        public static RemoteAddress ShopService = new RemoteAddress("ShopService", null, ShopServiceType);
        public static RemoteAddress ChannelService = new RemoteAddress("ChannelService:<string>", null, ChannelServiceType);
        public static RemoteAddress SessionService = new RemoteAddress("Session:<AccountID>", null, SessionServiceType);
        public static RemoteAddress LogicService = new RemoteAddress("Logic:<RoleID>", null, LogicServiceType);
        public static RemoteAddress AreaService = new RemoteAddress("Area:<Number>", null, AreaServiceType);
        public static RemoteAddress TeamCenterService = new RemoteAddress("TeamCenterService", null, AreaServiceType);
        public static RemoteAddress AuctionService = new RemoteAddress("AuctionService", null, AuctionServiceType);
        public static RemoteAddress MasterRaceService = new RemoteAddress("MasterRaceService", null, MasterRaceServiceType);
        public static RemoteAddress GlobalService = new RemoteAddress("GlobalService", null, GlobalServiceType);

        public static RemoteAddress CreateChannelServiceAddress(string channelName)
        {
            return new RemoteAddress("Channel:" + channelName, null, ChannelService.ServiceType);
        }


        //         public static RemoteAddress CreateAreaServiceAddress(int areaID)
        //         {
        //             return new RemoteAddress("Area:" + areaID, null, AreaService.ServiceType);
        //         }
        public static RemoteAddress CreateSessionServiceAddress(string accountID, RemoteAddress connectServer)
        {
            return new RemoteAddress("Session:" + accountID, connectServer.ServiceNode, SessionService.ServiceType);
        }

        /// <summary>
        /// 获取逻辑服务地址
        /// </summary>
        /// <param name="roleID"></param>
        /// <returns></returns>
        public static RemoteAddress GetLogicServiceAddress(string roleID, string logicNode = null)
        {
            return new RemoteAddress("Logic:" + roleID, logicNode, LogicService.ServiceType);
        }

        public static RemoteAddress GetGlobalServiceAddress(string GID, string node = null)
        {
            return new RemoteAddress(GlobalService.ServiceType + ":" + GID, node, GlobalService.ServiceType);
        }
    }
}
