using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace CoreUnity.Asset
{
    public class AssetManagerDebug
    {
        private GameObject mDebugGameObject;

        public AssetManagerDebug(GameObject debugGameObject)
        {
            Instance = this;
            mDebugGameObject = debugGameObject;
#if UNITY_EDITOR
            Editor.finishedDefaultHeaderGUI += OnFinishedDefaultHeaderGui;
#endif
        }

        public static AssetManagerDebug Instance { get; private set; }

#if UNITY_EDITOR
        private void OnFinishedDefaultHeaderGui(Editor obj)
        {
            if (obj.target != mDebugGameObject)
            {
                return;
            }

            GUI.skin.label.richText = true;
            EditorStyles.label.richText = true;
            EditorStyles.textField.wordWrap = true;
            EditorStyles.foldout.richText = true;

            EditorGUILayout.Separator();

            GUILayout.BeginHorizontal();

            var txt = $"GameObject缓存:{AssetManager.GameObjectPool.Capacity}/<color=#ffff00ff>{AssetManager.GameObjectPool.Count}</color>";
            AssetManager.GameObjectPool.Capacity = EditorGUILayout.IntField(txt, AssetManager.GameObjectPool.Capacity);

            if (GUILayout.Button("清除GameObject缓存", GUILayout.MaxWidth(200)))
            {
                AssetManager.GameObjectPool.Clear();
            }

            GUILayout.EndHorizontal();

            EditorGUILayout.Separator();

            if (AssetManager.BundlePool != null)
            {
                GUILayout.BeginHorizontal();
                txt = $"Bundle缓存:{AssetManager.BundlePool.Capacity}/ <color=#ffff00ff>{AssetManager.BundlePool.Count}</color>";
                AssetManager.BundlePool.Capacity = EditorGUILayout.IntField(txt, AssetManager.BundlePool.Capacity);

                if (GUILayout.Button("清除Bundle缓存", GUILayout.MaxWidth(200)))
                {
                    AssetManager.BundlePool.Clear();
                }

                GUILayout.EndHorizontal();
                EditorGUILayout.Separator();
            }

            // GUILayout.EndHorizontal();
            if (GUILayout.Button("UnloadUnusedAssets", GUILayout.MaxWidth(200)))
            {
                AssetManager.UnloadUnusedAssets();
            }

            EditorGUILayout.Separator();
        }
#endif
    }
}