using System;
using System.Linq;
using UnityEngine;

namespace DeepU3.SceneSplit
{
    [Serializable]
    public class SplitterLayerSetting
    {
        public string name;

        public string[] gameObjectPrefix;

        public Vector3Int splitSize = new Vector3Int(32, 32, 32);

        public float scaleBounds = 0.7f;
        
        public Color color;

        public Vector3Int loadingRange = new Vector3Int(3, 3, 3);

        public bool IsMatchLayer(string goName)
        {
            return gameObjectPrefix.Any(m => goName.StartsWith(m, StringComparison.OrdinalIgnoreCase));
        }

        public override string ToString()
        {
            return name;
        }
    }
}