using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeepU3.Asset
{
    public class Statistics : MonoBehaviour
    {
        public int destroyed;
        public int destroyedToUnloadUnused;
        public static Statistics Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void LateUpdate()
        {
            if (destroyedToUnloadUnused > 0 && destroyed > destroyedToUnloadUnused)
            {
                destroyed = 0;
                AssetManager.UnloadUnusedAssets();
            }
        }
        private void OnDestroy()
        {
            AssetManager.Cleanup();
        }




#if UNITY_EDITOR
        public bool enableStatistics;
        [Serializable]
        public class AssetNode
        {
            public Object Asset;
            public string LoadTrace;
            public BundleNode Bundle;

            public AssetNode(BundleNode bundleNode, Object obj, string trace)
            {
                Bundle = bundleNode;
                Asset = obj;
                LoadTrace = trace;
            }

            public override string ToString()
            {
                return Asset.ToString();
            }
        }

        [Serializable]
        public class BundleNode
        {
            public string Name;
            public List<BundleNode> Senders = new List<BundleNode>();
            public List<AssetNode> Assets = new List<AssetNode>();
            public StackTrace LoadTrace;
            public bool IsLoaded;

            public long LoadingMS;

            public override string ToString()
            {
                return Name;
            }

            public BundleNode(string name)
            {
                Name = name;
            }
        }

        [NonSerialized]
        private List<BundleNode> BundleNodes = new List<BundleNode>();

        [NonSerialized]
        private List<AssetNode> AssetNodes = new List<AssetNode>();

        public BundleNode GetBundleNode(string bundleName, bool getOrCreate = true)
        {
            var node = BundleNodes.FirstOrDefault(m => m.Name == bundleName);
            if (getOrCreate && node == null)
            {
                node = new BundleNode(bundleName);
                BundleNodes.Add(node);
            }

            return node;
        }

        public void AddBundle(string bundleName, string[] senders, StackTrace trace, long ms = 0)
        {
            if (!enableStatistics)
            {
                return;
            }
            var node = GetBundleNode(bundleName);
            foreach (var s in senders)
            {
                var parentNode = GetBundleNode(s);
                node.Senders.Add(parentNode);
            }

            node.LoadingMS = ms;
            node.LoadTrace = trace;
            node.IsLoaded = true;
        }


        public void PreAddBundle(string bundleName, StackTrace trace)
        {
            if (!enableStatistics)
            {
                return;
            }
            var node = GetBundleNode(bundleName);
            node.LoadTrace = trace;
        }

        public BundleNode[] LoadingBundles()
        {
            return BundleNodes.Where(bn => !bn.IsLoaded).ToArray();
        }
        public void RemoveBundle(string bundleName)
        {
            if (!enableStatistics)
            {
                return;
            }
            var node = GetBundleNode(bundleName, false);
            BundleNodes.Remove(node);
        }

        public void PreAddAsset(string assetName, string bundleName, StackTrace trace)
        {
            //todo
            throw new NotImplementedException();
        }

        public void AddAsset(Object asset, string bundleName, StackTrace trace)
        {
            if (!enableStatistics)
            {
                return;
            }
            var node = GetBundleNode(bundleName);
            var assetNode = new AssetNode(node, asset, trace?.ToString());
            node.Assets.Add(assetNode);
            AssetNodes.Add(assetNode);
        }

        public void RemoveAsset(Object asset, string bundleName)
        {
            if (!enableStatistics)
            {
                return;
            }
            var node = GetBundleNode(bundleName);
            node.Assets.RemoveAll(m => m.Asset == asset);
            AssetNodes.RemoveAll(m => m.Asset == asset);
        }

        public AssetNode[] SearchAssets(string part)
        {
            return AssetNodes.Where(m => m.Asset.name.Contains(part)).ToArray();
        }

        public BundleNode[] SearchBundles(string part)
        {
            return BundleNodes.Where(m => m.Name.Contains(part)).ToArray();
        }
#endif
    }
}