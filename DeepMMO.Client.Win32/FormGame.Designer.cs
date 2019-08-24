namespace DeepMMO.Client.Win32
{
    partial class FormGame
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.panelBattle = new System.Windows.Forms.Panel();
            this.gamePanel = new DeepMMO.Client.Win32.Battle.GamePanelContainer();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.timerUpdate = new System.Windows.Forms.Timer(this.components);
            this.timerTest = new System.Windows.Forms.Timer(this.components);
            this.panelBattle.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelBattle
            // 
            this.panelBattle.Controls.Add(this.gamePanel);
            this.panelBattle.Controls.Add(this.statusStrip1);
            this.panelBattle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelBattle.Location = new System.Drawing.Point(0, 0);
            this.panelBattle.Name = "panelBattle";
            this.panelBattle.Size = new System.Drawing.Size(1016, 700);
            this.panelBattle.TabIndex = 1;
            // 
            // battlePanelContainer1
            // 
            this.gamePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gamePanel.Location = new System.Drawing.Point(0, 0);
            this.gamePanel.Name = "battlePanelContainer1";
            this.gamePanel.Size = new System.Drawing.Size(1016, 678);
            this.gamePanel.TabIndex = 1;
            // 
            // statusStrip1
            // 
            this.statusStrip1.Location = new System.Drawing.Point(0, 678);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1016, 22);
            this.statusStrip1.TabIndex = 0;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // timerUpdate
            // 
            this.timerUpdate.Enabled = true;
            this.timerUpdate.Interval = 33;
            this.timerUpdate.Tick += new System.EventHandler(this.timerUpdate_Tick);
            // 
            // timerTest
            // 
            this.timerTest.Enabled = true;
            this.timerTest.Interval = 3000;
            this.timerTest.Tick += new System.EventHandler(this.timerTest_Tick);
            // 
            // FormGame
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1016, 700);
            this.Controls.Add(this.panelBattle);
            this.Name = "FormGame";
            this.Text = "FormTL";
            this.panelBattle.ResumeLayout(false);
            this.panelBattle.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Panel panelBattle;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.Timer timerUpdate;
        private System.Windows.Forms.Timer timerTest;
        private Battle.GamePanelContainer gamePanel;
    }
}

