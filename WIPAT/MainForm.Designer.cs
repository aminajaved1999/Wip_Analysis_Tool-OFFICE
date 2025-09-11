using System.Windows.Forms;

namespace WIPAT
{
    partial class MainForm
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
            this.stepperPanel = new System.Windows.Forms.Panel();
            this.step5 = new System.Windows.Forms.Label();
            this.step4 = new System.Windows.Forms.Label();
            this.step3 = new System.Windows.Forms.Label();
            this.step2 = new System.Windows.Forms.Label();
            this.step1 = new System.Windows.Forms.Label();
            this.mainPanel = new System.Windows.Forms.Panel();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.viewMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.itemsCatalogueMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addItemsToCatalogueToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.stepperPanel.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.menuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // stepperPanel
            // 
            this.stepperPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(64)))));
            this.stepperPanel.Controls.Add(this.step5);
            this.stepperPanel.Controls.Add(this.step4);
            this.stepperPanel.Controls.Add(this.step3);
            this.stepperPanel.Controls.Add(this.step2);
            this.stepperPanel.Controls.Add(this.step1);
            this.stepperPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.stepperPanel.Location = new System.Drawing.Point(0, 25);
            this.stepperPanel.Name = "stepperPanel";
            this.stepperPanel.Size = new System.Drawing.Size(1200, 70);
            this.stepperPanel.TabIndex = 0;
            // 
            // step5
            // 
            this.step5.Cursor = System.Windows.Forms.Cursors.Hand;
            this.step5.Dock = System.Windows.Forms.DockStyle.Left;
            this.step5.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.step5.ForeColor = System.Drawing.Color.LightGray;
            this.step5.Location = new System.Drawing.Point(800, 0);
            this.step5.Name = "step5";
            this.step5.Size = new System.Drawing.Size(200, 70);
            this.step5.TabIndex = 4;
            this.step5.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // step4
            // 
            this.step4.Cursor = System.Windows.Forms.Cursors.Hand;
            this.step4.Dock = System.Windows.Forms.DockStyle.Left;
            this.step4.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.step4.ForeColor = System.Drawing.Color.LightGray;
            this.step4.Location = new System.Drawing.Point(600, 0);
            this.step4.Name = "step4";
            this.step4.Size = new System.Drawing.Size(200, 70);
            this.step4.TabIndex = 3;
            this.step4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // step3
            // 
            this.step3.Cursor = System.Windows.Forms.Cursors.Hand;
            this.step3.Dock = System.Windows.Forms.DockStyle.Left;
            this.step3.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.step3.ForeColor = System.Drawing.Color.LightGray;
            this.step3.Location = new System.Drawing.Point(400, 0);
            this.step3.Name = "step3";
            this.step3.Size = new System.Drawing.Size(200, 70);
            this.step3.TabIndex = 2;
            this.step3.Text = "3. Export";
            this.step3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // step2
            // 
            this.step2.Cursor = System.Windows.Forms.Cursors.Hand;
            this.step2.Dock = System.Windows.Forms.DockStyle.Left;
            this.step2.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.step2.ForeColor = System.Drawing.Color.LightGray;
            this.step2.Location = new System.Drawing.Point(200, 0);
            this.step2.Name = "step2";
            this.step2.Size = new System.Drawing.Size(200, 70);
            this.step2.TabIndex = 1;
            this.step2.Text = "2. Calculate Wip";
            this.step2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // step1
            // 
            this.step1.Cursor = System.Windows.Forms.Cursors.Hand;
            this.step1.Dock = System.Windows.Forms.DockStyle.Left;
            this.step1.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.step1.ForeColor = System.Drawing.Color.LightGray;
            this.step1.Location = new System.Drawing.Point(0, 0);
            this.step1.Name = "step1";
            this.step1.Size = new System.Drawing.Size(200, 70);
            this.step1.TabIndex = 0;
            this.step1.Text = "1. Upload";
            this.step1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // mainPanel
            // 
            this.mainPanel.AutoScroll = true;
            this.mainPanel.BackColor = System.Drawing.Color.White;
            this.mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainPanel.Location = new System.Drawing.Point(0, 95);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Size = new System.Drawing.Size(1200, 583);
            this.mainPanel.TabIndex = 1;
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusLabel});
            this.statusStrip.Location = new System.Drawing.Point(0, 678);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(1200, 22);
            this.statusStrip.TabIndex = 2;
            // 
            // statusLabel
            // 
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(44, 17);
            this.statusLabel.Text = "Ready";
            // 
            // menuStrip
            // 
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.viewMenuItem});
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(1200, 25);
            this.menuStrip.TabIndex = 0;
            // 
            // viewMenuItem
            // 
            this.viewMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.itemsCatalogueMenuItem,
            this.addItemsToCatalogueToolStripMenuItem});
            this.viewMenuItem.Name = "viewMenuItem";
            this.viewMenuItem.Size = new System.Drawing.Size(47, 21);
            this.viewMenuItem.Text = "View";
            // 
            // itemsCatalogueMenuItem
            // 
            this.itemsCatalogueMenuItem.Name = "itemsCatalogueMenuItem";
            this.itemsCatalogueMenuItem.Size = new System.Drawing.Size(180, 22);
            this.itemsCatalogueMenuItem.Text = "Items Catalogue";
            this.itemsCatalogueMenuItem.Click += new System.EventHandler(this.ItemsCatalogueMenuItem_Click);
            // 
            // addItemsToCatalogueToolStripMenuItem
            // 
            this.addItemsToCatalogueToolStripMenuItem.Name = "addItemsToCatalogueToolStripMenuItem";
            this.addItemsToCatalogueToolStripMenuItem.Size = new System.Drawing.Size(214, 22);
            this.addItemsToCatalogueToolStripMenuItem.Text = "Add Items to Catalogue";
            this.addItemsToCatalogueToolStripMenuItem.Click += new System.EventHandler(this.addItemsToCatalogueToolStripMenuItem_Click);
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(1200, 700);
            this.Controls.Add(this.mainPanel);
            this.Controls.Add(this.stepperPanel);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.menuStrip);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "WIP Analysis Tool";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.stepperPanel.ResumeLayout(false);
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        private System.Windows.Forms.Panel stepperPanel;
        private System.Windows.Forms.Label step1;
        private System.Windows.Forms.Label step2;
        private System.Windows.Forms.Label step3;
        private System.Windows.Forms.Label step4;
        private System.Windows.Forms.Label step5;
        private System.Windows.Forms.Panel mainPanel;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel;
        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem viewMenuItem;
        private System.Windows.Forms.ToolStripMenuItem itemsCatalogueMenuItem;
        private ToolStripMenuItem addItemsToCatalogueToolStripMenuItem;
    }
}
