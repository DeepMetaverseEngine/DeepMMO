using DeepCore;
using DeepCore.IO;
using DeepCore.Xml;
using System;
using System.Collections.Generic;
using System.Xml;

namespace DeepMMO.Data
{
    [MessageType(Constants.LOGIN_START + 1)]
    public class ClientInfo : ISerializable
    {
        public string userAgent;
        public string network;
        public string deviceId;
        public string deviceType;
        public string deviceModel;
        public string region;
        public string channel;
        public string subChannel;
        public string clientVersion;
        public string sdkVersion;
        public string sdkName;
        public string userSource1;
        public string userSource2;
        public string platformAcount;
    }

    [MessageType(Constants.LOGIN_START + 2)]
    public class ServerInfo : ISerializable
    {
        /// <summary>服务器id</summary>
        public string id;
        /// <summary>服务器名称</summary>
        public string name;
        /// <summary>服务器ip</summary>
        public string address;
        /// <summary>区id</summary>
        public string realm;
        /// <summary>服务器状态</summary>
        public string state;
        /// <summary>服务器状态文字</summary>
        public string state_text;
        /// <summary>逻辑服务器id</summary>
        public string group;
        /// <summary>逻辑服可分配的节点</summary>
        public string[] nodes;

        /// <summary>服务器类型</summary>
        public string type;
        /// <summary>是否对玩家可见</summary>
        public bool is_open;
        /// <summary>区名称图片id</summary>
        public string icon;
        /// <summary>服务器排序</summary>
        public int view_index;
        /// <summary>服务器排序</summary>
        public int view_realm_index;
        /// <summary>服务器排序</summary>
        public string view_realm_name;
        /// <summary>颜色值</summary>
        public int view_rgba;

        /// <summary>
        /// 是否强制排队等待
        /// </summary>
        public bool isForceQueueUp;
        /// <summary>
        /// 开服时间.
        /// </summary>
        public DateTime open_at;

        public override string ToString()
        {
            return string.Format("{0} (id={1} realm={2})", this.name, this.id, this.realm);
        }


