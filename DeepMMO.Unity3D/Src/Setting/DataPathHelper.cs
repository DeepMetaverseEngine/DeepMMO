using DeepCore.Unity3D.Impl;

namespace DeepCore.Unity3D
{
    /// <summary>
    /// 路径统一不用"/"结尾
    /// </summary>
    public static class DataPathHelper
    {
        public static string ResRoot { get; private set; }
        public static bool IsUseMPQ { get; private set; }

        /// <summary>
        /// 战斗编辑器数据地址
        /// </summary>
        public static string GAME_EDITOR_DATA_ROOT { get; private set; }


        public static string GAME_EDITOR_ROOT { get; private set; }

        /// <summary>
        /// StreamingAssets资源地址
        /// </summary>
        public static string STREAMING_ASSETS_ROOT { get; private set; }

        /// <summary>
        /// UI编辑器根地址
        /// </summary>
        public static string UI_EDITOR_ROOT { get; private set; }

        /// <summary>
        /// UI编辑器资源地址
        /// </summary>
        public static string UI_EDITOR_RES_ROOT { get; private set; }

        /// <summary>
        /// UI编辑器XML地址
        /// </summary>
        public static string UI_EDITOR_XML_ROOT { get; private set; }

        /// <summary>
        /// 客户端Lua脚本地址
        /// </summary>
        public static string CLIENT_SCRIPT_ROOT { get; private set; }

        /// <summary>
        /// 下载MPQ资源后缀地址
        /// </summary>
        public static string HTTP_DOWNLOAD_MPQ_SUFFIX { get; private set; }

        /// <summary>
        /// 图片重定向后缀
        /// </summary>
        public static string REDIRECT_IMAGE_SUFFIX { get; private set; }


        public static void Init(ClientResourceData data)
        {
            IsUseMPQ = data.useMPQ;
            if (IsUseMPQ)
            {
                ResRoot = "mpq://";
            }
            else if (System.IO.File.Exists(UnityEngine.Application.dataPath + "/../resroot.txt"))
            {
                ResRoot = UnityEngine.Application.dataPath + "/.." + System.IO.File.ReadAllText(UnityEngine.Application.dataPath + "/../resroot.txt").Trim();
            }
            else
            {
                ResRoot = UnityEngine.Application.dataPath + data.relativeRootWhenStandalone;
                if (UnityEngine.Application.isEditor == false)
                {
                    switch (UnityEngine.Application.platform)
                    {
                        case UnityEngine.RuntimePlatform.Android:
                            ResRoot = UnityEngine.Application.streamingAssetsPath;
                            break;
                        case UnityEngine.RuntimePlatform.IPhonePlayer:
                            ResRoot = UnityEngine.Application.streamingAssetsPath;
                            break;
                    }
                }
            }

            UnityEngine.Debug.Log("ResRoot=" + ResRoot);
            GAME_EDITOR_ROOT = ResRoot + data.relativeGameEditor;
            GAME_EDITOR_DATA_ROOT = ResRoot + data.relativeGameEditor + "/data";
            STREAMING_ASSETS_ROOT = ResRoot + data.relativeGameEditor;
            UI_EDITOR_ROOT = ResRoot + data.relativeUIEdit;
            UI_EDITOR_RES_ROOT = UI_EDITOR_ROOT + "/res";
            UI_EDITOR_XML_ROOT = UI_EDITOR_ROOT + "/xml";
            CLIENT_SCRIPT_ROOT = ResRoot + data.relativeScript;
            HTTP_DOWNLOAD_MPQ_SUFFIX = "updates_png";
            REDIRECT_IMAGE_SUFFIX = ".png";
            switch (UnityEngine.Application.platform)
            {
                case UnityEngine.RuntimePlatform.Android:
                    STREAMING_ASSETS_ROOT = ResRoot + "/StreamingAssets/Android";
                    HTTP_DOWNLOAD_MPQ_SUFFIX = "updates_etc";
                    REDIRECT_IMAGE_SUFFIX = ".etc.m3z";
                    if (IsUseMPQ)
                    {
                        UnityDriver.UnityInstance.RedirectImage = RedirectImage;
                    }

                    break;
                case UnityEngine.RuntimePlatform.IPhonePlayer:
                    STREAMING_ASSETS_ROOT = ResRoot + "/StreamingAssets/iOS";
                    HTTP_DOWNLOAD_MPQ_SUFFIX = "updates_pvr";
                    REDIRECT_IMAGE_SUFFIX = ".pvr.m3z";
                    if (IsUseMPQ)
                    {
                        UnityDriver.UnityInstance.RedirectImage = RedirectImage;
                    }

                    break;
                default:
#if UNITY_ANDROID
                    STREAMING_ASSETS_ROOT = ResRoot + "/StreamingAssets/Android";
                    HTTP_DOWNLOAD_MPQ_SUFFIX = "updates_etc/";
                    REDIRECT_IMAGE_SUFFIX = ".etc.m3z";
                    if (IsUseMPQ)
                    {
                        DeepCore.Unity3D.Impl.UnityDriver.UnityInstance.RedirectImage = RedirectImage;
                    }
#elif UNITY_IOS
                    STREAMING_ASSETS_ROOT = ResRoot + "/StreamingAssets/iOS";
                    HTTP_DOWNLOAD_MPQ_SUFFIX = "updates_pvr/";
                    REDIRECT_IMAGE_SUFFIX = ".pvr.m3z";
                    if (IsUseMPQ)
                    {
                        DeepCore.Unity3D.Impl.UnityDriver.UnityInstance.RedirectImage = RedirectImage;
                    }
#endif
                    break;
            }

            UnityDriver.SetDirver();
        }


        public static string RedirectImage(string resource)
        {
            try
            {
                return resource.Substring(0, resource.LastIndexOf(".")) + REDIRECT_IMAGE_SUFFIX;
            }
            catch
            {
                return resource;
            }
        }
    }
}