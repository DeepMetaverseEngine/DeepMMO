using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeepMMO.Attributes
{

    //     [AttributeUsage(AttributeTargets.Field)]
    //     public class TemplateFiledAttribute : System.Attribute
    //     {
    //         public TemplateFiledAttribute(string desc, bool primaryKey = false)
    //         {
    //             this.Desc = desc;
    //             this.PrimaryKey = primaryKey;
    //         }
    //         public string Desc
    //         {
    //             get; private set;
    //         }
    //         public bool PrimaryKey
    //         {
    //             get; private set;
    //         }
    //     }

    /// <summary>
    /// 用于标记协议类型是从什么服务到什么服务，防止错误的服务调用。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ProtocolRouteAttribute : System.Attribute
    {
        public string From { get; private set; }
        public string To { get; private set; }
        public ProtocolRouteAttribute(string from, string to)
        {
            this.From = from;
            this.To = to;
        }
    }

    /// <summary>
    /// 标记错误代码表示的文字
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class MessageCodeAttribute : System.Attribute
    {
        public string Message { get; set; }
        public string[] Args { get; set; }
        public MessageCodeAttribute(string msg)
        {
            this.Message = msg;
        }
        public MessageCodeAttribute(string msg, string[] args)
        {
            this.Message = msg;
            this.Args = args;
        }
        public override string ToString()
        {
            return Message;
        }
    }
}
