using DeepU3.SceneConnect;
using UnityEditor;
using UnityEngine;

namespace DeepU3.Editor.SceneConnect
{
    [CustomEditor(typeof(ConnectArea))]
    [CanEditMultipleObjects]
    public class ConnectAreaEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            foreach (var o in targets)
            {
                var area = (ConnectArea) o;
                var bounds = new Bounds(area.size * 0.5f, area.size);
                if (!bounds.Contains(area.boundsMin))
                {
                    area.boundsMin = bounds.min;
                }

                if (!bounds.Contains(area.boundsMax))
                {
                    area.boundsMax = bounds.max;
                }
            }
        }
    }
}