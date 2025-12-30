using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing; // Added for Point/Padding
using System.Threading.Tasks;
using System.Windows.Forms;
using OfficeOpenXml;
using WIPAT.Entities;
using WIPAT.Entities.Enum;

namespace WIPAT
{
    public partial class ExportForm : Form
    {
        private readonly WipSession _session;
        private readonly Action<string, StatusType> _setStatus;

        private Button btnExport;
        private DataGridView previewGrid;
        private Label lblHint;

        // -- NEW CONTROLS FOR SEARCHING --
        private Panel pnlSearch;
        private Label lblSearch;
        private TextBox txtSearchAsin;
        private BindingSource _bindingSource;

        public ExportForm(WipSession session, Action<string, StatusType> setStatus)
        {
            InitializeComponent();
            _session = session;
            _setStatus = setStatus;
            _bindingSource = new BindingSource(); // Initialize BindingSource

            // UI
            Text = "Export WIP";
            Width = 900;
            Height = 600;
            StartPosition = FormStartPosition.CenterParent;

            // 1. Hint Label (Top)
            lblHint = new Label
            {
                Dock = DockStyle.Top,
                Height = 40,
                Text = "Review the data below (optional), then click Export to save an Excel file.",
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };

            // 2. Search Panel (Top, below Hint)
            pnlSearch = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(5)
            };

            lblSearch = new Label
            {
                Text = "Search C-ASIN:",
                AutoSize = true,
                Location = new Point(10, 12)
            };

            txtSearchAsin = new TextBox
            {
                Location = new Point(110, 8),
                Width = 200
            };
            // Event to trigger filter when typing
            txtSearchAsin.TextChanged += TxtSearchAsin_TextChanged;

            pnlSearch.Controls.Add(lblSearch);
            pnlSearch.Controls.Add(txtSearchAsin);

            // 3. Grid (Fill)
            previewGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            // 4. Export Button (Bottom)
            btnExport = new Button
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                Text = "Export to Excel (.xlsx)"
            };
            btnExport.Click += async (s, e) => await ExportAsync();

            // Add controls (Order matters for Docking)
            // Add Fill content first, then docked edges
            Controls.Add(previewGrid);
            Controls.Add(pnlSearch);    // Second Top
            Controls.Add(lblHint);      // First Top
            Controls.Add(btnExport);    // Bottom

