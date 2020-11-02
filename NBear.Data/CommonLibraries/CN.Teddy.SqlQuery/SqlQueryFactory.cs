using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;
using CN.Teddy.DesignByContract;

namespace NBear.Common
{
    public abstract class SqlQueryFactory : NBear.Common.ISqlQueryFactory
    {
        #region Protected Members

        protected readonly char leftToken;
        protected readonly char rightToken;
        protected readonly char paramPrefixToken;
        protected readonly char wildcharToken;
        protected readonly char wildsinglecharToken;
        protected readonly DbProviderFactory fac;

        protected virtual void PrepareCommand(DbCommand cmd)
        {
            Check.Require(cmd != null && (!string.IsNullOrEmpty(cmd.CommandText)), "cmd could not be null or empty!");

            foreach (DbParameter p in cmd.Parameters)
            {
                if (p.Direction == ParameterDirection.Output || p.Direction == ParameterDirection.ReturnValue)
                {
                    continue;
                }

                object value = p.Value;
                DbType dbType = p.DbType;

                if (value == DBNull.Value)
                {
                    continue;
                }

                if (value == null)
                {
                    p.Value = DBNull.Value;
                    continue;
                }

                Type type = value.GetType();

                if (type.IsEnum)
                {
                    p.DbType = DbType.Int32;
                    p.Value = Convert.ToInt32(value);
                    continue;
                }
                
                if (dbType == DbType.Guid && type != typeof(Guid))
                {
                    p.Value = new Guid(value.ToString());
                    continue;
                }

                if ((dbType == DbType.AnsiString || dbType == DbType.String) && (!(value is string)))
                {
                    p.Value = CN.Teddy.Common.SerializationManager.Instance.Serialize(value);
                    continue;
                }

                if (type == typeof(Boolean))
                {
                    p.Value = (((bool)value) ? 1 : 0);
                    continue;
                }
            }
        }

        protected void AddExpressionParameters(IExpression expr, DbCommand cmd)
        {
            Check.Require(expr != null, "expr could not be null!");
            Check.Require(cmd != null, "cmd could not be null!");

            Dictionary<string, KeyValuePair<DbType, object>>.Enumerator en = expr.Parameters.GetEnumerator();

            while (en.MoveNext())
            {
                DbParameter p = cmd.CreateParameter();
                p.ParameterName = paramPrefixToken + en.Current.Key.TrimStart(paramPrefixToken);
                p.DbType = en.Current.Value.Key;
                p.Value = en.Current.Value.Value == null ? DBNull.Value : en.Current.Value.Value;
                cmd.Parameters.Add(p);
            }
        }

        protected string MakeUniqueParamNameWithPrefixToken()
        {
            return CN.Teddy.Common.CommonUtils.MakeUniqueKey(16, paramPrefixToken + "p");
        }

        protected void AddInputParametersToCommand(string[] paramNames, DbType[] paramTypes, object[] paramValues, DbCommand cmd)
        {
            if (paramNames != null)
            {
                for (int i = 0; i < paramNames.Length; ++i)
                {
                    DbParameter p = cmd.CreateParameter();
                    p.ParameterName = paramPrefixToken + paramNames[i].TrimStart(paramPrefixToken);
                    p.DbType = paramTypes[i];
                    p.Value = paramValues[i] == null ? DBNull.Value : paramValues[i];
                    cmd.Parameters.Add(p);
                }
            }
        }

        protected void AddOutputParametersToCommand(string[] paramNames, DbType[] paramTypes, int[] paramSizes, DbCommand cmd)
        {
            if (paramNames != null)
            {
                for (int i = 0; i < paramNames.Length; ++i)
                {
                    DbParameter p = cmd.CreateParameter();
                    p.ParameterName = paramPrefixToken + paramNames[i].TrimStart(paramPrefixToken);
                    p.Direction = ParameterDirection.Output;
                    p.Size = paramSizes[i];
                    p.DbType = paramTypes[i];
                    cmd.Parameters.Add(p);
                }
            }
        }

