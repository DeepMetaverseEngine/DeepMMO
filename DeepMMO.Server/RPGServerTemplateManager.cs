using System;
using System.Collections.Generic;
using DeepCore;
using DeepMMO.Data;
using DeepMMO.Protocol.Client;
using DeepCrystal.RPC;
using DeepCore.Reflection;
using DeepCore.Log;
using DeepMMO.Server.Logic;
using DeepMMO.Server.AreaManager;
using DeepMMO.Server.Gate;
using DeepCore.Xml;
using System.Xml;
using DeepCore.IO;
using System.Threading;

namespace DeepMMO.Server
{
    public abstract class RPGServerTemplateManager
    {
        //--------------------------------------------------------------------------------------
        #region Singleton
        private static readonly object lock_init = new object();
        private static bool init_done = false;
        private static RPGServerTemplateManager instance;
        public static bool IsInitDone { get { return init_done; } }
        public static RPGServerTemplateManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lock_init)
                    {
                        if (!init_done)
                        {
                            var config = IService.GlobalConfig;
                            instance = ReflectionUtil.CreateInterface<RPGServerTemplateManager>(GlobalConfig.RPGServerTemplateManager);
                            instance.Init();
                            init_done = true;
                        }
                    }
                }
                return instance;
            }
        }
        #endregion
        //--------------------------------------------------------------------------------------
        protected readonly Logger log;
        protected RPGServerTemplateManager()
        {
            instance = this;
            log = new LazyLogger(GetType().Name);
        }
        public virtual void Init()
        {
            LoadServerList();
        }
        //--------------------------------------------------------------------------------------
        #region TemplatesData

        public abstract RoleTemplateData[] AllRoleTemplates { get; }
        public abstract MapTemplateData[] AllMapTemplates { get; }

        public abstract MapTemplateData GetDefaultMapData(RoleEnterZoneRequest enter);


        public abstract RoleTemplateData GetRoleTemplate(int id, byte gender);
        public abstract MapTemplateData GetMapTemplate(int id);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="local_code">zh_CN, zh_TW, en_US</param>
        /// <returns></returns>
        public abstract LanguageManager GetLanguage(string local_code);

        #endregion
        //--------------------------------------------------------------------------------------

        #region RandomName

        public virtual string RandomName(RoleTemplateData role)
        {
            string ret = ":" + new Random().Next();
            return ret;
        }

        #endregion
        //--------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------   
        /// <summary>
        /// 是否在屏蔽字库内.
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public virtual bool IsBlackWord(string word)
        {
            return false;
        }

        public int GetRoleMaxCount()
        {
            //TODO.
            return 5;
        }

        public virtual ServerRoleData CreateRoleData(ClientCreateRoleRequest req, string accountID, string serverid)
        {
            var roleTemplate = GetRoleTemplate(req.c2s_template_id, 0);
            if (roleTemplate == null)
            {
                return null;
            }

            var roleID = (Guid.NewGuid().ToString());
            //玩家角色信息.
            ServerRoleData srd = new ServerRoleData();
            //创建UUID.
            srd.uuid = roleID;
            srd.account_uuid = accountID;
            srd.name = req.c2s_name;
            srd.server_id = serverid;
            srd.role_template_id = req.c2s_template_id;
            srd.unit_template_id = req.c2s_template_id;
            srd.create_time = DateTime.UtcNow;
            return srd;
        }

        public virtual RoleIDSnap CreateRoleIDSnapData(ServerRoleData roleData)
        {
            var ret = new RoleIDSnap();
            ret.name = roleData.name;
            ret.roleUUID = roleData.uuid;
            ret.serverID = roleData.server_id;
            ret.lv = roleData.Level;
            return ret;
        }
        //--------------------------------------------------------------------------------------

        /// <summary>
        /// 野外.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public virtual bool IsPublicMap(MapTemplateData data)
        {
            return true;
        }

        /// <summary>
        /// 是否是连服场景.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public virtual bool IsCrossServerMap(MapTemplateData data)
        {
            return false;
        }

        /// <summary>
        /// 一次性地图，每次都重新分配.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public virtual bool IsDisposableMap(MapTemplateData data)
        {
            return false;
        }

        public virtual bool AllowChangeLine(MapTemplateData data)
        {
            return false;
        }


        //--------------------------------------------------------------------------------------
        #region ServerList

        private ReaderWriterLockSlim serverListLock = new ReaderWriterLockSlim();
        private HashMap<string, ServerInfo> serverList = new HashMap<string, ServerInfo>();
        private HashMap<string, List<ServerInfo>> groupList = new HashMap<string, List<ServerInfo>>();

        public virtual void LoadServerList()
        {
            var serverListPath = GlobalConfig.ServerListUrl;
            try
            {
                var sList = new HashMap<string, ServerInfo>();
                var gList = new HashMap<string, List<ServerInfo>>();
                //log.Warn("Load Server From : " + serverListPath + "\n" + Resource.LoadAllText(GlobalConfig.ServerListUrl));
                var doc = ServerInfo.LoadServerList(serverListPath, GlobalConfig.RealmID, sList, gList);
                log.Warn("LoadServerList From : " + serverListPath + "\n" + doc.ToXmlString());
                using (serverListLock.EnterWrite())
                {
                    foreach (var e in sList)
                    {
                        log.Warn($"{e.Value} reload");
                    }
                    this.serverList.PutAll(sList);
                    this.groupList.PutAll(gList);
                }
            }
            catch (Exception err)
            {
                throw new Exception("LoadServerList Error From : " + serverListPath, err);
            }
        }
        public virtual void LoadServerListTime()
        {
            var serverListPath = GlobalConfig.ServerListUrl;
            try
            {
                var sList = new HashMap<string, ServerInfo>();
                var gList = new HashMap<string, List<ServerInfo>>();
                //log.Warn("Load Server From : " + serverListPath + "\n" + Resource.LoadAllText(GlobalConfig.ServerListUrl));
                var doc = ServerInfo.LoadServerList(serverListPath, GlobalConfig.RealmID, sList, gList);
                log.Warn("LoadServerListTime From : " + serverListPath + "\n" + doc.ToXmlString());
                using (serverListLock.EnterWrite())
                {
                    foreach (var e in sList)
                    {
                        if (serverList.TryGetValue(e.Key, out var server))
                        {
                            server.open_at = e.Value.open_at;
                            log.Warn($"{server} open_at:{server.open_at}");
                        }
                    }
                }
            }
            catch (Exception err)
            {
                throw new Exception("LoadServerListTime Error From : " + serverListPath, err);
            }
        }


        public virtual string GetServerGroupID(string serverID)
        {
            using (serverListLock.EnterRead())
            {
                if (serverList.TryGetValue(serverID, out var info))
                {
                    return info.group;
                }
            }

            return null;
        }

        /// <summary>
        ///组对应的服务器ID.
        /// </summary>
        /// <param name="serverGroupID"></param>
        /// <returns></returns>
        public virtual List<string> GetServers(string serverGroupID)
        {
            using (serverListLock.EnterRead())
            {
                if (groupList.TryGetValue(serverGroupID, out var list))
                {
                    return list.ConvertAll(e => e.id);
                }
            }
            return null;
        }
        public virtual List<ServerInfo> GetServersInfo(string serverGroupID)
        {
            using (serverListLock.EnterRead())
            {
                if (groupList.TryGetValue(serverGroupID, out var list))
                {
                    return list;
                }
            }
            return null;
        }

        public virtual List<string> GetAllServerGroupID()
        {
            using (serverListLock.EnterRead())
            {
                return new List<string>(groupList.Keys);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="serverID"></param>
        /// <returns></returns>
        public virtual bool ServerIsOpen(string serverID)
        {
            using (serverListLock.EnterRead())
            {
                if (serverList.TryGetValue(serverID, out var info))
                {
                    return info.is_open;
                }
            }
            return false;
        }

        /// <summary>
        /// 获取所有服务器配置
        /// </summary>
        /// <returns></returns>
        public virtual List<ServerInfo> GetAllServers()
        {
            using (serverListLock.EnterRead())
            {
                return new List<ServerInfo>(serverList.Values);
            }
        }

        /// <summary>
        /// 开服时间,非UTC时间.
        /// </summary>
        /// <param name="serverID"></param>
        /// <returns></returns>
        public virtual DateTime GetServerOpenTime(string serverID)
        {
            using (serverListLock.EnterRead())
            {
                if (serverList.TryGetValue(serverID, out var info))
                {
                    return info.open_at;
                }
            }
            return new DateTime();
        }

        /// <summary>
        /// 根据group获得开服时间,合服的话以最早开的为主
        /// </summary>
        /// <param name="groupid"></param>
        /// <returns></returns>
        public DateTime GetOpenTimeByGroupID(string groupid)
        {
            DateTime openTime = DateTime.MaxValue;
            using (serverListLock.EnterRead())
            {
                if (groupList.TryGetValue(groupid, out var list))
                {
                    foreach (var server in list)
                    {
                        if (DateTime.Compare(openTime, server.open_at) > 0)
                        {
                            openTime = server.open_at;
                        }
                    }
                }
            }
            return openTime;
        }

        public virtual HashMap<string, DateTime> GetAllServerOpenTime()
        {
            HashMap<string, DateTime> dateTimes = new HashMap<string, DateTime>();
            using (serverListLock.EnterRead())
            {
                foreach (var data in serverList)
                {
                    dateTimes.Add(data.Key, data.Value.open_at);
                }
            }
            return dateTimes;
        }
        #endregion
        //--------------------------------------------------------------------------------------

        public virtual ISerializable GetCreateZoneExpandData(RoleEnterZoneRequest req)
        {
            return null;
        }

        public virtual bool IsValidOfPrivilege(int privilege, string gmContent)
        {
            return true;
        }


    }
}
