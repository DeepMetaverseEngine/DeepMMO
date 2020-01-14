using DeepCore;
using DeepCore.Reflection;
using DeepCore.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DeepMMO.Client.BotTest.Runner;

namespace DeepMMO.Client.BotTest
{
    public class AddBotConfig
    {
        //-------------------------------------------------------------------------------------
        [Desc("前缀", "Add")]
        public string name_format = "b{0}";
        [Desc("登录密码", "Add")]
        public string password = "123456";
        [Desc("计数器", "Add")]
        public int index = 0;
        [Desc("数量", "Add")]
        public int count = 10;
        [Desc("名字格式", "Add")]
        public string digit_format = "D6";
        [Desc("ServerID", "Add")]
        [OptionalValue()]
        public string serverID = "0";
        //-------------------------------------------------------------------------------------

        [Desc("随机角色名", "角色")]
        public bool RandomRoleName = false;
        [Desc("角色名格式", "角色")]
        [DependOnProperty(nameof(RandomRoleName), false)]
        public string[] RoleNameFormat = new string[] { "角色{0}" };

        //-------------------------------------------------------------------------------------
        [Desc("模块配置", "模块")]
        [Expandable]
        public List<object> ModuleConfigs = new List<object>();
        //-------------------------------------------------------------------------------------

        public static AddBotConfig TryLoadAddConfig()
        {
            var add = BotFactory.Instance.CreateAddBotConfig();
            try
            {
                var saved = XmlUtil.LoadXML(Application.StartupPath + "/bot_add.save");
                if (saved != null)
                {
                    add = XmlUtil.XmlToObject<AddBotConfig>(saved);
                }
                add.ModuleConfigs.Clear();
                var mts = BotFactory.Instance.GetModuleTypes();
                foreach (var mt in mts)
                {
                    var mt_config = mt.GetNestedType("Config");
                    if (mt_config != null)
                    {
                        add.ModuleConfigs.Add(Activator.CreateInstance(mt_config));
                    }
                }
                foreach (object mt_config in add.ModuleConfigs)
                {
                    var type = mt_config.GetType();
                    LoadModule(type.DeclaringType.Name, type);
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
            return add;
        }
        public static void TrySaveAddConfig(AddBotConfig add)
        {
            if (BotLauncher.IsAuto == false)
            {
                var save = XmlUtil.ObjectToXml(add);
                XmlUtil.SaveXML(Application.StartupPath + "/bot_add.save", save);
                foreach (object mt_config in add.ModuleConfigs)
                {
                    var type = mt_config.GetType();
                    SaveModule(type.DeclaringType.Name, type);
                }
            }
        }
        private static void SaveModule(string name, Type type)
        {
            var prop = new DeepCore.Properties();
            prop.SaveStaticFields(type);
            File.WriteAllText(Application.StartupPath + "/bot.module." + name + ".save", prop.ToString(), CUtils.UTF8);
        }
        private static void LoadModule(string name, Type type)
        {
            if (File.Exists(Application.StartupPath + "/bot.module." + name + ".save"))
            {
                var text = File.ReadAllText(Application.StartupPath + "/bot.module." + name + ".save", CUtils.UTF8);
                if (text != null)
                {
                    var prop = new DeepCore.Properties();
                    prop.TryParseText(text);
                    prop.LoadStaticFields(type);
                }
            }
        }

    }
    //-------------------------------------------------------------------------------------

    public class BotModulesConfig
    {
        [Desc("Modules")]
        [Expandable()]
        public HashMap<string, bool> Modules = new HashMap<string, bool>();
    }

}
