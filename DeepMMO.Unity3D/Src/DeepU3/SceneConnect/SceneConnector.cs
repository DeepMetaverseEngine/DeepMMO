using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DeepU3.Asset;
using DeepU3.Timers;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepU3.SceneConnect
{
    public class SceneConnector : MonoBehaviour
    {
        public static string ActiveScenePath { get; private set; }

        public static event Action<SceneConnector> OnSceneBecameActive;
        private static readonly Dictionary<string, Vector3> sLoadingAreas = new Dictionary<string, Vector3>();

        //todo 使用Dictionary
        private static readonly List<SceneConnector> sConnectors = new List<SceneConnector>();

        public static readonly ReadOnlyCollection<SceneConnector> Connectors = new ReadOnlyCollection<SceneConnector>(sConnectors);


        public Vector3 size = new Vector3(600, 600, 600);


        public float loadConnectSceneDistance = 300;
        public float unloadBindSceneDistance = 320;
        private Vector3 Min => transform.position;
        private Vector3 Max => transform.position + size;


        [NonSerialized]
        private readonly List<ConnectArea> mConnectAreas = new List<ConnectArea>();


        public static float PositionCheckTime = 0.1f;


        public bool IsActiveScene => ActiveScenePath == gameObject.scene.path;

        private Bounds mLocalBounds;
        private static readonly Dictionary<ConnectArea, ActiveTracker> sBindActiveTrackers = new Dictionary<ConnectArea, ActiveTracker>();


        private static Timer sUpdateTimer;

        private static Transform sPlayerTransform;
        private static string sMoverTag;
        private static readonly HashSet<ConnectArea> sCheckedAreas = new HashSet<ConnectArea>();

        public static void SetMover(Transform player)
        {
            sPlayerTransform = player;
            if (sUpdateTimer == null)
            {
                sUpdateTimer = Timer.Register(PositionCheckTime, OnUpdateConnectors, isLooped: true);
            }
        }

        public static void SetMoverTag(string tag)
        {
            sMoverTag = tag;
            if (sUpdateTimer == null)
            {
                sUpdateTimer = Timer.Register(PositionCheckTime, OnUpdateConnectors, isLooped: true);
            }
        }

        public static void Close()
        {
            ActiveScenePath = null;
        }

        private static void TrgGetTaggedMover()
        {
            if (sPlayerTransform || string.IsNullOrEmpty(sMoverTag))
            {
                return;
            }

            var o = GameObject.FindWithTag(sMoverTag);
            if (o)
            {
                sPlayerTransform = o.transform;
            }
        }

        private static void OnUpdateConnectors()
        {
            TrgGetTaggedMover();
            if (sConnectors.Count == 0)
            {
                sUpdateTimer.Pause();
                return;
            }
            if (!sPlayerTransform)
            {
                return;
            }
            
            foreach (var connector in sConnectors)
            {
                if (string.IsNullOrEmpty(ActiveScenePath))
                {
                    AssetManager.UnloadScene(connector.gameObject.scene);
                }
                else if (!connector.IsActiveScene)
                {
                    var localPos = connector.transform.InverseTransformPoint(sPlayerTransform.position);
                    var pos = connector.mLocalBounds.ClosestPoint(localPos);
                    var distance = Vector3.Distance(pos, localPos);
                    //noneTargetArea 暂不使用, 备用
                    var noneTargetArea = true;
                    var noneActiveTracker = true;
                    foreach (var area in connector.mConnectAreas)
                    {
                        if (noneTargetArea && area.TargetConnectArea)
                        {
                            noneTargetArea = false;
                        }

                        if (noneActiveTracker && area.TargetConnectArea && IsTrackerBindArea(area.TargetConnectArea))
                        {
                            noneActiveTracker = false;
                        }
                    }

                    if (noneActiveTracker && distance > connector.unloadBindSceneDistance)
                    {
                        AssetManager.UnloadScene(connector.gameObject.scene);
                    }
                }
            }

            var count = 0;
            var path = ActiveScenePath;
            sCheckedAreas.Clear();
            var targetConnector = TryCheckConnectArea(ref count);
            if (targetConnector && path != ActiveScenePath)
            {
                OnSceneBecameActive?.Invoke(targetConnector);
                SceneManager.SetActiveScene(targetConnector.gameObject.scene);
                Debug.Log($"OnSceneBecameActive {ActiveScenePath}");
            }
        }


        private static SceneConnector TryCheckConnectArea(ref int count)
        {
            var playerPos = sPlayerTransform.position;

            var connector = Connectors.FirstOrDefault(m => m.gameObject.scene.path == ActiveScenePath);
            if (!connector)
            {
                return null;
            }

            ConnectArea transportArea = null;
            foreach (var area in connector.mConnectAreas)
            {
                if (!area.isActiveAndEnabled || sCheckedAreas.Contains(area) || sCheckedAreas.Contains(area.TargetConnectArea))
                {
                    continue;
                }


                if (area.IsTransportable(playerPos, true, out var distance))
                {
                    transportArea = area;
                    sCheckedAreas.Add(area);
                    sCheckedAreas.Add(area.TargetConnectArea);
                }
                else if (distance < connector.loadConnectSceneDistance)
                {
                    TryLoadScene(area);
                }
            }

            if (transportArea == null)
            {
                return connector;
            }

            if (transportArea.TargetConnectArea)
            {
                ActiveScenePath = transportArea.connectScenePath;
                if (count < Connectors.Count)
                {
                    count += 1;
                    return TryCheckConnectArea(ref count);
                }
            }
            else
            {
                TryLoadScene(transportArea);
            }

            return connector;
        }

        private void Awake()
        {
            sConnectors.Add(this);
            mLocalBounds = new Bounds(size * 0.5f, size);

            sLoadingAreas.TryGetValue(gameObject.scene.path, out var v3);
            transform.position = v3;
            sLoadingAreas.Remove(gameObject.scene.path);

            sUpdateTimer?.Resume();
        }

        private void Start()
        {
            if (string.IsNullOrEmpty(ActiveScenePath))
            {
                var o = gameObject;
                ActiveScenePath = o.scene.path;
                SceneManager.SetActiveScene(o.scene);
            }

            foreach (var entry in ConnectArea.Areas)
            {
                var area = entry.Value;
                if (area.gameObject.scene == gameObject.scene)
                {
                    mConnectAreas.Add(area);
                }
            }
        }

        private void OnDestroy()
        {
            if (ActiveScenePath == gameObject.scene.path)
            {
                ActiveScenePath = null;
            }

            sConnectors.Remove(this);
        }

        public static bool IsTrackerBindArea(ConnectArea area)
        {
            if (!sBindActiveTrackers.TryGetValue(area, out var activeTracker))
            {
                return false;
            }

            if (!activeTracker)
            {
                sBindActiveTrackers.Remove(area);
            }

            return activeTracker && activeTracker.isActiveAndEnabled;
        }

        public static void TryLoadScene(ConnectArea connectArea, ActiveTracker tracker = null)
        {
            if (connectArea.TargetConnectArea)
            {
                return;
            }

            if (sLoadingAreas.ContainsKey(connectArea.connectScenePath))
            {
                return;
            }

            if (tracker)
            {
                sBindActiveTrackers[connectArea] = tracker;
            }

            var s = SceneManager.GetSceneByPath(connectArea.connectScenePath);
            if (!s.IsValid() && !AssetManager.IsSceneLoading(connectArea.connectScenePath))
            {
                sLoadingAreas.Add(connectArea.connectScenePath, connectArea.transform.position + connectArea.connectSceneOriginOffset);
                AssetManager.LoadScene(connectArea.connectScenePath, LoadSceneMode.Additive);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position + size * 0.5f, size);
        }

#if UNITY_EDITOR

        public Bounds WorldBounds => new Bounds(transform.position + size * 0.5f, size);
        public void GenericSceneConnect(SceneConnector connectTarget)
        {
            if (!WorldBounds.Intersects(connectTarget.WorldBounds))
            {
                return;
            }

            var pos = transform.position;

            var offset = pos - connectTarget.transform.position;
            var min = Min;
            var max = Max;
            var targetMin = connectTarget.Min;
            var targetMax = connectTarget.Max;


            var connectSize = new Vector3(0, 1000, 0);
            var connectPos = Vector3.zero;
            if (offset.x >= 0)
            {
                connectPos.x = min.x;
                connectSize.x = targetMax.x - pos.x;
            }
            else
            {
                connectPos.x = targetMin.x;
                connectSize.x = max.x - targetMin.x;
            }

            if (offset.z >= 0)
            {
                connectPos.z = min.z;
                connectSize.z = targetMax.z - pos.z;
            }
            else
            {
                connectPos.z = targetMin.z;
                connectSize.z = max.z - targetMin.z;
                // transportOrigin.z = min.z;
            }

            if (offset.y >= 0)
            {
                if (offset.y > connectTarget.size.y * 0.5f)
                {
                    connectPos.y = min.y;
                    connectSize.y = targetMax.y - pos.y;
                }
                else
                {
                    connectPos.y = targetMin.y;
                    connectSize.y = size.y;
                }
            }
            else
            {
                if (offset.y < -connectTarget.size.y * 0.5f)
                {
                    connectPos.y = targetMin.y;
                    connectSize.y = max.y - targetMin.y;
                }
                else
                {
                    connectPos.y = min.y;
                    connectSize.y = size.y;
                }
            }

            var lastArea = FindObjectsOfType<ConnectArea>().FirstOrDefault(m => m.gameObject.scene == gameObject.scene && m.connectScenePath == connectTarget.gameObject.scene.path);
            if (lastArea)
            {
                Undo.DestroyObjectImmediate(lastArea.gameObject);
            }

            var o = new GameObject($"connect_to_{connectTarget.gameObject.scene.name}");
            Undo.RegisterCreatedObjectUndo(o, o.name);

            var area = o.AddComponent<ConnectArea>();
            var t = o.transform;
            t.SetParent(transform);
            t.position = connectPos;
            area.size = connectSize;
            area.boundsMin = Vector3.zero;
            area.boundsMax = area.size;

            area.connectScenePath = connectTarget.gameObject.scene.path;

            area.connectSceneOriginOffset = connectTarget.transform.position - area.transform.position;
            area.connectSceneSize = connectTarget.size;
        }
#endif
    }
}