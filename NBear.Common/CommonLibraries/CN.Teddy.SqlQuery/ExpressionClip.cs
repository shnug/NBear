using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using CN.Teddy.DesignByContract;

namespace NBear.Common
{
    public abstract class ExpressionBase : IExpression
    {
        #region Protected Members

        protected StringBuilder sql = new StringBuilder();
        protected readonly Dictionary<string, KeyValuePair<DbType, object>> parameters = new Dictionary<string, KeyValuePair<DbType, object>>();

        internal protected static string MakeUniqueParamNameWithoutPrefixToken()
        {
            return CN.Teddy.Common.CommonUtils.MakeUniqueKey(15, "p");
        }

        #endregion

        #region IExpression Members

        public string Sql
        {
            get
            {
                return sql.ToString();
            }
            set
            {
                sql = new StringBuilder(value);
            }
        }

        public Dictionary<string, KeyValuePair<DbType, object>> Parameters
        {
            get
            {
                return parameters;
            }
        }

        public override string ToString()
        {
            return sql.ToString();
        }

        #endregion
    }

    [Serializable]
    public class ExpressionClip : ExpressionBase, ICloneable
    {
        #region Protected Members

        protected DbType dbType;

        #endregion

        #region Properties

        public DbType DbType
        {
            get
            {
                return dbType;
            }
            set
            {
                dbType = value;
            }
        }

        #endregion

        #region Constructors & factory methods

        public ExpressionClip()
        {
        }

        protected void InitColumnExpression(string columnName, DbType type)
        {
            if (this.sql.Length > 0)
            {
                this.sql = new StringBuilder();
            }
            SqlQueryUtils.AppendColumnName(this.sql, columnName);
            this.DbType = type;
        }

        internal protected ExpressionClip(string columnName, DbType type)
        {
            Check.Require(columnName != null, "columnName could not be null!");

            InitColumnExpression(columnName, type);
        }

        internal protected ExpressionClip(DbType type, object value)
        {
            string paramName = MakeUniqueParamNameWithoutPrefixToken();
            this.sql.Append('@');
            this.sql.Append(paramName.TrimStart('@'));
            this.parameters.Add(paramName, new KeyValuePair<DbType, object>(type, value));
            this.dbType = type;
        }

        internal protected ExpressionClip(string sql, DbType type, string[] paramNames, DbType[] types, object[] values)
        {
            Check.Require(!string.IsNullOrEmpty(sql), "sql could not be null or empty!");
            Check.Require(paramNames == null ||
                (types != null && values != null && paramNames.Length == types.Length && paramNames.Length == values.Length), 
                "length of paramNames, types and values must equal!");

            this.sql.Append(sql);
            this.dbType = type;
            if (paramNames != null)
            {
                for (int i = 0; i < paramNames.Length; ++i)
                {
                    this.parameters.Add(paramNames[i], new KeyValuePair<DbType, object>(types[i], values[i]));
                }
            }
        }

        internal protected ExpressionClip Append(QueryOperator op, ExpressionClip right)
        {
            this.sql.Append(SqlQueryUtils.ToString(op));
            this.sql.Append(SqlQueryUtils.ToString(right));
            SqlQueryUtils.AddParameters(this.parameters, right);
            return this;
        }

        #endregion

        #region ICloneable Members

        public virtual object Clone()
        {
            ExpressionClip newExpr = new ExpressionClip();
            newExpr.dbType = this.dbType;
            string tempSql = this.sql.ToString();

            Dictionary<string, KeyValuePair<DbType, object>>.Enumerator en = this.parameters.GetEnumerator();
            while (en.MoveNext())
            {
                object value = en.Current.Value.Value;
                if (value != null && value != DBNull.Value && value is ICloneable)
                {
                    value = ((ICloneable)value).Clone();
                }

                string newParamName = MakeUniqueParamNameWithoutPrefixToken();
                tempSql = tempSql.Replace('@' + en.Current.Key, '@' + newParamName);
                newExpr.Parameters.Add(newParamName, new KeyValuePair<DbType, object>(en.Current.Value.Key, value));
            }
            newExpr.sql.Append(tempSql);
            return newExpr;
        }

