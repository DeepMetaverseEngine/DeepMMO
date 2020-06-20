using System;
using UnityEngine;

namespace DeepU3.SceneSplit
{
    [ExecuteInEditMode]
    public class SplitBoundsGizmos : MonoBehaviour
    {
        public Bounds worldBounds;

        public Vector3 splitSize;
        public int[] posId;

        public float scaleBounds = 0.8f;
        public bool showSplitGrid = true;
        private Vector3 mLastSplitSize = Vector3.zero;


        private void Start()
        {
        }

        private void Awake()
        {
            worldBounds = Utils.TryGuessWorldBounds(gameObject, Utils.GuessOpt.IncludeChildren);
        }

        private void OnDisable()
        {
            mLastSplitSize = Vector3.zero;
        }


        private void OnDrawGizmosSelected()
        {
            if (!enabled)
            {
                return;
            }

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(worldBounds.center, worldBounds.size);

            var c = Color.green;
            c.a = 0.7f;
            Gizmos.color = c;
            Gizmos.DrawWireCube(worldBounds.center, worldBounds.size * 0.8f);

            if (!splitSize.Equals(mLastSplitSize))
            {
                mLastSplitSize = splitSize;
                var bounds = worldBounds;
                bounds.size *= scaleBounds;
                posId = SplitUtils.GetID(in splitSize, in bounds);
            }

            if (posId == null || !showSplitGrid)
            {
                return;
            }

            var s = splitSize;
            if (Math.Abs(s.y) < 0.0001)
            {
                s.y = 100;
            }

            var posRoot = gameObject.scene.GetRootGameObjects()[0].transform.position;
            Gizmos.color = Color.cyan;
            for (var i = 3; i < posId.Length; i += 3)
            {
                var position = new Vector3(posId[i] * s.x, posId[i + 1] * s.y, posId[i + 2] * s.z) + posRoot;
                Gizmos.DrawWireCube(position + s * 0.5f, s);
            }

            if (posId.Length > 0)
            {
                Gizmos.color = Color.green;
                var positionFirst = new Vector3(posId[0] * s.x, posId[1] * s.y, posId[2] * s.z) + posRoot;
                Gizmos.DrawWireCube(positionFirst + s * 0.5f, s);
            }
        }
    }
}