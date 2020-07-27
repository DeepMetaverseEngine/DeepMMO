// #define UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeepU3.Async;
using DeepU3.Cache;
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif
using Object = UnityEngine.Object;

namespace DeepU3.Asset
{
#if UNITY_EDITOR
    public class EditorAssetImpl : IAssetImpl
    {
        public int LoadingAssetCount { get; private set; }

        public Object[] LoadAllAssetsImmediate(AssetAddress address)
        {
            throw new NotImplementedException();
        }

        public T[] LoadAllAssetsImmediate<T>(AssetAddress address) where T : Object
        {
            throw new NotImplementedException();
        }

        public CollectionResultAsyncOperation<T> LoadAllAssets<T>(AssetAddress address) where T : Object
        {
            var assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(address.Address);
            var all = new T[assetPaths.Length];
            for (var i = 0; i < all.Length; i++)
            {
                all[i] = AssetDatabase.LoadAssetAtPath<T>(assetPaths[i]);
            }

            return new CollectionResultAsyncOperation<T>(all);
        }

        public CollectionResultAsyncOperation<Object> LoadAllAssets(AssetAddress address)
        {
            throw new NotImplementedException();
        }

        public ResultAsyncOperation<AsyncOperation> LoadScene(string path, ScenePathType pathType, LoadSceneMode mode)
        {
            switch (pathType)
            {
                case ScenePathType.SceneName:
                    return new ResultAsyncOperation<AsyncOperation>(SceneManager.LoadSceneAsync(path, mode));
                case ScenePathType.SceneAssetPath:
                    return new ResultAsyncOperation<AsyncOperation>(EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(mode)));
                case ScenePathType.Address:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException(nameof(pathType), pathType, null);
            }
        }

        public Scene LoadSceneImmediate(string path, ScenePathType pathType, LoadSceneMode mode)
        {
            switch (pathType)
            {
                case ScenePathType.SceneName:
                    return new Scene();
                case ScenePathType.SceneAssetPath:
                    return EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(mode));
                case ScenePathType.Address:
                    return new Scene();
                default:
                    throw new ArgumentOutOfRangeException(nameof(pathType), pathType, null);
            }
        }

        public ResultAsyncOperation<T> LoadAsset<T>(AssetAddress address) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(FixAddress(address.Address));
            return new ResultAsyncOperation<T>(asset);
        }

        public T LoadAssetImmediate<T>(AssetAddress address) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(FixAddress(address.Address));
            return asset;
        }

        public GameObject InstantiateImmediate(InstantiationAssetAddress address)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(FixAddress(address.Address));
            address.PreSetAsset(asset);
            return address.Instantiate();
        }

        public ResultAsyncOperation<GameObject> Instantiate(InstantiationAssetAddress address)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(FixAddress(address.Address));
            address.PreSetAsset(asset);
            return new ResultAsyncOperation<GameObject>(address.Instantiate());
        }

        private string FixAddress(string assetPath)
        {
            return assetPath;
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
            UnityEngine.Object.Destroy(obj);
            return true;
        }

        public void UnloadScene(Scene scene)
        {
            SceneManager.UnloadSceneAsync(scene);
        }

        public void Release(Object asset)
        {
            var assetPath = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(assetPath))
            {
                Object.DestroyImmediate(asset, true);
            }
        }

        public void Release<T>(T[] assets) where T : Object
        {
            foreach (var asset in assets)
            {
                Release(asset);
            }
        }

        public BaseAsyncOperation Initialize(AssetManagerParam param)
        {
            Prefix = param.BaseUrl;
            var ret = new BaseAsyncOperation(true);
            return ret;
        }


        public bool Initialized => true;

        public IObjectPoolControl BundlePool => null;

        public string Prefix { get; private set; }

        public void Dispose()
        {
        }

        public AssetAddress[] LoadingAssets => null;

        public string SceneNameToScenePath(string sceneName)
        {
            throw new NotImplementedException();
        }

        public string AssetPathToAddress(string assetPath)
        {
            return assetPath;
        }

        public string AddressToMainAssetPath(string address)
        {
            return address;
        }
    }
#endif
}