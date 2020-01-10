using System;
using System.Collections.Generic;
using System.Linq;
using CoreUnity.AssetBundles;
using CoreUnity.Async;
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

            public AssetContainer(LoadingContainer container, UnityEngine.Object asset) : base(container.AssetPath)
            {
                References = container.References;
                Bundle = container.Bundle;
                Asset = asset;
            }
        }

        internal class LoadingContainer : ReferencesContainer
        {
            public Action<UnityEngine.Object> OnComplete;
            public AssetBundle Bundle;

            public LoadingContainer(AssetAddress assetPath) : base(assetPath)
            {
            }
        }

        private readonly Dictionary<int, AssetContainer> mAssets = new Dictionary<int, AssetContainer>();
        private readonly Dictionary<int, LoadingContainer> mLoadingAssets = new Dictionary<int, LoadingContainer>();
        private readonly Dictionary<int, int> mAssetsHash = new Dictionary<int, int>();
        private readonly Dictionary<int, int> mGameObjectsHash = new Dictionary<int, int>();
        private AssetBundleManager mAbm;

        private void OnLoadAssetComplete(LoadingContainer con, UnityEngine.Object obj)
        {
            var hash = con.GetHashCode();
            mLoadingAssets.Remove(hash);
            var assetContainer = new AssetContainer(con, obj);
            if (obj)
            {
                mAssets.Add(hash, assetContainer);
                mAssetsHash.Add(obj.GetInstanceID(), hash);
            }
            else
            {
                mAbm.UnloadBundle(con.Bundle);
            }

            con.OnComplete?.Invoke(obj);
        }

        private void OnAssetBundleComplete<T>(LoadingContainer loadingContainer, AssetBundle bundle)
        {
            loadingContainer.Bundle = bundle;
            if (bundle == null)
            {
                OnLoadAssetComplete(loadingContainer, null);
            }
            else
            {
                var key = loadingContainer.AssetPath.Key;
                if (string.IsNullOrEmpty(key))
                {
                    key = ConvertToAssetKey(loadingContainer.AssetPath.Address);
                }

                var rq = bundle.LoadAssetAsync<T>(key);
                rq.completed += operation => { OnLoadAssetComplete(loadingContainer, rq.asset); };
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
            var ret = new ResultAsyncOperation<T>();
            if (mAssets.TryGetValue(hash, out var container))
            {
                container.References++;
                ret.SetComplete(container.Asset as T);
            }
            else
            {
                void InvokeCompleted(Object asset)
                {
                    ret.SetComplete(asset as T);
                }

                if (mLoadingAssets.TryGetValue(hash, out var loadingContainer))
                {
                    loadingContainer.References++;
                    loadingContainer.OnComplete += InvokeCompleted;
                }
                else
                {
                    loadingContainer = new LoadingContainer(assetAddress);
                    loadingContainer.OnComplete += InvokeCompleted;
                    mLoadingAssets.Add(hash, loadingContainer);
                    mAbm.GetBundle(assetAddress.Address, bundle => { OnAssetBundleComplete<T>(loadingContainer, bundle); });
                }
            }

            return ret;
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
                        ret.SetComplete(obj);
                        if (obj)
                        {
                            mGameObjectsHash.Add(obj.GetInstanceID(), address.GetHashCode());
                        }
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
            mAbm = new AssetBundleManager(p.PlatformBundlePath, p.UseLowerCasePlatform, (uint) p.BundleCacheCapacity);
            mAbm.SetHandler(p.Handler);
            mAbm.SetBaseUri(p.BaseUrl);
            ScenePrefix = p.PrefixScenePath;

            var rq = mAbm.InitializeAsync();
            var ret = new DecoratorAsyncOperation(rq);
            return ret;
        }


        public bool Initialized => mAbm?.Initialized ?? false;

        public string ScenePrefix { get; private set; }

        protected virtual string ConvertToSceneAddress(string sceneName)
        {
            return $"{ScenePrefix}{sceneName}.unity3d";
        }

        protected virtual string ConvertToAssetKey(string address)
        {
            return Path.GetFileNameWithoutExtension(address);
        }

        public void Dispose()
        {
            mAbm.Dispose();
        }
    }
}