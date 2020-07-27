using System;
using DeepU3.Cache;
using UnityEngine;

namespace DeepU3.Asset
{
    public class AssetAddress
    {
        public string Address { get; protected set; }
        public string Key { get; protected set; }

        public bool IsRunSynchronously { get; set; }

        public static int GetHashCode(string address, string key)
        {
            unchecked
            {
                var hashCode = (address != null ? address.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (key != null ? key.GetHashCode() : 0);
                return hashCode;
            }
        }

        private static readonly ObjectPool<AssetAddress> sAddressPool = new ObjectPool<AssetAddress>(100);

        public static AssetAddress String2Address(string address)
        {
            var ret = sAddressPool.Get() ?? new AssetAddress();
            ret.Address = address;
            return ret;
        }

        public static AssetAddress String2Address(string address, string key)
        {
            var ret = String2Address(address);
            ret.Key = key;
            return ret;
        }

        protected internal virtual void Release()
        {
            Address = null;
            Key = null;
            IsRunSynchronously = false;
            sAddressPool.Put(this);
        }

        public override int GetHashCode()
        {
            return GetHashCode(Address, Key);
        }

        protected AssetAddress()
        {
        }
    }
}