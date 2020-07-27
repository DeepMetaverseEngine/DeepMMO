using System;
using System.Collections.Generic;
using DeepU3.Timers;
using UnityEngine;


namespace DeepU3.SceneSplit
{
    public partial class SplitStreamer
    {
        private static readonly List<SplitStreamer> sStreamers = new List<SplitStreamer>();

        public static Transform Mover { get; private set; }

        public static float PositionCheckTime { get; set; }

        private static Timer sUpdateTimer;

        public Transform mover;


        private void Awake()
        {
            sStreamers.Add(this);
            sUpdateTimer?.Resume();
        }

        private void OnDestroy()
        {
            sStreamers.Remove(this);
        }


        public static void SetGlobalMover(Transform player)
        {
            Mover = player;
            if (sUpdateTimer == null)
            {
                sUpdateTimer = Timer.Register(PositionCheckTime, OnUpdateStreamers, isLooped: true);
            }
        }
        private static string sMoverTag;
        
        public static void SetMoverTag(string tag)
        {
            sMoverTag = tag;
            if (sUpdateTimer == null)
            {
                sUpdateTimer = Timer.Register(PositionCheckTime, OnUpdateStreamers, isLooped: true);
            }
        }

        private static void TrgGetTaggedMover()
        {
            if (Mover || string.IsNullOrEmpty(sMoverTag))
            {
                return;
            }

            var o = GameObject.FindWithTag(sMoverTag);
            if (o)
            {
                Mover = o.transform;
            }
        }
        public static void UnloadAll()
        {
            foreach (var streamer in sStreamers)
            {
                streamer.xPos = int.MinValue;
                streamer.yPos = int.MinValue;
                streamer.zPos = int.MinValue;
            }
        }

        private static void OnUpdateStreamers()
        {
            TrgGetTaggedMover();
            if (sStreamers.Count == 0)
            {
                sUpdateTimer.Pause();
                return;
            }
            if (!Mover)
            {
                return;
            }

            foreach (var streamer in sStreamers)
            {
                if (!streamer.isActiveAndEnabled)
                {
                    continue;
                }

                var mover = Mover;
                if (streamer.mover)
                {
                    mover = streamer.mover;
                }

                var pos = mover.position - streamer.transform.root.position;
                if (!streamer.CheckPositionTiles(in pos))
                {
                    continue;
                }

                streamer.SceneLoading();
                streamer.SceneUnloading();
            }
        }
    }
}