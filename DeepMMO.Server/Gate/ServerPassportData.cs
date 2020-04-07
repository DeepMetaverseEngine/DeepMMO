using DeepMMO.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeepMMO.Server.Gate
{
    public class ServerPassportData
    {
        public bool Verified;
        public RolePrivilege Privilege;
        public ServerPassportData(bool verified, RolePrivilege privilege)
        {
            this.Verified = verified;
            this.Privilege = privilege;
        }
    } public class ServerPassportEnterGame
    {
        public bool Verified;
        public string Message;
    }
}
