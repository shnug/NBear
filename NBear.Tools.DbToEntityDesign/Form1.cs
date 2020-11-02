using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using ADODB;
using System.IO;

using NBear.Data;

namespace NBear.Tools.DbToEntityDesign
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (txtConnStr.Text.Trim().Length == 0)
            {
                MessageBox.Show("Connection string cannot be null!");
                return;
            }

            if (btnConnect.Text == "Disconnect")
            {
                EnableGenEntity(false);
                return;
            }

            RefreshConnectionStringAutoComplete();

            DataSet dsTables = null;
            DataSet dsViews = null;

            if (radioSql.Checked || radioAccess.Checked)
            {
                try
                {
                    if (radioSql.Checked)
                    {
                        Gateway.SetDefaultDatabase(DatabaseType.SqlServer, txtConnStr.Text);
                        if (checkSql2005.Checked)
                        {
                            dsTables = Gateway.Default.FromCustomSql("select [name] from sysobjects where xtype = 'U' and [name] <> 'sysdiagrams' order by [name]").ToDataSet();
                        }
                        else
                        {
                            dsTables = Gateway.Default.FromCustomSql("select [name] from sysobjects where xtype = 'U' and status > 0 order by [name]").ToDataSet();
                        }
                        foreach (DataRow row in dsTables.Tables[0].Rows)
                        {
                            tables.Items.Add(row["Name"].ToString());
                        }

                        if (checkSql2005.Checked)
                        {
                            dsViews = Gateway.Default.FromCustomSql("select [name] from sysobjects where xtype = 'V' order by [name]").ToDataSet();
                        }
                        else
                        {
                            dsViews = Gateway.Default.FromCustomSql("select [name] from sysobjects where xtype = 'V' and status > 0 order by [name]").ToDataSet();
                        }
                        foreach (DataRow row in dsViews.Tables[0].Rows)
                        {
                            views.Items.Add(row["Name"].ToString());
                        }
                    }
                    else if (radioAccess.Checked)
                    {
                        Gateway.SetDefaultDatabase(DatabaseType.MsAccess, txtConnStr.Text);
                        ADODB.ConnectionClass conn = new ADODB.ConnectionClass();
                        conn.Provider = "Microsoft.Jet.OLEDB.4.0";
                        string connStr = txtConnStr.Text;
                        conn.Open(connStr.Substring(connStr.ToLower().IndexOf("data source") + "data source".Length).Trim('=', ' '), null, null, 0);

                        ADODB.Recordset rsTables = conn.GetType().InvokeMember("OpenSchema", BindingFlags.InvokeMethod, null, conn, new object[] { ADODB.SchemaEnum.adSchemaTables }) as ADODB.Recordset;
                        ADODB.Recordset rsViews = conn.GetType().InvokeMember("OpenSchema", BindingFlags.InvokeMethod, null, conn, new object[] { ADODB.SchemaEnum.adSchemaViews }) as ADODB.Recordset;

                        while (!rsViews.EOF)
                        {
                            if (!(rsViews.Fields["TABLE_NAME"].Value as string).StartsWith("MSys"))
                            {
                                views.Items.Add(rsViews.Fields["TABLE_NAME"].Value.ToString());
                            }
                            rsViews.MoveNext();
                        }

                        while (!rsTables.EOF)
                        {
                            if (!(rsTables.Fields["TABLE_NAME"].Value as string).StartsWith("MSys"))
                            {
                                bool isView = false;
                                foreach (string item in views.Items)
                                {
                                    if (item.Equals(rsTables.Fields["TABLE_NAME"].Value.ToString()))
                                    {
                                        isView = true;
                                        break;
                                    }
                                }
                                if (!isView)
                                {
                                    tables.Items.Add(rsTables.Fields["TABLE_NAME"].Value.ToString());
                                }
                            }
                            rsTables.MoveNext();
                        }

                        rsTables.Close();
                        rsViews.Close();

                        conn.Close();
                    }

                    EnableGenEntity(true);
                }
                catch (Exception ex)
                {
                    EnableGenEntity(false);
                    MessageBox.Show("Read/write database error!\r\n" + ex.ToString());
                }
            }
            else if (radioOracle.Checked)
            {
                Gateway.SetDefaultDatabase(DatabaseType.Oracle, txtConnStr.Text);

                dsTables = Gateway.Default.FromCustomSql("select * from user_tables where global_stats = 'NO' and (not table_name like '%$%')").ToDataSet();
                foreach (DataRow row in dsTables.Tables[0].Rows)
                {
                    tables.Items.Add(row["TABLE_NAME"].ToString());
                }

                dsViews = Gateway.Default.FromCustomSql("select * from user_views where (not view_name like '%$%') and (not view_name like 'MVIEW_%') and (not view_name like 'CTX_%') and (not view_name = 'PRODUCT_PRIVS')").ToDataSet();
                foreach (DataRow row in dsViews.Tables[0].Rows)
                {
                    views.Items.Add(row["VIEW_NAME"].ToString());
                }

                EnableGenEntity(true);
            }
            else if (radioMySql.Checked)
            {
                System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex("(^.*database=)([^;]+)(;.*)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                string dbName = r.Replace(txtConnStr.Text, "$2").ToLower();
                Gateway.SetDefaultDatabase(DatabaseType.MySql, r.Replace(txtConnStr.Text, "$1information_schema$3"));

                dsTables = Gateway.Default.FromCustomSql("select * from TABLES where TABLE_TYPE = 'BASE TABLE' and TABLE_SCHEMA = '" + dbName + "'").ToDataSet();
                foreach (DataRow row in dsTables.Tables[0].Rows)
                {
                    tables.Items.Add(row["TABLE_NAME"].ToString());
                }

                dsViews = Gateway.Default.FromCustomSql("select * from TABLES where TABLE_TYPE = 'VIEW' and TABLE_SCHEMA = '" + dbName + "'").ToDataSet();
                foreach (DataRow row in dsViews.Tables[0].Rows)
                {
                    views.Items.Add(row["TABLE_NAME"].ToString());
                }

                EnableGenEntity(true);
            }
            else
            {
                EnableGenEntity(false);
                MessageBox.Show("EntityGen tool only supports SqlServer, MsAccess, MySql and Oracle Database!");
            }
        }

        private AutoCompleteStringCollection connStrs = new AutoCompleteStringCollection();
        private const string CONN_STR_HIS_File = "ConnectionStringsHistory.txt";

        public void RefreshConnectionStringAutoComplete()
        {
            if (!string.IsNullOrEmpty(txtConnStr.Text))
            {
                if (!connStrs.Contains(txtConnStr.Text))
                {
                    connStrs.Add(txtConnStr.Text);
                }
            }
        }

        private void SaveConnectionStringAutoComplete()
        {
            StreamWriter sw = new StreamWriter(CONN_STR_HIS_File);

            foreach (string line in connStrs)
            {
                sw.WriteLine(line);
            }

            sw.Close();
        }

        private void LoadConnectionStringAutoComplete()
        {
            if (File.Exists(CONN_STR_HIS_File))
            {
                connStrs.Clear();

                StreamReader sr = new StreamReader(CONN_STR_HIS_File);
                while (!sr.EndOfStream)
                {
                    connStrs.Add(sr.ReadLine().Trim());
                }
                sr.Close();
            }

            txtConnStr.AutoCompleteCustomSource = connStrs;
        }

        private void EnableGenEntity(bool enable)
        {
            if (enable)
            {
                btnGen.Enabled = true;
                txtConnStr.Enabled = false;
                btnConnect.Text = "Disconnect";

                radioAccess.Enabled = false;
                radioMySql.Enabled = false;
                radioOracle.Enabled = false;
                radioSql.Enabled = false;
            }
            else
            {
                btnGen.Enabled = false;
                txtConnStr.Enabled = true;
                btnConnect.Text = "Connect";
                selectAll.Checked = false;
                tables.Items.Clear();
                views.Items.Clear();
                output.Text = "";

                radioAccess.Enabled = true;
                radioMySql.Enabled = true;
                radioOracle.Enabled = true;
                radioSql.Enabled = true;
            }
        }

        private void selectAll_CheckedChanged(object sender, EventArgs e)
        {
            if (selectAll.Checked)
            {
                for (int i = 0; i < tables.Items.Count; i++ )
                {
                    tables.SetItemChecked(i, true);
                }
                for (int i = 0; i < views.Items.Count; i++)
                {
                    views.SetItemChecked(i, true);
                }
            }
            else
            {
                for (int i = 0; i < tables.Items.Count; i++)
                {
                    tables.SetItemChecked(i, false);
                }
                for (int i = 0; i < views.Items.Count; i++)
                {
                    views.SetItemChecked(i, false);
                }
            }
        }

        private string GenType(string typeStr)
        {
            if (typeStr == typeof(string).ToString())
            {
                return "string";
            }
            else if (typeStr == typeof(int).ToString())
            {
                return "int";
            }
            else if (typeStr == typeof(long).ToString())
            {
                return "long";
            }
            else if (typeStr == typeof(short).ToString())
            {
                return "short";
            }
            else if (typeStr == typeof(byte).ToString())
            {
                return "byte";
            }
            else if (typeStr == typeof(byte[]).ToString())
            {
                return "byte[]";
            }
            else if (typeStr == typeof(bool).ToString())
            {
                return "bool";
            }
            else if (typeStr == typeof(decimal).ToString())
            {
                return "decimal";
            }
            else if (typeStr == typeof(char).ToString())
            {
                return "char";
            }
            else if (typeStr == typeof(sbyte).ToString())
            {
                return "sbyte";
            }
            else if (typeStr == typeof(float).ToString())
            {
                return "float";
            }
            else if (typeStr == typeof(double).ToString())
            {
                return "double";
            }
            else if (typeStr == typeof(object).ToString())
            {
                return "object";
            }
            else if (typeStr == typeof(Guid).ToString())
            {
                return "Guid";
            }
            else if (typeStr == typeof(DateTime).ToString())
            {
                return "DateTime";
            }
            else
            {
                return typeStr;
            }
        }

        private string GenTypeVB(string typeStr)
        {
            if (typeStr == typeof(string).ToString())
            {
                return "String";
            }
            else if (typeStr == typeof(int).ToString())
            {
                return "Integer";
            }
            else if (typeStr == typeof(uint).ToString())
            {
                return "UInteger";
            }
            else if (typeStr == typeof(long).ToString())
            {
                return "Long";
            }
            else if (typeStr == typeof(ulong).ToString())
            {
                return "ULong";
            }
            else if (typeStr == typeof(short).ToString())
            {
                return "Short";
            }
            else if (typeStr == typeof(ushort).ToString())
            {
                return "UShort";
            }
            else if (typeStr == typeof(byte).ToString())
            {
                return "Byte";
            }
            else if (typeStr == typeof(byte[]).ToString())
            {
                return "Byte()";
            }
            else if (typeStr == typeof(bool).ToString())
            {
                return "Boolean";
            }
            else if (typeStr == typeof(decimal).ToString())
            {
                return "Decimal";
            }
            else if (typeStr == typeof(char).ToString())
            {
                return "Char";
            }
            else if (typeStr == typeof(sbyte).ToString())
            {
                return "SByte";
            }
            else if (typeStr == typeof(Single).ToString())
            {
                return "Single";
            }
            else if (typeStr == typeof(double).ToString())
            {
                return "Double";
            }
            else if (typeStr == typeof(object).ToString())
            {
                return "Object";
            }
            else if (typeStr == typeof(Guid).ToString())
            {
                return "Guid";
            }
            else if (typeStr == typeof(DateTime).ToString())
            {
                return "Date";
            }
            else
            {
                return typeStr.Replace("[", "(").Replace("]", ")");
            }
        }

        private string GenEntity(string name, bool isView)
        {
            DataSet ds = null;

            if (radioAccess.Checked || radioSql.Checked)
            {
                ds = Gateway.Default.FromCustomSql(string.Format("select * from [{0}] where 1 = 2", name)).ToDataSet();
            }
            else if (radioMySql.Checked)
            {
                Gateway dbGateway = new Gateway(DatabaseType.MySql, txtConnStr.Text);
                ds = dbGateway.FromCustomSql(string.Format("select * from `{0}` where 1 = 2", name)).ToDataSet();
            }
            else
            {
                ds = Gateway.Default.FromCustomSql(string.Format("select * from \"{0}\" where 1 = 2", name)).ToDataSet();
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.Append("\r\n");

            if (outputLanguage.SelectedIndex == 0)
            {
                if (isView)
                {
                    sb.Append(string.Format("    [ReadOnly]\r\n"));
                }
                sb.Append(string.Format("    public interface {0} : Entity\r\n", UpperFirstChar(ParseTableName(name))));
                sb.Append("    {\r\n");

                foreach (DataColumn column in ds.Tables[0].Columns)
                {
                    if (IsColumnPrimaryKey(name, column.ColumnName))
                    {
                        sb.Append(string.Format("        [PrimaryKey]\r\n"));
                    }
                    if (radioSql.Checked)
                    {
                        GenSqlColumnType(sb, name, column.ColumnName);
                    }
                    sb.Append(string.Format("        {0}{2} {1} ", GenType(column.DataType.ToString()), UpperFirstChar(column.ColumnName), (column.DataType.IsValueType && IsColumnNullable(name, column.ColumnName) ? "?" : "")));
                    if (isView || IsColumnReadOnly(name, column.ColumnName))
                    {
                        sb.Append("{ get; }\r\n");
                    }
                    else
                    {
                        sb.Append("{ get; set; }\r\n");
                    }
                }

                sb.Append("    }\r\n");
            }
            else if (outputLanguage.SelectedIndex == 1)
            {
                if (isView)
                {
                    sb.Append(string.Format("    <NBear.Common.Design.ReadOnly()> _\r\n"));
                }
                sb.Append(string.Format("    Public Interface {0}\r\n    Inherits Entity\r\n", UpperFirstChar(ParseTableName(name))));

                foreach (DataColumn column in ds.Tables[0].Columns)
                {
                    if (IsColumnPrimaryKey(name, column.ColumnName))
                    {
                        sb.Append(string.Format("        <PrimaryKey()> _\r\n"));
                    }
                    if (radioSql.Checked)
                    {
                        GenSqlColumnType(sb, name, column.ColumnName);
                    }
                    sb.Append("        ");
                    if (isView || IsColumnReadOnly(name, column.ColumnName))
                    {
                        sb.Append("ReadOnly ");
                    }
                    if (column.DataType.IsValueType && IsColumnNullable(name, column.ColumnName))
                    {
                        sb.Append(string.Format("Property {1}() As Nullable(Of {0})\r\n", GenTypeVB(column.DataType.ToString()), UpperFirstChar(column.ColumnName)));
                    }
                    else
                    {
                        sb.Append(string.Format("Property {1}() As {0}\r\n", GenTypeVB(column.DataType.ToString()), UpperFirstChar(column.ColumnName)));
                    }
                }

                sb.Append("    End Interface\r\n");
            }

            return sb.ToString();
        }

        private void GenSqlColumnType(StringBuilder sb, string name, string column)
        {
            int tableid = Convert.ToInt32(Gateway.Default.FromCustomSql("select id from sysobjects where [name] = '" + name + "'").ToScalar());
            DataSet ds = Gateway.Default.FromCustomSql("select xtype, length, cdefault  from syscolumns where id = " + tableid + " and name = '" + column + "'").ToDataSet();
            object temp = Gateway.Default.FromCustomSql("select text from syscomments where id = " + ds.Tables[0].Rows[0]["cdefault"]).ToScalar();
            string df =null;
            if (temp != null && temp != DBNull.Value)
            {
                df = temp.ToString();
                df = outputLanguage.SelectedIndex == 0 ? df.Replace("\"", "\\\"") : df.Replace("\"", "\"\"");
                if (df.StartsWith("(") && df.EndsWith(")") && df.Length > 2)
                {
                    df = df.Substring(1, df.Length - 2);
                }
            }
            switch (int.Parse(ds.Tables[0].Rows[0]["xtype"].ToString()))
            {
                case 231:
                    if (outputLanguage.SelectedIndex == 0)
                    {
                        sb.Append(string.Format("        [SqlType(\"{0}\"{1})]\r\n", string.Format("nvarchar({0})", int.Parse(ds.Tables[0].Rows[0]["length"].ToString()) / 2), df == null ? null : string.Format(", DefaultValue=\"{0}\"", df)));
                    }
                    else if (outputLanguage.SelectedIndex == 1)
                    {
                        sb.Append(string.Format("        <SqlType(\"{0}\"{1})> _\r\n", string.Format("nvarchar({0})", int.Parse(ds.Tables[0].Rows[0]["length"].ToString()) / 2), df == null ? null : string.Format(", DefaultValue=\"{0}\"", df)));
                    }
                    break;
                case 239:
                    if (outputLanguage.SelectedIndex == 0)
                    {
                        sb.Append(string.Format("        [SqlType(\"{0}\"{1})]\r\n", string.Format("nchar({0})", int.Parse(ds.Tables[0].Rows[0]["length"].ToString()) / 2), df == null ? null : string.Format(", DefaultValue=\"{0}\"", df)));
                    }
                    else if (outputLanguage.SelectedIndex == 1)
                    {
                        sb.Append(string.Format("        <SqlType(\"{0}\"{1})> _\r\n", string.Format("nchar({0})", int.Parse(ds.Tables[0].Rows[0]["length"].ToString()) / 2), df == null ? null : string.Format(", DefaultValue=\"{0}\"", df)));
                    }
                    break;
                case 99:
                    if (outputLanguage.SelectedIndex == 0)
                    {
                        sb.Append(string.Format("        [SqlType(\"ntext\"{0})]\r\n", df == null ? null : string.Format(", DefaultValue=\"{0}\"", df)));
                    }
                    else if (outputLanguage.SelectedIndex == 1)
                    {
                        sb.Append(string.Format("        <SqlType(\"ntext\"{0})> _\r\n", df == null ? null : string.Format(", DefaultValue=\"{0}\"", df)));
                    }
                    break;
                case 167:
                    if (outputLanguage.SelectedIndex == 0)
                    {
                        sb.Append(string.Format("        [SqlType(\"{0}\"{1})]\r\n", string.Format("varchar({0})", int.Parse(ds.Tables[0].Rows[0]["length"].ToString())), df == null ? null : string.Format(", DefaultValue=\"{0}\"", df)));
                    }
                    else if (outputLanguage.SelectedIndex == 1)
                    {
                        sb.Append(string.Format("        <SqlType(\"{0}\"{1})> _\r\n", string.Format("varchar({0})", int.Parse(ds.Tables[0].Rows[0]["length"].ToString())), df == null ? null : string.Format(", DefaultValue=\"{0}\"", df)));
                    }
                    break;
                case 175:
                    if (outputLanguage.SelectedIndex == 0)
                    {
                        sb.Append(string.Format("        [SqlType(\"{0}\"{1})]\r\n", string.Format("char({0})", int.Parse(ds.Tables[0].Rows[0]["length"].ToString())), df == null ? null : string.Format(", DefaultValue=\"{0}\"", df)));
                    }
                    else if (outputLanguage.SelectedIndex == 1)
                    {
                        sb.Append(string.Format("        <SqlType(\"{0}\"{1})> _\r\n", string.Format("char({0})", int.Parse(ds.Tables[0].Rows[0]["length"].ToString())), df == null ? null : string.Format(", DefaultValue=\"{0}\"", df)));
                    }
                    break;
                case 35:
                    if (outputLanguage.SelectedIndex == 0)
                    {
                        sb.Append(string.Format("        [SqlType(\"text\"{0})]\r\n", df == null ? null : string.Format(", DefaultValue=\"{0}\"", df)));
                    }
                    else if (outputLanguage.SelectedIndex == 1)
                    {
                        sb.Append(string.Format("        <SqlType(\"text\"{0})> _\r\n", df == null ? null : string.Format(", DefaultValue=\"{0}\"", df)));
                    }
                    break;
            }

            if (!string.IsNullOrEmpty(df))
            {
                string appendTemp = (outputLanguage.SelectedIndex == 0 
                    ? "        [SqlType(\"{0}\", DefaultValue=\"" + df + "\")]\r\n" 
                    : "        <SqlType(\"{0}\", DefaultValue=\"" + df+ "\")> _\r\n");
                switch (int.Parse(ds.Tables[0].Rows[0]["xtype"].ToString()))
                {
                    case 34:
                        sb.Append(string.Format(appendTemp, "image"));
                        break;
                    case 36:
                        sb.Append(string.Format(appendTemp, "uniqueidentifier"));
                        break;
                    case 48:
                        sb.Append(string.Format(appendTemp, "tinyint"));
                        break;
                    case 52:
                        sb.Append(string.Format(appendTemp, "smallint"));
                        break;
                    case 56:
                        sb.Append(string.Format(appendTemp, "int"));
                        break;
                    case 58:
                        sb.Append(string.Format(appendTemp, "smalldatetime"));
                        break;
                    case 59:
                        sb.Append(string.Format(appendTemp, "real"));
                        break;
                    case 60:
                        sb.Append(string.Format(appendTemp, "money"));
                        break;
                    case 61:
                        sb.Append(string.Format(appendTemp, "datetime"));
                        break;
                    case 62:
                        sb.Append(string.Format(appendTemp, "float"));
                        break;
                    case 104:
                        sb.Append(string.Format(appendTemp, "bit"));
                        break;
                    case 106:
                        sb.Append(string.Format(appendTemp, "decimal"));
                        break;
                    case 108:
                        sb.Append(string.Format(appendTemp, "numeric"));
                        break;
                    case 122:
                        sb.Append(string.Format(appendTemp, "smallmoney"));
                        break;
                    case 127:
                        sb.Append(string.Format(appendTemp, "bigint"));
                        break;
                    case 165:
                        sb.Append(string.Format(appendTemp, "varbinary"));
                        break;
                    case 173:
                        sb.Append(string.Format(appendTemp, "binary"));
                        break;
                    case 189:
                        sb.Append(string.Format(appendTemp, "timestamp"));
                        break;
                }
            }
        }

        private bool IsColumnPrimaryKey(string name, string column)
        {
            if (radioSql.Checked)
            {
                int tableid = Convert.ToInt32(Gateway.Default.FromCustomSql("select id from sysobjects where [name] = '" + name + "'").ToScalar());
                DataSet ds = Gateway.Default.FromCustomSql("select a.name FROM syscolumns a inner join sysobjects d on a.id=d.id and d.xtype='U' and d.name<>'dtproperties' where (SELECT count(*) FROM sysobjects WHERE (name in (SELECT name FROM sysindexes WHERE (id = a.id) AND (indid in (SELECT indid FROM sysindexkeys WHERE (id = a.id) AND (colid in (SELECT colid FROM syscolumns WHERE (id = a.id) AND (name = a.name))))))) AND (xtype = 'PK'))>0 and d.id = " + tableid.ToString()).ToDataSet();
                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    if (ds.Tables[0].Rows[i][0].ToString() == column)
                    {
                        return true;
                    }
                }
            }
            else if (radioOracle.Checked)
            {
                DataSet ds = Gateway.Default.FromCustomSql("select b.COLUMN_NAME from USER_CONSTRAINTS a,USER_CONS_COLUMNS b where a.CONSTRAINT_NAME=b.CONSTRAINT_NAME and a.table_name=b.table_name and constraint_type='P' and a.owner=b.owner and a.table_name = '" + name + "'").ToDataSet();
                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    if (ds.Tables[0].Rows[i][0].ToString() == column)
                    {
                        return true;
                    }
                }
            }
            else if (radioMySql.Checked)
            {
                System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex("(^.*database=)([^;]+)(;.*)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                string dbName = r.Replace(txtConnStr.Text, "$2").ToLower();

                DataSet ds = Gateway.Default.FromCustomSql("select COLUMN_NAME from KEY_COLUMN_USAGE where CONSTRAINT_SCHEMA = '" + dbName + "' and CONSTRAINT_NAME = 'PRIMARY' and TABLE_NAME = '" + name + "'").ToDataSet();
                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    if (ds.Tables[0].Rows[i][0].ToString() == column)
                    {
                        return true;
                    }
                }
            }
            else
            {
                ADODB.ConnectionClass conn = new ADODB.ConnectionClass();
                conn.Provider = "Microsoft.Jet.OLEDB.4.0";
                string connStr = txtConnStr.Text;
                conn.Open(connStr.Substring(connStr.ToLower().IndexOf("data source") + "data source".Length).Trim('=', ' '), null, null, 0);

                ADODB.Recordset rs = conn.GetType().InvokeMember("OpenSchema", BindingFlags.InvokeMethod, null, conn, new object[] { ADODB.SchemaEnum.adSchemaPrimaryKeys }) as ADODB.Recordset;
                rs.Filter = "TABLE_NAME='" + name + "'";

                while (!rs.EOF)
                {
                    if ((rs.Fields["COLUMN_NAME"].Value as string) == column)
                    {
                        return true;
                    }

                    rs.MoveNext();
                }
            }

            return false;
        }

        private bool IsColumnReadOnly(string name, string column)
        {
            if (radioSql.Checked)
            {
                int tableid = Convert.ToInt32(Gateway.Default.FromCustomSql("select id from sysobjects where [name] = '" + name + "'").ToScalar());
                byte status = Convert.ToByte(Gateway.Default.FromCustomSql("select status from syscolumns where [name] = '" + column + "' and id = " + tableid.ToString()).ToScalar());
                return status == 128;
            }
            else if (radioOracle.Checked)
            {
                return false;
            }
            else if (radioMySql.Checked)
            {
                System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex("(^.*database=)([^;]+)(;.*)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                string dbName = r.Replace(txtConnStr.Text, "$2").ToLower();

                DataSet ds = Gateway.Default.FromCustomSql("select EXTRA from COLUMNS where TABLE_SCHEMA = '" + dbName + "' and COLUMN_NAME = '" + column + "' and TABLE_NAME = '" + name + "'").ToDataSet();
                for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                {
                    if (ds.Tables[0].Rows[i][0].ToString() == "auto_increment")
                    {
                        return true;
                    }
                }

                return false;
            }
            else
            {
                ADODB.ConnectionClass conn = new ADODB.ConnectionClass();
                conn.Provider = "Microsoft.Jet.OLEDB.4.0";
                string connStr = txtConnStr.Text;
                conn.Open(connStr.Substring(connStr.ToLower().IndexOf("data source") + "data source".Length).Trim('=', ' '), null, null, 0);

                ADODB.Recordset rs = conn.GetType().InvokeMember("OpenSchema", BindingFlags.InvokeMethod, null, conn, new object[] { ADODB.SchemaEnum.adSchemaColumns }) as ADODB.Recordset;
                rs.Filter = "TABLE_NAME='" + name + "'";

                while (!rs.EOF)
                {
                    if ((rs.Fields["COLUMN_NAME"].Value as string) == column && ((int)rs.Fields["DATA_TYPE"].Value) == 3 && Convert.ToByte(rs.Fields["COLUMN_FLAGS"].Value) == 90)
                    {
                        return true;
                    }

                    rs.MoveNext();
                }
            }

            return false;
        }

        private bool IsColumnNullable(string name, string column)
        {
            if (radioSql.Checked)
            {
                int tableid = Convert.ToInt32(Gateway.Default.FromCustomSql("select id from sysobjects where [name] = '" + name + "'").ToScalar());
                int isnullable = Convert.ToInt32(Gateway.Default.FromCustomSql("select isnullable from syscolumns where [name] = '" + column + "' and id = " + tableid.ToString()).ToScalar());
                return isnullable == 1;
            }
            else
            {
                return false;
            }
        }

        private static string ParseTableName(string name)
        {
            return name.Trim().Replace(" ", "_nbsp_");
        }

        private void btnGen_Click(object sender, EventArgs e)
        {
            output.Text = "";

            foreach (string table in tables.CheckedItems)
            {
                output.Text += GenEntity(table, false);
            }

            foreach (string view in views.CheckedItems)
            {
                output.Text += GenEntity(view, true);
            }
        }

        private string UpperFirstChar(string str)
        {
            if (!checkUpperFirstChar.Checked)
            {
                return str;
            }

            if (string.IsNullOrEmpty(str))
            {
                return str;
            }
            else if (str.Length > 1)
            {
                return str.Substring(0, 1).ToUpper() + str.Substring(1);
            }
            else
            {
                return str.ToUpper();
            }
        }

        private void copyAllToClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(output.Text);
        }

        private void txtConnStr_TextChanged(object sender, EventArgs e)
        {
            if (txtConnStr.Text.ToLower().Contains("microsoft.jet.oledb"))
            {
                radioAccess.Checked = true;
                checkSql2005.Enabled = false;
            }
            else if (txtConnStr.Text.ToLower().Contains("data source") && txtConnStr.Text.ToLower().Contains("user id") && txtConnStr.Text.ToLower().Contains("password"))
            {
                radioOracle.Checked = true;
                checkSql2005.Enabled = false;
            }
            else if (txtConnStr.Text.ToLower().Contains("dsn="))
            {
                radioMySql.Checked = true;
                checkSql2005.Enabled = false;
            }
            else
            {
                radioSql.Checked = true;
                checkSql2005.Enabled = true;
            }
        }

        private void radioSql_CheckedChanged(object sender, EventArgs e)
        {
            checkSql2005.Enabled = radioSql.Checked;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            outputLanguage.SelectedIndex = 0;

            LoadConnectionStringAutoComplete();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            SaveConnectionStringAutoComplete();
        }
    }
}