// #define UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using CoreUnity.Async;
using CoreUnity.Cache;
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
#endif
using Object = UnityEngine.Object;

namespace CoreUnity.Asset
{
#if UNITY_EDITOR
    public class EditorAssetImpl : IAssetImpl
    {
        public ResultAsyncOperation<AsyncOperation> LoadScene(object address, LoadSceneMode mode)
        {
            var assetAddress = AssetAddress.EvaluateAddress(address);
            var op = SceneManager.LoadSceneAsync(assetAddress.Address, mode);
            return new ResultAsyncOperation<AsyncOperation>(op);
        }

        public Scene LoadSceneImmediate(object address, LoadSceneMode mode)
        {
            var assetAddress = AssetAddress.EvaluateAddress(address);
            SceneManager.LoadScene(assetAddress.Address, mode);
            if (Path.GetExtension(assetAddress.Address) == null)
            {
                return SceneManager.GetSceneByName(assetAddress.Address);
            }

            return SceneManager.GetSceneByPath(assetAddress.Address);
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
            if (!assetPath.StartsWith(Prefix))
            {
                assetPath = Prefix + assetPath;
            }

            return assetPath;
        }

        public CollectionResultAsyncOperation<T> LoadAssets<T>(IList<object> address) where T : Object
        {
            throw new NotImplementedException();
        }

        public CollectionResultAsyncOperation<GameObject> Instantiates(IList<object> addresses)
        {
            throw new NotImplementedException();
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
            Object.DestroyImmediate(asset, true);
        }

        public BaseAsyncOperation Initialize(AssetManagerParam param)
        {
            Prefix = param.BaseUrl;
            var ret = new BaseAsyncOperation(true);
            return ret;
        }


        public bool Initialized => true;

        public string GameObjectFileExtension => ".prefab";

        public IObjectPoolControl BundlePool => null;

        public string Prefix { get; private set; }

        public void Dispose()
        {
        }
    }
#endif
}