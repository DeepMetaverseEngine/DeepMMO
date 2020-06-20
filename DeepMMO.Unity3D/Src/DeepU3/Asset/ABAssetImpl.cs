using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DeepU3.AssetBundles;
using DeepU3.Async;
using DeepU3.Cache;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using Path = System.IO.Path;

namespace DeepU3.Asset
{
    public class ABAssetImpl : IAssetImpl
    {
        internal class AssetContainer : ReferencesContainer
        {
            public readonly UnityEngine.Object Asset;
            public readonly AssetBundleData Data;

            public AssetContainer(LoadingContainer container, Object asset) : base(container.Address)
            {
                References = container.References;
                Data = container.Data;
                Asset = asset;
            }

            public AssetContainer(AssetAddress address, int references, AssetBundle bundle, Object asset) : base(address)
            {
                References = references;
                Data = new AssetBundleData(bundle);
                Asset = asset;
            }
        }

        internal class AssetContainer<T> : AssetContainer
        {
            public readonly ResultAsyncOperation<T> Operation;

            public AssetContainer(LoadingContainer container, Object asset, ResultAsyncOperation<T> operation) : base(container, asset)
            {
                Operation = operation;
            }

            public AssetContainer(AssetAddress address, int references, AssetBundle bundle, Object asset, ResultAsyncOperation<T> operation) : base(address, references, bundle, asset)
            {
                Operation = operation;
            }
        }

        public class AssetBundleData
        {
            public AssetBundle Bundle;
            private string[] mAssetNames;
            public string[] AssetNames => mAssetNames ?? (mAssetNames = Bundle?.GetAllAssetNames());

            public AssetBundleData(AssetBundle assetBundle)
            {
                Bundle = assetBundle;
            }

            public string GetAssetPath(string name)
            {
                if (AssetNames.Length == 1)
                {
                    return AssetNames[0];
                }

                return Bundle.mainAsset ? Bundle.mainAsset.name : Path.GetFileNameWithoutExtension(name);
            }
        }

        internal class LoadingContainer : ReferencesContainer
        {
            public AssetBundleData Data;

#if UNITY_EDITOR
            public StackTrace LoadTrace;
#endif
            public LoadingContainer(AssetAddress assetPath) : base(assetPath)
            {
            }
        }

        internal class LoadingContainer<T> : LoadingContainer
        {
            public readonly ResultAsyncOperation<T> Operation;

            public LoadingContainer(AssetAddress assetPath, ResultAsyncOperation<T> operation) : base(assetPath)
            {
                Operation = operation;
            }
        }

        private AssetBundleProfileManifest mProfileManifest;

        private readonly Dictionary<int, AssetContainer> mAssets = new Dictionary<int, AssetContainer>();
        private readonly Dictionary<int, LoadingContainer> mLoadingAssets = new Dictionary<int, LoadingContainer>();
        private readonly Dictionary<int, int> mAssetsHash = new Dictionary<int, int>();
        private readonly Dictionary<int, int> mGameObjectsHash = new Dictionary<int, int>();
        private AssetBundleManager mAbm;
        public int LoadingAssetCount => mLoadingAssets.Count;

        private void OnLoadAssetComplete<T>(LoadingContainer<T> con, Object obj) where T : Object
        {
            var hash = con.GetHashCode();
            if (!mLoadingAssets.Remove(hash))
            {
                throw new SystemException($"what ? {con.Address.Address}");
            }

            var assetContainer = new AssetContainer<T>(con, obj, con.Operation);
            if (obj)
            {
                mAssets.Add(hash, assetContainer);
                mAssetsHash.Add(obj.GetInstanceID(), hash);
#if UNITY_EDITOR
                Statistics.Instance?.AddAsset(obj, con.Address.Address, con.LoadTrace);
#endif
            }
            else if (con.Data != null)
            {
                mAbm.UnloadBundle(con.Data.Bundle);
            }

            assetContainer.Operation.SetComplete(obj as T);
        }

