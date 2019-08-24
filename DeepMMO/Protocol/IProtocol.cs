using DeepCore;
using DeepCore.IO;
using DeepCore.Reflection;
using DeepMMO.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DeepMMO.Protocol
{
    /// <summary>
    /// 网络序列化标识接口
    /// </summary>
    public interface INetProtocol : ISerializable { }
    public interface INetProtocolS2C { }
    public interface INetProtocolC2S { }
    public interface INetProtocolBotIgnore { }

    /// <summary>
    /// 请求
    /// </summary>
    public abstract class Request : INetProtocol, INetProtocolC2S
    {
        public override string ToString()
        {
            return string.Format("{0}", GetType().Name);
        }
    }

    /// <summary>
    /// 回馈
    /// </summary>
    public abstract class Response : INetProtocol, INetProtocolS2C
    {
        [MessageCodeAttribute("成功")]
        public const int CODE_OK = 200;
        [MessageCodeAttribute("未知错误")]
        public const int CODE_ERROR = 500;
        /// <summary>
        /// 返回码
        /// </summary>
        public int s2c_code = CODE_OK;
        /// <summary>
        /// 返回信息（优先网络消息，如果网络消息为空，则从MessageCode中找）
        /// </summary>
        public string s2c_msg;

        /// <summary>
        /// 请求是否成功
        /// </summary>
        public bool IsSuccess
        {
            get { return s2c_code >= 200 && s2c_code <= 299; }
        }
        public override string ToString()
        {
            return string.Format("{0}: {1} : {2}", GetType().Name, s2c_code, s2c_msg);
        }
        public static bool CheckSuccess(Response rsp)
        {
            return (rsp != null && rsp.IsSuccess);
        }
        public virtual void EndRead()
        {
            if (s2c_msg == null)
                s2c_msg = MessageCodeManager.Instance.GetCodeMessage(this);
        }
    }

    /// <summary>
    /// 单向通知
    /// </summary>
    public abstract class Notify : INetProtocol
    {
    }

}
