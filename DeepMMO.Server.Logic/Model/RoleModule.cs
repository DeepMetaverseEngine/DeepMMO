using DeepCore;
using DeepCrystal.ORM;
using DeepCrystal.ORM.Generic;
using DeepCrystal.RPC;
using DeepMMO.Data;
using DeepMMO.Protocol.Client;
using DeepMMO.Server.AreaManager;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DeepMMO.Server.Logic.Model
{
    public class RoleModule : ILogicModule
    {
        public MappingReference roleMapping { get; protected set; }
        public MappingReference snapMapping { get; protected set; }
        public LanguageManager Language { get; protected set; }

        public int LastMapID { get => GetRoleData().last_map_template_id; }
        public int LastPublicMapID { get => GetRoleData().last_public_mapID; }
        public string LastPublicMapUUID { get => GetRoleData().last_public_area_uuid; }
        public ZonePosition LastPublicPos { get => GetRoleData().last_public_map_pos; }
        public string LastZoneUUID { get => GetRoleData().last_zone_uuid; }
        public ZonePosition LastPos { get => GetRoleData().last_zone_pos; }
        public RoleModule(LogicService service) : base(service)
        {
        }
        public override async Task OnStartAsync()
        {
            var role = new MappingReference<ServerRoleData>(RPGServerPersistenceManager.TYPE_ROLE_DATA, service.roleID, service);
            await role.LoadDataAsync();
            var snap = new MappingReference<RoleSnap>(RPGServerPersistenceManager.TYPE_ROLE_SNAP_DATA, service.roleID, service);
            await snap.LoadDataAsync();
            roleMapping = role;
            snapMapping = snap;

            var trans = service.DBAdapter.CreateExecutableObjectTransaction(service);
            try
            {
                role.SetField(nameof(ServerRoleData.onlineState), RoleState.STATE_ONLINE);
                role.SetField(nameof(ServerRoleData.last_login_time), DateTime.Now);
                role.BatchFlush(trans);
                snap.SetField(nameof(RoleSnap.last_login_time), role.Data.last_login_time);
                snap.SetField(nameof(RoleSnap.onlineState), RoleState.STATE_ONLINE);
                snap.SetField(nameof(RoleSnap.session_name), service.sessionName);
                snap.BatchFlush(trans);
            }
            finally
            {
                await trans.ExecuteAsync();
            }
            this.Language = RPGServerTemplateManager.Instance.GetLanguage(GetRoleData().local_code);
       
        }
        public override Task OnStartedAsync()
        {
            return Task.CompletedTask;
        }
        public override Task OnStopAsync()
        {
            this.roleMapping.SetField(nameof(ServerRoleData.onlineState), RoleState.STATE_OFFLINE);
            this.snapMapping.SetField(nameof(ServerRoleData.onlineState), RoleState.STATE_OFFLINE);
            return Task.CompletedTask;
        }
        public override void OnSaveData(IObjectTransaction trans)
        {
            this.roleMapping.BatchFlush(trans);
            this.BeginSaveRoleSnap(trans);
            this.snapMapping.BatchFlush(trans);
        }

        protected virtual void BeginSaveRoleSnap(IObjectTransaction trans)
        {
            var data = GetRoleData();
            snapMapping.SetField(nameof(RoleSnap.name), data.name);
            snapMapping.SetField(nameof(RoleSnap.digitID), data.digitID);
            snapMapping.SetField(nameof(RoleSnap.uuid), data.uuid);
            snapMapping.SetField(nameof(RoleSnap.account_uuid), data.account_uuid);
            snapMapping.SetField(nameof(RoleSnap.role_template_id), data.role_template_id);
            snapMapping.SetField(nameof(RoleSnap.unit_template_id), data.unit_template_id);
            snapMapping.SetField(nameof(RoleSnap.server_id), data.server_id);
            snapMapping.SetField(nameof(RoleSnap.level), data.Level);
        }

        protected override void Disposing()
        {
            roleMapping.Dispose();
            snapMapping.Dispose();
        }


        public void NotifyPlayerSingleDynamic(string key, string value, bool isNum)
        {
            List<PropertyStruct> psList = new List<PropertyStruct>();
            PropertyStruct ps = new PropertyStruct(key, value, isNum);
            psList.Add(ps);
            this.NotifyPlayerDynamic(psList);
        }

        public void NotifyPlayerDynamic(List<PropertyStruct> ps)
        {
            PlayerDynamicNotify notify = new PlayerDynamicNotify();
            notify.s2c_data = ps;
            this.service.session.Invoke(notify);
        }

        public virtual void SaveEnterZoneInfo(RoleEnterZoneResponse rsp)
        {
            //如果记录野外场景的uuid和MapID.

            if (IsPublicMap(rsp.mapTemplateID) && this.GetRoleData().last_map_template_id != rsp.mapTemplateID)
            {
                //如果和上一次不同，记录.
                this.roleMapping.SetField(nameof(ServerRoleData.last_public_mapID), rsp.mapTemplateID);
                this.roleMapping.SetField(nameof(ServerRoleData.last_public_area_uuid), rsp.zoneUUID);
                this.roleMapping.SetField(nameof(ServerRoleData.last_public_map_pos), rsp.roleScenePos);
                //公共场景覆盖公会场景记录.
                this.roleMapping.SetField(nameof(ServerRoleData.last_guild_mapID), 0);
            }
            else if (IsGuildMap(rsp.mapTemplateID))//记录上一次的公会场景ID.
            {
                this.roleMapping.SetField(nameof(ServerRoleData.last_guild_mapID), rsp.mapTemplateID);
            }

            this.roleMapping.SetField(nameof(ServerRoleData.last_map_template_id), rsp.mapTemplateID);
            this.roleMapping.SetField(nameof(ServerRoleData.last_zone_uuid), rsp.zoneUUID);
            this.roleMapping.SetField(nameof(ServerRoleData.last_area_name), rsp.areaName);
            this.roleMapping.SetField(nameof(ServerRoleData.last_area_node), rsp.areaNode);
            this.roleMapping.SetField(nameof(ServerRoleData.last_zone_pos), rsp.roleScenePos);

        }

        public virtual void SaveLeaveZoneInfo(RoleLeaveZoneResponse pos)
        {
            this.roleMapping.SetField(nameof(ServerRoleData.last_zone_saved), pos.LeaveZoneSaveData);
            this.roleMapping.SetField(nameof(ServerRoleData.last_zone_pos), pos.lastScenePos);
            if (IsPublicMap(LastMapID))
            {
                this.roleMapping.SetField(nameof(ServerRoleData.last_public_map_pos), pos.lastScenePos);
            }
        }

        public virtual ClientRoleData ToClientRoleData()
        {
            return GetRoleData().ToClientRoleData();
        }

        protected virtual bool IsPublicMap(int mapID)
        {
            var temp = RPGServerTemplateManager.Instance.GetMapTemplate(mapID);

            return temp.is_public;
        }

        protected virtual bool IsGuildMap(int mapID)
        {
            return false;
        }

        internal ServerRoleData GetRoleData()
        {
            return ((ServerRoleData)roleMapping.Data);
        }

        // 死亡掉装备耐久度
        public virtual float GetDeadLoseDurablerate()
        {
            return 0.03f;
        }
    }
}
