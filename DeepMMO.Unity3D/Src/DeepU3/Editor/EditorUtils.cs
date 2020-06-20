using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace DeepU3.Editor
{
    public static class EditorUtils
    {
        public static List<FileInfo> ListAllFiles(DirectoryInfo dir)
        {
            List<FileInfo> list = new List<FileInfo>();
            ListAllFiles(list, dir);
            return list;
        }

        public static List<DirectoryInfo> ListAllDirectories(DirectoryInfo dir)
        {
            List<DirectoryInfo> list = new List<DirectoryInfo>();
            ListAllDirectories(list, dir);
            return list;
        }

        public static void ListAllFiles(List<FileInfo> list, DirectoryInfo dir)
        {
            foreach (FileInfo sub in dir.GetFiles())
            {
                list.Add(sub);
            }

            foreach (DirectoryInfo sub in dir.GetDirectories())
            {
                ListAllFiles(list, sub);
            }
        }

        public static void ListAllDirectories(List<DirectoryInfo> list, DirectoryInfo dir)
        {
            foreach (DirectoryInfo sub in dir.GetDirectories())
            {
                list.Add(sub);
                ListAllDirectories(list, sub);
            }
        }

        public static void WindowsCmd(string cmd)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                Arguments = $"/c {cmd}",
                FileName = "cmd.exe"
            };

            Process.Start(startInfo);
        }

        public static TComponent GetOrAddComponent<TComponent>(this GameObject go) where TComponent : Component
        {
            var comp = go.GetComponent<TComponent>();
            if (!comp)
            {
                comp = Undo.AddComponent<TComponent>(go);
            }

            return comp;
        }

        public static string PathToAssetPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            var assetFullPath = Path.GetFullPath("Assets").Replace('\\', '/');
            path = path.Replace('\\', '/');
            path = path.Replace(assetFullPath, "Assets");
            return path.StartsWith("Assets") ? path : null;
        }

        public static void RenameChildrenOverlappingNames(Transform transform)
        {
            var nameObjs = new Dictionary<string, GameObject>();
            foreach (Transform t in transform)
            {
                if (nameObjs.ContainsKey(t.name))
                {
                    Undo.RecordObject(t.gameObject, "RenameChildrenOverlappingNames");
                    t.name += "_" + t.GetHashCode();
                    Debug.Log($"duplicates: {t.gameObject.scene.name} {t.name}", t.gameObject);
                }

                nameObjs.Add(t.name, t.gameObject);
            }
        }

        public static void DrawGUILayoutLine(Color color, int thickness = 2, int padding = 10)
        {
            var r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            DrawGUILine(r, color, thickness, padding);
        }

        public static void DrawBounds(Bounds b, Color lineColor, float delay = 0)
        {
            // bottom
            var p1 = new Vector3(b.min.x, b.min.y, b.min.z);
            var p2 = new Vector3(b.max.x, b.min.y, b.min.z);
            var p3 = new Vector3(b.max.x, b.min.y, b.max.z);
            var p4 = new Vector3(b.min.x, b.min.y, b.max.z);

            Debug.DrawLine(p1, p2, lineColor, delay);
            Debug.DrawLine(p2, p3, lineColor, delay);
            Debug.DrawLine(p3, p4, lineColor, delay);
            Debug.DrawLine(p4, p1, lineColor, delay);

            // top
            var p5 = new Vector3(b.min.x, b.max.y, b.min.z);
            var p6 = new Vector3(b.max.x, b.max.y, b.min.z);
            var p7 = new Vector3(b.max.x, b.max.y, b.max.z);
            var p8 = new Vector3(b.min.x, b.max.y, b.max.z);

            Debug.DrawLine(p5, p6, lineColor, delay);
            Debug.DrawLine(p6, p7, lineColor, delay);
            Debug.DrawLine(p7, p8, lineColor, delay);
            Debug.DrawLine(p8, p5, lineColor, delay);

            // sides
            Debug.DrawLine(p1, p5, lineColor, delay);
            Debug.DrawLine(p2, p6, lineColor, delay);
            Debug.DrawLine(p3, p7, lineColor, delay);
            Debug.DrawLine(p4, p8, lineColor, delay);
        }


        public static void DrawGUILine(Rect r, Color color, int thickness = 2, int padding = 10)
        {
            r.height = thickness;
            r.y += padding / 2;
            r.x -= 2;
            r.width += 6;
            EditorGUI.DrawRect(r, color);
        }

        public static GUIStyle DefaultHeaderStyle { get; private set; }
        public static GUIStyle DefaultHeaderTipsStyle { get; private set; }

        private static void EnsureDefaultStyle()
        {
            if (DefaultHeaderStyle == null)
            {
                DefaultHeaderStyle = new GUIStyle {normal = {textColor = Color.white}, fontSize = 16, border = new RectOffset(2, 2, 2, 2)};
            }

            if (DefaultHeaderTipsStyle == null)
            {
                DefaultHeaderTipsStyle = new GUIStyle {normal = {textColor = Color.red}, alignment = TextAnchor.MiddleCenter};
            }
        }

        public static void DrawGUILayoutHeader(GUIContent header, GUIContent tips = null, GUIStyle headerStyle = null, GUIStyle tipsStyle = null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorUtils.DrawGUILayoutLine(Color.white, 1, 20);
            EnsureDefaultStyle();

            EditorGUILayout.LabelField(header, headerStyle ?? DefaultHeaderStyle);

            if (tips != null && !string.IsNullOrEmpty(tips.text))
            {
                if (tipsStyle == null)
                {
                    tipsStyle = DefaultHeaderTipsStyle;
                }

                GUILayout.Label(tips, tipsStyle);
                EditorGUILayout.Space();
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }

        public static void DrawOutline(Rect rect, float size)
        {
            Color color = new Color(0.6f, 0.6f, 0.6f, 1.333f);
            if (EditorGUIUtility.isProSkin)
            {
                color.r = 0.12f;
                color.g = 0.12f;
                color.b = 0.12f;
            }

            if (Event.current.type != EventType.Repaint)
                return;

            Color orgColor = UnityEngine.GUI.color;
            UnityEngine.GUI.color = UnityEngine.GUI.color * color;
            UnityEngine.GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, size), EditorGUIUtility.whiteTexture);
            UnityEngine.GUI.DrawTexture(new Rect(rect.x, rect.yMax - size, rect.width, size), EditorGUIUtility.whiteTexture);
            UnityEngine.GUI.DrawTexture(new Rect(rect.x, rect.y + 1, size, rect.height - 2 * size), EditorGUIUtility.whiteTexture);
            UnityEngine.GUI.DrawTexture(new Rect(rect.xMax - size, rect.y + 1, size, rect.height - 2 * size), EditorGUIUtility.whiteTexture);

            UnityEngine.GUI.color = orgColor;
        }

        public static T CopyAssetToDir<T>(T asset, string dirPath) where T : UnityEngine.Object
        {
            if (!asset)
            {
                return null;
            }

            var sourcePath = AssetDatabase.GetAssetPath(asset);
            var newPath = EditorUtils.PathToAssetPath($"{dirPath}/{Path.GetFileName(sourcePath)}");

            if (!File.Exists(newPath) || EditorUtils.Md5File(sourcePath) != EditorUtils.Md5File(newPath))
            {
                AssetDatabase.CopyAsset(sourcePath, newPath);
            }

            return AssetDatabase.LoadAssetAtPath<T>(newPath);
        }

        public static string Md5File(string inputFile, int length = 32)
        {
            var md5 = System.Security.Cryptography.MD5.Create();
            var inputBytes = File.ReadAllBytes(inputFile);
            var hash = md5.ComputeHash(inputBytes);
            // step 2, convert byte array to hex string
            var sb = new StringBuilder();
            for (var i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("x2"));
            }

            length = Math.Min(32, length);

            return sb.ToString().Substring(0, length);
        }

        public static string Md5String(string input, int length = 32)
        {
            // step 1, calculate MD5 hash from input
            var md5 = System.Security.Cryptography.MD5.Create();
            var inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            var hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            var sb = new StringBuilder();
            for (var i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("x2"));
            }

            length = Math.Min(32, length);

            return sb.ToString().Substring(0, length);
        }

        private static readonly Dictionary<Type, MethodInfo[]> sCacheStaticMethods = new Dictionary<Type, MethodInfo[]>();

        public static object InvokeStaticMethod(Type t, string methodName, params object[] args)
        {
            if (!sCacheStaticMethods.TryGetValue(t, out var methodInfos))
            {
                methodInfos = t.GetMethods(BindingFlags.NonPublic | BindingFlags.Static);
                sCacheStaticMethods.Add(t, methodInfos);
            }

            var ms = methodInfos.FirstOrDefault(m =>
            {
                if (m.Name != methodName)
                {
                    return false;
                }

                var ps = m.GetParameters();
                return !ps.Where((t1, i) => !t1.ParameterType.IsAssignableFrom(args[i]?.GetType())).Any();
            });
            return ms?.Invoke(null, args);
        }

        public static void DrawDefaultInspector(SerializedObject serializedObject)
        {
            InvokeStaticMethod(typeof(UnityEditor.Editor), "DoDrawDefaultInspector", serializedObject);
        }

        public static int GetHierarchyDepth(GameObject obj)
        {
            var p = obj.transform;
            var depth = 0;
            while (p)
            {
                depth++;
                p = p.parent;
            }

            return depth;
        }

        public static T FindSceneObjectOfType<T>(Scene scene) where T : Object
        {
            return FindSceneObjectsOfType<T>(scene).ElementAtOrDefault(0);
        }

        public static T[] FindSceneObjectsOfType<T>(Scene scene) where T : Object
        {
            var t = typeof(T);
            if (typeof(GameObject).IsAssignableFrom(t))
            {
                return Object.FindObjectsOfType<T>().Where(o => (o as GameObject).scene == scene).ToArray();
            }

            if (typeof(Component).IsAssignableFrom(t))
            {
                return Object.FindObjectsOfType<T>().Where(o => (o as Component).gameObject.scene == scene).ToArray();
            }

            return new T[0];
        }

        public static GameObject MergeSceneRootObjects(Scene scene)
        {
            GameObject root;
            var roots = scene.GetRootGameObjects();
            if (roots.Length == 1)
            {
                root = roots[0];
                if (root.name != scene.name)
                {
                    Undo.RecordObject(root, $"merge-{scene}");
                    root.name = scene.name;
                }
            }
            else
            {
                root = roots.FirstOrDefault(go => go.name == scene.name);
                if (!root)
                {
                    root = new GameObject(scene.name);
                    Undo.RegisterCreatedObjectUndo(root, $"merge-{scene}");
                }

                foreach (var go in roots)
                {
                    if (go != root)
                    {
                        Undo.SetTransformParent(go.transform, root.transform, $"merge-{scene}");
                    }
                }
            }

            if (root != null && root.scene != scene)
            {
                SceneManager.MoveGameObjectToScene(root, scene);
            }

            return root;
        }

        public static void CopySceneTo(string scenePath, string newScenePath)
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            Assert.IsTrue(sceneAsset);

            Assert.IsTrue(Path.GetExtension(newScenePath) == ".unity", newScenePath);

            var dependencies = AssetDatabase.GetDependencies(scenePath, false);
            var lightingDataPath = dependencies.FirstOrDefault(m => typeof(LightingDataAsset).IsAssignableFrom(AssetDatabase.GetMainAssetTypeAtPath(m)));
            Assert.IsTrue(AssetDatabase.CopyAsset(scenePath, newScenePath), newScenePath);

            var s = EditorSceneManager.OpenScene(newScenePath, OpenSceneMode.Single);
            if (lightingDataPath != null)
            {
                var dirPath = Path.Combine(Path.GetDirectoryName(newScenePath), Path.GetFileNameWithoutExtension(newScenePath));
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                var newLightingDataPath = PathToAssetPath($"{dirPath}/LightingData.asset");
                Assert.IsTrue(AssetDatabase.CopyAsset(lightingDataPath, newLightingDataPath));

                var newLightingDataAsset = AssetDatabase.LoadAssetAtPath<LightingDataAsset>(newLightingDataPath);
                var so = new SerializedObject(newLightingDataAsset);
                var sp = so.FindProperty("m_Scene");
                sp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<SceneAsset>(newScenePath);
                so.ApplyModifiedProperties();
                Lightmapping.lightingDataAsset = newLightingDataAsset;
            }

            foreach (var o in s.GetRootGameObjects())
            {
                if (o.name == sceneAsset.name)
                {
                    o.name = s.name;
                }
            }

            EditorSceneManager.SaveScene(s);
        }


        public static void SaveActiveSceneAs(string newScenePath)
        {
            var activeScene = SceneManager.GetActiveScene();
            //拷贝场景
            EditorSceneManager.SaveScene(activeScene, newScenePath, true);

            string newLightingDataPath = null;
            // LightmapData[] newLightmaps = null;
            //拷贝LightingData
            if (Lightmapping.lightingDataAsset)
            {
                var dirPath = Path.Combine(Path.GetDirectoryName(newScenePath), Path.GetFileNameWithoutExtension(newScenePath));
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                var ldPath = AssetDatabase.GetAssetPath(Lightmapping.lightingDataAsset);
                newLightingDataPath = PathToAssetPath($"{dirPath}/LightingData.asset");
                AssetDatabase.CopyAsset(ldPath, newLightingDataPath);
            }

            var s = EditorSceneManager.OpenScene(newScenePath, OpenSceneMode.Single);
            if (newLightingDataPath != null)
            {
                Lightmapping.lightingDataAsset = AssetDatabase.LoadAssetAtPath<LightingDataAsset>(newLightingDataPath);
                var so = new SerializedObject(Lightmapping.lightingDataAsset);
                var sp = so.FindProperty("m_Scene");
                sp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<SceneAsset>(newScenePath);
                so.ApplyModifiedProperties();
            }

            foreach (var o in s.GetRootGameObjects())
            {
                if (o.name == activeScene.name)
                {
                    o.name = s.name;
                }
            }

            EditorSceneManager.SaveScene(s);
        }


        public static void TryAddComponent<T>(bool allowMany, params GameObject[] gos) where T : Component
        {
            foreach (var go in gos)
            {
                if (allowMany || !go.GetComponent<T>())
                {
                    Undo.AddComponent<T>(go);
                }
            }
        }

        private static readonly HashSet<Type> sAllUnityObjects = new HashSet<Type>();

        private static void CollectUnityObjectTypes()
        {
            if (sAllUnityObjects.Count > 0)
            {
                return;
            }

            var all = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in all)
            {
                foreach (var t in assembly.DefinedTypes)
                {
                    if (typeof(Object).IsAssignableFrom(t))
                    {
                        sAllUnityObjects.Add(t);
                    }
                }
            }
        }

        static EditorUtils()
        {
            CollectUnityObjectTypes();
        }

        #region Unity MenuItem

        [MenuItem("DU3/Copy ActiveScene As...", false, 10000)]
        public static void SaveActiveSceneAs()
        {
            var activeScene = SceneManager.GetActiveScene();
            var scenePath = activeScene.path;
            var lastAppend = EditorPrefs.GetString("EditorUtils.SaveActiveSceneAsAppend");
            var lastDir = EditorPrefs.GetString("EditorUtils.SaveActiveSceneAsDir");
            var fPath = EditorUtility.SaveFilePanel("场景另存为", lastDir, activeScene.name + lastAppend, "unity");
            if (string.IsNullOrEmpty(fPath))
            {
                return;
            }

            var newScenePath = PathToAssetPath(fPath);
            if (newScenePath == scenePath)
            {
                return;
            }

            EditorPrefs.SetString("EditorUtils.SaveActiveSceneAsDir", Path.GetDirectoryName(newScenePath));
            var name = Path.GetFileNameWithoutExtension(newScenePath);
            if (name.Contains(activeScene.name))
            {
                var append = name.Replace(activeScene.name, "");
                EditorPrefs.SetString("EditorUtils.SaveActiveSceneAsAppend", append);
            }

            SaveActiveSceneAs(newScenePath);
        }

        [MenuItem("DU3/Copy ActiveScene As...(With Collect Lightmaps)", false, 10000)]
        public static void SaveActiveSceneAsWithCollectLightmaps()
        {
            SaveActiveSceneAs();
            EditorApplication.ExecuteMenuItem("DU3/Collect Baked Lightmaps");
        }

        private static readonly HashSet<GameObject> sCutGameObject = new HashSet<GameObject>();

        [MenuItem("GameObject/DU3/Cut", false, -1000)]
        private static void Cut(MenuCommand cmd)
        {
            sCutGameObject.Add(cmd.context as GameObject);
        }

        [MenuItem("GameObject/DU3/Paste", false, -1000)]
        private static void Paste(MenuCommand cmd)
        {
            Transform p = null;
            if (cmd.context is GameObject contextGo)
            {
                p = contextGo.transform;
            }

            foreach (var go in sCutGameObject)
            {
                if (go)
                {
                    Undo.SetTransformParent(go.transform, p, "DU3-Pause");
                }
            }

            sCutGameObject.Clear();
        }

        [MenuItem("DU3/Try Position To Bounds Center")]
        private static void TryPositionToBoundsCenter()
        {
            foreach (var go in Selection.gameObjects)
            {
                var worldBounds = Utils.TryGuessWorldBounds(go, Utils.GuessOpt.IncludeChildrenWithoutSelfTransform);
                var transforms = go.transform.Cast<Transform>().ToArray();
                var positions = transforms.Select(t => t.position).ToArray();

                go.transform.position = worldBounds.center;
                for (var i = 0; i < transforms.Length; i++)
                {
                    var t = transforms[i];
                    t.position = positions[i];
                }
            }
        }


        [MenuItem("DU3/Count Select Objects")]
        private static void CountSelectObjects()
        {
            Debug.Log($"select objects: {Selection.objects.Length}");
        }

        [MenuItem("DU3/Count Scene GameObjects")]
        private static void CountSceneGameObject()
        {
            Debug.Log($"scene game objects: {Object.FindObjectsOfType<GameObject>().Length}");
        }

        //[MenuItem("DU3/Start New Custom Profiler")]
        private static void StartCustomProfiler()
        {
            var path = EditorUtility.SaveFilePanel("Start New Profiler", null, null, "data");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            Profiler.logFile = path;
            Profiler.enableBinaryLog = true;
            Profiler.enabled = true;
        }

        //[MenuItem("DU3/Stop Custom Profiler")]
        private static void StopCustomProfiler()
        {
            Profiler.logFile = null;
            Profiler.enableBinaryLog = false;
        }

        #endregion
    }
}