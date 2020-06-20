using System;
using UnityEngine;

namespace DeepU3.Asset
{
    [Serializable]
    public class AssetAddress
    {
        public string Address => m_Address;
        public string Key => m_Key;

        public bool IsRunSynchronously { get; set; }

        [SerializeField]
        private string m_Address;

        [SerializeField]
        private string m_Key;

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
            m_Address = address;
            m_Key = key;
        }
    }

    [Serializable]
    public class GUIDAssetAddress : AssetAddress
    {
        public GUIDAssetAddress(string address, string key = null) : base(address, key)
        {
        }
    }
}