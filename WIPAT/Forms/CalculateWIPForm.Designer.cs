namespace WIPAT
{
    partial class CalculateWIPForm
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
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.lblTitle = new System.Windows.Forms.Label();
            this.pnlWipTypeCard = new System.Windows.Forms.Panel();
            this.lblPercentSymbol = new System.Windows.Forms.Label();
            this.textBoxPercentage = new System.Windows.Forms.TextBox();
            this.radioButtonSystem = new System.Windows.Forms.RadioButton();
            this.radioButtonPercentage = new System.Windows.Forms.RadioButton();
            this.radioButtonMonthOfSupply = new System.Windows.Forms.RadioButton();
            this.lblWipTypeHeader = new System.Windows.Forms.Label();
            this.pnlOptionsCard = new System.Windows.Forms.Panel();
            this.checkBoxCasePack = new System.Windows.Forms.CheckBox();
            this.textBoxMOQ = new System.Windows.Forms.TextBox();
            this.checkBoxMOQ = new System.Windows.Forms.CheckBox();
            this.lblOptionsHeader = new System.Windows.Forms.Label();
            this.btnReviewWIP = new System.Windows.Forms.Button();
            this.btnApproveWIP = new System.Windows.Forms.Button();
            this.pnlDivider = new System.Windows.Forms.Panel();
            this.radioButtonNewWorking = new System.Windows.Forms.RadioButton();
            this.pnlHeader.SuspendLayout();
            this.pnlWipTypeCard.SuspendLayout();
            this.pnlOptionsCard.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlHeader
            // 
            this.pnlHeader.BackColor = System.Drawing.Color.White;
            this.pnlHeader.Controls.Add(this.progressBar1);
            this.pnlHeader.Controls.Add(this.lblTitle);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Size = new System.Drawing.Size(400, 60);
            this.pnlHeader.TabIndex = 0;
            // 
            // progressBar1
            // 
            this.progressBar1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.progressBar1.Location = new System.Drawing.Point(0, 55);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(400, 5);
            this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar1.TabIndex = 1;
            this.progressBar1.Visible = false;
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI Semibold", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lblTitle.Location = new System.Drawing.Point(20, 18);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(182, 25);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Calculate && Set WIP";
            // 
            // pnlWipTypeCard
            // 
            this.pnlWipTypeCard.BackColor = System.Drawing.Color.White;
            this.pnlWipTypeCard.Controls.Add(this.radioButtonNewWorking);
            this.pnlWipTypeCard.Controls.Add(this.lblPercentSymbol);
            this.pnlWipTypeCard.Controls.Add(this.textBoxPercentage);
            this.pnlWipTypeCard.Controls.Add(this.radioButtonSystem);
            this.pnlWipTypeCard.Controls.Add(this.radioButtonPercentage);
            this.pnlWipTypeCard.Controls.Add(this.radioButtonMonthOfSupply);
            this.pnlWipTypeCard.Controls.Add(this.lblWipTypeHeader);
            this.pnlWipTypeCard.Location = new System.Drawing.Point(25, 80);
            this.pnlWipTypeCard.Name = "pnlWipTypeCard";
            this.pnlWipTypeCard.Size = new System.Drawing.Size(350, 197);
            this.pnlWipTypeCard.TabIndex = 1;
            // 
            // lblPercentSymbol
            // 
            this.lblPercentSymbol.AutoSize = true;
            this.lblPercentSymbol.ForeColor = System.Drawing.Color.Gray;
            this.lblPercentSymbol.Location = new System.Drawing.Point(190, 88);
            this.lblPercentSymbol.Name = "lblPercentSymbol";
            this.lblPercentSymbol.Size = new System.Drawing.Size(19, 17);
            this.lblPercentSymbol.TabIndex = 4;
            this.lblPercentSymbol.Text = "%";
            this.lblPercentSymbol.Visible = false;
            // 
            // textBoxPercentage
            // 
            this.textBoxPercentage.BackColor = System.Drawing.Color.WhiteSmoke;
            this.textBoxPercentage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxPercentage.Enabled = false;
            this.textBoxPercentage.Location = new System.Drawing.Point(125, 84);
            this.textBoxPercentage.Name = "textBoxPercentage";
            this.textBoxPercentage.Size = new System.Drawing.Size(60, 25);
            this.textBoxPercentage.TabIndex = 3;
            this.textBoxPercentage.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.textBoxPercentage.Visible = false;
            this.textBoxPercentage.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxPercentage_Validating);
            // 
            // radioButtonSystem
            // 
            this.radioButtonSystem.AutoSize = true;
            this.radioButtonSystem.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.radioButtonSystem.Location = new System.Drawing.Point(25, 120);
            this.radioButtonSystem.Name = "radioButtonSystem";
            this.radioButtonSystem.Size = new System.Drawing.Size(209, 23);
            this.radioButtonSystem.TabIndex = 5;
            this.radioButtonSystem.TabStop = true;
            this.radioButtonSystem.Text = "System (Standard Calculation)";
            this.radioButtonSystem.UseVisualStyleBackColor = true;
            // 
            // radioButtonPercentage
            // 
            this.radioButtonPercentage.AutoSize = true;
            this.radioButtonPercentage.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.radioButtonPercentage.Location = new System.Drawing.Point(25, 85);
            this.radioButtonPercentage.Name = "radioButtonPercentage";
            this.radioButtonPercentage.Size = new System.Drawing.Size(94, 23);
            this.radioButtonPercentage.TabIndex = 2;
            this.radioButtonPercentage.TabStop = true;
            this.radioButtonPercentage.Text = "Percentage";
            this.radioButtonPercentage.UseVisualStyleBackColor = true;
            this.radioButtonPercentage.CheckedChanged += new System.EventHandler(this.radioButtonPercentage_CheckedChanged);
            // 
            // radioButtonMonthOfSupply
            // 
            this.radioButtonMonthOfSupply.AutoSize = true;
            this.radioButtonMonthOfSupply.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.radioButtonMonthOfSupply.Location = new System.Drawing.Point(25, 50);
            this.radioButtonMonthOfSupply.Name = "radioButtonMonthOfSupply";
            this.radioButtonMonthOfSupply.Size = new System.Drawing.Size(130, 23);
            this.radioButtonMonthOfSupply.TabIndex = 1;
            this.radioButtonMonthOfSupply.TabStop = true;
            this.radioButtonMonthOfSupply.Text = "Month of Supply";
            this.radioButtonMonthOfSupply.UseVisualStyleBackColor = true;
            // 
            // lblWipTypeHeader
            // 
            this.lblWipTypeHeader.AutoSize = true;
            this.lblWipTypeHeader.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblWipTypeHeader.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.lblWipTypeHeader.Location = new System.Drawing.Point(15, 15);
            this.lblWipTypeHeader.Name = "lblWipTypeHeader";
            this.lblWipTypeHeader.Size = new System.Drawing.Size(127, 20);
            this.lblWipTypeHeader.TabIndex = 0;
            this.lblWipTypeHeader.Text = "Calculation Logic";
            // 
            // pnlOptionsCard
            // 
            this.pnlOptionsCard.BackColor = System.Drawing.Color.White;
            this.pnlOptionsCard.Controls.Add(this.checkBoxCasePack);
            this.pnlOptionsCard.Controls.Add(this.textBoxMOQ);
            this.pnlOptionsCard.Controls.Add(this.checkBoxMOQ);
            this.pnlOptionsCard.Controls.Add(this.lblOptionsHeader);
            this.pnlOptionsCard.Location = new System.Drawing.Point(25, 303);
            this.pnlOptionsCard.Name = "pnlOptionsCard";
            this.pnlOptionsCard.Size = new System.Drawing.Size(350, 120);
            this.pnlOptionsCard.TabIndex = 2;
            // 
            // checkBoxCasePack
            // 
            this.checkBoxCasePack.AutoSize = true;
            this.checkBoxCasePack.Location = new System.Drawing.Point(25, 85);
            this.checkBoxCasePack.Name = "checkBoxCasePack";
            this.checkBoxCasePack.Size = new System.Drawing.Size(170, 21);
            this.checkBoxCasePack.TabIndex = 3;
            this.checkBoxCasePack.Text = "Round to Case Pack Size";
            this.checkBoxCasePack.UseVisualStyleBackColor = true;
            // 
            // textBoxMOQ
            // 
            this.textBoxMOQ.BackColor = System.Drawing.Color.WhiteSmoke;
            this.textBoxMOQ.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxMOQ.Enabled = false;
            this.textBoxMOQ.Location = new System.Drawing.Point(125, 48);
            this.textBoxMOQ.Name = "textBoxMOQ";
            this.textBoxMOQ.Size = new System.Drawing.Size(100, 25);
            this.textBoxMOQ.TabIndex = 2;
            this.textBoxMOQ.Visible = false;
            this.textBoxMOQ.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxMOQ_Validating);
            // 
            // checkBoxMOQ
            // 
            this.checkBoxMOQ.AutoSize = true;
            this.checkBoxMOQ.Location = new System.Drawing.Point(25, 50);
            this.checkBoxMOQ.Name = "checkBoxMOQ";
            this.checkBoxMOQ.Size = new System.Drawing.Size(96, 21);
            this.checkBoxMOQ.TabIndex = 1;
            this.checkBoxMOQ.Text = "Apply MOQ";
            this.checkBoxMOQ.UseVisualStyleBackColor = true;
            this.checkBoxMOQ.CheckedChanged += new System.EventHandler(this.checkBoxMOQ_CheckedChanged);
            // 
            // lblOptionsHeader
            // 
            this.lblOptionsHeader.AutoSize = true;
            this.lblOptionsHeader.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Bold);
            this.lblOptionsHeader.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.lblOptionsHeader.Location = new System.Drawing.Point(15, 15);
            this.lblOptionsHeader.Name = "lblOptionsHeader";
            this.lblOptionsHeader.Size = new System.Drawing.Size(89, 20);
            this.lblOptionsHeader.TabIndex = 0;
            this.lblOptionsHeader.Text = "Constraints";
            // 
            // btnReviewWIP
            // 
            this.btnReviewWIP.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(25)))), ((int)(((byte)(118)))), ((int)(((byte)(210)))));
            this.btnReviewWIP.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnReviewWIP.FlatAppearance.BorderSize = 0;
            this.btnReviewWIP.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReviewWIP.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.btnReviewWIP.ForeColor = System.Drawing.Color.White;
            this.btnReviewWIP.Location = new System.Drawing.Point(25, 453);
            this.btnReviewWIP.Name = "btnReviewWIP";
            this.btnReviewWIP.Size = new System.Drawing.Size(165, 45);
            this.btnReviewWIP.TabIndex = 3;
            this.btnReviewWIP.Text = "Review WIP";
            this.btnReviewWIP.UseVisualStyleBackColor = false;
            this.btnReviewWIP.Click += new System.EventHandler(this.btnReviewWIP_Click);
            // 
            // btnApproveWIP
            // 
            this.btnApproveWIP.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(125)))), ((int)(((byte)(50)))));
            this.btnApproveWIP.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnApproveWIP.Enabled = false;
            this.btnApproveWIP.FlatAppearance.BorderSize = 0;
            this.btnApproveWIP.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnApproveWIP.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.btnApproveWIP.ForeColor = System.Drawing.Color.White;
            this.btnApproveWIP.Location = new System.Drawing.Point(210, 453);
            this.btnApproveWIP.Name = "btnApproveWIP";
            this.btnApproveWIP.Size = new System.Drawing.Size(165, 45);
            this.btnApproveWIP.TabIndex = 4;
            this.btnApproveWIP.Text = "Approve && Save";
            this.btnApproveWIP.UseVisualStyleBackColor = false;
            this.btnApproveWIP.Click += new System.EventHandler(this.btnApproveWIP_Click);
            // 
            // pnlDivider
            // 
            this.pnlDivider.Location = new System.Drawing.Point(0, 0);
            this.pnlDivider.Name = "pnlDivider";
            this.pnlDivider.Size = new System.Drawing.Size(200, 100);
            this.pnlDivider.TabIndex = 0;
            // 
            // radioButtonNewWorking
            // 
            this.radioButtonNewWorking.AutoSize = true;
            this.radioButtonNewWorking.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.radioButtonNewWorking.Location = new System.Drawing.Point(25, 159);
            this.radioButtonNewWorking.Name = "radioButtonNewWorking";
            this.radioButtonNewWorking.Size = new System.Drawing.Size(141, 23);
            this.radioButtonNewWorking.TabIndex = 6;
            this.radioButtonNewWorking.TabStop = true;
            this.radioButtonNewWorking.Text = "New Wip Working ";
            this.radioButtonNewWorking.UseVisualStyleBackColor = true;
            // 
            // CalculateWIPForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(244)))), ((int)(((byte)(249)))));
            this.ClientSize = new System.Drawing.Size(400, 520);
            this.Controls.Add(this.btnApproveWIP);
            this.Controls.Add(this.btnReviewWIP);
            this.Controls.Add(this.pnlOptionsCard);
            this.Controls.Add(this.pnlWipTypeCard);
            this.Controls.Add(this.pnlHeader);
            this.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "CalculateWIPForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "WIP Processor";
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.pnlWipTypeCard.ResumeLayout(false);
            this.pnlWipTypeCard.PerformLayout();
            this.pnlOptionsCard.ResumeLayout(false);
            this.pnlOptionsCard.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.ProgressBar progressBar1;
        
        private System.Windows.Forms.Panel pnlWipTypeCard;
        private System.Windows.Forms.Label lblWipTypeHeader;
        private System.Windows.Forms.RadioButton radioButtonMonthOfSupply;
        private System.Windows.Forms.RadioButton radioButtonPercentage;
        private System.Windows.Forms.TextBox textBoxPercentage;
        private System.Windows.Forms.Label lblPercentSymbol;
        private System.Windows.Forms.RadioButton radioButtonSystem;
        
        private System.Windows.Forms.Panel pnlOptionsCard;
        private System.Windows.Forms.Label lblOptionsHeader;
        private System.Windows.Forms.CheckBox checkBoxMOQ;
        private System.Windows.Forms.TextBox textBoxMOQ;
        private System.Windows.Forms.CheckBox checkBoxCasePack;
        
        private System.Windows.Forms.Button btnReviewWIP;
        private System.Windows.Forms.Button btnApproveWIP;
        private System.Windows.Forms.Panel pnlDivider;
        private System.Windows.Forms.RadioButton radioButtonNewWorking;
    }
}