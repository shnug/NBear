using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;

using NBear.Common;

namespace NBear.Data
{
    public sealed class FromSection : IQuery
    {
        #region Private & Internal Members

        internal Gateway gateway;
        internal NBear.Common.FromClip fromClip;
        internal EntityConfiguration ec;
        internal string aliasName;

        #endregion

        #region Constructors

        public FromSection(Gateway gateway, EntityConfiguration ec, string aliasName)
        {
            Check.Require(gateway != null, "gateway could not be null.");
            Check.Require(ec != null, "ec could not be null.");
            Check.Require(aliasName != null, "tableAliasName could not be null.");

            this.gateway = gateway;
            this.ec = ec;
            string aliasNamePrefix = aliasName == ec.ViewName ? string.Empty : aliasName + '_';
            this.fromClip = new NBear.Common.FromClip(ec.ViewName, aliasNamePrefix + ec.ViewName);
            this.aliasName = aliasName;
        }

        public FromSection(Gateway gateway, EntityConfiguration ec, NBear.Common.FromClip fromClip)
        {
            Check.Require(gateway != null, "gateway could not be null.");
            Check.Require(ec != null, "ec could not be null.");
            Check.Require(fromClip != null, "fromClip could not be null.");

            this.gateway = gateway;
            this.ec = ec;
            this.fromClip = fromClip;
        }

        #endregion

        #region Public Methods

        public FromSection Join<EntityType>(WhereClip onWhere) where EntityType : Entity, new()
        {
            EntityConfiguration joinEc = new EntityType().GetEntityConfiguration();
            return Join(joinEc, joinEc.ViewName, onWhere);
        }

        public FromSection Join<EntityType>(string aliasName, WhereClip onWhere) where EntityType : Entity, new()
        {
            return Join(new EntityType().GetEntityConfiguration(), aliasName, onWhere);
        }

        public FromSection Join(EntityConfiguration joinEc, string aliasName, WhereClip onWhere)
        {
            Check.Require(joinEc != null, "joinEc could not be null.");
            Check.Require(aliasName != null, "tableAliasName could not be null.");

            string aliasNamePrefix = aliasName == joinEc.ViewName ? string.Empty : aliasName + '_';
            if (joinEc.BaseEntity == null)
            {
                fromClip.Join(joinEc.ViewName, aliasNamePrefix + joinEc.ViewName, new WhereClip().And(onWhere));
            }
            else
            {
                NBear.Common.FromClip appendFromClip = gateway.ConstructFrom(joinEc, aliasNamePrefix);
                fromClip.Join(appendFromClip.TableOrViewName, appendFromClip.AliasName, onWhere);
                Dictionary<string, KeyValuePair<string, WhereClip>>.Enumerator en = appendFromClip.Joins.GetEnumerator();
                while (en.MoveNext())
                {
                    fromClip.Join(en.Current.Value.Key, en.Current.Key, en.Current.Value.Value);
                }
            }
            return this;
        }

        #endregion

        #region IQuery Members

        public QuerySection Where(WhereClip where)
        {
            Check.Require(((object)where) != null, "where could not be null.");

            return new QuerySection(this).Where(where);
        }

        public QuerySection OrderBy(OrderByClip orderBy)
        {
            Check.Require(orderBy != null, "orderBy could not be null.");

            return new QuerySection(this).OrderBy(orderBy);
        }

        public QuerySection GroupBy(GroupByClip groupBy)
        {
            Check.Require(groupBy != null, "groupBy could not be null.");

            return new QuerySection(this).GroupBy(groupBy);
        }

        public QuerySection Select(params NBear.Common.ExpressionClip[] properties)
        {
            return new QuerySection(this).Select(properties);
        }

        public EntityType[] ToArray<EntityType>(int topItemCount) where EntityType : NBear.Common.Entity, new()
        {
            return new QuerySection(this).ToArray<EntityType>(topItemCount);
        }

        public EntityType[] ToArray<EntityType>(int topItemCount, int skipItemCount) where EntityType : NBear.Common.Entity, new()
        {
            return new QuerySection(this).ToArray<EntityType>(topItemCount, skipItemCount);
        }

        public EntityType[] ToArray<EntityType>() where EntityType : NBear.Common.Entity, new()
        {
            return new QuerySection(this).ToArray<EntityType>();
        }

        public EntityArrayList<EntityType> ToArrayList<EntityType>(int topItemCount) where EntityType : Entity, new()
        {
            return new QuerySection(this).ToArrayList<EntityType>(topItemCount);
        }

        public EntityArrayList<EntityType> ToArrayList<EntityType>(int topItemCount, int skipItemCount) where EntityType : Entity, new()
        {
            return new QuerySection(this).ToArrayList<EntityType>(topItemCount, skipItemCount);
        }

