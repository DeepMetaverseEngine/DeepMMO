namespace DeepU3.AssetBundles
{
    public interface ICommandHandler<in T>
    {
        void Handle(AssetBundleManager bundleManager, T cmd);
        void SetBaseUrl(string url);
    }
}