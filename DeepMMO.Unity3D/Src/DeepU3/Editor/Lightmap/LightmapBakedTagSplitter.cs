using System;
using System.Collections.Generic;
using System.IO;
using DeepU3.Editor.SceneStreamer;
using DeepU3.SceneSplit;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepU3.Editor.Lightmap
{
    public class LightmapBakedTagSplitter : EditorWindow
    {
        private Vector3 _bakedSplitSize = new Vector3(128, 0, 128);
        private string _success;
        private string _outputPath = "Assets/GeneratedGIParameters";

        [MenuItem("DU3/Window/Lightmap BakedTag Splitter", false, -1100)]
        private static void ShowWindow()
        {
            var window = GetWindow<LightmapBakedTagSplitter>();
            window.titleContent = new GUIContent("Lightmap BakedTag Splitter");
            window.Show();
        }

        private void OnEnable()
        {
            if (!Directory.Exists(_outputPath))
            {
                Directory.CreateDirectory(_outputPath);
            }
        }

        private void OnGUI()
        {
            GUILayout.Space(30);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("GI Parameter保存目录"), new GUIContent(_outputPath));

            if (GUILayout.Button(new GUIContent("...", "Browse to a new location to save GI Parameter to"), EditorStyles.miniButton, GUILayout.Width(25)))
            {
                var selectFolder = EditorUtility.SaveFolderPanel("保存目录", "Assets", null);
                selectFolder = EditorUtils.PathToAssetPath(selectFolder);
                if (!string.IsNullOrEmpty(selectFolder))
                {
                    _outputPath = selectFolder;
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            _bakedSplitSize = EditorGUILayout.Vector3Field("块大小", _bakedSplitSize);
            EditorGUILayout.Space();

            //var styleTips = new GUIStyle {normal = {textColor = Color.cyan}, alignment = TextAnchor.UpperLeft};
            //GUILayout.Label("", styleTips);

            if (GUILayout.Button(new GUIContent("生成分块烘培参数")))
            {
                BakeParamGenerate();
            }

            EditorGUILayout.Space();
            if (GUILayout.Button(new GUIContent("还原分块烘培参数")))
            {
                RecoverBakeParam();
            }

            // if (GUILayout.Button(new GUIContent("Collect SceneLightmaps")))
            // {
            //     SceneLightmapsEditor.CollectSceneLightmaps(SceneManager.GetActiveScene().GetRootGameObjects()[0]);
            // }

            EditorGUILayout.Space();
            if (GUILayout.Button(new GUIContent("Lighting Settings")))
            {
                EditorApplication.ExecuteMenuItem("Window/Rendering/Lighting Settings");
            }

            if (!string.IsNullOrEmpty(_success))
            {
                GUILayout.Space(20);
                var textStyle = new GUIStyle {normal = {textColor = Color.green}, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold};
                GUILayout.Label(_success, textStyle);
                GUILayout.Space(20);
            }
        }

        public void SetLightMapBakedTag(Renderer r, int bakedLightmapTag, Dictionary<string, LightmapParameters> tagCache)
        {
            var so = new SerializedObject(r);
            var sp = so.FindProperty("m_LightmapParameters");
            RecoverBakeParam(sp);
            var sourceParam = sp.objectReferenceValue as LightmapParameters;
            if (sourceParam == null)
            {
                return;
            }

            var assetPath = AssetDatabase.GetAssetPath(sourceParam);
            var cacheKey = $"{assetPath}_{bakedLightmapTag}";
            if (!tagCache.TryGetValue(cacheKey, out var targetP))
            {
                // var fileName = Path.GetFileNameWithoutExtension(assetPath);
                var ext = Path.GetExtension(assetPath);

                var newPath = $"{_outputPath}/_bakedTag_{r.gameObject.scene.name}_{bakedLightmapTag}_{AssetDatabase.AssetPathToGUID(assetPath)}{ext}";

                var p = AssetDatabase.LoadAssetAtPath<LightmapParameters>(newPath);
                if (!p)
                {
                    p = Instantiate(sourceParam);
                    p.bakedLightmapTag = bakedLightmapTag;
                    AssetDatabase.CreateAsset(p, newPath);
                }
                else
                {
                    p.resolution = sourceParam.resolution;
                    p.blurRadius = sourceParam.blurRadius;
                    p.clusterResolution = sourceParam.clusterResolution;
                    p.irradianceBudget = sourceParam.irradianceBudget;
                    p.irradianceQuality = sourceParam.irradianceQuality;
                    p.isTransparent = sourceParam.isTransparent;
                    p.modellingTolerance = sourceParam.modellingTolerance;
                    p.stitchEdges = sourceParam.stitchEdges;
                    p.systemTag = sourceParam.systemTag;
                    p.antiAliasingSamples = sourceParam.antiAliasingSamples;
                    p.backFaceTolerance = sourceParam.backFaceTolerance;
                    p.directLightQuality = sourceParam.directLightQuality;
                    p.AOQuality = sourceParam.AOQuality;
                    p.AOAntiAliasingSamples = sourceParam.AOAntiAliasingSamples;

                    p.bakedLightmapTag = bakedLightmapTag;
                }


                tagCache.Add(cacheKey, p);
                // targetP = AssetDatabase.LoadAssetAtPath<LightmapParameters>(newPath);
                targetP = p;
            }

            sp.objectReferenceValue = targetP;
            so.ApplyModifiedProperties();
        }

        private void BakeParamGenerate()
        {
            var renderers = FindObjectsOfType<Renderer>();
            var cache = new Dictionary<string, LightmapParameters>();
            var count = 0;
            foreach (var r in renderers)
            {
                var isStatic = (GameObjectUtility.GetStaticEditorFlags(r.gameObject) & StaticEditorFlags.LightmapStatic) != 0;
                if (!isStatic)
                {
                    continue;
                }

                Vector3 pos;
                if (r is ParticleSystemRenderer p)
                {
                    pos = r.transform.position;
                }
                else
                {
                    pos = r.bounds.center;
                }

                var posId = SplitUtils.GetID(pos, _bakedSplitSize);
                var tag = GetSplitHashCode(posId[0], posId[1], posId[2]);
                SetLightMapBakedTag(r, tag.GetHashCode(), cache);
                count++;
            }

            _success = $"renders: {count}  create giparams: {cache.Count}";
        }

        private int GetSplitHashCode(int x, int y, int z)
        {
            return SplitUtils.GetSplitHashCode(x, y, z);
        }

        private void RecoverBakeParam(Renderer r)
        {
            var so = new SerializedObject(r);
            var sp = so.FindProperty("m_LightmapParameters");
            RecoverBakeParam(sp);
            so.ApplyModifiedProperties();
        }

        private void RecoverBakeParam(SerializedProperty sp)
        {
            var sourceParam = sp.objectReferenceValue as LightmapParameters;
            if (sourceParam != null && sourceParam.name.Contains("_bakedTag"))
            {
                var assetPath = AssetDatabase.GetAssetPath(sourceParam);
                var arr = Path.GetFileNameWithoutExtension(assetPath).Split('_');
                var oldGUID = arr[arr.Length - 1];
                var newPath = AssetDatabase.GUIDToAssetPath(oldGUID);
                var newObj = AssetDatabase.LoadAssetAtPath<LightmapParameters>(newPath);
                if (newObj)
                {
                    sp.objectReferenceValue = newObj;
                }
            }
        }

        private void RecoverBakeParam()
        {
            var renderers = FindObjectsOfType<Renderer>();
            foreach (var r in renderers)
            {
                var isStatic = (GameObjectUtility.GetStaticEditorFlags(r.gameObject) & StaticEditorFlags.LightmapStatic) != 0;
                if (isStatic)
                {
                    // Undo.RecordObject(r, r.name);
                    RecoverBakeParam(r);
                }
            }

            var s = SceneManager.GetActiveScene();
            var allGenAssets = AssetDatabase.FindAssets($"t:LightmapParameters _bakedTag_{s.name}");
            foreach (var genAsset in allGenAssets)
            {
                var path = AssetDatabase.GUIDToAssetPath(genAsset);
                AssetDatabase.DeleteAsset(path);
            }

            _success = $"renders: {renderers.Length}  delete giparams: {allGenAssets.Length}";
        }
    }
}