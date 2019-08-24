namespace DeepMMO.Client.BotTest
{
    partial class FormBotTest
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormBotTest));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.menu_Main = new System.Windows.Forms.ToolStripDropDownButton();
            this.btn_AddBots = new System.Windows.Forms.ToolStripMenuItem();
            this.btn_CleanBots = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.btn_GC = new System.Windows.Forms.ToolStripMenuItem();
            this.btn_StopAll = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.group_Module = new System.Windows.Forms.ToolStripDropDownButton();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.list_Bots = new System.Windows.Forms.ListView();
            this.column_Account = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.column_Role = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.column_Scene = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.column_Status = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.column_Net = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.menu_BotItem = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.btn_BotReconnect = new System.Windows.Forms.ToolStripMenuItem();
            this.btn_BotStop = new System.Windows.Forms.ToolStripMenuItem();
            this.btn_BotStart = new System.Windows.Forms.ToolStripMenuItem();
            this.btn_EmuDisconnect = new System.Windows.Forms.ToolStripMenuItem();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.text_Events = new System.Windows.Forms.TextBox();
            this.toolStrip2 = new System.Windows.Forms.ToolStrip();
            this.btn_ClearAllConsole = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.btn_ClearConsole = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.lbl_NetStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.timer_3000 = new System.Windows.Forms.Timer(this.components);
            this.timer_30 = new System.Windows.Forms.Timer(this.components);
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.menu_BotItem.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.toolStrip2.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menu_Main,
            this.toolStripSeparator1,
            this.group_Module,
            this.toolStripSeparator4});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Padding = new System.Windows.Forms.Padding(0, 0, 2, 0);
            this.toolStrip1.Size = new System.Drawing.Size(2136, 25);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // menu_Main
            // 
            this.menu_Main.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.menu_Main.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btn_AddBots,
            this.btn_CleanBots,
            this.toolStripMenuItem1,
            this.btn_GC,
            this.btn_StopAll});
            this.menu_Main.Image = ((System.Drawing.Image)(resources.GetObject("menu_Main.Image")));
            this.menu_Main.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.menu_Main.Name = "menu_Main";
            this.menu_Main.Size = new System.Drawing.Size(62, 22);
            this.menu_Main.Text = "菜单";
            // 
            // btn_AddBots
            // 
            this.btn_AddBots.Name = "btn_AddBots";
            this.btn_AddBots.Size = new System.Drawing.Size(252, 30);
            this.btn_AddBots.Text = "添加机器人";
            this.btn_AddBots.Click += new System.EventHandler(this.btn_AddBots_Click);
            // 
            // btn_CleanBots
            // 
            this.btn_CleanBots.Name = "btn_CleanBots";
            this.btn_CleanBots.Size = new System.Drawing.Size(252, 30);
            this.btn_CleanBots.Text = "清理机器人";
            this.btn_CleanBots.Click += new System.EventHandler(this.btn_CleanBots_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(249, 6);
            // 
            // btn_GC
            // 
            this.btn_GC.Name = "btn_GC";
            this.btn_GC.Size = new System.Drawing.Size(252, 30);
            this.btn_GC.Text = "GC";
            this.btn_GC.Click += new System.EventHandler(this.btn_GC_Click);
            // 
            // btn_StopAll
            // 
            this.btn_StopAll.Name = "btn_StopAll";
            this.btn_StopAll.Size = new System.Drawing.Size(252, 30);
            this.btn_StopAll.Text = "停止所有机器人";
            this.btn_StopAll.Click += new System.EventHandler(this.btn_StopAll_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // group_Module
            // 
            this.group_Module.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.group_Module.Image = ((System.Drawing.Image)(resources.GetObject("group_Module.Image")));
            this.group_Module.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.group_Module.Name = "group_Module";
            this.group_Module.Size = new System.Drawing.Size(98, 22);
            this.group_Module.Text = "测试模块";
            this.group_Module.Click += new System.EventHandler(this.group_Module_Click);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(6, 25);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer1.Location = new System.Drawing.Point(0, 25);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(4);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.list_Bots);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
            this.splitContainer1.Size = new System.Drawing.Size(2136, 1239);
            this.splitContainer1.SplitterDistance = 585;
            this.splitContainer1.SplitterWidth = 6;
            this.splitContainer1.TabIndex = 1;
            // 
            // list_Bots
            // 
            this.list_Bots.AutoArrange = false;
            this.list_Bots.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.column_Account,
            this.column_Role,
            this.column_Scene,
            this.column_Status,
            this.column_Net});
            this.list_Bots.ContextMenuStrip = this.menu_BotItem;
            this.list_Bots.Dock = System.Windows.Forms.DockStyle.Fill;
            this.list_Bots.FullRowSelect = true;
            this.list_Bots.GridLines = true;
            this.list_Bots.HideSelection = false;
            this.list_Bots.Location = new System.Drawing.Point(0, 0);
            this.list_Bots.Margin = new System.Windows.Forms.Padding(4);
            this.list_Bots.Name = "list_Bots";
            this.list_Bots.Size = new System.Drawing.Size(585, 1239);
            this.list_Bots.TabIndex = 0;
            this.list_Bots.UseCompatibleStateImageBehavior = false;
            this.list_Bots.View = System.Windows.Forms.View.Details;
            this.list_Bots.SelectedIndexChanged += new System.EventHandler(this.list_Bots_SelectedIndexChanged);
            // 
            // column_Account
            // 
            this.column_Account.Text = "帐号";
            this.column_Account.Width = 66;
            // 
            // column_Role
            // 
            this.column_Role.Text = "角色";
            this.column_Role.Width = 67;
            // 
            // column_Scene
            // 
            this.column_Scene.Text = "场景";
            this.column_Scene.Width = 74;
            // 
            // column_Status
            // 
            this.column_Status.Text = "状态";
            this.column_Status.Width = 178;
            // 
            // column_Net
            // 
            this.column_Net.Text = "网络";
            this.column_Net.Width = 154;
            // 
            // menu_BotItem
            // 
            this.menu_BotItem.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.menu_BotItem.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btn_BotReconnect,
            this.btn_BotStop,
            this.btn_BotStart,
            this.btn_EmuDisconnect});
            this.menu_BotItem.Name = "menu_BotItem";
            this.menu_BotItem.Size = new System.Drawing.Size(241, 149);
            this.menu_BotItem.Opening += new System.ComponentModel.CancelEventHandler(this.menu_BotItem_Opening);
            // 
            // btn_BotReconnect
            // 
            this.btn_BotReconnect.Name = "btn_BotReconnect";
            this.btn_BotReconnect.Size = new System.Drawing.Size(240, 28);
            this.btn_BotReconnect.Text = "Reconnect";
            this.btn_BotReconnect.Click += new System.EventHandler(this.btn_BotReconnect_Click);
            // 
            // btn_BotStop
            // 
            this.btn_BotStop.Name = "btn_BotStop";
            this.btn_BotStop.Size = new System.Drawing.Size(240, 28);
            this.btn_BotStop.Text = "Stop";
            this.btn_BotStop.Click += new System.EventHandler(this.btn_BotStop_Click);
            // 
            // btn_BotStart
            // 
            this.btn_BotStart.Name = "btn_BotStart";
            this.btn_BotStart.Size = new System.Drawing.Size(240, 28);
            this.btn_BotStart.Text = "Start";
            this.btn_BotStart.Click += new System.EventHandler(this.btn_BotStart_Click);
            // 
            // btn_EmuDisconnect
            // 
            this.btn_EmuDisconnect.Name = "btn_EmuDisconnect";
            this.btn_EmuDisconnect.Size = new System.Drawing.Size(240, 28);
            this.btn_EmuDisconnect.Text = "模拟断线";
            this.btn_EmuDisconnect.Click += new System.EventHandler(this.btn_EmuDisconnect_Click);
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Margin = new System.Windows.Forms.Padding(4);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.BackColor = System.Drawing.Color.Black;
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.text_Events);
            this.splitContainer2.Panel2.Controls.Add(this.toolStrip2);
            this.splitContainer2.Size = new System.Drawing.Size(1545, 1239);
            this.splitContainer2.SplitterDistance = 533;
            this.splitContainer2.SplitterWidth = 6;
            this.splitContainer2.TabIndex = 0;
            // 
            // text_Events
            // 
            this.text_Events.Dock = System.Windows.Forms.DockStyle.Fill;
            this.text_Events.Location = new System.Drawing.Point(0, 25);
            this.text_Events.Margin = new System.Windows.Forms.Padding(4);
            this.text_Events.Multiline = true;
            this.text_Events.Name = "text_Events";
            this.text_Events.Size = new System.Drawing.Size(1545, 675);
            this.text_Events.TabIndex = 0;
            // 
            // toolStrip2
            // 
            this.toolStrip2.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip2.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btn_ClearAllConsole,
            this.toolStripSeparator2,
            this.btn_ClearConsole,
            this.toolStripSeparator3});
            this.toolStrip2.Location = new System.Drawing.Point(0, 0);
            this.toolStrip2.Name = "toolStrip2";
            this.toolStrip2.Padding = new System.Windows.Forms.Padding(0, 0, 2, 0);
            this.toolStrip2.Size = new System.Drawing.Size(1545, 25);
            this.toolStrip2.TabIndex = 1;
            this.toolStrip2.Text = "toolStrip2";
            // 
            // btn_ClearAllConsole
            // 
            this.btn_ClearAllConsole.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btn_ClearAllConsole.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_ClearAllConsole.Image = ((System.Drawing.Image)(resources.GetObject("btn_ClearAllConsole.Image")));
            this.btn_ClearAllConsole.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btn_ClearAllConsole.Name = "btn_ClearAllConsole";
            this.btn_ClearAllConsole.Size = new System.Drawing.Size(84, 22);
            this.btn_ClearAllConsole.Text = "清除所有";
            this.btn_ClearAllConsole.Click += new System.EventHandler(this.btn_ClearAllConsole_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // btn_ClearConsole
            // 
            this.btn_ClearConsole.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btn_ClearConsole.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_ClearConsole.Image = ((System.Drawing.Image)(resources.GetObject("btn_ClearConsole.Image")));
            this.btn_ClearConsole.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btn_ClearConsole.Name = "btn_ClearConsole";
            this.btn_ClearConsole.Size = new System.Drawing.Size(84, 22);
            this.btn_ClearConsole.Text = "清除日志";
            this.btn_ClearConsole.Click += new System.EventHandler(this.btn_ClearConsole_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lbl_NetStatus});
            this.statusStrip1.Location = new System.Drawing.Point(0, 1264);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Padding = new System.Windows.Forms.Padding(2, 0, 21, 0);
            this.statusStrip1.Size = new System.Drawing.Size(2136, 29);
            this.statusStrip1.TabIndex = 1;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // lbl_NetStatus
            // 
            this.lbl_NetStatus.Name = "lbl_NetStatus";
            this.lbl_NetStatus.Size = new System.Drawing.Size(195, 24);
            this.lbl_NetStatus.Text = "toolStripStatusLabel1";
            // 
            // timer_3000
            // 
            this.timer_3000.Enabled = true;
            this.timer_3000.Interval = 3000;
            this.timer_3000.Tick += new System.EventHandler(this.timer_3000_Tick);
            // 
            // timer_30
            // 
            this.timer_30.Enabled = true;
            this.timer_30.Interval = 33;
            this.timer_30.Tick += new System.EventHandler(this.timer_30_Tick);
            // 
            // FormBotTest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(2136, 1293);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.toolStrip1);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "FormBotTest";
            this.Text = "FormBotTest";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormBotTest_FormClosing);
            this.Load += new System.EventHandler(this.FormBotTest_Load);
            this.Shown += new System.EventHandler(this.FormBotTest_Shown);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.menu_BotItem.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            this.splitContainer2.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.toolStrip2.ResumeLayout(false);
            this.toolStrip2.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripDropDownButton menu_Main;
        private System.Windows.Forms.ToolStripMenuItem btn_AddBots;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ListView list_Bots;
        private System.Windows.Forms.ColumnHeader column_Account;
        private System.Windows.Forms.ColumnHeader column_Status;
        private System.Windows.Forms.ColumnHeader column_Role;
        private System.Windows.Forms.Timer timer_3000;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.TextBox text_Events;
        private System.Windows.Forms.Timer timer_30;
        private System.Windows.Forms.ColumnHeader column_Net;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStrip toolStrip2;
        private System.Windows.Forms.ToolStripButton btn_ClearConsole;
        private System.Windows.Forms.ToolStripButton btn_ClearAllConsole;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripMenuItem btn_StopAll;
        private System.Windows.Forms.ToolStripMenuItem btn_GC;
        private System.Windows.Forms.ToolStripDropDownButton group_Module;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ColumnHeader column_Scene;
        private System.Windows.Forms.ContextMenuStrip menu_BotItem;
        private System.Windows.Forms.ToolStripMenuItem btn_BotReconnect;
        private System.Windows.Forms.ToolStripMenuItem btn_BotStop;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel lbl_NetStatus;
        private System.Windows.Forms.ToolStripMenuItem btn_EmuDisconnect;
        private System.Windows.Forms.ToolStripMenuItem btn_BotStart;
        private System.Windows.Forms.ToolStripMenuItem btn_CleanBots;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
    }
}