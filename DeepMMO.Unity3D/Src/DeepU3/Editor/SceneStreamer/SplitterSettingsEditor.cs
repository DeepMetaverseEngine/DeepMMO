using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeepU3.Editor.AssetBundle;
using DeepU3.Lightmap;
using DeepU3.SceneSplit;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace DeepU3.Editor.SceneStreamer
{
    public class SplitterSettingsEditor
    {
        public SplitterSettings SceneSplitterSettings { get; private set; }
        public Scene Scene { get; private set; }
        private readonly string _sceneDependSavePath;

        private LayerSettingTemplates _layerSettingTemplates;


        private string _warning;
        private string _success;

        private void ClearMessage()
        {
            _warning = null;
            _success = null;
        }

        [MenuItem("GameObject/DU3/Scene Streamer/SplitterSettings", false, -1000)]
        static void AddSplitterSettings()
        {
            var settings = EditorUtils.FindSceneObjectOfType<SplitterSettings>(SceneManager.GetActiveScene());
            if (!settings)
            {
                var o = new GameObject("SplitterSettings");
                Undo.AddComponent<SplitterSettings>(o);
            }
            else
            {
                Selection.activeGameObject = settings.gameObject;
            }
        }


        public SplitterSettingsEditor(SplitterSettings splitterSettings)
        {
            if (!splitterSettings)
            {
                throw new ArgumentNullException();
            }

            SceneSplitterSettings = splitterSettings;
            Scene = SceneSplitterSettings.gameObject.scene;
            _sceneDependSavePath = Scene.path.Substring(0, Scene.path.Length - ".unity".Length);
            if (SceneSplitterSettings)
            {
                ResetLayers();
            }
        }

        public SplitterSettingsEditor(Scene scene)
        {
            Scene = scene;
            _sceneDependSavePath = scene.path.Substring(0, scene.path.Length - ".unity".Length);
            SceneSplitterSettings = EditorUtils.FindSceneObjectOfType<SplitterSettings>(scene);
            if (SceneSplitterSettings)
            {
                ResetLayers();
            }
            else
            {
                TryLoadLastLayerTemplate();
            }
        }


        private void EnsureLightmaps()
        {
            EditorApplication.ExecuteMenuItem("DU3/Collect Baked Lightmaps");
        }

        private void EnsureSplitterSetting()
        {
            if (!SceneSplitterSettings)
            {
                var o = new GameObject("SplitterSettings");
                if (o.scene != Scene)
                {
                    SceneManager.MoveGameObjectToScene(o, Scene);
                }

                SceneSplitterSettings = o.AddComponent<SplitterSettings>();
                Undo.RegisterCreatedObjectUndo(o, "SplitterSettings");
                ResetLayers();
            }
        }

        public void ResetLayers(LayerSettingTemplates layerSettingTemplates)
        {
            if (!layerSettingTemplates)
            {
                return;
            }

            SceneSplitterSettings.config = layerSettingTemplates;
        }


        private void ResetLayers()
        {
            if (!SceneSplitterSettings)
            {
                return;
            }

            if (!_layerSettingTemplates && SceneSplitterSettings.config)
            {
                _layerSettingTemplates = SceneSplitterSettings.config;
            }

            ResetLayers(_layerSettingTemplates);
        }

        private void TryLoadLastLayerTemplate()
        {
            if (!_layerSettingTemplates && !SceneSplitterSettings)
            {
                var lastPath = EditorPrefs.GetString($"{nameof(SplitterSettingsEditor)}_LayerTemplate");
                if (!string.IsNullOrEmpty(lastPath))
                {
                    _layerSettingTemplates = AssetDatabase.LoadAssetAtPath<LayerSettingTemplates>(lastPath);
                }
            }
        }

        public void BuildPipeline()
        {
            Assert.IsTrue(_layerSettingTemplates);
            CollectionLayerObjects();
            SplitScene();
            PrefabGenerate();
            CleanScene();
        }

        public void OnGUI()
        {
            GUILayout.Space(30);
            
            var disableCollect = !_layerSettingTemplates || (SceneSplitterSettings && SceneSplitterSettings.splitStep != SplitterSettings.SplitSteps.Prepare);
            var disableSplit = !SceneSplitterSettings || SceneSplitterSettings.splitStep != SplitterSettings.SplitSteps.Prepare || (SceneSplitterSettings && FindSceneObjectsOfType<SplitStreamer>().Length != SceneSplitterSettings.config.layers.Count);
            var disablePrefab = !SceneSplitterSettings || SceneSplitterSettings.splitStep != SplitterSettings.SplitSteps.Split;

            using (new EditorGUI.DisabledScope(SceneSplitterSettings && SceneSplitterSettings.splitStep > 0))
            {
                var nextSettingTemplates = EditorGUILayout.ObjectField(new GUIContent("Layer Templates"), _layerSettingTemplates, typeof(LayerSettingTemplates), false) as LayerSettingTemplates;

                if (nextSettingTemplates != _layerSettingTemplates)
                {
                    _layerSettingTemplates = nextSettingTemplates;
                    EditorPrefs.SetString($"{nameof(SplitterSettingsEditor)}_LayerTemplate", AssetDatabase.GetAssetPath(_layerSettingTemplates));

                    ResetLayers();
                }
            }


            EditorUtils.DrawGUILayoutHeader(new GUIContent("分块场景制作"));


            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(disableCollect))
            {
                if (GUILayout.Button(new GUIContent("01 - 收集各层GameObject")))
                {
                    CollectionLayerObjects();
                }
            }

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(disableSplit))
            {
                if (GUILayout.Button(new GUIContent("02 - 按块切割")))
                {
                    SplitScene();
                    // CleanEmptyGameObject();
                }
            }

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(disablePrefab))
            {
                if (GUILayout.Button(new GUIContent("03 - 生成Prefab和分块场景")))
                {
                    PrefabGenerate();
                    CleanScene();
                }
            }

            if (!string.IsNullOrEmpty(_warning))
            {
                GUILayout.Space(20);
                var textStyle = new GUIStyle {normal = {textColor = Color.red}, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold};
                GUILayout.Label(_warning, textStyle);
                GUILayout.Space(20);
            }
            else if (!string.IsNullOrEmpty(_success))
            {
                GUILayout.Space(20);
                var textStyle = new GUIStyle {normal = {textColor = Color.green}, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold};
                GUILayout.Label(_success, textStyle);
                GUILayout.Space(20);
            }
        }

        private static int GameObjectCompare(GameObject x, GameObject y)
        {
            if (x.transform.IsChildOf(y.transform))
            {
                return 1;
            }

            var xDepth = EditorUtils.GetHierarchyDepth(x);
            var yDepth = EditorUtils.GetHierarchyDepth(y);

            if (xDepth != yDepth)
            {
                return xDepth.CompareTo(yDepth);
            }

            return string.Compare(x.name, y.name, StringComparison.Ordinal);
        }


        private bool IsIgnoreObject(GameObject go)
        {
            var ignoreChildren = go.GetComponentInParent<SmallestSplit>();
            if (ignoreChildren && ignoreChildren.gameObject != go)
            {
                return true;
            }

            if (go.GetComponentInParent<IgnoredSplit>())
            {
                return true;
            }

            if (go.GetComponent<SplitStreamer>())
            {
                return true;
            }

            if (go == SceneSplitterSettings.gameObject)
            {
                return true;
            }

            return false;
        }

        public void CollectionLayerObjects()
        {
            EnsureSplitterSetting();
            EnsureLightmaps();
            var setting = SceneSplitterSettings;
            var lastStreamers = EditorUtils.FindSceneObjectsOfType<SplitStreamer>(setting.gameObject.scene);
            var layerObjects = new GameObject[setting.config.layers.Count];
            for (var i = 0; i < setting.config.layers.Count; i++)
            {
                var layer = setting.config.layers[i];
                var index = Array.FindIndex(lastStreamers, s => s && s.name == layer.name);
                if (index >= 0)
                {
                    layerObjects[i] = lastStreamers[index].gameObject;
                    lastStreamers[index] = null;
                }
                else
                {
                    var obj = new GameObject(setting.config.layers[i].name) {tag = "SceneStreamer"};
                    Undo.RegisterCreatedObjectUndo(obj, obj.name);
                    var streamer = obj.AddComponent<SplitStreamer>();
                    streamer.transform.position = Vector3.zero;
                    streamer.layerSetting = layer;
                    streamer.template = setting.config;
                    layerObjects[i] = streamer.gameObject;
                }
            }

            foreach (var streamer in lastStreamers)
            {
                if (!streamer)
                {
                    continue;
                }

                foreach (Transform t in streamer.transform)
                {
                    Undo.SetTransformParent(t, null, "streamer destroy");
                }

                Undo.DestroyObjectImmediate(streamer.gameObject);
            }
            
            var lastSplits = EditorUtils.FindSceneObjectsOfType<SplitManager>(setting.gameObject.scene);
            foreach (var splitManager in lastSplits)
            {
                Undo.DestroyObjectImmediate(splitManager);
            }

            var allObjects = FindSceneObjectsOfType<GameObject>();
            Array.Sort(allObjects, GameObjectCompare);
            for (var i = 0; i < allObjects.Length; i++)
            {
                var obj = allObjects[i];
                var p = obj.transform.parent;
                while (p)
                {
                    if (PrefabUtility.IsAnyPrefabInstanceRoot(p.gameObject))
                    {
                        allObjects[i] = null;
                        break;
                    }

                    p = p.parent;
                }
            }

            allObjects = allObjects.Where(o => o).ToArray();


            for (var i = allObjects.Length - 1; i >= 0; i--)
            {
                var obj = allObjects[i];
                if (!setting.config.layers.Any(l => l.IsMatchLayer(obj.name)))
                {
                    allObjects[i] = null;
                }
                else if (IsIgnoreObject(obj))
                {
                    allObjects[i] = null;
                }
            }

            var allValidObjects = allObjects.Where(e => e != null).ToArray();
            // allValidObjects = FilterRootObjects(allValidObjects);
            for (var index = 0; index < layerObjects.Length; index++)
            {
                var layerObj = layerObjects[index];
                var layer = setting.config.layers[index];

                var layerValidObjects = allValidObjects.Where(e => e != null && layer.IsMatchLayer(e.name)).ToArray();

                foreach (var go in layerValidObjects)
                {
                    Undo.SetTransformParent(go.transform, layerObj.transform, $"{go}->{layerObj}");
                }
            }
        }


        private Dictionary<string, SplitManager> SplitScene(SplitStreamer streamer)
        {
            var layer = streamer.layerSetting;
            var splits = new Dictionary<string, SplitManager>();

            var allObjects = (from Transform t in streamer.transform select t.gameObject).ToList();
            // var allObjects = FindSceneObjectsOfType<GameObject>();
            foreach (var item in allObjects)
            {
                var posID = SplitUtils.GetID(item, layer.splitSize, layer.scaleBounds);

                var itemId = $"_{posID[0]}_{posID[1]}_{posID[2]}";

                if (!splits.TryGetValue(itemId, out var splitManager))
                {
                    var o = new GameObject(layer.name + itemId);
                    o.transform.SetParent(streamer.transform, false);
                    Undo.RegisterCreatedObjectUndo(o, nameof(SplitScene));

                    splitManager = o.AddComponent<SplitManager>();
                    var data = new SplitInfo
                    {
                        posID = posID,
                    };
                    splitManager.splitData = data;
                    splitManager.streamer = streamer;
                    splits.Add(itemId, splitManager);
                }

                Undo.SetTransformParent(item.transform, splitManager.transform, nameof(SplitScene));
            }

            foreach (var entry in splits)
            {
                var splitManager = entry.Value;
                //修正重名问题
                EditorUtils.RenameChildrenOverlappingNames(splitManager.transform);

                foreach (Transform t in splitManager.transform)
                {
                    var item = t.gameObject;
                    var lps = item.GetComponentsInChildren<LightmapPart>();
                    var lightmaps = new Dictionary<string, LightmapBakedParams>();

                    foreach (var c in lps)
                    {
                        if (c.lightmapIndex >= 0 && c.renderer)
                        {
                            var relativePath = GetRelativeGameObjectPath(item.transform.parent, c.transform);

                            if (lightmaps.ContainsKey(relativePath))
                            {
                                Debug.Log($"duplicates: {streamer.gameObject.scene.name} {c.name}", c.gameObject);
                                if (PrefabUtility.IsPartOfAnyPrefab(c.gameObject))
                                {
                                    var root = PrefabUtility.GetOutermostPrefabInstanceRoot(c.gameObject);
                                    Debug.Log($"UnpackPrefabInstance {root.name}", root);
                                    PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.UserAction);
                                }

                                relativePath += "_" + t.GetHashCode();
                                c.name = c.name += "_" + t.GetHashCode();
                            }

                            var info = new LightmapBakedParams
                            {
                                lightmapIndex = c.lightmapIndex,
                                bindRendererName = relativePath,
                                lightmapScaleOffset = c.lightmapScaleOffset,
                            };
                            lightmaps.Add(info.bindRendererName, info);
                        }

                        Undo.DestroyObjectImmediate(c);
                    }


                    var isMiniPart = false;

                    if (PrefabUtility.IsAnyPrefabInstanceRoot(item) &&
                        !PrefabUtility.IsPartOfModelPrefab(item))
                    {
                        var addedGameObjects = PrefabUtility.GetAddedGameObjects(item);
                        var addedComponents = PrefabUtility.GetAddedComponents(item);
                        var removeComponents = PrefabUtility.GetRemovedComponents(item);
                        var objectOverrides = PrefabUtility.GetObjectOverrides(item);
                        var isAdded = addedComponents.Count > 0 && !addedComponents.All(c => c.instanceComponent is LightmapPart);
                        //var isOverride = objectOverrides.Count > 0 && !objectOverrides.All(c => c.instanceObject is Transform || c.instanceObject is GameObject);
                        if (isAdded || addedGameObjects.Count > 0 || removeComponents.Count > 0)
                        {
                            Debug.Log($"prefab {item} has override", item);
                        }
                        else
                        {
                            isMiniPart = true;
                            var assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(item);
                            splitManager.AddMiniPart(assetPath, item.transform, lightmaps.Values.ToArray());
                        }
                    }

                    if (!isMiniPart)
                    {
                        splitManager.AddLightmaps(lightmaps.Values.ToArray());
                    }
                }
            }

            return splits;
        }

        private static string GetRelativeGameObjectPath(Transform parent, Transform child)
        {
            var p = child;
            var all = new List<string>();
            while (p && p != parent)
            {
                all.Add(p.name);
                p = p.parent;
            }

            all.Reverse();
            return string.Join("/", all);
        }

        private TV[] FindSceneObjectsOfType<TV>() where TV : UnityEngine.Object
        {
            var allObjects = EditorUtils.FindSceneObjectsOfType<TV>(Scene);
            return allObjects;
        }

        private TV FindSceneObjectOfType<TV>() where TV : UnityEngine.Object
        {
            return EditorUtils.FindSceneObjectOfType<TV>(SceneSplitterSettings.gameObject.scene);
        }

        public void SplitScene()
        {
            var streamers = FindSceneObjectsOfType<SplitStreamer>();

            if (streamers.Length != SceneSplitterSettings.config.layers.Count)
            {
                return;
            }

            EnsureLightmaps();
            CleanEmptyGameObject();
            var root = EditorUtils.MergeSceneRootObjects(SceneManager.GetActiveScene());
            root.transform.position = Vector3.zero;
            foreach (var streamer in streamers)
            {
                SplitScene(streamer);
            }

            SceneSplitterSettings.splitStep = SplitterSettings.SplitSteps.Split;
        }

        private void PrefabGenerate(SplitStreamer streamer, int currentLayerID, ref bool cancel)
        {
            if (cancel)
            {
                return;
            }

            var layer = streamer.layerSetting;
            if (!Directory.Exists(_sceneDependSavePath))
            {
                Directory.CreateDirectory(_sceneDependSavePath);
            }

            var mainSplits = streamer.GetComponentsInChildren<SplitManager>();


            if (mainSplits.Length == 0)
            {
                // warning = "No objects to build scenes.";
                return;
            }

            var splitsNames = mainSplits.Select(s => s.name).ToArray();
            Array.Sort(splitsNames);

            int i = 0;
            streamer.splits = mainSplits.Select(m => m.splitData).ToArray();
            foreach (var splitManager in mainSplits)
            {
                if (cancel)
                {
                    return;
                }

                var assetPath = PrefabGenerate(_sceneDependSavePath, splitManager);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    streamer.splits[i] = new SplitInfo
                    {
                        assetPath = assetPath,
                        posID = splitManager.splitData.posID
                    };
                }

                var title = "Creating Scenes " + (currentLayerID + 1) + '/' + SceneSplitterSettings.config.layers.Count + " (" + layer.name + ")";
                var info = "Creating scene " + Path.GetFileNameWithoutExtension(SceneManager.GetActiveScene().name) + " " + i + " from " + splitsNames.Length;
                if (EditorUtility.DisplayCancelableProgressBar(title, info, (currentLayerID + (i / (float) splitsNames.Length)) / SceneSplitterSettings.config.layers.Count))
                {
                    cancel = true;
                    EditorUtility.ClearProgressBar();
                    return;
                }

                i++;
            }
        }

        internal static string PrefabGenerate(string savePath, SplitManager splitManager)
        {
            var sceneName = splitManager.name;
            if (splitManager.splitData.parts != null)
            {
                foreach (var p in splitManager.splitData.parts)
                {
                    var item = splitManager.transform.Find(p.name)?.gameObject;
                    if (item)
                    {
                        Undo.DestroyObjectImmediate(item);
                    }
                }
            }

            if (splitManager.transform.childCount > 0)
            {
                Debug.LogWarning("[SaveAsPrefabAssetAndConnect]" + sceneName + ".prefab");
                splitManager.isPrefab = true;
                splitManager.streamer = null;
                var assetPath = $"{savePath}/{sceneName}.prefab";
                PrefabUtility.SaveAsPrefabAssetAndConnect(splitManager.gameObject, assetPath, InteractionMode.AutomatedAction);
                return assetPath;
            }

            return null;
        }

        public void PrefabGenerate()
        {
            var streamers = FindSceneObjectsOfType<SplitStreamer>();
            EditorUtility.DisplayProgressBar("Creating Scenes", "Preparing scene", 0);
            //EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            try
            {
                var cancel = false;
                for (var index = 0; index < streamers.Length; index++)
                {
                    var streamer = streamers[index];
                    var layer = streamer.layerSetting;

                    var title = "Preparing Scenes " + (index + 1) + '/' + streamers.Length;
                    var info = "Preparing scene " + layer.gameObjectPrefix;
                    if (EditorUtility.DisplayCancelableProgressBar(title, info, (index / (float) streamers.Length)))
                    {
                        break;
                    }

                    PrefabGenerate(streamer, index, ref cancel);

                    if (cancel)
                    {
                        break;
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private bool OnlyContainsTransform(GameObject o)
        {
            return !PrefabUtility.IsPartOfAnyPrefab(o) && o.transform.childCount == 0 && o.GetComponents<Component>().Length == 1;
        }


        public void GetAssetDependencies(SplitStreamer streamer, ICollection<string> ret)
        {
            if (streamer.splits == null)
            {
                return;
            }

            foreach (var s in streamer.splits)
            {
                if (s.parts != null)
                {
                    var all = (from part in s.parts where !string.IsNullOrEmpty(part.assetPath) select part.assetPath);
                    foreach (var str in all)
                    {
                        ret.Add(str);
                    }
                }

                if (!string.IsNullOrEmpty(s.assetPath))
                {
                    ret.Add(s.assetPath);
                    var go = AssetDatabase.LoadAssetAtPath<GameObject>(s.assetPath);
                    if (!go)
                    {
                        Debug.LogError($"{s.assetPath} not found");
                        continue;
                    }

                    var sp = go.GetComponent<SplitManager>();
                    if (sp && sp.splitData.parts != null)
                    {
                        var all = (from part in sp.splitData.parts where !string.IsNullOrEmpty(part.assetPath) select part.assetPath);
                        foreach (var str in all)
                        {
                            ret.Add(str);
                        }
                    }
                }
            }
        }

        public void CleanScene()
        {
            var scene = SceneManager.GetActiveScene();
            //场景动态依赖
            var assets = new HashSet<string>();
            var streamers = FindSceneObjectsOfType<SplitStreamer>();
            foreach (var streamer in streamers)
            {
                GetAssetDependencies(streamer, assets);
            }


            var obj = ScriptableObject.CreateInstance<SceneDynamicReference>();
            obj.scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
            obj.assets = assets.Select(AssetDatabase.LoadMainAssetAtPath).ToList();

            var lm = FindSceneObjectOfType<SceneLightmaps>();
            if (lm)
            {
                var textures = new List<string>();
                lm.lightmapSetting.DynamicLoadTexture = true;
                if (lm.lightmapSetting.dirsPath != null)
                {
                    textures.AddRange(lm.lightmapSetting.dirsPath.Where(m => !string.IsNullOrEmpty(m)));
                }

                if (lm.lightmapSetting.lightsPath != null)
                {
                    textures.AddRange(lm.lightmapSetting.lightsPath.Where(m => !string.IsNullOrEmpty(m)));
                }

                if (lm.lightmapSetting.shadowMasksPath != null)
                {
                    textures.AddRange(lm.lightmapSetting.shadowMasksPath.Where(m => !string.IsNullOrEmpty(m)));
                }

                obj.lightmapTextures = textures.Select(AssetDatabase.LoadAssetAtPath<Texture2D>).ToList();
            }


            var dirPath = Path.Combine(Path.GetDirectoryName(scene.path), Path.GetFileNameWithoutExtension(scene.path));
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            AssetDatabase.CreateAsset(obj, $"{dirPath}/{scene.name}_dref.asset");

            foreach (var streamer in streamers)
            {
                for (var i = streamer.transform.childCount - 1; i >= 0; i--)
                {
                    Undo.DestroyObjectImmediate(streamer.transform.GetChild(i).gameObject);
                }
            }

            var sceneRoot = EditorUtils.MergeSceneRootObjects(SceneManager.GetActiveScene());


            var allComps = SceneSplitterSettings.GetComponents<Component>();
            foreach (var c in allComps)
            {
                if (!(c is Transform))
                {
                    EditorUtils.InvokeStaticMethod(typeof(UnityEditorInternal.ComponentUtility), "MoveComponentToGameObject", c, sceneRoot);
                }
            }


            // 清理空的GameObject
            CleanEmptyGameObject();
            Selection.activeGameObject = sceneRoot;
            SceneSplitterSettings.splitStep = SplitterSettings.SplitSteps.Clean;
        }

        private void CleanEmptyGameObject()
        {
            var objs = FindSceneObjectsOfType<GameObject>();
            Array.Sort(objs, (x, y) =>
            {
                var xDepth = EditorUtils.GetHierarchyDepth(x);
                var yDepth = EditorUtils.GetHierarchyDepth(y);

                if (xDepth != yDepth)
                {
                    return xDepth.CompareTo(yDepth);
                }

                return string.Compare(x.name, y.name, StringComparison.Ordinal);
            });
            for (var i = objs.Length - 1; i >= 0; i--)
            {
                var o = objs[i];
                if (OnlyContainsTransform(o))
                {
                    Undo.DestroyObjectImmediate(o);
                }
            }
        }
    }
}