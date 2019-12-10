using DeepCrystal.RPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeepMMO.Server
{
    public static partial class ServerNames
    {

        public const string GateServerType /*    */ = "GateServer";
        public const string ConnectServerType /* */ = "ConnectServer";
        public const string SessionServiceType /**/ = "SessionService";
        public const string AreaManagerType /*   */ = "AreaManager";
        public const string AreaServiceType /*   */ = "AreaService";
        public const string LogicServiceType /*  */ = "LogicService";
        public const string RankingServiceType /*  */ = "RankingService";

        public static RemoteAddress GateServer = new RemoteAddress("GateServer", null, GateServerType);
        public static RemoteAddress ConnectServer = new RemoteAddress("Connect:<Number>", null, ConnectServerType);
        public static RemoteAddress SessionService = new RemoteAddress("Session:<AccountID>", null, SessionServiceType);
        public static RemoteAddress AreaManager = new RemoteAddress("AreaManager", null, AreaManagerType);
        public static RemoteAddress AreaService = new RemoteAddress("Area:<Number>", null, AreaServiceType);
        public static RemoteAddress LogicService = new RemoteAddress("Logic:<RoleID>", null, LogicServiceType);
        public static RemoteAddress RankingService = new RemoteAddress("RankingService", null, RankingServiceType);


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

    }
}
