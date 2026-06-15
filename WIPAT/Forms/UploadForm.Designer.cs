namespace WIPAT
{
    partial class UploadForm
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.tabControlMain = new System.Windows.Forms.TabControl();
            this.tabForecast = new System.Windows.Forms.TabPage();
            this.pnlGrid1Wrapper = new System.Windows.Forms.Panel();
            this.dgvForecast1 = new System.Windows.Forms.DataGridView();
            this.pnlStatsBar1 = new System.Windows.Forms.FlowLayoutPanel();
            this.lblTotal1 = new System.Windows.Forms.Label();
            this.lblActive1 = new System.Windows.Forms.Label();
            this.lblInactive1 = new System.Windows.Forms.Label();
            this.lblInvalid1 = new System.Windows.Forms.Label();
            this.pnlHeader1 = new System.Windows.Forms.Panel();
            this.pnlSearch1 = new System.Windows.Forms.Panel();
            this.txtSearchF1_Real = new System.Windows.Forms.TextBox();
            this.lblIcon1 = new System.Windows.Forms.Label();
            this.lblForecast1 = new System.Windows.Forms.Label();
            this.pnlForecastErrors = new System.Windows.Forms.Panel();
            this.dgvForecastErrors = new System.Windows.Forms.DataGridView();
            this.chkSelect = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.lblForecastErrorHeader = new System.Windows.Forms.Label();
            this.btnMarkInvalid = new System.Windows.Forms.Button();
            this.btnExportForecastErrors = new System.Windows.Forms.Button();
            this.pnlForecastHeader = new System.Windows.Forms.Panel();
            this.btnBrowseForecast = new System.Windows.Forms.Button();
            this.cmbDbForecasts = new System.Windows.Forms.ComboBox();
            this.progressBarTop = new System.Windows.Forms.ProgressBar();
            this.tabOrder = new System.Windows.Forms.TabPage();
            this.pnlOrderGridWrapper = new System.Windows.Forms.Panel();
            this.dgvOrder = new System.Windows.Forms.DataGridView();
            this.pnlStatsBarOrder = new System.Windows.Forms.FlowLayoutPanel();
            this.lblTotalOrder = new System.Windows.Forms.Label();
            this.lblActiveOrder = new System.Windows.Forms.Label();
            this.lblInactiveOrder = new System.Windows.Forms.Label();
            this.lblInvalidOrder = new System.Windows.Forms.Label();
            this.lblOrderMonth = new System.Windows.Forms.Label();
            this.pnlOrderErrors = new System.Windows.Forms.Panel();
            this.dgvOrderErrors = new System.Windows.Forms.DataGridView();
            this.lblOrderErrorHeader = new System.Windows.Forms.Label();
            this.btnExportOrderErrors = new System.Windows.Forms.Button();
            this.pnlOrderHeader = new System.Windows.Forms.Panel();
            this.pnlSearchOrder = new System.Windows.Forms.Panel();
            this.txtSearchOrder_Real = new System.Windows.Forms.TextBox();
            this.lblIconOrder = new System.Windows.Forms.Label();
            this.btnBrowseOrder = new System.Windows.Forms.Button();
            this.cmbDbOrders = new System.Windows.Forms.ComboBox();
            this.tabOpenNewForm = new System.Windows.Forms.TabPage();
            this.tabControlMain.SuspendLayout();
            this.tabForecast.SuspendLayout();
            this.pnlGrid1Wrapper.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvForecast1)).BeginInit();
            this.pnlStatsBar1.SuspendLayout();
            this.pnlHeader1.SuspendLayout();
            this.pnlSearch1.SuspendLayout();
            this.pnlForecastErrors.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvForecastErrors)).BeginInit();
            this.pnlForecastHeader.SuspendLayout();
            this.tabOrder.SuspendLayout();
            this.pnlOrderGridWrapper.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvOrder)).BeginInit();
            this.pnlStatsBarOrder.SuspendLayout();
            this.pnlOrderErrors.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvOrderErrors)).BeginInit();
            this.pnlOrderHeader.SuspendLayout();
            this.pnlSearchOrder.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControlMain
            // 
            this.tabControlMain.Controls.Add(this.tabForecast);
            this.tabControlMain.Controls.Add(this.tabOrder);
            this.tabControlMain.Controls.Add(this.tabOpenNewForm);
            this.tabControlMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControlMain.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            this.tabControlMain.ItemSize = new System.Drawing.Size(180, 45);
            this.tabControlMain.Location = new System.Drawing.Point(0, 0);
            this.tabControlMain.Name = "tabControlMain";
            this.tabControlMain.SelectedIndex = 0;
            this.tabControlMain.Size = new System.Drawing.Size(1100, 750);
            this.tabControlMain.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            this.tabControlMain.TabIndex = 0;
            this.tabControlMain.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.tabControlMain_DrawItem);
            this.tabControlMain.Selecting += new System.Windows.Forms.TabControlCancelEventHandler(this.tabControlMain_Selecting);
            // 
            // tabForecast
            // 
            this.tabForecast.BackColor = System.Drawing.Color.White;
            this.tabForecast.Controls.Add(this.pnlGrid1Wrapper);
            this.tabForecast.Controls.Add(this.pnlForecastErrors);
            this.tabForecast.Controls.Add(this.pnlForecastHeader);
            this.tabForecast.Location = new System.Drawing.Point(4, 49);
            this.tabForecast.Name = "tabForecast";
            this.tabForecast.Padding = new System.Windows.Forms.Padding(15);
            this.tabForecast.Size = new System.Drawing.Size(1092, 697);
            this.tabForecast.TabIndex = 0;
            this.tabForecast.Text = "Forecast Files";
            // 
            // pnlGrid1Wrapper
            // 
            this.pnlGrid1Wrapper.Controls.Add(this.dgvForecast1);
            this.pnlGrid1Wrapper.Controls.Add(this.pnlStatsBar1);
            this.pnlGrid1Wrapper.Controls.Add(this.pnlHeader1);
            this.pnlGrid1Wrapper.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlGrid1Wrapper.Location = new System.Drawing.Point(15, 75);
            this.pnlGrid1Wrapper.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
            this.pnlGrid1Wrapper.Name = "pnlGrid1Wrapper";
            this.pnlGrid1Wrapper.Padding = new System.Windows.Forms.Padding(0, 10, 10, 0);
            this.pnlGrid1Wrapper.Size = new System.Drawing.Size(612, 607);
            this.pnlGrid1Wrapper.TabIndex = 0;
            // 
            // dgvForecast1
            // 
            this.dgvForecast1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvForecast1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvForecast1.Location = new System.Drawing.Point(0, 80);
            this.dgvForecast1.Name = "dgvForecast1";
            this.dgvForecast1.ReadOnly = true;
            this.dgvForecast1.RowTemplate.Height = 25;
            this.dgvForecast1.Size = new System.Drawing.Size(602, 527);
            this.dgvForecast1.TabIndex = 1;
            // 
            // pnlStatsBar1
            // 
            this.pnlStatsBar1.BackColor = System.Drawing.Color.White;
            this.pnlStatsBar1.Controls.Add(this.lblTotal1);
            this.pnlStatsBar1.Controls.Add(this.lblActive1);
            this.pnlStatsBar1.Controls.Add(this.lblInactive1);
            this.pnlStatsBar1.Controls.Add(this.lblInvalid1);
            this.pnlStatsBar1.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlStatsBar1.Location = new System.Drawing.Point(0, 50);
            this.pnlStatsBar1.Name = "pnlStatsBar1";
            this.pnlStatsBar1.Padding = new System.Windows.Forms.Padding(5, 5, 10, 0);
            this.pnlStatsBar1.Size = new System.Drawing.Size(602, 30);
            this.pnlStatsBar1.TabIndex = 2;
            this.pnlStatsBar1.Visible = false;
            this.pnlStatsBar1.WrapContents = false;
            // 
            // lblTotal1
            // 
            this.lblTotal1.AutoSize = true;
            this.lblTotal1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblTotal1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(212)))));
            this.lblTotal1.Location = new System.Drawing.Point(5, 5);
            this.lblTotal1.Margin = new System.Windows.Forms.Padding(0, 0, 15, 0);
            this.lblTotal1.Name = "lblTotal1";
            this.lblTotal1.Size = new System.Drawing.Size(47, 15);
            this.lblTotal1.TabIndex = 0;
            this.lblTotal1.Text = "Total: 0";
            // 
            // lblActive1
            // 
            this.lblActive1.AutoSize = true;
            this.lblActive1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblActive1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(167)))), ((int)(((byte)(69)))));
            this.lblActive1.Location = new System.Drawing.Point(67, 5);
            this.lblActive1.Margin = new System.Windows.Forms.Padding(0, 0, 15, 0);
            this.lblActive1.Name = "lblActive1";
            this.lblActive1.Size = new System.Drawing.Size(56, 15);
            this.lblActive1.TabIndex = 1;
            this.lblActive1.Text = "Active: 0";
            // 
            // lblInactive1
            // 
            this.lblInactive1.AutoSize = true;
            this.lblInactive1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblInactive1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblInactive1.Location = new System.Drawing.Point(138, 5);
            this.lblInactive1.Margin = new System.Windows.Forms.Padding(0, 0, 15, 0);
            this.lblInactive1.Name = "lblInactive1";
            this.lblInactive1.Size = new System.Drawing.Size(65, 15);
            this.lblInactive1.TabIndex = 2;
            this.lblInactive1.Text = "Inactive: 0";
            // 
            // lblInvalid1
            // 
            this.lblInvalid1.AutoSize = true;
            this.lblInvalid1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblInvalid1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(53)))), ((int)(((byte)(69)))));
            this.lblInvalid1.Location = new System.Drawing.Point(218, 5);
            this.lblInvalid1.Margin = new System.Windows.Forms.Padding(0, 0, 15, 0);
            this.lblInvalid1.Name = "lblInvalid1";
            this.lblInvalid1.Size = new System.Drawing.Size(57, 15);
            this.lblInvalid1.TabIndex = 3;
            this.lblInvalid1.Text = "Invalid: 0";
            // 
            // pnlHeader1
            // 
            this.pnlHeader1.Controls.Add(this.pnlSearch1);
            this.pnlHeader1.Controls.Add(this.lblForecast1);
            this.pnlHeader1.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader1.Location = new System.Drawing.Point(0, 10);
            this.pnlHeader1.Name = "pnlHeader1";
            this.pnlHeader1.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
            this.pnlHeader1.Size = new System.Drawing.Size(602, 40);
            this.pnlHeader1.TabIndex = 0;
            // 
            // pnlSearch1
            // 
            this.pnlSearch1.Controls.Add(this.txtSearchF1_Real);
            this.pnlSearch1.Controls.Add(this.lblIcon1);
            this.pnlSearch1.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.pnlSearch1.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlSearch1.Location = new System.Drawing.Point(109, 4);
            this.pnlSearch1.Name = "pnlSearch1";
            this.pnlSearch1.Padding = new System.Windows.Forms.Padding(10, 7, 10, 5);
            this.pnlSearch1.Size = new System.Drawing.Size(240, 32);
            this.pnlSearch1.TabIndex = 1;
            this.pnlSearch1.Visible = false;
            // 
            // txtSearchF1_Real
            // 
            this.txtSearchF1_Real.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtSearchF1_Real.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtSearchF1_Real.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.txtSearchF1_Real.Location = new System.Drawing.Point(29, 7);
            this.txtSearchF1_Real.Name = "txtSearchF1_Real";
            this.txtSearchF1_Real.Size = new System.Drawing.Size(201, 17);
            this.txtSearchF1_Real.TabIndex = 1;
            this.txtSearchF1_Real.Text = "Search C-ASIN...";
            // 
            // lblIcon1
            // 
            this.lblIcon1.AutoSize = true;
            this.lblIcon1.BackColor = System.Drawing.Color.Transparent;
            this.lblIcon1.Dock = System.Windows.Forms.DockStyle.Left;
            this.lblIcon1.Font = new System.Drawing.Font("Segoe UI Symbol", 9F);
            this.lblIcon1.Location = new System.Drawing.Point(10, 7);
            this.lblIcon1.Name = "lblIcon1";
            this.lblIcon1.Padding = new System.Windows.Forms.Padding(0, 2, 0, 0);
            this.lblIcon1.Size = new System.Drawing.Size(19, 17);
            this.lblIcon1.TabIndex = 0;
            this.lblIcon1.Text = "🔍";
            // 
            // lblForecast1
            // 
            this.lblForecast1.AutoSize = true;
            this.lblForecast1.Dock = System.Windows.Forms.DockStyle.Left;
            this.lblForecast1.Font = new System.Drawing.Font("Segoe UI Semibold", 12F);
            this.lblForecast1.ForeColor = System.Drawing.Color.Silver;
            this.lblForecast1.Location = new System.Drawing.Point(0, 4);
            this.lblForecast1.Name = "lblForecast1";
            this.lblForecast1.Padding = new System.Windows.Forms.Padding(5, 0, 15, 0);
            this.lblForecast1.Size = new System.Drawing.Size(109, 21);
            this.lblForecast1.TabIndex = 0;
            this.lblForecast1.Text = "Empty Slot";
            this.lblForecast1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pnlForecastErrors
            // 
            this.pnlForecastErrors.BackColor = System.Drawing.Color.WhiteSmoke;
            this.pnlForecastErrors.Controls.Add(this.dgvForecastErrors);
            this.pnlForecastErrors.Controls.Add(this.lblForecastErrorHeader);
            this.pnlForecastErrors.Controls.Add(this.btnMarkInvalid);
            this.pnlForecastErrors.Controls.Add(this.btnExportForecastErrors);
            this.pnlForecastErrors.Dock = System.Windows.Forms.DockStyle.Right;
            this.pnlForecastErrors.Location = new System.Drawing.Point(627, 75);
            this.pnlForecastErrors.Name = "pnlForecastErrors";
            this.pnlForecastErrors.Padding = new System.Windows.Forms.Padding(10);
            this.pnlForecastErrors.Size = new System.Drawing.Size(450, 607);
            this.pnlForecastErrors.TabIndex = 2;
            this.pnlForecastErrors.Visible = false;
            // 
            // dgvForecastErrors
            // 
            this.dgvForecastErrors.AllowUserToAddRows = false;
            this.dgvForecastErrors.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvForecastErrors.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.dgvForecastErrors.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvForecastErrors.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.chkSelect});
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI", 10F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvForecastErrors.DefaultCellStyle = dataGridViewCellStyle1;
            this.dgvForecastErrors.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvForecastErrors.Location = new System.Drawing.Point(10, 50);
            this.dgvForecastErrors.Name = "dgvForecastErrors";
            this.dgvForecastErrors.RowHeadersVisible = false;
            this.dgvForecastErrors.RowTemplate.Height = 25;
            this.dgvForecastErrors.Size = new System.Drawing.Size(430, 477);
            this.dgvForecastErrors.TabIndex = 1;
            this.dgvForecastErrors.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvForecastErrors_CellContentClick);
            this.dgvForecastErrors.DataBindingComplete += new System.Windows.Forms.DataGridViewBindingCompleteEventHandler(this.dgvForecastErrors_DataBindingComplete);
            // 
            // chkSelect
            // 
            this.chkSelect.HeaderText = "Select";
            this.chkSelect.Name = "chkSelect";
            // 
            // lblForecastErrorHeader
            // 
            this.lblForecastErrorHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblForecastErrorHeader.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblForecastErrorHeader.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(53)))), ((int)(((byte)(69)))));
            this.lblForecastErrorHeader.Location = new System.Drawing.Point(10, 10);
            this.lblForecastErrorHeader.Name = "lblForecastErrorHeader";
            this.lblForecastErrorHeader.Size = new System.Drawing.Size(430, 40);
            this.lblForecastErrorHeader.TabIndex = 0;
            this.lblForecastErrorHeader.Text = "Invalid Forecast Items";
            // 
            // btnMarkInvalid
            // 
            this.btnMarkInvalid.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.btnMarkInvalid.Location = new System.Drawing.Point(10, 527);
            this.btnMarkInvalid.Name = "btnMarkInvalid";
            this.btnMarkInvalid.Size = new System.Drawing.Size(430, 35);
            this.btnMarkInvalid.TabIndex = 4;
            this.btnMarkInvalid.Text = "Mark Selected as Invalid";
            this.btnMarkInvalid.UseVisualStyleBackColor = true;
            this.btnMarkInvalid.Click += new System.EventHandler(this.btnMarkInvalid_Click);
            // 
            // btnExportForecastErrors
            // 
            this.btnExportForecastErrors.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.btnExportForecastErrors.Location = new System.Drawing.Point(10, 562);
            this.btnExportForecastErrors.Name = "btnExportForecastErrors";
            this.btnExportForecastErrors.Size = new System.Drawing.Size(430, 35);
            this.btnExportForecastErrors.TabIndex = 2;
            this.btnExportForecastErrors.Text = "Export Errors";
            this.btnExportForecastErrors.UseVisualStyleBackColor = true;
            this.btnExportForecastErrors.Click += new System.EventHandler(this.btnExportForecastErrors_Click);
            // 
            // pnlForecastHeader
            // 
            this.pnlForecastHeader.Controls.Add(this.btnBrowseForecast);
            this.pnlForecastHeader.Controls.Add(this.cmbDbForecasts);
            this.pnlForecastHeader.Controls.Add(this.progressBarTop);
            this.pnlForecastHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlForecastHeader.Location = new System.Drawing.Point(15, 15);
            this.pnlForecastHeader.Name = "pnlForecastHeader";
            this.pnlForecastHeader.Size = new System.Drawing.Size(1062, 60);
            this.pnlForecastHeader.TabIndex = 0;
            // 
            // btnBrowseForecast
            // 
            this.btnBrowseForecast.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(212)))));
            this.btnBrowseForecast.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBrowseForecast.FlatAppearance.BorderSize = 0;
            this.btnBrowseForecast.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBrowseForecast.ForeColor = System.Drawing.Color.White;
            this.btnBrowseForecast.Location = new System.Drawing.Point(0, 10);
            this.btnBrowseForecast.Name = "btnBrowseForecast";
            this.btnBrowseForecast.Size = new System.Drawing.Size(150, 36);
            this.btnBrowseForecast.TabIndex = 0;
            this.btnBrowseForecast.Text = " Upload Forecast";
            this.btnBrowseForecast.UseVisualStyleBackColor = false;
            this.btnBrowseForecast.Click += new System.EventHandler(this.btnBrowseForecast_Click);
            // 
            // cmbDbForecasts
            // 
            this.cmbDbForecasts.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDbForecasts.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbDbForecasts.FormattingEnabled = true;
            this.cmbDbForecasts.Location = new System.Drawing.Point(165, 15);
            this.cmbDbForecasts.Name = "cmbDbForecasts";
            this.cmbDbForecasts.Size = new System.Drawing.Size(200, 25);
            this.cmbDbForecasts.TabIndex = 1;
            this.cmbDbForecasts.SelectionChangeCommitted += new System.EventHandler(this.cmbDbForecasts_SelectionChangeCommitted);
            // 
            // progressBarTop
            // 
            this.progressBarTop.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.progressBarTop.Location = new System.Drawing.Point(0, 57);
            this.progressBarTop.Name = "progressBarTop";
            this.progressBarTop.Size = new System.Drawing.Size(1062, 3);
            this.progressBarTop.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBarTop.TabIndex = 2;
            this.progressBarTop.Visible = false;
            // 
            // tabOrder
            // 
            this.tabOrder.BackColor = System.Drawing.Color.White;
            this.tabOrder.Controls.Add(this.pnlOrderGridWrapper);
            this.tabOrder.Controls.Add(this.pnlOrderErrors);
            this.tabOrder.Controls.Add(this.pnlOrderHeader);
            this.tabOrder.Location = new System.Drawing.Point(4, 49);
            this.tabOrder.Name = "tabOrder";
            this.tabOrder.Padding = new System.Windows.Forms.Padding(15);
            this.tabOrder.Size = new System.Drawing.Size(1092, 697);
            this.tabOrder.TabIndex = 1;
            this.tabOrder.Text = "Order File";
            // 
            // pnlOrderGridWrapper
            // 
            this.pnlOrderGridWrapper.Controls.Add(this.dgvOrder);
            this.pnlOrderGridWrapper.Controls.Add(this.pnlStatsBarOrder);
            this.pnlOrderGridWrapper.Controls.Add(this.lblOrderMonth);
            this.pnlOrderGridWrapper.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlOrderGridWrapper.Location = new System.Drawing.Point(15, 75);
            this.pnlOrderGridWrapper.Name = "pnlOrderGridWrapper";
            this.pnlOrderGridWrapper.Size = new System.Drawing.Size(612, 607);
            this.pnlOrderGridWrapper.TabIndex = 4;
            // 
            // dgvOrder
            // 
            this.dgvOrder.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvOrder.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvOrder.Location = new System.Drawing.Point(0, 61);
            this.dgvOrder.Name = "dgvOrder";
            this.dgvOrder.ReadOnly = true;
            this.dgvOrder.RowTemplate.Height = 25;
            this.dgvOrder.Size = new System.Drawing.Size(612, 546);
            this.dgvOrder.TabIndex = 0;
            // 
            // pnlStatsBarOrder
            // 
            this.pnlStatsBarOrder.BackColor = System.Drawing.Color.White;
            this.pnlStatsBarOrder.Controls.Add(this.lblTotalOrder);
            this.pnlStatsBarOrder.Controls.Add(this.lblActiveOrder);
            this.pnlStatsBarOrder.Controls.Add(this.lblInactiveOrder);
            this.pnlStatsBarOrder.Controls.Add(this.lblInvalidOrder);
            this.pnlStatsBarOrder.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlStatsBarOrder.Location = new System.Drawing.Point(0, 31);
            this.pnlStatsBarOrder.Name = "pnlStatsBarOrder";
            this.pnlStatsBarOrder.Padding = new System.Windows.Forms.Padding(5, 5, 10, 0);
            this.pnlStatsBarOrder.Size = new System.Drawing.Size(612, 30);
            this.pnlStatsBarOrder.TabIndex = 1;
            this.pnlStatsBarOrder.Visible = false;
            this.pnlStatsBarOrder.WrapContents = false;
            // 
            // lblTotalOrder
            // 
            this.lblTotalOrder.AutoSize = true;
            this.lblTotalOrder.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblTotalOrder.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(212)))));
            this.lblTotalOrder.Location = new System.Drawing.Point(5, 5);
            this.lblTotalOrder.Margin = new System.Windows.Forms.Padding(0, 0, 15, 0);
            this.lblTotalOrder.Name = "lblTotalOrder";
            this.lblTotalOrder.Size = new System.Drawing.Size(47, 15);
            this.lblTotalOrder.TabIndex = 0;
            this.lblTotalOrder.Text = "Total: 0";
            // 
            // lblActiveOrder
            // 
            this.lblActiveOrder.AutoSize = true;
            this.lblActiveOrder.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblActiveOrder.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(167)))), ((int)(((byte)(69)))));
            this.lblActiveOrder.Location = new System.Drawing.Point(67, 5);
            this.lblActiveOrder.Margin = new System.Windows.Forms.Padding(0, 0, 15, 0);
            this.lblActiveOrder.Name = "lblActiveOrder";
            this.lblActiveOrder.Size = new System.Drawing.Size(56, 15);
            this.lblActiveOrder.TabIndex = 1;
            this.lblActiveOrder.Text = "Active: 0";
            // 
            // lblInactiveOrder
            // 
            this.lblInactiveOrder.AutoSize = true;
            this.lblInactiveOrder.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblInactiveOrder.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblInactiveOrder.Location = new System.Drawing.Point(138, 5);
            this.lblInactiveOrder.Margin = new System.Windows.Forms.Padding(0, 0, 15, 0);
            this.lblInactiveOrder.Name = "lblInactiveOrder";
            this.lblInactiveOrder.Size = new System.Drawing.Size(65, 15);
            this.lblInactiveOrder.TabIndex = 2;
            this.lblInactiveOrder.Text = "Inactive: 0";
            // 
            // lblInvalidOrder
            // 
            this.lblInvalidOrder.AutoSize = true;
            this.lblInvalidOrder.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblInvalidOrder.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(53)))), ((int)(((byte)(69)))));
            this.lblInvalidOrder.Location = new System.Drawing.Point(218, 5);
            this.lblInvalidOrder.Margin = new System.Windows.Forms.Padding(0, 0, 15, 0);
            this.lblInvalidOrder.Name = "lblInvalidOrder";
            this.lblInvalidOrder.Size = new System.Drawing.Size(57, 15);
            this.lblInvalidOrder.TabIndex = 3;
            this.lblInvalidOrder.Text = "Invalid: 0";
            // 
            // lblOrderMonth
            // 
            this.lblOrderMonth.AutoSize = true;
            this.lblOrderMonth.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblOrderMonth.Font = new System.Drawing.Font("Segoe UI Semibold", 12F);
            this.lblOrderMonth.ForeColor = System.Drawing.Color.Silver;
            this.lblOrderMonth.Location = new System.Drawing.Point(0, 0);
            this.lblOrderMonth.Name = "lblOrderMonth";
            this.lblOrderMonth.Padding = new System.Windows.Forms.Padding(5, 5, 15, 5);
            this.lblOrderMonth.Size = new System.Drawing.Size(158, 31);
            this.lblOrderMonth.TabIndex = 2;
            this.lblOrderMonth.Text = "No Order Loaded";
            this.lblOrderMonth.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pnlOrderErrors
            // 
            this.pnlOrderErrors.BackColor = System.Drawing.Color.WhiteSmoke;
            this.pnlOrderErrors.Controls.Add(this.dgvOrderErrors);
            this.pnlOrderErrors.Controls.Add(this.lblOrderErrorHeader);
            this.pnlOrderErrors.Controls.Add(this.btnExportOrderErrors);
            this.pnlOrderErrors.Dock = System.Windows.Forms.DockStyle.Right;
            this.pnlOrderErrors.Location = new System.Drawing.Point(627, 75);
            this.pnlOrderErrors.Name = "pnlOrderErrors";
            this.pnlOrderErrors.Padding = new System.Windows.Forms.Padding(10);
            this.pnlOrderErrors.Size = new System.Drawing.Size(450, 607);
            this.pnlOrderErrors.TabIndex = 3;
            this.pnlOrderErrors.Visible = false;
            // 
            // dgvOrderErrors
            // 
            this.dgvOrderErrors.AllowUserToAddRows = false;
            this.dgvOrderErrors.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvOrderErrors.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.dgvOrderErrors.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI", 10F);
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvOrderErrors.DefaultCellStyle = dataGridViewCellStyle2;
            this.dgvOrderErrors.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvOrderErrors.Location = new System.Drawing.Point(10, 50);
            this.dgvOrderErrors.Name = "dgvOrderErrors";
            this.dgvOrderErrors.ReadOnly = true;
            this.dgvOrderErrors.RowHeadersVisible = false;
            this.dgvOrderErrors.RowTemplate.Height = 25;
            this.dgvOrderErrors.Size = new System.Drawing.Size(430, 512);
            this.dgvOrderErrors.TabIndex = 1;
            // 
            // lblOrderErrorHeader
            // 
            this.lblOrderErrorHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblOrderErrorHeader.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblOrderErrorHeader.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(53)))), ((int)(((byte)(69)))));
            this.lblOrderErrorHeader.Location = new System.Drawing.Point(10, 10);
            this.lblOrderErrorHeader.Name = "lblOrderErrorHeader";
            this.lblOrderErrorHeader.Size = new System.Drawing.Size(430, 40);
            this.lblOrderErrorHeader.TabIndex = 0;
            this.lblOrderErrorHeader.Text = "Invalid Order Items";
            // 
            // btnExportOrderErrors
            // 
            this.btnExportOrderErrors.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.btnExportOrderErrors.Location = new System.Drawing.Point(10, 562);
            this.btnExportOrderErrors.Name = "btnExportOrderErrors";
            this.btnExportOrderErrors.Size = new System.Drawing.Size(430, 35);
            this.btnExportOrderErrors.TabIndex = 2;
            this.btnExportOrderErrors.Text = "Export Errors";
            this.btnExportOrderErrors.UseVisualStyleBackColor = true;
            this.btnExportOrderErrors.Click += new System.EventHandler(this.btnExportOrderErrors_Click);
            // 
            // pnlOrderHeader
            // 
            this.pnlOrderHeader.Controls.Add(this.pnlSearchOrder);
            this.pnlOrderHeader.Controls.Add(this.btnBrowseOrder);
            this.pnlOrderHeader.Controls.Add(this.cmbDbOrders);
            this.pnlOrderHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlOrderHeader.Location = new System.Drawing.Point(15, 15);
            this.pnlOrderHeader.Name = "pnlOrderHeader";
            this.pnlOrderHeader.Size = new System.Drawing.Size(1062, 60);
            this.pnlOrderHeader.TabIndex = 2;
            // 
            // pnlSearchOrder
            // 
            this.pnlSearchOrder.Controls.Add(this.txtSearchOrder_Real);
            this.pnlSearchOrder.Controls.Add(this.lblIconOrder);
            this.pnlSearchOrder.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.pnlSearchOrder.Location = new System.Drawing.Point(380, 14);
            this.pnlSearchOrder.Name = "pnlSearchOrder";
            this.pnlSearchOrder.Padding = new System.Windows.Forms.Padding(10, 7, 10, 5);
            this.pnlSearchOrder.Size = new System.Drawing.Size(240, 32);
            this.pnlSearchOrder.TabIndex = 3;
            this.pnlSearchOrder.Visible = false;
            // 
            // txtSearchOrder_Real
            // 
            this.txtSearchOrder_Real.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtSearchOrder_Real.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtSearchOrder_Real.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.txtSearchOrder_Real.Location = new System.Drawing.Point(29, 7);
            this.txtSearchOrder_Real.Name = "txtSearchOrder_Real";
            this.txtSearchOrder_Real.Size = new System.Drawing.Size(201, 17);
            this.txtSearchOrder_Real.TabIndex = 1;
            this.txtSearchOrder_Real.Text = "Search C-ASIN...";
            // 
            // lblIconOrder
            // 
            this.lblIconOrder.AutoSize = true;
            this.lblIconOrder.BackColor = System.Drawing.Color.Transparent;
            this.lblIconOrder.Dock = System.Windows.Forms.DockStyle.Left;
            this.lblIconOrder.Font = new System.Drawing.Font("Segoe UI Symbol", 9F);
            this.lblIconOrder.Location = new System.Drawing.Point(10, 7);
            this.lblIconOrder.Name = "lblIconOrder";
            this.lblIconOrder.Padding = new System.Windows.Forms.Padding(0, 2, 0, 0);
            this.lblIconOrder.Size = new System.Drawing.Size(19, 17);
            this.lblIconOrder.TabIndex = 0;
            this.lblIconOrder.Text = "🔍";
            // 
            // btnBrowseOrder
            // 
            this.btnBrowseOrder.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(212)))));
            this.btnBrowseOrder.FlatAppearance.BorderSize = 0;
            this.btnBrowseOrder.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBrowseOrder.ForeColor = System.Drawing.Color.White;
            this.btnBrowseOrder.Location = new System.Drawing.Point(0, 10);
            this.btnBrowseOrder.Name = "btnBrowseOrder";
            this.btnBrowseOrder.Size = new System.Drawing.Size(150, 36);
            this.btnBrowseOrder.TabIndex = 0;
            this.btnBrowseOrder.Text = " Upload Orders";
            this.btnBrowseOrder.UseVisualStyleBackColor = false;
            this.btnBrowseOrder.Click += new System.EventHandler(this.btnBrowseOrder_Click);
            // 
            // cmbDbOrders
            // 
            this.cmbDbOrders.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDbOrders.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbDbOrders.FormattingEnabled = true;
            this.cmbDbOrders.Location = new System.Drawing.Point(165, 15);
            this.cmbDbOrders.Name = "cmbDbOrders";
            this.cmbDbOrders.Size = new System.Drawing.Size(200, 25);
            this.cmbDbOrders.TabIndex = 1;
            this.cmbDbOrders.SelectionChangeCommitted += new System.EventHandler(this.cmbDbOrders_SelectionChangeCommitted);
            // 
            // tabOpenNewForm
            // 
            this.tabOpenNewForm.Location = new System.Drawing.Point(4, 49);
            this.tabOpenNewForm.Name = "tabOpenNewForm";
            this.tabOpenNewForm.Padding = new System.Windows.Forms.Padding(3);
            this.tabOpenNewForm.Size = new System.Drawing.Size(1092, 697);
            this.tabOpenNewForm.TabIndex = 2;
            this.tabOpenNewForm.Text = "Open Other Tool";
            this.tabOpenNewForm.UseVisualStyleBackColor = true;
            // 
            // UploadForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1100, 750);
            this.Controls.Add(this.tabControlMain);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Name = "UploadForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Data Upload Manager";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.UploadForm_FormClosing);
            this.tabControlMain.ResumeLayout(false);
            this.tabForecast.ResumeLayout(false);
            this.pnlGrid1Wrapper.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvForecast1)).EndInit();
            this.pnlStatsBar1.ResumeLayout(false);
            this.pnlStatsBar1.PerformLayout();
            this.pnlHeader1.ResumeLayout(false);
            this.pnlHeader1.PerformLayout();
            this.pnlSearch1.ResumeLayout(false);
            this.pnlSearch1.PerformLayout();
            this.pnlForecastErrors.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvForecastErrors)).EndInit();
            this.pnlForecastHeader.ResumeLayout(false);
            this.tabOrder.ResumeLayout(false);
            this.pnlOrderGridWrapper.ResumeLayout(false);
            this.pnlOrderGridWrapper.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvOrder)).EndInit();
            this.pnlStatsBarOrder.ResumeLayout(false);
            this.pnlStatsBarOrder.PerformLayout();
            this.pnlOrderErrors.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvOrderErrors)).EndInit();
            this.pnlOrderHeader.ResumeLayout(false);
            this.pnlSearchOrder.ResumeLayout(false);
            this.pnlSearchOrder.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControlMain;
        private System.Windows.Forms.TabPage tabForecast;
        private System.Windows.Forms.TabPage tabOrder;
        private System.Windows.Forms.TabPage tabOpenNewForm;

        // Forecast Controls
        private System.Windows.Forms.Panel pnlForecastHeader;
        private System.Windows.Forms.Button btnBrowseForecast;
        private System.Windows.Forms.ComboBox cmbDbForecasts;
        private System.Windows.Forms.ProgressBar progressBarTop;

        // Grid 1
        private System.Windows.Forms.Panel pnlGrid1Wrapper;
        private System.Windows.Forms.Panel pnlHeader1;
        private System.Windows.Forms.Panel pnlSearch1;
        private System.Windows.Forms.TextBox txtSearchF1_Real;
        private System.Windows.Forms.Label lblIcon1;
        private System.Windows.Forms.DataGridView dgvForecast1;
        private System.Windows.Forms.Label lblForecast1;

        // Grid 1 Stats
        private System.Windows.Forms.FlowLayoutPanel pnlStatsBar1;
        private System.Windows.Forms.Label lblTotal1;
        private System.Windows.Forms.Label lblActive1;
        private System.Windows.Forms.Label lblInactive1;
        private System.Windows.Forms.Label lblInvalid1;

        // Forecast Error Controls
        private System.Windows.Forms.Panel pnlForecastErrors;
        private System.Windows.Forms.DataGridView dgvForecastErrors;
        private System.Windows.Forms.DataGridViewCheckBoxColumn chkSelect;
        private System.Windows.Forms.Label lblForecastErrorHeader;
        private System.Windows.Forms.Button btnExportForecastErrors;
        private System.Windows.Forms.Button btnMarkInvalid;

        // Order Controls
        private System.Windows.Forms.Panel pnlOrderHeader;
        private System.Windows.Forms.Panel pnlSearchOrder;
        private System.Windows.Forms.TextBox txtSearchOrder_Real;
        private System.Windows.Forms.Label lblIconOrder;
        private System.Windows.Forms.Button btnBrowseOrder;
        private System.Windows.Forms.ComboBox cmbDbOrders;

        // Order Grid & Wrapper Update
        private System.Windows.Forms.Panel pnlOrderGridWrapper;
        private System.Windows.Forms.DataGridView dgvOrder;

        // Order Grid Stats Bar
        private System.Windows.Forms.FlowLayoutPanel pnlStatsBarOrder;
        private System.Windows.Forms.Label lblTotalOrder;
        private System.Windows.Forms.Label lblActiveOrder;
        private System.Windows.Forms.Label lblInactiveOrder;
        private System.Windows.Forms.Label lblInvalidOrder;
        private System.Windows.Forms.Label lblOrderMonth;

        // Order Error Controls
        private System.Windows.Forms.Panel pnlOrderErrors;
        private System.Windows.Forms.DataGridView dgvOrderErrors;
        private System.Windows.Forms.Label lblOrderErrorHeader;
        private System.Windows.Forms.Button btnExportOrderErrors;
    }
}