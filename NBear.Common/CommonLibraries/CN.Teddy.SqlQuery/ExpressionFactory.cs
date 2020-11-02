using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using CN.Teddy.DesignByContract;

namespace NBear.Common
{
    public sealed class ExpressionFactory
    {
        private ExpressionFactory() { }

        #region ExpressionClip Factory Methods

        public static ExpressionClip CreateColumnExpression(string columnName, DbType type)
        {
            return new ExpressionClip(columnName, type);
        }

        public static ExpressionClip CreateParameterExpression(DbType type, object value)
        {
            return new ExpressionClip(type, value);
        }

        public static ExpressionClip CreateCustomExpression(string sql, DbType type, string[] paramNames, DbType[] types, object[] values)
        {
            return new ExpressionClip(sql, type, paramNames, types, values);
        }

        public static ExpressionClip CreateCloneExpression(ExpressionClip expr)
        {
            Check.Require(!ExpressionClip.IsNullOrEmpty(expr));

            return (ExpressionClip)expr.Clone();
        }

        public static ExpressionClip AppendExpression(ExpressionClip left, QueryOperator op, ExpressionClip right)
        {
            return left.Append(op, right);
        }

        #endregion

        #region  WhereClip Factory Methods

        public static WhereClip AppendWhereAnd(WhereClip where, ExpressionClip left, QueryOperator op, ExpressionClip right)
        {
            return where.And(left, op, right);
        }

        public static WhereClip AppendWhereOr(WhereClip where, ExpressionClip left, QueryOperator op, ExpressionClip right)
        {
            return where.Or(left, op, right);
        }

        public static WhereClip CreateCustomWhere(string sql, string[] paramNames, DbType[] paramTypes, object[] paramValues)
        {
            return new WhereClip(sql, paramNames, paramTypes, paramValues);
        }

        public static WhereClip CreateCloneExpression(WhereClip where)
        {
            Check.Require(((object)where) != null, "where could not be null!");

            return (WhereClip)where.Clone();
        }

        #endregion
    }
}
