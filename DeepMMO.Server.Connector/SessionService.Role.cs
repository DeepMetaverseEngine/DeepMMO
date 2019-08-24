using DeepCore;
using DeepMMO.Data;
using DeepMMO.Protocol.Client;
using DeepCrystal.RPC;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DeepCrystal.ORM.Generic;
using DeepMMO.Server.Logic;

namespace DeepMMO.Server.Connect
{
    //---------------------------------------------------------------------------------------
    //处理角色相关逻辑.
    //---------------------------------------------------------------------------------------
    public partial class SessionService
    {
        [RpcHandler(typeof(ClientCreateRoleRequest), typeof(ClientCreateRoleResponse), ServerNames.ConnectServerType)]
        public virtual async Task<ClientCreateRoleResponse> client_rpc_Handle(ClientCreateRoleRequest req)
        {
            try
            {
                var serverID = await accountSave.LoadFieldAsync<string>(nameof(AccountData.lastLoginServerID));
                var privilege = await accountSave.LoadFieldAsync<RolePrivilege>(nameof(AccountData.privilege));
                var accountData = accountSave.Data;
                var roleIDMap = accountRoleSnapSave.Data.roleIDMap;
                if (roleIDMap == null)
                {
                    roleIDMap = new HashMap<string, RoleIDSnap>();
                }

                int roleCount = 0;
                foreach (var item in roleIDMap)
                {
                    if (item.Value.serverID == serverID)
                    {
                        roleCount++;
                    }
                }

                if (roleCount >= RPGServerTemplateManager.Instance.GetRoleMaxCount())//该服下账号是否达到创建上限.
                {
                    return new ClientCreateRoleResponse() { s2c_code = ClientCreateRoleResponse.CODE_CREATE_ROLE_LIMIT };
                }
                else if (RPGServerTemplateManager.Instance.IsBlackWord(req.c2s_name))
                {
                    return new ClientCreateRoleResponse() { s2c_code = ClientCreateRoleResponse.CODE_BLACK_NAME };
                }
                else if (!RPGServerPersistenceManager.Instance.CheckRoleName(req.c2s_name))
                {
                    //名字异常，长度不符合规范
                    return new ClientCreateRoleResponse() { s2c_code = ClientCreateRoleResponse.CODE_CREATE_ROLE_INVAILD };
                }
                // 创建纯数据
                var roleData = RPGServerTemplateManager.Instance.CreateRoleData(req, accountID, serverID);
                //用户权限.
                roleData.privilege = privilege;

                if (roleData == null)
                {
                    return new ClientCreateRoleResponse() { s2c_code = ClientCreateRoleResponse.CODE_TEMPLATE_NOT_EXIST };
                }
                var digitID = await RPGServerPersistenceManager.Instance.TryRegistRoleNameMappingAsync(roleData.uuid, roleData.name, this);
                if (digitID == null)
                {
                    return new ClientCreateRoleResponse() { s2c_code = ClientCreateRoleResponse.CODE_NAME_ALREADY_EXIST };
                }
                roleData.digitID = digitID;

                // Role数据映射
                var snapData = await RPGServerPersistenceManager.Instance.CreateRoleDataAsync(roleData, this);


                var roleIDSnap = RPGServerTemplateManager.Instance.CreateRoleIDSnapData(roleData);
                roleIDMap.Add(roleIDSnap.roleUUID, roleIDSnap);
                accountRoleSnapSave.SetField(nameof(AccountRoleSnap.roleIDMap), roleIDMap);
                await accountRoleSnapSave.FlushAsync();
                //单区内角色记录
                var serverRoleIDSet = RPGServerPersistenceManager.Instance.GetServerRoleIDMappingSet(this, roleData.server_id);
                await serverRoleIDSet.AddRoleIDAsync(roleData.uuid);
                var ret = new ClientCreateRoleResponse()
                {
                    s2c_role = snapData
                };
                //网络协议接口日志//
                //log.Log(ret);
                //BI创角记录.
                RPGServerPersistenceManager.Instance.SaveBICreateRoleInfo(log, roleData,Channel);
                return ret;
            }
            catch (Exception err)
            {
                log.Error(string.Format("ClientCreateRoleRequest Handle Error:account = {0}  msg = {1} ", accountID, err.Message), err);
                return (new ClientCreateRoleResponse()
                {
                    s2c_code = ClientCreateRoleResponse.CODE_ERROR,
                    s2c_msg = err.Message
                });
            }
        }

