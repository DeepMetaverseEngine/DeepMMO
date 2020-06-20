using System;
using System.Linq;
using UnityEngine;

namespace DeepU3.SceneSplit
{
    /// <summary>
    /// Stores scene splitter settings.
    /// </summary>
    public class SplitterSettings : MonoBehaviour
    {
        public float positionCheckTime = 0.1f;

        public float destroyTileDelay = 2f;

        [HideInInspector]
        public LayerSettingTemplates config;
    }
}