        #endregion

        #region Serializable

        public string SerializedParameters
        {
            get
            {
                string[] list = new string[parameters.Count * 4];
                int i = 0;
                Dictionary<string, KeyValuePair<DbType, object>>.Enumerator en = parameters.GetEnumerator();
                while (en.MoveNext())
                {
                    list[i++] = en.Current.Key;
                    list[i++] = ((int)en.Current.Value.Key).ToString();
                    if (en.Current.Value.Value == null)
                    {
                        list[i++] = null;
                        list[i++] = null;
                    }
                    else
                    {
                        list[i++] =  en.Current.Value.Value.GetType().ToString();
                        list[i++] = CN.Teddy.Common.SerializationManager.Instance.Serialize(en.Current.Value.Value);
                    }
                }

                return CN.Teddy.Common.SerializationManager.Instance.Serialize(list);
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    return;
                }

                string[] list = (string[])CN.Teddy.Common.SerializationManager.Instance.Deserialize(typeof(string[]), value);
                parameters.Clear();
                for (int i = 0; i < list.Length / 4; ++i)
                {
                    KeyValuePair<DbType,object> keyValue;
                    if (list[i + 2] == null)
                    {
                        keyValue = new KeyValuePair<DbType,object>(this.dbType, null);
                    }
                    else
                    {
                        keyValue = new KeyValuePair<DbType,object>((DbType)int.Parse(list[i + 1]),
                            CN.Teddy.Common.SerializationManager.Instance.Deserialize(CN.Teddy.Common.CommonUtils.GetType(list[i + 2]), list[i + 3]));
                    }
                    parameters.Add(list[i], keyValue);
                }
            }
        }

        #endregion

        #region Operators & Database Functions

        public static bool IsNullOrEmpty(ExpressionClip expr)
        {
            return ((object)expr) == null || expr.sql.Length == 0;
        }

        #region String Functions

        public WhereClip Like(string right)
        {
            Check.Require(right != null, "right could not be null.");

            return ExpressionFactory.AppendWhereAnd(new WhereClip(), this, NBear.Common.QueryOperator.Like, ExpressionFactory.CreateParameterExpression(this.DbType, right));
        }

        public WhereClip Contains(string subString)
        {
            Check.Require(!string.IsNullOrEmpty(subString), "subString could not be null or empty!");

            return ExpressionFactory.AppendWhereAnd(new WhereClip(), this, QueryOperator.Like, ExpressionFactory.CreateParameterExpression(this.dbType,  '%' + subString.Replace("%", "[%]").Replace("_", "[_]") + '%'));
        }

        public WhereClip StartsWith(string prefix)
        {
            Check.Require(!string.IsNullOrEmpty(prefix), "prefix could not be null or empty!");

            return ExpressionFactory.AppendWhereAnd(new WhereClip(), this, QueryOperator.Like, ExpressionFactory.CreateParameterExpression(this.dbType,  prefix.Replace("%", "[%]").Replace("_", "[_]") + '%'));
        }

        public WhereClip EndsWith(string suffix)
        {
            Check.Require(!string.IsNullOrEmpty(suffix), "suffix could not be null or empty!");

            return ExpressionFactory.AppendWhereAnd(new WhereClip(), this, QueryOperator.Like, ExpressionFactory.CreateParameterExpression(this.dbType,  '%' + suffix.Replace("%", "[%]").Replace("_", "[_]")));
        }

        public ExpressionClip Length
        {
            get
            {
                ExpressionClip expr = ExpressionFactory.CreateCloneExpression(this);
                expr.Sql = ColumnFormatter.Length(this.Sql);
                expr.dbType = DbType.Int32;

                return expr;
            }
        }

