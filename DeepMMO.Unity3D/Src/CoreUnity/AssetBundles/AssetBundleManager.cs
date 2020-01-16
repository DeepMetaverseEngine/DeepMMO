#if NET_4_6 || NET_STANDARD_2_0
#define AWAIT_SUPPORTED
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CoreUnity.Async;
using CoreUnity.Cache;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
#if AWAIT_SUPPORTED
using System.Threading.Tasks;

#endif

namespace CoreUnity.AssetBundles
{
    public struct AssetBundleCommand
    {
        public string BundleName;
        public bool Immediate;
        public Hash128 Hash;
        public uint Version;
        public Action<AssetBundle> OnComplete;
    }

    /// <summary>
    ///     Simple AssetBundle management
    /// </summary>
    public class AssetBundleManager : IDisposable
    {
        public enum DownloadSettings
        {
            UseCacheIfAvailable,
            DoNotUseCache
        }


        public enum PrimaryManifestType
        {
            None,
            Remote,
            RemoteCached,
            StreamingAssets,
        }

        public static string GetPlatformForAssetBundles(RuntimePlatform platform)
        {
            switch (platform)
            {
                case RuntimePlatform.Android:
                    return "Android";
                case RuntimePlatform.IPhonePlayer:
                    return "iOS";
                case RuntimePlatform.tvOS:
                    return "tvOS";
                case RuntimePlatform.WebGLPlayer:
                    return "WebGL";
                case RuntimePlatform.WindowsPlayer:
                    return "StandaloneWindows";
#if UNITY_2017_4_OR_NEWER
                case RuntimePlatform.OSXPlayer:
                    return "StandaloneOSX";
#else
                case RuntimePlatform.OSXPlayer:
                    return "StandaloneOSXIntel";
#endif
                case RuntimePlatform.LinuxPlayer:
                    return "StandaloneLinux";
#if UNITY_SWITCH
                case RuntimePlatform.Switch:
                    return "Switch";
#endif
                // Add more build targets for your own.
                // If you add more targets, don't forget to add the same platforms to the function above.
                default:
                    Debug.Log("Unknown BuildTarget: Using Default Enum Name: " + platform);
                    return platform.ToString();
            }
        }

        public static string GetPlatformName(bool toLower)
        {
#if UNITY_EDITOR
            var ret = GetPlatformForAssetBundles(EditorUserBuildSettings.activeBuildTarget);
#else
			var ret = GetPlatformForAssetBundles(Application.platform);
#endif
            return toLower ? ret.ToLower() : ret;
        }

#if UNITY_EDITOR
        public static string GetPlatformForAssetBundles(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.Android:
                    return "Android";
                case BuildTarget.iOS:
                    return "iOS";
                case BuildTarget.tvOS:
                    return "tvOS";
                case BuildTarget.WebGL:
                    return "WebGL";
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return "StandaloneWindows";
#if UNITY_2017_4_OR_NEWER
                case BuildTarget.StandaloneOSX:
                    return "StandaloneOSX";
#else
                case BuildTarget.StandaloneOSXIntel:
                case BuildTarget.StandaloneOSXIntel64:
                    return "StandaloneOSXIntel";
#endif
                // Add more build targets for your own.
                // If you add more targets, don't forget to add the same platforms to the function below.
                case BuildTarget.StandaloneLinux:
                case BuildTarget.StandaloneLinux64:
                case BuildTarget.StandaloneLinuxUniversal:
                    return "StandaloneLinux";
#if UNITY_SWITCH
                case BuildTarget.Switch:
                    return "Switch";
#endif
                default:
                    Debug.Log("Unknown BuildTarget: Using Default Enum Name: " + target);
                    return target.ToString();
            }
        }
#endif

        public bool Initialized { get; private set; }
        public AssetBundleManifest Manifest { get; private set; }
        public PrimaryManifestType PrimaryManifest { get; private set; }

        private const string MANIFEST_DOWNLOAD_IN_PROGRESS_KEY = "__manifest__";
        private const string MANIFEST_PLAYERPREFS_KEY = "__abm_manifest_version__";

        private string[] baseUri;
        private bool useHash;
        public string PlatformName { get; }

