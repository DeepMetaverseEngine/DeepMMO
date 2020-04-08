using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using DeepCore;
using DeepMMO.Unity3D;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

public class ClientFlyPath 
{
    // Start is called before the first frame update

    public LayerMask m_LayerMask;

    //查找地面最大高度
    [Tooltip("最大高度，这个范围内检测最近地表")] public float CheckNavMaxHeight = 500;
    [Tooltip("最近距离点，这个范围内检测目标最近点")]public float m_FindPathDistance = 3;
    [Tooltip("最小高度，这个范围内遇到障碍物不可寻路")] public float CheckNavMinHeight = 10;
    [Tooltip("角度可用范围，这个范围内遇到障碍物不可寻路")] public float CheckMinErrorAngle = 60;
    public float CheckMaxErrorAngle = 120;
    [Tooltip("检测次数")] public int CheckMaxTime = 50;
    [Tooltip("检测半径")] public float CheckRadius = 1f;
    [Tooltip("检测碰撞宽度")] public float CheckWidth = 0.5f;
    [Tooltip("检测碰撞高度")] public float CheckHeight = 2f;
    [Tooltip("检测步长")] public float Step = 3f;
    [Tooltip("优化路点")] public bool isOptimization = true;
    [Tooltip("迭代路点，当寻路不到位置时多次迭代")] public int IterTimes = 4;
    private RcdtcsUnityUtils.SystemHelper m_System ;
    [Tooltip("移动速度")] public float MoveSpeed = 0.6f;
    [Tooltip("地面高度")] public float LandOffset = 0.5f;
    private string m_strObjPath;
    private Vector3 m_EndPos = Vector3.positiveInfinity;
    private Vector3 m_StartPos = Vector3.positiveInfinity;
    private RcdtcsUnityUtils.SmoothPath m_SmoothPath;

    public Vector3 Offset = new Vector3(0, 1.5f, 0);

    private List<Vector3> m_Path = new List<Vector3>();
    private List<int> m_HitList = new List<int>();
    public enum PathType
    {
        Smooth,
        Straight,
    }

    public Object DataFile;
    public PathType pathtype = PathType.Smooth;
    private RcdtcsUnityUtils.StraightPath m_straightPath;
    private bool m_BeginFind = true;
    private float distance;
    private MoveStateType _moveState;

    public MoveStateType MoveState
    {
        get { return _moveState; }
    }

    public enum MoveStateType
    {
        None,
        Computing,
        Failure,
        Moving,
        End,
    }

    private List<Vector3> m_MovePoint = new List<Vector3>();

    private Dictionary<Vector3, List<Vector3>> m_PointDirList = new Dictionary<Vector3, List<Vector3>>();
    private Dictionary<Vector3, List<Vector3>> m_ComputeDirList = new Dictionary<Vector3, List<Vector3>>();
    private Vector3 m_DynamicPos = Vector3.positiveInfinity; //移动中碰撞开辟新路点
    
    public delegate List<Vector3> FindGroundPath(Vector3 startpos,Vector3 endpos);
    public FindGroundPath event_FindGroundPath;
    private bool Test = false;

    public ClientFlyPath(RcdtcsUnityUtils.SystemHelper _systemhelper,LayerMask _layerMask,float _FindPathDistance)
    {
        m_System = _systemhelper;
        m_LayerMask = _layerMask;
        m_FindPathDistance = _FindPathDistance;
    }
    public Vector3 CheckMoveNewDir(Vector3 pos, Vector3 end, List<Vector3> dirlist)
    {
        pos = pos.CurrentUnityPos2GlobalPos();
        end = end.CurrentUnityPos2GlobalPos();
        RaycastHit hit;
        var dir = Vector3.back;
        if (end.y > pos.y)
        {
            if (!dirlist.Contains(Vector3.up) && !Physics.Raycast(pos, Vector3.up, out hit, MoveSpeed, m_LayerMask))
            {
                dir = Vector3.up;
            }
        }
        else if (end.y < pos.y)
        {
            if (!dirlist.Contains(Vector3.down) && !Physics.Raycast(pos, Vector3.down, out hit, MoveSpeed, m_LayerMask))
            {
                dir = Vector3.down;
            }
        }
        else if (end.x < pos.x)
        {
            if (!dirlist.Contains(Vector3.left) && !Physics.Raycast(pos, Vector3.left, out hit, MoveSpeed, m_LayerMask))
            {
                dir = Vector3.left;
            }
        }
        else if (end.x > pos.x)
        {
            if (!dirlist.Contains(Vector3.right) && !Physics.Raycast(pos, Vector3.right, out hit, MoveSpeed, m_LayerMask))
            {
                dir = Vector3.right;
            }
        }

        if (!dirlist.Contains(Vector3.up) && !Physics.Raycast(pos, Vector3.up, out hit, MoveSpeed, m_LayerMask))
        {
            dir = Vector3.up;
        }
        else if (!dirlist.Contains(Vector3.left) && !Physics.Raycast(pos, Vector3.left, out hit, MoveSpeed, m_LayerMask))
        {
            dir = Vector3.left;
        }
        else if (!dirlist.Contains(Vector3.right) &&
                 !Physics.Raycast(pos, Vector3.right, out hit, MoveSpeed, m_LayerMask))
        {
            dir = Vector3.right;
        }
        else if (!dirlist.Contains(Vector3.down) && !Physics.Raycast(pos, Vector3.down, out hit, MoveSpeed, m_LayerMask))
        {
            dir = Vector3.down;
        }

        return dir;
    }

