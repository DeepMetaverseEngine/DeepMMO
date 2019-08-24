using DeepCore;
using DeepCore.Log;
using DeepCore.Statistics;
using DeepCrystal.ORM;
using DeepCrystal.RPC;
using System.Threading.Tasks;

namespace DeepMMO.Server.Logic
{
    public abstract class ILogicModule : Disposable
    {
        public static TimeStatisticsRecoder Statistics { get => LogicService.Statistics; }
        public LogicService service { get; private set; }
        protected Logger log { get; private set; }


        public ILogicModule(LogicService service)
        {
            this.service = service;
            this.log = service.log;
        }
        protected override void Disposing() { }

        public virtual Task OnStartAsync() { return Task.CompletedTask; }
        public virtual Task OnStartedAsync() { return Task.CompletedTask; }
        public virtual Task OnStopAsync() { return Task.CompletedTask; }
        public virtual Task OnStopedAsync() { return Task.CompletedTask; }

        public virtual void OnSaveData(IObjectTransaction trans) { }
        public virtual Task OnClientEnterGameAsync() { return Task.CompletedTask; }
        public virtual void OnSessionReconnect() { }

    }
}
