using System;

namespace DeepMMO.Unity3D.Entity
{
    public partial class GameEntity
    {
        public int Index { get; private set; }
        private bool mActive;

        public bool Active
        {
            get => mActive;
            set
            {
                if (mActive == value)
                {
                    return;
                }

                mActive = value;
                ActiveChanged();
            }
        }


        private void ActiveChanged()
        {
            foreach (var c in Layer.GetEntityComponents(this))
            {
                if (Active)
                {
                    c.OnEntityBecameActive();
                }
                else
                {
                    c.OnEntityBecameInactive();
                }
            }
        }


        public bool IsReleased { get; private set; }

        public void Release()
        {
            if (IsReleased)
            {
                return;
            }

            IsReleased = true;

            foreach (var c in Layer.GetEntityComponents(this))
            {
                Layer.ReleaseComponent(c);
                ;
            }
        }


        public IEntityComponent AddComponent(Type t, object key = null)
        {
            return Layer.AddEntityComponent(this, t, key);
        }

        public IEntityComponent GetComponent(Type t)
        {
            return Layer.GetEntityComponent(this, t);
        }

        public T AddComponent<T>(object key = null) where T : class, IEntityComponent
        {
            var t = typeof(T);
            return AddComponent(t, key) as T;
        }

        public T GetComponent<T>() where T : class, IEntityComponent
        {
            return GetComponent(typeof(T)) as T;
        }


        public ProxyComponent<T> AddProxyComponent<T>(T data, object key = null)
        {
            var c = AddComponent<ProxyComponent<T>>(key);
            c.Data = data;
            return c;
        }

        public void Update()
        {
        }

        public static implicit operator bool(GameEntity value)
        {
            return value != null && !value.IsReleased;
        }
    }
}