        public ExpressionClip ToUpper()
        {
            ExpressionClip expr = ExpressionFactory.CreateCloneExpression(this);
            expr.Sql = ColumnFormatter.ToUpper(this.Sql);

            return expr;
        }

        public ExpressionClip ToLower()
        {
            ExpressionClip expr = ExpressionFactory.CreateCloneExpression(this);
            expr.Sql = ColumnFormatter.ToLower(this.Sql);

            return expr;
        }

        public ExpressionClip Trim()
        {
            ExpressionClip expr = ExpressionFactory.CreateCloneExpression(this);
            expr.Sql = ColumnFormatter.Trim(this.Sql);

            return expr;
        }

        public ExpressionClip SubString(int start)
        {
            Check.Require(start >= 0, "start must >= 0!");

            ExpressionClip expr = ExpressionFactory.CreateCloneExpression(this);
            ExpressionClip cloneExpr = ExpressionFactory.CreateCloneExpression(this);
            StringBuilder sb = new StringBuilder("SUBSTRING(");
            SqlQueryUtils.AppendColumnName(sb, this.Sql);
            sb.Append(',');
            sb.Append(start + 1);
            sb.Append(",LEN(");
            SqlQueryUtils.AppendColumnName(sb, cloneExpr.Sql);
            sb.Append(')');
            sb.Append(')');

            expr.sql = sb;
            SqlQueryUtils.AddParameters(expr.parameters, cloneExpr);

            return expr;
        }

        public ExpressionClip SubString(int start, int length)
        {
            ExpressionClip expr = ExpressionFactory.CreateCloneExpression(this);
            expr.Sql = ColumnFormatter.SubString(this.Sql, start, length);
            return expr;
        }

        public ExpressionClip IndexOf(string subString)
        {
            Check.Require(!string.IsNullOrEmpty(subString), "subString could not be null or empty!");

            ExpressionClip expr = ExpressionFactory.CreateCloneExpression(this);
            StringBuilder sb = new StringBuilder();
            sb.Append("CHARINDEX(");
            string paramName = MakeUniqueParamNameWithoutPrefixToken();
            sb.Append(paramName);
            sb.Append(',');
            SqlQueryUtils.AppendColumnName(sb, this.Sql);
            sb.Append(')');
            sb.Append("-1");

            expr.sql = sb;
            expr.dbType = DbType.Int32;
            expr.parameters.Add(paramName, new KeyValuePair<DbType, object>(this.dbType, subString));

            return expr;
        }

        public ExpressionClip Replace(string subString, string replaceString)
        {
            Check.Require(!string.IsNullOrEmpty(subString), "subString could not be null or empty!");
            Check.Require(!string.IsNullOrEmpty(replaceString), "replaceString could not be null or empty!");

            ExpressionClip expr = ExpressionFactory.CreateCloneExpression(this);
            StringBuilder sb = new StringBuilder();
            sb.Append("REPLACE(");
            SqlQueryUtils.AppendColumnName(sb, this.Sql);
            sb.Append(',');
            string paramName = MakeUniqueParamNameWithoutPrefixToken();
            sb.Append(paramName);
            sb.Append(',');
            string paramName2 = MakeUniqueParamNameWithoutPrefixToken();
            sb.Append(paramName2);
            sb.Append(')');

            expr.sql = sb;
            expr.dbType = DbType.Int32;
            expr.parameters.Add(paramName, new KeyValuePair<DbType, object>(this.dbType, subString));    
            expr.Parameters.Add(paramName2, new KeyValuePair<DbType, object>(this.dbType, replaceString));
    
            return expr;
        }

        #endregion

        #region DateTime Functions

        public ExpressionClip GetYear()
        {
            ExpressionClip expr = ExpressionFactory.CreateCloneExpression(this);
            expr.Sql = ColumnFormatter.DatePart(this.Sql, ColumnFormatter.DatePartType.Year);
            expr.dbType = DbType.Int32;

            return expr;
        }

