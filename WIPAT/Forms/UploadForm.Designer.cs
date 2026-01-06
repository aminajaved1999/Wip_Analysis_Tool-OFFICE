namespace WIPAT
{
    partial class NewUploadForm
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
            this.tabControlMain = new System.Windows.Forms.TabControl();
            this.tabForecast = new System.Windows.Forms.TabPage();
            this.tableLayoutForecasts = new System.Windows.Forms.TableLayoutPanel();
            this.pnlGrid1Wrapper = new System.Windows.Forms.Panel();
            this.dgvForecast1 = new System.Windows.Forms.DataGridView();
            this.pnlHeader1 = new System.Windows.Forms.Panel();
            this.lblForecast1 = new System.Windows.Forms.Label();
            this.pnlGrid2Wrapper = new System.Windows.Forms.Panel();
            this.dgvForecast2 = new System.Windows.Forms.DataGridView();
            this.pnlHeader2 = new System.Windows.Forms.Panel();
            this.lblForecast2 = new System.Windows.Forms.Label();
            this.pnlForecastHeader = new System.Windows.Forms.Panel();
            this.btnBrowseForecast = new System.Windows.Forms.Button();
            this.cmbDbForecasts = new System.Windows.Forms.ComboBox();
            this.progressBarTop = new System.Windows.Forms.ProgressBar();
            this.tabOrder = new System.Windows.Forms.TabPage();
            this.dgvOrder = new System.Windows.Forms.DataGridView();
            this.pnlOrderErrors = new System.Windows.Forms.Panel();
            this.dgvOrderErrors = new System.Windows.Forms.DataGridView();
            this.lblOrderErrorHeader = new System.Windows.Forms.Label();
            this.btnExportErrors = new System.Windows.Forms.Button();
            this.pnlOrderHeader = new System.Windows.Forms.Panel();
            this.btnBrowseOrder = new System.Windows.Forms.Button();
            this.cmbDbOrders = new System.Windows.Forms.ComboBox();
            this.tabControlMain.SuspendLayout();
            this.tabForecast.SuspendLayout();
            this.tableLayoutForecasts.SuspendLayout();
            this.pnlGrid1Wrapper.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvForecast1)).BeginInit();
            this.pnlHeader1.SuspendLayout();
            this.pnlGrid2Wrapper.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvForecast2)).BeginInit();
            this.pnlHeader2.SuspendLayout();
            this.pnlForecastHeader.SuspendLayout();
            this.tabOrder.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvOrder)).BeginInit();
            this.pnlOrderErrors.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvOrderErrors)).BeginInit();
            this.pnlOrderHeader.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControlMain
            // 
            this.tabControlMain.Controls.Add(this.tabForecast);
            this.tabControlMain.Controls.Add(this.tabOrder);
            this.tabControlMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControlMain.Location = new System.Drawing.Point(0, 0);
            this.tabControlMain.Name = "tabControlMain";
            this.tabControlMain.SelectedIndex = 0;
            this.tabControlMain.Size = new System.Drawing.Size(1100, 750);
            this.tabControlMain.TabIndex = 0;
            // 
            // tabForecast
            // 
            this.tabForecast.BackColor = System.Drawing.Color.White;
            this.tabForecast.Controls.Add(this.tableLayoutForecasts);
            this.tabForecast.Controls.Add(this.pnlForecastHeader);
            this.tabForecast.Location = new System.Drawing.Point(4, 26);
            this.tabForecast.Name = "tabForecast";
            this.tabForecast.Padding = new System.Windows.Forms.Padding(15);
            this.tabForecast.Size = new System.Drawing.Size(1092, 720);
            this.tabForecast.TabIndex = 0;
            this.tabForecast.Text = "  Forecast Files  ";
            // 
            // tableLayoutForecasts
            // 
            this.tableLayoutForecasts.ColumnCount = 2;
            this.tableLayoutForecasts.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutForecasts.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutForecasts.Controls.Add(this.pnlGrid1Wrapper, 0, 0);
            this.tableLayoutForecasts.Controls.Add(this.pnlGrid2Wrapper, 1, 0);
            this.tableLayoutForecasts.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutForecasts.Location = new System.Drawing.Point(15, 75);
            this.tableLayoutForecasts.Name = "tableLayoutForecasts";
            this.tableLayoutForecasts.RowCount = 1;
            this.tableLayoutForecasts.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutForecasts.Size = new System.Drawing.Size(1062, 630);
            this.tableLayoutForecasts.TabIndex = 1;
            // 
            // pnlGrid1Wrapper
            // 
            this.pnlGrid1Wrapper.Controls.Add(this.dgvForecast1);
            this.pnlGrid1Wrapper.Controls.Add(this.pnlHeader1);
            this.pnlGrid1Wrapper.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlGrid1Wrapper.Location = new System.Drawing.Point(0, 0);
            this.pnlGrid1Wrapper.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
            this.pnlGrid1Wrapper.Name = "pnlGrid1Wrapper";
            this.pnlGrid1Wrapper.Padding = new System.Windows.Forms.Padding(0, 10, 0, 0);
            this.pnlGrid1Wrapper.Size = new System.Drawing.Size(521, 630);
            this.pnlGrid1Wrapper.TabIndex = 0;
            // 
            // dgvForecast1
            // 
            this.dgvForecast1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvForecast1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvForecast1.Location = new System.Drawing.Point(0, 50);
            this.dgvForecast1.Name = "dgvForecast1";
            this.dgvForecast1.RowTemplate.Height = 25;
            this.dgvForecast1.Size = new System.Drawing.Size(521, 580);
            this.dgvForecast1.TabIndex = 1;
            // 
            // pnlHeader1
            // 
            this.pnlHeader1.Controls.Add(this.lblForecast1);
            this.pnlHeader1.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader1.Location = new System.Drawing.Point(0, 10);
            this.pnlHeader1.Name = "pnlHeader1";
            this.pnlHeader1.Size = new System.Drawing.Size(521, 40);
            this.pnlHeader1.TabIndex = 0;
            // 
            // lblForecast1
            // 
            this.lblForecast1.Dock = System.Windows.Forms.DockStyle.Left;
            this.lblForecast1.Font = new System.Drawing.Font("Segoe UI Semibold", 12F);
            this.lblForecast1.ForeColor = System.Drawing.Color.Silver;
            this.lblForecast1.Location = new System.Drawing.Point(0, 0);
            this.lblForecast1.Name = "lblForecast1";
            this.lblForecast1.Size = new System.Drawing.Size(250, 40);
            this.lblForecast1.TabIndex = 0;
            this.lblForecast1.Text = "Empty Slot";
            this.lblForecast1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pnlGrid2Wrapper
            // 
            this.pnlGrid2Wrapper.Controls.Add(this.dgvForecast2);
            this.pnlGrid2Wrapper.Controls.Add(this.pnlHeader2);
            this.pnlGrid2Wrapper.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlGrid2Wrapper.Location = new System.Drawing.Point(541, 0);
            this.pnlGrid2Wrapper.Margin = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.pnlGrid2Wrapper.Name = "pnlGrid2Wrapper";
            this.pnlGrid2Wrapper.Padding = new System.Windows.Forms.Padding(0, 10, 0, 0);
            this.pnlGrid2Wrapper.Size = new System.Drawing.Size(521, 630);
            this.pnlGrid2Wrapper.TabIndex = 1;
            // 
            // dgvForecast2
            // 
            this.dgvForecast2.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvForecast2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvForecast2.Location = new System.Drawing.Point(0, 50);
            this.dgvForecast2.Name = "dgvForecast2";
            this.dgvForecast2.RowTemplate.Height = 25;
            this.dgvForecast2.Size = new System.Drawing.Size(521, 580);
            this.dgvForecast2.TabIndex = 1;
            // 
            // pnlHeader2
            // 
            this.pnlHeader2.Controls.Add(this.lblForecast2);
            this.pnlHeader2.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader2.Location = new System.Drawing.Point(0, 10);
            this.pnlHeader2.Name = "pnlHeader2";
            this.pnlHeader2.Size = new System.Drawing.Size(521, 40);
            this.pnlHeader2.TabIndex = 0;
            // 
            // lblForecast2
            // 
            this.lblForecast2.Dock = System.Windows.Forms.DockStyle.Left;
            this.lblForecast2.Font = new System.Drawing.Font("Segoe UI Semibold", 12F);
            this.lblForecast2.ForeColor = System.Drawing.Color.Silver;
            this.lblForecast2.Location = new System.Drawing.Point(0, 0);
            this.lblForecast2.Name = "lblForecast2";
            this.lblForecast2.Size = new System.Drawing.Size(250, 40);
            this.lblForecast2.TabIndex = 0;
            this.lblForecast2.Text = "Empty Slot";
            this.lblForecast2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
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
            this.tabOrder.Controls.Add(this.dgvOrder);
            this.tabOrder.Controls.Add(this.pnlOrderErrors);
            this.tabOrder.Controls.Add(this.pnlOrderHeader);
            this.tabOrder.Location = new System.Drawing.Point(4, 26);
            this.tabOrder.Name = "tabOrder";
            this.tabOrder.Padding = new System.Windows.Forms.Padding(15);
            this.tabOrder.Size = new System.Drawing.Size(1092, 720);
            this.tabOrder.TabIndex = 1;
            this.tabOrder.Text = "  Order File  ";
            // 
            // dgvOrder
            // 
            this.dgvOrder.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvOrder.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvOrder.Location = new System.Drawing.Point(15, 75);
            this.dgvOrder.Name = "dgvOrder";
            this.dgvOrder.RowTemplate.Height = 25;
            this.dgvOrder.Size = new System.Drawing.Size(712, 630);
            this.dgvOrder.TabIndex = 0;
            // 
            // pnlOrderErrors
            // 
            this.pnlOrderErrors.BackColor = System.Drawing.Color.WhiteSmoke;
            this.pnlOrderErrors.Controls.Add(this.dgvOrderErrors);
            this.pnlOrderErrors.Controls.Add(this.lblOrderErrorHeader);
            this.pnlOrderErrors.Controls.Add(this.btnExportErrors);
            this.pnlOrderErrors.Dock = System.Windows.Forms.DockStyle.Right;
            this.pnlOrderErrors.Location = new System.Drawing.Point(727, 75);
            this.pnlOrderErrors.Name = "pnlOrderErrors";
            this.pnlOrderErrors.Padding = new System.Windows.Forms.Padding(10);
            this.pnlOrderErrors.Size = new System.Drawing.Size(350, 630);
            this.pnlOrderErrors.TabIndex = 1;
            this.pnlOrderErrors.Visible = false;
            // 
            // dgvOrderErrors
            // 
            this.dgvOrderErrors.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvOrderErrors.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvOrderErrors.Location = new System.Drawing.Point(10, 50);
            this.dgvOrderErrors.Name = "dgvOrderErrors";
            this.dgvOrderErrors.RowTemplate.Height = 25;
            this.dgvOrderErrors.Size = new System.Drawing.Size(330, 535);
            this.dgvOrderErrors.TabIndex = 1;
            // 
            // lblOrderErrorHeader
            // 
            this.lblOrderErrorHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblOrderErrorHeader.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblOrderErrorHeader.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(53)))), ((int)(((byte)(69)))));
            this.lblOrderErrorHeader.Location = new System.Drawing.Point(10, 10);
            this.lblOrderErrorHeader.Name = "lblOrderErrorHeader";
            this.lblOrderErrorHeader.Size = new System.Drawing.Size(330, 40);
            this.lblOrderErrorHeader.TabIndex = 0;
            this.lblOrderErrorHeader.Text = "Invalid Items";
            // 
            // btnExportErrors
            // 
            this.btnExportErrors.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.btnExportErrors.Location = new System.Drawing.Point(10, 585);
            this.btnExportErrors.Name = "btnExportErrors";
            this.btnExportErrors.Size = new System.Drawing.Size(330, 35);
            this.btnExportErrors.TabIndex = 2;
            this.btnExportErrors.Text = "Export Errors";
            this.btnExportErrors.UseVisualStyleBackColor = true;
            this.btnExportErrors.Click += new System.EventHandler(this.btnExportErrors_Click);
            // 
            // pnlOrderHeader
            // 
            this.pnlOrderHeader.Controls.Add(this.btnBrowseOrder);
            this.pnlOrderHeader.Controls.Add(this.cmbDbOrders);
            this.pnlOrderHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlOrderHeader.Location = new System.Drawing.Point(15, 15);
            this.pnlOrderHeader.Name = "pnlOrderHeader";
            this.pnlOrderHeader.Size = new System.Drawing.Size(1062, 60);
            this.pnlOrderHeader.TabIndex = 2;
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
            // NewUploadForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1100, 750);
            this.Controls.Add(this.tabControlMain);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Name = "NewUploadForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Data Upload Manager";
            this.tabControlMain.ResumeLayout(false);
            this.tabForecast.ResumeLayout(false);
            this.tableLayoutForecasts.ResumeLayout(false);
            this.pnlGrid1Wrapper.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvForecast1)).EndInit();
            this.pnlHeader1.ResumeLayout(false);
            this.pnlGrid2Wrapper.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvForecast2)).EndInit();
            this.pnlHeader2.ResumeLayout(false);
            this.pnlForecastHeader.ResumeLayout(false);
            this.tabOrder.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvOrder)).EndInit();
            this.pnlOrderErrors.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvOrderErrors)).EndInit();
            this.pnlOrderHeader.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControlMain;
        private System.Windows.Forms.TabPage tabForecast;
        private System.Windows.Forms.TabPage tabOrder;

        // Forecast Controls
        private System.Windows.Forms.Panel pnlForecastHeader;
        private System.Windows.Forms.Button btnBrowseForecast;
        private System.Windows.Forms.ComboBox cmbDbForecasts;
        private System.Windows.Forms.ProgressBar progressBarTop;
        private System.Windows.Forms.TableLayoutPanel tableLayoutForecasts;

        // Grid 1
        private System.Windows.Forms.Panel pnlGrid1Wrapper;
        private System.Windows.Forms.Panel pnlHeader1;
        private System.Windows.Forms.DataGridView dgvForecast1;
        private System.Windows.Forms.Label lblForecast1;

        // Grid 2
        private System.Windows.Forms.Panel pnlGrid2Wrapper;
        private System.Windows.Forms.Panel pnlHeader2;
        private System.Windows.Forms.DataGridView dgvForecast2;
        private System.Windows.Forms.Label lblForecast2;

        // Order Controls
        private System.Windows.Forms.Panel pnlOrderHeader;
        private System.Windows.Forms.Button btnBrowseOrder;
        private System.Windows.Forms.ComboBox cmbDbOrders;
        private System.Windows.Forms.DataGridView dgvOrder;
        private System.Windows.Forms.Panel pnlOrderErrors;
        private System.Windows.Forms.DataGridView dgvOrderErrors;
        private System.Windows.Forms.Label lblOrderErrorHeader;
        private System.Windows.Forms.Button btnExportErrors;
    }
}