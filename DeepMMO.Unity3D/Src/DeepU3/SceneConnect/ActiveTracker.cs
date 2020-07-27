using System;
using System.Collections.Generic;
using DeepU3.Asset;
using DeepU3.Timers;
using UnityEngine;

namespace DeepU3.SceneConnect
{
    public class ActiveTracker : MonoBehaviour
    {
        private bool IsActiveScene => SceneConnector.ActiveScenePath == null || SceneConnector.ActiveScenePath == gameObject.scene.path;

        private ConnectArea mBindArea;

        public bool triggerLoadScene = false;

        public string bindScenePath;

        private static Timer sCheckTimer;


        private static readonly HashSet<ActiveTracker> sTrackers = new HashSet<ActiveTracker>();

        private void Awake()
        {
            AssetManager.MarkInstanceDontCache(gameObject);
            sTrackers.Add(this);
            if (sCheckTimer == null)
            {
                sCheckTimer = Timer.Register(int.MaxValue, null, isLooped: true, onUpdate: ResetActive);
            }
            else
            {
                sCheckTimer.Resume();
            }

            var hashCode = ConnectArea.GetConnectHashCode(gameObject.scene.path, bindScenePath);
            ConnectArea.Areas.TryGetValue(hashCode, out mBindArea);
            if (triggerLoadScene && mBindArea)
            {
                SceneConnector.TryLoadScene(mBindArea, this);
            }

            ResetActive(false);
        }

        private void OnDestroy()
        {
            sTrackers.Remove(this);
            if (sTrackers.Count == 0)
            {
                sCheckTimer.Pause();
            }
        }

        private static void ResetActive(float f)
        {
            foreach (var tracker in sTrackers)
            {
                tracker.ResetActive(true);
            }
        }

        private string mScenePath;

        private void OnEnable()
        {
            mScenePath = gameObject.scene.path;
        }

        private void ResetActive(bool checkEnable)
        {
            if (checkEnable && !enabled)
            {
                return;
            }

            //非当前场景且绑定的对应场景未加载
            var active = IsActiveScene;
            if (!active && mBindArea != null)
            {
                if (!mBindArea.IsConnectSceneLoaded)
                {
                    active = true;
                }
                else
                {
                    active = string.Compare(mScenePath, bindScenePath, StringComparison.Ordinal) < 0;
                }
            }

            if (gameObject.activeSelf != active)
            {
                gameObject.SetActive(active);
            }
        }
    }
}