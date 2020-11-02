using System;
using System.Collections.Generic;
using System.Text;
using CN.Teddy.DesignByContract;

namespace NBear.Common
{
    public class FromClip
    {
        #region Protected Members

        protected readonly string tableOrViewName;
        protected readonly string aliasName;
        protected readonly Dictionary<string, KeyValuePair<string, WhereClip>> joins = new Dictionary<string, KeyValuePair<string, WhereClip>>();

        #endregion

        #region Properties

        public string TableOrViewName
        {
            get
            {
                return tableOrViewName;
            }
        }

        public string AliasName
        {
            get
            {
                return aliasName;
            }
        }

        public Dictionary<string, KeyValuePair<string, WhereClip>> Joins
        {
            get
            {
                return joins;
            }
        }

        #endregion

        #region Constructors

        public FromClip(string tableOrViewName, string aliasName)
        {
            Check.Require(!string.IsNullOrEmpty(tableOrViewName), "tableName could not be null or empty!");
            Check.Require(!string.IsNullOrEmpty(aliasName), "aliasName could not be null or empty!");

            this.tableOrViewName = tableOrViewName;
            this.aliasName = aliasName;
        }

        public FromClip(string tableOrViewName) : this(tableOrViewName, tableOrViewName)
        {
        }

        #endregion

        #region Public Members

        public FromClip Join(string tableOrViewName, WhereClip onWhere)
        {
            return Join(tableOrViewName, tableOrViewName, onWhere);
        }

        public FromClip Join(string tableOrViewName, string aliasName, WhereClip onWhere)
        {
            Check.Require(!string.IsNullOrEmpty(tableOrViewName), "tableName could not be null or empty!");
            Check.Require(!string.IsNullOrEmpty(aliasName), "aliasName could not be null or empty!");
            Check.Require(((object)onWhere) != null && onWhere.From == null, "onWhere could not be null, onWhere.From must be null in Join!");

            if (joins.ContainsKey(aliasName))
            {
                throw new NameDuplicatedException("In joins list: aliasName - " + aliasName);
            }

            joins.Add(aliasName, new KeyValuePair<string, WhereClip>(tableOrViewName, onWhere));

            return this;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append('[');
            sb.Append(tableOrViewName.TrimStart('[').TrimEnd(']'));
            sb.Append(']');
            if (aliasName != tableOrViewName)
            {
                sb.Append(' ');
                sb.Append('[');
                sb.Append(aliasName.TrimStart('[').TrimEnd(']'));
                sb.Append(']');
            }

            foreach (string joinAliasName in joins.Keys)
            {
                if (sb.ToString().Contains("INNER JOIN"))
                {
                    sb = new StringBuilder('(' + sb.ToString() + ')');
                }

                KeyValuePair<string, WhereClip> keyWhere = joins[joinAliasName];
                sb.Append(" INNER JOIN ");
                sb.Append('[');
                sb.Append(keyWhere.Key.TrimStart('[').TrimEnd(']'));
                sb.Append(']');
                if (joinAliasName != keyWhere.Key)
                {
                    sb.Append(' ');
                    sb.Append('[');
                    sb.Append(joinAliasName.TrimStart('[').TrimEnd(']'));
                    sb.Append(']');
                }
                sb.Append(" ON ");
                sb.Append(keyWhere.Value.ToString());
            }

            return sb.ToString();
        }

        #endregion
    }
}
