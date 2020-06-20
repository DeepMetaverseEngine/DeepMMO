using System.Collections.Generic;
using UnityEngine;

namespace DeepU3.SceneSplit
{
    public class SplitStreamerManager : MonoBehaviour
    {
        public SplitterSettings setting;
        public SplitStreamer[] streamers;

        public string playerTag = "Player";

        /// <summary>
        /// Streamer will wait for player spawn and fill it automatically
        /// </summary>
        [Tooltip("Streamer will wait for player spawn and fill it automatically")]
        public bool spawnedPlayer = true;

        [SerializeField]
        internal Transform playerTransform;

        private static readonly Dictionary<string, SplitStreamerManager> sStreamerManagers = new Dictionary<string, SplitStreamerManager>();
        private string mSceneName;

        private void Awake()
        {
            foreach (var streamer in streamers)
            {
                streamer.manager = this;
            }

            mSceneName = gameObject.scene.name;
            sStreamerManagers.Add(mSceneName, this);
        }

        public static SplitStreamerManager GetStreamerManager(string sceneName)
        {
            sStreamerManagers.TryGetValue(sceneName, out var ret);
            return ret;
        }


        private void OnDestroy()
        {
            sStreamerManagers.Remove(mSceneName);
        }

        private float mPositionCheckPassTime = 0;


        /// <summary>
        /// checks player position
        /// </summary>
        /// <returns>The checker.</returns>
        private void PositionChecker()
        {
            mPositionCheckPassTime += Time.deltaTime;
            if (mPositionCheckPassTime < setting.positionCheckTime)
            {
                return;
            }

            mPositionCheckPassTime = 0;
            CheckPositionTiles();
        }

        public void UnloadAll()
        {
            foreach (var streamer in streamers)
            {
                streamer.xPos = int.MinValue;
                streamer.yPos = int.MinValue;
                streamer.zPos = int.MinValue;
            }
        }

        /// <summary>
        /// Checks the position of player in tiles.
        /// </summary>
        private void CheckPositionTiles()
        {
            if (spawnedPlayer && !playerTransform && !string.IsNullOrEmpty(playerTag))
            {
                var go = GameObject.FindGameObjectWithTag(playerTag);
                if (go != null)
                {
                    playerTransform = go.transform;
                }
            }

            if (!playerTransform)
            {
                UnloadAll();
                return;
            }

            //transform.position即地图偏移值
            var pos = playerTransform.position - transform.position;
            foreach (var streamer in streamers)
            {
                if (streamer.isActiveAndEnabled)
                {
                    streamer.CheckPositionTiles(in pos);
                }
            }
        }

        private void Update()
        {
            PositionChecker();
        }
    }
}