namespace CommonRPG.Client.Win32.Forms
{
    partial class FormMail
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
            System.Windows.Forms.ListViewItem listViewItem1 = new System.Windows.Forms.ListViewItem(new string[] {
            "",
            "dasf",
            "fff"}, -1);
            System.Windows.Forms.ListViewItem listViewItem2 = new System.Windows.Forms.ListViewItem("");
            System.Windows.Forms.ListViewItem listViewItem3 = new System.Windows.Forms.ListViewItem("");
            System.Windows.Forms.ListViewItem listViewItem4 = new System.Windows.Forms.ListViewItem("");
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMail));
            this.listView1 = new System.Windows.Forms.ListView();
            this.columnHeader0 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButton2 = new System.Windows.Forms.ToolStripButton();
            this.btn_GetMailDetail = new System.Windows.Forms.ToolStripButton();
            this.btn_GetAttachment = new System.Windows.Forms.ToolStripButton();
            this.btn_WriteMail = new System.Windows.Forms.ToolStripButton();
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // listView1
            // 
            this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader0,
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader6,
            this.columnHeader3,
            this.columnHeader4,
            this.columnHeader5});
            this.listView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listView1.GridLines = true;
            this.listView1.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
            listViewItem1,
            listViewItem2,
            listViewItem3,
            listViewItem4});
            this.listView1.Location = new System.Drawing.Point(0, 0);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(804, 471);
            this.listView1.TabIndex = 0;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader0
            // 
            this.columnHeader0.Text = "Status";
            this.columnHeader0.Width = 76;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "UUID";
            this.columnHeader1.Width = 96;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Title";
            this.columnHeader2.Width = 65;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Sender";
            this.columnHeader3.Width = 83;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "SenderUUID";
            this.columnHeader4.Width = 93;
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "time";
            this.columnHeader5.Width = 104;
            // 
            // toolStrip1
            // 
            this.toolStrip1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButton1,
            this.toolStripButton2,
            this.btn_GetMailDetail,
            this.btn_GetAttachment,
            this.btn_WriteMail});
            this.toolStrip1.Location = new System.Drawing.Point(0, 471);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(804, 26);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripButton1
            // 
            this.toolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton1.Image")));
            this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton1.Name = "toolStripButton1";
            this.toolStripButton1.Size = new System.Drawing.Size(65, 23);
            this.toolStripButton1.Text = "删除邮件";
            this.toolStripButton1.Click += new System.EventHandler(this.btn_DeleteMail_Click);
            // 
            // toolStripButton2
            // 
            this.toolStripButton2.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButton2.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton2.Image")));
            this.toolStripButton2.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton2.Name = "toolStripButton2";
            this.toolStripButton2.Size = new System.Drawing.Size(65, 23);
            this.toolStripButton2.Text = "清空邮件";
            this.toolStripButton2.Click += new System.EventHandler(this.btn_ClearMail_Click);
            // 
            // btn_GetMailDetail
            // 
            this.btn_GetMailDetail.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btn_GetMailDetail.Image = ((System.Drawing.Image)(resources.GetObject("btn_GetMailDetail.Image")));
            this.btn_GetMailDetail.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btn_GetMailDetail.Name = "btn_GetMailDetail";
            this.btn_GetMailDetail.Size = new System.Drawing.Size(65, 23);
            this.btn_GetMailDetail.Text = "获取详情";
            this.btn_GetMailDetail.Click += new System.EventHandler(this.btn_GetMailDetail_Click);
            // 
            // btn_GetAttachment
            // 
            this.btn_GetAttachment.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btn_GetAttachment.Image = ((System.Drawing.Image)(resources.GetObject("btn_GetAttachment.Image")));
            this.btn_GetAttachment.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btn_GetAttachment.Name = "btn_GetAttachment";
            this.btn_GetAttachment.Size = new System.Drawing.Size(65, 23);
            this.btn_GetAttachment.Text = "获取附件";
            this.btn_GetAttachment.Click += new System.EventHandler(this.btn_GetAttachment_Click);
            // 
            // btn_WriteMail
            // 
            this.btn_WriteMail.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btn_WriteMail.Image = ((System.Drawing.Image)(resources.GetObject("btn_WriteMail.Image")));
            this.btn_WriteMail.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btn_WriteMail.Name = "btn_WriteMail";
            this.btn_WriteMail.Size = new System.Drawing.Size(65, 23);
            this.btn_WriteMail.Text = "新建邮件";
            this.btn_WriteMail.Click += new System.EventHandler(this.btn_WriteMail_Click);
            // 
            // columnHeader6
            // 
            this.columnHeader6.Text = "Attachment";
            this.columnHeader6.Width = 106;
            // 
            // FormMail
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(804, 497);
            this.Controls.Add(this.listView1);
            this.Controls.Add(this.toolStrip1);
            this.Name = "FormMail";
            this.Text = "FormMail";
            this.Load += new System.EventHandler(this.FormMail_Load);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton btn_GetAttachment;
        private System.Windows.Forms.ColumnHeader columnHeader0;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.ToolStripButton btn_WriteMail;
        private System.Windows.Forms.ToolStripButton toolStripButton1;
        private System.Windows.Forms.ToolStripButton toolStripButton2;
        private System.Windows.Forms.ToolStripButton btn_GetMailDetail;
        private System.Windows.Forms.ColumnHeader columnHeader6;
    }
}