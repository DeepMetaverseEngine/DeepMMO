using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DeepCore;
using DeepCore.Log;
using DeepMMO.Data;

namespace DeepMMO.Client
{
    public abstract class RPGClientTemplateManager
    {
        #region Singleton
        private static RPGClientTemplateManager s_instance;
        public static RPGClientTemplateManager Instance { get { return s_instance; } }
        protected RPGClientTemplateManager()
        {
            s_instance = this;
            log = LoggerFactory.GetLogger(GetType().Name);
        }
        #endregion
        protected readonly Logger log;

        //--------------------------------------------------------------------------------------
        #region Roles

        public abstract RoleTemplateData[] AllRoleTemplates { get; }

        public abstract RoleTemplateData GetRoleTemplate(int id, byte gender);
        

        #endregion
        //--------------------------------------------------------------------------------------
        #region ServerList

        private HashMap<string, ServerInfo> serverList = new HashMap<string, ServerInfo>();
        private HashMap<string, List<ServerInfo>> groupList = new HashMap<string, List<ServerInfo>>();
        private List<ServerInfo> recommendServers = new List<ServerInfo>();

        public virtual void LoadServerList(string serverListPath)
        {
            serverList.Clear();
            groupList.Clear();
            recommendServers.Clear();
            try
            {
                log.Info("Load Server From : " + serverListPath);
                ServerInfo.LoadServerList(serverListPath, serverList, groupList, recommendServers);
            }
            catch (Exception err)
            {
                throw new Exception("Load Server List Error From : " + serverListPath, err);
            }
        }
        
        public virtual ServerInfo GetServer(string serverID)
        {
            if (serverList.TryGetValue(serverID, out var info))
            {
                return info;
            }
            return null;
        }


        public virtual string GetServerGroupID(string serverID)
        {
            if (serverList.TryGetValue(serverID, out var info))
            {
                return info.group;
            }
            return serverID;
        }

        /// <summary>
        ///组对应的服务器ID.
        /// </summary>
        /// <param name="serverGroupID"></param>
        /// <returns></returns>
        public virtual List<ServerInfo> GetServers(string serverGroupID)
        {
            if (groupList.TryGetValue(serverGroupID, out var list))
            {
                return new List<ServerInfo>(list);
            }
            return null;
        }

        public virtual List<ServerInfo> GetRecommendServers()
        {
            return new List<ServerInfo>(recommendServers);
        }

        /// 
        /// </summary>
        /// <param name="serverID"></param>
        /// <returns></returns>
        public virtual bool ServerIsOpen(string serverID)
        {
            if (serverList.TryGetValue(serverID, out var info))
            {
                return info.is_open;
            }
            return false;
        }

        /// <summary>
        /// 获取所有服务器配置
        /// </summary>
        /// <returns></returns>
        public virtual List<ServerInfo> GetAllServers()
        {
            return new List<ServerInfo>(serverList.Values);
        }

        #endregion
        //--------------------------------------------------------------------------------------
    }
}
