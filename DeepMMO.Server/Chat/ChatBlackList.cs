using CommonLang;
using CommonLang.IO;
using System;
using System.Collections.Generic;

namespace CommonRPG.Server.Chat
{
    public class ChatBlackList : ISerializable
    {
        public List<string> blacklist;
    }

    public class ChatBanList : ISerializable
    {
        public HashMap<string, DateTime> banlist;
    }
}
