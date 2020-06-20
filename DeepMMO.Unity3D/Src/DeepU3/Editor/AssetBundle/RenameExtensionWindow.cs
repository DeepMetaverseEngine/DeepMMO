using System;
using UnityEditor;
using UnityEngine;

namespace DeepU3.Editor.AssetBundle
{
    public class RenameExtensionWindow : EditorWindow
    {
        internal static void Show(EditorWindow parentWindow, AssetBundleProfile profile, Vector2 position)
        {
            var window = GetWindow<RenameExtensionWindow>();
            window.titleContent = new GUIContent("RenameExtension");
            window.position = new Rect(position, new Vector2(250, 200));
            window.mProfile = profile;
            window.mParent = parentWindow;
            window.Show();
        }

        private AssetBundleProfile mProfile;
        private EditorWindow mParent;
        private void OnGUI()
        {
            GUILayout.Space(50);
            var assetExt = EditorGUILayout.TextField(new GUIContent("Extension"), mProfile.assetExt);
            if (assetExt != mProfile.assetExt)
            {
                assetExt = assetExt.ToLower();
                mProfile.assetExt = assetExt;
                mParent.Repaint();
            }

            if (Event.current.isKey && Event.current.keyCode == KeyCode.Return)
            {
                Close();
            }
        }

        private void OnLostFocus()
        {
            Close();
        }
    }
}