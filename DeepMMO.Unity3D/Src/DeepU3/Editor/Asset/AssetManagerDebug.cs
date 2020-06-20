using System.Collections.Generic;
using DeepU3.Asset;
using UnityEditor;
using UnityEngine;

namespace DeepU3.Editor.Asset
{
    [CustomEditor(typeof(Statistics))]
    public class AssetManagerDebug : UnityEditor.Editor
    {
        private static Vector2 sScrollPosition = Vector2.zero;

        // private bool[] mFoldOuts;
        private Statistics mTarget;

        private void OnEnable()
        {
            mTarget = (Statistics) target;
        }

        private bool mLoadingFoldout;
        private bool mLoadingBundlesFoldout;

        private bool mLoadingSceneFoldout;

        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();

            GUI.skin.label.richText = true;
            EditorStyles.label.richText = true;
            EditorStyles.textField.wordWrap = true;
            EditorStyles.foldout.richText = true;

            EditorGUILayout.IntField($"destroyedToUnloadUnused(destroyed:<color=#ffff00ff>{mTarget.destroyed}</color>)", mTarget.destroyedToUnloadUnused);
            EditorGUILayout.Separator();

            GUILayout.BeginHorizontal();

            var txt = $"GameObject缓存: {AssetManager.GameObjectPool.Capacity}/<color=#ffff00ff>{AssetManager.GameObjectPool.Count}</color>";
            AssetManager.GameObjectPool.Capacity = EditorGUILayout.IntField(txt, AssetManager.GameObjectPool.Capacity);

            if (GUILayout.Button("清除GameObject缓存", GUILayout.MaxWidth(200)))
            {
                AssetManager.GameObjectPool.Clear();
            }

            GUILayout.EndHorizontal();

            EditorGUILayout.Separator();

            var txtLoading = $"正在加载中Assets: <color=#ffff00ff>{AssetManager.LoadingAssetCount}</color>";
            // GUILayout.Label(txtLoading);

            mLoadingFoldout = EditorGUILayout.Foldout(mLoadingFoldout, new GUIContent(txtLoading));
            if (mLoadingFoldout && AssetManager.LoadingAssets != null)
            {
                foreach (var asset in AssetManager.LoadingAssets)
                {
                    EditorGUILayout.LabelField($"{asset.Address}#{asset.Key}");
                }

                EditorGUILayout.Space();
            }

            var loadingBundles = Statistics.Instance.LoadingBundles();
            var txtBundleLoading = $"正在加载中Bundles: <color=#ffff00ff>{loadingBundles.Length}</color>";
            // GUILayout.Label(txtLoading);
            mLoadingBundlesFoldout = EditorGUILayout.Foldout(mLoadingBundlesFoldout, new GUIContent(txtBundleLoading));
            if (mLoadingBundlesFoldout)
            {
                foreach (var bundle in loadingBundles)
                {
                    DrawBundleNode(bundle);
                }

                EditorGUILayout.Space();
            }

            mLoadingSceneFoldout = EditorGUILayout.Foldout(mLoadingSceneFoldout, "正在加载中场景");
            if (mLoadingSceneFoldout)
            {
                foreach (var s in AssetManager.LoadingScenes)
                {
                    EditorGUILayout.LabelField($"{s}");
                }

                EditorGUILayout.Space();
            }

            // GUILayout.EndHorizontal();
            if (GUILayout.Button("UnloadUnusedAssets", GUILayout.MaxWidth(200)))
            {
                AssetManager.UnloadUnusedAssets();
            }


            EditorGUILayout.Separator();


            sScrollPosition = EditorGUILayout.BeginScrollView(sScrollPosition);

            GUILayout.BeginHorizontal();
            mLastAssetPart = EditorGUILayout.TextField(mLastAssetPart, GUILayout.MaxWidth(200));
            if (GUILayout.Button("查找已加载Asset", GUILayout.MaxWidth(200)))
            {
                mSearchAssets = Statistics.Instance.SearchAssets(mLastAssetPart);
                mAssetFoldout = true;
            }

            GUILayout.EndHorizontal();

            mAssetFoldout = EditorGUILayout.Foldout(mAssetFoldout, "Asset查找结果");
            if (mAssetFoldout && mSearchAssets != null)
            {
                foreach (var m in mSearchAssets)
                {
                    DrawAssetNode(m);
                }
            }

            EditorGUILayout.Separator();

            GUILayout.BeginHorizontal();
            mLastBundlePart = EditorGUILayout.TextField(mLastBundlePart, GUILayout.MaxWidth(200));
            if (GUILayout.Button("查找已加载Bundle", GUILayout.MaxWidth(200)))
            {
                mSearchBundles = Statistics.Instance.SearchBundles(mLastBundlePart);
                mBundleFoldout = true;
            }

            GUILayout.EndHorizontal();

            mBundleFoldout = EditorGUILayout.Foldout(mBundleFoldout, "Bundle查找结果");
            if (mBundleFoldout && mSearchBundles != null)
            {
                foreach (var m in mSearchBundles)
                {
                    DrawBundleNode(m);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private Dictionary<Statistics.BundleNode, bool> mFoldOutBundles = new Dictionary<Statistics.BundleNode, bool>();
        private Dictionary<Statistics.AssetNode, bool> mFoldOutAssets = new Dictionary<Statistics.AssetNode, bool>();

        private void DrawBundleNode(Statistics.BundleNode m, bool useFoldOut = true, Statistics.AssetNode exceptNode = null)
        {
            var foldOut = !useFoldOut;
            if (useFoldOut)
            {
                mFoldOutBundles.TryGetValue(m, out foldOut);
                foldOut = EditorGUILayout.Foldout(foldOut, m.Name);
                mFoldOutBundles[m] = foldOut;
            }

            if (foldOut)
            {
                GUILayout.BeginHorizontal();
                if (!useFoldOut)
                {
                    EditorGUILayout.LabelField($"{m.Name}");
                }

                if (GUILayout.Button("LoadStackTrace", GUILayout.MaxWidth(200)))
                {
                    Debug.Log($"<color='olive'>[{m.Name}] - {m.LoadTrace}</color>");
                }

                if (m.IsLoaded)
                {
                    EditorGUILayout.LabelField($"Loading time : {m.LoadingMS}ms");
                }

                GUILayout.EndHorizontal();
                // EditorGUILayout.Separator();
                foreach (var assetNode in m.Assets)
                {
                    if (exceptNode != assetNode)
                    {
                        DrawAssetNode(assetNode, false, false);
                    }
                }
            }
        }

        private void DrawAssetNode(Statistics.AssetNode m, bool drawBundle = true, bool useFoldOut = true)
        {
            var foldOut = !useFoldOut;
            if (useFoldOut)
            {
                mFoldOutAssets.TryGetValue(m, out foldOut);
                foldOut = EditorGUILayout.Foldout(foldOut, m.Asset.name);
                mFoldOutAssets[m] = foldOut;
            }

            if (foldOut)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(m.Asset, typeof(UnityEngine.Object), false);
                if (GUILayout.Button("LoadStackTrace", GUILayout.MaxWidth(200)))
                {
                    Debug.Log($"<color='olive'>[{m.Asset}] - {m.LoadTrace}</color>");
                }

                GUILayout.EndHorizontal();

                if (drawBundle)
                {
                    DrawBundleNode(m.Bundle, false, m);
                }
            }
        }

        private string mLastAssetPart;
        private string mLastBundlePart;
        private bool mAssetFoldout;
        private bool mBundleFoldout;
        private Statistics.AssetNode[] mSearchAssets;
        private Statistics.BundleNode[] mSearchBundles;
    }
}
