using System;
using System.Collections.Generic;
using System.Linq;
using DeepU3.SceneConnect;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace DeepU3.Editor.SceneConnect
{
    [CustomEditor(typeof(SceneConnector))]
    [CanEditMultipleObjects]
    public class SceneConnectorEditor : UnityEditor.Editor
    {
        [MenuItem("Component/DU3/Scene Connect/ActiveTracker")]
        static void AddConnectTracker()
        {
            EditorUtils.TryAddComponent<ActiveTracker>(false, Selection.gameObjects);
        }

        [MenuItem("GameObject/DU3/Scene Connect/SceneConnector", false, -1000)]
        static void AddSceneConnector()
        {
            var root = EditorUtils.MergeSceneRootObjects(Selection.activeGameObject.scene);
            var connector = root.GetOrAddComponent<SceneConnector>();
            Selection.activeGameObject = connector.gameObject;
        }

        [MenuItem("GameObject/DU3/Scene Connect/Overlap Point", false, -1000)]
        static void FixSceneConnectorPosition()
        {
            var scenes = Selection.gameObjects.Select(go => go.scene).ToArray();
            Assert.IsTrue(scenes[0] != scenes[1]);

            var root1 = EditorUtils.MergeSceneRootObjects(scenes[0]);
            var root2 = EditorUtils.MergeSceneRootObjects(scenes[1]);

            Undo.RecordObject(root2.transform, "FixSceneConnectorPosition");

            var t1 = Selection.gameObjects[0].transform;
            var t2 = Selection.gameObjects[1].transform;

            var pos1 = root1.transform.InverseTransformPoint(t1.position);
            var pos2 = root2.transform.InverseTransformPoint(t2.position);

            var offset = pos1 - pos2;
            root2.transform.position = root1.transform.position + offset;

            var s = Math.Max(Math.Abs(offset.x), Math.Abs(offset.y));
            s = Math.Max(Math.Abs(offset.z), s);
            s += 100;
            var connector1 = root1.GetComponent<SceneConnector>();
            if (!connector1)
            {
                connector1 = Undo.AddComponent<SceneConnector>(root1);
                connector1.size = new Vector3(s, s, s);
            }

            var connector2 = root2.GetComponent<SceneConnector>();
            if (!connector2)
            {
                connector2 = Undo.AddComponent<SceneConnector>(root2);
                connector2.size = new Vector3(s, s, s);
            }
        }

        [MenuItem("GameObject/DU3/Scene Connect/Link SceneConnectArea", false, -1000)]
        static void GenericSceneConnect()
        {
            Assert.IsTrue(Selection.gameObjects.Length == 2);
            var scenes = Selection.gameObjects.Select(go => go.scene).ToArray();
            Assert.IsTrue(scenes[0] != scenes[1]);
            var go1 = scenes[0].GetRootGameObjects()[0];
            var go2 = scenes[1].GetRootGameObjects()[0];


            var sceneConnector1 = go1.GetComponent<SceneConnector>();
            var sceneConnector2 = go2.GetComponent<SceneConnector>();
            Assert.IsTrue(sceneConnector1);
            Assert.IsTrue(sceneConnector2);
            sceneConnector1.GenericSceneConnect(sceneConnector2);
            sceneConnector2.GenericSceneConnect(sceneConnector1);
        }

        [MenuItem("DU3/Scene Connect/Connect Opened Scenes", false, 101)]
        static void ConnectHierarchyScenes()
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                var rtSource = EditorUtils.MergeSceneRootObjects(scene);
                rtSource.transform.position = Vector3.zero;
            }

            var allConnectors = FindObjectsOfType<SceneConnector>();
            var allSides = FindObjectsOfType<ConnectArea>();
            var excepts = new HashSet<ConnectArea>();
            foreach (var connector in allConnectors)
            {
                ConnectSideScene(connector, allConnectors, allSides, excepts);
            }
        }

        static void ConnectSideScene(SceneConnector connector, SceneConnector[] allConnectors, ConnectArea[] allSides, HashSet<ConnectArea> excepts)
        {
            var sceneSides = allSides.Where(s => s.gameObject.scene == connector.gameObject.scene).ToArray();
            foreach (var side in sceneSides)
            {
                var targetSide = allSides.FirstOrDefault(e => e.connectScenePath == side.gameObject.scene.path &&
                                                              side.connectScenePath == e.gameObject.scene.path && !excepts.Contains(e));
                if (!targetSide)
                {
                    continue;
                }

                var nextScene = SceneManager.GetSceneByPath(side.connectScenePath);
                var rtSource = EditorUtils.MergeSceneRootObjects(side.gameObject.scene);

                var rt = EditorUtils.MergeSceneRootObjects(nextScene);
                rt.transform.position = rtSource.transform.position;

                var transform1 = side.transform;
                var transform2 = targetSide.transform;
                var offset = transform1.position - transform2.position;
                rt.transform.position += offset;

                excepts.Add(side);
                excepts.Add(targetSide);

                var targetConnector = allConnectors.FirstOrDefault(or => or.gameObject.scene == targetSide.gameObject.scene);
                ConnectSideScene(targetConnector, allConnectors, allSides, excepts);
            }
        }


        private Tuple<Scene, Scene>[] PairScenes()
        {
            int Factorial(int n)
            {
                return n > 1 ? n * Factorial(n - 1) : 1;
            }

            var ret = new Tuple<Scene, Scene>[Factorial(SceneManager.sceneCount) / 4];
            var p = 0;
            for (var i = 0; i < SceneManager.sceneCount - 1; i++)
            {
                for (var j = i + 1; j < SceneManager.sceneCount; j++)
                {
                    ret[p++] = new Tuple<Scene, Scene>(SceneManager.GetSceneAt(i), SceneManager.GetSceneAt(j));
                }
            }

            return ret;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (targets.Length == 2)
            {
                if (GUILayout.Button(new GUIContent("生成连接点")))
                {
                    var connectors = targets.Cast<SceneConnector>().ToArray();
                    connectors[0].GenericSceneConnect(connectors[1]);
                    connectors[1].GenericSceneConnect(connectors[0]);
                }
            }
        }
    }

    [CustomEditor(typeof(ActiveTracker))]
    [CanEditMultipleObjects]
    public class ActiveTrackerEditor : UnityEditor.Editor
    {
        private ConnectArea mBindArea;

        private SerializedProperty mTriggerLoadScene;

        private void OnEnable()
        {
            var tracker = (ActiveTracker) target;
            mTriggerLoadScene = serializedObject.FindProperty(nameof(ActiveTracker.triggerLoadScene));
            var areas = FindObjectsOfType<ConnectArea>();
            mBindArea = areas.FirstOrDefault(a => a.connectScenePath == tracker.bindScenePath && a.gameObject.scene == tracker.gameObject.scene);
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(mTriggerLoadScene, new GUIContent("Trigger LoadScene"));
            if (targets.Length == 1)
            {
                var tracker = (ActiveTracker) target;

                var area = EditorGUILayout.ObjectField(new GUIContent("Bind Area"), mBindArea, typeof(ConnectArea), true);
                if (area != mBindArea)
                {
                    mBindArea = (ConnectArea) area;
                    tracker.bindScenePath = mBindArea.connectScenePath;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}