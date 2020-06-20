using DeepU3.SceneSplit;
using UnityEditor;
using UnityEngine;

namespace DeepU3.Editor.SceneStreamer
{
    [CustomEditor(typeof(SplitterSettings))]
    public class SplitterSettingsInspectorEditor : UnityEditor.Editor
    {
        private SplitterSettingsEditor _settingsEditor;

        private void OnEnable()
        {
            _settingsEditor = new SplitterSettingsEditor(target as SplitterSettings);
        }


        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
            _settingsEditor.OnGUI();
        }
    }

   
}