using System;
using System.Collections.Generic;

namespace DeepMMO.Unity3D.Entity
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ReferenceBaseTypeAttribute : Attribute
    {
        public readonly Type TargetType;

        public ReferenceBaseTypeAttribute(Type t)
        {
            TargetType = t;
        }
    }


    public sealed partial class EntityLayer
    {
        private static readonly Dictionary<Type, Type> sRefTypes = new Dictionary<Type, Type>(20);

        private static Type GetTargetType(Type type)
        {
            if (sRefTypes.TryGetValue(type, out var target))
            {
                return target;
            }

            var attrs = type.GetCustomAttributes(typeof(ReferenceBaseTypeAttribute), true);
            if (attrs.Length > 0)
            {
                Array.Sort(attrs, (o1, o2) =>
                {
                    var a1 = (ReferenceBaseTypeAttribute) o1;
                    var a2 = (ReferenceBaseTypeAttribute) o2;
                    if (a1.TargetType.IsSubclassOf(a2.TargetType))
                    {
                        return 1;
                    }

                    if (a1.TargetType == a2.TargetType)
                    {
                        return 0;
                    }

                    return -1;
                });
                target = ((ReferenceBaseTypeAttribute) attrs[0]).TargetType;
                if (!type.IsSubclassOf(target))
                {
                    throw new Exception(type + "is not subclass of " + target);
                }
            }
            else
            {
                target = type;
            }

            sRefTypes.Add(type, target);

            return target;
        }
    }
}