using System;
using UnityEngine;

namespace DeepU3.Timers
{
    /// <summary>
    /// 帧分片任务, 例: 把50个任务分为3秒执行
    /// </summary>
    public sealed class FrameScheduler
    {
        public int TaskCount { get; private set; }
        public object UserState { get; set; }
        public int NextExecuteIndex { get; private set; }

        private float mSlice;

        private Action<int> _execute;

        private readonly Timer mTimer;
        private Action mCompleteAct;
        private bool mIsCompleted;
        private readonly MonoBehaviour _autoDestroyOwner;
        private readonly bool mUseAutoDestroyOwner = false;

        private FrameScheduler(float duration, Action<int> execute = null, Action onComplete = null, MonoBehaviour autoDestroyOwner = null)
        {
            _execute = execute;
            mCompleteAct = onComplete;
            _autoDestroyOwner = autoDestroyOwner;
            mTimer = new Timer(duration, Complete, OnUpdate, false, true, autoDestroyOwner);
            if (_autoDestroyOwner)
            {
                mUseAutoDestroyOwner = true;
            }
        }

        public static FrameScheduler Register(float duration, Action<int> execute = null, Action onComplete = null, MonoBehaviour autoDestroyOwner = null)
        {
            var scheduler = new FrameScheduler(duration, execute, onComplete, autoDestroyOwner);
            return scheduler;
        }

        public void Cancel()
        {
            mTimer.Cancel();
        }

        public void Schedule(int taskCount)
        {
            if (taskCount <= 0)
            {
                return;
            }
            mSlice = mTimer.duration / taskCount;
            TaskCount = taskCount;
            mIsCompleted = false;
            mTimer.Reset();
            if (mUseAutoDestroyOwner && !_autoDestroyOwner)
            {
                throw new ObjectDisposedException(_autoDestroyOwner.ToString());
            }

            Timer.Manager.RegisterTimer(mTimer);
        }

        public void Schedule(int taskCount, object state, Action<int> execute, Action onComplete = null)
        {
            Schedule(taskCount);
            _execute = execute;
            mCompleteAct = onComplete;
        }


        public void Complete()
        {
            if (mIsCompleted)
            {
                return;
            }

            while (NextExecuteIndex < TaskCount)
            {
                _execute(NextExecuteIndex);
                NextExecuteIndex++;
            }

            mIsCompleted = true;
            mCompleteAct?.Invoke();
        }

        private void OnUpdate(float timeElapsed)
        {
            var count = Mathf.FloorToInt(timeElapsed / mSlice);
            while (NextExecuteIndex < count)
            {
                _execute(NextExecuteIndex);
                NextExecuteIndex++;
            }
        }
    }
}