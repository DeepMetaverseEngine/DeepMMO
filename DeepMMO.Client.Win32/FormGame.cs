using DeepMMO.Client.Win32.Forms;


using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace DeepMMO.Client.Win32
{
    public partial class FormGame : Form
    {
        protected RPGClient client { get; private set; }
        private Stopwatch interval = Stopwatch.StartNew();
        private long last_time_ms;

        public FormGame()
        {
            InitializeComponent();
        }

        public virtual void Init(RPGClient client)
        {
            this.client = client;
            this.gamePanel.Init(client);
            this.client.GameClient.OnDisconnected += GameClient_OnDisconnected;
            this.gamePanel.do_login();
        }
        private void GameClient_OnDisconnected(DeepCore.FuckPomeloClient.CloseReason arg1, string arg2)
        {
            MessageBox.Show(this, $"游戏服已断开: {arg1} : {arg2}", this.Text);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }
        protected virtual void timerUpdate_Tick(object sender, EventArgs e)
        {
            var ct = interval.ElapsedMilliseconds;
            int ms = (int)(ct - last_time_ms);
            last_time_ms = ct;
            this.client.Update(ms);
            this.gamePanel.UpdateBattle(ms);
            this.Text = string.Format("角色:{0}    ({1})", client.RoleName, client.GameClient.IsConnected ? "已连接" : "未连接");
        }
        protected virtual void timerTest_Tick(object sender, EventArgs e)
        {
        }

    }
}
