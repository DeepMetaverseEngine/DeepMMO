using System.Linq;
using DeepU3.Lightmap;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepU3.Editor.Lightmap
{
    [CustomEditor(typeof(SceneLightmaps))]
    public class SceneLightmapsEditor : UnityEditor.Editor
    {
        private SceneLightmaps _sceneLightmaps;

        [MenuItem("DU3/Collect Baked Lightmaps",false, 9999)]
        private static void CollectSceneLightmaps()
        {
            var s = SceneManager.GetActiveScene();
            var lightmaps = EditorUtils.FindSceneObjectOfType<SceneLightmaps>(s);
            if (lightmaps)
            {
                return;
            }

            var rootTrans = s.rootCount == 1 ? s.GetRootGameObjects()[0].transform : null;
            var o = new GameObject("CollectedLightmaps");

            Undo.RegisterCreatedObjectUndo(o, "CollectedLightmaps");
            if (rootTrans)
            {
                o.transform.SetParent(rootTrans, false);
            }
            else
            {
                SceneManager.MoveGameObjectToScene(o, s);
            }

            CollectSceneLightmaps(o);
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("renderSetting"), true);

            var lightmapSetting = _sceneLightmaps.lightmapSetting;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("lightmapSetting"), true);
            lightmapSetting.DynamicLoadTexture = EditorGUILayout.Toggle(new GUIContent("Lightmap动态加载"), lightmapSetting.DynamicLoadTexture);

            if (GUILayout.Button(new GUIContent("Recollect Lightmaps")))
            {
                CollectSceneLightmaps(_sceneLightmaps.gameObject);
            }

            serializedObject.ApplyModifiedProperties();
        }


        private void OnEnable()
        {
            _sceneLightmaps = (SceneLightmaps) target;
        }


        public static SceneLightmaps CollectSceneLightmaps(GameObject go)
        {
            var lm = go.GetComponent<SceneLightmaps>();
            if (!lm)
            {
                lm = Undo.AddComponent<SceneLightmaps>(go);
            }
            else
            {
                Undo.RecordObject(lm, "CollectSceneLightmaps");
            }

            lm.lightmapSetting.count = LightmapSettings.lightmaps.Length;
            lm.lightmapSetting.lights = LightmapSettings.lightmaps.Select(m => m.lightmapColor).ToArray();
            lm.lightmapSetting.dirs = LightmapSettings.lightmaps.Select(m => m.lightmapDir).ToArray();
            lm.lightmapSetting.shadowMasks = LightmapSettings.lightmaps.Select(m => m.shadowMask).ToArray();

            lm.lightmapSetting.lightsPath = LightmapSettings.lightmaps.Select(m => AssetDatabase.GetAssetPath(m.lightmapColor)).ToArray();
            lm.lightmapSetting.dirsPath = LightmapSettings.lightmaps.Select(m => AssetDatabase.GetAssetPath(m.lightmapDir)).ToArray();
            lm.lightmapSetting.shadowMasksPath = LightmapSettings.lightmaps.Select(m => AssetDatabase.GetAssetPath(m.shadowMask)).ToArray();


            lm.lightmapSetting.mode = LightmapSettings.lightmapsMode;
            lm.renderSetting.skybox = RenderSettings.skybox;
            lm.renderSetting.lightProbes = LightmapSettings.lightProbes;

            //环境反射
            lm.renderSetting.defaultReflectionMode = RenderSettings.defaultReflectionMode;
            lm.renderSetting.customReflection = lm.renderSetting.defaultReflectionMode == UnityEngine.Rendering.DefaultReflectionMode.Custom ? RenderSettings.customReflection : null;
            lm.renderSetting.reflectionBounces = RenderSettings.reflectionBounces;
            lm.renderSetting.reflectionIntensity = RenderSettings.reflectionIntensity;


            var rs = FindObjectsOfType<Renderer>(); //GameObject.FindObjectsOfType<Renderer>();

            foreach (var r in rs)
            {
#if UNITY_2019_4_OR_NEWER
                var isStatic = (GameObjectUtility.GetStaticEditorFlags(r.gameObject) & StaticEditorFlags.ContributeGI) != 0;
#else
                var isStatic = (GameObjectUtility.GetStaticEditorFlags(r.gameObject) & StaticEditorFlags.LightmapStatic) != 0;
#endif

                //先全删掉
                var lps = r.gameObject.GetComponents<LightmapPart>();
                foreach (var lp in lps)
                {
                    Undo.DestroyObjectImmediate(lp);
                }

                if (isStatic && r.lightmapIndex >= 0)
                {
                    var lp = Undo.AddComponent<LightmapPart>(r.gameObject);
                    lp.renderer = r;
                    lp.lightmapIndex = r.lightmapIndex;
                    lp.lightmapScaleOffset = r.lightmapScaleOffset;
                }
            }

            return lm;
        }
    }
}