using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WIPAT.Forms
{
    public enum CustomMessageBoxIcon
    {
        Success,
        Error,
        Warning,
        Info
    }

    public partial class CustomMessageBox : Form
    {
        public CustomMessageBox(string message, string title, CustomMessageBoxIcon iconType)
        {
            InitializeComponent();

            this.Text = title;
            lblMessage.Text = message;
            picIcon.Image = GenerateIcon(iconType);

            ApplyTheme(iconType);
            AdjustFormSize();
        }

        private void ApplyTheme(CustomMessageBoxIcon iconType)
        {
            // Standard colors
            this.BackColor = Color.White;
            panelBottom.BackColor = Color.FromArgb(245, 246, 250);
            lblMessage.ForeColor = Color.FromArgb(50, 50, 50);

            // Button styling
            btnOK.FlatStyle = FlatStyle.Flat;
            btnOK.FlatAppearance.BorderSize = 0;
            btnOK.ForeColor = Color.White;
            btnOK.Font = new Font("Segoe UI Semibold", 9F);

            // Color coordinate the button with the icon
            switch (iconType)
            {
                case CustomMessageBoxIcon.Success:
                    btnOK.BackColor = Color.FromArgb(40, 167, 69); // Green
                    break;
                case CustomMessageBoxIcon.Error:
                    btnOK.BackColor = Color.FromArgb(220, 53, 69); // Red
                    break;
                case CustomMessageBoxIcon.Warning:
                    btnOK.BackColor = Color.FromArgb(255, 152, 0); // Orange
                    btnOK.ForeColor = Color.White;
                    break;
                case CustomMessageBoxIcon.Info:
                    btnOK.BackColor = Color.FromArgb(0, 120, 215); // Blue
                    break;
            }
        }

        private void AdjustFormSize()
        {
            // Auto-size the form height based on the text height
            int padding = 20;
            int textHeight = lblMessage.Height;
            int iconHeight = picIcon.Height;

            int contentHeight = Math.Max(textHeight, iconHeight);

            // Add padding top, content, padding bottom, and the bottom panel
            int requiredFormHeight = padding + contentHeight + padding + panelBottom.Height + 40; // 40 for title bar

            // Ensure a minimum height so it doesn't look squished
            this.Height = Math.Max(180, requiredFormHeight);
        }

        // Draws the icons dynamically so no external image resources are needed
        private Image GenerateIcon(CustomMessageBoxIcon type)
        {
            Bitmap bmp = new Bitmap(48, 48);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Pen whitePen = new Pen(Color.White, 4) { StartCap = LineCap.Round, EndCap = LineCap.Round };

                switch (type)
                {
                    case CustomMessageBoxIcon.Error:
                        g.FillEllipse(new SolidBrush(Color.FromArgb(220, 53, 69)), 2, 2, 44, 44);
                        g.DrawLine(whitePen, 15, 15, 33, 33);
                        g.DrawLine(whitePen, 33, 15, 15, 33);
                        break;

                    case CustomMessageBoxIcon.Success:
                        g.FillEllipse(new SolidBrush(Color.FromArgb(40, 167, 69)), 2, 2, 44, 44);
                        g.DrawLine(whitePen, 14, 24, 21, 31);
                        g.DrawLine(whitePen, 21, 31, 34, 16);
                        break;

                    case CustomMessageBoxIcon.Warning:
                        Point[] triangle = { new Point(24, 4), new Point(4, 42), new Point(44, 42) };
                        g.FillPolygon(new SolidBrush(Color.FromArgb(255, 152, 0)), triangle);
                        g.FillRectangle(Brushes.White, 22, 16, 4, 14);
                        g.FillEllipse(Brushes.White, 22, 34, 4, 4);
                        break;

                    case CustomMessageBoxIcon.Info:
                        g.FillEllipse(new SolidBrush(Color.FromArgb(0, 120, 215)), 2, 2, 44, 44);
                        g.FillRectangle(Brushes.White, 22, 20, 4, 16);
                        g.FillEllipse(Brushes.White, 22, 12, 4, 4);
                        break;
                }
            }
            return bmp;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        // Static helper method to drop-in replace standard MessageBox
        public static DialogResult Show(string message, string title, CustomMessageBoxIcon iconType = CustomMessageBoxIcon.Info)
        {
            using (CustomMessageBox box = new CustomMessageBox(message, title, iconType))
            {
                return box.ShowDialog();
            }
        }
    }
}