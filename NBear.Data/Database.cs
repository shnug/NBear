using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Reflection;
using System.Text;
using NBear.Common;

namespace NBear.Data
{
    /// <summary>
    /// As a DAAB like database object.
    /// </summary>
    public sealed class Database : ILogable
    {
        #region Cache

        private DbParameterCache parameterCache;
        private Dictionary<string, string[]> parameterNameCache = new Dictionary<string, string[]>();

        /// <summary>
        /// Gets the parameter cache.
        /// </summary>
        /// <value>The parameter cache.</value>
        internal DbParameterCache ParameterCache
        {
            get
            {
                return parameterCache;
            }
        }

        /// <summary>
        /// Gets the parameter name cache.
        /// </summary>
        /// <value>The parameter name cache.</value>
        internal Dictionary<string, string[]> ParameterNameCache
        {
            get
            {
                return parameterNameCache;
            }
        }

        /// <summary>
        /// Parses the param pks.
        /// </summary>
        /// <param name="sql">The SQL.</param>
        /// <returns></returns>
        internal string[] ParseParamNames(string sql)
        {
            if (string.IsNullOrEmpty(sql))
            {
                return null;
            }

            string[] paramNames;
            lock (parameterNameCache)
            {
                if (parameterNameCache.ContainsKey(sql))
                {
                    paramNames = parameterNameCache[sql];
                }
                else
                {
                    paramNames = DiscoverParams(sql);
                    parameterNameCache.Add(sql, paramNames);

                }
            }

            return paramNames;
        }

        #endregion

        #region Static Members

        /// <summary>
        /// Get the default database, a default database is mapping the last connection string in config file, and can be modified manually.
        /// </summary>
        public static Database Default;

        /// <summary>
        /// Initializes the <see cref="T:Database"/> class.
        /// </summary>
        static Database()
        {
            if (DbProviderFactory.Default == null)
            {
                Default = null;
            }
            else
            {
                Default = new Database(DbProviderFactory.Default);
            }
        }

        #endregion

        #region Private Members

        private DbProvider dbProvider;

        private DbCommand CreateCommandByCommandType(CommandType commandType, string commandText)
        {
            DbCommand command = dbProvider.DbProviderFactory.CreateCommand();
            command.CommandType = commandType;
            command.CommandText = commandText;

            return command;
        }
        private void DoLoadDataSet(DbCommand command, DataSet dataSet, string[] tableNames)
        {
            Check.Require(tableNames != null && tableNames.Length > 0, "tableNames could not be null or empty.");
            Check.Require(dataSet != null, "dataSet could not be null.");

            using (DbDataAdapter adapter = GetDataAdapter())
            {
                WriteLog(command);

                ((IDbDataAdapter)adapter).SelectCommand = command;

                try
                {
                    string systemCreatedTableNameRoot = "Table";
                    for (int i = 0; i < tableNames.Length; i++)
                    {
                        string systemCreatedTableName = (i == 0)
                             ? systemCreatedTableNameRoot
                             : systemCreatedTableNameRoot + i;

                        adapter.TableMappings.Add(systemCreatedTableName, tableNames[i]);
                    }

                    adapter.Fill(dataSet);
                }
                catch
                {
                    throw;
                }
            }
        }
        private object DoExecuteScalar(DbCommand command)
        {
            try
            {
                WriteLog(command);

                object returnValue = command.ExecuteScalar();
                return returnValue;
            }
            catch
            {
                throw;
            }
            finally
            {
                CloseConnection(command);
            }
        }

        private int DoExecuteNonQuery(DbCommand command)
        {
            if (IsBatchConnection)
            {
                batchCommander.Process(command);
                return 0;
            }

            try
            {
                WriteLog(command);

                int rowsAffected = command.ExecuteNonQuery();
                return rowsAffected;
            }
            catch
            {
                throw;
            }
        }
        private IDataReader DoExecuteReader(DbCommand command, CommandBehavior cmdBehavior)
        {
            try
            {
                WriteLog(command);

                IDataReader reader = command.ExecuteReader(cmdBehavior);
                return reader;
            }
            catch
            {
                throw;
            }
        }
        private DbTransaction BeginTransaction(DbConnection connection)
        {
            return connection.BeginTransaction();
        }
        private IDbTransaction BeginTransaction(DbConnection connection, IsolationLevel il)
        {
            return connection.BeginTransaction(il);
        }
        private void PrepareCommand(DbCommand command, DbConnection connection)
        {
            Check.Require(command != null, "command could not be null.");
            Check.Require(connection != null, "connection could not be null.");

            command.Connection = connection;

            if (this.dbProvider.GetType() == typeof(MsAccess.AccessDbProvider))
            {
                command.CommandText = FilterNTextPrefix(command.CommandText);
            }
        }

