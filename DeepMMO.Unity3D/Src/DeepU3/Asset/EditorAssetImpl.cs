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

        public Object[] LoadAllAssetsImmediate(object address)
        {
            throw new NotImplementedException();
        }

        public T[] LoadAllAssetsImmediate<T>(object address) where T : Object
        {
            throw new NotImplementedException();
        }

        public CollectionResultAsyncOperation<T> LoadAllAssets<T>(object address) where T : Object
        {
            var assetAddress = AssetAddress.EvaluateAddress(address);
            var assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(assetAddress.Address);
            var all = new T[assetPaths.Length];
            for (var i = 0; i < all.Length; i++)
            {
                all[i] = AssetDatabase.LoadAssetAtPath<T>(assetPaths[i]);
            }

            return new CollectionResultAsyncOperation<T>(all);
        }

        public CollectionResultAsyncOperation<Object> LoadAllAssets(object address)
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

        public ResultAsyncOperation<T> LoadAsset<T>(object address) where T : Object
        {
            var assetAddress = AssetAddress.EvaluateAddress(address);
            var asset = AssetDatabase.LoadAssetAtPath<T>(FixAddress(assetAddress.Address));
            return new ResultAsyncOperation<T>(asset);
        }

        public T LoadAssetImmediate<T>(object address) where T : Object
        {
            var assetAddress = AssetAddress.EvaluateAddress(address);
            var asset = AssetDatabase.LoadAssetAtPath<T>(FixAddress(assetAddress.Address));
            return asset;
        }

        public GameObject InstantiateImmediate(object address)
        {
            var assetAddress = AssetAddress.EvaluateAs<InstantiationAssetAddress>(address);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(FixAddress(assetAddress.Address));
            assetAddress.PreSetAsset(asset);
            return assetAddress.Instantiate();
        }

        public ResultAsyncOperation<GameObject> Instantiate(object address)
        {
            var assetAddress = AssetAddress.EvaluateAs<InstantiationAssetAddress>(address);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(FixAddress(assetAddress.Address));
            assetAddress.PreSetAsset(asset);
            return new ResultAsyncOperation<GameObject>(assetAddress.Instantiate());
        }

        private string FixAddress(string assetPath)
        {
            return assetPath;
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

        public string AddressToScenePath(string address)
        {
            return address;
        }
    }
#endif
}