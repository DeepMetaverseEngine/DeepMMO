using System;
using System.Collections.Generic;
using DeepCore.Astar;
using DeepCore.Game3D.Slave;
using DeepCore.Game3D.Slave.Helper;
using DeepCore.Game3D.Slave.Layer;
using DeepCore.Game3D.Voxel;
using DeepCore.GameData.Data;
using DeepCore.GameData.Zone;
using DeepCore.IO;
using DeepCore.Unity3D;
using UnityEngine;
using ILayerWayPoint = DeepCore.Game3D.Slave.ILayerWayPoint;
using Vector2 = DeepCore.Geometry.Vector2;
using Vector3 = DeepCore.Geometry.Vector3;

namespace DeepMMO.Unity3D.Terrain
{
    public class ClientMapNode : IMapNode
    {
        internal int areaLinkValue;
        internal ClientMapNode[] nexts;
        public Vector3 Position { get; private set; }

        public ClientMapNode(Vector3 pos)
        {
            this.Position = pos;
        }
        public override bool TestCross(IMapNode other)
        {
            return true;
        }

        public override float GetG(IMapNode target)
        {
            var tt = target as ClientMapNode;
            return Vector3.Distance(Position, tt.Position);
        }

        public override float GetH(IMapNode father)
        {
            var ft = father as ClientMapNode;
            return Vector3.Distance(Position, ft.Position);
        }

        public override void Dispose()
        {
           
        }

        protected override void SetCloseArea(int index)
        {
            this.areaLinkValue = index;
        }

        public override int CloseAreaIndex
        {
            get => this.areaLinkValue;
        }
        public override object Tag { get; set; }
        public override IMapNode[] Nexts {get { return nexts; }}
    }
    public class NavMeshWayPoint : IWayPoint<ClientMapNode, NavMeshWayPoint>
    {
        public float X { get => Position.X; }
        public float Y { get => Position.Y; }
        public float Z { get => Position.Z; }
        public Vector3 Position { get; set; }
        internal NavMeshWayPoint(ClientMapNode mapNode) : base(mapNode)
        {
            this.Position = base.Node.Position;
        }
        public override bool PosEquals(NavMeshWayPoint w)
        {
            return this.Position == w.Position;
        }
        public virtual float GetTotalDistance()
        {
            float ret = 0;
            var cur = this;
            while (cur != null)
            {
                var nex = cur.Next;
                if (cur != null && nex != null)
                {
                    ret += Vector3.Distance(cur.Position, nex.Position);
                }
                cur = nex;
            }
            return ret;
        }
    public class NavMeshClientTerrain3D : ILayerZoneTerrain
    {
        private float TerrainWidth;
        private float TerrainHeight;
        private int TerrainGridCellSize;
        protected float StepIntercept;
        private string[] laymaskNames =
        {
            "NavLayer",
        };

        private int layMasks;
        private  ClientUnityObject preUnitObj;
        private float Height = 2f;// 碰撞高度
        private float FindPathDistance;//最近路点采样距离
        private RcdtcsUnityUtils.SystemHelper m_System = new RcdtcsUnityUtils.SystemHelper();
        public ClientFlyPath clientFlyPath;
        public float LandOffSet = 0.5f;
//        public GameObject cubeobj;
        public NavMeshClientTerrain3D(string scenePathFindFileName,float terrainWidth,float terrainheight,int terrainGridCellSize,float stepIntercept,float findPathDistance = 3f)
        {
            TerrainWidth = terrainWidth;
            TerrainHeight = terrainheight;
            TerrainGridCellSize = terrainGridCellSize;
            StepIntercept = stepIntercept;
            layMasks =  LayerMask.GetMask(laymaskNames);
            if (preUnitObj == null)
            {
                preUnitObj = new ClientUnityObject(terrainWidth,terrainheight,stepIntercept,Height,terrainGridCellSize);
            }

            FindPathDistance = findPathDistance;
            
          
            var path = DataPathHelper.GAME_EDITOR_ROOT+scenePathFindFileName;
            if (!string.IsNullOrEmpty(path))
            {
                var data = Resource.LoadData(path);
                
                if (data == null)
                {
                    m_System = null;
                    return;
                }
                m_System.ComputeSystem(data,0);
            }

            clientFlyPath = new ClientFlyPath(m_System, layMasks, FindPathDistance)
            {
                LandOffset = LandOffSet, event_FindGroundPath = event_FindGroundPath
            };
            
//            cubeobj = GameObject.CreatePrimitive(PrimitiveType.Cube);
//            cubeobj.GetComponent<Collider>().enabled = false;

        }
            
        
        public UnityEngine.Vector3 FixPos(UnityEngine.Vector3 pos)
        {
            if (clientFlyPath != null)
            {
               return clientFlyPath.FixPos(pos);
            }

            return pos;
        }
        public UnityEngine.Vector3 UpdateAirMove(UnityEngine.Vector3 startPos,float speed,ref List<UnityEngine.Vector3> path)
        {
            if (clientFlyPath != null)
            {
                startPos = clientFlyPath.Update(startPos,speed);
                path = clientFlyPath.GetPath();
            }
            return startPos;
        }

