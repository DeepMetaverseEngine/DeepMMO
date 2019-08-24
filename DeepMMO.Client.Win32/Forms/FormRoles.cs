using DeepCore;
using DeepMMO.Data;
using DeepMMO.Protocol;
using DeepMMO.Protocol.Client;
using System;
using System.Windows.Forms;

namespace DeepMMO.Client.Win32.Forms
{
    public partial class FormRoles : Form
    {
        private readonly RPGClient client;

        public RoleTemplateData SelectedRoleTemplate
        {
            get { return combo_Pro.SelectedItem as RoleTemplateData; }
        }
        public RoleSnap SelectedRole
        {
            get
            {
                if (list_Roles.SelectedItems.Count > 0)
                {
                    return list_Roles.SelectedItems[0].Tag as RoleSnap;
                }
                return null;
            }
        }

        public FormRoles(RPGClient client)
        {
            this.client = client;
            InitializeComponent();
            this.DialogResult = DialogResult.Ignore;
            init_role_templates();
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            do_random_name();
            do_get_role_list();
        }
        //-----------------------------------------------------------------------------------------------------------
        #region Operation

        protected virtual void init_role_templates()
        {
            var prolist = RPGClientTemplateManager.Instance.AllRoleTemplates;
            foreach (var rt in prolist)
            {
                combo_Pro.Items.Add(rt);
            }
            var pro = new Random().GetRandomInArray(prolist);
            combo_Pro.SelectedItem = pro;
        }
        private void do_random_name()
        {
            var pro = SelectedRoleTemplate;
            if (pro != null)
            {
                this.client.GameClient.Request<ClientGetRandomNameResponse>(new ClientGetRandomNameRequest() { c2s_role_template_id = pro.id }, (err, rsp) =>
                {
                    if (Response.CheckSuccess(rsp))
                    {
                        txt_RoleName.Text = rsp.s2c_name;
                    }
                    else
                    {
                        MessageBox.Show("ClientGetRandomNameRequest : " + rsp);
                    }
                });
            }
        }
        protected virtual void do_get_role_list()
        {
            list_Roles.Items.Clear();
            this.client.GameClient.Request<ClientGetRolesResponse>(new ClientGetRolesRequest() { }, (err, rsp) =>
            {
                if (Response.CheckSuccess(rsp))
                {
                    if (rsp.s2c_roles == null || rsp.s2c_roles.Count == 0)
                    {
                        tabControl1.SelectedTab = tabPage_CreateRole;
                    }
                    else
                    {
                        tabControl1.SelectedTab = tabPage_SelectRole;
                        rsp.s2c_roles.Sort((a, b) =>
                        {
                            return (a.last_login_time - b.last_login_time).Seconds;
                        });
                        foreach (var p in rsp.s2c_roles)
                        {
                            if (p != null)
                            {
                                add_role_to_list(p);
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("ClientGetRolesRequest : " + rsp);
                }
            });
        }
        protected virtual void add_role_to_list(RoleSnap p)
        {
            var rt = RPGClientTemplateManager.Instance.GetRoleTemplate(p.role_template_id, 0);
            var item = list_Roles.Items.Add(p.name);
            item.SubItems.Add(p.level.ToString());
            item.SubItems.Add(rt != null ? rt.name : "");
            item.SubItems.Add(p.last_login_time.ToString());
            item.SubItems.Add((p.uuid == client.last_EnterGateResponse.s2c_lastLoginRoleID) ? "True" : "");
            item.SubItems.Add(p.server_id + "");
            item.Tag = p;
            item.Selected = true;
        }
        protected virtual void do_create_role()
        {
            var pro = SelectedRoleTemplate;
            if (pro != null)
            {
                this.client.GameClient.Request<ClientCreateRoleResponse>(new ClientCreateRoleRequest()
                {
                    c2s_name = txt_RoleName.Text,
                    c2s_template_id = pro.id,
                }, (err, rsp) =>
             {
                 if (Response.CheckSuccess(rsp))
                 {
                     tabControl1.SelectedTab = tabPage_SelectRole;
                     add_role_to_list(rsp.s2c_role);
                 }
                 else
                 {
                     MessageBox.Show("ClientCreateRoleResponse : " + rsp);
                 }
             });
            }
            else
            {
                MessageBox.Show("未选择职业");
            }
        }
        protected virtual void do_enter_game()
        {
            var role = SelectedRole;
            if (role != null)
            {
                var last_roleID = client.last_EnterGateResponse.s2c_lastLoginRoleID;
                if (last_roleID != null && role.uuid != last_roleID)
                {

                }

                this.client.GameClient.Request<ClientEnterGameResponse>(new ClientEnterGameRequest()
                {
                    c2s_roleUUID = role.uuid
                }, (err, rsp) =>
                {
                    if (Response.CheckSuccess(rsp))
                    {
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("ClientEnterGameResponse : " + rsp);
                    }
                });
            }
            else
            {
                MessageBox.Show("未选择角色");
            }
        }

        #endregion
        //-----------------------------------------------------------------------------------------------------------
        #region FormEvent

        private void btn_RandomName_Click(object sender, EventArgs e)
        {
            do_random_name();
        }
        private void btn_CreateRole_Click(object sender, EventArgs e)
        {
            do_create_role();
        }
        private void btn_Enter_Click(object sender, EventArgs e)
        {
            do_enter_game();
        }
        #endregion
    }
}
