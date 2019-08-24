using DeepMMO.Attributes;
using DeepMMO.Protocol;

namespace DeepMMO.Server.GameEvent
{
    [ProtocolRoute("*", "*")]
    public class ServerGameEventNotify : Notify
    {
        public string From;
        public string To;
        public bool Broadcast;
        public string ServerGroupID;
        public byte[] EventMessageData;
    }
    
}

