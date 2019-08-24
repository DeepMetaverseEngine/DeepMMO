using DeepCore.Xml;
using DeepMMO.Data;
using DeepMMO.Protocol;
using DeepMMO.Protocol.Client;
using System;
using System.ComponentModel;
using System.Windows.Forms;


namespace DeepMMO.Client.Win32.Forms
{
    public partial class FormLogin : Form
    {
        private string save_file = Application.StartupPath + "/" + typeof(FormLogin).Name + ".save";

        private readonly RPGClient client;

        public FormLogin(RPGClient client)
        {
            this.client = client;
            this.client.OnGateEntered += Client_OnGateEntered;
            InitializeComponent();
            DeepCore.Properties.LoadStaticFieldsFromFile(new System.IO.FileInfo(save_file), typeof(FormLoginSave));
            this.txt_Password.Text = FormLoginSave.password;
            if (FormLoginSave.ipaddress != null)
            {
                for (int i = 0; i < FormLoginSave.ipaddress.Length; i++)
                {
                    if (i == 0)
                    {
                        this.txt_Server.Text = FormLoginSave.ipaddress[0];
                    }
                    else if (!this.txt_Server.Items.Contains(FormLoginSave.ipaddress[i]))
                    {
                        this.txt_Server.Items.Add(FormLoginSave.ipaddress[i]);
                    }
                }
            }
            if (FormLoginSave.accounts != null)
            {
                for (int i = 0; i < FormLoginSave.accounts.Length; i++)
                {
                    if (i == 0)
                    {
                        this.txt_Account.Text = FormLoginSave.accounts[0];
                    }
                    else if (!this.txt_Account.Items.Contains(FormLoginSave.accounts[i]))
                    {
                        this.txt_Account.Items.Add(FormLoginSave.accounts[i]);
                    }
                }
            }
            if (FormLoginSave.serverID != null)
            {
                for (int i = 0; i < FormLoginSave.serverID.Length; i++)
                {
                    if (i == 0)
                    {
                        this.txt_ServerID.Text = FormLoginSave.serverID[0];
                    }
                    else if (!this.txt_ServerID.Items.Contains(FormLoginSave.serverID[i]))
                    {
                        this.txt_ServerID.Items.Add(FormLoginSave.serverID[i]);
                    }
                }
            }
            foreach (var server in RPGClientTemplateManager.Instance.GetAllServers())
            {
                this.com_ServerInfo.Items.Add(server);
            }
            this.DialogResult = DialogResult.Ignore;
        }


        protected override void OnClosing(CancelEventArgs e)
        {
            this.client.OnGateEntered -= Client_OnGateEntered;
            FormLoginSave.accounts = new string[this.txt_Account.Items.Count + 1];
            FormLoginSave.accounts[0] = this.txt_Account.Text;
            for (int i = 0; i < this.txt_Account.Items.Count; i++)
            {
                FormLoginSave.accounts[i + 1] = this.txt_Account.Items[i].ToString();
            }

            FormLoginSave.ipaddress = new string[this.txt_Server.Items.Count + 1];
            FormLoginSave.ipaddress[0] = this.txt_Server.Text;
            for (int i = 0; i < this.txt_Server.Items.Count; i++)
            {
                FormLoginSave.ipaddress[i + 1] = this.txt_Server.Items[i].ToString();
            }

            FormLoginSave.serverID = new string[this.txt_ServerID.Items.Count + 1];
            FormLoginSave.serverID[0] = this.txt_ServerID.Text;
            for (int i = 0; i < this.txt_ServerID.Items.Count; i++)
            {
                FormLoginSave.serverID[i + 1] = this.txt_ServerID.Items[i].ToString();
            }

            FormLoginSave.password = this.txt_Password.Text;
            DeepCore.Properties.SaveStaticFieldsToFile(new System.IO.FileInfo(save_file), typeof(FormLoginSave));
            base.OnClosing(e);
        }

        private void Client_OnGateEntered(ClientEnterGateResponse obj)
        {
            if (!this.txt_Account.Items.Contains(this.txt_Account.Text))
            {
                this.txt_Account.Items.Add(this.txt_Account.Text);
            }
            if (!this.txt_Server.Items.Contains(this.txt_Server.Text))
            {
                this.txt_Server.Items.Add(this.txt_Server.Text);
            }
            if (!this.txt_ServerID.Items.Contains(this.txt_ServerID.Text))
            {
                this.txt_ServerID.Items.Add(this.txt_ServerID.Text);
            }
            client.Connect_Connect((rsp2) =>
            {
                if (Response.CheckSuccess(rsp2))
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Can Not Connect Connector : " + rsp2);
                }
            });
        }
        protected virtual void btn_Regist_Click(object sender, EventArgs e)
        {

        }
        protected virtual void btn_Login_Click(object sender, EventArgs e)
        {
            try
            {
                var address = txt_Server.Text;
                var kv = address.Split(':');
                if (kv.Length > 1)
                {
                    client.Gate_Connect(kv[0], int.Parse(kv[1]),
                        this.txt_Account.Text,
                        this.txt_Password.Text,
                        this.txt_ServerID.Text, (rsp) =>
                    {
                        if (rsp.s2c_code == ClientEnterGateResponse.CODE_OK_IN_QUEUE)
                        {
                            new FormLoginQueue(this.client, rsp).ShowDialog(this);
                        }
                        else if (rsp.IsSuccess)
                        {

                        }
                        else
                        {
                            MessageBox.Show("Can Not Connect Gate : " + rsp);
                        }
                    });
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }
        private void com_ServerInfo_SelectedIndexChanged(object sender, EventArgs e)
        {
            var server = com_ServerInfo.SelectedItem as ServerInfo;
            if (server != null)
            {
                this.txt_Server.Text = server.address;
                this.txt_ServerID.Text = server.id;
                this.g2DPropertyGrid1.SetSelectedObject(XmlUtil.CloneObject(server));
            }
        }

        protected virtual void on_error(Exception err)
        {
            MessageBox.Show(err.Message);
        }

        public class FormLoginSave
        {
            public static string[] ipaddress = new string[] { "127.0.0.1:19001" };
            public static string[] accounts = new string[] { "hzdsb" };
            public static string[] serverID = new string[] { "0" };
            public static string password = "123456";
        }

    }
}
