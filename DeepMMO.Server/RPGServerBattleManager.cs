using DeepCore.GameData;
using DeepCore.GameData.Zone;
using DeepCore.GameData.Zone.ZoneEditor;
using DeepCore.IO;
using DeepCore.Log;
using DeepCore.Reflection;
using DeepCrystal.RPC;
using DeepMMO.Server.AreaManager;
using System;
using DeepCore.Game3D.Host;
using DeepCore.Game3D.Host.ZoneServer;
using DeepCore.Game3D.Voxel;
using System.Threading.Tasks.Dataflow;
using System.Collections.Concurrent;
using System.Collections.Generic;
using DeepCore;

namespace DeepMMO.Server
{
    public abstract class RPGServerBattleManager
    {
        //--------------------------------------------------------------------------------------------------------------------------
        #region Singleton
        private static readonly object lock_init = new object();
        private static bool init_done = false;
        private static RPGServerBattleManager instance;
        public static bool CACHE_ALL_VOXEL = true;
        public static bool IsInitDone { get { return init_done; } }
        public static RPGServerBattleManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (lock_init)
                    {
                        if (!init_done)
                        {
                            var config = IService.GlobalConfig;
                            instance = ReflectionUtil.CreateInterface<RPGServerBattleManager>(GlobalConfig.RPGServerBattleManager);
                            instance.Init();
                            init_done = true;
                        }
                    }
                }
                return instance;
            }
        }
        #endregion
        //--------------------------------------------------------------------------------------------------------------------------
        public static ZoneDataFactory DataFactory { get; private set; }
        public static ZoneHostFactory ZoneFactory { get; private set; }
        public static ZoneNodeConfig NodeConfig { get; private set; }
        public static EditorTemplates DataRoot { get; private set; }
        public static TemplateManager Templates { get; private set; }
        /// <summary>
        /// 跨场景寻路网格
        /// </summary>
        public static MapSceneGrapAstar SceneGrapAstar { get; private set; }

        //--------------------------------------------------------------------------------------------------------------------------
        protected readonly Logger log;
        public RPGServerBattleManager()
        {
            instance = this;
            this.log = LoggerFactory.GetLogger(GetType().Name);
        }
        public virtual void Init()
        {
            try
            {

                EditorTemplates.RUNTIME_IN_SERVER = true;
                RPGServerTemplateManager.Instance.ToString();
                if (ZoneFactory == null)
                {
                    log.Info("********************************************************");
                    log.Info("# 初始化战斗编辑器扩展 ");
                    log.Info("********************************************************");
                    DataFactory = ReflectionUtil.CreateInterface<ZoneDataFactory>(GlobalConfig.ZoneDataFactory);
                    ZoneFactory = ReflectionUtil.CreateInterface<ZoneHostFactory>(GlobalConfig.InstanceZoneFactory, GlobalConfig.GameEditorRoot);
                    log.Info(" 战斗编辑器插件 : " + ZoneFactory);
                }
                if (NodeConfig == null)
                {
                    log.Info("********************************************************");
                    log.Info("# 加载配置文件");
                    log.Info("********************************************************");
                    NodeConfig = ZoneFactory.GetServerConfig();
                    var node_cfg = GlobalConfig.ZoneNodeConfig;
                    if (node_cfg != null)
                    {
                        log.Info(node_cfg.ToString());
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
                        DataRoot = DataFactory.CreateEditorTemplates(GlobalConfig.BattleDataRoot);
                        DataRoot.Verbose = true;
                        DataRoot.LoadAllTemplates();
                        DataRoot.CacheAllScenes();
                        Templates = DataRoot.Templates;
                    }
                    catch (Exception err)
                    {
                        throw new Exception("EditorTemplates init error : " + err.Message + "\n" + err.StackTrace, err);
                    }

                    if (CACHE_ALL_VOXEL)
                    {
                        log.Info("********************************************************");
                        log.Info("# 缓存体素");
                        log.Info("********************************************************");
                        var list = new HashMap<string, SceneData>();
                        var caches = new ConcurrentDictionary<string, VoxelWorld>();
                        var cacheTasks = new ActionBlock<KeyValuePair<string, SceneData>>(run_CacheScene, new ExecutionDataflowBlockOptions()
                        {
                            MaxDegreeOfParallelism = Environment.ProcessorCount - 1
                        });
                        foreach (var sd in DataRoot.CacheAllScenes())
                        {
                            if (string.IsNullOrEmpty(sd.VoxelFileName) is false)
                            {
                                list.TryAdd(GlobalConfig.GameEditorRoot + sd.VoxelFileName, sd);
                            }
                        }
                        foreach (var path in list)
                        {
                            cacheTasks.Post(path);
                        }
                        void run_CacheScene(KeyValuePair<string, SceneData> sd)
                        {
                            try
                            {
                                log.Info($"Cache voxel data : {sd.Key}");
                                var wx = VoxelWorld.LoadFromFile(sd.Key);
                                caches.TryAdd(sd.Key, wx);
                                log.Info($"Cache voxel data : {sd.Key} : OK ({caches.Count}/{list.Count})");
                            }
                            catch (Exception err)
                            {
                                log.Error($"Load Voxel Error : >>>{sd.Value}<<< {sd.Key}");
                                log.Error(err);
                            }
                        }
                        cacheTasks.Complete();
                        cacheTasks.Completion.Wait();
                        VoxelWorldManager.Instance.CacheAll(caches);
                    }

                    log.Info("********************************************************");
                    log.Info("# 从地图配置表重新构建场景传送点");
                    log.Info("********************************************************");
                    FillSceneTransport();
                    SceneGrapAstar = new MapSceneGrapAstar(RPGServerTemplateManager.Instance.AllMapTemplates);

                }
                {
                    log.Info("********************************************************");
                    log.Info("# 战斗编解码器");
                    log.Info("********************************************************");
                    if (DataFactory.MessageCodec is MessageFactoryGenerator)
                    {
                        log.Info(DataFactory.MessageCodec);
                    }
                }
                log.Info("********************************************************");
            }
            catch
            {
                log.Error("检查战斗编辑器是否重新保存");

                throw;
            }
        }

        public virtual SceneData GetSceneAsCache(int mapID)
        {
            if (mapID != 0)
            {
                return DataRoot.LoadScene(mapID, true, false, false);
            }
            return null;
        }
        /// <summary>
        /// 从地图配置表，重新构建场景传送点
        /// </summary>
        protected virtual void FillSceneTransport()
        {
            foreach (var map in RPGServerTemplateManager.Instance.AllMapTemplates)
            {
                var sd = GetSceneAsCache(map.zone_template_id);
                if (sd != null)
                {
                    sd.Terrain.ZoneData.ToString();
                    FillSceneTransport(sd, map);
                }
                else
                {
                    log.ErrorFormat("Map Zone Template Not Exist : map={0} zone={1}", map, map.zone_template_id);
                }
            }
        }
        /// <summary>
        /// 从地图配置表，重新构建场景传送点
        /// </summary>
        /// <param name="from_sd"></param>
        /// <param name="from_map"></param>
        protected virtual void FillSceneTransport(SceneData from_sd, MapTemplateData from_map)
        {
            if (from_map.connect == null)
            {
                log.ErrorFormat("from_map connect = null : FromMap={0}", from_map.name);
                return;
            }

            foreach (var link in from_map.connect)
            {
                var from_flag = from_sd.Regions.Find(region => { return region.Name == link.from_flag_name; });
                if (from_flag == null)
                {
                    log.ErrorFormat("Transport From Flag Not Found : FromMap={0} FromScene={1} >>>FromFlag={2}<<<", from_map, from_sd, link.from_flag_name);
                    continue;
                }
                var to_map = RPGServerTemplateManager.Instance.GetMapTemplate(link.to_map_id);
                if (to_map == null)
                {
                    log.ErrorFormat("Transport Target Map Not Found : FromMap={0} >>>ToMap={1}<<<", from_map, link.to_map_id);
                    continue;
                }
                var to_zone = GetSceneAsCache(to_map.zone_template_id);
                if (to_zone == null)
                {
                    log.ErrorFormat("Transport Target Scene Not Found : FromMap={0} ToMap={1} >>>ToZone={2}<<<", from_map, to_map, to_map.zone_template_id);
                    continue;
                }
                var to_flag = to_zone.Regions.Find(region => { return region.Name == link.to_flag_name; });
                if (to_flag == null)
                {
                    log.ErrorFormat("Transport Target Flag Not Found : FromMap={0} ToMap={1} ToScene={2} >>>ToFlag={2}<<<", from_map, to_map, to_zone, link.to_flag_name);
                    continue;
                }
                var tp = from_flag.GetAbilityOf<SceneTransportAbilityData>();
                if (tp != null)
                {
                    log.ErrorFormat("Transport Already Exist : FromMap={0} FromZone={1} FromFlag={2} >>>Ability={3}<<<", from_map, from_sd, from_flag.Name, tp);
                }
                else
                {
                    tp = new SceneTransportAbilityData()
                    {
                        AcceptForceForAll = true,
                        Name = "fill with " + from_map,
                        NextSceneID = to_map.id,
                        NextScenePosition = link.to_flag_name,
                    };
                    from_flag.Abilities.Add(tp);
                    log.InfoFormat("Fill Scene Transport : FromMap={0} FromZone={1} FromFlag={2} ToMap={3} Ability={4}", from_map, from_sd, from_flag.Name, to_map, tp);
                }
            }
        }

        //--------------------------------------------------------------------------------------------------------------------------

    }
}
