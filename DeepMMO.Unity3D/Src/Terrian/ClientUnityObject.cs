using System;
using DeepCore;
using DeepCore.Game3D.Slave.Layer;
using DeepCore.Game3D.Voxel;
using UnityEngine;
using UnityEngine.AI;
using Vector2 = DeepCore.Geometry.Vector2;
using Vector3 = DeepCore.Geometry.Vector3;


namespace DeepMMO.Unity3D.Terrain
{
    //----------------------------------------------------------------------------------------------------------------------------------------

    public class ClientUnityObject
    {
        private Vector3 currentPos;
        private float zspeed = 0;
        private float height = 2f;
        public float Gravity { get; private set; } = 9.8f;
        public Vector3 Position { get => currentPos; }
        public float X { get => currentPos.X; }
        public float Y { get => currentPos.Y; }
        public float Z { 
            get => currentPos.Z;
            set => currentPos.Z = value;
        }

        private UnityEngine.Vector3 mCurrentUnityPos; 

        
        public static UnityEngine.Vector3 ScreenOffset = UnityEngine.Vector3.zero;
        public UnityEngine.Vector3 currentUnityPos
        {
            get
            {
                mCurrentUnityPos = ConvertToUnityPos(currentPos);
                return mCurrentUnityPos;
                
            }
            set
            {
                mCurrentUnityPos = value;
                currentPos = UnityPos2ZonePos(mCurrentUnityPos);
            }
        }

        private UnityEngine.Vector3 ConvertToUnityPos(Vector3 pos)
        {
            var ret = pos.ConvertToRealUnityPos(TotalHeight);
            return ret;
        }

        private Vector3 UnityPos2ZonePos(UnityEngine.Vector3 pos)
        {
            return BattleUtils.UnityPos2RealZonePos(TotalHeight,pos);
        }
        
        public float UnityX { get => ConvertToUnityPos(currentPos).x; }
        
        public float UnityY { get => ConvertToUnityPos(currentPos).y; }
        public float UnityZ { get => ConvertToUnityPos(currentPos).z; }
        public float Height { get => height; set { height = value; } }
        public float SpeedZ { get => zspeed; set { zspeed = value; } }

        public float TotalWidth { get; set; }
        public float TotalHeight { get; set; }
        
        public float StepIntercept { get; set; }
        private bool mIsInAir;

        /// <summary>
        /// 是否在空中
        /// </summary>
        public bool IsMidair { get => mIsInAir ; }

            
        private CheckBoxTouchComponent mCheckBoxTouchComponent;            
        private float gridcellsize;
        public float GridCellSize;

        public ClientUnityObject(float totalwidth,float totalHeight, float stepIntercept, float height,float gridcellsize,float radius = 0.5f,float gravity = 9.8f )
        {
            TotalWidth = totalwidth;
            TotalHeight = totalHeight;
            StepIntercept = stepIntercept;
            Height = 2f;//height;
            mCheckBoxTouchComponent = new CheckBoxTouchComponent(stepIntercept);
            mCheckBoxTouchComponent.height = height;
            mCheckBoxTouchComponent.radius = radius;
            GridCellSize = gridcellsize;
            Gravity = gravity;
            //LayerMasks = LayerMask.GetMask(LayerName);
        }
        public void Transport(Vector3 pos)
        {
           //NavMeshHit navMeshHit = new NavMeshHit();
           currentPos = pos;
           //UnityEngine.Debug.Log(" Transport1============="+pos);
//           var ret = IsBottomhit(currentUnityPos);
//           if (ret.Item1)
//           {
//               //UnityEngine.Debug.Log("currentUnityPos.y==============" + ret.Item2);
//               currentUnityPos = new UnityEngine.Vector3(currentUnityPos.x, ret.Item2.y, currentUnityPos.z);
//               currentPos = BattleUtils.UnityPos2ZonePos(TotalHeight, currentUnityPos);
//               //UnityEngine.Debug.Log(" Transport2============="+currentPos);
//           }
           
        }
        private Vector2 WorldPosToVoxel(float x, float y, out int bx, out int by)
        {
            bx = (int)(x / GridCellSize);
            by = (int)(y / GridCellSize);
            return new Vector2(bx,by);
        }

