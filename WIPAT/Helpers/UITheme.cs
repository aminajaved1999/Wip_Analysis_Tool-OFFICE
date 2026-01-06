using System.Drawing;
using System.Windows.Forms;

namespace WIPAT.Helpers
{
    public static class UITheme
    {
        // --- PALETTE ---
        // Microsoft Blue (Used for Headers, Primary Actions, Active Elements)
        public static readonly Color PrimaryBlue = Color.FromArgb(0, 120, 212);
        // Darker Blue (Used for Hover states)
        public static readonly Color PrimaryDark = Color.FromArgb(0, 90, 158);
        // Success Green (Used for Approve/Save)
        public static readonly Color AccentGreen = Color.FromArgb(46, 125, 50);

        // Backgrounds
        public static readonly Color BackgroundCanvas = Color.FromArgb(240, 244, 249); // Modern Light Gray
        public static readonly Color SurfaceWhite = Color.White; // Cards/Grids

        // Text
        public static readonly Color TextDark = Color.FromArgb(64, 64, 64);
        public static readonly Color TextLight = Color.White;
        public static readonly Color TextGray = Color.Gray;

        // --- HELPER METHODS ---

        public static void ApplyButtonTheme(Button btn, bool isPrimary = true)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Cursor = Cursors.Hand;
            btn.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);

            if (isPrimary)
            {
                btn.BackColor = PrimaryBlue;
                btn.ForeColor = TextLight;
                btn.MouseEnter += (s, e) => btn.BackColor = PrimaryDark;
                btn.MouseLeave += (s, e) => btn.BackColor = PrimaryBlue;
            }
            else
            {
                // Green / Success Action
                btn.BackColor = AccentGreen;
                btn.ForeColor = TextLight;
                btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(27, 94, 32);
                btn.MouseLeave += (s, e) => btn.BackColor = AccentGreen;
            }
        }
            
        public static void StyleGrid(DataGridView dgv, bool isError = false)
        {
            dgv.BackgroundColor = SurfaceWhite;
            dgv.BorderStyle = BorderStyle.None;
            dgv.EnableHeadersVisualStyles = false;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.GridColor = Color.FromArgb(240, 240, 240);

            // Headers
            dgv.ColumnHeadersDefaultCellStyle.BackColor = isError ? Color.FromArgb(220, 53, 69) : PrimaryBlue;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = TextLight;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgv.ColumnHeadersHeight = 45;

            // Rows
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(232, 240, 254);
            dgv.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 9);
            dgv.RowTemplate.Height = 35;
        }
    }
}