using CommonAI.Zone;
using CommonAI.Zone.ZoneEditor;
using CommonAIServer.Node;
using CommonLang;
using CommonLang.Log;
using CommonLang.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonRPG.Server.Battle
{
    public static class BattleUtils
    {
        public static ZoneNodeFactory ZoneServerFactory { get; private set; }
        public static InstanceZoneFactory ZoneFactory { get; private set; }
        public static ZoneNodeConfig NodeConfig { get; private set; }
        public static EditorTemplates DataRoot { get; private set; }

        public static void Init(Logger log, Properties config)
        {
            if (ZoneFactory == null)
            {
                log.Info("********************************************************");
                log.Info("# 初始化战斗编辑器扩展 ");
                log.Info("********************************************************");
                ZoneFactory = ReflectionUtil.CreateInterface<InstanceZoneFactory>(config["ZoneFactory"]);
                TemplateManager.SetFactory(ZoneFactory);
                ZoneServerFactory = ReflectionUtil.CreateInterface<ZoneNodeFactory>(config["ZoneServerFactory"]);
                log.Info(" 战斗编辑器插件 : " + ZoneFactory);
                log.Info(" 战斗服务器插件 : " + ZoneServerFactory);
            }
            if (NodeConfig == null)
            {
                log.Info("********************************************************");
                log.Info("# 加载配置文件");
                log.Info("********************************************************");
                NodeConfig = ZoneServerFactory.GetConfig();
                var node_cfg = config.SubProperties("ZoneNodeConfig.");
                if (node_cfg != null)
                {
                    node_cfg.LoadFields(NodeConfig);
                }
            }
            if (DataRoot == null)
            {
                log.Info("********************************************************");
                log.Info("# 加载模板数据");
                log.Info("********************************************************");
                try
                {
                    DataRoot = TemplateManager.Factory.CreateEditorTemplates(config["DataRootPath"]);
                    DataRoot.LoadAllTemplates();
                    DataRoot.CacheAllScenes();
                }
                catch (Exception err)
                {
                    throw new Exception("EditorTemplates init error : " + err.Message + "\n" + err.StackTrace, err);
                }
                log.Info("********************************************************");
            }

        }

    }
}
