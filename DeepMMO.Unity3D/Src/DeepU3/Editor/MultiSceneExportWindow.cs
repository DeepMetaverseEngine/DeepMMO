using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using DeepU3.Editor.SceneStreamer;
using DeepU3.SceneConnect;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DeepU3.Editor
{
    public class MultiSceneExportWindow : EditorWindow
    {
        [MenuItem("DU3/Window/MultiSceneExport")]
        private static void ShowWindow()
        {
            var window = GetWindow<MultiSceneExportWindow>();
            window.titleContent = new GUIContent("MultiSceneExport");
            window.Show();
        }


        private readonly MultiSceneExportEditor mEditor = new MultiSceneExportEditor();

        private void OnGUI()
        {
            mEditor.SceneNamePrefix = EditorGUILayout.TextField(new GUIContent("Prefix"), mEditor.SceneNamePrefix, GUILayout.Width(300));
            mEditor.SceneNameSuffix = EditorGUILayout.TextField(new GUIContent("Suffix"), mEditor.SceneNameSuffix, GUILayout.Width(300));


            mEditor.CollectLightmaps = EditorGUILayout.Toggle(new GUIContent("Collect Baked Lightmaps"), mEditor.CollectLightmaps);
            mEditor.SplitScene = EditorGUILayout.Toggle(new GUIContent("SplitScene"), mEditor.SplitScene);
            mEditor.OnGUI(new Rect(0, 100, position.width, position.height - 100));
        }

        private void OnEnable()
        {
            var splitSceneKey = $"{mEditor.GetType().Name}_{nameof(MultiSceneExportEditor.SplitScene)}";
            mEditor.SplitScene = EditorPrefs.GetBool(splitSceneKey, false);
            var lightmapKey = $"{mEditor.GetType().Name}_{nameof(MultiSceneExportEditor.CollectLightmaps)}";
            mEditor.CollectLightmaps = EditorPrefs.GetBool(lightmapKey, true);

            var prefixKey = $"{mEditor.GetType().Name}_{nameof(MultiSceneExportEditor.SceneNamePrefix)}";
            mEditor.SceneNamePrefix = EditorPrefs.GetString(prefixKey, "");

            var suffixKey = $"{mEditor.GetType().Name}_{nameof(MultiSceneExportEditor.SceneNameSuffix)}";
            mEditor.SceneNameSuffix = EditorPrefs.GetString(suffixKey, "");
        }
    }

    public class MultiSceneExportEditor
    {
        public string SceneNamePrefix { get; set; }
        public string SceneNameSuffix { get; set; }
        public bool CollectLightmaps { get; set; }
        public bool SplitScene { get; set; }

        #region treeview

        private class MultiSceneExportTreeViewState : TreeViewState
        {
            private readonly List<string> mScenePaths = new List<string>();

            public ReadOnlyCollection<string> ScenePaths => new ReadOnlyCollection<string>(mScenePaths);

            public void AddScenePath(string path)
            {
                if (mScenePaths.Contains(path))
                {
                    return;
                }

                mScenePaths.Add(path);
            }

            public int RemoveSelects() => mScenePaths.RemoveAll(m => selectedIDs.Contains(m.GetHashCode()));
        }

        private class MultiSceneExportTreeView : TreeView
        {
            private static MultiColumnHeader CreateMultiColumnHeader()
            {
                var retVal = new[]
                {
                    new MultiColumnHeaderState.Column(),
                };

                var counter = 0;
                retVal[counter].headerContent = new GUIContent("Scene Path", "场景路径");
                retVal[counter].minWidth = 300;
                retVal[counter].width = 400;
                retVal[counter].maxWidth = 10000;
                retVal[counter].headerTextAlignment = TextAlignment.Left;
                retVal[counter].canSort = false;
                retVal[counter].autoResize = true;
                return new MultiColumnHeader(new MultiColumnHeaderState(retVal));
            }


            public MultiSceneExportTreeViewState ViewState => (MultiSceneExportTreeViewState) state;

            public MultiSceneExportTreeView(MultiSceneExportTreeViewState state) : base(state
                // , CreateMultiColumnHeader()
            )
            {
            }

            protected override TreeViewItem BuildRoot()
            {
                var root = new TreeViewItem(-1, -1);
                foreach (var path in ViewState.ScenePaths)
                {
                    root.AddChild(new TreeViewItem(path.GetHashCode(), -1, path));
                }

                if (!root.hasChildren)
                {
                    root.AddChild(new TreeViewItem());
                }

                return root;
            }

            protected override void KeyEvent()
            {
                if (Event.current.keyCode == KeyCode.Delete && ViewState.RemoveSelects() > 0)
                {
                    Reload();
                }

                base.KeyEvent();
            }

            protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
            {
                if (!args.performDrop)
                {
                    return DragAndDropVisualMode.Generic;
                }

                var addedScenes = DragAndDrop.objectReferences.Where(node => node is SceneAsset).Select(AssetDatabase.GetAssetPath).ToArray();
                if (addedScenes.Length == 0)
                {
                    return DragAndDropVisualMode.Generic;
                }

                foreach (var assetPath in addedScenes)
                {
                    ViewState.AddScenePath(assetPath);
                }

                Reload();

                return DragAndDropVisualMode.Generic;
            }
        }

        #endregion

        private MultiSceneExportTreeView mTreeView;


        private void TopToolbar(Rect toolbarPos)
        {
            //todo TopToolbar
            GUILayout.BeginArea(new Rect(toolbarPos.x, toolbarPos.y, toolbarPos.width, 20));
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.FlexibleSpace();
            var guiMode = new GUIContent("SaveAs");
            var rMode = GUILayoutUtility.GetRect(guiMode, EditorStyles.toolbarButton);
            if (GUI.Button(rMode, guiMode, EditorStyles.toolbarButton))
            {
                var key = $"{this.GetType().Name}_Folder";
                var outPath = EditorPrefs.GetString(key);
                outPath = EditorUtility.SaveFolderPanel("目标目录", outPath, null);
                if (!string.IsNullOrEmpty(outPath))
                {
                    EditorPrefs.SetString(key, outPath);
                    var splitSceneKey = $"{this.GetType().Name}_{nameof(SplitScene)}";
                    EditorPrefs.SetBool(splitSceneKey, SplitScene);
                    var lightmapKey = $"{this.GetType().Name}_{nameof(CollectLightmaps)}";
                    EditorPrefs.SetBool(lightmapKey, CollectLightmaps);
                    
                    var prefixKey = $"{this.GetType().Name}_{nameof(MultiSceneExportEditor.SceneNamePrefix)}";
                    EditorPrefs.SetString(prefixKey, SceneNamePrefix);

                    var suffixKey = $"{this.GetType().Name}_{nameof(MultiSceneExportEditor.SceneNameSuffix)}";
                    EditorPrefs.SetString(suffixKey, SceneNameSuffix);

                    var sceneKey = $"{this.GetType().Name}_Scenes";
                    EditorPrefs.SetString(sceneKey, string.Join(";", mTreeView.ViewState.ScenePaths));
                    try
                    {
                        for (var i = 0; i < mTreeView.ViewState.ScenePaths.Count; i++)
                        {
                            var scenePath = mTreeView.ViewState.ScenePaths[i];
                            EditorUtility.DisplayProgressBar("ProcessScene", scenePath, (float) (i + 1) / mTreeView.ViewState.ScenePaths.Count);
                            var newScenePath = NewScenePath(outPath, scenePath);
                            EditorUtils.CopySceneTo(scenePath, newScenePath);
                            Assert.IsTrue(SceneManager.GetActiveScene().path == newScenePath);
                            if (CollectLightmaps)
                            {
                                EditorApplication.ExecuteMenuItem("DU3/Collect Baked Lightmaps");
                            }

                            FixSceneConnect(outPath);

                            if (SplitScene)
                            {
                                DoSplitScene();
                            }
                        }
                    }
                    finally
                    {
                        EditorUtility.ClearProgressBar();
                    }
                }
            }


            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private string NewScenePath(string outPath, string oldScenePath)
        {
            return EditorUtils.PathToAssetPath($"{outPath}/{SceneNamePrefix}{Path.GetFileNameWithoutExtension(oldScenePath)}{SceneNameSuffix}.unity");
        }

        private void FixSceneConnect(string outPath)
        {
            var trackers = Object.FindObjectsOfType<ActiveTracker>();
            var areas = Object.FindObjectsOfType<ConnectArea>();
            var scenePaths = mTreeView.ViewState.ScenePaths;
            foreach (var tracker in trackers)
            {
                if (scenePaths.Contains(tracker.bindScenePath))
                {
                    tracker.bindScenePath = NewScenePath(outPath, tracker.bindScenePath);
                }
            }

            foreach (var area in areas)
            {
                if (scenePaths.Contains(area.connectScenePath))
                {
                    area.connectScenePath = NewScenePath(outPath, area.connectScenePath);
                }
            }

            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        private void DoSplitScene()
        {
            var editor = new SplitterSettingsEditor(SceneManager.GetActiveScene());
            editor.BuildPipeline();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        public void OnGUI(in Rect position)
        {
            var contentRect = position;

            TopToolbar(contentRect);
            var treeRect = new Rect(contentRect.xMin, contentRect.yMin + 20, contentRect.width, contentRect.height - 20);
            if (mTreeView == null)
            {
                mTreeView = new MultiSceneExportTreeView(new MultiSceneExportTreeViewState());
                var sceneKey = $"{this.GetType().Name}_Scenes";
                var str = EditorPrefs.GetString(sceneKey, null);
                if (!string.IsNullOrEmpty(str))
                {
                    var arr = str.Split(';');
                    foreach (var s in arr)
                    {
                        mTreeView.ViewState.AddScenePath(s);
                    }
                }

                mTreeView.Reload();
            }

            mTreeView.OnGUI(treeRect);
        }
    }
}