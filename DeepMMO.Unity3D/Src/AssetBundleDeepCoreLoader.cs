using System.Threading;
using CoreUnity.AssetBundles;
using DeepCore.Unity3D.Impl;
using UnityEngine;

namespace DeepCore.Unity3D.AssetBundles
{
    public class AssetBundleDeepCoreLoader : ICommandHandler<AssetBundleCommand>
    {
        public void Handle(AssetBundleCommand cmd)
        {
            if (UnityDriver.LOAD_ASSETBUNDLE_USE_STREAM)
            {
                if (UnityDriver.UnityInstance.TryOpenStream(mBaseUrl + cmd.BundleName, out var stream))
                {
                    if (cmd.Immediate)
                    {
                        var ab = AssetBundle.LoadFromStream(stream, 0, 128 * 1024);
                        cmd.OnComplete(ab);
                    }
                    else
                    {
                        var request = AssetBundle.LoadFromStreamAsync(stream, 0, 128 * 1024);
                        request.completed += (e) =>
                        {
                            stream.Dispose();
                            cmd.OnComplete(request.assetBundle);
                        };
                    }
                }
                else
                {
                    cmd.OnComplete(null);
                }
            }
            else
            {
                if (cmd.Immediate)
                {
                    if (UnityDriver.UnityInstance.TryLoadData(mBaseUrl + cmd.BundleName, out var bin) && bin != null)
                    {
                        var ab = AssetBundle.LoadFromMemory(bin);
                        cmd.OnComplete(ab);
                    }
                    else
                    {
                        Debugger.LogError($"{cmd.BundleName} error");
                    }
                }
                else
                {
                    ThreadPool.QueueUserWorkItem((obj) =>
                    {
                        if (UnityDriver.UnityInstance.TryLoadData(mBaseUrl + cmd.BundleName, out var bin) && bin != null)
                        {
                            UnityHelper.MainThreadInvoke(() =>
                            {
                                var request = AssetBundle.LoadFromMemoryAsync(bin);
                                request.completed += (e) => { cmd.OnComplete(request.assetBundle); };
                            });
                        }
                        else
                        {
                            Debugger.LogError($"{cmd.BundleName} error");
                            UnityHelper.MainThreadInvoke(() => { cmd.OnComplete(null); });
                        }
                    });
                }
            }
        }

        private string mBaseUrl;

        public void SetBaseUrl(string url)
        {
            mBaseUrl = url;
        }
    }
}