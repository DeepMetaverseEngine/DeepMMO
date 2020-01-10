namespace DeepCore.Unity3D
{
    public class ClientResourceData
    {
        /// <summary>
        /// 是否使用MPQ资源
        /// </summary>
        public bool useMPQ;


        /// <summary>
        /// 语言代码
        /// </summary>
        public string localeCode;
        
        /// <summary>
        /// 相对于根路径的GameEditor路径
        /// </summary>
        public string relativeGameEditor = "/GameEditor";

        /// <summary>
        /// 相对于根路径的UIEdit路径
        /// </summary>
        public string relativeUIEdit = "/UIEdit";

        /// <summary>
        /// 相对于根路径的客户端Lua脚本路径
        /// </summary>
        public string relativeScript = "/ClientScript";

        /// <summary>
        /// Standalone时相对于主路径的相对路径
        /// </summary>
        public string relativeRootWhenStandalone = "/../../data/GameEditors";
    }
}