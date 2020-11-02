using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using CN.Teddy.DesignByContract;

namespace NBear.Common
{
    public class WhereClip : ExpressionClip, ICloneable
    {
        #region Private Members

        private FromClip from;
        private string orderBy;
        private string groupBy;
        private bool isNot = false;

        #endregion

        #region Constructors & factory methods

        public WhereClip(FromClip from): this()
        {
            this.from = from;
        }

        public WhereClip()
        {
            this.dbType = DbType.Boolean;
        }

        public WhereClip(string sql, string[] paramNames, DbType[] paramTypes, object[] paramValues) : this()
        {
            Check.Require(!string.IsNullOrEmpty(sql), "sql could not be null!");
            Check.Require(paramNames == null ||
                (paramNames != null && paramTypes != null && paramValues != null &&
                paramNames.Length == paramTypes.Length && paramNames.Length == paramValues.Length));

            this.sql.Append(sql);

            if (paramNames != null)
            {
                for (int i = 0; i < paramNames.Length; ++i)
                {
                    this.parameters.Add(paramNames[i], new KeyValuePair<DbType, object>(paramTypes[i], paramValues[i]));
                }
            }
        }

        #endregion

        #region Internal Members

        public bool OrderByStartsWith(string str)
        {
            return orderBy == null ? false : orderBy.StartsWith(str);
        }

        #endregion

        #region Properties

        public string OrderBy
        {
            get
            {
                return orderBy;
            }
            set
            {
                orderBy = value;
            }
        }

        public string GroupBy
        {
            get
            {
                return groupBy;
            }
            set
            {
                groupBy = value;
            }
        }

        public bool IsNot
        {
            get
            {
                return isNot;
            }
        }

        #endregion

        #region Public Members

        public static bool IsNullOrEmpty(WhereClip where)
        {
            return ((object)where) == null || where.ToString().Length == 0;
        }

        public FromClip From
        {
            get
            {
                return from;
            }
            set
            {
                from = value;
            }
        }

        public void SetOrderBy(KeyValuePair<string, bool>[] columns)
        {
            if (columns == null || columns.Length == 0)
            {
                orderBy = null;
                return;
            }

            StringBuilder sb  = new StringBuilder();
            for (int i = 0; i < columns.Length; ++i)
            {
                SqlQueryUtils.AppendColumnName(sb, columns[i].Key);

                if (columns[i].Value)
                {
                    sb.Append(" DESC");
                }

                if (i < columns.Length - 1)
                {
                    sb.Append(',');
                }
            }
            orderBy = sb.ToString();
        }

        public void SetGroupBy(string[] columns)
        {
            if (columns == null || columns.Length == 0)
            {
                groupBy = null;
                return;
            }

            StringBuilder sb  = new StringBuilder();
            for (int i = 0; i < columns.Length; ++i)
            {
                SqlQueryUtils.AppendColumnName(sb, columns[i]);

                if (i < columns.Length - 1)
                {
                    sb.Append(',');
                }
            }
            groupBy = sb.ToString();
        }

        public override string ToString()
        {
            StringBuilder retSql = new StringBuilder();
            if (from != null)
            {
                retSql.Append(from.ToString());

                if (!string.IsNullOrEmpty(groupBy))
                {
                    retSql.Append(" GROUP BY ");
                    retSql.Append(groupBy);
                }

                if (sql.Length > 0)
                {
                    if (!string.IsNullOrEmpty(groupBy))
                    {
                        retSql.Append(" HAVING ");
                    }
                    else
                    {
                        retSql.Append(" WHERE ");
                    }
                    if (isNot)
                    {
                        retSql.Append("NOT (");
                    }
                    retSql.Append(sql.ToString());
                    if (isNot)
                    {
                        retSql.Append(')');
                    }
                }
                if (!string.IsNullOrEmpty(orderBy))
                {
                    retSql.Append(" ORDER BY ");
                    retSql.Append(orderBy);
                }
            }
            else
            {
                if (sql.Length > 0)
                {
                    if (isNot)
                    {
                        retSql.Append("NOT (");
                    }
                    retSql.Append(sql.ToString());
                    if (isNot)
                    {
                        retSql.Append(')');
                    }
                }
            }

            return retSql.ToString();
        }

        #endregion

        #region ICloneable Members

