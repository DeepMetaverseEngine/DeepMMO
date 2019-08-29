using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DeepEditor.Plugin3D.BattleClient;
using DeepMMO.Client.Battle;
using DeepEditor.Common.Net;
using System.IO;

namespace DeepMMO.Client.Win32.Battle
{
    public partial class GamePanel : PanelBattleView3D
    {
        private readonly GamePanelContainer mContainer;
        private readonly RPGBattleClient mClient;
        private readonly FormNetSession mSessionView;

        public GamePanel(GamePanelContainer container, RPGBattleClient client)
        {
            InitializeComponent();
            this.mContainer = container;
            this.mClient = client;
            this.mSessionView = new FormNetSession(client.Client.GameClient);
            this.mSessionView.ShowInTaskbar = false;
            this.mSessionView.FormClosing += (object sender, FormClosingEventArgs e) =>
            {
                if (this.Visible)
                {
                    e.Cancel = true;
                    mSessionView.Hide();
                }
            };
            this.Disposed += (object sender, EventArgs e) =>
            {
                mSessionView.Dispose();
            };
            base.btn_NetView.Click += (object sender, EventArgs e) =>
            {
                mSessionView.Show();
            };
            base.timerInfo.Tick += (object sender, EventArgs e) =>
            {
//                 string conn = mClient.Session.IsConnected ? "已连接" : "未连接";
//                 this.Text = mClient.PlayerUUID + " - [" + conn + "]";
            };
            base.LoadTemplates += (DirectoryInfo dataRoot) =>
            {
                return mClient.DataRoot;
            };
            base.CreateAbstractBattle += (int sceneID) =>
            {
                return mClient;
            };

            base.Init();

            base.RenderFPS = mClient.DataRoot.Templates.CFG.SYSTEM_FPS;

        }


    

    }
}