        protected ICommandHandler<AssetBundleCommand> mHandler;
        private IDictionary<string, AssetBundleContainer> activeBundles = new Dictionary<string, AssetBundleContainer>(StringComparer.OrdinalIgnoreCase);
        private IDictionary<string, DownloadInProgressContainer> downloadsInProgress = new Dictionary<string, DownloadInProgressContainer>(StringComparer.OrdinalIgnoreCase);
        private IDictionary<string, string> unhashedToHashedBundleNameMap = new Dictionary<string, string>(10, StringComparer.OrdinalIgnoreCase);


        public class CachedAssetBundle
        {
            public AssetBundle Bundle;
            public bool UnloadAllLoadedObjects;
        }

        private KeyObjectPool<string, CachedAssetBundle> mCachedPool;

        private void OnRemoveCachedBundle(string key, CachedAssetBundle cache)
        {
            cache.Bundle.Unload(cache.UnloadAllLoadedObjects);
        }

        public bool EnablePlatformBundlePath { get; }

        public IObjectPoolControl BundlePool => mCachedPool;

        public AssetBundleManager(bool platformBundlePath, bool useLowerCasePlatform, int cacheCapacity = 50)
        {
            PlatformName = GetPlatformName(useLowerCasePlatform);
            EnablePlatformBundlePath = platformBundlePath;
            mCachedPool = new KeyObjectPool<string, CachedAssetBundle>(cacheCapacity, OnRemoveCachedBundle);
        }

        public void SetHandler(ICommandHandler<AssetBundleCommand> handler)
        {
            mHandler = handler;
        }

        /// <summary>
        ///     Sets the base uri used for AssetBundle calls.
        /// </summary>
        public AssetBundleManager SetBaseUri(string uri)
        {
            if (uri == null)
            {
                uri = "";
            }

            return SetBaseUri(new[] {uri});
        }

        /// <summary>
        ///     Sets the base uri used for AssetBundle calls.
        /// </summary>
        /// <param name="uris">List of uris to use.  In order of priority (highest to lowest).</param>
        public AssetBundleManager SetBaseUri(string[] uris)
        {
            if (baseUri == null || baseUri.Length == 0)
            {
                Debug.LogFormat("Setting base uri to [{0}].", string.Join(",", uris));
            }
            else
            {
                Debug.LogWarningFormat("Overriding base uri from [{0}] to [{1}].", string.Join(",", baseUri), string.Join(",", uris));
            }

            baseUri = new string[uris.Length];

            for (int i = 0; i < uris.Length; i++)
            {
                var builder = new StringBuilder(uris[i]);

                if (!uris[i].EndsWith("/"))
                {
                    builder.Append("/");
                }

                if (EnablePlatformBundlePath)
                {
                    builder.Append(PlatformName).Append("/");
                }

                baseUri[i] = builder.ToString();
            }

            return this;
        }

        /// <summary>
        ///     Sets the base uri used for AssetBundle calls to the one created by the AssetBundleBrowser when the bundles are
        ///     built.
        ///     Used for easier testing in the editor
        /// </summary>
        public AssetBundleManager UseSimulatedUri()
        {
            SetBaseUri(new[] {"file://" + Application.dataPath + "/../AssetBundles/"});
            return this;
        }

        /// <summary>
        ///     Sets the base uri used for AssetBundle calls to the StreamingAssets folder.
        /// </summary>
        public AssetBundleManager UseStreamingAssetsFolder()
        {
#if UNITY_ANDROID
            var url = Application.streamingAssetsPath;
#else
            var url = "file:///" + Application.streamingAssetsPath;
#endif
            SetBaseUri(new[] {url});
            return this;
        }

        /// <summary>
        ///     Tell ABM to append the hash name to bundle names before downloading.
        ///     If you are using AssetBundleBrowser then you need to enable "Append Hash" in the advanced settings for this to
        ///     work.
        /// </summary>
        public AssetBundleManager AppendHashToBundleNames(bool appendHash = true)
        {
            if (appendHash && Initialized)
            {
                GenerateUnhashToHashMap(Manifest);
            }

            useHash = appendHash;
            return this;
        }

        /// <summary>
        ///     Downloads the AssetBundle manifest and prepares the system for bundle management.
        ///     Uses the platform name as the manifest name.  This is the default behaviour when
        ///     using Unity's AssetBundleBrowser to create your bundles.
        /// </summary>
        /// <param name="manifestName"></param>
        /// <param name="getFreshManifest"></param>
        /// <param name="onComplete">Called when initialization is complete.</param>
        public void Initialize(string manifestName, bool getFreshManifest, Action<bool> onComplete)
        {
            if (baseUri == null || baseUri.Length == 0)
            {
                SetBaseUri("/");
            }

            GetManifest(manifestName, getFreshManifest, bundle => onComplete(bundle != null));
        }

