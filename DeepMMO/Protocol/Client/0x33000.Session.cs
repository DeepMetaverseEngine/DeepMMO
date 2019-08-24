using DeepCore.IO;
using DeepCore.IO;
using DeepMMO.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DeepCore.Reflection;
using DeepMMO.Attributes;

namespace DeepMMO.Protocol.Client
{

    public interface ISessionProtocol { }
    //--------------------------------------------------------------------------------
    /// <summary>
    /// 选择角色
    /// </summary>
    [MessageType(Constants.SESSION_START + 1)]
    public class ClientSelectRoleRequest : Request, ISessionProtocol
    {
        public string c2s_roleUUID;
    }
    [MessageType(Constants.SESSION_START + 2)]
    public class ClientSelectRoleResponse : Response, ISessionProtocol
    {
    }

    //--------------------------------------------------------------------------------
    /// <summary>
    /// 创建角色
    /// </summary>
    [MessageType(Constants.SESSION_START + 3)]
    public class ClientCreateRoleRequest : Request, ISessionProtocol
    {
        public string c2s_name;
        public int c2s_template_id;
        /// <summary>
        /// 自定义扩展数据.
        /// </summary>
        public ISerializable c2s_extension_data;
    }
    [MessageType(Constants.SESSION_START + 4)]
    public class ClientCreateRoleResponse : Response, ISessionProtocol
    {
        [MessageCode("角色创建已达上限！")]
        public const int CODE_CREATE_ROLE_LIMIT = CODE_ERROR + 1;
        [MessageCode("无效的创建信息！")]
        public const int CODE_CREATE_ROLE_INVAILD = CODE_ERROR + 2;
        [MessageCode("角色模板信息不存在！")]
        public const int CODE_TEMPLATE_NOT_EXIST = CODE_ERROR + 3;
        [MessageCode("角色名已存在！")]
        public const int CODE_NAME_ALREADY_EXIST = CODE_ERROR + 4;
        [MessageCode("名字中含有敏感字符")]
        public const int CODE_BLACK_NAME = CODE_ERROR + 5;


        [DependOnProperty(nameof(IsSuccess))]
        public RoleSnap s2c_role;
    }

    [MessageType(Constants.SESSION_START + 5)]
    public class ClientGetRandomNameRequest : Request, ISessionProtocol
    {
        //0男1女.
        public byte c2s_role_gender;
        public int c2s_role_template_id;
    }
    [MessageType(Constants.SESSION_START + 6)]
    public class ClientGetRandomNameResponse : Response, ISessionProtocol
    {
        [DependOnProperty(nameof(IsSuccess))]
        public string s2c_name;
    }
    //--------------------------------------------------------------------------------

    /// <summary>
    /// 获取角色列表
    /// </summary>
    [MessageType(Constants.SESSION_START + 7)]
    public class ClientGetRolesRequest : Request, ISessionProtocol
    {

    }
    [MessageType(Constants.SESSION_START + 8)]
    public class ClientGetRolesResponse : Response, ISessionProtocol
    {
        [DependOnProperty(nameof(IsSuccess))]
        public List<RoleSnap> s2c_roles;
    }

    //--------------------------------------------------------------------------------


    //--------------------------------------------------------------------------------
    /// <summary>
    /// 删除角色请求.
    /// </summary>
    [MessageType(Constants.SESSION_START + 11)]
    public class ClientDeleteRoleRequest : Request, ISessionProtocol
    {
        public string c2s_role_uuid = null;

    }
    /// <summary>
    /// 删除角色结果.
    /// </summary>
    [MessageType(Constants.SESSION_START + 12)]
    public class ClientDeleteRoleResponse : Response, ISessionProtocol
    {
        [MessageCode("无效的角色ID！")]
        public const int CODE_ROLEID_INVAILD = 501;
    }

    //--------------------------------------------------------------------------------

    /// <summary>
    /// 进入游戏
    /// </summary>
    [MessageType(Constants.SESSION_START + 13)]
    public class ClientEnterGameRequest : Request, ISessionProtocol, INetProtocolBotIgnore
    {
        public string c2s_roleUUID;
    }
    [MessageType(Constants.SESSION_START + 14)]
    public class ClientEnterGameResponse : Response, ISessionProtocol, INetProtocolBotIgnore
    {
        [MessageCode("无效的角色ID！")]
        public const int CODE_ROLEID_INVAILD = 501;
        [MessageCode("角色逻辑不存在！")]
        public const int CODE_LOGIC_NOT_FOUND = 502;
        [MessageCode("角色已登录！")]
        public const int CODE_LOGIC_ALREADY_LOGIN = 503;
        [MessageCode("角色已封停")]
        public const int CODE_ROLE_SUSPEND = 504;
        [DependOnProperty(nameof(IsSuccess))]
        public ClientRoleData s2c_role;

        public DateTime s2c_suspendTime;
    }
    //--------------------------------------------------------------------------------

    /// <summary>
    /// 退出游戏
    /// </summary>
    [MessageType(Constants.SESSION_START + 15)]
    public class ClientExitGameRequest : Request, ISessionProtocol, INetProtocolBotIgnore
    {
        public string c2s_roleUUID;
    }
    [MessageType(Constants.SESSION_START + 16)]
    public class ClientExitGameResponse : Response, ISessionProtocol, INetProtocolBotIgnore
    {
    }

}
