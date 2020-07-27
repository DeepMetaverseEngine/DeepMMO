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
            public readonly Object Asset;
            public readonly AssetBundle Bundle;

            public AssetContainer(LoadingContainer container, Object asset) : base(container.Address)
            {
                References = container.References;
                Bundle = container.Bundle;
                Asset = asset;
            }

            public AssetContainer(AssetAddress address, int references, AssetBundle bundle, Object asset) : base(address)
            {
                References = references;
                Bundle = bundle;
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

        internal class LoadingContainer : ReferencesContainer
        {
            public AssetBundle Bundle;

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
            else if (con.Bundle != null)
            {
                mAbm.UnloadBundle(con.Bundle);
            }

            assetContainer.Operation.SetComplete(obj as T);
        }

        private readonly Dictionary<string, string> mBundleMainKey = new Dictionary<string, string>();

        public string GetAssetKey(AssetBundle bundle, AssetAddress address)
        {
            if (!string.IsNullOrEmpty(address.Key))
            {
                return address.Key;
            }

            var bundleName = address.Address;
            if (mBundleMainKey.TryGetValue(bundleName, out var key))
            {
                return key;
            }

            if (bundle.mainAsset)
            {
                key = bundle.mainAsset.name;
            }
            else
            {
                key = AddressToMainAssetPath(bundleName);
                if (string.IsNullOrEmpty(key))
                {
                    var names = bundle.GetAllAssetNames();
                    key = names.Length == 1 ? names[0] : Path.GetFileNameWithoutExtension(bundleName);
                }
            }

            mBundleMainKey[bundleName] = key;
            return key;
        }

        private void OnAssetBundleComplete<T>(LoadingContainer<T> loadingContainer, AssetBundle bundle) where T : Object
        {
            if (bundle == null)
            {
                OnLoadAssetComplete(loadingContainer, null);
            }
            else
            {
                loadingContainer.Bundle = bundle;
                var assetKey = GetAssetKey(bundle, loadingContainer.Address);

                if (loadingContainer.Address.IsRunSynchronously)
                {
                    var asset = bundle.LoadAsset<T>(assetKey);
                    OnLoadAssetComplete(loadingContainer, asset);
                    return;
                }

                var rq = bundle.LoadAssetAsync<T>(assetKey);
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
                var objAssetAddress = AssetAddress.String2Address(address, obj.name + obj.GetHashCode());
                var con = NewLoadingContainer<UnityEngine.Object>(objAssetAddress);
                OnLoadAssetComplete(con, obj);
            }
        }

        public Object[] LoadAllAssetsImmediate(AssetAddress address)
        {
            return LoadAllAssetsImmediate<Object>(address);
        }

        public T[] LoadAllAssetsImmediate<T>(AssetAddress assetAddress) where T : Object
        {
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

        public CollectionResultAsyncOperation<T> LoadAllAssets<T>(AssetAddress assetAddress) where T : Object
        {
            var op = new CollectionResultAsyncOperation<T>();
            var opt = assetAddress.IsRunSynchronously ? AssetBundleLoadOption.TryImmediate : AssetBundleLoadOption.Async;
            mAbm.GetBundle(FixBundlePath(assetAddress.Address), opt, ab =>
            {
                if (ab == null)
                {
                    var assets = new T[0];
                    op.TrySetResults(assets);
                    OnBundleAllAssetLoaded(assetAddress.Address, assets);
                    return;
                }
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

        public ResultAsyncOperation<T> LoadAsset<T>(AssetAddress assetAddress) where T : Object
        {
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

        public T LoadAssetImmediate<T>(AssetAddress assetAddress) where T : Object
        {
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

            loadingContainer.Bundle = bundle;
            var key = GetAssetKey(bundle, loadingContainer.Address);
            var asset = bundle.LoadAsset<T>(key);
            OnLoadAssetComplete<T>((LoadingContainer<T>) loadingContainer, asset);
            return asset;
        }

        public GameObject InstantiateImmediate(InstantiationAssetAddress address)
        {
            var asset = LoadAssetImmediate<GameObject>(address);
            address.PreSetAsset(asset);
            var obj = address.Instantiate();
            if (obj)
            {
                mGameObjectsHash.Add(obj.GetInstanceID(), address.GetHashCode());
            }

            return obj;
        }

        public ResultAsyncOperation<GameObject> Instantiate(InstantiationAssetAddress address)
        {
            var ret = new ResultAsyncOperation<GameObject>();
            if (!address.ExistsParts)
            {
                if (address.Instance != null)
                {
                    address.Instantiate();
                    ret.SetComplete(address.Instance);
                }
                else
                {
                    var op1 = LoadAsset<GameObject>(address);
                    op1.Subscribe(ao =>
                    {
                        var asset = ao.Result;
                        address.PreSetAsset(asset);
                        var obj = address.Instantiate();
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
                address.GetAllDependencies(listAll);
                listAll.RemoveAll(e => e.Instance != null);

                var all = listAll.Select(LoadAsset<GameObject>);
                var op = new CollectionResultAsyncOperation<GameObject>(all);
                op.Subscribe((GameObject[] gos) =>
                {
                    var go = address.Instantiate();
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

        public CollectionResultAsyncOperation<T> LoadAssets<T>(IList<AssetAddress> addresses) where T : Object
        {
            var all = addresses.Select(LoadAsset<T>);
            return new CollectionResultAsyncOperation<T>(all);
        }

        public CollectionResultAsyncOperation<GameObject> Instantiates(IList<InstantiationAssetAddress> addresses)
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
                con.Address.Release();
#if UNITY_EDITOR
                Statistics.Instance?.RemoveAsset(con.Asset, con.Address.Address);
#endif
                mAbm.UnloadBundle(con.Bundle);
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
            ret = ret.ContinueWith(_ => { return LoadAsset<AssetBundleProfileManifest>(AssetAddress.String2Address(ab)).ContinueWith(op => InitAssetBundleProfileManifest(((ResultAsyncOperation<AssetBundleProfileManifest>) op).Result)); });
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

        public string AddressToMainAssetPath(string address)
        {
            return mProfileManifest?.AssetBundleNameToMainAssetPath(address);
        }

        public string SceneNameToScenePath(string sceneName)
        {
            return mProfileManifest?.SceneNameToAssetPath(sceneName);
        }
    }
}