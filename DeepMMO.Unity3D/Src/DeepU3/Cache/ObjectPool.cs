using System;
using System.Collections.Generic;

namespace DeepU3.Cache
{
    public interface IGetterPutter
    {
        object Get();
        void Put(object obj);
    }

    public interface IKeyValueGetterPutter
    {
        object Get(object key);
        void Put(object key, object obj);
    }

    public interface IObjectPoolControl
    {
        int Count { get; }
        bool EnablePut { get; set; }
        int Capacity { get; set; }
        void Clear();
    }

    /// <summary>
    /// LRU object pool
    /// </summary>
    public interface IKeyObjectPool : IObjectPoolControl, IKeyValueGetterPutter
    {
        Type KeyType { get; }
        Type ObjectType { get; }
    }

    public interface IObjectPool : IObjectPoolControl, IGetterPutter
    {
        Type ObjectType { get; }
    }

    public abstract class ObjectPoolControl : IObjectPoolControl
    {
        private int mCapacity = DefaultCapacity;

        private bool mEnablePut = true;
        protected const int DefaultCapacity = 50;

        public abstract int Count { get; }
        protected abstract void RemoveOne();

        public bool EnablePut
        {
            get => mEnablePut && Capacity > 0;
            set
            {
                mEnablePut = value;
                if (mEnablePut)
                {
                    EnsureCapacity();
                }
            }
        }

        public int Capacity
        {
            get => mCapacity;
            set
            {
                mCapacity = value;
                EnsureCapacity();
            }
        }

        protected void EnsureCapacity()
        {
            while (Count > Capacity)
            {
                RemoveOne();
            }
        }

        public void Clear()
        {
            while (Count > 0)
            {
                RemoveOne();
            }
        }
    }

    public interface ICacheObject
    {
        void OnCacheHit();
        bool BeforePutCache();
    }

    public interface IKeyCacheObject
    {
        void OnCacheHit(object key);
        bool BeforePutCache(object key);
    }

    public abstract class BaseObjectPool : ObjectPoolControl, IObjectPool
    {
        private readonly LinkedList<object> mPool = new LinkedList<object>();
        public Type ObjectType { get; }

        protected BaseObjectPool(Type t, int capacity)
        {
            ObjectType = t;
            Capacity = capacity;
        }

        public override int Count => mPool.Count;

        public object Get()
        {
            if (mPool.Count == 0)
            {
                return null;
            }

            var ret = mPool.First.Value;
            mPool.RemoveFirst();
            HitCache(ret);
            return ret;
        }

        protected virtual void HitCache(object obj)
        {
            if (obj is ICacheObject cacheObject)
            {
                cacheObject.OnCacheHit();
            }
        }

        protected virtual bool CheckPutCache(object obj)
        {
            if (!EnablePut)
            {
                return false;
            }

            if (obj is ICacheObject cacheObject)
            {
                if (!cacheObject.BeforePutCache())
                {
                    return false;
                }
            }

            return true;
        }

        protected virtual void RemoveObject(object obj)
        {
        }

        public void Put(object obj)
        {
            if (!CheckPutCache(obj))
            {
                RemoveObject(obj);
            }
            else
            {
                mPool.AddFirst(obj);
                EnsureCapacity();
            }
        }

        protected override void RemoveOne()
        {
            RemoveObject(mPool.Last.Value);
            mPool.RemoveLast();
        }
    }

    public class ObjectPool : BaseObjectPool
    {
        public delegate bool BeforePutDelegate(object obj);

        private readonly BeforePutDelegate mBeforePutMethod;
        private readonly Action<object> mRemove;
        private readonly Action<object> mHit;


        public ObjectPool(Type t, int capacity = DefaultCapacity, Action<object> removeMethod = null, BeforePutDelegate beforePutMethod = null, Action<object> hitMethod = null) : base(t, capacity)
        {
            mBeforePutMethod = beforePutMethod;
            mRemove = removeMethod;
            mHit = hitMethod;
        }

