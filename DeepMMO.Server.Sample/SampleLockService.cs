using System;
using DeepCrystal.RPC;
using DeepMMO.Protocol;
using System.Threading.Tasks;

namespace CommonRPG.Server.Sample
{
    public class SampleLockService : IService
    {
        //消耗硬币的服务
        IRemoteService machine;     
        //硬币数量
        int coinCount;
        //硬币锁
        IAsyncLock coinLocker;

        public override ServiceProperties Properties
        {
            get
            {
                var basep = base.Properties;
                // 允许并发
                basep.IsConcurrent = true;
                return basep;
            }
        }

        public SampleLockService(ServiceStartInfo start) : base(start)        {
        }
        protected override void OnDisposed()
        {
            //销毁锁
            this.coinLocker.Dispose();
        }
        protected override async Task OnStartAsync()
        {
            //创建异步锁（内存）
            this.coinLocker = Provider.CreateLock();
            //获得消耗者
            this.machine = await this.Provider.GetAsync(new RemoteAddress("Machine"));
        }
        protected override Task OnStopAsync(ServiceStopInfo stop)
        {
            return Task.CompletedTask;
        }
        //----------------------------------------------------------------------------------------------

        //----------------------------------------------------------------------------------------------
        // Async 异步写法
        [RpcHandler(typeof(PlayGameRequest), typeof(PlayGameResponse))]
        public async Task<PlayGameResponse> rpc_PlayGameAsync(PlayGameRequest req)
        {
            // <--- 此处如果不加锁，假如同时两个PlayGameRequest过来，则可能会导致coinCount为负数
            using (await this.coinLocker.LockAsync()) 
            {
                //判断硬币数量够不够
                if (coinCount > req.count)
                {
                    //异步调用
                    var rsp = await machine.CallAsync<PlayGameResponse>(req);
                    //消耗硬币
                    coinCount -= rsp.usedCoin;
                    //返回结果
                    return rsp;
                }
                //返回失败结果
                return new PlayGameResponse() { s2c_code = Response.CODE_ERROR };
            }
        }
        //----------------------------------------------------------------------------------------------
        // Promise 写法，等同于 rpc_PlayGameAsync
        [RpcHandler(typeof(PlayGameRequest), typeof(PlayGameResponse))]
        public void rpc_PlayGame(PlayGameRequest req, OnRpcReturn<PlayGameResponse> callback)
        {
            // <--- 此处如果不加锁，假如同时两个PlayGameRequest过来，则可能会导致coinCount为负数
            this.coinLocker.LockAsync().ContinueWith((_lock)=>
            {
                //判断硬币数量够不够
                if (coinCount > req.count)
                {
                    //异步调用，消耗硬币
                    machine.Call<PlayGameResponse>(req, new OnRpcReturn<PlayGameResponse>((rsp, err) =>
                    {
                        //消耗硬币
                        coinCount -= rsp.usedCoin;
                        //返回结果
                        callback(rsp);
                        _lock.Dispose();
                    }));
                }
                else
                {
                    //返回失败结果
                    callback(new PlayGameResponse() { s2c_code = Response.CODE_ERROR });
                    _lock.Dispose();
                }
            });
        }
        //----------------------------------------------------------------------------------------------
    }

    public class PlayGameRequest : Request
    {
        public int count;
    }
    public class PlayGameResponse : Response
    {
        public int usedCoin;
    }
}
