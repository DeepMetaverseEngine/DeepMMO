using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeepCore.IO;
using DeepCore.ORM;
using DeepMMO.Data;

namespace DeepMMO.Server.Logic
{
    /// <summary>
    /// 副本存档数据
    /// </summary>
    public class DungeonData : ISerializable
    {
        /// <summary>
        /// 副本地图ID
        /// </summary>
        public int mapID;

        /// <summary>
        /// 已进入次数
        /// </summary>
        public int enterCount;
        /// <summary>
        /// 已复活次数
        /// </summary>
        public int rebirthCount;

        /// <summary>
        /// 最后在此副本的位置
        /// </summary>
        public ZonePosition lastPosition;
        /// <summary>
        /// 最后进入副本时间
        /// </summary>
        public DateTime lastEnterTime;
        /// <summary>
        /// 最后离开时间
        /// </summary>
        public DateTime lastLeaveTime;


    }
}
