using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using CN.Teddy.DesignByContract;

namespace NBear.Common.MsAccess
{
    public class MsAccessQueryFactory : SqlQueryFactory
    {
        public MsAccessQueryFactory() : base('[', ']', '@', '%', '_', OleDbFactory.Instance)
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
                OleDbParameter oleDbParam = (OleDbParameter)p;

                if (type == typeof(Guid))
                {
                    oleDbParam.OleDbType = OleDbType.Char;
                    oleDbParam.Size = 32;
                    continue;
                }

                if ((p.DbType == DbType.Time || p.DbType == DbType.DateTime) && type == typeof(TimeSpan))
                {
                    oleDbParam.OleDbType = OleDbType.Double;
                    oleDbParam.Value = ((TimeSpan)value).TotalDays;
                    continue;
                }

                switch (p.DbType)
                {
                    case DbType.Binary:
                        if (((byte[])value).Length > 2000)
                        {
                            oleDbParam.OleDbType = OleDbType.LongVarBinary;
                        }
                        break;
                    case DbType.Time:
                        oleDbParam.OleDbType = OleDbType.LongVarWChar;
                        p.Value = value.ToString();
                        break;
                    case DbType.DateTime:
                        oleDbParam.OleDbType = OleDbType.LongVarWChar;
                        p.Value = value.ToString();
                        break;
                    case DbType.AnsiString:
                        if (value.ToString().Length > 4000)
                        {
                            oleDbParam.OleDbType = OleDbType.LongVarChar;
                        }
                        break;
                    case DbType.String:
                        if (value.ToString().Length > 2000)
                        {
                            oleDbParam.OleDbType = OleDbType.LongVarWChar;
                        }
                        break;
                    case DbType.Object:
                        oleDbParam.OleDbType = OleDbType.LongVarWChar;
                        p.Value = CN.Teddy.Common.SerializationManager.Instance.Serialize(value);
                        break;
                }
            }

