namespace NBear.Tools.DbToEntityDesign
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.tables = new System.Windows.Forms.CheckedListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.selectAll = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.views = new System.Windows.Forms.CheckedListBox();
            this.btnGen = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.output = new System.Windows.Forms.RichTextBox();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.copyAllToClipboardToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.label4 = new System.Windows.Forms.Label();
            this.txtConnStr = new System.Windows.Forms.TextBox();
            this.btnConnect = new System.Windows.Forms.Button();
            this.radioSql = new System.Windows.Forms.RadioButton();
            this.radioAccess = new System.Windows.Forms.RadioButton();
            this.checkSql2005 = new System.Windows.Forms.CheckBox();
            this.checkUpperFirstChar = new System.Windows.Forms.CheckBox();
            this.radioOracle = new System.Windows.Forms.RadioButton();
            this.radioMySql = new System.Windows.Forms.RadioButton();
            this.outputLanguage = new System.Windows.Forms.ComboBox();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tables
            // 
            this.tables.CheckOnClick = true;
            this.tables.FormattingEnabled = true;
            this.tables.Location = new System.Drawing.Point(12, 60);
            this.tables.Name = "tables";
            this.tables.Size = new System.Drawing.Size(220, 289);
            this.tables.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 44);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(39, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Tables";
            // 
            // selectAll
            // 
            this.selectAll.AutoSize = true;
            this.selectAll.Location = new System.Drawing.Point(12, 12);
            this.selectAll.Name = "selectAll";
            this.selectAll.Size = new System.Drawing.Size(117, 17);
            this.selectAll.TabIndex = 2;
            this.selectAll.Text = "Select/Unselect All";
            this.selectAll.UseVisualStyleBackColor = true;
            this.selectAll.CheckedChanged += new System.EventHandler(this.selectAll_CheckedChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 357);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(35, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Views";
            // 
            // views
            // 
            this.views.CheckOnClick = true;
            this.views.FormattingEnabled = true;
            this.views.Location = new System.Drawing.Point(12, 373);
            this.views.Name = "views";
            this.views.Size = new System.Drawing.Size(220, 289);
            this.views.TabIndex = 3;
            // 
            // btnGen
            // 
            this.btnGen.Enabled = false;
            this.btnGen.Location = new System.Drawing.Point(570, 107);
            this.btnGen.Name = "btnGen";
            this.btnGen.Size = new System.Drawing.Size(312, 23);
            this.btnGen.TabIndex = 5;
            this.btnGen.Text = "Generate Entities Design";
            this.btnGen.UseVisualStyleBackColor = true;
            this.btnGen.Click += new System.EventHandler(this.btnGen_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(250, 144);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(39, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Output";
            // 
            // output
            // 
            this.output.ContextMenuStrip = this.contextMenuStrip1;
            this.output.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.output.Location = new System.Drawing.Point(253, 163);
            this.output.Name = "output";
            this.output.Size = new System.Drawing.Size(629, 499);
            this.output.TabIndex = 7;
            this.output.Text = "";
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyAllToClipboardToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(161, 26);
            // 
            // copyAllToClipboardToolStripMenuItem
            // 
            this.copyAllToClipboardToolStripMenuItem.Name = "copyAllToClipboardToolStripMenuItem";
            this.copyAllToClipboardToolStripMenuItem.Size = new System.Drawing.Size(160, 22);
            this.copyAllToClipboardToolStripMenuItem.Text = "Copy to Clipboard";
            this.copyAllToClipboardToolStripMenuItem.Click += new System.EventHandler(this.copyAllToClipboardToolStripMenuItem_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(250, 12);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(91, 13);
            this.label4.TabIndex = 8;
            this.label4.Text = "Connection String";
            // 
            // txtConnStr
            // 
            this.txtConnStr.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.txtConnStr.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
            this.txtConnStr.Location = new System.Drawing.Point(253, 28);
            this.txtConnStr.Name = "txtConnStr";
            this.txtConnStr.Size = new System.Drawing.Size(629, 20);
            this.txtConnStr.TabIndex = 9;
            this.txtConnStr.TextChanged += new System.EventHandler(this.txtConnStr_TextChanged);
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(253, 107);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(311, 23);
            this.btnConnect.TabIndex = 10;
            this.btnConnect.Text = "Connect";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // radioSql
            // 
            this.radioSql.AutoSize = true;
            this.radioSql.Checked = true;
            this.radioSql.Location = new System.Drawing.Point(253, 60);
            this.radioSql.Name = "radioSql";
            this.radioSql.Size = new System.Drawing.Size(80, 17);
            this.radioSql.TabIndex = 11;
            this.radioSql.TabStop = true;
            this.radioSql.Text = "SQL Server";
            this.radioSql.UseVisualStyleBackColor = true;
            this.radioSql.CheckedChanged += new System.EventHandler(this.radioSql_CheckedChanged);
            // 
            // radioAccess
            // 
            this.radioAccess.AutoSize = true;
            this.radioAccess.Location = new System.Drawing.Point(339, 60);
            this.radioAccess.Name = "radioAccess";
            this.radioAccess.Size = new System.Drawing.Size(79, 17);
            this.radioAccess.TabIndex = 12;
            this.radioAccess.Text = "MS Access";
            this.radioAccess.UseVisualStyleBackColor = true;
            // 
            // checkSql2005
            // 
            this.checkSql2005.AutoSize = true;
            this.checkSql2005.Location = new System.Drawing.Point(425, 60);
            this.checkSql2005.Name = "checkSql2005";
            this.checkSql2005.Size = new System.Drawing.Size(247, 17);
            this.checkSql2005.TabIndex = 13;
            this.checkSql2005.Text = "Check this box when access SQL Server 2005";
            this.checkSql2005.UseVisualStyleBackColor = true;
            // 
            // checkUpperFirstChar
            // 
            this.checkUpperFirstChar.AutoSize = true;
            this.checkUpperFirstChar.Location = new System.Drawing.Point(679, 60);
            this.checkUpperFirstChar.Name = "checkUpperFirstChar";
            this.checkUpperFirstChar.Size = new System.Drawing.Size(155, 17);
            this.checkUpperFirstChar.TabIndex = 14;
            this.checkUpperFirstChar.Text = "Upper First Charactor for All";
            this.checkUpperFirstChar.UseVisualStyleBackColor = true;
            // 
            // radioOracle
            // 
            this.radioOracle.AutoSize = true;
            this.radioOracle.Location = new System.Drawing.Point(253, 83);
            this.radioOracle.Name = "radioOracle";
            this.radioOracle.Size = new System.Drawing.Size(56, 17);
            this.radioOracle.TabIndex = 15;
            this.radioOracle.TabStop = true;
            this.radioOracle.Text = "Oracle";
            this.radioOracle.UseVisualStyleBackColor = true;
            // 
            // radioMySql
            // 
            this.radioMySql.AutoSize = true;
            this.radioMySql.Location = new System.Drawing.Point(339, 84);
            this.radioMySql.Name = "radioMySql";
            this.radioMySql.Size = new System.Drawing.Size(54, 17);
            this.radioMySql.TabIndex = 16;
            this.radioMySql.TabStop = true;
            this.radioMySql.Text = "MySql";
            this.radioMySql.UseVisualStyleBackColor = true;
            // 
            // outputLanguage
            // 
            this.outputLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.outputLanguage.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.outputLanguage.FormattingEnabled = true;
            this.outputLanguage.Items.AddRange(new object[] {
            "C#",
            "VB.NET"});
            this.outputLanguage.Location = new System.Drawing.Point(293, 138);
            this.outputLanguage.Name = "outputLanguage";
            this.outputLanguage.Size = new System.Drawing.Size(100, 21);
            this.outputLanguage.TabIndex = 18;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(894, 675);
            this.Controls.Add(this.outputLanguage);
            this.Controls.Add(this.radioMySql);
            this.Controls.Add(this.radioOracle);
            this.Controls.Add(this.checkUpperFirstChar);
            this.Controls.Add(this.checkSql2005);
            this.Controls.Add(this.radioAccess);
            this.Controls.Add(this.radioSql);
            this.Controls.Add(this.txtConnStr);
            this.Controls.Add(this.btnConnect);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.output);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btnGen);
            this.Controls.Add(this.views);
            this.Controls.Add(this.selectAll);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tables);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "Form1";
            this.Text = "DbToEntityDesign";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form1_FormClosed);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckedListBox tables;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox selectAll;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckedListBox views;
        private System.Windows.Forms.Button btnGen;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.RichTextBox output;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtConnStr;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.RadioButton radioSql;
        private System.Windows.Forms.RadioButton radioAccess;
        private System.Windows.Forms.CheckBox checkSql2005;
        private System.Windows.Forms.CheckBox checkUpperFirstChar;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem copyAllToClipboardToolStripMenuItem;
        private System.Windows.Forms.RadioButton radioOracle;
        private System.Windows.Forms.RadioButton radioMySql;
        private System.Windows.Forms.ComboBox outputLanguage;
    }
}

