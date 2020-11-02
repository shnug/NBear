using System;
using System.Data;
using System.Data.Common;

namespace NBear.Common
{
    public interface ISqlQueryFactory
    {
        DbCommand CreateDeleteCommand(string tableName, WhereClip where);
        DbCommand CreateInsertCommand(string tableOrViewName, string[] columns, DbType[] types, object[] values);
        DbCommand CreateSelectCommand(WhereClip where, string[] columns);
        DbCommand CreateUpdateCommand(string tableName, WhereClip where, string[] columns, DbType[] types, object[] values);
        DbCommand CreateSelectRangeCommand(WhereClip where, string[] columns, int topCount, int skipCount, string identyColumn, bool identyColumnIsNumber);
        DbCommand CreateCustomSqlCommand(string sql, string[] paramNames, DbType[] paramTypes, object[] paramValues);
        DbCommand CreateStoredProcedureCommand(string procedureName, string[] paramNames, DbType[] paramTypes, object[] paramValues);
        DbCommand CreateStoredProcedureCommand(string procedureName, string[] paramNames, DbType[] paramTypes, object[] paramValues, 
            string[] outParamNames, DbType[] outParamTypes, int[] outParamSizes);
        DbCommand CreateStoredProcedureCommand(string procedureName, string[] paramNames, DbType[] paramTypes, object[] paramValues, 
            string[] outParamNames, DbType[] outParamTypes, int[] outParamSizes, 
            string[] inOutParamNames, DbType[] inOutParamTypes, int[] inOutParamSizes, object[] inOutParamValues);
        DbCommand CreateStoredProcedureCommand(string procedureName, string[] paramNames, DbType[] paramTypes, object[] paramValues, 
            string[] outParamNames, DbType[] outParamTypes, int[] outParamSizes, 
            string[] inOutParamNames, DbType[] inOutParamTypes, int[] inOutParamSizes, object[] inOutParamValues, 
            string returnValueParamName, DbType returnValueParamType, int returnValueParamSize);
    }
}
