using DeepCore;
using DeepCore.Log;
using DeepCore.MPQ;
using DeepCore.MPQ.Updater;
using DeepCore.Xml;
using DeepEditor.Common.G2D;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace DeepMMO.Client.BotTest
{
    public partial class FormLauncher : Form
    {
        private static MPQFileSystem FileSystem { get; set; }
        private BotConfig bot_config;
        private BotConfigHistory bot_config_history = new BotConfigHistory();
        public static BotConfig LastConfig { get; private set; }
        public BotConfig Config
        {
            get { return bot_config; }
            set { SetConfig(bot_config); }
        }

        private Action<FormLauncher, BotConfig> m_OnStart;
        public event Action<FormLauncher, BotConfig> OnStart { add { m_OnStart += value; } remove { m_OnStart -= value; } }

        public FormLauncher()
        {
            this.bot_config = BotFactory.Instance.CreateBotConfig();
            InitializeComponent();
        }
        public void SetConfig(BotConfig value)
        {
            bot_config = value;
            var desc = this.prop_Config.SetSelectedObject(bot_config);
            desc.AppendOptionals(bot_config_history.List);
            bot_config_history.List = desc.GetOptionalsMap();
            this.prop_Config.Refresh();

        }
        private void FormLauncher_Load(object sender, EventArgs e)
        {
            try
            {
                var saved = XmlUtil.LoadXML(Application.StartupPath + "/bot_config.save");
                if (saved != null)
                {
                    this.bot_config = XmlUtil.XmlToObject(BotFactory.Instance.CreateBotConfig().GetType(), saved) as BotConfig;
                }
                var saved_history = XmlUtil.LoadXML(Application.StartupPath + "/bot_config_history.save");
                if (saved_history != null)
                {
                    this.bot_config_history = XmlUtil.XmlToObject<BotConfigHistory>(saved_history);
                }
            }
            catch (Exception err)
            {
                MessageBox.Show("加载配置失败: " + err.Message);
                this.Show();
                return;
            }
        }
        private void FormLauncher_Shown(object sender, EventArgs e)
        {
            this.SetConfig(bot_config);
        }
        private void btn_Start_Click(object sender, EventArgs e)
        {
            Start();
        }

        public void Start()
        {
            var config = XmlUtil.CloneObject(Config);
            try
            {
                if (BotLauncher.IsAuto == false)
                {
                    var save = XmlUtil.ObjectToXml(Config);
                    XmlUtil.SaveXML(Application.StartupPath + "/bot_config.save", save);
                }
                var desc = this.prop_Config.SelectedDescriptorObject;
                this.prop_Config.AppendCurrentToHistory();
                try
                {
                    bot_config_history.List = desc.GetOptionalsMap();
                }
                catch { }
                if (BotLauncher.IsAuto == false)
                {
                    var save_history = XmlUtil.ObjectToXml(bot_config_history);
                    XmlUtil.SaveXML(Application.StartupPath + "/bot_config_history.save", save_history);
                }
                this.Hide();
                BotFactory.Instance.Init(config);
            }
            catch (Exception err)
            {
                MessageBox.Show("初始化失败: " + err.Message);
                this.Show();
                return;
            }
            LastConfig = config;
            if (m_OnStart != null) { m_OnStart.Invoke(this, config); }
        }


    }
}
