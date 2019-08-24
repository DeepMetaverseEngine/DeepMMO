using DeepCrystal;
using DeepCrystal.Persistence;
using DeepMMO.Data;
using DeepMMO.Server.Logic;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace DeepMMO.Server.Persistence
{
    public class RolePersistenceOperator : IPersistenceOperator
    {
        public string roleID { get; private set; }
        public ServerRoleData roleData { get; private set; }
        private IPersistenceGetSet<ServerRoleData> roleSave { set; get; }
        public static DynamicMethodMap methodMap;

        static RolePersistenceOperator()
        {
            methodMap = DynamicMethodMap.GetMethodMap(typeof(ServerRoleData));
        }

        internal protected RolePersistenceOperator(string uuid)
        {
            this.roleID = uuid;
        }

        public virtual void Create(ServerRoleData roleData)
        {
            roleSave = PersistenceFactory.Instance.Get<ServerRoleData>(this, roleID);
            this.roleData = roleData;
        }


        public virtual void Load()
        {
            roleSave = PersistenceFactory.Instance.Get<ServerRoleData>(this, roleID);
            {
                roleData = roleSave.Get();
                if (roleData == null)
                {
                    throw new Exception("Cant Load Role Data : " + roleID);
                }
                roleData.last_login_time = DateTime.Now;
                roleSave.UpdateValue<DateTime>(roleData.last_login_time, WhenCode.UpdateAlways, nameof(ServerRoleData.last_login_time));
            }

            using (var saveSnap = PersistenceFactory.Instance.Get<RoleSnap>(this, roleID))
            {
                saveSnap.UpdateValue<DateTime>(roleData.last_login_time, WhenCode.UpdateAlways, nameof(RoleSnap.last_login_time));
            }
        }
        public virtual void Flush()
        {
            // Flush to db //
            roleSave.Lock();
            try
            {
                roleSave.Update(roleData);
                if (roleData == null)
                {
                    throw new Exception("Cant Load Role Data : " + roleID);
                }
                roleSave.Flush();
            }
            finally
            {
                roleSave.Unlock();
            }

        }
        public void Dispose()
        {
            roleSave.Dispose();
            roleData = null;
        }

        /// <summary>
        /// 更改属性
        /// 此方法用于对相应属性进行持久化操作
        /// </summary>
        /// <param name="key"></param>
        public void UpdateAttribute(string key)
        {
            object value = methodMap.InvokeGet(this.roleData, key);
            roleSave.UpdateValue<object>(value, WhenCode.UpdateAlways, key);

            //等ORM重构.
            if (this.IsContainPropertyOfRoleSnap(key))
            {
                this.SaveRoleSnap();
            }
        }
        /// <summary>
        /// 更改属性
        /// 此方法用于对内存数据更改以及持久化操作
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void UpdateAttribute<T>(string key, T value)
        {
            roleSave = PersistenceFactory.Instance.Get<ServerRoleData>(this, roleID);

            methodMap.InvokeSet(this.roleData, key, value);

            //roleSave.UpdateValue<T>(value, WhenCode.UpdateAlways, key);
            roleSave.UpdateValueAsync(value, WhenCode.UpdateAlways, key);
            if (this.IsContainPropertyOfRoleSnap(key))
            {
                this.SaveRoleSnap();
            }

        }
        /// <summary>
        /// 更改属性
        /// 此方法用于更改属性并不进行持久化操作
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void UpdateAttributeNotPersist<T>(string key, T value)
        {
            methodMap.InvokeSet(this.roleData, key, value);
            // DynamicSetField method = methodMap.InvokeSet(this.roleData, key, value);
            // method(this.roleData, value);
        }
        public void UpdateAttributes(List<string> keys)
        {
            // TODO 此方法待优化
            foreach (string key in keys)
            {
                this.UpdateAttribute(key);
            }
        }

        private void SaveRoleSnap()
        {
            using (var saveSnap = PersistenceFactory.Instance.Get<RoleSnap>(null, this.roleData.uuid))
            {
                RoleSnap snap = this.roleData.ToSnap();
                saveSnap.Update(snap);
            }
        }
        protected virtual bool IsContainPropertyOfRoleSnap(string key)
        {
            FieldInfo info = typeof(RoleSnap).GetField(key);
            return info != null;
        }

        public void SaveRoleData()
        {
            roleSave.Update(roleData);
        }

    }
}
