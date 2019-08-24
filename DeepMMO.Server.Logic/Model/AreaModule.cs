using DeepCore.IO;
using DeepCrystal.ORM;
using DeepCrystal.RPC;
using DeepMMO.Data;
using DeepMMO.Protocol;
using DeepMMO.Protocol.Client;
using DeepMMO.Server.Area;
using DeepMMO.Server.AreaManager;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DeepMMO.Server.Logic.Model
{
    public class AreaModule : ILogicModule
    {

        public IRemoteService areaManager { get; private set; }
        public IRemoteService currentArea { get; protected set; }
        public string currentAreaName { get { return currentArea != null ? currentArea.Address.ServiceName : null; } }
        private List<ZoneInfoSnap> mLastZoneInfoSnaps;
        private bool mZoneInfoDirtyFlag = true;


        private DateTime LastGetZoneInfoTimeStamp;

        public AreaModule(LogicService service) : base(service)
        {

        }
        protected override void Disposing()
        {
        }
        public override async Task OnStartAsync()
        {
            this.areaManager = await service.Provider.GetAsync(ServerNames.AreaManager);
            if (areaManager == null)
            {
                throw new Exception("Cant Find AreaManager Service");
            }
        }
        public override Task OnStartedAsync()
        {
            return BeginEnterZoneAsync();
        }
        public override Task OnStopAsync()
        {
            OnEnterZone = null;
            return this.RequestLeaveZoneAsync();
        }
        protected virtual Task BeginEnterZoneAsync()
        {
            // 寻找一个场景 //
            var rd = service.roleModule.GetRoleData();
            return this.RequestEnterZoneAsync(new RoleEnterZoneRequest()
            {
                serverID = service.serverID,
                servergroupID = service.serverGroupID,
                expectMapTemplateID = rd.last_map_template_id,
                expectZoneUUID = rd.last_zone_uuid,
                roleScenePos = rd.last_zone_pos,

                teamID = GetTeamID(),
            });
        }

        public virtual ISerializable ToRoleBattleData(RoleEnterZoneRequest req)
        {
            return null;
        }

        /// <summary>
        /// 请求进入场景
        /// </summary>
        public virtual async Task<RoleEnterZoneResponse> RequestEnterZoneAsync(RoleEnterZoneRequest req)
        {
            var rd = service.roleModule.GetRoleData();
            req.roleUUID = service.roleID;
            req.roleSessionName = service.sessionName;
            req.roleSessionNode = service.sessionNode;
            req.roleLogicNode = service.SelfAddress.ServiceNode;
            req.roleDisplayName = rd.name;
            req.roleUnitTemplateID = rd.unit_template_id;
            req.roleData = ToRoleBattleData(req);//< ---战斗相关数据
            req.lastPublicMapID = rd.last_public_mapID;
            req.lastPublicMapUUID = rd.last_public_area_uuid;
            req.lastPublicPos = rd.last_public_map_pos;
            req.expectAreaNode = service.SelfAddress.ServiceNode;
            var result = await this.areaManager.CallAsync<RoleEnterZoneResponse>(req);
            if (RoleEnterZoneResponse.CheckSuccess(result))
            {
                currentArea = await service.Provider.GetAsync(new RemoteAddress(result.areaName, result.areaNode));
                this.service.roleModule.SaveEnterZoneInfo(result);
                CurZoneChange(req, result);
            }
            else
            {
                service.log.Error("Enter Zone Error : " + result);
            }
            return result;
        }

        public virtual async Task<RoleLeaveZoneResponse> RequestLeaveZoneAsync()
        {
            var rd = service.roleModule.GetRoleData();
            var request = new RoleLeaveZoneRequest()
            {
                zoneUUID = rd.last_zone_uuid,
                roleID = service.roleID,
                keepObject = false,
            };
            var result = await this.areaManager.CallAsync<RoleLeaveZoneResponse>(request);
            if (result != null && RoleLeaveZoneResponse.CheckSuccess(result))
            {
                this.service.roleModule.SaveLeaveZoneInfo(result);
                //TODO;
                OnLeaveZone?.Invoke(request, result);
            }
            return result;
        }

        public virtual async Task<Response> RequestTransportAsync(RoleNeedTransportNotify tp)
        {
            var leave_result = await RequestLeaveZoneAsync();

            if (leave_result == null || Response.CheckSuccess(leave_result) == false)
            {
                return leave_result;
            }

            var enter_result = await this.RequestEnterZoneAsync(new RoleEnterZoneRequest()
            {
                servergroupID = service.serverGroupID,
                serverID = service.serverID,
                expectMapTemplateID = tp.nextMapID,
                roleScenePos = new ZonePosition() { flagName = tp.nextZoneFlagName },

                teamID = GetTeamID(),
            });

            return enter_result;
        }

        public async Task<List<ZoneInfoSnap>> RequestGetZonesSnapInfoAsync(int mapID)
        {
            var req = new GetZonesInfoRequest();
            req.servergroupID = this.service.serverGroupID;
            req.mapID = mapID;
            //向AreaManager请求.
            var rsp = await areaManager.CallAsync<GetZonesInfoResponse>(req);

            return rsp.snaps;
        }

        protected virtual void CurZoneChange(RoleEnterZoneRequest req, RoleEnterZoneResponse result)
        {
            //变更过场景，场景线缓存失效.
            mZoneInfoDirtyFlag = true;
            OnEnterZone?.Invoke(req, result);
        }

        /// <summary>
        /// 验证出场景条件.
        /// </summary>
        /// <param name="mapID"></param>
        /// <returns></returns>
        public virtual bool ValidateLeaveZoneCondition(int mapID, out string reason)
        {
            reason = null;
            return true;
        }

        /// <summary>
        /// 验证进场景条件.
        /// </summary>
        /// <param name="mapID"></param>
        /// <returns></returns>
        public virtual bool ValidateEnterZoneCondition(int mapID, out string reason)
        {
            reason = null;
            return true;
        }

        public virtual bool ValidateChangeZoneCondition(int mapID, out string reason)
        {
            int curMapID = service.roleModule.LastMapID;

            if (ValidateLeaveZoneCondition(curMapID, out reason) == false)
            {
                return false;
            }

            return ValidateEnterZoneCondition(mapID, out reason);
        }

        public async virtual Task<ClientGetZoneInfoSnapResponse> DoClientGetZoneInfoSnapRequest(ClientGetZoneInfoSnapRequest req)
        {
            ClientGetZoneInfoSnapResponse rsp = new ClientGetZoneInfoSnapResponse();
            var roleData = service.roleModule.GetRoleData();
            rsp.s2c_curZoneUUID = roleData.last_zone_uuid;

            //同场景同线内使用缓存的线数据.
            if (mZoneInfoDirtyFlag == true || (DateTime.UtcNow - LastGetZoneInfoTimeStamp).TotalMilliseconds > 0)
            {
                var ret = await RequestGetZonesSnapInfoAsync(roleData.last_map_template_id);
                rsp.s2c_snaps = ret;
                mLastZoneInfoSnaps = ret;
                mZoneInfoDirtyFlag = false;
                LastGetZoneInfoTimeStamp = DateTime.UtcNow.AddMinutes(2);
            }
            else
            {
                rsp.s2c_snaps = mLastZoneInfoSnaps;
            }

            return rsp;
        }

        public virtual async Task<ClientChangeZoneLineResponse> DoClientChangeZoneLineRequest(ClientChangeZoneLineRequest req)
        {
            var rsp = new ClientChangeZoneLineResponse();
            var mapData = RPGServerTemplateManager.Instance.GetMapTemplate(service.roleModule.LastMapID);
            if (!RPGServerTemplateManager.Instance.AllowChangeLine(mapData) ||
                mLastZoneInfoSnaps == null ||
                service.roleModule.LastZoneUUID == req.c2s_zoneuuid)
            {
                rsp.s2c_code = ClientChangeZoneLineResponse.CODE_ERROR;
                return rsp;
            }

            ZoneInfoSnap snap = null;
            for (int i = 0; i < mLastZoneInfoSnaps.Count; i++)
            {
                snap = mLastZoneInfoSnaps[i];
                if (req.c2s_zoneuuid == snap.uuid)
                {
                    if (snap.curPlayerCount >= snap.playerMaxCount)
                    {
                        rsp.s2c_code = ClientChangeZoneLineResponse.CODE_LINE_BUSY;
                        return rsp;
                    }
                    else
                    {
                        await DoChangeZoneLineAsync(req.c2s_zoneuuid, service.roleModule.LastMapID);
                        return rsp;
                    }

                }
            }

            rsp.s2c_code = ClientChangeZoneLineResponse.CODE_NOT_EXIST;
            return rsp;
        }

        /// <summary>
        /// 请求进入指定场景.
        /// </summary>
        /// <param name="sceneUUID"></param>
        public virtual async Task DoEnterTargetSceneAsync(string sceneUUID, int mapID)
        {
            // var serverRoleData = service.roleModule.GetRoleData();
            await RequestLeaveZoneAsync();
            // 寻找一个场景 //
            await this.RequestEnterZoneAsync(new RoleEnterZoneRequest()
            {
                serverID = service.serverID,
                servergroupID = service.serverGroupID,
                expectZoneUUID = sceneUUID,
                expectMapTemplateID = mapID,
                teamID = GetTeamID(),
            });
        }

        public async Task DoChangeZoneLineAsync(string sceneUUID, int mapID)
        {
            // var serverRoleData = service.roleModule.GetRoleData();
            var rsp = await RequestLeaveZoneAsync();
            // 寻找一个场景 //
            await this.RequestEnterZoneAsync(new RoleEnterZoneRequest()
            {
                serverID = service.serverID,
                servergroupID = service.serverGroupID,
                expectZoneUUID = sceneUUID,
                expectMapTemplateID = mapID,
                roleScenePos = rsp.lastScenePos,
                teamID = GetTeamID(),
            });
        }

        public virtual void DoAreaGameOverNotify(AreaGameOverNotify notify)
        {

        }
        protected virtual string GetTeamID()
        {
            return null;
        }



        public delegate void PlayerEnterZoneHandler(RoleEnterZoneRequest req, RoleEnterZoneResponse result);
        public delegate void PlayerLeaveZoneHandler(RoleLeaveZoneRequest req, RoleLeaveZoneResponse result);
        public event PlayerEnterZoneHandler OnEnterZone;
        public event PlayerLeaveZoneHandler OnLeaveZone;
    }
}
