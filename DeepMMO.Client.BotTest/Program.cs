using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DeepCore.Reflection;
using DeepMMO.Client.BotTest.Runner;

namespace DeepMMO.Client.BotTest
{

    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main(params string[] args)
        {
            ReflectionUtil.LoadDlls(new System.IO.DirectoryInfo(Application.StartupPath));
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(BotLauncher.Start(args));
        }
        
    }
}
