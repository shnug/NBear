using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;
using CN.Teddy.DesignByContract;

namespace NBear.Common.SqlServer
{
    public class SqlServer9QueryFactory : SqlServerQueryFactory
    {
        protected override System.Data.Common.DbCommand CreateSelectRangeCommandForUnsortedRows(WhereClip where, string[] columns, int topCount, int skipCount, string identyColumn)
        {
            //page split algorithm using ROW_NUMBER() in SqlServer 2005

            DbCommand cmd = fac.CreateCommand();
            cmd.CommandType = CommandType.Text;

            StringBuilder sb = new StringBuilder("WITH [__T] AS (SELECT ");
            if (topCount < int.MaxValue && (int.MaxValue - topCount > skipCount))
            {
                sb.Append("TOP ");
                sb.Append(topCount + skipCount);
                sb.Append(' ');
            }
            for (int i = 0; i < columns.Length; ++i)
            {
                SqlQueryUtils.AppendColumnName(sb, columns[i]);
                sb.Append(" AS [__C");
                sb.Append(i);
                sb.Append(']');

                if (i < columns.Length - 1)
                {
                    sb.Append(',');
                }
            }
            sb.Append(",ROW_NUMBER() OVER (ORDER BY ");
            if (string.IsNullOrEmpty(where.OrderBy))
            {
                sb.Append(identyColumn);
            }
            else
            {
                sb.Append(where.OrderBy);
            }
            sb.Append(") AS [__Pos]");
            sb.Append(" FROM ");

            if (string.IsNullOrEmpty(where.OrderBy))
            {
                sb.Append(where.ToString());
            }
            else
            {
                lock (where)
                {
                    string tempOrderBy = where.OrderBy;
                    where.OrderBy = null;
                    sb.Append(where.ToString());
                    where.OrderBy = tempOrderBy;
                }
            }
            sb.Append(") SELECT ");
            for (int i = 0; i < columns.Length; ++i)
            {
                sb.Append("[__T].[__C");
                sb.Append(i);
                sb.Append(']');

                if (i < columns.Length - 1)
                {
                    sb.Append(',');
                }
            }
            sb.Append(" FROM [__T] WHERE [__T].[__Pos]>");
            sb.Append(skipCount);
            if (topCount < int.MaxValue && (int.MaxValue - topCount > skipCount))
            {
                sb.Append(" AND [__T].[__Pos]<=");
                sb.Append(topCount + skipCount);
                sb.Append(' ');
            }

            AddExpressionParameters(where, cmd);

            cmd.CommandText = SqlQueryUtils.ReplaceDatabaseTokens(sb.ToString(), leftToken, rightToken, paramPrefixToken, wildcharToken, wildsinglecharToken);
            PrepareCommand(cmd);
            return cmd;    
        }
    }
}
