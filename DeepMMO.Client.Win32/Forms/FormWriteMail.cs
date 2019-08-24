using CommonRPG.Data;
using CommonRPG.Protocol.Client;
using DDogProtocol.Data;
using DDogProtocol.Protocol.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CommonRPG.Client.Win32.Forms
{
    public partial class FormWriteMail : Form
    {
        private readonly RPGClient client;

        public FormWriteMail(RPGClient client)
        {
            InitializeComponent();
            this.client = client;
        }

        private void btn_send_Click(object sender, EventArgs e)
        {
            DDogClientSendMailRequest req = new DDogClientSendMailRequest();
            DDogMailContentData content = new DDogMailContentData();
            content.txt_content = textBox_mailContent.Text;

            //req.c2s_receiver_uuid = combobox_receiver.Text;
            req.c2s_receiver_uuid = "";
            req.c2s_title = textbox_mailTitle.Text;
            req.c2s_content = content;

            this.client.GameClient.Request<DDogClientSendMailResponse>(req, (err, rsp) =>
            {
                if (rsp.IsSuccess)
                {
                    MessageBox.Show("邮件发送成功 : " + rsp);
                }
                else
                {
                    MessageBox.Show("邮件发送失败 : " + rsp.s2c_code);
                }

            });
        }
    }
}
