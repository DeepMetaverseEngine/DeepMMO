using DeepCore.IO;

namespace DeepMMO.Server.SystemMessage
{

    /// <summary>
    /// 由系统发出关闭服务器协议，收到此协议后，Connector和Gate不在处理新的链接，并且将现有所有链接下线。
    /// </summary>
    public class SystemShutdownNotify : ISerializable
    {
        public string reason;
    }
    public class SystemGateReloadServerList : ISerializable
    {
    }
    /// <summary>
    /// 由系统发出允许正常玩家登陆。
    /// </summary>
    public class SystemGMServerOpenNotify : ISerializable
    {

    }

    /// <summary>
    /// 由系统发出所有静态服务已启动完毕。
    /// </summary>
    public class SystemStaticServicesStartedNotify : ISerializable
    {

    }

}
