using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using CN.Teddy.DesignByContract;

namespace NBear.Common
{
    public abstract class SqlQueryUtils
    {
        private SqlQueryUtils() { }

        public static string ToString(QueryOperator op)
        {
            switch (op)
            {
                case QueryOperator.Add:
                    return " + ";
                case QueryOperator.BitwiseAND:
                    return " & ";
                case QueryOperator.BitwiseNOT:
                    return " ~ ";
                case QueryOperator.BitwiseOR:
                    return " | ";
                case QueryOperator.BitwiseXOR:
                    return " ^ ";
                case QueryOperator.Divide:
                    return " / ";
                case QueryOperator.Equal:
                    return " = ";
                case QueryOperator.Greater:
                    return " > ";
                case QueryOperator.GreaterOrEqual:
                    return " >= ";
                case QueryOperator.IsNULL:
                    return " IS NULL ";
                case QueryOperator.Less:
                    return " < ";
                case QueryOperator.LessOrEqual:
                    return " <= ";
                case QueryOperator.Like:
                    return " LIKE ";
                case QueryOperator.Modulo:
                    return " % ";
                case QueryOperator.Multiply:
                    return " * ";
                case QueryOperator.NotEqual:
                    return " <> ";
                case QueryOperator.Subtract:
                    return " - ";
            }

            throw new NotSupportedException("Unknown QueryOperator: " + op.ToString() + "!");
        }

        public static string ToString(IExpression expr)
        {
            return expr == null ? string.Empty : expr.ToString();
        }

        public static void AddParameters(Dictionary<string, KeyValuePair<DbType, object>> targetParameters, IExpression hp)
        {
            Check.Require(targetParameters != null, "targetParameters could not be null!");
            
            if (hp == null || hp.Parameters == null)
            {
                return;
            }

            Dictionary<string, KeyValuePair<DbType, object>>.Enumerator en = hp.Parameters.GetEnumerator();
            while (en.MoveNext())
            {
                if (targetParameters.ContainsKey(en.Current.Key))
                {
                    throw new NameDuplicatedException(en.Current.Key);
                }
                targetParameters.Add(en.Current.Key, en.Current.Value);
            }        
        }

        public static string ReplaceDatabaseTokens(string sql, char leftToken, char rightToken, char paramPrefixToken, char wildcharToken, char wildsinglecharToken)
        {
            string retSql = sql;
            if (leftToken != '[')
            {
                retSql = retSql.Replace("[", leftToken.ToString());
            }
            if (rightToken != ']')
            {
                retSql = retSql.Replace("]", rightToken.ToString());
            }
            if (paramPrefixToken != '@')
            {
                retSql = retSql.Replace("@", paramPrefixToken.ToString());
            }
            if (wildcharToken != '%')
            {
                retSql = retSql.Replace("%", wildcharToken.ToString());
            }
            if (wildsinglecharToken != '_')
            {
                retSql = retSql.Replace("_", wildsinglecharToken.ToString());
            }
            return retSql;
        }

        public static void AppendColumnName(StringBuilder sb, string columnName)
        {
            Check.Require(sb != null, "sb could not be null!");
            Check.Require(columnName != null, "columnName could not be null!");

            if (columnName.Contains("."))
            {
                string[] splittedColumnSections = columnName.Split('.');
                for (int i = 0; i < splittedColumnSections.Length; ++i)
                {
                    if (splittedColumnSections[i] == "*" || splittedColumnSections[i].Contains("(") || splittedColumnSections[i].Contains("["))
                    {
                        sb.Append(splittedColumnSections[i]);
                    }
                    else
                    {
                        sb.Append('[');
                        sb.Append(splittedColumnSections[i]);
                        sb.Append(']');
                    }

                    if (i < splittedColumnSections.Length - 1)
                    {
                        sb.Append('.');
                    }
                }
            }
            else
            {
                if (columnName == "*" || columnName.Contains("(") || columnName.Contains("["))
                {
                    sb.Append(columnName);
                }
                else
                {
                    sb.Append('[');
                    sb.Append(columnName);
                    sb.Append(']');
                }
            }
        }

        public static int GetEndIndexOfMethod(string cmdText, int startIndexOfCharIndex)
        {
            int foundEnd = -1;
            int endIndexOfCharIndex = 0;
            for (int i = startIndexOfCharIndex; i < cmdText.Length; ++i)
            {
                if (cmdText[i] == '(')
                {
                    --foundEnd;
                }
                else if (cmdText[i] == ')')
                {
                    ++foundEnd;
                }

                if (foundEnd == 0)
                {
                    endIndexOfCharIndex = i;
                    break;
                }
            }
            return endIndexOfCharIndex;
        }

        public static string[] SplitTwoParamsOfMethodBody(string bodyText)
        {
            int colonIndex = 0;
            int foundEnd = 0;
            for (int i = 1; i < bodyText.Length - 1; i++)
            {
                if (bodyText[i] == '(')
                {
                    --foundEnd;
                }
                else if (bodyText[i] == ')')
                {
                    ++foundEnd;
                }

                if (bodyText[i] == ',' && foundEnd == 0)
                {
                    colonIndex = i;
                    break;
                }
            }

            return new string[] { bodyText.Substring(0, colonIndex), bodyText.Substring(colonIndex + 1) };
        }

        public static string RemoveTableAliasNamePrefixes(string sql)
        {
            System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex(@"\[([\w\d\s_]+)\].\[([\w\d\s_]+)\]");
            sql = r.Replace(sql, "[$2]");
            return sql;
        }
    }
}
