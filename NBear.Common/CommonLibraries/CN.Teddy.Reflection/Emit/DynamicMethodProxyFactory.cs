using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace CN.Teddy.Reflection.Emit
{
    /// <summary>
    /// Delegate for calling static method
    /// </summary>
    /// <param name="paramObjs">The parameters passing to the invoking method.</param>
    /// <returns>The return value.</returns>
    public delegate object StaticDynamicMethodProxyHandler(params object[] paramObjs);

    /// <summary>
    /// Delegate for calling non-static method
    /// </summary>
    /// <param name="ownerInstance">The object instance owns the invoking method.</param>
    /// <param name="paramObjs">The parameters passing to the invoking method.</param>
    /// <returns>The return value.</returns>
    public delegate object DynamicMethodProxyHandler(object ownerInstance, params object[] paramObjs);

    public class DynamicMethodProxyFactory
    {
        #region Helper Methods

        protected static MethodInfo MakeMethodGeneric(MethodInfo genericMethodInfo, Type[] genericParameterTypes)
        {
            if (genericMethodInfo == null)
            {
                throw new ArgumentNullException("genericMethodInfo");
            }

            MethodInfo makeGenericMethodInfo;
            if (genericParameterTypes != null && genericParameterTypes.Length > 0)
            {
                makeGenericMethodInfo = genericMethodInfo.MakeGenericMethod(genericParameterTypes);
            }
            else
            {
                makeGenericMethodInfo = genericMethodInfo;
            }
            return makeGenericMethodInfo;
        }

        protected static void LoadParameters(ILGenerator il, ParameterInfo[] pis, bool isMethodStatic)
        {
            if (il == null)
            {
                throw new ArgumentNullException("il");
            }

            if (pis == null)
            {
                throw new ArgumentNullException("pis");
            }

            for (int i = 0; i < pis.Length; ++i)
            {
                if (isMethodStatic)
                {
                    il.Emit(OpCodes.Ldarg_0);
                }
                else
                {
                    il.Emit(OpCodes.Ldarg_1);
                }
                EmitUtils.LoadInt32(il, i);
                il.Emit(OpCodes.Ldelem_Ref);
                if (pis[i].ParameterType.IsValueType)
                {
                    il.Emit(OpCodes.Unbox_Any, pis[i].ParameterType);
                }
                else if (pis[i].ParameterType != typeof(object))
                {
                    il.Emit(OpCodes.Castclass, pis[i].ParameterType);
                }
                //StoreLocal(il, i);
            }
        }

        #endregion

        #region Get static method delegate

        protected static StaticDynamicMethodProxyHandler DoGetStaticMethodDelegate(
            Module targetModule, 
            MethodInfo genericMethodInfo, 
            params Type[] genericParameterTypes)
        {
            #region Validate parameters

            if (targetModule == null)
            {
                throw new ArgumentNullException("targetModule");
            }

            if (genericMethodInfo == null)
            {
                throw new ArgumentNullException("genericMethodInfo");
            }

            if (genericParameterTypes != null)
            {
                if (genericParameterTypes.Length != genericMethodInfo.GetGenericArguments().Length)
                {
                    throw new ArgumentException("The number of generic type parameter of genericMethodInfo and the input types must equal!");
                }
            }
            else
            {
                if (genericMethodInfo.GetGenericArguments().Length > 0)
                {
                    throw new ArgumentException("Must specify types of type parameters for genericMethodInfo!");
                }
            }

            if (!genericMethodInfo.IsStatic)
            {
                throw new ArgumentException("genericMethodInfo must be static here!");
            }

            #endregion

            //Create a dynamic method proxy delegate used to call the specified methodinfo
            DynamicMethod dm = new DynamicMethod(
                Guid.NewGuid().ToString("N"), 
                typeof(object), 
                new Type[] { typeof(object[]) }, 
                targetModule);

            ILGenerator il = dm.GetILGenerator();

            MethodInfo makeGenericMethodInfo = MakeMethodGeneric(genericMethodInfo, genericParameterTypes);
            ParameterInfo[] pis = makeGenericMethodInfo.GetParameters();
            LoadParameters(il, pis, true);
            il.Emit(OpCodes.Call, makeGenericMethodInfo);
            if (makeGenericMethodInfo.ReturnType == typeof(void))
            {
                il.Emit(OpCodes.Ldnull);
            }

            il.Emit(OpCodes.Ret);

            return (StaticDynamicMethodProxyHandler)dm.CreateDelegate(typeof(StaticDynamicMethodProxyHandler));
        }

        public virtual StaticDynamicMethodProxyHandler GetStaticMethodDelegate(
            MethodInfo genericMethodInfo, 
            params Type[] genericParameterTypes)
        {
            return DoGetStaticMethodDelegate(typeof(string).Module, genericMethodInfo, genericParameterTypes);
        }

        public virtual StaticDynamicMethodProxyHandler GetStaticMethodDelegate(
            Module targetModule, 
            MethodInfo genericMethodInfo, 
            params Type[] genericParameterTypes)
        {
            return DoGetStaticMethodDelegate(targetModule, genericMethodInfo, genericParameterTypes);
        }

        #endregion

        #region Get non-static method delegate

        protected static DynamicMethodProxyHandler DoGetMethodDelegate(
            Module targetModule, 
            MethodInfo genericMethodInfo, 
            params Type[] genericParameterTypes)
        {
            #region Validate parameters

            if (targetModule == null)
            {
                throw new ArgumentNullException("targetModule");
            }

            if (genericMethodInfo == null)
            {
                throw new ArgumentNullException("genericMethodInfo");
            }

            if (genericParameterTypes != null)
            {
                if (genericParameterTypes.Length != genericMethodInfo.GetGenericArguments().Length)
                {
                    throw new ArgumentException("The number of generic type parameter of genericMethodInfo and the input types must equal!");
                }
            }
            else
            {
                if (genericMethodInfo.GetGenericArguments().Length > 0)
                {
                    throw new ArgumentException("Must specify types of type parameters for genericMethodInfo!");
                }
            }

            if (genericMethodInfo.IsStatic)
            {
                throw new ArgumentException("genericMethodInfo must not be static here!");
            }

            #endregion

            //Create a dynamic method proxy delegate used to call the specified methodinfo
            DynamicMethod dm = new DynamicMethod(
                Guid.NewGuid().ToString("N"), 
                typeof(object), 
                new Type[] { typeof(object), typeof(object[]) }, 
                targetModule);

            ILGenerator il = dm.GetILGenerator();

            MethodInfo makeGenericMethodInfo = MakeMethodGeneric(genericMethodInfo, genericParameterTypes);
            ParameterInfo[] pis = makeGenericMethodInfo.GetParameters();
            il.Emit(OpCodes.Ldarg_0);
            LoadParameters(il, pis, false);
            il.Emit(OpCodes.Callvirt, makeGenericMethodInfo);
            if (makeGenericMethodInfo.ReturnType == typeof(void))
            {
                il.Emit(OpCodes.Ldnull);
            }

            il.Emit(OpCodes.Ret);

            return (DynamicMethodProxyHandler)dm.CreateDelegate(typeof(DynamicMethodProxyHandler));
        }

        public virtual DynamicMethodProxyHandler GetMethodDelegate(
            MethodInfo genericMethodInfo, 
            params Type[] genericParameterTypes)
        {
            return DoGetMethodDelegate(typeof(string).Module, genericMethodInfo, genericParameterTypes);
        }

        public virtual DynamicMethodProxyHandler GetMethodDelegate(
            Module targetModule, 
            MethodInfo genericMethodInfo, 
            params Type[] genericParameterTypes)
        {
            return DoGetMethodDelegate(targetModule, genericMethodInfo, genericParameterTypes);
        }

        #endregion

        #region Visit internal members

        public object CreateInstance(Module targetModule, string typeFullName, bool ignoreCase, bool isPublic, Binder binder, System.Globalization.CultureInfo culture, object[] activationAttrs, params object[] paramObjs)
        {
            if (targetModule == null)
            {
                throw new ArgumentNullException("targetModule");
            }

            //get method info of Assembly.CreateInstance() method first
            MethodInfo mi = ReflectionUtils.GetMethodInfoFromArrayBySignature(
                "System.Object CreateInstance(System.String, Boolean, System.Reflection.BindingFlags, System.Reflection.Binder, System.Object[], System.Globalization.CultureInfo, System.Object[])", 
                typeof(Assembly).GetMethods());

            DynamicMethodProxyHandler dmd = GetMethodDelegate(targetModule, mi);
            return dmd(targetModule.Assembly, typeFullName, ignoreCase, BindingFlags.Instance | (isPublic ? BindingFlags.Public : BindingFlags.NonPublic), binder, paramObjs, culture, activationAttrs);
        }

        public object CreateInstance(Module targetModule, string typeFullName, bool ignoreCase, bool isPublic, params object[] paramObjs)
        {
            return CreateInstance(targetModule, typeFullName, ignoreCase, isPublic, null, null, null, paramObjs);
        }

        #endregion
    }

    public class CachableDynamicMethodProxyFactory : DynamicMethodProxyFactory
    {
        private Dictionary<string, StaticDynamicMethodProxyHandler> cache = new Dictionary<string, StaticDynamicMethodProxyHandler>();
        private Dictionary<string, DynamicMethodProxyHandler> cache2 = new Dictionary<string, DynamicMethodProxyHandler>();

        #region Get static method delegate

        public override StaticDynamicMethodProxyHandler GetStaticMethodDelegate(
            Module targetModule, 
            MethodInfo genericMethodInfo, 
            params Type[] genericParameterTypes)
        {
            #region  Construct cache key
            
            if (targetModule == null)
            {
                throw new ArgumentNullException("targetModule");
            }

            string key = targetModule.FullyQualifiedName + "|" + genericMethodInfo.DeclaringType.ToString() + "|" + genericMethodInfo.ToString();
            if (genericParameterTypes != null)
            {
                for (int i = 0; i < genericParameterTypes.Length; ++i)
                {
                    key += "|" + genericParameterTypes[i].ToString();
                }
            }

            #endregion

            StaticDynamicMethodProxyHandler dmd;

            lock (cache)
            {
                if (cache.ContainsKey(key))
                {
                    dmd = cache[key];
                }
                else
                {
                    dmd = DoGetStaticMethodDelegate(targetModule, genericMethodInfo, genericParameterTypes);
                    cache.Add(key, dmd);
                }
            }

            return dmd;
        }

        public override StaticDynamicMethodProxyHandler GetStaticMethodDelegate(
            MethodInfo genericMethodInfo, 
            params Type[] genericParameterTypes)
        {
            return GetStaticMethodDelegate(typeof(string).Module, genericMethodInfo, genericParameterTypes);
        }

        #endregion

        #region Get non-static method delegate

        public override DynamicMethodProxyHandler GetMethodDelegate(
            Module targetModule, 
            MethodInfo genericMethodInfo, 
            params Type[] genericParameterTypes)
        {
            #region  Construct cache key

            if (targetModule == null)
            {
                throw new ArgumentNullException("targetModule");
            }

            string key = targetModule.FullyQualifiedName + "|" + genericMethodInfo.DeclaringType.ToString() + "|" + genericMethodInfo.ToString();
            if (genericParameterTypes != null)
            {
                for (int i = 0; i < genericParameterTypes.Length; ++i)
                {
                    key += "|" + genericParameterTypes[i].ToString();
                }
            }

            #endregion

            DynamicMethodProxyHandler dmd;

            lock (cache2)
            {
                if (cache2.ContainsKey(key))
                {
                    dmd = cache2[key];
                }
                else
                {
                    dmd = DoGetMethodDelegate(targetModule, genericMethodInfo, genericParameterTypes);
                    cache2.Add(key, dmd);
                }
            }

            return dmd;
        }

        public override DynamicMethodProxyHandler GetMethodDelegate(
            MethodInfo genericMethodInfo, 
            params Type[] genericParameterTypes)
        {
            return GetMethodDelegate(typeof(string).Module, genericMethodInfo, genericParameterTypes);
        }

        #endregion
    }
}
