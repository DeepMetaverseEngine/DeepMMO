using DeepCore.GameData.Zone;
using DeepCore.GameData.Zone.ZoneEditor;
using DeepCore.Protocol;
using DeepEditor.Plugin3D.BattleClient;
using DeepMMO.Client.Battle;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace DeepMMO.Client.Win32.Battle
{
    public class PanelBattle : PanelBattleView3D
    {
        public readonly GamePanelContainer container;
        public readonly RPGBattleClient battle;

        private static float s_TurboX = 1f;
        private static DeepCore.Vector.Vector2 s_CameraScal;
        private static Pen s_RangePen = new Pen(Color.FromArgb(128, 255, 255, 255));

        public PanelBattle(GamePanelContainer container, RPGBattleClient bot)
        {
            this.container = container;
            this.battle = bot;
            base.Start(new BotBattleFactory(battle));
            base.pictureBox1.MouseWheel += PictureBox1_MouseWheel;
            base.OnNetworkViewClicked += PanelBattle_OnNetworkViewClicked;
            this.OnTurboChanged += PanelBattle_OnTurboChanged;
            this.OnTimerBeginUpdate += PanelBattle_OnTimerBeginUpdate;
        }

        private void PanelBattle_OnTimerBeginUpdate(int intervalMS)
        {
            if (s_TurboX != base.TurboX)
            {
                base.TurboX = s_TurboX;
            }
        }
        private void PanelBattle_OnTurboChanged(float turbo)
        {
            s_TurboX = turbo;
        }

        private void PanelBattle_OnNetworkViewClicked()
        {
            container.SessionView.Show();
        }

        public override string ToString()
        {
            return this.battle.ToString();
        }
        public override void updateBattle(int intervalMS)
        {
            base.updateBattle(intervalMS);
        }

        protected override void timer1_Tick(object sender, EventArgs e)
        {
            if (base.DisplayWorld != null && s_CameraScal != null)
            {
                if (base.DisplayWorld.getCameraScaleX() != s_CameraScal.x || base.DisplayWorld.getCameraScaleY() != s_CameraScal.y)
                {
                    base.DisplayWorld.setCameraScale(s_CameraScal.x, s_CameraScal.y);
                }
            }
        }
        private void PictureBox1_MouseWheel(object sender, MouseEventArgs e)
        {
            if (base.DisplayWorld != null)
            {
                s_CameraScal = new DeepCore.Vector.Vector2(base.DisplayWorld.getCameraScaleX(), base.DisplayWorld.getCameraScaleY());
            }
        }


        protected override void Layer_ActorAdded(ZoneLayer layer, ZoneActor actor)
        {
            this.btn_Guard.Checked = battle.Layer.Actor.IsGuard;
        }
        protected override void Layer_MessageReceived(ZoneLayer layer, IMessage e)
        {
            if (e is ServerExceptionB2C)
            {
                ServerExceptionB2C err = e as ServerExceptionB2C;
                //MessageBox.Show(over.Message + "\n" + over.StackTrace, e.GetType().Name);
                Console.WriteLine("ServerExceptionB2C : " + err.Message + "\r\n" + err.StackTrace);
                return;
            }
            base.Layer_MessageReceived(layer, e);
        }
        public class BotBattleFactory : IAbstractBattleFactory
        {
            public readonly RPGBattleClient battle;
            public BotBattleFactory(RPGBattleClient bot) { this.battle = bot; }
            public EditorTemplates DataRoot { get { return RPGClientBattleManager.DataRoot; } }
            public AbstractBattle GenBattle() { return battle; }
            public DisplayLayerWorld GenDisplay(PictureBox control) { return new BotBattleDisplay(); }
        }
        public class BotBattleDisplay : DisplayLayerWorld
        {
            public BotBattleDisplay()
            {
                base.ShowHP = true;
                base.ShowLog = false;
                base.ShowName = true;
                base.ShowFlagName = true;
                base.ShowAOI = true;
                base.ShowTerrainMesh = true;
            }
            //             protected override void clientUpdate(int intervalMS)
            //             {
            //             }
            public override void Dispose()
            {

            }
        }

        private void InitializeComponent()
        {
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // PanelBattle
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.Name = "PanelBattle";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
    }
}