        public void Initialize(Action<bool> onComplete)
        {
            Initialize(PlatformName, true, onComplete);
        }

        public void Initialize(string manifestName, Action<bool> onComplete)
        {
            Initialize(manifestName, true, onComplete);
        }

        /// <summary>
        ///     Downloads the AssetBundle manifest and prepares the system for bundle management.
        ///     Uses the platform name as the manifest name.  This is the default behaviour when
        ///     using Unity's AssetBundleBrowser to create your bundles.
        /// </summary>
        /// <returns>An IEnumerator that can be yielded to until the system is ready.</returns>
        public AssetBundleManifestAsync InitializeAsync()
        {
            return InitializeAsync(PlatformName, true);
        }

        public AssetBundleManifestAsync InitializeAsync(string manifestName)
        {
            return InitializeAsync(manifestName, true);
        }

        /// <summary>
        ///     Downloads the AssetBundle manifest and prepares the system for bundle management.
        /// </summary>
        /// <param name="manifestName">The name of the manifest file to download.</param>
        /// <param name="getFreshManifest">
        ///     Always try to download a new manifest even if one has already been cached.
        /// </param>
        /// <returns>An IEnumerator that can be yielded to until the system is ready.</returns>
        public AssetBundleManifestAsync InitializeAsync(string manifestName, bool getFreshManifest)
        {
            if (baseUri == null || baseUri.Length == 0)
            {
                Debug.LogError("You need to set the base uri before you can initialize.");
                return null;
            }

            // Wrap the GetManifest with an async operation.
            return new AssetBundleManifestAsync(manifestName, getFreshManifest, GetManifest);
        }

        private void GetManifest(string bundleName, bool getFreshManifest, Action<AssetBundle> onComplete)
        {
            DownloadInProgressContainer inProgress;
            if (downloadsInProgress.TryGetValue(MANIFEST_DOWNLOAD_IN_PROGRESS_KEY, out inProgress))
            {
                inProgress.References++;
                inProgress.OnComplete += onComplete;
                return;
            }

            inProgress = new DownloadInProgressContainer(onComplete);

            downloadsInProgress.Add(MANIFEST_DOWNLOAD_IN_PROGRESS_KEY, inProgress);
            PrimaryManifest = PrimaryManifestType.Remote;

            uint manifestVersion = 1;

            if (getFreshManifest)
            {
                // Find the first cached version and then get the "next" one.
                manifestVersion = (uint) PlayerPrefs.GetInt(MANIFEST_PLAYERPREFS_KEY, 0) + 1;

                // The PlayerPrefs value may have been wiped so we have to calculate what the next uncached manifest version is.
                while (Caching.IsVersionCached(bundleName, new Hash128(0, 0, 0, manifestVersion)))
                {
                    manifestVersion++;
                }
            }

            var loaded = AssetBundle.GetAllLoadedAssetBundles();

            foreach (var bundle in loaded)
            {
                if (Manifest == null && bundle.LoadAsset<AssetBundleManifest>("assetbundlemanifest"))
                {
                    mHandler.SetBaseUrl(baseUri[0]);
                    OnInitializationComplete(bundle, bundleName, manifestVersion, false);
                }
                else if (Manifest && !string.IsNullOrEmpty(bundle.name))
                {
                    activeBundles.Add(bundle.name, new AssetBundleContainer
                    {
                        References = 1, AssetBundle = bundle, Dependencies = Manifest.GetDirectDependencies(bundle.name)
                    });
                }
            }

            if (!Manifest)
            {
                GetManifestInternal(bundleName, manifestVersion, 0);
            }
        }