        protected void AddInputOutputParametersToCommand(string[] paramNames, DbType[] paramTypes, int[] paramSizes, object[] paramValues, DbCommand cmd)
        {
            if (paramNames != null)
            {
                for (int i = 0; i < paramNames.Length; ++i)
                {
                    DbParameter p = cmd.CreateParameter();
                    p.ParameterName = paramPrefixToken + paramNames[i].TrimStart(paramPrefixToken);
                    p.Direction = ParameterDirection.InputOutput;
                    p.Size = paramSizes[i];
                    p.DbType = paramTypes[i];
                    p.Value = paramValues[i] == null ? DBNull.Value : paramValues[i];
                    cmd.Parameters.Add(p);
                }
            }
        }

        protected void AddReturnValueParameterToCommand(string paramName, DbType paramType, int paramSize, DbCommand cmd)
        {
            if (!string.IsNullOrEmpty(paramName))
            {
                DbParameter p = cmd.CreateParameter();
                p.ParameterName = paramPrefixToken + paramName.TrimStart(paramPrefixToken);
                p.Direction = ParameterDirection.Output;
                p.Size = paramSize;
                p.DbType = paramType;
                cmd.Parameters.Add(p);
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlQueryFactory"/> class.
        /// The default factory generates TSQL for MS SQL database
        /// </summary>
        public SqlQueryFactory() : this('[', ']', '@', '%', '_', System.Data.SqlClient.SqlClientFactory.Instance)
        {
        }

        public SqlQueryFactory(char leftToken, char rightToken, char paramPrefixToken, char wildcharToken, char wildsinglecharToken, DbProviderFactory fac)
        {
            Check.Require(fac != null, "fac could not be null!");

            this.leftToken = leftToken;
            this.rightToken = rightToken;
            this.paramPrefixToken = paramPrefixToken;
            this.wildcharToken = wildcharToken;
            this.wildsinglecharToken = wildsinglecharToken;
            this.fac = fac;
        }

        #endregion

        #region Query Commands

        public DbCommand CreateInsertCommand(string tableName, string[] columns, DbType[] types, object[] values)
        {
            Check.Require(!string.IsNullOrEmpty(tableName), "tableName could not be null or empty!");
            Check.Require(columns == null || 
                (columns != null && types != null && values != null && columns.Length == types.Length && columns.Length == values.Length),
                "length of columns, types and values must equal!");

            DbCommand cmd = fac.CreateCommand();
            cmd.CommandType = CommandType.Text;

            StringBuilder sb = new StringBuilder("INSERT INTO ");
            sb.Append(leftToken);
            sb.Append(tableName.TrimStart(leftToken).TrimEnd(rightToken));
            sb.Append(rightToken);
            sb.Append(' ');
            if (columns == null || columns.Length == 0)
            {
                sb.Append("DEFAULT VALUES");
            }
            else
            {
                sb.Append('(');
                for (int i = 0; i < columns.Length; ++i)
                {
                    sb.Append(leftToken);
                    sb.Append(columns[i].TrimStart(leftToken).TrimEnd(rightToken));
                    sb.Append(rightToken);
                    if (i < columns.Length - 1)
                    {
                        sb.Append(',');
                    }
                }
                sb.Append(") VALUES (");
                for (int i = 0; i < columns.Length; ++i)
                {
                    string paramName = MakeUniqueParamNameWithPrefixToken();
                    sb.Append(paramName);
                    if (i < columns.Length - 1)
                    {
                        sb.Append(',');
                    }

                    DbParameter p = cmd.CreateParameter();
                    p.ParameterName = paramName;
                    p.DbType = types[i];
                    p.Value = values[i] == null ? DBNull.Value : values[i];
                    cmd.Parameters.Add(p);
                }
                sb.Append(')');
            }

            cmd.CommandText = SqlQueryUtils.ReplaceDatabaseTokens(sb.ToString(), leftToken, rightToken, paramPrefixToken, wildcharToken, wildsinglecharToken);
            PrepareCommand(cmd);
            return cmd;
        }

        public DbCommand CreateUpdateCommand(string tableName, WhereClip where, string[] columns, DbType[] types, object[] values)
		{
            Check.Require(!string.IsNullOrEmpty(tableName), "tableName could not be null or empty!");
            //if (where != null)
            //{
            //    Check.Require(where.From == null, "expr.From must be null in Update");
            //}
            Check.Require(columns != null && types != null && values != null && columns.Length == types.Length && columns.Length == values.Length,
                "length of columns, types and values must equal!");

            DbCommand cmd = fac.CreateCommand();
            cmd.CommandType = CommandType.Text;

            StringBuilder sb = new StringBuilder("UPDATE ");
            sb.Append(leftToken);
            sb.Append(tableName.TrimStart(leftToken).TrimEnd(rightToken));
            sb.Append(rightToken);
            sb.Append(' ');
            sb.Append("SET ");
            for (int i = 0; i < columns.Length; ++i)
            {
                sb.Append(leftToken);
                sb.Append(columns[i].TrimStart(leftToken).TrimEnd(rightToken));
                sb.Append(rightToken);
                sb.Append('=');
                if (values[i] != null && values[i] is ExpressionClip)
                {
                    ExpressionClip expr = (ExpressionClip)values[i];
                    sb.Append(expr.ToString());
                    AddExpressionParameters(expr, cmd);
                }
                else
                {
                    string paramName = MakeUniqueParamNameWithPrefixToken();
                    sb.Append(paramName);
                    DbParameter p = cmd.CreateParameter();
                    p.ParameterName = paramName;
                    p.DbType = types[i];
                    p.Value = values[i] == null ? DBNull.Value : values[i];
                    cmd.Parameters.Add(p);
                }

                if (i < columns.Length - 1)
                {
                    sb.Append(',');
                }
            }

            if ((!WhereClip.IsNullOrEmpty(where)) && where.Sql.Length > 0)
            {
                sb.Append(" WHERE ");
                sb.Append(SqlQueryUtils.RemoveTableAliasNamePrefixes(where.Sql));
                AddExpressionParameters(where, cmd);
            }

            cmd.CommandText = SqlQueryUtils.ReplaceDatabaseTokens(sb.ToString(), leftToken, rightToken, paramPrefixToken, wildcharToken, wildsinglecharToken);
            PrepareCommand(cmd);
            return cmd;
		}

        public DbCommand CreateDeleteCommand(string tableName, WhereClip where)
		{
            Check.Require(!string.IsNullOrEmpty(tableName), "tableName could not be null or empty!");
            //if (where != null)
            //{
            //    Check.Require(where.From == null, "expr.From must be null in Delete");
            //}

            DbCommand cmd = fac.CreateCommand();
            cmd.CommandType = CommandType.Text;

            StringBuilder sb = new StringBuilder("DELETE FROM ");
            sb.Append(leftToken);
            sb.Append(tableName.TrimStart(leftToken).TrimEnd(rightToken));
            sb.Append(rightToken);

            if ((!WhereClip.IsNullOrEmpty(where)) && where.Sql.Length > 0)
            {
                sb.Append(" WHERE ");
                sb.Append(SqlQueryUtils.RemoveTableAliasNamePrefixes(where.Sql));
                AddExpressionParameters(where, cmd);
            }

            cmd.CommandText = SqlQueryUtils.ReplaceDatabaseTokens(sb.ToString(), leftToken, rightToken, paramPrefixToken, wildcharToken, wildsinglecharToken);
            PrepareCommand(cmd);
            return cmd;
		}

        public DbCommand CreateSelectCommand(WhereClip where, string[] columns)
        {
            Check.Require(((object)where) != null && where.From != null, "expr and expr.From could not be null!");
            Check.Require(columns != null && columns.Length > 0, "columns could not be null or empty!");

            DbCommand cmd = fac.CreateCommand();
            cmd.CommandType = CommandType.Text;

            StringBuilder sb = new StringBuilder("SELECT ");
            for (int i = 0; i < columns.Length; ++i)
            {
                SqlQueryUtils.AppendColumnName(sb, columns[i]);

                if (i < columns.Length - 1)
                {
                    sb.Append(',');
                }
            }
            sb.Append(" FROM ");
            sb.Append(where.ToString());

            AddExpressionParameters(where, cmd);

            cmd.CommandText = SqlQueryUtils.ReplaceDatabaseTokens(sb.ToString(), leftToken, rightToken, paramPrefixToken, wildcharToken, wildsinglecharToken);
            PrepareCommand(cmd);
            return cmd;
        }

        public abstract DbCommand CreateSelectRangeCommand(WhereClip where, string[] columns, int topCount, int skipCount, string identyColumn, bool identyColumnIsNumber);

        public DbCommand CreateCustomSqlCommand(string sql, string[] paramNames, DbType[] paramTypes, object[] paramValues)
        {
            Check.Require(!string.IsNullOrEmpty(sql), "sql could not be null or empty!");
            Check.Require(paramNames == null || 
                (paramNames != null && paramTypes != null && paramValues != null &&
                paramNames.Length == paramTypes.Length && paramNames.Length == paramValues.Length), 
                "length of paramNames, paramTypes and paramValues should equal!");

            DbCommand cmd = fac.CreateCommand();
            cmd.CommandType = CommandType.Text;

            AddInputParametersToCommand(paramNames, paramTypes, paramValues, cmd);

            cmd.CommandText = SqlQueryUtils.ReplaceDatabaseTokens(sql, leftToken, rightToken, paramPrefixToken, wildcharToken, wildsinglecharToken);
            PrepareCommand(cmd);
            return cmd;
        }

        public DbCommand CreateStoredProcedureCommand(string procedureName, string[] paramNames, DbType[] paramTypes, object[] paramValues)
        {
            Check.Require(!string.IsNullOrEmpty(procedureName), "procedureName could not be null or empty!");
            Check.Require(paramNames == null || 
                (paramNames != null && paramTypes != null && paramValues != null &&
                paramNames.Length == paramTypes.Length && paramNames.Length == paramValues.Length), 
                "length of paramNames, paramTypes and paramValues should equal!");

            DbCommand cmd = fac.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = procedureName;

            AddInputParametersToCommand(paramNames, paramTypes, paramValues, cmd);

            PrepareCommand(cmd);
            return cmd;
        }

        public DbCommand CreateStoredProcedureCommand(string procedureName, string[] paramNames, DbType[] paramTypes, object[] paramValues, 
            string[] outParamNames, DbType[] outParamTypes, int[] outParamSizes)
        {
            Check.Require(!string.IsNullOrEmpty(procedureName), "procedureName could not be null or empty!");
            Check.Require(paramNames == null || 
                (paramNames != null && paramTypes != null && paramValues != null &&
                paramNames.Length == paramTypes.Length && paramNames.Length == paramValues.Length), 
                "length of paramNames, paramTypes and paramValues should equal!");
            Check.Require(outParamNames == null || 
                (outParamNames != null && outParamTypes != null && outParamSizes != null &&
                outParamNames.Length == outParamTypes.Length && outParamNames.Length == outParamSizes.Length), 
                "length of outParamNames, outParamTypes and outParamSizes should equal!");

            DbCommand cmd = fac.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = procedureName;

            AddInputParametersToCommand(paramNames, paramTypes, paramValues, cmd);
            AddOutputParametersToCommand(outParamNames, outParamTypes, outParamSizes, cmd);

            PrepareCommand(cmd);
            return cmd;
        }

        public DbCommand CreateStoredProcedureCommand(string procedureName, string[] paramNames, DbType[] paramTypes, object[] paramValues, 
            string[] outParamNames, DbType[] outParamTypes, int[] outParamSizes, 
            string[] inOutParamNames, DbType[] inOutParamTypes, int[] inOutParamSizes, object[] inOutParamValues)
        {
            Check.Require(!string.IsNullOrEmpty(procedureName), "procedureName could not be null or empty!");
            Check.Require(paramNames == null || 
                (paramNames != null && paramTypes != null && paramValues != null &&
                paramNames.Length == paramTypes.Length && paramNames.Length == paramValues.Length), 
                "length of paramNames, paramTypes and paramValues should equal!");
            Check.Require(outParamNames == null || 
                (outParamNames != null && outParamTypes != null && outParamSizes != null &&
                outParamNames.Length == outParamTypes.Length && outParamNames.Length == outParamSizes.Length), 
                "length of outParamNames, outParamTypes and outParamSizes should equal!");
            Check.Require(inOutParamNames == null || 
                (inOutParamNames != null && inOutParamTypes != null && inOutParamSizes != null && inOutParamValues != null &&
                inOutParamNames.Length == inOutParamTypes.Length && inOutParamNames.Length == inOutParamSizes.Length && inOutParamNames.Length == inOutParamValues.Length), 
                "length of inOutParamNames, inOutParamTypes, inOutParamSizes and inOutParamValues should equal!");

            DbCommand cmd = fac.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = procedureName;

            AddInputParametersToCommand(paramNames, paramTypes, paramValues, cmd);
            AddOutputParametersToCommand(outParamNames, outParamTypes, outParamSizes, cmd);
            AddInputOutputParametersToCommand(inOutParamNames, inOutParamTypes, inOutParamSizes, inOutParamValues, cmd);

            PrepareCommand(cmd);
            return cmd;
        }

        public DbCommand CreateStoredProcedureCommand(string procedureName, string[] paramNames, DbType[] paramTypes, object[] paramValues, 
            string[] outParamNames, DbType[] outParamTypes, int[] outParamSizes, 
            string[] inOutParamNames, DbType[] inOutParamTypes, int[] inOutParamSizes, object[] inOutParamValues, 
            string returnValueParamName, DbType returnValueParamType, int returnValueParamSize)
        {
            Check.Require(!string.IsNullOrEmpty(procedureName), "procedureName could not be null or empty!");
            Check.Require(paramNames == null || 
                (paramNames != null && paramTypes != null && paramValues != null &&
                paramNames.Length == paramTypes.Length && paramNames.Length == paramValues.Length), 
                "length of paramNames, paramTypes and paramValues should equal!");
            Check.Require(outParamNames == null || 
                (outParamNames != null && outParamTypes != null && outParamSizes != null &&
                outParamNames.Length == outParamTypes.Length && outParamNames.Length == outParamSizes.Length), 
                "length of outParamNames, outParamTypes and outParamSizes should equal!");
            Check.Require(inOutParamNames == null || 
                (inOutParamNames != null && inOutParamTypes != null && inOutParamSizes != null && inOutParamValues != null &&
                inOutParamNames.Length == inOutParamTypes.Length && inOutParamNames.Length == inOutParamSizes.Length && inOutParamNames.Length == inOutParamValues.Length), 
                "length of inOutParamNames, inOutParamTypes, inOutParamSizes and inOutParamValues should equal!");

            DbCommand cmd = fac.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = procedureName;

            AddInputParametersToCommand(paramNames, paramTypes, paramValues, cmd);
            AddOutputParametersToCommand(outParamNames, outParamTypes, outParamSizes, cmd);
            AddInputOutputParametersToCommand(inOutParamNames, inOutParamTypes, inOutParamSizes, inOutParamValues, cmd);
            AddReturnValueParameterToCommand(returnValueParamName, returnValueParamType, returnValueParamSize, cmd);

            PrepareCommand(cmd);
            return cmd;
        }

        #endregion
    }
}
