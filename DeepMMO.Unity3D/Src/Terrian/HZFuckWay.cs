using DeepCore.GameData.Zone;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using DeepCore.Game3D.Helper;
using DeepCore.Game3D.Slave;
using DeepCore.Game3D.Slave.Agent;
using DeepCore.Game3D.Slave.Layer;
using DeepCore.GameData.Helper;
using DeepCore.GameData.Zone.ZoneEditor;
using DeepCore.Vector;
using UnityEngine;
using UnityEngine.AI;
using Debug = UnityEngine.Debug;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace DeepMMO.Unity3D.Terrain
{


    /// <summary>
    /// 按照策划预先设置好的路线走路
    /// </summary>
    public class HZFuckWay : AbstractMoveAgent
    {
        private bool mFinish = false;
        public float EndDistance { get; set; }
        public object UserData { get; set; }

        public override bool IsEnd
        {
            get
            {
                return mNavPathPoints == null;
            }
        }

        public override bool IsDuplicate
        {
            get { return false; }
        }

        public override ILayerWayPoint WayPoints
        {
            get
            {
                if (way_points != null && way_points.Count > 0)
                {
                    return way_points[0];
                }
                return null;
            }
        }

        public override bool IsFinish
        {
            get { return mFinish; }
        }

        //private LayerEditorPoint start_point;
//        private LayerEditorPoint end_point;
        private Predicate<LayerEditorPoint> select;
        //private ILayerWayPoint way_points;
        //todo 做多段寻路
        private List<ILayerWayPoint> way_points = new List<ILayerWayPoint>();
        private float cur_dir = 0;
        private bool auto_adjust;
        private Vector3 targetpos;
        private Vector3 cur_pos = Vector3.zero;
        private List<Vector3> mNavPathPoints;
        // 传送区域
        public List<RegionData> mRegionDatas;
        private bool b_wait;
        private bool b_waitFLy;
        private bool b_hasTransport;
        private bool m_FlyAbility;
        private bool m_IsFlying;
        private bool b_ReadyToFly = false;
        public float LandOffset = 0.5f;
        private Vector3 LastPos;
       
        public enum FlyState
        {
            None,
            ReadyToFly,
            WaitToFly,
            Flying,
            Failure,
            
        }
        private FlyState m_CurFlyState = FlyState.None;
        public delegate FlyState BeginHandle();
        public delegate FlyState FlyStateHandle(FlyState state);
        public event BeginHandle event_BeginFlyHandle;
        public event FlyStateHandle event_FlyStateHandle;
        public bool isCrossMap = false;
        public HZFuckWay(
            Vector3 targetpos,
            bool _FlyAbility,
            bool isCrossMap = false,
            float endDistance = 0.05f,
            Predicate<LayerEditorPoint> select = null,
            bool autoAdjust = true,
            object ud = null)
        {
            this.auto_adjust = autoAdjust;
            this.EndDistance = endDistance;
            this.UserData = ud;
            this.targetpos = targetpos;
            this.select = select;
            this.m_FlyAbility = _FlyAbility;
            this.isCrossMap = isCrossMap;

        }

        public void SetFlyHandle(BeginHandle _event_BeginFlyHandle,FlyStateHandle _event_FlyStateHandle)
        {
            this.event_BeginFlyHandle = _event_BeginFlyHandle;
            this.event_FlyStateHandle = _event_FlyStateHandle;
        }
        
      
        protected override void OnInit(LayerPlayer actor)
        {
            this.Owner.OnDoEvent += Owner_OnDoEvent;
            this.OnEnd += ActorMoveAgent_OnEnd;
            this.Start();
        }

        private void ActorMoveAgent_OnEnd(AbstractAgent agent)
        {
            if (this.Owner != null)
            {
                this.Owner.OnDoEvent -= Owner_OnDoEvent;
            }
        }

        protected override void OnDispose()
        {
            this.Owner.OnDoEvent -= Owner_OnDoEvent;
            base.OnDispose();
            mNavPathPoints = null;
        }


        private void Owner_OnDoEvent(LayerObject obj, ObjectEvent e)
        {
            //这里能不能给个reason
            if (e is UnitForceSyncPosEvent)
            {
                //this.Stop();
                if (b_hasTransport)
                {
                  b_wait = true;
                }
            }
        }


        private NavMeshWayPoint.NavMeshClientWayPoint NewFindPath(DeepCore.Geometry.Vector3 beginpos, DeepCore.Geometry.Vector3 endpos)
        {
            var clientTerrain = Layer.Terrain3D as NavMeshWayPoint.NavMeshClientTerrain3D;
            ILayerWayPoint points = null;
            if (m_IsFlying)//已经在飞行
            {
                clientTerrain.SetReadyToFly(false);
                clientTerrain.LandOffSet = this.LandOffset;
                points = clientTerrain.FindAirPath(beginpos, endpos);
                
            }
            else
            {
                clientTerrain.SetReadyToFly(b_ReadyToFly);
                points = Layer.Terrain3D.FindPath(beginpos, endpos);
            }
               

            return points as NavMeshWayPoint.NavMeshClientWayPoint;
        }

        private ILayerWayPoint GetWay_Points(DeepCore.Geometry.Vector3 beginpos, DeepCore.Geometry.Vector3 endpos)
        {
            
            ILayerWayPoint points = null;
            if (!auto_adjust) //直接寻路
            {
                points = NewFindPath(beginpos, endpos);
            }
            else
            {

                bool hasFindPath = true;
                bool isStraight = false;
                WayPointAstar.FlagGraphPath wp_path = null;
             

                LayerEditorPoint startFlag_point = null;
                LayerEditorPoint endFlag_point = null;
                //开始段
                if (hasFindPath && select != null)
                {
                    startFlag_point =  Layer.GetNearZoneFlag<LayerEditorPoint>(beginpos, select);
                    if (startFlag_point == null)
                    {
                        //Debug.Log("start_point == null");
                        hasFindPath = false;
                    }
                    else
                    {
                        
                        //结尾段
                        endFlag_point = Layer.GetNearZoneFlag<LayerEditorPoint>(endpos, select);
                        if (endFlag_point == null)
                        {
                            //Debug.Log("end_point == null");
                            hasFindPath = false;
                        }
                        else if (startFlag_point == endFlag_point) //开始结尾一致
                        {
                            hasFindPath = false;
                        }
                        else
                        {
                            wp_path = Layer.FindPathWayPoint(startFlag_point.Name, endFlag_point.Name);
                            if (wp_path == null)
                            {
                                hasFindPath = false;
                            }

                            wp_path = OptimizePath(wp_path);
                        }
                    }
                }

                if (!hasFindPath )
                {
                    if (!isStraight)
                    {
                        points = NewFindPath(beginpos, endpos);
//                        points = Layer.Terrain3D.FindPath(beginpos, endpos);
                    }
                   
                }
                else
                {

                    var zonepos = new DeepCore.Geometry.Vector3(wp_path.Data.X, wp_path.Data.Y, wp_path.Data.Z);
                    var navwaypoint = NewFindPath(beginpos, zonepos) ;//Layer.Terrain3D.FindPath(beginpos, zonepos) as NavMeshWayPoint.NavMeshClientWayPoint;
                    if (navwaypoint != null)
                    {
                        if (Layer.Terrain3D is NavMeshWayPoint.NavMeshClientTerrain3D)
                        {
                            var pos = new List<DeepCore.Geometry.Vector3>();
                            zonepos = new DeepCore.Geometry.Vector3(wp_path.Data.X, wp_path.Data.Y, wp_path.Data.Z);
                            pos.Add(zonepos);
                            var curway = wp_path;
                            while (curway.Next != null)
                            {
                                var _postion = new DeepCore.Geometry.Vector3(curway.Next.Data.X, curway.Next.Data.Y, curway.Next.Data.Z);
                                pos.Add(_postion);
                                curway = curway.Next;
                            }

                            var isbreak = false;
                            for (int i = 0; i < pos.Count - 1; i++)
                            {
                                var navpoint = NewFindPath(pos[i], pos[i + 1]);//Layer.Terrain3D.FindPath(pos[i], pos[i + 1]) as NavMeshWayPoint.NavMeshClientWayPoint;
                                if (navpoint != null)
                                {
                                    navwaypoint.LinkNext(navpoint);
                                }
                                else
                                {
                                    isbreak = true;
                                    break;
                                }

                            }

                            if (isbreak)
                            {
                                navwaypoint = NewFindPath(beginpos, endFlag_point.Position);//Layer.Terrain3D.FindPath(beginpos, endFlag_point.Position) as NavMeshWayPoint.NavMeshClientWayPoint;
                            }

                        }

                        var end_points = NewFindPath(endFlag_point.Position, endpos);//Layer.Terrain3D.FindPath(endFlag_point.Position, endpos) as NavMeshWayPoint.NavMeshClientWayPoint;
                        if (end_points != null)
                        {
                            navwaypoint.LinkNext(end_points);
                        }



                        points = navwaypoint;

                    }
                    else
                    {
                        points = NewFindPath(beginpos,endpos); //Layer.Terrain3D.FindPath(beginpos, endpos);
                    }
                    
                }
            }

            return points;
        }

        private Tuple<float,ILayerWayPoint> Distance(DeepCore.Geometry.Vector3 beginpos,DeepCore.Geometry.Vector3 endpos)
        {
            var waypoint = GetWay_Points(beginpos,endpos);
            if (waypoint == null)
            {
                return new Tuple<float, ILayerWayPoint>(-1f,null);
            }
            return new Tuple<float,ILayerWayPoint>(waypoint.GetTotalDistance(),waypoint);
        }


        private Tuple<List<Vector3>,List<ILayerWayPoint>> CalculatePath(Vector3 startpos,Vector3 endpos)
        {
            var start_pos = BattleUtils.UnityPos2ZonePos(Owner.Parent.Terrain3D.TotalHeight, startpos);
            var target_pos = BattleUtils.UnityPos2ZonePos(Owner.Parent.Terrain3D.TotalHeight, endpos);
            //Debug.Log("target_pos==========="+FixPos(targetpos));
            var tarMaxdis = 99999f;
            ILayerWayPoint beginlayerwp = null;
            ILayerWayPoint endlaywp = null;
            //var stopwatch = Stopwatch.StartNew();
            if (mRegionDatas != null && mRegionDatas.Count > 0)//传送点判断
            {
                foreach (var regionData in mRegionDatas)
                {
                    var pos = new DeepCore.Geometry.Vector3(regionData.X, regionData.Y, regionData.Z);
                    foreach (var abilityData in regionData.Abilities)
                    {
                        if (abilityData is UnitTransportAbilityData tp)
                        {
                            if (string.IsNullOrEmpty(tp.NextPosition))
                            {
                                continue;
                            }
                            //stopwatch.Reset();
                            var pathbegin = Distance(start_pos, pos);
                            
                            //Debug.Log("pathbegin "+pos+" cost time" + stopwatch.ElapsedMilliseconds / 1000f);
                            if (pathbegin.Item1 == -1)
                            {
                                continue;
                            }
                            if (tp.AcceptForceForAll || 
                                (!tp.AcceptForceForAll 
                                 && tp.AcceptForce == Owner.Force))
                            {
                                var flag = Layer.GetFlag(tp.NextPosition);
                                
                                if (flag != null)
                                {
                                    //stopwatch.Reset();
                                    var pathend = Distance(flag.Position,target_pos);
                                    //Debug.Log("pathend "+ flag.Position +" cost time" + stopwatch.ElapsedMilliseconds / 1000f);
                                    
                                    if (pathend.Item1 == -1)
                                    {
                                        continue;
                                    }
                                    if (pathbegin.Item1 + pathend.Item1 < tarMaxdis)
                                    {
                                        tarMaxdis = pathend.Item1 + pathbegin.Item1;
                                        beginlayerwp = pathbegin.Item2;
                                        endlaywp = pathend.Item2;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            //stopwatch.Reset();
            var distance = Distance(start_pos,target_pos);
            var _way_points = new List<ILayerWayPoint>();
            var _navPathPoints = new List<Vector3>();
            //Debug.Log("orgpathfind cost time" + stopwatch.ElapsedMilliseconds / 1000f);
            if (distance.Item1 != -1 && distance.Item1 < tarMaxdis)
            {
                _way_points.AddRange(IWayPointToList(distance.Item2));
            }
            else if(beginlayerwp != null && endlaywp != null)
            {
                _way_points.AddRange(IWayPointToList(beginlayerwp));
                _way_points.AddRange(IWayPointToList(endlaywp));
                b_hasTransport = true;
            }

            if (_way_points != null && _way_points.Count > 0)
            {
                _navPathPoints = NavMeshWayPoint.NavMeshClientTerrain3D.GetRoadPoint(_way_points[0],Owner.Parent.Terrain3D.TotalHeight);
            }
            return new Tuple<List<Vector3>, List<ILayerWayPoint>>(_navPathPoints,_way_points);
            
        }

        private List<ILayerWayPoint> IWayPointToList(ILayerWayPoint wp)
        {
            var list = new List<ILayerWayPoint>();
            if (wp == null)
            {
                return list;
            }
           
            var curwp = wp;
            list.Add(curwp);
            while (curwp.Next != null)
            {    
                list.Add(curwp.Next);
                curwp = curwp.Next;
            }

            return list;
        }
        private float GetEndDistance(Vector3 point)
        {
            var start2d = new Vector2(float.Parse(point.x.ToString("f2")),float.Parse(point.z.ToString("f2")));
            var tartpos2d = new Vector2(float.Parse(targetpos.x.ToString("f2")),float.Parse(targetpos.z.ToString("f2")));

            var dis = Vector2.Distance(start2d, tartpos2d);//Vector3.Distance(point, targetpos);
            Debug.Log("GetEndDistance"+dis);
            if (Math.Abs(point.y - targetpos.y) <= 1f)
            {
              return dis;
            }
            return Vector3.Distance(point, targetpos);
        }
        /// <summary>
        /// 再次开始
        /// </summary>
        public bool Start()
        {
            var stopwatch = Stopwatch.StartNew();
            this.m_IsFlying = Owner.IsZeroGravityFly;
           // UnityEngine.Debug.Log("targetpos1" + targetpos);
            targetpos = FixPos(targetpos);
//            UnityEngine.Debug.Log("targetpos2" + targetpos);
            var ret = CalculatePath(Owner.GetUnityPos(),targetpos);
            
            mNavPathPoints = ret.Item1;
            way_points = ret.Item2;
            var startpos = Owner.GetUnityPos();
            if (mNavPathPoints.Count > 0)
            {
                startpos = mNavPathPoints[mNavPathPoints.Count - 1];
            }
            var dis = GetEndDistance(startpos);
            if (dis <= EndDistance + 0.1f)
            {
                stopwatch.Stop();
                Debug.Log("cost time ====== "+stopwatch.ElapsedMilliseconds / 1000f);
                return false;
            }
            if (m_FlyAbility && !m_IsFlying)//地面寻路 接空中寻路
            {
                m_IsFlying = true;
                targetpos = FixPos(targetpos);
                var ret1 = CalculatePath(startpos,targetpos);
                var _NavPathPoints = ret1.Item1;
                if (_NavPathPoints.Count > 0 && GetEndDistance(_NavPathPoints[_NavPathPoints.Count - 1]) <= EndDistance )
                {
                    b_ReadyToFly = true;
                    m_CurFlyState = FlyState.ReadyToFly;
                    m_IsFlying = false;
                   
                }
                else
                {
                    StopAutoRun();
                    return false;
                }
                
            }
            else
            {
                if (dis > EndDistance)
                {
                    StopAutoRun();
                    return false;
                }

            }
            stopwatch.Stop();
            Debug.Log("cost time ====== "+stopwatch.ElapsedMilliseconds / 1000f);
            return true;
        }


        //路线优化
        private WayPointAstar.FlagGraphPath OptimizePath(WayPointAstar.FlagGraphPath navwaypoint)
        {

            if (navwaypoint.Next == null)
            {
                return navwaypoint;
            }

            var curwaypoint = navwaypoint;
      
            var srcUnitypos = Owner.Position.ConvertToUnityPos(Owner.Parent.Terrain3D.TotalHeight);

            var forward = (targetpos - srcUnitypos).normalized;
            while (curwaypoint.Next != null) //把能直接走到的点都拿出来 然后找最近的那个
            {
                var pos = new DeepCore.Geometry.Vector3(curwaypoint.Data.X, curwaypoint.Data.Y, curwaypoint.Data.Z);
                var curwayUnitypos = pos.ConvertToUnityPos(Owner.Parent.Terrain3D.TotalHeight);

                if (!NavMesh.Raycast(srcUnitypos, curwayUnitypos, out NavMeshHit hit, 1))
                {
                    var dir = (curwayUnitypos - srcUnitypos).normalized;
                    var ret = Vector3.Dot(forward, dir);
                    if (ret >= 0)
                    {
                        return curwaypoint;
                    }
                }
                else
                {
                    break;
                }

                curwaypoint = curwaypoint.Next;
            }

            return curwaypoint;


        }

        /// <summary>
        /// 外部打断寻路.
        /// </summary>
        public void Stop()
        {
            if (way_points != null)
            {
               way_points.Clear();
            }
            
            mNavPathPoints = null;
        }


        public Vector2 Get3DTo2DZonePostion(Vector3 pos)
        {
            var nextposZonePos = BattleUtils.UnityPos2ZonePos(Owner.Parent.Terrain3D.TotalHeight, pos);
            return new Vector2(nextposZonePos.X, nextposZonePos.Y);
        }

        private Vector3 FixPos(Vector3 pos)
        {
            var _targetpos = pos;
            if (m_IsFlying)
            {
                var clientTerrain = Layer.Terrain3D as NavMeshWayPoint.NavMeshClientTerrain3D;
                _targetpos = clientTerrain.FixPos(pos);
               
            }

            return _targetpos;
        }
        
        private DeepCore.Geometry.Vector3 FixPos(DeepCore.Geometry.Vector3 pos)
        {
            var _targetpos = pos;
            if (m_IsFlying)
            {
                var clientTerrain = Layer.Terrain3D as NavMeshWayPoint.NavMeshClientTerrain3D;
                var unitypos = pos.ConvertToUnityPos(Layer.Terrain3D.TotalHeight);
                unitypos = clientTerrain.FixPos(unitypos);
                _targetpos = BattleUtils.UnityPos2ZonePos(Layer.Terrain3D.TotalHeight,unitypos);
            }

            return _targetpos;
        }
        private bool checkTargetDistance()
        {
          
            float distance = Vector3.Distance(cur_pos, targetpos);
                
            if (distance <= (m_IsFlying?0.2f:EndDistance))
            {
                return true;
            }

            return false;
        }

        private void FixMove(ref Vector2 srcpos,Vector2 targetpos,float length)
        {
            float nextdistance = Vector2.Distance(srcpos, targetpos);
            if (MathVector3D.moveTo(ref srcpos, targetpos, length))
            {
                if ((nextdistance <= length || nextdistance <= 0.1f)
                    && mNavPathPoints != null && mNavPathPoints.Count > 0)
                {
                    mNavPathPoints.RemoveAt(0);
                    if (mNavPathPoints.Count == 0)
                    {
                        if (!b_hasTransport && !b_ReadyToFly)
                        {
                            mFinish = true;
                            Stop();
                        }
                        Owner.SendUnitAxisAngle(0, 0, cur_dir);
                        return;
                    }

                    targetpos = Get3DTo2DZonePostion(mNavPathPoints[0]);
                    FixMove(ref srcpos, targetpos, length - nextdistance);

                }
            }
        }

        private void StopAutoRun()
        {
            mFinish = true;
            Stop();
        }
        
        protected override void BeginUpdate(int intervalMS)
        {
            if (!Owner.IsReady)
            {
                return;
            }
            this.m_IsFlying = Owner.IsZeroGravityFly;
            cur_dir = Owner.Direction;
            cur_pos = Owner.GetUnityPos();


            if (mNavPathPoints.Count > 0)
            {
                #if UNITY_EDITOR
                for (int i = 1; i < mNavPathPoints.Count; i++)
                {
                    Debug.DrawLine(mNavPathPoints[i-1].CurrentUnityPos2GlobalPos(),mNavPathPoints[i].CurrentUnityPos2GlobalPos(),Color.red);
                }
                #endif
            }
            if (checkTargetDistance())
            {
                StopAutoRun();
                return;
            }
            
            if (m_CurFlyState == FlyState.ReadyToFly && (mNavPathPoints == null || mNavPathPoints.Count == 0))
            {
                b_ReadyToFly = false;
                targetpos = FixPos(targetpos);
                var ret = CalculatePath(cur_pos + LandOffset*Vector3.up ,targetpos);
                mNavPathPoints = ret.Item1;
                way_points = ret.Item2;
                if (mNavPathPoints == null || mNavPathPoints.Count == 0 ||event_BeginFlyHandle == null)
                {
                    StopAutoRun();
                    return;
                }
                
                m_CurFlyState = event_BeginFlyHandle();
                if (m_CurFlyState == FlyState.Failure)
                {
                    StopAutoRun();
                }
                

            }
            else if (m_CurFlyState >= FlyState.WaitToFly && m_CurFlyState < FlyState.Flying)
            {
                if (event_FlyStateHandle != null)
                {
                    m_CurFlyState = event_FlyStateHandle(m_CurFlyState);
                    if (m_CurFlyState == FlyState.Failure)
                    {
                        StopAutoRun();
                    }
                    
                }
                else
                {
                    StopAutoRun();
                }
            }
            else
            if ( !b_wait && (mNavPathPoints == null || mNavPathPoints.Count == 0))
            {
                if (b_hasTransport)
                {
                    Owner.SendUnitAxisAngle(cur_dir, 0, cur_dir);
                }
                
            }
            else
            {
                if (b_wait)
                {
                    b_wait = false;
                    b_hasTransport = false;
                    if (way_points.Count > 1)
                    {
                        way_points.RemoveAt(0);
                        mNavPathPoints = NavMeshWayPoint.NavMeshClientTerrain3D.GetRoadPoint(way_points[0],Owner.Parent.Terrain3D.TotalHeight);
                    }
                }
                
              
                float length = MoveHelper.GetDistance(intervalMS, Owner.MoveSpeedSEC);

             
                var ownerpos = Owner.Position;
                var nextpos = cur_pos;
                if (m_IsFlying)
                {
                  
                    var clientTerrain = Layer.Terrain3D as NavMeshWayPoint.NavMeshClientTerrain3D;
                    var _targetpos = targetpos;
                    var pos = clientTerrain.UpdateAirMove(cur_pos,length,ref mNavPathPoints);
                    //Fix3DMove(ref cur_pos, nextpos, length);
                    //Debug.Log("cur_pos3========"+curpos2d );
                    if (mNavPathPoints != null && mNavPathPoints.Count > 0)
                    {
                        nextpos = mNavPathPoints[0];
                    }
                    
                    var zonepos = BattleUtils.UnityPos2ZonePos(Owner.Parent.Terrain3D.TotalHeight, pos);
//                    var dis = MathVector3D.Get2DDistance(zonepos.X, zonepos.Y, ownerpos.X, ownerpos.Y);
                    float targetdistance = Vector3.Distance(cur_pos, _targetpos);
                    if (targetdistance <= 0.05f)
                    {
                        mFinish = true;
                        Stop();
                    }
                    else
                    {
                       
                        var newzonepos = BattleUtils.UnityPos2ZonePos(Owner.Parent.Terrain3D.TotalHeight,
                            nextpos);
                        var dir = MathVector.getDegree(ownerpos.X, ownerpos.Y, newzonepos.X, newzonepos.Y);
                        if (dir != 0)
                        {
                            cur_dir = dir;
                        }
                        
                        var dxy = DeepCore.Geometry.Vector2.Distance(ownerpos, zonepos);
                        var maxSpeedZ = (zonepos.Z - ownerpos.Z)*1000f / intervalMS;
                        var maxSpeedXY = (dxy)*1000f / intervalMS;
                        var distance = maxSpeedXY == 0 ? 0 : 1;
                        if (targetdistance > 1 && maxSpeedZ == 0 && distance == 0)
                        {//异常处理
                            StopAutoRun();
                            return;
                        }
                        //Debug.Log(" nextpos3d=========="+nextpos+" maxSpeedXY= "+maxSpeedXY + " maxSpeedZ= "+maxSpeedZ +" distance="+distance);
                        Owner.SendUnit3DAxisAngle(cur_dir,distance, cur_dir,maxSpeedZ, maxSpeedXY);
// 底层3daxis实现--------------------------
                        // var absSpeed = Math.Min(Math.Abs(axis3D.ZControlSpeed), MoveSpeedSEC);
                        // var zSpeed = CMath.GetDirect(axis3D.ZControlSpeed) * absSpeed;
                        // ZeroGravityFly.ZMove(zSpeed, intervalMS);
                        // if (axis3D.distance != 0)
                        // {
                        //     var xySpeed = axis3D.XYControlSpeed == 0 ? MoveSpeedSEC : axis3D.XYControlSpeed;
                        //     xySpeed = Math.Min(xySpeed, MoveSpeedSEC);
                        //     MoveBlockTo(axis3D.angle, CMath.GetDirect(axis3D.distance) * xySpeed, intervalMS);
                        // }
//-----------------------------------------
                    }
                }
                else
                {
                    nextpos = mNavPathPoints[0];
                    var curpos2d = new Vector2(Owner.Position.X, Owner.Position.Y);
                    var nextpos2d = Get3DTo2DZonePostion(nextpos); //nextpos.GetToZonePosSimpleNumberVector2(Owner.Parent.Terrain3D.TotalHeight);

                   

                    FixMove(ref curpos2d, nextpos2d, length);
                    //Debug.Log("cur_pos3========"+curpos2d );
                    var pos = curpos2d;
                   
                    var dis = MathVector3D.Get2DDistance(pos.x, pos.y, ownerpos.X, ownerpos.Y);
                    if (dis <= 0.05f) //精度修正
                    {
                        pos = nextpos2d;
                        //Debug.Log(" nextpos2=========="+pos);
                    }
                    
                  
                    cur_dir = MathVector.getDegree(ownerpos.X, ownerpos.Y, pos.x, pos.y);
                    Owner.SendUnitAxisAngle(cur_dir, length, cur_dir);
                    
                }

                var _dis = Vector3.Distance(LastPos, cur_pos);
                if (_dis<0.001f)
                { 
                    Start();
                }
                else
                {
                    LastPos = cur_pos;
                }


            }

        }

    }

}