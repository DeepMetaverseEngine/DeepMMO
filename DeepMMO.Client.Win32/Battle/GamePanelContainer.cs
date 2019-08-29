using System;
using System.Drawing;
using System.Windows.Forms;
using DeepMMO.Client.Win32.Forms;
using DeepMMO.Protocol;
using DeepMMO.Protocol.Client;
using DeepEditor.Common.Net;
using DeepEditor.Common.Drawing;

namespace DeepMMO.Client.Win32.Battle
{
    public partial class GamePanelContainer : UserControl, FormSessionTracer.ISession
    {
        public RPGClient client { get; private set; }
        public GamePanel battle_view { get; private set; }
        public FormSessionTracer SessionView { get; private set; }

        public GamePanelContainer()
        {
            InitializeComponent();
            this.Disposed += GamePanelContainer_Disposed;
        }
        public void Init(RPGClient client)
        {
            this.client = client;
            this.client.OnZoneChanged += Client_OnZoneChanged;
            this.client.OnZoneLeaved += Client_OnZoneLeaved;
            this.client.OnGameDisconnected += Client_OnGameDisconnected1;
            this.client.OnError += Client_OnError;

            this.SessionView = new FormSessionTracer(this);
            this.SessionView.ShowInTaskbar = false;
            this.SessionView.FormClosing += (object sender, FormClosingEventArgs e) =>
            {
                if (this.Visible)
                {
                    e.Cancel = true;
                    SessionView.Hide();
                }
            };
        }

        long FormSessionTracer.ISession.TotalRecvBytes
        {
            get { return this.client.GameClient.TotalRecvBytes; }
        }
        long FormSessionTracer.ISession.TotalSentBytes
        {
            get { return this.client.GameClient.TotalSentBytes; }
        }
        string FormSessionTracer.ISession.Title
        {
            get { return string.Format("GameClient-{0} : Time={1}", client.GameClient.Name, DateTime.Now - client.GameClient.ConnectTime); }
        }

        //------------------------------------------------------------------------------------------------------
        public void UpdateBattle(int intervalMS)
        {
            if (battle_view != null)
            {
                //battle_view.updateBattle(intervalMS);
                this.lbl_ZoneUUID.Text = battle_view.ToString();
            }
        }
        //------------------------------------------------------------------------------------------------------
        public void do_login()
        {
            client.GameClient.Disconnect();
            if (new FormLogin(client).ShowDialog(this) == DialogResult.OK)
            {
                if (new FormRoles(client).ShowDialog(this) == DialogResult.OK)
                {
                    btn_Login.Enabled = false;
                }
                else
                {
                    client.GameClient.Disconnect();
                    btn_Login.Enabled = true;
                }
            }
            else
            {
                btn_Login.Enabled = true;
            }
        }
        public void do_logout()
        {
            this.client.GameClient.Request<ClientExitGameResponse>(new ClientExitGameRequest(), (err2, rsp2) =>
            {
                if (Response.CheckSuccess(rsp2))
                {
                    client.GameClient.Disconnect();
                }
                else
                {
                    Client_OnErrorResponse(rsp2, err2);
                }
            });
        }
        public void do_reconnect()
        {
            client.GameClient.Disconnect();
            client.Connect_Connect((rsp1) =>
            {
                if (Response.CheckSuccess(rsp1))
                {
                    this.client.GameClient.Request<ClientEnterGameResponse>(new ClientEnterGameRequest()
                    {
                        c2s_roleUUID = client.LastRoleData.uuid
                    },
                    (err2, rsp2) =>
                    {
                        if (Response.CheckSuccess(rsp2))
                        {

                        }
                        else
                        {
                            Client_OnErrorResponse(rsp2, err2);
                        }
                    });
                }
                else
                {
                    Client_OnErrorResponse(rsp1);
                }
            });
        }
        //------------------------------------------------------------------------------------------------------
        #region 游戏客户端事件

