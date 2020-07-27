using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DeepU3.Async;
using DeepU3.Cache;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace DeepU3.Asset
{
    internal class AsyncOperationUpdater : MonoBehaviour
    {
        [NonSerialized]
        internal static readonly List<BaseAsyncOperation> Operations = new List<BaseAsyncOperation>();

        private void LateUpdate()
        {
            for (var i = Operations.Count - 1; i >= 0; i--)
            {
                var op = Operations[i];
                if (op.InvokeNextFrame)
                {
                    op.InvokeCompleteEvent();
                    Operations.RemoveAt(i);
                }
                else if (op.IsDone)
                {
                    Operations.RemoveAt(i);
                }
                else
                {
                    op.Execute();
                }
            }
        }
    }

    internal class InternalAssetManager
    {
        private IAssetImpl mImpl;

        internal IAssetImpl Impl => mImpl;
        private KeyObjectPool<int, GameObject> mGameObjectPool;

        private readonly Dictionary<int, int> mActiveGameObject = new Dictionary<int, int>();
        private readonly HashSet<int> mDontCache = new HashSet<int>();


        public bool IsInitialized => mImpl.Initialized;

        public int LoadingAssetCount => mImpl.LoadingAssetCount;

        public IObjectPoolControl GameObjectPool => mGameObjectPool;
        public GameObject CacheGameObject => sCacheGameObject;

        public string[] LoadingScenes => mSceneList.Where(IsSceneLoading).ToArray();
        private static GameObject sCacheGameObject;

        private readonly HashSet<string> mSceneList = new HashSet<string>();
        private readonly HashSet<string> mUnloadSceneNext = new HashSet<string>();
        private readonly Dictionary<string, AsyncOperation> mUnloadOperations = new Dictionary<string, AsyncOperation>();

        [RuntimeInitializeOnLoadMethod]
        static void Init()
        {
            var obj = new GameObject("DeepU3");
            obj.AddComponent<AsyncOperationUpdater>();
            var statistics = obj.AddComponent<Statistics>();
            //todo 修改为配置, 或删除此功能
            // statistics.destroyedToUnloadUnused = 1000;
            GameObject.DontDestroyOnLoad(obj);
            sCacheGameObject = new GameObject("CacheParent");
            sCacheGameObject.transform.SetParent(obj.transform);
            sCacheGameObject.SetActive(false);
        }

        // internal InternalAssetManager()
        // {
        //     SceneManager.sceneLoaded += OnSceneLoaded;
        // }
        // private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        // {
        //     
        // }

        private void BeforeLoadScene(string scenePath)
        {
            mSceneList.Add(scenePath);
            Debug.Log($"start loading scene {Path.GetFileNameWithoutExtension(scenePath)}");
        }

        private string GetSceneAssetPath(string path, out ScenePathType sourcePathType)
        {
            var ext = Path.GetExtension(path);
            ScenePathType type;
            string scenePath;
            if (ext == ".unity")
            {
                //scene asset path
                type = ScenePathType.SceneAssetPath;
                scenePath = path;
            }
            else if (string.IsNullOrEmpty(ext))
            {
                //scene name
                type = ScenePathType.SceneName;
                scenePath = mImpl.SceneNameToScenePath(path);
            }
            else
            {
                //bundle path
                type = ScenePathType.Address;
                scenePath = mImpl.AddressToMainAssetPath(path);
                if (scenePath != null && !scenePath.EndsWith(".unity"))
                {
                    scenePath = null;
                }
            }

            sourcePathType = type;
            return scenePath;
        }

        public ResultAsyncOperation<AsyncOperation> LoadScene(string path, LoadSceneMode mode)
        {
            var scenePath = GetSceneAssetPath(path, out var type);
            if (mSceneList.Contains(scenePath) || mUnloadSceneNext.Contains(scenePath))
            {
                //todo 支持多处同时加载同一个场景
                return null;
            }

            BeforeLoadScene(scenePath);
            //ResultAsyncOperation<AsyncOperation> ret;
            //if (IsLoadPaused)
            //{
            //    ret = new ResultAsyncOperationDecorator<AsyncOperation>();
            //    WaitUnloadUnusedAssets(() => { ((ResultAsyncOperationDecorator<AsyncOperation>) ret).SetSourceAsyncOperation(mImpl.LoadScene(path, type, mode).Subscribe(op => mPauseAssetsOperation = op)); });
            //}
            //else
            //{
            //    ret = mImpl.LoadScene(path, type, mode).Subscribe(op => mPauseAssetsOperation = op);
            //}

            return mImpl.LoadScene(path, type, mode);
            // .Subscribe(op => mPauseAssetsOperation = op);
        }


        public Scene LoadSceneImmediate(string path, LoadSceneMode mode)
        {
            var scenePath = GetSceneAssetPath(path, out var type);
            if (mSceneList.Contains(scenePath))
            {
                return SceneManager.GetSceneByPath(scenePath);
            }

            if (mUnloadSceneNext.Contains(scenePath))
            {
                return new Scene();
            }

            BeforeLoadScene(scenePath);
            return mImpl.LoadSceneImmediate(path, type, mode);
        }

        public bool IsSceneLoading(string path)
        {
            var scenePath = GetSceneAssetPath(path, out _);
            return mSceneList.Contains(scenePath);
        }

        public bool IsSceneLoading(ref Scene s)
        {
            if (s.isLoaded)
            {
                return false;
            }

            return s.IsValid() || mSceneList.Contains(s.path);
        }

        public bool IsAnySceneLoading => mSceneList.Any(IsSceneLoading);

        private void UnloadScene(Scene scene, bool immediate)
        {
            mSceneList.Remove(scene.path);
            mImpl.UnloadScene(scene);
            if (!immediate)
            {
                var asyncOperation = SceneManager.UnloadSceneAsync(scene, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
                mUnloadOperations[scene.path] = asyncOperation;
            }
            else
            {
                throw new NotSupportedException();
                // SceneManager.UnloadScene(scene);
            }
        }


        // public void UnloadSceneImmediate(Scene scene)
        // {
        //     UnloadScene(scene, true);
        // }
        //
        // public void UnloadSceneImmediate(string path)
        // {
        //     UnloadScene(path, true);
        // }

        public void UnloadScene(Scene scene)
        {
            UnloadScene(scene, false);
        }

        public void UnloadScene(string path)
        {
            UnloadScene(path, false);
        }

        private void UnloadScene(string path, bool immediate)
        {
            var scenePath = GetSceneAssetPath(path, out _);

            var s = SceneManager.GetSceneByPath(scenePath);
            if (s.isLoaded)
            {
                UnloadScene(s, immediate);
            }
            else if (mSceneList.Contains(scenePath))
            {
                mUnloadSceneNext.Add(scenePath);
            }

            mSceneList.Remove(scenePath);
        }

        private bool IsLoadPaused => mPauseAssetsOperation != null && !mPauseAssetsOperation.isDone;

#if UNITY_EDITOR
        private readonly Dictionary<Action, StackTrace> mWaitCallTraceMap = new Dictionary<Action, StackTrace>();
#endif
        private void WaitUnloadUnusedAssets(Action callBack)
        {
            if (mPauseAssetsOperation == null || mPauseAssetsOperation.isDone)
            {
                callBack.Invoke();
                return;
            }
#if UNITY_EDITOR
            mWaitCallTraceMap.Add(callBack, new StackTrace(true));
#endif

            mPauseAssetsOperation.completed += operation =>
            {
                try
                {
                    callBack.Invoke();
                }
                catch (Exception e)
                {
#if UNITY_EDITOR
                    Debug.LogError(e.Message + e.StackTrace + mWaitCallTraceMap[callBack]);
#else
                    Debug.LogError(e.Message + e.StackTrace);
#endif
                    throw;
                }
#if UNITY_EDITOR
                mWaitCallTraceMap.Remove(callBack);
#endif
            };
        }

        public T LoadAssetImmediate<T>(AssetAddress address) where T : Object
        {
            return mImpl.LoadAssetImmediate<T>(address);
        }

        public GameObject InstantiateImmediate(InstantiationAssetAddress assetAddress)
        {
            var hash = assetAddress.GetHashCode();
            var cacheGameObject = GetGameObjectFromPool(hash);
            if (cacheGameObject)
            {
                assetAddress.PreSetInstance(cacheGameObject);
                assetAddress.Instantiate();
                cacheGameObject.BroadcastMessage("CacheAwake", SendMessageOptions.DontRequireReceiver);
                return cacheGameObject;
            }

            var go = mImpl.InstantiateImmediate(assetAddress);
            if (go)
            {
                OnLoadedGameObject(go, hash);
            }

            return go;
        }

        public ResultAsyncOperation<T> LoadAsset<T>(AssetAddress address) where T : Object
        {
            ResultAsyncOperation<T> ret;
            if (IsLoadPaused)
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


        public Object[] LoadAllAssetsImmediate(AssetAddress address)
        {
            return mImpl.LoadAllAssetsImmediate(address);
        }

        public T[] LoadAllAssetsImmediate<T>(AssetAddress address) where T : Object
        {
            return mImpl.LoadAllAssetsImmediate<T>(address);
        }

        public CollectionResultAsyncOperation<T> LoadAllAssets<T>(AssetAddress address) where T : Object
        {
            return mImpl.LoadAllAssets<T>(address);
        }


        public CollectionResultAsyncOperation<T> LoadAssets<T>(IList<AssetAddress> addresses) where T : Object
        {
            CollectionResultAsyncOperation<T> ret;
            if (IsLoadPaused)
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

        public ResultAsyncOperation<GameObject> Instantiate(InstantiationAssetAddress assetAddress)
        {
            var hash = assetAddress.GetHashCode();
            var cacheGameObject = GetGameObjectFromPool(hash);
            if (cacheGameObject)
            {
                assetAddress.PreSetInstance(cacheGameObject);
                assetAddress.Instantiate();
                cacheGameObject.BroadcastMessage("CacheAwake", SendMessageOptions.DontRequireReceiver);
                return new ResultAsyncOperation<GameObject>(cacheGameObject);
            }

            ResultAsyncOperation<GameObject> ret;
            if (IsLoadPaused)
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


        public CollectionResultAsyncOperation<GameObject> Instantiates(IList<InstantiationAssetAddress> addresses)
        {
            throw new NotImplementedException();
            foreach (var address in addresses)
            {
                var hash = address.GetHashCode();
                var cacheGameObject = GetGameObjectFromPool(hash);
                if (cacheGameObject)
                {
                    address.PreSetInstance(cacheGameObject);
                }
            }

            CollectionResultAsyncOperation<GameObject> ret;
            if (IsLoadPaused)
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

        private AsyncOperation mPauseAssetsOperation;

        public void UnloadUnusedAssets()
        {
            if (mPauseAssetsOperation == null || mPauseAssetsOperation.isDone)
            {
                Debug.Log("[AssetManager.UnloadUnusedAssets] starting");
                mPauseAssetsOperation = Resources.UnloadUnusedAssets();
            }
        }

        public void MarkInstanceDontCache(GameObject go)
        {
            mDontCache.Add(go.GetInstanceID());
        }

        public void MarkInstanceDestroyed(GameObject obj)
        {
            if (!mActiveGameObject.TryGetValue(obj.GetInstanceID(), out var hash))
            {
                return;
            }

            if (IsLoadPaused)
            {
                WaitUnloadUnusedAssets(() => { MarkInstanceDestroyed(obj); });
            }
            else
            {
                mImpl.ReleaseInstance(obj);
                Statistics.Instance.destroyed++;
            }
        }

        public bool IsReleasedInstance(GameObject obj)
        {
            return mGameObjectPool.ContainsValue(obj);
        }


        public bool ReleaseInstance(GameObject obj, bool destroy)
        {
            var instanceID = obj.GetInstanceID();
            if (!mActiveGameObject.TryGetValue(instanceID, out var hash))
            {
                Debug.LogWarning("ReleaseInstance not success: " + obj);
                return false;
            }

            if (IsLoadPaused)
            {
                WaitUnloadUnusedAssets(() => { ReleaseInstance(obj, destroy); });
            }
            else
            {
                if (!destroy && !mDontCache.Contains(instanceID))
                {
                    obj.BroadcastMessage("OnBeforeToCache", SendMessageOptions.DontRequireReceiver);
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
            if (IsLoadPaused)
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

        public void Dispose()
        {
            mImpl?.Dispose();
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

            SceneManager.sceneLoaded += SceneManagerOnSceneLoaded;
            return mImpl.Initialize(param);
        }

        private void SceneManagerOnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mUnloadSceneNext.Contains(scene.name))
            {
                UnloadScene(scene);
            }
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
        private static int sSynchronouslyRequest;

        public static bool IsInitialized => sAssetManager.IsInitialized;

        public static GameObject CacheGameObject => sAssetManager.CacheGameObject;

        public static IAssetPathConverter PathConverter => sAssetManager.Impl;

        public static ResultAsyncOperation<AsyncOperation> LoadScene(string sceneName, LoadSceneMode mode)
        {
            return sAssetManager.LoadScene(sceneName, mode);
        }


        public static bool IsRunSynchronouslyAsSoonAsPossible => sSynchronouslyRequest > 0;


        private struct Synchronously : IDisposable
        {
            public void Dispose()
            {
                sSynchronouslyRequest--;
            }
        }

        public static IDisposable TryRunSynchronously()
        {
            sSynchronouslyRequest++;
            return new Synchronously();
        }

        public static bool IsSceneLoading(string sceneName)
        {
            return sAssetManager.IsSceneLoading(sceneName);
        }


        public static bool IsSceneLoading(ref Scene s)
        {
            return sAssetManager.IsSceneLoading(ref s);
        }

        public static Scene LoadSceneImmediate(string sceneName, LoadSceneMode mode)
        {
            return sAssetManager.LoadSceneImmediate(sceneName, mode);
        }

        public static void UnloadScene(Scene scene)
        {
            sAssetManager.UnloadScene(scene);
        }

        public static void UnloadScene(string sceneName)
        {
            sAssetManager.UnloadScene(sceneName);
        }

        public static CollectionResultAsyncOperation<T> LoadAssets<T>(IList<string> addresses) where T : Object
        {
            var assetAddresses = addresses.Select(AssetAddress.String2Address).ToList();
            foreach (var address in assetAddresses)
            {
                if (IsRunSynchronouslyAsSoonAsPossible)
                {
                    address.IsRunSynchronously = true;
                }
            }

            return sAssetManager.LoadAssets<T>(assetAddresses);
        }

        public static CollectionResultAsyncOperation<GameObject> Instantiates(IList<string> addresses)
        {
            var instanceAddresses = addresses.Select(InstantiationAssetAddress.String2Address).ToList();
            return sAssetManager.Instantiates(instanceAddresses);
        }

        public static CollectionResultAsyncOperation<GameObject> Instantiates(IList<InstantiationAssetAddress> addresses)
        {
            for (var i = 0; i < addresses.Count; i++)
            {
                var address = addresses[i];
                if (IsRunSynchronouslyAsSoonAsPossible)
                {
                    address.IsRunSynchronously = true;
                }
            }

            return sAssetManager.Instantiates(addresses);
        }

        public static T LoadAssetImmediate<T>(string address) where T : Object
        {
            return LoadAssetImmediate<T>(AssetAddress.String2Address(address));
        }

        public static T LoadAssetImmediate<T>(AssetAddress address) where T : Object
        {
            return sAssetManager.LoadAssetImmediate<T>(address);
        }

        public static GameObject InstantiateImmediate(string address)
        {
            return InstantiateImmediate(InstantiationAssetAddress.String2Address(address));
        }

        public static GameObject InstantiateImmediate(InstantiationAssetAddress address)
        {
            return sAssetManager.InstantiateImmediate(address);
        }

        public static GameObject InstantiateImmediate(string address, Vector3 position, Quaternion rotation, Transform parent = null, bool worldPositionStays = true)
        {
            var assetAddress = InstantiationAssetAddress.String2Address(address);
            assetAddress.Parameters = new InstantiationParameters(position, rotation, parent, worldPositionStays);
            return InstantiateImmediate(assetAddress);
        }

        public static ResultAsyncOperation<T> LoadAsset<T>(AssetAddress address) where T : Object
        {
            if (IsRunSynchronouslyAsSoonAsPossible)
            {
                address.IsRunSynchronously = true;
            }

            return sAssetManager.LoadAsset<T>(address);
        }

        public static ResultAsyncOperation<T> LoadAsset<T>(string address) where T : Object
        {
            return LoadAsset<T>(AssetAddress.String2Address(address));
        }


        public static T[] LoadAllAssetsImmediate<T>(AssetAddress address) where T : Object
        {
            if (IsRunSynchronouslyAsSoonAsPossible)
            {
                address.IsRunSynchronously = true;
            }

            return sAssetManager.LoadAllAssetsImmediate<T>(address);
        }

        public static T[] LoadAllAssetsImmediate<T>(string address) where T : Object
        {
            return LoadAllAssetsImmediate<T>(AssetAddress.String2Address(address));
        }

        public static CollectionResultAsyncOperation<T> LoadAllAssets<T>(string address) where T : Object
        {
            return LoadAllAssets<T>(AssetAddress.String2Address(address));
        }

        public static CollectionResultAsyncOperation<T> LoadAllAssets<T>(AssetAddress address) where T : UnityEngine.Object
        {
            if (IsRunSynchronouslyAsSoonAsPossible)
            {
                address.IsRunSynchronously = true;
            }

            return sAssetManager.LoadAllAssets<T>(address);
        }

        public static void LoadAsset<T>(string address, Action<T> cb) where T : Object
        {
            LoadAsset<T>(address).Subscribe(cb);
        }

        public static ResultAsyncOperation<GameObject> Instantiate(string address)
        {
            return Instantiate(InstantiationAssetAddress.String2Address(address));
        }

        public static ResultAsyncOperation<GameObject> Instantiate(InstantiationAssetAddress address)
        {
            if (IsRunSynchronouslyAsSoonAsPossible)
            {
                address.IsRunSynchronously = true;
            }

            return sAssetManager.Instantiate(address);
        }

        public static ResultAsyncOperation<GameObject> Instantiate(string address, Vector3 position, Quaternion rotation, Transform parent, bool worldPosition)
        {
            var assetAddress = InstantiationAssetAddress.String2Address(address);
            assetAddress.Parameters = new InstantiationParameters(position, rotation, parent, worldPosition);
            return Instantiate(assetAddress);
        }

        public static ResultAsyncOperation<GameObject> Instantiate(string address, Transform parent, bool worldPositionStays)
        {
            var assetAddress = InstantiationAssetAddress.String2Address(address);
            assetAddress.Parameters = new InstantiationParameters(parent, worldPositionStays);
            return Instantiate(assetAddress);
        }

        public static ResultAsyncOperation<GameObject> Instantiate(InstantiationAssetAddress address, Transform parent, bool worldPositionStays)
        {
            address.Parameters = new InstantiationParameters(parent, worldPositionStays);
            return Instantiate(address);
        }

        public static void Instantiate(string address, Action<GameObject> cb)
        {
            Instantiate(address).Subscribe(cb);
        }

        public static void Instantiate(InstantiationAssetAddress address, Action<GameObject> cb)
        {
            Instantiate(address).Subscribe(cb);
        }

        public static void Instantiate(string address, Transform parent, bool worldPositionStays, Action<GameObject> cb)
        {
            Instantiate(address, parent, worldPositionStays).Subscribe(cb);
        }

        public static void Instantiate(string address, Vector3 position, Quaternion rotation, Transform parent, bool worldPosition, Action<GameObject> cb)
        {
            Instantiate(address, position, rotation, parent, worldPosition).Subscribe(cb);
        }

        public static void Instantiate(string address, Vector3 position, Quaternion rotation, Action<GameObject> cb)
        {
            Instantiate(address, position, rotation, null, true).Subscribe(cb);
        }

        public static bool ReleaseInstance(GameObject obj)
        {
            return sAssetManager.ReleaseInstance(obj, false);
        }

        public static void MarkInstanceDontCache(GameObject obj)
        {
            sAssetManager.MarkInstanceDontCache(obj);
        }

        public static void MarkInstanceDestroyed(GameObject obj)
        {
            sAssetManager.MarkInstanceDestroyed(obj);
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


        public static void Cleanup()
        {
            sAssetManager.Dispose();
        }

        public static void UnloadUnusedAssets()
        {
            sAssetManager.UnloadUnusedAssets();
        }

        // public static void UnloadSceneImmediate(Scene scene) => sAssetManager.UnloadSceneImmediate(scene);
        //
        // public static void UnloadSceneImmediate(string path) => sAssetManager.UnloadSceneImmediate(path);
        public static IObjectPoolControl GameObjectPool => sAssetManager.GameObjectPool;
        public static int LoadingAssetCount => sAssetManager.LoadingAssetCount;

        public static bool IsAnySceneLoading => sAssetManager.IsAnySceneLoading;
        public static AssetAddress[] LoadingAssets => sAssetManager.Impl.LoadingAssets;

        public static string[] LoadingScenes => sAssetManager.LoadingScenes;
    }
}