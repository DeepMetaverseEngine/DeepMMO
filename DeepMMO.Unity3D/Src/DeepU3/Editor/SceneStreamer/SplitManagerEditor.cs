using DeepU3.SceneSplit;
using UnityEditor;
using UnityEngine;

namespace DeepU3.Editor.SceneStreamer
{
    [CustomEditor(typeof(SplitManager))]
    [CanEditMultipleObjects]
    public class SplitManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (!Application.isPlaying)
            {
                if (GUILayout.Button("PrefabTest"))
                {
                    foreach (var t in targets)
                    {
                        var splitManager = (SplitManager) t;
                        var s = splitManager.gameObject.scene;
                        var savePath = s.path.Substring(0, s.path.Length - ".unity".Length);
                        SplitterSettingsEditor.PrefabGenerate(savePath, splitManager);
                    }
                }
            }
            else
            {
                if (GUILayout.Button("Reload"))
                {
                    foreach (var t in targets)
                    {
                        var splitManager = (SplitManager) t;
                        splitManager.ReleaseChildren();
                        splitManager.Init(splitManager.streamer, splitManager.splitData, splitManager.name);
                    }
                }
            }
        }
    }
}