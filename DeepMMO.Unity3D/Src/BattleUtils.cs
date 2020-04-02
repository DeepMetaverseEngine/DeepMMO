using System;
using DeepCore.FuckPomeloClient;
using DeepCore.Game3D.Slave;
using DeepCore.Game3D.Slave.Layer;
using DeepCore.IO;
using DeepMMO.Unity3D.Terrain;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace DeepMMO.Unity3D
{
    public class NetRequestAsync<T> : CustomYieldInstruction where T : ISerializable
    {
        public bool IsDone { get; private set; }
        public T Result { get; private set; }
        public PomeloException Exception { get; private set; }

        public NetRequestAsync(ISerializable req, Action<ISerializable, Action<PomeloException, T>, object> callToAction)
        {
            callToAction(req, OnRecived, null);
        }

        private void OnRecived(PomeloException arg1, T arg2)
        {
            Result = arg2;
            Exception = arg1;
            IsDone = true;
        }


        public override bool keepWaiting => !IsDone;
    }
    
    public static class BattleUtils
    {
        private static int sNavLayer = -1;

        public static int NavLayer
        {
            get
            {
                if (sNavLayer < 0)
                {
                    sNavLayer = LayerMask.NameToLayer("NavLayer");
                }

                return sNavLayer;
            }
            set => sNavLayer = value;
        }

        public static bool ForceDisableNav = true;


        public static Vector3 ConvertToUnityPos(float h, float x, float y, float z)
        {
            return new Vector3(x, z, h - y);
        }

        public static Vector3 ConvertToUnityPos(this DeepCore.Geometry.Vector3 pos, float h)
        {
            return new Vector3(pos.X, pos.Z, h - pos.Y);
        }
        
       

        public static float MaxRayOffset = 1f;

        public static bool RaycastNavlayer(this Vector3 pos, out float ret, float maxRayOffset = float.NaN)
        {
            ret = 0;
            if (ForceDisableNav)
            {
                return false;
            }

            if (float.IsNaN(maxRayOffset))
            {
                maxRayOffset = MaxRayOffset;
            }
            //Debug.DrawLine(pos, new Vector3(pos.x, pos.y - maxRayOffset - 1,  pos.z));

            if (!Physics.Raycast(pos, Vector3.down, out var rayHit, maxRayOffset + 0.1f, 1 << NavLayer))
            {
                return false;
            }

            ret = rayHit.point.y;
            return true;
        }

        public static Vector3 RaycastNavlayer(this Vector3 pos, float maxRayOffset = float.NaN)
        {
            var ret = pos;
            if (RaycastNavlayer(pos, out var h, maxRayOffset))
            {
                ret.y = h;
                return ret;
            }

            return ret;
        }


        public static Vector3 GetUnityPos(this LayerZoneObject obj)
        {
            return obj.Position.ConvertToUnityPos(obj.Parent.Terrain3D.TotalHeight);
        }

        public static Quaternion ConvertToUnityRotation(float direction)
        {
            return Quaternion.AngleAxis(direction * Mathf.Rad2Deg + 90, Vector3.up);
        }

        public static DeepCore.Geometry.Vector3 UnityPos2ZonePos(ILayerZoneTerrain terrain, Vector3 pos)
        {
            return UnityPos2ZonePos(terrain.TotalHeight, pos);
        }

        public static DeepCore.Geometry.Vector3 UnityPos2ZonePos(float h, Vector3 pos)
        {
            return new DeepCore.Geometry.Vector3(pos.x, h - pos.z, pos.y);
        }
        
        public static DeepCore.Geometry.Vector3 UnityPos2RealZonePos(ILayerZoneTerrain terrain, Vector3 pos)
        {
            return UnityPos2ZonePos(terrain.TotalHeight, pos);
        }
        
        public static Vector3 GlobalPos2CurrentUnityPos(this Vector3 pos)
        {
            return new Vector3(pos.x - ClientUnityObject.ScreenOffset.x, pos.y - ClientUnityObject.ScreenOffset.y, pos.z - ClientUnityObject.ScreenOffset.z);
        }
        
        public static Vector3 CurrentUnityPos2GlobalPos(this Vector3 pos)
        {
            return new Vector3(pos.x + ClientUnityObject.ScreenOffset.x, pos.y + ClientUnityObject.ScreenOffset.y, pos.z + ClientUnityObject.ScreenOffset.z);
        }
        public static DeepCore.Geometry.Vector3 UnityPos2RealZonePos(float h, Vector3 pos)
        {
            return new DeepCore.Geometry.Vector3(pos.x - ClientUnityObject.ScreenOffset.x, h - pos.z + ClientUnityObject.ScreenOffset.z, pos.y - ClientUnityObject.ScreenOffset.y);
        }
        
        public static Vector3 ConvertToRealUnityPos(float h, float x, float y, float z)
        {
            return new Vector3(x + ClientUnityObject.ScreenOffset.x, z + ClientUnityObject.ScreenOffset.y, h - y + ClientUnityObject.ScreenOffset.z);
        }

        public static Vector3 ConvertToRealUnityPos(this DeepCore.Geometry.Vector3 pos, float h)
        {
            return new Vector3(pos.X + ClientUnityObject.ScreenOffset.x, pos.Z + ClientUnityObject.ScreenOffset.y, h - pos.Y + ClientUnityObject.ScreenOffset.z);
        }
    }
}