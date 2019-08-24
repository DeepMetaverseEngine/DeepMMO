using DeepCore;
using DeepCore.IO;
using DeepCore.ORM;
using System;
using System.Collections.Generic;

namespace DeepMMO.Data
{
    [MessageType(Constants.ACCOUNT_START + 1)]
    public class AccountData : ISerializable, IObjectMapping
    {
        [PersistField]
        public string uuid;

        [PersistField]
        public string token;

        [PersistField]
        public DateTime lastLoginTime;

        [PersistField]
        public string lastLoginConnectAddress;

        [PersistField]
        public string lastLoginServerID;
        [PersistField]
        public string lastLoginServerGroupID;

        [PersistField]
        public string lastLoginRoleID;

        //[PersistField]
        //public List<RoleIDSnap> roleIDList;

        [PersistField(PersistStrategy.SaveLoadImmediately)]
        public string lastLoginToken;

        /// <summary>
        /// 用户权限
        /// </summary>
        [PersistField]
        public RolePrivilege privilege = RolePrivilege.User_Player;
    }

    public class RoleIDSnap : ISerializable, IStructMapping
    {
        public string roleUUID;
        public string serverID;
        public int lv;
        public byte pro;
        public byte gender;
        public string name;
    }

    [MessageType(Constants.ACCOUNT_START + 2)]
    public class AccountRoleSnap : ISerializable, IObjectMapping
    {
        [PersistField]
        public HashMap<string, RoleIDSnap> roleIDMap;

        public AccountRoleSnap()
        {
            roleIDMap = new HashMap<string, RoleIDSnap>();
        }
    }

    [MessageType(Constants.ACCOUNT_START + 3)]
    public class AccountExtData : ISerializable, IObjectMapping
    {
        [PersistField]
        public HashMap<string, string> data;

        public AccountExtData()
        {
            data = new HashMap<string, string>();
        }
    }
}