        public float TotalWidth => TerrainWidth;
        public float TotalHeight => TerrainHeight;
        public int GridCellSize => TerrainGridCellSize;

        public bool m_CanFly = false;

        public void Dispose()
        {
            preUnitObj = null;
            if (m_System != null)
            {
                m_System.Clear();
                m_System = null;
            }
           
        }
       
        public void ForEachNodes<T>(List<T> mapNode,Action<T> action)
        {
            foreach (var _mapnode in mapNode)
            {
                if (action != null)
                {
                    action(_mapnode);
                }
            }
        }
        
        private NavMeshWayPoint GenNavMeshWayPoint(List<UnityEngine.Vector3> path)
        {
            
            if (path != null && path.Count > 0)
            {
                List<ClientMapNode> mapnodes = new List<ClientMapNode>();
                List<NavMeshWayPoint> waypoints = new List<NavMeshWayPoint>();
                
                foreach (var point in path)
                {
                    var zonepoint = BattleUtils.UnityPos2ZonePos(TerrainHeight, point);
                    var mapnode = new ClientMapNode(zonepoint);
                    mapnodes.Add(mapnode);
                }
                var list = new List<ClientMapNode>(8);
                ForEachNodes(mapnodes, e =>
                {
                    list.Clear();
                    var index = mapnodes.IndexOf(e);
                    if (index != mapnodes.Count - 1)
                    {
                        list = mapnodes.GetRange(index + 1, mapnodes.Count - index - 1);
                    }

                    e.nexts = list.ToArray();
                    waypoints.Add(new NavMeshWayPoint(e));
                    
                });

                for (int i = 0;i< mapnodes.Count - 1;i++)
                {
                    waypoints[i].LinkNext(waypoints[i + 1]);
                }

                return waypoints[0];

            }

            return null;
        }
        public bool TryGetVoxelDownRange(Vector3 pos, out float downward)
        {
            throw new System.NotImplementedException();
        }

        
        public bool TryGetVoxelUpRange(Vector3 pos, out float upward)
        {
            
            var _unitypos = pos.ConvertToRealUnityPos(TerrainHeight);
             _unitypos.y +=  0.1f;
//            cubeobj.transform.position = _unitypos;
            var downhit = Physics.Raycast(_unitypos, UnityEngine.Vector3.down,out RaycastHit hitInfo, 400, layMasks);
            if (downhit)
            {

                var point = hitInfo.point;
                var zonepos = BattleUtils.UnityPos2RealZonePos(TotalHeight,point);
                upward = zonepos.Z;
                return true;
            }

            upward = float.NegativeInfinity;
            return false;
        }

        public bool TryGetVoxelTopRange(Vector3 pos, out float top)
        {
            var _unitypos = pos.ConvertToRealUnityPos(TerrainHeight);
//            _unitypos.y += ClientUnityObject.ScreenOffset.y;
            var uphit = Physics.Raycast(_unitypos, UnityEngine.Vector3.up, out RaycastHit hitInfo,400, layMasks);
            if (uphit)
            {
                var zonepos = BattleUtils.UnityPos2RealZonePos(TotalHeight,hitInfo.point);
                top = zonepos.Z ;
                return true;
            }
            top = float.MaxValue;
            return false;
        }
     
        private static string[] layname = {"SelectableUnit","SelfLayer","CharacterUnlit"};
        private bool b_ReadyToFly;

        private static bool isPlayerLayer(GameObject obj)
        {
            LayerMask lm = LayerMask.GetMask(layname);
            if ((lm.value & (int)Mathf.Pow(2,obj.layer)) == (int)Mathf.Pow(2,obj.layer))
            {
                return true;
            }

            return false;
        }