    //垂直向量
    public Vector3 GetVerticalDir(Vector3 dir)
    {
        if (dir.z == 0)
        {
            return new Vector3(0, dir.y, -1);
        }
        else
        {
            return new Vector3(-dir.z / dir.x, dir.y, 1).normalized;
        }
    }

    private Vector3 UpdateMove()
    {
        if (MoveState != MoveStateType.Moving)
        {
            return m_StartPos;
        }


        var pos = UpdateAirMove();

        
        var dir = pos - m_StartPos;
        var moveStep = MoveSpeed;
        var dis = Vector3.Distance(pos, m_StartPos);
        if (dis <= MoveSpeed)
        {
            moveStep = dis;
            
        }
            
//        Debug.Log("pos ="+ pos +" position"+this.transform.position );
        pos = m_StartPos + dir.normalized * moveStep;
        if (!GetContainPos(m_MovePoint, pos))
        {
            m_MovePoint.Add(pos);
        }
        

        return pos;
    }
    private Vector3 UpdateAirMove()
    {
        var dis = 0f;
        RaycastHit hit;
        var pos = m_StartPos;
        var enddis = Vector3.Distance(m_StartPos, m_EndPos);
        if (enddis > MoveSpeed && CheckAround(m_StartPos, m_StartPos, out Vector3 Closestpos, out Collider trans))
        {
            if (!m_HitList.Contains(trans.transform.GetInstanceID()))
            {
                m_Path = RecomputePath(m_StartPos, m_EndPos, Step);
                m_HitList.Add(trans.transform.GetInstanceID());
            }
        }
        var targetdis = Vector3.Distance(m_StartPos, m_EndPos);
        int Count = m_Path.Count;
        if (Count == 0 && targetdis > 0.3f)
        {
            m_Path = RecomputePath(m_StartPos, m_EndPos, Step);
            if (m_Path.Count == 0)
            {
                _moveState = MoveStateType.Failure;
                return pos;
            }
        }
        if (!m_DynamicPos.Equals(Vector3.positiveInfinity))
        {
            pos = m_DynamicPos;
            dis = Vector3.Distance(m_DynamicPos, m_StartPos);
            if (dis < 0.1f)
            {
                m_DynamicPos = Vector3.positiveInfinity;
                if (m_Path.Count == 0)
                {
                    m_Path = RecomputePath(m_StartPos, m_EndPos, MoveSpeed);
                }
                else
                {
                    m_Path = RecomputePath(m_StartPos, m_EndPos, Step);
                }

                
            }
        }
        else
        {
            Count = m_Path.Count;
            if (Count > 0)
            {
                var curPos = m_Path[0];
                while (Count > 0)
                {
                    pos = m_Path[0];
                    dis = Vector3.Distance(m_StartPos, pos);
					                   
                    if (dis <= MoveSpeed + 0.2f ||  GetContainPos(m_MovePoint, pos))
                    {
                        m_Path.RemoveAt(0);
                        Count--;
                        
                    }
                    else
                    {
                        break;
                    }
                }
            }
           
            

          
        }
        
        
//        var moveStep = MoveSpeed;
//        int CheckTime = 0;
       
//        var isTouch = CheckAround(m_StartPos,pos, out Vector3 closepos, out collider);
//        if (isTouch)
//        {
//            var newdir1 = GetVerticalDir(closepos - m_StartPos);
//            var newdir2 = m_EndPos - m_StartPos;
//            var newdir = newdir1 + newdir2;
//            pos = m_StartPos + newdir.normalized * moveStep;
//
//            RaycastHit hit;
//            
//            if (collider.bounds.Contains(pos))
//            {
//                newdir = -newdir1 + newdir2.normalized;
//                pos = m_StartPos + newdir.normalized * moveStep;
//            }
//
//           
////            if (TestFlyObject != null)
////            {
////                var Sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
////                Sphere.transform.position = pos;
////                Sphere.transform.parent = TestFlyObject.transform;
////                
////            }
//
//            m_DynamicPos = pos;
//        }
        
        
        return pos;
    }

