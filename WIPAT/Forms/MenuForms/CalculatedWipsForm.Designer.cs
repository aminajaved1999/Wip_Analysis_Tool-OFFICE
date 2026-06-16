namespace WIPAT
{
    partial class CalculatedWipsForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.headerPanel = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.toolbarPanel = new System.Windows.Forms.Panel();
            this.pnlSearch = new System.Windows.Forms.Panel();
            this.btnSearch = new System.Windows.Forms.Button();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.lblSearch = new System.Windows.Forms.Label();
            this.btnCommitToDb = new System.Windows.Forms.Button();
            this.btnPreviewExcelUpdate = new System.Windows.Forms.Button();
            this.btnExport = new System.Windows.Forms.Button();
            this.btnLoadDetails = new System.Windows.Forms.Button();
            this.pnlFilter = new System.Windows.Forms.Panel();
            this.cmbPeriods = new System.Windows.Forms.ComboBox();
            this.lblPeriod = new System.Windows.Forms.Label();
            this.lblModeIndicator = new System.Windows.Forms.Label();
            this.dgvWipDetails = new System.Windows.Forms.DataGridView();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.pnlStatusBar = new System.Windows.Forms.Panel();
            this.lblTotalItems = new System.Windows.Forms.Label();
            this.lblActiveItems = new System.Windows.Forms.Label();
            this.lblInactiveItems = new System.Windows.Forms.Label();
            this.lblInvalidItems = new System.Windows.Forms.Label();
            this.headerPanel.SuspendLayout();
            this.toolbarPanel.SuspendLayout();
            this.pnlSearch.SuspendLayout();
            this.pnlFilter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvWipDetails)).BeginInit();
            this.statusStrip.SuspendLayout();
            this.pnlStatusBar.SuspendLayout();
            this.SuspendLayout();
            // 
            // headerPanel
            // 
            this.headerPanel.Controls.Add(this.lblTitle);
            this.headerPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.headerPanel.Location = new System.Drawing.Point(0, 0);
            this.headerPanel.Name = "headerPanel";
            this.headerPanel.Size = new System.Drawing.Size(1200, 60);
            this.headerPanel.TabIndex = 0;
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Location = new System.Drawing.Point(12, 15);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(280, 30);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Calculated WIPs Manager";
            // 
            // toolbarPanel
            // 
            this.toolbarPanel.Controls.Add(this.pnlSearch);
            this.toolbarPanel.Controls.Add(this.btnCommitToDb);
            this.toolbarPanel.Controls.Add(this.btnPreviewExcelUpdate);
            this.toolbarPanel.Controls.Add(this.btnExport);
            this.toolbarPanel.Controls.Add(this.btnLoadDetails);
            this.toolbarPanel.Controls.Add(this.pnlFilter);
            this.toolbarPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.toolbarPanel.Location = new System.Drawing.Point(0, 60);
            this.toolbarPanel.Name = "toolbarPanel";
            this.toolbarPanel.Size = new System.Drawing.Size(1200, 55);
            this.toolbarPanel.TabIndex = 1;
            // 
            // pnlSearch
            // 
            this.pnlSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlSearch.Controls.Add(this.btnSearch);
            this.pnlSearch.Controls.Add(this.txtSearch);
            this.pnlSearch.Controls.Add(this.lblSearch);
            this.pnlSearch.Location = new System.Drawing.Point(835, 10);
            this.pnlSearch.Name = "pnlSearch";
            this.pnlSearch.Size = new System.Drawing.Size(350, 35);
            this.pnlSearch.TabIndex = 8;
            // 
            // btnSearch
            // 
            this.btnSearch.Location = new System.Drawing.Point(265, 3);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(75, 29);
            this.btnSearch.TabIndex = 2;
            this.btnSearch.Text = "Search";
            this.btnSearch.Click += new System.EventHandler(this.BtnSearch_Click);
            // 
            // txtSearch
            // 
            this.txtSearch.Location = new System.Drawing.Point(60, 6);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(199, 23);
            this.txtSearch.TabIndex = 1;
            this.txtSearch.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TxtSearch_KeyDown);
            // 
            // lblSearch
            // 
            this.lblSearch.AutoSize = true;
            this.lblSearch.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblSearch.Location = new System.Drawing.Point(5, 10);
            this.lblSearch.Name = "lblSearch";
            this.lblSearch.Size = new System.Drawing.Size(45, 15);
            this.lblSearch.TabIndex = 0;
            this.lblSearch.Text = "CASIN:";
            // 
            // btnCommitToDb
            // 
            this.btnCommitToDb.Location = new System.Drawing.Point(650, 10);
            this.btnCommitToDb.Name = "btnCommitToDb";
            this.btnCommitToDb.Size = new System.Drawing.Size(140, 35);
            this.btnCommitToDb.TabIndex = 7;
            this.btnCommitToDb.Text = "Review Changes";
            this.btnCommitToDb.Click += new System.EventHandler(this.BtnCommitToDb_Click);
            // 
            // btnPreviewExcelUpdate
            // 
            this.btnPreviewExcelUpdate.Location = new System.Drawing.Point(500, 10);
            this.btnPreviewExcelUpdate.Name = "btnPreviewExcelUpdate";
            this.btnPreviewExcelUpdate.Size = new System.Drawing.Size(140, 35);
            this.btnPreviewExcelUpdate.TabIndex = 6;
            this.btnPreviewExcelUpdate.Text = "Import Updates";
            this.btnPreviewExcelUpdate.Click += new System.EventHandler(this.BtnPreviewExcelUpdate_Click);
            // 
            // btnExport
            // 
            this.btnExport.Location = new System.Drawing.Point(360, 10);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(130, 35);
            this.btnExport.TabIndex = 4;
            this.btnExport.Text = "Export to Excel";
            this.btnExport.Click += new System.EventHandler(this.BtnExport_Click);
            // 
            // btnLoadDetails
            // 
            this.btnLoadDetails.Location = new System.Drawing.Point(230, 10);
            this.btnLoadDetails.Name = "btnLoadDetails";
            this.btnLoadDetails.Size = new System.Drawing.Size(120, 35);
            this.btnLoadDetails.TabIndex = 0;
            this.btnLoadDetails.Text = "View WIPs";
            this.btnLoadDetails.Click += new System.EventHandler(this.BtnLoadDetails_Click);
            // 
            // pnlFilter
            // 
            this.pnlFilter.Controls.Add(this.cmbPeriods);
            this.pnlFilter.Controls.Add(this.lblPeriod);
            this.pnlFilter.Location = new System.Drawing.Point(10, 10);
            this.pnlFilter.Name = "pnlFilter";
            this.pnlFilter.Size = new System.Drawing.Size(210, 35);
            this.pnlFilter.TabIndex = 5;
            // 
            // cmbPeriods
            // 
            this.cmbPeriods.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPeriods.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbPeriods.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.cmbPeriods.FormattingEnabled = true;
            this.cmbPeriods.Location = new System.Drawing.Point(60, 5);
            this.cmbPeriods.Name = "cmbPeriods";
            this.cmbPeriods.Size = new System.Drawing.Size(140, 25);
            this.cmbPeriods.TabIndex = 1;
            this.cmbPeriods.SelectedIndexChanged += new System.EventHandler(this.CmbPeriods_SelectedIndexChanged);
            // 
            // lblPeriod
            // 
            this.lblPeriod.AutoSize = true;
            this.lblPeriod.Location = new System.Drawing.Point(5, 8);
            this.lblPeriod.Name = "lblPeriod";
            this.lblPeriod.Size = new System.Drawing.Size(44, 15);
            this.lblPeriod.TabIndex = 0;
            this.lblPeriod.Text = "Period:";
            // 
            // lblModeIndicator
            // 
            this.lblModeIndicator.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblModeIndicator.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblModeIndicator.Location = new System.Drawing.Point(0, 115);
            this.lblModeIndicator.Name = "lblModeIndicator";
            this.lblModeIndicator.Size = new System.Drawing.Size(1200, 30);
            this.lblModeIndicator.TabIndex = 4;
            this.lblModeIndicator.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblModeIndicator.Visible = false;
            // 
            // dgvWipDetails
            // 
            this.dgvWipDetails.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvWipDetails.Location = new System.Drawing.Point(0, 145);
            this.dgvWipDetails.Name = "dgvWipDetails";
            this.dgvWipDetails.Size = new System.Drawing.Size(1200, 433);
            this.dgvWipDetails.TabIndex = 2;
            this.dgvWipDetails.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.DataGridView_ItemStatus_CellFormatting);
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusLabel});
            this.statusStrip.Location = new System.Drawing.Point(0, 578);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(1200, 22);
            this.statusStrip.TabIndex = 3;
            // 
            // statusLabel
            // 
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(39, 17);
            this.statusLabel.Text = "Ready";
            // 
            // pnlStatusBar
            // 
            this.pnlStatusBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(246)))), ((int)(((byte)(250)))));
            this.pnlStatusBar.Controls.Add(this.lblTotalItems);
            this.pnlStatusBar.Controls.Add(this.lblActiveItems);
            this.pnlStatusBar.Controls.Add(this.lblInactiveItems);
            this.pnlStatusBar.Controls.Add(this.lblInvalidItems);
            this.pnlStatusBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlStatusBar.Location = new System.Drawing.Point(0, 538);
            this.pnlStatusBar.Name = "pnlStatusBar";
            this.pnlStatusBar.Size = new System.Drawing.Size(1200, 40);
            this.pnlStatusBar.TabIndex = 6;
            // 
            // lblTotalItems
            // 
            this.lblTotalItems.AutoSize = true;
            this.lblTotalItems.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTotalItems.ForeColor = System.Drawing.Color.Black;
            this.lblTotalItems.Location = new System.Drawing.Point(20, 12);
            this.lblTotalItems.Name = "lblTotalItems";
            this.lblTotalItems.Size = new System.Drawing.Size(92, 17);
            this.lblTotalItems.TabIndex = 0;
            this.lblTotalItems.Text = "Total Items: 0";
            // 
            // lblActiveItems
            // 
            this.lblActiveItems.AutoSize = true;
            this.lblActiveItems.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblActiveItems.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(125)))), ((int)(((byte)(50)))));
            this.lblActiveItems.Location = new System.Drawing.Point(150, 12);
            this.lblActiveItems.Name = "lblActiveItems";
            this.lblActiveItems.Size = new System.Drawing.Size(61, 17);
            this.lblActiveItems.TabIndex = 1;
            this.lblActiveItems.Text = "Active: 0";
            // 
            // lblInactiveItems
            // 
            this.lblInactiveItems.AutoSize = true;
            this.lblInactiveItems.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblInactiveItems.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.lblInactiveItems.Location = new System.Drawing.Point(260, 12);
            this.lblInactiveItems.Name = "lblInactiveItems";
            this.lblInactiveItems.Size = new System.Drawing.Size(96, 17);
            this.lblInactiveItems.TabIndex = 2;
            this.lblInactiveItems.Text = "Deactivated: 0";
            // 
            // lblInvalidItems
            // 
            this.lblInvalidItems.AutoSize = true;
            this.lblInvalidItems.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblInvalidItems.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(53)))), ((int)(((byte)(69)))));
            this.lblInvalidItems.Location = new System.Drawing.Point(380, 12);
            this.lblInvalidItems.Name = "lblInvalidItems";
            this.lblInvalidItems.Size = new System.Drawing.Size(65, 17);
            this.lblInvalidItems.TabIndex = 3;
            this.lblInvalidItems.Text = "Invalid: 0";
            // 
            // CalculatedWipsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 600);
            this.Controls.Add(this.pnlStatusBar);
            this.Controls.Add(this.dgvWipDetails);
            this.Controls.Add(this.lblModeIndicator);
            this.Controls.Add(this.toolbarPanel);
            this.Controls.Add(this.headerPanel);
            this.Controls.Add(this.statusStrip);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.Name = "CalculatedWipsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Calculated WIPs Manager";
            this.Load += new System.EventHandler(this.CalculatedWipsForm_Load);
            this.headerPanel.ResumeLayout(false);
            this.headerPanel.PerformLayout();
            this.toolbarPanel.ResumeLayout(false);
            this.pnlSearch.ResumeLayout(false);
            this.pnlSearch.PerformLayout();
            this.pnlFilter.ResumeLayout(false);
            this.pnlFilter.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvWipDetails)).EndInit();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.pnlStatusBar.ResumeLayout(false);
            this.pnlStatusBar.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel headerPanel;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Panel toolbarPanel;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.Button btnPreviewExcelUpdate;
        private System.Windows.Forms.Button btnCommitToDb;
        private System.Windows.Forms.Button btnLoadDetails;
        private System.Windows.Forms.Panel pnlFilter;
        private System.Windows.Forms.ComboBox cmbPeriods;
        private System.Windows.Forms.Label lblPeriod;
        private System.Windows.Forms.Label lblModeIndicator;
        private System.Windows.Forms.DataGridView dgvWipDetails;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel;
        private System.Windows.Forms.Panel pnlSearch;
        private System.Windows.Forms.Button btnSearch;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Label lblSearch;
        private System.Windows.Forms.Panel pnlStatusBar;
        private System.Windows.Forms.Label lblTotalItems;
        private System.Windows.Forms.Label lblActiveItems;
        private System.Windows.Forms.Label lblInactiveItems;
        private System.Windows.Forms.Label lblInvalidItems;
    }
}