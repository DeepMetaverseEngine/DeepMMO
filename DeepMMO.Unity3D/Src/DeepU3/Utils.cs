using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DeepU3
{
    public static class Utils
    {
        public static int CombineHashCodes(int h1, int h2)
        {
            unchecked
            {
                var hashCode = h1 * 397;
                hashCode ^= h2;
                return hashCode;
                // return (((h1 << 5) + h1) ^ h2);
            }
        }

        public enum GuessOpt
        {
            OnlySelf,
            IncludeChildren,
            IncludeChildrenWithoutSelfTransform,
        }

        public static Bounds TryGuessWorldBounds(GameObject go, GuessOpt opt)
        {
            if (opt == GuessOpt.OnlySelf)
            {
                var render = go.GetComponent<Renderer>();
                if (render && render.bounds.Contains(go.transform.position))
                {
                    return render.bounds;
                }

                return new Bounds(go.transform.position, new Vector3(1, 1, 1));
            }

            var min = Vector3.positiveInfinity;
            var max = Vector3.negativeInfinity;
            var doneTransforms = new HashSet<Transform>();
            var renders = go.GetComponentsInChildren<Renderer>();
            foreach (var r in renders)
            {
                if (r is ParticleSystemRenderer p)
                {
                    continue;
                }

                min = Vector3.Min(min, r.bounds.min);
                max = Vector3.Max(max, r.bounds.max);
                doneTransforms.Add(r.transform);
            }

            var transforms = go.GetComponentsInChildren<Transform>();
            foreach (var t in transforms)
            {
                if (t == go.transform || doneTransforms.Contains(t))
                {
                    continue;
                }

                min = Vector3.Min(min, t.position);
                max = Vector3.Max(max, t.position);
            }

            if (min.Equals(Vector3.positiveInfinity) || max.Equals(Vector3.negativeInfinity))
            {
                return new Bounds(go.transform.position, new Vector3(1, 1, 1));
            }

            var worldBounds = new Bounds();
            worldBounds.SetMinMax(min, max);

            if (opt == GuessOpt.IncludeChildrenWithoutSelfTransform || doneTransforms.Contains(go.transform))
            {
                return worldBounds;
            }

            //超过1.5f的偏差,不计算自身
            var checkOutBounds = new Bounds(worldBounds.center, worldBounds.size * 1.5f);
            if (checkOutBounds.Contains(go.transform.position))
            {
                worldBounds.Encapsulate(go.transform.position);
            }

            return worldBounds;
        }

        public static string GetStringPath(this Transform go)
        {
            var name = go.name;
            while (go.parent != null)
            {
                go = go.parent;
                name = go.name + "/" + name;
            }

            return name;
        }

        public static string GetStringPath(this GameObject go)
        {
            return GetStringPath(go.transform);
        }

        ///Gets existing T component or adds new one if not exists
        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            return GetOrAddComponent(go, typeof(T)) as T;
        }

        ///Gets existing T component or adds new one if not exists
        public static T GetOrAddComponent<T>(this Component comp) where T : Component
        {
            return GetOrAddComponent(comp.gameObject, typeof(T)) as T;
        }

        ///Gets existing component or adds new one if not exists
        public static Component GetOrAddComponent(this GameObject go, System.Type type)
        {
            var result = go.GetComponent(type);
            if (!result)
            {
                result = go.AddComponent(type);
            }

            return result;
        }


        public static void RenameChildrenOverlappingNames(Transform transform)
        {
            var nameObjs = new Dictionary<string, GameObject>();
            foreach (Transform t in transform)
            {
                if (nameObjs.ContainsKey(t.name))
                {
                    t.name += "_" + t.name.GetHashCode();
                }

                nameObjs.Add(t.name, t.gameObject);
            }
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
                    root.name = scene.name;
                }
            }
            else
            {
                root = roots.FirstOrDefault(go => go.name == scene.name);
                if (root == null)
                {
                    root = new GameObject(scene.name);
                }

                foreach (var go in roots)
                {
                    if (go != root)
                    {
                        go.transform.SetParent(root.transform, true);
                    }
                }
            }

            if (root != null && root.scene != scene)
            {
                SceneManager.MoveGameObjectToScene(root, scene);
            }

            return root;
        }
    }
}