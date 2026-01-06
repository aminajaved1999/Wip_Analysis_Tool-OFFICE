using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using WIPAT.BLL;
using WIPAT.DAL;
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

        private readonly WipManager2 wipManager2;
        private readonly WipRepository wipRepository;
        private readonly StockRepository stockRepository;

        private int percentage;
        private string WipProcessingType;
        private string wipStatus;
        private DataTable FinalDataTable;
        private DataTable StockDataTable;

        // Cached preview window so it isn’t disposed when switching steps
        private Form _wipFormCached;
        private BusyOverlayHelper _busyHelper;

        // Let MainForm know when Step 2 is completed
        public event Action CalculationCompleted;

        #endregion

        #region Constructor & Load

        public CalculateWIPForm(WipSession session, Action<string, StatusType> setStatus)
        {
            InitializeComponent();

            // --- THEME UPDATE START ---
            this.BackColor = UITheme.BackgroundCanvas;

            // Make cards white
            pnlHeader.BackColor = UITheme.SurfaceWhite;
            pnlWipTypeCard.BackColor = UITheme.SurfaceWhite;
            pnlOptionsCard.BackColor = UITheme.SurfaceWhite;

            // Apply colors to Labels
            lblTitle.ForeColor = UITheme.TextDark;
            lblWipTypeHeader.ForeColor = UITheme.PrimaryBlue;
            lblOptionsHeader.ForeColor = UITheme.PrimaryBlue;

            // Apply colors to Buttons
            UITheme.ApplyButtonTheme(btnReviewWIP, isPrimary: true);
            UITheme.ApplyButtonTheme(btnApproveWIP, isPrimary: false);
            // --- THEME UPDATE END ---

            _session = session;
            _setStatus = setStatus;

            // Initialize Managers
            stockRepository = new StockRepository(_session);
            wipManager2 = new WipManager2(_session);
            wipRepository = new WipRepository();

            // Initialize Helpers
            _busyHelper = new BusyOverlayHelper(this, progressBar1, SetStatusThreadSafe);

            // Wire up Events
            this.FormClosing += NewCalculateWIPForm_FormClosing;

            // Input Validations
            textBoxMOQ.Validating += textBoxMOQ_Validating;
            textBoxPercentage.Validating += textBoxPercentage_Validating;

            // Initial UI State
            textBoxMOQ.Visible = false;
            textBoxMOQ.Enabled = false;

            textBoxPercentage.Visible = false;
            textBoxPercentage.Enabled = false;
            lblPercentSymbol.Visible = false;

            // Ensure radio button state triggers UI update immediately if set in designer
            if (radioButtonPercentage.Checked) radioButtonPercentage_CheckedChanged(this, EventArgs.Empty);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            try
            {
                // Restore selected processing type from session if present
                if (!string.IsNullOrWhiteSpace(_session.WipProcessingType))
                {
                    radioButtonSystem.Checked = _session.WipProcessingType == ProcessingWipType.System.ToString();
                    radioButtonPercentage.Checked = _session.WipProcessingType == ProcessingWipType.Percentage.ToString();
                    radioButtonMonthOfSupply.Checked = _session.WipProcessingType == ProcessingWipType.MonthOfSupply.ToString();

                    // Force UI sync
                    radioButtonPercentage_CheckedChanged(this, EventArgs.Empty);
                }

                // If a final table already exists (from a previous review), re-show preview & enable Approve
                if (_session.FinalDataTable != null && _session.FinalDataTable.Rows.Count > 0)
                {
                    FinalDataTable = _session.FinalDataTable;
                    ShowWipTable(FinalDataTable, _session.TargetMonth);
                    btnApproveWIP.Enabled = true;
                    // Style the approved button to look active/ready
                    btnApproveWIP.BackColor = Color.FromArgb(46, 125, 50);
                }
                else
                {
                    btnApproveWIP.Enabled = false;
                    btnApproveWIP.BackColor = Color.Gray;
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Error while restoring Calculate view: {ex.Message}", StatusType.Error);
            }
        }

        #endregion

        #region UI Event Handlers

        private void NewCalculateWIPForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide(); // Keep instance alive for state preservation
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

        #endregion

        #region Logic: Review WIP

        private async void btnReviewWIP_Click(object sender, EventArgs e)
        {
            SetStatus(string.Empty, StatusType.Reset);

            try
            {
                // 1. Pre-Flight Checks
                if (!ValidateSessionAndInputs()) return;

                WipProcessingType = GetSelectedProcessingWipType();

                if (WipProcessingType == ProcessingWipType.MonthOfSupply.ToString())
                {
                    MessageBox.Show("The calculation will use data from the first 2 months only.",
                        "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                // 2. Prepare Inputs
                int? MOQ = null;
                if (checkBoxMOQ.Checked)
                {
                    if (int.TryParse(textBoxMOQ.Text, out int moqValue)) MOQ = moqValue;
                }

                if (radioButtonPercentage.Checked)
                {
                    if (int.TryParse(textBoxPercentage.Text, out int pct)) percentage = pct;
                }

                // 3. Calculation Process
                _busyHelper.ShowBusy("Calculating WIP Data...");
                await Task.Yield(); // Free up UI thread momentarily

                // 

                var tableResponse = await Task.Run(() =>
                                    wipManager2.BuildCommonWipDataTable(
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

                // 4. Update State
                StockDataTable = tableResponse.Data;
                FinalDataTable = tableResponse.Data;

                // 5. Show Results
                _busyHelper.ShowBusy("Rendering Final Table...");
                ShowWipTable(FinalDataTable, _session.TargetMonth);

                SetStatus("WIP calculated successfully.", StatusType.Success);

                btnApproveWIP.Enabled = true;
                btnApproveWIP.BackColor = Color.FromArgb(46, 125, 50); // Enable Green Visual

                wipStatus = WipStatus.Reviewed.ToString();

                // Persist state in session
                _session.FinalDataTable = FinalDataTable;
                _session.WipProcessingType = WipProcessingType;
                _session.WipStatus = WipStatus.Reviewed.ToString();
            }
            catch (Exception ex)
            {
                SetStatus($"Exception during WIP Review: {ex.Message}", StatusType.Error);
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

                var saveResponse = await wipManager2.SaveWipRecordsAsync(FinalDataTable, WipProcessingType, wipColName, StockDataTable);

                if (!saveResponse.Success)
                {
                    SetStatus(saveResponse.Message, StatusType.Warning);
                    return;
                }

                SetStatus("WIP Saved Successfully.", StatusType.Success);
                wipStatus = WipStatus.Approved.ToString();
                _session.WipStatus = wipStatus;

                // Notify Main Form
                CalculationCompleted?.Invoke();

                _busyHelper.ShowBusy("Updating Forecast Grid...");
                await Task.Yield();
                UpdateCurrentForecastGridWithApprovedWip();

                SetStatus("Success", StatusType.Success);

            }
            catch (Exception ex)
            {
                SetStatus($"Exception during Approval: {ex.Message}", StatusType.Error);
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
            // Database Checks
            var checkPrevious = wipRepository.CheckIfWipCalculated(_session.Prev.Month, _session.Prev.Year);
            if (checkPrevious.Success)
            {
                string msg = $"{checkPrevious.Message}\nCalculate Wip of '{_session.Prev.Month} {_session.Prev.Year}' before calculating {_session.Curr.Month} {_session.Curr.Year}";
                SetStatus(msg, StatusType.Error);
                MessageBox.Show(msg, "Sequence Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return false;
            }

            //var checkCurrent = wipRepository.CheckIfWipCalculated(_session.Curr.Month, _session.Curr.Year);
            //if (!checkCurrent.Success && checkCurrent.Data)
            //{
            //    SetStatus(checkCurrent.Message, StatusType.Error);
            //    return false;
            //}

            // Data Readiness Checks
            if (!TryValidateReadyForCalculation(out var missingMsg, checkBoxCasePack.Checked))
            {
                SetStatus(missingMsg, StatusType.Error);
                return false;
            }

            // UI Selection Checks
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

            if (_session.ForecastFiles == null || _session.ForecastFiles.Count < 2)
                missing.Add("At least 2 forecast files");

            if (_session.Orders == null) missing.Add("Order file");
            if (_session.Prev == null) missing.Add("Previous forecast (Prev)");
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
            // Logic to reflect the calculated WIP back onto the main Excel grid view
            try
            {
                if (_session?.ForecastFiles == null) return;

                var currFile = _session.ForecastFiles.FirstOrDefault(f => f.ProjectionMonth == _session.CurrentMonth);
                if (currFile == null) return;

                DataTable currentGridDataTable = currFile.FullTable;
                DataTable newSourceTableForWipValues = _session.FinalDataTable;
                DataTable updatedTable = currentGridDataTable.Clone();

                foreach (DataRow row in currentGridDataTable.Rows) updatedTable.ImportRow(row);

                // Match and Update Logic
                foreach (DataRow sourceRow in newSourceTableForWipValues.Rows)
                {
                    string cAsin = sourceRow["C-ASIN"]?.ToString()?.Trim();
                    string reviewWip = sourceRow["Review_Wip"]?.ToString()?.Trim();
                    // Note: In a real scenario, you might want to map specific columns like PO Date/Commitment period for exact matching

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
                SetStatus($"Failed to update current grid: {ex.Message}", StatusType.Warning);
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
                    // Create Form
                    _wipFormCached = new Form
                    {
                        Text = $"Remaining Stock & WIP for {targetMonth}",
                        Width = 1400,
                        Height = 700,
                        StartPosition = FormStartPosition.CenterParent,
                        Icon = this.Icon
                    };

                    // Data Grid
                    var dgv = new DataGridView
                    {
                        Dock = DockStyle.Fill,
                        ReadOnly = true,
                        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                        AllowUserToOrderColumns = false,
                        DataSource = stockTable,
                        AlternatingRowsDefaultCellStyle = { BackColor = Color.WhiteSmoke }
                    };

                    // Coloring Logic
                    dgv.CellFormatting += (s, e) =>
                    {
                        if (e.RowIndex >= 0 && dgv.Columns.Contains("C-ASIN") && e.ColumnIndex == dgv.Columns["C-ASIN"].Index)
                        {
                            var val = e.Value?.ToString();
                            if (val != null) e.CellStyle.BackColor = GenerateColorFromString(val);
                        }
                    };

                    // Control Panel
                    var pnlControls = new Panel { Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(10) };

                    // Search
                    var txtSearch = new TextBox { Width = 200, Dock = DockStyle.Left, Font = new Font("Segoe UI", 10) };
                    var btnSearch = new Button { Text = "Filter", Dock = DockStyle.Left, Width = 80, FlatStyle = FlatStyle.Flat };

                    btnSearch.Click += (s, e) => {
                        string term = txtSearch.Text.Replace("'", "''");
                        stockTable.DefaultView.RowFilter = string.IsNullOrWhiteSpace(term) ? "" : $"[C-ASIN] LIKE '%{term}%'";
                    };

                    // Export
                    var btnExport = new Button { Text = "Export to Excel", Dock = DockStyle.Right, Width = 150, FlatStyle = FlatStyle.Flat, BackColor = Color.SeaGreen, ForeColor = Color.White };
                    btnExport.Click += (s, e) => {
                        using (var sfd = new SaveFileDialog { Filter = "Excel|*.xlsx", FileName = $"WIP_{targetMonth}.xlsx" })
                        {
                            if (sfd.ShowDialog() == DialogResult.OK) ExportDataTableToExcel(stockTable, sfd.FileName);
                        }
                    };

                    pnlControls.Controls.Add(btnExport);
                    pnlControls.Controls.Add(btnSearch);
                    pnlControls.Controls.Add(txtSearch);

                    _wipFormCached.Controls.Add(dgv);
                    _wipFormCached.Controls.Add(pnlControls);
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
                SetStatus($"Error showing preview: {ex.Message}", StatusType.Error);
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
                MessageBox.Show($"Export Failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Color GenerateColorFromString(string input)
        {
            int hash = Math.Abs(input.GetHashCode());
            // Generate pastel colors
            return Color.FromArgb((hash % 50) + 200, ((hash / 256) % 50) + 200, ((hash / 65536) % 50) + 200);
        }

        #endregion
    }

    public static class GuiHelper
    {
        private const int EM_SETCUEBANNER = 0x1501;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern Int32 SendMessage(IntPtr hWnd, int msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

        public static void SetPlaceholder(TextBox textBox, string placeholderText)
        {
            SendMessage(textBox.Handle, EM_SETCUEBANNER, 0, placeholderText);
        }
    }
}