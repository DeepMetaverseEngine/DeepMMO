using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DeepMMO.Unity3D.Entity
{
    public partial class GameEntity
    {
        public IEntityLayer Layer { get; private set; }

        public abstract class BaseEntityLayer : IEntityLayer
        {
            public bool IsDisposed { get; private set; }

            public void Dispose()
            {
                if (IsDisposed)
                {
                    return;
                }
                IsDisposed = true;
                Disposing();
            }


            protected abstract int EntityAdded(GameEntity entity);
            protected abstract void Disposing();
            private readonly Func<GameEntity> mEntityFactorMethod;

            protected BaseEntityLayer(Func<GameEntity> entityFactory)
            {
                mEntityFactorMethod = entityFactory;
            }

            public GameEntity CreateEntity()
            {
                var ret = mEntityFactorMethod.Invoke();
                ret.Layer = this;
                ret.Index = EntityAdded(ret);
                return ret;
            }

            public abstract void ReleaseComponent(IEntityComponent component);

            public abstract IEntityComponent AddEntityComponent(GameEntity entity, Type t, object key = null);
            public abstract IEntityComponent GetEntityComponent(GameEntity entity, Type t);

            public abstract T GetSingletonComponent<T>() where T : class, IEntityComponent;

            public abstract IEnumerable<T> GetComponents<T>(Predicate<T> condition = null) where T : class, IEntityComponent;
            public abstract IEnumerable<IEntityComponent> GetEntityComponents(GameEntity entity);

            public abstract T GetComponent<T>(object key) where T : class, IEntityComponent;
            public abstract T GetComponent<T>(Predicate<T> condition) where T : class, IEntityComponent;
            public abstract void Update();
        }
    }
}