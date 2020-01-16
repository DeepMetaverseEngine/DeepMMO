using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using CoreUnity.AssetBundles;
using CoreUnity.Async;
using CoreUnity.Cache;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CoreUnity.Asset
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


    public class AssetAddress
    {
        public readonly string Address;
        public readonly string Key;

        public static int GetHashCode(string address, string key)
        {
            unchecked
            {
                var hashCode = (address != null ? address.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (key != null ? key.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static AssetAddress EvaluateAddress(object address)
        {
            if (address is AssetAddress assetAddress)
            {
                return assetAddress;
            }

            return address.ToString();
        }

        public static T EvaluateAs<T>(object address) where T : AssetAddress
        {
            if (address is T assetAddress)
            {
                return assetAddress;
            }

            if (typeof(InstantiationAssetAddress).IsAssignableFrom(typeof(T)))
            {
                return new InstantiationAssetAddress(address.ToString()) as T;
            }

            throw new ArgumentException();
        }

        public override int GetHashCode()
        {
            return GetHashCode(Address, Key);
        }


        public static implicit operator AssetAddress(string address)
        {
            return new AssetAddress(address, null);
        }

        public static explicit operator string(AssetAddress address)
        {
            return address.Address;
        }

        public AssetAddress(string address, string key = null)
        {
            Address = address;
            Key = key;
        }
    }


    public struct InstantiationParameters
    {
        Vector3 m_Position;
        Quaternion m_Rotation;
        internal Transform m_Parent;
        bool m_InstantiateInWorldPosition;
        bool m_SetPositionRotation;

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
            m_InstantiateInWorldPosition = instantiateInWorldSpace;
            m_SetPositionRotation = false;
        }


        /// <summary>
        /// Create a new InstantationParameters class that will set the position, rotation, and Transform parent of the instance.
        /// <param name="position">Position relative to the parent to set on the instance.</param>
        /// <param name="rotation">Rotation relative to the parent to set on the instance.</param>
        /// <param name="parent">Transform to set as the parent of the instantiated object.</param>
        /// </summary>
        public InstantiationParameters(Vector3 position, Quaternion rotation, Transform parent)
        {
            m_Position = position;
            m_Rotation = rotation;
            m_Parent = parent;
            m_InstantiateInWorldPosition = false;
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

            if (m_Parent == null)
            {
                result = m_SetPositionRotation ? Object.Instantiate(source, m_Position, m_Rotation) : Object.Instantiate(source);
            }
            else
            {
                result = m_SetPositionRotation ? Object.Instantiate(source, m_Position, m_Rotation, m_Parent) : Object.Instantiate(source, m_Parent, m_InstantiateInWorldPosition);
            }

            return result;
        }

        public void Reset(GameObject instance)
        {
            instance.transform.SetParent(Parent, InstantiateInWorldPosition);
            if (SetPositionRotation)
            {
                instance.transform.position = Position;
                instance.transform.rotation = Rotation;
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

        public InstantiationAssetAddress(AssetAddress address, InstantiationParameters parameters) : base(address.Address, address.Key)
        {
            Parameters = parameters;
        }

        public InstantiationAssetAddress(string address) : this(address, new InstantiationParameters())
        {
        }
    }

    public class AssetManagerParam
    {
        public int BundleCacheCapacity;
        public int InstanceCacheCapacity = 100;
        public string PrefixScenePath;
        public string BaseUrl;
    }

    public class ABAssetManagerParam : AssetManagerParam
    {
        public bool PlatformBundlePath = false;
        public bool UseLowerCasePlatform = true;
        public ICommandHandler<AssetBundleCommand> Handler;
    }


    public interface IAssetImplInstantiate
    {
        GameObject InstantiateImmediate(object address);
        ResultAsyncOperation<GameObject> Instantiate(object address);
        CollectionResultAsyncOperation<GameObject> Instantiates(IList<object> addresses);
    
        bool ReleaseInstance(GameObject obj);
    }

    public interface IAssetImpl : IDisposable, IAssetImplInstantiate
    {
        /// <summary>
        /// load scene
        /// </summary>
        /// <param name="address">address or asset bundle name</param>
        /// <param name="mode"></param>
        ResultAsyncOperation<AsyncOperation> LoadScene(object address, LoadSceneMode mode);

        Scene LoadSceneImmediate(object address, LoadSceneMode mode);
        void UnloadScene(Scene scene);

        /// <summary>
        /// load asset
        /// </summary>
        /// <param name="address">address or asset bundle name</param>
        /// <param name="key">can be null</param>
        /// <typeparam name="T"></typeparam>
        ResultAsyncOperation<T> LoadAsset<T>(object address) where T : Object;
        
        T LoadAssetImmediate<T>(object address) where T : Object;

        CollectionResultAsyncOperation<T> LoadAssets<T>(IList<object> address) where T : Object;


        void Release(Object asset);

        BaseAsyncOperation Initialize(AssetManagerParam param);
        bool Initialized { get; }

        string GameObjectFileExtension { get; }

        IObjectPoolControl BundlePool { get; }
    }
}