        public Tuple<bool,UnityEngine.Vector3> IsInWater(UnityEngine.Vector3 unityPos)
        {
            var IsInWater = mCheckBoxTouchComponent.IsInWater(unityPos);
            //Debug.Log("ClientUnityObjectIsInWater = "+IsInWater);
            return IsInWater;
        }
        public Tuple<bool,UnityEngine.Vector3> IsSidehit(UnityEngine.Vector3 startpos,UnityEngine.Vector3 unityPos)
        {
            var isSidehit = mCheckBoxTouchComponent.IsSideHit(startpos,unityPos);
            return isSidehit;
        }
        public Tuple<bool,UnityEngine.Vector3> IsBottomhit(UnityEngine.Vector3 unityPos)
        {
           
            var isBottomhit = mCheckBoxTouchComponent.IsBottomHit(unityPos);
           
            return isBottomhit;
        }
        public Tuple<bool,UnityEngine.Vector3> IsTophit(UnityEngine.Vector3 unityPos)
        {
            var isTophit = mCheckBoxTouchComponent.IsTopHit(unityPos);
            return isTophit;
        }
        
       

        [Flags]
        public enum TouchType
        {
            None = 1,
            Bottom = 2,
            Side = 4,
            Top = 8,
            InSide = 16,
        };

        private const bool isDebug = false;
        public VoxelObject.MoveResult TryMoveToNext3D(Vector2 target, bool land)
        {
            target.X = Position.X + target.X;
            target.Y = Position.Y + target.Y;
            target.X = Mathf.Clamp(target.X, 0, TotalWidth);
            target.Y = Mathf.Clamp(target.Y, 0, TotalHeight);
            //UnityEngine.Debug.Log("currentPos======"+currentPos);
            var curCell = WorldPosToVoxel(Position.X,Position.Y,out var tx,out var ty);
            var targetCell = WorldPosToVoxel(target.X, target.Y, out  tx, out  ty);
            var newpos = new Vector3(target.X, target.Y, Z);
            var newunitypos = ConvertToUnityPos(newpos);
           

           
            var touchFlag = TouchType.None;
            var tophit = IsTophit(newunitypos);
            if (tophit.Item1)
            {
                touchFlag |= TouchType.Top;
            }
            
            var bottomhit = IsInWater(newunitypos);
            var isBottomhit = bottomhit.Item1;
            if (!bottomhit.Item1)
            {
                bottomhit = IsBottomhit(newunitypos);
                isBottomhit = bottomhit.Item1;
                // touchFlag |= TouchType.InSide;
                // newunitypos.y = touchInWater.Item2.y;
                // var zonepos = UnityPos2ZonePos(newunitypos);
                // this.currentPos = zonepos;
                // return VoxelObject.MoveResult.Arrive;
            }
            
            if (isBottomhit)
            {
                touchFlag |= TouchType.Bottom; 
            }

            if (tophit.Item1 && bottomhit.Item1)
            {
                if (Mathf.Abs(tophit.Item2.y - bottomhit.Item2.y) < height)//下一格空间不够卡路来凑
                {
                    if (isDebug)
                        UnityEngine.Debug.Log("Blocked======"+currentPos);
                    return VoxelObject.MoveResult.Blocked;
                }
            }
          
            
            var Sidehit = IsSidehit(currentUnityPos,newunitypos);
            var isSidehit = Sidehit.Item1;
            if (isSidehit)
            {
                //UnityEngine.Debug.Log("isSidehitBlockedFront======"+currentPos);
                newunitypos = Sidehit.Item2;
                var zonepos = UnityPos2ZonePos(newunitypos);
                this.currentPos = zonepos;
                //UnityEngine.Debug.Log("isSidehitAfter======"+currentPos);
                return VoxelObject.MoveResult.Blocked;
               
            }
//
         
            
            var pathhit = new NavMeshPath();
            if (isBottomhit && !NavMesh.CalculatePath(currentUnityPos,newunitypos,1,pathhit))
            {
                if (bottomhit.Item2.y - StepIntercept> currentUnityPos.y )
                {
                    if (isDebug)
                    {
                        UnityEngine.Debug.Log("pathhit======"+currentPos);
                    }
                  
                    return VoxelObject.MoveResult.Blocked;
                }
            }
            
            
            if (curCell.X == targetCell.X && curCell.Y == targetCell.Y)
            {
                this.currentPos.X = target.X;
                this.currentPos.Y = target.Y;
                var zonepos = UnityPos2ZonePos(bottomhit.Item2);
                if ((Mathf.Abs(zonepos.Z - this.currentPos.Z) <= StepIntercept) && !IsMidair)
                {
                    this.currentPos = zonepos;
                    
                }
                if (isDebug)
                UnityEngine.Debug.Log("Arrive======"+currentPos);
                return VoxelObject.MoveResult.Arrive;
            }
            //var step = StepIntercept;
            if (IsMidair) { land = false; }
            
            if (bottomhit.Item1)
            {
               land = true;
            }
//            
            
            var isSide = touchFlag & TouchType.Side;
            if (land && isSide == 0)
            {//边界 判断
               
                var zonepos = UnityPos2ZonePos(bottomhit.Item2);
                
                if (this.currentPos.Z > zonepos.Z || Mathf.Abs(zonepos.Z - this.currentPos.Z) <= StepIntercept)
                {
                    if (isDebug)
                    {
                        Debug.Log("newunitypos.z =" + newunitypos.z + "bottomhit.Item2.z=" + bottomhit.Item2.z);
                        Debug.LogError("currentpos="+currentPos); 
                    }
                    
                    //从高到低
                    if (this.currentPos.Z > zonepos.Z && this.currentPos.Z - zonepos.Z > height)
                    { 
                        this.currentPos = UnityPos2ZonePos(newunitypos);
                        if (isDebug)
                        {
                            Debug.LogError("afterStepInterceptcurrentpos="+this.currentPos); 
                        }
                        
                    }
                    else
                    {
                        this.currentPos = zonepos;
                        if (isDebug)
                        {
                            Debug.LogError("aftercurrentpos=" + currentPos);
                        }
                    }
                      
                    if (isDebug)
                        UnityEngine.Debug.Log("land======"+currentPos);
                    return VoxelObject.MoveResult.Arrive;
                }
             
            }
            else
            {
                if (touchFlag == TouchType.None)
                {
                    currentPos.X = target.X;
                    currentPos.Y = target.Y;
                    if (isDebug)
                        UnityEngine.Debug.Log("Cross======"+currentPos);
                    return VoxelObject.MoveResult.Cross;
                }
                else
                {
                   
                    //周围撞到
                    //var isSide = touchFlag & TouchType.Side;
//                    if (isSide > 0)
//                    {
//                        newunitypos.x = Sidehit.Item2.x;
//                        newunitypos.z = Sidehit.Item2.z;
//                        var zonepos = BattleUtils.UnityPos2ZonePos(TotalHeight, newunitypos);
//                        this.currentPos = zonepos;
//                        UnityEngine.Debug.Log("TouchX======");
//                        return VoxelObject.MoveResult.TouchX;
//                        
//                    } 
                   
                }
            }
            
            var dx = targetCell.X - curCell.X;
            var dy = targetCell.Y - curCell.Y;
            var adx = Math.Abs(dx);
            var ady = Math.Abs(dy);
            if (adx <= 1 && ady <= 1)
            {
                if (curCell.Y == targetCell.Y)
                {
                    this.currentPos.Y = target.Y;
                }
                if (curCell.X == targetCell.Y)
                {
                    this.currentPos.X = target.X;
                }
            }

            if (isDebug)
            {
                UnityEngine.Debug.Log("MoveResultBlocked======"+currentPos);
            }
             
            
            //不可行走面//
            return VoxelObject.MoveResult.Blocked;
        }
        public void Jump(float speed)
        {
            this.zspeed = speed;
        }
        public void Update(int intervalMS)
        {
            ProcessGravity(intervalMS);
            fixPostion();
        }

       
        private UnityEngine.Vector3 LastPosition = UnityEngine.Vector3.zero;
       