        public ServerInfo Clone()
        {
            var ret = (ServerInfo)MemberwiseClone();
            return ret;
        }
        /*
        <?xml version="1.0" encoding="utf-8"?>
        <doc>
          <serverList>
            <server realm="1" id="1"     group="1"  name="外网测试服"        address="103.242.169.212:38000"   state ="1:正常"  is_open="1"  view_index="1"  />
            <server realm="1" id="2"     group="1"  name="内网策划服"        address="192.168.1.231:19001"     state ="1:正常"  is_open="1"  view_index="2"  />
            <server realm="1" id="3"     group="1"  name="内网测试服"        address="192.168.1.226:19001"     state ="1:正常"  is_open="1"  view_index="3"  />
            <server realm="1" id="4"     group="1"  name="审核服"            address="192.168.1.226:29001"     state ="1:正常"  is_open="1"  view_index="4"  />
            <server realm="1" id="5"     group="1"  name="六七"              address="192.168.1.19:19001"      state ="1:正常"  is_open="1"  view_index="5"  />
            <server realm="1" id="6"     group="1"  name="从现在开始"        address="192.168.1.102:19001"     state ="1:正常"  is_open="1"  view_index="6"  />
            <server realm="1" id="7"     group="1"  name="从现在开始-外网"   address="103.242.169.212:48001"   state ="1:正常"  is_open="1"  view_index="7"  />
            <server realm="1" id="2000"  group="1"  name="(VS)尼霸霸"        address="192.168.1.20:19001"      state ="1:正常"  is_open="1"  view_index="8"  />
            <server realm="1" id="3000"  group="1"  name="(VS)luo"           address="192.168.1.21:19001"      state ="1:正常"  is_open="1"  view_index="9"  />
            <server realm="1" id="4000"  group="1"  name="(VS)吴"            address="192.168.1.11:19001"      state ="1:正常"  is_open="1"  view_index="10" />
            <server realm="1" id="5000"  group="1"  name="(VS)老蔡"          address="192.168.1.12:19001"      state ="1:正常"  is_open="1"  view_index="11" />
            <server realm="1" id="6000"  group="1"  name="(VS)老Q"           address="192.168.1.17:19001"      state ="1:正常"  is_open="1"  view_index="12" />
            <server realm="1" id="7000"  group="1"  name="(VS)飞哥"          address="192.168.1.22:19001"      state ="1:正常"  is_open="1"  view_index="13" />
            <server realm="1" id="9001"  group="1"  name="(VS)本地开发服G1"  address="127.0.0.1:19001"         state ="1:正常"  is_open="1"  view_index="0"  nodes="LogicNode1"/>
            <server realm="1" id="9002"  group="2"  name="(VS)本地开发服G2"  address="127.0.0.1:19001"         state ="1:正常"  is_open="1"  view_index="0"  nodes="LogicNode2"/>
            <server realm="1" id="9003"  group="3"  name="(VS)本地开发服G3"  address="127.0.0.1:19001"         state ="1:正常"  is_open="1"  view_index="0"  nodes="LogicNode2,LogicNode1"/>
            <server realm="1" id="9003"  group="4"  name="(VS)本地开发服G4"  address="127.0.0.1:19001"         state ="1:正常"  is_open="1"  view_index="0"  nodes="LogicNode2,LogicNode1"/>
          </serverList>
          <recomList>
            <serverId>3</serverId>
            <serverId>2</serverId>
          </recomList>
        </doc> 
        */
        public static void LoadServerList(XmlDocument xml, string realmID, HashMap<string, ServerInfo> serverList, HashMap<string, List<ServerInfo>> groupList = null, List<ServerInfo> recommendList = null)
        {
            var serverListXml = XmlUtil.FindChild<XmlElement>(xml.DocumentElement, "serverList");
            if (serverListXml != null)
            {
                serverListXml.ForEachChilds<XmlElement>("server", (e) =>
                {
                    var serverProp = Properties.LoadFromXML(e, true);
                    var serverInfo = serverProp.CreateInstance<ServerInfo>();
                    if (realmID == null || realmID == serverInfo.realm || serverInfo.realm == "10086")
                    {
                        serverList.Add(serverInfo.id, serverInfo);
                        if (groupList != null)
                        {
                            var group = groupList.GetOrAdd(serverInfo.group, (g) => new List<ServerInfo>());
                            group.Add(serverInfo);
                        }
                    }
                });
            }
            if (recommendList != null)
            {
                var recomListXml = XmlUtil.FindChild<XmlElement>(xml.DocumentElement, "recomList");
                if (recomListXml != null)
                {
                    recomListXml.ForEachChilds<XmlElement>("serverId", (e) =>
                    {
                        var serverID = e.GetXmlNodeText();
                        if (serverList.TryGetValue(serverID, out var serverInfo))
                        {
                            if (realmID == null || realmID == serverInfo.realm)
                            {
                                recommendList.Add(serverInfo);
                            }
                        }
                    });
                }
            }
        }
        public static XmlDocument LoadServerList(string path, string realmID, HashMap<string, ServerInfo> serverList, HashMap<string, List<ServerInfo>> groupList = null, List<ServerInfo> recommendList = null)
        {
            try
            {
                var xml = XmlUtil.LoadXML(path);
                LoadServerList(xml, realmID, serverList, groupList, recommendList);
                return xml;
            }
            catch (Exception err)
            {
                throw new Exception("Load Server List Error From : " + path, err);
            }
        }
        public static void LoadServerList(XmlDocument xml, HashMap<string, ServerInfo> serverList, HashMap<string, List<ServerInfo>> groupList = null, List<ServerInfo> recommendList = null)
        {
            LoadServerList(xml, null, serverList, groupList, recommendList);
        }
        public static void LoadServerList(string path, HashMap<string, ServerInfo> serverList, HashMap<string, List<ServerInfo>> groupList = null, List<ServerInfo> recommendList = null)
        {
            LoadServerList(path, null, serverList, groupList, recommendList);
        }

    }


}
