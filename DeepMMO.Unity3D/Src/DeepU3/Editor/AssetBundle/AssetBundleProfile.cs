using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using DeepU3.AssetBundles;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace DeepU3.Editor.AssetBundle
{
    public class AssetBundleProfile : ScriptableObject
    {
        [Serializable]
        public class AssetItem
        {
            public string assetPath;
            public string abName;
            public bool isCombine;
            public List<string> ignoredTypesFullName;

            [NonSerialized]
            private HashSet<Type> mIgnoredTypes;

            public HashSet<Type> IgnoredTypes
            {
                get
                {
                    if (ignoredTypesFullName == null)
                    {
                        return null;
                    }

                    if (mIgnoredTypes != null)
                    {
                        return mIgnoredTypes;
                    }

                    mIgnoredTypes = new HashSet<Type>();
                    foreach (var fullName in ignoredTypesFullName)
                    {
                        mIgnoredTypes.Add(EditorUtils.GetUnityObjectTypeByFullName(fullName));
                    }

                    return mIgnoredTypes;
                }
            }

            internal void SetTypeDirty()
            {
                mIgnoredTypes = null;
            }

            public bool IsAllowType(Type type)
            {
                return IgnoredTypes == null || !IgnoredTypes.Any(t => t.IsAssignableFrom(type));
            }
        }


        private class ResolveItem : IComparable<ResolveItem>
        {
            public string AssetPath { get; }
            public string AssetBundleName { get; }
            public Type AssetType { get; }

            public bool IsSceneAsset => typeof(SceneAsset).IsAssignableFrom(AssetType);

            public readonly AssetItem RootAssetItem;

            public override string ToString()
            {
                return $"t:{AssetType.Name} {AssetBundleName} {AssetPath}";
            }

            public string ToString(string ext)
            {
                return $"t:{AssetType.Name} {AssetBundleName}{ext} {AssetPath}";
            }

            public ResolveItem(string assetPath, string abName, Type assetType)
            {
                AssetPath = assetPath;
                AssetBundleName = abName;
                AssetType = assetType;
            }

            public ResolveItem(AssetNode node) : this(node.AssetPath, node.AssetBundleName, node.AssetType)
            {
                RootAssetItem = node.RootAssetItem;
            }

            public int CompareTo(ResolveItem other)
            {
                if (AssetBundleName == other.AssetBundleName)
                {
                    var xContains = AssetBundleName.Contains(Path.GetFileNameWithoutExtension(AssetPath).ToLower()) ? -1 : 1;
                    var yContains = AssetBundleName.Contains(Path.GetFileNameWithoutExtension(other.AssetPath).ToLower()) ? -1 : 1;
                    return xContains.CompareTo(yContains);
                }

                return string.Compare(AssetBundleName, other.AssetBundleName, StringComparison.Ordinal);
            }
        }

        private class TopResolveItem : ResolveItem
        {
            public bool IsCombine { get; }

            public TopResolveItem(AssetNode node) : base(node)
            {
                IsCombine = node.RootAssetItem.isCombine;
            }
        }

        public class AssetNode : IComparable<AssetNode>
        {
            public AssetNode Parent { get; private set; }
            public string AssetPath { get; }
            public string AssetBundleName { get; }

            private bool _isIgnore;
            private bool _isCompleteAsset;
            private bool _isBuiltIn;

            public bool IsIgnore
            {
                get => _isIgnore;
                set
                {
                    _isIgnore = value;
                    foreach (var assetNode in Children)
                    {
                        assetNode.IsIgnore = value;
                    }
                }
            }

            public bool IsRootAsset => RootAssetItem?.assetPath == AssetPath;

            public bool IsCompleteAsset
            {
                get => _isCompleteAsset;
                set
                {
                    _isCompleteAsset = value;
                    foreach (var assetNode in Children)
                    {
                        assetNode.IsCompleteAsset = value;
                    }
                }
            }

            public bool IsBuiltIn
            {
                get => _isBuiltIn;
                set
                {
                    _isBuiltIn = value;
                    foreach (var assetNode in Children)
                    {
                        assetNode.IsBuiltIn = value;
                    }
                }
            }


            public bool IsFolder => !string.IsNullOrEmpty(AssetPath) && AssetType == null;
            public Type AssetType { get; }
            private readonly List<AssetNode> _children = new List<AssetNode>();
            public int TotalCount => _children.Sum(item => item.TotalCount) + (string.IsNullOrEmpty(AssetPath) ? 0 : 1);

            public bool HasChildren => _children.Count > 0;
            internal readonly AssetItem RootAssetItem;

            public int Depth
            {
                get
                {
                    var ret = -1;
                    var p = Parent;
                    while (p != null)
                    {
                        ret++;
                        p = p.Parent;
                    }

                    return ret;
                }
            }

            public int CompareTo(AssetNode other)
            {
                if (Depth != other.Depth)
                {
                    return Depth.CompareTo(other.Depth);
                }

                if (IsFolder && !other.IsFolder)
                {
                    return -1;
                }

                if (other.IsFolder && !IsFolder)
                {
                    return 1;
                }

                return string.Compare(AssetBundleName, other.AssetBundleName, StringComparison.Ordinal);
            }

            public override string ToString()
            {
                return $"{AssetBundleName}<=>{AssetPath}";
            }

            public AssetNode(AssetItem rootAssetItem, string assetPath, string abName, Type assetType = null)
            {
                RootAssetItem = rootAssetItem;
                AssetPath = assetPath;
                AssetBundleName = abName;
                AssetType = assetType;
            }

            public void AddChild(AssetNode node)
            {
                _children.Add(node);
                node.Parent = this;
            }

            public void RemoveChild(AssetNode node)
            {
                _children.Remove(node);
                node.Parent = null;
            }


            public ReadOnlyCollection<AssetNode> Children => new ReadOnlyCollection<AssetNode>(_children);


            public void ListAllNodes(List<AssetNode> nodes)
            {
                nodes.AddRange(_children);
                foreach (var child in _children)
                {
                    child.ListAllNodes(nodes);
                }
            }

            public HashSet<Type> GetAllTypes()
            {
                var nodes = new List<AssetNode>();
                ListAllNodes(nodes);
                var types = new HashSet<Type>();
                nodes.ForEach(m =>
                {
                    if (m.AssetType != null)
                    {
                        types.Add(m.AssetType);
                    }
                });
                return types;
            }


            public void ListAllResolvableNodes(List<AssetNode> items)
            {
                if (IsIgnore)
                {
                    return;
                }

                if (HasChildren)
                {
                    foreach (var child in _children)
                    {
                        child.ListAllResolvableNodes(items);
                    }
                }
                else if (!string.IsNullOrEmpty(AssetPath))
                {
                    items.Add(this);
                }
            }

            internal void CollectDependencies(ICollection<string> sharedDependencies, Dictionary<string, string> rootRefs)
            {
                if (IsIgnore)
                {
                    return;
                }

                if (HasChildren)
                {
                    foreach (var child in _children)
                    {
                        child.CollectDependencies(sharedDependencies, rootRefs);
                    }
                }
                else
                {
                    CollectDependencies(AssetPath, AssetPath, sharedDependencies, rootRefs);
                }
            }

            internal static void CollectDependencies(string rootAssetPath, string assetPath, ICollection<string> sharedDependencies, Dictionary<string, string> rootRefs)
            {
                var directDependencies = AssetDatabase.GetDependencies(assetPath, false);
                foreach (var directDependency in directDependencies)
                {
                    if (directDependency == rootAssetPath)
                    {
                        continue;
                    }

                    if (sharedDependencies.Contains(directDependency))
                    {
                        rootRefs.Remove(directDependency);
                        continue;
                    }

                    var t = AssetDatabase.GetMainAssetTypeAtPath(directDependency);
                    if (!IsAllowAssetType(t))
                    {
                        continue;
                    }

                    if (IsSceneAssetType(t))
                    {
                        Debug.LogError($"{rootAssetPath}:\n {assetPath} has scene directDependency {directDependency}");
                    }

                    sharedDependencies.Add(directDependency);
                    rootRefs.Add(directDependency, assetPath);
                    CollectDependencies(rootAssetPath, directDependency, sharedDependencies, rootRefs);
                }
            }
        }


        [SerializeField]
        public int hashLength = 8;

        [SerializeField]
        public bool clearFolderBeforeBuild = false;

        [SerializeField]
        public string assetExt = ".ab";


        /// <summary>
        /// 尽可能减少依赖的ab包数量
        /// </summary>
        [SerializeField]
        public bool lessDependencyBundles;

        [SerializeField]
        public bool savePreviewFileAfterBuild;

        public enum CompressOptions
        {
            Uncompressed = 0,
            StandardCompression,
            ChunkBasedCompression,
        }
        
        [SerializeField]
        public CompressOptions compression = CompressOptions.StandardCompression;

        [SerializeField]
        public List<AssetItem> containsFolder = new List<AssetItem>();

        [SerializeField]
        public List<string> ignoreAssetsPath = new List<string>();

        [SerializeField]
        public List<AssetItem> invalidDependencyFolder = new List<AssetItem>();


        [SerializeField]
        public List<AssetItem> allInOneAssetsPath = new List<AssetItem>();

        private AssetNode _cacheAssetNode;

        public AssetNode RootAssetNode => _cacheAssetNode ?? (_cacheAssetNode = BuildResolveTree(containsFolder, ignoreAssetsPath));


        private AssetNode _cacheBuiltInAssetNode;

        public AssetNode RootBuiltInAssetNode
        {
            get
            {
                var node = _cacheBuiltInAssetNode ?? (_cacheBuiltInAssetNode = BuildResolveTree(invalidDependencyFolder, new List<string>()));
                node.IsBuiltIn = true;
                return node;
            }
        }

        public AssetNode RootCompleteAssetNode
        {
            get
            {
                var node = BuildResolveTree(allInOneAssetsPath, new List<string>());
                node.IsCompleteAsset = true;
                return node;
            }
        }

        public string ProfileManifestPath => EditorUtils.PathToAssetPath($"{Path.GetDirectoryName(AssetDatabase.GetAssetPath(this))}/{name}_Manifest.asset");

        private AssetNode BuildResolveTree(ICollection<AssetItem> collection, ICollection<string> ignorePaths)
        {
            var watch = Stopwatch.StartNew();
            var allPaths = AssetDatabase.GetAllAssetPaths();
            var hashPaths = new HashSet<string>();
            Array.ForEach(allPaths, s => hashPaths.Add(s));
            var root = new AssetNode(null, null, null);
            var depthPath = new Stack<string>();
            foreach (var item in collection)
            {
                if (File.Exists(item.assetPath))
                {
                    var t = AssetDatabase.GetMainAssetTypeAtPath(item.assetPath);
                    var assetNode = new AssetNode(item, item.assetPath, item.abName, t);
                    root.AddChild(assetNode);
                    assetNode.IsIgnore = ignorePaths.Contains(item.assetPath);
                }
                else
                {
                    var assetNode = BuildResolveTree(item, new DirectoryInfo(item.assetPath), hashPaths, ignorePaths, depthPath);
                    if (assetNode != null)
                    {
                        root.AddChild(assetNode);
                    }
                }
            }

            var allAssetNodes = new List<AssetNode>();

            root.ListAllNodes(allAssetNodes);
            //去除depth较深的节点
            allAssetNodes.Sort();

            var mapping = new Dictionary<string, AssetNode>();
            foreach (var node in allAssetNodes)
            {
                if (mapping.ContainsKey(node.AssetPath))
                {
                    node.Parent.RemoveChild(node);
                }
                else
                {
                    mapping.Add(node.AssetPath, node);
                }
            }

            // Debug.Log($"Resolve AssetsTree : {watch.ElapsedMilliseconds}ms");
            return root;
        }

        private static string GetAssetKey(AssetItem rootItem, bool isFolder, string assetPath, Stack<string> depthPath)
        {
            if (rootItem.isCombine || assetPath == rootItem.assetPath)
            {
                return rootItem.abName;
            }

            if (string.IsNullOrEmpty(rootItem.abName))
            {
                return null;
            }

            if (isFolder)
            {
                return string.Join("/", depthPath.Reverse());
            }

            var assetName = Path.GetFileNameWithoutExtension(assetPath);
            depthPath.Push(assetName.ToLower());
            var ret = string.Join("/", depthPath.Reverse());
            depthPath.Pop();
            return ret;
        }

        private AssetNode BuildResolveTree(AssetItem rootItem, DirectoryInfo d, ICollection<string> checkContains, ICollection<string> ignorePaths, Stack<string> depthPath)
        {
            if (!d.Exists)
            {
                return null;
            }

            var assetFolder = EditorUtils.PathToAssetPath(d.FullName);

            if (depthPath.Count == 0)
            {
                depthPath.Push(rootItem.abName);
            }
            else if (!rootItem.isCombine)
            {
                depthPath.Push(d.Name.ToLower());
            }
            else
            {
                depthPath.Push("");
            }

            var abName = GetAssetKey(rootItem, true, assetFolder, depthPath);
            var tree = new AssetNode(rootItem, assetFolder, abName);

            foreach (var sub in d.GetDirectories())
            {
                var subTree = BuildResolveTree(rootItem, sub, checkContains, ignorePaths, depthPath);
                if (subTree != null)
                {
                    tree.AddChild(subTree);
                }
            }


            foreach (var fileInfo in d.GetFiles())
            {
                var assetPath = EditorUtils.PathToAssetPath(fileInfo.FullName);
                if (!checkContains.Contains(assetPath))
                {
                    continue;
                }

                var t = EditorUtils.GetMainAssetTypeAtPath(assetPath);

                if (IgnoreTypes.Contains(t))
                {
                    continue;
                }

                if (!rootItem.IsAllowType(t))
                {
                    continue;
                }

                var nextABName = GetAssetKey(rootItem, false, assetPath, depthPath);
                var node = new AssetNode(rootItem, assetPath, nextABName, t);
                tree.AddChild(node);
                node.IsIgnore = ignorePaths.Contains(assetPath);
            }

            depthPath.Pop();

            if (ignorePaths.Contains(assetFolder))
            {
                tree.IsIgnore = true;
            }

            return tree.HasChildren ? tree : null;
        }

        public void SetCombine(string assetPath, bool combine)
        {
            var info = containsFolder.Find(m => m.assetPath == assetPath);
            if (info != null)
            {
                info.isCombine = combine;
                SetDirty(true, false);
            }
        }

        public bool IsCombine(string assetPath)
        {
            var info = containsFolder.Find(m => m.assetPath == assetPath);
            return info?.isCombine ?? false;
        }

        public void SetInvalidDependency(string assetFolder, bool val)
        {
            var info = invalidDependencyFolder.Find(m => m.assetPath == assetFolder);
            if (!val && info != null)
            {
                invalidDependencyFolder.Remove(info);
                SetDirty(false, true);
            }
            else if (val && info == null)
            {
                invalidDependencyFolder.Add(new AssetItem {assetPath = assetFolder});
                SetDirty(false, true);
            }
        }

        public static bool IsAllowAssetType(Type t)
        {
            if (t == null || IgnoreTypes.Contains(t))
            {
                return false;
            }

            return true;
        }

        public static bool IsSceneAssetType(Type t) => typeof(SceneAsset).IsAssignableFrom(t);

        public bool IsAllowAssetPath(string assetPath)
        {
            var t = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            return IsAllowAssetPath(assetPath, t);
        }

        public bool IsAllowAssetPath(string assetPath, Type t)
        {
            if (!IsAllowAssetType(t) || IsBuiltIn(assetPath))
            {
                return false;
            }

            return true;
        }

        public HashSet<Type> GetIgnoredTypes(string assetPath)
        {
            var info = containsFolder.Find(m => m.assetPath == assetPath);
            return info?.IgnoredTypes;
        }

        public void SetIgnoredType(string assetPath, Type t, bool ignore)
        {
            var info = containsFolder.Find(m => m.assetPath == assetPath);
            if (info == null)
            {
                return;
            }

            if (info.ignoredTypesFullName == null)
            {
                info.ignoredTypesFullName = new List<string>();
            }

            info.SetTypeDirty();
            if (ignore)
            {
                if (!info.ignoredTypesFullName.Contains(t.FullName))
                {
                    info.ignoredTypesFullName.Add(t.FullName);
                }
            }
            else
            {
                info.ignoredTypesFullName.Remove(t.FullName);
            }

            SetDirty(true, false);
        }

        public bool SetAssetItem(string assetPath, string abName)
        {
            if (!IsAllowAssetType(EditorUtils.GetMainAssetTypeAtPath(assetPath)))
            {
                return false;
            }

            var info = containsFolder.Find(m => m.assetPath == assetPath);
            if (info != null)
            {
                if (abName == null)
                {
                    containsFolder.Remove(info);
                }
                else
                {
                    info.abName = abName.ToLower();
                }
            }
            else if (abName != null)
            {
                containsFolder.Add(new AssetItem {assetPath = assetPath, abName = abName.ToLower()});
            }


            SetDirty(true, false);
            return true;
        }

        public void RemoveAssetItem(string assetFolder)
        {
            containsFolder.RemoveAll(m => m.assetPath == assetFolder);
            ignoreAssetsPath.RemoveAll(m => m.StartsWith(assetFolder));
            SetDirty(true, false);
        }

        public void SetCompleteAssetItem(string assetPath, string abName)
        {
            var info = allInOneAssetsPath.Find(m => m.assetPath == assetPath);
            if (info != null)
            {
                if (abName == null)
                {
                    allInOneAssetsPath.Remove(info);
                }
                else
                {
                    info.abName = abName.ToLower();
                }
            }
            else if (abName != null)
            {
                allInOneAssetsPath.Add(new AssetItem {assetPath = assetPath, abName = abName.ToLower()});
            }
        }

        public bool IsResolveItemDirty => _cacheAssetNode == null;
        public bool IsBuildInDataDirty => _cacheBuiltInAssetNode == null;

        public void SetDirty(bool asset, bool builtIn)
        {
            if (asset)
            {
                _cacheAssetNode = null;
            }

            if (builtIn)
            {
                _cacheBuiltInAssetNode = null;
            }

            if (asset || builtIn)
            {
                Sort();
                EditorUtility.SetDirty(this);
            }
        }


        public void SetIgnore(string assetPath, bool ignore)
        {
            var index = ignoreAssetsPath.IndexOf(assetPath);
            if (index >= 0)
            {
                if (ignore)
                {
                    return;
                }

                ignoreAssetsPath.RemoveAt(index);
                SetDirty(false, false);
            }
            else
            {
                if (ignore)
                {
                    ignoreAssetsPath.Add(assetPath);

                    SetDirty(false, false);
                }
            }

            var allAssetNodes = new List<AssetNode>();
            RootAssetNode.ListAllNodes(allAssetNodes);
            var node = allAssetNodes.FirstOrDefault(m => m.AssetPath == assetPath);
            if (node != null)
            {
                node.IsIgnore = ignore;
            }
        }

        private static bool IsIgnore(string assetPath, ICollection<string> ignoreAssetsPath)
        {
            var arr = assetPath.Split('/').ToList();
            while (arr.Count > 1)
            {
                if (ignoreAssetsPath.Contains(string.Join("/", arr)))
                {
                    return true;
                }

                arr.RemoveAt(arr.Count - 1);
            }

            return false;
        }

        public bool IsIgnore(string assetPath) => IsIgnore(assetPath, ignoreAssetsPath);

        public bool IsBuiltIn(string assetPath)
        {
            //todo builtInAssetsPath 缓存
            var builtInAssetsPath = new HashSet<string>();

            foreach (var s in invalidDependencyFolder.Select(m => m.assetPath))
            {
                builtInAssetsPath.Add(s);
            }

            return IsIgnore(assetPath, builtInAssetsPath);
        }

        private void OnEnable()
        {
            Sort();
        }

        private void Sort()
        {
            containsFolder.Sort((x, y) =>
            {
                var xFolder = Directory.Exists(x.assetPath) ? 0 : 1;
                var yFolder = Directory.Exists(y.assetPath) ? 0 : 1;
                return xFolder == yFolder ? string.Compare(x.assetPath, y.assetPath, StringComparison.Ordinal) : xFolder.CompareTo(yFolder);
            });
        }

        public static readonly HashSet<Type> IgnoreTypes = new HashSet<Type>
        {
            typeof(MonoScript),
            typeof(LightmapParameters),
            typeof(AssemblyDefinitionAsset),
        };

        private List<ResolveItem> _cacheResolveItems;
        private int _cacheResolveDependStartIndex;

        private List<ResolveItem> CollectResolveItem(out int dependStartIndex, List<SceneDynamicReference> sceneRefs = null)
        {
            if (_cacheResolveItems != null && !IsResolveItemDirty && !IsBuildInDataDirty)
            {
                dependStartIndex = _cacheResolveDependStartIndex;
                return _cacheResolveItems;
            }

            EditorUtility.DisplayProgressBar("Hold", "ResolveItems", 0.5f);
            try
            {
                var nodes = new List<AssetNode>();

                RootAssetNode.ListAllResolvableNodes(nodes);

                //生成缓存,用于Dirty判断
                var buildInRoot = RootBuiltInAssetNode;
                nodes.Sort();
                _cacheResolveItems = nodes.Where(node => !node.IsIgnore).Select(node => node.IsRootAsset ? new TopResolveItem(node) : new ResolveItem(node)).ToList();

                dependStartIndex = _cacheResolveDependStartIndex = _cacheResolveItems.Count;
                var builtInAssetsPath = new HashSet<string>();

                foreach (var assetPath in invalidDependencyFolder.Select(m => m.assetPath))
                {
                    builtInAssetsPath.Add(assetPath);
                }

                var sharedDependencies = _cacheResolveItems.Select(item => item.AssetPath).ToList();

                //只被引用一次的依赖[依赖项路径:资源路径]
                var rootRefs = new Dictionary<string, string>();
                var guidDependIndex = sharedDependencies.Count;
                RootAssetNode.CollectDependencies(sharedDependencies, rootRefs);
                //场景动态依赖
                foreach (var resolveItem in _cacheResolveItems)
                {
                    if (!typeof(SceneAsset).IsAssignableFrom(resolveItem.AssetType))
                    {
                        continue;
                    }

                    var sceneRef = SceneDynamicReference.GetSceneDynamicReferenceByScenePath(resolveItem.AssetPath);
                    if (sceneRef)
                    {
                        var directDependencies = AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(sceneRef), false);
                        foreach (var dependency in directDependencies)
                        {
                            if (!sharedDependencies.Contains(dependency))
                            {
                                sharedDependencies.Add(dependency);
                            }

                            AssetNode.CollectDependencies(dependency, dependency, sharedDependencies, rootRefs);
                        }

                        sceneRefs?.Add(sceneRef);
                    }
                }

                for (var i = sharedDependencies.Count - 1; i >= guidDependIndex; i--)
                {
                    var dependency = sharedDependencies[i];
                    var t = EditorUtils.GetMainAssetTypeAtPath(dependency);
                    if (!IsIgnore(dependency, builtInAssetsPath) && IsAllowAssetType(t))
                    {
                        continue;
                    }

                    rootRefs.Remove(dependency);
                    sharedDependencies.RemoveAt(i);
                }


                for (var i = guidDependIndex; i < sharedDependencies.Count; i++)
                {
                    var dependency = sharedDependencies[i];
                    string abName;
                    var t = EditorUtils.GetMainAssetTypeAtPath(dependency);
                    if (IsSceneAssetType(t))
                    {
                        abName = Path.GetFileNameWithoutExtension(dependency);
                    }
                    else if (rootRefs.TryGetValue(dependency, out var hashAssetPath))
                    {
                        var first = _cacheResolveItems.First(m => m.AssetPath == hashAssetPath);
                        //dependency只被first依赖
                        if (first != null && !first.IsSceneAsset && (lessDependencyBundles || (first is TopResolveItem topResolveItem && topResolveItem.IsCombine)))
                        {
                            abName = first.AssetBundleName;
                        }
                        else
                        {
                            abName = AssetDatabase.AssetPathToGUID(dependency).Substring(0, hashLength);
                        }
                    }
                    else
                    {
                        abName = AssetDatabase.AssetPathToGUID(dependency).Substring(0, hashLength);
                    }

                    var item = new ResolveItem(dependency, abName, t);
                    _cacheResolveItems.Add(item);
                }


                return _cacheResolveItems;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        public void SavePreviewFiles(string filePath, AssetBundleBuild[] builds, List<AssetBundleProfileManifest.AssetPair> assetPairs)
        {
            try
            {
                var asset2Build = new Dictionary<string, AssetBundleBuild>();
                foreach (var item in builds)
                {
                    foreach (var assetName in item.assetNames)
                    {
                        asset2Build.Add(assetName, item);
                    }
                }

                var dictLeader = new Dictionary<string, bool>();
                foreach (var assetPair in assetPairs)
                {
                    if (assetPair.isLeader)
                    {
                        dictLeader.Add(assetPair.assetPath, true);
                    }
                }

                using (var f = new StreamWriter(filePath))
                {
                    var nodes = new List<AssetNode>();
                    RootCompleteAssetNode.ListAllResolvableNodes(nodes);
                    var resolveItems = nodes.Select(n => new ResolveItem(n));
                    foreach (var item in resolveItems)
                    {
                        var leaderStr = "[leader]";
                        f.WriteLine($"+ {item.AssetBundleName}{assetExt}");
                        f.WriteLine($"\tAssets:");
                        f.WriteLine($"\t- {item.AssetPath} {leaderStr}");
                    }

                    // var str = new StringBuilder();
                    for (var i = 0; i < builds.Length; i++)
                    {
                        var item = builds[i];
                        EditorUtility.DisplayProgressBar("SavePreviewFiles", item.assetBundleName, (float) (i + 1) / builds.Length);

                        f.WriteLine($"+ {item.assetBundleName} {item.assetBundleVariant}");
                        f.WriteLine($"\tAssets:");
                        foreach (var assetName in item.assetNames)
                        {
                            var leaderStr = dictLeader.ContainsKey(assetName) ? "[leader]" : "";
                            f.WriteLine($"\t- {assetName} {leaderStr}");
                        }

                        var hashDependencies = new HashSet<string>();
                        foreach (var assetName in item.assetNames)
                        {
                            var all = AssetDatabase.GetDependencies(assetName, true);
                            foreach (var s in all)
                            {
                                if (asset2Build.TryGetValue(s, out var build) && build.assetBundleName != item.assetBundleName)
                                {
                                    hashDependencies.Add(build.assetBundleName + " <==> " + s);
                                }
                            }
                        }

                        if (hashDependencies.Count > 0)
                        {
                            f.WriteLine($"\tDependencies: ");
                            foreach (var dependency in hashDependencies)
                            {
                                f.WriteLine($"\t- {dependency}");
                            }
                        }
                    }
                }

                // File.WriteAllText(filePath, str.ToString());
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.DeleteAsset(ProfileManifestPath);
            }
        }

        public void SavePreviewFiles(string filePath)
        {
            var builds = Resolve(out var pairs);
            SavePreviewFiles(filePath, builds, pairs);
        }

        public void SavePreviewTypes(string filePath)
        {
            var builds = Resolve(out var assetPairs);
            EditorUtility.DisplayProgressBar("Hold", "SavePreviewTypes", 0.5f);
            try
            {
                var types = new HashSet<Type>();
                foreach (var item in builds)
                {
                    foreach (var assetName in item.assetNames)
                    {
                        var t = EditorUtils.GetMainAssetTypeAtPath(assetName);
                        types.Add(t);
                    }
                }

                var str = string.Join("\n", types);
                File.WriteAllText(filePath, str);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.DeleteAsset(ProfileManifestPath);
            }
        }

        public static void CleanAssetBundleNames()
        {
            try
            {
                var names = AssetDatabase.GetAllAssetBundleNames();

                for (var i = 0; i < names.Length; i++)
                {
                    var item = names[i];

                    if (EditorUtility.DisplayCancelableProgressBar("Clean", item, (float) (i + 1) / names.Length))
                    {
                        break;
                    }

                    AssetDatabase.RemoveAssetBundleName(item, true);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private AssetBundleBuild[] Resolve(out List<AssetBundleProfileManifest.AssetPair> assetPairs)
        {
            SetDirty(true, true);
            var sceneRefs = new List<SceneDynamicReference>();

            var resolveItems = CollectResolveItem(out var dependStartIndex, sceneRefs);

            var resolveMap = resolveItems.ToDictionary(m => m.AssetPath);
            var buildMap = new Dictionary<string, List<string>>();
            assetPairs = new List<AssetBundleProfileManifest.AssetPair>();
            var namedAssetBundles = new HashSet<string>();
            for (var i = 0; i < dependStartIndex; i++)
            {
                namedAssetBundles.Add(resolveItems[i].AssetBundleName + assetExt);
            }

            // var scenePairs = new List<AssetBundleProfileManifest.ScenePair>();
            for (var i = 0; i < resolveItems.Count; i++)
            {
                var item = resolveItems[i];
                var abName = item.AssetBundleName + assetExt;
                if (!buildMap.TryGetValue(abName, out var assetList))
                {
                    assetList = new List<string>();
                    buildMap.Add(abName, assetList);
                }

                assetList.Add(item.AssetPath);
                var pair = new AssetBundleProfileManifest.AssetPair(item.AssetPath, abName, item.AssetType.Name);
                if ((i < dependStartIndex && item.RootAssetItem != null && (!item.RootAssetItem.isCombine || item.RootAssetItem.assetPath == item.AssetPath)) ||
                    AssetDatabase.AssetPathToGUID(item.AssetPath).Contains(item.AssetBundleName))
                {
                    pair.isLeader = true;
                }

                assetPairs.Add(pair);
            }


            var ignoreDependenciesAssetPairs = new List<AssetBundleProfileManifest.IgnoreDependenciesAssetPair>();
            foreach (var sceneRef in sceneRefs)
            {
                if (sceneRef.lightmapTextures.Count > 0)
                {
                    ignoreDependenciesAssetPairs.Add(new AssetBundleProfileManifest.IgnoreDependenciesAssetPair
                    {
                        parentBundleName = resolveMap[AssetDatabase.GetAssetPath(sceneRef.scene)].AssetBundleName + assetExt,
                        ignoreBundleNames = sceneRef.lightmapTextures.Where(m => m).Select(AssetDatabase.GetAssetPath).Select(p => resolveMap[p].AssetBundleName + assetExt).ToArray()
                    });
                }
            }

            // profile manifest
            var manifest = CreateInstance<AssetBundleProfileManifest>();
            manifest.assetPairs = assetPairs.ToArray();
            manifest.ignoreDependenciesAssetPairs = ignoreDependenciesAssetPairs.ToArray();
            // manifest.scenePairs = scenePairs.ToArray();
            manifest.hashLength = hashLength;
            manifest.fileExtension = assetExt;

            AssetDatabase.CreateAsset(manifest, ProfileManifestPath);
            buildMap.Add($"{nameof(AssetBundleProfileManifest).ToLower()}", new List<string> {ProfileManifestPath});
            var builds = new AssetBundleBuild[buildMap.Count];
            var keys = buildMap.Keys.ToArray();
            for (var i = 0; i < keys.Length; i++)
            {
                var k = keys[i];
                var v = buildMap[k];
                builds[i] = new AssetBundleBuild {assetBundleName = k, assetNames = v.ToArray()};
            }

            Array.Sort(builds, (x, y) =>
            {
                var isDepX = namedAssetBundles.Contains(x.assetBundleName) ? 0 : 1;
                var isDepY = namedAssetBundles.Contains(y.assetBundleName) ? 0 : 1;
                return isDepX == isDepY ? string.CompareOrdinal(x.assetBundleName, y.assetBundleName) : isDepX.CompareTo(isDepY);
            });

            return builds;
        }

        private const BuildAssetBundleOptions BUILD_OPT_ALL_IN_ONE =
            BuildAssetBundleOptions.CompleteAssets |
            BuildAssetBundleOptions.CollectDependencies |
            BuildAssetBundleOptions.DeterministicAssetBundle;

        public string[] FindDependencies(Type t)
        {
            var resolveItems = CollectResolveItem(out _);
            var ret = new HashSet<string>();
            foreach (var item in resolveItems)
            {
                if (t.IsAssignableFrom(item.AssetType))
                {
                    ret.Add(item.AssetPath);
                }
            }

            //self contained todo 
            var nodes = new List<AssetNode>();
            RootCompleteAssetNode.ListAllResolvableNodes(nodes);
            foreach (var assetNode in nodes)
            {
                if (t.IsAssignableFrom(assetNode.AssetType))
                {
                    ret.Add(assetNode.AssetPath);
                }
            }

            return ret.ToArray();
        }

        public bool Build(string outPath, BuildTarget bt, bool forceBuild = false)
        {
            var opt = BuildAssetBundleOptions.DisableLoadAssetByFileNameWithExtension;
            if (forceBuild)
            {
                opt |= BuildAssetBundleOptions.ForceRebuildAssetBundle;
            }
            if (compression == CompressOptions.Uncompressed)
            {
                opt |= BuildAssetBundleOptions.UncompressedAssetBundle;
            }
            else if (compression == CompressOptions.ChunkBasedCompression)
            {
                opt |= BuildAssetBundleOptions.ChunkBasedCompression;
            }
            
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();

            var builds = Resolve(out var assetPairs);

            if (builds == null)
            {
                return false;
            }

            if (builds.Length > 0)
            {
                var sourceManifest = $"{outPath}/{Path.GetFileName(outPath)}";
                // outPath = $"{outPath}/{bt.ToString().ToLower()}";
                if (!Directory.Exists(outPath))
                {
                    Directory.CreateDirectory(outPath);
                }

                if (File.Exists(sourceManifest))
                {
                    File.Delete(sourceManifest);
                }

                if (File.Exists(sourceManifest + ".manifest"))
                {
                    File.Delete(sourceManifest + ".manifest");
                }

                var bundleManifest = BuildPipeline.BuildAssetBundles(outPath, builds, opt, bt);
                if (bundleManifest == null)
                {
                    Debug.LogError("Error in build");
                    return false;
                }

                AssetDatabase.DeleteAsset(ProfileManifestPath);


                //移除不需要的ab
                if (clearFolderBeforeBuild)
                {
                    var outDirectoryInfo = new DirectoryInfo(outPath);
                    var filesMap = EditorUtils.ListAllFiles(outDirectoryInfo).ToDictionary(f => f.FullName);
                    foreach (var bundle in bundleManifest.GetAllAssetBundles())
                    {
                        var fullPath = Path.GetFullPath($"{outPath}/{bundle}");
                        filesMap.Remove(fullPath);
                        filesMap.Remove($"{fullPath}.manifest");
                    }

                    filesMap.Remove(Path.GetFullPath(sourceManifest));
                    filesMap.Remove($"{Path.GetFullPath(sourceManifest)}.manifest");

                    foreach (var fileInfo in filesMap)
                    {
                        fileInfo.Value.Delete();
                    }

                    var allSub = EditorUtils.ListAllDirectories(outDirectoryInfo);
                    allSub.Sort((x, y) => y.FullName.Length.CompareTo(x.FullName.Length));
                    //删除空的目录
                    foreach (var sub in allSub)
                    {
                        if (EditorUtils.ListAllFiles(sub).Count == 0 && sub.Exists)
                        {
                            sub.Delete(true);
                        }
                    }
                }

                var manifestFileName = $"{outPath}/{bt.ToString().ToLower()}";
                if (File.Exists(manifestFileName))
                {
                    File.Delete(manifestFileName);
                    File.Delete($"{manifestFileName}.manifest");
                }

                File.Move($"{sourceManifest}", manifestFileName);
                File.Move($"{sourceManifest}.manifest", $"{manifestFileName}.manifest");
            }

            //单打资源
            var nodes = new List<AssetNode>();
            RootCompleteAssetNode.ListAllResolvableNodes(nodes);
            var resolveItems = nodes.Select(n => new ResolveItem(n));
            foreach (var s in resolveItems)
            {
                if (s.IsSceneAsset)
                {
                    BuildPipeline.BuildStreamedSceneAssetBundle(new string[] {s.AssetPath}, $"{outPath}/{s.AssetBundleName}{assetExt}", bt, BuildOptions.BuildAdditionalStreamedScenes);
                }
                else
                {
                    BuildPipeline.PushAssetDependencies();
                    var asset = AssetDatabase.LoadMainAssetAtPath(s.AssetPath);
                    BuildPipeline.BuildAssetBundle(asset, new[] {asset}, $"{outPath}/{s.AssetBundleName}{assetExt}", BUILD_OPT_ALL_IN_ONE, bt);
                    BuildPipeline.PopAssetDependencies();
                }
            }

            if (savePreviewFileAfterBuild)
            {
                SavePreviewFiles($"{outPath}/{name.ToLower()}_preview_files.txt", builds, assetPairs);
            }

            return true;
        }
    }
}