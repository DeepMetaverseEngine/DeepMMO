using DeepCore.IO;
using System;
using System.Collections.Generic;
using DeepMMO.Data;

namespace DeepMMO.Protocol.Client
{
    public interface ILogicProtocol { }
    //--------------------------------------------------------------------------------
    [MessageType(Constants.LOGIC_START + 1)]
    public class ClientPing : Request, ILogicProtocol
    {
        public DateTime time = DateTime.Now;
        public byte[] rawdata;
    }
    [MessageType(Constants.LOGIC_START + 2)]
    public class ClientPong : Response, ILogicProtocol
    {
        public DateTime time;
        public byte[] rawdata;
    }
    [MessageType(Constants.LOGIC_START + 4)]
    public class LogicTimeNotify : Response, ILogicProtocol
    {
        public int index;
        public DateTime time;
    }

    //--------------------------------------------------------------------------------

    /// <summary>
    /// 通知客户端角色信息变更
    /// </summary>
    [MessageType(Constants.LOGIC_START + 3)]
    public class PlayerDynamicNotify : Notify, ILogicProtocol, INetProtocolS2C
    {
        public List<PropertyStruct> s2c_data;
    }


}
