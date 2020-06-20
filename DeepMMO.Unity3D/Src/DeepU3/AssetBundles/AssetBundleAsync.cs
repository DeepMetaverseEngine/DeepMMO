using System;
using System.Collections;
using UnityEngine;

namespace DeepU3.AssetBundles
{
    /// <summary>
    ///     An asynchronous wrapper for the AssetBundleManager downloading system
    /// </summary>
    public class AssetBundleAsync : IEnumerator
    {
        public AssetBundle AssetBundle { get; private set; }

        public bool IsDone { get; private set; }
        public bool Failed { get; private set; }


        private Action<AssetBundleAsync> mCompleted;

        public event Action<AssetBundleAsync> Completed
        {
            add
            {
                if (IsDone)
                {
                    value.Invoke(this);
                }
                else
                {
                    mCompleted += value;
                }
            }
            remove
            {
                if (mCompleted != null)
                {
                    mCompleted -= value;
                }
            }
        }


        public AssetBundleAsync(string bundleName, Action<string, Action<AssetBundle>> callToAction)
        {
            IsDone = false;
            callToAction(bundleName, OnAssetBundleComplete);
        }

        public AssetBundleAsync()
        {
            IsDone = true;
            Failed = true;
        }

        private void OnAssetBundleComplete(AssetBundle bundle)
        {
            AssetBundle = bundle;
            Failed = bundle == null;
            IsDone = true;
            mCompleted?.Invoke(this);
        }

        public bool MoveNext()
        {
            return !IsDone;
        }

        public void Reset()
        {
        }

        public object Current => null;
    }

    /// <summary>
    ///     An asynchronous wrapper for the AssetBundleManager manifest downloading system
    /// </summary>
    public class AssetBundleManifestAsync : IEnumerator
    {
        public bool Success { get; private set; }
        public bool IsDone { get; private set; }

        public AssetBundleManifestAsync(string bundleName, bool getFreshManifest, Action<string, bool, Action<AssetBundle>> callToAction)
        {
            IsDone = false;
            callToAction(bundleName, getFreshManifest, OnAssetBundleManifestComplete);
        }

        private void OnAssetBundleManifestComplete(AssetBundle bundle)
        {
            Success = bundle != null;
            IsDone = true;
        }

        public bool MoveNext()
        {
            return !IsDone;
        }

        public void Reset()
        {
        }

        public object Current => null;
    }
}