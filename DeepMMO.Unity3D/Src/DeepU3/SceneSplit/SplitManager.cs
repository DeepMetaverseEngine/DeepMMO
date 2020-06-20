using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Text;
using DeepU3.Asset;
using DeepU3.Async;
using DeepU3.Lightmap;
using UnityEngine;

namespace DeepU3.SceneSplit
{
    [Serializable]
    public class MiniSplitInfo
    {
        public string assetPath;
        public string name;
        public Vector3 position = Vector3.zero;
        public Quaternion rotation = Quaternion.identity;
        public Vector3 scale = Vector3.one;
        public LightmapBakedParams[] lightmapCollection;
    }

    [Serializable]
    public class LightmapBakedParams
    {
        public int lightmapIndex;
        public Vector4 lightmapScaleOffset;
        public string bindRendererName;
    }

    [Serializable]
    public class SplitInfo
    {
        /// <summary>
        /// int[3]
        /// </summary>
        public int[] posID;

        public string assetPath;

        public List<MiniSplitInfo> parts;

        /// <summary>
        /// 该块的Lightmap信息, 不包含parts
        /// </summary>
        public List<LightmapBakedParams> lightmapCollection = new List<LightmapBakedParams>();
    }


    public class SplitRes : MonoBehaviour
    {
        private void OnDestroy()
        {
            AssetManager.MarkInstanceDestroyed(gameObject);
        }

        private static readonly List<SplitRes> sCacheList = new List<SplitRes>();
        private static readonly List<LightmapPart> sCacheLightmapList = new List<LightmapPart>();

        protected virtual bool IsAssetInstance => true;

        private void ReleaseChildren(bool includeSelf)
        {
            sCacheList.Clear();
            GetComponentsInChildren(sCacheList);
            GetComponentsInChildren(sCacheLightmapList);
            foreach (var part in sCacheLightmapList)
            {
                part.enabled = false;
            }

            for (var i = 0; i < sCacheList.Count; i++)
            {
                var s = sCacheList[i];
                if (s == this && !includeSelf)
                {
                    continue;
                }

                if (s.IsAssetInstance)
                {
                    AssetManager.ReleaseInstance(s.gameObject);
                }
                else
                {
                    Destroy(s.gameObject);
                }
            }

            sCacheList.Clear();
        }

        public void ReleaseChildren() => ReleaseChildren(false);

        public void Release()
        {
            ReleaseChildren(true);
        }
    }

    /// <summary>
    /// Scene split manager, finds streamer and adds scene.
    /// release => release parts and self
    /// destroy => mark parts and self destroy
    /// </summary>
    ///
    public class SplitManager : SplitRes
    {
        [SerializeField]
        public SplitInfo splitData;

        public SplitStreamer streamer;

        public bool isPrefab;

        protected override bool IsAssetInstance => isPrefab;

        private void ReloadParts()
        {
            if (splitData.parts == null)
            {
                return;
            }

            foreach (var part in splitData.parts)
            {
                streamer.LoadMiniSplit(this, part, OnSubInstantiateComplete);
            }
        }

        private void Start()
        {
        }

        public void Init(SplitStreamer s, SplitInfo splitInfo, string objName)
        {
            streamer = s;
            if (!isPrefab)
            {
                splitData = splitInfo;
            }

            name = objName;
            if (isPrefab && splitData.lightmapCollection != null && splitData.lightmapCollection.Count > 0)
            {
                BindRendererLightmap(transform, splitData.lightmapCollection);
            }

            ReloadParts();
        }

        private readonly static Stack<string> sPathStack = new Stack<string>(10);

        public static void BindRendererLightmap(Transform root, ICollection<LightmapBakedParams> lightmapCollection)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                sPathStack.Clear();
                sPathStack.Push(r.name);
                var p = r.transform.parent;
                while (p != root && p)
                {
                    sPathStack.Push(p.name);
                    p = p.parent;
                }

                var str = ZString.Join<string>('/', sPathStack);
                var lParam = lightmapCollection.FirstOrDefault(m => m.bindRendererName == str);
                if (lParam != null)
                {
                    var lp = r.gameObject.GetOrAddComponent<LightmapPart>();
                    lp.renderer = r;
                    lp.lightmapIndex = lParam.lightmapIndex;
                    lp.lightmapScaleOffset = lParam.lightmapScaleOffset;
                    if (!lp.enabled)
                    {
                        lp.enabled = true;
                    }
                }
            }
        }

        private void OnSubInstantiateComplete(ResultAsyncOperation<GameObject> op)
        {
            var go = op.Result;
            var part = (MiniSplitInfo) op.UserData;
            if (!go)
            {
                return;
            }

            if (!this)
            {
                AssetManager.ReleaseInstance(go);
                return;
            }

            if (!string.IsNullOrEmpty(part.name))
            {
                go.name = part.name;
            }

            go.transform.localScale = part.scale;

            if (part.lightmapCollection != null && part.lightmapCollection.Length > 0)
            {
                BindRendererLightmap(transform, part.lightmapCollection);
            }

            go.GetOrAddComponent<SplitRes>();
        }
#if UNITY_EDITOR
        public void AddLightmaps(ICollection<LightmapBakedParams> lightmaps)
        {
            splitData.lightmapCollection.AddRange(lightmaps);
        }

        public void AddMiniPart(string assetPath, Transform t, LightmapBakedParams[] lightmaps)
        {
            if (splitData.parts == null)
            {
                splitData.parts = new List<MiniSplitInfo>();
            }

            var part = new MiniSplitInfo
            {
                name = t.name,
                position = t.localPosition,
                rotation = t.localRotation,
                scale = t.localScale,
                assetPath = assetPath,
                lightmapCollection = lightmaps
            };
            var lightmapParams = t.gameObject.GetComponentsInChildren<LightmapPart>().Where(p => p.lightmapIndex >= 0 && p.renderer).ToArray();
            if (lightmapParams.Length > 0)
            {
                part.lightmapCollection = new LightmapBakedParams[lightmapParams.Length];
                for (var i = 0; i < lightmapParams.Length; i++)
                {
                    var p = lightmapParams[i];
                    part.lightmapCollection[i] = new LightmapBakedParams
                    {
                        lightmapIndex = p.lightmapIndex,
                        lightmapScaleOffset = p.lightmapScaleOffset,
                        bindRendererName = p.renderer.name
                    };
                }
            }

            splitData.parts.Add(part);
        }
#endif
        void OnDrawGizmosSelected()
        {
            if (!streamer || !enabled)
            {
                return;
            }

            var s = (Vector3) streamer.layerSetting.splitSize;
            if (Math.Abs(s.y) < 0.0001)
            {
                s.y = 100;
            }

            var c = streamer.layerSetting.color;
            if (transform.childCount == 0)
            {
                c.a = 0.3f;
            }


            var posRoot = gameObject.scene.GetRootGameObjects()[0].transform.position;
            Gizmos.color = c;
            for (var i = 3; i < splitData.posID.Length; i += 3)
            {
                var position = new Vector3(splitData.posID[i] * s.x, splitData.posID[i + 1] * s.y, splitData.posID[i + 2] * s.z) + posRoot;
                Gizmos.DrawWireCube(position + s * 0.5f, s);
            }

            Gizmos.color = Color.green;
            var positionFirst = new Vector3(splitData.posID[0] * s.x, splitData.posID[1] * s.y, splitData.posID[2] * s.z) + posRoot;
            Gizmos.DrawWireCube(positionFirst + s * 0.5f, s);
        }
    }
}