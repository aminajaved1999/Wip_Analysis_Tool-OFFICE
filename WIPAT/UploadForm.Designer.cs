using System;
using System.Drawing;
using System.Windows.Forms;

namespace WIPAT
{
    partial class UploadForm
    {
        private System.ComponentModel.IContainer components = null;
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

        private TabControl tabControl;
        private TabPage tabPageForecast;
        private TabPage tabPageStock;
        private TabPage tabPageOrder;

        private ProgressBar progressBar1;
        private Panel headerForecast;
        private FlowLayoutPanel headerFLayout;
        private TableLayoutPanel gridForecast;
        private Panel cardF1;
        private Panel cardF2;
        private Panel cardF3;
        private Panel cardF4;

        private Panel headerStock;
        private FlowLayoutPanel headerSLayout;
        private Panel cardStock;

        private Panel headerOrder;
        private FlowLayoutPanel headerOLayout;
        private Panel cardOrder;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.spacerHeaderF1 = new System.Windows.Forms.Panel();
            this.spacerHeaderF2 = new System.Windows.Forms.Panel();
            this.spacerF1 = new System.Windows.Forms.Panel();
            this.spacerF2 = new System.Windows.Forms.Panel();
            this.spacerF3 = new System.Windows.Forms.Panel();
            this.spacerF4 = new System.Windows.Forms.Panel();
            this.spacerS = new System.Windows.Forms.Panel();
            this.spacerO = new System.Windows.Forms.Panel();
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabPageForecast = new System.Windows.Forms.TabPage();
            this.gridForecast = new System.Windows.Forms.TableLayoutPanel();
            this.cardF1 = new System.Windows.Forms.Panel();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.lblDGV1 = new System.Windows.Forms.Label();
            this.cardF2 = new System.Windows.Forms.Panel();
            this.dataGridView2 = new System.Windows.Forms.DataGridView();
            this.lblDGV2 = new System.Windows.Forms.Label();
            this.cardF3 = new System.Windows.Forms.Panel();
            this.dataGridView3 = new System.Windows.Forms.DataGridView();
            this.lblDGV3 = new System.Windows.Forms.Label();
            this.cardF4 = new System.Windows.Forms.Panel();
            this.dataGridView4 = new System.Windows.Forms.DataGridView();
            this.lblDGV4 = new System.Windows.Forms.Label();
            this.headerForecast = new System.Windows.Forms.Panel();
            this.headerFLayout = new System.Windows.Forms.FlowLayoutPanel();
            this.btnBrowseForecast = new System.Windows.Forms.Button();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.tabPageStock = new System.Windows.Forms.TabPage();
            this.cardStock = new System.Windows.Forms.Panel();
            this.dataGridViewStock = new System.Windows.Forms.DataGridView();
            this.lblStock = new System.Windows.Forms.Label();
            this.headerStock = new System.Windows.Forms.Panel();
            this.headerSLayout = new System.Windows.Forms.FlowLayoutPanel();
            this.btnBrowseStock = new System.Windows.Forms.Button();
            this.tabPageOrder = new System.Windows.Forms.TabPage();
            this.cardOrder = new System.Windows.Forms.Panel();
            this.dataGridViewOrder = new System.Windows.Forms.DataGridView();
            this.lblOrder = new System.Windows.Forms.Label();
            this.headerOrder = new System.Windows.Forms.Panel();
            this.headerOLayout = new System.Windows.Forms.FlowLayoutPanel();
            this.btnBrowseOrder = new System.Windows.Forms.Button();
            this.tabControl.SuspendLayout();
            this.tabPageForecast.SuspendLayout();
            this.gridForecast.SuspendLayout();
            this.cardF1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.cardF2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView2)).BeginInit();
            this.cardF3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView3)).BeginInit();
            this.cardF4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView4)).BeginInit();
            this.headerForecast.SuspendLayout();
            this.headerFLayout.SuspendLayout();
            this.tabPageStock.SuspendLayout();
            this.cardStock.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewStock)).BeginInit();
            this.headerStock.SuspendLayout();
            this.headerSLayout.SuspendLayout();
            this.tabPageOrder.SuspendLayout();
            this.cardOrder.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewOrder)).BeginInit();
            this.headerOrder.SuspendLayout();
            this.headerOLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // spacerHeaderF1
            // 
            this.spacerHeaderF1.Location = new System.Drawing.Point(0, 0);
            this.spacerHeaderF1.Margin = new System.Windows.Forms.Padding(0);
            this.spacerHeaderF1.Name = "spacerHeaderF1";
            this.spacerHeaderF1.Size = new System.Drawing.Size(8, 1);
            this.spacerHeaderF1.TabIndex = 1;
            // 
            // spacerHeaderF2
            // 
            this.spacerHeaderF2.Location = new System.Drawing.Point(156, 0);
            this.spacerHeaderF2.Margin = new System.Windows.Forms.Padding(0);
            this.spacerHeaderF2.Name = "spacerHeaderF2";
            this.spacerHeaderF2.Size = new System.Drawing.Size(8, 1);
            this.spacerHeaderF2.TabIndex = 3;
            // 
            // spacerF1
            // 
            this.spacerF1.Dock = System.Windows.Forms.DockStyle.Top;
            this.spacerF1.Location = new System.Drawing.Point(14, 40);
            this.spacerF1.Name = "spacerF1";
            this.spacerF1.Size = new System.Drawing.Size(584, 8);
            this.spacerF1.TabIndex = 1;
            // 
            // spacerF2
            // 
            this.spacerF2.Dock = System.Windows.Forms.DockStyle.Top;
            this.spacerF2.Location = new System.Drawing.Point(14, 40);
            this.spacerF2.Name = "spacerF2";
            this.spacerF2.Size = new System.Drawing.Size(584, 8);
            this.spacerF2.TabIndex = 1;
            // 
            // spacerF3
            // 
            this.spacerF3.Dock = System.Windows.Forms.DockStyle.Top;
            this.spacerF3.Location = new System.Drawing.Point(14, 40);
            this.spacerF3.Name = "spacerF3";
            this.spacerF3.Size = new System.Drawing.Size(584, 8);
            this.spacerF3.TabIndex = 1;
            // 
            // spacerF4
            // 
            this.spacerF4.Dock = System.Windows.Forms.DockStyle.Top;
            this.spacerF4.Location = new System.Drawing.Point(14, 40);
            this.spacerF4.Name = "spacerF4";
            this.spacerF4.Size = new System.Drawing.Size(584, 8);
            this.spacerF4.TabIndex = 1;
            // 
            // spacerS
            // 
            this.spacerS.Dock = System.Windows.Forms.DockStyle.Top;
            this.spacerS.Location = new System.Drawing.Point(14, 40);
            this.spacerS.Name = "spacerS";
            this.spacerS.Size = new System.Drawing.Size(1242, 8);
            this.spacerS.TabIndex = 1;
            // 
            // spacerO
            // 
            this.spacerO.Dock = System.Windows.Forms.DockStyle.Top;
            this.spacerO.Location = new System.Drawing.Point(14, 40);
            this.spacerO.Name = "spacerO";
            this.spacerO.Size = new System.Drawing.Size(1242, 8);
            this.spacerO.TabIndex = 1;
            // 
            // tabControl
            // 
            this.tabControl.Controls.Add(this.tabPageForecast);
            this.tabControl.Controls.Add(this.tabPageStock);
            this.tabControl.Controls.Add(this.tabPageOrder);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Font = new System.Drawing.Font("Segoe UI", 10.5F);
            this.tabControl.ItemSize = new System.Drawing.Size(140, 28);
            this.tabControl.Location = new System.Drawing.Point(0, 0);
            this.tabControl.Name = "tabControl";
            this.tabControl.Padding = new System.Drawing.Point(22, 6);
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(1280, 720);
            this.tabControl.TabIndex = 0;
            // 
            // tabPageForecast
            // 
            this.tabPageForecast.BackColor = System.Drawing.Color.Transparent;
            this.tabPageForecast.Controls.Add(this.gridForecast);
            this.tabPageForecast.Controls.Add(this.headerForecast);
            this.tabPageForecast.Location = new System.Drawing.Point(4, 32);
            this.tabPageForecast.Name = "tabPageForecast";
            this.tabPageForecast.Size = new System.Drawing.Size(1272, 684);
            this.tabPageForecast.TabIndex = 0;
            this.tabPageForecast.Text = "Forecast File";
            // 
            // gridForecast
            // 
            this.gridForecast.BackColor = System.Drawing.Color.Transparent;
            this.gridForecast.ColumnCount = 2;
            this.gridForecast.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.gridForecast.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.gridForecast.Controls.Add(this.cardF1, 0, 0);
            this.gridForecast.Controls.Add(this.cardF2, 1, 0);
            this.gridForecast.Controls.Add(this.cardF3, 0, 1);
            this.gridForecast.Controls.Add(this.cardF4, 1, 1);
            this.gridForecast.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridForecast.Location = new System.Drawing.Point(0, 72);
            this.gridForecast.Name = "gridForecast";
            this.gridForecast.Padding = new System.Windows.Forms.Padding(16);
            this.gridForecast.RowCount = 2;
            this.gridForecast.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.gridForecast.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.gridForecast.Size = new System.Drawing.Size(1272, 612);
            this.gridForecast.TabIndex = 0;
            // 
            // cardF1
            // 
            this.cardF1.BackColor = System.Drawing.Color.White;
            this.cardF1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.cardF1.Controls.Add(this.dataGridView1);
            this.cardF1.Controls.Add(this.spacerF1);
            this.cardF1.Controls.Add(this.lblDGV1);
            this.cardF1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardF1.Location = new System.Drawing.Point(19, 19);
            this.cardF1.Name = "cardF1";
            this.cardF1.Padding = new System.Windows.Forms.Padding(14);
            this.cardF1.Size = new System.Drawing.Size(614, 284);
            this.cardF1.TabIndex = 0;
            // 
            // dataGridView1
            // 
            this.dataGridView1.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridView1.BackgroundColor = System.Drawing.Color.White;
            this.dataGridView1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView1.Location = new System.Drawing.Point(14, 48);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersVisible = false;
            this.dataGridView1.Size = new System.Drawing.Size(584, 220);
            this.dataGridView1.TabIndex = 0;
            // 
            // lblDGV1
            // 
            this.lblDGV1.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblDGV1.Font = new System.Drawing.Font("Segoe UI Semibold", 11F, System.Drawing.FontStyle.Bold);
            this.lblDGV1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(55)))), ((int)(((byte)(72)))));
            this.lblDGV1.Location = new System.Drawing.Point(14, 14);
            this.lblDGV1.Name = "lblDGV1";
            this.lblDGV1.Size = new System.Drawing.Size(584, 26);
            this.lblDGV1.TabIndex = 2;
            this.lblDGV1.Text = "Forecast 1 Data";
            // 
            // cardF2
            // 
            this.cardF2.BackColor = System.Drawing.Color.White;
            this.cardF2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.cardF2.Controls.Add(this.dataGridView2);
            this.cardF2.Controls.Add(this.spacerF2);
            this.cardF2.Controls.Add(this.lblDGV2);
            this.cardF2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardF2.Location = new System.Drawing.Point(639, 19);
            this.cardF2.Name = "cardF2";
            this.cardF2.Padding = new System.Windows.Forms.Padding(14);
            this.cardF2.Size = new System.Drawing.Size(614, 284);
            this.cardF2.TabIndex = 1;
            // 
            // dataGridView2
            // 
            this.dataGridView2.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridView2.BackgroundColor = System.Drawing.Color.White;
            this.dataGridView2.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dataGridView2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView2.Location = new System.Drawing.Point(14, 48);
            this.dataGridView2.Name = "dataGridView2";
            this.dataGridView2.RowHeadersVisible = false;
            this.dataGridView2.Size = new System.Drawing.Size(584, 220);
            this.dataGridView2.TabIndex = 0;
            // 
            // lblDGV2
            // 
            this.lblDGV2.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblDGV2.Font = new System.Drawing.Font("Segoe UI Semibold", 11F, System.Drawing.FontStyle.Bold);
            this.lblDGV2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(55)))), ((int)(((byte)(72)))));
            this.lblDGV2.Location = new System.Drawing.Point(14, 14);
            this.lblDGV2.Name = "lblDGV2";
            this.lblDGV2.Size = new System.Drawing.Size(584, 26);
            this.lblDGV2.TabIndex = 2;
            this.lblDGV2.Text = "Forecast 2 Data";
            // 
            // cardF3
            // 
            this.cardF3.BackColor = System.Drawing.Color.White;
            this.cardF3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.cardF3.Controls.Add(this.dataGridView3);
            this.cardF3.Controls.Add(this.spacerF3);
            this.cardF3.Controls.Add(this.lblDGV3);
            this.cardF3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardF3.Location = new System.Drawing.Point(19, 309);
            this.cardF3.Name = "cardF3";
            this.cardF3.Padding = new System.Windows.Forms.Padding(14);
            this.cardF3.Size = new System.Drawing.Size(614, 284);
            this.cardF3.TabIndex = 2;
            // 
            // dataGridView3
            // 
            this.dataGridView3.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridView3.BackgroundColor = System.Drawing.Color.White;
            this.dataGridView3.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dataGridView3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView3.Location = new System.Drawing.Point(14, 48);
            this.dataGridView3.Name = "dataGridView3";
            this.dataGridView3.RowHeadersVisible = false;
            this.dataGridView3.Size = new System.Drawing.Size(584, 220);
            this.dataGridView3.TabIndex = 0;
            // 
            // lblDGV3
            // 
            this.lblDGV3.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblDGV3.Font = new System.Drawing.Font("Segoe UI Semibold", 11F, System.Drawing.FontStyle.Bold);
            this.lblDGV3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(55)))), ((int)(((byte)(72)))));
            this.lblDGV3.Location = new System.Drawing.Point(14, 14);
            this.lblDGV3.Name = "lblDGV3";
            this.lblDGV3.Size = new System.Drawing.Size(584, 26);
            this.lblDGV3.TabIndex = 2;
            this.lblDGV3.Text = "Forecast 3 Data";
            // 
            // cardF4
            // 
            this.cardF4.BackColor = System.Drawing.Color.White;
            this.cardF4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.cardF4.Controls.Add(this.dataGridView4);
            this.cardF4.Controls.Add(this.spacerF4);
            this.cardF4.Controls.Add(this.lblDGV4);
            this.cardF4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardF4.Location = new System.Drawing.Point(639, 309);
            this.cardF4.Name = "cardF4";
            this.cardF4.Padding = new System.Windows.Forms.Padding(14);
            this.cardF4.Size = new System.Drawing.Size(614, 284);
            this.cardF4.TabIndex = 3;
            // 
            // dataGridView4
            // 
            this.dataGridView4.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridView4.BackgroundColor = System.Drawing.Color.White;
            this.dataGridView4.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dataGridView4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView4.Location = new System.Drawing.Point(14, 48);
            this.dataGridView4.Name = "dataGridView4";
            this.dataGridView4.RowHeadersVisible = false;
            this.dataGridView4.Size = new System.Drawing.Size(584, 220);
            this.dataGridView4.TabIndex = 0;
            // 
            // lblDGV4
            // 
            this.lblDGV4.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblDGV4.Font = new System.Drawing.Font("Segoe UI Semibold", 11F, System.Drawing.FontStyle.Bold);
            this.lblDGV4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(55)))), ((int)(((byte)(72)))));
            this.lblDGV4.Location = new System.Drawing.Point(14, 14);
            this.lblDGV4.Name = "lblDGV4";
            this.lblDGV4.Size = new System.Drawing.Size(584, 26);
            this.lblDGV4.TabIndex = 2;
            this.lblDGV4.Text = "Forecast 4 Data";
            // 
            // headerForecast
            // 
            this.headerForecast.Controls.Add(this.headerFLayout);
            this.headerForecast.Dock = System.Windows.Forms.DockStyle.Top;
            this.headerForecast.Location = new System.Drawing.Point(0, 0);
            this.headerForecast.Name = "headerForecast";
            this.headerForecast.Padding = new System.Windows.Forms.Padding(16, 16, 16, 8);
            this.headerForecast.Size = new System.Drawing.Size(1272, 72);
            this.headerForecast.TabIndex = 1;
            // 
            // headerFLayout
            // 
            this.headerFLayout.Controls.Add(this.spacerHeaderF1);
            this.headerFLayout.Controls.Add(this.btnBrowseForecast);
            this.headerFLayout.Controls.Add(this.spacerHeaderF2);
            this.headerFLayout.Controls.Add(this.progressBar1);
            this.headerFLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headerFLayout.Location = new System.Drawing.Point(16, 16);
            this.headerFLayout.Name = "headerFLayout";
            this.headerFLayout.Size = new System.Drawing.Size(1240, 48);
            this.headerFLayout.TabIndex = 0;
            this.headerFLayout.WrapContents = false;
            // 
            // btnBrowseForecast
            // 
            this.btnBrowseForecast.AutoSize = true;
            this.btnBrowseForecast.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(126)))), ((int)(((byte)(255)))));
            this.btnBrowseForecast.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBrowseForecast.FlatAppearance.BorderSize = 0;
            this.btnBrowseForecast.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBrowseForecast.ForeColor = System.Drawing.Color.White;
            this.btnBrowseForecast.Location = new System.Drawing.Point(11, 3);
            this.btnBrowseForecast.Name = "btnBrowseForecast";
            this.btnBrowseForecast.Size = new System.Drawing.Size(142, 38);
            this.btnBrowseForecast.TabIndex = 2;
            this.btnBrowseForecast.Text = "Browse Forecast File";
            this.btnBrowseForecast.UseVisualStyleBackColor = false;
            this.btnBrowseForecast.Click += new System.EventHandler(this.btnBrowseForecast_Click);
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(167, 3);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(160, 38);
            this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar1.TabIndex = 4;
            this.progressBar1.Visible = false;
            // 
            // tabPageStock
            // 
            this.tabPageStock.BackColor = System.Drawing.Color.Transparent;
            this.tabPageStock.Controls.Add(this.cardStock);
            this.tabPageStock.Controls.Add(this.headerStock);
            this.tabPageStock.Location = new System.Drawing.Point(4, 32);
            this.tabPageStock.Name = "tabPageStock";
            this.tabPageStock.Size = new System.Drawing.Size(1272, 684);
            this.tabPageStock.TabIndex = 1;
            this.tabPageStock.Text = "Stock File";
            // 
            // cardStock
            // 
            this.cardStock.BackColor = System.Drawing.Color.White;
            this.cardStock.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.cardStock.Controls.Add(this.dataGridViewStock);
            this.cardStock.Controls.Add(this.spacerS);
            this.cardStock.Controls.Add(this.lblStock);
            this.cardStock.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardStock.Location = new System.Drawing.Point(0, 72);
            this.cardStock.Name = "cardStock";
            this.cardStock.Padding = new System.Windows.Forms.Padding(14);
            this.cardStock.Size = new System.Drawing.Size(1272, 612);
            this.cardStock.TabIndex = 0;
            // 
            // dataGridViewStock
            // 
            this.dataGridViewStock.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridViewStock.BackgroundColor = System.Drawing.Color.White;
            this.dataGridViewStock.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dataGridViewStock.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridViewStock.Location = new System.Drawing.Point(14, 48);
            this.dataGridViewStock.Name = "dataGridViewStock";
            this.dataGridViewStock.RowHeadersVisible = false;
            this.dataGridViewStock.Size = new System.Drawing.Size(1242, 548);
            this.dataGridViewStock.TabIndex = 0;
            // 
            // lblStock
            // 
            this.lblStock.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblStock.Font = new System.Drawing.Font("Segoe UI Semibold", 11F, System.Drawing.FontStyle.Bold);
            this.lblStock.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(55)))), ((int)(((byte)(72)))));
            this.lblStock.Location = new System.Drawing.Point(14, 14);
            this.lblStock.Name = "lblStock";
            this.lblStock.Size = new System.Drawing.Size(1242, 26);
            this.lblStock.TabIndex = 2;
            this.lblStock.Text = "Stock Data";
            // 
            // headerStock
            // 
            this.headerStock.Controls.Add(this.headerSLayout);
            this.headerStock.Dock = System.Windows.Forms.DockStyle.Top;
            this.headerStock.Location = new System.Drawing.Point(0, 0);
            this.headerStock.Name = "headerStock";
            this.headerStock.Padding = new System.Windows.Forms.Padding(16, 16, 16, 8);
            this.headerStock.Size = new System.Drawing.Size(1272, 72);
            this.headerStock.TabIndex = 1;
            // 
            // headerSLayout
            // 
            this.headerSLayout.Controls.Add(this.btnBrowseStock);
            this.headerSLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headerSLayout.Location = new System.Drawing.Point(16, 16);
            this.headerSLayout.Name = "headerSLayout";
            this.headerSLayout.Size = new System.Drawing.Size(1240, 48);
            this.headerSLayout.TabIndex = 0;
            // 
            // btnBrowseStock
            // 
            this.btnBrowseStock.AutoSize = true;
            this.btnBrowseStock.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(126)))), ((int)(((byte)(255)))));
            this.btnBrowseStock.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBrowseStock.FlatAppearance.BorderSize = 0;
            this.btnBrowseStock.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBrowseStock.ForeColor = System.Drawing.Color.White;
            this.btnBrowseStock.Location = new System.Drawing.Point(3, 3);
            this.btnBrowseStock.Name = "btnBrowseStock";
            this.btnBrowseStock.Size = new System.Drawing.Size(124, 38);
            this.btnBrowseStock.TabIndex = 0;
            this.btnBrowseStock.Text = "Browse Stock File";
            this.btnBrowseStock.UseVisualStyleBackColor = false;
            this.btnBrowseStock.Click += new System.EventHandler(this.btnBrowseStock_Click);
            // 
            // tabPageOrder
            // 
            this.tabPageOrder.BackColor = System.Drawing.Color.Transparent;
            this.tabPageOrder.Controls.Add(this.cardOrder);
            this.tabPageOrder.Controls.Add(this.headerOrder);
            this.tabPageOrder.Location = new System.Drawing.Point(4, 32);
            this.tabPageOrder.Name = "tabPageOrder";
            this.tabPageOrder.Size = new System.Drawing.Size(1272, 684);
            this.tabPageOrder.TabIndex = 2;
            this.tabPageOrder.Text = "Order File";
            // 
            // cardOrder
            // 
            this.cardOrder.BackColor = System.Drawing.Color.White;
            this.cardOrder.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.cardOrder.Controls.Add(this.dataGridViewOrder);
            this.cardOrder.Controls.Add(this.spacerO);
            this.cardOrder.Controls.Add(this.lblOrder);
            this.cardOrder.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardOrder.Location = new System.Drawing.Point(0, 72);
            this.cardOrder.Name = "cardOrder";
            this.cardOrder.Padding = new System.Windows.Forms.Padding(14);
            this.cardOrder.Size = new System.Drawing.Size(1272, 612);
            this.cardOrder.TabIndex = 0;
            // 
            // dataGridViewOrder
            // 
            this.dataGridViewOrder.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridViewOrder.BackgroundColor = System.Drawing.Color.White;
            this.dataGridViewOrder.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dataGridViewOrder.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridViewOrder.Location = new System.Drawing.Point(14, 48);
            this.dataGridViewOrder.Name = "dataGridViewOrder";
            this.dataGridViewOrder.RowHeadersVisible = false;
            this.dataGridViewOrder.Size = new System.Drawing.Size(1242, 548);
            this.dataGridViewOrder.TabIndex = 0;
            // 
            // lblOrder
            // 
            this.lblOrder.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblOrder.Font = new System.Drawing.Font("Segoe UI Semibold", 11F, System.Drawing.FontStyle.Bold);
            this.lblOrder.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(55)))), ((int)(((byte)(72)))));
            this.lblOrder.Location = new System.Drawing.Point(14, 14);
            this.lblOrder.Name = "lblOrder";
            this.lblOrder.Size = new System.Drawing.Size(1242, 26);
            this.lblOrder.TabIndex = 2;
            this.lblOrder.Text = "Order Data";
            // 
            // headerOrder
            // 
            this.headerOrder.Controls.Add(this.headerOLayout);
            this.headerOrder.Dock = System.Windows.Forms.DockStyle.Top;
            this.headerOrder.Location = new System.Drawing.Point(0, 0);
            this.headerOrder.Name = "headerOrder";
            this.headerOrder.Padding = new System.Windows.Forms.Padding(16, 16, 16, 8);
            this.headerOrder.Size = new System.Drawing.Size(1272, 72);
            this.headerOrder.TabIndex = 1;
            // 
            // headerOLayout
            // 
            this.headerOLayout.Controls.Add(this.btnBrowseOrder);
            this.headerOLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.headerOLayout.Location = new System.Drawing.Point(16, 16);
            this.headerOLayout.Name = "headerOLayout";
            this.headerOLayout.Size = new System.Drawing.Size(1240, 48);
            this.headerOLayout.TabIndex = 0;
            // 
            // btnBrowseOrder
            // 
            this.btnBrowseOrder.AutoSize = true;
            this.btnBrowseOrder.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(126)))), ((int)(((byte)(255)))));
            this.btnBrowseOrder.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBrowseOrder.FlatAppearance.BorderSize = 0;
            this.btnBrowseOrder.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBrowseOrder.ForeColor = System.Drawing.Color.White;
            this.btnBrowseOrder.Location = new System.Drawing.Point(3, 3);
            this.btnBrowseOrder.Name = "btnBrowseOrder";
            this.btnBrowseOrder.Size = new System.Drawing.Size(127, 38);
            this.btnBrowseOrder.TabIndex = 0;
            this.btnBrowseOrder.Text = "Browse Order File";
            this.btnBrowseOrder.UseVisualStyleBackColor = false;
            this.btnBrowseOrder.Click += new System.EventHandler(this.btnBrowseOrder_Click);
            // 
            // UploadForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(253)))));
            this.ClientSize = new System.Drawing.Size(1280, 720);
            this.Controls.Add(this.tabControl);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.MinimumSize = new System.Drawing.Size(1000, 640);
            this.Name = "UploadForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Upload Form";
            this.tabControl.ResumeLayout(false);
            this.tabPageForecast.ResumeLayout(false);
            this.gridForecast.ResumeLayout(false);
            this.cardF1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.cardF2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView2)).EndInit();
            this.cardF3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView3)).EndInit();
            this.cardF4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView4)).EndInit();
            this.headerForecast.ResumeLayout(false);
            this.headerFLayout.ResumeLayout(false);
            this.headerFLayout.PerformLayout();
            this.tabPageStock.ResumeLayout(false);
            this.cardStock.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewStock)).EndInit();
            this.headerStock.ResumeLayout(false);
            this.headerSLayout.ResumeLayout(false);
            this.headerSLayout.PerformLayout();
            this.tabPageOrder.ResumeLayout(false);
            this.cardOrder.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewOrder)).EndInit();
            this.headerOrder.ResumeLayout(false);
            this.headerOLayout.ResumeLayout(false);
            this.headerOLayout.PerformLayout();
            this.ResumeLayout(false);

        }

        private Panel spacerHeaderF1;
        private Panel spacerHeaderF2;
        private Panel spacerF1;
        private Panel spacerF2;
        private Panel spacerF3;
        private Panel spacerF4;
        private Panel spacerS;
        private Panel spacerO;
    }
}