    public List<Vector3> GetPath()
    {
        return m_Path;
    }
    public Vector3 Update(Vector3 startpos,float speed)
    {
        
        if (MoveState == MoveStateType.End)
        {
            return startpos;
        }

        if (m_System == null)
        {
            return startpos;
        }

        MoveSpeed = speed;
        m_StartPos = startpos;
       


//        if (Physics.Linecast(m_StartPos, m_EndPos, out RaycastHit hit, m_LayerMask))
//        {
//            Debug.DrawRay(hit.point, hit.normal * 10, Color.green);
//            Debug.DrawRay(hit.point, -hit.normal * 10, Color.cyan);
//            var dir = m_EndPos - m_StartPos;
//            var newdir = -hit.normal + dir.normalized;
//            Debug.DrawRay(m_StartPos, newdir * 10, Color.magenta);
//        }

        //Debug.DrawRay(m_StartPos, m_EndPos - m_StartPos, Color.yellow);
//        Debug.DrawRay(m_EndPos, Vector3.down * CheckNavMaxHeight, Color.red);
      

        var pos = UpdateMove();
        return pos;
    }

    public bool GetContainPos(List<Vector3> vector3list, Vector3 pos)
    {
        if (vector3list.Count == 0)
        {
            return false;
        }

        return vector3list.Contains(pos);
    }


    public class FlyPath
    {
        public Vector3 path;
        public Vector3 dir = Vector3.zero;
        public FlyPath next;
        public FlyPath prev;
    }

    public void CheckPath(FlyPath srcPath, Vector3 dirforward, List<Vector3> pointList, Vector3 endPos, int checkTime,
        float steplength, ref bool result)
    {
        if (checkTime >= CheckMaxTime)
        {
            result = false;
            return;
        }

//      var orgpos = srcPath.path;
        var startpos = srcPath.path;
        var dir = endPos - startpos;
        dir.Normalize();

        RaycastHit hit;
        var flypath = new FlyPath {prev = srcPath};
        var movestep = steplength;
        var distance = Vector3.Distance(endPos, startpos);
        var forward = dirforward;

        if(!Physics.Linecast(startpos.CurrentUnityPos2GlobalPos(),endPos.CurrentUnityPos2GlobalPos(),m_LayerMask))
        {
            var dis = Vector3.Distance(startpos, endPos);
            var fixTimeLimit = (int)(dis / steplength);
            var fixtime = 0;
            var tempos = startpos;
            if (fixTimeLimit > 0)
            {
                srcPath.next = flypath;
                while (fixtime < fixTimeLimit)
                {
                    var tempfly = new FlyPath {prev = flypath};
                    tempos = tempos + dir * steplength;
                    flypath.path = tempos;
                    flypath.next = tempfly;
                    flypath = tempfly;
                    fixtime++;
                }
                flypath.path = endPos;
            }
            else
            {
                if (!flypath.path.Equals(endPos))
                {
                    flypath.path = endPos;
                    srcPath.next = flypath;
                }
            }
            
            result = true;
            return;
        }

        var dirret = GetNewDir(startpos, dir, forward, movestep + CheckRadius + CheckWidth, true);
        dir = dirret.Item2;

        if (dir == Vector3.zero)
        {
//            flypath.path = endPos;
//            srcPath.next = flypath;
            
            result = false;
            return;
        }


        if (Physics.Raycast(startpos.CurrentUnityPos2GlobalPos(), dir.normalized, out hit, movestep + CheckRadius + CheckWidth, m_LayerMask))
        {
            var dis = Vector3.Distance(startpos.CurrentUnityPos2GlobalPos(), hit.point);
            if (movestep + CheckRadius + CheckWidth > dis)
            {
                movestep = dis - CheckRadius - CheckWidth;
            }
            AddHitAround(hit.collider);
        }

        if (distance < movestep)
        {
            movestep = distance;
        }

        var pos = startpos + dir.normalized * movestep;
        var changeTimes = 0;
        while (GetContainPos(m_MovePoint, pos))
        {
            if (!this.m_PointDirList.ContainsKey(pos))
            {
                this.m_PointDirList.Add(pos, new List<Vector3>());
            }

            var dirlist = this.m_PointDirList[pos];
            var newdir = CheckMoveNewDir(startpos, m_EndPos, dirlist);
            if (!dirlist.Contains(newdir))
            {
                dirlist.Add(newdir);
                pos = startpos + newdir * movestep;
            }
            else
            {
                //理论上这里应该是返回上一个点 这样就会变成又一个递归 先跳出循环处理
                changeTimes++;
                if (changeTimes > 5)
                {
                    break;
                }
            }
        }

        if (CheckAround(startpos,pos, out Vector3 Closestpos, out Collider trans))
        {
            AddHitAround(trans);
            var tempdir = (startpos - Closestpos).normalized;
            pos = Closestpos + tempdir * (CheckWidth + CheckRadius);
        }
        

        dirforward = dir.normalized;

        distance = Vector3.Distance(endPos, startpos);

        if (distance <= 0.3f)
        {
            flypath.path = endPos;
            srcPath.next = flypath;
            result = true;
            return;
        }

    
        startpos = CheckPoint(pointList, srcPath, pos, ref dirforward, movestep + CheckWidth + CheckRadius);


        flypath.path = startpos;
        flypath.dir = dirforward;

        pointList.Add(startpos);
        checkTime++;
        CheckPath(flypath, dirforward, pointList, endPos, checkTime, movestep, ref result);
        srcPath.next = flypath;
    }