        [RpcHandler(typeof(ClientGetRandomNameRequest), typeof(ClientGetRandomNameResponse), ServerNames.ConnectServerType)]
        public virtual Task<ClientGetRandomNameResponse> client_rpc_Handle(ClientGetRandomNameRequest req)
        {
            try
            {
                var rd = RPGServerTemplateManager.Instance.GetRoleTemplate(req.c2s_role_template_id, req.c2s_role_gender);
                if (rd != null)
                {
                    //获取随机名字方法.
                    return Task.FromResult(new ClientGetRandomNameResponse()
                    {
                        s2c_name = RPGServerTemplateManager.Instance.RandomName(rd)
                    });
                }
                else
                {
                    return Task.FromResult(new ClientGetRandomNameResponse() { s2c_code = ClientCreateRoleResponse.CODE_ERROR, });
                }
            }
            catch (Exception err)
            {
                log.ErrorFormat("ClientGetRandomNameRequest Handle Error:account = {0} msg = {1} ", accountID, err.Message);
                return Task.FromResult(new ClientGetRandomNameResponse() { s2c_code = ClientCreateRoleResponse.CODE_ERROR, s2c_msg = err.Message });
            }
        }

        [RpcHandler(typeof(ClientGetRolesRequest), typeof(ClientGetRolesResponse), ServerNames.ConnectServerType)]
        public virtual async Task<ClientGetRolesResponse> client_rpc_Handle(ClientGetRolesRequest req)
        {
            try
            {
                var serverID = await accountSave.LoadFieldAsync<string>(nameof(AccountData.lastLoginServerID));
                var accountData = accountSave.Data;
                var roleIDMap = accountRoleSnapSave.Data.roleIDMap;
                // using (var saveAcc = PersistenceFactory.Instance.Get<AccountData>(null, accountID))

                if (roleIDMap != null && roleIDMap.Count > 0)
                {
                    List<RoleSnap> ret = new List<RoleSnap>();
                    foreach (var item in roleIDMap)
                    {
                        if (item.Value.serverID == serverID)
                        {
                            var snap = await queryRoleSnap.LoadDataAsync(item.Value.roleUUID);
                            if (snap != null && snap.server_id == serverID)
                            {
                                ret.Add(snap);
                            }
                        }
                    }
                    return (new ClientGetRolesResponse()
                    {
                        s2c_code = ClientGetRolesResponse.CODE_OK,
                        s2c_roles = ret
                    });
                }
                else
                {
                    return (new ClientGetRolesResponse() { s2c_code = ClientGetRolesResponse.CODE_OK });
                }
            }
            catch (Exception err)
            {
                log.ErrorFormat("ClientGetRolesRequest Handle Error:account = {0} msg = {1} ", accountID, err.Message);
                return (new ClientGetRolesResponse() { s2c_code = ClientGetRolesResponse.CODE_ERROR, s2c_msg = err.Message });
            }
        }

        [RpcHandler(typeof(ClientDeleteRoleRequest), typeof(ClientDeleteRoleResponse), ServerNames.ConnectServerType)]
        public virtual async Task<ClientDeleteRoleResponse> client_rpc_Handle(ClientDeleteRoleRequest req)
        {
            try
            {
                if (string.IsNullOrEmpty(req.c2s_role_uuid))
                {
                    return (new ClientDeleteRoleResponse() { s2c_code = ClientDeleteRoleResponse.CODE_ROLEID_INVAILD });
                }
                else
                {
                    var roleIDMap = accountRoleSnapSave.Data.roleIDMap;
                    if (roleIDMap.Remove(req.c2s_role_uuid))
                    {
                        accountRoleSnapSave.SetMappingField(nameof(AccountRoleSnap.roleIDMap), roleIDMap);
                        await accountRoleSnapSave.FlushAsync();
                        //ILogBI接口日志//
                        //                         log.Log(new ClientDeleteRoleLog()
                        //                         {
                        //                             account = this.accountID,
                        //                             c2s_role_uuid = req.c2s_role_uuid
                        //                         });
                        return (new ClientDeleteRoleResponse());
                    }
                    else
                    {
                        return (new ClientDeleteRoleResponse() { s2c_code = ClientDeleteRoleResponse.CODE_ROLEID_INVAILD });
                    }
                }

            }
            catch (Exception err)
            {
                log.ErrorFormat("ClientDeleteRoleRequest Handle Error:account = {0}  msg = {1} ", accountID, err.Message);
                return (new ClientDeleteRoleResponse() { s2c_code = ClientDeleteRoleResponse.CODE_ERROR, s2c_msg = err.Message });
            }
        }
    }

    //     public class ClientDeleteRoleLog
    //     {
    //         public string account;
    //         public string c2s_role_uuid;
    //     }
}