        public EntityArrayList<EntityType> ToArrayList<EntityType>() where EntityType : Entity, new()
        {
            return new QuerySection(this).ToArrayList<EntityType>();
        }

        public IDataReader ToDataReader(int topItemCount)
        {
            return new QuerySection(this).ToDataReader(topItemCount);
        }

        public IDataReader ToDataReader(int topItemCount, int skipItemCount)
        {
            return new QuerySection(this).ToDataReader(topItemCount, skipItemCount);
        }

        public IDataReader ToDataReader()
        {
            return new QuerySection(this).ToDataReader();
        }

        public DataSet ToDataSet(int topItemCount)
        {
            return new QuerySection(this).ToDataSet(topItemCount);
        }

        public DataSet ToDataSet(int topItemCount, int skipItemCount)
        {
            return new QuerySection(this).ToDataSet(topItemCount, skipItemCount);
        }

        public DataSet ToDataSet()
        {
            return new QuerySection(this).ToDataSet();
        }

        public EntityType ToFirst<EntityType>() where EntityType : Entity, new()
        {
            return new QuerySection(this).ToFirst<EntityType>();
        }

        public object ToScalar()
        {
            return new QuerySection(this).ToScalar();
        }

        #endregion
    }

    public sealed class QuerySection : NBear.Data.IQuery
    {
        #region Private Members

        private WhereClip whereClip;
        private EntityConfiguration entityConfig;
        private Gateway gateway;
        private List<string> selectColumns = new List<string>();
        private DbType firstColumnDbType = DbType.Int32;
        private string identyColumnName = null;
        private bool identyColumnIsNumber = false;

        private void PrepareWhere()
        {
            bool isWhereWithoutAliasName = whereClip.From.AliasName == whereClip.From.TableOrViewName;

            if (isWhereWithoutAliasName)
            {
                gateway.AdjustWhereForAutoCascadeJoin(whereClip, entityConfig, selectColumns);
            }
            else
            {
                if (whereClip.Sql.Contains("/*"))
                {
                    throw new NotSupportedException("A Gateway.From query with cascade where expression could not begin from an entity with alias name!");
                }
            }
        }

        private IDataReader FindDataReader()
        {
            DbCommand cmd = gateway.queryFactory.CreateSelectCommand(whereClip, selectColumns.ToArray());
            return gateway.Db.ExecuteReader(cmd);
        }

        private IDataReader FindDataReader(int topItemCount, int skipItemCount)
        {
            DbCommand cmd = gateway.queryFactory.CreateSelectRangeCommand(whereClip, selectColumns.ToArray(), topItemCount, skipItemCount, identyColumnName, identyColumnIsNumber);
            return gateway.Db.ExecuteReader(cmd);
        }

        private DataSet FindDataSet()
        {
            DbCommand cmd = gateway.queryFactory.CreateSelectCommand(whereClip, selectColumns.ToArray());
            return gateway.Db.ExecuteDataSet(cmd);
        }

        private DataSet FindDataSet(int topItemCount, int skipItemCount)
        {
            DbCommand cmd = gateway.queryFactory.CreateSelectRangeCommand(whereClip, selectColumns.ToArray(), topItemCount, skipItemCount, identyColumnName, identyColumnIsNumber);
            return gateway.Db.ExecuteDataSet(cmd);
        }

        private object FindScalar()
        {
            DbCommand cmd = gateway.queryFactory.CreateSelectCommand(whereClip, selectColumns.ToArray());
            return gateway.Db.ExecuteScalar(cmd);
        }

        #endregion

        #region Constructors

        public QuerySection(FromSection from)
        {
            Check.Require(from != null, "could not be null!");
            
            this.whereClip = new WhereClip(from.fromClip);
            this.entityConfig = from.ec;
            this.gateway = from.gateway;
            string[] selectColumnNames = entityConfig.GetAllSelectColumns();
            string autoIdColumn = MetaDataManager.GetEntityAutoId(entityConfig.Name);
            string aliasNamePrefix = (string.IsNullOrEmpty(from.aliasName) || from.aliasName == entityConfig.ViewName ? string.Empty : from.aliasName + '_');
            for (int i =0; i < selectColumnNames.Length; ++i)
            {
                selectColumns.Add(selectColumnNames[i]);

                selectColumns[i] = aliasNamePrefix + selectColumns[i];

                if (autoIdColumn != null && selectColumns[i].EndsWith('.' + autoIdColumn))
                {
                    identyColumnName = selectColumns[i];
                    identyColumnIsNumber = true;
                }
                else
                {
                    List<PropertyConfiguration> pkConfigs = entityConfig.GetPrimaryKeyProperties();
                    PropertyConfiguration pkConfig;
                    if (pkConfigs.Count > 0)
                    {
                        pkConfig = pkConfigs[0];
                    }
                    else
                    {
                        List<string> list = new List<string>();
                        Entity.GuessPrimaryKey(entityConfig, list);
                        pkConfig = entityConfig.GetPropertyConfiguration(list[0]);
                    }
                    identyColumnName = aliasNamePrefix + entityConfig.MappingName + '.' + pkConfig.MappingName;
                    if (pkConfig.DbType == DbType.Int16 || pkConfig.DbType == DbType.Int32 || pkConfig.DbType == DbType.Int64 ||
                        pkConfig.DbType == DbType.Single || pkConfig.DbType == DbType.Double)
                    {
                        identyColumnIsNumber = true;
                    }
                }
            }

            if (entityConfig.BaseEntity != null)
            {
                gateway.AppendBaseEntitiesJoins(entityConfig, aliasNamePrefix, whereClip.From);
            }
        }

