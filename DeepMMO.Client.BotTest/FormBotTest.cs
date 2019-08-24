
using DeepCore;
using DeepCore.Reflection;
using DeepCore.Xml;
using DeepEditor.Common.G2D;
using DeepMMO.Client.BotTest.Runner;
using DeepMMO.Client.Win32.Battle;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DeepMMO.Client.BotTest
{
    public partial class FormBotTest : Form
    {
        private static bool first_start = true;
        private int lastIndex = 0;
        private long last_update_time;
        private BotConfig config;
        private bool started = false;

        public FormBotTest(BotConfig cfg)
        {
            this.config = cfg;
            InitializeComponent();
            InitBotModules();
        }

        public BotListViewItem SelectedBotItem
        {
            get
            {
                if (list_Bots.SelectedItems.Count > 0)
                {
                    return list_Bots.SelectedItems[0] as BotListViewItem;
                }
                return null;
            }
        }
        public BotListViewItem[] SelectedBotItems
        {
            get
            {
                return list_Bots.SelectedItems.ToArray<BotListViewItem>();
            }
        }

        private void FormBotTest_Load(object sender, EventArgs e)
        {
            if (BotLauncher.NoBattleView)
            {
                splitContainer2.Panel1Collapsed = true;
            }
        }
        private void FormBotTest_Shown(object sender, EventArgs e)
        {
            if (BotLauncher.IsAuto && first_start)
            {
                first_start = false;
                StartBots(BotLauncher.DefaultBotPrefix, BotLauncher.DefaultBotCount);
            }
        }
        private void FormBotTest_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (BotListViewItem item in list_Bots.Items)
            {
                try
                {
                    item.Runner.Dispose();
                }
                catch (Exception err)
                {
                    Console.WriteLine(err.Message);
                }
            }
        }

        private void btn_GC_Click(object sender, EventArgs e)
        {
            System.GC.Collect();
        }
        private void timer_30_Tick(object sender, EventArgs e)
        {
            if (base.Visible)
            {
                long curTime = DeepCore.CUtils.CurrentTimeMS;
                if (last_update_time == 0)
                {
                    last_update_time = curTime;
                }
                int intervalMS = (int)(curTime - last_update_time);
                last_update_time = curTime;
                intervalMS = Math.Min(intervalMS, timer_30.Interval * 2);
                foreach (BotListViewItem item in list_Bots.Items)
                {
                    item.Update(intervalMS);
                }
            }
            var selected = SelectedBotItem;
            if (text_Events.Tag != selected)
            {
                text_Events.Tag = selected;
                text_Events.Clear();
            }
            if (selected != null)
            {
                if (text_Events.TextLength < selected.Events.Length)
                {
                    char[] copyto = new char[selected.Events.Length - text_Events.TextLength];
                    selected.Events.CopyTo(text_Events.TextLength, copyto, 0, copyto.Length);
                    text_Events.AppendText(new string(copyto));
                }
                if (text_Events.TextLength > selected.Events.Length)
                {
                    text_Events.Clear();
                }
            }
        }

        private void timer_3000_Tick(object sender, EventArgs e)
        {
            long totalRecvBytes = 0;
            long totalSentBytes = 0;
            foreach (var item in list_Bots.Items.ToArray<BotListViewItem>())
            {
                item.Refresh();
                totalRecvBytes += item.Client.GameClient.TotalRecvBytes;
                totalSentBytes += item.Client.GameClient.TotalSentBytes;
                if (config.KeepBotCount > 0)
                {
                    if (item.Runner.IsDisposed)
                    {
                        try { list_Bots.Items.Remove(item); } catch { }
                    }
                    else if (item.Runner.Client.IsGameDisconnected)
                    {
                        try { item.Runner.Dispose(); } catch { }
                    }
                }
            }
            if (started && config.KeepBotCount > 0 && list_Bots.Items.Count < config.KeepBotCount)
            {
                StartBots(BotLauncher.DefaultBotPrefix, config.KeepBotCount - list_Bots.Items.Count, true);
            }
            this.lbl_NetStatus.Text = string.Format("TotalRecv={0} TotalSent={1}",
            CUtils.ToBytesSizeString(totalRecvBytes),
            CUtils.ToBytesSizeString(totalSentBytes));
            this.Text = "BotTest :  (" + list_Bots.Items.Count + ")";
        }

        private void btn_AddBots_Click(object sender, EventArgs e)
        {
            StartBots();
        }
        private void btn_StopAll_Click(object sender, EventArgs e)
        {
            foreach (BotListViewItem item in list_Bots.Items)
            {
                try
                {
                    item.Runner.Stop();
                }
                catch (Exception err)
                {
                    Console.WriteLine(err.Message);
                }
            }
        }
        private void btn_CleanBots_Click(object sender, EventArgs e)
        {
            for (int i = list_Bots.Items.Count - 1; i >= 0; --i)
            {
                try
                {
                    BotListViewItem item = list_Bots.Items[i] as BotListViewItem;
                    if (item.Client.IsGameDisconnected)
                    {
                        list_Bots.Items.RemoveAt(i);
                        try { item.Runner.Dispose(); } catch { }
                    }
                }
                catch (Exception err)
                {
                    Console.WriteLine(err.Message);
                }
            }
        }
        private void btn_ClearConsole_Click(object sender, EventArgs e)
        {
            text_Events.Clear();
            var selected = SelectedBotItem;
            if (selected != null)
            {
                selected.Events.Clear();
            }
        }
        private void btn_ClearAllConsole_Click(object sender, EventArgs e)
        {
            text_Events.Clear();
            foreach (BotListViewItem item in list_Bots.Items)
            {
                item.Events.Clear();
            }
        }
        private void list_Bots_SelectedIndexChanged(object sender, EventArgs e)
        {
            splitContainer2.Panel1.Controls.Clear();
            var selected = SelectedBotItem;
            if (selected != null && selected.BattleView != null)
            {
                splitContainer2.Panel1.Controls.Add(selected.BattleView);
            }
        }

        private void moduleItem_CheckedChanged(object sender, EventArgs e)
        {
            var item = sender as ToolStripMenuItem;
            var mt = item.Tag as Type;
            BotModule.SetModuleEnable(mt, item.Checked);
            SaveBotModules();
        }
        private void group_Module_Click(object sender, EventArgs e)
        {

        }

        private void menu_BotItem_Opening(object sender, CancelEventArgs e)
        {
            var menu = sender as ContextMenuStrip;
            if (menu != null)
            {
                var mp = menu.PointToScreen(new Point(0, 0));
                var lp = list_Bots.PointToClient(mp);
                var item = list_Bots.GetItemAt(lp.X, lp.Y);
                if (item != null)
                {
                    item.Selected = true;
                    var selected = SelectedBotItem;
                    if (selected == null)
                    {
                        e.Cancel = true;
                    }
                }
                else
                {
                    e.Cancel = true;
                }
            }
        }
        private void btn_BotReconnect_Click(object sender, EventArgs e)
        {
            foreach (var bot in SelectedBotItems)
            {
                bot.Runner.Reconnect();
            }
        }
        private void btn_BotStart_Click(object sender, EventArgs e)
        {
            foreach (var bot in SelectedBotItems)
            {
                bot.Runner.Start();
            }
        }
        private void btn_BotStop_Click(object sender, EventArgs e)
        {
            foreach (var bot in SelectedBotItems)
            {
                bot.Runner.Stop();
            }
        }

        private void btn_EmuDisconnect_Click(object sender, EventArgs e)
        {
            foreach (var bot in SelectedBotItems)
            {
                bot.Runner.Disconnect();
            }
        }
        //-------------------------------------------------------------------------------------------------------------
        #region OP

        private void StartBots(string name_prefix = null, int count = 1, bool keep = false)
        {
            var add = AddBotConfig.TryLoadAddConfig();
            if (name_prefix != null)
            {
                add.name_format = name_prefix;
                add.count = count;
                add.index = lastIndex;
            }
            else
            {
                var servers = RPGClientTemplateManager.Instance.GetAllServers();
                var servers_optionals = new DeepEditor.Common.G2D.DataGrid.OptionalList();
                servers_optionals.AddRange(servers);
                servers_optionals.Converter = new Func<System.Reflection.MemberInfo, object, object>((field, value) =>
                {
                    var server = value as Data.ServerInfo;
                    return server.id;
                });
                add.index = lastIndex;
                if (keep == false)
                {
                    var dialog = new G2DPropertyDialog<AddBotConfig>(add);
                    {
                        dialog.Text = "1";
                        dialog.PropertyGrid.SelectedDescriptorObject.AppendOptionals(nameof(AddBotConfig.serverID), servers_optionals);
                        var res = dialog.ShowDialog();
                        if (res == System.Windows.Forms.DialogResult.OK)
                        {
                            add = dialog.SelectedObject;
                        }
                        else
                        {
                            return;
                        }
                    }
                }
                else
                {
                    add.count = count;
                }
                //  add = G2DPropertyDialog<AddBotConfig>.Show("1", add);

            }
            if (add != null)
            {
                try
                {
                    if (add.name_format.Contains("{0}") && !string.IsNullOrEmpty(add.digit_format))
                    {
                        var starting = new List<BotListViewItem>();
                        for (int i = 0; i < add.count; i++)
                        {
                            var id = add.index + i;
                            var name = string.Format(add.name_format, id.ToString(add.digit_format));
                            var item = new BotListViewItem(name, id, add, config);
                            list_Bots.Items.Add(item);
                            starting.Add(item);
                        }
                        Task.Run(async () =>
                        {
                            foreach (var e in starting)
                            {
                                if (config.AddBotIntervalMS > 0)
                                {
                                    await Task.Delay(config.AddBotIntervalMS);
                                }
                                e.Runner.Start();
                            }
                        }).ConfigureAwait(true);
                    }
                    else
                    {
                        var name = add.name_format;
                        var item = new BotListViewItem(name, lastIndex, add, config);
                        list_Bots.Items.Add(item);
                        item.Runner.Start();
                    }
                }
                catch (Exception err)
                {
                    MessageBox.Show(err.Message);
                }
                this.started = true;
                this.lastIndex = add.index + add.count;
                if (keep == false)
                {
                    AddBotConfig.TrySaveAddConfig(add);
                }
            }
        }

        private void InitBotModules()
        {
            try
            {
                var saved = XmlUtil.LoadXML(Application.StartupPath + "/bot_modules.save");
                if (saved != null)
                {
                    var cfg = XmlUtil.XmlToObject<BotModuleConfig>(saved);
                    foreach (var me in cfg.Modules)
                    {
                        var mt = ReflectionUtil.GetType(me.Key);
                        if (mt != null)
                        {
                            BotModule.SetModuleEnable(mt, me.Value);
                        }
                    }
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
            foreach (var mt in BotFactory.Instance.GetModuleTypes())
            {
                var enable = BotModule.GetModuleEnable(mt);
                var item = new ToolStripMenuItem();
                item.CheckOnClick = true;
                item.Size = new System.Drawing.Size(152, 22);
                item.Text = "模块:" + mt.Name;
                item.Tag = mt;
                item.Checked = enable;
                item.CheckedChanged += moduleItem_CheckedChanged;
                group_Module.DropDownItems.Add(item);
            }
        }
        private void SaveBotModules()
        {
            BotModuleConfig cfg = new BotModuleConfig();
            foreach (var mt in BotFactory.Instance.GetModuleTypes())
            {
                cfg.Modules.Add(mt.FullName, BotModule.GetModuleEnable(mt));
            }
            var save = XmlUtil.ObjectToXml(cfg);
            XmlUtil.SaveXML(Application.StartupPath + "/bot_modules.save", save);
        }
        #endregion

    }

    public class BotGamePanelContainer : GamePanelContainer
    {
        private readonly BotRunner bot;
        public BotGamePanelContainer(BotRunner bot)
        {
            this.bot = bot;
        }
        protected override void Client_OnErrorResponse(Protocol.Response rsp, Exception err = null)
        {
            var prefix = (rsp != null) ? (rsp.GetType().Name + " : " + rsp + Environment.NewLine) : "";
            var suffix = (err != null) ? (err.Message + Environment.NewLine + err.StackTrace) : "";
            bot.LogEvents.Error(prefix + suffix);
        }
        protected override void Client_OnError(Exception err)
        {
            bot.LogEvents.Error(err.Message + Environment.NewLine + err.StackTrace);
        }
    }
    public class BotListViewItem : ListViewItem
    {
        public RPGClient Client { get; private set; }
        public BotRunner Runner { get; private set; }
        public StringBuilder Events { get; private set; }
        public GamePanelContainer BattleView { get; private set; }


        public BotListViewItem(string name, int index, AddBotConfig add, BotConfig cfg)
            : base(name)
        {
            var bot = BotFactory.Instance.CreateBotRunner(cfg, add, name);
            this.Client = bot.Client;
            this.Runner = bot;
            this.Events = new StringBuilder();
            if (BotLauncher.NoBattleView == false)
            {
                this.Runner.Client.IsAutoUpdateBattle = false;
                this.BattleView = new BotGamePanelContainer(bot);
                this.BattleView.Init(this.Client);
                this.BattleView.Dock = DockStyle.Fill;
            }
            else
            {
                this.Runner.Client.IsAutoUpdateBattle = true;
            }
            this.Tag = bot;
            var colums = bot.Columns;
            for (int i = 1; i < colums.Length; i++)
            {
                this.SubItems.Add(colums[i]);
            }
        }
        public void Update(int intervalMS)
        {
            if (BattleView != null)
            {
                Runner.Update(intervalMS);
                BattleView.UpdateBattle(intervalMS);
            }
            else
            {
                Runner.Update(intervalMS);
            }
        }
        public void Refresh()
        {
            if (Events.Length > 10000)
            {
                Events.Clear();
            }
            using (var list = ListObjectPool<string>.AllocAutoRelease())
            {
                this.Runner.PopLogs(list);
                foreach (var e in list)
                {
                    Events.AppendLine(e);
                }
            }
            var colums = Runner.Columns;
            for (int i = 1; i < colums.Length; i++)
            {
                if (this.SubItems[i].Text != colums[i])
                    this.SubItems[i].Text = colums[i];
            }
            if (Client.GameClient.IsConnected == false)
            {
                this.ForeColor = Color.Gray;
            }
            else
            {
                this.ForeColor = Color.Black;
            }
        }


    }
}
