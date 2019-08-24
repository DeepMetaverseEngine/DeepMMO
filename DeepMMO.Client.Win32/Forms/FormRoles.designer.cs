namespace DeepMMO.Client.Win32.Forms
{
    partial class FormRoles
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
            this.combo_Pro = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txt_RoleName = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.btn_RandomName = new System.Windows.Forms.Button();
            this.list_Roles = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage_SelectRole = new System.Windows.Forms.TabPage();
            this.btn_Enter = new System.Windows.Forms.Button();
            this.tabPage_CreateRole = new System.Windows.Forms.TabPage();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btn_CreateRole = new System.Windows.Forms.Button();
            this.tabControl1.SuspendLayout();
            this.tabPage_SelectRole.SuspendLayout();
            this.tabPage_CreateRole.SuspendLayout();
            this.SuspendLayout();
            // 
            // combo_Pro
            // 
            this.combo_Pro.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.combo_Pro.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.combo_Pro.FormattingEnabled = true;
            this.combo_Pro.Location = new System.Drawing.Point(168, 441);
            this.combo_Pro.Margin = new System.Windows.Forms.Padding(4);
            this.combo_Pro.Name = "combo_Pro";
            this.combo_Pro.Size = new System.Drawing.Size(180, 46);
            this.combo_Pro.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(84, 446);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(75, 38);
            this.label1.TabIndex = 1;
            this.label1.Text = "职业";
            // 
            // txt_RoleName
            // 
            this.txt_RoleName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.txt_RoleName.Location = new System.Drawing.Point(538, 441);
            this.txt_RoleName.Margin = new System.Windows.Forms.Padding(4);
            this.txt_RoleName.Name = "txt_RoleName";
            this.txt_RoleName.Size = new System.Drawing.Size(372, 45);
            this.txt_RoleName.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(398, 446);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(133, 38);
            this.label2.TabIndex = 3;
            this.label2.Text = "输入名字";
            // 
            // btn_RandomName
            // 
            this.btn_RandomName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btn_RandomName.Location = new System.Drawing.Point(921, 441);
            this.btn_RandomName.Margin = new System.Windows.Forms.Padding(4);
            this.btn_RandomName.Name = "btn_RandomName";
            this.btn_RandomName.Size = new System.Drawing.Size(112, 52);
            this.btn_RandomName.TabIndex = 4;
            this.btn_RandomName.Text = "随机名字";
            this.btn_RandomName.UseVisualStyleBackColor = true;
            this.btn_RandomName.Click += new System.EventHandler(this.btn_RandomName_Click);
            // 
            // list_Roles
            // 
            this.list_Roles.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.list_Roles.AutoArrange = false;
            this.list_Roles.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader4,
            this.columnHeader5,
            this.columnHeader6});
            this.list_Roles.FullRowSelect = true;
            this.list_Roles.GridLines = true;
            this.list_Roles.HideSelection = false;
            this.list_Roles.LabelWrap = false;
            this.list_Roles.Location = new System.Drawing.Point(15, 9);
            this.list_Roles.Margin = new System.Windows.Forms.Padding(4);
            this.list_Roles.MultiSelect = false;
            this.list_Roles.Name = "list_Roles";
            this.list_Roles.Size = new System.Drawing.Size(1214, 432);
            this.list_Roles.TabIndex = 5;
            this.list_Roles.UseCompatibleStateImageBehavior = false;
            this.list_Roles.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Name";
            this.columnHeader1.Width = 134;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Level";
            this.columnHeader2.Width = 85;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Pro";
            this.columnHeader3.Width = 76;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "LastTime";
            this.columnHeader4.Width = 206;
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "最后登录";
            this.columnHeader5.Width = 114;
            // 
            // columnHeader6
            // 
            this.columnHeader6.Text = "ServerID";
            this.columnHeader6.Width = 120;
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage_SelectRole);
            this.tabControl1.Controls.Add(this.tabPage_CreateRole);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Font = new System.Drawing.Font("微软雅黑", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Margin = new System.Windows.Forms.Padding(4);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1252, 692);
            this.tabControl1.TabIndex = 6;
            // 
            // tabPage_SelectRole
            // 
            this.tabPage_SelectRole.Controls.Add(this.btn_Enter);
            this.tabPage_SelectRole.Controls.Add(this.list_Roles);
            this.tabPage_SelectRole.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tabPage_SelectRole.Location = new System.Drawing.Point(4, 47);
            this.tabPage_SelectRole.Margin = new System.Windows.Forms.Padding(4);
            this.tabPage_SelectRole.Name = "tabPage_SelectRole";
            this.tabPage_SelectRole.Padding = new System.Windows.Forms.Padding(4);
            this.tabPage_SelectRole.Size = new System.Drawing.Size(1244, 641);
            this.tabPage_SelectRole.TabIndex = 0;
            this.tabPage_SelectRole.Text = "已有角色";
            this.tabPage_SelectRole.UseVisualStyleBackColor = true;
            // 
            // btn_Enter
            // 
            this.btn_Enter.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.btn_Enter.Location = new System.Drawing.Point(539, 500);
            this.btn_Enter.Margin = new System.Windows.Forms.Padding(4);
            this.btn_Enter.Name = "btn_Enter";
            this.btn_Enter.Size = new System.Drawing.Size(192, 74);
            this.btn_Enter.TabIndex = 6;
            this.btn_Enter.Text = "进入游戏";
            this.btn_Enter.UseVisualStyleBackColor = true;
            this.btn_Enter.Click += new System.EventHandler(this.btn_Enter_Click);
            // 
            // tabPage_CreateRole
            // 
            this.tabPage_CreateRole.Controls.Add(this.panel1);
            this.tabPage_CreateRole.Controls.Add(this.btn_CreateRole);
            this.tabPage_CreateRole.Controls.Add(this.txt_RoleName);
            this.tabPage_CreateRole.Controls.Add(this.btn_RandomName);
            this.tabPage_CreateRole.Controls.Add(this.combo_Pro);
            this.tabPage_CreateRole.Controls.Add(this.label2);
            this.tabPage_CreateRole.Controls.Add(this.label1);
            this.tabPage_CreateRole.Location = new System.Drawing.Point(4, 47);
            this.tabPage_CreateRole.Margin = new System.Windows.Forms.Padding(4);
            this.tabPage_CreateRole.Name = "tabPage_CreateRole";
            this.tabPage_CreateRole.Padding = new System.Windows.Forms.Padding(4);
            this.tabPage_CreateRole.Size = new System.Drawing.Size(1111, 647);
            this.tabPage_CreateRole.TabIndex = 1;
            this.tabPage_CreateRole.Text = "创建角色";
            this.tabPage_CreateRole.UseVisualStyleBackColor = true;
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.Location = new System.Drawing.Point(24, 26);
            this.panel1.Margin = new System.Windows.Forms.Padding(4);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1047, 398);
            this.panel1.TabIndex = 8;
            // 
            // btn_CreateRole
            // 
            this.btn_CreateRole.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.btn_CreateRole.Location = new System.Drawing.Point(454, 537);
            this.btn_CreateRole.Margin = new System.Windows.Forms.Padding(4);
            this.btn_CreateRole.Name = "btn_CreateRole";
            this.btn_CreateRole.Size = new System.Drawing.Size(192, 74);
            this.btn_CreateRole.TabIndex = 7;
            this.btn_CreateRole.Text = "创建角色";
            this.btn_CreateRole.UseVisualStyleBackColor = true;
            this.btn_CreateRole.Click += new System.EventHandler(this.btn_CreateRole_Click);
            // 
            // FormRoles
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1252, 692);
            this.Controls.Add(this.tabControl1);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "FormRoles";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "FormCreateRole";
            this.tabControl1.ResumeLayout(false);
            this.tabPage_SelectRole.ResumeLayout(false);
            this.tabPage_CreateRole.ResumeLayout(false);
            this.tabPage_CreateRole.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox combo_Pro;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txt_RoleName;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btn_RandomName;
        private System.Windows.Forms.ListView list_Roles;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage_SelectRole;
        private System.Windows.Forms.TabPage tabPage_CreateRole;
        private System.Windows.Forms.Button btn_Enter;
        private System.Windows.Forms.Button btn_CreateRole;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ColumnHeader columnHeader6;
    }
}