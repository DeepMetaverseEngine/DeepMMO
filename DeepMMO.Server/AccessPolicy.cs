using DeepMMO.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeepMMO.Server
{
    public class PolicyInfo
    {
        public string Key { get; }
        public RolePrivilege[] Privileges { get; }
    }

    public class AccessPolicy
    {
        public void AddPolicy(PolicyInfo info) { }

        public bool ValidateRolePolicy(string policy, RolePrivilege privilege)
        {
            return true;
        }
    }
}