        private void GetManifestInternal(string manifestName, uint version, int uriIndex)
        {
            var baseUrl = baseUri[uriIndex];

            mHandler.SetBaseUrl(baseUrl);


            mHandler.Handle(new AssetBundleCommand
            {
                BundleName = manifestName,
                Version = version,
                OnComplete = manifest =>
                {
                    var maxIndex = baseUri.Length - 1;
                    if (manifest == null && uriIndex < maxIndex && version > 1)
                    {
                        Debug.LogFormat("Unable to download manifest from [{0}], attempting [{1}]", baseUri[uriIndex], baseUri[uriIndex + 1]);
                        GetManifestInternal(manifestName, version, uriIndex + 1);
                    }
                    else if (manifest == null && uriIndex >= maxIndex && version > 1 && PrimaryManifest != PrimaryManifestType.RemoteCached)
                    {
                        PrimaryManifest = PrimaryManifestType.RemoteCached;
                        Debug.LogFormat("Unable to download manifest, attempting to use one previously downloaded (version [{0}]).", version);
                        GetManifestInternal(manifestName, version - 1, uriIndex);
                    }
                    else
                    {
                        OnInitializationComplete(manifest, manifestName, version, true);
                    }
                }
            });
        }

        private void OnInitializationComplete(AssetBundle manifestBundle, string bundleName, uint version, bool unloadAB)
        {
            if (manifestBundle == null)
            {
                Debug.LogError("AssetBundleManifest not found.");
            }
            else
            {
                Manifest = manifestBundle.LoadAsset<AssetBundleManifest>("assetbundlemanifest");
                PlayerPrefs.SetInt(MANIFEST_PLAYERPREFS_KEY, (int) version);

#if UNITY_2017_1_OR_NEWER
                Caching.ClearOtherCachedVersions(bundleName, new Hash128(0, 0, 0, version));
#endif
            }

            if (Manifest == null)
            {
                PrimaryManifest = PrimaryManifestType.None;
            }
            else
            {
                Initialized = true;

                if (useHash)
                {
                    GenerateUnhashToHashMap(Manifest);
                }
            }

            var inProgress = downloadsInProgress[MANIFEST_DOWNLOAD_IN_PROGRESS_KEY];
            downloadsInProgress.Remove(MANIFEST_DOWNLOAD_IN_PROGRESS_KEY);
            inProgress.OnComplete(manifestBundle);

            // Need to do this after OnComplete, otherwise the bundle will always be null
            if (manifestBundle != null && unloadAB)
            {
                manifestBundle.Unload(false);
            }
        }

        private void GenerateUnhashToHashMap(AssetBundleManifest manifest)
        {
            unhashedToHashedBundleNameMap.Clear();
            var allBundles = manifest.GetAllAssetBundles();

            for (int i = 0; i < allBundles.Length; i++)
            {
                var indexOfHashSplit = allBundles[i].LastIndexOf('_');
                if (indexOfHashSplit < 0) continue;
                unhashedToHashedBundleNameMap[allBundles[i].Substring(0, indexOfHashSplit)] = allBundles[i];
            }
        }

        /// <summary>
        ///     Downloads an AssetBundle or returns a cached AssetBundle if it has already been downloaded.
        ///     Remember to call <see cref="UnloadBundle(UnityEngine.AssetBundle,bool)" /> for every bundle you download once you
        ///     are done with it.
        /// </summary>
        /// <param name="bundleName">Name of the bundle to download.</param>
        /// <param name="onComplete">Action to perform when the bundle has been successfully downloaded.</param>
        public void GetBundle(string bundleName, Action<AssetBundle> onComplete)
        {
            GetBundle(bundleName, onComplete, DownloadSettings.UseCacheIfAvailable);
        }

        public AssetBundle GetBundleImmediate(string bundleName)
        {
            AssetBundle ret = null;
            GetBundle(bundleName, (bundle) => { ret = bundle; }, DownloadSettings.UseCacheIfAvailable, true);
            return ret;
        }

        public void LoadScene(string bundleName, LoadSceneMode loadSceneMode, Action<AsyncOperation> onComplete)
        {
            GetBundle(bundleName, bundle =>
            {
                var rq = SceneManager.LoadSceneAsync(Path.GetFileNameWithoutExtension(bundleName), loadSceneMode);
                onComplete?.Invoke(rq);
            });
        }

        public Scene LoadSceneImmediate(string address, LoadSceneMode mode)
        {
            var ab = GetBundleImmediate(address);
			if(ab == null)
			{
				return new Scene();
			}
            var sceneName = Path.GetFileNameWithoutExtension(address);
            SceneManager.LoadScene(sceneName, mode);
            return SceneManager.GetSceneByName(sceneName);
        }

        private void HandleCommand(AssetBundleCommand mainBundle)
        {
            var ab = mCachedPool.Get(mainBundle.BundleName);
            if (ab != null)
            {
                mainBundle.OnComplete?.Invoke(ab.Bundle);
            }
            else
            {
                mHandler.Handle(mainBundle);
            }
        }

