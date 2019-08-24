using DeepCore;
using DeepCore.FuckPomeloClient;
using DeepMMO.Data;
using DeepMMO.Protocol;
using DeepMMO.Protocol.Client;
using System;

namespace DeepMMO.Client.BotTest.Runner
{
    public partial class BotRunner
    {
        protected virtual void on_gate_connect_callback(ClientEnterGateResponse token)
        {
            this.net_status.Value = "";
            log_response(token);
            client.Connect_Connect(on_game_connect_callback);
        }
        protected virtual void on_gate_in_queue(ClientEnterGateInQueueNotify ntf)
        {
            this.net_status.Value = $"排队中: 人数:{ntf.QueueIndex} 预计时间:{ntf.ExpectTime}";
        }

        protected virtual void on_game_connect_callback(ClientEnterServerResponse rsp)
        {
            log_response(rsp);
            if (Response.CheckSuccess(rsp))
            {
                do_get_role_list(on_get_role_list);
            }
        }
        protected virtual RoleTemplateData get_random_role_template()
        {
            var prolist = RPGClientTemplateManager.Instance.AllRoleTemplates;
            var pro = random.GetRandomInArray(prolist);
            return pro;
        }
        //---------------------------------------------------------------------------------------------------------------------------
        protected virtual void do_get_role_list(Action<PomeloException, ClientGetRolesResponse> cb)
        {
            this.client.GameClient.Request<ClientGetRolesResponse>(
                new ClientGetRolesRequest() { }, cb);
        }
        protected virtual void on_get_role_list(PomeloException err, ClientGetRolesResponse rsp)
        {
            log_response(rsp, err);
            if (Response.CheckSuccess(rsp))
            {
                if (rsp.s2c_roles != null && rsp.s2c_roles.Count > 0)
                {
                    var role = random.GetRandomInArray(rsp.s2c_roles);
                    do_enter_game(role.uuid, on_enter_game);
                }
                else
                {
                    do_get_random_name(on_get_random_name);
                }
            }
        }
        //---------------------------------------------------------------------------------------------------------------------------
        protected virtual void do_get_random_name(Action<PomeloException, ClientGetRandomNameResponse> cb)
        {
            var pro = get_random_role_template();
            if (pro != null)
            {
                this.client.GameClient.Request<ClientGetRandomNameResponse>(new ClientGetRandomNameRequest()
                {
                    c2s_role_template_id = pro.id
                }, cb);
            }
        }
        protected virtual void on_get_random_name(PomeloException err, ClientGetRandomNameResponse rsp)
        {
            log_response(rsp, err);
            if (Response.CheckSuccess(rsp))
            {
                var pro = get_random_role_template();
                do_create_role(rsp.s2c_name, pro.id, on_create_role);
            }
        }
        //---------------------------------------------------------------------------------------------------------------------------
        protected virtual void do_create_role(string roleName, int proId, Action<PomeloException, ClientCreateRoleResponse> cb)
        {
            var pro = get_random_role_template();
            if (pro != null)
            {
                this.client.GameClient.Request<ClientCreateRoleResponse>(new ClientCreateRoleRequest()
                {
                    c2s_name = roleName,
                    c2s_template_id = proId
                }, cb);
            }
        }
        protected virtual void on_create_role(PomeloException err, ClientCreateRoleResponse rsp)
        {
            log_response(rsp, err);
            if (Response.CheckSuccess(rsp))
            {
                do_enter_game(rsp.s2c_role.uuid, on_enter_game);
            }
            else if (rsp.s2c_code == ClientCreateRoleResponse.CODE_NAME_ALREADY_EXIST)
            {
                var rn = client.GameClient.GetLastResponse<ClientGetRandomNameResponse>();
                if (rn != null)
                {
                    var name = rn.s2c_name + this.random.Next(100).ToString();
                    var pro = get_random_role_template();
                    do_create_role(name, pro.id, on_create_role);
                }
                else
                {
                    var name = this.account + this.random.Next(100).ToString();
                    var pro = get_random_role_template();
                    do_create_role(name, pro.id, on_create_role);
                }
            }
            else if (rsp.s2c_code == ClientCreateRoleResponse.CODE_BLACK_NAME)
            {
                var name = this.account + this.random.Next(100).ToString();
                var pro = get_random_role_template();
                do_create_role(name, pro.id, on_create_role);
            }
            else
            {
                var name = client.GameClient.GetLastResponse<ClientGetRandomNameResponse>();
                var pro = get_random_role_template();
                do_create_role(name.s2c_name, pro.id, on_create_role);
            }
        }
        //---------------------------------------------------------------------------------------------------------------------------
        protected virtual void do_enter_game(string roleUUID, Action<PomeloException, ClientEnterGameResponse> cb)
        {
            this.client.GameClient.Request<ClientEnterGameResponse>(new ClientEnterGameRequest()
            {
                c2s_roleUUID = roleUUID
            }, cb);
        }
        protected virtual void on_enter_game(PomeloException err, ClientEnterGameResponse rsp)
        {
            log_response(rsp, err);
            if (Response.CheckSuccess(rsp))
            {

            }
        }
        //---------------------------------------------------------------------------------------------------------------------------
    }
}
