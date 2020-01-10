using System;
using System.Collections.Generic;
using CoreUnity.Async;
using CoreUnity.Cache;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CoreUnity.Asset
{
    internal class InternalAssetManager
    {
        private IAssetImpl mImpl;

        private KeyObjectPool<int, GameObject> mGameObjectCache;

        private readonly Dictionary<int, int> mActiveGameObject = new Dictionary<int, int>();

        public ResultAsyncOperation<AsyncOperation> LoadScene(object address, LoadSceneMode mode)
        {
            return mImpl.LoadScene(address, mode);
        }

        public bool IsInitialized => mImpl.Initialized;

        private GameObject mCacheGameObject;

        private GameObject CacheGameObject
        {
            get
            {
                if (!mCacheGameObject)
                {
                    mCacheGameObject = new GameObject("AssetManagerCache");
                    mCacheGameObject.SetActive(false);
                    GameObject.DontDestroyOnLoad(mCacheGameObject);
                }

                return mCacheGameObject;
            }
        }

        public Scene LoadSceneImmediate(object address, LoadSceneMode mode)
        {
            return mImpl.LoadSceneImmediate(address, mode);
        }

        public void UnloadScene(Scene scene)
        {
            mImpl.UnloadScene(scene);
        }

        public ResultAsyncOperation<T> LoadAsset<T>(object address) where T : Object
        {
            return mImpl.LoadAsset<T>(address);
        }

        public ResultAsyncOperation<GameObject> Instantiate(object address)
        {
            var assetAddress = AssetAddress.EvaluateAs<InstantiationAssetAddress>(address);
            var hash = assetAddress.GetHashCode();
            var cacheGameObject = mGameObjectCache.Get(hash);
            if (cacheGameObject)
            {
                assetAddress.PreSetInstance(cacheGameObject);
                assetAddress.Instantiate();
                return new ResultAsyncOperation<GameObject>(cacheGameObject);
            }

            return mImpl.Instantiate(assetAddress).Subscribe(go =>
            {
                if (go)
                {
                    mActiveGameObject.Add(go.GetInstanceID(), assetAddress.GetHashCode());
                }
            });
        }

        public CollectionResultAsyncOperation<T> LoadAssets<T>(IList<object> addresses) where T : Object
        {
            return mImpl.LoadAssets<T>(addresses);
        }

        public CollectionResultAsyncOperation<GameObject> Instantiates(IList<object> addresses)
        {
            foreach (var address in addresses)
            {
                var assetAddress = AssetAddress.EvaluateAs<InstantiationAssetAddress>(address);
                var hash = assetAddress.GetHashCode();
                var cacheGameObject = mGameObjectCache.Get(hash);
                if (cacheGameObject)
                {
                    assetAddress.PreSetInstance(cacheGameObject);
                }
            }

            return mImpl.Instantiates(addresses);
        }


        public void ReleaseInstance(GameObject obj, bool recursive)
        {
            if (recursive)
            {
                var all = obj.GetComponentsInChildren<Transform>();
                foreach (var t in all)
                {
                    var go = t.gameObject;
                    ReleaseInstance(go);
                }
            }
            else
            {
                ReleaseInstance(obj);
            }
        }

        public bool ReleaseInstance(GameObject obj)
        {
            if (!mActiveGameObject.TryGetValue(obj.GetInstanceID(), out var hash))
            {
                return false;
            }

            mActiveGameObject.Remove(obj.GetInstanceID());
            obj.transform.SetParent(CacheGameObject.transform);
            mGameObjectCache.Put(hash, obj);
            return true;
        }

        public void DestroyInstance(GameObject obj)
        {
            ReleaseInstance(obj, true);
            Object.Destroy(obj);
        }

        public void Release(Object asset)
        {
            mImpl.Release(asset);
        }

        public BaseAsyncOperation Initialize<T>(AssetManagerParam param) where T : IAssetImpl, new()
        {
            mImpl?.Dispose();
            mImpl = new T();
            if (mGameObjectCache == null)
            {
                mGameObjectCache = new KeyObjectPool<int, GameObject>((uint) param.InstanceCacheCapacity, RemoveGameObject);
            }

            return mImpl.Initialize(param);
        }

        private void RemoveGameObject(int hash, GameObject go)
        {
            mImpl.ReleaseInstance(go);
        }
    }

    public static class AssetManager
    {
        private static readonly InternalAssetManager sAssetManager = new InternalAssetManager();

        public static bool IsInitialized => sAssetManager.IsInitialized;

        public static ResultAsyncOperation<AsyncOperation> LoadScene(object address, LoadSceneMode mode)
        {
            return sAssetManager.LoadScene(address, mode);
        }

        public static Scene LoadSceneImmediate(object address, LoadSceneMode mode)
        {
            return sAssetManager.LoadSceneImmediate(address, mode);
        }

        public static void UnloadScene(Scene scene)
        {
            sAssetManager.UnloadScene(scene);
        }


        public static CollectionResultAsyncOperation<T> LoadAssets<T>(IList<object> addresses) where T : Object
        {
            return sAssetManager.LoadAssets<T>(addresses);
        }

        public static CollectionResultAsyncOperation<GameObject> Instantiates(IList<object> address)
        {
            return sAssetManager.Instantiates(address);
        }

        public static ResultAsyncOperation<T> LoadAsset<T>(object address) where T : Object
        {
            return sAssetManager.LoadAsset<T>(address);
        }

        public static void LoadAsset<T>(object address, Action<T> cb) where T : Object
        {
            sAssetManager.LoadAsset<T>(address).Subscribe(cb);
        }

        public static ResultAsyncOperation<GameObject> Instantiate(object address)
        {
            return sAssetManager.Instantiate(address);
        }

        public static ResultAsyncOperation<GameObject> Instantiate(object address, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            var assetAddress = AssetAddress.EvaluateAs<InstantiationAssetAddress>(address);
            assetAddress.Parameters = new InstantiationParameters(position, rotation, parent);
            return Instantiate(assetAddress);
        }

        public static ResultAsyncOperation<GameObject> Instantiate(object address, Transform parent, bool worldPositionStays = false)
        {
            var assetAddress = AssetAddress.EvaluateAs<InstantiationAssetAddress>(address);
            assetAddress.Parameters = new InstantiationParameters(parent, worldPositionStays);
            return Instantiate(assetAddress);
        }

        public static void Instantiate(object address, Action<GameObject> cb)
        {
            Instantiate(address).Subscribe(cb);
        }

        public static void Instantiate(object address, Transform parent, bool worldPositionStays, Action<GameObject> cb)
        {
            Instantiate(address, parent, worldPositionStays).Subscribe(cb);
        }

        public static void Instantiate(object address, Vector3 position, Quaternion rotation, Transform parent, Action<GameObject> cb)
        {
            Instantiate(address, position, rotation, parent).Subscribe(cb);
        }

        public static void Instantiate(object address, Vector3 position, Quaternion rotation, Action<GameObject> cb)
        {
            Instantiate(address, position, rotation).Subscribe(cb);
        }

        public static bool ReleaseInstance(GameObject obj)
        {
            return sAssetManager.ReleaseInstance(obj);
        }

        public static void Release(Object asset)
        {
            sAssetManager.Release(asset);
        }

        public static BaseAsyncOperation Initialize<T>(AssetManagerParam param) where T : IAssetImpl, new()
        {
            return sAssetManager.Initialize<T>(param);
        }
    }
}