        public void GetBundle(string bundleName, Action<AssetBundle> onComplete, DownloadSettings downloadSettings)
        {
            GetBundle(bundleName, onComplete, downloadSettings, false);
        }

        /// <summary>
        ///     Downloads an AssetBundle or returns a cached AssetBundle if it has already been downloaded.
        ///     Remember to call <see cref="UnloadBundle(UnityEngine.AssetBundle,bool)" /> for every bundle you download once you
        ///     are done with it.
        /// </summary>
        /// <param name="bundleName">Name of the bundle to download.</param>
        /// <param name="onComplete">Action to perform when the bundle has been successfully downloaded.</param>
        /// <param name="downloadSettings">
        ///     Tell the function to use a previously downloaded version of the bundle if available.
        ///     Important!  If the bundle is currently "active" (it has not been unloaded) then the active bundle will be used
        ///     regardless of this setting.  If it's important that a new version is downloaded then be sure it isn't active.
        /// </param>
        private void GetBundle(string bundleName, Action<AssetBundle> onComplete, DownloadSettings downloadSettings, bool immediate)
        {
            if (!Initialized)
            {
                Debug.LogError("AssetBundleManager must be initialized before you can get a bundle.");
                onComplete(null);
                return;
            }

            if (useHash) bundleName = GetHashedBundleName(bundleName);

            AssetBundleContainer active;

            if (activeBundles.TryGetValue(bundleName, out active))
            {
                active.References++;
                onComplete(active.AssetBundle);
                return;
            }

            DownloadInProgressContainer inProgress;

            if (downloadsInProgress.TryGetValue(bundleName, out inProgress))
            {
                inProgress.References++;
                inProgress.OnComplete += onComplete;
                return;
            }

            downloadsInProgress.Add(bundleName, new DownloadInProgressContainer(onComplete));


            var mainBundle = new AssetBundleCommand
            {
                BundleName = bundleName,
                Hash = downloadSettings == DownloadSettings.UseCacheIfAvailable ? Manifest.GetAssetBundleHash(bundleName) : default(Hash128),
                OnComplete = bundle => OnAssetBundleLoadComplete(bundleName, bundle),
                Immediate = immediate
            };

            var dependencies = Manifest.GetDirectDependencies(bundleName);
            var dependenciesToDownload = new List<string>();

            for (int i = 0; i < dependencies.Length; i++)
            {
                if (activeBundles.TryGetValue(dependencies[i], out active))
                {
                    active.References++;
                }
                else
                {
                    dependenciesToDownload.Add(dependencies[i]);
                }
            }

            if (dependenciesToDownload.Count > 0)
            {
                var dependencyCount = dependenciesToDownload.Count;

                void OnDependenciesComplete(AssetBundle dependency)
                {
                    if (--dependencyCount == 0)
                    {
                        HandleCommand(mainBundle);
                    }
                }

                for (int i = 0; i < dependenciesToDownload.Count; i++)
                {
                    var dependencyName = dependenciesToDownload[i];
                    GetBundle(dependencyName, OnDependenciesComplete, downloadSettings, immediate);
                }
            }
            else
            {
                HandleCommand(mainBundle);
            }
        }

#if AWAIT_SUPPORTED
        /// <summary>
        ///     Downloads the AssetBundle manifest and prepares the system for bundle management.
        ///     Uses the platform name as the manifest name.  This is the default behaviour when
        ///     using Unity's AssetBundleBrowser to create your bundles.
        /// </summary>
        public async Task<bool> Initialize()
        {
            return await Initialize(PlatformName, true);
        }

        public async Task<bool> Initialize(string manifestName)
        {
            return await Initialize(manifestName, true);
        }

        /// <summary>
        ///     Downloads the AssetBundle manifest and prepares the system for bundle management.
        /// </summary>
        /// <param name="manifestName">Name of the manifest to download. </param>
        /// <param name="getFreshManifest">
        ///     Always try to download a new manifest even if one has already been cached.
        /// </param>
        public async Task<bool> Initialize(string manifestName, bool getFreshManifest)
        {
            var completionSource = new TaskCompletionSource<bool>();
            var onComplete = new Action<bool>(b => completionSource.SetResult(b));
            Initialize(manifestName, getFreshManifest, onComplete);
            return await completionSource.Task;
        }

