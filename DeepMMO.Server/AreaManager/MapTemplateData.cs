
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeepCore;
using DeepCore.IO;
using DeepMMO.Server.Common;

namespace DeepMMO.Server.AreaManager
{
    public class MapTemplateData : ISerializable
    {
        static MapTemplateData()
        {
            Parser.RegistParser(new SceneNextLinkParser());
        }

        /**<summary>场景ID<summary/> */
        public int id;
        /**<summary>场景名称<summary/> */
        public string name;
        /**<summary>战斗地图ID<summary/> */
        public int zone_template_id;
        /**<summary>重置时间<summary/> */
        public string reset_time;
        /**<summary>复活地图ID<summary/> */
        public int revival_map_id;
        /**<summary>场景小地图<summary/> */
        public string small_map;
        /**<summary>场景连接<summary/> */
        public ArrayList<SceneNextLink> connect;
        /**<summary>人数软上限<summary/> */
        public int full_players;
        /**<summary>人数硬上限<summary/> */
        public int max_players;
        /**<summary>开放策略<summary/> */
        public int open_rule;
        /**<summary>开放日<summary/> */
        public string open_time;
        /**<summary>结束后倒计时时间<summary/> */
        public int countdown_time_sec;
        /**<summary>是否为公共地图<summary/> */
        public bool is_public;
        /// <summary>
        /// 是否允许主动切线.
        /// </summary>
        public int is_changeline;

        public override string ToString()
        {
            return string.Format("{0}({1})", name, id);
        }
    }

    public class SceneNextLink : ISerializable
    {
        public string from_flag_name;
        public int to_map_id;
        public string to_flag_name;
    }

    public class SceneNextLinkParser : ListParser<SceneNextLink>
    {
        public SceneNextLinkParser() : base(';') { }
        //  flagName, sceneID, flagName ; flagName, sceneID, flagName 
        public override SceneNextLink StringToElement(string text)
        {
            try
            {
                var kvc = text.Split(',');
                var ret = new SceneNextLink();
                ret.from_flag_name = kvc[0];
                ret.to_map_id = int.Parse(kvc[1]);
                ret.to_flag_name = kvc[2];
                return ret;
            }
            catch (Exception err)
            {
                throw new Exception("Parse SceneNextLink Error : " + text + " : " + err.Message, err);
            }
        }
    }


}