    private void AddHitAround(Collider trans)
    {
        if (!m_HitList.Contains(trans.GetInstanceID()))
        {
            m_HitList.Add(trans.GetInstanceID());
        }
    }

    // 判断周围有没有障碍物
    private bool CheckAround(Vector3 startpos,Vector3 orgpos, out Vector3 Closestpos, out Collider collider)
    {
        startpos = startpos.CurrentUnityPos2GlobalPos();
        orgpos = orgpos.CurrentUnityPos2GlobalPos();
        collider = null;
        Closestpos = Vector3.zero;
        var centerpos = orgpos;
        centerpos.y = orgpos.y + CheckHeight / 2;
        var halfhit = Physics.OverlapBox(centerpos, new Vector3(CheckWidth, CheckHeight / 2, CheckWidth),
            Quaternion.identity,
            m_LayerMask);
        bool isTouch = false;
        bool hasClosestPoint = false;
        var MaxDis = 9999f;
        if (halfhit != null && halfhit.Length > 0)
        {
            for (int i = 0; i < halfhit.Length; i++)
            {
                collider = halfhit[i];
                if (collider is MeshCollider mc) 
                {
                    if (mc.convex)
                    {
                        
                    }
                }

                var pos = halfhit[i].ClosestPoint(orgpos);
                
                var dis = Vector3.Distance(orgpos, pos);
                if (dis < MaxDis && startpos.y < pos.y)
                {
                    isTouch = true;
                    MaxDis = dis;
                    collider = halfhit[i];
                    Closestpos = pos.GlobalPos2CurrentUnityPos();
                }
            }
        }

        if (!isTouch) return false;
        var enddis = Vector3.Distance(Closestpos, m_EndPos);
        if (enddis < CheckWidth + CheckRadius)
        {
            isTouch = false;
        }

        return isTouch;
    }

    private Vector3 CheckPoint(List<Vector3> pointList, FlyPath flyPath, Vector3 orgpos, ref Vector3 dir,
        float stepLength)
    {
        RaycastHit hit;
        var srcPath = flyPath;
        var isHit = CheckAround(srcPath.path, orgpos, out Vector3 closetpos, out Collider trans);
        if (!isHit && Physics.Linecast(srcPath.path.CurrentUnityPos2GlobalPos(),orgpos.CurrentUnityPos2GlobalPos(),out hit, m_LayerMask))
        {
            trans = hit.collider;
            isHit = true;
        }

        orgpos = orgpos.CurrentUnityPos2GlobalPos();
        while (isHit || GetContainPos(pointList, orgpos))
        {
            if (isHit)
            {
                AddHitAround(trans);
            }
            if (!m_ComputeDirList.ContainsKey(orgpos))
            {
                m_ComputeDirList.Add(orgpos, new List<Vector3>());
            }

            var dirList = m_ComputeDirList[orgpos];
            bool isExist = false;
            if (!dirList.Contains(Vector3.up) && !Physics.Raycast(orgpos, Vector3.up, out hit, stepLength, m_LayerMask))
            {
                orgpos = orgpos + Vector3.up * stepLength;
                dir = Vector3.up;
                isExist = true;
            }
            else if (!dirList.Contains(Vector3.left) &&
                     !Physics.Raycast(orgpos, Vector3.left, out hit, stepLength, m_LayerMask))
            {
                orgpos = orgpos + Vector3.left * stepLength;
                dir = Vector3.left;
                isExist = true;
            }
            else if (!dirList.Contains(Vector3.right) &&
                     !Physics.Raycast(orgpos, Vector3.right, out hit, stepLength, m_LayerMask))
            {
                orgpos = orgpos + Vector3.right * stepLength;
                dir = Vector3.right;
                isExist = true;
            }
            else if (!dirList.Contains(Vector3.down) &&
                     !Physics.Raycast(orgpos, Vector3.down, out hit, stepLength, m_LayerMask))
            {
                orgpos = orgpos + Vector3.down * stepLength;
                dir = Vector3.down;
                isExist = true;
            }

            if (!isExist && srcPath.prev != null)
            {
                orgpos = srcPath.prev.path;
                srcPath = srcPath.prev;
                isHit = CheckAround(srcPath.path, orgpos, out  closetpos, out  trans);
                continue;
            }
            if (!dirList.Contains(dir))
            {
                dirList.Add(dir);
                break;
            }
            if (srcPath.prev != null)
            {
                if (!Physics.Linecast(srcPath.path,orgpos,out hit, m_LayerMask))
                {
                    orgpos = srcPath.prev.path;
                    srcPath = srcPath.prev;
                    isHit = CheckAround(srcPath.path, orgpos, out  closetpos, out  trans);
                }
            }
            else
            {
                break;
            }
        }

        
        return orgpos.GlobalPos2CurrentUnityPos();
    }

  
    private Tuple<bool, Vector3> GetNewDir(Vector3 startpos, Vector3 dir, Vector3 dirforward, float stepLength,
        bool isforceParallel = false)
    {
        startpos = startpos.CurrentUnityPos2GlobalPos();
        var newdir = dir;
        RaycastHit hit;
        var rightpos = startpos;
        rightpos.x += CheckWidth + CheckRadius;
        var leftpos = startpos;
        leftpos.x -= CheckWidth + CheckRadius;
        // 头顶射线
        var toppos = startpos;
        toppos.y += Offset.y;
        var isHit = false;
        if (Physics.Raycast(startpos, dir, out hit, stepLength, m_LayerMask))
        {
            isHit = true;
        }
        else if (Physics.Raycast(rightpos, dir, out hit, stepLength, m_LayerMask))
        {
            isHit = true;
        }
        else if (Physics.Raycast(leftpos, dir, out hit, stepLength, m_LayerMask))
        {
            isHit = true;
        }

        else if (Physics.Raycast(toppos, dir, out hit, stepLength, m_LayerMask))
        {
            isHit = true;
        }
        var angle = GetAngle(startpos, m_EndPos);
        //Debug.Log("startpos ="+startpos+" angle="+angle);
        if (isHit)
        {
           
            AddHitAround(hit.collider);
           
            if ( m_EndPos .y < startpos.y && angle >= 80 && angle <= 100)
            {
                newdir = hit.normal + dirforward;
            }
            else
                newdir = Vector3.Cross(hit.normal, Vector3.Cross(dirforward, hit.normal));
        }

        return new Tuple<bool, Vector3>(isHit, newdir);
    }