        /// <summary>
        ///     Downloads an AssetBundle or returns a cached AssetBundle if it has already been downloaded.
        ///     Remember to call <see cref="UnloadBundle(UnityEngine.AssetBundle,bool)" /> for every bundle you download once you
        ///     are done with it.
        /// </summary>
        /// <param name="bundleName">Name of the bundle to download.</param>
        public async Task<AssetBundle> GetBundle(string bundleName)
        {
            var completionSource = new TaskCompletionSource<AssetBundle>();
            var onComplete = new Action<AssetBundle>(bundle => completionSource.SetResult(bundle));
            GetBundle(bundleName, onComplete);
            return await completionSource.Task;
        }

        /// <summary>
        ///     Downloads an AssetBundle or returns a cached AssetBundle if it has already been downloaded.
        ///     Remember to call <see cref="UnloadBundle(UnityEngine.AssetBundle,bool)" /> for every bundle you download once you
        ///     are done with it.
        /// </summary>
        /// <param name="bundleName">Name of the bundle to download.</param>
        /// <param name="downloadSettings">
        ///     Tell the function to use a previously downloaded version of the bundle if available.
        ///     Important!  If the bundle is currently "active" (it has not been unloaded) then the active bundle will be used
        ///     regardless of this setting.  If it's important that a new version is downloaded then be sure it isn't active.
        /// </param>
        public async Task<AssetBundle> GetBundle(string bundleName, DownloadSettings downloadSettings)
        {
            var completionSource = new TaskCompletionSource<AssetBundle>();
            var onComplete = new Action<AssetBundle>(bundle => completionSource.SetResult(bundle));
            GetBundle(bundleName, onComplete, downloadSettings);
            return await completionSource.Task;
        }

        /// <summary>
        ///     Downloads a bundle (or uses a cached bundle) and loads a Unity scene contained in an asset bundle asynchronously.
        /// </summary>
        /// <param name="bundleName">Name of the bundle to donwnload.</param>
        /// <param name="levelName">Name of the unity scene to load.</param>
        /// <param name="loadSceneMode">See <see cref="LoadSceneMode">UnityEngine.SceneManagement.LoadSceneMode</see>.</param>
        /// <returns></returns>
        public async Task<AsyncOperation> LoadScene(string bundleName, LoadSceneMode loadSceneMode)
        {
            try
            {
                var completionSource = new TaskCompletionSource<AsyncOperation>();
                var onComplete = new Action<AsyncOperation>(bundle => completionSource.SetResult(bundle));
                LoadScene(bundleName, loadSceneMode, onComplete);
                return await completionSource.Task;
            }
            catch
            {
                Debug.LogError($"Error while loading the scene from {bundleName}");
                throw;
            }
        }
#endif

        /// <summary>
        ///     Asynchronously downloads an AssetBundle or returns a cached AssetBundle if it has already been downloaded.
        ///     Remember to call <see cref="UnloadBundle(UnityEngine.AssetBundle,bool)" /> for every bundle you download once you
        ///     are done with it.
        /// </summary>
        /// <param name="bundleName"></param>
        /// <returns></returns>
        public AssetBundleAsync GetBundleAsync(string bundleName)
        {
            if (!Initialized)
            {
                Debug.LogError("AssetBundleManager must be initialized before you can get a bundle.");
                return new AssetBundleAsync();
            }

            return new AssetBundleAsync(bundleName, GetBundle);
        }

        public ResultAsyncOperation<AsyncOperation> LoadSceneAsync(string bundleName, LoadSceneMode mode)
        {
            return new ResultAsyncOperation<AsyncOperation, string, LoadSceneMode>(bundleName, mode, LoadScene);
        }


        /// <summary>
        ///     Returns the bundle name with the bundle hash appended to it.  Needed if you have hash naming enabled via
        ///     <code>AppendHashToBundleNames(true)</code>
        /// </summary>
        public string GetHashedBundleName(string bundleName)
        {
            try
            {
                bundleName = unhashedToHashedBundleNameMap[bundleName];
            }
            catch
            {
                Debug.LogWarningFormat("Unable to find hash for bundle [{0}], this request is likely to fail.", bundleName);
            }

            return bundleName;
        }

        /// <summary>
        ///     Check to see if a specific asset bundle is cached or needs to be downloaded.
        /// </summary>
        public bool IsVersionCached(string bundleName)
        {
            if (Manifest == null) return false;
            if (useHash) bundleName = GetHashedBundleName(bundleName);
            if (string.IsNullOrEmpty(bundleName)) return false;
            return Caching.IsVersionCached(bundleName, Manifest.GetAssetBundleHash(bundleName));
        }