        private void OnAssetBundleComplete<T>(LoadingContainer<T> loadingContainer, AssetBundle bundle) where T : Object
        {
            if (bundle == null)
            {
                OnLoadAssetComplete(loadingContainer, null);
            }
            else
            {
                var data = new AssetBundleData(bundle);
                loadingContainer.Data = data;
                var assetKey = data.GetAssetPath(bundle.name);

                var key = loadingContainer.Address.Key ?? assetKey;
                if (loadingContainer.Address.IsRunSynchronously)
                {
                    var asset = bundle.LoadAsset<T>(key);
                    OnLoadAssetComplete(loadingContainer, asset);
                    return;
                }

                var rq = bundle.LoadAssetAsync<T>(key);
                if (rq.isDone)
                {
                    OnLoadAssetComplete(loadingContainer, rq.asset);
                }
                else
                {
                    rq.completed += operation => { OnLoadAssetComplete(loadingContainer, rq.asset); };
                }
            }
        }

        private void OnBundleAllAssetLoaded<T>(string address, T[] all) where T : Object
        {
            foreach (var obj in all)
            {
                var objAssetAddress = new AssetAddress(address, obj.name + obj.GetHashCode());
                var con = NewLoadingContainer<UnityEngine.Object>(objAssetAddress);
                OnLoadAssetComplete(con, obj);
            }
        }

        public Object[] LoadAllAssetsImmediate(object address)
        {
            return LoadAllAssetsImmediate<Object>(address);
        }

        public T[] LoadAllAssetsImmediate<T>(object address) where T : Object
        {
            var assetAddress = AssetAddress.EvaluateAddress(address);
            var bundle = mAbm.GetBundleImmediate(FixBundlePath(assetAddress.Address));
            if (bundle == null)
            {
                return null;
            }

            var all = bundle.LoadAllAssets<T>();
            OnBundleAllAssetLoaded(assetAddress.Address, all);
            return all;
        }

        private string FixBundlePath(string abName)
        {
            var ret = mProfileManifest?.AssetPathToAssetBundleName(abName);
            return ret ?? abName;
        }

        public CollectionResultAsyncOperation<T> LoadAllAssets<T>(object address) where T : Object
        {
            var op = new CollectionResultAsyncOperation<T>();
            var assetAddress = AssetAddress.EvaluateAddress(address);
            var opt = assetAddress.IsRunSynchronously ? AssetBundleLoadOption.TryImmediate : AssetBundleLoadOption.Async;
            mAbm.GetBundle(FixBundlePath(assetAddress.Address), opt, ab =>
            {
                var req = ab.LoadAllAssetsAsync<T>();
                if (req.isDone)
                {
                    op.TrySetResults(req.allAssets);
                    OnBundleAllAssetLoaded(assetAddress.Address, req.allAssets);
                }
                else
                {
                    req.completed += operation =>
                    {
                        op.TrySetResults(req.allAssets);
                        OnBundleAllAssetLoaded(assetAddress.Address, req.allAssets);
                    };
                }
            });
            return op;
        }

        public ResultAsyncOperation<AsyncOperation> LoadScene(string path, ScenePathType pathType, LoadSceneMode mode)
        {
            var bundleName = GetSceneAddress(path, pathType);
            var ret = new ResultAsyncOperation<AsyncOperation>();
            mAbm.LoadScene(bundleName, mode, op => { ret.SetComplete(op); });
            return ret;
        }

        private string GetSceneAddress(string path, ScenePathType pathType)
        {
            string abName;
            switch (pathType)
            {
                case ScenePathType.SceneName:
                    abName = SceneNameToAddress(path);
                    break;
                case ScenePathType.SceneAssetPath:
                    abName = AssetPathToAddress(path);
                    break;
                case ScenePathType.Address:
                    abName = path;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(pathType), pathType, null);
            }

            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError($"not found address for {path}");
            }

