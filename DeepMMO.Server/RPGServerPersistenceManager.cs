using DeepCore.Log;
using DeepCore.ORM;
using DeepCore.Reflection;
using DeepCrystal;
using DeepCrystal.ORM;
using DeepCrystal.ORM.Generic;
using DeepCrystal.ORM.Query;
using DeepCrystal.RPC;
using DeepCrystal.Threading;
using DeepMMO.Data;
using DeepMMO.Protocol.Client;
using DeepMMO.Server.Logic;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DeepMMO.Server
{
    public class RPGServerPersistenceManager : DeepCore.Disposable
    {
        //--------------------------------------------------------------------------------------------------------------------------
        #region Singleton
        private static readonly object lock_init = new object();
        private static bool init_done = false;
        private static RPGServerPersistenceManager instance;
        public static bool IsInitDone { get { return init_done; } }
        public static RPGServerPersistenceManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lock_init)
                    {
                        if (!init_done)
                        {
                            var config = IService.GlobalConfig;
                            instance = ReflectionUtil.CreateInterface<RPGServerPersistenceManager>(GlobalConfig.RPGServerPersistenceManager);
                            instance.Init();
                            init_done = true;
                        }
                    }
                }
                return instance;
            }
        }
        #endregion
        //--------------------------------------------------------------------------------------------------------------------------


        private DeepCrystal.ORM.IMappingHash mappingNameToUUID;
        private DeepCrystal.ORM.IMappingHash mappingUUIDToName;
        private DeepCrystal.ORM.IMappingHash mappingDigitToUUID;
        private DeepCrystal.ORM.IMappingHash mappingUUIDToDigit;
        public DateTime ServerInitTimeUTC { get; private set; }
        public RPGServerPersistenceManager()
        {
            instance = this;
        }
        public virtual void Init()
        {
            Task.Run(async () =>
            {
                using (var start = ORMFactory.Instance.DefaultAdapter.GetHash("SERVER_INIT", null))
                {
                    var now = DateTime.UtcNow;
                    if (await start.SetAsync(nameof(ServerInitTimeUTC), now, When.NotExists))
                    {
                        ServerInitTimeUTC = now;
                    }
                    else
                    {
                        ServerInitTimeUTC = await start.GetAsync<DateTime>(nameof(ServerInitTimeUTC));
                    }
                }
                this.mappingNameToUUID = DeepCrystal.ORM.ORMFactory.Instance.DefaultAdapter.GetHash("Mapping:NameToUUID", null);
                this.mappingUUIDToName = DeepCrystal.ORM.ORMFactory.Instance.DefaultAdapter.GetHash("Mapping:UUIDToName", null);
                this.mappingDigitToUUID = DeepCrystal.ORM.ORMFactory.Instance.DefaultAdapter.GetHash("Mapping:DigitToUUID", null);
                this.mappingUUIDToDigit = DeepCrystal.ORM.ORMFactory.Instance.DefaultAdapter.GetHash("Mapping:UUIDToDigit", null);
            }).Wait();
        }
        protected override void Disposing()
        {
            mappingNameToUUID?.Dispose();
            mappingUUIDToName?.Dispose();
            mappingDigitToUUID?.Dispose();
            mappingUUIDToDigit?.Dispose();
            this.mappingNameToUUID = null;
            this.mappingUUIDToName = null;
            this.mappingDigitToUUID = null;
            this.mappingUUIDToDigit = null;
        }
        //--------------------------------------------------------------------------------------------------------------------------
        #region RoleNameMapping
        //-------------------------------------------------------------------------------------------------------------------------- 
        protected virtual string GenDigitID(string roleUUID)
        {
            var duration = DateTime.UtcNow - ServerInitTimeUTC;
            var prifix = ((int)duration.TotalMilliseconds);
            var suffix = ((int)roleUUID[0]) % 10;
            return $"{prifix}{suffix}";
        }
        public virtual Task<string> GetRoleNameByUUIDAsync(string roleUUID, ITaskExecutor svc)
        {
            return svc.Execute(mappingUUIDToName.GetAsync(roleUUID).ContinueWith<string>(t => t.GetResultToString()));
        }

        public virtual Task<IConvertible[]> GetRoleNameByUUIDAsync(string[] roleUUID, ITaskExecutor svc)
        {
            return svc.Execute(mappingUUIDToName.GetAsync(roleUUID));
        }

        public virtual Task<string> GetRoleUUIDByNameAsync(string roleName, ITaskExecutor svc)
        {
            return svc.Execute(mappingNameToUUID.GetAsync(roleName).ContinueWith<string>(t => t.GetResultToString()));
        }
        public virtual Task<string> GetRoleDigitByUUIDAsync(string roleUUID, ITaskExecutor svc)
        {
            return svc.Execute(mappingUUIDToDigit.GetAsync(roleUUID).ContinueWith<string>(t => t.GetResultToString()));
        }
        public virtual Task<string[]> GetRoleUUIDByDigitAsync(string digit, ITaskExecutor svc)
        {
            return svc.Execute(mappingDigitToUUID.GetAsync(digit).ContinueWith<string[]>(t =>
            {
                if (t.IsCompleted)
                {
                    var exist = t.GetResultToString();
                    if (exist != null)
                    {
                        return exist.Split(',');
                    }
                }
                return null;
            }));
        }
        public virtual Task<string> TryRegistRoleNameMappingAsync(string roleUUID, string roleName, ITaskExecutor svc)
        {
            return svc.Execute(async () =>
            {
                if (await mappingNameToUUID.SetAsync(roleName, roleUUID, When.NotExists))
                {
                    await mappingUUIDToName.SetAsync(roleUUID, roleName);
                    var digitID = Instance.GenDigitID(roleUUID);
                    if (await mappingDigitToUUID.SetAsync(digitID, roleUUID, When.NotExists) == false)
                    {
                        var exist = await mappingDigitToUUID.GetAsync(digitID);
                        await mappingDigitToUUID.SetAsync(digitID, $"{exist},{roleUUID}");
                    }
                    await mappingUUIDToDigit.SetAsync(roleUUID, digitID);
                    return digitID;
                }
                return null;
            });
        }

        public virtual Task<bool> RoleChangeNameMappingAsync(string roleUUID, string newName, string curName, ITaskExecutor svc)
        {
            return svc.Execute(async () =>
            {
              
                if (await mappingNameToUUID.SetAsync(newName, roleUUID, When.NotExists))
                {
                    //删除旧的名字.
                    await mappingNameToUUID.DeleteAsync(curName);
                    //删除旧的UUID关联.
                    await mappingUUIDToName.DeleteAsync(roleUUID);
                    //设置新的UUID关联.
                    await mappingUUIDToName.SetAsync(roleUUID, newName);
                    return true;
                }
                return false;
            });
        }

        public virtual Task<bool> RoleNameExist(string roleName,ITaskExecutor svc)
        {
            return svc.Execute(async () =>
            {
                return await mappingNameToUUID.ExistsAsync(roleName);
            });
           
        }

        #endregion
        //--------------------------------------------------------------------------------------------------------------------------
        #region NameChecking
        //匹配中文，英文字母和数字及_: 
        private Regex roleNamePattern = new Regex(@"^[\u4e00-\u9fa5_a-zA-Z0-9]+$");
        /// <summary>
        /// 检查角色名是否合法
        /// </summary>
        /// <param name="roleName"></param>
        /// <returns></returns>
        public virtual bool CheckRoleName(string roleName)
        {
            if (roleNamePattern.IsMatch(roleName))
            {
                return true;
            }
            return false;
        }

        #endregion
        //--------------------------------------------------------------------------------------------------------------------------

        public const string TYPE_ACCOUNT_DATA = "Account:";
        //账号下所有角色数据.
        public const string TYPE_ACCOUNT_ROLE_SNAP_DATA = "Account:AccountRoleSnap:";
        public const string TYPE_ACCOUNT_EXT_DATA = "Account:AccountExt";
        public const string TYPE_ROLE_DATA = "Role:";
        public const string TYPE_ROLE_SNAP_DATA = "RoleSnap:";
        public const string TYPE_ROLE_SNAP_EXT_DATA = "RoleSnapExt:";

        public const string TYPE_ROLE_SNAP_EXT_BIN_DATA = "RoleSnapBinExt:";

        //同区内每个服记录.
        public const string TYPE_SERVER_RECORD_DATA = "ServerRecord";
        //账号封停数据.
        public const string TYPE_ROLE_DATA_STATUS_SNAP_DATA = "RoleDataStatusSnap:";

        /// <summary>
        /// 创建角色，由Session.Roled调用ORM.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public virtual async Task<RoleSnap> CreateRoleDataAsync(ServerRoleData data, ITaskExecutor svc)
        {
            var roleMapping = new MappingReference<ServerRoleData>(TYPE_ROLE_DATA, data.uuid, svc);
            await roleMapping.SaveDataAsync(data);
            var snapData = InitRoleSnap(data, new RoleSnap());

            // Snap数据映射
            var snapMapping = new MappingReference<RoleSnap>(TYPE_ROLE_SNAP_DATA, data.uuid, svc);
            await snapMapping.SaveDataAsync(snapData);

            return snapData;
        }

        public virtual async Task DeleteRoleDataAsync(string c2s_role_uuid, ITaskExecutor svc)
        {
            var snapMapping = new MappingReference<RoleSnap>(TYPE_ROLE_SNAP_DATA, c2s_role_uuid, svc);
            var roleSnap =   await snapMapping.LoadDataAsync();
            // TODO 
        }

        public virtual async Task<AccountData> GetOrCreateAccountDataAsync(MappingReference<AccountData> saveAcc, string accountName, string accountToken)
        {
            if (await saveAcc.EnterLockAsync(out var token))
            {
                try
                {
                    var accountData = await saveAcc.LoadOrCreateDataAsync(() =>
                    {
                        var ret = new AccountData();
                        ret.uuid = accountName;
                        ret.token = accountToken;
                        return ret;
                    });
                    return accountData;
                }
                finally
                {
                    await saveAcc.ExitLockAsync(token);
                }
            }

            return null;
        }

        public virtual QueryMappingReference<T> GetQueryReference<T>(string typeName, ITaskExecutor svc, IMappingAdapter db = null) where T : IObjectMapping
        {
            /* TEST
            Task.Run(async () => 
            {
                var trans = ORMFactory.Instance.CreateTransaction(ORMFactory.Instance.DefaultAdapter);
                trans.AddCondition(ORMFactory.Instance.Conditions.HashEqual("key", "fieldA", 12345));
                trans.AddCondition(ORMFactory.Instance.Conditions.HashNotEqual("key", "fieldB", 12345));
                using (var save = trans.GetHash("key", svc))
                {
                    await save.SetAsync("fieldA", 1);
                    await save.SetAsync("fieldB", 2);
                    await save.SetAsync("fieldC", "ccc");
                    await trans.ExecuteAsync(svc);
                }
            });
            */
            return new QueryMappingReference<T>(typeName, svc, db);
        }

        protected virtual RoleSnap InitRoleSnap(ServerRoleData roleData, RoleSnap ret)
        {
            ret.uuid = roleData.uuid;
            ret.digitID = roleData.digitID;
            ret.name = roleData.name;
            ret.account_uuid = roleData.account_uuid;
            ret.role_template_id = roleData.role_template_id;
            ret.unit_template_id = roleData.unit_template_id;
            ret.level = roleData.Level;
            ret.create_time = roleData.create_time;
            ret.last_login_time = roleData.last_login_time;
            ret.server_id = roleData.server_id;
            ret.privilege = roleData.privilege;
            return ret;
        }

        //--------------------------------------------------------------------------------------------------------------------------
        #region ServerRoleIDMapping.

        public class ServerRoleIDMappingSet
        {
            private const string TYPE_SERVER_ROLEID_DATA = "ServerID:{0}:RoleID:";
            private readonly DeepCrystal.ORM.IMappingSet mappingSet;

            public ServerRoleIDMappingSet(ITaskExecutor svc, string serverID)
            {
                string key = string.Format(TYPE_SERVER_ROLEID_DATA, serverID);
                this.mappingSet = DeepCrystal.ORM.ORMFactory.Instance.DefaultAdapter.GetSet(key, svc);
            }

            public Task AddRoleIDAsync(string playerUUID)
            {
                return mappingSet.AddAsync(playerUUID);
            }

            public Task<string[]> GetRoleIDsAsync()
            {
                return mappingSet.MembersAsync().ContinueWith(t =>
                {
                    var rst = t.GetResultAs();
                    if (rst != null) return Array.ConvertAll(rst, (s) => s.ToString());
                    return null;
                });
            }
        }

        public virtual ServerRoleIDMappingSet GetServerRoleIDMappingSet(ITaskExecutor svc, string serverid)
        {
            return new ServerRoleIDMappingSet(svc, serverid);
        }
        #endregion
        //--------------------------------------------------------------------------------------------------------------------------

        public virtual void SaveBICreateRoleInfo(Logger log, ServerRoleData data, string channel)
        {

        }
    }
}