        public ILayerWayPoint ConverToLayerWayPoint(List<UnityEngine.Vector3> NavPathPoints)
        {
           return NavMeshClientWayPoint.CreateFromVoxel(GenNavMeshWayPoint(NavPathPoints)); 
        }
        public ILayerWayPoint FindAirPath(Vector3 src, Vector3 dst)
        {
            var srctounitypos = src.ConvertToUnityPos(TerrainHeight);
            var dsttounitypos = dst.ConvertToUnityPos(TerrainHeight);

            if (m_System == null)
            {
                return null;
            }
            List<UnityEngine.Vector3> NavPathPoints = new List<UnityEngine.Vector3>();
            if (clientFlyPath != null)
            {
                NavPathPoints = clientFlyPath.ComputePath(srctounitypos,dsttounitypos);
              if (NavPathPoints == null || NavPathPoints.Count == 0)
              {
                  NavPathPoints.Add(srctounitypos);
              }
              
            }
            return NavMeshClientWayPoint.CreateFromVoxel(GenNavMeshWayPoint(NavPathPoints)); 
        }
        public static List<UnityEngine.Vector3> GetRoadPoint(ILayerWayPoint waypoint,float TotalHeight)
        {
            var points = new List<UnityEngine.Vector3>();
            if (waypoint != null)
            {
                var postion = waypoint.Position.ConvertToUnityPos(TotalHeight);
                points.Add(postion);
                var curway = waypoint;
                while (curway.Next != null)
                {
                    var _postion = curway.Next.Position.ConvertToUnityPos(TotalHeight);
                    points.Add(_postion);
                    curway = curway.Next;
                }

            }

            else
            {
                return null;
            }
            return points;
        }
        private List<UnityEngine.Vector3> event_FindGroundPath(UnityEngine.Vector3 startpos, UnityEngine.Vector3 endpos)
        {
            var src = BattleUtils.UnityPos2ZonePos(TotalHeight,startpos);
            var dst = BattleUtils.UnityPos2ZonePos(TotalHeight,endpos);
            List<UnityEngine.Vector3> NavPathPoints = new List<UnityEngine.Vector3>();
            m_CanFly = true;
            var waypoint = FindPath(src, dst);
            if (waypoint != null)
            {
                NavPathPoints = GetRoadPoint(waypoint,TotalHeight);
            }

            return NavPathPoints;

        }

