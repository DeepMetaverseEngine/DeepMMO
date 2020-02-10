using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DeepMMO.Unity3D.Entity
{
    public sealed partial class EntityLayer : GameEntity.BaseEntityLayer
    {
        private GameEntity[] mEntities = new GameEntity[100];

        private readonly Dictionary<Type, IEntityComponent[]> mComponentsMap = new Dictionary<Type, IEntityComponent[]>(20);
        private readonly Dictionary<KeyValuePair<Type, Type>, Dictionary<object, IEntityComponent>> mCustomIndexes = new Dictionary<KeyValuePair<Type, Type>, Dictionary<object, IEntityComponent>>();

        private readonly Dictionary<Type, bool[]> mComponentReleaseFlag = new Dictionary<Type, bool[]>(20);

        /// <summary>
        /// 标记新增component未经过Update
        /// </summary>
        private readonly List<IEntityComponent> mComponentsAdded = new List<IEntityComponent>();

        private IEntityComponent[] GetTypeComponents(Type target)
        {
            if (!mComponentsMap.TryGetValue(target, out var ret))
            {
                ret = new IEntityComponent[mEntities.Length];
                mComponentsMap.Add(target, ret);
            }
            else if (ret.Length != mEntities.Length)
            {
                Array.Resize(ref ret, mEntities.Length);
            }

            return ret;
        }

        private bool[] GetTypeComponentsReleaseFlag(Type target)
        {
            if (!mComponentReleaseFlag.TryGetValue(target, out var ret))
            {
                ret = new bool[mEntities.Length];
                mComponentReleaseFlag.Add(target, ret);
            }
            else if (ret.Length != mEntities.Length)
            {
                Array.Resize(ref ret, mEntities.Length);
            }

            return ret;
        }


        private readonly List<int> mSingletonIndexes = new List<int>(10);

        #region Enumerator & Enumerable

        public struct Enumerator : IEnumerator<IEntityComponent>
        {
            private int mIndex;
            private readonly IEntityComponent[] mArray;
            private readonly Predicate<IEntityComponent> mPredicate;


            private readonly EntityLayer mLayer;

            internal Enumerator(EntityLayer layer, Type t, Predicate<IEntityComponent> predicate = null)
            {
                mLayer = layer;
                mArray = layer.GetTypeComponents(t);
                mIndex = 0;
                Current = null;
                mPredicate = predicate;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                Current = null;
                if (mArray == null || mIndex >= mArray.Length)
                {
                    return false;
                }

                while (mIndex < mArray.Length)
                {
                    var p = mArray[mIndex];
                    mIndex++;
                    if (p == null || mLayer.IsReleased(p))
                    {
                        continue;
                    }

                    if (mPredicate != null && !mPredicate.Invoke(p))
                    {
                        continue;
                    }

                    Current = p;
                    break;
                }

                return Current != null;
            }

            public void Reset()
            {
                mIndex = 0;
                Current = null;
            }

            object IEnumerator.Current => Current;

            public IEntityComponent Current { get; private set; }
        }

        public struct EnumeratorMore : IEnumerator<IEntityComponent[]>
        {
            private readonly Type[] mTypes;
            private readonly IEnumerator<IEntityComponent> mFirst;
            private readonly IEntityComponent[] mEachComponents;
            private readonly EntityLayer mLayer;

            public EnumeratorMore(EntityLayer layer, Type[] types)
            {
                mLayer = layer;
                mTypes = types;

                mFirst = new Enumerator(layer, mTypes[0]);
                mEachComponents = new IEntityComponent[types.Length];
            }

            public bool MoveNext()
            {
                mFirst.MoveNext();
                if (mFirst.Current == null)
                {
                    Array.Clear(mEachComponents, 0, mEachComponents.Length);
                    return false;
                }

                var c = mFirst.Current;
                var index = c.EntityIndex;
                mEachComponents[0] = c;
                for (var i = 1; i < mTypes.Length; i++)
                {
                    var targetType = GetTargetType(mTypes[i]);
                    var next = mLayer.GetTypeComponents(targetType)[index];
                    if (next == null || !mTypes[i].IsInstanceOfType(next))
                    {
                        Array.Clear(mEachComponents, 0, mEachComponents.Length);
                        return false;
                    }

                    mEachComponents[i] = next;
                }


                return true;
            }


            public void Reset()
            {
                mFirst.Reset();
            }

            public IEntityComponent[] Current => mEachComponents[0] != null ? mEachComponents : null;

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                mFirst.Dispose();
            }
        }

        public struct Enumerator<T> : IEnumerator<T> where T : class, IEntityComponent
        {
            private int mIndex;
            private readonly EntityLayer mLayer;
            private readonly IEntityComponent[] mArray;
            private Predicate<T> mPredicate;
            private T mCurrent;


            internal Enumerator(EntityLayer layer, Type t, Predicate<T> predicate = null)
            {
                mLayer = layer;
                mArray = layer.GetTypeComponents(t);
                mIndex = 0;
                mCurrent = null;
                mPredicate = predicate;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                mCurrent = null;
                if (mArray == null || mIndex >= mArray.Length)
                {
                    return false;
                }

                while (mIndex < mArray.Length)
                {
                    var p = mArray[mIndex];
                    mIndex++;
                    if (!(p is T) || mLayer.IsReleased(p))
                    {
                        continue;
                    }

                    if (mPredicate != null && !mPredicate.Invoke((T) p))
                    {
                        continue;
                    }

                    mCurrent = (T) p;
                }

                return mCurrent != null;
            }

            public void Reset()
            {
                mIndex = 0;
                mCurrent = null;
            }

            object IEnumerator.Current
            {
                get { return mCurrent; }
            }

            public T Current
            {
                get { return mCurrent; }
            }
        }

        public struct IndexEnumerator : IEnumerator<IEntityComponent>
        {
            private IEntityComponent mCurrent;
            private Dictionary<Type, IEntityComponent[]>.Enumerator mEnumerator;

            private readonly int mEntityIndex;

            private readonly EntityLayer mLayer;

            public IndexEnumerator(int entityIndex, EntityLayer layer)
            {
                mEntityIndex = entityIndex;
                mCurrent = null;
                mLayer = layer;
                mEnumerator = layer.mComponentsMap.GetEnumerator();
            }

            public bool MoveNext()
            {
                mCurrent = null;
                while (mEnumerator.MoveNext())
                {
                    var p = mEnumerator.Current.Value[mEntityIndex];
                    if (p == null || mLayer.IsReleased(p))
                    {
                        continue;
                    }

                    mCurrent = p;
                    break;
                }

                return mCurrent != null;
            }

            public void Reset()
            {
                ((IEnumerator) mEnumerator).Reset();
                mCurrent = null;
            }

            public IEntityComponent Current => mCurrent;

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                mEnumerator.Dispose();
            }
        }

        public struct EntityComponentsEnumerable : IEnumerable<IEntityComponent>
        {
            private readonly int mEntityIndex;
            private readonly EntityLayer mEntityLayer;

            public EntityComponentsEnumerable(EntityLayer layer, GameEntity entity)
            {
                mEntityLayer = layer;
                mEntityIndex = entity.Index;
            }

            public IEnumerator<IEntityComponent> GetEnumerator()
            {
                return new IndexEnumerator(mEntityIndex, mEntityLayer);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public struct ComponentsEnumerable<T> : IEnumerable<T> where T : class, IEntityComponent
        {
            private readonly EntityLayer mEntityLayer;
            private readonly Predicate<T> mPredicate;

            public ComponentsEnumerable(EntityLayer layer, Predicate<T> predicate = null)
            {
                mEntityLayer = layer;
                mPredicate = predicate;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return new Enumerator<T>(mEntityLayer, typeof(T), mPredicate);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        #endregion

        protected override void Disposing()
        {
            for (var i = 0; i < mEntities.Length; i++)
            {
                var e = mEntities[i];
                if (e)
                {
                    e.Release();
                }
            }

            Array.Clear(mEntities, 0, mEntities.Length);
        }


        public override void ReleaseComponent(IEntityComponent component)
        {
            var t = component.GetType();
            var targetType = GetTargetType(t);
            var flagArr = GetTypeComponentsReleaseFlag(targetType);

            flagArr[component.EntityIndex] = true;
        }

        public bool IsReleased(IEntityComponent component)
        {
            var targetType = GetTargetType(component.GetType());
            var flagArr = GetTypeComponentsReleaseFlag(targetType);
            return flagArr[component.EntityIndex];
        }

        protected override int EntityAdded(GameEntity entity)
        {
            var index = 0;
            for (index = 0; index < mEntities.Length; index++)
            {
                if (mEntities[index] != null)
                {
                    continue;
                }

                mEntities[index] = entity;
                break;
            }

            if (index >= mEntities.Length)
            {
                Array.Resize(ref mEntities, mEntities.Length << 1);
                foreach (var key in mComponentsMap.Keys.ToArray())
                {
                    var arr = mComponentsMap[key];
                    Array.Resize(ref arr, mEntities.Length << 1);
                    mComponentsMap[key] = arr;
                }

                mEntities[index] = entity;
            }

            return index;
        }

        private void AddCustomIndex(Type target, object key, IEntityComponent component)
        {
            var t = new KeyValuePair<Type, Type>(key.GetType(), target);
            if (!mCustomIndexes.TryGetValue(t, out var ret))
            {
                ret = new Dictionary<object, IEntityComponent>();
                mCustomIndexes[t] = ret;
            }

            ret.Add(key, component);
            ret[key] = component;
        }

        private IEntityComponent GetCustomIndexComponent(Type target, object key)
        {
            var t = new KeyValuePair<Type, Type>(key.GetType(), target);
            if (!mCustomIndexes.TryGetValue(t, out var map))
            {
                return null;
            }


            map.TryGetValue(key, out var ret);
            return ret;
        }

        private readonly Func<Type, object> mComponentFactorMethod;


        public EntityLayer(Func<GameEntity> entityFactory, Func<Type, object> componentFactory) : base(entityFactory)
        {
            mComponentFactorMethod = componentFactory;
        }

        public override IEntityComponent AddEntityComponent(GameEntity entity, Type t, object key = null)
        {
            var target = GetTargetType(t);
            var arr = GetTypeComponents(target);
            var component = (IEntityComponent) mComponentFactorMethod.Invoke(t);


            arr[entity.Index] = component;
            component.EntityIndex = entity.Index;
            component.Attached(entity);

            if (key != null)
            {
                AddCustomIndex(target, key, component);
            }

            mComponentsAdded.Add(component);
            return component;
        }


        private readonly List<BaseSystem> mSystems = new List<BaseSystem>();

        public override IEntityComponent GetEntityComponent(GameEntity entity, Type t)
        {
            var target = GetTargetType(t);
            var arr = GetTypeComponents(target);
            var c = arr[entity.Index];
            if (c != null && !IsReleased(c))
            {
                return c;
            }

            return null;
        }

        public override IEnumerable<IEntityComponent> GetEntityComponents(GameEntity entity)
        {
            return new EntityComponentsEnumerable(this, entity);
        }

        public override T GetSingletonComponent<T>()
        {
            T c;
            foreach (var i in mSingletonIndexes)
            {
                var e = mEntities[i];
                if (!e)
                {
                    continue;
                }

                c = e.GetComponent<T>();
                if (c != null)
                {
                    return c;
                }
            }

            c = CreateEntity().AddComponent<T>();
            mSingletonIndexes.Add(c.EntityIndex);
            return c;
        }


        public override IEnumerable<T> GetComponents<T>(Predicate<T> condition = null)
        {
            return new ComponentsEnumerable<T>(this, condition);
        }


        public override T GetComponent<T>(Predicate<T> condition)
        {
            if (condition == null)
            {
                return null;
            }

            var target = GetTargetType(typeof(T));
            var arr = GetTypeComponents(target);
            foreach (var c in arr)
            {
                if (c != null && !IsReleased(c) && c is T component && condition.Invoke(component))
                {
                    return component;
                }
            }

            return null;
        }

        public override T GetComponent<T>(object key)
        {
            if (key == null)
            {
                return null;
            }

            var target = GetTargetType(typeof(T));
            return GetCustomIndexComponent(target, key) as T;
        }

        public abstract class BaseSystem : ISystem
        {
            public interface IComponentGroup
            {
                void Update(EntityLayer layer);
                Type[] Types { get; }
                void GroupElementRemoved(GameEntity entity);
                void GroupElementAdded(IEntityComponent[] elements);
            }

            #region group

            protected abstract class ComponentGroup : IComponentGroup
            {
                public Type[] Types { get; private set; }

                public delegate void GroupElementValidHandler(IEntityComponent[] comps);

                public delegate void GroupElementInvalidHandler(GameEntity entity);


                private readonly GroupElementInvalidHandler mRemoved;

                protected ComponentGroup(GroupElementInvalidHandler removed, params Type[] types)
                {
                    mRemoved = removed;
                    Types = types;
                }

                protected abstract void UpdateComponents(IEntityComponent[] comps);

                public void Update(EntityLayer layer)
                {
                    using (var it = new EnumeratorMore(layer, Types))
                    {
                        while (it.MoveNext())
                        {
                            UpdateComponents(it.Current);
                        }
                    }
                }

                public virtual void GroupElementAdded(IEntityComponent[] comps)
                {
                }

                public virtual void GroupElementRemoved(GameEntity entity)
                {
                    mRemoved?.Invoke(entity);
                }
            }

            protected class ComponentGroup<T1, T2> : ComponentGroup
            {
                public delegate void GroupElementHandler(T1 c1, T2 c2);

                private readonly GroupElementHandler mAct;
                private readonly GroupElementHandler mAdded;

                public ComponentGroup(GroupElementHandler act, GroupElementHandler added = null, GroupElementInvalidHandler removed = null) : base(removed, typeof(T1), typeof(T2))
                {
                    mAct = act;
                    mAdded = added;
                }

                protected override void UpdateComponents(IEntityComponent[] comps)
                {
                    mAct?.Invoke((T1) comps[0], (T2) comps[1]);
                }

                public override void GroupElementAdded(IEntityComponent[] comps)
                {
                    mAdded?.Invoke((T1) comps[0], (T2) comps[1]);
                }
            }

            protected class ComponentGroup<T1, T2, T3> : ComponentGroup
            {
                public delegate void GroupElementHandler(T1 c1, T2 c2, T3 c3);

                private readonly GroupElementHandler mAct;
                private readonly GroupElementHandler mAdded;

                public ComponentGroup(GroupElementHandler act, GroupElementHandler added = null, GroupElementInvalidHandler removed = null) : base(removed, typeof(T1), typeof(T2), typeof(T3))
                {
                    mAct = act;
                    mAdded = added;
                }

                protected override void UpdateComponents(IEntityComponent[] comps)
                {
                    mAct?.Invoke((T1) comps[0], (T2) comps[1], (T3) comps[2]);
                }

                public override void GroupElementAdded(IEntityComponent[] comps)
                {
                    mAdded?.Invoke((T1) comps[0], (T2) comps[1], (T3) comps[2]);
                }
            }

            protected class ComponentGroup<T1, T2, T3, T4> : ComponentGroup
            {
                public delegate void GroupElementHandler(T1 c1, T2 c2, T3 c3, T4 c4);

                private readonly GroupElementHandler mAct;
                private readonly GroupElementHandler mAdded;

                public ComponentGroup(GroupElementHandler act, GroupElementHandler added = null, GroupElementInvalidHandler removed = null) : base(removed, typeof(T1), typeof(T2), typeof(T3), typeof(T4))
                {
                    mAct = act;
                    mAdded = added;
                }

                protected override void UpdateComponents(IEntityComponent[] comps)
                {
                    mAct?.Invoke((T1) comps[0], (T2) comps[1], (T3) comps[2], (T4) comps[3]);
                }

                public override void GroupElementAdded(IEntityComponent[] comps)
                {
                    mAdded?.Invoke((T1) comps[0], (T2) comps[1], (T3) comps[2], (T4) comps[3]);
                }
            }

            protected class ComponentGroup<T1, T2, T3, T4, T5> : ComponentGroup
            {
                public delegate void GroupElementHandler(T1 c1, T2 c2, T3 c3, T4 c4, T5 c5);

                private readonly GroupElementHandler mAct;
                private readonly GroupElementHandler mAdded;

                public ComponentGroup(GroupElementHandler act, GroupElementHandler added = null, GroupElementInvalidHandler removed = null) : base(removed, typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5))
                {
                    mAct = act;
                    mAdded = added;
                }

                protected override void UpdateComponents(IEntityComponent[] comps)
                {
                    mAct?.Invoke((T1) comps[0], (T2) comps[1], (T3) comps[2], (T4) comps[3], (T5) comps[4]);
                }

                public override void GroupElementAdded(IEntityComponent[] comps)
                {
                    mAdded?.Invoke((T1) comps[0], (T2) comps[1], (T3) comps[2], (T4) comps[3], (T5) comps[4]);
                }
            }

            protected class ComponentGroup<T1, T2, T3, T4, T5, T6> : ComponentGroup
            {
                public delegate void GroupElementHandler(T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6);

                private readonly GroupElementHandler mAct;
                private readonly GroupElementHandler mAdded;

                public ComponentGroup(GroupElementHandler act, GroupElementHandler added = null, GroupElementInvalidHandler removed = null) : base(removed, typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6))
                {
                    mAct = act;
                    mAdded = added;
                }

                protected override void UpdateComponents(IEntityComponent[] comps)
                {
                    mAct?.Invoke((T1) comps[0], (T2) comps[1], (T3) comps[2], (T4) comps[3], (T5) comps[4], (T6) comps[5]);
                }

                public override void GroupElementAdded(IEntityComponent[] comps)
                {
                    mAdded?.Invoke((T1) comps[0], (T2) comps[1], (T3) comps[2], (T4) comps[3], (T5) comps[4], (T6) comps[5]);
                }
            }

            #endregion

            public readonly List<IComponentGroup> Groups = new List<IComponentGroup>();

            public readonly string Name;

            protected BaseSystem(string name)
            {
                Name = name;
                Enable = true;
            }

            public EntityLayer Layer { get; private set; }

            public void Initialize(EntityLayer layer)
            {
                Layer = layer;
            }

            public void Update()
            {
                OnUpdate();
                foreach (var componentGroup in Groups)
                {
                    componentGroup.Update(Layer);
                }
            }

            protected virtual void OnUpdate()
            {
            }

            protected void AddGroup(ComponentGroup group)
            {
                Groups.Add(group);
            }

            public bool Enable { get; set; }
        }


        public override void Update()
        {
            for (var i = 0; i < mEntities.Length; i++)
            {
                var entity = mEntities[i];
                if (entity == null)
                {
                    continue;
                }

                if (entity.IsReleased)
                {
                    mEntities[i] = null;
                }
                else
                {
                    entity.Update();
                }
            }

            foreach (var component in mComponentsAdded)
            {
                var entity = mEntities[component.EntityIndex];
                foreach (var s in mSystems)
                {
                    foreach (var componentGroup in s.Groups)
                    {
                        var comps = new IEntityComponent[componentGroup.Types.Length];
                        var index = 0;
                        var checkOk = componentGroup.Types.All(ct =>
                        {
                            var c = entity.GetComponent(ct);
                            comps[index++] = c;
                            return c != null;
                        });
                        if (checkOk)
                        {
                            componentGroup.GroupElementAdded(comps);
                        }
                    }
                }
            }

            mComponentsAdded.Clear();


            //components update
            foreach (var entry in mComponentsMap)
            {
                for (var i = 0; i < entry.Value.Length; i++)
                {
                    var c = entry.Value[i];
                    if (c == null)
                    {
                        continue;
                    }

                    if (IsReleased(c))
                    {
                        entry.Value[i] = null;

                        foreach (var s in mSystems)
                        {
                            if (!s.Enable)
                            {
                                continue;
                            }

                            foreach (var g in s.Groups)
                            {
                                var checkOk = g.Types.All(ct =>
                                {
                                    if (c.GetType().IsAssignableFrom(ct))
                                    {
                                        return true;
                                    }

                                    var arr = GetTypeComponents(ct);
                                    return arr[c.EntityIndex] != null;
                                });

                                if (checkOk)
                                {
                                    g.GroupElementRemoved(mEntities[c.EntityIndex]);
                                }
                            }
                        }
                    }
                    else
                    {
                        c.Update(mEntities[c.EntityIndex]);
                    }
                }
            }

            foreach (var s in mSystems)
            {
                if (!s.Enable)
                {
                    continue;
                }

                s.Update();
            }
        }


        public void AddSystem(BaseSystem s)
        {
            mSystems.Add(s);
            s.Initialize(this);
        }

        public void RemoveSystem(BaseSystem s)
        {
            mSystems.Remove(s);
        }

        public BaseSystem GetSystem(string name)
        {
            return mSystems.FirstOrDefault(s => s.Name == name);
        }

        public T FindSystemAs<T>(Predicate<T> predicate = null) where T : BaseSystem
        {
            return mSystems.FirstOrDefault(s => (s is T ts) && (predicate == null || predicate.Invoke(ts))) as T;
        }
    }
}