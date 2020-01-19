using UnityEngine;

namespace CoreUnity.Asset
{
    internal class Statistics : MonoBehaviour
    {
        public int destroyed;
        public static Statistics Instance { get; private set; }

        public int destroyedToUnloadUnused;

        private void Awake()
        {
            Instance = this;
        }

        private void LateUpdate()
        {
            if (destroyed > destroyedToUnloadUnused)
            {
                destroyed = 0;
                AssetManager.UnloadUnusedAssets();
            }
        }
    }
}