        public override object Clone()
        {
            WhereClip where = new WhereClip(this.from);
            where.groupBy = this.groupBy;
            where.isNot = this.isNot;
            where.orderBy = this.orderBy;
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
                where.parameters.Add(newParamName, new KeyValuePair<DbType, object>(en.Current.Value.Key, value));
            }
            where.sql = new StringBuilder(tempSql);
            return where;
        }

        #endregion

        #region Operators

        public WhereClip And(WhereClip where)
        {
            if (WhereClip.IsNullOrEmpty(where) || where.sql.Length == 0)
            {
                return this;
            }

            if (sql.Length > 0)
            {
                sql.Append(" AND ");
            }

            if (where.isNot && where.sql.Length > 0)
            {
                sql.Append("NOT ");
            }

            if (where.sql.Length > 0)
            {
                sql.Append('(');
                sql.Append(where.sql);
                sql.Append(')');
            }
            
            SqlQueryUtils.AddParameters(parameters, where);

            return this;
        }

        internal protected WhereClip And(ExpressionClip left, QueryOperator op, ExpressionClip right)
        {
            if (this.sql.Length > 0)
            {
                this.sql.Append(" AND ");
            }
            this.sql.Append(SqlQueryUtils.ToString(left));
            this.sql.Append(SqlQueryUtils.ToString(op));
            this.sql.Append(SqlQueryUtils.ToString(right));

            SqlQueryUtils.AddParameters(this.parameters, left);
            SqlQueryUtils.AddParameters(this.parameters, right);
            
            return this;
        }

        public WhereClip Or(WhereClip where)
        {
            if (WhereClip.IsNullOrEmpty(where) || where.Sql.Length == 0)
            {
                return this;
            }

            if (sql.Length > 0)
            {
                sql.Append(" OR ");
            }

            if (where.isNot && where.sql.Length > 0)
            {
                sql.Append("NOT ");
            }

            if (where.sql.Length > 0)
            {
                sql.Append('(');
                sql.Append(where.sql);
                sql.Append(')');
            }
            
            SqlQueryUtils.AddParameters(parameters, where);

            return this;
        }

        internal protected WhereClip Or(ExpressionClip left, QueryOperator op, ExpressionClip right)
        {
            if (this.sql.Length > 0)
            {
                this.sql.Append(" OR ");
            }
            this.sql.Append(SqlQueryUtils.ToString(left));
            this.sql.Append(SqlQueryUtils.ToString(op));
            this.sql.Append(SqlQueryUtils.ToString(right));

            SqlQueryUtils.AddParameters(this.parameters, left);
            SqlQueryUtils.AddParameters(this.parameters, right);
            
            return this;
        }

        public WhereClip Not()
        {
            isNot = !isNot;
            return this;
        }

        public static bool operator true(WhereClip right)
        {
            return false;
        }

        public static bool operator false(WhereClip right)
        {
            return false;
        }

        public static WhereClip operator &(WhereClip left, WhereClip right)
        {
            Check.Require(((object)left) != null, "left could not be null.");
            Check.Require(((object)right) != null, "right could not be null.");

            WhereClip newWhere = new WhereClip();
            newWhere.And(left);
            newWhere.And(right);
            return newWhere;
        }

        public static WhereClip operator |(WhereClip left, WhereClip right)
        {
            Check.Require(((object)left) != null, "left could not be null.");
            Check.Require(((object)right) != null, "right could not be null.");

            WhereClip newWhere = new WhereClip();
            newWhere.Or(left);
            newWhere.Or(right);
            return newWhere;
        }

        public static WhereClip operator !(WhereClip right)
        {
            WhereClip newWhere = new WhereClip();
            newWhere.Or(right).Not();
            return newWhere;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            else if (obj is WhereClip)
            {
                return obj.ToString() == this.ToString();
            }

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion

        #region Combined Code From Previous Versiob

        [Obsolete("Please use new WhereClip(sql, paramNames, paramTypes, paramValues) instead.")]
        public WhereClip(string whereStr, params object[] paramValues)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the param values.
        /// </summary>
        /// <value>The param values.</value>
        [Obsolete]
        public object[] ParamValues
        {
            get
            {
                object[] values = new object[this.Parameters.Values.Count];

                Dictionary<string, KeyValuePair<DbType, object>>.ValueCollection.Enumerator  en = this.Parameters.Values.GetEnumerator();
                int i = 0;
                while (en.MoveNext())
                {
                    values[i] = en.Current.Value;

                    ++i;
                }

                return values;
            }
        }

        public static WhereClip All
        {
            get
            {
                return new WhereClip();
            }
        }

        #endregion
    }
}
