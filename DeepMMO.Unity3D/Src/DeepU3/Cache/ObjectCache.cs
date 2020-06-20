using System;
using System.Collections.Generic;

namespace DeepU3.Cache
{
    public class ObjectCache
    {
        private struct KeyValueType
        {
            private readonly Type Key;
            private readonly Type Value;

            public KeyValueType(Type key, Type value)
            {
                Key = key;
                Value = value;
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (Key.GetHashCode() * 397) ^ Value.GetHashCode();
                }
            }
        }

        private readonly Dictionary<Type, IObjectPool> mPools = new Dictionary<Type, IObjectPool>();
        private readonly Dictionary<KeyValueType, IKeyObjectPool> mKeyValuePools = new Dictionary<KeyValueType, IKeyObjectPool>();


        protected virtual IObjectPool CreateObjectPool(Type t)
        {
            var obj = new ObjectPool(t);
            return obj;
        }


        public IObjectPool GetObjectPool(Type t)
        {
            if (!mPools.TryGetValue(t, out var obj))
            {
                obj = CreateObjectPool(t);
                RegisterObjectPool(t, obj);
            }

            return obj;
        }

        public IObjectPool GetObjectPool<T>()
        {
            return GetObjectPool(typeof(T));
        }


        public T Get<T>() where T : class
        {
            return Get(typeof(T)) as T;
        }

        public object Get(Type t)
        {
            return GetObjectPool(t).Get();
        }

        public void Put(object obj)
        {
            GetObjectPool(obj.GetType()).Put(obj);
        }


        public void RegisterObjectPool(Type t, IObjectPool objectPool)
        {
            mPools.Add(t, objectPool);
        }

        public void RegisterObjectPool<T>(IObjectPool objectPool)
        {
            RegisterObjectPool(typeof(T), objectPool);
        }

        public void RegisterObjectPool(Type tKey, Type tValue, IKeyObjectPool objectPool)
        {
            mKeyValuePools.Add(new KeyValueType(tKey, tValue), objectPool);
        }

        public void RegisterObjectPool<TKey, T>(IKeyObjectPool objectPool)
        {
            RegisterObjectPool(typeof(TKey), typeof(T), objectPool);
        }

        protected virtual IKeyObjectPool CreateObjectPool(Type tkey, Type tValue)
        {
            var obj = new KeyObjectPool(tkey, tValue);
            return obj;
        }


        public IKeyObjectPool GetObjectPool(Type tKey, Type tValue)
        {
            if (!mKeyValuePools.TryGetValue(new KeyValueType(tKey, tValue), out var obj))
            {
                obj = CreateObjectPool(tKey, tValue);
                RegisterObjectPool(tKey, tValue, obj);
            }

            return obj;
        }

        public IKeyObjectPool GetObjectPool<TKey, TValue>()
        {
            return GetObjectPool(typeof(TKey), typeof(TValue));
        }


        public object Get(Type tKey, Type tValue, object key)
        {
            return GetObjectPool(tKey, tValue).Get(key);
        }


        public T Get<TKey, T>(TKey key) where T : class
        {
            return Get(typeof(TKey), typeof(T), key) as T;
        }

        public T Get<T>(object key) where T : class
        {
            return Get(key.GetType(), typeof(T), key) as T;
        }

        public void Put(object key, object value)
        {
            GetObjectPool(key.GetType(), value.GetType()).Put(key, value);
        }

        public void Clear()
        {
            foreach (var entry in mPools)
            {
                entry.Value.Clear();
            }

            foreach (var entry in mKeyValuePools)
            {
                entry.Value.Clear();
            }
        }

        public ICollection<IObjectPoolControl> GetObjectPoolControls()
        {
            var ret = new List<IObjectPoolControl>();
            ret.AddRange(mPools.Values);
            ret.AddRange(mKeyValuePools.Values);

            return ret;
        }
    }
}