        public ExpressionClip GetMonth()
        {
            ExpressionClip expr = ExpressionFactory.CreateCloneExpression(this);
            expr.Sql = ColumnFormatter.DatePart(this.Sql, ColumnFormatter.DatePartType.Month);
            expr.dbType = DbType.Int32;

            return expr;
        }

        public ExpressionClip GetDay()
        {
            ExpressionClip expr = ExpressionFactory.CreateCloneExpression(this);
            expr.Sql = ColumnFormatter.DatePart(this.Sql, ColumnFormatter.DatePartType.Day);
            expr.dbType = DbType.Int32;

            return expr;
        }

        public static ExpressionClip GetCurrentDate()
        {
            ExpressionClip expr = new ExpressionClip();
            expr.Sql = ColumnFormatter.GetCurrentDate();
            expr.dbType = DbType.DateTime;
            return expr;
        }

        #endregion

        #region Aggregation Functions

        public ExpressionClip Distinct
        {
            get
            {
                ExpressionClip expr = ExpressionFactory.CreateCloneExpression(this);
                expr.Sql = "DISTINCT " + this.Sql;

                return expr;
            }
        }

        public ExpressionClip Count()
        {
            ExpressionClip expr = ExpressionFactory.CreateCloneExpression(this);
            expr.Sql = ColumnFormatter.Count(this.Sql);
            expr.dbType = DbType.Int32;

            return expr;
        }

        public ExpressionClip Count(bool isDistinct)
        {
            ExpressionClip expr = ExpressionFactory.CreateCloneExpression(this);
            expr.Sql = ColumnFormatter.Count(this.Sql, isDistinct);
            expr.dbType = DbType.Int32;

            return expr;
        }

        public ExpressionClip Sum()
        {
            ExpressionClip expr = ExpressionFactory.CreateCloneExpression(this);
            expr.Sql = ColumnFormatter.Sum(this.Sql);

            return expr;
        }

        public ExpressionClip Min()
        {
            ExpressionClip expr = ExpressionFactory.CreateCloneExpression(this);
            expr.Sql = ColumnFormatter.Min(this.Sql);

            return expr;
        }

        public ExpressionClip Max()
        {
            ExpressionClip expr = ExpressionFactory.CreateCloneExpression(this);
            expr.Sql = ColumnFormatter.Max(this.Sql);

            return expr;
        }

        public ExpressionClip Avg()
        {
            ExpressionClip expr = ExpressionFactory.CreateCloneExpression(this);
            expr.Sql = ColumnFormatter.Avg(this.Sql);

            return expr;
        }

        #endregion

        #region Equals and Not Equals

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public static WhereClip operator ==(ExpressionClip left, ExpressionClip right)
        {
            WhereClip where = new WhereClip();
            if (ExpressionClip.IsNullOrEmpty(right))
            {
                ExpressionFactory.AppendWhereAnd(where, left, QueryOperator.IsNULL, null);
            }
            else if (ExpressionClip.IsNullOrEmpty(left))
            {
                ExpressionFactory.AppendWhereAnd(where, right, QueryOperator.IsNULL, null);
            }
            else
            {
                ExpressionFactory.AppendWhereAnd(where, left, QueryOperator.Equal, right);
            }

            return where;
        }

        public static WhereClip operator !=(ExpressionClip left, ExpressionClip right)
        {
            WhereClip where = new WhereClip();
            if (ExpressionClip.IsNullOrEmpty(right))
            {
                where = ExpressionFactory.AppendWhereAnd(where, left, QueryOperator.IsNULL, null).Not();
            }
            else if (ExpressionClip.IsNullOrEmpty(left))
            {
                where = ExpressionFactory.AppendWhereAnd(where, right, QueryOperator.IsNULL, null).Not();
            }
            else
            {
                ExpressionFactory.AppendWhereAnd(where, left, QueryOperator.NotEqual, right);
            }

            return where;
        }