        public void SetReadyToFly(bool _readytofly)
        {
            b_ReadyToFly = _readytofly;
        }
        public ILayerWayPoint FindPath(Vector3 src, Vector3 dst)
        {
            var srctounitypos = src.ConvertToUnityPos(TerrainHeight);
            var dsttounitypos = dst.ConvertToUnityPos(TerrainHeight);
           

            if (m_System == null)
            {
                return null;
            }

            srctounitypos.y += m_System.GetNavMeshParams().m_cellHeight;
            dsttounitypos.y += m_System.GetNavMeshParams().m_cellHeight;
            List<UnityEngine.Vector3> NavPathPoints = new List<UnityEngine.Vector3>();
            if (!Physics.Linecast(srctounitypos,dsttounitypos))
            {
                NavPathPoints.Add(dsttounitypos);
                return NavMeshClientWayPoint.CreateFromVoxel(GenNavMeshWayPoint(NavPathPoints)); 
            }
            
            
            var haspos = m_System.GetClosestPointOnNavMeshForUnity(srctounitypos,FindPathDistance);
            if (haspos.Item1)
            {
                srctounitypos = haspos.Item2;
            }
            else
            {
                if (b_ReadyToFly && clientFlyPath != null)
                {
                    var retEnd = clientFlyPath.GetTopBottom(dsttounitypos, clientFlyPath.CheckNavMaxHeight);
                    var targetBottomPos = retEnd.Item2;
                    if (targetBottomPos.Equals( UnityEngine.Vector3.positiveInfinity))
                    {
                        return null;
                    }
                    dsttounitypos = targetBottomPos;
                }
            }
            
            var m_SmoothPath = RcdtcsUnityUtils.ComputeSmoothPath(m_System.m_navQuery, srctounitypos, dsttounitypos);
            if (m_SmoothPath != null && m_SmoothPath.m_smoothPath != null && m_SmoothPath.m_nsmoothPath >= 1 && m_SmoothPath.m_smoothPath.Length > 3)
            {
                if (m_SmoothPath.m_nsmoothPath == 1)
                {
                    var path = m_SmoothPath.m_smoothPath;
                    var a = new UnityEngine.Vector3( path[0], path[1], path[2]);
                    NavPathPoints.Add(a);
                }
                else
                {
                    var path = m_SmoothPath.m_smoothPath;
                    for (int i = 1; i < m_SmoothPath.m_nsmoothPath;++i) {
                        int v = i * 3;
                        var a = new UnityEngine.Vector3( path[v-3], path[v-2], path[v-1]);
                        //var b = new UnityEngine.Vector3( path[v+0], path[v+1], path[v+2]);
                        NavPathPoints.Add(a);
                        if (i == m_SmoothPath.m_nsmoothPath - 1)
                        {
                            a = new UnityEngine.Vector3( path[v+0], path[v+1], path[v+2]);
                            NavPathPoints.Add(a);
                        }
                    }
                    var lastPoint = NavPathPoints[NavPathPoints.Count - 1];

                    if (!lastPoint.Equals(dsttounitypos))
                    {
                        var endpos = dsttounitypos;
                        endpos.y += m_System.GetNavMeshParams().m_cellHeight;
                        if (!Physics.Linecast(lastPoint,endpos,layMasks))
                        {
                            NavPathPoints.Add(dsttounitypos);
                            lastPoint = dsttounitypos;
                        }
                    }
                    
                    
                    var lpVec2 = new UnityEngine.Vector2(lastPoint.x,lastPoint.z);
                    var dstVec2 = new UnityEngine.Vector2(dsttounitypos.x,dsttounitypos.z);
                    float distance =  UnityEngine.Vector2.Distance(lpVec2,dstVec2);
                    var dis3D = UnityEngine.Vector3.Distance(lastPoint,dsttounitypos);
                    if (!b_ReadyToFly)
                    {
                        //寻路修正 高度单独处理修正
                        if ((dis3D > 0f && Mathf.Abs(lastPoint.y - dsttounitypos.y) < 1f && distance <= FindPathDistance))
                        {
                            var point1 = lastPoint;
                            var point2 = dsttounitypos;
                            point1.y -= 0.5f;
                            point2.y -= 0.5f;
                            var ray = new Ray(point1, point2 - point1);
                            var hits = Physics.RaycastAll(ray, distance);
                            bool hasobstacle = false;
                            if (hits != null)
                            {
                                foreach (var hit in hits)
                                {
                                    if (hit.transform != null && !isPlayerLayer(hit.transform.gameObject))
                                    {
                                        hasobstacle = true;
                                        break;
                                    }
                                }
                                if(!hasobstacle)
                                    NavPathPoints.Add(dsttounitypos);
//                                else
//                                {
//                                    return null;
//                                }
                            }
//                            else
//                            {
//                                return null;
//                            }
                        }

                        else
                        {
//                            var dis = UnityEngine.Vector3.Distance(lastPoint,dsttounitypos);
//                            if (dis > FindPathDistance) //地面寻路专用
//                            {
//                                return null;
//                            }
                            
                        }
                    }
                    
                }
                
               
                return NavMeshClientWayPoint.CreateFromVoxel(GenNavMeshWayPoint(NavPathPoints)); 
                
            }
            

            return null;

        }