            Load += ExportForm_Load;
        }

        private void SetStatus(string msg, StatusType type) => _setStatus?.Invoke(msg, type);

        private void ExportForm_Load(object sender, EventArgs e)
        {
            // Guards
            if (!string.Equals(_session.WipStatus, WipStatus.Approved.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                SetStatus("Please Approve the WIP before exporting.", StatusType.Error);
                btnExport.Enabled = false;
            }

            if (_session.FinalDataTable == null || _session.FinalDataTable.Rows.Count == 0)
            {
                SetStatus("No data available to export.", StatusType.Error);
                btnExport.Enabled = false;
            }

            // -- CHANGED: Bind Data via BindingSource to enable filtering --
            if (_session.FinalDataTable != null)
            {
                _bindingSource.DataSource = _session.FinalDataTable;
                previewGrid.DataSource = _bindingSource;
            }
        }

        // -- NEW: Search Logic --
        private void TxtSearchAsin_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (_session.FinalDataTable == null) return;

                string searchValue = txtSearchAsin.Text.Trim();

                // Sanitize input to prevent injection errors in RowFilter (escape single quotes)
                searchValue = searchValue.Replace("'", "''");

                // Assuming the column name in DataTable is exactly "C-ASIN"
                // [ ] brackets are required for column names with hyphens
                if (string.IsNullOrEmpty(searchValue))
                {
                    _bindingSource.RemoveFilter();
                }
                else
                {
                    _bindingSource.Filter = string.Format("[C-ASIN] LIKE '%{0}%'", searchValue);
                }
            }
            catch (Exception ex)
            {
                // Silently fail or log if column doesn't exist to prevent crash while typing
                System.Diagnostics.Debug.WriteLine("Filter Error: " + ex.Message);
            }
        }

        private static string DetermineWipColumn(DataTable dataTable)
        {
            if (dataTable.Columns.Contains("CasePack_Wip"))
                return "CasePack_Wip";
            if (dataTable.Columns.Contains("MOQ_Wip"))
                return "MOQ_Wip";
            if (dataTable.Columns.Contains("Review_Wip"))
                return "Review_Wip";
            return string.Empty;
        }

        private async Task ExportAsync()
        {
            try
            {
                var dt = _session.FinalDataTable;
                if (dt == null || dt.Rows.Count == 0)
                {
                    SetStatus("No data to export.", StatusType.Error);
                    return;
                }

                if (!string.Equals(_session.WipStatus, WipStatus.Approved.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    SetStatus("Please Approve the WIP to Download.", StatusType.Error);
                    return;
                }

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                var parts = (_session.CurrentMonthWithYear ?? "").Split(' ');
                if (parts.Length < 2)
                {
                    SetStatus("Invalid current month/year format.", StatusType.Error);
                    return;
                }
                string currentForecastMonth = parts[0];
                string currentForecastYear = parts[1];

                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("WIP Data");

                    string[] headers = new[]
                    { "C-ASIN", "Month", "Year", "WIP Quantity", "CommitmentPeriod", "Issued Month", "Issued Year", "WIP Type" };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        var cell = ws.Cells[1, i + 1];
                        cell.Value = headers[i];
                        cell.Style.Font.Bold = true;
                    }

                    int row = 2;

                    var requiredColumns = new List<string>
                    {
                        "C-ASIN", $"Requested_Quantity ({_session.Curr.Month})", $"CommitmentPeriod ({_session.Curr.Month})", "PO_Date", "Month", "Year", "Review_Wip"
                    };
                    foreach (var col in requiredColumns)
                    {
                        if (!dt.Columns.Contains(col))
                        {
                            SetStatus($"Missing required column: {col}", StatusType.Error);
                            return;
                        }
                    }

                    string wipCol = DetermineWipColumn(dt);
                    if (string.IsNullOrEmpty(wipCol))
                    {
                        SetStatus("Invalid WIP column in final table.", StatusType.Error);
                        return;
                    }

                    // NOTE: We use dt.Rows (Original Source) for export, 
                    // not the filtered grid view, so export always contains ALL data.
                    // If you want to export only what is filtered, use _bindingSource instead.
                    foreach (DataRow r in dt.Rows)
                    {
                        string asin = r["C-ASIN"]?.ToString();
                        string month = r["Month"]?.ToString();
                        string year = r["Year"]?.ToString();
                        string cpStr = r[$"CommitmentPeriod ({_session.Curr.Month})"]?.ToString() ?? "0";
                        string wipQty = r[wipCol]?.ToString() ?? string.Empty;

                        if (!int.TryParse(cpStr, out int cp)) cp = 0;
                        bool pass = cp >= (_session.CommitmentPeriod + 1);
                        if (!pass) continue;

                        ws.Cells[row, 1].Value = asin;
                        ws.Cells[row, 2].Value = month;
                        ws.Cells[row, 3].Value = year;
                        ws.Cells[row, 4].Value = wipQty;
                        ws.Cells[row, 5].Value = cp.ToString();
                        ws.Cells[row, 6].Value = currentForecastMonth;
                        ws.Cells[row, 7].Value = currentForecastYear;

                        string typeText = _session.WipType ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(_session.WipProcessingType))
                            typeText += $"-{_session.WipProcessingType}";

                        if (dt.Columns.Contains("MOQ") && r["MOQ"] != DBNull.Value)
                        {
                            var moq = r["MOQ"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(moq))
                                typeText += $"-MOQ ({moq})";
                        }

                        if (dt.Columns.Contains("CasePack") && r["CasePack"] != DBNull.Value)
                        {
                            var cpv = r["CasePack"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(cpv))
                                typeText += $"-CasePack ({cpv})";
                        }

                        ws.Cells[row, 8].Value = typeText;

                        row++;
                    }

                    ws.Cells[ws.Dimension.Address].AutoFitColumns();

                    using (var dlg = new SaveFileDialog())
                    {
                        dlg.Filter = "Excel Files (*.xlsx)|*.xlsx";
                        dlg.FileName = $"WipData_{currentForecastMonth}-{currentForecastYear}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

                        if (dlg.ShowDialog() == DialogResult.OK)
                        {
                            var fi = new System.IO.FileInfo(dlg.FileName);
                            await Task.Run(() => package.SaveAs(fi));

                            SetStatus($"WIP Excel file saved to {fi.FullName}", StatusType.Success);
                        }
                        else
                        {
                            SetStatus("File save was canceled by the user.", StatusType.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Export failed: {ex.Message}", StatusType.Error);
            }
        }
    }
}