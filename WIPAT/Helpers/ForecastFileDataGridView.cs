using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WIPAT
{
    public partial class ForecastFileDataGridView : UserControl
    {
        public DataGridView Grid { get; private set; }

        public ForecastFileDataGridView()
        {
            InitializeComponent();

            // Initialize the DataGridView control
            Grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToOrderColumns = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells
            };

            this.Controls.Add(Grid);

            // Apply styles
            StyleDataGridView(Grid);
        }

        private void StyleDataGridView(DataGridView dgv)
        {
            // General appearance settings
            dgv.BackgroundColor = Color.White; // Set background color of DataGridView
            dgv.BorderStyle = BorderStyle.None; // Remove borders around DataGridView
            dgv.EnableHeadersVisualStyles = false; // Disable default header style for customization

            // Row settings
            dgv.DefaultCellStyle.BackColor = Color.White; // Set background color for data rows
            dgv.DefaultCellStyle.ForeColor = Color.Black; // Set font color for data rows
            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 8); // Set font style and size
            dgv.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter; // Align text to center

            // Header settings
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(0, 123, 255); // Header background color
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White; // Header text color
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold); // Header font
            dgv.ColumnHeadersHeight = 40; // Increase header row height for a better look

            // Alternating rows for better readability
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240); // Light grey for alternating rows
            dgv.AlternatingRowsDefaultCellStyle.Font = new Font("Segoe UI", 8); // Keep font consistent

            // Hover effect (Highlight the row on mouse hover)
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 123, 255); // Hover row background color
            dgv.DefaultCellStyle.SelectionForeColor = Color.White; // Hover row font color
        }
    }
}