        public static WhereClip operator ==(ExpressionClip left, object right)
        {
            WhereClip where = new WhereClip();
            if (right == null || right == DBNull.Value)
            {
                ExpressionFactory.AppendWhereAnd(where, left, QueryOperator.IsNULL, null);
            }
            else
            {
                ExpressionFactory.AppendWhereAnd(where, left, QueryOperator.Equal, 
                    ExpressionFactory.CreateParameterExpression(left.DbType, right));
            }

            return where;
        }

        public static WhereClip operator !=(ExpressionClip left, object right)
        {
            WhereClip where = new WhereClip();
            if (right == null || right == DBNull.Value)
            {
                where = ExpressionFactory.AppendWhereAnd(where, left, QueryOperator.IsNULL, null).Not();
            }
            else
            {
                ExpressionFactory.AppendWhereAnd(where, left, QueryOperator.NotEqual, 
                    ExpressionFactory.CreateParameterExpression(left.DbType, right));
            }

            return where;
        }

        public static WhereClip operator ==(object left, ExpressionClip right)
        {
            WhereClip where = new WhereClip();
            if (left == null || left == DBNull.Value)
            {
                ExpressionFactory.AppendWhereAnd(where, right, QueryOperator.IsNULL, null);
            }
            else
            {
                ExpressionFactory.AppendWhereAnd(where, ExpressionFactory.CreateParameterExpression(right.DbType, left), 
                    QueryOperator.Equal, right);
            }

            return where;
        }

        public static WhereClip operator !=(object left, ExpressionClip right)
        {
            WhereClip where = new WhereClip();
            if (left == null || left == DBNull.Value)
            {
                where = ExpressionFactory.AppendWhereAnd(where, right, QueryOperator.IsNULL, null).Not();
            }
            else
            {
                ExpressionFactory.AppendWhereAnd(where, ExpressionFactory.CreateParameterExpression(right.DbType, left), 
                    QueryOperator.NotEqual, right);
            }

            return where;
        }

        public WhereClip In(params object[] objs)
        {
            Check.Require(objs != null && objs.Length > 0, "objs could not be null or empty.");

            WhereClip where = new WhereClip();
            foreach (object obj in objs)
            {
                where.Or(this == obj);
            }

            return where;
        }

        #endregion

        #region Greater and Less

        public static WhereClip operator >(ExpressionClip left, ExpressionClip right)
        {
            WhereClip where = new WhereClip();
            if (ExpressionClip.IsNullOrEmpty(right))
            {
                ExpressionFactory.AppendWhereAnd(where, left, QueryOperator.Greater, null);
            }
            else if (ExpressionClip.IsNullOrEmpty(left))
            {
                ExpressionFactory.AppendWhereAnd(where, right, QueryOperator.Less, null);
            }
            else
            {
                ExpressionFactory.AppendWhereAnd(where, left, QueryOperator.Greater, right);
            }

            return where;
        }

        public static WhereClip operator <(ExpressionClip left, ExpressionClip right)
        {
            WhereClip where = new WhereClip();
            if (ExpressionClip.IsNullOrEmpty(right))
            {
                where = ExpressionFactory.AppendWhereAnd(where, left, QueryOperator.Less, null);
            }
            else if (ExpressionClip.IsNullOrEmpty(left))
            {
                where = ExpressionFactory.AppendWhereAnd(where, right, QueryOperator.Greater, null);
            }
            else
            {
                ExpressionFactory.AppendWhereAnd(where, left, QueryOperator.Less, right);
            }

            return where;
        }

        public static WhereClip operator >(ExpressionClip left, object right)
        {
            WhereClip where = new WhereClip();
            if (right == null || right == DBNull.Value)
            {
                ExpressionFactory.AppendWhereAnd(where, left, QueryOperator.Greater, null);
            }
            else
            {
                ExpressionFactory.AppendWhereAnd(where, left, QueryOperator.Greater, 
                    ExpressionFactory.CreateParameterExpression(left.DbType, right));
            }

            return where;
        }