        #endregion

        #region Public Methods

        public QuerySection Where(WhereClip where)
        {
            Check.Require(((object)where) != null, "where could not be null.");

            this.whereClip.And(where);
            return this;
        }

        public QuerySection OrderBy(OrderByClip orderBy)
        {
            Check.Require(orderBy != null, "orderBy could not be null.");

            this.whereClip.SetOrderBy(orderBy.OrderBys.ToArray());
            return this;
        }

        public QuerySection GroupBy(GroupByClip groupBy)
        {
            Check.Require(groupBy != null, "groupBy could not be null.");

            this.whereClip.SetGroupBy(groupBy.GroupBys.ToArray());
            return this;
        }

        public QuerySection Select(params NBear.Common.ExpressionClip[] properties)
        {
            Check.Require(properties != null && properties.Length > 0, "properties could not be null or empty!");

            selectColumns.Clear();
            for (int i =0; i < properties.Length; ++i)
            {
                //selectColumns.Add(properties[i].ColumnName);
                selectColumns.Add(properties[i].ToString());
                if (i == 0)
                {
                    firstColumnDbType = properties[i].DbType;
                }
            }
            return this;
        }

        public object ToScalar()
        {
            PrepareWhere();

            if (gateway.IfFindFromPreload(entityConfig, whereClip))
            {
                try
                {
                    //return gateway.FindScalarFromPreLoadEx(ExpressionFactory.CreateColumnExpression(selectColumns[0], firstColumnDbType), entityConfig, whereClip);
                    System.Reflection.MethodInfo mi = Gateway.GetGatewayMethodInfo("System.Object FindScalarFromPreLoadEx[EntityType](NBear.Common.ExpressionClip, NBear.Common.EntityConfiguration, NBear.Common.WhereClip)");
                    Type entityType = Util.GetType(entityConfig.Name);
                    return mi.MakeGenericMethod(entityType).Invoke(gateway, new object[] { ExpressionFactory.CreateColumnExpression(selectColumns[0], firstColumnDbType), entityConfig, whereClip });
                }
                catch
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("find from auto-preload failed, visit database directly...");
#endif
                }
            }

            string cacheKey = null;
            int expireSeconds = gateway.GetTableCacheExpireSeconds(entityConfig.ViewName);
            bool cacheEnabled = gateway.IsCacheTurnedOn && expireSeconds > 0;
            if (cacheEnabled)
            {
                cacheKey = gateway.ComputeCacheKey(entityConfig.Name + "|ToScalar_" + string.Join("_", selectColumns.ToArray()), whereClip);
                //if (Gateway.cache.Contains(cacheKey))
                //{
                //    return gateway.GetCache(cacheKey);
                //}
                object cacheObj = gateway.GetCache(cacheKey);
                if (cacheObj != null)
                {
                    return cacheObj;
                }
            }

            object obj = FindScalar();

            if (cacheEnabled && cacheKey != null)
            {
                gateway.AddCache(cacheKey, obj, expireSeconds);
            }

            return obj;
        }

        public EntityType ToFirst<EntityType>() where EntityType : Entity, new()
        {
            PrepareWhere();
            if (typeof(EntityType).ToString() == entityConfig.Name && gateway.IfFindFromPreload(entityConfig, whereClip))
            {
                try
                {
                    return gateway.FindFromPreLoadEx<EntityType>(entityConfig, whereClip);
                }
                catch
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("find from auto-preload failed, visit database directly...");
#endif
                }
            }

            EntityConfiguration ec = typeof(EntityType).ToString() == entityConfig.Name ? entityConfig : new EntityType().GetEntityConfiguration();

            string cacheKey = null;
            int expireSeconds = gateway.GetTableCacheExpireSeconds(ec.ViewName);
            bool cacheEnabled = gateway.IsCacheTurnedOn && expireSeconds > 0;
            if (cacheEnabled)
            {
                cacheKey = gateway.ComputeCacheKey(ec.Name + "|ToFirst_" + string.Join("_", selectColumns.ToArray()), whereClip);
                //if (Gateway.cache.Contains(cacheKey))
                //{
                //    return (EntityType)gateway.GetCache(cacheKey);
                //}
                object cacheObj = gateway.GetCache(cacheKey);
                if (cacheObj != null)
                {
                    return (EntityType)cacheObj;
                }
            }

