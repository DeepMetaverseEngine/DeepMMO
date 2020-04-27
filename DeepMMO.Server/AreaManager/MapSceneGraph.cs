using DeepCore;
using DeepCore.Astar;
using DeepCore.GameData.Zone.ZoneEditor;
using DeepCore.Geometry;
using DeepCore.Log;
using System;
using System.Collections.Generic;

namespace DeepMMO.Server.AreaManager
{
    /// <summary>
    /// 跨场景寻路网格
    /// </summary>
    public class MapSceneGrapAstar : Astar<MapSceneGrapAstar.SceneGraphNode, MapSceneGrapAstar.SceneGraphPath>
    {
        private static Logger log = new LazyLogger(nameof(MapSceneGrapAstar));
        private SceneGraphMap terrain;
        public MapSceneGrapAstar(MapTemplateData[] nodes)
        {
            this.terrain = new SceneGraphMap(nodes);
            base.InitGraph(terrain);
        }
        public override SceneGraphPath GenWayPoint(SceneGraphNode node)
        {
            return new SceneGraphPath(node);
        }
        protected override void SetTempNode(IMapNode node, ITempMapNode temp)
        {
            (node as SceneGraphNode).TempNode = temp;
        }
        protected override ITempMapNode GetTempNode(IMapNode node)
        {
            return (node as SceneGraphNode).TempNode;
        }

        /// <summary>
        /// 跨场景寻路
        /// </summary>
        /// <param name="srcMapID"></param>
        /// <param name="dstMapID"></param>
        /// <returns></returns>
        public ArrayList<SceneNextLink> FindPath(int srcMapID, int dstMapID)
        {
            var snode = terrain.GetNode(srcMapID);
            if (snode == null) return null;
            var dnode = terrain.GetNode(dstMapID);
            if (dnode == null) return null;
            var path = base.FindPath(snode, dnode, null);
            if (path != null)
            {
                var ret = new ArrayList<SceneNextLink>();
                foreach (var wp in path)
                {
                    var next = wp.Next;
                    if (next != null)
                    {
                        var info = wp.Node.GetNextInfo(next.Node.MapID);
                        ret.Add(info);
                    }
                }
                return ret;
            }
            return null;
        }
        public class SceneGraphMap : IAstarGraph<SceneGraphNode>
        {
            private readonly HashMap<int, SceneGraphNode> nodes;
            public int TotalNodeCount { get { return nodes.Count; } }
            public SceneGraphMap(MapTemplateData[] nodes)
            {
                this.nodes = new HashMap<int, SceneGraphNode>(nodes.Length);
                foreach (var data in nodes)
                {
                    var node = new SceneGraphNode(data);
                    this.nodes.Add(node.MapID, node);
                }
                foreach (var node in this.nodes.Values)
                {
                    node.InitNexts(this);
                }
            }
            public void Dispose()
            {
                foreach (var node in this.nodes.Values)
                {
                    node.Dispose();
                }
                nodes.Clear();
            }
            public void ForEachNodes(Action<SceneGraphNode> action)
            {
                foreach (var node in this.nodes.Values)
                {
                    action(node);
                }
            }
            internal SceneGraphNode GetNode(int mapID)
            {
                return nodes.Get(mapID);
            }
        }
        public class SceneGraphNode : IMapNode
        {
            private SceneGraphNode[] nexts_array;
            private HashMap<int, SceneNextLink> nexts = new HashMap<int, SceneNextLink>(1);

            public int MapID { get; private set; }
            public MapTemplateData Data { get; private set; }
            public override IMapNode[] Nexts { get { return nexts_array; } }
            public override int CloseAreaIndex { get { return 0; } }
            public override object Tag { get; set; }
            internal ITempMapNode TempNode;
            public SceneGraphNode(MapTemplateData data)
            {
                this.Data = data;
                this.MapID = data.id;
            }
            public override void Dispose()
            {
                nexts.Clear();
            }
            public override bool TestCross(IMapNode other)
            {
                return nexts.ContainsKey((other as SceneGraphNode).MapID);
            }
            public override float GetG(IMapNode target) { return 1; }
            public override float GetH(IMapNode father) { return 1; }
            internal void InitNexts(SceneGraphMap map)
            {
                nexts.Clear();
                var list = new List<SceneGraphNode>(1);
                if (Data.connect != null)
                {
                    foreach (var next in Data.connect)
                    {
                        var next_node = map.GetNode(next.to_map_id);
                        if (next_node != null)
                        {
                            if (!nexts.ContainsKey(next_node.MapID))
                            {
                                var ds = RPGServerBattleManager.Instance.GetSceneAsCache(next_node.Data.zone_template_id);
                                if (ds != null && ds.Regions.TryFind(e => e.Name == next.to_flag_name, out var next_rg))
                                {
                                    next.to_flag_pos = new Vector3(next_rg.X, next_rg.Y, next_rg.Z);
                                    nexts.Add(next_node.MapID, next);
                                }
                                else
                                {
                                    //throw new Exception($"Next Link Data Error : MapID={MapID} : {next}");
                                    log.Error($"Next Link Data Error : MapID={MapID} : {next}");
                                }
                            }
                            list.Add(next_node);
                        }
                    }
                }
                this.nexts_array = list.ToArray();
            }
            internal SceneNextLink GetNextInfo(int mapID)
            {
                return nexts.Get(mapID);
            }
        }
        public class SceneGraphPath : IWayPoint<SceneGraphNode, SceneGraphPath>
        {
            public MapTemplateData Data { get; private set; }
            public SceneGraphPath(SceneGraphNode map_node) : base(map_node)
            {
                this.Data = base.Node.Data;
            }
            public override bool PosEquals(SceneGraphPath w)
            {
                return Data.id == w.Data.id;
            }
        }

    }


}
