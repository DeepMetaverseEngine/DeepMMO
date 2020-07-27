using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeepU3.AssetBundles
{
    [ExecuteAlways]
    public class AssetBundleTester : MonoBehaviour
    {
        public string path;
        public Object[] assets;
        public string[] assetPaths;

        private readonly HashSet<AssetBundle> _assetBundles = new HashSet<AssetBundle>();

        private AssetBundleManifest _manifest;
        private AssetBundle _selfBundle;

        private bool _selfLoadedManifest;


        private List<string> _allNames;
        private string _baseDir;
        private int _selfPathHash;

        private void OnDestroy()
        {
            Unload(true);
        }

        private void Reset()
        {
            Unload(true);
        }

        private void OnDisable()
        {
            Unload(true);
        }

        private void TryGetManifest()
        {
            if (string.IsNullOrEmpty(path) || _manifest)
            {
                return;
            }

            var loaded = AssetBundle.GetAllLoadedAssetBundles();
            foreach (var bundle in loaded)
            {
                _manifest = bundle.LoadAsset<AssetBundleManifest>(nameof(AssetBundleManifest).ToLower());
                if (_manifest)
                {
                    break;
                }
            }

            if (!_manifest)
            {
                TryFindBaseFolder();
                var platformStr = AssetBundleManager.GetPlatformName(true);
                var manifestPath = Path.Combine(_baseDir, platformStr);
                var m = AssetBundle.LoadFromFile(manifestPath);
                if (m)
                {
                    _assetBundles.Add(m);
                    _manifest = m.LoadAsset<AssetBundleManifest>(nameof(AssetBundleManifest).ToLower());
                    _assetBundles.Add(m);
                    _selfLoadedManifest = true;
                }
            }

            _allNames = _manifest.GetAllAssetBundles().ToList();
        }

        private void TryFindBaseFolder()
        {
            if (!string.IsNullOrEmpty(_baseDir))
            {
                return;
            }

            var platformStr = AssetBundleManager.GetPlatformName(true);
            var dir = new DirectoryInfo(Path.GetDirectoryName(path));
            while (dir != null)
            {
                var manifestPath = Path.Combine(dir.FullName, platformStr);

                if (!File.Exists(manifestPath))
                {
                    dir = dir.Parent;
                    continue;
                }

                _baseDir = dir.FullName;
                break;
            }
        }

        private AssetBundle LoadAssetBundle(string abName)
        {
            var ret = AssetBundle.GetAllLoadedAssetBundles().FirstOrDefault(ab => ab.name == abName);
            if (ret)
            {
                return ret;
            }

            var fName = Path.Combine(_baseDir, abName);

            ret = AssetBundle.LoadFromFile(fName);
            if (ret != null)
            {
                _assetBundles.Add(ret);
            }

            return ret;
        }

        public void Unload(bool includeManifest)
        {
            foreach (var assetBundle in _assetBundles)
            {
                assetBundle.Unload(false);
            }

            _assetBundles.Clear();
            if (includeManifest)
            {
                if (_selfLoadedManifest && _manifest)
                {
                    DestroyImmediate(_manifest, true);
                }

                _manifest = null;
            }

            assets = null;
            assetPaths = null;
        }

        public void TryLoad()
        {
            if (string.IsNullOrEmpty(path) || (_selfBundle != null && _selfPathHash == path.GetHashCode()))
            {
                return;
            }

            TryGetManifest();
            if (!_manifest)
            {
                return;
            }

            Unload(false);

            var names = _allNames.FindAll(m => m.EndsWith(Path.GetFileName(path)));
            if (names.Count > 0)
            {
                names.Sort((x, y) =>
                {
                    var a = x.Intersect(path);
                    var b = y.Intersect(path);
                    return b.Count().CompareTo(a.Count());
                });
            }

            var abName = names[0];
            var dependencies = _manifest.GetAllDependencies(abName);
            TryFindBaseFolder();
            foreach (var depPath in dependencies)
            {
                LoadAssetBundle(depPath);
            }

            _selfBundle = LoadAssetBundle(path);
            if (_selfBundle != null)
            {
                _selfPathHash = path.GetHashCode();
                assetPaths = _selfBundle.GetAllAssetNames();
                assets = new Object[assetPaths.Length];
                for (var i = 0; i < assetPaths.Length; i++)
                {
                    assets[i] = _selfBundle.LoadAsset(assetPaths[i]);
                }
            }

        }

        private void Update()
        {
            TryLoad();
        }
    }
}