        public bool TouchMapByPos(LayerUnit u, Vector3 pos)
        {
            var tarunitypos = pos.ConvertToUnityPos(TerrainHeight) ;
            var curunitypos = u.Position.ConvertToUnityPos(TerrainHeight);
            
            bool blocked = false;
            var Sidehit = preUnitObj.IsSidehit(curunitypos,tarunitypos);
            var isSidehit = Sidehit.Item1;
            if (isSidehit && UnityEngine.Vector3.Distance(curunitypos,Sidehit.Item2)<=0.5f)
            {
                blocked = true;
            }
            return blocked; //world.Terrain.TryIntersectMapByPos(pos, out var layer);
        }
        public bool TryMoveTo(ref Vector3 pos)
        {
            var _unitypos = pos.ConvertToUnityPos(TerrainHeight);

            var Sidehit = preUnitObj.IsSidehit(preUnitObj.currentUnityPos,_unitypos);
            var isSidehit = Sidehit.Item1;
            var blocked = false;
            if (isSidehit && UnityEngine.Vector3.Distance(_unitypos,Sidehit.Item2)<=0.5f)
            {
                blocked = true;
            }
            var tophit = preUnitObj.IsTophit(_unitypos);
            var istophit = tophit.Item1;
            if (istophit && UnityEngine.Vector3.Distance(_unitypos,tophit.Item2)<= StepIntercept)
            {
                blocked = true;
            }
            var bottomhit = preUnitObj.IsBottomhit(_unitypos);
            var isbottomhit = bottomhit.Item1;
            if (isbottomhit && UnityEngine.Vector3.Distance(_unitypos,bottomhit.Item2)<= StepIntercept)
            {
                _unitypos.y = bottomhit.Item2.y;
                pos = BattleUtils.UnityPos2ZonePos(TerrainHeight, _unitypos);
            }

            return blocked; //world.Terrain.TryMoveTo(ref pos, out var layer);
        }

       
        public virtual ILayerUnitPosition CreateUnitPosition(LayerUnit unit)
        {
            if (unit is LayerPlayer)
            {
                return new ClientPlayerPosition(unit,TotalWidth,TotalHeight,StepIntercept,GridCellSize,0.3f);
            }
            else
            {
                return new ClientUnitPostion(unit,TotalWidth,TotalHeight,StepIntercept,GridCellSize,0.3f);
            }
        }

        protected class ClientUnitPostion : ILayerUnitPosition
        {
            public float X => vobj.X;
            public float Y => vobj.Y;
            public float Z => vobj.Z;
            public float Upward => vobj.Upward;

            public Vector3 Position => vobj.Position;
            public float SpeedZ
            {
                get => vobj.SpeedZ;
                set => vobj.SpeedZ = value;
            }

            public bool IsInAir => vobj.IsMidair;

            public float Gravity
            {
                get => vobj.Gravity;
                set => vobj.SetGravity(value);
            }
            protected readonly LayerUnit unit;
            protected readonly ClientUnityObject vobj;

            private float StepIntercept;
            //单位，地图高度 ，台阶高度，格子尺寸，碰撞半径
            public ClientUnitPostion(LayerUnit unit,float totalwidth,float totalHeigt,float stepIntercept,float gridcellsize,float radius = 0.5f)
            {
                this.unit = unit;
                this.vobj = new ClientUnityObject(totalwidth,totalHeigt,stepIntercept,unit.BodyHeight,gridcellsize,radius,unit.Parent.Gravity);
                this.StepIntercept = stepIntercept;
            }

            public void SetPos(float x, float y, float z)
            {
                vobj.Transport(new Vector3(x, y, z));
            }

            public void SetPos(Vector3 target)
            {
                vobj.Transport(target);
            }
             
            public virtual bool FixLerp(Vector3 remotePos, float amount)
            {
                
                var src = vobj.Position;

                if (Math.Abs(remotePos.X - src.X) < 0.01f && Math.Abs(remotePos.Y - src.Y) < 0.01f && Mathf.Abs(src.Z - remotePos.Z)<= 0.2f)//精度有点问题
                {
                    return true;
                }

                var ret = Vector3.Lerp(src, remotePos, amount);
                if (unit.CurrentState == UnitActionStatus.Jump)//黑科技一下？
                {
                    vobj.Transport(new Vector3(ret.X,ret.Y,src.Z));
                }

                //其他玩家
                if (unit.Info.UType == UnitInfo.UnitType.TYPE_PLAYER && unit.SyncInfo.HasPlayerUUID && Math.Abs(remotePos.X - src.X) < 0.01f && Math.Abs(remotePos.Y - src.Y) < 0.01f && Mathf.Abs(src.Z - remotePos.Z)<= StepIntercept)
                {
                      return true;
                }
                
                else //npc为什么会有playeruuid了？暂时没时间跟踪
                {
                    
                    if (Mathf.Abs(src.Z - remotePos.Z)<= StepIntercept)
                    {
                        vobj.Transport(new Vector3(ret.X, ret.Y, src.Z));
                    }
                    else
                    {
                        if (unit.Info.UType == UnitInfo.UnitType.TYPE_PLAYER)
                        {
                            vobj.Transport(new Vector3(ret.X, ret.Y, src.Z));
                        }
                        else
                            vobj.Transport(ret);
                    }

                    if (unit.Info.UType == UnitInfo.UnitType.TYPE_NPC || !unit.SyncInfo.HasPlayerUUID)
                    {
                        return true;
                    }
                    
                }
                
//                else
//                {
//                    vobj.Transport(ret);
//                }
                return Math.Abs(ret.X - src.X) < 0.01f && Math.Abs(ret.Y - src.Y) < 0.01f && Mathf.Abs(src.Z - ret.Z)<= StepIntercept;
            }

