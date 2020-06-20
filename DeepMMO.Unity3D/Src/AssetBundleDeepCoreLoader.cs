// #define USE_FILE

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Text;
using DeepCore.Unity3D;
using DeepU3.AssetBundles;
using DeepCore.Unity3D.Impl;
using UnityEngine;

namespace DeepMMO.Unity3D.AssetBundles
{
    public class AssetBundleDeepCoreLoader : ICommandHandler<AssetBundleCommand>
    {
        public struct HandleTask
        {
            public string Path;
            public Action<byte[]> CallBack;
            public static HandleTask Default = new HandleTask();
        }


        private readonly Queue<HandleTask> mQueues = new Queue<HandleTask>();

        // private readonly SemaphoreSlim mSlim = new SemaphoreSlim(0, 1000);

        public AssetBundleDeepCoreLoader()
        {
            var thread = new Thread(ThreadRun);
            thread.Start();
        }

        private void ThreadRun()
        {
            while (true)
            {
                var cmd = HandleTask.Default;
                lock (mQueues)
                {
                    if (mQueues.Count > 0)
                    {
                        cmd = mQueues.Dequeue();
                    }
                }

                if (cmd.CallBack != null)
                {
                    using (var sb = new Utf16ValueStringBuilder(true))
                    {
                        sb.Append(mBaseUrl);
                        sb.Append(cmd.Path);
                        //todo TryLoadData内 路径优化,去除SubString
                        UnityDriver.UnityInstance.TryLoadData(sb.ToString(), out var bin);
                        UnityHelper.MainThreadInvoke(() => { cmd.CallBack(bin); });
                    }
                }

                Thread.Sleep(10);
            }
        }

        public void Handle(AssetBundleManager mgr, AssetBundleCommand cmd)
        {
            if (UnityDriver.LOAD_ASSETBUNDLE_USE_STREAM)
            {
                if (UnityDriver.UnityInstance.TryOpenStream(ConvertToAssetBundleName(cmd), out var stream))
                {
                    if ((cmd.Option & AssetBundleLoadOption.SupportImmediate) != 0)
                    {
                        var ab = AssetBundle.LoadFromStream(stream, 0, 128 * 1024);
                        cmd.SetComplete(ab);
                    }
                    else
                    {
                        var request = AssetBundle.LoadFromStreamAsync(stream, 0, 128 * 1024);
                        request.completed += (e) =>
                        {
                            stream.Dispose();
                            cmd.SetComplete(request.assetBundle);
                        };
                    }
                }
                else
                {
                    cmd.SetComplete(null);
                }
            }
            else
            {
#if UNITY_STANDALONE && USE_FILE
                if (DataPathHelper.IsUseMPQ)
                {
                    HandleFromMemory(cmd);
                }
                else
                {
                    if (cmd.Immediate)
                    {
                        var ab = AssetBundle.LoadFromFile(ConvertToAssetBundleName(cmd));
                        cmd.SetComplete(ab);
                    }
                    else
                    {
                        var request = AssetBundle.LoadFromFileAsync(ConvertToAssetBundleName(cmd));
                        request.completed += (e) => { cmd.OnComplete(cmd, request.assetBundle); };
                    }                 
                }

#else
                HandleFromMemory(mgr, cmd);

#endif
            }
        }

        private string mBaseUrl;


        private string ConvertToAssetBundleName(AssetBundleCommand cmd)
        {
            using (var sb = new Utf16ValueStringBuilder(true))
            {
                sb.Append(mBaseUrl);
                sb.Append(cmd.BundleName);
                return sb.ToString();
            }
        }

        private void HandleFromMemory(AssetBundleManager mgr, AssetBundleCommand cmd)
        {
            if ((cmd.Option & AssetBundleLoadOption.SupportImmediate) != 0)
            {
                if (UnityDriver.UnityInstance.TryLoadData(ConvertToAssetBundleName(cmd), out var bin) && bin != null)
                {
                    var ab = AssetBundle.LoadFromMemory(bin);
                    cmd.SetComplete(ab);
                }
                else
                {
                    cmd.Error = "UnityDriver.UnityInstance.TryLoadData Error";
                    cmd.SetComplete(null);
                }
            }
            else
            {
                lock (mQueues)
                {
                    mQueues.Enqueue(new HandleTask
                    {
                        Path = cmd.BundleName, CallBack = (bin) =>
                        {
                            if (bin == null)
                            {
                                cmd.SetComplete(null);
                            }
                            else
                            {
                                var request = AssetBundle.LoadFromMemoryAsync(bin);
                                if (request.isDone)
                                {
                                    cmd.SetComplete(request.assetBundle);
                                }
                                else
                                {
                                    request.completed += (e) => { cmd.SetComplete(request.assetBundle); };
                                }
                            }
                        }
                    });
                }
            }
        }

        public void SetBaseUrl(string url)
        {
            mBaseUrl = url;
        }
    }
}