using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DeepU3.Asset;
using DeepU3.Async;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;

#endif
namespace DeepU3.Lightmap
{
    public interface ILightmapReference : IDisposable
    {
        int SceneStartLightmapIndex { get; }
        bool IsLoaded { get; }
    }

    public class SceneLightmaps : MonoBehaviour
    {
        public RenderSettingsData renderSetting = new RenderSettingsData();
        public LightmapSettingsData lightmapSetting = new LightmapSettingsData();

        private LinkedListNode<LightmapSettingsData> mCurrentNode;
        private static readonly LinkedList<LightmapSettingsData> sAllLightmapSettings = new LinkedList<LightmapSettingsData>();

        [Serializable]
        public class RenderSettingsData
        {
            public Material skybox;
            public DefaultReflectionMode defaultReflectionMode;
            public Cubemap customReflection;
            public float reflectionIntensity;
            public int reflectionBounces;

            public void Apply()
            {
                RenderSettings.skybox = skybox;
                RenderSettings.defaultReflectionMode = defaultReflectionMode;
                RenderSettings.customReflection = customReflection != null ? customReflection : null;
                RenderSettings.reflectionIntensity = reflectionIntensity;
                RenderSettings.reflectionBounces = reflectionBounces;
                RenderSettings.fog = false;
            }
        }

        [Serializable]
        public class LightmapSettingsData : IDisposable
        {
            public int count;
            public LightmapsMode mode;
            public Texture2D[] lights;
            public Texture2D[] dirs;
            public Texture2D[] shadowMasks;

            [HideInInspector]
            public string[] lightsPath;

            [HideInInspector]
            public string[] dirsPath;

            [HideInInspector]
            public string[] shadowMasksPath;

            internal int StartIndexInLms;

            [HideInInspector]
            [SerializeField]
            private bool m_DynamicLoadTexture;

            [HideInInspector]
            public string sceneName;

            private Dictionary<int, HashSet<LightmapPart>> mDynamicRefs = new Dictionary<int, HashSet<LightmapPart>>();

            public bool IsDisposed { get; private set; }