            public virtual void Update(int intervalMS)
            {
                vobj.Update(intervalMS);
                
            }

            public void StartJump(float zspeed, float gravity, float zlimit)
            {
                vobj.SetGravity(gravity);
                vobj.Jump(zspeed);
            }

            public void Fly(float zoffset)
            {
                FlyTo(this.Z + zoffset);
            }
            public void FlyTo(float dz)
            {
                // dz = Math.Max(dz, vobj.Upward);
                // dz = Math.Min(dz, currentLayer.Top);
                vobj.Z = dz;
            }     

        }
        //--------------------------------------------------------------------------------------------------------
        protected class ClientPlayerPosition : ClientUnitPostion,ILayerPlayerPosition
        {

            private float AsyncUnitPosModifyMinRange = 20f;
            //单位，地图高度 ，台阶高度，格子尺寸，碰撞半径
            public ClientPlayerPosition(LayerUnit unit,float totalwidth,float totalHeigt,float stepIntercept,float gridcellsize,float radius = 0.5f):base(
                 unit, totalwidth,totalHeigt, stepIntercept, gridcellsize, radius )
            {
                AsyncUnitPosModifyMinRange = unit.Parent.AsyncUnitPosModifyMinRange;

            }
            
            public void Move(float addX, float addY)
            {
                var src = vobj.Position;
                src.X += addX;
                src.Y += addY;
                vobj.Transport(src);
            }

            public TryMoveToMapBorderResult TryMoveToMapBorder(float addX, float addY)
            {
                var result = vobj.TryMoveToNext3D(new Vector2(addX, addY), true);
                //Debug.Log("result========="+result);
                switch (result)
                {
                    case VoxelObject.MoveResult.Cross:
                    case VoxelObject.MoveResult.Arrive:
                        return TryMoveToMapBorderResult.ARRIVE;
                    case VoxelObject.MoveResult.Blocked:
                        return TryMoveToMapBorderResult.BLOCK;
                    case VoxelObject.MoveResult.TouchX:
                    case VoxelObject.MoveResult.TouchY:
                        return TryMoveToMapBorderResult.TOUCH;
                }
                return TryMoveToMapBorderResult.ARRIVE;
            }

            public override void Update(int intervalMS)
            {
                if (unit is LayerPlayer p && p.IsReady)
                {
                    vobj.Update(intervalMS);
                }
            }

        }
        
        //--------------------------------------------------------------------------------------------------------
        
        }
        public class NavMeshClientWayPoint : ILayerWayPoint
        {
            private NavMeshWayPoint p;
            public NavMeshClientWayPoint(NavMeshWayPoint p)
            {
                this.p = p;
            }

            public void LinkNext(NavMeshClientWayPoint next)
            {
                this.Next = next;
                next.Prev = this;
//                (Tail as NavMeshClientWayPoint).Next = next;
//                next.Prev = (Tail as NavMeshClientWayPoint);
            }
            public static NavMeshClientWayPoint CreateFromVoxel(NavMeshWayPoint p)
            {
                if (p == null)
                {
                    return null;
                }
                var start = new NavMeshClientWayPoint(p);
                var wp = start;
                while (p.Next != null)
                {
                    p = p.Next;
                    var next = new NavMeshClientWayPoint(p);
                    wp.Next = next;
                    next.Prev = wp;
                    wp = next;
                }
                return start;
            }
            public ILayerWayPoint Next { get; private set; }
            public ILayerWayPoint Prev { get; private set; }
            public ILayerWayPoint Tail { get { var wp = this as ILayerWayPoint; while (wp.Next != null) { wp = wp.Next; } return wp; } }
            public float X { get => p.X; }
            public float Y { get => p.Y; }
            public float Z { get => p.Z; }
            public Vector3 Position { get => p.Position; }
            public bool PosEquals(ILayerWayPoint w)
            {
                return p.PosEquals((w as NavMeshClientWayPoint).p);
            }
            public float GetTotalDistance()
            {
                return p.GetTotalDistance();
            }
            //--------------------------------------------------------------------------------------------------------
        }

    }
}