        public static WhereClip operator <(ExpressionClip left, object right)
        {
            WhereClip where = new WhereClip();
            if (right == null || right == DBNull.Value)
            {
                where = ExpressionFactory.AppendWhereAnd(where, left, QueryOperator.Less, null);
            }
            else
            {
                ExpressionFactory.AppendWhereAnd(where, left, QueryOperator.Less, 
                    ExpressionFactory.CreateParameterExpression(left.DbType, right));
            }

            return where;
        }

        public static WhereClip operator >(object left, ExpressionClip right)
        {
            WhereClip where = new WhereClip();
            if (left == null || left == DBNull.Value)
            {
                ExpressionFactory.AppendWhereAnd(where, right, QueryOperator.Less, null);
            }
            else
            {
                ExpressionFactory.AppendWhereAnd(where, ExpressionFactory.CreateParameterExpression(right.DbType, left), 
                    QueryOperator.Greater, right);
            }

            return where;
        }

        public static WhereClip operator <(object left, ExpressionClip right)
        {
            WhereClip where = new WhereClip();
            if (left == null || left == DBNull.Value)
            {
                where = ExpressionFactory.AppendWhereAnd(where, right, QueryOperator.Greater, null);
            }
            else
            {
                ExpressionFactory.AppendWhereAnd(where, ExpressionFactory.CreateParameterExpression(right.DbType, left), 
                    QueryOperator.Less, right);
            }

            return where;
        }

        #endregion

        #region Greater or Equals and Less or Equals

        public static WhereClip operator >=(ExpressionClip left, ExpressionClip right)
        {
            WhereClip where = new WhereClip();
            if (ExpressionClip.IsNullOrEmpty(right))
            {
                ExpressionFactory.AppendWhereAnd(where, left, QueryOperator.GreaterOrEqual, null);
            }
            else if (ExpressionClip.IsNullOrEmpty(left))
            {
                ExpressionFactory.AppendWhereAnd(where, right, QueryOperator.LessOrEqual, null);
            }
            else
            {
                ExpressionFactory.AppendWhereAnd(where, left, QueryOperator.GreaterOrEqual, right);
            }

            return where;
        }

        public static WhereClip operator <=(ExpressionClip left, ExpressionClip right)
        {
            WhereClip where = new WhereClip();
            if (ExpressionClip.IsNullOrEmpty(right))
            {
                where = ExpressionFactory.AppendWhereAnd(where, left, QueryOperator.LessOrEqual, null);
            }
            else if (ExpressionClip.IsNullOrEmpty(left))
            {
                where = ExpressionFactory.AppendWhereAnd(where, right, QueryOperator.GreaterOrEqual, null);
            }
            else
            {
                ExpressionFactory.AppendWhereAnd(where, left, QueryOperator.LessOrEqual, right);
            }

            return where;
        }

        public static WhereClip operator >=(ExpressionClip left, object right)
        {
            WhereClip where = new WhereClip();
            if (right == null || right == DBNull.Value)
            {
                ExpressionFactory.AppendWhereAnd(where, left, QueryOperator.GreaterOrEqual, null);
            }
            else
            {
                ExpressionFactory.AppendWhereAnd(where, left, QueryOperator.GreaterOrEqual, 
                    ExpressionFactory.CreateParameterExpression(left.DbType, right));
            }

            return where;
        }

        public static WhereClip operator <=(ExpressionClip left, object right)
        {
            WhereClip where = new WhereClip();
            if (right == null || right == DBNull.Value)
            {
                where = ExpressionFactory.AppendWhereAnd(where, left, QueryOperator.LessOrEqual, null);
            }
            else
            {
                ExpressionFactory.AppendWhereAnd(where, left, QueryOperator.LessOrEqual, 
                    ExpressionFactory.CreateParameterExpression(left.DbType, right));
            }

            return where;
        }

        public static WhereClip operator >=(object left, ExpressionClip right)
        {
            WhereClip where = new WhereClip();
            if (left == null || left == DBNull.Value)
            {
                ExpressionFactory.AppendWhereAnd(where, right, QueryOperator.LessOrEqual, null);
            }
            else
            {
                ExpressionFactory.AppendWhereAnd(where, ExpressionFactory.CreateParameterExpression(right.DbType, left), 
                    QueryOperator.GreaterOrEqual, right);
            }

            return where;
        }

