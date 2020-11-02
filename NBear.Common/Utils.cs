using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace NBear.Common
{
    /// <summary>
    /// The Util class.
    /// </summary>
    public abstract class Util
    {
        private Util()
        {
        }

        /// <summary>
        /// Gets the default value of a specified Type.
        /// </summary>
        /// <returns>The default value.</returns>
        public static object DefaultValue<MemberType>()
        {
            return default(MemberType);
        }

        /// <summary>
        /// Gets the default value of a specified Type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public static object DefaultValue(Type type)
        {
            return CN.Teddy.Common.CommonUtils.DefaultValue(type);
        }

        /// <summary>
        /// Deeply gets property infos.
        /// </summary>
        /// <param name="types">The types.</param>
        /// <returns>Property infos of all the types and there base classes/interfaces</returns>
        public static PropertyInfo[] DeepGetProperties(params Type[] types)
        {
            return CN.Teddy.Reflection.ReflectionUtils.DeepGetProperties(types);
        }

        /// <summary>
        /// Gets the type of the original type of array.
        /// </summary>
        /// <param name="returnType">Type of the return.</param>
        /// <returns></returns>
        public static Type GetOriginalTypeOfArrayType(Type returnType)
        {
            return returnType.GetElementType();
        }

        /// <summary>
        /// Gets a type in all loaded assemblies of current app domain.
        /// </summary>
        /// <param name="fullName">The full name.</param>
        /// <returns></returns>
        public static Type GetType(string fullName)
        {
            return CN.Teddy.Common.CommonUtils.GetType(fullName);
        }

        /// <summary>
        /// Deeply get property info from specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns></returns>
        public static PropertyInfo DeepGetProperty(Type type, string propertyName)
        {
            return CN.Teddy.Reflection.ReflectionUtils.DeepGetProperty(type, propertyName);
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
            return CN.Teddy.Reflection.ReflectionUtils.DeepGetField(type, name, isPublic);
        }

        /// <summary>
        /// Parses the relative path to absolute path.
        /// </summary>
        /// <param name="basePath">The base path.</param>
        /// <param name="relativePath">The relative path.</param>
        /// <returns></returns>
        public static string ParseRelativePath(string basePath, string relativePath)
        {
            Check.Require(basePath != null, "basePath could not be null.");
            Check.Require(relativePath != null, "relativePath could not be null.");

            if (relativePath.StartsWith("\\") || relativePath.StartsWith(".\\") || relativePath.Contains(":"))
            {
                return System.IO.Path.GetFullPath(relativePath);
            }

            basePath = basePath.Trim().Replace("/", "\\");
            relativePath = relativePath.Trim().Replace("/", "\\");

            string[] splittedBasePath = basePath.Split('\\');
            string[] splittedRelativePath = relativePath.Split('\\');

            StringBuilder sb = new StringBuilder();
            int parentTokenCount = 0;
            for (int i = 0; i < splittedRelativePath.Length; i++)
            {
                if (splittedRelativePath[i] == "..")
                {
                    parentTokenCount++;
                }
                else
                {
                    break;
                }
            }

            for (int i = 0; i < splittedBasePath.Length - parentTokenCount; i++)
            {
                if (!string.IsNullOrEmpty(splittedBasePath[i]))
                {
                    sb.Append(splittedBasePath[i]);
                    sb.Append("\\");
                }
            }

            for (int i = parentTokenCount; i < splittedRelativePath.Length; i++)
            {
                if (!string.IsNullOrEmpty(splittedRelativePath[i]))
                {
                    sb.Append(splittedRelativePath[i]);
                    sb.Append("\\");
                }
            }

            return sb.ToString().TrimEnd('\\');
        }

        /// <summary>
        /// Formats the param val.
        /// </summary>
        /// <param name="val">The val.</param>
        /// <returns></returns>
        public static string FormatParamVal(object val)
        {
            if (val == null || val == DBNull.Value)
            {
                return "NULL";
            }

            Type type = val.GetType();

            if (type == typeof(string))
            {
                return string.Format("N'{0}'", val.ToString().Replace("'", "''"));
            }
            else if (type == typeof(DateTime) || type == typeof(Guid))
            {
                return string.Format("'{0}'", val);
            }
            else if (type== typeof(TimeSpan))
            {
                DateTime baseTime = new DateTime(1949, 10, 1);
                return string.Format("(CAST('{0}' AS datetime) - CAST('{1}' AS datetime))", baseTime + ((TimeSpan)val), baseTime);
            }
            else if (type == typeof(bool))
            {
                return ((bool)val) ? "1" : "0";
            }
            else if (val is NBear.Common.ExpressionClip)
            {
                return Util.ToString((NBear.Common.ExpressionClip)val) ;
            }
            else if (type.IsEnum)
            {
                return Convert.ToInt32(val).ToString();
            }
            else if (type.IsValueType)
            {
                return val.ToString();
            }
            else
            {
                return string.Format("'{0}'", val.ToString().Replace("'", "''"));
            }
        }

        public static string ToString(NBear.Common.IExpression expr)
        {
            if (expr == null)
            {
                return null;
            }

            string sql = expr.ToString();

            if (!string.IsNullOrEmpty(sql))
            {
                Dictionary<string, KeyValuePair<DbType, object>>.Enumerator en = expr.Parameters.GetEnumerator();

                while (en.MoveNext())
                {
                    sql = sql.Replace('@' + en.Current.Key, Util.FormatParamVal(en.Current.Value.Value));
                }
            }

            return sql;
        }
    }
}
