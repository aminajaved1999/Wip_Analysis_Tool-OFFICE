using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using WIPAT.Entities;
using WIPAT.Entities.Enum;
using WIPAT.Helpers;
using LicenseContext = OfficeOpenXml.LicenseContext;

namespace WIPAT
{
    public partial class ExportForm : Form
    {
        #region Fields & Dependencies
        private readonly WipSession _session;
        private readonly Action<string, StatusType> _setStatus;
        private readonly User _currentUser;
        private BindingSource _bindingSource;

        private DataTable _filteredDataTable;
        private DataTable _exportDataTable;
        #endregion

        #region Constructor & Initialization
        public ExportForm(WipSession session, Action<string, StatusType> setStatus, User currentUser)
        {
            InitializeComponent();

            _session = session;
            _setStatus = setStatus;
            _currentUser = currentUser;
            _bindingSource = new BindingSource();

            ApplyTheme();

            string targetMonth = _session?.TargetMonth ?? string.Empty;
            this.Text = $"Export WIP Data for {targetMonth}".Trim();
            lblHeaderTitle.Text = $"Export Calculated WIPs for {targetMonth}".Trim();
        }

        private void ApplyTheme()
        {
            this.BackColor = UITheme.BackgroundCanvas;
            pnlHeader.BackColor = UITheme.BackgroundCanvas;
            pnlHeader.ForeColor = UITheme.GridRowText;
            lblHeaderTitle.ForeColor = UITheme.GridRowText;

            UITheme.SetFormIcon(this);
            UITheme.StyleButton(btnResetSort, AppButtonStyle.Refresh);
            UITheme.StyleButton(btnExport, AppButtonStyle.ExportToExcel);
            UITheme.StyleButton(btnSearch, AppButtonStyle.Search);

            if (previewGrid != null)
            {
                UITheme.StyleGrid(previewGrid);
            }
        }
        #endregion

