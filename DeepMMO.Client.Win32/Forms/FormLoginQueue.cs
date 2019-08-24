using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DeepMMO.Client.Win32.Forms
{
    public partial class FormLoginQueue : Form
    {
        private readonly RPGClient client;
        private readonly Data.ServerInfo server;
        public FormLoginQueue(RPGClient client, Protocol.Client.ClientEnterGateResponse rsp)
        {
            InitializeComponent();
            this.client = client;
            this.client.OnGateQueueUpdated += Client_OnGateQueueUpdated;
            this.server = RPGClientTemplateManager.Instance.GetServer(client.last_EnterGateRequest.c2s_serverID);
            this.label1.Text = $"【{server?.name}】服务器人数已满，目前排位在{rsp.s2c_queueCount + 1}，预计等待时间{rsp.s2c_queuetTime}。";
        }
        protected override void OnClosed(EventArgs e)
        {
            this.client.OnGateQueueUpdated -= Client_OnGateQueueUpdated;
            base.OnClosed(e);
        }
        private void Client_OnGateQueueUpdated(Protocol.Client.ClientEnterGateInQueueNotify obj)
        {
            if (!IsDisposed)
            {
                this.label1.Text = $"【{server?.name}】服务器人数已满，目前排位在{obj.QueueIndex + 1}，预计等待时间{obj.ExpectTime}。";
                if (obj.IsEnetered) this.Close();
            }
        }
    }
}
