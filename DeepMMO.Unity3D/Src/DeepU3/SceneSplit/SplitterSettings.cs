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
        [HideInInspector]
        public LayerSettingTemplates config;

        public enum SplitSteps
        {
            Prepare = 0,
            Split,
            Clean
        }

        public SplitSteps splitStep = SplitSteps.Prepare;
    }
}