    public Tuple<Vector3, Vector3> GetTopBottom(Vector3 targetpos, float distance)
    {
        targetpos = targetpos.CurrentUnityPos2GlobalPos();
        
        var toppos = Vector3.positiveInfinity;
        var bottompos = Vector3.negativeInfinity;
        var upward = Vector3.up;
        var downward = Vector3.down;
        RaycastHit hit;
        
       
        if (Physics.Raycast(targetpos, upward, out hit, distance, m_LayerMask))
        {
            toppos = hit.point;
        }

//        if (Physics.Raycast(targetpos, downward, out hit, distance, m_LayerMask))
//        {
//            bottompos = hit.point;
//        }


        if (Physics.Raycast(targetpos + Vector3.up * CheckHeight/2, downward, out hit, distance, m_LayerMask))
        {
            bottompos = FixPos(hit.point);
        }
        

        return new Tuple<Vector3, Vector3>(toppos.GlobalPos2CurrentUnityPos(), bottompos.GlobalPos2CurrentUnityPos());
    }


    public float GetAngle(Vector3 startpos, Vector3 targetpos)
    {
        var dir = targetpos - startpos;
        var dot = Vector3.Dot(Vector3.forward.normalized, dir.normalized);
        dot = Mathf.Clamp(dot, -1, 1);
        float angle = Mathf.Acos(dot) * Mathf.Rad2Deg; //点乘出夹角

        //Debug.Log("angle"+angle);
        return angle;
    }
    private Vector3 GetMoveDir(Vector3 startpos, Vector3 forward, Vector3 targetPos)
    {
        startpos = startpos.CurrentUnityPos2GlobalPos();
        targetPos = targetPos.CurrentUnityPos2GlobalPos();
        var dir = targetPos - startpos;
        var dis = Vector3.Distance(targetPos, startpos);
        RaycastHit hit;
        if (Physics.Raycast(startpos, dir, out hit, dis + 1, m_LayerMask))
        {
            dir = Vector3.Cross(hit.normal, Vector3.Cross(forward, hit.normal));
        }

        return dir;
    }


//    private bool HasObstacle(Vector3 pos, Vector3 endpos)
//    {
//        RaycastHit hit;
//        if (Physics.Linecast(pos, endpos, out hit, m_LayerMask))
//        {
//            return true;
//        }
//
//        return false;
//    }

    

//    private void OnDrawGizmos()
//    {
//        Gizmos.color = Color.red;
//        for (int i = 0; i < m_Path.Count; ++i)
//        {
//            Gizmos.DrawSphere(m_Path[i], 0.5f);
//
//        }
//    }

    public List<Vector3> FindPath(Vector3 startpos,Vector3 endpos)
    {
        var pathlist = new List<Vector3>();


        if (pathtype == PathType.Smooth)
        {
            m_SmoothPath = RcdtcsUnityUtils.ComputeSmoothPath(m_System.m_navQuery, startpos, endpos);
            if (m_SmoothPath != null && m_SmoothPath.m_smoothPath != null && m_SmoothPath.m_smoothPath.Length > 3)
            {
                var path = m_SmoothPath.m_smoothPath;
                for (int i = 1; i < m_SmoothPath.m_nsmoothPath; ++i)
                {
                    int v = i * 3;
                    Vector3 a = new Vector3(path[v - 3], path[v - 2], path[v - 1]);
                    Vector3 b = new Vector3(path[v + 0], path[v + 1], path[v + 2]);
                    //Debug.DrawLine(a,b,Color.red);
                    pathlist.Add(a);
                    pathlist.Add(b);
                }
            }
        }
        else
        {
            m_straightPath = RcdtcsUnityUtils.ComputeStraightPath(m_System.m_navQuery, startpos, endpos);
            if (m_straightPath != null && m_straightPath.m_straightPath != null &&
                m_straightPath.m_straightPath.Length > 3)
            {
                var path = m_straightPath.m_straightPath;
                for (int i = 1; i < m_straightPath.m_straightPathCount; ++i)
                {
                    int v = i * 3;
                    Vector3 a = new Vector3(path[v - 3], path[v - 2], path[v - 1]);
                    Vector3 b = new Vector3(path[v + 0], path[v + 1], path[v + 2]);
                    pathlist.Add(a);
                    pathlist.Add(b);
                }
            }
        }

        return pathlist;
    }

