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
            this.radioButtonNewWorking = new System.Windows.Forms.RadioButton();
            this.radioButtonSystem = new System.Windows.Forms.RadioButton();
            this.radioButtonPercentage = new System.Windows.Forms.RadioButton();
            this.radioButtonMonthOfSupply = new System.Windows.Forms.RadioButton();
            this.lblWipTypeHeader = new System.Windows.Forms.Label();
            this.pnlOptionsCard = new System.Windows.Forms.Panel();
            this.textBoxMOQ = new System.Windows.Forms.TextBox();
            this.checkBoxCasePack = new System.Windows.Forms.CheckBox();
            this.checkBoxMOQ = new System.Windows.Forms.CheckBox();
            this.lblOptionsHeader = new System.Windows.Forms.Label();
            this.btnReviewWIP = new System.Windows.Forms.Button();
            this.btnApproveWIP = new System.Windows.Forms.Button();
            this.pnlHeader.SuspendLayout();
            this.pnlWipTypeCard.SuspendLayout();
            this.pnlOptionsCard.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlHeader
            // 
            this.pnlHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(64)))));
            this.pnlHeader.Controls.Add(this.progressBar1);
            this.pnlHeader.Controls.Add(this.lblTitle);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Size = new System.Drawing.Size(420, 60);
            this.pnlHeader.TabIndex = 0;
            // 
            // progressBar1
            // 
            this.progressBar1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.progressBar1.Location = new System.Drawing.Point(0, 56);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(420, 4);
            this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar1.TabIndex = 5;
            this.progressBar1.Visible = false;
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(15, 15);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(221, 30);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Calculate && Set WIP";
            // 
            // pnlWipTypeCard
            // 
            this.pnlWipTypeCard.BackColor = System.Drawing.Color.White;
            this.pnlWipTypeCard.Controls.Add(this.lblPercentSymbol);
            this.pnlWipTypeCard.Controls.Add(this.textBoxPercentage);
            this.pnlWipTypeCard.Controls.Add(this.radioButtonNewWorking);
            this.pnlWipTypeCard.Controls.Add(this.radioButtonSystem);
            this.pnlWipTypeCard.Controls.Add(this.radioButtonPercentage);
            this.pnlWipTypeCard.Controls.Add(this.radioButtonMonthOfSupply);
            this.pnlWipTypeCard.Controls.Add(this.lblWipTypeHeader);
            this.pnlWipTypeCard.Location = new System.Drawing.Point(30, 85);
            this.pnlWipTypeCard.Name = "pnlWipTypeCard";
            this.pnlWipTypeCard.Size = new System.Drawing.Size(360, 200);
            this.pnlWipTypeCard.TabIndex = 1;
            // 
            // lblPercentSymbol
            // 
            this.lblPercentSymbol.AutoSize = true;
            this.lblPercentSymbol.Location = new System.Drawing.Point(220, 87);
            this.lblPercentSymbol.Name = "lblPercentSymbol";
            this.lblPercentSymbol.Size = new System.Drawing.Size(17, 15);
            this.lblPercentSymbol.TabIndex = 6;
            this.lblPercentSymbol.Text = "%";
            this.lblPercentSymbol.Visible = false;
            // 
            // textBoxPercentage
            // 
            this.textBoxPercentage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxPercentage.Location = new System.Drawing.Point(150, 83);
            this.textBoxPercentage.Name = "textBoxPercentage";
            this.textBoxPercentage.Size = new System.Drawing.Size(60, 23);
            this.textBoxPercentage.TabIndex = 5;
            this.textBoxPercentage.Visible = false;
            // 
            // radioButtonNewWorking
            // 
            this.radioButtonNewWorking.AutoSize = true;
            this.radioButtonNewWorking.Location = new System.Drawing.Point(30, 155);
            this.radioButtonNewWorking.Name = "radioButtonNewWorking";
            this.radioButtonNewWorking.Size = new System.Drawing.Size(121, 19);
            this.radioButtonNewWorking.TabIndex = 4;
            this.radioButtonNewWorking.Text = "Wip Working";
            this.radioButtonNewWorking.UseVisualStyleBackColor = true;
            // 
            // radioButtonSystem
            // 
            this.radioButtonSystem.AutoSize = true;
            this.radioButtonSystem.Location = new System.Drawing.Point(30, 120);
            this.radioButtonSystem.Name = "radioButtonSystem";
            this.radioButtonSystem.Size = new System.Drawing.Size(184, 19);
            this.radioButtonSystem.TabIndex = 3;
            this.radioButtonSystem.Text = "System (Standard Calculation)";
            this.radioButtonSystem.UseVisualStyleBackColor = true;
            this.radioButtonSystem.Enabled = false;

            // 
            // radioButtonPercentage
            // 
            this.radioButtonPercentage.AutoSize = true;
            this.radioButtonPercentage.Location = new System.Drawing.Point(30, 85);
            this.radioButtonPercentage.Name = "radioButtonPercentage";
            this.radioButtonPercentage.Size = new System.Drawing.Size(84, 19);
            this.radioButtonPercentage.TabIndex = 2;
            this.radioButtonPercentage.Text = "Percentage";
            this.radioButtonPercentage.UseVisualStyleBackColor = true;
            this.radioButtonPercentage.Enabled = false;
            this.radioButtonPercentage.CheckedChanged += new System.EventHandler(this.radioButtonPercentage_CheckedChanged);
            // 
            // radioButtonMonthOfSupply
            // 
            this.radioButtonMonthOfSupply.AutoSize = true;
            this.radioButtonMonthOfSupply.Checked = true;
            this.radioButtonMonthOfSupply.Location = new System.Drawing.Point(30, 50);
            this.radioButtonMonthOfSupply.Name = "radioButtonMonthOfSupply";
            this.radioButtonMonthOfSupply.Size = new System.Drawing.Size(114, 19);
            this.radioButtonMonthOfSupply.TabIndex = 1;
            this.radioButtonMonthOfSupply.TabStop = true;
            this.radioButtonMonthOfSupply.Text = "Month of Supply";
            this.radioButtonMonthOfSupply.UseVisualStyleBackColor = true;
            this.radioButtonMonthOfSupply.Enabled = false;
            // 
            // lblWipTypeHeader
            // 
            this.lblWipTypeHeader.AutoSize = true;
            this.lblWipTypeHeader.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblWipTypeHeader.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.lblWipTypeHeader.Location = new System.Drawing.Point(15, 15);
            this.lblWipTypeHeader.Name = "lblWipTypeHeader";
            this.lblWipTypeHeader.Size = new System.Drawing.Size(123, 19);
            this.lblWipTypeHeader.TabIndex = 0;
            this.lblWipTypeHeader.Text = "Calculation Logic";
            // 
            // pnlOptionsCard
            // 
            this.pnlOptionsCard.BackColor = System.Drawing.Color.White;
            this.pnlOptionsCard.Controls.Add(this.textBoxMOQ);
            this.pnlOptionsCard.Controls.Add(this.checkBoxCasePack);
            this.pnlOptionsCard.Controls.Add(this.checkBoxMOQ);
            this.pnlOptionsCard.Controls.Add(this.lblOptionsHeader);
            this.pnlOptionsCard.Location = new System.Drawing.Point(30, 305);
            this.pnlOptionsCard.Name = "pnlOptionsCard";
            this.pnlOptionsCard.Size = new System.Drawing.Size(360, 130);
            this.pnlOptionsCard.TabIndex = 2;
            // 
            // textBoxMOQ
            // 
            this.textBoxMOQ.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxMOQ.Location = new System.Drawing.Point(130, 48);
            this.textBoxMOQ.Name = "textBoxMOQ";
            this.textBoxMOQ.Size = new System.Drawing.Size(100, 23);
            this.textBoxMOQ.TabIndex = 6;
            this.textBoxMOQ.Visible = false;
            // 
            // checkBoxCasePack
            // 
            this.checkBoxCasePack.AutoSize = true;
            this.checkBoxCasePack.Location = new System.Drawing.Point(30, 85);
            this.checkBoxCasePack.Name = "checkBoxCasePack";
            this.checkBoxCasePack.Size = new System.Drawing.Size(131, 19);
            this.checkBoxCasePack.TabIndex = 2;
            this.checkBoxCasePack.Text = "Round to Case Pack";
            this.checkBoxCasePack.UseVisualStyleBackColor = true;
            this.checkBoxCasePack.Enabled = false;

            // 
            // checkBoxMOQ
            // 
            this.checkBoxMOQ.AutoSize = true;
            this.checkBoxMOQ.Location = new System.Drawing.Point(30, 50);
            this.checkBoxMOQ.Name = "checkBoxMOQ";
            this.checkBoxMOQ.Size = new System.Drawing.Size(89, 19);
            this.checkBoxMOQ.TabIndex = 1;
            this.checkBoxMOQ.Text = "Apply MOQ";
            this.checkBoxMOQ.UseVisualStyleBackColor = true;
            this.checkBoxMOQ.Enabled = false;
            this.checkBoxMOQ.CheckedChanged += new System.EventHandler(this.checkBoxMOQ_CheckedChanged);
            // 
            // lblOptionsHeader
            // 
            this.lblOptionsHeader.AutoSize = true;
            this.lblOptionsHeader.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblOptionsHeader.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.lblOptionsHeader.Location = new System.Drawing.Point(15, 15);
            this.lblOptionsHeader.Name = "lblOptionsHeader";
            this.lblOptionsHeader.Size = new System.Drawing.Size(83, 19);
            this.lblOptionsHeader.TabIndex = 0;
            this.lblOptionsHeader.Text = "Constraints";
            // 
            // btnReviewWIP
            // 
            this.btnReviewWIP.Location = new System.Drawing.Point(30, 455);
            this.btnReviewWIP.Name = "btnReviewWIP";
            this.btnReviewWIP.Size = new System.Drawing.Size(170, 45);
            this.btnReviewWIP.TabIndex = 3;
            this.btnReviewWIP.Text = "Calculate && Review";
            this.btnReviewWIP.UseVisualStyleBackColor = true;
            this.btnReviewWIP.Click += new System.EventHandler(this.btnReviewWIP_Click);
            // 
            // btnApproveWIP
            // 
            this.btnApproveWIP.Location = new System.Drawing.Point(215, 455);
            this.btnApproveWIP.Name = "btnApproveWIP";
            this.btnApproveWIP.Size = new System.Drawing.Size(175, 45);
            this.btnApproveWIP.TabIndex = 4;
            this.btnApproveWIP.Text = "Approve && Save";
            this.btnApproveWIP.UseVisualStyleBackColor = true;
            this.btnApproveWIP.Click += new System.EventHandler(this.btnApproveWIP_Click);
            // 
            // CalculateWIPForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(246)))), ((int)(((byte)(250)))));
            this.ClientSize = new System.Drawing.Size(420, 531);
            this.Controls.Add(this.btnApproveWIP);
            this.Controls.Add(this.btnReviewWIP);
            this.Controls.Add(this.pnlOptionsCard);
            this.Controls.Add(this.pnlWipTypeCard);
            this.Controls.Add(this.pnlHeader);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            //this.MaximizeBox = false;
            //this.MaximumSize = new System.Drawing.Size(436, 570);
            //this.MinimumSize = new System.Drawing.Size(436, 570);
            this.MaximizeBox = true;
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
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
        private System.Windows.Forms.Panel pnlWipTypeCard;
        private System.Windows.Forms.Label lblWipTypeHeader;
        private System.Windows.Forms.RadioButton radioButtonMonthOfSupply;
        private System.Windows.Forms.RadioButton radioButtonPercentage;
        private System.Windows.Forms.RadioButton radioButtonSystem;
        private System.Windows.Forms.RadioButton radioButtonNewWorking;
        private System.Windows.Forms.TextBox textBoxPercentage;
        private System.Windows.Forms.Label lblPercentSymbol;
        private System.Windows.Forms.Panel pnlOptionsCard;
        private System.Windows.Forms.Label lblOptionsHeader;
        private System.Windows.Forms.CheckBox checkBoxMOQ;
        private System.Windows.Forms.CheckBox checkBoxCasePack;
        private System.Windows.Forms.TextBox textBoxMOQ;
        private System.Windows.Forms.Button btnReviewWIP;
        private System.Windows.Forms.Button btnApproveWIP;
        private System.Windows.Forms.ProgressBar progressBar1;
    }
}