        public static WhereClip operator <=(object left, ExpressionClip right)
        {
            WhereClip where = new WhereClip();
            if (left == null || left == DBNull.Value)
            {
                where = ExpressionFactory.AppendWhereAnd(where, right, QueryOperator.GreaterOrEqual, null);
            }
            else
            {
                ExpressionFactory.AppendWhereAnd(where, ExpressionFactory.CreateParameterExpression(right.DbType, left), 
                    QueryOperator.LessOrEqual, right);
            }

            return where;
        }

        public WhereClip Between(object left, object right)
        {
            Check.Require(left != null, "left could not be null.");
            Check.Require(right != null, "right could not be null.");

            return (this >= left).And(this <= right);
        }

        #endregion

        #region + - * / %

        public static ExpressionClip operator +(ExpressionClip left, ExpressionClip right)
        {
            ExpressionClip expr = ExpressionFactory.CreateCloneExpression(left);
            ExpressionFactory.AppendExpression(expr, QueryOperator.Add, right);
            return expr;
        }

        public static ExpressionClip operator +(object left, ExpressionClip right)
        {
            ExpressionClip expr = ExpressionFactory.CreateParameterExpression(right.dbType, left);
            ExpressionFactory.AppendExpression(expr, QueryOperator.Add, right);
            return expr;
        }

        public static ExpressionClip operator +(ExpressionClip left, object right)
        {
            ExpressionClip expr = ExpressionFactory.CreateCloneExpression(left);
            ExpressionFactory.AppendExpression(expr, QueryOperator.Add, 
                ExpressionFactory.CreateParameterExpression(left.dbType, right));
            return expr;
        }

        public static ExpressionClip operator -(ExpressionClip left, ExpressionClip right)
        {
            ExpressionClip expr = ExpressionFactory.CreateCloneExpression(left);
            ExpressionFactory.AppendExpression(expr, QueryOperator.Subtract, right);
            return expr;
        }

        public static ExpressionClip operator -(object left, ExpressionClip right)
        {
            ExpressionClip expr = ExpressionFactory.CreateParameterExpression(right.dbType, left);
            ExpressionFactory.AppendExpression(expr, QueryOperator.Subtract, right);
            return expr;
        }

        public static ExpressionClip operator -(ExpressionClip left, object right)
        {
            ExpressionClip expr = ExpressionFactory.CreateCloneExpression(left);
            ExpressionFactory.AppendExpression(expr, QueryOperator.Subtract, 
                ExpressionFactory.CreateParameterExpression(left.dbType, right));
            return expr;
        }

        public static ExpressionClip operator *(ExpressionClip left, ExpressionClip right)
        {
            ExpressionClip expr = ExpressionFactory.CreateCloneExpression(left);
            ExpressionFactory.AppendExpression(expr, QueryOperator.Multiply, right);
            return expr;
        }

        public static ExpressionClip operator *(object left, ExpressionClip right)
        {
            ExpressionClip expr = ExpressionFactory.CreateParameterExpression(right.dbType, left);
            ExpressionFactory.AppendExpression(expr, QueryOperator.Multiply, right);
            return expr;
        }

        public static ExpressionClip operator *(ExpressionClip left, object right)
        {
            ExpressionClip expr = ExpressionFactory.CreateCloneExpression(left);
            ExpressionFactory.AppendExpression(expr, QueryOperator.Multiply, 
                ExpressionFactory.CreateParameterExpression(left.dbType, right));
            return expr;
        }

        public static ExpressionClip operator /(ExpressionClip left, ExpressionClip right)
        {
            ExpressionClip expr = ExpressionFactory.CreateCloneExpression(left);
            ExpressionFactory.AppendExpression(expr, QueryOperator.Divide, right);
            return expr;
        }

