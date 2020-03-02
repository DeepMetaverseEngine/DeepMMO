
using DeepCore.Reflection;
using DeepCrystal.RPC;

namespace DeepMMO.Server
{
    public abstract class RPGServerManager
    {
        //--------------------------------------------------------------------------------------
        #region Singleton 
        private static readonly object lock_init = new object();
        private static bool init_done = false;
        private static RPGServerManager instance;
        public static bool IsInitDone { get { return init_done; } }
        public static RPGServerManager Instance
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
                            instance = ReflectionUtil.CreateInterface<RPGServerManager>(GlobalConfig.RPGServerManager);
                            instance.Init();
                            init_done = true;
                        }
                    }
                }
                return instance;
            }
        }
        #endregion
        //--------------------------------------------------------------------------------------
        public virtual AccessPolicy Access { get; protected set; }
        public virtual ServerPassport Passport { get; protected set; }
        //--------------------------------------------------------------------------------------
        public RPGServerManager()
        {
            instance = this;
        }
        public virtual void Init()
        {
            var subcfg = IService.GlobalConfig.SubProperties(typeof(TimerConfig).FullName + ".");
            if (subcfg != null)
            {
                subcfg.LoadStaticFields(typeof(TimerConfig));
            }
            this.Access = CreateAccessPolicy();
            this.Passport = CreatePassport();
        }
        public virtual AccessPolicy CreateAccessPolicy()
        {
            return new AccessPolicy();
        }
        public virtual ServerPassport CreatePassport()
        {
            return new ServerPassport();
        }

        //         public virtual Persistence.AccountPersistenceOperator CreateAccountPersistenceOperator()
        //         {
        //             return new Persistence.AccountPersistenceOperator();
        //         }
        //         public virtual Persistence.RolePersistenceOperator CreateRolePersistenceOperator(string roleID)
        //         {
        //             return new Persistence.RolePersistenceOperator(roleID);
        //         }

    }


}