        /// <summary>
        ///     Cleans up all downloaded bundles
        /// </summary>
        public void Dispose()
        {
            foreach (var cache in activeBundles.Values)
            {
                if (cache.AssetBundle != null)
                {
                    cache.AssetBundle.Unload(true);
                }
            }

            mCachedPool.Clear();
            activeBundles.Clear();
        }

        /// <summary>
        ///     Unloads an AssetBundle.  Objects that were loaded from this bundle will need to be manually destroyed.
        /// </summary>
        /// <param name="bundle">Bundle to unload.</param>
        public bool UnloadBundle(AssetBundle bundle)
        {
            return UnloadBundle(bundle, false);
        }

        /// <summary>
        ///     Unloads an AssetBundle.
        /// </summary>
        /// <param name="bundle">Bundle to unload.</param>
        /// <param name="unloadAllLoadedObjects">
        ///     When true, all objects that were loaded from this bundle will be destroyed as
        ///     well. If there are game objects in your scene referencing those assets, the references to them will become missing.
        /// </param>
        public bool UnloadBundle(AssetBundle bundle, bool unloadAllLoadedObjects)
        {
            if (bundle == null) return false;
            var bundleName = bundle.name;
            if (string.IsNullOrEmpty(bundleName))
            {
                var ret = activeBundles.FirstOrDefault(entry => entry.Value.AssetBundle == bundle);
                if (ret.Value != null)
                {
                    bundleName = ret.Key;
                }
            }

            return UnloadBundle(bundleName, unloadAllLoadedObjects, false);
        }

        /// <summary>
        ///     Unloads an AssetBundle.
        /// </summary>
        /// <param name="bundleName">Bundle to unload.</param>
        /// <param name="unloadAllLoadedObjects">
        ///     When true, all objects that were loaded from this bundle will be destroyed as
        ///     well. If there are game objects in your scene referencing those assets, the references to them will become missing.
        /// </param>
        /// <param name="force">Unload the bundle even if we believe there are other dependencies on it.</param>
        public bool UnloadBundle(string bundleName, bool unloadAllLoadedObjects, bool force)
        {
            if (bundleName == null) return false;

            AssetBundleContainer cache;

            if (!activeBundles.TryGetValue(bundleName, out cache)) return false;

            if (force || --cache.References <= 0)
            {
                if (cache.AssetBundle != null)
                {
                    mCachedPool.Put(bundleName, new CachedAssetBundle {Bundle = cache.AssetBundle, UnloadAllLoadedObjects = unloadAllLoadedObjects});
                    //cache.AssetBundle.Unload(unloadAllLoadedObjects);
                }

                activeBundles.Remove(bundleName);

                for (int i = 0; i < cache.Dependencies.Length; i++)
                {
                    UnloadBundle(cache.Dependencies[i], unloadAllLoadedObjects, force);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        ///     Caches the downloaded bundle and pushes it to the onComplete callback.
        /// </summary>
        private void OnAssetBundleLoadComplete(string bundleName, AssetBundle bundle)
        {
            var inProgress = downloadsInProgress[bundleName];
            downloadsInProgress.Remove(bundleName);

            if (bundle != null)
            {
                string[] dependencies;
                try
                {
                    dependencies = Manifest.GetDirectDependencies(bundleName);
                }
                catch
                {
                    dependencies = new string[0];
                }

                try
                {
                    activeBundles.Add(bundleName, new AssetBundleContainer
                    {
                        AssetBundle = bundle,
                        References = inProgress.References,
                        Dependencies = dependencies
                    });
                }
                catch (ArgumentException)
                {
                    Debug.LogWarning("Attempted to activate a bundle that was already active.  Not sure how this happened, attempting to fail gracefully.");
                    activeBundles[bundleName].References++;
                }
            }

            inProgress.OnComplete(bundle);
        }

        internal class AssetBundleContainer
        {
            public AssetBundle AssetBundle;
            public int References = 1;
            public string[] Dependencies;
        }

        internal class DownloadInProgressContainer
        {
            public int References;
            public Action<AssetBundle> OnComplete;

            public DownloadInProgressContainer(Action<AssetBundle> onComplete)
            {
                References = 1;
                OnComplete = onComplete;
            }
        }
    }
}