using System;
using System.Collections.Generic;
using DeepU3.Asset;
using DeepU3.Async;
using UnityEngine;

namespace DeepU3.SceneSplit
{
    public class SplitStreamerPriorityLoader : MonoBehaviour
    {
        public class LoadQueueNode : IComparable<LoadQueueNode>
        {
            public SplitStreamer Streamer;
            public object UserData;
            public AssetAddress Address;
            public Action<ResultAsyncOperation<GameObject>> Callback;
            public int[] Pos;

            public bool IsLoading;
            public bool IsCompleted;

            public LoadQueueNode(AssetAddress assetAddress, SplitStreamer streamer, object userData, int[] pos, Action<ResultAsyncOperation<GameObject>> callback)
            {
                Address = assetAddress;
                Streamer = streamer;
                UserData = userData;
                Pos = pos;
                Callback = callback;
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

            private float GetDistance()
            {
                var r1 = Streamer.xPos - Pos[0];
                var r2 = Streamer.yPos - Pos[1];
                var r3 = Streamer.zPos - Pos[2];
                return Mathf.Sqrt(r1 * r1 + r2 * r2 + r3 * r3);
            }

            public int CompareTo(LoadQueueNode other)
            {
                var ret = other.Streamer.layerSetting.splitSize.magnitude.CompareTo(Streamer.layerSetting.splitSize.magnitude);
                return ret == 0 ? GetDistance().CompareTo(other.GetDistance()) : ret;
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

        static void SortedInsert<T>(LinkedList<T> list, T value) where T : IComparable<T>
        {
            if (list.First == null || value.CompareTo(list.First.Value) <= 0)
            {
                list.AddFirst(value);
            }
            else if (list.Last != null && value.CompareTo(list.Last.Value) >= 0)
            {
                list.AddLast(value);
            }
            else
            {
                var node = list.First;
                LinkedListNode<T> next;
                while ((next = node.Next) != null && next.Value.CompareTo(value) < 0)
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

                    if (p?.Value.Streamer.layerSetting.gameObjectPrefix != cur.Value.Streamer.layerSetting.gameObjectPrefix)
                    {
                        break;
                    }
                }
            }
        }

        public void AddQueue(AssetAddress assetAddress, SplitStreamer streamer, object userData, int[] pos, Action<ResultAsyncOperation<GameObject>> callback)
        {
            var node = new LoadQueueNode(assetAddress, streamer, userData, pos, callback);
            if (streamer.manager.playerTransform && streamer.manager.playerTransform.gameObject.scene != streamer.gameObject.scene)
            {
                mLoadQueues.AddLast(node);
            }
            else
            {
                SortedInsert(mLoadQueues, node);
            }

            mTotalCount++;
        }


        public float Progress => mCompletedCount / (float) mTotalCount;
    }
}