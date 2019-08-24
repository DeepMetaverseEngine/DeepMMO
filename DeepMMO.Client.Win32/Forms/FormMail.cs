using CommonRPG.Data;
using CommonRPG.Protocol;
using CommonRPG.Protocol.Client;
using DDogProtocol.Data;
using DDogProtocol.Protocol.Client;
using DDogServer.Common.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace CommonRPG.Client.Win32.Forms
{
    public partial class FormMail : Form
    {
        private readonly RPGClient client;

        public FormMail(RPGClient client)
        {
            InitializeComponent();
            this.client = client;
            AddListener();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            e.Cancel = true;
            this.Hide();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            UpdateMailListInfo();
        }

        private void AddListener()
        {
            this.client.GameClient.Listen<DDogClientIncomingMailNotify>(OnReceiveClientIncomingMailNotify);
        }

        private void OnReceiveClientIncomingMailNotify(DDogClientIncomingMailNotify notify)
        {
            MessageBox.Show("收到新邮件!");
            UpdateMailListInfo();
        }

        private void FormMail_Load(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// 更新邮箱界面.
        /// </summary>
        private void UpdateMailListInfo()
        {
            this.client.GameClient.Request<DDogClientGetMailBoxInfoResponse>(new DDogClientGetMailBoxInfoRequest(), (err, rsp) =>
            {
                if (Response.CheckSuccess(rsp))
                {
                    UpdateMailListInfo(rsp.s2c_mailsnap_list);
                }
                else
                {
                    MessageBox.Show("更新邮件列表失败 : " + rsp.s2c_code);
                }
            });
        }

        /// <summary>
        /// 更新邮箱界面.
        /// </summary>
        /// <param name="list"></param>
        private void UpdateMailListInfo(List<DDogMailSnapInfoData> list)
        {
            listView1.Items.Clear();

            if (list != null)
            {
                DDogMailSnapInfoData snap = null;

                for (int i = 0; i < list.Count; i++)
                {
                    snap = list[i];

                    var item = new ListViewItem(snap.uuid);

                    switch (snap.mail_status)
                    {
                        case DDogMailData.MailStatus.Status_Read:
                            item.Text = "已读";
                            break;
                        case DDogMailData.MailStatus.Status_UnRead:
                            item.Text = "未读";
                            break;
                        default:
                            item.Text = "Error";
                            break;
                    }

                    item.SubItems.Add(snap.uuid);
                    item.SubItems.Add(snap.title);
                    item.SubItems.Add(snap.attachment.ToString());
                    item.SubItems.Add(snap.sender_name);
                    item.SubItems.Add(snap.sender_uuid);
                    item.SubItems.Add(snap.create_time.ToString());

                    item.Tag = snap;

                    listView1.Items.Add(item);
                }
            }
        }

        /// <summary>
        /// 删邮件.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_DeleteMail_Click(object sender, EventArgs e)
        {
            DDogMailSnapInfoData snap = null;
            List<string> removelist = new List<string>(); ;

            foreach (ListViewItem item in listView1.SelectedItems)
            {
                snap = item.Tag as DDogMailSnapInfoData;
                removelist.Add(snap.uuid);
            }

            DDogClientDeleteMailRequest req = new DDogClientDeleteMailRequest();

            req.c2s_delete_all = false;
            req.c2s_remove_uuid_list = removelist;

            this.client.GameClient.Request<DDogClientDeleteMailResponse>(req, (err, rsp) =>
            {
                if (DDogClientDeleteMailResponse.CheckSuccess(rsp))
                {
                    UpdateMailListInfo();
                    MessageBox.Show("邮件删除成功 : " + rsp);
                }
                else
                {
                    MessageBox.Show("邮件删除失败 : " + rsp);
                }
            }
            );
        }

        /// <summary>
        /// 写邮件.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_WriteMail_Click(object sender, EventArgs e)
        {
            DDogClientSendMailRequest req = new DDogClientSendMailRequest();

            //临时用，发给自己.
            req.c2s_receiver_uuid = this.client.LastRoleData.uuid;
            req.c2s_title = "邮件测试";
            req.c2s_content = new DDogMailContentData { txt_content = "暗地里发了快递费骄傲了看对方进度发", attachment_list = null };

            this.client.GameClient.Request<DDogClientSendMailResponse>(req, (err, rsp) =>
             {
                 if (Response.CheckSuccess(rsp))
                 {
                     MessageBox.Show("邮件发送成功 : " + rsp);
                 }
                 else if (err != null)
                 {
                     MessageBox.Show("邮件发送失败 : " + err.Message);
                 }
             });
        }

        /// <summary>
        /// 清空邮件.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_ClearMail_Click(object sender, EventArgs e)
        {
            DDogClientDeleteMailRequest req = new DDogClientDeleteMailRequest();

            req.c2s_delete_all = true;

            this.client.GameClient.Request<DDogClientDeleteMailResponse>(req, (err, rsp) =>
            {
                if (rsp.IsSuccess)
                {
                    UpdateMailListInfo(rsp.s2c_mailsnap_list);
                    MessageBox.Show("清空邮件成功 : " + rsp);
                }
                else
                {
                    MessageBox.Show("清空邮件失败 : " + rsp);
                }
            }
          );
        }

        /// <summary>
        /// 获取附件.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_GetAttachment_Click(object sender, EventArgs e)
        {
            DDogClientGetMailAttachmentRequest req = new DDogClientGetMailAttachmentRequest();

            DDogMailSnapInfoData ms = null;

            foreach (ListViewItem item in listView1.SelectedItems)
            {
                ms = item.Tag as DDogMailSnapInfoData;
            }

            if (ms != null)
            {
                req.c2s_mailuuid = ms.uuid;

                this.client.GameClient.Request<DDogClientGetMailAttachmentResponse>(req, (err, rsp) =>
                {
                    if (rsp.IsSuccess)
                    {
                        UpdateMailListInfo();
                        MessageBox.Show("获取附件成功: " + rsp);
                    }
                    else
                    {
                        MessageBox.Show("获取详情失败 : " + rsp);
                    }
                });
            }
        }

        /// <summary>
        /// 获取邮件附件通知.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_GetMailDetail_Click(object sender, EventArgs e)
        {
            DDogClientGetMailDetailRequest req = new DDogClientGetMailDetailRequest();

            DDogMailSnapInfoData ms = null;

            foreach (ListViewItem item in listView1.SelectedItems)
            {
                ms = item.Tag as DDogMailSnapInfoData;
            }

            if (ms != null)
            {
                req.c2s_mailuuid = ms.uuid;

                this.client.GameClient.Request<DDogClientGetMailDetailResponse>(req, (err, rsp) =>
                {
                    if (rsp.IsSuccess)
                    {
                        UpdateMailListInfo();
                        MessageBox.Show("邮件内容: " + rsp.s2c_mail_detail.content.txt_content);
                    }
                    else
                    {
                        MessageBox.Show("获取详情失败 : " + rsp);
                    }
                }
              );
            }
        }
    }
}
