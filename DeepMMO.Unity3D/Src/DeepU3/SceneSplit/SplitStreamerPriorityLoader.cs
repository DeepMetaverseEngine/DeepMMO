using System;
using System.Collections.Generic;
using DeepU3.Asset;
using DeepU3.Async;
using DeepU3.Cache;
using UnityEngine;

namespace DeepU3.SceneSplit
{
    public class SplitStreamerPriorityLoader : MonoBehaviour
    {
        private class LoadQueueNode : IComparable<LoadQueueNode>
        {
            public object UserData;
            public InstantiationAssetAddress Address;
            public Action<ResultAsyncOperation<GameObject>> Callback;

            public bool IsLoading;
            public bool IsCompleted;

            public float Distance;

            private LoadQueueNode()
            {
            }

            private static readonly ObjectPool<LoadQueueNode> sObjectPool = new ObjectPool<LoadQueueNode>(100);

            public static LoadQueueNode Alloc(InstantiationAssetAddress assetAddress, SplitStreamer streamer, object userData, int[] pos, Action<ResultAsyncOperation<GameObject>> callback)
            {
                var ret = sObjectPool.Get() ?? new LoadQueueNode();

                ret.Address = assetAddress;
                ret.UserData = userData;
                ret.Callback = callback;
                var r1 = streamer.xPos - pos[0];
                var r2 = streamer.yPos - pos[1];
                var r3 = streamer.zPos - pos[2];
                ret.Distance = Mathf.Sqrt(r1 * r1 + r2 * r2 + r3 * r3) / streamer.SqrMagnitude;
                return ret;
            }

            public void Release()
            {
                Address = null;
                UserData = null;
                Callback = null;
                IsLoading = false;
                IsCompleted = false;
                sObjectPool.Put(this);
            }

            private void OnLoadComplete(ResultAsyncOperation<GameObject> o)
            {
                Callback(o);
                IsCompleted = true;
                IsLoading = false;
            }

            public void Load()
            {
                IsLoading = true;
                var op = AssetManager.Instantiate(Address).Subscribe(OnLoadComplete);
                op.UserData = UserData;
            }


            public int CompareTo(LoadQueueNode other)
            {
                return Distance.CompareTo(other.Distance);
            }
        }

        private const int LoadingSameTime = 20;
        private uint mCompletedCount;
        private uint mTotalCount;


        private readonly LinkedList<LoadQueueNode> mLoadQueues = new LinkedList<LoadQueueNode>();


        private static SplitStreamerPriorityLoader sInstance;

        public static SplitStreamerPriorityLoader Instance
        {
            get
            {
                if (!sInstance)
                {
                    var obj = new GameObject("StreamerPriority");
                    sInstance = obj.AddComponent<SplitStreamerPriorityLoader>();
                    GameObject.DontDestroyOnLoad(obj);
                }

                return sInstance;
            }
        }

        private float mImmediateStart;
        private float mImmediateLimitTime;


        public void ImmediateAsSoonAsPossible(float sec)
        {
            mImmediateLimitTime = sec;
            mImmediateStart = Time.realtimeSinceStartup;
        }

        private void SortedInsert(LoadQueueNode value)
        {
            var list = mLoadQueues;
            if (list.First == null || value.Distance <= list.First.Value.Distance)
            {
                list.AddFirst(value);
            }
            else if (list.Last != null && value.Distance >= list.Last.Value.Distance)
            {
                list.AddLast(value);
            }
            else
            {
                var node = list.First;
                LinkedListNode<LoadQueueNode> next;
                while ((next = node.Next) != null && next.Value.Distance < value.Distance)
                {
                    node = next;
                }

                list.AddAfter(node, value);
            }
        }


        private void Update()
        {
            if (mLoadQueues.Count <= 0)
            {
                return;
            }

            var loadingCount = 0;
            var p = mLoadQueues.First;
            while (p != null && loadingCount < LoadingSameTime)
            {
                var cur = p;
                p = cur.Next;
                if (cur.Value.IsCompleted)
                {
                    mCompletedCount++;
                    cur.Value.Release();
                    mLoadQueues.Remove(cur);
                }
                else
                {
                    if (!cur.Value.IsLoading)
                    {
                        if (Time.realtimeSinceStartup - mImmediateStart < mImmediateLimitTime)
                        {
                            using (AssetManager.TryRunSynchronously())
                            {
                                cur.Value.Load();
                            }
                        }
                        else
                        {
                            cur.Value.Load();
                        }
                    }

                    if (!cur.Value.IsCompleted)
                    {
                        loadingCount++;
                    }
                }
            }
        }

        private Transform GetUsedMover(SplitStreamer streamer)
        {
            return streamer.mover ? streamer.mover : SplitStreamer.Mover;
        }

        public void AddQueue(InstantiationAssetAddress assetAddress, SplitStreamer streamer, object userData, int[] pos, Action<ResultAsyncOperation<GameObject>> callback)
        {
            var node = LoadQueueNode.Alloc(assetAddress, streamer, userData, pos, callback);
            var mover = GetUsedMover(streamer);
            if (mover && mover.gameObject.scene != streamer.gameObject.scene)
            {
                mLoadQueues.AddLast(node);
            }
            else
            {
                SortedInsert(node);
            }

            mTotalCount++;
        }


        public float Progress => mCompletedCount / (float) mTotalCount;
    }
}