        private void PrepareCommand(DbCommand command, DbTransaction transaction)
        {
            Check.Require(command != null, "command could not be null.");
            Check.Require(transaction != null, "transaction could not be null.");

            PrepareCommand(command, transaction.Connection);
            command.Transaction = transaction;

            if (this.dbProvider.GetType() == typeof(MsAccess.AccessDbProvider))
            {
                command.CommandText = FilterNTextPrefix(command.CommandText);
            }
        }
        private static void ConfigureParameter(DbParameter param, string name, DbType dbType, int size, ParameterDirection direction, bool nullable, byte precision, byte scale, string sourceColumn, DataRowVersion sourceVersion, object value)
        {
            param.DbType = dbType;
            param.Size = size;
            param.Value = (value == null) ? DBNull.Value : value;
            param.Direction = direction;
            param.IsNullable = nullable;
            param.SourceColumn = sourceColumn;
            param.SourceVersion = sourceVersion;
        }
        private DbParameter CreateParameter(string name, DbType dbType, int size, ParameterDirection direction, bool nullable, byte precision, byte scale, string sourceColumn, DataRowVersion sourceVersion, object value)
        {
            DbParameter param = CreateParameter(name);
            ConfigureParameter(param, name, dbType, size, direction, nullable, precision, scale, sourceColumn, sourceVersion, value);
            return param;
        }
        private DbParameter CreateParameter(string name)
        {
            DbParameter param = dbProvider.DbProviderFactory.CreateParameter();
            param.ParameterName = dbProvider.BuildParameterName(name);

            return param;
        }

        private string FilterNTextPrefix(string sql)
        {
            if (sql == null)
            {
                return sql;
            }

            return sql.Replace(" N'", " '");
        }

        #endregion

        #region Close Connection

        /// <summary>
        /// Closes the connection.
        /// </summary>
        /// <param name="command">The command.</param>
        public void CloseConnection(DbCommand command)
        {
            if (command != null && command.Connection.State != ConnectionState.Closed && batchConnection == null)
            {
                if (command.Transaction == null)
                {
                    CloseConnection(command.Connection);
                    command.Dispose();
                }
            }
        }

        /// <summary>
        /// Closes the connection.
        /// </summary>
        /// <param name="conn">The conn.</param>
        public void CloseConnection(DbConnection conn)
        {
            if (conn != null && conn.State != ConnectionState.Closed)
            try
            {
                conn.Close();
                conn.Dispose();
            }
            catch
            {
            }
        }

