using DeepCore.IO;

namespace DeepMMO.Protocol.Client
{
    [MessageType(Constants.GAMEEVENT_START + 1)]
    public class ClientGameEventNotify : Notify, INetProtocolC2S, INetProtocolS2C
    {
        public string From;
        public string To;
        public byte[] EventMessageData;
    }


}