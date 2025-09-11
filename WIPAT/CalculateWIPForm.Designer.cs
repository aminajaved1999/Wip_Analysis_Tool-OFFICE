namespace WIPAT
{
    partial class CalculateWIPForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.GroupBox groupBoxWipType;
        private System.Windows.Forms.RadioButton radioButtonSystem;
        private System.Windows.Forms.RadioButton radioButtonPercentage;
        private System.Windows.Forms.RadioButton radioButtonMonthOfSupply;
        private System.Windows.Forms.GroupBox groupBoxOptions;
        private System.Windows.Forms.CheckBox checkBoxMOQ;
        private System.Windows.Forms.CheckBox checkBoxCasePack;
        private System.Windows.Forms.Button btnReviewWIP;
        private System.Windows.Forms.Button btnApproveWIP;
        private System.Windows.Forms.Label labelWIPType;
        private System.Windows.Forms.Label labelOptions;
        private System.Windows.Forms.TextBox textBoxMOQ;
        private System.Windows.Forms.TextBox textBoxPercentage;

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
            this.groupBoxWipType = new System.Windows.Forms.GroupBox();
            this.textBoxPercentage = new System.Windows.Forms.TextBox();
            this.radioButtonSystem = new System.Windows.Forms.RadioButton();
            this.radioButtonPercentage = new System.Windows.Forms.RadioButton();
            this.radioButtonMonthOfSupply = new System.Windows.Forms.RadioButton();
            this.groupBoxOptions = new System.Windows.Forms.GroupBox();
            this.textBoxMOQ = new System.Windows.Forms.TextBox();
            this.checkBoxMOQ = new System.Windows.Forms.CheckBox();
            this.checkBoxCasePack = new System.Windows.Forms.CheckBox();
            this.btnReviewWIP = new System.Windows.Forms.Button();
            this.btnApproveWIP = new System.Windows.Forms.Button();
            this.labelWIPType = new System.Windows.Forms.Label();
            this.labelOptions = new System.Windows.Forms.Label();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.groupBoxWipType.SuspendLayout();
            this.groupBoxOptions.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBoxWipType
            // 
            this.groupBoxWipType.Controls.Add(this.textBoxPercentage);
            this.groupBoxWipType.Controls.Add(this.radioButtonSystem);
            this.groupBoxWipType.Controls.Add(this.radioButtonPercentage);
            this.groupBoxWipType.Controls.Add(this.radioButtonMonthOfSupply);
            this.groupBoxWipType.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.groupBoxWipType.Location = new System.Drawing.Point(30, 60);
            this.groupBoxWipType.Name = "groupBoxWipType";
            this.groupBoxWipType.Size = new System.Drawing.Size(260, 120);
            this.groupBoxWipType.TabIndex = 0;
            this.groupBoxWipType.TabStop = false;
            this.groupBoxWipType.Text = "Select WIP Type";
            // 
            // textBoxPercentage
            // 
            this.textBoxPercentage.Location = new System.Drawing.Point(120, 60);
            this.textBoxPercentage.Name = "textBoxPercentage";
            this.textBoxPercentage.Size = new System.Drawing.Size(100, 25);
            this.textBoxPercentage.TabIndex = 0;
            this.textBoxPercentage.Visible = false;
            this.textBoxPercentage.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxPercentage_Validating);
            // 
            // radioButtonSystem
            // 
            this.radioButtonSystem.AutoSize = true;
            this.radioButtonSystem.Location = new System.Drawing.Point(20, 85);
            this.radioButtonSystem.Name = "radioButtonSystem";
            this.radioButtonSystem.Size = new System.Drawing.Size(71, 23);
            this.radioButtonSystem.TabIndex = 2;
            this.radioButtonSystem.TabStop = true;
            this.radioButtonSystem.Text = "System";
            this.radioButtonSystem.UseVisualStyleBackColor = true;
            // 
            // radioButtonPercentage
            // 
            this.radioButtonPercentage.AutoSize = true;
            this.radioButtonPercentage.Location = new System.Drawing.Point(20, 60);
            this.radioButtonPercentage.Name = "radioButtonPercentage";
            this.radioButtonPercentage.Size = new System.Drawing.Size(94, 23);
            this.radioButtonPercentage.TabIndex = 1;
            this.radioButtonPercentage.TabStop = true;
            this.radioButtonPercentage.Text = "Percentage";
            this.radioButtonPercentage.UseVisualStyleBackColor = true;
            this.radioButtonPercentage.CheckedChanged += new System.EventHandler(this.radioButtonPercentage_CheckedChanged);
            // 
            // radioButtonMonthOfSupply
            // 
            this.radioButtonMonthOfSupply.AutoSize = true;
            this.radioButtonMonthOfSupply.Location = new System.Drawing.Point(20, 35);
            this.radioButtonMonthOfSupply.Name = "radioButtonMonthOfSupply";
            this.radioButtonMonthOfSupply.Size = new System.Drawing.Size(130, 23);
            this.radioButtonMonthOfSupply.TabIndex = 0;
            this.radioButtonMonthOfSupply.TabStop = true;
            this.radioButtonMonthOfSupply.Text = "Month of Supply";
            this.radioButtonMonthOfSupply.UseVisualStyleBackColor = true;
            // 
            // groupBoxOptions
            // 
            this.groupBoxOptions.Controls.Add(this.textBoxMOQ);
            this.groupBoxOptions.Controls.Add(this.checkBoxMOQ);
            this.groupBoxOptions.Controls.Add(this.checkBoxCasePack);
            this.groupBoxOptions.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.groupBoxOptions.Location = new System.Drawing.Point(30, 200);
            this.groupBoxOptions.Name = "groupBoxOptions";
            this.groupBoxOptions.Size = new System.Drawing.Size(260, 113);
            this.groupBoxOptions.TabIndex = 1;
            this.groupBoxOptions.TabStop = false;
            this.groupBoxOptions.Text = "Options";
            // 
            // textBoxMOQ
            // 
            this.textBoxMOQ.Location = new System.Drawing.Point(120, 35);
            this.textBoxMOQ.Name = "textBoxMOQ";
            this.textBoxMOQ.Size = new System.Drawing.Size(100, 25);
            this.textBoxMOQ.TabIndex = 0;
            this.textBoxMOQ.Visible = false;
            // 
            // checkBoxMOQ
            // 
            this.checkBoxMOQ.AutoSize = true;
            this.checkBoxMOQ.Location = new System.Drawing.Point(20, 35);
            this.checkBoxMOQ.Name = "checkBoxMOQ";
            this.checkBoxMOQ.Size = new System.Drawing.Size(63, 23);
            this.checkBoxMOQ.TabIndex = 0;
            this.checkBoxMOQ.Text = "MOQ";
            this.checkBoxMOQ.UseVisualStyleBackColor = true;
            this.checkBoxMOQ.CheckedChanged += new System.EventHandler(this.checkBoxMOQ_CheckedChanged);
            // 
            // checkBoxCasePack
            // 
            this.checkBoxCasePack.AutoSize = true;
            this.checkBoxCasePack.Location = new System.Drawing.Point(20, 65);
            this.checkBoxCasePack.Name = "checkBoxCasePack";
            this.checkBoxCasePack.Size = new System.Drawing.Size(89, 23);
            this.checkBoxCasePack.TabIndex = 1;
            this.checkBoxCasePack.Text = "Case Pack";
            this.checkBoxCasePack.UseVisualStyleBackColor = true;
            // 
            // btnReviewWIP
            // 
            this.btnReviewWIP.BackColor = System.Drawing.Color.DarkBlue;
            this.btnReviewWIP.FlatAppearance.BorderSize = 0;
            this.btnReviewWIP.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReviewWIP.ForeColor = System.Drawing.Color.White;
            this.btnReviewWIP.Location = new System.Drawing.Point(30, 341);
            this.btnReviewWIP.Name = "btnReviewWIP";
            this.btnReviewWIP.Size = new System.Drawing.Size(119, 35);
            this.btnReviewWIP.TabIndex = 2;
            this.btnReviewWIP.Text = "Review WIP";
            this.btnReviewWIP.UseVisualStyleBackColor = false;
            this.btnReviewWIP.Click += new System.EventHandler(this.btnReviewWIP_Click);
            // 
            // btnApproveWIP
            // 
            this.btnApproveWIP.BackColor = System.Drawing.Color.Navy;
            this.btnApproveWIP.Enabled = false;
            this.btnApproveWIP.FlatAppearance.BorderSize = 0;
            this.btnApproveWIP.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnApproveWIP.ForeColor = System.Drawing.Color.White;
            this.btnApproveWIP.Location = new System.Drawing.Point(171, 341);
            this.btnApproveWIP.Name = "btnApproveWIP";
            this.btnApproveWIP.Size = new System.Drawing.Size(119, 35);
            this.btnApproveWIP.TabIndex = 3;
            this.btnApproveWIP.Text = "Approve WIP";
            this.btnApproveWIP.UseVisualStyleBackColor = false;
            this.btnApproveWIP.Click += new System.EventHandler(this.btnApproveWIP_Click);
            // 
            // labelWIPType
            // 
            this.labelWIPType.AutoSize = true;
            this.labelWIPType.Location = new System.Drawing.Point(30, 40);
            this.labelWIPType.Name = "labelWIPType";
            this.labelWIPType.Size = new System.Drawing.Size(91, 13);
            this.labelWIPType.TabIndex = 5;
            this.labelWIPType.Text = "Select WIP Type:";
            // 
            // labelOptions
            // 
            this.labelOptions.AutoSize = true;
            this.labelOptions.Location = new System.Drawing.Point(30, 180);
            this.labelOptions.Name = "labelOptions";
            this.labelOptions.Size = new System.Drawing.Size(46, 13);
            this.labelOptions.TabIndex = 6;
            this.labelOptions.Text = "Options:";
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(30, 12);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(260, 23);
            this.progressBar1.TabIndex = 7;
            this.progressBar1.Visible = false;
            // 
            // CalculateWIPForm
            // 
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(320, 420);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.labelOptions);
            this.Controls.Add(this.labelWIPType);
            this.Controls.Add(this.btnApproveWIP);
            this.Controls.Add(this.btnReviewWIP);
            this.Controls.Add(this.groupBoxOptions);
            this.Controls.Add(this.groupBoxWipType);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "CalculateWIPForm";
            this.Text = "Calculate WIP";
            this.groupBoxWipType.ResumeLayout(false);
            this.groupBoxWipType.PerformLayout();
            this.groupBoxOptions.ResumeLayout(false);
            this.groupBoxOptions.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }



        #endregion

        private System.Windows.Forms.ProgressBar progressBar1;
    }
}