        public static ExpressionClip operator /(object left, ExpressionClip right)
        {
            ExpressionClip expr = ExpressionFactory.CreateParameterExpression(right.dbType, left);
            ExpressionFactory.AppendExpression(expr, QueryOperator.Divide, right);
            return expr;
        }

        public static ExpressionClip operator /(ExpressionClip left, object right)
        {
            ExpressionClip expr = ExpressionFactory.CreateCloneExpression(left);
            ExpressionFactory.AppendExpression(expr, QueryOperator.Divide, 
                ExpressionFactory.CreateParameterExpression(left.dbType, right));
            return expr;
        }

        public static ExpressionClip operator %(ExpressionClip left, ExpressionClip right)
        {
            ExpressionClip expr = ExpressionFactory.CreateCloneExpression(left);
            ExpressionFactory.AppendExpression(expr, QueryOperator.Modulo, right);
            expr.dbType = DbType.Int32;
            return expr;
        }

        public static ExpressionClip operator %(object left, ExpressionClip right)
        {
            ExpressionClip expr = ExpressionFactory.CreateParameterExpression(right.dbType, left);
            ExpressionFactory.AppendExpression(expr, QueryOperator.Modulo, right);
            expr.dbType = DbType.Int32;
            return expr;
        }

        public static ExpressionClip operator %(ExpressionClip left, object right)
        {
            ExpressionClip expr = ExpressionFactory.CreateCloneExpression(left);
            ExpressionFactory.AppendExpression(expr, QueryOperator.Modulo, 
                ExpressionFactory.CreateParameterExpression(left.dbType, right));
            expr.dbType = DbType.Int32;
            return expr;
        }

        #endregion

        #region Bitwise

        public static ExpressionClip operator!(ExpressionClip left)
        {
            ExpressionClip expr = ExpressionFactory.AppendExpression(new ExpressionClip(), QueryOperator.BitwiseNOT,
                left);
            return expr;
        }

        public ExpressionClip BitwiseAnd(ExpressionClip right)
        {
            ExpressionClip expr = ExpressionFactory.CreateCloneExpression(this);
            ExpressionFactory.AppendExpression(expr, QueryOperator.BitwiseAND,
                right);

            return expr;
        }

        public ExpressionClip BitwiseAnd(object right)
        {
            Check.Require(right != null, "right could not be null!");

            ExpressionClip expr = ExpressionFactory.CreateCloneExpression(this);
            ExpressionFactory.AppendExpression(expr, QueryOperator.BitwiseAND,
                ExpressionFactory.CreateParameterExpression(this.dbType, right));

            return expr;
        }

        public ExpressionClip BitwiseOr(ExpressionClip right)
        {
            ExpressionClip expr = ExpressionFactory.CreateCloneExpression(this);
            ExpressionFactory.AppendExpression(expr, QueryOperator.BitwiseOR,
                right);

            return expr;
        }

        public ExpressionClip BitwiseOr(object right)
        {
            Check.Require(right != null, "right could not be null!");

            ExpressionClip expr = ExpressionFactory.CreateCloneExpression(this);
            ExpressionFactory.AppendExpression(expr, QueryOperator.BitwiseOR,
                ExpressionFactory.CreateParameterExpression(this.dbType, right));

            return expr;
        }

        public ExpressionClip BitwiseXOr(ExpressionClip right)
        {
            ExpressionClip expr = ExpressionFactory.CreateCloneExpression(this);
            ExpressionFactory.AppendExpression(expr, QueryOperator.BitwiseXOR,
                right);

            return expr;
        }

        public ExpressionClip BitwiseXOr(object right)
        {
            Check.Require(right != null, "right could not be null!");

            ExpressionClip expr = ExpressionFactory.CreateCloneExpression(this);
            ExpressionFactory.AppendExpression(expr, QueryOperator.BitwiseXOR,
                ExpressionFactory.CreateParameterExpression(this.dbType, right));

            return expr;
        }

        #endregion

        #endregion
    }
}