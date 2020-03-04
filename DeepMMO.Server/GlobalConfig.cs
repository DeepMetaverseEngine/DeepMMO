
using DeepCore;
using DeepCore.Log;
using DeepCore.Reflection;
using DeepCrystal.RPC;
using System;
using System.Reflection;

namespace DeepMMO.Server
{
    public static class GlobalConfig
    {
        //         public const string RPGServerBattleManagerKey = "RPGServerBattleManager";
        //         public const string RPGServerManagerKey = "RPGServerManagerKey";
        //         public const string RPGServerTemplateManagerKey = "RPGServerTemplateManager";
        //         public const string ZoneDataFactoryKey = "ZoneDataFactory";
        //         public const string InstanceZoneFactoryKey = "InstanceZoneFactory";
        //         public const string ZoneNodeFactoryKey = "ZoneNodeFactory";
        //         public const string DataRootKey = "DataRoot";

        public static bool EnableServerTest
        {
            get { return IService.GlobalConfig.GetAs<bool>(nameof(EnableServerTest)); }
        }
        public static string RealmID
        {
            get { return IService.GlobalConfig.Get(nameof(RealmID)); }
        }
        public static string RPGServerBattleManager
        {
            get { return IService.GlobalConfig.Get(nameof(RPGServerBattleManager)); }
        }
        public static string RPGServerPersistenceManager
        {
            get { return IService.GlobalConfig.Get(nameof(RPGServerPersistenceManager)); }
        }
        public static string RPGServerManager
        {
            get { return IService.GlobalConfig.Get(nameof(RPGServerManager)); }
        }
        public static string RPGServerTemplateManager
        {
            get { return IService.GlobalConfig.Get(nameof(RPGServerTemplateManager)); }
        }

        public static string ZoneDataFactory
        {
            get { return IService.GlobalConfig.Get(nameof(ZoneDataFactory)); }
        }
        public static string InstanceZoneFactory
        {
            get { return IService.GlobalConfig.Get(nameof(InstanceZoneFactory)); }
        }
        public static string ZoneServerFactory
        {
            get { return IService.GlobalConfig.Get(nameof(ZoneServerFactory)); }
        }
        public static Properties ZoneNodeConfig
        {
            get { return IService.GlobalConfig.SubProperties(nameof(ZoneNodeConfig) + "."); }
        }

        public static string GMTUrl
        {
            get { return IService.GlobalConfig.Get(nameof(GMTUrl)); }
        }

        public static string ServerListUrl
        {
            get { return IService.GlobalConfig.Get(nameof(ServerListUrl)); }
        }
        public static string BattleDataRoot
        {
            get { return IService.GlobalConfig.Get(nameof(BattleDataRoot)); }
        }

        public static string GameEditorRoot
        {
            get { return IService.GlobalConfig.Get(nameof(GameEditorRoot)); }
        }
        public static string ServerDataRoot
        {
            get { return IService.GlobalConfig.Get(nameof(ServerDataRoot)); }
        }
        public static string ReplaceNetHost
        {
            get { return IService.GlobalConfig.Get(nameof(ReplaceNetHost)); }
        }

        public static string TemplateDataRoot
        {
            get { return ServerDataRoot + "/templates_lua/"; }
        }
        public static string EventScriptRoot
        {
            get { return ServerDataRoot + "/event_script/"; }
        }


        internal static void LoadAll()
        {
            foreach (var cfgType in ReflectionUtil.GetAllTypes())
            {
                if (cfgType.TryGetAttribute<LoadFromGlobalConfigAttribute>(out var attr))
                {
                    LoadStaticFieldsFromGlobal(cfgType);
                }
            }
        }
        public static void LoadStaticFieldsFromGlobal(Type type)
        {
            var log = new LazyLogger(type.FullName);
            var subcfg = IService.GlobalConfig.SubProperties(type.FullName + ".");
            if (subcfg != null)
            {
                subcfg.LoadStaticFields(type, (f) =>
                {
                    log.Error($"'{f.Name}' Not Exist In GlobalConfig");
                });
            }
        }
    }


    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class LoadFromGlobalConfigAttribute : Attribute
    {

    }
}
