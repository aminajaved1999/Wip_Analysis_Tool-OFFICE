using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WIPAT.BLL.Interfaces;
using WIPAT.DAL.Interfaces;
using WIPAT.Entities;
using WIPAT.Entities.Enum;
using WIPAT.Helpers;
using LicenseContext = OfficeOpenXml.LicenseContext;

namespace WIPAT
{
    public partial class CalculateWIPForm : Form
    {
        #region Fields & Dependencies
        private readonly WipSession _session;
        private readonly Action<string, StatusType> _setStatus;

        private readonly IWipManager _wipManager;
        private readonly IWipRepository _wipRepository;
        private readonly INewWorkingWipManager _newWorkingWipManager;
        private readonly IStockRepository _stockRepository;

        private int percentage;
        private string WipProcessingType;
        private string wipStatus;
        private DataTable FinalDataTable;
        private DataTable StockDataTable;

        private Form _wipFormCached;
        private BusyOverlayHelper _busyHelper;

        public event Action CalculationCompleted;
        #endregion

        #region Constructor & Initialization
        public CalculateWIPForm(WipSession session,
            Action<string, StatusType> setStatus,
            IWipManager wipManager,
            IWipRepository wipRepository,
            IStockRepository stockRepository,
            INewWorkingWipManager newWorkingWipManager)
        {
            InitializeComponent();
            ApplyTheme();

            _session = session;
            _setStatus = setStatus;

            _stockRepository = stockRepository;
            _wipManager = wipManager;
            _wipRepository = wipRepository;
            _newWorkingWipManager = newWorkingWipManager;

            _busyHelper = new BusyOverlayHelper(this, progressBar1, SetStatusThreadSafe);

            this.FormClosing += NewCalculateWIPForm_FormClosing;

            textBoxMOQ.Validating += textBoxMOQ_Validating;
            textBoxPercentage.Validating += textBoxPercentage_Validating;

            textBoxMOQ.Visible = false;
            textBoxMOQ.Enabled = false;

            textBoxPercentage.Visible = false;
            textBoxPercentage.Enabled = false;
            lblPercentSymbol.Visible = false;

            if (radioButtonPercentage.Checked) radioButtonPercentage_CheckedChanged(this, EventArgs.Empty);
        }

        private void ApplyTheme()
        {
            UITheme.SetFormIcon(this);
            this.BackColor = UITheme.BackgroundCanvas;

            pnlHeader.BackColor = UITheme.SurfaceWhite;
            pnlHeader.ForeColor = UITheme.GridRowText;
            lblTitle.ForeColor = UITheme.GridRowText;

            pnlWipTypeCard.BackColor = UITheme.SurfaceWhite;
            pnlOptionsCard.BackColor = UITheme.SurfaceWhite;

            lblWipTypeHeader.ForeColor = UITheme.Upload_Color;
            lblOptionsHeader.ForeColor = UITheme.Upload_Color;

            UITheme.StyleButton(btnReviewWIP, AppButtonStyle.Calculate);
            UITheme.StyleButton(btnApproveWIP, AppButtonStyle.ApproveAndSave);
            SetApproveButtonState(false);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            try
            {
                if (!string.IsNullOrWhiteSpace(_session.WipProcessingType))
                {
                    radioButtonSystem.Checked = _session.WipProcessingType == ProcessingWipType.System.ToString();
                    radioButtonPercentage.Checked = _session.WipProcessingType == ProcessingWipType.Percentage.ToString();
                    radioButtonMonthOfSupply.Checked = _session.WipProcessingType == ProcessingWipType.MonthOfSupply.ToString();
                    radioButtonNewWorking.Checked = _session.WipProcessingType == ProcessingWipType.WipWorking.ToString();

                    radioButtonPercentage_CheckedChanged(this, EventArgs.Empty);
                }

                if (_session.FinalDataTable != null && _session.FinalDataTable.Rows.Count > 0)
                {
                    FinalDataTable = _session.FinalDataTable;
                    ShowWipTable(FinalDataTable, _session.TargetMonth);
                    SetApproveButtonState(true);
                }
                else
                {
                    SetApproveButtonState(false);
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"An unexpected error occurred while restoring the Calculate view: {ex.Message}"
                                  + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                                  + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "");
                SetStatus(errorMsg, StatusType.Error);
            }
        }
        #endregion

