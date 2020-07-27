using System;
using System.Collections.Generic;
using DeepU3.Asset;
using DeepU3.SceneSplit;
using UnityEditor;
using UnityEngine;

namespace DeepU3.Editor.SceneStreamer
{
    [CustomEditor(typeof(SplitStreamer))]
    [CanEditMultipleObjects]
    public class SceneStreamerEditor : UnityEditor.Editor
    {
        private SerializedProperty splitsProperty;
        private SerializedProperty layerSettingProperty;
        private SerializedProperty templateProperty;
        private SerializedProperty xPosProperty;
        private SerializedProperty yPosProperty;
        private SerializedProperty zPosProperty;

        private bool mFoldoutSplits;
        private readonly Dictionary<int, bool> mFoldouts = new Dictionary<int, bool>();

        private void OnEnable()
        {
            layerSettingProperty = serializedObject.FindProperty(nameof(SplitStreamer.layerSetting));
            templateProperty = serializedObject.FindProperty(nameof(SplitStreamer.template));
            splitsProperty = serializedObject.FindProperty(nameof(SplitStreamer.splits));
            xPosProperty = serializedObject.FindProperty(nameof(SplitStreamer.xPos));
            yPosProperty = serializedObject.FindProperty(nameof(SplitStreamer.yPos));
            zPosProperty = serializedObject.FindProperty(nameof(SplitStreamer.zPos));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(layerSettingProperty, true);
            EditorGUILayout.PropertyField(templateProperty, true);
            EditorGUILayout.PropertyField(xPosProperty);
            EditorGUILayout.PropertyField(yPosProperty);
            EditorGUILayout.PropertyField(zPosProperty);

            if (Application.isPlaying || targets.Length > 1)
            {
                EditorGUILayout.PropertyField(splitsProperty, true);
            }
            else
            {
                var streamer = (SplitStreamer) target;
                mFoldoutSplits = EditorGUILayout.Foldout(mFoldoutSplits, new GUIContent("Splits"));
                if (!mFoldoutSplits || streamer.splits == null)
                {
                    return;
                }

                using (new EditorGUI.IndentLevelScope())
                {
                    for (var index = 0; index < streamer.splits.Length; index++)
                    {
                        var s = streamer.splits[index];
                        var splitName = $"{index}:{streamer.name}_{s.posID[0]}_{s.posID[1]}_{s.posID[2]}";
                        mFoldouts.TryGetValue(s.GetHashCode(), out var foldout);
                        foldout = EditorGUILayout.Foldout(foldout, splitName);
                        mFoldouts[s.GetHashCode()] = foldout;

                        if (!foldout)
                        {
                            continue;
                        }

                        using (new EditorGUI.IndentLevelScope())
                        {
                            if (!string.IsNullOrEmpty(s.assetPath))
                            {
                                var o = AssetDatabase.LoadAssetAtPath<GameObject>(s.assetPath);
                                EditorGUILayout.ObjectField(splitName, o, typeof(GameObject), false);
                            }


                            foreach (var part in s.parts)
                            {
                                mFoldouts.TryGetValue(part.GetHashCode(), out var partFoldout);
                                partFoldout = EditorGUILayout.Foldout(partFoldout, part.name);
                                mFoldouts[part.GetHashCode()] = partFoldout;
                                if (!partFoldout)
                                {
                                    continue;
                                }

                                using (new EditorGUI.IndentLevelScope())
                                {
                                    if (!string.IsNullOrEmpty(part.assetPath))
                                    {
                                        var o = AssetDatabase.LoadAssetAtPath<GameObject>(part.assetPath);
                                        EditorGUILayout.ObjectField(part.name, o, typeof(GameObject), false);
                                    }

                                    EditorGUILayout.Vector3Field(nameof(part.position), part.position);
                                    EditorGUILayout.Vector3Field(nameof(part.rotation), part.rotation.eulerAngles);
                                    EditorGUILayout.Vector3Field(nameof(part.scale), part.scale);
                                }
                            }
                        }
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}