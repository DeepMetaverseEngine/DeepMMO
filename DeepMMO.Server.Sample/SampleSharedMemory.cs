using DeepCrystal.RPC;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeepMMO.Server.Sample
{
    public class SampleSharedMemoryA : IService
    {
        public SampleSharedMemoryA(ServiceStartInfo start) : base(start) { }
        protected override void OnDisposed() { }
        protected override Task OnStartAsync()
        {
            //共享内存, 写入当前服务启动时间戳
            var dict = this.SharedMemory.GetDictionary<DateTime>("ServiceStartDateTime");
            dict[SelfAddress.ServiceName] = DateTime.Now;
            return Task.CompletedTask;
        }
        protected override Task OnStopAsync(ServiceStopInfo stop)
        {
            return Task.CompletedTask;
        }
    }

    public class SampleSharedMemoryB : IService
    {
        public SampleSharedMemoryB(ServiceStartInfo start) : base(start) { }
        protected override void OnDisposed() { }
        protected override Task OnStartAsync()
        {
            //共享内存, 写入当前服务启动时间戳
            var dict = this.SharedMemory.GetDictionary<DateTime>("ServiceStartDateTime");
            // 读取指定服务启动时间戳
            dict.TryGetValue("FuckService", out DateTime startingTime);
            return Task.CompletedTask;
        }
        protected override Task OnStopAsync(ServiceStopInfo stop)
        {
            return Task.CompletedTask;
        }
    }
}
