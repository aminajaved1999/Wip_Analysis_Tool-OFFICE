using System;
using System.Drawing;
using System.Windows.Forms;

namespace WIPAT
{
    partial class UploadForm : Form
    {
        private System.ComponentModel.IContainer components = null;
        private Button btnUpload;
        private Button btnBrowseForecast;
        private Button btnBrowseStock;
        private Button btnBrowseOrder;

        private Label lblDGV1;
        private Label lblDGV2;
        private Label lblDGV3;
        private Label lblDGV4;

        private DataGridView dataGridView1;
        private DataGridView dataGridView2;
        private DataGridView dataGridView3;
        private DataGridView dataGridView4;

        private Label lblStock;
        private Label lblOrder;
        private DataGridView dataGridViewOrder;
        private DataGridView dataGridViewStock;

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabPageForecast;
        private System.Windows.Forms.TabPage tabPageStock;
        private System.Windows.Forms.TabPage tabPageOrder;



        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.btnUpload = new System.Windows.Forms.Button();
            this.btnBrowseForecast = new System.Windows.Forms.Button();
            this.btnBrowseStock = new System.Windows.Forms.Button();
            this.btnBrowseOrder = new System.Windows.Forms.Button();
            this.lblDGV1 = new System.Windows.Forms.Label();
            this.lblDGV2 = new System.Windows.Forms.Label();
            this.lblDGV3 = new System.Windows.Forms.Label();
            this.lblDGV4 = new System.Windows.Forms.Label();
            this.lblStock = new System.Windows.Forms.Label();
            this.lblOrder = new System.Windows.Forms.Label();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.dataGridView2 = new System.Windows.Forms.DataGridView();
            this.dataGridView3 = new System.Windows.Forms.DataGridView();
            this.dataGridView4 = new System.Windows.Forms.DataGridView();
            this.dataGridViewOrder = new System.Windows.Forms.DataGridView();
            this.dataGridViewStock = new System.Windows.Forms.DataGridView();
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabPageForecast = new System.Windows.Forms.TabPage();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.tabPageStock = new System.Windows.Forms.TabPage();
            this.tabPageOrder = new System.Windows.Forms.TabPage();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView4)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewOrder)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewStock)).BeginInit();
            this.tabControl.SuspendLayout();
            this.tabPageForecast.SuspendLayout();
            this.tabPageStock.SuspendLayout();
            this.tabPageOrder.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnUpload
            // 
            this.btnUpload.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnUpload.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnUpload.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUpload.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.btnUpload.ForeColor = System.Drawing.Color.White;
            this.btnUpload.Location = new System.Drawing.Point(8, 860);
            this.btnUpload.Name = "btnUpload";
            this.btnUpload.Size = new System.Drawing.Size(1108, 40);
            this.btnUpload.TabIndex = 13;
            this.btnUpload.Text = "Finish Upload";
            this.btnUpload.UseVisualStyleBackColor = false;
            this.btnUpload.Click += new System.EventHandler(this.btnUpload_Click);
            // 
            // btnBrowseForecast
            // 
            this.btnBrowseForecast.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(215)))));
            this.btnBrowseForecast.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBrowseForecast.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBrowseForecast.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.btnBrowseForecast.ForeColor = System.Drawing.Color.White;
            this.btnBrowseForecast.Location = new System.Drawing.Point(21, 38);
            this.btnBrowseForecast.Name = "btnBrowseForecast";
            this.btnBrowseForecast.Size = new System.Drawing.Size(180, 48);
            this.btnBrowseForecast.TabIndex = 16;
            this.btnBrowseForecast.Text = "Browse Forecast File";
            this.btnBrowseForecast.UseVisualStyleBackColor = false;
            this.btnBrowseForecast.Click += new System.EventHandler(this.btnBrowseForecast_Click);
            // 
            // btnBrowseStock
            // 
            this.btnBrowseStock.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(215)))));
            this.btnBrowseStock.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBrowseStock.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBrowseStock.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.btnBrowseStock.ForeColor = System.Drawing.Color.White;
            this.btnBrowseStock.Location = new System.Drawing.Point(21, 11);
            this.btnBrowseStock.Name = "btnBrowseStock";
            this.btnBrowseStock.Size = new System.Drawing.Size(180, 48);
            this.btnBrowseStock.TabIndex = 17;
            this.btnBrowseStock.Text = "Browse Stock File";
            this.btnBrowseStock.UseVisualStyleBackColor = false;
            this.btnBrowseStock.Click += new System.EventHandler(this.btnBrowseStock_Click);
            // 
            // btnBrowseOrder
            // 
            this.btnBrowseOrder.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(215)))));
            this.btnBrowseOrder.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBrowseOrder.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBrowseOrder.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.btnBrowseOrder.ForeColor = System.Drawing.Color.White;
            this.btnBrowseOrder.Location = new System.Drawing.Point(21, 11);
            this.btnBrowseOrder.Name = "btnBrowseOrder";
            this.btnBrowseOrder.Size = new System.Drawing.Size(180, 48);
            this.btnBrowseOrder.TabIndex = 17;
            this.btnBrowseOrder.Text = "Browse Order File";
            this.btnBrowseOrder.UseVisualStyleBackColor = false;
            this.btnBrowseOrder.Click += new System.EventHandler(this.btnBrowseOrder_Click);
            // 
            // lblDGV1
            // 
            this.lblDGV1.AutoSize = true;
            this.lblDGV1.Location = new System.Drawing.Point(18, 99);
            this.lblDGV1.Name = "lblDGV1";
            this.lblDGV1.Size = new System.Drawing.Size(83, 13);
            this.lblDGV1.TabIndex = 21;
            this.lblDGV1.Text = "Forecast 1 Data";
            this.lblDGV1.Visible = false;
            // 
            // lblDGV2
            // 
            this.lblDGV2.AutoSize = true;
            this.lblDGV2.Location = new System.Drawing.Point(855, 99);
            this.lblDGV2.Name = "lblDGV2";
            this.lblDGV2.Size = new System.Drawing.Size(83, 13);
            this.lblDGV2.TabIndex = 22;
            this.lblDGV2.Text = "Forecast 2 Data";
            this.lblDGV2.Visible = false;
            // 
            // lblDGV3
            // 
            this.lblDGV3.AutoSize = true;
            this.lblDGV3.Location = new System.Drawing.Point(18, 310);
            this.lblDGV3.Name = "lblDGV3";
            this.lblDGV3.Size = new System.Drawing.Size(83, 13);
            this.lblDGV3.TabIndex = 23;
            this.lblDGV3.Text = "Forecast 3 Data";
            this.lblDGV3.Visible = false;
            // 
            // lblDGV4
            // 
            this.lblDGV4.AutoSize = true;
            this.lblDGV4.Location = new System.Drawing.Point(855, 310);
            this.lblDGV4.Name = "lblDGV4";
            this.lblDGV4.Size = new System.Drawing.Size(83, 13);
            this.lblDGV4.TabIndex = 24;
            this.lblDGV4.Text = "Forecast 4 Data";
            this.lblDGV4.Visible = false;
            // 
            // lblStock
            // 
            this.lblStock.AutoSize = true;
            this.lblStock.Location = new System.Drawing.Point(18, 88);
            this.lblStock.Name = "lblStock";
            this.lblStock.Size = new System.Drawing.Size(61, 13);
            this.lblStock.TabIndex = 18;
            this.lblStock.Text = "Stock Data";
            // 
            // lblOrder
            // 
            this.lblOrder.AutoSize = true;
            this.lblOrder.Location = new System.Drawing.Point(18, 89);
            this.lblOrder.Name = "lblOrder";
            this.lblOrder.Size = new System.Drawing.Size(59, 13);
            this.lblOrder.TabIndex = 18;
            this.lblOrder.Text = "Order Data";
            // 
            // dataGridView1
            // 
            this.dataGridView1.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridView1.Location = new System.Drawing.Point(21, 115);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(804, 165);
            this.dataGridView1.TabIndex = 17;
            // 
            // dataGridView2
            // 
            this.dataGridView2.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridView2.Location = new System.Drawing.Point(858, 115);
            this.dataGridView2.Name = "dataGridView2";
            this.dataGridView2.Size = new System.Drawing.Size(804, 165);
            this.dataGridView2.TabIndex = 18;
            // 
            // dataGridView3
            // 
            this.dataGridView3.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridView3.Location = new System.Drawing.Point(21, 326);
            this.dataGridView3.Name = "dataGridView3";
            this.dataGridView3.Size = new System.Drawing.Size(804, 165);
            this.dataGridView3.TabIndex = 19;
            // 
            // dataGridView4
            // 
            this.dataGridView4.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridView4.Location = new System.Drawing.Point(858, 326);
            this.dataGridView4.Name = "dataGridView4";
            this.dataGridView4.Size = new System.Drawing.Size(804, 165);
            this.dataGridView4.TabIndex = 20;
            // 
            // dataGridViewOrder
            // 
            this.dataGridViewOrder.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridViewOrder.Location = new System.Drawing.Point(21, 109);
            this.dataGridViewOrder.Name = "dataGridViewOrder";
            this.dataGridViewOrder.Size = new System.Drawing.Size(415, 150);
            this.dataGridViewOrder.TabIndex = 19;
            // 
            // dataGridViewStock
            // 
            this.dataGridViewStock.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridViewStock.Location = new System.Drawing.Point(21, 118);
            this.dataGridViewStock.Name = "dataGridViewStock";
            this.dataGridViewStock.Size = new System.Drawing.Size(415, 150);
            this.dataGridViewStock.TabIndex = 19;
            // 
            // tabControl
            // 
            this.tabControl.Appearance = System.Windows.Forms.TabAppearance.FlatButtons;
            this.tabControl.Controls.Add(this.tabPageForecast);
            this.tabControl.Controls.Add(this.tabPageStock);
            this.tabControl.Controls.Add(this.tabPageOrder);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.ItemSize = new System.Drawing.Size(140, 50);
            this.tabControl.Location = new System.Drawing.Point(0, 0);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(1708, 585);
            this.tabControl.TabIndex = 13;
            // 
            // tabPageForecast
            // 
            this.tabPageForecast.BackColor = System.Drawing.Color.White;
            this.tabPageForecast.Controls.Add(this.progressBar1);
            this.tabPageForecast.Controls.Add(this.btnBrowseForecast);
            this.tabPageForecast.Controls.Add(this.dataGridView1);
            this.tabPageForecast.Controls.Add(this.dataGridView2);
            this.tabPageForecast.Controls.Add(this.dataGridView3);
            this.tabPageForecast.Controls.Add(this.dataGridView4);
            this.tabPageForecast.Controls.Add(this.lblDGV1);
            this.tabPageForecast.Controls.Add(this.lblDGV2);
            this.tabPageForecast.Controls.Add(this.lblDGV3);
            this.tabPageForecast.Controls.Add(this.lblDGV4);
            this.tabPageForecast.Location = new System.Drawing.Point(4, 54);
            this.tabPageForecast.Name = "tabPageForecast";
            this.tabPageForecast.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageForecast.Size = new System.Drawing.Size(1700, 527);
            this.tabPageForecast.TabIndex = 0;
            this.tabPageForecast.Text = "Forecast File";
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(8, 9);
            this.progressBar1.MarqueeAnimationSpeed = 40;
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(300, 23);
            this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar1.TabIndex = 101;
            this.progressBar1.Visible = false;
            // 
            // tabPageStock
            // 
            this.tabPageStock.BackColor = System.Drawing.Color.White;
            this.tabPageStock.Controls.Add(this.btnBrowseStock);
            this.tabPageStock.Controls.Add(this.lblStock);
            this.tabPageStock.Controls.Add(this.dataGridViewStock);
            this.tabPageStock.Location = new System.Drawing.Point(4, 54);
            this.tabPageStock.Name = "tabPageStock";
            this.tabPageStock.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageStock.Size = new System.Drawing.Size(1700, 527);
            this.tabPageStock.TabIndex = 1;
            this.tabPageStock.Text = "Stock File";
            // 
            // tabPageOrder
            // 
            this.tabPageOrder.BackColor = System.Drawing.Color.White;
            this.tabPageOrder.Controls.Add(this.btnBrowseOrder);
            this.tabPageOrder.Controls.Add(this.lblOrder);
            this.tabPageOrder.Controls.Add(this.dataGridViewOrder);
            this.tabPageOrder.Location = new System.Drawing.Point(4, 54);
            this.tabPageOrder.Name = "tabPageOrder";
            this.tabPageOrder.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageOrder.Size = new System.Drawing.Size(1700, 527);
            this.tabPageOrder.TabIndex = 2;
            this.tabPageOrder.Text = "Order File";
            // 
            // UploadForm
            // 
            this.ClientSize = new System.Drawing.Size(1708, 585);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.btnUpload);
            this.Name = "UploadForm";
            this.Text = "Upload Form";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView4)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewOrder)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewStock)).EndInit();
            this.tabControl.ResumeLayout(false);
            this.tabPageForecast.ResumeLayout(false);
            this.tabPageForecast.PerformLayout();
            this.tabPageStock.ResumeLayout(false);
            this.tabPageStock.PerformLayout();
            this.tabPageOrder.ResumeLayout(false);
            this.tabPageOrder.PerformLayout();
            this.ResumeLayout(false);

        }

        private ProgressBar progressBar1;
    }
}
