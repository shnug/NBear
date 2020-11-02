using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace NBear.Common
{
    /// <summary>
    /// A property item stands for a property in strong typed query.
    /// </summary>
    [Serializable]
    public class PropertyItem : NBear.Common.ExpressionClip
    {
        #region Const Members

        /// <summary>
        /// All stands for *, which is only used in Gateway.Count query.
        /// </summary>
        public static readonly PropertyItem All = new PropertyItem("*");

        #endregion

        #region Private Members

        private string name;
        private string columnName;
        private string tableAliasName;
        private EntityConfiguration entityConfig;
        private PropertyConfiguration propertyConfig;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the entity configuration.
        /// </summary>
        /// <value>The entity configuration.</value>
        public EntityConfiguration EntityConfiguration
        {
            get
            {
                return entityConfig;
            }
        }

        /// <summary>
        /// Gets the property configuration.
        /// </summary>
        /// <value>The property configuration.</value>
        public PropertyConfiguration PropertyConfiguration
        {
            get
            {
                return propertyConfig;
            }
        }

        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        /// <value>The name of the property.</value>
        public string PropertyName
        {
            get
            {
                return name ?? (propertyConfig == null ? null : propertyConfig.Name);
            }
        }

        /// <summary>
        /// Gets the name of the mapping column name of the property.
        /// </summary>
        /// <value>The name of the column.</value>
        public string ColumnName
        {
            get
            {
                return TableAliasName == null ? this.columnName : TableAliasName + '.' + this.columnName;
            }
        }

        public string ColumnNameWithoutPrefix
        {
            get
            {
                return this.columnName;
            }
        }

        [System.Xml.Serialization.XmlIgnore]
        public string TableAliasName
        {
            get
            {
                if (tableAliasName == null)
                {
                    return entityConfig == null ? null : entityConfig.MappingName;
                }
                else
                {
                    if (entityConfig == null)
                    {
                        return tableAliasName;
                    }
                    else
                    {
                        return tableAliasName + '_' + entityConfig.MappingName;
                    }
                }
            }
            set
            {
                tableAliasName = value;
                InitColumnExpression(this.ColumnName, this.DbType);
            }
        }

        /// <summary>
        /// Gets the ascendent order by clip of this property.
        /// </summary>
        /// <value>The asc.</value>
        public OrderByClip Asc
        {
            get
            {
                return new OrderByClip(this, false);
            }
        }

        /// <summary>
        /// Gets the descendent order by clip of this property.
        /// </summary>
        /// <value>The desc.</value>
        public OrderByClip Desc
        {
            get
            {
                return new OrderByClip(this, true);
            }
        }

        /// <summary>
        /// Get the order by clip of this property.
        /// </summary>
        public GroupByClip GroupBy
        {
            get
            {
                return new GroupByClip(this);
            }
        }

        public string[] AdditionalSerializedData
        {
            get
            {
                string[] list = new string[4];

                list[0] = this.name;
                list[1] = this.columnName;
                list[2] = this.tableAliasName;
                if (entityConfig != null)
                {
                    list[3] = entityConfig.Name;
                }

                return list;
            }
            set
            {
                string[] list = value;
                if (list != null && list.Length == 4)
                {
                    this.name = list[0];
                    this.columnName = list[1];
                    this.tableAliasName = list[2];
                    if (list[3] != null)
                    {
                        this.entityConfig = MetaDataManager.GetEntityConfiguration(list[3]);
                        if (this.entityConfig != null)
                        {
                            this.propertyConfig = entityConfig.GetPropertyConfiguration(this.name);
                        }
                    }
                }
            }
        }

        #endregion

        #region Constructors

        public PropertyItem() : base()
        {
        }

        public PropertyItem(string propertyName) : this(propertyName, (EntityConfiguration)null)
        {
        }

        public PropertyItem(string propertyName, EntityConfiguration entityConfig) : this(propertyName, entityConfig, null, null)
        {
        }

        public PropertyItem(string propertyName, EntityConfiguration entityConfig, PropertyConfiguration pc, string aliasName) : this()
        {
            Check.Require(propertyName != null, "propertyName could not be null!");

            this.name = propertyName;
            this.entityConfig = entityConfig;
            if (!string.IsNullOrEmpty(aliasName))
            {
                this.tableAliasName = aliasName;
            }

            this.propertyConfig = pc;

            if (entityConfig != null)
            {
                if (this.propertyConfig == null)
                {
                    this.propertyConfig = entityConfig.GetPropertyConfiguration(this.name);
                }
                if (propertyConfig != null)
                {
                    this.columnName = propertyConfig.MappingName;
                    this.DbType = propertyConfig.DbType;
                }
                else
                {
                    this.columnName = this.name;
                }
            }
            else
            {
                this.columnName = this.name;
            }

            InitColumnExpression(this.ColumnName, this.DbType);
        }

        public PropertyItem(string propertyName, string entityTypeName) : 
            this(propertyName, MetaDataManager.GetEntityConfiguration(entityTypeName))
        {
        }

        #endregion

        #region Static Members

        /// <summary>
        /// Parses the expression by entity meta data to actual sql.
        /// </summary>
        /// <param name="inStr">The in STR.</param>
        /// <param name="propertyToColumnMapHandler">The property to column map handler.</param>
        /// <param name="leftToken">The left token.</param>
        /// <param name="rightToken">The right token.</param>
        /// <param name="paramPrefix">The param prefix.</param>
        /// <returns>The actual sql.</returns>
        public static string ParseExpressionByMetaData(string inStr, PropertyToColumnMapHandler propertyToColumnMapHandler, string leftToken, string rightToken, string paramPrefix)
        {
            if (inStr == null)
            {
                return null;
            }

            string retStr = inStr;

            System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex(@"\{[\w\d_]+\}", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline);
            System.Text.RegularExpressions.MatchCollection ms = r.Matches(retStr);
            foreach (System.Text.RegularExpressions.Match m in ms)
            {
                retStr = retStr.Replace(m.Value, string.Format("{0}{1}{2}", leftToken, propertyToColumnMapHandler(m.Value.Trim('{', '}')), rightToken));
            }

            r = new System.Text.RegularExpressions.Regex(@"@[\w\d_]+\s+", System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline);
            ms = r.Matches(retStr);
            foreach (System.Text.RegularExpressions.Match m in ms)
            {
                retStr = retStr.Replace(m.Value, string.Format("{0}{1} ", paramPrefix, m.Value.Replace("@", "")));
            }

            return retStr;
        }

        #endregion
    }

    #region Moved Code - Combined code of WhereClip here to CommonLibraries\CN.Teddy.SqlQuery\WhereClip.cs

    ///// <summary>
    ///// Strong typed where clip.
    ///// </summary>
    //public sealed class WhereClip : NBear.Common.WhereClip
    //{
    //    public static WhereClip All
    //    {
    //        get
    //        {
    //            return new WhereClip();
    //        }
    //    }

    //    /// <summary>
    //    /// Gets the param values.
    //    /// </summary>
    //    /// <value>The param values.</value>
    //    [Obsolete]
    //    public object[] ParamValues
    //    {
    //        get
    //        {
    //            object[] values = new object[this.Parameters.Values.Count];

    //            Dictionary<string, KeyValuePair<DbType, object>>.ValueCollection.Enumerator  en = this.Parameters.Values.GetEnumerator();
    //            int i = 0;
    //            while (en.MoveNext())
    //            {
    //                values[i] = en.Current.Value;

    //                ++i;
    //            }

    //            return values;
    //        }
    //    }

    //    /// <summary>
    //    /// Initializes a new instance of the <see cref="WhereClip"/> class.
    //    /// </summary>
    //    public WhereClip()
    //    {
    //    }

    //    [Obsolete("Please use new WhereClip(sql, paramNames, paramTypes, paramValues) instead.")]
    //    public WhereClip(string whereStr, params object[] paramValues)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public WhereClip(string sql, string[] paramNames, DbType[] paramTypes, object[] paramValues) 
    //        : base(sql, paramNames, paramTypes, paramValues)
    //    {
    //    }
    //}

    #endregion

    /// <summary>
    /// Strong typed orderby clip.
    /// </summary>
    public class OrderByClip
    {
        private static readonly OrderByClip _Default = new OrderByClip(null);

        /// <summary>
        /// Gets the default order by condition.
        /// </summary>
        /// <value>The default.</value>
        public static OrderByClip Default
        {
            get
            {
                return _Default;
            }
        }

        //private string orderByStr;
        List<KeyValuePair<string, bool>> orderBys = new List<KeyValuePair<string, bool>>();

        /// <summary>
        /// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            List<KeyValuePair<string, bool>>.Enumerator en = orderBys.GetEnumerator();
            while (en.MoveNext())
            {
                if (sb.Length > 0)
                {
                    sb.Append(',');
                }
                if (en.Current.Key.Contains("."))
                {
                    string[] splittedColumnSections = en.Current.Key.Split('.');
                    for (int i = 0; i < splittedColumnSections.Length; ++i)
                    {
                        sb.Append('[');
                        sb.Append(splittedColumnSections[i]);
                        sb.Append(']');

                        if (i < splittedColumnSections.Length - 1)
                        {
                            sb.Append('.');
                        }
                    }
                }
                else
                {
                    sb.Append('[');
                    sb.Append(en.Current.Key);
                    sb.Append(']');
                }
                if (en.Current.Value)
                {
                    sb.Append(" DESC");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderByClip"/> class.
        /// </summary>
        public OrderByClip()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderByClip"/> class.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="descend">if set to <c>true</c> [descend].</param>
        public OrderByClip(ExpressionClip item, bool descend)
        {
            string columnName;
            if (item is PropertyItem)
            {
                columnName = ((PropertyItem)item).ColumnName;
            }
            else
            {
                columnName = item.ToString().Replace("[", string.Empty).Replace("]", string.Empty);
            }
            orderBys.Add(new KeyValuePair<string, bool>(columnName, descend));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderByClip"/> class.
        /// </summary>
        /// <param name="orderByStr">The order by STR.</param>
        public OrderByClip(string orderByStr)
        {
            if (orderByStr == null)
            {
                return;
            }

            string[] splittedOrderByStr = orderByStr.Split(',');
            for (int i = 0; i < splittedOrderByStr.Length; ++i)
            {
                bool isDesc = false;
                splittedOrderByStr[i] = splittedOrderByStr[i].Trim();
                if (splittedOrderByStr[i].ToUpper().EndsWith(" DESC"))
                {
                    isDesc = true;
                    splittedOrderByStr[i] = splittedOrderByStr[i].Substring(0, splittedOrderByStr[i].Length - 5);
                }
                orderBys.Add(new KeyValuePair<string, bool>(splittedOrderByStr[i], isDesc));
            }
        }

        /// <summary>
        /// And two orderby clips.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns>The combined order by clip.</returns>
        public static OrderByClip operator &(OrderByClip left, OrderByClip right)
        {
            Check.Require(left != null, "left could not be null.");
            Check.Require(right != null, "right could not be null.");

            if (left.orderBys.Count >= 0 && right.orderBys.Count >= 0)
            {
                OrderByClip newOrderBy = new OrderByClip();
                List<KeyValuePair<string, bool>>.Enumerator en = left.orderBys.GetEnumerator();
                while (en.MoveNext())
                {
                    newOrderBy.orderBys.Add(new KeyValuePair<string, bool>(en.Current.Key, en.Current.Value));
                }
                en = right.orderBys.GetEnumerator();
                while (en.MoveNext())
                {
                    newOrderBy.orderBys.Add(new KeyValuePair<string, bool>(en.Current.Key, en.Current.Value));
                }
                return newOrderBy;
            }
            else if (left.orderBys.Count >= 0 && right.orderBys.Count == 0)
            {
                return left;
            }
            else if (left.orderBys.Count == 0 && right.orderBys.Count > 0)
            {
                return right;
            }
            else
            {
                return Default;
            }
        }

        /// <summary>
        /// Operator trues the specified right.
        /// </summary>
        /// <param name="right">The right.</param>
        /// <returns></returns>
        public static bool operator true(OrderByClip right)
        {
            return false;
        }

        /// <summary>
        /// Operator falses the specified right.
        /// </summary>
        /// <param name="right">The right.</param>
        /// <returns></returns>
        public static bool operator false(OrderByClip right)
        {
            return false;
        }

        /// <summary>
        /// Gets or sets the order by str.
        /// </summary>
        /// <value>The order by.</value>
        public string OrderBy
        {
            get
            {
                return this.ToString();
            }
        }

        public List<KeyValuePair<string, bool>> OrderBys
        {
            get
            {
                return orderBys;
            }
        }
    }

    public class GroupByClip
    {
        private static readonly GroupByClip _Default = new GroupByClip((string)null);

        public static GroupByClip Default
        {
            get
            {
                return _Default;
            }
        }

        List<string> groupBys = new List<string>();

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            List<string>.Enumerator en = groupBys.GetEnumerator();
            while (en.MoveNext())
            {
                if (sb.Length > 0)
                {
                    sb.Append(',');
                }
                sb.Append(en.Current);
            }

            return sb.ToString();
        }

        public GroupByClip()
        {
        }

        public GroupByClip(PropertyItem item)
        {
            groupBys.Add(item.ColumnName);
        }

        public GroupByClip(string groupByStr)
        {
            if (groupByStr == null)
            {
                return;
            }

            string[] splittedGroupByStr = groupByStr.Split(',');
            for (int i = 0; i < splittedGroupByStr.Length; ++i)
            {
                splittedGroupByStr[i] = splittedGroupByStr[i].Trim();
                groupBys.Add(splittedGroupByStr[i]);
            }
        }

        public static GroupByClip operator &(GroupByClip left, GroupByClip right)
        {
            Check.Require(left != null, "left could not be null.");
            Check.Require(right != null, "right could not be null.");

            if (left.groupBys.Count >= 0 && right.groupBys.Count >= 0)
            {
                GroupByClip newGroupBy = new GroupByClip();
                List<string>.Enumerator en = left.groupBys.GetEnumerator();
                while (en.MoveNext())
                {
                    newGroupBy.groupBys.Add(en.Current);
                }
                en = right.groupBys.GetEnumerator();
                while (en.MoveNext())
                {
                    newGroupBy.groupBys.Add(en.Current);
                }
                return newGroupBy;
            }
            else if (left.groupBys.Count >= 0 && right.groupBys.Count == 0)
            {
                return left;
            }
            else if (left.groupBys.Count == 0 && right.groupBys.Count > 0)
            {
                return right;
            }
            else
            {
                return Default;
            }
        }

        /// <summary>
        /// Operator trues the specified right.
        /// </summary>
        /// <param name="right">The right.</param>
        /// <returns></returns>
        public static bool operator true(GroupByClip right)
        {
            return false;
        }

        /// <summary>
        /// Operator falses the specified right.
        /// </summary>
        /// <param name="right">The right.</param>
        /// <returns></returns>
        public static bool operator false(GroupByClip right)
        {
            return false;
        }

        /// <summary>
        /// Gets or sets the order by str.
        /// </summary>
        /// <value>The order by.</value>
        public string GroupBy
        {
            get
            {
                return this.ToString();
            }
        }

        public List<string> GroupBys
        {
            get
            {
                return groupBys;
            }
        }
    }

    /// <summary>
    /// Delegate used to map a property name to a column name.
    /// </summary>
    /// <param name="propertyName"></param>
    /// <returns>The mapping column name.</returns>
    public delegate string PropertyToColumnMapHandler(string propertyName);

    ///// <summary>
    ///// special class used when updating a specified column with value of some other column or combination of several other columns' values.
    ///// </summary>
    //public class PropertyItemParam : PropertyItem
    //{
    //    /// <summary>
    //    /// Initializes a new instance of the <see cref="PropertyItemParam"/> class.
    //    /// </summary>
    //    /// <param name="value">The value.</param>
    //    [Obsolete]
    //    public PropertyItemParam(string value)
    //    {
    //        this.sql.Append(value);
    //    }

    //    public PropertyItemParam(NBear.Common.ExpressionClip expr) : base()
    //    {
    //        this.expr = NBear.Common.ExpressionClip.CreateCloneExpression(expr);
    //    }

    //    /// <summary>
    //    /// custom value
    //    /// </summary>
    //    [Obsolete]
    //    public string CustomValue;

    //    #region + - * / %

    //    /// <summary>
    //    /// Operator +s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns></returns>
    //    public static PropertyItemParam operator +(PropertyItemParam left, PropertyItemParam right)
    //    {
    //        NBear.Common.ExpressionClip expr = NBear.Common.ExpressionClip.CreateCloneExpression(left.expr)
    //            .AppendExpressions(NBear.Common.QueryOperator.Add, right.expr);
    //        PropertyItemParam pip = new PropertyItemParam(expr);
    //        pip.propertyConfig = right.propertyConfig;
    //        return pip;
    //    }

    //    /// <summary>
    //    /// Operator +s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns></returns>
    //    public static PropertyItemParam operator +(object left, PropertyItemParam right)
    //    {
    //        Check.Require(left != null, "left could not be null.");

    //        NBear.Common.ExpressionClip expr = NBear.Common.ExpressionClip.CreateParameterExpression(right.DbType, left)
    //            .AppendExpressions(NBear.Common.QueryOperator.Add, right.expr);
    //        PropertyItemParam pip = new PropertyItemParam(expr);
    //        pip.propertyConfig = right.propertyConfig;
    //        return pip;
    //    }

    //    /// <summary>
    //    /// Operator +s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns></returns>
    //    public static PropertyItemParam operator +(PropertyItemParam left, object right)
    //    {
    //        Check.Require(right != null, "right could not be null.");

    //        NBear.Common.ExpressionClip expr = NBear.Common.ExpressionClip.CreateCloneExpression(left.expr)
    //            .AppendExpressions(NBear.Common.QueryOperator.Add, 
    //            NBear.Common.ExpressionClip.CreateParameterExpression(left.DbType, right));
    //        PropertyItemParam pip = new PropertyItemParam(expr);
    //        pip.propertyConfig = left.propertyConfig;
    //        return pip;
    //    }

    //    /// <summary>
    //    /// Operator +s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns></returns>
    //    public static PropertyItemParam operator +(PropertyItem left, PropertyItemParam right)
    //    {
    //        NBear.Common.ExpressionClip expr = NBear.Common.ExpressionClip.CreateCloneExpression(left.expr)
    //            .AppendExpressions(NBear.Common.QueryOperator.Add, right.expr);
    //        PropertyItemParam pip = new PropertyItemParam(expr);
    //        pip.propertyConfig = left.propertyConfig;
    //        return pip;
    //    }

    //    /// <summary>
    //    /// Operator +s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns></returns>
    //    public static PropertyItemParam operator +(PropertyItemParam left, PropertyItem right)
    //    {
    //        NBear.Common.ExpressionClip expr = NBear.Common.ExpressionClip.CreateCloneExpression(left.expr)
    //            .AppendExpressions(NBear.Common.QueryOperator.Add, right.expr);
    //        PropertyItemParam pip = new PropertyItemParam(expr);
    //        pip.propertyConfig = right.propertyConfig;
    //        return pip;
    //    }

    //    /// <summary>
    //    /// Operator -s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns></returns>
    //    public static PropertyItemParam operator -(PropertyItemParam left, PropertyItemParam right)
    //    {
    //        NBear.Common.ExpressionClip expr = NBear.Common.ExpressionClip.CreateCloneExpression(left.expr)
    //            .AppendExpressions(NBear.Common.QueryOperator.Subtract, right.expr);
    //        PropertyItemParam pip = new PropertyItemParam(expr);
    //        pip.propertyConfig = right.propertyConfig;
    //        return pip;
    //    }

    //    /// <summary>
    //    /// Operator -s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns></returns>
    //    public static PropertyItemParam operator -(object left, PropertyItemParam right)
    //    {
    //        Check.Require(left != null, "left could not be null.");

    //        NBear.Common.ExpressionClip expr = NBear.Common.ExpressionClip.CreateParameterExpression(right.DbType, left)
    //            .AppendExpressions(NBear.Common.QueryOperator.Subtract, right.expr);
    //        PropertyItemParam pip = new PropertyItemParam(expr);
    //        pip.propertyConfig = right.propertyConfig;
    //        return pip;
    //    }

    //    /// <summary>
    //    /// Operator -s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns></returns>
    //    public static PropertyItemParam operator -(PropertyItemParam left, object right)
    //    {
    //        Check.Require(right != null, "right could not be null.");

    //        NBear.Common.ExpressionClip expr = NBear.Common.ExpressionClip.CreateCloneExpression(left.expr)
    //            .AppendExpressions(NBear.Common.QueryOperator.Subtract, 
    //            NBear.Common.ExpressionClip.CreateParameterExpression(left.DbType, right));
    //        PropertyItemParam pip = new PropertyItemParam(expr);
    //        pip.propertyConfig = left.propertyConfig;
    //        return pip;
    //    }

    //    /// <summary>
    //    /// Operator -s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns></returns>
    //    public static PropertyItemParam operator -(PropertyItem left, PropertyItemParam right)
    //    {
    //        NBear.Common.ExpressionClip expr = NBear.Common.ExpressionClip.CreateCloneExpression(left.expr)
    //            .AppendExpressions(NBear.Common.QueryOperator.Subtract, right.expr);
    //        PropertyItemParam pip = new PropertyItemParam(expr);
    //        pip.propertyConfig = left.propertyConfig;
    //        return pip;
    //    }

    //    /// <summary>
    //    /// Operator -s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns></returns>
    //    public static PropertyItemParam operator -(PropertyItemParam left, PropertyItem right)
    //    {
    //        NBear.Common.ExpressionClip expr = NBear.Common.ExpressionClip.CreateCloneExpression(left.expr)
    //            .AppendExpressions(NBear.Common.QueryOperator.Subtract, right.expr);
    //        PropertyItemParam pip = new PropertyItemParam(expr);
    //        pip.propertyConfig = right.propertyConfig;
    //        return pip;
    //    }

    //    /// <summary>
    //    /// Operator *s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns></returns>
    //    public static PropertyItemParam operator *(PropertyItemParam left, PropertyItemParam right)
    //    {
    //        NBear.Common.ExpressionClip expr = NBear.Common.ExpressionClip.CreateCloneExpression(left.expr)
    //            .AppendExpressions(NBear.Common.QueryOperator.Multiply, right.expr);
    //        PropertyItemParam pip = new PropertyItemParam(expr);
    //        pip.propertyConfig = right.propertyConfig;
    //        return pip;
    //    }

    //    /// <summary>
    //    /// Operator *s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns></returns>
    //    public static PropertyItemParam operator *(object left, PropertyItemParam right)
    //    {
    //        Check.Require(left != null, "left could not be null.");

    //        NBear.Common.ExpressionClip expr = NBear.Common.ExpressionClip.CreateParameterExpression(right.DbType, left)
    //            .AppendExpressions(NBear.Common.QueryOperator.Multiply, right.expr);
    //        PropertyItemParam pip = new PropertyItemParam(expr);
    //        pip.propertyConfig = right.propertyConfig;
    //        return pip;
    //    }

    //    /// <summary>
    //    /// Operator *s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns></returns>
    //    public static PropertyItemParam operator *(PropertyItemParam left, object right)
    //    {
    //        Check.Require(right != null, "right could not be null.");

    //        NBear.Common.ExpressionClip expr = NBear.Common.ExpressionClip.CreateCloneExpression(left.expr)
    //            .AppendExpressions(NBear.Common.QueryOperator.Multiply, 
    //            NBear.Common.ExpressionClip.CreateParameterExpression(left.DbType, right));
    //        PropertyItemParam pip = new PropertyItemParam(expr);
    //        pip.propertyConfig = left.propertyConfig;
    //        return pip;
    //    }

    //    /// <summary>
    //    /// Operator *s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns></returns>
    //    public static PropertyItemParam operator *(PropertyItem left, PropertyItemParam right)
    //    {
    //        NBear.Common.ExpressionClip expr = NBear.Common.ExpressionClip.CreateCloneExpression(left.expr)
    //            .AppendExpressions(NBear.Common.QueryOperator.Multiply, right.expr);
    //        PropertyItemParam pip = new PropertyItemParam(expr);
    //        pip.propertyConfig = left.propertyConfig;
    //        return pip;
    //    }

    //    /// <summary>
    //    /// Operator *s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns></returns>
    //    public static PropertyItemParam operator *(PropertyItemParam left, PropertyItem right)
    //    {
    //        NBear.Common.ExpressionClip expr = NBear.Common.ExpressionClip.CreateCloneExpression(left.expr)
    //            .AppendExpressions(NBear.Common.QueryOperator.Multiply, right.expr);
    //        PropertyItemParam pip = new PropertyItemParam(expr);
    //        pip.propertyConfig = right.propertyConfig;
    //        return pip;
    //    }

    //    /// <summary>
    //    /// Operator /s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns></returns>
    //    public static PropertyItemParam operator /(PropertyItemParam left, PropertyItemParam right)
    //    {
    //        NBear.Common.ExpressionClip expr = NBear.Common.ExpressionClip.CreateCloneExpression(left.expr)
    //            .AppendExpressions(NBear.Common.QueryOperator.Divide, right.expr);
    //        PropertyItemParam pip = new PropertyItemParam(expr);
    //        pip.propertyConfig = right.propertyConfig;
    //        return pip;
    //    }

    //    /// <summary>
    //    /// Operator /s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns></returns>
    //    public static PropertyItemParam operator /(object left, PropertyItemParam right)
    //    {
    //        Check.Require(left != null, "left could not be null.");

    //        NBear.Common.ExpressionClip expr = NBear.Common.ExpressionClip.CreateParameterExpression(right.DbType, left)
    //            .AppendExpressions(NBear.Common.QueryOperator.Divide, right.expr);
    //        PropertyItemParam pip = new PropertyItemParam(expr);
    //        pip.propertyConfig = right.propertyConfig;
    //        return pip;
    //    }

    //    /// <summary>
    //    /// Operator /s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns></returns>
    //    public static PropertyItemParam operator /(PropertyItemParam left, object right)
    //    {
    //        Check.Require(right != null, "right could not be null.");

    //        NBear.Common.ExpressionClip expr = NBear.Common.ExpressionClip.CreateCloneExpression(left.expr)
    //            .AppendExpressions(NBear.Common.QueryOperator.Divide, 
    //            NBear.Common.ExpressionClip.CreateParameterExpression(left.DbType, right));
    //        PropertyItemParam pip = new PropertyItemParam(expr);
    //        pip.propertyConfig = left.propertyConfig;
    //        return pip;
    //    }

    //    /// <summary>
    //    /// Operator /s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns></returns>
    //    public static PropertyItemParam operator /(PropertyItem left, PropertyItemParam right)
    //    {
    //        NBear.Common.ExpressionClip expr = NBear.Common.ExpressionClip.CreateCloneExpression(left.expr)
    //            .AppendExpressions(NBear.Common.QueryOperator.Divide, right.expr);
    //        PropertyItemParam pip = new PropertyItemParam(expr);
    //        pip.propertyConfig = left.propertyConfig;
    //        return pip;
    //    }

    //    /// <summary>
    //    /// Operator /s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns></returns>
    //    public static PropertyItemParam operator /(PropertyItemParam left, PropertyItem right)
    //    {
    //        NBear.Common.ExpressionClip expr = NBear.Common.ExpressionClip.CreateCloneExpression(left.expr)
    //            .AppendExpressions(NBear.Common.QueryOperator.Divide, right.expr);
    //        PropertyItemParam pip = new PropertyItemParam(expr);
    //        pip.propertyConfig = right.propertyConfig;
    //        return pip;
    //    }

    //    /// <summary>
    //    /// Operator %s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns></returns>
    //    public static PropertyItemParam operator %(PropertyItemParam left, PropertyItemParam right)
    //    {
    //        NBear.Common.ExpressionClip expr = NBear.Common.ExpressionClip.CreateCloneExpression(left.expr)
    //            .AppendExpressions(NBear.Common.QueryOperator.Modulo, right.expr);
    //        PropertyItemParam pip = new PropertyItemParam(expr);
    //        pip.propertyConfig = right.propertyConfig;
    //        return pip;
    //    }

    //    /// <summary>
    //    /// Operator %s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns></returns>
    //    public static PropertyItemParam operator %(object left, PropertyItemParam right)
    //    {
    //        Check.Require(left != null, "left could not be null.");

    //        NBear.Common.ExpressionClip expr = NBear.Common.ExpressionClip.CreateParameterExpression(right.DbType, left)
    //            .AppendExpressions(NBear.Common.QueryOperator.Modulo, right.expr);
    //        PropertyItemParam pip = new PropertyItemParam(expr);
    //        pip.propertyConfig = right.propertyConfig;
    //        return pip;
    //    }

    //    /// <summary>
    //    /// Operator %s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns></returns>
    //    public static PropertyItemParam operator %(PropertyItemParam left, object right)
    //    {
    //        Check.Require(right != null, "right could not be null.");

    //        NBear.Common.ExpressionClip expr = NBear.Common.ExpressionClip.CreateCloneExpression(left.expr)
    //            .AppendExpressions(NBear.Common.QueryOperator.Modulo, 
    //            NBear.Common.ExpressionClip.CreateParameterExpression(left.DbType, right));
    //        PropertyItemParam pip = new PropertyItemParam(expr);
    //        pip.propertyConfig = left.propertyConfig;
    //        return pip;
    //    }

    //    /// <summary>
    //    /// Operator %s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns></returns>
    //    public static PropertyItemParam operator %(PropertyItem left, PropertyItemParam right)
    //    {
    //        NBear.Common.ExpressionClip expr = NBear.Common.ExpressionClip.CreateCloneExpression(left.expr)
    //            .AppendExpressions(NBear.Common.QueryOperator.Modulo, right.expr);
    //        PropertyItemParam pip = new PropertyItemParam(expr);
    //        pip.propertyConfig = left.propertyConfig;
    //        return pip;
    //    }

    //    /// <summary>
    //    /// Operator %s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns></returns>
    //    public static PropertyItemParam operator %(PropertyItemParam left, PropertyItem right)
    //    {
    //        NBear.Common.ExpressionClip expr = NBear.Common.ExpressionClip.CreateCloneExpression(left.expr)
    //            .AppendExpressions(NBear.Common.QueryOperator.Modulo, right.expr);
    //        PropertyItemParam pip = new PropertyItemParam(expr);
    //        pip.propertyConfig = right.propertyConfig;
    //        return pip;
    //    }

    //    #endregion

    //    /// <summary>
    //    /// Serves as a hash function for a particular type. <see cref="M:System.Object.GetHashCode"></see> is suitable for use in hashing algorithms and data structures like a hash table.
    //    /// </summary>
    //    /// <returns>
    //    /// A hash code for the current <see cref="T:System.Object"></see>.
    //    /// </returns>
    //    public override int GetHashCode()
    //    {
    //        return base.GetHashCode();
    //    }

    //    /// <summary>
    //    /// Determines whether the specified <see cref="T:System.Object"></see> is equal to the current <see cref="T:System.Object"></see>.
    //    /// </summary>
    //    /// <param name="obj">The <see cref="T:System.Object"></see> to compare with the current <see cref="T:System.Object"></see>.</param>
    //    /// <returns>
    //    /// true if the specified <see cref="T:System.Object"></see> is equal to the current <see cref="T:System.Object"></see>; otherwise, false.
    //    /// </returns>
    //    public override bool Equals(object obj)
    //    {
    //        return base.Equals(obj);
    //    }

    //    #region Equals and Not Equals

    //    /// <summary>
    //    /// Operator ==s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns>A where clip.</returns>
    //    public static WhereClip operator ==(PropertyItemParam left, PropertyItemParam right)
    //    {
    //        WhereClip newWhere = new WhereClip();
    //        if (((object)right) == null)
    //        {
    //            newWhere.whereClip.And(left.expr, NBear.Common.QueryOperator.IsNULL, null);
    //        }
    //        else if (((object)left) == null)
    //        {
    //            newWhere.whereClip.And(right.expr, NBear.Common.QueryOperator.IsNULL, null);
    //        }
    //        else
    //        {
    //            newWhere.whereClip.And(left.expr, NBear.Common.QueryOperator.Equal, right.expr);
    //        }

    //        return newWhere;
    //    }

    //    /// <summary>
    //    /// Operator !=s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns>A where clip.</returns>
    //    public static WhereClip operator !=(PropertyItemParam left, PropertyItemParam right)
    //    {
    //        return !(left == right);
    //    }

    //    /// <summary>
    //    /// Operator ==s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns>A where clip.</returns>
    //    public static WhereClip operator ==(PropertyItemParam left, object right)
    //    {
    //        WhereClip newWhere = new WhereClip();
    //        if (right == null || right == DBNull.Value)
    //        {
    //            newWhere.whereClip.And(left.expr, NBear.Common.QueryOperator.IsNULL, null);
    //        }
    //        else
    //        {
    //            newWhere.whereClip.And(left.expr, NBear.Common.QueryOperator.Equal, 
    //                NBear.Common.ExpressionClip.CreateParameterExpression(left.DbType, right));
    //        }

    //        return newWhere;
    //    }

    //    /// <summary>
    //    /// Operator !=s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns>A where clip.</returns>
    //    public static WhereClip operator !=(PropertyItemParam left, object right)
    //    {
    //        return !(left == right);
    //    }

    //    /// <summary>
    //    /// Operator ==s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns>A where clip.</returns>
    //    public static WhereClip operator ==(object left, PropertyItemParam right)
    //    {
    //        WhereClip newWhere = new WhereClip();
    //        if (left == null || left == DBNull.Value)
    //        {
    //            newWhere.whereClip.And(right.expr, NBear.Common.QueryOperator.IsNULL, null);
    //        }
    //        else
    //        {
    //            newWhere.whereClip.And(NBear.Common.ExpressionClip.CreateParameterExpression(right.DbType, left), 
    //                NBear.Common.QueryOperator.Equal, right.expr);
    //        }

    //        return newWhere;
    //    }

    //    /// <summary>
    //    /// Operator !=s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns>A where clip.</returns>
    //    public static WhereClip operator !=(object left, PropertyItemParam right)
    //    {
    //        return !(left == right);
    //    }

    //    /// <summary>
    //    /// Operator ==s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns>A where clip.</returns>
    //    public static WhereClip operator ==(PropertyItemParam left, PropertyItem right)
    //    {
    //        WhereClip newWhere = new WhereClip();
    //        if (((object)right) == null)
    //        {
    //            newWhere.whereClip.And(left.expr, NBear.Common.QueryOperator.IsNULL, null);
    //        }
    //        else if (((object)left) == null)
    //        {
    //            newWhere.whereClip.And(right.expr, NBear.Common.QueryOperator.IsNULL, null);
    //        }
    //        else
    //        {
    //            newWhere.whereClip.And(left.expr, NBear.Common.QueryOperator.Equal, right.expr);
    //        }

    //        return newWhere;
    //    }

    //    /// <summary>
    //    /// Operator !=s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns>A where clip.</returns>
    //    public static WhereClip operator !=(PropertyItemParam left, PropertyItem right)
    //    {
    //        return !(left == right);
    //    }

    //    /// <summary>
    //    /// Operator ==s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns>A where clip.</returns>
    //    public static WhereClip operator ==(PropertyItem left, PropertyItemParam right)
    //    {
    //        WhereClip newWhere = new WhereClip();
    //        if (((object)right) == null)
    //        {
    //            newWhere.whereClip.And(left.expr, NBear.Common.QueryOperator.IsNULL, null);
    //        }
    //        else if (((object)left) == null)
    //        {
    //            newWhere.whereClip.And(right.expr, NBear.Common.QueryOperator.IsNULL, null);
    //        }
    //        else
    //        {
    //            newWhere.whereClip.And(left.expr, NBear.Common.QueryOperator.Equal, right.expr);
    //        }

    //        return newWhere;
    //    }

    //    /// <summary>
    //    /// Operator !=s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns>A where clip.</returns>
    //    public static WhereClip operator !=(PropertyItem left, PropertyItemParam right)
    //    {
    //        return !(left == right);
    //    }

    //    #endregion

    //    #region Greater and Less

    //    /// <summary>
    //    /// Operator &gt;s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns>A where clip.</returns>
    //    public static WhereClip operator >(PropertyItemParam left, PropertyItemParam right)
    //    {
    //        WhereClip newWhere = new WhereClip();
    //        if (((object)right) == null)
    //        {
    //            return (left > (object)null);
    //        }
    //        else if (((object)left) == null)
    //        {
    //            return ((object)null > right);
    //        }
    //        else
    //        {
    //            newWhere.whereClip.And(left.expr, NBear.Common.QueryOperator.Greater, right.expr);
    //        }

    //        return newWhere;
    //    }

    //    /// <summary>
    //    /// Operator &lt;s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns>A where clip.</returns>
    //    public static WhereClip operator <(PropertyItemParam left, PropertyItemParam right)
    //    {
    //        WhereClip newWhere = new WhereClip();
    //        if (((object)right) == null)
    //        {
    //            return (left < (object)null);
    //        }
    //        else if (((object)left) == null)
    //        {
    //            return ((object)null < right);
    //        }
    //        else
    //        {
    //            newWhere.whereClip.And(left.expr, NBear.Common.QueryOperator.Less, right.expr);
    //        }

    //        return newWhere;
    //    }

    //    /// <summary>
    //    /// Operator &gt;s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns>A where clip.</returns>
    //    public static WhereClip operator >(PropertyItemParam left, object right)
    //    {
    //        WhereClip newWhere = new WhereClip();

    //        newWhere.whereClip.And(left.expr, NBear.Common.QueryOperator.Greater, 
    //            NBear.Common.ExpressionClip.CreateParameterExpression(left.DbType, right));

    //        return newWhere;
    //    }

    //    /// <summary>
    //    /// Operator &lt;s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns>A where clip.</returns>
    //    public static WhereClip operator <(PropertyItemParam left, object right)
    //    {
    //        WhereClip newWhere = new WhereClip();

    //        newWhere.whereClip.And(left.expr, NBear.Common.QueryOperator.Less, 
    //            NBear.Common.ExpressionClip.CreateParameterExpression(left.DbType, right));

    //        return newWhere;
    //    }

    //    /// <summary>
    //    /// Operator &gt;s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns>A where clip.</returns>
    //    public static WhereClip operator >(object left, PropertyItemParam right)
    //    {
    //        WhereClip newWhere = new WhereClip();

    //        newWhere.whereClip.And(NBear.Common.ExpressionClip.CreateParameterExpression(right.DbType, left), 
    //            NBear.Common.QueryOperator.Greater, right.expr);

    //        return newWhere;
    //    }

    //    /// <summary>
    //    /// Operator &lt;s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns>A where clip.</returns>
    //    public static WhereClip operator <(object left, PropertyItemParam right)
    //    {
    //        WhereClip newWhere = new WhereClip();

    //        newWhere.whereClip.And(NBear.Common.ExpressionClip.CreateParameterExpression(right.DbType, left), 
    //            NBear.Common.QueryOperator.Less, right.expr);

    //        return newWhere;
    //    }

    //    /// <summary>
    //    /// Operator &gt;s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns>A where clip.</returns>
    //    public static WhereClip operator >(PropertyItemParam left, PropertyItem right)
    //    {
    //        WhereClip newWhere = new WhereClip();
    //        if (((object)right) == null)
    //        {
    //            return (left > (object)null);
    //        }
    //        else if (((object)left) == null)
    //        {
    //            return ((object)null > right);
    //        }
    //        else
    //        {
    //            newWhere.whereClip.And(left.expr, NBear.Common.QueryOperator.Greater, right.expr);
    //        }

    //        return newWhere;
    //    }

    //    /// <summary>
    //    /// Operator &lt;s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns>A where clip.</returns>
    //    public static WhereClip operator <(PropertyItemParam left, PropertyItem right)
    //    {
    //        WhereClip newWhere = new WhereClip();
    //        if (((object)right) == null)
    //        {
    //            return (left < (object)null);
    //        }
    //        else if (((object)left) == null)
    //        {
    //            return ((object)null < right);
    //        }
    //        else
    //        {
    //            newWhere.whereClip.And(left.expr, NBear.Common.QueryOperator.Less, right.expr);
    //        }

    //        return newWhere;
    //    }

    //    /// <summary>
    //    /// Operator &gt;s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns>A where clip.</returns>
    //    public static WhereClip operator >(PropertyItem left, PropertyItemParam right)
    //    {
    //        WhereClip newWhere = new WhereClip();
    //        if (((object)right) == null)
    //        {
    //            return (left > (object)null);
    //        }
    //        else if (((object)left) == null)
    //        {
    //            return ((object)null > right);
    //        }
    //        else
    //        {
    //            newWhere.whereClip.And(left.expr, NBear.Common.QueryOperator.Greater, right.expr);
    //        }

    //        return newWhere;
    //    }

    //    /// <summary>
    //    /// Operator &lt;s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns>A where clip.</returns>
    //    public static WhereClip operator <(PropertyItem left, PropertyItemParam right)
    //    {
    //        WhereClip newWhere = new WhereClip();
    //        if (((object)right) == null)
    //        {
    //            return (left < (object)null);
    //        }
    //        else if (((object)left) == null)
    //        {
    //            return ((object)null < right);
    //        }
    //        else
    //        {
    //            newWhere.whereClip.And(left.expr, NBear.Common.QueryOperator.Less, right.expr);
    //        }

    //        return newWhere;
    //    }

    //    #endregion

    //    #region Greater or Equals and Less and Equals

    //    /// <summary>
    //    /// Operator &gt;=s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns>A where clip.</returns>
    //    public static WhereClip operator >=(PropertyItemParam left, PropertyItemParam right)
    //    {
    //        WhereClip newWhere = new WhereClip();
    //        if (((object)right) == null)
    //        {
    //            return (left >= (object)null);
    //        }
    //        else if (((object)left) == null)
    //        {
    //            return ((object)null >= right);
    //        }
    //        else
    //        {
    //            newWhere.whereClip.And(left.expr, NBear.Common.QueryOperator.GreaterOrEqual, right.expr);
    //        }

    //        return newWhere;
    //    }

    //    /// <summary>
    //    /// Operator &lt;=s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns>A where clip.</returns>
    //    public static WhereClip operator <=(PropertyItemParam left, PropertyItemParam right)
    //    {
    //        WhereClip newWhere = new WhereClip();
    //        if (((object)right) == null)
    //        {
    //            return (left <= (object)null);
    //        }
    //        else if (((object)left) == null)
    //        {
    //            return ((object)null <= right);
    //        }
    //        else
    //        {
    //            newWhere.whereClip.And(left.expr, NBear.Common.QueryOperator.LessOrEqual, right.expr);
    //        }

    //        return newWhere;
    //    }

    //    /// <summary>
    //    /// Operator &gt;=s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns>A where clip.</returns>
    //    public static WhereClip operator >=(PropertyItemParam left, object right)
    //    {
    //        WhereClip newWhere = new WhereClip();

    //        newWhere.whereClip.And(left.expr, NBear.Common.QueryOperator.GreaterOrEqual, 
    //            NBear.Common.ExpressionClip.CreateParameterExpression(left.DbType, right));

    //        return newWhere;
    //    }

    //    /// <summary>
    //    /// Operator &lt;=s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns>A where clip.</returns>
    //    public static WhereClip operator <=(PropertyItemParam left, object right)
    //    {
    //        WhereClip newWhere = new WhereClip();

    //        newWhere.whereClip.And(left.expr, NBear.Common.QueryOperator.LessOrEqual, 
    //            NBear.Common.ExpressionClip.CreateParameterExpression(left.DbType, right));

    //        return newWhere;
    //    }

    //    /// <summary>
    //    /// Operator &gt;=s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns>A where clip.</returns>
    //    public static WhereClip operator >=(object left, PropertyItemParam right)
    //    {
    //        WhereClip newWhere = new WhereClip();

    //        newWhere.whereClip.And(NBear.Common.ExpressionClip.CreateParameterExpression(right.DbType, left), 
    //            NBear.Common.QueryOperator.GreaterOrEqual, right.expr);

    //        return newWhere;
    //    }

    //    /// <summary>
    //    /// Operator &lt;=s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns>A where clip.</returns>
    //    public static WhereClip operator <=(object left, PropertyItemParam right)
    //    {
    //        WhereClip newWhere = new WhereClip();

    //        newWhere.whereClip.And(NBear.Common.ExpressionClip.CreateParameterExpression(right.DbType, left), 
    //            NBear.Common.QueryOperator.LessOrEqual, right.expr);

    //        return newWhere;
    //    }

    //    /// <summary>
    //    /// Operator &gt;=s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns>A where clip.</returns>
    //    public static WhereClip operator >=(PropertyItemParam left, PropertyItem right)
    //    {
    //        WhereClip newWhere = new WhereClip();
    //        if (((object)right) == null)
    //        {
    //            return (left >= (object)null);
    //        }
    //        else if (((object)left) == null)
    //        {
    //            return ((object)null >= right);
    //        }
    //        else
    //        {
    //            newWhere.whereClip.And(left.expr, NBear.Common.QueryOperator.GreaterOrEqual, right.expr);
    //        }

    //        return newWhere;
    //    }

    //    /// <summary>
    //    /// Operator &lt;=s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns>A where clip.</returns>
    //    public static WhereClip operator <=(PropertyItemParam left, PropertyItem right)
    //    {
    //        WhereClip newWhere = new WhereClip();
    //        if (((object)right) == null)
    //        {
    //            return (left <= (object)null);
    //        }
    //        else if (((object)left) == null)
    //        {
    //            return ((object)null <= right);
    //        }
    //        else
    //        {
    //            newWhere.whereClip.And(left.expr, NBear.Common.QueryOperator.LessOrEqual, right.expr);
    //        }

    //        return newWhere;
    //    }

    //    /// <summary>
    //    /// Operator &gt;=s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns>A where clip.</returns>
    //    public static WhereClip operator >=(PropertyItem left, PropertyItemParam right)
    //    {
    //        WhereClip newWhere = new WhereClip();
    //        if (((object)right) == null)
    //        {
    //            return (left >= (object)null);
    //        }
    //        else if (((object)left) == null)
    //        {
    //            return ((object)null >= right);
    //        }
    //        else
    //        {
    //            newWhere.whereClip.And(left.expr, NBear.Common.QueryOperator.GreaterOrEqual, right.expr);
    //        }

    //        return newWhere;
    //    }

    //    /// <summary>
    //    /// Operator &lt;=s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <param name="right">The right.</param>
    //    /// <returns>A where clip.</returns>
    //    public static WhereClip operator <=(PropertyItem left, PropertyItemParam right)
    //    {
    //        WhereClip newWhere = new WhereClip();
    //        if (((object)right) == null)
    //        {
    //            return (left <= (object)null);
    //        }
    //        else if (((object)left) == null)
    //        {
    //            return ((object)null <= right);
    //        }
    //        else
    //        {
    //            newWhere.whereClip.And(left.expr, NBear.Common.QueryOperator.LessOrEqual, right.expr);
    //        }

    //        return newWhere;
    //    }

    //    #endregion

    //    #region Additional Operations

    //    /// <summary>
    //    /// Operator !s the specified left.
    //    /// </summary>
    //    /// <param name="left">The left.</param>
    //    /// <returns></returns>
    //    public static PropertyItemParam operator!(PropertyItemParam left)
    //    {
    //        NBear.Common.ExpressionClip expr = NBear.Common.ExpressionClip.CreateEmptyExpression()
    //            .AppendExpressions(NBear.Common.QueryOperator.BitwiseNOT, 
    //            left.expr);
    //        PropertyItemParam pip = new PropertyItemParam(expr);
    //        pip.propertyConfig = left.propertyConfig;
    //        return pip;
    //    }

    //    #endregion

    //    public override string ToString()
    //    {
    //        return Util.ToString(expr);
    //    }
    //}

    /// <summary>
    /// Strong type query class for entity array
    /// </summary>
    /// <typeparam name="EntityType"></typeparam>
    public class EntityArrayQuery<EntityType>
        where EntityType : Entity, new()
    {
        #region Private Members

        private DataTable dt;
        private static EntityConfiguration ec = new EntityType().GetEntityConfiguration();
        private Entity.QueryProxyHandler handler;

        [Obsolete]
        private static string ParseExpressionByMetaData(string sql)
        {
            return PropertyItem.ParseExpressionByMetaData(sql, new PropertyToColumnMapHandler(ec.GetMappingColumnName), "[", "]", "@");
        }

        private static string[] DiscoverParams(string sql)
        {
            if (sql == null)
            {
                return null;
            }

            System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex(@"@([\w\d_]+)");
            System.Text.RegularExpressions.MatchCollection ms = r.Matches(sql);

            if (ms.Count == 0)
            {
                return null;
            }

            string[] paramNames = new string[ms.Count];
            for (int i = 0; i < ms.Count; i++)
            {
                paramNames[i] = ms[i].Value;
            }
            return paramNames;
        }

        private static string ParseWhereClip(NBear.Common.WhereClip where)
        {
            if (NBear.Common.WhereClip.IsNullOrEmpty(where))
            {
                return null;
            }

            string sql = where.Sql;

            //remove table alias name prefixes
            sql = RemoveTableAliasNamePrefixes(sql);

            if (!string.IsNullOrEmpty(sql))
            {
                Dictionary<string, KeyValuePair<DbType, object>>.Enumerator en = where.Parameters.GetEnumerator();

                while (en.MoveNext())
                {
                    if (en.Current.Value.Value == null || en.Current.Value.Value == DBNull.Value)
                    {
                        sql = sql.Replace('@' + en.Current.Key, Util.FormatParamVal(en.Current.Value.Value));
                    }
                    else if (en.Current.Value.Value is NBear.Common.ExpressionClip)
                    {
                        sql = sql.Replace('@' + en.Current.Key, Util.ToString((NBear.Common.ExpressionClip)en.Current.Value.Value));
                    }
                    else
                    {
                        sql = sql.Replace('@' + en.Current.Key, Util.FormatParamVal(en.Current.Value.Value));
                    }
                }
            }

            sql = sql.Replace(" N'", " '");

            return where.IsNot ? "NOT (" + sql + ")" : sql;
        }

        private static string RemoveTableAliasNamePrefixes(string sql)
        {
            System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex(@"\[([\w\d\s_]+)\].\[([\w\d\s_]+)\]");
            sql = r.Replace(sql, "[$2]");
            return sql;
        }

        private EntityType CreateEntity()
        {
            EntityType obj = new EntityType();
            obj.SetQueryProxy(handler);
            obj.Attach();
            return obj;
        }
        
        private EntityType[] ToEntityArray(DataRow[] rows)
        {
            List<EntityType> list = new List<EntityType>();
            if (rows != null)
            {
                foreach (DataRow row in rows)
                {
                    EntityType retObj = CreateEntity();
                    retObj.SetPropertyValues(row);
                    list.Add(retObj);
                }
            }
            return list.ToArray();
        }

        private EntityType[] ToEntityArray(DataRow[] rows, int topCount, int skipCount)
        {
            List<EntityType> list = new List<EntityType>();
            if (rows != null)
            {
                int i = 0;
                foreach (DataRow row in rows)
                {
                    if (i >= skipCount)
                    {
                        EntityType retObj = CreateEntity();
                        retObj.SetPropertyValues(row);
                        list.Add(retObj);
                    }

                    i++;

                    if (list.Count >= topCount)
                    {
                        break;
                    }
                }
            }
            return list.ToArray();
        }

        private EntityType ToEntity(DataRow[] rows)
        {
            EntityType[] list = ToEntityArray(rows);
            return list.Length > 0 ? list[0] : null;
        }

        private static NBear.Common.WhereClip BuildEqualWhereClip(object[] values, List<PropertyConfiguration> pks)
        {
            NBear.Common.WhereClip where = null;

            for (int i = 0; i < pks.Count; i++)
            {
                if (((object)where) == null)
                {
                    where = new PropertyItem(pks[i].Name, ec) == values[i];
                }
                else
                {
                    where = where & (new PropertyItem(pks[i].Name, ec) == values[i]);
                }
            }
            return where;
        }

        private object FindScalar(string column, NBear.Common.WhereClip where)
        {
            if (column.Contains("("))
            {
                //aggregate query
                if (column.StartsWith("COUNT(DISTINCT "))
                {
                    string columName = column.Substring(15).Trim(' ', ')').Replace("[", string.Empty).Replace("]", string.Empty);
                    List<object> list = new List<object>();
                    foreach (DataRow row in dt.Rows)
                    {
                        object columnValue = row[columName];
                        if (!list.Contains(columnValue))
                        {
                            list.Add(columnValue);
                        }
                    }
                    return list.Count;
                }
                else if (column.StartsWith("COUNT("))
                {
                    return dt.Rows.Count;
                }
                else
                {
                    return dt.Compute(column, ParseWhereClip(where));
                }
            }
            else
            {
                //scalar query
                DataRow[] rows;
                if (NBear.Common.WhereClip.IsNullOrEmpty(where))
                {
                    rows = dt.Select();
                }
                else
                {
                    rows = dt.Select(ParseWhereClip(where));
                }
                if (rows != null && rows.Length > 0)
                {
                    return rows[0][column.TrimStart('[').TrimEnd(']')];
                }
            }

            return 0;
        }

        #endregion

        #region Public Members

        public EntityArrayQuery(EntityType[] arr)
        {
            Check.Require(arr != null && arr.Length > 0, "arr could not be null or empty.");

            dt = Entity.EntityArrayToDataTable<EntityType>(arr);
            handler = arr[0].onQuery;
        }

        /// <summary>
        /// Finds the specified entity.
        /// </summary>
        /// <param name="pkValues">The pk values.</param>
        /// <returns></returns>
        public EntityType Find(params object[] pkValues)
        {
            Check.Require(pkValues != null && pkValues.Length > 0, "pkValues could not be null or empty.");

            List<PropertyConfiguration> pks = ec.GetPrimaryKeyProperties();
            NBear.Common.WhereClip where = BuildEqualWhereClip(pkValues, pks);

            return Find(where);
        }

        /// <summary>
        /// Finds the specified entity.
        /// </summary>
        /// <param name="where">The where.</param>
        /// <returns></returns>
        public EntityType Find(NBear.Common.WhereClip where)
        {
            //Check.Require(where != null, "where could not be null.");

            return ToEntity(dt.Select(ParseWhereClip(where)));
        }

        /// <summary>
        /// Existses the specified entity.
        /// </summary>
        /// <param name="pkValues">The pk values.</param>
        /// <returns></returns>
        public bool Exists(params object[] pkValues)
        {
            Check.Require(pkValues != null && pkValues.Length > 0, "pkValues could not be null or empty.");

            return Find(pkValues) != null;
        }

        /// <summary>
        /// Existses the specified entity.
        /// </summary>
        /// <param name="where">The where.</param>
        /// <returns></returns>
        public bool Exists(NBear.Common.WhereClip where)
        {
            //Check.Require(where != null, "where could not be null.");

            return Find(where) != null;
        }

        /// <summary>
        /// Finds all entities.
        /// </summary>
        /// <returns></returns>
        public EntityType[] FindArray()
        {
            return ToEntityArray(dt.Select());
        }

        /// <summary>
        /// Finds the array.
        /// </summary>
        /// <param name="where">The where.</param>
        /// <returns></returns>
        public EntityType[] FindArray(NBear.Common.WhereClip where)
        {
            return ToEntityArray(dt.Select(ParseWhereClip(where)));
        }

        /// <summary>
        /// Finds the array.
        /// </summary>
        /// <param name="where">The where.</param>
        /// <param name="orderBy">The order by.</param>
        /// <returns></returns>
        public EntityType[] FindArray(NBear.Common.WhereClip where, OrderByClip orderBy)
        {
            return ToEntityArray(dt.Select(ParseWhereClip(where), RemoveTableAliasNamePrefixes(orderBy.ToString())));
        }

        public EntityType[] FindArray(NBear.Common.WhereClip where, OrderByClip orderBy, int topCount, int skipCount)
        {
            return ToEntityArray(dt.Select(ParseWhereClip(where), RemoveTableAliasNamePrefixes(orderBy.ToString())), topCount, skipCount);
        }

        /// <summary>
        /// Finds the array.
        /// </summary>
        /// <param name="orderBy">The order by.</param>
        /// <returns></returns>
        public EntityType[] FindArray(OrderByClip orderBy)
        {
            return ToEntityArray(dt.Select(null, RemoveTableAliasNamePrefixes(orderBy.ToString())));
        }

        /// <summary>
        /// Avgs the specified property.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="where">The where.</param>
        /// <returns></returns>
        public object Avg(PropertyItem property, NBear.Common.WhereClip where)
        {
            return FindScalar(string.Format("AVG({0})", property.ColumnNameWithoutPrefix), where);
        }

        /// <summary>
        /// Sums the specified property.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="where">The where.</param>
        /// <returns></returns>
        public object Sum(PropertyItem property, NBear.Common.WhereClip where)
        {
            return FindScalar(string.Format("SUM({0})", property.ColumnNameWithoutPrefix), where);
        }

        /// <summary>
        /// MAXs the specified property.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="where">The where.</param>
        /// <returns></returns>
        public object Max(PropertyItem property, NBear.Common.WhereClip where)
        {
            return FindScalar(string.Format("MAX({0})", property.ColumnNameWithoutPrefix), where);
        }

        /// <summary>
        /// MINs the specified property.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="where">The where.</param>
        /// <returns></returns>
        public object Min(PropertyItem property, NBear.Common.WhereClip where)
        {
            return FindScalar(string.Format("MIN({0})", property.ColumnNameWithoutPrefix), where);
        }

        /// <summary>
        /// Counts the specified property.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="where">The where.</param>
        /// <param name="isDistinct">if set to <c>true</c> [is distinct].</param>
        /// <returns></returns>
        public object Count(PropertyItem property, NBear.Common.WhereClip where, bool isDistinct)
        {
            return FindScalar(string.Format("COUNT({1}{0})", property.ColumnNameWithoutPrefix, isDistinct ? "DISTINCT " : string.Empty), where);
        }

        /// <summary>
        /// Counts this instance.
        /// </summary>
        /// <returns></returns>
        public int Count()
        {
            return dt.Rows.Count;
        }

        #endregion
    }
}
