namespace CommonRPG.Client.Win32.Forms
{
    partial class FormStore
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
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabShop = new System.Windows.Forms.TabPage();
            this.tabStore = new System.Windows.Forms.TabPage();
            this.button1 = new System.Windows.Forms.Button();
            this.tabControl1.SuspendLayout();
            this.tabShop.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabShop);
            this.tabControl1.Controls.Add(this.tabStore);
            this.tabControl1.ItemSize = new System.Drawing.Size(88, 30);
            this.tabControl1.Location = new System.Drawing.Point(12, 3);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(866, 428);
            this.tabControl1.TabIndex = 0;
            // 
            // tabShop
            // 
            this.tabShop.Controls.Add(this.button1);
            this.tabShop.Location = new System.Drawing.Point(4, 34);
            this.tabShop.Name = "tabShop";
            this.tabShop.Padding = new System.Windows.Forms.Padding(3);
            this.tabShop.Size = new System.Drawing.Size(858, 390);
            this.tabShop.TabIndex = 0;
            this.tabShop.Text = "商店";
            this.tabShop.UseVisualStyleBackColor = true;
            this.tabShop.Click += new System.EventHandler(this.tabShop_Click);
            // 
            // tabStore
            // 
            this.tabStore.Location = new System.Drawing.Point(4, 34);
            this.tabStore.Name = "tabStore";
            this.tabStore.Padding = new System.Windows.Forms.Padding(3);
            this.tabStore.Size = new System.Drawing.Size(858, 390);
            this.tabStore.TabIndex = 1;
            this.tabStore.Text = "商城";
            this.tabStore.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(686, 30);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // FormStore
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(890, 443);
            this.Controls.Add(this.tabControl1);
            this.Name = "FormStore";
            this.Text = "FormStore";
            this.tabControl1.ResumeLayout(false);
            this.tabShop.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabShop;
        private System.Windows.Forms.TabPage tabStore;
        private System.Windows.Forms.Button button1;
    }
}