    public List<Vector3> ComputePath(Vector3 startPos,Vector3 endPos)
    {
       
        _moveState = MoveStateType.Computing;

        m_Path.Clear();
        m_MovePoint.Clear();
        m_PointDirList.Clear();
        m_ComputeDirList.Clear();
        m_HitList.Clear();
        
        m_DynamicPos = Vector3.positiveInfinity;
        m_StartPos = startPos;
        m_EndPos = endPos;
        
        m_Path = RecomputePath(m_StartPos, m_EndPos, Step);
        if (m_Path.Count == 0)
        {
            _moveState = MoveStateType.Failure;
        }

        return m_Path;

    }
    private Tuple<float,FlyPath> GetPath(Vector3 startPos,Vector3 dir,Vector3 endPos,float moveStepLength)
    {
        var pointList = new List<Vector3>();
        var curPath = new FlyPath() {path = startPos};
        bool Checkresult = false;
        CheckPath(curPath, dir.normalized, pointList, endPos, 0, moveStepLength, ref Checkresult);
        var distance = 0f;
        var temppath = curPath;

        var curpoint = curPath.path;
        while (temppath.next != null)
        {
            distance += Vector3.Distance(curpoint, temppath.next.path);
            curpoint = temppath.next.path;
            temppath = temppath.next;
        }

        var dis = Vector3.Distance(temppath.path, endPos);
        if (dis <= LandOffset)
        {
            return new Tuple<float,FlyPath>(distance,curPath);
        }

        else
        {
            return new Tuple<float,FlyPath>(-1,null);
        }
    }

 private List<Vector3> FindAirPath(Vector3 startPos,Vector3 endPos,float moveStepLength)
    {
        var flypath = new FlyPath() {path = startPos};
        var dir = endPos - startPos;
        //var pointList = new List<Vector3>();
        var iter = 0;
        var Maxdistance = float.MaxValue;
        while (iter < IterTimes)
        {
            var tempdir = dir.normalized;
            if (iter == 1)
            {
                tempdir = Vector3.forward;
            }
            else if (iter == 2)
            {
                tempdir = Vector3.back;
            }
            else if (iter == 3)
            {
                tempdir = Vector3.left;
            }
            else if (iter == 4)
            {
                tempdir = Vector3.right;
            }
            else if (iter == 5)
            {
                tempdir = Vector3.up;
            }
            else if (iter == 6)
            {
                tempdir = Vector3.down;
            }

            if (iter != 0 && tempdir.Equals(dir.normalized))
            {   
                iter++;
                continue;
            }
            var ret = GetPath(startPos, tempdir, endPos, moveStepLength);
            if (ret.Item1 != -1&& ret.Item1 < Maxdistance)
            {
                Maxdistance = ret.Item1; 
                flypath = ret.Item2;
            }

            iter++;
        }
        
        
        var path = new List<Vector3>();
        if (flypath.next != null)
        {
            path.Add(flypath.path);
            while (flypath.next != null)
            {
                path.Add(flypath.next.path);
                flypath = flypath.next;
            }
        }

        return path;
    }

//    private List<Vector3> FindAirPath(Vector3 startPos,Vector3 endPos,float moveStepLength)
//    {
//        var flypath = new FlyPath() {path = startPos};
//        var dir = endPos - startPos;
//        var pointList = new List<Vector3>();
//        var iter = 0;
//        var curPath = flypath;
//        bool Checkresult = false;
//       
//        while (iter < IterTimes)
//        {
//            int checkTime = 0;
//            CheckPath(curPath, dir.normalized, pointList, endPos, 0, moveStepLength, ref Checkresult);
//          
//            var temppath = curPath;
//            while (temppath.next != null)
//            {
//                temppath = temppath.next;
//            }
//
//            var dis = Vector3.Distance(temppath.path, endPos);
//            if (dis > 0.3f)
//            {
//                iter++;
//                curPath = temppath;
//                dir = temppath.dir;
//            }
//            else
//            {
//                break;
//            }
//        }
//        
//        var path = new List<Vector3>();
//        if (flypath.next != null)
//        {
//           
//            path.Add(flypath.path);
//            while (flypath.next != null)
//            {
//                path.Add(flypath.next.path);
//                flypath = flypath.next;
//            }
//        }
//
//        return path;
//    }

