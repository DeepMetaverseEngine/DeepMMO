using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoreUnity.Audio
{
    public class AudioManager
    {
        public static AudioManager Instance { get; private set; }

        private readonly Dictionary<string, string> mSoundKeys = new Dictionary<string, string>();

        static AudioManager()
        {
            Instance = new AudioManager();
        }

        private AudioHandler mBgm;

        private AudioHandler BGM
        {
            get
            {
                if (!mBgm)
                {
                    mBgm = AudioHandler.GetOrCreate(AudioHandler.AudioType.BGM, false);
                }

                return mBgm;
            }
        }

        private readonly Stack<string> mBgmResoucePath = new Stack<string>();

        private float mDefaultBGMVolume = 1f;
        private float mDefaultSoundVolume = 1f;

        public float DefaultBGMVolume
        {
            get { return mDefaultBGMVolume; }
            set
            {
                mDefaultBGMVolume = value;
                BGM.Volume = value;
            }
        }

        public float DefaultSoundVolume
        {
            get { return mDefaultSoundVolume; }
            set
            {
                mDefaultSoundVolume = value;
                SetSoundVolume(value);
            }
        }


        private string FixBundleName(string bundleName)
        {
            if (!string.IsNullOrEmpty(bundleName))
            {
                return bundleName.ToLower();
            }

            return null;
        }

        private void PlayStackBGM(bool stopNow)
        {
            BGM.SetResource(FixBundleName(mBgmResoucePath.Peek()));
            if (stopNow)
            {
                BGM.Play();
            }
            else if (!BGM.IsPlaying)
            {
                BGM.Play();
            }
        }

        public void PlayBGM(string bundleName, float volume)
        {
            mBgmResoucePath.Clear();
            mBgmResoucePath.Push(bundleName);
            BGM.Volume = volume;
            PlayStackBGM(true);
        }

        public void PlayBGM(string bundleName)
        {
            PlayBGM(bundleName, DefaultBGMVolume);
        }

        public void PushBGM(string bundleName)
        {
            mBgmResoucePath.Push(bundleName);
            PlayStackBGM(false);
        }

        public void PopBGM()
        {
            mBgmResoucePath.Pop();
            PlayStackBGM(false);
        }

        public string GetCurrentBGMBundleName()
        {
            return BGM.BundleName;
        }

        //单独修改音量保留原始音量专用（临时修改音量用）
        public void SetCurrentBGMVol(float vol)
        {
            if (BGM != null)
            {
                BGM.Volume = vol;
            }
        }

        /// <summary>
        /// 修改音效资源，可以实现无缝切换效果
        /// </summary>
        /// <param name="bundleName"></param>
        public void ChangeBGM(string bundleName)
        {
            if (mBgmResoucePath.Count > 0)
            {
                mBgmResoucePath.Pop();
            }

            mBgmResoucePath.Push(bundleName);
            PlayStackBGM(false);
        }

        public void PauseBGM()
        {
            if (BGM.IsPlaying)
            {
                BGM.Pause();
            }
        }

        public void ResumeBGM()
        {
            if (!BGM.IsPlaying)
            {
                BGM.Play();
            }
        }

        public void StopBGM()
        {
            BGM.Stop();
        }

        public void AddSoundKey(string key, string bundleName)
        {
            mSoundKeys[key] = FixBundleName(bundleName);
        }

        public int PlaySoundByKey(string soundKey, bool loop = false)
        {
            if (string.IsNullOrEmpty(soundKey))
            {
                return 0;
            }

            return mSoundKeys.TryGetValue(soundKey, out var bundleName) ? PlaySound(bundleName, loop) : 0;
        }

        public int PlaySound(string bundleName, bool loop = false)
        {
            return PlaySound(bundleName, DefaultSoundVolume, loop);
        }

        public int PlaySound(string bundleName, float volume, bool loop = false)
        {
            return PlaySound(bundleName, 0, volume, loop);
        }

        public int PlaySound(string bundleName, float duration, Vector3 pos, bool loop = false)
        {
            return PlaySound(bundleName, duration, pos, DefaultSoundVolume, loop);
        }

        public int PlaySound(string bundleName, float duration, Transform parent, bool loop = false)
        {
            return PlaySound(bundleName, duration, parent, DefaultSoundVolume, loop);
        }

        public int PlaySound(string bundleName, Vector3 pos, bool loop = false)
        {
            return PlaySound(bundleName, 0, pos, DefaultSoundVolume, loop);
        }

        public int PlaySound(string bundleName, Transform parent, bool loop = false)
        {
            return PlaySound(bundleName, 0, parent, DefaultSoundVolume, loop);
        }

        public int PlaySound(string bundleName, Vector3 pos, float volume, bool loop = false)
        {
            return PlaySound(bundleName, 0, pos, DefaultSoundVolume, loop);
        }

        public int PlaySound(string bundleName, Transform parent, float volume, bool loop = false)
        {
            return PlaySound(bundleName, 0, parent, volume, loop);
        }

        /// <summary>
        /// 播放2d音效
        /// </summary>
        /// <param name="bundleName"></param>
        /// <param name="duration"></param>
        /// <param name="volume"></param>
        /// <param name="loop"></param>
        /// <returns></returns>
        public int PlaySound(string bundleName, float duration, float volume, bool loop = false)
        {
            var aa = AudioHandler.GetOrCreate(AudioHandler.AudioType.UI, true);
            aa.SetResource(FixBundleName(bundleName));
            aa.Volume = volume;
            aa.SpatialBlend = 0;
            aa.Duration = duration;
            aa.Loop = loop;
            aa.Play();
            return aa.GetInstanceID();
        }

        private AudioHandler CreateAssetAudio(string bundleName, float duration, float volume, bool loop = false, AudioHandler.AudioType audioType = AudioHandler.AudioType.Dynamic)
        {
            var aa = AudioHandler.GetOrCreate(audioType, true);
            aa.SetResource(FixBundleName(bundleName));
            aa.Duration = duration;
            aa.Volume = volume;
            aa.Loop = loop;
            aa.SetSourceDistance();
            return aa;
        }

        /// <summary>
        /// 指定音源位置播放音效，Vector.zero播放2d音效
        /// </summary>
        /// <param name="bundleName"></param>
        /// <param name="duration"></param>
        /// <param name="pos"></param>
        /// <param name="volume"></param>
        /// <param name="loop"></param>
        /// <returns></returns>
        public int PlaySound(string bundleName, float duration, Vector3 pos, float volume, bool loop = false)
        {
            if (pos.Equals(Vector3.zero))
            {
                return PlaySound(bundleName, duration, volume);
            }

            var aa = CreateAssetAudio(bundleName, duration, volume, loop);
            aa.transform.position = pos;
            aa.Play();
            return aa.GetInstanceID();
        }


        /// <summary>
        /// 通知指定音源父节点播放音效，parent为空播放2d音效
        /// </summary>
        /// <param name="bundleName"></param>
        /// <param name="duration"></param>
        /// <param name="parent"></param>
        /// <param name="volume"></param>
        /// <param name="loop"></param>
        /// <returns></returns>
        public int PlaySound(string bundleName, float duration, Transform parent, float volume, bool loop = false)
        {
            if (!parent)
            {
                return PlaySound(bundleName, duration, volume);
            }

            var aa = CreateAssetAudio(bundleName, duration, volume, loop);
            aa.transform.SetParent(parent);
            aa.transform.localPosition = Vector3.zero;
            aa.Play();
            return aa.GetInstanceID();
        }


        /// <summary>
        /// 通知指定音源父节点播放音效，parent为空播放2d音效
        /// </summary>
        /// <param name="bundleName"></param>
        /// <param name="duration"></param>
        /// <param name="parent"></param>
        /// <param name="volume"></param>
        /// <param name="mindistance"></param>
        /// <param name="maxdistance"></param>
        /// <param name="loop"></param>
        /// <returns></returns>
        public int PlaySound(string bundleName, float duration, Transform parent, float volume, float mindistance, float maxdistance, bool loop = false, AudioHandler.AudioType audiotype = AudioHandler.AudioType.Dynamic)
        {
            if (!parent)
            {
                return PlaySound(bundleName, duration, volume);
            }

            var aa = CreateAssetAudio(bundleName, duration, volume, loop, audiotype);
            aa.transform.SetParent(parent);
            aa.transform.localPosition = Vector3.zero;
            aa.SetSourceDistance(mindistance, maxdistance);
            aa.Play();
            return aa.GetInstanceID();
        }


        /// <summary>
        /// 停止播放音效
        /// </summary>
        /// <param name="instanceID"></param>
        public void StopSound(int instanceID)
        {
            var aa = AudioHandler.GetAudioHandler(instanceID);
            if (aa)
            {
                aa.Stop();
            }
        }

        public void PauseSound(int instanceID)
        {
            var aa = AudioHandler.GetAudioHandler(instanceID);
            if (aa)
            {
                aa.Pause();
            }
        }

        public void ResumeSound(int instanceID)
        {
            var aa = AudioHandler.GetAudioHandler(instanceID);
            if (aa && !aa.IsPlaying)
            {
                aa.Play();
            }
        }

        public void Pause()
        {
            //AudioHandler.StopAll(AudioHandler.AudioType.UI | AudioHandler.AudioType.Dynamic);
            ////todo 场景内部音效
            //var srcs = (AudioSource[])Object.FindObjectsOfType(typeof(AudioSource));
            //foreach (var s in srcs)
            //{

            //}
            throw new NotImplementedException();
        }

        public void Resume()
        {
            throw new NotImplementedException();
        }

        public void StopAllSound()
        {
            AudioHandler.StopAll(AudioHandler.AudioType.UI | AudioHandler.AudioType.Dynamic);
        }

        public void SetSoundVolume(float value)
        {
            AudioHandler.SetSoundVolume(AudioHandler.AudioType.UI | AudioHandler.AudioType.Dynamic | AudioHandler.AudioType.Scene, value);
        }

        /// <summary>
        /// 修改音效资源，可以实现无缝切换效果
        /// </summary>
        /// <param name="instanceID"></param>
        /// <param name="bundleName"></param>
        public void ChangeSoundResource(int instanceID, string bundleName)
        {
            var aa = AudioHandler.GetAudioHandler(instanceID);
            if (!aa)
            {
                return;
            }

            aa.SetResource(FixBundleName(bundleName));
            if (!aa.IsPlaying)
            {
                aa.Play();
            }
        }
    }
}