            IDataReader reader = FindDataReader();
            EntityType obj = null;
            if (reader.Read())
            {
                obj = gateway.CreateEntity<EntityType>();
                obj.SetPropertyValues(reader);
            }
            reader.Close();
            reader.Dispose();

            if (cacheEnabled && cacheKey != null)
            {
                gateway.AddCache(cacheKey, obj, expireSeconds);
            }

            return obj;
        }

        public EntityType[] ToArray<EntityType>(int topItemCount) where EntityType : NBear.Common.Entity, new()
        {
            EntityArrayList<EntityType> list = ToArrayList<EntityType>(topItemCount);
            return list == null ? null : list.ToArray();
        }

        public EntityType[] ToArray<EntityType>(int topItemCount, int skipItemCount) where EntityType : NBear.Common.Entity, new()
        {
            EntityArrayList<EntityType> list = ToArrayList<EntityType>(topItemCount, skipItemCount);
            return list == null ? null : list.ToArray();
        }

        public EntityType[] ToArray<EntityType>() where EntityType : NBear.Common.Entity, new()
        {
            EntityArrayList<EntityType> list = ToArrayList<EntityType>();
            return list == null ? null : list.ToArray();
        }

        public EntityArrayList<EntityType> ToArrayList<EntityType>() where EntityType : Entity, new()
        {
            PrepareWhere();

            if (typeof(EntityType).ToString() == entityConfig.Name && gateway.IfFindFromPreload(entityConfig, whereClip))
            {
                try
                {
                    EntityArrayList<EntityType> al = new EntityArrayList<EntityType>();
                    al.AddRange(gateway.FindArrayFromPreLoadEx<EntityType>(entityConfig, whereClip));
                    return al;                
                }
                catch
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("find from auto-preload failed, visit database directly...");
#endif
                }
            }

            EntityConfiguration ec = typeof(EntityType).ToString() == entityConfig.Name ? entityConfig : new EntityType().GetEntityConfiguration();
            string cacheKey = null;
            int expireSeconds = gateway.GetTableCacheExpireSeconds(ec.ViewName);
            bool cacheEnabled = gateway.IsCacheTurnedOn && expireSeconds > 0;
            if (cacheEnabled)
            {
                cacheKey = gateway.ComputeCacheKey(ec.Name + "|ToArrayList_" + string.Join("_", selectColumns.ToArray()), whereClip);
                //if (Gateway.cache.Contains(cacheKey))
                //{
                //    return (EntityArrayList<EntityType>)gateway.GetCache(cacheKey);
                //}
                object cacheObj = gateway.GetCache(cacheKey);
                if (cacheObj != null)
                {
                    return (EntityArrayList<EntityType>)cacheObj;
                }
            }

            IDataReader reader = FindDataReader();
            EntityArrayList<EntityType> list = new EntityArrayList<EntityType>();
            while (reader.Read())
            {
                EntityType obj = gateway.CreateEntity<EntityType>();
                obj.SetPropertyValues(reader);
                list.Add(obj);
            }
            reader.Close();
            reader.Dispose();

            if (cacheEnabled && cacheKey != null)
            {
                gateway.AddCache(cacheKey, list, expireSeconds);
            }

            return list;
        }

        public EntityArrayList<EntityType> ToArrayList<EntityType>(int topItemCount) where EntityType : Entity, new()
        {
            return ToArrayList<EntityType>(topItemCount, 0);
        }

        public EntityArrayList<EntityType> ToArrayList<EntityType>(int topItemCount, int skipItemCount) where EntityType : Entity, new()
        {
            PrepareWhere();

            if (typeof(EntityType).ToString() == entityConfig.Name && gateway.IfFindFromPreload(entityConfig, whereClip))
            {
                try
                {
                    EntityArrayList<EntityType> al = new EntityArrayList<EntityType>();
                    al.AddRange(gateway.FindArrayFromPreLoadEx<EntityType>(entityConfig, whereClip, topItemCount, skipItemCount));
                    return al;
                }
                catch
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("find from auto-preload failed, visit database directly...");
#endif
                }
            }

            EntityConfiguration ec = typeof(EntityType).ToString() == entityConfig.Name ? entityConfig : new EntityType().GetEntityConfiguration();
            string cacheKey = null;
            int expireSeconds = gateway.GetTableCacheExpireSeconds(ec.ViewName);
            bool cacheEnabled = gateway.IsCacheTurnedOn && expireSeconds > 0;
            if (cacheEnabled)
            {
                cacheKey = gateway.ComputeCacheKey(ec.Name + "|ToArrayList_" + topItemCount.ToString() + '_' + skipItemCount.ToString() + '_' + string.Join("_", selectColumns.ToArray()), whereClip);
                //if (Gateway.cache.Contains(cacheKey))
                //{
                //    return (EntityArrayList<EntityType>)gateway.GetCache(cacheKey);
                //}
                object cacheObj = gateway.GetCache(cacheKey);
                if (cacheObj != null)
                {
                    return (EntityArrayList<EntityType>)cacheObj;
                }
            }

            IDataReader reader = FindDataReader(topItemCount, skipItemCount);
            EntityArrayList<EntityType> list = new EntityArrayList<EntityType>();
            while (reader.Read())
            {
                EntityType obj = gateway.CreateEntity<EntityType>();
                obj.SetPropertyValues(reader);
                list.Add(obj);
            }
            reader.Close();
            reader.Dispose();

            if (cacheEnabled && cacheKey != null)
            {
                gateway.AddCache(cacheKey, list, expireSeconds);
            }

            return list;
        }

        public IDataReader ToDataReader()
        {
            PrepareWhere();
            return FindDataReader();
        }

        public IDataReader ToDataReader(int topItemCount)
        {
            PrepareWhere();
            return ToDataReader(topItemCount, 0);
        }

        public IDataReader ToDataReader(int topItemCount, int skipItemCount)
        {
            PrepareWhere();
            return FindDataReader(topItemCount, skipItemCount);
        }

        public DataSet ToDataSet()
        {
            PrepareWhere();

            if (gateway.IfFindFromPreload(entityConfig, whereClip))
            {
                try
                {
                    DataSet dataSet = new DataSet(entityConfig.ViewName);

                    //DataTable dt = gateway.FindTableFromPreLoadEx(entityConfig, whereClip);
                    System.Reflection.MethodInfo mi = Gateway.GetGatewayMethodInfo("System.Data.DataTable FindTableFromPreLoadEx[EntityType](NBear.Common.EntityConfiguration, NBear.Common.WhereClip)");
                    Type entityType = Util.GetType(entityConfig.Name);
                    DataTable dt = (DataTable)mi.MakeGenericMethod(entityType).Invoke(gateway, new object[] { entityConfig, whereClip });

                    dataSet.Tables.Add(dt);
                    return dataSet;
                }
                catch
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("find from auto-preload failed, visit database directly...");
#endif
                }
            }

            string cacheKey = null;
            int expireSeconds = gateway.GetTableCacheExpireSeconds(entityConfig.ViewName);
            bool cacheEnabled = gateway.IsCacheTurnedOn && expireSeconds > 0;
            if (cacheEnabled)
            {
                cacheKey = gateway.ComputeCacheKey(entityConfig.Name + "|ToDataSet_" + string.Join("_", selectColumns.ToArray()), whereClip);
                //if (Gateway.cache.Contains(cacheKey))
                //{
                //    return (DataSet)gateway.GetCache(cacheKey);
                //}
                object cacheObj = gateway.GetCache(cacheKey);
                if (cacheObj != null)
                {
                    return (DataSet)cacheObj;
                }
            }

            DataSet ds = FindDataSet();

            if (cacheEnabled && cacheKey != null)
            {
                gateway.AddCache(cacheKey, ds, expireSeconds);
            }

            return ds;
        }

        public DataSet ToDataSet(int topItemCount)
        {
            return ToDataSet(topItemCount, 0);
        }

        public DataSet ToDataSet(int topItemCount, int skipItemCount)
        {
            PrepareWhere();

            if (gateway.IfFindFromPreload(entityConfig, whereClip))
            {
                try
                {
                    DataSet dataSet = new DataSet(entityConfig.ViewName);

                    //DataTable dt = gateway.FindTableFromPreLoadEx(entityConfig, whereClip, topItemCount, skipItemCount);
                    System.Reflection.MethodInfo mi = Gateway.GetGatewayMethodInfo("System.Data.DataTable FindTableFromPreLoadEx[EntityType](NBear.Common.EntityConfiguration, NBear.Common.WhereClip, Int32, Int32)");
                    Type entityType = Util.GetType(entityConfig.Name);
                    DataTable dt = (DataTable)mi.MakeGenericMethod(entityType).Invoke(gateway, new object[] { entityConfig, whereClip, topItemCount, skipItemCount });

                    dataSet.Tables.Add(dt);
                    return dataSet;
                }
                catch
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("find from auto-preload failed, visit database directly...");
#endif
                }
            }

            string cacheKey = null;
            int expireSeconds = gateway.GetTableCacheExpireSeconds(entityConfig.ViewName);
            bool cacheEnabled = gateway.IsCacheTurnedOn && expireSeconds > 0;
            if (cacheEnabled)
            {
                cacheKey = gateway.ComputeCacheKey(entityConfig.Name + "|ToDataSet_" + topItemCount.ToString() + '_' + skipItemCount.ToString() + '_' + string.Join("_", selectColumns.ToArray()), whereClip);
                //if (Gateway.cache.Contains(cacheKey))
                //{
                //    return (DataSet)gateway.GetCache(cacheKey);
                //}
                object cacheObj = gateway.GetCache(cacheKey);
                if (cacheObj != null)
                {
                    return (DataSet)cacheObj;
                }
            }

            DataSet ds = FindDataSet(topItemCount, skipItemCount);

            if (cacheEnabled && cacheKey != null)
            {
                gateway.AddCache(cacheKey, ds, expireSeconds);
            }

            return ds;
        }

        #endregion
    }

    public sealed class CustomSqlSection
    {
        #region Private Members

        private Gateway gateway;
        private string sql;
        private DbTransaction tran;
        private List<string> inputParamNames = new List<string>();
        private List<DbType> inputParamTypes = new List<DbType>();
        private List<object> inputParamValues = new List<object>();

        private IDataReader FindDataReader()
        {
            DbCommand cmd = gateway.queryFactory.CreateCustomSqlCommand(sql, inputParamNames.ToArray(),
                inputParamTypes.ToArray(), inputParamValues.ToArray());
            return tran == null ? gateway.Db.ExecuteReader(cmd) : gateway.Db.ExecuteReader(cmd, tran);
        }

        private DataSet FindDataSet()
        {
            DbCommand cmd = gateway.queryFactory.CreateCustomSqlCommand(sql, inputParamNames.ToArray(),
                inputParamTypes.ToArray(), inputParamValues.ToArray());
            return tran == null ? gateway.Db.ExecuteDataSet(cmd) : gateway.Db.ExecuteDataSet(cmd, tran);
        }

        #endregion

        #region Constructors

        public CustomSqlSection(Gateway gateway, string sql)
        {
            Check.Require(gateway != null, "gateway could not be null.");
            Check.Require(sql != null, "sql could not be null.");

            this.gateway = gateway;
            this.sql = sql;
        }

        #endregion

        #region Public Members

        public CustomSqlSection AddInputParameter(string name, DbType type, object value)
        {
            Check.Require(!string.IsNullOrEmpty(name), "name could not be null or empty!");

            inputParamNames.Add(name);
            inputParamTypes.Add(type);
            inputParamValues.Add(value);

            return this;
        }
        
        public CustomSqlSection SetTransaction(DbTransaction tran)
        {
            this.tran = tran;

            return this;
        }

        public int ExecuteNonQuery()
        {
            DbCommand cmd = gateway.queryFactory.CreateCustomSqlCommand(sql, inputParamNames.ToArray(),
                inputParamTypes.ToArray(), inputParamValues.ToArray());
            return tran == null ? gateway.Db.ExecuteNonQuery(cmd) : gateway.Db.ExecuteNonQuery(cmd, tran);
        }

        public object ToScalar()
        {
            IDataReader reader = FindDataReader();
            object retObj = null;
            if (reader.Read())
            {
                retObj = reader.GetValue(0);
            }
            reader.Close();
            reader.Dispose();

            return retObj;
        }

        public EntityType ToFirst<EntityType>() where EntityType : Entity, new()
        {
            IDataReader reader = FindDataReader();
            EntityType obj = null;
            if (reader.Read())
            {
                obj = gateway.CreateEntity<EntityType>();
                obj.SetPropertyValues(reader);
            }
            reader.Close();
            reader.Dispose();

            return obj;
        }

        public EntityArrayList<EntityType> ToArrayList<EntityType>() where EntityType : Entity, new()
        {
            IDataReader reader = FindDataReader();
            EntityArrayList<EntityType> list = new EntityArrayList<EntityType>();
            while (reader.Read())
            {
                EntityType obj = gateway.CreateEntity<EntityType>();
                obj.SetPropertyValues(reader);
                list.Add(obj);
            }
            reader.Close();
            reader.Dispose();

            return list;
        }

        public EntityType[] ToArray<EntityType>() where EntityType : Entity, new()
        {
            return ToArrayList<EntityType>().ToArray();
        }

        public IDataReader ToDataReader()
        {
            return FindDataReader();
        }

        public DataSet ToDataSet()
        {
            return FindDataSet();
        }

        #endregion
    }

    public sealed class StoredProcedureSection
    {
        #region Private Members

        private Gateway gateway;
        private string spName;
        private DbTransaction tran;

        private List<string> inputParamNames = new List<string>();
        private List<DbType> inputParamTypes = new List<DbType>();
        private List<object> inputParamValues = new List<object>();

        private List<string> outputParamNames = new List<string>();
        private List<DbType> outputParamTypes = new List<DbType>();
        private List<int> outputParamSizes = new List<int>();

        private List<string> inputOutputParamNames = new List<string>();
        private List<DbType> inputOutputParamTypes = new List<DbType>();
        private List<object> inputOutputParamValues = new List<object>();
        private List<int> inputOutputParamSizes = new List<int>();

        private string returnValueParamName;
        private DbType returnValueParamType;
        private int returnValueParamSize;

        private IDataReader FindDataReader()
        {
            DbCommand cmd = gateway.queryFactory.CreateStoredProcedureCommand(spName, 
                inputParamNames.ToArray(), inputParamTypes.ToArray(), inputParamValues.ToArray(),
                outputParamNames.ToArray(), outputParamTypes.ToArray(), outputParamSizes.ToArray(),
                inputOutputParamNames.ToArray(), inputOutputParamTypes.ToArray(), inputOutputParamSizes.ToArray(), inputOutputParamValues.ToArray(),
                returnValueParamName, returnValueParamType, returnValueParamSize);
            return tran == null ? gateway.Db.ExecuteReader(cmd) : gateway.Db.ExecuteReader(cmd, tran);
        }

        private DataSet FindDataSet()
        {
            DbCommand cmd = gateway.queryFactory.CreateStoredProcedureCommand(spName, 
                inputParamNames.ToArray(), inputParamTypes.ToArray(), inputParamValues.ToArray(),
                outputParamNames.ToArray(), outputParamTypes.ToArray(), outputParamSizes.ToArray(),
                inputOutputParamNames.ToArray(), inputOutputParamTypes.ToArray(), inputOutputParamSizes.ToArray(), inputOutputParamValues.ToArray(),
                returnValueParamName, returnValueParamType, returnValueParamSize);
            return tran == null ? gateway.Db.ExecuteDataSet(cmd) : gateway.Db.ExecuteDataSet(cmd, tran);
        }

        private DataSet FindDataSet(out Dictionary<string, object> outValues)
        {
            DbCommand cmd = gateway.queryFactory.CreateStoredProcedureCommand(spName, 
                inputParamNames.ToArray(), inputParamTypes.ToArray(), inputParamValues.ToArray(),
                outputParamNames.ToArray(), outputParamTypes.ToArray(), outputParamSizes.ToArray(),
                inputOutputParamNames.ToArray(), inputOutputParamTypes.ToArray(), inputOutputParamSizes.ToArray(), inputOutputParamValues.ToArray(),
                returnValueParamName, returnValueParamType, returnValueParamSize);
            DataSet ds = (tran == null ? gateway.Db.ExecuteDataSet(cmd) : gateway.Db.ExecuteDataSet(cmd, tran));
            outValues = GetOutputParameterValues(cmd);
            return ds;
        }

        private static Dictionary<string, object> GetOutputParameterValues(DbCommand cmd)
        {
            Dictionary<string, object> outValues;
            outValues = new Dictionary<string, object>();
            for (int i = 0; i < cmd.Parameters.Count; ++i)
            {
                if (cmd.Parameters[i].Direction == ParameterDirection.InputOutput || cmd.Parameters[i].Direction == ParameterDirection.Output || cmd.Parameters[i].Direction == ParameterDirection.ReturnValue)
                {
                    outValues.Add(cmd.Parameters[i].ParameterName.Substring(1, cmd.Parameters[i].ParameterName.Length - 1),
                        cmd.Parameters[i].Value);
                }
            }
            return outValues;
        }

        #endregion

        #region Constructors

        public StoredProcedureSection(Gateway gateway, string spName) : base()
        {
            Check.Require(gateway != null, "gateway could not be null.");
            Check.Require(spName != null, "spName could not be null.");

            this.gateway = gateway;
            this.spName = spName;
        }

        #endregion

        #region Public Members

        public StoredProcedureSection AddInputParameter(string name, DbType type, object value)
        {
            Check.Require(!string.IsNullOrEmpty(name), "name could not be null or empty!");

            inputParamNames.Add(name);
            inputParamTypes.Add(type);
            inputParamValues.Add(value);

            return this;
        }

        public StoredProcedureSection AddOutputParameter(string name, DbType type, int size)
        {
            Check.Require(!string.IsNullOrEmpty(name), "name could not be null or empty!");

            outputParamNames.Add(name);
            outputParamTypes.Add(type);
            outputParamSizes.Add(size);

            return this;
        }

        public StoredProcedureSection AddInputOutputParameter(string name, DbType type, int size, object value)
        {
            Check.Require(!string.IsNullOrEmpty(name), "name could not be null or empty!");

            inputOutputParamNames.Add(name);
            inputOutputParamTypes.Add(type);
            inputOutputParamSizes.Add(size);
            inputOutputParamValues.Add(value);

            return this;
        }

        public StoredProcedureSection SetReturnParameter(string name, DbType type, int size)
        {
            Check.Require(!string.IsNullOrEmpty(name), "name could not be null or empty!");

            returnValueParamName = name;
            returnValueParamType = type;
            returnValueParamSize = size;

            return this;
        }
        
        public StoredProcedureSection SetTransaction(DbTransaction tran)
        {
            this.tran = tran;

            return this;
        }

        public int ExecuteNonQuery()
        {
            DbCommand cmd = gateway.queryFactory.CreateStoredProcedureCommand(spName, 
                inputParamNames.ToArray(), inputParamTypes.ToArray(), inputParamValues.ToArray(),
                outputParamNames.ToArray(), outputParamTypes.ToArray(), outputParamSizes.ToArray(),
                inputOutputParamNames.ToArray(), inputOutputParamTypes.ToArray(), inputOutputParamSizes.ToArray(), inputOutputParamValues.ToArray(),
                returnValueParamName, returnValueParamType, returnValueParamSize);
            return tran == null ? gateway.Db.ExecuteNonQuery(cmd) : gateway.Db.ExecuteNonQuery(cmd, tran);
        }

        public int ExecuteNonQuery(out Dictionary<string, object> outValues)
        {
            DbCommand cmd = gateway.queryFactory.CreateStoredProcedureCommand(spName, 
                inputParamNames.ToArray(), inputParamTypes.ToArray(), inputParamValues.ToArray(),
                outputParamNames.ToArray(), outputParamTypes.ToArray(), outputParamSizes.ToArray(),
                inputOutputParamNames.ToArray(), inputOutputParamTypes.ToArray(), inputOutputParamSizes.ToArray(), inputOutputParamValues.ToArray(),
                returnValueParamName, returnValueParamType, returnValueParamSize);
            int affactRows = (tran == null ? gateway.Db.ExecuteNonQuery(cmd) : gateway.Db.ExecuteNonQuery(cmd, tran));
            outValues = GetOutputParameterValues(cmd);
            return affactRows;
        }

        public object ToScalar()
        {
            IDataReader reader = FindDataReader();
            object retObj = null;
            if (reader.Read())
            {
                retObj = reader.GetValue(0);
            }
            reader.Close();
            reader.Dispose();

            return retObj;
        }

        public EntityType ToFirst<EntityType>() where EntityType : Entity, new()
        {
            IDataReader reader = FindDataReader();
            EntityType obj = null;
            if (reader.Read())
            {
                obj = gateway.CreateEntity<EntityType>();
                obj.SetPropertyValues(reader);
            }
            reader.Close();
            reader.Dispose();

            return obj;
        }

        public EntityArrayList<EntityType> ToArrayList<EntityType>() where EntityType : Entity, new()
        {
            IDataReader reader = FindDataReader();
            EntityArrayList<EntityType> list = new EntityArrayList<EntityType>();
            while (reader.Read())
            {
                EntityType obj = gateway.CreateEntity<EntityType>();
                obj.SetPropertyValues(reader);
                list.Add(obj);
            }
            reader.Close();
            reader.Dispose();

            return list;
        }

        public EntityType[] ToArray<EntityType>() where EntityType : Entity, new()
        {
            return ToArrayList<EntityType>().ToArray();
        }

        public IDataReader ToDataReader()
        {
            return FindDataReader();
        }

        public DataSet ToDataSet()
        {
            return FindDataSet();
        }

        public object ToScalar(out Dictionary<string, object> outValues)
        {
            DataSet ds = FindDataSet(out outValues);
            object retObj = null;
            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                retObj = ds.Tables[0].Rows[0][0];
            }
            ds.Dispose();

            return retObj;
        }

        public EntityType ToFirst<EntityType>(out Dictionary<string, object> outValues) where EntityType : Entity, new()
        {
            DataSet ds = FindDataSet(out outValues);
            EntityType obj = null;
            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                obj = gateway.CreateEntity<EntityType>();
                obj.SetPropertyValues(ds.Tables[0].Rows[0]);
            }
            ds.Dispose();

            return obj;
        }

        public EntityArrayList<EntityType> ToArrayList<EntityType>(out Dictionary<string, object> outValues) where EntityType : Entity, new()
        {
            DataSet ds = FindDataSet(out outValues);
            EntityArrayList<EntityType> list = new EntityArrayList<EntityType>();
            for (int i = 0; i < ds.Tables[0].Rows.Count; ++i)
            {
                EntityType obj = gateway.CreateEntity<EntityType>();
                obj.SetPropertyValues(ds.Tables[0].Rows[i]);
                list.Add(obj);
            }
            ds.Dispose();

            return list;
        }

        public EntityType[] ToArray<EntityType>(out Dictionary<string, object> outValues) where EntityType : Entity, new()
        {
            return ToArrayList<EntityType>(out outValues).ToArray();
        }

        public DataSet ToDataSet(out Dictionary<string, object> outValues)
        {
            return FindDataSet(out outValues);
        }

        #endregion
    }
}