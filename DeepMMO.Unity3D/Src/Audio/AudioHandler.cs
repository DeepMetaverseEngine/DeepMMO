using System;
using System.Collections.Generic;
using CoreUnity.Asset;
using CoreUnity.Async;
using DeepCore.Unity3D;
using UnityEngine;

namespace CoreUnity.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioHandler : MonoBehaviour
    {
        [Flags]
        public enum AudioType
        {
            BGM = 1,
            UI = 1 << 1,
            Dynamic = 1 << 2,
            Scene = 1 << 3,
        }

        private AudioSource mSource;

        //3d音乐音距
        private const int MinDistance = 3;
        private const int MaxDistance = 32;

        private AudioSource Source
        {
            get
            {
                if (!mSource)
                {
                    mSource = GetComponent<AudioSource>();
                }

                return mSource;
            }
        }

        public void SetSourceDistance(float min = MinDistance, float max = MaxDistance)
        {
            Source.minDistance = min;
            Source.maxDistance = max;
        }

        private static Transform sAudioSourceRoot;

        private static Transform AudioSourceRoot
        {
            get
            {
                if (!sAudioSourceRoot)
                {
                    var obj = new GameObject("AudioSourceRoot");
                    GameObject.DontDestroyOnLoad(obj);
                    sAudioSourceRoot = obj.transform;
                }

                return sAudioSourceRoot;
            }
        }

        private static readonly Dictionary<int, AudioHandler> sEnableAudioHandler = new Dictionary<int, AudioHandler>();


        internal static AudioHandler GetAudioHandler(int id)
        {
            sEnableAudioHandler.TryGetValue(id, out var ret);
            return ret;
        }

        internal static void StopAll(AudioType t)
        {
            foreach (var entry in sEnableAudioHandler)
            {
                if ((entry.Value.Type & t) != 0)
                {
                    entry.Value.Stop();
                }
            }
        }

        internal static void SetSoundVolume(AudioType t, float v)
        {
            foreach (var entry in sEnableAudioHandler)
            {
                if ((entry.Value.Type & t) != 0)
                {
                    entry.Value.Volume = v;
                }
            }
        }


        #region 动态控制参数

        /// <summary>
        /// 最大持续时间 小于等于0表示随AudioSource停止而处理Unload逻辑
        /// </summary>
        public float Duration;

        /// <summary>
        /// 持续时间是否包含加载时间
        /// </summary>
        public bool DurationContainsLoadTime;

        /// <summary>
        /// 是否为循环播放模式
        /// </summary>
        public bool Loop
        {
            get { return Source.loop; }
            set { Source.loop = value; }
        }

        /// <summary>
        /// 音量控制
        /// </summary>
        public float Volume
        {
            get { return Source.volume; }
            set { Source.volume = value; }
        }

        /// <summary>
        /// 3d, 2d 音效控制
        /// </summary>
        public float SpatialBlend
        {
            get { return Source.spatialBlend; }
            set { Source.spatialBlend = value; }
        }

        /// <summary>
        /// 是否正在播放
        /// </summary>
        public bool IsPlaying
        {
            get { return Source.isPlaying; }
        }

        #endregion

        public AudioType Type { get; private set; }
        public bool AutoUnload { get; private set; }

        private ResultAsyncOperation<AudioClip> mNextLoader;

        /// <summary>
        /// 加载失败
        /// </summary>
        private bool mLoadFailed;

        /// <summary>
        /// 是否已暂停
        /// </summary>
        private bool mPause;

        /// <summary>
        /// 是否已停止
        /// </summary>
        private bool mStoped;

        /// <summary>
        /// 是否可以开始播放
        /// </summary>
        private bool mStartPlaying;

        /// <summary>
        /// 持续总时间
        /// </summary>
        public float PassTime { get; private set; }

        private bool mStopUpdate = false;

        private bool TryUnload()
        {
            var ret = false;
            if (mLoadFailed || mStoped)
            {
                // 加载失败
                ret = true;
            }
            else if (Duration > 0.001)
            {
                // 时长超出限制
                ret = Duration <= PassTime;
            }
            else if (mNextLoader != null)
            {
                //正在加载
                ret = false;
            }
            else if (!Loop && mStartPlaying)
            {
                // 已播放完成
                ret = !Source.isPlaying && !mPause;
            }

            if (ret)
            {
                if (AutoUnload)
                {
                    Unload();
                }
                else if (!mStoped)
                {
                    Stop();
                }
            }

            return ret;
        }


        private void SetAudioClip(AudioClip clip)
        {
            Source.clip = clip;

            //加载完成 ，如果调用过Play且没有Pause，自动播放
            if (mStartPlaying && !mPause)
            {
                Source.Play();
            }
        }

        private bool TryLoading()
        {
            if (mNextLoader != null && mNextLoader.IsDone)
            {
                ReleaseCurrent();
                //处理加载完成逻辑
                if (mNextLoader.Result)
                {
                    SetAudioClip(mNextLoader.Result);
                }
                else
                {
                    mLoadFailed = true;
                    Debug.LogWarning(this + "load error ");
                }

                mNextLoader = null;
            }

            return Source.clip;
        }

        private void Update()
        {
            if (mStopUpdate || TryUnload())
            {
                return;
            }

            if (mStartPlaying && DurationContainsLoadTime && !mPause)
            {
                PassTime = PassTime + Time.deltaTime;
            }

            if (!TryLoading())
            {
                return;
            }

            if (mStartPlaying && !mPause)
            {
                PassTime = PassTime + Time.deltaTime;
            }
        }

        private void Reset()
        {
            Stop();
            Duration = 0;
            PassTime = 0;
            mLoadFailed = false;
            transform.position = Vector3.zero;
            ReleaseCurrent();
        }

        private void Unload()
        {
            Reset();

            mStopUpdate = true;
            sAudioHandlerPool.Put(this);
        }

        public void Pause()
        {
            mStoped = false;
            mPause = true;
            if (!Source.clip)
            {
                return;
            }

            Source.Pause();
        }

        public void Stop()
        {
            if (Source.clip)
            {
                Source.Stop();
            }

            mPause = false;
            mStartPlaying = false;
            mStoped = true;
        }

        public void Play()
        {
            if (mPause)
            {
                //从暂停恢复
                mPause = false;
            }
            else
            {
                //重播 or 初次播放
                Stop();
            }

            mStoped = false;
            mStartPlaying = true;
            if (Source.clip)
            {
                Source.Play();
            }
        }

        private void ReleaseCurrent()
        {
            if (Source.clip)
            {
                AssetManager.Release(Source.clip);
            }

            Source.clip = null;
        }


        public string BundleName { get; private set; }

        /// <summary>
        /// 设置加载资源
        /// </summary>
        /// <param name="resName"></param>
        public void SetResource(string resName)
        {
            if (mNextLoader != null)
            {
                mNextLoader.Subscribe(AssetManager.Release);
                mNextLoader = null;
            }

            BundleName = resName;
            mLoadFailed = false;
            if (!string.IsNullOrEmpty(resName))
            {
                mNextLoader = AssetManager.LoadAsset<AudioClip>(BundleName);
                if (mNextLoader.IsDone)
                {
                    TryLoading();
                }
            }
            else
            {
                ReleaseCurrent();
            }
        }

        public int ID { get; private set; }

        private void OnEnable()
        {
            if (ID == 0)
            {
                ID = this.GetInstanceID();
            }

            sEnableAudioHandler.Add(ID, this);
        }

        private void OnDisable()
        {
            sEnableAudioHandler.Remove(ID);
        }

        private void OnDestroy()
        {
            ReleaseCurrent();
        }

        /// <summary>
        /// AudioType自带的属性设置
        /// </summary>
        /// <param name="t"></param>
        private void SetOption(AudioType t)
        {
            //set option
            Source.volume = 1;
            switch (t)
            {
                case AudioType.BGM:
                    Source.spatialBlend = 0;
                    Source.loop = true;
                    break;
                case AudioType.UI:
                    Source.spatialBlend = 0;
                    break;
                case AudioType.Scene:
                case AudioType.Dynamic:
                    Source.spatialBlend = 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("t", t, null);
            }
        }


        private static readonly Cache.ObjectPool<AudioHandler> sAudioHandlerPool = new Cache.ObjectPool<AudioHandler>(20, RemovePoolAudioHandler);

        private static void RemovePoolAudioHandler(AudioHandler arg)
        {
            UnityHelper.Destroy(arg.gameObject);
        }


        //todo xxxxxx 显示AudioHandler的同时播放数量
        internal static AudioHandler GetOrCreate(AudioType t, bool autoUnload)
        {
            var ao = sAudioHandlerPool.Get();
            if (!ao)
            {
                ao = new GameObject(t.ToString()).AddComponent<AudioHandler>();
            }
            else
            {
                ao.name = t.ToString();
            }

            ao.Source.minDistance = MinDistance;
            ao.Source.maxDistance = MaxDistance;
            ao.Type = t;
            ao.SetOption(t);

            ao.mStopUpdate = false;
            ao.AutoUnload = autoUnload;
            ao.transform.SetParent(AudioSourceRoot);
            return ao;
        }
    }
}