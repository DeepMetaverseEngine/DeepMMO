using DeepCore.GameData;
using DeepCore.GameData.Zone;
using DeepCore.GameData.Zone.ZoneEditor;
using DeepCore.Log;


namespace DeepMMO.Client
{
    public class RPGClientBattleManager
    {
        protected Logger log = LoggerFactory.GetLogger("TLBattleManager");
        protected EditorTemplates data_root;
        protected BattleCodec battle_codec;

        public static RPGClientBattleManager Instance { get; private set; }
        public static EditorTemplates DataRoot { get { return Instance.data_root; } }
        public static TemplateManager Templates { get { return Instance.data_root.Templates; } }
        public static BattleCodec BattleCodec { get { return Instance.battle_codec; } }

        public RPGClientBattleManager()
        {
            Instance = this;
            log = LoggerFactory.GetLogger(GetType().Name);
        }

        /// <summary>
        /// 初始化所有模板以及工厂类，游戏启动时调用
        /// </summary>
        public void Init(string data_path)
        {
            this.data_root = ZoneDataFactory.Factory.CreateEditorTemplates(data_path, true);
            this.data_root.LoadAllTemplates();
            this.battle_codec = new BattleCodec(data_root.Templates);
        }
        
    }

}
