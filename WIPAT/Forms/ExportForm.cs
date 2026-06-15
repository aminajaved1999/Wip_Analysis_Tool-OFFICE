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

        private ComboBox _cmbViewMode;
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

            Label lblView = new Label
            {
                Text = "View:",
                AutoSize = true,
                Location = new Point(320, 21),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = UITheme.TextSecondaryColor
            };

            _cmbViewMode = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(370, 18),
                Width = 140,
                Font = new Font("Segoe UI", 9.5F),
                BackColor = UITheme.SurfaceWhite,
                ForeColor = UITheme.GridRowText
            };

            _cmbViewMode.Items.Add("Standard View");
            _cmbViewMode.Items.Add("Export Preview");
            _cmbViewMode.SelectedIndex = 1;
            _cmbViewMode.SelectedIndexChanged += (s, e) => BindGrid();

            pnlToolbar.Controls.Add(lblView);
            pnlToolbar.Controls.Add(_cmbViewMode);

            // Wire up the global UITheme widget method
            if (previewGrid != null)
            {
                previewGrid.DataBindingComplete += (s, e) =>
                    UITheme.UpdateGridSummaryCounts(previewGrid, lblTotalItems, lblActiveItems, lblInactiveItems, lblInvalidItems);
            }
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
                txtSearchAsin.Clear();
                previewGrid.Refresh();
            }
        }

        private void TxtSearchAsin_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (_bindingSource.DataSource == null) return;

                string searchValue = txtSearchAsin.Text.Trim().Replace("'", "''");

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
                string errorMsg = $"An unexpected error occurred while applying the search filter: {ex.Message}"
                                  + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                                  + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "");
                System.Diagnostics.Debug.WriteLine(errorMsg);
            }
        }

        private async void BtnExport_Click(object sender, EventArgs e)
        {
            await ExportAsync();
        }
        #endregion

        #region Data Binding & Transformation
        private void BindGrid()
        {
            if (_cmbViewMode.SelectedIndex == 1)
            {
                _bindingSource.DataSource = _exportDataTable;
            }
            else
            {
                _bindingSource.DataSource = _filteredDataTable;
            }

            previewGrid.DataSource = _bindingSource;

            TxtSearchAsin_TextChanged(this, EventArgs.Empty);
        }

        private DataTable GenerateExportDataTable(DataTable sourceTable)
        {
            DataTable dtExport = new DataTable();
            dtExport.Columns.Add("C-ASIN", typeof(string));
            dtExport.Columns.Add("IsActive", typeof(bool));
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

                // Get IsActive
                bool isActive = false;
                if (sourceTable.Columns.Contains("IsActive") && r["IsActive"] != DBNull.Value)
                {
                    var val = r["IsActive"];
                    if (val is bool b) isActive = b;
                    else if (val.ToString() == "1" || val.ToString().Equals("true", StringComparison.OrdinalIgnoreCase)) isActive = true;
                }

                // Get ItemStatus
                string itemStatus = "";
                if (sourceTable.Columns.Contains("ItemStatus") && r["ItemStatus"] != DBNull.Value)
                {
                    itemStatus = r["ItemStatus"]?.ToString();
                }
                else if (sourceTable.Columns.Contains("Item Status") && r["Item Status"] != DBNull.Value)
                {
                    itemStatus = r["Item Status"]?.ToString();
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

                dtExport.Rows.Add(asin, isActive, itemStatus, month, year, wipQty, cpStr, currentForecastMonth, currentForecastYear, casePackVal, typeText, userName);
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