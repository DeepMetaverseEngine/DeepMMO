namespace DeepMMO.Client.BotTest
{
    partial class FormLauncher
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
            this.prop_Config = new DeepEditor.Common.G2D.DataGrid.G2DPropertyGrid();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.btn_Start = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // prop_Config
            // 
            this.prop_Config.CategoryForeColor = System.Drawing.SystemColors.InactiveCaptionText;
            this.prop_Config.DescriptionAreaHeight = 81;
            this.prop_Config.DescriptionAreaLineCount = 5;
            this.prop_Config.Dock = System.Windows.Forms.DockStyle.Fill;
            this.prop_Config.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.prop_Config.LineColor = System.Drawing.SystemColors.ControlDark;
            this.prop_Config.Location = new System.Drawing.Point(0, 0);
            this.prop_Config.MinDescriptionAreaLineCount = 5;
            this.prop_Config.Name = "prop_Config";
            this.prop_Config.Size = new System.Drawing.Size(730, 660);
            this.prop_Config.TabIndex = 0;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.prop_Config);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.btn_Start);
            this.splitContainer1.Size = new System.Drawing.Size(730, 747);
            this.splitContainer1.SplitterDistance = 660;
            this.splitContainer1.TabIndex = 1;
            // 
            // btn_Start
            // 
            this.btn_Start.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_Start.Location = new System.Drawing.Point(590, 30);
            this.btn_Start.Name = "btn_Start";
            this.btn_Start.Size = new System.Drawing.Size(129, 40);
            this.btn_Start.TabIndex = 0;
            this.btn_Start.Text = "开始";
            this.btn_Start.UseVisualStyleBackColor = true;
            this.btn_Start.Click += new System.EventHandler(this.btn_Start_Click);
            // 
            // FormLauncher
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(730, 747);
            this.Controls.Add(this.splitContainer1);
            this.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.Name = "FormLauncher";
            this.Text = "配置机器人";
            this.Load += new System.EventHandler(this.FormLauncher_Load);
            this.Shown += new System.EventHandler(this.FormLauncher_Shown);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private DeepEditor.Common.G2D.DataGrid.G2DPropertyGrid prop_Config;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Button btn_Start;
    }
}