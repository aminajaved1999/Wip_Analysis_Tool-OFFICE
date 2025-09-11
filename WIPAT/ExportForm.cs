using System;
using System.Collections.Generic;
using System.Data;
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
        private DataGridView previewGrid;   // optional: quick peek at what will export
        private Label lblHint;

        public ExportForm(WipSession session, Action<string, StatusType> setStatus)
        {
            InitializeComponent();
            _session = session;
            _setStatus = setStatus;

            // UI
            Text = "Export WIP";
            Width = 900;
            Height = 600;
            StartPosition = FormStartPosition.CenterParent;

            lblHint = new Label
            {
                Dock = DockStyle.Top,
                Height = 40,
                Text = "Review the data below (optional), then click Export to save an Excel file.",
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };

            previewGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            btnExport = new Button
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                Text = "Export to Excel (.xlsx)"
            };
            btnExport.Click += async (s, e) => await ExportAsync();

            Controls.Add(previewGrid);
            Controls.Add(btnExport);
            Controls.Add(lblHint);

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

            // Optional preview
            previewGrid.DataSource = _session.FinalDataTable;
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
                        "C-ASIN", "Requested_Quantity", "Commitment_Period", "PO_Date", "Month", "Year", "Review_Wip"
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

                    foreach (DataRow r in dt.Rows)
                    {
                        string asin = r["C-ASIN"]?.ToString();
                        string month = r["Month"]?.ToString();
                        string year = r["Year"]?.ToString();
                        string cpStr = r["Commitment_Period"]?.ToString() ?? "0";
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
