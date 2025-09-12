using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using OfficeOpenXml;
using WIPAT.BLL;
using WIPAT.Entities;
using WIPAT.Entities.Dto;
using WIPAT.Entities.Enum;
using WIPAT.Helpers;

namespace WIPAT
{
    public partial class CalculateWIPForm : Form
    {
        private readonly WipSession _session;
        private readonly Action<string, StatusType> _setStatus;

        private readonly WipManager wipManager = new WipManager();
        private readonly ExcelHelper excelHelper = new ExcelHelper();

        private int percentage;
        private string WipProcessingType;
        private string wipStatus;
        private DataTable FinalDataTable;

        // Cached preview window so it isn’t disposed when switching steps
        private Form _wipFormCached;
        private BusyOverlayHelper _busyHelper;

        // Let MainForm know when Step 2 is completed
        public event Action CalculationCompleted;

        public CalculateWIPForm(WipSession session, Action<string, StatusType> setStatus)
        {
            InitializeComponent();
            _session = session;
            _setStatus = setStatus;

            // UX wiring
            this.FormClosing += CalculateWIPForm_FormClosing;
            checkBoxMOQ.CheckedChanged += checkBoxMOQ_CheckedChanged;
            radioButtonPercentage.CheckedChanged += radioButtonPercentage_CheckedChanged;
            textBoxMOQ.Validating += textBoxMOQ_Validating;
            textBoxPercentage.Validating += textBoxPercentage_Validating;

            btnReviewWIP.Click += btnReviewWIP_Click;
            btnApproveWIP.Click += btnApproveWIP_Click;
            // btnDownloadWip.Click += btnDownloadWip_Click;

            // Initial UI state
            textBoxMOQ.Visible = false;
            textBoxMOQ.Enabled = false;

            textBoxPercentage.Visible = radioButtonPercentage.Checked;
            textBoxPercentage.Enabled = radioButtonPercentage.Checked;

            // Guard: make sure uploads & metadata exist; show exactly what's missing
            if (!TryValidateReadyForCalculation(out var missingMsg, checkBoxCasePack.Checked))
            {
                SetStatus(missingMsg, StatusType.Warning);
            }

            _busyHelper = new BusyOverlayHelper(this, progressBar1, SetStatusThreadSafe);

        }

        // === NEW: restore UI when the form is re-shown ===
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

                    // keep percentage textbox visibility in sync
                    textBoxPercentage.Visible = radioButtonPercentage.Checked;
                    textBoxPercentage.Enabled = radioButtonPercentage.Checked;
                }

