using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace DeepU3.SceneConnect
{
    [ExecuteInEditMode]
    public class ConnectArea : MonoBehaviour
    {
        // [HideInInspector]
        public string connectScenePath;

        public Vector3 size;


        public Vector3 boundsMin;
        public Vector3 boundsMax;

        [HideInInspector]
        public Vector3 connectSceneSize;

        [HideInInspector]
        public Vector3 connectSceneOriginOffset;


        private Bounds mTransportBounds;
        private Bounds mSceneBounds;
        private Bounds mConnectSceneBounds;
        private Bounds mSourceSceneBounds;
        private Bounds mAreaWorldBounds;

        public static int GetConnectHashCode(string formScenePath, string connectScenePath)
        {
            unchecked
            {
                var hashCode = formScenePath.GetHashCode();
                hashCode = (hashCode * 397) ^ connectScenePath.GetHashCode();
                return hashCode;
            }
        }

        public bool IsConnectSceneLoaded => TargetConnectArea;

        [NonSerialized]
        private SceneConnector mConnector;

        private ConnectArea mTargetConnectArea;

        internal ConnectArea TargetConnectArea
        {
            get
            {
                if (!mTargetConnectArea)
                {
                    TryLinkTargetArea();
                }

                return mTargetConnectArea;
            }
        }

        private static readonly Dictionary<int, ConnectArea> sAreas = new Dictionary<int, ConnectArea>();

        public static readonly ReadOnlyDictionary<int, ConnectArea> Areas = new ReadOnlyDictionary<int, ConnectArea>(sAreas);

        public override string ToString()
        {
            return $"{gameObject.scene.name}_{name}";
        }


        private void Awake()
        {
            mTransportBounds = new Bounds();
            mTransportBounds.SetMinMax(boundsMin, boundsMax);
            mConnectSceneBounds = new Bounds(connectSceneOriginOffset + connectSceneSize * 0.5f, connectSceneSize);
            mAreaWorldBounds = new Bounds(transform.position + size * 0.5f, size);
            if (Application.isPlaying)
            {
                sAreas.Add(GetConnectHashCode(gameObject.scene.path, connectScenePath), this);
            }
        }

        private void Start()
        {
        }

        private void OnDestroy()
        {
            if (Application.isPlaying)
            {
                sAreas.Remove(GetConnectHashCode(gameObject.scene.path, connectScenePath));
            }
        }

        private bool TryLinkTargetArea()
        {
            if (mTargetConnectArea)
            {
                return true;
            }

            var hashCode = GetConnectHashCode(connectScenePath, gameObject.scene.path);
            if (!Areas.TryGetValue(hashCode, out var targetArea))
            {
                return false;
            }

            mConnector = SceneConnector.Connectors.FirstOrDefault(m => m.gameObject.scene == gameObject.scene);

            mTargetConnectArea = targetArea;
            mSourceSceneBounds = new Bounds(transform.InverseTransformPoint(mConnector.transform.position + mConnector.size * 0.5f), mConnector.size);
            if (boundsMin == Vector3.zero && boundsMax == size)
            {
                mSceneBounds = mSourceSceneBounds;
            }
            else
            {
                var position = mConnector.transform.position;
                var sceneMin = position;
                var sceneMax = position + mConnector.size;
                var wBoundsMin = transform.TransformPoint(boundsMin);
                var wBoundsMax = transform.TransformPoint(boundsMax);
                var targetRoot = targetArea.gameObject.scene.GetRootGameObjects()[0];
                var offset = position - targetRoot.transform.position;
                if (offset.x >= 0)
                {
                    sceneMin.x = wBoundsMin.x;
                }
                else
                {
                    sceneMax.x = wBoundsMax.x;
                }

                if (offset.z >= 0)
                {
                    sceneMin.z = wBoundsMin.z;
                }
                else
                {
                    sceneMax.z = wBoundsMax.z;
                }

                if (offset.y >= 0)
                {
                    sceneMin.y = wBoundsMin.y;
                }
                else
                {
                    sceneMax.y = wBoundsMax.y;
                }

                mSceneBounds = new Bounds();
                mSceneBounds.SetMinMax(transform.InverseTransformPoint(sceneMin), transform.InverseTransformPoint(sceneMax));
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="useCalcBounds">为true,表示不使用boundsMin, boundsMax</param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public bool IsTransportable(in Vector3 pos, bool useCalcBounds, out float distance)
        {
            var localPos = transform.InverseTransformPoint(pos);
            var nextPos = mTransportBounds.ClosestPoint(localPos);
            distance = Vector3.Distance(localPos, nextPos);

            if (!TargetConnectArea)
            {
                return false;
            }

            var inConnectScenePos = mConnectSceneBounds.Contains(localPos);
            var inScenePos = useCalcBounds ? mSceneBounds.Contains(localPos) : mSourceSceneBounds.Contains(localPos);
            var inTransportBounds = nextPos == localPos;

            var ret = !inTransportBounds && inConnectScenePos && !inScenePos;
            return ret;
        }

        public bool ContainsPosition(in Vector3 pos)
        {
            return mAreaWorldBounds.Contains(pos);
        }

        public void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            var s = size;

            // s.y = s.y > 50 ? s.y : connector.size.y;
            Gizmos.DrawWireCube(transform.position + s * 0.5f, s);

            Gizmos.color = Color.green;
            var bounds = new Bounds();
            bounds.SetMinMax(boundsMin, boundsMax);
            mTransportBounds = bounds;
            Gizmos.DrawWireCube(transform.position + bounds.center, bounds.size);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.TransformPoint(mSceneBounds.center), mSceneBounds.size);
        }
    }
}