            public bool DynamicLoadTexture
            {
                get => m_DynamicLoadTexture;
#if UNITY_EDITOR
                set
                {
                    if (m_DynamicLoadTexture == value)
                    {
                        return;
                    }

                    m_DynamicLoadTexture = value;
                    if (m_DynamicLoadTexture)
                    {
                        lights = null;
                        dirs = null;
                        shadowMasks = null;
                    }
                    else
                    {
                        lights = new Texture2D[count];
                        dirs = new Texture2D[count];
                        shadowMasks = new Texture2D[count];
                        for (var i = 0; i < count; i++)
                        {
                            var lightPath = lightsPath.ElementAtOrDefault(i);
                            var dirPath = dirsPath.ElementAtOrDefault(i);
                            var shadowMaskPath = shadowMasksPath.ElementAtOrDefault(i);
                            if (lightPath != null)
                            {
                                lights[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(lightPath);
                            }

                            if (dirPath != null)
                            {
                                dirs[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(dirPath);
                            }

                            if (shadowMaskPath != null)
                            {
                                shadowMasks[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(shadowMaskPath);
                            }
                        }
                    }
                }
#endif
            }

            internal class LightmapPartReference : ILightmapReference
            {
                private readonly LightmapSettingsData _data;
                private readonly LightmapPart _part;

                public LightmapPartReference(LightmapSettingsData data, LightmapPart part = null)
                {
                    _data = data;
                    _part = part;
                }


                public void Dispose()
                {
                    _data.UnRefLightmapPart(_part);
                }

                public int SceneStartLightmapIndex => _data.StartIndexInLms;

                public bool IsLoaded
                {
                    get
                    {
                        if (_part == null)
                        {
                            return true;
                        }
                        return _data.lights != null && _part.lightmapIndex < _data.lights.Length && _data.lights[_part.lightmapIndex] != null;
                    }
                }
            }

            public void Dispose()
            {
                mDynamicRefs.Clear();
                DestroyTextures(lights);
                DestroyTextures(dirs);
                DestroyTextures(shadowMasks);
                IsDisposed = true;
            }

            private void EnsureTexArray()
            {
                if (count > 0 && (lights == null || lights.Length != count))
                {
                    lights = new Texture2D[count];
                    dirs = new Texture2D[count];
                    shadowMasks = new Texture2D[count];
                }
            }

            public void NotifyRebind()
            {
                foreach (var part in mDynamicRefs.SelectMany(entry => entry.Value))
                {
                    part.Rebind();
                }
            }

            public ILightmapReference RefLightmapPart(LightmapPart part)
            {
                if (!DynamicLoadTexture)
                {
                    return new LightmapPartReference(this);
                }

                var index = part.lightmapIndex;
                if (mDynamicRefs.TryGetValue(index, out var refs))
                {
                    refs.Add(part);
                    return new LightmapPartReference(this, part);
                }

                refs = new HashSet<LightmapPart> {part};

                mDynamicRefs.Add(index, refs);
                EnsureTexArray();

                var collectionOp = new CollectionResultAsyncOperation<Texture2D>();
                var ops = new List<IEnumerator>();
                var op1 = AssetManager.LoadAsset<Texture2D>(AssetManager.PathConverter.AssetPathToAddress(lightsPath[index]));
                op1.UserData = lights;
                ops.Add(op1);

                if (index < shadowMasksPath.Length && !string.IsNullOrEmpty(shadowMasksPath[index]))
                {
                    var op2 = AssetManager.LoadAsset<Texture2D>(AssetManager.PathConverter.AssetPathToAddress(shadowMasksPath[index]));
                    op2.UserData = shadowMasks;
                    ops.Add(op2);
                }

                if (index < dirsPath.Length && !string.IsNullOrEmpty(dirsPath[index]))
                {
                    var op3 = AssetManager.LoadAsset<Texture2D>(AssetManager.PathConverter.AssetPathToAddress(dirsPath[index]));
                    op3.UserData = dirs;
                    ops.Add(op3);
                }

                collectionOp.SetPreEnumerator(ops);
                collectionOp.UserData = index;

                collectionOp.Subscribe(OnTexture2DLoaded);
                return new LightmapPartReference(this, part);
            }

            private void OnTexture2DLoaded(BaseAsyncOperation baseOp)
            {
                var collectionOp = (CollectionResultAsyncOperation<Texture2D>) (baseOp);
                var index = (int) collectionOp.UserData;
                foreach (var op in collectionOp.CastTo<ResultAsyncOperation<Texture2D>>())
                {
                    var arr = (Texture2D[]) op.UserData;
                    arr[index] = op.Result;
                }

                var lData = new LightmapData
                {
                    lightmapColor = lights[index],
                    shadowMask = shadowMasks.ElementAtOrDefault(index),
                    lightmapDir = shadowMasks.ElementAtOrDefault(index)
                };
                UpdateLightMap(StartIndexInLms + index, lData, true);
                if (!DynamicLoadTexture)
                {
                    return;
                }

                if (mDynamicRefs.TryGetValue(index, out var refs))
                {
                    foreach (var part in refs)
                    {
                        part.Rebind();
                    }
                }
            }

            private void UnRefLightmapPart(LightmapPart part)
            {
                if (!DynamicLoadTexture || !part)
                {
                    return;
                }

                if (!mDynamicRefs.TryGetValue(part.lightmapIndex, out var refs) || !refs.Contains(part))
                {
                    return;
                }

                refs.Remove(part);
                if (refs.Count == 0)
                {
                    DestroyTexture(part.lightmapIndex);
                    mDynamicRefs.Remove(part.lightmapIndex);
                }
            }

            internal void FillLightmaps()
            {
                EnsureTexArray();

                for (var i = 0; i < count; i++)
                {
                    var lData = new LightmapData
                    {
                        lightmapColor = i < lights.Length ? lights[i] : null,
                        lightmapDir = i < dirs.Length ? dirs[i] : null,
                        shadowMask = i < shadowMasks.Length ? shadowMasks[i] : null,
                    };
                    UpdateLightMap(StartIndexInLms + i, lData, false);
                }

                RefreshLightMap();
            }


            public void Apply()
            {
                LightmapSettings.lightmapsMode = mode;
                if (count <= 0 || lights == null || lights.Length == 0)
                {
                    RefreshLightMap();
                    return;
                }

                FillLightmaps();
            }

            private void DestroyTexture(int index)
            {
                if (index < lights.Length)
                {
                    DestroyTexture(lights[index]);
                    lights[index] = null;
                }

                if (index < dirs.Length)
                {
                    DestroyTexture(dirs[index]);
                    dirs[index] = null;
                }

                if (index < shadowMasks.Length)
                {
                    DestroyTexture(shadowMasks[index]);
                    shadowMasks[index] = null;
                }

                UpdateLightMap(StartIndexInLms + index, new LightmapData(), true);
            }

            private void DestroyTexture(Texture2D t2d)
            {
                if (!t2d)
                {
                    return;
                }

                if (DynamicLoadTexture)
                {
                    AssetManager.Release(t2d);
                }
            }

            private void DestroyTextures(Texture2D[] t2ds)
            {
                for (var i = 0; i < t2ds.Length; i++)
                {
                    if (t2ds[i])
                    {
                        DestroyTexture(t2ds[i]);
                        t2ds[i] = null;
                    }
                }
            }
        }


        public void Apply()
        {
            renderSetting?.Apply();
            lightmapSetting?.Apply();
        }


        private void Awake()
        {
            var startIndex = 0;
            if (sAllLightmapSettings.Count > 0)
            {
                var last = sAllLightmapSettings.Last.Value;
                startIndex = last.StartIndexInLms + last.count;
            }

            lightmapSetting.StartIndexInLms = startIndex;
            lightmapSetting.sceneName = gameObject.scene.name;
            mCurrentNode = sAllLightmapSettings.AddLast(lightmapSetting);
        }

        void Start()
        {
            Apply();
        }

        public void Reset()
        {
            if (mCurrentNode != null)
            {
                Apply();
            }
        }

        private void OnDestroy()
        {
            if (lightmapSetting == null)
            {
                return;
            }

            lightmapSetting.Dispose();
            var isLastOne = mCurrentNode == sAllLightmapSettings.Last;
            sAllLightmapSettings.Remove(mCurrentNode);


            if (!isLastOne && sAllLightmapSettings.Count > 0)
            {
                //重排
                var index = 0;
                foreach (var setting in sAllLightmapSettings)
                {
                    setting.StartIndexInLms = index;
                    index += setting.count;
                    EnsureArraySize(index);
                    for (var i = 0; i < setting.count; i++)
                    {
                        var ld = sCurrentLms[i + setting.StartIndexInLms];
                        if (i < setting.lights.Length)
                        {
                            ld.lightmapColor = setting.lights[i];
                        }

                        if (i < setting.shadowMasks.Length)
                        {
                            ld.shadowMask = setting.shadowMasks[i];
                        }

                        if (i < setting.dirs.Length)
                        {
                            ld.lightmapDir = setting.dirs[i];
                        }
                    }

                    setting.NotifyRebind();
                }
            }

            if (sAllLightmapSettings.Count == 0)
            {
                sCurrentLms = null;
            }
            else
            {
                var setting = sAllLightmapSettings.Last.Value;
                var count = setting.StartIndexInLms + setting.count;
                for (var i = count; i < sCurrentLms.Length; i++)
                {
                    var ld = sCurrentLms[i];
                    ld.lightmapColor = null;
                    ld.shadowMask = null;
                    ld.lightmapDir = null;
                }
            }

            RefreshLightMap();
        }


        private static void EnsureArraySize(int index)
        {
            if (sCurrentLms == null)
            {
                sCurrentLms = new LightmapData[12];
                for (var i = 0; i < sCurrentLms.Length; i++)
                {
                    sCurrentLms[i] = new LightmapData();
                }
            }

            if (index < sCurrentLms.Length)
            {
                return;
            }

            var len = sCurrentLms.Length;
            Array.Resize(ref sCurrentLms, (index + 1) << 1);
            for (var i = len; i < sCurrentLms.Length; i++)
            {
                sCurrentLms[i] = new LightmapData();
            }
        }


        public static ILightmapReference Ref(LightmapPart part)
        {
            var sceneName = part.gameObject.scene.name;
            //optimize find setting 
            var setting = sAllLightmapSettings.FirstOrDefault(m => m.sceneName == sceneName);

            if (setting == null || part.lightmapIndex < 0 || part.lightmapIndex >= setting.count)
            {
                return null;
            }

            return setting.RefLightmapPart(part);
        }


        private static void UpdateLightMap(int index, LightmapData lmd, bool refresh = true)
        {
            EnsureArraySize(index);
            if (lmd == null)
            {
                lmd = new LightmapData();
            }

            sCurrentLms[index] = lmd;
            if (refresh)
            {
                LightmapSettings.lightmaps = sCurrentLms;
            }
        }

        private static LightmapData[] sCurrentLms;

        private static void RefreshLightMap()
        {
            LightmapSettings.lightmaps = sCurrentLms;
        }
    }
}