using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;
using NBear.Common;

namespace NBear.Data
{
    /// <summary>
    /// BatchCommander is used to execute batch queries.
    /// </summary>
    public sealed class BatchCommander
    {
        #region Private Members

        private Database db;
        private int batchSize;
        private DbTransaction tran;
        private List<DbCommand> batchCommands;
        private bool isUsingOutsideTransaction = false;

        private DbCommand MergeCommands()
        {
            DbCommand cmd = db.GetSqlStringCommand("init");
            StringBuilder sb = new StringBuilder();
            foreach (DbCommand item in batchCommands)
            {
                if (item.CommandType == CommandType.Text)
                {
                    string sql = item.CommandText;
                    for (int i = 0; i < item.Parameters.Count; i++)
                    {
                        DbParameter p = (DbParameter)((ICloneable)item.Parameters[i]).Clone();
                        string newParamName = CN.Teddy.Common.CommonUtils.MakeUniqueKey(16, "@p");
                        sql = sql.Replace(p.ParameterName, newParamName);
                        p.ParameterName = newParamName;
                        cmd.Parameters.Add(p);
                    }
                    sb.Append(sql);
                    sb.Append(';');
                }
            }

            cmd.CommandText = sb.ToString();
            return cmd;
        }

        #endregion

        #region Public Members

        public void ExecuteBatch()
        {
            DbCommand cmd = MergeCommands();

            if (cmd.CommandText.Trim().Length > 0)
            {
                if (tran != null)
                {
                    cmd.Transaction = tran;
                    cmd.Connection = tran.Connection;
                }
                else
                {
                    cmd.Connection = db.GetConnection();
                }

                db.WriteLog(cmd);

                cmd.ExecuteNonQuery();
            }

            batchCommands.Clear();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BatchCommander"/> class.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="batchSize">Size of the batch.</param>
        /// <param name="il">The il.</param>
        public BatchCommander(Database db, int batchSize, IsolationLevel il) : this(db, batchSize)
        {
            Check.Require(db != null, "db could not be null.");
            Check.Require(batchSize > 0, "Arguments error - batchSize should > 0.");

            tran = db.BeginTransaction(il);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BatchCommander"/> class.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="batchSize">Size of the batch.</param>
        /// <param name="tran">The tran.</param>
        public BatchCommander(Database db, int batchSize, DbTransaction tran) : this(db, batchSize)
        {
            Check.Require(db != null, "db could not be null.");
            Check.Require(batchSize > 0, "Arguments error - batchSize should > 0.");

            this.tran = tran;
            if (tran != null)
            {
                isUsingOutsideTransaction = true;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BatchCommander"/> class.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="batchSize">Size of the batch.</param>
        public BatchCommander(Database db, int batchSize)
        {
            Check.Require(db != null, "db could not be null.");
            Check.Require(batchSize > 0, "Arguments error - batchSize should > 0.");

            this.db = db;
            this.batchSize = batchSize;
            batchCommands = new List<DbCommand>(batchSize);
        }

        /// <summary>
        /// Processes the specified CMD.
        /// </summary>
        /// <param name="cmd">The CMD.</param>
        public void Process(DbCommand cmd)
        {
            if (cmd == null)
            {
                return;
            }

            cmd.Transaction = null;
            cmd.Connection = null;
            batchCommands.Add(cmd);

            if (batchCommands.Count >= batchSize)
            {
                try
                {
                    ExecuteBatch();
                }
                catch
                {
                    if (tran != null && (!isUsingOutsideTransaction))
                    {
                        tran.Rollback();
                    }

                    throw;
                }
            }
        }

        /// <summary>
        /// Closes this instance.
        /// </summary>
        public void Close()
        {
            try
            {
                ExecuteBatch();

                if (tran != null && (!isUsingOutsideTransaction))
                {
                    tran.Commit();
                }
            }
            catch
            {
                if (tran != null && (!isUsingOutsideTransaction))
                {
                    tran.Rollback();
                }

                throw;
            }
            finally
            {
                if (tran != null && (!isUsingOutsideTransaction))
                {
                    db.CloseConnection(tran);
                }
            }
        }

        #endregion
    }
}
