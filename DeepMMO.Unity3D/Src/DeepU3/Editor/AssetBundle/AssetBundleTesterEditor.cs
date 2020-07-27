using System.Collections.Generic;
using System.IO;
using DeepU3.AssetBundles;
using UnityEditor;
using UnityEngine;

namespace DeepU3.Editor.AssetBundle
{
    [CustomEditor(typeof(AssetBundleTester))]
    public class AssetBundleTesterEditor : UnityEditor.Editor
    {
        private static Vector2 sScrollPosition = Vector2.zero;

        private Dictionary<string, Object> _cacheObjects = new Dictionary<string, Object>();

        private Object LoadAsset(string path)
        {
            if (!_cacheObjects.TryGetValue(path, out var obj))
            {
                obj = AssetDatabase.LoadMainAssetAtPath(path);
                _cacheObjects.Add(path, obj);
            }

            return obj;
        }

        public override void OnInspectorGUI()
        {
            var myTarget = (AssetBundleTester) target;
            // GUILayout.Space(30);

            if (GUILayout.Button("Unload All"))
            {
                myTarget.Unload(true);
                UnityEngine.AssetBundle.UnloadAllAssetBundles(false);
                Resources.UnloadUnusedAssets();
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("AssetBundle路径"), new GUIContent(Path.GetFileName(myTarget.path)));

            if (GUILayout.Button(new GUIContent("...", "Browse to a new location"), EditorStyles.miniButton, GUILayout.Width(25)))
            {
                var filePath = EditorUtility.OpenFilePanel("加载AssetBundle", "Assets/..", null);
                myTarget.path = filePath;
                myTarget.TryLoad();
            }

            EditorGUILayout.EndHorizontal();

            sScrollPosition = EditorGUILayout.BeginScrollView(sScrollPosition);
            if (myTarget.assets != null)
            {
                for (var i = 0; i < myTarget.assets.Length; i++)
                {
                    var asset = myTarget.assets[i];
                    EditorGUILayout.BeginHorizontal();
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.ObjectField("in bundle", asset, asset.GetType(), false);
                        if (asset is GameObject)
                        {
                            if (GUILayout.Button(new GUIContent("实例化", "Instantiate")))
                            {
                                var obj = Object.Instantiate(asset) as GameObject;
                                var bounds = Utils.TryGuessWorldBounds(obj, Utils.GuessOpt.IncludeChildrenWithoutSelfTransform);
                                var p = new Vector3(bounds.size.x * 0.5f, bounds.size.y, bounds.size.z * 0.5f);
                                obj.transform.position = p;
                                EditorGUIUtility.PingObject(obj);
                            }
                        }

                        EditorGUILayout.EndHorizontal();
                        var assetPath = myTarget.assetPaths[i];
                        var objProject = LoadAsset(assetPath);
                        if (objProject)
                        {
                            EditorGUILayout.ObjectField("in project", objProject, objProject.GetType(), false);
                        }
                        else
                        {
                            EditorGUILayout.LabelField("in project", assetPath);
                        }
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }
}