            return abName;
        }

        public Scene LoadSceneImmediate(string path, ScenePathType pathType, LoadSceneMode mode)
        {
            var bundleName = GetSceneAddress(path, pathType);
            var ret = mAbm.LoadSceneImmediate(bundleName, mode);
            return ret;
        }

        private LoadingContainer<T> NewLoadingContainer<T>(AssetAddress assetAddress)
        {
            var lCon = new LoadingContainer<T>(assetAddress, new ResultAsyncOperation<T>());
#if UNITY_EDITOR
            lCon.LoadTrace = new StackTrace(true);
#endif
            mLoadingAssets.Add(assetAddress.GetHashCode(), lCon);
            return lCon;
        }

        public ResultAsyncOperation<T> LoadAsset<T>(object address) where T : Object
        {
            var assetAddress = AssetAddress.EvaluateAddress(address);
            var hash = assetAddress.GetHashCode();
            if (mAssets.TryGetValue(hash, out var container))
            {
                container.References++;
                return ((AssetContainer<T>) container).Operation;
            }

            if (mLoadingAssets.TryGetValue(hash, out var loadingContainer))
            {
                loadingContainer.References++;
                return ((LoadingContainer<T>) loadingContainer).Operation;
            }

            var lCon = NewLoadingContainer<T>(assetAddress);
            var opt = assetAddress.IsRunSynchronously ? AssetBundleLoadOption.TryImmediate : AssetBundleLoadOption.Async;
            mAbm.GetBundle(FixBundlePath(assetAddress.Address), opt, bundle => { OnAssetBundleComplete<T>(lCon, bundle); });
            return lCon.Operation;
        }

        public T LoadAssetImmediate<T>(object address) where T : Object
        {
            var assetAddress = AssetAddress.EvaluateAddress(address);
            var hash = assetAddress.GetHashCode();
            if (mAssets.TryGetValue(hash, out var container))
            {
                container.References++;
                return (T) container.Asset;
            }

            if (mLoadingAssets.TryGetValue(hash, out var loadingContainer))
            {
                loadingContainer.References++;
            }
            else
            {
                loadingContainer = NewLoadingContainer<T>(assetAddress);
            }

            var bundle = mAbm.GetBundleImmediate(FixBundlePath(assetAddress.Address));

            if (bundle == null)
            {
                OnLoadAssetComplete<T>((LoadingContainer<T>) loadingContainer, null);
                return null;
            }

            loadingContainer.Data = new AssetBundleData(bundle);
            var key = loadingContainer.Address.Key ?? loadingContainer.Data.GetAssetPath(bundle.name);
            var asset = bundle.LoadAsset<T>(key);
            OnLoadAssetComplete<T>((LoadingContainer<T>) loadingContainer, asset);
            return asset;
        }

        public GameObject InstantiateImmediate(object address)
        {
            var assetAddress = AssetAddress.EvaluateAs<InstantiationAssetAddress>(address);
            var asset = LoadAssetImmediate<GameObject>(address);
            assetAddress.PreSetAsset(asset);
            var obj = assetAddress.Instantiate();
            if (obj)
            {
                mGameObjectsHash.Add(obj.GetInstanceID(), address.GetHashCode());
            }

            return obj;
        }

        public ResultAsyncOperation<GameObject> Instantiate(object address)
        {
            var assetAddress = AssetAddress.EvaluateAs<InstantiationAssetAddress>(address);
            var ret = new ResultAsyncOperation<GameObject>();
            if (!assetAddress.ExistsParts)
            {
                if (assetAddress.Instance != null)
                {
                    assetAddress.Instantiate();
                    ret.SetComplete(assetAddress.Instance);
                }
                else
                {
                    var op1 = LoadAsset<GameObject>(address);
                    op1.Subscribe(ao =>
                    {
                        var asset = ao.Result;
                        assetAddress.PreSetAsset(asset);
                        var obj = assetAddress.Instantiate();
                        if (obj)
                        {
                            mGameObjectsHash.Add(obj.GetInstanceID(), address.GetHashCode());
                        }

                        ret.SetComplete(obj);
                    });
                }
            }
            else
            {
                var listAll = new List<InstantiationAssetAddress>();
                assetAddress.GetAllDependencies(listAll);
                listAll.RemoveAll(e => e.Instance != null);

                var all = listAll.Select(LoadAsset<GameObject>);
                var op = new CollectionResultAsyncOperation<GameObject>(all);
                op.Subscribe((GameObject[] gos) =>
                {
                    var go = assetAddress.Instantiate();
                    ret.SetComplete(go);
                    foreach (var entry in listAll)
                    {
                        if (entry.Instance != null)
                        {
                            mGameObjectsHash.Add(entry.Instance.GetInstanceID(), entry.GetHashCode());
                        }
                    }
                });
            }

            return ret;
        }

        public CollectionResultAsyncOperation<T> LoadAssets<T>(IList<object> addresses) where T : Object
        {
            var all = addresses.Select(LoadAsset<T>);
            return new CollectionResultAsyncOperation<T>(all);
        }

        public CollectionResultAsyncOperation<GameObject> Instantiates(IList<object> addresses)
        {
            var all = addresses.Select(Instantiate);
            var dSync = new CollectionResultAsyncOperation<GameObject>(all);
            return dSync;
        }

        public bool ReleaseInstance(GameObject obj)
        {
            if (!mGameObjectsHash.TryGetValue(obj.GetInstanceID(), out var hash) || !mAssets.TryGetValue(hash, out var con))
            {
                return false;
            }

            Object.Destroy(obj);
            Release(con);
            return true;
        }

        public void UnloadScene(Scene scene)
        {
            var bundleName = AssetPathToAddress(scene.path);
            mAbm.UnloadBundle(bundleName);
        }


        private void Release(AssetContainer con)
        {
            if (--con.References <= 0)
            {
                mAssetsHash.Remove(con.Asset.GetInstanceID());
                mAssets.Remove(con.GetHashCode());
#if UNITY_EDITOR
                Statistics.Instance?.RemoveAsset(con.Asset, con.Address.Address);
#endif
                mAbm.UnloadBundle(con.Data.Bundle);
            }
        }


        public void Release(Object asset)
        {
            if (!mAssetsHash.TryGetValue(asset.GetInstanceID(), out var hash) || !mAssets.TryGetValue(hash, out var con))
            {
                return;
            }

            Release(con);
        }

        public void Release<T>(T[] assets) where T : Object
        {
            foreach (var asset in assets)
            {
                Release(asset);
            }
        }


        private void InitAssetBundleProfileManifest(AssetBundleProfileManifest profileManifest)
        {
            mProfileManifest = profileManifest;
            mAbm.SetAssetBundleProfileManifest(mProfileManifest);
        }

        public BaseAsyncOperation Initialize(AssetManagerParam param)
        {
            mAbm?.Dispose();

            var p = param as ABAssetManagerParam;
            mAbm = new AssetBundleManager(p.PlatformBundlePath, p.UseLowerCasePlatform, p.BundleCacheCapacity);
            mAbm.SetHandler(p.Handler);
            mAbm.SetBaseUri(p.BaseUrl);
            BaseAsyncOperation ret = new DecoratorAsyncOperation(mAbm.InitializeAsync());
            var ab = nameof(AssetBundleProfileManifest).ToLower();
            ret = ret.ContinueWith(_ => { return LoadAsset<AssetBundleProfileManifest>(ab).ContinueWith(op => InitAssetBundleProfileManifest(((ResultAsyncOperation<AssetBundleProfileManifest>) op).Result)); });
            return ret;
        }


        public bool Initialized => mAbm?.Initialized ?? false;

        public IObjectPoolControl BundlePool => mAbm.BundlePool;

        public void Dispose()
        {
            mAbm.Dispose();
        }

        public AssetAddress[] LoadingAssets
        {
            get { return mLoadingAssets.Values.Select(m => m.Address).ToArray(); }
        }


        private string SceneNameToAddress(string sceneName)
        {
            return mProfileManifest?.SceneNameToAssetBundleName(sceneName);
        }


        public string AssetPathToAddress(string address)
        {
            return mProfileManifest?.AssetPathToAssetBundleName(address);
        }


        public string SceneNameToScenePath(string sceneName)
        {
            return mProfileManifest?.SceneNameToAssetPath(sceneName);
        }

        public string AddressToScenePath(string address)
        {
            return mProfileManifest?.AssetBundleNameToScenePath(address);
        }
    }
}