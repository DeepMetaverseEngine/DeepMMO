using System;
using System.Collections.Generic;

namespace DeepMMO.Unity3D.Entity
{
    public interface ISystem
    {
        EntityLayer Layer { get; }
        void Initialize(EntityLayer layer);
        bool Enable { get; set; }
    }

    //if EntiyComponent is IDisposable, destroy call Dispose
    public interface IEntityComponent
    {
        int EntityIndex { get; set; }
        void OnEntityUpdate();
        void OnEntityBecameActive();
        void OnEntityBecameInactive();
        void OnAttached();
    }

    public interface IEntityLayer : IDisposable
    {
        bool IsDisposed { get; }
        IEntityComponent AddEntityComponent(GameEntity entity, Type t, object key = null);
        IEntityComponent GetEntityComponent(GameEntity entity, Type t);
        T GetSingletonComponent<T>() where T : class, IEntityComponent;
        IEnumerable<T> GetComponents<T>(Predicate<T> condition = null) where T : class, IEntityComponent;
        IEnumerable<IEntityComponent> GetEntityComponents(GameEntity entity);
        T GetComponent<T>(Predicate<T> condition) where T : class, IEntityComponent;
        T GetComponent<T>(object key) where T : class, IEntityComponent;
        void ReleaseComponent(IEntityComponent component);
        void Update();
    }


    public interface IEntityLayerManager
    {
        IEntityLayer CreateLayer(string layerTag);
        void Update();
    }

    public abstract class DefaultEntityComponent : IEntityComponent
    {
        public int EntityIndex { get; set; }

        public virtual void OnEntityUpdate()
        {
        }

        public virtual void OnEntityBecameActive()
        {
        }

        public virtual void OnEntityBecameInactive()
        {
        }

        public virtual void OnAttached()
        {
        }
    }


    public class ProxyComponent : DefaultEntityComponent
    {
        public object Data;
    }

    public class ProxyComponent<T> : ProxyComponent
    {
        public new T Data
        {
            get => (T) base.Data;
            set => base.Data = value;
        }

        public static implicit operator T(ProxyComponent<T> value)
        {
            return value != null ? (T) value.Data : default;
        }
    }
}