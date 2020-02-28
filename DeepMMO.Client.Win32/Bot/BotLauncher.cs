using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DeepCore.Reflection;
using DeepMMO.Client.BotTest.Runner;

namespace DeepMMO.Client.BotTest
{
    
    public static class BotLauncher
    {
        public static string DefaultBotPrefix { get; private set; }
        public static int DefaultBotCount { get; private set; }
        public static bool IsAuto { get; private set; }

        private static bool first_start = true;

        public static FormLauncher Start(string[] args)
        {
            var argp = DeepCore.Properties.ParseArgs(args);
            return Start(argp);
        }
        public static FormLauncher Start(DeepCore.Properties argp)
        {
            string b_name = argp.Get("name");
            string b_count = argp.Get("count");
            if (b_name != null && b_count != null)
            {
                BotLauncher.IsAuto = true;
                BotLauncher.DefaultBotPrefix = b_name;
                BotLauncher.DefaultBotCount = int.Parse(b_count);
            }
            else
            {
                BotLauncher.IsAuto = false;
            }
            var launcher = new FormLauncher();
            launcher.Shown += Launcher_Shown;
            launcher.OnStart += Launcher_OnStart;
            return launcher;
        }

        private static void Launcher_Shown(object sender, EventArgs e)
        {
            if (IsAuto && first_start)
            {
                first_start = false;
                (sender as FormLauncher).Start();
            }
        }
        private static void Launcher_OnStart(FormLauncher sender, BotConfig config)
        {
            var bot = new FormBotTest(config);
            bot.FormClosed += new FormClosedEventHandler((object sender2, FormClosedEventArgs e2) =>
            {
                sender.Show();
            });
            bot.Show();
        }

        public static string ArgsHelper
        {
            get
            {
                return @"name=xxx count=100";
            }
        }
    }
}