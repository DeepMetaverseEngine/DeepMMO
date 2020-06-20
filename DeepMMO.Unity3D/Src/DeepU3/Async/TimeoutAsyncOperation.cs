using UnityEngine;

namespace DeepU3.Async
{
    public class TimeoutAsyncOperation : BaseAsyncOperation
    {
        protected internal override void Execute()
        {
            if (Time.time - mCreateTime > TimeoutSec)
            {
                SetComplete(true);
            }
        }

        private readonly float mCreateTime;

        public TimeoutAsyncOperation(float timeoutSec)
        {
            mCreateTime = Time.time;
            TimeoutSec = timeoutSec;
        }

        public float TimeoutSec { get; }
    }
}