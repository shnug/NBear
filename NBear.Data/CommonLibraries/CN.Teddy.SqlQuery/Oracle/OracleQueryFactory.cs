using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Data.OracleClient;
using CN.Teddy.DesignByContract;

namespace NBear.Common.Oracle
{
    public class OracleQueryFactory : SqlQueryFactory
    {
        public OracleQueryFactory() : base('\"', '\"', ':', '%', '_', OracleClientFactory.Instance)
        {
        }

        protected override void PrepareCommand(DbCommand cmd)
        {
            base.PrepareCommand(cmd);

            foreach (DbParameter p in cmd.Parameters)
            {
                if (p.Direction == ParameterDirection.Output || p.Direction == ParameterDirection.ReturnValue)
                {
                    continue;
                }

                object value = p.Value;
                if (value == DBNull.Value)
                {
                    continue;
                }
                Type type = value.GetType();
                OracleParameter oracleParam = (OracleParameter)p;

                if (type == typeof(Guid))
                {
                    oracleParam.OracleType = OracleType.Char;
                    oracleParam.Size = 32;
                    continue;
                }

                if ((p.DbType == DbType.Time || p.DbType == DbType.DateTime) && type == typeof(TimeSpan))
                {
                    oracleParam.OracleType = OracleType.Double;
                    oracleParam.Value = ((TimeSpan)value).TotalDays;
                    continue;
                }

                switch (p.DbType)
                {
                    case DbType.Binary:
                        if (((byte[])value).Length > 2000)
                        {
                            oracleParam.OracleType = OracleType.Blob;
                        }
                        break;
                    case DbType.Time:
                        oracleParam.OracleType = OracleType.DateTime;
                        break;
                    case DbType.DateTime:
                        oracleParam.OracleType = OracleType.DateTime;
                        break;
                    case DbType.AnsiString:
                        if (value.ToString().Length > 4000)
                        {
                            oracleParam.OracleType = OracleType.Clob;
                        }
                        break;
                    case DbType.String:
                        if (value.ToString().Length > 2000)
                        {
                            oracleParam.OracleType = OracleType.NClob;
                        }
                        break;
                    case DbType.Object:
                        oracleParam.OracleType = OracleType.NClob;
                        p.Value = CN.Teddy.Common.SerializationManager.Instance.Serialize(value);
                        break;
                }
            }

            //replace oracle specific function names in cmd.CommandText
            cmd.CommandText = cmd.CommandText
                .Replace("LEN(", "LENGTH(")
                .Replace("SUBSTRING(", "SUBSTR(")
                .Replace("GETDATE()", "TO_CHAR(CURRENT_DATE,'DD-MON-YYYY HH:MI:SS')");

            //replace CHARINDEX with INSTR and reverse seqeunce of param items in CHARINDEX()
            int startIndexOfCharIndex = cmd.CommandText.IndexOf("CHARINDEX(");
            while (startIndexOfCharIndex > 0)
            {
                int endIndexOfCharIndex = SqlQueryUtils.GetEndIndexOfMethod(cmd.CommandText, startIndexOfCharIndex + "CHARINDEX(".Length);
                string[] itemsInCharIndex = SqlQueryUtils.SplitTwoParamsOfMethodBody(
                    cmd.CommandText.Substring(startIndexOfCharIndex + "CHARINDEX(".Length, 
                    endIndexOfCharIndex - startIndexOfCharIndex - "CHARINDEX(".Length));
                cmd.CommandText = cmd.CommandText.Substring(0, startIndexOfCharIndex) 
                    + "INSTR(" + itemsInCharIndex[1] + "," + itemsInCharIndex[0] + ")" 
                    + (cmd.CommandText.Length - 1 > endIndexOfCharIndex ? 
                    cmd.CommandText.Substring(endIndexOfCharIndex + 1) : string.Empty);

                startIndexOfCharIndex = cmd.CommandText.IndexOf("CHARINDEX(");
            }

            //replace DATEPART with TO_CHAR(CURRENT_DATE,'XXXX')
            startIndexOfCharIndex = cmd.CommandText.IndexOf("DATEPART(");
            if (startIndexOfCharIndex > 0)
            {
                cmd.CommandText = cmd.CommandText                
                    .Replace("DATEPART(Year", "TO_CHAR('YYYY'")
                    .Replace("DATEPART(Month", "TO_CHAR('MM'")
                    .Replace("DATEPART(Day", "TO_CHAR('DD'");

                startIndexOfCharIndex = cmd.CommandText.IndexOf("TO_CHAR(");
                while (startIndexOfCharIndex > 0)
                {
                    int endIndexOfCharIndex = SqlQueryUtils.GetEndIndexOfMethod(cmd.CommandText, startIndexOfCharIndex + "TO_CHAR(".Length);
                    string[] itemsInCharIndex = SqlQueryUtils.SplitTwoParamsOfMethodBody(
                        cmd.CommandText.Substring(startIndexOfCharIndex + "TO_CHAR(".Length, 
                        endIndexOfCharIndex - startIndexOfCharIndex - "TO_CHAR(".Length));
                    cmd.CommandText = cmd.CommandText.Substring(0, startIndexOfCharIndex) 
                        + "TO_CHAR(" + itemsInCharIndex[1] + "," + itemsInCharIndex[0] + ")" 
                        + (cmd.CommandText.Length - 1 > endIndexOfCharIndex ? 
                        cmd.CommandText.Substring(endIndexOfCharIndex + 1) : string.Empty);

                    startIndexOfCharIndex = cmd.CommandText.IndexOf("TO_CHAR(");
                }
            }
        }

        public override DbCommand CreateSelectRangeCommand(WhereClip where, string[] columns, int topCount, int skipCount, string identyColumn, bool identyColumnIsNumber)
        {
            Check.Require(((object)where) != null && where.From != null, "expr and expr.From could not be null!");
            Check.Require(columns != null && columns.Length > 0, "columns could not be null or empty!");
            Check.Require(topCount > 0, "topCount must > 0!");

            if (string.IsNullOrEmpty(where.OrderBy) && identyColumn != null)
            {
                where.SetOrderBy(new KeyValuePair<string,bool>[] { new KeyValuePair<string,bool>(identyColumn, false) });
            }

            if (topCount == int.MaxValue && skipCount == 0)
            {
                return CreateSelectCommand(where, columns);
            }
            else
            {
                Check.Require(!string.IsNullOrEmpty(identyColumn), "identyColumn could not be null or empty!");

                identyColumn = ColumnFormatter.ValidColumnName(identyColumn);

                //page split algorithm using ROW_NUMBER() in Oracle9+

                DbCommand cmd = fac.CreateCommand();
                cmd.CommandType = CommandType.Text;

                StringBuilder sb = new StringBuilder();
                sb.Append("SELECT ");
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
                sb.Append(" FROM (");
                sb.Append("SELECT ");
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
                sb.Append(") [__T] WHERE [__T].[__Pos]>");
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
}
