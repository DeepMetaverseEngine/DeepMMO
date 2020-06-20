using System;
using UnityEngine;

namespace DeepU3.SceneConnect
{
    public class ConnectAreaGizmos : MonoBehaviour
    {
        private void Start()
        {
        }

        private void OnDrawGizmosSelected()
        {
            var areas = FindObjectsOfType<ConnectArea>();
            Gizmos.color = Color.green;

            foreach (var area in areas)
            {
                var bounds = new Bounds();
                bounds.SetMinMax(area.boundsMin, area.boundsMax);
#if UNITY_EDITOR
                UnityEditor.Handles.Label(bounds.center, transform.name);
#endif
                Gizmos.DrawWireCube(area.transform.position + bounds.center, bounds.size);
            }
        }
    }
}