        #region UI Event Handlers
        private void ExportForm_Load(object sender, EventArgs e)
        {
            if (!string.Equals(_session.WipStatus, WipStatus.Approved.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                SetStatus("Please Approve the WIP before exporting.", StatusType.Error);
                btnExport.Enabled = false;
                UITheme.StyleButton(btnExport, AppButtonStyle.Secondary);
            }

            if (_session.FinalDataTable == null || _session.FinalDataTable.Rows.Count == 0)
            {
                SetStatus("No data available to export.", StatusType.Error);
                btnExport.Enabled = false;
                UITheme.StyleButton(btnExport, AppButtonStyle.Secondary);
            }

            if (_session.FinalDataTable != null)
            {
                _filteredDataTable = _session.FinalDataTable.Clone();
                string cpColName = $"CommitmentPeriod ({_session.Curr.Month})";

                if (_session.FinalDataTable.Columns.Contains(cpColName))
                {
                    foreach (DataRow row in _session.FinalDataTable.Rows)
                    {
                        string cpStr = row[cpColName]?.ToString();
                        if (int.TryParse(cpStr, out int cp) && cp == 3)
                        {
                            _filteredDataTable.ImportRow(row);
                        }
                    }
                }

                _exportDataTable = GenerateExportDataTable(_filteredDataTable);

                BindGrid();
            }
        }

        private void BtnResetSort_Click(object sender, EventArgs e)
        {
            if (_bindingSource != null)
            {
                _bindingSource.RemoveSort();
                _bindingSource.RemoveFilter();
                txtSearch.Clear();
                previewGrid.Refresh();
                SetStatus("Sort and filters reset.", StatusType.Success);
            }
        }

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            try
            {
                if (_bindingSource.DataSource == null) return;

                string searchValue = txtSearch.Text.Trim().Replace("'", "''");

                if (string.IsNullOrEmpty(searchValue))
                {
                    _bindingSource.RemoveFilter();
                    SetStatus("Search filter cleared.", StatusType.Success);
                }
                else
                {
                    _bindingSource.Filter = string.Format("[C-ASIN] LIKE '%{0}%'", searchValue);
                    SetStatus($"Filtered by C-ASIN containing: {searchValue}", StatusType.Success);
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"An unexpected error occurred while applying the search filter: {ex.Message}"
                                  + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                                  + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "");
                System.Diagnostics.Debug.WriteLine(errorMsg);
                SetStatus("Error applying search filter.", StatusType.Error);
            }
        }

        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                BtnSearch_Click(sender, e);
            }
        }

        private async void BtnExport_Click(object sender, EventArgs e)
        {
            await ExportAsync();
        }

        private void PreviewGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            var dgv = sender as DataGridView;
            if (dgv != null && e.ColumnIndex >= 0 && e.RowIndex >= 0 && e.Value != null && e.Value != DBNull.Value)
            {
                if (dgv.Columns[e.ColumnIndex].Name == "ItemStatus")
                {
                    string valStr = e.Value.ToString().Trim();

                    if (int.TryParse(valStr, out int statusVal))
                    {
                        switch (statusVal)
                        {
                            case 0:
                                e.Value = "Inactive";
                                e.CellStyle.ForeColor = Color.DarkGray;
                                break;
                            case 1:
                                e.Value = "Active";
                                e.CellStyle.ForeColor = Color.Green;
                                break;
                            case 2:
                                e.Value = "Invalid";
                                e.CellStyle.ForeColor = Color.Red;
                                break;
                        }
                        e.FormattingApplied = true;
                    }
                    else
                    {
                        // Fallback if it's already translated to string
                        if (valStr.Equals("Inactive", StringComparison.OrdinalIgnoreCase)) e.CellStyle.ForeColor = Color.DarkGray;
                        else if (valStr.Equals("Active", StringComparison.OrdinalIgnoreCase) || valStr.Equals("Valid", StringComparison.OrdinalIgnoreCase)) e.CellStyle.ForeColor = Color.Green;
                        else if (valStr.Equals("Invalid", StringComparison.OrdinalIgnoreCase) || valStr.Equals("Missing", StringComparison.OrdinalIgnoreCase)) e.CellStyle.ForeColor = Color.Red;
                    }
                }
            }
        }

        private void PreviewGrid_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            UITheme.UpdateGridSummaryCounts(previewGrid, lblTotalItems, lblActiveItems, lblInactiveItems, lblInvalidItems);
        }
        #endregion

        #region Data Binding & Transformation
        private void BindGrid()
        {
            _bindingSource.DataSource = _exportDataTable;
            previewGrid.DataSource = _bindingSource;
        }

        private DataTable GenerateExportDataTable(DataTable sourceTable)
        {
            DataTable dtExport = new DataTable();
            dtExport.Columns.Add("C-ASIN", typeof(string));
            // Explicitly dropped "IsActive"
            dtExport.Columns.Add("ItemStatus", typeof(string));
            dtExport.Columns.Add("Month", typeof(string));
            dtExport.Columns.Add("Year", typeof(string));
            dtExport.Columns.Add("WIP Quantity", typeof(string));
            dtExport.Columns.Add("CommitmentPeriod", typeof(string));
            dtExport.Columns.Add("Issued Month", typeof(string));
            dtExport.Columns.Add("Issued Year", typeof(string));
            dtExport.Columns.Add("CasePack", typeof(string));
            dtExport.Columns.Add("WIP Type", typeof(string));
            dtExport.Columns.Add("Calculated By", typeof(string));

            string wipCol = DetermineWipColumn(sourceTable);

            var parts = (_session.CurrentMonthWithYear ?? "").Split(' ');
            string currentForecastMonth = parts.Length > 0 ? parts[0] : "";
            string currentForecastYear = parts.Length > 1 ? parts[1] : "";

            string userName = _currentUser?.UserName ?? "System";

            foreach (DataRow r in sourceTable.Rows)
            {
                string asin = r["C-ASIN"]?.ToString();

                string itemStatusText = "Unknown";
                if (sourceTable.Columns.Contains("ItemStatus") && r["ItemStatus"] != DBNull.Value)
                {
                    if (int.TryParse(r["ItemStatus"].ToString(), out int statVal))
                    {
                        switch (statVal)
                        {
                            case 0: itemStatusText = "Inactive"; break;
                            case 1: itemStatusText = "Active"; break;
                            case 2: itemStatusText = "Invalid"; break;
                        }
                    }
                    else
                    {
                        itemStatusText = r["ItemStatus"].ToString();
                    }
                }

                string month = r["Month"]?.ToString();
                string year = r["Year"]?.ToString();
                string wipQty = string.IsNullOrEmpty(wipCol) ? "" : r[wipCol]?.ToString() ?? "";
                string cpStr = r[$"CommitmentPeriod ({_session.Curr.Month})"]?.ToString() ?? "0";

                string casePackVal = "";
                if (sourceTable.Columns.Contains("CasePack") && r["CasePack"] != DBNull.Value)
                {
                    casePackVal = r["CasePack"]?.ToString();
                }

                string typeText = _session.WipType ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(_session.WipProcessingType))
                    typeText += $"-{_session.WipProcessingType}";

                if (sourceTable.Columns.Contains("MOQ") && r["MOQ"] != DBNull.Value)
                {
                    var moq = r["MOQ"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(moq)) typeText += $"-MOQ ({moq})";
                }

                // Add row without IsActive
                dtExport.Rows.Add(asin, itemStatusText, month, year, wipQty, cpStr, currentForecastMonth, currentForecastYear, casePackVal, typeText, userName);
            }

            return dtExport;
        }
        #endregion

        #region Export Logic
        private async Task ExportAsync()
        {
            try
            {
                if (_exportDataTable == null || _exportDataTable.Rows.Count == 0)
                {
                    SetStatus("No data with Commitment Period 3 available to export.", StatusType.Error);
                    return;
                }

                if (!string.Equals(_session.WipStatus, WipStatus.Approved.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    SetStatus("Please Approve the WIP to Download.", StatusType.Error);
                    return;
                }

                DataView exportView = new DataView(_exportDataTable);
                exportView.RowFilter = _bindingSource.Filter;
                DataTable finalExportData = exportView.ToTable();

                if (finalExportData.Rows.Count == 0)
                {
                    SetStatus("Search filter resulted in 0 rows to export.", StatusType.Warning);
                    return;
                }

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                var parts = (_session.CurrentMonthWithYear ?? "").Split(' ');
                string currentForecastMonth = parts.Length > 0 ? parts[0] : "";
                string currentForecastYear = parts.Length > 1 ? parts[1] : "";

                using (var package = new ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("WIP Data");

                    ws.Cells["A1"].LoadFromDataTable(finalExportData, true);

                    using (var range = ws.Cells[1, 1, 1, finalExportData.Columns.Count])
                    {
                        range.Style.Font.Bold = true;
                    }

                    ws.Cells[ws.Dimension.Address].AutoFitColumns();

                    using (var dlg = new SaveFileDialog())
                    {
                        dlg.Filter = "Excel Files (*.xlsx)|*.xlsx";
                        dlg.FileName = $"WipData_{currentForecastMonth}-{currentForecastYear}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

                        if (dlg.ShowDialog() == DialogResult.OK)
                        {
                            var fi = new FileInfo(dlg.FileName);
                            await Task.Run(() => package.SaveAs(fi));

                            SetStatus($"WIP Excel file saved successfully", StatusType.Success);
                        }
                        else
                        {
                            SetStatus("File save was canceled.", StatusType.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"An unexpected error occurred while exporting the data to Excel: {ex.Message}"
                                  + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                                  + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "");
                SetStatus(errorMsg, StatusType.Error);
            }
        }
        #endregion

        #region Helpers
        private void SetStatus(string msg, StatusType type) => _setStatus?.Invoke(msg, type);

        private static string DetermineWipColumn(DataTable dataTable)
        {
            if (dataTable.Columns.Contains("CasePack_Wip")) return "CasePack_Wip";
            if (dataTable.Columns.Contains("MOQ_Wip")) return "MOQ_Wip";
            if (dataTable.Columns.Contains("Review_Wip")) return "Review_Wip";
            return string.Empty;
        }
        #endregion
    }
}