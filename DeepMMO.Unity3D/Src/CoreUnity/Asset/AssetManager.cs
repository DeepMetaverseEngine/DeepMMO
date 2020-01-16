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

        private KeyObjectPool<int, GameObject> mGameObjectPool;

        private readonly Dictionary<int, int> mActiveGameObject = new Dictionary<int, int>();

        public ResultAsyncOperation<AsyncOperation> LoadScene(object address, LoadSceneMode mode)
        {
            return mImpl.LoadScene(address, mode);
        }

        public string GameObjectFileExtension => mImpl.GameObjectFileExtension;
        public bool IsInitialized => mImpl.Initialized;


        public IObjectPoolControl GameObjectPool => mGameObjectPool;
        public IObjectPoolControl BundlePool => mImpl.BundlePool;

        private static GameObject sCacheGameObject;

        [RuntimeInitializeOnLoadMethod]
        static void Init()
        {
            var obj = new GameObject("CoreUnity.Asset.AssetManager");
            var statistics = obj.AddComponent<Statistics>();
            //todo 修改为配置, 或删除此功能
            statistics.destroyedToUnloadUnused = 1000;
            GameObject.DontDestroyOnLoad(obj);
#if UNITY_EDITOR
            new AssetManagerDebug(obj);
#endif
            sCacheGameObject = new GameObject("CacheParent");
            sCacheGameObject.transform.SetParent(obj.transform);
            sCacheGameObject.SetActive(false);
        }

        public Scene LoadSceneImmediate(object address, LoadSceneMode mode)
        {
            return mImpl.LoadSceneImmediate(address, mode);
        }

        public void UnloadScene(Scene scene)
        {
            mImpl.UnloadScene(scene);
        }

        private bool IsUnusedAssetsUnloading => mUnloadUnusedAssetsOperation != null && !mUnloadUnusedAssetsOperation.isDone;

        private void WaitUnloadUnusedAssets(Action callBack)
        {
            mUnloadUnusedAssetsOperation.completed += operation => { callBack.Invoke(); };
        }

        public T LoadAssetImmediate<T>(object address) where T : Object
        {
            return mImpl.LoadAssetImmediate<T>(address);
        }

        public GameObject InstantiateImmediate(object address)
        {
            var assetAddress = AssetAddress.EvaluateAs<InstantiationAssetAddress>(address);
            var hash = assetAddress.GetHashCode();
            var cacheGameObject = GetGameObjectFromPool(hash);
            if (cacheGameObject)
            {
                assetAddress.PreSetInstance(cacheGameObject);
                assetAddress.Instantiate();
                return cacheGameObject;
            }

            var go = mImpl.InstantiateImmediate(address);
            OnLoadedGameObject(go, hash);
            return go;
        }

        public ResultAsyncOperation<T> LoadAsset<T>(object address) where T : Object
        {
            ResultAsyncOperation<T> ret;
            if (IsUnusedAssetsUnloading)
            {
                ret = new ResultAsyncOperationDecorator<T>();
                WaitUnloadUnusedAssets(() => { ((ResultAsyncOperationDecorator<T>) ret).SetSourceAsyncOperation(LoadAsset<T>(address)); });
            }
            else
            {
                ret = mImpl.LoadAsset<T>(address);
            }

            return ret;
        }

        public CollectionResultAsyncOperation<T> LoadAssets<T>(IList<object> addresses) where T : Object
        {
            CollectionResultAsyncOperation<T> ret;
            if (IsUnusedAssetsUnloading)
            {
                ret = new CollectionResultAsyncOperation<T>();
                WaitUnloadUnusedAssets(() => { ret.SetPreEnumerator(LoadAssets<T>(addresses).PreEnumerators); });
            }
            else
            {
                ret = mImpl.LoadAssets<T>(addresses);
            }

            return ret;
        }

        private GameObject GetGameObjectFromPool(int hash)
        {
            var cacheGameObject = mGameObjectPool.Get(hash);
            if (cacheGameObject)
            {
                OnLoadedGameObject(cacheGameObject, hash);
                return cacheGameObject;
            }

            return null;
        }

        private void OnLoadedGameObject(GameObject go, int hash)
        {
            mActiveGameObject.Add(go.GetInstanceID(), hash);
        }

        public ResultAsyncOperation<GameObject> Instantiate(object address)
        {
            var assetAddress = AssetAddress.EvaluateAs<InstantiationAssetAddress>(address);
            var hash = assetAddress.GetHashCode();
            var cacheGameObject = GetGameObjectFromPool(hash);
            if (cacheGameObject)
            {
                assetAddress.PreSetInstance(cacheGameObject);
                assetAddress.Instantiate();
                return new ResultAsyncOperation<GameObject>(cacheGameObject);
            }

            ResultAsyncOperation<GameObject> ret;
            if (IsUnusedAssetsUnloading)
            {
                ret = new ResultAsyncOperation<GameObject>();
                WaitUnloadUnusedAssets(() =>
                {
                    mImpl.Instantiate(assetAddress).Subscribe(go =>
                    {
                        if (go)
                        {
                            OnLoadedGameObject(go, hash);
                        }

                        ret.SetComplete(go);
                    });
                });
            }
            else
            {
                ret = mImpl.Instantiate(assetAddress).Subscribe(go =>
                {
                    if (go)
                    {
                        OnLoadedGameObject(go, hash);
                    }
                });
            }

            return ret;
        }


        public CollectionResultAsyncOperation<GameObject> Instantiates(IList<object> addresses)
        {
            foreach (var address in addresses)
            {
                var assetAddress = AssetAddress.EvaluateAs<InstantiationAssetAddress>(address);
                var hash = assetAddress.GetHashCode();
                var cacheGameObject = mGameObjectPool.Get(hash);
                if (cacheGameObject)
                {
                    assetAddress.PreSetInstance(cacheGameObject);
                }
            }

            CollectionResultAsyncOperation<GameObject> ret;
            if (IsUnusedAssetsUnloading)
            {
                ret = new CollectionResultAsyncOperation<GameObject>();
                WaitUnloadUnusedAssets(() => { ret.SetPreEnumerator(mImpl.Instantiates(addresses).PreEnumerators); });
            }
            else
            {
                ret = mImpl.Instantiates(addresses);
            }

            return ret;
        }

        private AsyncOperation mUnloadUnusedAssetsOperation;

        public void UnloadUnusedAssets()
        {
            if (mUnloadUnusedAssetsOperation == null || mUnloadUnusedAssetsOperation.isDone)
            {
                Debug.Log("[AssetManager.UnloadUnusedAssets] starting");
                mUnloadUnusedAssetsOperation = Resources.UnloadUnusedAssets();
            }
        }

        public bool ReleaseInstance(GameObject obj, bool destroy)
        {
            if (!mActiveGameObject.TryGetValue(obj.GetInstanceID(), out var hash))
            {
                Debug.LogWarning("ReleaseInstance not success: " + obj);
                return false;
            }

            if (IsUnusedAssetsUnloading)
            {
                WaitUnloadUnusedAssets(() => { ReleaseInstance(obj, destroy); });
            }
            else
            {
                if (!destroy)
                {
                    mActiveGameObject.Remove(obj.GetInstanceID());
                    obj.transform.SetParent(sCacheGameObject.transform);
                    mGameObjectPool.Put(hash, obj);
                }
                else
                {
                    RemoveGameObject(hash, obj);
                }
            }

            return true;
        }


        public void Release(Object asset)
        {
            if (IsUnusedAssetsUnloading)
            {
                WaitUnloadUnusedAssets(() => { Release(asset); });
            }
            else
            {
                mImpl.Release(asset);
            }
        }

        private void RemoveGameObject(int hash, GameObject go)
        {
            if (!mImpl.ReleaseInstance(go))
            {
                Debug.LogWarning("RemoveGameObject not success: " + go);
                Object.Destroy(go);
            }

            Statistics.Instance.destroyed++;
        }

        public BaseAsyncOperation Initialize<T>(AssetManagerParam param) where T : IAssetImpl, new()
        {
            mImpl?.Dispose();
            mImpl = new T();
            if (mGameObjectPool == null)
            {
                mGameObjectPool = new KeyObjectPool<int, GameObject>(param.InstanceCacheCapacity, RemoveGameObject, OnBeforePutGameObject, OnHitGameObject);
            }
            else
            {
                mGameObjectPool.Capacity = param.InstanceCacheCapacity;
            }

            return mImpl.Initialize(param);
        }

        private void OnHitGameObject(int key, GameObject obj)
        {
        }

        private bool OnBeforePutGameObject(int key, GameObject obj)
        {
            return true;
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

        public static T LoadAssetImmediate<T>(object address) where T : Object
        {
            return sAssetManager.LoadAssetImmediate<T>(address);
        }

        public static GameObject InstantiateImmediate(object address)
        {
            return sAssetManager.InstantiateImmediate(address);
        }

        public static GameObject InstantiateImmediate(object address, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            var assetAddress = AssetAddress.EvaluateAs<InstantiationAssetAddress>(address);
            assetAddress.Parameters = new InstantiationParameters(position, rotation, parent);
            return InstantiateImmediate(assetAddress);
        }

        public static ResultAsyncOperation<T> LoadAsset<T>(object address) where T : Object
        {
            return sAssetManager.LoadAsset<T>(address);
        }

        public static void LoadAsset<T>(object address, Action<T> cb) where T : Object
        {
            LoadAsset<T>(address).Subscribe(cb);
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
            return sAssetManager.ReleaseInstance(obj, false);
        }

        public static void DestroyInstance(GameObject obj) => sAssetManager.ReleaseInstance(obj, true);

        public static void Release(Object asset)
        {
            sAssetManager.Release(asset);
        }

        public static BaseAsyncOperation Initialize<T>(AssetManagerParam param) where T : IAssetImpl, new()
        {
            return sAssetManager.Initialize<T>(param);
        }

        public static void UnloadUnusedAssets()
        {
            sAssetManager.UnloadUnusedAssets();
        }

        public static IObjectPoolControl GameObjectPool => sAssetManager.GameObjectPool;
        public static IObjectPoolControl BundlePool => sAssetManager.BundlePool;
        public static string GameObjectFileExtension => sAssetManager.GameObjectFileExtension;
    }
}