using System;
using NBear.Common;
namespace NBear.Data
{
    interface IQuery
    {
        QuerySection GroupBy(NBear.Common.GroupByClip groupBy);
        QuerySection OrderBy(NBear.Common.OrderByClip orderBy);
        QuerySection Select(params NBear.Common.ExpressionClip[] properties);
        NBear.Common.EntityArrayList<EntityType> ToArrayList<EntityType>(int topItemCount) where EntityType : NBear.Common.Entity, new();
        NBear.Common.EntityArrayList<EntityType> ToArrayList<EntityType>(int topItemCount, int skipItemCount) where EntityType : NBear.Common.Entity, new();
        NBear.Common.EntityArrayList<EntityType> ToArrayList<EntityType>() where EntityType : NBear.Common.Entity, new();
        EntityType[] ToArray<EntityType>(int topItemCount) where EntityType : NBear.Common.Entity, new();
        EntityType[] ToArray<EntityType>(int topItemCount, int skipItemCount) where EntityType : NBear.Common.Entity, new();
        EntityType[] ToArray<EntityType>() where EntityType : NBear.Common.Entity, new();
        System.Data.IDataReader ToDataReader(int topItemCount);
        System.Data.IDataReader ToDataReader(int topItemCount, int skipItemCount);
        System.Data.IDataReader ToDataReader();
        System.Data.DataSet ToDataSet(int topItemCount);
        System.Data.DataSet ToDataSet(int topItemCount, int skipItemCount);
        System.Data.DataSet ToDataSet();
        EntityType ToFirst<EntityType>() where EntityType : NBear.Common.Entity, new();
        object ToScalar();
        QuerySection Where(WhereClip where);
    }
}