                // If a final table already exists (from a previous review), re-show preview & enable Approve
                if (_session.FinalDataTable != null && _session.FinalDataTable.Rows.Count > 0)
                {
                    FinalDataTable = _session.FinalDataTable; // local ref for convenience
                    ShowWipTable(FinalDataTable, _session.TargetMonth);
                    btnApproveWIP.Enabled = true;
                }
                else
                {
                    btnApproveWIP.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Error while restoring Calculate view: {ex.Message}", StatusType.Error);
            }
        }

        private void SetStatus(string message, StatusType statusType) => _setStatus?.Invoke(message, statusType);

        private bool TryValidateReadyForCalculation(out string message, bool requireCasePack)
        {
            var missing = new List<string>();

            if (_session == null)
            {
                message = "Internal error: session is null.";
                return false;
            }

            // Forecasts
            if (_session.ForecastFiles == null || _session.ForecastFiles.Count == 0)
                missing.Add("Forecast files (none loaded)");
            else if (_session.ForecastFiles.Count < 2)
                missing.Add($"At least 2 forecast files (currently {_session.ForecastFiles.Count})");

            // Optional but useful
            if (_session.AsinList == null || _session.AsinList.Count == 0)
                missing.Add("ASIN list (not extracted from forecasts)");

            // Stock / Orders
            if (_session.Stock == null)
                missing.Add("Stock file");
            if (_session.Orders == null)
                missing.Add("Order file");

            // Forecast masters
            if (_session.Prev == null)
                missing.Add("Previous forecast (Prev)");
            if (_session.Curr == null)
                missing.Add("Current forecast (Curr)");

            // Metadata / selections
            if (string.IsNullOrWhiteSpace(_session.WipType))
                missing.Add("WIP Type (Analyst / Layman / LaymanFormula)");
            if (string.IsNullOrWhiteSpace(_session.TargetMonth))
                missing.Add("Target month (e.g., \"August 2025\")");
            if (string.IsNullOrWhiteSpace(_session.CurrentMonth))
                missing.Add("Current month (e.g., \"July 2025\")");
            if (string.IsNullOrWhiteSpace(_session.CurrentMonthWithYear))
                missing.Add("Current month with year (e.g., \"July 2025\")");

            // CasePack dependency (only if user enabled it)
            if (requireCasePack && (_session.ItemCatalogue == null || _session.ItemCatalogue.Count == 0))
                missing.Add("Item catalogue (required for CasePack adjustment)");

            // CommitmentPeriod (used during export filtering)
            if (_session.CommitmentPeriod <= 0)
                missing.Add("Commitment period");

            if (missing.Count > 0)
            {
                message = "Please Provide Following:\n• " + string.Join("\n• ", missing);
                return false;
            }

            message = string.Empty;
            return true;
        }

        #region UI Event Handlers

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
                    MessageBox.Show("Please enter a positive integer for MOQ.");
                    e.Cancel = true;
                }
            }
        }

        private void radioButtonPercentage_CheckedChanged(object sender, EventArgs e)
        {
            bool on = radioButtonPercentage.Checked;
            textBoxPercentage.Visible = on;
            textBoxPercentage.Enabled = on;
        }

        private void textBoxPercentage_Validating(object sender, CancelEventArgs e)
        {
            if (radioButtonPercentage.Checked)
            {
                if (!int.TryParse(textBoxPercentage.Text, out int result) || result <= 0 || result > 100)
                {
                    MessageBox.Show("Please enter a percentage between 1 and 100.");
                    e.Cancel = true;
                }
                else
                {
                    percentage = result;
                }
            }
        }

        private void CalculateWIPForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide(); // keep instance/state alive
            }
        }

        private string GetSelectedProcessingWipType()
        {
            if (radioButtonMonthOfSupply.Checked) return ProcessingWipType.MonthOfSupply.ToString();
            if (radioButtonPercentage.Checked) return ProcessingWipType.Percentage.ToString();
            if (radioButtonSystem.Checked) return ProcessingWipType.System.ToString();
            return string.Empty;
        }

        #endregion UI Event Handlers

        #region Review

        private async void btnReviewWIP_Click(object sender, EventArgs e)
        {
            SetStatus(string.Empty, StatusType.Reset);

            try
            {
                _busyHelper.ShowBusy("Calculating WIP...");
                await Task.Yield(); // let UI pump once before we start

                if (!TryValidateReadyForCalculation(out var missingMsg, checkBoxCasePack.Checked))
                {
                    SetStatus(missingMsg, StatusType.Error);
                    return;
                }

                WipProcessingType = GetSelectedProcessingWipType();
                if (string.IsNullOrEmpty(WipProcessingType))
                {
                    SetStatus("Please select a WIP processing type.", StatusType.Error);
                    return;
                }
                if (string.IsNullOrWhiteSpace(_session.WipType))
                {
                    SetStatus("Please select a WIP type.", StatusType.Error);
                    return;
                }

                if (WipProcessingType == radioButtonMonthOfSupply.Text)
                {
                    MessageBox.Show(
                        "The calculation will use data from the first 2 months only.",
                        "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                // Inputs
                int? MOQ = null;
                if (checkBoxMOQ.Checked)
                {
                    if (!int.TryParse(textBoxMOQ.Text, out int moqValue) || moqValue <= 0)
                    {
                        SetStatus("Please enter a valid positive integer for MOQ.", StatusType.Error);
                        return;
                    }
                    MOQ = moqValue;
                }

                if (radioButtonPercentage.Checked)
                {
                    if (!int.TryParse(textBoxPercentage.Text, out int pct) || pct <= 0 || pct > 100)
                    {
                        SetStatus("Please enter a percentage between 1 and 100.", StatusType.Error);
                        return;
                    }
                    percentage = pct;
                }

                // ---- Heavy work starts (run in background) ----
                _busyHelper.ShowBusy("Build WIP DataTable...");

                var tableResponse = await Task.Run(() =>
                    wipManager.nBuildCommonWipDataTable(
                        _session.AsinList,
                        _session.Prev,
                        _session.Curr,
                        _session.Curr.ForecastingFor,
                        _session.WipType,
                        WipProcessingType,
                        percentage
                    )
                );

                if (!tableResponse.Success)
                {
                    SetStatus($"Error generating {_session.WipType} stock table: {tableResponse.Message}", StatusType.Error);
                    return;
                }

                var stockTable = tableResponse.Data;

                _busyHelper.ShowBusy("Build final table...");

                var finalDataTableResponse = await Task.Run(() =>
                    MakeFinalTable(
                        _session.ItemCatalogue,
                        stockTable,
                        _session.Curr.Month,
                        $"{_session.Curr.Month} { _session.Curr.Year}",
                        _session.Curr.ForecastingFor,
                        _session.WipType,
                        MOQ
                    )
                );

                if (!finalDataTableResponse.Success)
                {
                    SetStatus($"Error generating Final Table: {finalDataTableResponse.Message}", StatusType.Error);
                    return;
                }

                FinalDataTable = finalDataTableResponse.Data;

                // ---- Back to UI work ----
                _busyHelper.ShowBusy("Show WIP Table...");
                ShowWipTable(FinalDataTable, _session.TargetMonth);

                SetStatus("WIP calculated successfully.", StatusType.Success);
                btnApproveWIP.Enabled = true;
                wipStatus = WipStatus.Reviewed.ToString();

                // Persist state in session so it restores when revisiting this step
                _session.FinalDataTable = FinalDataTable;
                _session.WipProcessingType = WipProcessingType;
                _session.WipStatus = WipStatus.Reviewed.ToString();
            }
            catch (Exception ex)
            {
                SetStatus($"An Exception occurred while Reviewing WIP: {ex.Message}", StatusType.Error);
            }
            finally
            {
                _busyHelper.HideBusy();
            }
        }

        public Response<DataTable> MakeFinalTable(List<ItemCatalogue> itemsCatalogueData, DataTable stockTable, string currentMonth, string currentMonthWithYear, string targetMonthYear, string wipType, int? MOQ)
        {
            var res = new Response<DataTable>();

            try
            {
                var targetMonthYearParts = targetMonthYear.Split(' ');
                if (targetMonthYearParts.Length < 2)
                {
                    res.Success = false;
                    res.Message = "Invalid target month format.";
                    return res;
                }

                string targetMonth = targetMonthYearParts[0];
                string targetYear = targetMonthYearParts[1];

                // Source column by wipType
                string stockColumn = string.Empty;
                if (wipType == WipType.Analyst.ToString())
                {
                    stockColumn = "Remaining";
                }
                else if (wipType == WipType.Layman.ToString() || wipType == WipType.LaymanFormula.ToString())
                {
                    stockColumn = "Remaining_Layman";
                }

                var requiredColumns = new List<string>
                {
                    "C-ASIN",
                    $"Requested_Quantity ({currentMonth})",
                    $"CommitmentPeriod ({currentMonth})",
                    $"{wipType}({currentMonth})",
                    stockColumn
                };

                foreach (var column in requiredColumns)
                {
                    if (!stockTable.Columns.Contains(column))
                    {
                        res.Success = false;
                        res.Message = $"Missing required column: '{column}' in stock table.";
                        return res;
                    }
                }

                // Final table schema
                DataTable finalDT = new DataTable();
                finalDT.Columns.Add("C-ASIN", typeof(string));
                finalDT.Columns.Add("Requested_Quantity", typeof(int));
                finalDT.Columns.Add("Commitment_Period", typeof(int));
                finalDT.Columns.Add("Month", typeof(string));
                finalDT.Columns.Add("Year", typeof(int));
                finalDT.Columns.Add("PO_Date", typeof(DateTime));
                finalDT.Columns.Add("Stock", typeof(string));
                finalDT.Columns.Add("Review_Wip", typeof(string));
                if (MOQ != null)
                {
                    finalDT.Columns.Add("MOQ_Wip", typeof(string));
                    finalDT.Columns.Add("MOQ", typeof(string));
                }
                if (checkBoxCasePack.Checked)
                {
                    finalDT.Columns.Add("CasePack_Wip", typeof(string));
                    finalDT.Columns.Add("CasePack", typeof(string));
                }

                foreach (DataRow stockRow in stockTable.Rows)
                {
                    DataRow newRow = finalDT.NewRow();

                    string casin = stockRow["C-ASIN"].ToString();
                    int requestedQuantity = Convert.ToInt32(stockRow[$"Requested_Quantity ({currentMonth})"]);
                    int commitmentPeriod = Convert.ToInt32(stockRow[$"CommitmentPeriod ({currentMonth})"]);
                    string stockMonthYear = stockRow["Month"].ToString();
                    var stockMonthYearParts = stockMonthYear.Split(' ');
                    string stockMonth = stockMonthYearParts[0];
                    string stockYear = stockMonthYearParts.Length > 1 ? stockMonthYearParts[1] : targetYear;

                    var matchingDetail = _session.Curr?.Details?.FirstOrDefault(d => d.CASIN == casin);
                    if (matchingDetail == null)
                    {
                        continue; // Skip this iteration if no matching detail
                    }

                    var poDate = matchingDetail?.PODate;

                    string reviewWip = stockRow[$"{wipType}({currentMonth})"].ToString();
                    string stock = stockRow[stockColumn].ToString();
                    int? moq_wip = null;
                    int? casepack_wip = null;

                    int? reviewWipValue = string.IsNullOrWhiteSpace(reviewWip) ? (int?)null : Convert.ToInt32(reviewWip);

                    newRow["C-ASIN"] = casin;
                    newRow["Requested_Quantity"] = requestedQuantity;
                    newRow["Commitment_Period"] = commitmentPeriod;
                    newRow["Month"] = stockMonth;
                    newRow["Year"] = stockYear;
                    newRow["PO_Date"] = poDate;
                    newRow["Stock"] = stock;
                    newRow["Review_Wip"] = reviewWip;

                    // MOQ adjust
                    if (reviewWipValue.HasValue && MOQ != null)
                    {
                        if (MOQ > reviewWipValue) moq_wip = 0;
                        else if (MOQ <= reviewWipValue) moq_wip = reviewWipValue;

                        if (moq_wip.HasValue) reviewWipValue = moq_wip;

                        newRow["MOQ_Wip"] = moq_wip;
                        newRow["MOQ"] = MOQ;
                    }

                    // CasePack adjust
                    if (checkBoxCasePack.Checked && reviewWipValue.HasValue)
                    {
                        int? casepackNullable = itemsCatalogueData?.FirstOrDefault(item => item.Casin == casin)?.CasePackQty;
                        if (casepackNullable.HasValue && casepackNullable.Value > 0)
                        {
                            double value = (double)reviewWipValue / casepackNullable.Value;
                            value = Math.Floor(value);
                            casepack_wip = (int)value * casepackNullable.Value;
                        }
                        newRow["CasePack_Wip"] = casepack_wip;
                        newRow["CasePack"] = casepackNullable;
                    }

                    finalDT.Rows.Add(newRow);
                }

                res.Success = true;
                res.Data = finalDT;
                res.Message = "Table processed successfully.";
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Message = $"Exception while processing rows for FinalTable: {ex.Message}";
            }

            return res;
        }

        private void ShowWipTable(DataTable stockTable, string targetMonth)
        {
            try
            {
                if (_wipFormCached == null || _wipFormCached.IsDisposed)
                {
                    _wipFormCached = new Form
                    {
                        Text = $"Remaining Stock & WIP for {targetMonth}",
                        Width = 1000,
                        Height = 600,
                        StartPosition = FormStartPosition.CenterParent,
                        AutoSize = true
                    };

                    var dgv = new DataGridView
                    {
                        Dock = DockStyle.Fill,
                        ReadOnly = true,
                        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                        AllowUserToOrderColumns = false,
                        AutoGenerateColumns = true,
                        DataSource = stockTable
                    };

                    // Optional row coloring by CASIN
                    dgv.CellFormatting += (gridSender, eventArgs) =>
                    {
                        if (eventArgs.RowIndex >= 0 && eventArgs.ColumnIndex >= 0)
                        {
                            if (dgv.Columns.Contains("C-ASIN"))
                            {
                                var idx = dgv.Columns["C-ASIN"].Index;
                                var cellValue = dgv.Rows[eventArgs.RowIndex].Cells[idx]?.Value;

                                if (cellValue != null)
                                {
                                    string cAsinValue = cellValue.ToString();
                                    Color generatedColor = GenerateColorFromString(cAsinValue);
                                    eventArgs.CellStyle.BackColor = generatedColor;
                                }
                                else
                                {
                                    eventArgs.CellStyle.BackColor = Color.Gray;
                                }
                            }
                        }
                    };

                    _wipFormCached.Controls.Add(dgv);
                }
                else
                {
                    // Rebind new data and update title
                    var dgv = _wipFormCached.Controls.OfType<DataGridView>().FirstOrDefault();
                    if (dgv != null) dgv.DataSource = stockTable;
                    _wipFormCached.Text = $"Remaining Stock & WIP for {targetMonth}";
                }

                _wipFormCached.Show(this);
                _wipFormCached.BringToFront();
            }
            catch (Exception ex)
            {
                SetStatus($"Error showing WIP preview: {ex.Message}", StatusType.Error);
            }
        }

        private Color GenerateColorFromString(string input)
        {
            int hash = input.GetHashCode();
            hash = Math.Abs(hash);

            // Light pastel-ish colors
            int red = (hash % 50) + 200;
            int green = ((hash / 256) % 50) + 200;
            int blue = ((hash / 65536) % 50) + 200;
            return Color.FromArgb(red, green, blue);
        }

        #endregion Review

        #region Approve

        private async void btnApproveWIP_Click(object sender, EventArgs e)
        {
            try
            {
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

                _busyHelper.ShowBusy("Saving Wip Records in Database...");

                var saveResponse = await wipManager.SaveWipRecordsAsync3(
                    FinalDataTable,
                    _session.Curr.FileName,
                    _session.WipType,
                    WipProcessingType,
                    _session.TargetMonth,
                    wipColName
                );

                if (!saveResponse.Success)
                {
                    SetStatus(saveResponse.Message, StatusType.Warning);
                    return;
                }

                SetStatus("WIP Saved Successfully.", StatusType.Success);
                wipStatus = WipStatus.Approved.ToString();
                _session.WipStatus = wipStatus; // persist status in session

                // Notify MainForm that Step 2 is complete (enables Step 3)
                CalculationCompleted?.Invoke();

                _busyHelper.ShowBusy("Updating Current Forecast Grid With Approved Wip value...");

                UpdateCurrentForecastGridWithApprovedWip();
            }
            catch (Exception ex)
            {
                SetStatus($"An Exception occurred while Approving WIP: {ex.Message}", StatusType.Error);
            }
            finally
            {
                _busyHelper.HideBusy();
            }
        }

        private string DetermineWipColumn(DataTable dataTable)
        {
            if (dataTable.Columns.Contains("CasePack_Wip"))
                return "CasePack_Wip";
            else if (dataTable.Columns.Contains("MOQ_Wip"))
                return "MOQ_Wip";
            else if (dataTable.Columns.Contains("Review_Wip"))
                return "Review_Wip";

            return string.Empty;
        }

        #endregion Approve

        // thread-safe status setter
        private void SetStatusThreadSafe(string msg, StatusType type)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
                BeginInvoke(new Action(() => _setStatus?.Invoke(msg, type)));
            else
                _setStatus?.Invoke(msg, type);
        }


        private void UpdateCurrentForecastGridWithApprovedWip()
        {
            try
            {
                if (_session?.ForecastFiles == null || _session.ForecastFiles.Count == 0) { return; }
                if (FinalDataTable == null || FinalDataTable.Rows.Count == 0) { return; }

                // Which column in FinalDataTable is "approved": CasePack_Wip -> MOQ_Wip -> Review_Wip
                string approvedWipColName = DetermineWipColumn(FinalDataTable);
                if (string.IsNullOrWhiteSpace(approvedWipColName)) { return; }

                // Find the current month forecast grid Datatable
                var currFile = _session.ForecastFiles.FirstOrDefault(f => f.ProjectionMonth == _session.CurrentMonth);
                if(currFile == null)
                {
                    SetStatus("Could not locate the current forecast grid to update.", StatusType.Warning);
                    return;
                }

                DataTable currentGridDataTable = currFile.FullTable;
                DataTable newSourceTableForWipValues = _session.FinalDataTable;

                // Clone structure of current table
                DataTable updatedTable = currentGridDataTable.Clone();

                // Import existing data
                foreach (DataRow row in currentGridDataTable.Rows)
                {
                    updatedTable.ImportRow(row);
                }

                foreach (DataRow sourceRow in newSourceTableForWipValues.Rows)
                {
                    string cAsin = sourceRow["C-ASIN"]?.ToString()?.Trim();
                    string requestedQty = sourceRow["Requested_Quantity"]?.ToString()?.Trim();
                    string commitmentPeriod = sourceRow["Commitment_Period"]?.ToString()?.Trim();
                    string poDate = sourceRow["PO_Date"]?.ToString()?.Trim();
                    string reviewWip = sourceRow["Review_Wip"]?.ToString()?.Trim();

                    foreach (DataRow targetRow in updatedTable.Rows)
                    {
                        bool isMatch =
                            string.Equals(targetRow["C-ASIN"]?.ToString()?.Trim(), cAsin, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(targetRow["Requested Quantity"]?.ToString()?.Trim(), requestedQty, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(targetRow["Commitment period"]?.ToString()?.Trim(), commitmentPeriod, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(targetRow["PO Date"]?.ToString()?.Trim(), poDate, StringComparison.OrdinalIgnoreCase);

                        if (isMatch)
                        {
                            //targetRow["WIP"] = reviewWip;
                            if (int.TryParse(reviewWip, out int wipValue))
                            {
                                targetRow["WIP"] = wipValue;
                            }
                            else
                            {
                                targetRow["WIP"] = DBNull.Value;
                            }
                            break; // Assuming unique match
                        }
                    }
                }


                var grid = currFile.BoundGrid;
                if (grid != null && !grid.IsDisposed)
                {
                    grid.DataSource = null;
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




    }
}
 