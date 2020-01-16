using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace CoreUnity.Async
{
    public enum ResultOption
    {
        Auto,
        Succeeded,
        Failed,
    }

    public class BaseAsyncOperation : IEnumerator
    {
        public bool IsDone => Status != OperationStatus.None;
        public bool IsSucceeded => Status == OperationStatus.Succeeded;
        public bool IsFailed => Status == OperationStatus.Failed;
        public object Current => null;

        private bool mInvokeNextFrame;

        public bool MoveNext()
        {
            if (mInvokeNextFrame)
            {
                InvokeCompleteEvent();
            }

            Execute();
            return !IsDone;
        }

        public enum OperationStatus
        {
            None,
            Succeeded,
            Failed
        }

        public OperationStatus Status { get; private set; }
        private Action<BaseAsyncOperation> mCompleted;


        public BaseAsyncOperation Subscribe(Action<BaseAsyncOperation> cb)
        {
            if (IsDone)
            {
                cb.Invoke(this);
            }
            else
            {
                mCompleted += cb;
            }

            return this;
        }

        internal void SetComplete(bool success, bool invokeNextFrame = false)
        {
            Status = success ? OperationStatus.Succeeded : OperationStatus.Failed;
            mInvokeNextFrame = invokeNextFrame;
            if (!mInvokeNextFrame)
            {
                InvokeCompleteEvent();
            }
        }

        protected virtual void InvokeCompleteEvent()
        {
            mInvokeNextFrame = false;
            mCompleted?.Invoke(this);
            mWaitHandle?.Set();
        }


        public BaseAsyncOperation(bool success)
        {
            Status = success ? OperationStatus.Succeeded : OperationStatus.Failed;
        }


        public BaseAsyncOperation()
        {
        }

        protected virtual void Execute()
        {
        }

        public void Reset()
        {
        }


        private EventWaitHandle mWaitHandle;

        protected EventWaitHandle WaitHandle
        {
            get
            {
                if (mWaitHandle == null)
                {
                    mWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
                }

                mWaitHandle.Reset();
                return mWaitHandle;
            }
        }

        public Task Task
        {
            get
            {
                if (Status != OperationStatus.None)
                {
                    return Task.CompletedTask;
                }

                var handle = WaitHandle;
                return Task.Factory.StartNew(o => { handle.WaitOne(); }, this);
            }
        }
    }


    public class DecoratorAsyncOperation : BaseAsyncOperation
    {
        public IEnumerable<IEnumerator> PreEnumerators { get; private set; }

        public DecoratorAsyncOperation(params IEnumerator[] preEnumerators)
        {
            SetPreEnumerator(preEnumerators);
        }

        public DecoratorAsyncOperation(IEnumerable<IEnumerator> preEnumerators)
        {
            SetPreEnumerator(preEnumerators);
        }

        public DecoratorAsyncOperation()
        {
        }

        public void SetPreEnumerator(IEnumerable<IEnumerator> preEnumerators)
        {
            PreEnumerators = preEnumerators;
        }

        public void SetPreEnumerator(params IEnumerator[] preEnumerators)
        {
            PreEnumerators = preEnumerators;
        }

        public IEnumerable<TEnumerator> CastTo<TEnumerator>() where TEnumerator : IEnumerator
        {
            return PreEnumerators.Cast<TEnumerator>();
        }

        protected override void Execute()
        {
            base.Execute();
            if (PreEnumerators != null && PreEnumerators.All(e => !e.MoveNext()))
            {
                SetComplete(true);
            }
        }
    }


    public class CollectionResultAsyncOperation<TV> : DecoratorAsyncOperation
    {
        public TV[] Result => CastTo<ResultAsyncOperation<TV>>().Select(e => e.Result).ToArray();

        public CollectionResultAsyncOperation(params ResultAsyncOperation<TV>[] preEnumerators) : base(preEnumerators)
        {
        }

        public CollectionResultAsyncOperation(IEnumerable<ResultAsyncOperation<TV>> preEnumerators) : base(preEnumerators)
        {
        }

        public CollectionResultAsyncOperation()
        {
        }


        private Action<TV[]> mCompleted;
        private Action<CollectionResultAsyncOperation<TV>> mCompletedFullType;

        protected override void InvokeCompleteEvent()
        {
            base.InvokeCompleteEvent();
            mCompleted?.Invoke(Result);
            mCompletedFullType?.Invoke(this);
        }
        
        
        public CollectionResultAsyncOperation<TV> Subscribe(Action<TV[]> cb)
        {
            if (IsDone)
            {
                cb.Invoke(Result);
            }
            else
            {
                mCompleted += cb;
            }

            return this;
        }

        public new Task<TV[]> Task
        {
            get
            {
                if (Status != OperationStatus.None)
                {
                    return System.Threading.Tasks.Task.FromResult(Result);
                }

                var handle = WaitHandle;
                return System.Threading.Tasks.Task.Factory.StartNew(o =>
                {
                    var ao = (CollectionResultAsyncOperation<TV>) o;
                    handle.WaitOne();
                    return ao.Result;
                }, this);
            }
        }
    }


    public class TimeoutAsyncOperation : BaseAsyncOperation
    {
        protected override void Execute()
        {
            if (Time.time - mCreateTime > TimeoutSec)
            {
                SetComplete(true);
            }
        }

        private readonly float mCreateTime;

        public TimeoutAsyncOperation()
        {
            mCreateTime = Time.time;
        }

        public float TimeoutSec { get; set; } = 60f;
    }

    public class SceneLoadedAsyncOperation : TimeoutAsyncOperation
    {
        private readonly Scene mScene;

        public SceneLoadedAsyncOperation(Scene scene)
        {
            mScene = scene;
            SceneManager.sceneLoaded += OnSceneLoaded;
            if (!scene.IsValid())
            {
                SetComplete(false);
            }
            else if (scene.isLoaded)
            {
                SetComplete(true);
            }
            else
            {
                SceneManager.sceneLoaded += OnSceneLoaded;
            }
        }

        public SceneLoadedAsyncOperation(string sceneName) : this(SceneManager.GetSceneByName(sceneName))
        {
        }

        ~SceneLoadedAsyncOperation()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene s, LoadSceneMode mode)
        {
            if (s == mScene)
            {
                SetComplete(true);
            }
        }
    }

    public class ResultAsyncOperationDecorator<TV> : ResultAsyncOperation<TV>
    {
        private ResultAsyncOperation<TV> mSource;

        public ResultAsyncOperationDecorator()
        {
        }

        public ResultAsyncOperationDecorator(ResultAsyncOperation<TV> op)
        {
            SetSourceAsyncOperation(op);
        }

        protected override void Execute()
        {
            base.Execute();
            if (mSource != null && mSource.IsDone)
            {
                SetComplete(mSource.Result);
            }
        }

        public void SetSourceAsyncOperation(ResultAsyncOperation<TV> op)
        {
            mSource = op;
        }
    }

    public class ResultAsyncOperation<TV> : BaseAsyncOperation
    {
        public TV Result { get; private set; }
        private Action<TV> mCompleted;
        private Action<ResultAsyncOperation<TV>> mCompletedFullType;

        public ResultAsyncOperation<TV> Subscribe(Action<TV> cb)
        {
            if (IsDone)
            {
                cb.Invoke(Result);
            }
            else
            {
                mCompleted += cb;
            }

            return this;
        }

        public ResultAsyncOperation<TV> Subscribe(Action<ResultAsyncOperation<TV>> cb)
        {
            if (IsDone)
            {
                cb.Invoke(this);
            }
            else
            {
                mCompletedFullType += cb;
            }

            return this;
        }

        public static ResultAsyncOperation<TV> FromResult(TV result)
        {
            return new ResultAsyncOperation<TV>(result);
        }


        protected override void InvokeCompleteEvent()
        {
            base.InvokeCompleteEvent();
            mCompleted?.Invoke(Result);
            mCompletedFullType?.Invoke(this);
        }

        protected void OnComplete(TV obj)
        {
            SetComplete(obj);
        }


        public ResultAsyncOperation(Action<Action<TV>> callToAction)
        {
            callToAction.Invoke(OnComplete);
        }

        public ResultAsyncOperation(TV result)
        {
            OnComplete(result);
        }

        internal void SetComplete(TV obj, ResultOption opt = ResultOption.Auto)
        {
            Result = obj;
            var success = opt == ResultOption.Auto ? !Equals(default(TV), Result) : opt == ResultOption.Succeeded;
            SetComplete(success);
        }

        public ResultAsyncOperation()
        {
        }

        public new Task<TV> Task
        {
            get
            {
                if (Status != OperationStatus.None)
                {
                    return System.Threading.Tasks.Task.FromResult(Result);
                }

                var handle = WaitHandle;
                return System.Threading.Tasks.Task.Factory.StartNew(o =>
                {
                    var ao = (ResultAsyncOperation<TV>) o;
                    handle.WaitOne();
                    return ao.Result;
                }, this);
            }
        }
    }

    public class ResultAsyncOperation<TV, T1> : ResultAsyncOperation<TV>
    {
        public ResultAsyncOperation(T1 arg1, Action<T1, Action<TV>> callToAction)
        {
            callToAction.Invoke(arg1, OnComplete);
        }
    }

    public class ResultAsyncOperation<TV, T1, T2> : ResultAsyncOperation<TV>
    {
        public ResultAsyncOperation(T1 arg1, T2 arg2, Action<T1, T2, Action<TV>> callToAction)
        {
            callToAction.Invoke(arg1, arg2, OnComplete);
        }
    }

    public class ResultAsyncOperation<TV, T1, T2, T3> : ResultAsyncOperation<TV>
    {
        public ResultAsyncOperation(T1 arg1, T2 arg2, T3 arg3, Action<T1, T2, T3, Action<TV>> callToAction)
        {
            callToAction.Invoke(arg1, arg2, arg3, OnComplete);
        }
    }

    public class ResultAsyncOperation<TV, T1, T2, T3, T4> : ResultAsyncOperation<TV>
    {
        public ResultAsyncOperation(T1 arg1, T2 arg2, T3 arg3, T4 arg4, Action<T1, T2, T3, T4, Action<TV>> callToAction)
        {
            callToAction.Invoke(arg1, arg2, arg3, arg4, OnComplete);
        }
    }

    public class ResultAsyncOperation<TV, T1, T2, T3, T4, T5> : ResultAsyncOperation<TV>
    {
        public ResultAsyncOperation(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, Action<T1, T2, T3, T4, T5, Action<TV>> callToAction)
        {
            callToAction.Invoke(arg1, arg2, arg3, arg4, arg5, OnComplete);
        }
    }

    public class ResultAsyncOperation<TV, T1, T2, T3, T4, T5, T6> : ResultAsyncOperation<TV>
    {
        public ResultAsyncOperation(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, Action<T1, T2, T3, T4, T5, T6, Action<TV>> callToAction)
        {
            callToAction.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, OnComplete);
        }
    }

    public class ResultAsyncOperation<TV, T1, T2, T3, T4, T5, T6, T7> : ResultAsyncOperation<TV>
    {
        public ResultAsyncOperation(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, Action<T1, T2, T3, T4, T5, T6, T7, Action<TV>> callToAction)
        {
            callToAction.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, OnComplete);
        }
    }
}