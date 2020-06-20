using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


namespace DeepU3.AssetBundles
{
    public class AssetBundleProfileManifest : ScriptableObject
    {
        public static readonly string ManifestAddress = nameof(AssetBundleProfileManifest);

        [Serializable]
        public class AssetPair
        {
            public string assetPath;
            public string assetBundleName;
            public string assetTypeName;

            public override int GetHashCode()
            {
                return assetPath.GetHashCode();
            }

            public AssetPair(string path, string ab, string t)
            {
                assetPath = path;
                assetBundleName = ab;
                assetTypeName = t;
            }
        }

        [Serializable]
        public class IgnoreDependenciesAssetPair
        {
            public string parentBundleName;
            public string[] ignoreBundleNames;

            private Lazy<HashSet<string>> mDepends;

            public IgnoreDependenciesAssetPair()
            {
                mDepends = new Lazy<HashSet<string>>(() =>
                {
                    var ret = new HashSet<string>();
                    foreach (var s in ignoreBundleNames)
                    {
                        ret.Add(s);
                    }

                    return ret;
                });
            }

            public bool IsIgnoreBundleName(string bundleName)
            {
                return mDepends.Value.Contains(bundleName);
            }
        }

        public AssetPair[] assetPairs;


        public string fileExtension;

        public IgnoreDependenciesAssetPair[] ignoreDependenciesAssetPairs;

        /// <summary>
        /// hash 字符串长度
        /// </summary>
        public int hashLength = 8;

        /// <summary>
        /// asset path : AssetPair
        /// </summary>
        private readonly Dictionary<string, AssetPair> mAssetPath2Pair = new Dictionary<string, AssetPair>();


        private readonly Dictionary<string, string> mSceneName2ScenePath = new Dictionary<string, string>();
        private readonly Dictionary<string, string> mBundleName2ScenePath = new Dictionary<string, string>();
        private readonly Dictionary<string, IgnoreDependenciesAssetPair> mBundleName2IgnoreDependsAssetPair = new Dictionary<string, IgnoreDependenciesAssetPair>();

        private void OnEnable()
        {
            if (assetPairs != null && mAssetPath2Pair.Count == 0)
            {
                foreach (var pair in assetPairs)
                {
                    if (pair.assetPath.EndsWith(".unity"))
                    {
                        mSceneName2ScenePath.Add(Path.GetFileNameWithoutExtension(pair.assetPath), pair.assetPath);
                        mBundleName2ScenePath.Add(pair.assetBundleName, pair.assetPath);
                    }

                    mAssetPath2Pair.Add(pair.assetPath, pair);
                }
            }

            if (ignoreDependenciesAssetPairs != null && mBundleName2IgnoreDependsAssetPair.Count == 0)
            {
                foreach (var pair in ignoreDependenciesAssetPairs)
                {
                    mBundleName2IgnoreDependsAssetPair.Add(pair.parentBundleName, pair);
                }
            }
        }


        public bool IsIgnoreDependency(string bundleName, string dependencyBundleName)
        {
            if (!mBundleName2IgnoreDependsAssetPair.TryGetValue(bundleName, out var pair))
            {
                return false;
            }

            return pair.IsIgnoreBundleName(dependencyBundleName);
        }

        public bool ContainsIgnoreDependency(string bundleName)
        {
            return mBundleName2IgnoreDependsAssetPair.ContainsKey(bundleName);
        }

        public bool TryGetIgnoreDependencies(string bundleName, out IgnoreDependenciesAssetPair ret)
        {
            return mBundleName2IgnoreDependsAssetPair.TryGetValue(bundleName, out ret);
        }

        public string AssetPathToAssetBundleName(string assetPath)
        {
            mAssetPath2Pair.TryGetValue(assetPath, out var ret);
            return ret?.assetBundleName;
        }

        public string SceneNameToAssetBundleName(string sceneName)
        {
            return mSceneName2ScenePath.TryGetValue(sceneName, out var scenePath) ? AssetPathToAssetBundleName(scenePath) : sceneName;
        }


        public string SceneNameToAssetPath(string sceneName)
        {
            mSceneName2ScenePath.TryGetValue(sceneName, out var scenePath);
            return scenePath;
        }

        public string AssetBundleNameToScenePath(string bundleName)
        {
            mBundleName2ScenePath.TryGetValue(bundleName, out var scenePath);
            return scenePath;
        }

        public string AssetBundleNameToSceneName(string bundleName)
        {
            return mBundleName2ScenePath.TryGetValue(bundleName, out var scenePath) ? Path.GetFileNameWithoutExtension(scenePath) : null;
        }
    }
}