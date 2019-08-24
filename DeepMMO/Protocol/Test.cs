using DeepCore.IO;
using DeepMMO.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepMMO.Protocol
{
    [MessageType(0x00ff0000)]
    public class TestRequest : Request
    {
        public string c2s_account;
        public string c2s_gate_token;
        public string c2s_serverName;
        public DateTime c2s_time;
    }

    [MessageType(0x00ff0001)]
    public class TestResponse : Response
    {
        [MessageCode("没有连接服务器！")]
        public const int CODE_NO_CONNECT_SERVER = 404;
        [MessageCode("账户或密码错误！")]
        public const int CODE_ACCOUNT_OR_PASSWORD = 501;
        [MessageCode("账户不存在！")]
        public const int CODE_NO_ACCOUNT = 502;

        public string c2s_account;
        public string c2s_session_token;
    }
}
