using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DeepU3.AssetBundles;
using DeepU3.Async;
using DeepU3.Cache;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DeepU3.Asset
{
    internal class ReferencesContainer
    {
        public int References = 1;

        public AssetAddress Address { get; }

        public ReferencesContainer(AssetAddress assetPath)
        {
            Address = assetPath;
        }

        public override int GetHashCode()
        {
            return Address.GetHashCode();
        }
    }


    public struct InstantiationParameters
    {
        Vector3 m_Position;
        Quaternion m_Rotation;
        internal Transform m_Parent;
        bool m_InstantiateInWorldPosition;
        bool m_SetPositionRotation;

        public static InstantiationParameters Default = new InstantiationParameters();
        /// <summary>
        /// Position in world space to instantiate object.
        /// </summary>
        public Vector3 Position
        {
            get { return m_Position; }
        }

        /// <summary>
        /// Rotation in world space to instantiate object.
        /// </summary>
        public Quaternion Rotation
        {
            get { return m_Rotation; }
        }

        /// <summary>
        /// Transform to set as the parent of the instantiated object.
        /// </summary>

        public Transform Parent
        {
            get { return m_Parent; }
        }

        /// <summary>
        /// When setting the parent Transform, this sets whether to preserve instance transform relative to world space or relative to the parent.
        /// </summary>

        public bool InstantiateInWorldPosition
        {
            get { return m_InstantiateInWorldPosition; }
        }

        /// <summary>
        /// Flag to tell the IInstanceProvider whether to set the position and rotation on new instances.
        /// </summary>

        public bool SetPositionRotation
        {
            get { return m_SetPositionRotation; }
        }

        public bool HasParent { get; }

        /// <summary>
        /// Create a new InstantationParameters class that will set the parent transform and use the prefab transform.
        /// <param name="parent">Transform to set as the parent of the instantiated object.</param>
        /// <param name="instantiateInWorldSpace">Flag to tell the IInstanceProvider whether to set the position and rotation on new instances.</param>
        /// </summary>
        public InstantiationParameters(Transform parent, bool instantiateInWorldSpace)
        {
            m_Position = Vector3.zero;
            m_Rotation = Quaternion.identity;
            m_Parent = parent;
            HasParent = parent;
            m_InstantiateInWorldPosition = instantiateInWorldSpace;
            m_SetPositionRotation = false;
        }


        /// <summary>
        /// Create a new InstantationParameters class that will set the position, rotation, and Transform parent of the instance.
        /// <param name="position">Position relative to the parent to set on the instance.</param>
        /// <param name="rotation">Rotation relative to the parent to set on the instance.</param>
        /// <param name="parent">Transform to set as the parent of the instantiated object.</param>
        /// </summary>
        public InstantiationParameters(Vector3 position, Quaternion rotation, Transform parent, bool instantiateInWorldSpace)
        {
            m_Position = position;
            m_Rotation = rotation;
            m_Parent = parent;
            HasParent = parent;
            m_InstantiateInWorldPosition = instantiateInWorldSpace;
            m_SetPositionRotation = true;
        }

        /// <summary>
        /// Instantiate an object with the parameters of this object.
        /// <param name="source">Object to instantiate.</param>
        /// <returns>Instantiated object.</returns>
        /// <typeparam name="TObject">Object type. This type must be of type UnityEngine.Object.</typeparam>
        /// </summary>
        public GameObject Instantiate(GameObject source)
        {
            GameObject result;

            if (!HasParent)
            {
                result = m_SetPositionRotation ? Object.Instantiate(source, m_Position, m_Rotation) : Object.Instantiate(source);
            }
            else
            {
                if (m_InstantiateInWorldPosition && m_SetPositionRotation)
                {
                    result = Object.Instantiate(source, m_Position, m_Rotation, m_Parent);
                }
                else
                {
                    result = Object.Instantiate(source, m_Parent, m_InstantiateInWorldPosition);
                    if (m_SetPositionRotation)
                    {
                        if (m_InstantiateInWorldPosition)
                        {
                            result.transform.position = m_Position;
                            result.transform.rotation = m_Rotation;
                        }
                        else
                        {
                            result.transform.localPosition = m_Position;
                            result.transform.localRotation = m_Rotation;
                        }
                    }
                }
            }

            return result;
        }

        public void Reset(GameObject instance)
        {
            instance.transform.SetParent(Parent, InstantiateInWorldPosition);
            if (m_SetPositionRotation)
            {
                if (m_InstantiateInWorldPosition)
                {
                    instance.transform.position = m_Position;
                    instance.transform.rotation = m_Rotation;
                }
                else
                {
                    instance.transform.localPosition = m_Position;
                    instance.transform.localRotation = m_Rotation;
                }
            }
        }
    }


    public class InstantiationAssetAddress : AssetAddress
    {
        internal InstantiationParameters Parameters;

        public bool IsDone { get; private set; }
        internal GameObject Instance { get; private set; }

        private GameObject mAsset;

        internal Dictionary<InstantiationAssetAddress, string> Parts { get; private set; }
        
        private static readonly ObjectPool<InstantiationAssetAddress> sAddressPool = new ObjectPool<InstantiationAssetAddress>(100);
        
        public new static InstantiationAssetAddress String2Address(string address)
        {
            var ret = sAddressPool.Get() ?? new InstantiationAssetAddress();
            ret.Address = address;
            
            return ret;
        }

        public static InstantiationAssetAddress String2Address(string address, InstantiationParameters parameters)
        {
            var ret = sAddressPool.Get() ?? new InstantiationAssetAddress();
            ret.Address = address;
            ret.Parameters = parameters;
            return ret;
        }

        protected internal override void Release()
        {
            Address = null;
            Key = null;
            IsRunSynchronously = false;
            Instance = null;
            mAsset = null;
            IsDone = false;
            Parameters = InstantiationParameters.Default;
            sAddressPool.Put(this);
        }

        public void AddPart(InstantiationAssetAddress part, string bindGameObjectName)
        {
            if (Parts == null)
            {
                Parts = new Dictionary<InstantiationAssetAddress, string> {{part, bindGameObjectName}};
            }
            else
            {
                Parts.Add(part, bindGameObjectName);
            }
        }

        public void GetAllInstance(List<GameObject> ret)
        {
            if (Instance)
            {
                ret.Add(Instance);
            }

            if (Parts == null)
            {
                return;
            }

            foreach (var entry in Parts)
            {
                entry.Key.GetAllInstance(ret);
            }
        }

        internal void PreSetAsset(GameObject asset)
        {
            mAsset = asset;
        }

        internal void PreSetInstance(GameObject instance)
        {
            Instance = instance;
        }

        public void GetAllDependencies(List<InstantiationAssetAddress> dependencies)
        {
            dependencies.Add(this);
            if (Parts == null)
            {
                return;
            }

            foreach (var entry in Parts)
            {
                entry.Key.GetAllDependencies(dependencies);
            }
        }

        public bool ExistsParts => Parts != null && Parts.Count > 0;


        public GameObject Instantiate()
        {
            if (IsDone)
            {
                return Instance;
            }

            IsDone = true;

            if (Parameters.HasParent && !Parameters.Parent)
            {
                if (mAsset)
                {
                    AssetManager.Release(mAsset);
                }

                return null;
            }

            if (Instance == null)
            {
                if (mAsset != null)
                {
                    Instance = Parameters.Instantiate(mAsset);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                Parameters.Reset(Instance);
            }

            if (Parts != null && Instance != null)
            {
                foreach (var entry in Parts)
                {
                    if (!entry.Key.Parameters.Parent)
                    {
                        entry.Key.Parameters.m_Parent = Instance.transform;
                    }

                    entry.Key.Instantiate();
                }
            }

            return Instance;
        }

    }

    public class AssetManagerParam
    {
        public int BundleCacheCapacity;
        public int InstanceCacheCapacity = 100;
        public string BaseUrl;
    }

    public class ABAssetManagerParam : AssetManagerParam
    {
        public bool PlatformBundlePath = false;
        public bool UseLowerCasePlatform = true;
        public ICommandHandler<AssetBundleCommand> Handler;
    }

    public enum ScenePathType
    {
        SceneName,
        SceneAssetPath,
        Address
    }


    public interface IAssetImplInstantiate
    {
        GameObject InstantiateImmediate(InstantiationAssetAddress address);
        ResultAsyncOperation<GameObject> Instantiate(InstantiationAssetAddress address);
        CollectionResultAsyncOperation<GameObject> Instantiates(IList<InstantiationAssetAddress> addresses);

        bool ReleaseInstance(GameObject obj);
    }


    public interface IAssetDebug
    {
        AssetAddress[] LoadingAssets { get; }
    }

    public interface IAssetPathConverter
    {
        string SceneNameToScenePath(string sceneName);
        string AddressToMainAssetPath(string address);

        string AssetPathToAddress(string assetPath);
    }

    public interface IAssetImpl : IDisposable, IAssetImplInstantiate, IAssetDebug, IAssetPathConverter
    {
        int LoadingAssetCount { get; }
        Object[] LoadAllAssetsImmediate(AssetAddress address);
        T[] LoadAllAssetsImmediate<T>(AssetAddress address) where T : Object;
        CollectionResultAsyncOperation<T> LoadAllAssets<T>(AssetAddress address) where T : Object;

        ResultAsyncOperation<AsyncOperation> LoadScene(string path, ScenePathType pathType, LoadSceneMode mode);


        Scene LoadSceneImmediate(string path, ScenePathType pathType, LoadSceneMode mode);

        void UnloadScene(Scene scene);

        /// <summary>
        /// load asset
        /// </summary>
        /// <param name="address">address or asset bundle name</param>
        /// <param name="key">can be null</param>
        /// <typeparam name="T"></typeparam>
        ResultAsyncOperation<T> LoadAsset<T>(AssetAddress address) where T : Object;

        T LoadAssetImmediate<T>(AssetAddress address) where T : Object;

        CollectionResultAsyncOperation<T> LoadAssets<T>(IList<AssetAddress> address) where T : Object;


        void Release(Object asset);

        void Release<T>(T[] assets) where T : Object;
        BaseAsyncOperation Initialize(AssetManagerParam param);
        bool Initialized { get; }
    }
}