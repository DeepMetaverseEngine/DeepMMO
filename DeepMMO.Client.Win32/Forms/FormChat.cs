using CommonRPG.Protocol.Client;
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
    public partial class FormChat : Form
    {
        private readonly RPGClient client;
        public FormChat(RPGClient client)
        {
            this.client = client;
            InitializeComponent();
            this.comboBox1.Text = "world";
            richTextBox.LinkClicked += OnClickLink;
            this.client.GameClient.Listen<ClientChatNotify>(OnChatNotify);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            e.Cancel = true;
            this.Hide();
        }

        public void OnClickLink(object sender, LinkClickedEventArgs e)
        {
            this.comboBox1.Text = e.LinkText;
        }

        private void OnChatNotify(ClientChatNotify notify)
        {
            this.richTextBox.SelectedText = GetChannelTag(notify.channel_type);
            this.richTextBox.InsertLink(notify.from_name, notify.from_uuid);
            if (notify.channel_type == ClientChatRequest.CHANNEL_TYPE_INVALID)
            {
                this.richTextBox.SelectedText = " to you";
            }
            this.richTextBox.SelectedText = ":" + notify.content + "\n";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.comboBox1.Text))
            {
                MessageBox.Show("发送对象不能为空");
                return;
            }

            if (!string.IsNullOrEmpty(this.textBox1.Text))
            {
                ClientChatRequest req = new ClientChatRequest();
                req.content = this.textBox1.Text;

                if (this.comboBox1.Text.Equals("world", StringComparison.OrdinalIgnoreCase))
                {
                    req.channel_type = ClientChatRequest.CHANNEL_TYPE_WORLD;
                }
                else if (this.comboBox1.Text.Equals("trade", StringComparison.OrdinalIgnoreCase))
                {
                    req.channel_type = ClientChatRequest.CHANNEL_TYPE_TRADE;
                }
                else if (this.comboBox1.Text.Equals("guild", StringComparison.OrdinalIgnoreCase))
                {
                    req.channel_type = ClientChatRequest.CHANNEL_TYPE_GUILD;
                }
                else if (this.comboBox1.Text.Equals("team", StringComparison.OrdinalIgnoreCase))
                {
                    req.channel_type = ClientChatRequest.CHANNEL_TYPE_TEAM;
                }
                else
                {
                    req.channel_type = ClientChatRequest.CHANNEL_TYPE_INVALID;
                    req.to_uuid = this.comboBox1.Text.Split('#')[1];
                }

                this.client.GameClient.Request<ClientChatResponse>(req, (err, rsp) =>
                {
                    if (rsp.IsSuccess)
                    {
                        this.richTextBox.SelectedText = GetChannelTag(req.channel_type);
                        this.richTextBox.SelectedText = "you";
                        if (req.to_uuid != null)
                        {
                            this.richTextBox.SelectedText = " to ";
                            this.richTextBox.InsertLink(this.comboBox1.Text.Split('#')[0], req.to_uuid);
                        }
                        this.richTextBox.SelectedText = ":" + req.content + "\n";
                    }
                    else
                    {
                        this.richTextBox.SelectedText = "! errcode:" + rsp.s2c_code + "\n";
                    }
                });
            }
            this.textBox1.Text = "";
        }

        private string GetChannelTag(short channel_type)
        {
            switch (channel_type)
            {
                case ClientChatRequest.CHANNEL_TYPE_INVALID:
                    return "[whisper]";
                case ClientChatRequest.CHANNEL_TYPE_WORLD:
                    return "[world]";
                case ClientChatRequest.CHANNEL_TYPE_TRADE:
                    return "[trade]";
                case ClientChatRequest.CHANNEL_TYPE_GUILD:
                    return "[guild]";
                case ClientChatRequest.CHANNEL_TYPE_TEAM:
                    return "[team]";
                case ClientChatRequest.CHANNEL_TYPE_BATTLE:
                    return "[battle]";
                case ClientChatRequest.CHANNEL_TYPE_AREA:
                    return "[area]";
            }
            return "[error_channel]";
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
        }

        private void FormChat_Load(object sender, EventArgs e)
        {

        }
    }
}
