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
    public partial class FormStore : Form
    {
        private readonly RPGClient mClient;
        public FormStore(RPGClient client)
        {
            this.mClient = client;
            InitializeComponent();
        }

        private void tabShop_Click(object sender, EventArgs e)
        {
            //this.mClient.GameClient.Request(new ClientGetShopListRequest()

            //ClientGetShopListRequest req = new ClientGetShopListRequest();
            //req.c2s_shop_type = 2;
            //this.mClient.GameClient.Request<ClientGetShopListResponse>(req, (err, rsp) =>
            //{
            //    if (rsp.s2c_code == ClientGetShopListResponse.CODE_OK)
            //    {

            //    }
            //    else
            //    {
            //        MessageBox.Show("获取商品列表信息 结果：" + rsp.s2c_code + " 原因："
            //        + rsp.s2c_msg);
            //    }
            //});
        }

        private void button1_Click(object sender, EventArgs e)
        {

            //ClientGetShopListRequest req = new ClientGetShopListRequest();
            //req.c2s_shop_type = 2;
            //this.mClient.GameClient.Request<ClientGetShopListResponse>(req, (err, rsp) =>
            //{
            //    if (rsp.s2c_code == ClientGetShopListResponse.CODE_OK)
            //    {

            //    }
            //    else
            //    {
            //        MessageBox.Show("获取商品列表信息 结果：" + rsp.s2c_code + " 原因："
            //        + rsp.s2c_msg);
            //    }
            //});
        }
    }



}
