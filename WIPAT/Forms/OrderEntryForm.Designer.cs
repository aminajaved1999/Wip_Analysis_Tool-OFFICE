namespace WIPAT
{
    partial class OrderEntryForm
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
            this.pnlHeaderCard = new System.Windows.Forms.Panel();
            this.btnFillKill = new System.Windows.Forms.Button();
            this.lblHeaderTitle = new System.Windows.Forms.Label();
            this.lblDocNo = new System.Windows.Forms.Label();
            this.txtDocNo = new System.Windows.Forms.TextBox();
            this.btnPreview = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.progressBarTop = new System.Windows.Forms.ProgressBar();
            this.pnlGridCard = new System.Windows.Forms.Panel();
            this.splitGridContainer = new System.Windows.Forms.SplitContainer();
            this.dgvValid = new System.Windows.Forms.DataGridView();
            this.lblValidTitle = new System.Windows.Forms.Label();
            this.pnlInvalidWrapper = new System.Windows.Forms.Panel();
            this.dgvInvalid = new System.Windows.Forms.DataGridView();
            this.lblInvalidTitle = new System.Windows.Forms.Label();
            this.btnExportErrors = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.pnlHeaderCard.SuspendLayout();
            this.pnlGridCard.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitGridContainer)).BeginInit();
            this.splitGridContainer.Panel1.SuspendLayout();
            this.splitGridContainer.Panel2.SuspendLayout();
            this.splitGridContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvValid)).BeginInit();
            this.pnlInvalidWrapper.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvInvalid)).BeginInit();
            this.SuspendLayout();
            // 
            // pnlHeaderCard
            // 
            this.pnlHeaderCard.BackColor = System.Drawing.Color.White;
            this.pnlHeaderCard.Controls.Add(this.btnFillKill);
            this.pnlHeaderCard.Controls.Add(this.lblHeaderTitle);
            this.pnlHeaderCard.Controls.Add(this.lblDocNo);
            this.pnlHeaderCard.Controls.Add(this.txtDocNo);
            this.pnlHeaderCard.Controls.Add(this.btnPreview);
            this.pnlHeaderCard.Controls.Add(this.btnSave);
            this.pnlHeaderCard.Controls.Add(this.progressBarTop);
            this.pnlHeaderCard.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeaderCard.Location = new System.Drawing.Point(15, 15);
            this.pnlHeaderCard.Name = "pnlHeaderCard";
            this.pnlHeaderCard.Padding = new System.Windows.Forms.Padding(15, 15, 15, 0);
            this.pnlHeaderCard.Size = new System.Drawing.Size(954, 115);
            this.pnlHeaderCard.TabIndex = 0;
            // 
            // btnFillKill
            // 
            this.btnFillKill.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnFillKill.BackColor = System.Drawing.Color.Gray;
            this.btnFillKill.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnFillKill.Enabled = false;
            this.btnFillKill.FlatAppearance.BorderSize = 0;
            this.btnFillKill.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnFillKill.ForeColor = System.Drawing.Color.White;
            this.btnFillKill.Location = new System.Drawing.Point(828, 52);
            this.btnFillKill.Name = "btnFillKill";
            this.btnFillKill.Size = new System.Drawing.Size(110, 36);
            this.btnFillKill.TabIndex = 10;
            this.btnFillKill.Text = "3. Fill && Kill";
            this.btnFillKill.UseVisualStyleBackColor = false;
            this.btnFillKill.Click += new System.EventHandler(this.btnFillKill_Click);
            // 
            // lblHeaderTitle
            // 
            this.lblHeaderTitle.AutoSize = true;
            this.lblHeaderTitle.Font = new System.Drawing.Font("Segoe UI Semibold", 11.25F, System.Drawing.FontStyle.Bold);
            this.lblHeaderTitle.Location = new System.Drawing.Point(15, 10);
            this.lblHeaderTitle.Name = "lblHeaderTitle";
            this.lblHeaderTitle.Size = new System.Drawing.Size(125, 20);
            this.lblHeaderTitle.TabIndex = 11;
            this.lblHeaderTitle.Text = "New Order Entry";
            // 
            // lblDocNo
            // 
            this.lblDocNo.AutoSize = true;
            this.lblDocNo.ForeColor = System.Drawing.Color.DimGray;
            this.lblDocNo.Location = new System.Drawing.Point(16, 45);
            this.lblDocNo.Name = "lblDocNo";
            this.lblDocNo.Size = new System.Drawing.Size(56, 17);
            this.lblDocNo.TabIndex = 7;
            this.lblDocNo.Text = "Doc No:";
            // 
            // txtDocNo
            // 
            this.txtDocNo.BackColor = System.Drawing.Color.WhiteSmoke;
            this.txtDocNo.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtDocNo.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.txtDocNo.Location = new System.Drawing.Point(19, 64);
            this.txtDocNo.Name = "txtDocNo";
            this.txtDocNo.Size = new System.Drawing.Size(150, 24);
            this.txtDocNo.TabIndex = 0;
            // 
            // btnPreview
            // 
            this.btnPreview.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnPreview.BackColor = System.Drawing.Color.DodgerBlue;
            this.btnPreview.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPreview.FlatAppearance.BorderSize = 0;
            this.btnPreview.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPreview.ForeColor = System.Drawing.Color.White;
            this.btnPreview.Location = new System.Drawing.Point(596, 52);
            this.btnPreview.Name = "btnPreview";
            this.btnPreview.Size = new System.Drawing.Size(110, 36);
            this.btnPreview.TabIndex = 8;
            this.btnPreview.Text = "1. Preview";
            this.btnPreview.UseVisualStyleBackColor = false;
            this.btnPreview.Click += new System.EventHandler(this.btnPreview_Click);
            // 
            // btnSave
            // 
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.BackColor = System.Drawing.Color.Gray;
            this.btnSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSave.FlatAppearance.BorderSize = 0;
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(712, 52);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(110, 36);
            this.btnSave.TabIndex = 9;
            this.btnSave.Text = "2. Save";
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // progressBarTop
            // 
            this.progressBarTop.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.progressBarTop.Location = new System.Drawing.Point(15, 112);
            this.progressBarTop.Name = "progressBarTop";
            this.progressBarTop.Size = new System.Drawing.Size(924, 3);
            this.progressBarTop.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBarTop.TabIndex = 12;
            this.progressBarTop.Visible = false;
            // 
            // pnlGridCard
            // 
            this.pnlGridCard.BackColor = System.Drawing.Color.White;
            this.pnlGridCard.Controls.Add(this.splitGridContainer);
            this.pnlGridCard.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlGridCard.Location = new System.Drawing.Point(15, 130);
            this.pnlGridCard.Name = "pnlGridCard";
            this.pnlGridCard.Padding = new System.Windows.Forms.Padding(1);
            this.pnlGridCard.Size = new System.Drawing.Size(954, 446);
            this.pnlGridCard.TabIndex = 1;
            // 
            // splitGridContainer
            // 
            this.splitGridContainer.BackColor = System.Drawing.Color.WhiteSmoke;
            this.splitGridContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitGridContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitGridContainer.Location = new System.Drawing.Point(1, 1);
            this.splitGridContainer.Name = "splitGridContainer";
            // 
            // splitGridContainer.Panel1
            // 
            this.splitGridContainer.Panel1.BackColor = System.Drawing.Color.White;
            this.splitGridContainer.Panel1.Controls.Add(this.dgvValid);
            this.splitGridContainer.Panel1.Controls.Add(this.lblValidTitle);
            // 
            // splitGridContainer.Panel2
            // 
            this.splitGridContainer.Panel2.BackColor = System.Drawing.Color.White;
            this.splitGridContainer.Panel2.Controls.Add(this.pnlInvalidWrapper);
            this.splitGridContainer.Panel2Collapsed = true;
            this.splitGridContainer.Size = new System.Drawing.Size(952, 444);
            this.splitGridContainer.SplitterDistance = 650;
            this.splitGridContainer.SplitterWidth = 5;
            this.splitGridContainer.TabIndex = 0;
            // 
            // dgvValid
            // 
            this.dgvValid.AllowUserToAddRows = false;
            this.dgvValid.AllowUserToDeleteRows = false;
            this.dgvValid.BackgroundColor = System.Drawing.Color.White;
            this.dgvValid.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvValid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvValid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvValid.Location = new System.Drawing.Point(0, 25);
            this.dgvValid.Name = "dgvValid";
            this.dgvValid.ReadOnly = true;
            this.dgvValid.RowHeadersVisible = false;
            this.dgvValid.Size = new System.Drawing.Size(952, 419);
            this.dgvValid.TabIndex = 1;
            // 
            // lblValidTitle
            // 
            this.lblValidTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblValidTitle.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblValidTitle.ForeColor = System.Drawing.Color.DarkGreen;
            this.lblValidTitle.Location = new System.Drawing.Point(0, 0);
            this.lblValidTitle.Name = "lblValidTitle";
            this.lblValidTitle.Padding = new System.Windows.Forms.Padding(5);
            this.lblValidTitle.Size = new System.Drawing.Size(952, 25);
            this.lblValidTitle.TabIndex = 0;
            this.lblValidTitle.Text = "Valid Items";
            this.lblValidTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pnlInvalidWrapper
            // 
            this.pnlInvalidWrapper.Controls.Add(this.dgvInvalid);
            this.pnlInvalidWrapper.Controls.Add(this.lblInvalidTitle);
            this.pnlInvalidWrapper.Controls.Add(this.btnExportErrors);
            this.pnlInvalidWrapper.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlInvalidWrapper.Location = new System.Drawing.Point(0, 0);
            this.pnlInvalidWrapper.Name = "pnlInvalidWrapper";
            this.pnlInvalidWrapper.Size = new System.Drawing.Size(96, 100);
            this.pnlInvalidWrapper.TabIndex = 0;
            // 
            // dgvInvalid
            // 
            this.dgvInvalid.AllowUserToAddRows = false;
            this.dgvInvalid.AllowUserToDeleteRows = false;
            this.dgvInvalid.BackgroundColor = System.Drawing.Color.White;
            this.dgvInvalid.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvInvalid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvInvalid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvInvalid.Location = new System.Drawing.Point(0, 30);
            this.dgvInvalid.Name = "dgvInvalid";
            this.dgvInvalid.ReadOnly = true;
            this.dgvInvalid.RowHeadersVisible = false;
            this.dgvInvalid.Size = new System.Drawing.Size(96, 30);
            this.dgvInvalid.TabIndex = 2;
            // 
            // lblInvalidTitle
            // 
            this.lblInvalidTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblInvalidTitle.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblInvalidTitle.ForeColor = System.Drawing.Color.Crimson;
            this.lblInvalidTitle.Location = new System.Drawing.Point(0, 0);
            this.lblInvalidTitle.Name = "lblInvalidTitle";
            this.lblInvalidTitle.Padding = new System.Windows.Forms.Padding(5);
            this.lblInvalidTitle.Size = new System.Drawing.Size(96, 30);
            this.lblInvalidTitle.TabIndex = 1;
            this.lblInvalidTitle.Text = "Invalid Items";
            this.lblInvalidTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnExportErrors
            // 
            this.btnExportErrors.BackColor = System.Drawing.Color.ForestGreen;
            this.btnExportErrors.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.btnExportErrors.FlatAppearance.BorderSize = 0;
            this.btnExportErrors.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExportErrors.ForeColor = System.Drawing.Color.White;
            this.btnExportErrors.Location = new System.Drawing.Point(0, 60);
            this.btnExportErrors.Name = "btnExportErrors";
            this.btnExportErrors.Size = new System.Drawing.Size(96, 40);
            this.btnExportErrors.TabIndex = 3;
            this.btnExportErrors.Text = "Export Errors";
            this.btnExportErrors.UseVisualStyleBackColor = false;
            this.btnExportErrors.Click += new System.EventHandler(this.btnExportErrors_Click);
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblStatus.ForeColor = System.Drawing.Color.DimGray;
            this.lblStatus.Location = new System.Drawing.Point(15, 576);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Padding = new System.Windows.Forms.Padding(0, 10, 0, 10);
            this.lblStatus.Size = new System.Drawing.Size(45, 37);
            this.lblStatus.TabIndex = 2;
            this.lblStatus.Text = "Ready";
            // 
            // OrderEntryForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ClientSize = new System.Drawing.Size(984, 616);
            this.Controls.Add(this.pnlGridCard);
            this.Controls.Add(this.pnlHeaderCard);
            this.Controls.Add(this.lblStatus);
            this.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "OrderEntryForm";
            this.Padding = new System.Windows.Forms.Padding(15, 15, 15, 3);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "New Order Entry";
            this.Load += new System.EventHandler(this.OrderEntryForm_Load);
            this.pnlHeaderCard.ResumeLayout(false);
            this.pnlHeaderCard.PerformLayout();
            this.pnlGridCard.ResumeLayout(false);
            this.splitGridContainer.Panel1.ResumeLayout(false);
            this.splitGridContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitGridContainer)).EndInit();
            this.splitGridContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvValid)).EndInit();
            this.pnlInvalidWrapper.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvInvalid)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel pnlHeaderCard;
        private System.Windows.Forms.Label lblHeaderTitle;
        private System.Windows.Forms.Label lblDocNo;
        private System.Windows.Forms.TextBox txtDocNo;
        private System.Windows.Forms.Button btnPreview;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnFillKill;
        private System.Windows.Forms.ProgressBar progressBarTop;
        private System.Windows.Forms.Panel pnlGridCard;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.SplitContainer splitGridContainer;
        private System.Windows.Forms.DataGridView dgvValid;
        private System.Windows.Forms.Label lblValidTitle;
        private System.Windows.Forms.Panel pnlInvalidWrapper;
        private System.Windows.Forms.DataGridView dgvInvalid;
        private System.Windows.Forms.Label lblInvalidTitle;
        private System.Windows.Forms.Button btnExportErrors;
    }
}