    public Vector3 FixPos(Vector3 pos)
    {
        pos = pos.CurrentUnityPos2GlobalPos();
        var newpos = pos;
        newpos.y += LandOffset;
        RaycastHit hit;
        if (Physics.Raycast(newpos,UnityEngine.Vector3.down,out hit,3,m_LayerMask))
        {//接触地面了
            var dis = Mathf.Abs(hit.distance - LandOffset);
            if (hit.distance > LandOffset && dis > 0.01f)
            {
                newpos = pos;
            }
            else
            {
                newpos = hit.point;
                newpos.y += LandOffset;
            }
            
        }

        return newpos.GlobalPos2CurrentUnityPos();
    }
    private List<Vector3> RecomputePath(Vector3 startpos, Vector3 endPos, float moveStepLength)
    {
        
       
        //var stopwatch = Stopwatch.StartNew();
        var path = new List<Vector3>();

        if (m_System == null)
        {
            return path;
        }
//******************************************
        //能直达目标点
        if (!Physics.Linecast(startpos.CurrentUnityPos2GlobalPos(),endPos.CurrentUnityPos2GlobalPos(),m_LayerMask))
        {
            path.Add(endPos);
            _moveState = MoveStateType.Moving;
            return path;
        }
        var retEnd = GetTopBottom(endPos, CheckNavMaxHeight);
        var targetBottomPos = retEnd.Item2;
        if (targetBottomPos.Equals( Vector3.positiveInfinity) || targetBottomPos.Equals(Vector3.negativeInfinity))
        {
            return path;
        }

        var retStart = GetTopBottom(startpos, CheckNavMaxHeight);
        var startBottomPos = retStart.Item2;
        path = event_FindGroundPath != null ?event_FindGroundPath(targetBottomPos,startBottomPos):FindPath(targetBottomPos, startBottomPos); //反向地面寻路
        if (path.Count > 0) //有地面寻路信息
        {
            path.Reverse();
            
            var dis = Vector3.Distance(path[path.Count - 1], endPos);
            for (int i = 0;i< path.Count;i++)
            {
                path[i] = FixPos(path[i]);
            }
            if (dis > LandOffset*2) //末尾开始再飞
            {
                var nextpath = FindAirPath(path[path.Count - 1], endPos, moveStepLength);
              
                if (nextpath.Count > 0 )
                {
                    var enddis = Vector3.Distance(nextpath[nextpath.Count - 1], endPos);
                    if (enddis <= LandOffset)
                    {
                        if (path[path.Count - 1].Equals(nextpath[0]))
                        {
                            nextpath.RemoveAt(0);
                        }
                        path.AddRange(nextpath);
                    }
                    else
                    {
                        path.Clear();
                        return path;
                    }
                }
                else
                {
                    path.Clear();
                    return path;
                }
                
            }
           
            else
            {
                //能直达普通寻路起点
             
                if (!Physics.Linecast(startpos.CurrentUnityPos2GlobalPos(), path[0].CurrentUnityPos2GlobalPos(), m_LayerMask))
                {
                    _moveState = MoveStateType.Moving;
                }
                else
                {
                    var nextpath = FindAirPath(startpos, path[0], moveStepLength);

                    if (nextpath.Count > 0)
                    {
                        var lastpos = nextpath[nextpath.Count - 1];
                        
                        if (!Physics.Linecast(lastpos.CurrentUnityPos2GlobalPos(),path[0].CurrentUnityPos2GlobalPos(),out RaycastHit hit,m_LayerMask))
                        {
                            if (nextpath[nextpath.Count - 1].Equals(path[0]))
                            {
                                path.RemoveAt(0);
                            }
                            nextpath.AddRange(path);
                            path = nextpath;
                            _moveState = MoveStateType.Moving;
                        }

                        else
                        {//飞行接地面失败 
                            _moveState = MoveStateType.Failure;
                        }
                    }
                    else
                    {
                        var startpath = event_FindGroundPath != null ?event_FindGroundPath(startBottomPos,endPos):FindPath(startBottomPos, endPos); //正向飞行地面寻路
                        if (startpath.Count > 0)
                        {
                            for (int i = 0;i< startpath.Count;i++)
                            {
                                startpath[i] = FixPos(startpath[i]);
                            }
                            nextpath = FindAirPath(startpos, startpath[0], moveStepLength);
                            if (nextpath.Count == 0)
                            {
                                _moveState = MoveStateType.Failure;
                            }
                            else
                            {
                                bool isSuccessfind = false;
                                for (int i = startpath.Count - 1;i>=0;i--)
                                {
                                    var point = startpath[i];
                                    var tempnextpath = FindAirPath(point, path[0], moveStepLength);
                                    if (tempnextpath.Count > 0)
                                    {
                                        if (tempnextpath[tempnextpath.Count - 1].Equals(path[0]))
                                        {
                                            path.RemoveAt(0);
                                        }
                                        tempnextpath.AddRange(path);
                                    

                                        var tempstartpath = startpath.GetRange(0, i);
                                        if (nextpath[nextpath.Count - 1].Equals(tempstartpath[0]))
                                        {
                                            tempstartpath.RemoveAt(0);
                                        }
                                        nextpath.AddRange(tempstartpath);
                                        if (nextpath[nextpath.Count - 1].Equals(tempnextpath[0]))
                                        {
                                            tempnextpath.RemoveAt(0);
                                        }
                                        nextpath.AddRange(tempnextpath);
                                        path = nextpath;
                                        isSuccessfind = true;
                                        break;
                                    }


                                }
                                if (!isSuccessfind)
                                {
                                    _moveState = MoveStateType.Failure;
                                }
                                else
                                {
                                    _moveState = MoveStateType.Moving;
                                }
                            
                            }
                        
                        }
                        else
                            _moveState = MoveStateType.Failure;
                    }
                
                }
            }
            
            
        }
        else
        {
            path = FindAirPath(startpos, endPos, moveStepLength);
            if (path.Count > 0)
            {
                var dis = Vector3.Distance(endPos, path[path.Count - 1]);
                if (dis > 0.3f) //飞行不能直达
                {
                    _moveState = MoveStateType.Failure;
                }
                else
                {
                    _moveState = MoveStateType.Moving;
                }
            }
           
            else
            {
                _moveState = MoveStateType.Failure;
            }
        }
        
//*******************************************
        if (path.Count == 0 || _moveState == MoveStateType.Failure)
        {
            path.Clear();
            return path;
        }

        var srcPath = new FlyPath() {path = startpos};
        var flypath = srcPath;
       
        if (path.Count > 0 && !path[path.Count - 1].Equals(m_EndPos))
        {
            var lastpath = path[path.Count - 1];
            //var lastdis = Vector3.Distance(lastpath , m_EndPos);
            if (!Physics.Linecast(lastpath.CurrentUnityPos2GlobalPos(),m_EndPos.CurrentUnityPos2GlobalPos(),m_LayerMask))//强制修正
            {
                path.Add(m_EndPos);
            }
        }
        for (int i = 0;i< path.Count;i++)
        {
            var nextflypath = new FlyPath {path = path[i], prev = flypath};
            flypath.next = nextflypath;
            flypath = nextflypath;
        }
        
        optimization(srcPath);
        path.Clear();
//        stopwatch.Stop();
//        Debug.Log("cost findpath time" + stopwatch.ElapsedMilliseconds / 1000f);
       
        if (srcPath.next != null)
        {
            if (Test)
            {
                var gameObject = GameObject.Find("TestFlyObject");
                
                if (gameObject != null)
                {
                   GameObject.DestroyImmediate(gameObject);
                }
                gameObject = new GameObject("TestFlyObject");
                var Sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                var collider = Sphere.GetComponent<SphereCollider>();
                if (collider != null)
                {
                    collider.enabled = false;
                }
                Sphere.transform.position = srcPath.next.path;
                Sphere.transform.parent = gameObject.transform;
               
            }
            path.Add(FixPos(srcPath.path));
            
           
            while (srcPath.next != null)
            {
                path.Add(FixPos(srcPath.next.path));
                if (Test)
                {
                    var gameObject = GameObject.Find("TestFlyObject");
                    var Sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    var collider = Sphere.GetComponent<SphereCollider>();
                    if (collider != null)
                    {
                        collider.enabled = false;
                    }
                    Sphere.transform.position = srcPath.next.path;
                    Sphere.transform.parent = gameObject.transform;
                }
                srcPath = srcPath.next;
            }

            
        }

//        var index = 0;
//        foreach (var data in path)
//        {
//         Debug.Log("AllPath[" + index++ + "]="+data);   
//        }
        return path;
    }

