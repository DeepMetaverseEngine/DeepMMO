namespace CoreUnity.AssetBundles
{
    public interface ICommandHandler<in T>
    {
        void Handle(T cmd);
        void SetBaseUrl(string url);
    }
}