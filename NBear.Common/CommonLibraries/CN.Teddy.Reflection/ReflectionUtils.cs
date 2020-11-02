using System;
using System.Collections.Generic;
using System.Reflection;

namespace CN.Teddy.Reflection
{
    public sealed class ReflectionUtils
    {
        private ReflectionUtils() { }

        /// <summary>
        /// Deeply gets property infos.
        /// </summary>
        /// <param name="types">The types.</param>
        /// <returns>Property infos of all the types and there base classes/interfaces</returns>
        public static PropertyInfo[] DeepGetProperties(params Type[] types)
        {
            if (types == null || types.Length == 0)
            {
                return new PropertyInfo[0];
            }
            List<PropertyInfo> list = new List<PropertyInfo>();
            foreach (Type t in types)
            {
                if (t != null)
                {
                    foreach (PropertyInfo pi in t.GetProperties())
                    {
                        list.Add(pi);
                    }

                    if (t.IsInterface)
                    {
                        Type[] interfaceTypes = t.GetInterfaces();

                        if (interfaceTypes != null)
                        {
                            foreach (PropertyInfo pi in DeepGetProperties(interfaceTypes))
                            {
                                bool isContained = false;

                                foreach (PropertyInfo item in list)
                                {
                                    if (item.Name == pi.Name)
                                    {
                                        isContained = true;
                                        break;
                                    }
                                }

                                if (!isContained)
                                {
                                    list.Add(pi);
                                }
                            }
                        }
                    }
                    else
                    {
                        Type baseType = t.BaseType;

                        if (baseType != typeof(object) && baseType != typeof(ValueType))
                        {
                            foreach (PropertyInfo pi in DeepGetProperties(baseType))
                            {
                                bool isContained = false;

                                foreach (PropertyInfo item in list)
                                {
                                    if (item.Name == pi.Name)
                                    {
                                        isContained = true;
                                        break;
                                    }
                                }

                                if (!isContained)
                                {
                                    list.Add(pi);
                                }
                            }
                        }
                    }
                }
            }

            return list.ToArray();
        }

        /// <summary>
        /// Deeply get property info from specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns></returns>
        public static PropertyInfo DeepGetProperty(Type type, string propertyName)
        {
            foreach (PropertyInfo pi in DeepGetProperties(type))
            {
                if (pi.Name == propertyName)
                {
                    return pi;
                }
            }

            return null;
        }

        /// <summary>
        /// Deeps the get field from specific type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="name">The name.</param>
        /// <param name="isPublic">if is public.</param>
        /// <returns>The field info</returns>
        public static FieldInfo DeepGetField(Type type, string name, bool isPublic)
        {
            Type t = type;
            if (t != null)
            {
                FieldInfo fi = (isPublic ? t.GetField(name) : t.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic));
                if (fi != null)
                {
                    return fi;
                }

                if (t.IsInterface)
                {
                    Type[] interfaceTypes = t.GetInterfaces();

                    if (interfaceTypes != null)
                    {
                        foreach (Type interfaceType in interfaceTypes)
                        {
                            fi = DeepGetField(interfaceType, name, isPublic);
                            if (fi != null)
                            {
                                return fi;
                            }
                        }
                    }
                }
                else
                {
                    Type baseType = t.BaseType;

                    if (baseType != typeof(object) && baseType != typeof(ValueType))
                    {
                        return DeepGetField(baseType, name, isPublic);
                    }
                }
            }
            return null;
        }

        public static MethodInfo GetMethodInfoFromArrayBySignature(string signature, MethodInfo[] mis)
        {
            if (mis == null)
            {
                return null;
            }

            foreach (MethodInfo mi in mis)
            {
                if (mi.ToString() == signature)
                {
                    return mi;
                }
            }

            return null;
        }

        public static MethodInfo GetMethodInfoBySignature(Type type, string signature, bool isPublic, bool isStatic)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            BindingFlags flags = BindingFlags.Instance | (isPublic ? BindingFlags.Public : BindingFlags.NonPublic);
            if (isStatic)
            {
                flags = flags | BindingFlags.Static;
            }
            return GetMethodInfoFromArrayBySignature(signature, type.GetMethods(flags));
        }
    }
}
