using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DeepU3.Editor.AssetBundle
{
    public class SceneDynamicReference : ScriptableObject
    {
        public SceneAsset scene;

        public List<Object> assets = new List<Object>();

        public List<Texture2D> lightmapTextures = new List<Texture2D>();

        public static SceneDynamicReference GetSceneDynamicReferenceByScenePath(string scenePath)
        {
            var sceneDynamics = AssetDatabase.FindAssets("t:SceneDynamicReference");
            foreach (var guid in sceneDynamics)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var dependencies = AssetDatabase.GetDependencies(assetPath, false).ToList();
                if (dependencies.Contains(scenePath))
                {
                    return AssetDatabase.LoadAssetAtPath<SceneDynamicReference>(assetPath);
                }
            }

            return null;
        }

        public static string[] GetDependenciesByScenePath(string scenePath, bool recursive)
        {
            var sceneDynamics = AssetDatabase.FindAssets("t:SceneDynamicReference");
            foreach (var guid in sceneDynamics)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var dependencies = AssetDatabase.GetDependencies(assetPath, false).ToList();
                if (dependencies.Contains(scenePath))
                {
                    dependencies.Remove(scenePath);
                    if (!recursive)
                    {
                        return dependencies.ToArray();
                    }

                    var all = new HashSet<string>();
                    foreach (var dependency in dependencies)
                    {
                        all.Add(dependency);
                        var subs = AssetDatabase.GetDependencies(dependency, true);
                        foreach (var s in subs)
                        {
                            all.Add(s);
                        }
                    }

                    return all.ToArray();
                }
            }

            return new string[0];
        }
    }
}