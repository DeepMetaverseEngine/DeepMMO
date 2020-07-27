using System;
using System.Collections.Generic;
using DeepU3.SceneSplit;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepU3.Editor.SceneStreamer
{
    public class SceneStreamerWindow : EditorWindow
    {
        [MenuItem("DU3/Window/Scene Streamer", false, -1100)]
        private static void ShowWindow()
        {
            var window = GetWindow<SceneStreamerWindow>();
            window.titleContent = new GUIContent("Scene Streamer");
            window.Show();
        }


        [MenuItem("Component/DU3/Scene Streamer/SmallestSplit")]
        static void SetAsSmallestSplitGameObject()
        {
            EditorUtils.TryAddComponent<SmallestSplit>(false, Selection.gameObjects);
        }

        [MenuItem("Component/DU3/Scene Streamer/IgnoredSplit")]
        static void CancelSmallestSplitGameObject()
        {
            EditorUtils.TryAddComponent<IgnoredSplit>(false, Selection.gameObjects);
        }

        [MenuItem("Component/DU3/Scene Streamer/SplitBoundsGizmos")]
        static void SplitBoundsGizmos()
        {
            EditorUtils.TryAddComponent<SplitBoundsGizmos>(false, Selection.gameObjects);
        }

        // private static Vector2 sScrollPosition = Vector2.zero;

        private SplitterSettingsEditor _settingsEditor;

        private LayerSettingTemplates _layerSettingTemplates;


        private void OnEnable()
        {
            var s = SceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(s.path))
            {
                _settingsEditor = new SplitterSettingsEditor(SceneManager.GetActiveScene());
            }
            
        }

        private void OnGUI()
        {
            if (Application.isPlaying || _settingsEditor == null)
            {
                return;
            }

            if (_settingsEditor.Scene != SceneManager.GetActiveScene())
            {
                _settingsEditor = new SplitterSettingsEditor(SceneManager.GetActiveScene());
            }

            GUILayout.Space(30);
            var style = new GUIStyle {normal = {textColor = Color.white}, fontSize = 16, border = new RectOffset(2, 2, 2, 2)};
            EditorGUILayout.LabelField(new GUIContent(_settingsEditor.Scene.path), style);
            _settingsEditor.OnGUI();
        }
    }
}