using UnityEditor;
using UnityEngine;

namespace DeepU3.Editor
{
    public static class EditorUtilsExtensions
    {
        public static TComponent GetOrAddComponent<TComponent>(this GameObject go) where TComponent : Component
        {
            var comp = go.GetComponent<TComponent>();
            if (!comp)
            {
                comp = Undo.AddComponent<TComponent>(go);
            }

            return comp;
        }
            
    }
}