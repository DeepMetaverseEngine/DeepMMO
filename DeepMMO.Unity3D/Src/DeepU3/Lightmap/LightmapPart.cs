using System;
using UnityEngine;

namespace DeepU3.Lightmap
{
    [RequireComponent(typeof(Renderer))]
    public class LightmapPart : MonoBehaviour
    {
        public int lightmapIndex;
        public Vector4 lightmapScaleOffset;
        public new Renderer renderer;

        private ILightmapReference _lightmapReference;

        private void Start()
        {
            if (!renderer)
            {
                renderer = GetComponent<Renderer>();
            }

            RecheckLightmap();
        }

        private void OnDisable()
        {
            UnRefLightmap();
        }

        private void OnEnable()
        {
            RecheckLightmap();
        }

        internal void Rebind()
        {
            if (_lightmapReference == null || !_lightmapReference.IsLoaded)
            {
                return;
            }

            var index = _lightmapReference.SceneStartLightmapIndex + lightmapIndex;
            renderer.lightmapIndex = index;
            renderer.lightmapScaleOffset = lightmapScaleOffset;
        }

        private void RecheckLightmap()
        {
            if (!renderer || !isActiveAndEnabled)
            {
                return;
            }

            if (_lightmapReference != null && renderer.lightmapIndex == _lightmapReference.SceneStartLightmapIndex + lightmapIndex)
            {
                return;
            }

            UnRefLightmap();
            _lightmapReference = SceneLightmaps.Ref(this);
            Rebind();
        }

        private void OnDestroy()
        {
            UnRefLightmap();
        }

        private void UnRefLightmap()
        {
            _lightmapReference?.Dispose();
            _lightmapReference = null;
            renderer.lightmapIndex = -1;
        }
    }
}