        public object GetOrCreate(Func<object> factory)
        {
            var ret = Get();
            if (ret != null)
            {
                return ret;
            }

            return factory.Invoke();
        }

        protected override void HitCache(object obj)
        {
            base.HitCache(obj);
            mHit?.Invoke(obj);
        }

        protected override bool CheckPutCache(object obj)
        {
            return base.CheckPutCache(obj) && (mBeforePutMethod == null || mBeforePutMethod.Invoke(obj));
        }

        protected override void RemoveObject(object obj)
        {
            base.RemoveObject(obj);
            mRemove?.Invoke(obj);
        }
    }

    public class ObjectPool<T> : BaseObjectPool
    {
        public delegate bool BeforePutDelegate(T obj);

        private readonly BeforePutDelegate mBeforePutMethod;
        private readonly Action<T> mRemove;
        private readonly Action<T> mHit;

        public ObjectPool(int capacity = DefaultCapacity, Action<T> removeMethod = null, BeforePutDelegate beforePutMethod = null, Action<T> hitMethod = null) : base(typeof(T), capacity)
        {
            mRemove = removeMethod;
            mHit = hitMethod;
            mBeforePutMethod = beforePutMethod;
        }

        public new T Get()
        {
            var ret = base.Get();
            if (ret != null)
            {
                return (T) ret;
            }

            return default(T);
        }

        public T GetOrCreate(Func<T> factory)
        {
            var ret = Get();
            if (ret != null)
            {
                return ret;
            }

            return factory.Invoke();
        }

        protected override void HitCache(object obj)
        {
            base.HitCache(obj);
            mHit?.Invoke((T) obj);
        }

        protected override bool CheckPutCache(object obj)
        {
            return base.CheckPutCache(obj) && (mBeforePutMethod == null || mBeforePutMethod.Invoke((T) obj));
        }

        protected override void RemoveObject(object obj)
        {
            base.RemoveObject(obj);
            mRemove?.Invoke((T) obj);
        }
    }


    public abstract class BaseKeyObjectPool : ObjectPoolControl, IKeyObjectPool
    {
        private struct CacheItem
        {
            public object Key;
            public object Value;
            public LinkedListNode<CacheItem> Node;
        }

        private readonly LinkedList<CacheItem> mPool = new LinkedList<CacheItem>();
        private readonly Dictionary<object, LinkedList<CacheItem>> mKeyMap = new Dictionary<object, LinkedList<CacheItem>>();

        public Type KeyType { get; }

        public Type ObjectType { get; }

        protected BaseKeyObjectPool(Type keyType, Type objectType, int capacity)
        {
            KeyType = keyType;
            ObjectType = objectType;
            Capacity = capacity;
        }

        public bool ContainsValue(object value)
        {
            var p = mPool.First;
            while (p != null)
            {
                if (p.Value.Value == value)
                {
                    return true;
                }

                p = p.Next;
            }

            return false;
        }

        public object Get(object key)
        {
            if (key == null)
            {
                return null;
            }

            if (!mKeyMap.TryGetValue(key, out var vlist) || vlist.Count == 0)
            {
                return null;
            }

            var v = vlist.First.Value;
            vlist.RemoveFirst();
            if (vlist.Count == 0)
            {
                mKeyMap.Remove(key);
            }

            mPool.Remove(v.Node);
            v.Node = null;
            HitCache(key, v.Value);
            return v.Value;
        }

        public override int Count => mPool.Count;

        protected virtual void HitCache(object key, object obj)
        {
            if (obj is IKeyCacheObject cacheObject)
            {
                cacheObject.OnCacheHit(key);
            }

            if (obj is ICacheObject o)
            {
                o.OnCacheHit();
            }
        }

        protected virtual bool CheckPutCache(object key, object obj)
        {
            switch (obj)
            {
                case IKeyCacheObject cacheObject when !cacheObject.BeforePutCache(key):
                case ICacheObject o when !o.BeforePutCache():
                    return false;
                default:
                    return EnablePut;
            }
        }

        protected virtual void RemoveObject(object key, object obj)
        {
        }

