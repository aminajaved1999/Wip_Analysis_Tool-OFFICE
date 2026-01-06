namespace WIPAT
{
    partial class SplashScreen
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        private void InitializeComponent()
        {
            this.titleLabel = new System.Windows.Forms.Label();
            this.taglineLabel = new System.Windows.Forms.Label();
            this.versionLabel = new System.Windows.Forms.Label();
            this.footerLabel = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            // 
            // titleLabel
            // 
            this.titleLabel.BackColor = System.Drawing.Color.Transparent;
            this.titleLabel.Font = new System.Drawing.Font("Segoe UI", 28F, System.Drawing.FontStyle.Bold);
            this.titleLabel.ForeColor = System.Drawing.Color.White;
            this.titleLabel.Location = new System.Drawing.Point(12, 165);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(776, 61);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "[AppName]";
            this.titleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // taglineLabel
            // 
            this.taglineLabel.BackColor = System.Drawing.Color.Transparent;
            this.taglineLabel.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Italic);
            this.taglineLabel.ForeColor = System.Drawing.Color.LightGray;
            this.taglineLabel.Location = new System.Drawing.Point(95, 226);
            this.taglineLabel.Name = "taglineLabel";
            this.taglineLabel.Size = new System.Drawing.Size(609, 26);
            this.taglineLabel.TabIndex = 1;
            this.taglineLabel.Text = "[AppTagline]";
            this.taglineLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // versionLabel
            // 
            this.versionLabel.BackColor = System.Drawing.Color.Transparent;
            this.versionLabel.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.versionLabel.ForeColor = System.Drawing.Color.Silver;
            this.versionLabel.Location = new System.Drawing.Point(12, 475);
            this.versionLabel.Name = "versionLabel";
            this.versionLabel.Size = new System.Drawing.Size(171, 17);
            this.versionLabel.TabIndex = 3;
            this.versionLabel.Text = "[AppVersion]";
            this.versionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // footerLabel
            // 
            this.footerLabel.BackColor = System.Drawing.Color.Transparent;
            this.footerLabel.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.footerLabel.ForeColor = System.Drawing.Color.Silver;
            this.footerLabel.Location = new System.Drawing.Point(617, 474);
            this.footerLabel.Name = "footerLabel";
            this.footerLabel.Size = new System.Drawing.Size(171, 17);
            this.footerLabel.TabIndex = 4;
            this.footerLabel.Text = "[AppDeveloper]";
            this.footerLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // progressBar
            // 
            this.progressBar.ForeColor = System.Drawing.Color.DodgerBlue;
            this.progressBar.Location = new System.Drawing.Point(266, 286);
            this.progressBar.MarqueeAnimationSpeed = 40;
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(266, 13);
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar.TabIndex = 2;
            // 
            // SplashScreen
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = global::WIPAT.Properties.Resources.splash;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(800, 500);
            this.Controls.Add(this.footerLabel);
            this.Controls.Add(this.versionLabel);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.taglineLabel);
            this.Controls.Add(this.titleLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "SplashScreen";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label taglineLabel;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label versionLabel;
        private System.Windows.Forms.Label footerLabel;
    }
}