            //replace msaccess specific function names in cmd.CommandText
            cmd.CommandText = cmd.CommandText
                .Replace("UPPER(", "UCASE(")
                .Replace("LOWER(", "LCASE(")
                .Replace("SUBSTRING(", "MID(")
                .Replace("GETDATE()", "DATE() + TIME()")
                .Replace("DATEPART(Year", "DATEPART('yyyy'")
                .Replace("DATEPART(Month", "DATEPART('m'")
                .Replace("DATEPART(Day", "DATEPART('d'");

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
            else if (skipCount == 0)
            {
                return CreateSelectTopCommand(where, columns, topCount);
            }
            else
            {
                Check.Require(!string.IsNullOrEmpty(identyColumn), "identyColumn could not be null or empty!");

                identyColumn = ColumnFormatter.ValidColumnName(identyColumn);

                if (identyColumnIsNumber && where.OrderByStartsWith(identyColumn) && (string.IsNullOrEmpty(where.GroupBy) || where.GroupBy == identyColumn))
                {
                    return CreateSelectRangeCommandForSortedRows(where, columns, topCount, skipCount, identyColumn, where.OrderByStartsWith(identyColumn + " DESC"));
                }
                else
                {
                    return CreateSelectRangeCommandForUnsortedRows(where, columns, topCount, skipCount, identyColumn);
                }
            }
        }

        protected virtual DbCommand CreateSelectTopCommand(WhereClip where, string[] columns, int topCount)
        {
            DbCommand cmd = fac.CreateCommand();
            cmd.CommandType = CommandType.Text;

            StringBuilder sb = new StringBuilder("SELECT TOP ");
            sb.Append(topCount);
            sb.Append(' ');
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

        protected virtual DbCommand CreateSelectRangeCommandForSortedRows(WhereClip where, string[] columns, int topCount, int skipCount, string identyColumn, bool isIdentyColumnDesc)
        {
            //SELECT TOP 10 *
            //FROM TestTable
            //WHERE (ID >
            //          (SELECT MAX(id)/MIN(id)
            //         FROM (SELECT TOP 20 id
            //                 FROM TestTable
            //                 ORDER BY id) AS T))
            //ORDER BY ID

            DbCommand cmd = fac.CreateCommand();
            cmd.CommandType = CommandType.Text;

            StringBuilder sb = new StringBuilder("SELECT ");
            if (topCount < int.MaxValue)
            {
                sb.Append("TOP ");
                sb.Append(topCount);
                sb.Append(' ');
            }
            for (int i = 0; i < columns.Length; ++i)
            {
                SqlQueryUtils.AppendColumnName(sb, columns[i]);

                if (i < columns.Length - 1)
                {
                    sb.Append(',');
                }
            }
            sb.Append(" FROM ");

            WhereClip cloneWhere = (WhereClip)where.Clone();

            #region Construct & extend CloneWhere

            StringBuilder sbInside = new StringBuilder();
            sbInside.Append(identyColumn);
            sbInside.Append(isIdentyColumnDesc ? '<' : '>');
            sbInside.Append('(');
            sbInside.Append("SELECT ");
            sbInside.Append(isIdentyColumnDesc ? "MIN(" : "MAX(");
            sbInside.Append("[__T].");
            string[] splittedIdentyColumn = identyColumn.Split('.');
            sbInside.Append(splittedIdentyColumn[splittedIdentyColumn.Length - 1]);
            sbInside.Append(") FROM (SELECT TOP ");
            sbInside.Append(skipCount);
            sbInside.Append(' ');
            sbInside.Append(identyColumn);
            sbInside.Append(" AS ");
            sbInside.Append(splittedIdentyColumn[splittedIdentyColumn.Length - 1]);
            sbInside.Append(" FROM ");
            sbInside.Append(where.ToString());
            sbInside.Append(") [__T])");

            if (cloneWhere.Sql.Length == 0)
            {
                cloneWhere.Sql = sbInside.ToString();
            }
            else
            {
                cloneWhere.Sql = "(" + cloneWhere.Sql.ToString() + ") AND " + sbInside.ToString();
            }

            #endregion

            sb.Append(cloneWhere.ToString());

            AddExpressionParameters(where, cmd);
            AddExpressionParameters(cloneWhere, cmd);

            cmd.CommandText = SqlQueryUtils.ReplaceDatabaseTokens(sb.ToString(), leftToken, rightToken, paramPrefixToken, wildcharToken, wildsinglecharToken);
            PrepareCommand(cmd);
            return cmd;            
        }

        protected virtual DbCommand CreateSelectRangeCommandForUnsortedRows(WhereClip where, string[] columns, int topCount, int skipCount, string identyColumn)
        {
            //SELECT TOP 10 *
            //FROM TestTable
            //WHERE (ID NOT IN
            //          (SELECT TOP 20 id
            //         FROM TestTable
            //         ORDER BY id))
            //ORDER BY ID

            DbCommand cmd = fac.CreateCommand();
            cmd.CommandType = CommandType.Text;

            StringBuilder sb = new StringBuilder("SELECT ");
            if (topCount < int.MaxValue)
            {
                sb.Append("TOP ");
                sb.Append(topCount);
                sb.Append(' ');
            }
            for (int i = 0; i < columns.Length; ++i)
            {
                SqlQueryUtils.AppendColumnName(sb, columns[i]);

                if (i < columns.Length - 1)
                {
                    sb.Append(',');
                }
            }
            sb.Append(" FROM ");

            WhereClip cloneWhere = (WhereClip)where.Clone();

            #region Construct & extend CloneWhere

            StringBuilder sbInside = new StringBuilder();
            sbInside.Append(identyColumn);
            sbInside.Append(" NOT IN (SELECT TOP ");
            sbInside.Append(skipCount);
            sbInside.Append(' ');
            sbInside.Append(identyColumn);
            sbInside.Append(" FROM ");
            sbInside.Append(where.ToString());
            sbInside.Append(")");

            if (cloneWhere.Sql.Length == 0)
            {
                cloneWhere.Sql = sbInside.ToString();
            }
            else
            {
                cloneWhere.Sql = "(" + cloneWhere.Sql.ToString() + ") AND " + sbInside.ToString();
            }

            #endregion

            sb.Append(cloneWhere.ToString());

            AddExpressionParameters(where, cmd);
            AddExpressionParameters(cloneWhere, cmd);

            cmd.CommandText = SqlQueryUtils.ReplaceDatabaseTokens(sb.ToString(), leftToken, rightToken, paramPrefixToken, wildcharToken, wildsinglecharToken);
            PrepareCommand(cmd);
            return cmd;    
        }
    }
}
