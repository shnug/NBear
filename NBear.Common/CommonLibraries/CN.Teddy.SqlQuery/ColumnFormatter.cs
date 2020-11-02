using System;
using System.Collections.Generic;
using System.Text;
using CN.Teddy.DesignByContract;

namespace NBear.Common
{
    public abstract class ColumnFormatter
    {
        private ColumnFormatter() { }

        #region Private Members

        private static string Func(string funcName, string columnName)
        {
            Check.Require(!string.IsNullOrEmpty(funcName), "funcName could not be null or empty!");
            Check.Require(!string.IsNullOrEmpty(columnName), "columnName could not be null or empty!");

            StringBuilder sb = new StringBuilder(funcName);
            sb.Append("(");
            SqlQueryUtils.AppendColumnName(sb, columnName);
            sb.Append(')');
            return sb.ToString();
        }

        #endregion

        #region Aggregate

        public static string Count(string columnName)
        {
            return Count(columnName, false);
        }

        public static string Count(string columnName, bool isDistinct)
        {
            Check.Require(!string.IsNullOrEmpty(columnName), "columnName could not be null or empty!");

            StringBuilder sb = new StringBuilder();
            sb.Append("COUNT(");
            if (isDistinct)
            {
                sb.Append("DISTINCT ");
            }
            SqlQueryUtils.AppendColumnName(sb, columnName);
            sb.Append(')');
            return sb.ToString();
        }

        public static string Sum(string columnName)
        {
            return Func("SUM", columnName);
        }

        public static string Min(string columnName)
        {
            return Func("MIN", columnName);
        }

        public static string Max(string columnName)
        {
            return Func("MAX", columnName);
        }

        public static string Avg(string columnName)
        {
            return Func("AVG", columnName);
        }

        #endregion

        #region String & Date Functions

        public static string ValidColumnName(string columnName)
        {
            Check.Require(columnName != null, "columnName could not be null!");

            StringBuilder sb = new StringBuilder();
            SqlQueryUtils.AppendColumnName(sb, columnName);
            return sb.ToString();
        }

        public static string Length(string columnName)
        {
            return Func("LEN", columnName);
        }

        public static string ToUpper(string columnName)
        {
            return Func("UPPER", columnName);
        }

        public static string ToLower(string columnName)
        {
            return Func("LOWER", columnName);
        }

        public static string Trim(string columnName)
        {
            return Func("LTRIM", Func("(RTRIM", columnName));
        }

        //public static string SubString(string columnName, int start)
        //{
        //    Check.Require(!string.IsNullOrEmpty(columnName), "columnName could not be null or empty!");
        //    Check.Require(start >= 0, "start must >= 0!");

        //    StringBuilder sb = new StringBuilder("SUBSTRING(");
        //    SqlQueryUtils.AppendColumnName(sb, columnName);
        //    sb.Append(',');
        //    sb.Append(start + 1);
        //    sb.Append(",LEN(");
        //    SqlQueryUtils.AppendColumnName(sb, columnName);
        //    sb.Append(')');
        //    sb.Append(')');
        //    return sb.ToString();
        //}

        public static string SubString(string columnName, int start, int length)
        {
            Check.Require(!string.IsNullOrEmpty(columnName), "columnName could not be null or empty!");
            Check.Require(start >= 0, "start must >= 0!");
            Check.Require(length >= 0, "length must = 0!");

            StringBuilder sb = new StringBuilder("SUBSTRING(");
            SqlQueryUtils.AppendColumnName(sb, columnName);
            sb.Append(',');
            sb.Append(start + 1);
            sb.Append(',');
            sb.Append(length);
            sb.Append(')');
            return sb.ToString();
        }

        public enum DatePartType
        {
            Year,
            Month,
            Day
        }

        public static string DatePart(string columnName, DatePartType partType)
        {
            Check.Require(!string.IsNullOrEmpty(columnName), "columnName could not be null or empty!");

            StringBuilder sb = new StringBuilder("DATEPART(");
            sb.Append(partType.ToString());
            sb.Append(',');
            SqlQueryUtils.AppendColumnName(sb, columnName);
            sb.Append(')');
            return sb.ToString();
        }

        public static string GetCurrentDate()
        {
            return "GETDATE()";
        }

        #endregion
    }
}
