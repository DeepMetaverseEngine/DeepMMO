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
            //服务A，往共享内存内写入数据
            //共享内存工作机制：
            //1、向当前进程内写入数据
            //2、广播给整个集群，每个节点会自动同步写入操作，但不是实时的。
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
            //服务B，从共享内存里读取数据
            //服务B当前获取的可能不是最新的数据。
            var dict = this.SharedMemory.GetDictionary<DateTime>("ServiceStartDateTime");
            dict.TryGetValue("FuckService", out DateTime startingTime);
            return Task.CompletedTask;
        }
        protected override Task OnStopAsync(ServiceStopInfo stop)
        {
            return Task.CompletedTask;
        }
    }
}