        #region UI Event Handlers
        private void NewCalculateWIPForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        private void checkBoxMOQ_CheckedChanged(object sender, EventArgs e)
        {
            textBoxMOQ.Visible = checkBoxMOQ.Checked;
            textBoxMOQ.Enabled = checkBoxMOQ.Checked;
        }

        private void textBoxMOQ_Validating(object sender, CancelEventArgs e)
        {
            if (checkBoxMOQ.Checked)
            {
                if (!int.TryParse(textBoxMOQ.Text, out int result) || result <= 0)
                {
                    MessageBox.Show("Please enter a positive integer for MOQ.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.Cancel = true;
                }
            }
        }

        private void radioButtonPercentage_CheckedChanged(object sender, EventArgs e)
        {
            bool on = radioButtonPercentage.Checked;
            textBoxPercentage.Visible = on;
            textBoxPercentage.Enabled = on;
            lblPercentSymbol.Visible = on;
        }

        private void textBoxPercentage_Validating(object sender, CancelEventArgs e)
        {
            if (radioButtonPercentage.Checked)
            {
                if (!int.TryParse(textBoxPercentage.Text, out int result) || result <= 0 || result > 100)
                {
                    MessageBox.Show("Please enter a percentage between 1 and 100.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.Cancel = true;
                }
                else
                {
                    percentage = result;
                }
            }
        }

        private void SetApproveButtonState(bool isEnabled)
        {
            btnApproveWIP.Enabled = isEnabled;
            if (isEnabled)
            {
                UITheme.StyleButton(btnApproveWIP, AppButtonStyle.ApproveAndSave);
            }
            else
            {
                UITheme.StyleButton(btnApproveWIP, AppButtonStyle.Secondary);
            }
        }
        #endregion

        #region Logic: Review WIP
        private async void btnReviewWIP_Click(object sender, EventArgs e)
        {
            SetStatus(string.Empty, StatusType.Reset);

            try
            {
                if (!ValidateSessionAndInputs()) return;

                WipProcessingType = GetSelectedProcessingWipType();

                if (WipProcessingType == ProcessingWipType.MonthOfSupply.ToString())
                {
                    MessageBox.Show("The calculation will use data from the first 2 months only.",
                        "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                int? MOQ = null;
                if (checkBoxMOQ.Checked)
                {
                    if (int.TryParse(textBoxMOQ.Text, out int moqValue)) MOQ = moqValue;
                }

                if (radioButtonPercentage.Checked)
                {
                    if (int.TryParse(textBoxPercentage.Text, out int pct)) percentage = pct;
                }

                _busyHelper.ShowBusy("Calculating WIP Data...");
                await Task.Yield();

                if (WipProcessingType == ProcessingWipType.WipWorking.ToString())
                {
                    checkBoxCasePack.Checked = true;

                    var tableResponse = await Task.Run(() =>
                                    _newWorkingWipManager.BuildCommonWipDataTable(
                                        _session.AsinList,
                                        _session.Prev,
                                        _session.Curr,
                                        _session.Curr.ForecastingFor,
                                        _session.WipType,
                                        _session.ItemCatalogue,
                                        MOQ,
                                        checkBoxCasePack.Checked,
                                        WipProcessingType,
                                        percentage
                                    )
                                );

                    if (!tableResponse.Success)
                    {
                        SetStatus($"Error generating stock table: {tableResponse.Message}", StatusType.Error);
                        return;
                    }

                    StockDataTable = tableResponse.Data;
                    FinalDataTable = tableResponse.Data;
                }
                else
                {
                    var tableResponse = await Task.Run(() =>
                                    _wipManager.BuildCommonWipDataTable(
                                        _session.AsinList,
                                        _session.Prev,
                                        _session.Curr,
                                        _session.Curr.ForecastingFor,
                                        _session.WipType,
                                        _session.ItemCatalogue,
                                        MOQ,
                                        checkBoxCasePack.Checked,
                                        WipProcessingType,
                                        percentage
                                    )
                                );

                    if (!tableResponse.Success)
                    {
                        SetStatus($"Error generating stock table: {tableResponse.Message}", StatusType.Error);
                        return;
                    }

                    StockDataTable = tableResponse.Data;
                    FinalDataTable = tableResponse.Data;
                }

                _busyHelper.ShowBusy("Rendering Final Table...");
                ShowWipTable(FinalDataTable, _session.TargetMonth);

                SetStatus("WIP calculated successfully.", StatusType.Success);
                SetApproveButtonState(true);

                wipStatus = WipStatus.Reviewed.ToString();

                _session.FinalDataTable = FinalDataTable;
                _session.WipProcessingType = WipProcessingType;
                _session.WipStatus = WipStatus.Reviewed.ToString();
            }
            catch (Exception ex)
            {
                string errorMsg = $"An unexpected error occurred while reviewing the WIP data: {ex.Message}"
                                  + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                                  + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "");
                SetStatus(errorMsg, StatusType.Error);
            }
            finally
            {
                _busyHelper.HideBusy();
            }
        }
        #endregion

        #region Logic: Approve WIP
        private async void btnApproveWIP_Click(object sender, EventArgs e)
        {
            try
            {
                if (wipStatus == WipStatus.Approved.ToString())
                {
                    MessageBox.Show("This WIP has already been approved.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (FinalDataTable == null || FinalDataTable.Rows.Count == 0)
                {
                    SetStatus("Please Review the WIP first.", StatusType.Error);
                    return;
                }

                _busyHelper.ShowBusy("Determining Wip Column...");

                string wipColName = DetermineWipColumn(FinalDataTable);
                if (string.IsNullOrEmpty(wipColName))
                {
                    SetStatus("Invalid WIP column in final table.", StatusType.Error);
                    return;
                }

                _busyHelper.ShowBusy("Saving Records to Database...");

                var saveResponse = await _newWorkingWipManager.SaveWipRecordsAsync(FinalDataTable, WipProcessingType, wipColName, StockDataTable, _session);

                if (!saveResponse.Success)
                {
                    SetStatus(saveResponse.Message, StatusType.Warning);
                    return;
                }

                SetStatus("WIP Saved Successfully.", StatusType.Success);
                wipStatus = WipStatus.Approved.ToString();
                _session.WipStatus = wipStatus;

                CalculationCompleted?.Invoke();

                _busyHelper.ShowBusy("Updating Forecast Grid...");
                await Task.Yield();
                UpdateCurrentForecastGridWithApprovedWip();

                SetStatus("Success", StatusType.Success);
            }
            catch (Exception ex)
            {
                string errorMsg = $"An unexpected error occurred while approving the WIP data: {ex.Message}"
                                  + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                                  + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "");
                SetStatus(errorMsg, StatusType.Error);
            }
            finally
            {
                _busyHelper.HideBusy();
            }
        }
        #endregion

        #region Helper Methods
        private bool ValidateSessionAndInputs()
        {
            if (_session.Prev != null)
            {
                var checkPrevious = _wipRepository.CheckIfWipCalculated(_session.Prev.Month, _session.Prev.Year);
                if (checkPrevious.Success)
                {
                    string msg = $"{checkPrevious.Message}\nCalculate Wip of '{_session.Prev.Month} {_session.Prev.Year}' before calculating {_session.Curr.Month} {_session.Curr.Year}";
                    SetStatus(msg, StatusType.Error);
                    MessageBox.Show(msg, "Sequence Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return false;
                }
            }

            var checkCurrent = _wipRepository.CheckIfWipCalculated(_session.Curr.Month, _session.Curr.Year);
            if (!checkCurrent.Success && checkCurrent.Data)
            {
                SetStatus(checkCurrent.Message, StatusType.Error);
                return false;
            }

            if (!TryValidateReadyForCalculation(out var missingMsg, checkBoxCasePack.Checked))
            {
                SetStatus(missingMsg, StatusType.Error);
                return false;
            }

            if (string.IsNullOrEmpty(GetSelectedProcessingWipType()))
            {
                SetStatus("Please select a WIP processing type.", StatusType.Error);
                return false;
            }

            return true;
        }

        private string GetSelectedProcessingWipType()
        {
            if (radioButtonMonthOfSupply.Checked) return ProcessingWipType.MonthOfSupply.ToString();
            if (radioButtonPercentage.Checked) return ProcessingWipType.Percentage.ToString();
            if (radioButtonSystem.Checked) return ProcessingWipType.System.ToString();
            if (radioButtonNewWorking.Checked) return ProcessingWipType.WipWorking.ToString();
            return string.Empty;
        }

        private bool TryValidateReadyForCalculation(out string message, bool requireCasePack)
        {
            var missing = new List<string>();

            if (_session == null)
            {
                message = "Internal error: session is null.";
                return false;
            }

            if (_session.ForecastFiles == null || _session.ForecastFiles.Count == 0)
                missing.Add("Current Forecast file");

            if (_session.Orders == null) missing.Add("Order file");

            if (_session.Curr == null) missing.Add("Current forecast (Curr)");

            if (string.IsNullOrWhiteSpace(_session.WipType)) missing.Add("WIP Type selection");

            if (requireCasePack && (_session.ItemCatalogue == null || _session.ItemCatalogue.Count == 0))
                missing.Add("Item catalogue (for CasePack)");

            if (missing.Count > 0)
            {
                message = "Missing Required Data:\n• " + string.Join("\n• ", missing);
                return false;
            }

            message = string.Empty;
            return true;
        }

        private string DetermineWipColumn(DataTable dataTable)
        {
            if (dataTable.Columns.Contains("CasePack_Wip")) return "CasePack_Wip";
            if (dataTable.Columns.Contains("MOQ_Wip")) return "MOQ_Wip";
            if (dataTable.Columns.Contains("Review_Wip")) return "Review_Wip";
            return string.Empty;
        }

        private void SetStatus(string message, StatusType statusType) => _setStatus?.Invoke(message, statusType);

        private void SetStatusThreadSafe(string msg, StatusType type)
        {
            if (IsDisposed) return;
            if (InvokeRequired) BeginInvoke(new Action(() => _setStatus?.Invoke(msg, type)));
            else _setStatus?.Invoke(msg, type);
        }

        private void UpdateCurrentForecastGridWithApprovedWip()
        {
            try
            {
                if (_session?.ForecastFiles == null) return;

                var currFile = _session.ForecastFiles.FirstOrDefault(f => f.ProjectionMonth == _session.CurrentMonth);
                if (currFile == null) return;

                DataTable currentGridDataTable = currFile.FullTable;
                DataTable newSourceTableForWipValues = _session.FinalDataTable;
                DataTable updatedTable = currentGridDataTable.Clone();

                foreach (DataRow row in currentGridDataTable.Rows) updatedTable.ImportRow(row);

                foreach (DataRow sourceRow in newSourceTableForWipValues.Rows)
                {
                    string cAsin = sourceRow["C-ASIN"]?.ToString()?.Trim();
                    string reviewWip = sourceRow["Review_Wip"]?.ToString()?.Trim();

                    var targetRow = updatedTable.AsEnumerable().FirstOrDefault(r => r["C-ASIN"].ToString() == cAsin);

                    if (targetRow != null)
                    {
                        if (int.TryParse(reviewWip, out int wipValue)) targetRow["WIP"] = wipValue;
                        else targetRow["WIP"] = DBNull.Value;
                    }
                }

                var grid = currFile.BoundGrid;
                if (grid != null && !grid.IsDisposed)
                {
                    grid.DataSource = updatedTable;
                    grid.Refresh();
                }

                SetStatus($"Updated WIP in '{_session.CurrentMonthWithYear}' grid.", StatusType.Success);
            }
            catch (Exception ex)
            {
                string errorMsg = $"An unexpected error occurred while updating the current forecast grid: {ex.Message}"
                                  + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                                  + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "");
                SetStatus(errorMsg, StatusType.Warning);
            }
        }
        #endregion

        #region Preview Window Logic (Result Table)
        private void ShowWipTable(DataTable stockTable, string targetMonth)
        {
            try
            {
                if (_wipFormCached == null || _wipFormCached.IsDisposed)
                {
                    _wipFormCached = new Form
                    {
                        Text = $"Remaining Stock & WIP for {targetMonth}",
                        Width = 1400,
                        Height = 700,
                        StartPosition = FormStartPosition.CenterParent,
                        Icon = this.Icon,
                        BackColor = UITheme.BackgroundCanvas
                    };

                    var dgv = new DataGridView
                    {
                        Dock = DockStyle.Fill,
                        ReadOnly = true,
                        DataSource = stockTable
                    };
                    UITheme.StyleGrid(dgv);

                    // Replicated exact formatting logic here!
                    dgv.CellFormatting += (s, e) =>
                    {
                        if (e.RowIndex < 0 || e.Value == null || e.Value == DBNull.Value) return;

                        string colName = dgv.Columns[e.ColumnIndex].Name;

                        if (colName == "C-ASIN")
                        {
                            var val = e.Value.ToString();
                            e.CellStyle.BackColor = GenerateColorFromString(val);
                        }
                        else if (colName == "ItemStatus")
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
                                if (valStr.Equals("Inactive", StringComparison.OrdinalIgnoreCase)) e.CellStyle.ForeColor = Color.DarkGray;
                                else if (valStr.Equals("Active", StringComparison.OrdinalIgnoreCase) || valStr.Equals("Valid", StringComparison.OrdinalIgnoreCase)) e.CellStyle.ForeColor = Color.Green;
                                else if (valStr.Equals("Invalid", StringComparison.OrdinalIgnoreCase) || valStr.Equals("Missing", StringComparison.OrdinalIgnoreCase)) e.CellStyle.ForeColor = Color.Red;
                            }
                        }
                    };

                    // Add Status Bar for Counts
                    var pnlStatusBar = new FlowLayoutPanel
                    {
                        Dock = DockStyle.Bottom,
                        Height = 40,
                        BackColor = Color.FromArgb(245, 246, 250),
                        Padding = new Padding(15, 10, 15, 5),
                        WrapContents = false
                    };

                    var lblTotal = new Label { AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.Black, Margin = new Padding(0, 0, 20, 0) };
                    var lblActive = new Label { AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(46, 125, 50), Margin = new Padding(0, 0, 20, 0) };
                    var lblInactive = new Label { AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.DarkGray, Margin = new Padding(0, 0, 20, 0) };
                    var lblInvalid = new Label { AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.Red, Margin = new Padding(0, 0, 20, 0) };

                    pnlStatusBar.Controls.AddRange(new Control[] { lblTotal, lblActive, lblInactive, lblInvalid });

                    // Action to cleanly calculate metrics uniquely by CASIN
                    Action updateStats = () =>
                    {
                        if (dgv.IsDisposed) return;

                        // Ensure IsActive is hidden so the user only sees ItemStatus
                        if (dgv.Columns.Contains("IsActive")) dgv.Columns["IsActive"].Visible = false;

                        int t = 0, a = 0, i = 0, inv = 0;
                        bool hasStatusCol = dgv.Columns.Contains("ItemStatus");
                        bool hasCasinCol = dgv.Columns.Contains("C-ASIN");

                        if (hasCasinCol && hasStatusCol)
                        {
                            // Track which CASINs we've already counted to avoid multiplying by commitment periods
                            var processedCasins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                            foreach (DataGridViewRow r in dgv.Rows)
                            {
                                if (r.IsNewRow) continue;

                                var casinCell = r.Cells["C-ASIN"].Value?.ToString();
                                if (string.IsNullOrWhiteSpace(casinCell)) continue;

                                // Only process the status count for each CASIN once!
                                if (processedCasins.Add(casinCell.Trim()))
                                {
                                    t++;
                                    var statCell = r.Cells["ItemStatus"].Value;
                                    if (statCell != null && int.TryParse(statCell.ToString(), out int stat))
                                    {
                                        if (stat == 1) a++;
                                        else if (stat == 0) i++;
                                        else if (stat == 2) inv++;
                                    }
                                }
                            }
                        }

                        lblTotal.Text = $"Total Items: {t}";
                        lblActive.Text = $"Active: {a}";
                        lblInactive.Text = $"Inactive: {i}";
                        lblInvalid.Text = $"Invalid: {inv}";
                    };

                    dgv.DataBindingComplete += (s, e) => updateStats();

                    var pnlControls = new Panel { Dock = DockStyle.Top, Height = 60, Padding = new Padding(15), BackColor = UITheme.SurfaceWhite };

                    var lblSearch = new Label { Text = "Filter CASIN:", Dock = DockStyle.Left, Width = 100, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
                    var txtSearch = new TextBox { Width = 200, Dock = DockStyle.Left, Font = new Font("Segoe UI", 10), BorderStyle = BorderStyle.FixedSingle };

                    var btnSearch = new Button { Text = "Search", Dock = DockStyle.Left, Width = 80, Margin = new Padding(10, 0, 0, 0) };
                    UITheme.StyleButton(btnSearch, AppButtonStyle.Search);

                    btnSearch.Click += (s, e) => {
                        string term = txtSearch.Text.Replace("'", "''");
                        stockTable.DefaultView.RowFilter = string.IsNullOrWhiteSpace(term) ? "" : $"[C-ASIN] LIKE '%{term}%'";
                        updateStats();
                    };

                    var btnExport = new Button { Text = "Export to Excel", Dock = DockStyle.Right, Width = 150 };
                    UITheme.StyleButton(btnExport, AppButtonStyle.ExportToExcel);
                    btnExport.Click += (s, e) => {
                        using (var sfd = new SaveFileDialog { Filter = "Excel|*.xlsx", FileName = $"WIP_{targetMonth}.xlsx" })
                        {
                            if (sfd.ShowDialog() == DialogResult.OK) ExportDataTableToExcel(stockTable, sfd.FileName);
                        }
                    };

                    pnlControls.Controls.Add(btnExport);
                    pnlControls.Controls.Add(btnSearch);
                    pnlControls.Controls.Add(txtSearch);
                    pnlControls.Controls.Add(lblSearch);

                    _wipFormCached.Controls.Add(dgv);
                    _wipFormCached.Controls.Add(pnlStatusBar); // Add bottom status bar
                    _wipFormCached.Controls.Add(pnlControls);  // Add top toolbar
                }
                else
                {
                    var dgv = _wipFormCached.Controls.OfType<DataGridView>().FirstOrDefault();
                    if (dgv != null) dgv.DataSource = stockTable;
                }

                _wipFormCached.Show(this);
                _wipFormCached.BringToFront();
            }
            catch (Exception ex)
            {
                string errorMsg = $"An unexpected error occurred while displaying the WIP preview table: {ex.Message}"
                                  + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                                  + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "");
                SetStatus(errorMsg, StatusType.Error);
            }
        }

        private void ExportDataTableToExcel(DataTable dt, string filePath)
        {
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets.Add("Data");
                    worksheet.Cells["A1"].LoadFromDataTable(dt, true);
                    worksheet.Cells.AutoFitColumns();
                    package.Save();
                }
                MessageBox.Show("Export Successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                string errorMsg = $"An unexpected error occurred while exporting data to Excel: {ex.Message}"
                                  + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                                  + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "");
                MessageBox.Show(errorMsg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Color GenerateColorFromString(string input)
        {
            int hash = Math.Abs(input.GetHashCode());
            return Color.FromArgb((hash % 50) + 200, ((hash / 256) % 50) + 200, ((hash / 65536) % 50) + 200);
        }
        #endregion
    }
}