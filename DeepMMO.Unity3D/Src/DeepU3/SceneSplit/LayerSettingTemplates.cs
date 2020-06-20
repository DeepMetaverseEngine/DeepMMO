using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepU3.SceneSplit
{
    [Serializable]
    [CreateAssetMenu(fileName = "NewSceneStreamerLayers", menuName = "D3U/Scene Streamer/Layer Templates")]
    public class LayerSettingTemplates : ScriptableObject
    {
        public Vector3Int deloadingOffset = new Vector3Int(0, 0, 0);
        public List<SplitterLayerSetting> layers = new List<SplitterLayerSetting>();
    }
}