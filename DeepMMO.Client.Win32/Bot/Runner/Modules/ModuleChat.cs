using CommonAI.ZoneClient;
using CommonLang;
using CommonLang.Concurrent;
using CommonLang.Reflection;
using CommonRPG.Protocol.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;


namespace CommonRPG.Client.BotTest.Runner.Modules
{
    public class ModuleChat : RunnerModule
    {
        #region ChatData
        private static AtomicInteger s_index = new AtomicInteger(0);
        private static List<string> s_chat_list = new List<string>();
        static ModuleChat()
        {
            var all = File.ReadAllLines(Application.StartupPath + @"\ChatData.txt");
            foreach (var line in all)
            {
                if (line.Trim().Length > 0)
                {
                    var kv = line.Split(new char[] { '：' }, 2);
                    if (kv.Length == 2)
                    {
                        s_chat_list.Add(kv[1]);
                    }
                    else
                    {
                        s_chat_list.Add(line);
                    }
                }
            }
        }
        #endregion

        private Random random = new Random();

        public ModuleChat(BotRunner r) : base(r)
        {
            base.Client.OnZoneActorEntered += Client_OnZoneActorEntered;
        }
        private void Client_OnZoneActorEntered(CommonAI.ZoneClient.ZoneActor obj)
        {
            obj.Parent.AddTimePeriodicMS(Config.ChatIntervalMS, (t) =>
            {
                if (base.IsEnable)
                {
                    do_interval();
                }
            });
        }

        private void do_interval()
        {
            var text = s_chat_list[(int)(s_index.GetAndIncrement() % s_chat_list.Count)];
            try
            {
                var channel = CUtils.GetRandomInArray(Config.ChatChannels, this.random);
                base.Runner.Client.GameClient.Request<ClientChatResponse>(new ClientChatRequest()
                {
                    channel_type = (short)channel,
                    content = text,
                }, (err, rsp) =>
                {
                    if (err != null) log.Error("SentChat : " + err.Message);
                    else log.Info("SentChat : " + rsp);
                });
                //                 bot.chat_SendChat((err, rsp) =>
                //                 {
                //                     if (err != null) log.Error("SentChat : " + err.Message);
                //                     else log.Info("SentChat : " + rsp);
                //                 }, text, "", channel);
            }
            catch (Exception err)
            {
                log.Error(err.Message, err);
            }
        }

        //-------------------------------------------------------------------------------------------------------------------

        [Desc("聊天配置")]
        [Expandable]
        public class Config : RunnerModuleConfig
        {
            [Desc("聊天发送间隔")]
            public static int ChatIntervalMS = 5000;
            [Desc("聊天发送频道")]
            public static ChatChannel[] ChatChannels = new ChatChannel[]
            {
                ChatChannel.World,
                ChatChannel.Trade,
            };
            public override string ToString()
            {
                return "聊天配置";
            }
        }
        public enum ChatChannel
        {
            World = ClientChatRequest.CHANNEL_TYPE_WORLD,
            Trade = ClientChatRequest.CHANNEL_TYPE_TRADE,
            Guild = ClientChatRequest.CHANNEL_TYPE_GUILD,
            Team = ClientChatRequest.CHANNEL_TYPE_TEAM,
            Battle = ClientChatRequest.CHANNEL_TYPE_BATTLE,
            Area = ClientChatRequest.CHANNEL_TYPE_AREA,
        }

    }
}
