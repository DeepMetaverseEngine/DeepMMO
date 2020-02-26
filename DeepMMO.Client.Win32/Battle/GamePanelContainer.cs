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
        public RPGClient Client { get; private set; }
        public GamePanel BattlePanel { get; private set; }
        public FormSessionTracer SessionView { get; private set; }
        //public bool AutoUpdateBattleClient { get; set; } = true;
        public GamePanelContainer()
        {
            InitializeComponent();
            this.Disposed += GamePanelContainer_Disposed;
        }
        public void Init(RPGClient client)
        {
            this.Client = client;
            this.Client.OnZoneChanged += Client_OnZoneChanged;
            this.Client.OnZoneLeaved += Client_OnZoneLeaved;
            this.Client.OnGameDisconnected += Client_OnGameDisconnected1;
            this.Client.OnError += Client_OnError;

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
            get { return this.Client.GameClient.TotalRecvBytes; }
        }
        long FormSessionTracer.ISession.TotalSentBytes
        {
            get { return this.Client.GameClient.TotalSentBytes; }
        }
        string FormSessionTracer.ISession.Title
        {
            get { return string.Format("GameClient-{0} : Time={1}", Client.GameClient.Name, DateTime.Now - Client.GameClient.ConnectTime); }
        }

        //------------------------------------------------------------------------------------------------------
        public void UpdateBattle(int intervalMS)
        {
            if (BattlePanel != null)
            {
                //battle_view.updateBattle(intervalMS);
                this.lbl_ZoneUUID.Text = BattlePanel.ToString();
            }
        }
        //------------------------------------------------------------------------------------------------------
        public void do_login()
        {
            Client.GameClient.Disconnect();
            if (new FormLogin(Client).ShowDialog(this) == DialogResult.OK)
            {
                if (new FormRoles(Client).ShowDialog(this) == DialogResult.OK)
                {
                    btn_Login.Enabled = false;
                }
                else
                {
                    Client.GameClient.Disconnect();
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
            this.Client.GameClient.Request<ClientExitGameResponse>(new ClientExitGameRequest(), (err2, rsp2) =>
            {
                if (Response.CheckSuccess(rsp2))
                {
                    Client.GameClient.Disconnect();
                }
                else
                {
                    Client_OnErrorResponse(rsp2, err2);
                }
            });
        }
        public void do_reconnect()
        {
            Client.GameClient.Disconnect();
            Client.Connect_Connect((rsp1) =>
            {
                if (Response.CheckSuccess(rsp1))
                {
                    this.Client.GameClient.Request<ClientEnterGameResponse>(new ClientEnterGameRequest()
                    {
                        c2s_roleUUID = Client.LastRoleData.uuid
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
            err.ShowMessageBox();
        }
        protected virtual void Client_OnGameDisconnected1(DeepCore.FuckPomeloClient.PomeloClient arg1, DeepCore.FuckPomeloClient.CloseReason arg2)
        {
            btn_Login.Enabled = true;
        }

        protected virtual void Client_OnZoneChanged(DeepMMO.Client.Battle.RPGBattleClient obj)
        {
            this.SuspendLayout();
            if (BattlePanel != null)
            {
                this.panel1.Controls.Remove(this.BattlePanel);
                BattlePanel.Dispose();
                BattlePanel = null;
            }
            this.BattlePanel = new GamePanel(this, obj);
            this.BattlePanel.Dock = DockStyle.Fill;
            this.BattlePanel.BattleView.OnDrawHUD += BattleView_OnDrawHUD;
            //this.BattlePanel.BattleView.AutoUpdateBattleClient = this.AutoUpdateBattleClient;
            this.ResumeLayout(false);
            this.panel1.Controls.Add(BattlePanel);
        }

        private void BattleView_OnDrawHUD(DeepEditor.Common.G3D.GLView v, Graphics g)
        {
            g.DrawString("NetPing=" + Client.GameClient.CurrentPing, DefaultFont, Brushes.White, new Point(10, 200));
        }

        protected virtual void Client_OnZoneLeaved(DeepMMO.Client.Battle.RPGBattleClient obj)
        {
            this.SuspendLayout();
            if (BattlePanel != null)
            {
                this.panel1.Controls.Remove(this.BattlePanel);
                BattlePanel.Dispose();
                BattlePanel = null;
            }
            this.ResumeLayout(false);
        }
        #endregion
        //------------------------------------------------------------------------------------------------------
        #region 窗体事件
        private void GamePanelContainer_Disposed(object sender, EventArgs e)
        {
            this.SessionView.Dispose();
            this.Client.Dispose();
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