    //路点优化
    
    private void optimization(FlyPath srcFlypath)
    {
        if (!isOptimization) return;
        var flypath = srcFlypath;
        while (flypath.next != null)
        {
            var nextflypath = flypath.next;
            var startpos = flypath.path;
            while (nextflypath.next != null)
            {
                var endpos = nextflypath.next.path;
                if (!Physics.Linecast(startpos.CurrentUnityPos2GlobalPos(),endpos.CurrentUnityPos2GlobalPos(),m_LayerMask))
                {
                    flypath.next = nextflypath.next;
                }
//                var dir = endpos - startpos;
//                dir.Normalize();
//                var dis = Vector3.Distance(startpos, endpos);
//                var dirRet = GetNewDir(startpos, dir, dir, dis);
//                if (!dirRet.Item1)
//                {
//                    flypath.next = nextflypath.next;
//                }

                nextflypath = nextflypath.next;
            }

            flypath = flypath.next;
        }
    }


    private void OnDestroy()
    {
        m_System?.OnDestroy();
    }


    private byte[] DataRead(string path)
    {
        FileStream writer = new FileStream(path, FileMode.Open);
        int filelength = (int) writer.Length;
        byte[] data = new byte[filelength];
        writer.Read(data, 0, data.Length);
        writer.Close();
        return data;
    }


    /// <summary>
    /// endpos - startposy
    /// </summary>
    /// <returns></returns>
    private float GetHeightDifference()
    {
        return m_EndPos.y - m_StartPos.y;
    }

    private float GetHeightDifference(Vector3 srcPos)
    {
        return m_EndPos.y - srcPos.y;
    }
}