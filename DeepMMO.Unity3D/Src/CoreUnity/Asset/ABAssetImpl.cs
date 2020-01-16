using System;
using System.Collections.Generic;
using System.Linq;
using CoreUnity.AssetBundles;
using CoreUnity.Async;
using CoreUnity.Cache;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Path = System.IO.Path;

namespace CoreUnity.Asset
{
    public class ABAssetImpl : IAssetImpl
    {
        internal class AssetContainer : ReferencesContainer
        {
            public readonly UnityEngine.Object Asset;
            public readonly AssetBundle Bundle;

            public AssetContainer(LoadingContainer container, Object asset) : base(container.Address)
            {
                References = container.References;
                Bundle = container.Bundle;
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
        }

        internal class LoadingContainer : ReferencesContainer
        {
            public AssetBundle Bundle;

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

        private readonly Dictionary<int, AssetContainer> mAssets = new Dictionary<int, AssetContainer>();
        private readonly Dictionary<int, LoadingContainer> mLoadingAssets = new Dictionary<int, LoadingContainer>();
        private readonly Dictionary<int, int> mAssetsHash = new Dictionary<int, int>();
        private readonly Dictionary<int, int> mGameObjectsHash = new Dictionary<int, int>();
        private AssetBundleManager mAbm;

        private void OnLoadAssetComplete<T>(LoadingContainer<T> con, Object obj) where T : Object
        {
            var hash = con.GetHashCode();
            if (!mLoadingAssets.Remove(hash))
            {
                throw new SystemException("what ?");
            }

            mLoadingAssets.Remove(hash);
            var assetContainer = new AssetContainer<T>(con, obj, con.Operation);
            if (obj)
            {
                mAssets.Add(hash, assetContainer);
                mAssetsHash.Add(obj.GetInstanceID(), hash);
            }
            else
            {
                mAbm.UnloadBundle(con.Bundle);
            }

            assetContainer.Operation.SetComplete((T) obj);
        }

        private void OnAssetBundleComplete<T>(LoadingContainer<T> loadingContainer, AssetBundle bundle) where T : Object
        {
            loadingContainer.Bundle = bundle;
            if (bundle == null)
            {
                OnLoadAssetComplete(loadingContainer, null);
            }
            else
            {
                var key = loadingContainer.Address.Key;
                if (string.IsNullOrEmpty(key))
                {
                    key = ConvertToAssetKey<T>(loadingContainer.Address.Address);
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

        private string FixSceneAddress(string address)
        {
            var isAbPath = !string.IsNullOrEmpty(Path.GetExtension(address));
            if (!isAbPath)
            {
                address = ConvertToSceneAddress(address);
            }

            return address;
        }

        public ResultAsyncOperation<AsyncOperation> LoadScene(object address, LoadSceneMode mode)
        {
            var assetAddress = AssetAddress.EvaluateAddress(address);
            var scenePath = FixSceneAddress(assetAddress.Address);
            var ret = new ResultAsyncOperation<AsyncOperation>();
            mAbm.LoadScene(scenePath, mode, op =>
            {
                ret.SetComplete(op);
                if (op != null)
                {
                    op.completed += operation => { mAbm.UnloadBundle(scenePath, false, false); };
                }
            });
            return ret;
        }

        public Scene LoadSceneImmediate(object address, LoadSceneMode mode)
        {
            var assetAddress = AssetAddress.EvaluateAddress(address);
            var scenePath = FixSceneAddress(assetAddress.Address);
            return mAbm.LoadSceneImmediate(scenePath, mode);
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

            var lCon = new LoadingContainer<T>(assetAddress, new ResultAsyncOperation<T>());
            mLoadingAssets.Add(hash, lCon);
            mAbm.GetBundle(assetAddress.Address, bundle => { OnAssetBundleComplete<T>(lCon, bundle); });
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
                loadingContainer = new LoadingContainer<T>(assetAddress, new ResultAsyncOperation<T>());
            }

            var bundle = mAbm.GetBundleImmediate(assetAddress.Address);
            loadingContainer.Bundle = bundle;

            if (bundle == null)
            {
                OnLoadAssetComplete<T>((LoadingContainer<T>) loadingContainer, null);
                return null;
            }
            else
            {
                var key = loadingContainer.Address.Key;
                if (string.IsNullOrEmpty(key))
                {
                    key = ConvertToAssetKey<T>(loadingContainer.Address.Address);
                }

                var asset = bundle.LoadAsset<T>(key);
                OnLoadAssetComplete<T>((LoadingContainer<T>) loadingContainer, asset);
                return asset;
            }
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
                op.Subscribe(_ =>
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
            SceneManager.UnloadSceneAsync(scene);
        }


        private void Release(AssetContainer con)
        {
            if (--con.References <= 0)
            {
                mAssetsHash.Remove(con.Asset.GetInstanceID());
                mAssets.Remove(con.GetHashCode());
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

        public BaseAsyncOperation Initialize(AssetManagerParam param)
        {
            mAbm?.Dispose();

            var p = param as ABAssetManagerParam;
            mAbm = new AssetBundleManager(p.PlatformBundlePath, p.UseLowerCasePlatform, p.BundleCacheCapacity);
            mAbm.SetHandler(p.Handler);
            mAbm.SetBaseUri(p.BaseUrl);
            ScenePrefix = p.PrefixScenePath;

            var rq = mAbm.InitializeAsync();
            var ret = new DecoratorAsyncOperation(rq);
            return ret;
        }


        public bool Initialized => mAbm?.Initialized ?? false;

        public string GameObjectFileExtension => ".assetbundles";

        public IObjectPoolControl BundlePool => mAbm.BundlePool;

        public string ScenePrefix { get; private set; }

        protected virtual string ConvertToSceneAddress(string sceneName)
        {
            return $"{ScenePrefix}{sceneName}.unity3d";
        }

        protected virtual string ConvertToAssetKey<T>(string address) where T : Object
        {
            var ret = Path.GetFileNameWithoutExtension(address);
            if (typeof(GameObject).IsAssignableFrom(typeof(T)))
            {
                ret += ".prefab";
            }

            return ret;
        }

        public void Dispose()
        {
            mAbm.Dispose();
        }
    }
}