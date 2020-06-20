using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using DeepU3.Asset;

namespace DeepU3.Async
{
    public enum ResultOption
    {
        Auto,
        Succeeded,
        Failed,
    }

    public class ContinueAsyncOperation : BaseAsyncOperation
    {
        private BaseAsyncOperation mAsyncOperation;
        private Func<BaseAsyncOperation, BaseAsyncOperation> mFunc;

        private Action<BaseAsyncOperation> mAction;

        public ContinueAsyncOperation(BaseAsyncOperation ao, Func<BaseAsyncOperation, BaseAsyncOperation> func)
        {
            mAsyncOperation = ao;
            mFunc = func;
        }

        public ContinueAsyncOperation(BaseAsyncOperation ao, Action<BaseAsyncOperation> act)
        {
            mAsyncOperation = ao;
            mAction = act;
        }

        protected internal override void Execute()
        {
            base.Execute();
            if (mFunc != null)
            {
                if (!mAsyncOperation.IsDone)
                {
                    return;
                }

                mAsyncOperation = mFunc.Invoke(mAsyncOperation);
                mFunc = null;
                mAction = null;
            }
            else if (mAction != null)
            {
                if (!mAsyncOperation.IsDone)
                {
                    return;
                }

                mAction.Invoke(mAsyncOperation);
                mFunc = null;
                mAction = null;
            }
            else if (mAsyncOperation.IsDone)
            {
                SetComplete(true);
            }
        }
    }

    public class BaseAsyncOperation : IEnumerator, IDisposable
    {
        public bool IsDone => Status != OperationStatus.None && !InvokeNextFrame;
        public bool IsSucceeded => Status == OperationStatus.Succeeded;
        public bool IsFailed => Status == OperationStatus.Failed;
        public object Current => null;
        public object UserData;

        internal bool InvokeNextFrame;
        public bool IsDisposed { get; private set; }

        public static bool AlwaysDelayFrame = true;

        public bool MoveNext()
        {
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

        public BaseAsyncOperation ContinueWith(Func<BaseAsyncOperation, BaseAsyncOperation> func)
        {
            return new ContinueAsyncOperation(this, func);
        }

        public BaseAsyncOperation ContinueWith(Action<BaseAsyncOperation> func)
        {
            return new ContinueAsyncOperation(this, func);
        }

        internal BaseAsyncOperation SetComplete(bool success)
        {
            Status = success ? OperationStatus.Succeeded : OperationStatus.Failed;
            if (AlwaysDelayFrame)
            {
                InvokeNextFrame = true;
            }
            else
            {
                InvokeCompleteEvent();
            }

            return this;
        }

        protected internal virtual void InvokeCompleteEvent()
        {
            InvokeNextFrame = false;
            mCompleted?.Invoke(this);
            mWaitHandle?.Set();
        }


        public BaseAsyncOperation(bool success) : this()
        {
            Status = success ? OperationStatus.Succeeded : OperationStatus.Failed;
        }


        protected BaseAsyncOperation()
        {
            AsyncOperationUpdater.IOperations.Add(this);
        }

        protected internal virtual void Execute()
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

        public static BaseAsyncOperation CompletedOperation => new BaseAsyncOperation().SetComplete(true);

        public Task Task
        {
            get
            {
                if (IsDone)
                {
                    return Task.CompletedTask;
                }

                var handle = WaitHandle;
                return Task.Factory.StartNew(o => { handle.WaitOne(); }, this);
            }
        }

        protected virtual void OnDisposing()
        {
            mCompleted = null;
            mWaitHandle?.Dispose();
        }

        ~BaseAsyncOperation()
        {
            OnDisposing();
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            IsDisposed = true;
            OnDisposing();
            GC.SuppressFinalize(this);
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

        protected internal override void Execute()
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

        public CollectionResultAsyncOperation(IEnumerable<ResultAsyncOperation<TV>> preEnumerators) : base(preEnumerators)
        {
        }

        public CollectionResultAsyncOperation()
        {
        }


        public bool TrySetResults(ICollection results)
        {
            var completes = new IEnumerator[results.Count];
            var i = 0;
            foreach (var o in results)
            {
                if (o is TV tv)
                {
                    completes[i++] = new ResultAsyncOperation<TV>(tv);
                }
                else
                {
                    return false;
                }
            }

            SetPreEnumerator(completes);
            SetComplete(true);
            return true;
        }

        public CollectionResultAsyncOperation(TV[] ret)
        {
            var its = new IEnumerator[ret.Length];
            for (var i = 0; i < its.Length; i++)
            {
                its[i] = new ResultAsyncOperation<TV>(ret[i]);
            }

            SetPreEnumerator(its);
            SetComplete(true);
        }

        private Action<TV[]> mCompleted;
        private Action<ResultAsyncOperation<TV>[]> mCompletedFullArray;
        private Action<CollectionResultAsyncOperation<TV>> mCompletedFullType;

        protected internal override void InvokeCompleteEvent()
        {
            base.InvokeCompleteEvent();
            mCompleted?.Invoke(Result);
            mCompletedFullArray?.Invoke(CastTo<ResultAsyncOperation<TV>>().ToArray());
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

        protected override void OnDisposing()
        {
            base.OnDisposing();
            mCompletedFullArray = null;
            mCompletedFullType = null;
        }

        public CollectionResultAsyncOperation<TV> Subscribe(Action<ResultAsyncOperation<TV>[]> cb)
        {
            if (IsDone)
            {
                cb.Invoke(CastTo<ResultAsyncOperation<TV>>().ToArray());
            }
            else
            {
                mCompletedFullArray += cb;
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

        protected internal override void Execute()
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


        protected internal override void InvokeCompleteEvent()
        {
            base.InvokeCompleteEvent();
            mCompleted?.Invoke(Result);
            mCompletedFullType?.Invoke(this);
        }

        protected override void OnDisposing()
        {
            base.OnDisposing();
            mCompleted = null;
            mCompletedFullType = null;
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