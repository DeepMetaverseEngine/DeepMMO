using System;
using System.Collections.Generic;
using DeepCore.Astar;
using DeepCore.Game3D.Helper;
using DeepCore.Game3D.Slave;
using DeepCore.Game3D.Slave.Helper;
using DeepCore.Game3D.Slave.Layer;
using DeepCore.Game3D.Voxel;
using DeepCore.GameData.Data;
using DeepCore.GameData.Zone;
using DeepCore.GameData.Zone.ZoneEditor;
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

        public override float GetFatherG(IMapNode target)
        {
            var tt = target as ClientMapNode;
            return Vector3.Distance(Position, tt.Position);
        }

        public override float GetTargetH(IMapNode father)
        {
            var ft = father as ClientMapNode;
            return Vector3.Distance(Position, ft.Position);
        }

        public override void Dispose()
        {

        }

        public override int CloseAreaIndex
        {
            get => this.areaLinkValue;
            protected set { areaLinkValue = value; }
        }
        public override object Tag { get; set; }
        public override IMapNode[] Nexts { get { return nexts; } }
    }
    public class NavMeshClientTerrain3D : ILayerZoneTerrain
    {
        private float TerrainWidth;
        private float TerrainHeight;
        private float TerrainGridCellSize;
        protected float mStepIntercept;
        public float StepIntercept => mStepIntercept;
        private string[] laymaskNames =
        {
                "NavLayer",
                "Water"
            };

        private int layMasks;
        private ClientUnityObject preUnitObj;
        private float Height = 2f;// 碰撞高度
        protected float FindPathDistance;//最近路点采样距离
        private RcdtcsUnityUtils.SystemHelper m_System = new RcdtcsUnityUtils.SystemHelper();
        public ClientFlyPath clientFlyPath;
        private float _LandOffset = 0.5f;

        public float LandOffSet
        {
            get { return _LandOffset; }
            set
            {
                _LandOffset = value;
                if (clientFlyPath != null)
                {
                    clientFlyPath.LandOffset = value;
                }
            }
        }

        public virtual int CreateLayerMask(string[] layers)
        {
            return LayerMask.GetMask(layers);
        }
        //        public GameObject cubeobj;
        public NavMeshClientTerrain3D(byte[] PathFindData, SceneData sceneData, float stepIntercept, float findPathDistance = 3f)
        {
            TerrainWidth = sceneData.Voxel.VoxelXCount;
            TerrainHeight = sceneData.Voxel.VoxelYCount;
            TerrainGridCellSize = sceneData.Voxel.VoxelGridCellW;
            mStepIntercept = stepIntercept;
            layMasks = CreateLayerMask(laymaskNames);
            if (preUnitObj == null)
            {
                preUnitObj = new ClientUnityObject(sceneData, stepIntercept, Height, TerrainGridCellSize);
            }

            FindPathDistance = findPathDistance;


            if (PathFindData != null)
            {
                m_System.ComputeSystem(PathFindData, 0);
            }

            clientFlyPath = new ClientFlyPath(m_System, layMasks, FindPathDistance)
            {
                LandOffset = LandOffSet,
                event_FindGroundPath = event_FindGroundPath
            };

            //            cubeobj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //            cubeobj.GetComponent<Collider>().enabled = false;

        }


        public bool GetPolyHeight(UnityEngine.Vector2 pos, ref float height)
        {
            if (m_System == null) return false;

            return m_System.GetPolyHeight(pos, ref height);
        }
        public UnityEngine.Vector3 FixPos(UnityEngine.Vector3 pos)
        {
            if (clientFlyPath != null)
            {
                return clientFlyPath.FixPos(pos);
            }

            return pos;
        }
        public UnityEngine.Vector3 UpdateAirMove(UnityEngine.Vector3 startPos, float speed, ref List<UnityEngine.Vector3> path)
        {
            if (clientFlyPath != null)
            {
                startPos = clientFlyPath.Update(startPos, speed);
                path = clientFlyPath.GetPath();
            }
            return startPos;
        }

        public float TotalWidth => TerrainWidth;
        public float TotalHeight => TerrainHeight;
        public float GridCellSize => TerrainGridCellSize;

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

        public void ForEachNodes<T>(List<T> mapNode, Action<T> action)
        {
            foreach (var _mapnode in mapNode)
            {
                if (action != null)
                {
                    action(_mapnode);
                }
            }
        }

        protected virtual NavMeshWayPoint GenNavMeshWayPoint(List<UnityEngine.Vector3> path)
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

                for (int i = 0; i < mapnodes.Count - 1; i++)
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
            _unitypos.y += 0.1f;
            //            cubeobj.transform.position = _unitypos;
            var downhit = Physics.Raycast(_unitypos, UnityEngine.Vector3.down, out RaycastHit hitInfo, 400, layMasks);
            if (downhit)
            {

                var point = hitInfo.point;
                var zonepos = BattleUtils.UnityPos2RealZonePos(TotalHeight, point);
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
            var uphit = Physics.Raycast(_unitypos, UnityEngine.Vector3.up, out RaycastHit hitInfo, 400, layMasks);
            if (uphit)
            {
                var zonepos = BattleUtils.UnityPos2RealZonePos(TotalHeight, hitInfo.point);
                top = zonepos.Z;
                return true;
            }
            top = float.MaxValue;
            return false;
        }

        private static string[] layname = { "SelectableUnit", "SelfLayer", "CharacterUnlit" };
        private bool b_ReadyToFly;

        private static bool isPlayerLayer(GameObject obj)
        {
            LayerMask lm = LayerMask.GetMask(layname);
            if ((lm.value & (int)Mathf.Pow(2, obj.layer)) == (int)Mathf.Pow(2, obj.layer))
            {
                return true;
            }

            return false;
        }

        public ILayerWayPoint ConverToLayerWayPoint(List<UnityEngine.Vector3> NavPathPoints)
        {
            return NavMeshClientWayPoint.CreateFromVoxel(GenNavMeshWayPoint(NavPathPoints));
        }
        public virtual ILayerWayPoint FindAirPath(Vector3 src, Vector3 dst)
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
                NavPathPoints = clientFlyPath.ComputePath(srctounitypos, dsttounitypos);
                if (NavPathPoints == null || NavPathPoints.Count == 0)
                {
                    NavPathPoints.Add(srctounitypos);
                }

            }
            return NavMeshClientWayPoint.CreateFromVoxel(GenNavMeshWayPoint(NavPathPoints));
        }
        public static List<UnityEngine.Vector3> GetRoadPoint(ILayerWayPoint waypoint, float TotalHeight)
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
            var src = BattleUtils.UnityPos2ZonePos(TotalHeight, startpos);
            var dst = BattleUtils.UnityPos2ZonePos(TotalHeight, endpos);
            List<UnityEngine.Vector3> NavPathPoints = new List<UnityEngine.Vector3>();
            m_CanFly = true;
            var waypoint = FindPath(src, dst);
            if (waypoint != null)
            {
                NavPathPoints = GetRoadPoint(waypoint, TotalHeight);
            }

            return NavPathPoints;

        }

        public void SetReadyToFly(bool _readytofly)
        {
            b_ReadyToFly = _readytofly;
        }
        public virtual ILayerWayPoint FindPath(Vector3 src, Vector3 dst)
        {
            var srctounitypos = src.ConvertToUnityPos(TerrainHeight);
            var dsttounitypos = dst.ConvertToUnityPos(TerrainHeight);


            if (m_System == null)
            {
                return null;
            }

            var dirStartUnitypos = srctounitypos;
            var dirEndUnitypos = dsttounitypos;
            dirStartUnitypos.y += StepIntercept / 2;
            dirEndUnitypos.y += StepIntercept / 2;
            List<UnityEngine.Vector3> NavPathPoints = new List<UnityEngine.Vector3>();
            var dir = dirEndUnitypos - dirStartUnitypos;
            var dis = UnityEngine.Vector3.Distance(dirStartUnitypos, dirEndUnitypos);
            if (!clientFlyPath.CheckBoxCast(dirStartUnitypos.CurrentUnityPos2GlobalPos(), dir.normalized, out RaycastHit hit1, dis, layMasks))
            {
                NavPathPoints.Add(dirStartUnitypos);
                NavPathPoints.Add(dsttounitypos);
                return NavMeshClientWayPoint.CreateFromVoxel(GenNavMeshWayPoint(NavPathPoints));
            }
            // if (!Physics.Linecast(dirStartUnitypos.CurrentUnityPos2GlobalPos(),dirEndUnitypos.CurrentUnityPos2GlobalPos(),layMasks))
            // {
            //     NavPathPoints.Add(dsttounitypos);
            //     return NavMeshClientWayPoint.CreateFromVoxel(GenNavMeshWayPoint(NavPathPoints)); 
            // }

            srctounitypos.y += m_System.GetNavMeshParams().m_cellHeight;
            dsttounitypos.y += m_System.GetNavMeshParams().m_cellHeight;
            var haspos = m_System.GetClosestPointOnNavMeshForUnity(srctounitypos, FindPathDistance);
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
                    if (targetBottomPos.Equals(UnityEngine.Vector3.positiveInfinity))
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
                    var a = new UnityEngine.Vector3(path[0], path[1], path[2]);
                    NavPathPoints.Add(a);
                }
                else
                {
                    var path = m_SmoothPath.m_smoothPath;
                    for (int i = 1; i < m_SmoothPath.m_nsmoothPath; ++i)
                    {
                        int v = i * 3;
                        var a = new UnityEngine.Vector3(path[v - 3], path[v - 2], path[v - 1]);
                        //var b = new UnityEngine.Vector3( path[v+0], path[v+1], path[v+2]);
                        NavPathPoints.Add(a);
                        if (i == m_SmoothPath.m_nsmoothPath - 1)
                        {
                            a = new UnityEngine.Vector3(path[v + 0], path[v + 1], path[v + 2]);
                            NavPathPoints.Add(a);
                        }
                    }
                    var lastPoint = NavPathPoints[NavPathPoints.Count - 1];

                    if (!lastPoint.Equals(dsttounitypos))
                    {
                        var endpos = dsttounitypos;
                        endpos.y += m_System.GetNavMeshParams().m_cellHeight;
                        if (!Physics.Linecast(lastPoint.CurrentUnityPos2GlobalPos(), endpos.CurrentUnityPos2GlobalPos(), layMasks))
                        {
                            NavPathPoints.Add(dsttounitypos);
                            lastPoint = dsttounitypos;
                        }
                    }


                    var lpVec2 = new UnityEngine.Vector2(lastPoint.x, lastPoint.z);
                    var dstVec2 = new UnityEngine.Vector2(dsttounitypos.x, dsttounitypos.z);
                    float distance = UnityEngine.Vector2.Distance(lpVec2, dstVec2);
                    var dis3D = UnityEngine.Vector3.Distance(lastPoint, dsttounitypos);
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
                                if (!hasobstacle)
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
            var tarunitypos = pos.ConvertToUnityPos(TerrainHeight);
            var curunitypos = u.Position.ConvertToUnityPos(TerrainHeight);

            bool blocked = false;
            var Sidehit = preUnitObj.IsSidehit(curunitypos, tarunitypos);
            var isSidehit = Sidehit.Item1;
            if (isSidehit && UnityEngine.Vector3.Distance(curunitypos, Sidehit.Item2) <= 0.5f)
            {
                blocked = true;
            }
            return blocked; //world.Terrain.TryIntersectMapByPos(pos, out var layer);
        }
        public bool TryMoveTo(ref Vector3 pos)
        {
            var _unitypos = pos.ConvertToUnityPos(TerrainHeight);

            var Sidehit = preUnitObj.IsSidehit(preUnitObj.currentUnityPos, _unitypos);
            var isSidehit = Sidehit.Item1;
            bool blocked = isSidehit && UnityEngine.Vector3.Distance(_unitypos, Sidehit.Item2) <= 0.5f;
            var tophit = preUnitObj.IsTophit(_unitypos);
            var istophit = tophit.Item1;
            if (istophit && UnityEngine.Vector3.Distance(_unitypos, tophit.Item2) <= StepIntercept)
            {
                blocked = true;
            }
            var bottomhit = preUnitObj.IsInWater(_unitypos);
            if (!bottomhit.Item1)
            {
                bottomhit = preUnitObj.IsBottomhit(_unitypos);
            }

            var isbottomhit = bottomhit.Item1;
            if (isbottomhit && UnityEngine.Vector3.Distance(_unitypos, bottomhit.Item2) <= StepIntercept)
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
                return new ClientPlayerPosition(unit, StepIntercept, GridCellSize, 0.3f);
            }
            else
            {
                return new ClientUnitPostion(unit, StepIntercept, GridCellSize, 0.3f);
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
            protected readonly NavMeshClientTerrain3D world;
            private float StepIntercept;

            private float WaterStandDistance;
            //单位，地图高度 ，台阶高度，格子尺寸，碰撞半径
            public ClientUnitPostion(LayerUnit unit, float stepIntercept, float gridcellsize, float radius = 0.5f)
            {
                this.unit = unit;
                this.vobj = new ClientUnityObject(unit.Parent.Data, stepIntercept, unit.BodyHeight, gridcellsize, radius, unit.Parent.Gravity);
                this.world = unit.Parent.Terrain3D as NavMeshClientTerrain3D;
                this.StepIntercept = stepIntercept;
            }

            public void SetWaterStandDistance(float value)
            {
                WaterStandDistance = value;
                if (this.vobj != null)
                {
                    this.vobj.WaterStandDistance = value;
                }

            }
            public void SetPos(float x, float y, float z)
            {
                vobj.Transport(new Vector3(x, y, z));
            }

            public void SetPos(Vector3 target)
            {
                vobj.Transport(target);
            }

            public virtual bool FixLerp(in Vector3 remotePos, float amount)
            {

                var src = vobj.Position;

                var ret = Vector3.Lerp(src, remotePos, amount);
                var fixHeight = StepIntercept;
                if (unit.Info.UType == UnitInfo.UnitType.TYPE_PLAYER)
                {
                    fixHeight = StepIntercept / 3;
                }
                if (Mathf.Abs(src.Z - remotePos.Z) <= fixHeight)//精度有点问题
                {
                    ret.Z = Math.Max(ret.Z, src.Z);
                }
                vobj.Transport(ret);

                return Math.Abs(ret.X - src.X) < 0.01f && Math.Abs(ret.Y - src.Y) < 0.01f && Mathf.Abs(src.Z - ret.Z) <= StepIntercept;
            }

            public virtual bool Update(in Vector3 remotePos, int intervalMS)
            {
                // if (unit.Info.UType == UnitInfo.UnitType.TYPE_NPC || !unit.SyncInfo.HasPlayerUUID)
                // {
                //     return;
                // }

                //在半空中
                //                     if (unit.RemoteMidAir > 0)
                //                     {
                //                         vobj.Update(intervalMS);
                //                     }
                //                     //在上升趋势
                //                     else if (vobj.SpeedZ > 0)
                //                     {
                //                         vobj.Update(intervalMS);
                //                     }
                //                     else
                //                     {
                //                         FixLerp(in remotePos, 0.1f);
                //                     }
                vobj.Update(intervalMS, this.unit);
                return true;

            }

            /// <summary>
            /// 修正本地坐标//
            /// </summary>
            public virtual bool FixPos(in DeepCore.Geometry.Vector3 remote_pos, int intervalMS)
            {
                //ILayerUnitPosition local_pos = mLocalPos;
                if (unit.MoveSpeedSEC == 0)
                {
                    this.SetPos(remote_pos.X, remote_pos.Y, remote_pos.Z);
                    return true;
                }
                //float fdistance2D = Geometry.Vector2.Distance(this.Position, remote_pos);
                float fdistance = DeepCore.Geometry.Vector3.Distance(this.Position, remote_pos);
                if (fdistance > 0)
                {
                    float dspeed = MoveHelper.GetDistance(intervalMS, unit.MoveSpeedSEC);
                    if (fdistance >= unit.Parent.AsyncUnitPosModifyMaxRange)
                    {
                        SetPos(remote_pos.X, remote_pos.Y, remote_pos.Z);
                        return true;
                    }
                    else
                    {
                        if (unit.CurrentState.IsControlMoveable())
                        {
                            return FixLerp(in remote_pos, Math.Min(dspeed / fdistance, 1f));
                            // MathVector.moveTo(, remote_pos.X, remote_pos.Y, db);
                        }
                        else
                        {
                            return FixLerp(in remote_pos, 1);
                            //MathVector.moveTo(local_pos, remote_pos.X, remote_pos.Y, db);
                        }
                    }
                }
                return true;
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
        protected class ClientPlayerPosition : ClientUnitPostion, ILayerPlayerPosition
        {

            private float AsyncUnitPosModifyMinRange = 20f;

            private float StepIntercept;
            //单位，地图高度 ，台阶高度，格子尺寸，碰撞半径
            public ClientPlayerPosition(LayerUnit unit, float stepIntercept, float gridSize, float radius = 0.5f) : base(
                 unit, stepIntercept, gridSize, radius)
            {
                AsyncUnitPosModifyMinRange = unit.Parent.AsyncUnitPosModifyMinRange;
                StepIntercept = stepIntercept;
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
                    case VoxelObject.MoveResult.MoveSmooth:
                    case VoxelObject.MoveResult.MoveCross:
                    case VoxelObject.MoveResult.MoveArrived:
                        return TryMoveToMapBorderResult.ARRIVE;
                    case VoxelObject.MoveResult.Blocked:
                        return TryMoveToMapBorderResult.BLOCK;
                    case VoxelObject.MoveResult.MoveTouchX:
                    case VoxelObject.MoveResult.MoveTouchY:
                        return TryMoveToMapBorderResult.TOUCH;
                }
                return TryMoveToMapBorderResult.ARRIVE;
            }

            public override bool FixLerp(in Vector3 remotePos, float amount)
            {
                var src = vobj.Position;

                var ret = Vector3.Lerp(src, remotePos, amount);
                if (Math.Abs(remotePos.X - src.X) < 0.01f && Math.Abs(remotePos.Y - src.Y) < 0.01f && Mathf.Abs(src.Z - remotePos.Z) <= 0.2f)//精度有点问题
                {
                    return true;
                }

                if (unit.CurrentState == UnitActionStatus.Jump)//黑科技一下？
                {
                    vobj.Transport(new Vector3(ret.X, ret.Y, src.Z));
                }


                if (Mathf.Abs(src.Z - remotePos.Z) <= StepIntercept)
                {
                    vobj.Transport(new Vector3(ret.X, ret.Y, src.Z));
                }
                else
                {
                    vobj.Transport(ret);
                }


                return true;


            }

            public override bool Update(in Vector3 remotePos, int intervalMS)
            {
                if (unit is LayerPlayer p && p.IsReady)
                {
                    base.Update(in remotePos, intervalMS);
                }
                return true;
            }

        }

        //--------------------------------------------------------------------------------------------------------

    }

}