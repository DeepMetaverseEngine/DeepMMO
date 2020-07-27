using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeepU3.Editor.AssetBundle
{
    class AssetEntryTreeView : TreeView
    {
        private enum ColumnType
        {
            AssetPath,
            AssetType,
            AssetKey,
        }

        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState()
        {
            var retVal = new[]
            {
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column(),
                // new MultiColumnHeaderState.Column(),
            };

            var counter = 0;


            retVal[counter].headerContent = new GUIContent("Asset Path", "资源地址");
            retVal[counter].minWidth = 300;
            retVal[counter].width = 350;
            retVal[counter].maxWidth = 10000;
            retVal[counter].headerTextAlignment = TextAlignment.Left;
            retVal[counter].canSort = false;
            retVal[counter].autoResize = true;
            counter++;

            retVal[counter].headerContent = new GUIContent(EditorGUIUtility.FindTexture("FilterByType"), "资源类型");
            retVal[counter].minWidth = 30;
            retVal[counter].width = 30;
            retVal[counter].maxWidth = 30;
            retVal[counter].headerTextAlignment = TextAlignment.Left;
            retVal[counter].canSort = false;
            retVal[counter].autoResize = true;
            counter++;

            retVal[counter].headerContent = new GUIContent("AssetBundle Name");
            retVal[counter].minWidth = 200;
            retVal[counter].width = 350;
            retVal[counter].maxWidth = 10000;
            retVal[counter].headerTextAlignment = TextAlignment.Left;
            retVal[counter].canSort = false;
            retVal[counter].autoResize = true;
            counter++;


            return new MultiColumnHeaderState(retVal);
        }


        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return true;
        }

        private AssetGroupEditor mEditor;
        GUIStyle m_LabelStyle;
        GUIStyle m_LabelCompletedStyle;

        protected override TreeViewItem BuildRoot()
        {
            mAssetCache.Clear();
            var root = new TreeViewItem(-1, -1);
            mEditor.InitTreeViewItems(root);
            if (!root.hasChildren)
            {
                root.AddChild(new TreeViewItem());
            }

            return root;
        }


        public AssetEntryTreeView(TreeViewState state, MultiColumnHeaderState s, AssetGroupEditor editor) : base(state, new MultiColumnHeader(s))
        {
            showBorder = true;
            columnIndexForTreeFoldouts = 0;
            mEditor = editor;
        }

        private AssetEntryTreeViewItem FindItemInVisibleRows(int id)
        {
            var rows = GetRows();
            foreach (var r in rows)
            {
                if (r.id == id)
                {
                    return r as AssetEntryTreeViewItem;
                }
            }

            return null;
        }

        protected override void DoubleClickedItem(int id)
        {
            base.DoubleClickedItem(id);
            var item = FindItemInVisibleRows(id);
            if (item?.AssetPath != null)
            {
                var o = LoadMainAssetAtPath(item.AssetPath);
                EditorGUIUtility.PingObject(o);
                Selection.activeObject = o;
            }
        }

        protected override bool CanRename(TreeViewItem item)
        {
            return !(item.parent is AssetEntryTreeViewItem) && item is AssetEntryTreeViewItem && item.parent.displayName != "Built In Data";
        }

        protected override Rect GetRenameRect(Rect rowRect, int row, TreeViewItem item)
        {
            var ret = new Rect(rowRect.position, rowRect.size);
            for (var i = 0; i < (int) ColumnType.AssetKey; i++)
            {
                var column = multiColumnHeader.GetColumn(i);
                ret.position = ret.position + new Vector2(column.width, 0);
            }

            return ret;
        }

        private void RenameItem(object context)
        {
            if (context is List<AssetEntryTreeViewItem> selectedNodes && selectedNodes.Count >= 1)
            {
                var item = selectedNodes.First();
                if (CanRename(item))
                {
                    BeginRename(item);
                }
            }
        }

        protected override void RenameEnded(RenameEndedArgs args)
        {
            base.RenameEnded(args);
            var item = FindItemInVisibleRows(args.itemID);
            if (item != null)
            {
                item.displayName = args.newName;
                if (item.AssetNode.IsCompleteAsset)
                {
                    mEditor.SetCompleteAssetItem(item.AssetPath, item.displayName);
                }
                else
                {
                    mEditor.AddOrUpdateAssetItem(item.AssetPath, item.displayName, true);
                }
            }
        }


        protected override void KeyEvent()
        {
            if (Event.current.keyCode == KeyCode.Delete)
            {
                var selectedNodes = new List<AssetEntryTreeViewItem>();
                foreach (var nodeId in GetSelection())
                {
                    var item = FindItemInVisibleRows(nodeId);
                    if (item != null)
                    {
                        selectedNodes.Add(item);
                    }
                }

                if (selectedNodes.All(item => !(item.parent is AssetEntryTreeViewItem)))
                {
                    foreach (var item in selectedNodes)
                    {
                        if (string.IsNullOrEmpty(item.AssetNode.AssetBundleName))
                        {
                            mEditor.SetInvalidDependency(item.AssetPath, false);
                        }
                        else if (item.AssetNode.IsCompleteAsset)
                        {
                            mEditor.SetCompleteAssetItem(item.AssetPath, null);
                        }
                        else
                        {
                            mEditor.RemoveAssetItem(item.AssetPath);
                        }
                    }

                    Reload();
                }
            }

            base.KeyEvent();
        }

        private Dictionary<string, Object> mAssetCache = new Dictionary<string, Object>();

        private Object LoadMainAssetAtPath(string path)
        {
            if (mAssetCache.TryGetValue(path, out var obj))
            {
                return obj;
            }

            obj = AssetDatabase.LoadMainAssetAtPath(path);
            mAssetCache.Add(path, obj);
            return obj;
        }


        protected override void SelectionChanged(IList<int> selectedIds)
        {
            base.SelectionChanged(selectedIds);

            // UnityEngine.Object[] selectedObjects = new UnityEngine.Object[selectedIds.Count];
            // for (int i = 0; i < selectedIds.Count; i++)
            // {
            //     var item = FindItemInVisibleRows(selectedIds[i]);
            //     if (item?.AssetPath != null)
            //     {
            //         selectedObjects[i] = LoadMainAssetAtPath(item.AssetPath);
            //     }
            // }
            // // change selection
            // Selection.objects = selectedObjects; 
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            if (!(args.item is AssetEntryTreeViewItem item))
            {
                using (new EditorGUI.DisabledScope(args.item.displayName == "Built In Data"))
                {
                    base.RowGUI(args);
                    return;
                }
            }

            var disable = mEditor.IsIgnoreItem(item.AssetPath) || string.IsNullOrEmpty(item.AssetNode.AssetBundleName) || item.AssetNode.IsIgnore;
            using (new EditorGUI.DisabledScope(disable))
            {
                for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
                {
                    CellGUI(args.GetCellRect(i), item, args.GetColumn(i), disable, ref args);
                }
            }
        }


        //所有资源都统一文件名后缀
        //场景名->abname 映射 =====> ABSceneNameAsset
        private void CellGUI(Rect cellRect, AssetEntryTreeViewItem item, int column, bool disable, ref RowGUIArgs args)
        {
            if (m_LabelStyle == null)
            {
                m_LabelStyle = new GUIStyle("PR Label");
                m_LabelCompletedStyle = new GUIStyle
                {
                    normal = {textColor = Color.magenta},
                    margin = m_LabelStyle.margin,
                    padding = m_LabelStyle.padding,
                    alignment = m_LabelStyle.alignment,
                    border = m_LabelStyle.border,
                    fixedHeight = m_LabelStyle.fixedHeight,
                    fixedWidth = m_LabelStyle.fixedWidth,
                    stretchWidth = m_LabelStyle.stretchWidth
                };
            }

            var columnType = (ColumnType) column;
            switch (columnType)
            {
                case ColumnType.AssetPath:
                {
                    cellRect.x += item.depth * 16;

                    if (Event.current.type == EventType.Repaint)
                    {
                        var assetPath = item.parent is AssetEntryTreeViewItem ? Path.GetFileName(item.AssetPath) : item.AssetPath.Substring("Assets/".Length);
                        if (item.AssetNode.IsCompleteAsset)
                        {
                            m_LabelCompletedStyle.Draw(cellRect, assetPath, false, false, args.selected, args.focused);
                        }
                        else
                        {
                            m_LabelStyle.Draw(cellRect, assetPath, false, false, args.selected, args.focused);
                        }
                    }

                    break;
                }
                case ColumnType.AssetType when item.AssetIcon && !disable:
                {
                    GUI.DrawTexture(cellRect, item.AssetIcon, ScaleMode.ScaleToFit, true);
                    break;
                }
                case ColumnType.AssetKey when !disable && Event.current.type == EventType.Repaint:
                {
                    var displayName = item.hasChildren ? item.displayName : $"{item.displayName}{mEditor.CurrentProfile.assetExt}";
                    if (item.AssetNode.IsCompleteAsset)
                    {
                        var style = new GUIStyle {normal = {textColor = Color.magenta}};
                        EditorGUI.LabelField(cellRect, new GUIContent(displayName), style);
                    }
                    else
                    {
                        EditorGUI.LabelField(cellRect, new GUIContent(displayName));
                    }


                    break;
                }
            }
        }


        private AssetEntryTreeViewItem GetRootItem(AssetEntryTreeViewItem item)
        {
            var p = item.parent;
            while (p.depth > 0)
            {
                p = p.parent;
            }

            return p as AssetEntryTreeViewItem;
        }


        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            if (!args.performDrop)
            {
                return DragAndDropVisualMode.Generic;
            }

            foreach (var node in DragAndDrop.objectReferences)
            {
                var assetPath = AssetDatabase.GetAssetPath(node);
                if (string.IsNullOrEmpty(assetPath))
                {
                    continue;
                }

                if (args.parentItem != null && args.parentItem.depth == 0)
                {
                    if (args.parentItem.displayName == "Built In Data")
                    {
                        mEditor.SetInvalidDependency(assetPath, true);
                    }
                    else if (args.parentItem.displayName == "Self Contained")
                    {
                        mEditor.SetCompleteAssetItem(assetPath, node.name);
                    }
                }
                else
                {
                    mEditor.AddOrUpdateAssetItem(assetPath, node.name, false);
                }
            }

            Reload();

            return DragAndDropVisualMode.Generic;
        }

        protected override void ContextClickedItem(int id)
        {
            List<AssetEntryTreeViewItem> selectedNodes = new List<AssetEntryTreeViewItem>();
            foreach (var nodeId in GetSelection())
            {
                var item = FindItemInVisibleRows(nodeId);
                if (item != null)
                {
                    selectedNodes.Add(item);
                }
            }

            if (selectedNodes.Count == 0)
                return;

            GenericMenu menu = new GenericMenu();

            var isNormal = true;
            var depthGtZero = true;
            var top = true;
            var isFolder = true;
            foreach (var item in selectedNodes)
            {
                isNormal = isNormal && !string.IsNullOrEmpty(item.AssetNode.AssetBundleName) && !item.AssetNode.IsCompleteAsset && !item.AssetNode.IsBuiltIn;
                depthGtZero = depthGtZero && item.AssetNode.Depth > 0;
                top = top && !(item.parent is AssetEntryTreeViewItem);
                isFolder = isFolder && item.AssetNode.IsFolder;
            }

            if (isNormal)
            {
                menu.AddItem(new GUIContent("Enable"), false, () =>
                {
                    foreach (var item in selectedNodes)
                    {
                        mEditor.SetIgnore(item.AssetPath, false);
                    }
                });

                menu.AddItem(new GUIContent("Disable"), false, () =>
                {
                    foreach (var item in selectedNodes)
                    {
                        mEditor.SetIgnore(item.AssetPath, true);
                    }
                });
                if (depthGtZero)
                {
                    menu.AddItem(new GUIContent("Move To Top"), false, () =>
                    {
                        foreach (var item in selectedNodes)
                        {
                            var root = GetRootItem(item);
                            mEditor.AddOrUpdateAssetItem(item.AssetPath, root.AssetNode.AssetBundleName + "/" + Path.GetFileNameWithoutExtension(item.AssetPath), false);
                        }

                        Reload();
                    });
                }
            }

            if (top)
            {
                menu.AddItem(new GUIContent("Remove"), false, () =>
                {
                    foreach (var item in selectedNodes)
                    {
                        if (string.IsNullOrEmpty(item.AssetNode.AssetBundleName))
                        {
                            mEditor.SetInvalidDependency(item.AssetPath, false);
                        }
                        else if (item.AssetNode.IsCompleteAsset)
                        {
                            mEditor.SetCompleteAssetItem(item.AssetPath, null);
                        }
                        else
                        {
                            mEditor.RemoveAssetItem(item.AssetPath);
                        }
                    }

                    Reload();
                });

                if (isNormal)
                {
                    if (selectedNodes.Count == 1)
                    {
                        menu.AddItem(new GUIContent("Rename"), false, RenameItem, selectedNodes);
                    }

                    if (isFolder)
                    {
                        menu.AddItem(new GUIContent("Combine"), false, () =>
                        {
                            foreach (var item in selectedNodes)
                            {
                                mEditor.CombineFolder(item.AssetPath, true);
                            }

                            Reload();
                        });
                        menu.AddItem(new GUIContent("Disable Combine"), false, () =>
                        {
                            foreach (var item in selectedNodes)
                            {
                                mEditor.CombineFolder(item.AssetPath, false);
                            }

                            Reload();
                        });
                        if (selectedNodes.Count == 1)
                        {
                            var currentTypes = selectedNodes[0].AssetNode.GetAllTypes();
                            var ignoresTypes = mEditor.CurrentProfile.GetIgnoredTypes(selectedNodes[0].AssetPath);
                            var allTypes = new List<Type>();
                            allTypes.AddRange(currentTypes);
                            allTypes.AddRange(ignoresTypes);
                            allTypes.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));

                            foreach (var t in allTypes)
                            {
                                menu.AddItem(new GUIContent($"Allow Types/{t.Name}"), currentTypes.Contains(t), () =>
                                {
                                    mEditor.CurrentProfile.SetIgnoredType(selectedNodes[0].AssetPath, t, !ignoresTypes.Contains(t));
                                    Reload();
                                });
                            }
                        }
                    }
                    else if (selectedNodes.Count == 1)
                    {
                        var item = selectedNodes[0];
                        var isCombine = item.AssetNode.RootAssetItem.isCombine;
                        menu.AddItem(new GUIContent("Combine Dependencies As Soon As Possible"), isCombine, () =>
                        {
                            item.AssetNode.RootAssetItem.isCombine = !isCombine;
                            mEditor.CurrentProfile.SetDirty(true, false);
                        });
                    }
                }
            }

            menu.ShowAsContext();
        }
    }

    sealed class AssetEntryTreeViewItem : TreeViewItem
    {
        public readonly Texture AssetIcon;
        public AssetBundleProfile.AssetNode AssetNode { get; }
        public string AssetPath => AssetNode.AssetPath;

        public AssetEntryTreeViewItem(AssetBundleProfile.AssetNode node, int depth) : base(node.GetHashCode(), depth, node.AssetBundleName)
        {
            AssetNode = node;
            AssetIcon = AssetDatabase.GetCachedIcon(node.AssetPath) as Texture2D;
        }
    }

    public class AssetGroupEditor : EditorWindow
    {
        [MenuItem("DU3/Window/AssetBundle Manager", false, -1100)]
        internal static void Init()
        {
            var window = GetWindow<AssetGroupEditor>();
            window.titleContent = new GUIContent("AssetBundle Manager");
            var screenCenter = new Rect(Screen.width / 2, Screen.height / 2, 800, 600);
            window.position = screenCenter;
            window.Show();
        }

        [SerializeField]
        MultiColumnHeaderState m_Mchs;

        private TreeViewState m_TreeState;

        internal AssetBundleProfile CurrentProfile { get; private set; }
        private AssetEntryTreeView m_EntryTree;

        [NonSerialized]
        GUIStyle m_ButtonStyle;

        [NonSerialized]
        Texture2D m_CogIcon;

        private BuildTarget m_BuildTarget;

        private void OnEnable()
        {
            m_BuildTarget = EditorUserBuildSettings.activeBuildTarget;
            if (m_BuildTarget == BuildTarget.StandaloneWindows64)
            {
                m_BuildTarget = BuildTarget.StandaloneWindows;
            }
        }


        public void AddOrUpdateAssetItem(string assetPath, string abName, bool reloadTree)
        {
            assetPath = EditorUtils.PathToAssetPath(assetPath);

            if (CurrentProfile.SetAssetItem(assetPath, abName) && reloadTree)
            {
                m_EntryTree.Reload();
            }
        }

        /// <summary>
        /// 单打资源
        /// </summary>
        /// <param name="assetPath"></param>
        /// <param name="abName"></param>
        public void SetCompleteAssetItem(string assetPath, string abName)
        {
            assetPath = EditorUtils.PathToAssetPath(assetPath);
            CurrentProfile.SetCompleteAssetItem(assetPath, abName);
        }

        public void SetInvalidDependency(string assetFolder, bool val)
        {
            assetFolder = EditorUtils.PathToAssetPath(assetFolder);
            CurrentProfile.SetInvalidDependency(assetFolder, val);
        }

        public void RemoveAssetItem(string assetFolder)
        {
            assetFolder = EditorUtils.PathToAssetPath(assetFolder);
            CurrentProfile.RemoveAssetItem(assetFolder);
        }

        public void CombineFolder(string assetFolder, bool combine)
        {
            CurrentProfile.SetCombine(assetFolder, combine);
        }

        public void SetIgnore(string assetPath, bool ignore)
        {
            CurrentProfile.SetIgnore(assetPath, ignore);
        }

        public bool IsIgnoreItem(string assetPath)
        {
            return CurrentProfile.IsIgnore(assetPath);
        }

        public void InitTreeViewItems(TreeViewItem parent)
        {
            if (CurrentProfile == null)
            {
                return;
            }

            CurrentProfile.SetDirty(true, true);
            //add built in
            var builtInData = new TreeViewItem(-1, 0, "Built In Data");
            parent.AddChild(builtInData);
            foreach (var child in CurrentProfile.RootBuiltInAssetNode.Children)
            {
                var item = GetTreeViewItem(child, 1);
                builtInData.AddChild(item);
            }

            var selfContainer = new TreeViewItem(-2, 0, "Self Contained");
            parent.AddChild(selfContainer);
            //add complete
            foreach (var child in CurrentProfile.RootCompleteAssetNode.Children)
            {
                var item = GetTreeViewItem(child, 1);
                selfContainer.AddChild(item);
            }


            //add normal 
            foreach (var child in CurrentProfile.RootAssetNode.Children)
            {
                var item = GetTreeViewItem(child, child.Depth);
                parent.AddChild(item);
            }
        }

        private AssetEntryTreeViewItem GetTreeViewItem(AssetBundleProfile.AssetNode assetNode, int depth)
        {
            var ret = new AssetEntryTreeViewItem(assetNode, depth);
            foreach (var child in assetNode.Children)
            {
                var item = GetTreeViewItem(child, depth + 1);
                ret.AddChild(item);
            }

            return ret;
        }


        private void OnGUI()
        {
            var contentRect = new Rect(0, 0, position.width, position.height);
            TopToolbar(contentRect);

            var treeRect = new Rect(contentRect.xMin, contentRect.yMin + 20, contentRect.width, contentRect.height - 20);
            if (m_EntryTree == null)
            {
                if (m_TreeState == null)
                {
                    m_TreeState = new TreeViewState();
                }

                var headerState = AssetEntryTreeView.CreateDefaultMultiColumnHeaderState();
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_Mchs, headerState))
                {
                    MultiColumnHeaderState.OverwriteSerializedFields(m_Mchs, headerState);
                }

                m_Mchs = headerState;
                m_EntryTree = new AssetEntryTreeView(m_TreeState, m_Mchs, this);
                m_EntryTree.Reload();
            }

            m_EntryTree.OnGUI(treeRect);
        }

        private void SelectProfile(object assetPath)
        {
            if (assetPath == null)
            {
                return;
            }

            if (!(assetPath is AssetBundleProfile newProfile))
            {
                newProfile = AssetDatabase.LoadAssetAtPath<AssetBundleProfile>(assetPath.ToString());
            }

            var needReload = CurrentProfile != null && CurrentProfile != newProfile;
            CurrentProfile = newProfile;

            CurrentProfile.SetDirty(true, true);
            EditorPrefs.SetString($"{nameof(AssetBundleProfile)}_SelectProfile", newProfile.name);
            if (needReload)
            {
                m_EntryTree.Reload();
            }

            EditorGUIUtility.PingObject(newProfile);
            Selection.activeObject = newProfile;
        }

        private List<AssetBundleProfile> _allAssetBundleProfile;

        private void InitProfiles(bool force, string selectProfileName = null)
        {
            if (_allAssetBundleProfile == null || force)
            {
                _allAssetBundleProfile = AssetDatabase.FindAssets("t:AssetBundleProfile").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<AssetBundleProfile>).ToList();

                var key = $"{nameof(AssetBundleProfile)}_SelectProfile";
                selectProfileName = selectProfileName ?? EditorPrefs.GetString(key);
                var first = _allAssetBundleProfile.FirstOrDefault(m => m.name == selectProfileName);
                if (!first)
                {
                    first = _allAssetBundleProfile.ElementAtOrDefault(0);
                }

                SelectProfile(first);
            }
        }

        void CreateProfileDropdown()
        {
            InitProfiles(false);
            var profileButton = new GUIContent($"Profile: {CurrentProfile?.name}");

            var r = GUILayoutUtility.GetRect(profileButton, EditorStyles.toolbarDropDown);
            if (EditorGUI.DropdownButton(r, profileButton, FocusType.Passive, EditorStyles.toolbarDropDown))
            {
                //GUIUtility.hotControl = 0;
                var menu = new GenericMenu();

                foreach (var profile in _allAssetBundleProfile)
                {
                    var curName = profile.name;
                    menu.AddItem(new GUIContent(curName), CurrentProfile?.name == curName, SelectProfile, profile);
                }

                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent("Create"), false, () =>
                {
                    var savePath = EditorUtility.SaveFilePanel("创建配置", "Assets", "NewAssetBundleProfile", "asset");
                    if (!string.IsNullOrEmpty(savePath))
                    {
                        var assetProfile = ScriptableObject.CreateInstance<AssetBundleProfile>();
                        var assetPath = EditorUtils.PathToAssetPath(savePath);
                        AssetDatabase.CreateAsset(assetProfile, assetPath);
                        _allAssetBundleProfile.Insert(0, assetProfile);
                        SelectProfile(assetProfile);
                    }
                });
                menu.DropDown(r);
            }
        }

        GUIStyle GetStyle(string styleName)
        {
            GUIStyle s = UnityEngine.GUI.skin.FindStyle(styleName);
            if (s == null)
                s = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle(styleName);
            if (s == null)
            {
                Debug.LogError("Missing built-in guistyle " + styleName);
                s = new GUIStyle();
            }

            return s;
        }


        private string mLastFolder;

        private void CreateAddDropdown()
        {
            var guiMode = new GUIContent("Add Assets");
            Rect rMode = GUILayoutUtility.GetRect(guiMode, EditorStyles.toolbarDropDown);
            if (EditorGUI.DropdownButton(rMode, guiMode, FocusType.Passive, EditorStyles.toolbarDropDown))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Folder"), false, () =>
                {
                    var selectFolder = EditorUtility.OpenFolderPanel("添加目录", "Assets", null);
                    if (!string.IsNullOrEmpty(selectFolder))
                    {
                        mLastFolder = $"{selectFolder}/..";
                        AddOrUpdateAssetItem(selectFolder, Path.GetFileName(selectFolder), true);
                    }
                });
                menu.AddItem(new GUIContent("File"), false, () =>
                {
                    var selectFile = EditorUtility.OpenFilePanel("添加文件", mLastFolder ?? "Assets", null);
                    if (!string.IsNullOrEmpty(selectFile))
                    {
                        mLastFolder = Path.GetDirectoryName(selectFile);
                        AddOrUpdateAssetItem(selectFile, Path.GetFileNameWithoutExtension(selectFile), true);
                    }
                });
                menu.AddItem(new GUIContent("CompleteAsset", "自包含所有依赖"), false, () =>
                {
                    var selectFile = EditorUtility.OpenFilePanel("添加文件", "Assets", null);
                    if (!string.IsNullOrEmpty(selectFile))
                    {
                        mLastFolder = Path.GetDirectoryName(selectFile);
                        SetCompleteAssetItem(selectFile, Path.GetFileNameWithoutExtension(selectFile));
                        m_EntryTree.Reload();
                    }
                });
                menu.AddItem(new GUIContent("Built In Folder", "内建资源, 不导出"), false, () =>
                {
                    var selectFolder = EditorUtility.OpenFolderPanel("添加目录", "Assets", null);
                    if (!string.IsNullOrEmpty(selectFolder))
                    {
                        mLastFolder = $"{selectFolder}/..";
                        SetInvalidDependency(selectFolder, true);
                        m_EntryTree.Reload();
                    }
                });
                menu.AddItem(new GUIContent("Built In File", "内建资源, 不导出"), false, () =>
                {
                    var selectFile = EditorUtility.OpenFilePanel("添加文件", "Assets", null);
                    if (!string.IsNullOrEmpty(selectFile))
                    {
                        SetInvalidDependency(selectFile, true);
                        m_EntryTree.Reload();
                    }
                });
                menu.DropDown(rMode);
            }
        }


        private void ShowDependenciesInProjectBrowser<T>(Predicate<string> checkPath, Comparison<Object> comparison, Func<string, Object> loadFunc) where T : Object
        {
            try
            {
                var t = typeof(T);
                var arr = CurrentProfile.FindDependencies(t);
                EditorUtility.DisplayProgressBar("Hold", "ShowObjectsInProjectBrowser", 0.5f);
                var objPaths = checkPath != null ? arr.Where(m => checkPath(m)).ToList() : arr.ToList();
                objPaths.Sort();
                var objs = objPaths.Select(loadFunc).Where(m => m).ToList();
                if (comparison != null)
                {
                    objs.Sort(comparison);
                }

                EditorUtils.ShowObjectsInProjectBrowser(objs.ToArray());
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void ShowDependenciesInProjectBrowser<T>(Predicate<string> checkPath = null, Comparison<T> comparison = null) where T : Object
        {
            Comparison<Object> finalComparison = null;
            if (comparison != null)
            {
                finalComparison = (x, y) => comparison.Invoke(x as T, y as T);
            }

            ShowDependenciesInProjectBrowser<T>(checkPath, finalComparison, AssetDatabase.LoadMainAssetAtPath);
        }

        private void ShowDependenciesInProjectBrowser<T, TSub>(Predicate<string> checkPath = null, Comparison<TSub> comparison = null) where T : Object where TSub : Object
        {
            Comparison<Object> finalComparison = null;
            if (comparison != null)
            {
                finalComparison = (x, y) => comparison.Invoke(x as TSub, y as TSub);
            }

            ShowDependenciesInProjectBrowser<T>(checkPath, finalComparison, AssetDatabase.LoadAssetAtPath<TSub>);
        }

        void TopToolbar(Rect toolbarPos)
        {
            if (m_ButtonStyle == null)
                m_ButtonStyle = GetStyle("ToolbarButton");
            if (m_CogIcon == null)
                m_CogIcon = EditorGUIUtility.FindTexture("_Popup");


            GUILayout.BeginArea(new Rect(0, 0, toolbarPos.width, 20));

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                var spaceBetween = 4f;
                CreateProfileDropdown();
                using (new EditorGUI.DisabledScope(!CurrentProfile))
                {
                    CreateAddDropdown();

                    {
                        GUILayout.Space(50);
                        var guiMode = new GUIContent($"Platform:{m_BuildTarget}");
                        Rect rMode = GUILayoutUtility.GetRect(guiMode, EditorStyles.toolbarDropDown);
                        if (EditorGUI.DropdownButton(rMode, guiMode, FocusType.Passive, EditorStyles.toolbarDropDown))
                        {
                            var menu = new GenericMenu();
                            menu.AddItem(new GUIContent(BuildTarget.StandaloneWindows.ToString()), BuildTarget.StandaloneWindows == m_BuildTarget, () => { m_BuildTarget = BuildTarget.StandaloneWindows; });
                            menu.AddItem(new GUIContent(BuildTarget.Android.ToString()), BuildTarget.Android == m_BuildTarget, () => { m_BuildTarget = BuildTarget.Android; });
                            menu.AddItem(new GUIContent(BuildTarget.iOS.ToString()), BuildTarget.iOS == m_BuildTarget, () => { m_BuildTarget = BuildTarget.iOS; });
                            menu.DropDown(rMode);
                        }
                    }
                    {
                        GUILayout.Space(20);
                        var guiMode = new GUIContent($"Refresh");
                        var rMode = GUILayoutUtility.GetRect(guiMode, EditorStyles.toolbarButton);
                        if (GUI.Button(rMode, guiMode, EditorStyles.toolbarButton))
                        {
                            InitProfiles(true, CurrentProfile.name);
                            m_EntryTree.Reload();
                        }
                    }
                    {
                        GUILayout.Space(20);
                        var guiMode = new GUIContent("Tools");
                        var rMode = GUILayoutUtility.GetRect(guiMode, EditorStyles.toolbarDropDown);
                        if (EditorGUI.DropdownButton(rMode, guiMode, FocusType.Passive, EditorStyles.toolbarDropDown))
                        {
                            var menu = new GenericMenu();
                            menu.AddItem(new GUIContent("See/Texture/Order By MaxTextureSize"), false, () =>
                            {
                                var dict = new Dictionary<string, TextureImporter>();
                                var settingTarget = m_BuildTarget.ToString();
                                if (settingTarget.StartsWith("Standalone"))
                                {
                                    settingTarget = "Standalone";
                                }

                                ShowDependenciesInProjectBrowser<Texture>(m =>
                                {
                                    var importer = AssetImporter.GetAtPath(m) as TextureImporter;
                                    if (importer == null)
                                    {
                                        return false;
                                    }

                                    dict.Add(m, importer);
                                    return true;
                                }, (x, y) =>
                                {
                                    var importerX = dict[AssetDatabase.GetAssetPath(x)];
                                    var importerY = dict[AssetDatabase.GetAssetPath(y)];
                                    var overrideX = importerX.GetPlatformTextureSettings(settingTarget);
                                    var overrideY = importerY.GetPlatformTextureSettings(settingTarget);
                                    var isOverrideX = overrideX.overridden ? 1 : 0;
                                    var isOverrideY = overrideY.overridden ? 1 : 0;
                                    var isT2dX = x is Texture2D ? 1 : 0;
                                    var isT2dY = y is Texture2D ? 1 : 0;

                                    var ret = isT2dY.CompareTo(isT2dX);
                                    if (ret != 0)
                                    {
                                        return ret;
                                    }

                                    ret = isOverrideY.CompareTo(isOverrideX);
                                    if (ret != 0)
                                    {
                                        return ret;
                                    }

                                    ret = overrideX.overridden ? overrideY.maxTextureSize.CompareTo(overrideX.maxTextureSize) : importerY.maxTextureSize.CompareTo(importerX.maxTextureSize);
                                    if (ret != 0)
                                    {
                                        return ret;
                                    }

                                    return string.Compare(x.name, y.name, StringComparison.Ordinal);
                                });
                            });
                            menu.AddItem(new GUIContent("See/Texture/Order By Name"), false, () => { ShowDependenciesInProjectBrowser<Texture>(m => AssetImporter.GetAtPath(m)); });
                            menu.AddItem(new GUIContent("See/Texture/Order By TextureFormat"), false, () =>
                            {
                                var dict = new Dictionary<string, TextureImporter>();
                                var settingTarget = m_BuildTarget.ToString();
                                if (settingTarget.StartsWith("Standalone"))
                                {
                                    settingTarget = "Standalone";
                                }

                                ShowDependenciesInProjectBrowser<Texture>(m =>
                                {
                                    var importer = AssetImporter.GetAtPath(m) as TextureImporter;
                                    if (importer == null)
                                    {
                                        return false;
                                    }

                                    dict.Add(m, importer);
                                    return true;
                                }, (x, y) =>
                                {
                                    var importerX = dict[AssetDatabase.GetAssetPath(x)];
                                    var importerY = dict[AssetDatabase.GetAssetPath(y)];
                                    var overrideX = importerX.GetPlatformTextureSettings(settingTarget);
                                    var overrideY = importerY.GetPlatformTextureSettings(settingTarget);
                                    return overrideX.format.CompareTo(overrideY.format);
                                });
                            });

                            menu.AddItem(new GUIContent("See/Material/Order By EnableInstancing"), false, () =>
                            {
                                ShowDependenciesInProjectBrowser<Material>(null, (x, y) =>
                                {
                                    var ix = x.enableInstancing ? 1 : 0;
                                    var iy = y.enableInstancing ? 1 : 0;
                                    return ix == iy ? string.Compare(x.name, y.name, StringComparison.Ordinal) : iy.CompareTo(ix);
                                });
                            });
                            menu.AddItem(new GUIContent("See/Material/Order By Name"), false, () => { ShowDependenciesInProjectBrowser<Material>(); });
                            menu.AddItem(new GUIContent("See/Prefab"), false, () => { ShowDependenciesInProjectBrowser<GameObject>(m => m.EndsWith(".prefab")); });
                            menu.AddItem(new GUIContent("See/Module"), false, () => { ShowDependenciesInProjectBrowser<GameObject>(m => !m.EndsWith(".prefab")); });
                            menu.AddItem(new GUIContent("See/Mesh (Sort By VertexCount)"), false, () => { ShowDependenciesInProjectBrowser<Mesh>(null, (x, y) => y.vertexCount.CompareTo(x.vertexCount)); });
                            menu.AddItem(new GUIContent("See/Mesh in Module(Sort By VertexCount)"), false, () => { ShowDependenciesInProjectBrowser<GameObject, Mesh>(m => !m.EndsWith(".prefab"), (x, y) => y.vertexCount.CompareTo(x.vertexCount)); });
                            menu.AddItem(new GUIContent("See/Shader"), false, () => { ShowDependenciesInProjectBrowser<Shader>(); });
                            menu.AddItem(new GUIContent("See/AnimationClip"), false, () => { ShowDependenciesInProjectBrowser<AnimationClip>(); });
                            menu.AddItem(new GUIContent("See/AudioClip"), false, () => { ShowDependenciesInProjectBrowser<AudioClip>(); });
                            menu.DropDown(rMode);
                        }
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.Space(spaceBetween * 2f);

                    {
                        GUILayout.Space(8);
                        var guiMode = new GUIContent("Advanced");
                        Rect rMode = GUILayoutUtility.GetRect(guiMode, EditorStyles.toolbarDropDown);
                        if (EditorGUI.DropdownButton(rMode, guiMode, FocusType.Passive, EditorStyles.toolbarDropDown))
                        {
                            var menu = new GenericMenu();

                            menu.AddItem(new GUIContent("Hash Length/8", "依赖资源Hash长度"), CurrentProfile.hashLength == 8, () => CurrentProfile.hashLength = 8);
                            menu.AddItem(new GUIContent("Hash Length/16", "依赖资源Hash长度"), CurrentProfile.hashLength == 16, () => CurrentProfile.hashLength = 16);
                            menu.AddItem(new GUIContent("Hash Length/24", "依赖资源Hash长度"), CurrentProfile.hashLength == 24, () => CurrentProfile.hashLength = 24);
                            menu.AddItem(new GUIContent("Hash Length/32", "依赖资源Hash长度"), CurrentProfile.hashLength == 32, () => CurrentProfile.hashLength = 32);
                            menu.AddItem(new GUIContent("Compression/No Compression"), CurrentProfile.compression == AssetBundleProfile.CompressOptions.Uncompressed, () => CurrentProfile.compression = AssetBundleProfile.CompressOptions.Uncompressed);
                            menu.AddItem(new GUIContent("Compression/Standard Compression (LZMA)"), CurrentProfile.compression == AssetBundleProfile.CompressOptions.StandardCompression, () => CurrentProfile.compression = AssetBundleProfile.CompressOptions.StandardCompression);
                            menu.AddItem(new GUIContent("Compression/Chunk Based Compression (LZ4)"), CurrentProfile.compression == AssetBundleProfile.CompressOptions.ChunkBasedCompression, () => CurrentProfile.compression = AssetBundleProfile.CompressOptions.ChunkBasedCompression);

                            menu.AddItem(new GUIContent($"Extension/{CurrentProfile.assetExt}"), true, () => { });
                            menu.AddItem(new GUIContent($"Extension/Change"), false, () => { RenameExtensionWindow.Show(this, CurrentProfile, new Vector2(position.xMax - 200, position.yMin + 60)); });
                            menu.AddItem(new GUIContent("Combine Dependencies As Soon As Possible"), CurrentProfile.lessDependencyBundles, () => CurrentProfile.lessDependencyBundles = !CurrentProfile.lessDependencyBundles);
                            menu.AddItem(new GUIContent("Clear Folder Before Build"), CurrentProfile.clearFolderBeforeBuild, () => CurrentProfile.clearFolderBeforeBuild = !CurrentProfile.clearFolderBeforeBuild);
                            menu.AddItem(new GUIContent("Save Preview File After Build"), CurrentProfile.savePreviewFileAfterBuild, () => CurrentProfile.savePreviewFileAfterBuild = !CurrentProfile.savePreviewFileAfterBuild);


                            menu.DropDown(rMode);
                        }
                    }
                    {
                        var guiBuild = new GUIContent("Build");
                        Rect rBuild = GUILayoutUtility.GetRect(guiBuild, EditorStyles.toolbarDropDown);
                        if (EditorGUI.DropdownButton(rBuild, guiBuild, FocusType.Passive, EditorStyles.toolbarDropDown))
                        {
                            var menu = new GenericMenu();

                            menu.AddItem(new GUIContent("Export Preview/AssetBundle Files"), false, () =>
                            {
                                var fileName = EditorUtility.SaveFilePanel("导出文件", Environment.CurrentDirectory, $"{CurrentProfile.name}_preview_files.txt", "txt");
                                if (!string.IsNullOrEmpty(fileName))
                                {
                                    CurrentProfile.SavePreviewFiles(fileName);
                                    EditorUtility.RevealInFinder(fileName);
                                    // Utils.WindowsCmd($"explorer /select, {Path.GetFullPath(fileName)}");
                                }
                            });
                            menu.AddItem(new GUIContent("Export Preview/AssetBundle Types"), false, () =>
                            {
                                var fileName = EditorUtility.SaveFilePanel("导出文件", Environment.CurrentDirectory, $"{CurrentProfile.name}_preview_types.txt", "txt");
                                if (!string.IsNullOrEmpty(fileName))
                                {
                                    CurrentProfile.SavePreviewTypes(fileName);
                                    EditorUtility.RevealInFinder(fileName);
                                }
                            });

                            menu.AddItem(new GUIContent("Build"), false, () =>
                            {
                                var key = $"{this.GetType().Name}_BuildFolder";
                                var outPath = EditorPrefs.GetString(key) ?? Environment.CurrentDirectory;
                                outPath = EditorUtility.SaveFolderPanel("目标目录", outPath, null);
                                if (!string.IsNullOrEmpty(outPath))
                                {
                                    EditorPrefs.SetString(key, outPath);
                                    var ok = CurrentProfile.Build(outPath, m_BuildTarget);
                                    if (ok)
                                    {
                                        EditorUtility.DisplayDialog(CurrentProfile.name, "Build Success", "确定");
                                    }
                                    else
                                    {
                                        EditorUtility.DisplayDialog(CurrentProfile.name, "Build Failed", "确定");
                                    }
                                }
                            });
                            menu.AddItem(new GUIContent("ForceBuild"), false, () =>
                            {
                                var outPath = EditorUtility.SaveFolderPanel("目标目录", Environment.CurrentDirectory, null);
                                if (!string.IsNullOrEmpty(outPath))
                                {
                                    var manifest = CurrentProfile.Build(outPath, m_BuildTarget, true);
                                    if (manifest)
                                    {
                                        EditorUtility.DisplayDialog(CurrentProfile.name, "Build Success", "确定");
                                    }
                                }
                            });
                            menu.AddItem(new GUIContent("Clean AssetBundle Name"), false, AssetBundleProfile.CleanAssetBundleNames);
                            menu.DropDown(rBuild);
                        }
                    }

                    GUILayout.Space(4);
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}