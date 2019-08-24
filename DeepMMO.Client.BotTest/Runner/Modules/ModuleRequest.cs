using CommonAI.Zone.Helper;
using CommonAI.ZoneClient;
using CommonLang;
using CommonLang.ByteOrder;
using CommonLang.Reflection;
using Pomelo.DotNetClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZeusBotTest.Runner
{
    public class ModuleRequest : BotRunner.RunnerModule
    {
        private static List<Type> all_request = new List<Type>(EventTypes.RequestTypes);
        private static List<Type> all_notify = new List<Type>(EventTypes.NotifyTypes);

        public ModuleRequest(BotRunner r) : base(r) { }

        protected internal override void OnBattleActorReady(CommonAI.ZoneClient.ZoneLayer layer, CommonAI.ZoneClient.ZoneActor actor)
        {
            layer.AddTimePeriodicMS(1000, (t) =>
            {
                if (Enable)
                {
                    on_do_1s(layer);
                }
            });
        }

        private void on_do_1s(ZoneLayer layer)
        {
            layer.AddTimeDelayMS(bot.Random.Next(1000, 10000), (t) =>
            {
                if (Enable)
                {
                    client.GameSocket.playerHandler.battleEventNotify(null);
                    client.GameSocket.rankHandler.getRankInfoRequest((err, rsp) => { });
                }
            });
            
            {
                var req_type = CUtils.GetRandomInArray<Type>(all_request, bot.Random);
                var req = ReflectionUtil.CreateInstance(req_type);
                fill_random(req);
                log.Info("request : " + req_type);
                bot.Client.GameSocket.request(req, (err, rsp) =>
                {
                    log.Info("response : rsp=" + rsp + " : err=" + err);
                });
            }
            {
                var ntf_type = CUtils.GetRandomInArray<Type>(all_notify, bot.Random);
                var ntf = ReflectionUtil.CreateInstance(ntf_type);
                fill_random(ntf);
                log.Info("notify : " + ntf_type);
                bot.Client.GameSocket.notify(ntf);
            }
            {
                var battleMessage = random_bytes();
                client.GameSocket.playerHandler.battleEventNotify(battleMessage);
            }
            {
                var req_type = CUtils.GetRandomInArray<Type>(all_request, bot.Random);
                socket_start_send(EventTypes.GetRequestKey(req_type), (uint)bot.Random.Next());
            }
            {
                var ntf_type = CUtils.GetRandomInArray<Type>(all_notify, bot.Random);
                socket_start_send(EventTypes.GetNotifyKey(ntf_type));
            }
        }

        private byte[] random_bytes(int min_len = 32, int max_len = 1024 * 1024)
        {
            var random = bot.Random;
            var bin = new byte[bot.Random.Next(min_len, max_len)];
            for (int i = 0; i < bin.Length; i++)
            {
                bin[i] = (byte)random.Next();
            }
            return bin;
        }




        private void fill_random(object obj)
        {
            var type = obj.GetType();
            var random = bot.Random;
            foreach (var f in type.GetProperties())
            {
                try
                {
                    var ft = f.GetGetMethod().ReturnType;
                    if (ft == typeof(sbyte))
                    {
                        f.GetSetMethod().Invoke(obj, new object[] { (sbyte)random.Next() });
                    }
                    else if (ft == typeof(int))
                    {
                        f.GetSetMethod().Invoke(obj, new object[] { (int)random.Next() });
                    }
                    else if (ft == typeof(short))
                    {
                        f.GetSetMethod().Invoke(obj, new object[] { (short)random.Next() });
                    }
                    else if (ft == typeof(long))
                    {
                        f.GetSetMethod().Invoke(obj, new object[] { (long)random.Next() });
                    }
                    else if (ft == typeof(byte))
                    {
                        f.GetSetMethod().Invoke(obj, new object[] { (byte)random.Next() });
                    }
                    else if (ft == typeof(uint))
                    {
                        f.GetSetMethod().Invoke(obj, new object[] { (uint)random.Next() });
                    }
                    else if (ft == typeof(ushort))
                    {
                        f.GetSetMethod().Invoke(obj, new object[] { (ushort)random.Next() });
                    }
                    else if (ft == typeof(ulong))
                    {
                        f.GetSetMethod().Invoke(obj, new object[] { (ulong)random.Next() });
                    }
                    else if (ft == typeof(float))
                    {
                        f.GetSetMethod().Invoke(obj, new object[] { (float)random.NextDouble() });
                    }
                    else if (ft == typeof(double))
                    {
                        f.GetSetMethod().Invoke(obj, new object[] { (double)random.NextDouble() });
                    }
                    else if (ft == typeof(string))
                    {
                        f.GetSetMethod().Invoke(obj, new object[] { random.Next().ToString() });
                    }
                    else if (ft == typeof(byte[]))
                    {
                        f.GetSetMethod().Invoke(obj, new object[] { random_bytes() });
                    }
                }
                catch (Exception err)
                {
                    err.ToString();
                }
            }
        }


        private void socket_start_send(string route, uint msgid = 0)
        {
            var send_object = SendMessage.Alloc(route, msgid, random_bytes());
            try
            {
                client.GameSocket.Session.BeginSend(
                    send_object.Buffer, 0, send_object.BufferLength,
                    System.Net.Sockets.SocketFlags.None,
                    socket_end_send, send_object);
            }
            catch (Exception err)
            {
                log.Error(err.Message);
                send_object.Dispose();
            }
        }
        private void socket_end_send(IAsyncResult asyncSend)
        {
            var send_object = asyncSend.AsyncState as SendMessage;
            try
            {
                client.GameSocket.Session.EndSend(asyncSend);
            }
            catch (Exception err)
            {
                log.Error(err.Message);
            }
            finally
            {
                send_object.Dispose();
            }
        }
    }
}