        public void fixPostion()
        {
            if (LastPosition != currentUnityPos)
            {
                var dir = -(LastPosition - currentUnityPos).normalized;
                var distance = UnityEngine.Vector3.Distance(currentUnityPos, LastPosition);
                var isTouch = Physics.Raycast(LastPosition, dir, out RaycastHit fixhit, distance, mCheckBoxTouchComponent.GetLayerMask());
                if (isTouch)
                {
                    LastPosition = fixhit.point;
                    currentUnityPos = fixhit.point;
                }
                       
            }
           
        }

        public void SetGravity(float gravity)
        {
            Gravity = gravity;
        }

        private float _UpWard;
        public float Upward {
            get { return _UpWard; }
            set { _UpWard = value - ScreenOffset.y; } 
        }

        private bool mInitPos = false;
        private UnityEngine.Vector3 mInitVector3 = UnityEngine.Vector3.zero;
        protected virtual void ProcessGravity(int intervalMS)
        {
            
           
            if(!mInitPos)
            {
                if (mInitVector3 == UnityEngine.Vector3.zero && currentUnityPos != UnityEngine.Vector3.zero)
                {
                    mInitVector3 = currentUnityPos;
                }
                else
                {
                    if (!mInitVector3.Equals(currentUnityPos))
                    {
                        mInitPos = true;
                    }
                }
                
            }
           
            LastPosition = currentUnityPos;

           
            var topHeight = float.MaxValue;
            var bottomHeight = 0f;
            var bottomrayhit = mCheckBoxTouchComponent.RayHit(currentUnityPos + UnityEngine.Vector3.up * height/2, UnityEngine.Vector3.down, 100);
            if (bottomrayhit.Item1)// 底
            {
                bottomHeight = bottomrayhit.Item2.point.y;
                Upward = bottomHeight;
            }
            else
            {
                Upward = float.NegativeInfinity;
            }

            
            Physics.queriesHitBackfaces = true;
            var toprayhit = mCheckBoxTouchComponent.RayHit(currentUnityPos + UnityEngine.Vector3.up * height, UnityEngine.Vector3.up, 100);
            Physics.queriesHitBackfaces = false;
            if (toprayhit.Item1 && topHeight >= bottomHeight  + height)// 顶
            {
                topHeight = toprayhit.Item2.point.y;
            }

            
            //是否触底
            var bottomhit = IsInWater(currentUnityPos);
            var isInwater = bottomhit.Item1;
            if (!bottomhit.Item1)//是否在水里
            {
                //是否触底
                bottomhit = IsBottomhit(currentUnityPos);
                
            }
            else
            {
                bottomHeight = bottomhit.Item2.y;
                Upward = bottomHeight;
                //UnityEngine.Debug.LogError("Upward===="+Upward);
            }
           
           // UnityEngine.Debug.Log("topHeight.Item1===="+topHeight);
           // UnityEngine.Debug.Log("bottomHeight.Item1===="+bottomHeight);
           if (isInwater && zspeed == 0)
           {
               mIsInAir = false;
           }
           else
           {
               mIsInAir =  (zspeed > 0 || Gravity == 0 || !bottomhit.Item1);
           }
          //UnityEngine.Debug.Log("zspeed===="+zspeed + " bottomhit.Item1==="+ bottomhit.Item1);

            if (zspeed > 0 || !bottomhit.Item1)
            {
                currentPos.Z += CMath.GetSpeedDistance(intervalMS, zspeed);
                var hitpos = currentUnityPos;
                 hitpos.y = topHeight;
                var tophitppos = UnityPos2ZonePos( hitpos);
                if (currentPos.Z > tophitppos.Z - this.height - StepIntercept / 2 )
                {
                    currentPos.Z = tophitppos.Z - this.height - StepIntercept / 2  ;
                    //UnityEngine.Debug.Log("tophitppos.Item1===="+currentPos);
                    if (zspeed != 0)
                    {
                        event_OnBumpHead?.Invoke(this, zspeed);
                        zspeed = 0;
                    }

                    return;
                } 

                hitpos.y = bottomHeight;
                var bottomzonepos = UnityPos2ZonePos(hitpos);
                //UnityEngine.Debug.LogError("bottomzonepos===="+bottomzonepos);
                if (currentPos.Z <= bottomzonepos.Z)
                {
                    currentPos.Z = bottomzonepos.Z;
                    if (zspeed != 0)
                    {
                        event_OnFallenDown?.Invoke(this, zspeed);
                        zspeed = 0;
                        mIsInAir = false;
                    }
                    return;
                }

                if (mInitPos)
                {
                    zspeed -= CMath.GetSpeedDistance(intervalMS, Gravity);
                }
                
               
            }
            else
            {
                //UnityEngine.Debug.Log("bottomhit.Item1===="+bottomhit.Item1 + " currentPos==="+ currentPos);
                if (bottomhit.Item1 )
                {
                    var hitpos = bottomhit.Item2;
                    if (hitpos.y < bottomHeight)//高度修正
                    {
                        hitpos.y = bottomHeight;
                    }
                    var bottomzonepos = UnityPos2ZonePos(hitpos);
                    if (currentPos.Z < bottomzonepos.Z )
                    {   
                        //UnityEngine.Debug.Log("bottomhit.Item2===="+bottomzonepos.Z + " currentPos.Z==="+ currentPos.Z);
                        currentPos.Z = bottomzonepos.Z ;
                        zspeed = 0;
                    }
                }
                else
                {
                    zspeed = 0;
                }
            }
           

        }
//        //-----------------------------------------------------------------------------------------------------
        /// <summary>
        /// 已切换体素
        /// </summary>
        public event LayerChanged OnLayerChanged { add { event_OnLayerChanged += value; } remove { event_OnLayerChanged -= value; } }
        /// <summary>
        /// 头撞到顶
        /// </summary>
        public event BumpHead OnBumpHead { add { event_OnBumpHead += value; } remove { event_OnBumpHead -= value; } }
        /// <summary>
        /// 摔落到地面
        /// </summary>
        public event FallenDown OnFallenDown { add { event_OnFallenDown += value; } remove { event_OnFallenDown -= value; } }
        private LayerChanged event_OnLayerChanged;
        private BumpHead event_OnBumpHead;
        private FallenDown event_OnFallenDown;
        public delegate void LayerChanged(ClientUnityObject obj, VoxelLayer src, VoxelLayer dst);
        public delegate void BumpHead(ClientUnityObject obj, float zspeed);
        public delegate void FallenDown(ClientUnityObject obj, float zspeed);
    }

    //----------------------------------------------------------------------------------------------------------------------------------------
}