        protected virtual void Client_OnErrorResponse(Response rsp, Exception err = null)
        {
            var prefix = (rsp != null) ? (rsp.GetType().Name + " : " + rsp + Environment.NewLine) : "";
            var suffix = (err != null) ? (err.Message + Environment.NewLine + err.StackTrace) : "";
            MessageBox.Show(prefix + suffix, "Client_Response");
        }
        protected virtual void Client_OnError(Exception err)
        {
            MessageBox.Show(err.Message + Environment.NewLine + err.StackTrace, "Error");
        }
        protected virtual void Client_OnGameDisconnected1(DeepCore.FuckPomeloClient.PomeloClient arg1, DeepCore.FuckPomeloClient.CloseReason arg2)
        {
            btn_Login.Enabled = true;
        }

        protected virtual void Client_OnZoneChanged(DeepMMO.Client.Battle.RPGBattleClient obj)
        {
            this.SuspendLayout();
            if (battle_view != null)
            {
                this.panel1.Controls.Remove(this.battle_view);
                battle_view.Dispose();
                battle_view = null;
            }
            this.battle_view = new GamePanel(this, obj);
            this.battle_view.Dock = DockStyle.Fill;
            this.battle_view.BattleView.OnDrawHUD += BattleView_OnDrawHUD; ;
            this.ResumeLayout(false);
            this.panel1.Controls.Add(battle_view);
        }

        private void BattleView_OnDrawHUD(DeepEditor.Common.G3D.GLView v, Graphics g)
        {
            g.DrawString("NetPing=" + client.GameClient.CurrentPing, DefaultFont, Brushes.White, new Point(10, 200));
        }

        protected virtual void Client_OnZoneLeaved(DeepMMO.Client.Battle.RPGBattleClient obj)
        {
            this.SuspendLayout();
            if (battle_view != null)
            {
                this.panel1.Controls.Remove(this.battle_view);
                battle_view.Dispose();
                battle_view = null;
            }
            this.ResumeLayout(false);
        }
        #endregion
        //------------------------------------------------------------------------------------------------------
        #region 窗体事件
        private void GamePanelContainer_Disposed(object sender, EventArgs e)
        {
            this.SessionView.Dispose();
            this.client.Dispose();
        }
        #endregion
        //------------------------------------------------------------------------------------------------------
        #region 功能按钮

        //protected FormMail formMail = null;
        //private FormWriteMail formWriteMail = null;
        //protected Form formBag = null;
        //private Form formShop = null;
        //private FormChat formChat = null;

        private void menu_Main_Click(object sender, EventArgs e)
        {
        }
        protected virtual void btn_Login_Click(object sender, EventArgs e)
        {
            do_login();
        }
        private void btn_Logout_Click(object sender, EventArgs e)
        {
            do_logout();
        }
        private void btn_Reconnect_Click(object sender, EventArgs e)
        {
            do_reconnect();
        }

        //         private void btn_Chat_Click(object sender, EventArgs e)
        //         {
        //             if (formChat == null)
        //             {
        //                 formChat = new FormChat(client);
        //             }
        //             formChat.Show();
        //         }
        //         private void btn_TC_Click(object sender, EventArgs e)
        //         {
        //         }
        //         private void btn_Mail_Click(object sender, EventArgs e)
        //         {
        //             if (formMail == null)
        //             {
        //                 formMail = new FormMail(client);
        //             }
        //             formMail.Show();
        //         }
        //         private void btn_Bag_Click(object sender, EventArgs e)
        //         {
        //             if (formBag == null || formBag.IsDisposed)
        //             {
        //                 //formBag = new FormBag(client);
        //             }
        //             formBag.Show();
        //         }
        //         private void btn_Shop_Click(object sender, EventArgs e)
        //         {
        //             if (formShop == null || formBag.IsDisposed)
        //             {
        //                 formShop = new FormStore(client);
        //             }
        //             formShop.Show();
        //         }

        #endregion
        //------------------------------------------------------------------------------------------------------

    }
}
