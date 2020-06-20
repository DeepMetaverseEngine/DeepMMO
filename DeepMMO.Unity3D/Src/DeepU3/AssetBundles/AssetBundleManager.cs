using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using DeepU3.Cache;
using DeepU3.Asset;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace DeepU3.AssetBundles
{
    [Flags]
    public enum AssetBundleLoadOption
    {
        Async = 1,
        Immediate = 2,
        TryImmediate = 4,
        SupportImmediate = Immediate | TryImmediate
    }

    public class AssetBundleCommand
    {
        public string BundleName;
        public AssetBundleLoadOption Option;
        public Hash128 Hash;
        public int References;
        public bool IsDone;
        public AssetBundle Bundle { get; internal set; }
        public string Error;
        public uint Version;
        public event Action<AssetBundle> OnComplete;
        public event Action<AssetBundleCommand> OnInternalComplete;
        private readonly Dictionary<string, AssetBundleCommand> mDependencies = new Dictionary<string, AssetBundleCommand>();

        private static readonly ObjectPool<AssetBundleCommand> sCommandPool = new ObjectPool<AssetBundleCommand>(1000);
        private bool mMainAssetBundleDone;

        private AssetBundleManager mAbm;

        public void AsyncOperationComplete(AsyncOperation request)
        {
            if (request is AssetBundleCreateRequest abRequest)
            {
                SetComplete(abRequest.assetBundle);
            }
            else
            {
                SetComplete(null);
            }
        }

        public void SetComplete(AssetBundle bundle, string error = null)
        {
            Bundle = bundle;
            Error = error;
            mMainAssetBundleDone = true;

            if (References <= 0)
            {
                IsDone = true;
                Unload();
            }
            else
            {
                IsDone = mDependencies.Count == 0 || mDependencies.All(entry => entry.Value.IsDone);
                OnInternalComplete?.Invoke(this);
                OnInternalComplete = null;
                TryDone();
            }
        }


        internal static AssetBundleCommand Create(AssetBundleManager abm, string bundleName, AssetBundleLoadOption opt, Action<AssetBundle> onComplete = null)
        {
            var ret = sCommandPool.Get() ?? new AssetBundleCommand();

            ret.BundleName = bundleName;
            if (abm.Manifest != null)
            {
                ret.Hash = abm.Manifest.GetAssetBundleHash(bundleName);
            }

            ret.References = 1;
            ret.mAbm = abm;
            ret.Option = opt;
            ret.OnComplete += onComplete;
            ret.OnInternalComplete += abm.OnAssetBundleLoadComplete;
#if UNITY_EDITOR
            ret.LoadingTime.Restart();
            ret.LoadTrace = new StackTrace(true);
            ret.Senders.Clear();
#endif
            abm.mCommands.Add(bundleName, ret);
            return ret;
        }

        private void TryDone()
        {
            if (!IsDone)
            {
                return;
            }

            OnComplete?.Invoke(Bundle);
        }


        public void AddDependency(AssetBundleCommand dependency)
        {
            mDependencies.Add(dependency.BundleName, dependency);
            if (!dependency.IsDone)
            {
                if ((Option & AssetBundleLoadOption.SupportImmediate) != 0 && dependency.Option == AssetBundleLoadOption.Async)
                {
                    var err = $"{BundleName} sync, {dependency.BundleName} async";
                    if (Option == AssetBundleLoadOption.TryImmediate)
                    {
                        Option = AssetBundleLoadOption.Async;
                        Debug.Log(err);
                    }
                    else
                    {
                        throw new Exception(err);
                    }
                }

                dependency.OnInternalComplete += OnDependencyLoadComplete;
            }
#if UNITY_EDITOR
            dependency.Senders.Add(BundleName);
#endif
        }

        private void OnDependencyLoadComplete(AssetBundleCommand dependency)
        {
            IsDone = mMainAssetBundleDone && (mDependencies.Count == 0 || mDependencies.All(entry => entry.Value.IsDone));
            TryDone();
        }

        public void Unload()
        {
            References--;

            if (References > 0)
            {
                return;
            }

            foreach (var entry in mDependencies)
            {
                if (!entry.Value.IsDone)
                {
                    entry.Value.OnInternalComplete -= OnDependencyLoadComplete;
                }

                entry.Value.Unload();
            }

            mDependencies.Clear();

            if (!IsDone)
            {
                return;
            }

            if (Bundle)
            {
                Bundle.Unload(true);
            }

            mAbm.mCommands.Remove(BundleName);

#if UNITY_EDITOR
            Statistics.Instance?.RemoveBundle(BundleName);
#endif
            Reset();
            sCommandPool.Put(this);
        }

        internal void Reset()
        {
            BundleName = default;
            Option = default;
            Hash = default;
            Version = default;
            OnComplete = default;
            OnInternalComplete = default;
            References = default;
            Bundle = default;
            Error = null;
            IsDone = false;
            mAbm = null;
            mMainAssetBundleDone = false;
            mDependencies.Clear();
        }
#if UNITY_EDITOR
        public readonly Stopwatch LoadingTime = new Stopwatch();
        public StackTrace LoadTrace;
        public readonly List<string> Senders = new List<string>();
        public override string ToString()
        {
            return $"{BundleName}:{LoadingTime.ElapsedMilliseconds}\n {LoadTrace}";
        }
#else
        public override string ToString()
        {
            return BundleName;
        }
#endif
    }


    /// <summary>
    ///     Simple AssetBundle management
    /// </summary>
    public class AssetBundleManager : IDisposable
    {
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

        private const string MANIFEST_PLAYERPREFS_KEY = "__abm_manifest_version__";

        private string[] baseUri;
        public string PlatformName { get; }

        private ICommandHandler<AssetBundleCommand> mHandler;
        internal readonly IDictionary<string, AssetBundleCommand> mCommands = new Dictionary<string, AssetBundleCommand>(StringComparer.OrdinalIgnoreCase);

        private readonly Queue<AssetBundleCommand> mFailLoadedBundles = new Queue<AssetBundleCommand>();


        private AssetBundleProfileManifest mAssetBundleProfileManifest;

        internal void SetAssetBundleProfileManifest(AssetBundleProfileManifest bundleProfileManifest)
        {
            mAssetBundleProfileManifest = bundleProfileManifest;
        }


        public bool EnablePlatformBundlePath { get; }

        public IObjectPoolControl BundlePool => null;

        public AssetBundleManager(bool platformBundlePath, bool useLowerCasePlatform, int cacheCapacity = 50)
        {
            PlatformName = GetPlatformName(useLowerCasePlatform);
            EnablePlatformBundlePath = platformBundlePath;
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
            if (mCommands.TryGetValue(bundleName, out var cmd))
            {
                cmd.References++;
                if (cmd.IsDone)
                {
                    onComplete?.Invoke(cmd.Bundle);
                }
                else
                {
                    cmd.OnComplete += onComplete;
                }

                return;
            }


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
                    var loadedCommand = AssetBundleCommand.Create(this, bundle.name, AssetBundleLoadOption.Async);
                    loadedCommand.Bundle = bundle;
                    loadedCommand.IsDone = true;
                }
            }

            if (Manifest)
            {
                return;
            }

            cmd = GetManifestInternal(bundleName, manifestVersion, 0);
            cmd.OnComplete += onComplete;
        }


        private AssetBundleCommand GetManifestInternal(string manifestName, uint version, int uriIndex)
        {
            var baseUrl = baseUri[uriIndex];

            mHandler.SetBaseUrl(baseUrl);

            var cmd = AssetBundleCommand.Create(this, manifestName, AssetBundleLoadOption.Async);

            cmd.Version = version;
            cmd.OnComplete += (manifest) =>
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
            };
            mHandler.Handle(this, cmd);
            return cmd;
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
            }

            // Need to do this after OnComplete, otherwise the bundle will always be null
            if (manifestBundle != null && unloadAB)
            {
                manifestBundle.Unload(false);
            }
        }

        public AssetBundle GetBundleImmediate(string bundleName)
        {
            AssetBundle ret = null;
            GetBundle(bundleName, AssetBundleLoadOption.Immediate, (bundle) => { ret = bundle; });
            return ret;
        }

        public void LoadScene(string bundleName, LoadSceneMode loadSceneMode, Action<AsyncOperation> onComplete)
        {
            GetBundle(bundleName, AssetBundleLoadOption.Async, bundle =>
            {
                var allScenePaths = bundle.GetAllScenePaths();
                var rq = SceneManager.LoadSceneAsync(allScenePaths[0], loadSceneMode);
                onComplete?.Invoke(rq);
            });
        }

        public Scene LoadSceneImmediate(string address, LoadSceneMode mode)
        {
            var bundle = GetBundleImmediate(address);
            if (bundle == null)
            {
                return new Scene();
            }

            var allScenePaths = bundle.GetAllScenePaths();
            var scenePath = allScenePaths[0];
            SceneManager.LoadScene(scenePath, mode);
            return SceneManager.GetSceneByPath(scenePath);
        }

        private void HandleCommand(AssetBundleCommand cmd)
        {
            mHandler.Handle(this, cmd);
        }


        public void GetBundle(string bundleName, AssetBundleLoadOption opt, Action<AssetBundle> onComplete)
        {
            if (!Initialized)
            {
                Debug.LogError("AssetBundleManager must be initialized before you can get a bundle.");
                onComplete(null);
                return;
            }

            if (string.IsNullOrEmpty(bundleName))
            {
                onComplete(null);
                return;
            }

            if (mCommands.TryGetValue(bundleName, out var cmd))
            {
                cmd.References++;
                if (cmd.IsDone)
                {
                    onComplete(cmd.Bundle);
                }

                else
                {
                    cmd.OnComplete += onComplete;
                }

                return;
            }


            var dependencies = Manifest.GetAllDependencies(bundleName);
            if (mAssetBundleProfileManifest)
            {
                if (mAssetBundleProfileManifest.TryGetIgnoreDependencies(bundleName, out var ignorePairs))
                {
                    dependencies = dependencies.Where(m => !ignorePairs.IsIgnoreBundleName(m)).ToArray();
                }
            }


            var command = AssetBundleCommand.Create(this, bundleName, opt, onComplete);
            foreach (var s in dependencies)
            {
                if (mCommands.TryGetValue(s, out var dep))
                {
                    command.AddDependency(dep);
                    dep.References++;
                }
                else
                {
                    dep = AssetBundleCommand.Create(this, s, opt);
                    dep.OnInternalComplete += OnAssetBundleLoadComplete;
                    command.AddDependency(dep);
                    GetBundle(dep);
                }
            }

            GetBundle(command);
        }


        internal void OnAssetBundleLoadComplete(AssetBundleCommand command)
        {
#if UNITY_EDITOR
            command.LoadingTime.Stop();
#endif
            if (command.Bundle != null)
            {
#if UNITY_EDITOR
                Statistics.Instance?.AddBundle(command.BundleName, command.Senders.ToArray(), command.LoadTrace, command.LoadingTime.ElapsedMilliseconds);
#endif
            }
            else
            {
                ErrorLoadBundle(command);
            }
        }

        private void GetBundle(AssetBundleCommand cmd)
        {
            if (!Initialized)
            {
                Debug.LogError("AssetBundleManager must be initialized before you can get a bundle.");
                return;
            }
#if UNITY_EDITOR
            Statistics.Instance?.PreAddBundle(cmd.BundleName, cmd.LoadTrace);
#endif
            HandleCommand(cmd);
            if ((cmd.Option & AssetBundleLoadOption.SupportImmediate) != 0 && !cmd.IsDone)
            {
                ErrorLoadBundle(cmd);
            }
        }


        /// <summary>
        ///     Cleans up all downloaded bundles
        /// </summary>
        public void Dispose()
        {
            foreach (var entry in mCommands)
            {
                entry.Value.Unload();
            }

            mCommands.Clear();
        }

        /// <summary>
        ///     Unloads an AssetBundle.
        /// </summary>
        /// <param name="bundle">Bundle to unload.</param>
        /// <param name="unloadAllLoadedObjects">
        ///     When true, all objects that were loaded from this bundle will be destroyed as
        ///     well. If there are game objects in your scene referencing those assets, the references to them will become missing.
        /// </param>
        public bool UnloadBundle(AssetBundle bundle)
        {
            if (bundle == null)
            {
                return false;
            }

            var bundleName = bundle.name;
            if (string.IsNullOrEmpty(bundleName))
            {
                var ret = mCommands.FirstOrDefault(entry => entry.Value.Bundle == bundle);
                if (ret.Value != null)
                {
                    bundleName = ret.Key;
                }
            }

            return UnloadBundle(bundleName);
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
        public bool UnloadBundle(string bundleName)
        {
            if (bundleName == null) return false;

            if (!mCommands.TryGetValue(bundleName, out var cmd))
            {
                return false;
            }

            cmd.Unload();
            return cmd.References <= 0;
        }

        private void ErrorLoadBundle(AssetBundleCommand command)
        {
            while (mFailLoadedBundles.Count > 10)
            {
                mFailLoadedBundles.Dequeue();
            }

            mFailLoadedBundles.Enqueue(command);

            var strError = $"{command.BundleName} load error!\n";
#if UNITY_EDITOR
            if (command.Senders != null)
            {
                strError += "Senders: \n";
                foreach (var sender in command.Senders)
                {
                    strError += sender;
                    strError += "\n";
                }

                strError += "---------------------------------------------------\n";
            }

            strError += $"<color='olive'>{command.LoadTrace}</color>";
#endif
            Debug.LogError(strError);
        }
    }
}