        /// <summary>
        /// Closes the connection.
        /// </summary>
        /// <param name="tran">The tran.</param>
        public void CloseConnection(DbTransaction tran)
        {
            if (tran.Connection != null)
            {
                CloseConnection(tran.Connection);
                tran.Dispose();
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Database"/> class.
        /// </summary>
        /// <param name="dbProvider">The db provider.</param>
        public Database(DbProvider dbProvider) : this()
        {
            Check.Require(dbProvider != null, "dbProvider could not be null.");

            this.dbProvider = dbProvider;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Database"/> class.
        /// </summary>
        public Database()
        {
            this.parameterCache = new DbParameterCache(this);
        }

        #endregion

        #region Public Members

        #region Properties

        /// <summary>
        /// Gets the connect string.
        /// </summary>
        /// <value>The connect string.</value>
        public string ConnectionString
        {
            get
            {
                return dbProvider.ConnectionString;
            }
        }

        /// <summary>
        /// Gets the db provider.
        /// </summary>
        /// <value>The db provider.</value>
        public DbProvider DbProvider
        {
            get
            {
                return dbProvider;
            }
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Gets the connection.
        /// </summary>
        /// <returns></returns>
        public DbConnection GetConnection()
        {
            if (batchConnection == null)
            {
                return CreateConnection();
            }
            else
            {
                return batchConnection;
            }
        }

        /// <summary>
        /// Gets the connection.
        /// </summary>
        /// <param name="tryOpen">if set to <c>true</c> [try open].</param>
        /// <returns></returns>
        public DbConnection GetConnection(bool tryOpen)
        {
            if (batchConnection == null)
            {
                return CreateConnection(tryOpen);
            }
            else
            {
                return batchConnection;
            }
        }

        /// <summary>
        /// <para>When overridden in a derived class, gets the connection for this database.</para>
        /// <seealso cref="DbConnection"/>        
        /// </summary>
        /// <returns>
        /// <para>The <see cref="DbConnection"/> for this database.</para>
        /// </returns>
        public DbConnection CreateConnection()
        {
            DbConnection newConnection = dbProvider.DbProviderFactory.CreateConnection();
            newConnection.ConnectionString = ConnectionString;

            return newConnection;
        }

        /// <summary>
        /// <para>When overridden in a derived class, gets the connection for this database.</para>
        /// <seealso cref="DbConnection"/>        
        /// </summary>
        /// <returns>
        /// <para>The <see cref="DbConnection"/> for this database.</para>
        /// </returns>
        public DbConnection CreateConnection(bool tryOpenning)
        {
            if (!tryOpenning)
            {
                return CreateConnection();
            }

            DbConnection connection = null;
            try
            {
                connection = CreateConnection();
                connection.Open();
            }
            catch
            {
                try
                {
                    connection.Close();
                }
                catch
                {
                }

                throw;
            }

            return connection;
        }

        /// <summary>
        /// <para>When overridden in a derived class, creates a <see cref="DbCommand"/> for a stored procedure.</para>
        /// </summary>
        /// <param name="storedProcedureName"><para>The name of the stored procedure.</para></param>
        /// <returns><para>The <see cref="DbCommand"/> for the stored procedure.</para></returns>       
        public DbCommand GetStoredProcCommand(string storedProcedureName)
        {
            Check.Require(!string.IsNullOrEmpty(storedProcedureName), "storedProcedureName could not be null.");

            return CreateCommandByCommandType(CommandType.StoredProcedure, storedProcedureName);
        }

        /// <summary>
        /// <para>When overridden in a derived class, creates an <see cref="DbCommand"/> for a SQL query.</para>
        /// </summary>
        /// <param name="query"><para>The text of the query.</para></param>        
        /// <returns><para>The <see cref="DbCommand"/> for the SQL query.</para></returns>        
        public DbCommand GetSqlStringCommand(string query)
        {
            Check.Require(!string.IsNullOrEmpty(query), "query could not be null.");

            return CreateCommandByCommandType(CommandType.Text, query);
        }

        /// <summary>
        /// Gets a DbDataAdapter with Standard update behavior.
        /// </summary>
        /// <returns>A <see cref="DbDataAdapter"/>.</returns>
        /// <seealso cref="DbDataAdapter"/>
        public DbDataAdapter GetDataAdapter()
        {
            return dbProvider.DbProviderFactory.CreateDataAdapter();
        }

        /// <summary>
        /// <para>Creates an <see cref="IPageSplit"/> for a SQL page splitable select query.</para>
        /// </summary>
        /// <param name="selectStatement"><para>The text of the basic select query for all rows.</para></param>        
        /// <param name="keyColumn"><para>The sigle main DEFAULT_KEY of the query.</para></param>        
        /// <param name="paramValues"><para>The param values of the query.</para></param>        
        /// <returns><para>The <see cref="IPageSplit"/> for the SQL query.</para></returns>        
        [Obsolete]
        public IPageSplit GetPageSplit(string selectStatement, string keyColumn, object[] paramValues)
        {
            IPageSplit ps = dbProvider.CreatePageSplit(this, selectStatement, keyColumn, paramValues);
            return ps;
        }

        /// <summary>
        /// Gets the statement factory.
        /// </summary>
        /// <returns></returns>
        [Obsolete]
        public IStatementFactory GetStatementFactory()
        {
            return dbProvider.CreateStatementFactory();
        }

        #endregion

        #region Load & Execute Methods

        /// <summary>
        /// <para>Loads a <see cref="DataSet"/> from command text in a transaction.</para>
        /// </summary>
        /// <param name="transaction">
        /// <para>The <see cref="IDbTransaction"/> to execute the command in.</para>
        /// </param>
        /// <param name="commandType">
        /// <para>One of the <see cref="CommandType"/> values.</para>
        /// </param>
        /// <param name="commandText">
        /// <para>The command text to execute.</para>
        /// </param>
        /// <param name="dataSet">
        /// <para>The <see cref="DataSet"/> to fill.</para>
        /// </param>
        /// <param name="tableNames">
        /// <para>An array of table name mappings for the <see cref="DataSet"/>.</para>
        /// </param>
        public void LoadDataSet(DbTransaction transaction, CommandType commandType, string commandText, DataSet dataSet, string[] tableNames)
        {
            using (DbCommand command = CreateCommandByCommandType(commandType, commandText))
            {
                LoadDataSet(command, dataSet, tableNames, transaction);
            }
        }

        /// <summary>
        /// <para>Executes the <paramref name="commandText"/> interpreted as specified by the <paramref name="commandType" /> and returns an <see cref="IDataReader"></see> through which the result can be read.
        /// It is the responsibility of the caller to close the connection and reader when finished.</para>
        /// </summary>
        /// <param name="commandType">
        /// <para>One of the <see cref="CommandType"/> values.</para>
        /// </param>
        /// <param name="commandText">
        /// <para>The command text to execute.</para>
        /// </param>
        /// <returns>
        /// <para>An <see cref="IDataReader"/> object.</para>
        /// </returns>        
        public IDataReader ExecuteReader(CommandType commandType, string commandText)
        {
            using (DbCommand command = CreateCommandByCommandType(commandType, commandText))
            {
                return ExecuteReader(command);
            }
        }

        /// <summary>
        /// <para>Executes the <paramref name="commandText"/> interpreted as specified by the <paramref name="commandType" /> within the given 
        /// <paramref name="transaction" /> and returns an <see cref="IDataReader"></see> through which the result can be read.
        /// It is the responsibility of the caller to close the connection and reader when finished.</para>
        /// </summary>
        /// <param name="transaction">
        /// <para>The <see cref="IDbTransaction"/> to execute the command within.</para>
        /// </param>
        /// <param name="commandType">
        /// <para>One of the <see cref="CommandType"/> values.</para>
        /// </param>
        /// <param name="commandText">
        /// <para>The command text to execute.</para>
        /// </param>
        /// <returns>
        /// <para>An <see cref="IDataReader"/> object.</para>
        /// </returns>        
        public IDataReader ExecuteReader(DbTransaction transaction, CommandType commandType, string commandText)
        {
            using (DbCommand command = CreateCommandByCommandType(commandType, commandText))
            {
                return ExecuteReader(command, transaction);
            }
        }

        /// <summary>
        /// <para>Executes the <paramref name="command"/> and adds a new <see cref="DataTable"></see> to the existing <see cref="DataSet"></see>.</para>
        /// </summary>
        /// <param name="command">
        /// <para>The <see cref="DbCommand"/> to execute.</para>
        /// </param>
        /// <param name="dataSet">
        /// <para>The <see cref="DataSet"/> to load.</para>
        /// </param>
        /// <param name="tableName">
        /// <para>The name for the new <see cref="DataTable"/> to add to the <see cref="DataSet"/>.</para>
        /// </param>        
        /// <exception cref="System.ArgumentNullException">Any input parameter was <see langword="null"/> (<b>Nothing</b> in Visual Basic)</exception>
        /// <exception cref="System.ArgumentException">tableName was an empty string</exception>
        public void LoadDataSet(DbCommand command, DataSet dataSet, string tableName)
        {
            LoadDataSet(command, dataSet, new string[] { tableName });
        }

        /// <summary>
        /// <para>Executes the <paramref name="command"/> within the given <paramref name="transaction" /> and adds a new <see cref="DataTable"></see> to the existing <see cref="DataSet"></see>.</para>
        /// </summary>
        /// <param name="command">
        /// <para>The <see cref="DbCommand"/> to execute.</para>
        /// </param>
        /// <param name="dataSet">
        /// <para>The <see cref="DataSet"/> to load.</para>
        /// </param>
        /// <param name="tableName">
        /// <para>The name for the new <see cref="DataTable"/> to add to the <see cref="DataSet"/>.</para>
        /// </param>
        /// <param name="transaction">
        /// <para>The <see cref="IDbTransaction"/> to execute the command within.</para>
        /// </param>        
        /// <exception cref="System.ArgumentNullException">Any input parameter was <see langword="null"/> (<b>Nothing</b> in Visual Basic).</exception>
        /// <exception cref="System.ArgumentException">tableName was an empty string.</exception>
        public void LoadDataSet(DbCommand command, DataSet dataSet, string tableName, DbTransaction transaction)
        {
            LoadDataSet(command, dataSet, new string[] { tableName }, transaction);
        }

        /// <summary>
        /// <para>Loads a <see cref="DataSet"/> from a <see cref="DbCommand"/>.</para>
        /// </summary>
        /// <param name="command">
        /// <para>The command to execute to fill the <see cref="DataSet"/>.</para>
        /// </param>
        /// <param name="dataSet">
        /// <para>The <see cref="DataSet"/> to fill.</para>
        /// </param>
        /// <param name="tableNames">
        /// <para>An array of table name mappings for the <see cref="DataSet"/>.</para>
        /// </param>
        public void LoadDataSet(DbCommand command, DataSet dataSet, string[] tableNames)
        {
            using (DbConnection connection = GetConnection())
            {
                PrepareCommand(command, connection);
                DoLoadDataSet(command, dataSet, tableNames);
            }
        }

        /// <summary>
        /// <para>Loads a <see cref="DataSet"/> from a <see cref="DbCommand"/> in  a transaction.</para>
        /// </summary>
        /// <param name="command">
        /// <para>The command to execute to fill the <see cref="DataSet"/>.</para>
        /// </param>
        /// <param name="dataSet">
        /// <para>The <see cref="DataSet"/> to fill.</para>
        /// </param>
        /// <param name="tableNames">
        /// <para>An array of table name mappings for the <see cref="DataSet"/>.</para>
        /// </param>
        /// <param name="transaction">
        /// <para>The <see cref="IDbTransaction"/> to execute the command in.</para>
        /// </param>
        public void LoadDataSet(DbCommand command, DataSet dataSet, string[] tableNames, DbTransaction transaction)
        {
            PrepareCommand(command, transaction);
            DoLoadDataSet(command, dataSet, tableNames);
        }

        /// <summary>
        /// <para>Loads a <see cref="DataSet"/> from command text.</para>
        /// </summary>
        /// <param name="commandType">
        /// <para>One of the <see cref="CommandType"/> values.</para>
        /// </param>
        /// <param name="commandText">
        /// <para>The command text to execute.</para>
        /// </param>
        /// <param name="dataSet">
        /// <para>The <see cref="DataSet"/> to fill.</para>
        /// </param>
        /// <param name="tableNames">
        /// <para>An array of table name mappings for the <see cref="DataSet"/>.</para>
        /// </param>
        public void LoadDataSet(CommandType commandType, string commandText, DataSet dataSet, string[] tableNames)
        {
            using (DbCommand command = CreateCommandByCommandType(commandType, commandText))
            {
                LoadDataSet(command, dataSet, tableNames);
            }
        }

        /// <summary>
        /// <para>Executes the <paramref name="command"/> and returns the results in a new <see cref="DataSet"/>.</para>
        /// </summary>
        /// <param name="command"><para>The <see cref="DbCommand"/> to execute.</para></param>
        /// <returns>A <see cref="DataSet"/> with the results of the <paramref name="command"/>.</returns>        
        public DataSet ExecuteDataSet(DbCommand command)
        {
            DataSet dataSet = new DataSet();
            dataSet.Locale = CultureInfo.InvariantCulture;
            LoadDataSet(command, dataSet, "Table");
            return dataSet;
        }

        /// <summary>
        /// <para>Executes the <paramref name="command"/> as part of the <paramref name="transaction" /> and returns the results in a new <see cref="DataSet"/>.</para>
        /// </summary>
        /// <param name="command"><para>The <see cref="DbCommand"/> to execute.</para></param>
        /// <param name="transaction">
        /// <para>The <see cref="IDbTransaction"/> to execute the command within.</para>
        /// </param>
        /// <returns>A <see cref="DataSet"/> with the results of the <paramref name="command"/>.</returns>        
        public DataSet ExecuteDataSet(DbCommand command, DbTransaction transaction)
        {
            DataSet dataSet = new DataSet();
            dataSet.Locale = CultureInfo.InvariantCulture;
            LoadDataSet(command, dataSet, "Table", transaction);
            return dataSet;
        }

        /// <summary>
        /// <para>Executes the <paramref name="commandText"/> interpreted as specified by the <paramref name="commandType" /> and returns the results in a new <see cref="DataSet"/>.</para>
        /// </summary>
        /// <param name="commandType">
        /// <para>One of the <see cref="CommandType"/> values.</para>
        /// </param>
        /// <param name="commandText">
        /// <para>The command text to execute.</para>
        /// </param>
        /// <returns>
        /// <para>A <see cref="DataSet"/> with the results of the <paramref name="commandText"/>.</para>
        /// </returns>
        public DataSet ExecuteDataSet(CommandType commandType, string commandText)
        {
            using (DbCommand command = CreateCommandByCommandType(commandType, commandText))
            {
                return ExecuteDataSet(command);
            }
        }

        /// <summary>
        /// <para>Executes the <paramref name="commandText"/> as part of the given <paramref name="transaction" /> and returns the results in a new <see cref="DataSet"/>.</para>
        /// </summary>
        /// <param name="transaction">
        /// <para>The <see cref="IDbTransaction"/> to execute the command within.</para>
        /// </param>
        /// <param name="commandType">
        /// <para>One of the <see cref="CommandType"/> values.</para>
        /// </param>
        /// <param name="commandText">
        /// <para>The command text to execute.</para>
        /// </param>
        /// <returns>
        /// <para>A <see cref="DataSet"/> with the results of the <paramref name="commandText"/>.</para>
        /// </returns>
        public DataSet ExecuteDataSet(DbTransaction transaction, CommandType commandType, string commandText)
        {
            using (DbCommand command = CreateCommandByCommandType(commandType, commandText))
            {
                return ExecuteDataSet(command, transaction);
            }
        }

        /// <summary>
        /// <para>Executes the <paramref name="command"/> and returns the first column of the first row in the result set returned by the query. Extra columns or rows are ignored.</para>
        /// </summary>
        /// <param name="command">
        /// <para>The command that contains the query to execute.</para>
        /// </param>
        /// <returns>
        /// <para>The first column of the first row in the result set.</para>
        /// </returns>
        /// <seealso cref="IDbCommand.ExecuteScalar"/>
        public object ExecuteScalar(DbCommand command)
        {
            using (DbConnection connection = GetConnection(true))
            {
                PrepareCommand(command, connection);
                return DoExecuteScalar(command);
            }
        }

        /// <summary>
        /// <para>Executes the <paramref name="command"/> within a <paramref name="transaction" />, and returns the first column of the first row in the result set returned by the query. Extra columns or rows are ignored.</para>
        /// </summary>
        /// <param name="command">
        /// <para>The command that contains the query to execute.</para>
        /// </param>
        /// <param name="transaction">
        /// <para>The <see cref="IDbTransaction"/> to execute the command within.</para>
        /// </param>
        /// <returns>
        /// <para>The first column of the first row in the result set.</para>
        /// </returns>
        /// <seealso cref="IDbCommand.ExecuteScalar"/>
        public object ExecuteScalar(DbCommand command, DbTransaction transaction)
        {
            PrepareCommand(command, transaction);
            return DoExecuteScalar(command);
        }

        /// <summary>
        /// <para>Executes the <paramref name="commandText"/> interpreted as specified by the <paramref name="commandType" />  and returns the first column of the first row in the result set returned by the query. Extra columns or rows are ignored.</para>
        /// </summary>
        /// <param name="commandType">
        /// <para>One of the <see cref="CommandType"/> values.</para>
        /// </param>
        /// <param name="commandText">
        /// <para>The command text to execute.</para>
        /// </param>
        /// <returns>
        /// <para>The first column of the first row in the result set.</para>
        /// </returns>
        /// <seealso cref="IDbCommand.ExecuteScalar"/>
        public object ExecuteScalar(CommandType commandType, string commandText)
        {
            using (DbCommand command = CreateCommandByCommandType(commandType, commandText))
            {
                return ExecuteScalar(command);
            }
        }

        /// <summary>
        /// <para>Executes the <paramref name="commandText"/> interpreted as specified by the <paramref name="commandType" /> 
        /// within the given <paramref name="transaction" /> and returns the first column of the first row in the result set returned by the query. Extra columns or rows are ignored.</para>
        /// </summary>
        /// <param name="transaction">
        /// <para>The <see cref="IDbTransaction"/> to execute the command within.</para>
        /// </param>
        /// <param name="commandType">
        /// <para>One of the <see cref="CommandType"/> values.</para>
        /// </param>
        /// <param name="commandText">
        /// <para>The command text to execute.</para>
        /// </param>
        /// <returns>
        /// <para>The first column of the first row in the result set.</para>
        /// </returns>
        /// <seealso cref="IDbCommand.ExecuteScalar"/>
        public object ExecuteScalar(DbTransaction transaction, CommandType commandType, string commandText)
        {
            using (DbCommand command = CreateCommandByCommandType(commandType, commandText))
            {
                return ExecuteScalar(command, transaction);
            }
        }

        /// <summary>
        /// <para>Executes the <paramref name="command"/> and returns the number of rows affected.</para>
        /// </summary>
        /// <param name="command">
        /// <para>The command that contains the query to execute.</para>
        /// </param>       
        /// <seealso cref="IDbCommand.ExecuteScalar"/>
        public int ExecuteNonQuery(DbCommand command)
        {
            if (IsBatchConnection)
            {
                PrepareCommand(command, GetConnection(true));
                return DoExecuteNonQuery(command);
            }
            else
            {
                using (DbConnection connection = GetConnection(true))
                {
                    PrepareCommand(command, connection);
                    return DoExecuteNonQuery(command);
                }
            }
        }

        /// <summary>
        /// <para>Executes the <paramref name="command"/> within the given <paramref name="transaction" />, and returns the number of rows affected.</para>
        /// </summary>
        /// <param name="command">
        /// <para>The command that contains the query to execute.</para>
        /// </param>
        /// <param name="transaction">
        /// <para>The <see cref="IDbTransaction"/> to execute the command within.</para>
        /// </param>
        /// <seealso cref="IDbCommand.ExecuteScalar"/>
        public int ExecuteNonQuery(DbCommand command, DbTransaction transaction)
        {
            PrepareCommand(command, transaction);
            return DoExecuteNonQuery(command);
        }

        /// <summary>
        /// <para>Executes the <paramref name="commandText"/> interpreted as specified by the <paramref name="commandType" /> and returns the number of rows affected.</para>
        /// </summary>
        /// <param name="commandType">
        /// <para>One of the <see cref="CommandType"/> values.</para>
        /// </param>
        /// <param name="commandText">
        /// <para>The command text to execute.</para>
        /// </param>
        /// <returns>
        /// <para>The number of rows affected.</para>
        /// </returns>
        /// <seealso cref="IDbCommand.ExecuteScalar"/>
        public int ExecuteNonQuery(CommandType commandType, string commandText)
        {
            using (DbCommand command = CreateCommandByCommandType(commandType, commandText))
            {
                return ExecuteNonQuery(command);
            }
        }

        /// <summary>
        /// <para>Executes the <paramref name="commandText"/> interpreted as specified by the <paramref name="commandType" /> as part of the given <paramref name="transaction" /> and returns the number of rows affected.</para>
        /// </summary>
        /// <param name="transaction">
        /// <para>The <see cref="IDbTransaction"/> to execute the command within.</para>
        /// </param>
        /// <param name="commandType">
        /// <para>One of the <see cref="CommandType"/> values.</para>
        /// </param>
        /// <param name="commandText">
        /// <para>The command text to execute.</para>
        /// </param>
        /// <returns>
        /// <para>The number of rows affected</para>
        /// </returns>
        /// <seealso cref="IDbCommand.ExecuteScalar"/>
        public int ExecuteNonQuery(DbTransaction transaction, CommandType commandType, string commandText)
        {
            using (DbCommand command = CreateCommandByCommandType(commandType, commandText))
            {
                return ExecuteNonQuery(command, transaction);
            }
        }

        /// <summary>
        /// <para>Executes the <paramref name="command"/> and returns an <see cref="IDataReader"></see> through which the result can be read.
        /// It is the responsibility of the caller to close the connection and reader when finished.</para>
        /// </summary>
        /// <param name="command">
        /// <para>The command that contains the query to execute.</para>
        /// </param>
        /// <returns>
        /// <para>An <see cref="IDataReader"/> object.</para>
        /// </returns>        
        public IDataReader ExecuteReader(DbCommand command)
        {
            DbConnection connection = GetConnection(true);
            PrepareCommand(command, connection);

            try
            {
                return DoExecuteReader(command, CommandBehavior.CloseConnection);
            }
            catch
            {
                try
                {
                    connection.Close();
                }
                catch
                {
                }

                throw;
            }
        }

        /// <summary>
        /// <para>Executes the <paramref name="command"/> within a transaction and returns an <see cref="IDataReader"></see> through which the result can be read.
        /// It is the responsibility of the caller to close the connection and reader when finished.</para>
        /// </summary>
        /// <param name="command">
        /// <para>The command that contains the query to execute.</para>
        /// </param>
        /// <param name="transaction">
        /// <para>The <see cref="IDbTransaction"/> to execute the command within.</para>
        /// </param>
        /// <returns>
        /// <para>An <see cref="IDataReader"/> object.</para>
        /// </returns>        
        public IDataReader ExecuteReader(DbCommand command, DbTransaction transaction)
        {
            PrepareCommand(command, transaction);
            return DoExecuteReader(command, CommandBehavior.Default);
        }

        #endregion

        #region Transactions

        /// <summary>
        /// Begins the transaction.
        /// </summary>
        /// <returns></returns>
        public DbTransaction BeginTransaction()
        {
            return GetConnection(true).BeginTransaction();
        }

        /// <summary>
        /// Begins the transaction.
        /// </summary>
        /// <param name="il">The il.</param>
        /// <returns></returns>
        public DbTransaction BeginTransaction(IsolationLevel il)
        {
            return GetConnection(true).BeginTransaction(il);
        }

        #endregion

        #region DbCommand Parameter Methods

        /// <summary>
        /// Adds a new In <see cref="DbParameter"/> object to the given <paramref name="command"/>.
        /// </summary>
        /// <param name="command">The command to add the parameter.</param>
        /// <param name="name"><para>The name of the parameter.</para></param>
        /// <param name="dbType"><para>One of the <see cref="DbType"/> values.</para></param>
        /// <param name="size"><para>The maximum size of the data within the column.</para></param>
        /// <param name="direction"><para>One of the <see cref="ParameterDirection"/> values.</para></param>
        /// <param name="nullable"><para>Avalue indicating whether the parameter accepts <see langword="null"/> (<b>Nothing</b> in Visual Basic) values.</para></param>
        /// <param name="precision"><para>The maximum number of digits used to represent the <paramref name="value"/>.</para></param>
        /// <param name="scale"><para>The number of decimal places to which <paramref name="value"/> is resolved.</para></param>
        /// <param name="sourceColumn"><para>The name of the source column mapped to the DataSet and used for loading or returning the <paramref name="value"/>.</para></param>
        /// <param name="sourceVersion"><para>One of the <see cref="DataRowVersion"/> values.</para></param>
        /// <param name="value"><para>The value of the parameter.</para></param>       
        public void AddParameter(DbCommand command, string name, DbType dbType, int size, ParameterDirection direction, bool nullable, byte precision, byte scale, string sourceColumn, DataRowVersion sourceVersion, object value)
        {
            DbParameter parameter = CreateParameter(name, dbType == DbType.Object ? DbType.String : dbType, size, direction, nullable, precision, scale, sourceColumn, sourceVersion, value);
            command.Parameters.Add(parameter);
        }

        /// <summary>
        /// <para>Adds a new instance of a <see cref="DbParameter"/> object to the command.</para>
        /// </summary>
        /// <param name="command">The command to add the parameter.</param>
        /// <param name="name"><para>The name of the parameter.</para></param>
        /// <param name="dbType"><para>One of the <see cref="DbType"/> values.</para></param>        
        /// <param name="direction"><para>One of the <see cref="ParameterDirection"/> values.</para></param>                
        /// <param name="sourceColumn"><para>The name of the source column mapped to the DataSet and used for loading or returning the <paramref name="value"/>.</para></param>
        /// <param name="sourceVersion"><para>One of the <see cref="DataRowVersion"/> values.</para></param>
        /// <param name="value"><para>The value of the parameter.</para></param>    
        public void AddParameter(DbCommand command, string name, DbType dbType, ParameterDirection direction, string sourceColumn, DataRowVersion sourceVersion, object value)
        {
            AddParameter(command, name, dbType, 0, direction, false, 0, 0, sourceColumn, sourceVersion, value);
        }

        /// <summary>
        /// Adds a new Out <see cref="DbParameter"/> object to the given <paramref name="command"/>.
        /// </summary>
        /// <param name="command">The command to add the out parameter.</param>
        /// <param name="name"><para>The name of the parameter.</para></param>
        /// <param name="dbType"><para>One of the <see cref="DbType"/> values.</para></param>        
        /// <param name="size"><para>The maximum size of the data within the column.</para></param>        
        public void AddOutParameter(DbCommand command, string name, DbType dbType, int size)
        {
            AddParameter(command, name, dbType, size, ParameterDirection.Output, true, 0, 0, String.Empty, DataRowVersion.Default, DBNull.Value);
        }

        /// <summary>
        /// Adds a new In <see cref="DbParameter"/> object to the given <paramref name="command"/>.
        /// </summary>
        /// <param name="command">The command to add the in parameter.</param>
        /// <param name="name"><para>The name of the parameter.</para></param>
        /// <param name="dbType"><para>One of the <see cref="DbType"/> values.</para></param>                
        /// <remarks>
        /// <para>This version of the method is used when you can have the same parameter object multiple times with different values.</para>
        /// </remarks>        
        public void AddInParameter(DbCommand command, string name, DbType dbType)
        {
            AddParameter(command, name, dbType, ParameterDirection.Input, String.Empty, DataRowVersion.Default, null);
        }

        /// <summary>
        /// Adds a new In <see cref="DbParameter"/> object to the given <paramref name="command"/>.
        /// </summary>
        /// <param name="command">The commmand to add the parameter.</param>
        /// <param name="name"><para>The name of the parameter.</para></param>
        /// <param name="dbType"><para>One of the <see cref="DbType"/> values.</para></param>                
        /// <param name="value"><para>The value of the parameter.</para></param>      
        public void AddInParameter(DbCommand command, string name, DbType dbType, object value)
        {
            AddParameter(command, name, dbType, ParameterDirection.Input, String.Empty, DataRowVersion.Default, value);
        }

        /// <summary>
        /// Adds a new In <see cref="DbParameter"/> object to the given <paramref name="command"/>.
        /// </summary>
        /// <param name="command">The commmand to add the parameter.</param>
        /// <param name="name"><para>The name of the parameter.</para></param>
        /// <param name="value"><para>The value of the parameter.</para></param>      
        public void AddInParameter(DbCommand command, string name, object value)
        {
            AddParameter(command, name, DbType.Object, ParameterDirection.Input, String.Empty, DataRowVersion.Default, value);
        }

        /// <summary>
        /// Adds a new In <see cref="DbParameter"/> object to the given <paramref name="command"/>.
        /// </summary>
        /// <param name="command">The command to add the parameter.</param>
        /// <param name="name"><para>The name of the parameter.</para></param>
        /// <param name="dbType"><para>One of the <see cref="DbType"/> values.</para></param>                
        /// <param name="sourceColumn"><para>The name of the source column mapped to the DataSet and used for loading or returning the value.</para></param>
        /// <param name="sourceVersion"><para>One of the <see cref="DataRowVersion"/> values.</para></param>
        public void AddInParameter(DbCommand command, string name, DbType dbType, string sourceColumn, DataRowVersion sourceVersion)
        {
            AddParameter(command, name, dbType, 0, ParameterDirection.Input, true, 0, 0, sourceColumn, sourceVersion, null);
        }
        
        #endregion

        #region Helper Methods

        /// <summary>
        /// Discovers params from SQL text.
        /// </summary>
        /// <param name="sql">The full or part of SQL text.</param>
        /// <returns>The discovered params.</returns>
        public string[] DiscoverParams(string sql)
        {
            return dbProvider.DiscoverParams(sql);
        }

        /// <summary>
        /// Writes the log.
        /// </summary>
        /// <param name="command">The command.</param>
        public void WriteLog(DbCommand command)
        {
            if (OnLog != null)
            {
                StringBuilder sb = new StringBuilder();

                sb.Append(string.Format("{0}\t{1}\t\r\n", command.CommandType, command.CommandText));
                if (command.Parameters != null && command.Parameters.Count > 0)
                {
                    sb.Append("Parameters:\r\n");
                    foreach (DbParameter p in command.Parameters)
                    {
                        sb.Append(string.Format("{0}[{2}] = {1}\r\n", p.ParameterName, p.Value, p.DbType));
                    }
                }
                sb.Append("\r\n");

                OnLog(sb.ToString());
            }
        }

        /// <summary>
        /// Writes the log.
        /// </summary>
        /// <param name="logMsg">The log MSG.</param>
        public void WriteLog(string logMsg)
        {
            if (OnLog != null)
            {
                OnLog(logMsg);
            }
        }

        #endregion

        #endregion

        #region Batch Database

        private DbConnection batchConnection = null;
        //private int batchSize = 1;
        private BatchCommander batchCommander = null;

        /// <summary>
        /// Gets a value indicating whether this instance is batch connection.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is batch connection; otherwise, <c>false</c>.
        /// </value>
        public bool IsBatchConnection
        {
            get
            {
                return (batchConnection != null);
            }
        }

        /// <summary>
        /// Begins the batch connection.
        /// </summary>
        /// <param name="batchSize">Size of the batch.</param>
        public void BeginBatchConnection(int batchSize)
        {
            batchConnection = CreateConnection(true);
            //this.batchSize = batchSize;
            batchCommander = new BatchCommander(this, batchSize);
        }

        /// <summary>
        /// Begins the batch connection.
        /// </summary>
        /// <param name="batchSize">Size of the batch.</param>
        /// <param name="tran">The tran.</param>
        public void BeginBatchConnection(int batchSize, DbTransaction tran)
        {
            batchConnection = CreateConnection(true);
            //this.batchSize = batchSize;
            batchCommander = new BatchCommander(this, batchSize, tran);
        }

        /// <summary>
        /// Begins the batch connection.
        /// </summary>
        /// <param name="batchSize">Size of the batch.</param>
        /// <param name="il">The il.</param>
        public void BeginBatchConnection(int batchSize, IsolationLevel il)
        {
            batchConnection = CreateConnection(true);
            //this.batchSize = batchSize;
            batchCommander = new BatchCommander(this, batchSize, il);
        }

        /// <summary>
        /// Ends the batch connection.
        /// </summary>
        public void EndBatchConnection()
        {
            batchCommander.Close();
            CloseConnection(batchConnection);
            batchConnection = null;
            batchCommander = null;
        }

        /// <summary>
        /// Executes the pending batch operations.
        /// </summary>
        public void ExecutePendingBatchOperations()
        {
            batchCommander.ExecuteBatch();
        }

        #endregion

        #region ILogable Members

        /// <summary>
        /// OnLog event.
        /// </summary>
        public event LogHandler OnLog;

        #endregion
    }
}
