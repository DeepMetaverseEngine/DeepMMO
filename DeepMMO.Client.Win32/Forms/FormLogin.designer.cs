namespace DeepMMO.Client.Win32.Forms
{
    partial class FormLogin
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btn_Regist = new System.Windows.Forms.Button();
            this.btn_Login = new System.Windows.Forms.Button();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.label5 = new System.Windows.Forms.Label();
            this.com_ServerInfo = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txt_Server = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txt_ServerID = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txt_Account = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txt_Password = new System.Windows.Forms.TextBox();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.g2DPropertyGrid1 = new DeepEditor.Common.G2D.DataGrid.G2DPropertyGrid();
            this.flowLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btn_Regist
            // 
            this.btn_Regist.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btn_Regist.Location = new System.Drawing.Point(11, 350);
            this.btn_Regist.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btn_Regist.Name = "btn_Regist";
            this.btn_Regist.Size = new System.Drawing.Size(151, 51);
            this.btn_Regist.TabIndex = 20;
            this.btn_Regist.Text = "注册";
            this.btn_Regist.UseVisualStyleBackColor = true;
            this.btn_Regist.Visible = false;
            this.btn_Regist.Click += new System.EventHandler(this.btn_Regist_Click);
            // 
            // btn_Login
            // 
            this.btn_Login.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btn_Login.Location = new System.Drawing.Point(171, 350);
            this.btn_Login.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btn_Login.Name = "btn_Login";
            this.btn_Login.Size = new System.Drawing.Size(151, 51);
            this.btn_Login.TabIndex = 19;
            this.btn_Login.Text = "登录";
            this.btn_Login.UseVisualStyleBackColor = true;
            this.btn_Login.Click += new System.EventHandler(this.btn_Login_Click);
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoScroll = true;
            this.flowLayoutPanel1.AutoScrollMargin = new System.Drawing.Size(8, 8);
            this.flowLayoutPanel1.AutoScrollMinSize = new System.Drawing.Size(8, 8);
            this.flowLayoutPanel1.Controls.Add(this.label5);
            this.flowLayoutPanel1.Controls.Add(this.com_ServerInfo);
            this.flowLayoutPanel1.Controls.Add(this.label3);
            this.flowLayoutPanel1.Controls.Add(this.txt_Server);
            this.flowLayoutPanel1.Controls.Add(this.label4);
            this.flowLayoutPanel1.Controls.Add(this.txt_ServerID);
            this.flowLayoutPanel1.Controls.Add(this.label1);
            this.flowLayoutPanel1.Controls.Add(this.txt_Account);
            this.flowLayoutPanel1.Controls.Add(this.label2);
            this.flowLayoutPanel1.Controls.Add(this.txt_Password);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel1.Margin = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Padding = new System.Windows.Forms.Padding(8, 8, 8, 8);
            this.flowLayoutPanel1.Size = new System.Drawing.Size(638, 329);
            this.flowLayoutPanel1.TabIndex = 26;
            this.flowLayoutPanel1.WrapContents = false;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 8);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(98, 18);
            this.label5.TabIndex = 27;
            this.label5.Text = "服务器列表";
            // 
            // com_ServerInfo
            // 
            this.com_ServerInfo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.com_ServerInfo.FormattingEnabled = true;
            this.com_ServerInfo.Location = new System.Drawing.Point(12, 32);
            this.com_ServerInfo.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.com_ServerInfo.Name = "com_ServerInfo";
            this.com_ServerInfo.Size = new System.Drawing.Size(479, 26);
            this.com_ServerInfo.TabIndex = 26;
            this.com_ServerInfo.SelectedIndexChanged += new System.EventHandler(this.com_ServerInfo_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label3.Location = new System.Drawing.Point(12, 64);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(479, 18);
            this.label3.TabIndex = 23;
            this.label3.Text = "Gate地址";
            // 
            // txt_Server
            // 
            this.txt_Server.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txt_Server.FormattingEnabled = true;
            this.txt_Server.Location = new System.Drawing.Point(12, 88);
            this.txt_Server.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.txt_Server.Name = "txt_Server";
            this.txt_Server.Size = new System.Drawing.Size(479, 26);
            this.txt_Server.TabIndex = 22;
            this.txt_Server.Text = "127.0.0.1:19001";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label4.Location = new System.Drawing.Point(12, 120);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(479, 18);
            this.label4.TabIndex = 25;
            this.label4.Text = "服务器ID";
            // 
            // txt_ServerID
            // 
            this.txt_ServerID.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txt_ServerID.FormattingEnabled = true;
            this.txt_ServerID.Items.AddRange(new object[] {
            "1",
            "2",
            "3",
            "4"});
            this.txt_ServerID.Location = new System.Drawing.Point(12, 144);
            this.txt_ServerID.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.txt_ServerID.Name = "txt_ServerID";
            this.txt_ServerID.Size = new System.Drawing.Size(479, 26);
            this.txt_ServerID.TabIndex = 24;
            this.txt_ServerID.Text = "1";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Location = new System.Drawing.Point(12, 176);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(479, 18);
            this.label1.TabIndex = 17;
            this.label1.Text = "用户名";
            // 
            // txt_Account
            // 
            this.txt_Account.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txt_Account.FormattingEnabled = true;
            this.txt_Account.Location = new System.Drawing.Point(12, 200);
            this.txt_Account.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.txt_Account.Name = "txt_Account";
            this.txt_Account.Size = new System.Drawing.Size(479, 26);
            this.txt_Account.TabIndex = 15;
            this.txt_Account.Text = "hzdsb";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label2.Location = new System.Drawing.Point(12, 232);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(479, 18);
            this.label2.TabIndex = 18;
            this.label2.Text = "密码";
            // 
            // txt_Password
            // 
            this.txt_Password.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txt_Password.Location = new System.Drawing.Point(12, 256);
            this.txt_Password.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.txt_Password.Name = "txt_Password";
            this.txt_Password.PasswordChar = '*';
            this.txt_Password.Size = new System.Drawing.Size(479, 28);
            this.txt_Password.TabIndex = 16;
            this.txt_Password.Text = "123456";
            this.txt_Password.UseSystemPasswordChar = true;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer1.Location = new System.Drawing.Point(10, 0);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.flowLayoutPanel1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.g2DPropertyGrid1);
            this.splitContainer1.Size = new System.Drawing.Size(907, 329);
            this.splitContainer1.SplitterDistance = 638;
            this.splitContainer1.SplitterWidth = 3;
            this.splitContainer1.TabIndex = 27;
            // 
            // g2DPropertyGrid1
            // 
            this.g2DPropertyGrid1.DescriptionAreaHeight = 116;
            this.g2DPropertyGrid1.DescriptionAreaLineCount = 5;
            this.g2DPropertyGrid1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.g2DPropertyGrid1.ImeMode = System.Windows.Forms.ImeMode.Off;
            this.g2DPropertyGrid1.Location = new System.Drawing.Point(0, 0);
            this.g2DPropertyGrid1.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.g2DPropertyGrid1.MinDescriptionAreaLineCount = 5;
            this.g2DPropertyGrid1.Name = "g2DPropertyGrid1";
            this.g2DPropertyGrid1.Size = new System.Drawing.Size(266, 329);
            this.g2DPropertyGrid1.TabIndex = 0;
            // 
            // FormLogin
            // 
            this.AcceptButton = this.btn_Login;
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(926, 410);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.btn_Login);
            this.Controls.Add(this.btn_Regist);
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MinimumSize = new System.Drawing.Size(820, 458);
            this.Name = "FormLogin";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "FormLogin";
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button btn_Regist;
        private System.Windows.Forms.Button btn_Login;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox txt_Server;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox txt_ServerID;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox txt_Account;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txt_Password;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox com_ServerInfo;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private DeepEditor.Common.G2D.DataGrid.G2DPropertyGrid g2DPropertyGrid1;
    }
}