        protected override void RemoveOne()
        {
            RemoveObject(mPool.Last.Value.Key, mPool.Last.Value.Value);
            mKeyMap.Remove(mPool.Last.Value.Key);
            mPool.RemoveLast();
        }

        public void Put(object key, object obj)
        {
            if (!CheckPutCache(key, obj))
            {
                RemoveObject(key, obj);
            }
            else
            {
                var item = new CacheItem {Key = key, Value = obj};
                item.Node = new LinkedListNode<CacheItem>(item);
                mPool.AddFirst(item.Node);
                if (!mKeyMap.TryGetValue(key, out var vList))
                {
                    vList = new LinkedList<CacheItem>();
                    mKeyMap[key] = vList;
                }

                vList.AddFirst(item);
                EnsureCapacity();
            }
        }
    }

    public class KeyObjectPool : BaseKeyObjectPool
    {
        public delegate bool BeforePutDelegate(object key, object obj);

        private readonly Func<object, Type, object> mFactory;
        private readonly BeforePutDelegate mBeforePutMethod;
        private readonly Action<object, object> mRemove;
        private readonly Action<object, object> mHit;


        public KeyObjectPool(Type tKey, Type tValue, int capacity = DefaultCapacity,
            Action<object, object> removeMethod = null,
            BeforePutDelegate beforePutMethod = null,
            Action<object, object> hitMethod = null) : base(tKey, tValue, capacity)
        {
            mRemove = removeMethod;
            mBeforePutMethod = beforePutMethod;
            mHit = hitMethod;
        }


        public object GetOrCreate(object key, Func<object, object> factory)
        {
            var ret = Get(key);
            if (ret != null)
            {
                return ret;
            }

            return factory.Invoke(key);
        }

        protected override void HitCache(object key, object obj)
        {
            base.HitCache(key, obj);
            mHit?.Invoke(key, obj);
        }

        protected override bool CheckPutCache(object key, object obj)
        {
            return base.CheckPutCache(key, obj) && (mBeforePutMethod == null || mBeforePutMethod.Invoke(key, obj));
        }

        protected override void RemoveObject(object key, object obj)
        {
            base.RemoveObject(key, obj);
            mRemove?.Invoke(key, obj);
        }
    }

    public class KeyObjectPool<TKey, TValue> : BaseKeyObjectPool
    {
        public delegate bool BeforePutDelegate(TKey key, TValue obj);

        private readonly BeforePutDelegate mBeforePutMethod;

        private readonly Action<TKey, TValue> mRemove;
        private readonly Action<TKey, TValue> mHit;


        public KeyObjectPool(int capacity = DefaultCapacity, Action<TKey, TValue> removeMethod = null,
            BeforePutDelegate beforePutMethod = null,
            Action<TKey, TValue> hitMethod = null) : base(typeof(TKey), typeof(TValue), capacity)
        {
            mRemove = removeMethod;
            mBeforePutMethod = beforePutMethod;
            mHit = hitMethod;
        }

        public void Put(TKey key, TValue value)
        {
            base.Put(key, value);
        }

        public TValue GetOrCreate(TKey key, Func<TKey, TValue> factory)
        {
            var ret = Get(key);
            if (ret != null)
            {
                return ret;
            }

            return factory.Invoke(key);
        }

        public TValue Get(TKey key)
        {
            return (TValue) base.Get(key);
        }

        public bool ContainsValue(TValue v)
        {
            return base.ContainsValue(v);
        }

        protected override void HitCache(object key, object obj)
        {
            base.HitCache(key, obj);
            mHit?.Invoke((TKey) key, (TValue) obj);
        }

        protected override bool CheckPutCache(object key, object obj)
        {
            return base.CheckPutCache(key, obj) && (mBeforePutMethod == null || mBeforePutMethod.Invoke((TKey) key, (TValue) obj));
        }

        protected override void RemoveObject(object key, object obj)
        {
            base.RemoveObject(key, obj);
            mRemove?.Invoke((TKey) key, (TValue) obj);
        }
    }
}