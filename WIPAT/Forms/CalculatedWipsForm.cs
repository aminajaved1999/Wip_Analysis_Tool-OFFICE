using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WIPAT.BLL.Interfaces;
using WIPAT.DAL.Interfaces;
using WIPAT.Entities;
using WIPAT.Entities.Dto;
using WIPAT.Entities.Enum;
using WIPAT.Helpers;

namespace WIPAT
{
    public partial class CalculatedWipsForm : Form
    {
        #region Fields & Enums
        private readonly IWipRepository _wipRepository;
        private readonly IExcelService _excelService;
        private readonly int _loggedInUserId;

        private enum AppState { Idle, ViewingDB, PreviewingUpdate }
        private AppState _currentState = AppState.Idle;

        private List<WipDetail> _currentWipData = new List<WipDetail>();
        private List<WipDetail> _pendingUpdates = new List<WipDetail>();
        private List<WipDetail> _currentDisplayList = new List<WipDetail>();
        private string _selectedMonth = string.Empty;
        private string _selectedYear = string.Empty;
        #endregion

        #region Constructor & Initialization
        public CalculatedWipsForm(IWipRepository wipRepository, IExcelService excelService, int loggedInUserId)
        {
            InitializeComponent();
            ApplyTheme();

            _wipRepository = wipRepository;
            _excelService = excelService;
            _loggedInUserId = loggedInUserId;

            this.Load += async (s, e) => await LoadPeriodsAsync();
            cmbPeriods.SelectedIndexChanged += async (s, e) => await LoadWipDetailsAsync();
            btnLoadDetails.Click += async (s, e) => await LoadWipDetailsAsync();

            btnExport.Click += BtnExport_Click;
            btnPreviewExcelUpdate.Click += BtnPreviewExcelUpdate_Click;
            btnCommitToDb.Click += async (s, e) => await CommitUpdatesAsync();

            btnSearch.Click += BtnSearch_Click;
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) BtnSearch_Click(s, e); };

            // Hook the cell formatting so integers (0,1,2) display as beautiful words
            dgvWipDetails.CellFormatting += DataGridView_ItemStatus_CellFormatting;

            //dgvWipDetails.DataBindingComplete += (s, e) =>
                //UITheme.UpdateGridSummaryCounts(dgvWipDetails, lblTotalItems, lblActiveItems, lblInactiveItems, lblInvalidItems);

            UpdateUIState();
        }
        #endregion

        #region UI & Theme Setup
        private void ApplyTheme()
        {
            UITheme.SetFormIcon(this);
            this.BackColor = UITheme.BackgroundCanvas;
            headerPanel.BackColor = UITheme.MainColor;
            toolbarPanel.BackColor = UITheme.BackgroundCanvas;
            pnlSearch.BackColor = UITheme.BackgroundCanvas;
            pnlFilter.BackColor = UITheme.BackgroundCanvas;

            lblTitle.ForeColor = UITheme.SurfaceWhite;
            lblSearch.ForeColor = UITheme.TextSecondaryColor;
            lblPeriod.ForeColor = UITheme.TextSecondaryColor;
            lblModeIndicator.ForeColor = UITheme.SurfaceWhite;

            UITheme.StyleButton(btnLoadDetails, AppButtonStyle.LoadDbData);
            UITheme.StyleButton(btnExport, AppButtonStyle.ExportToExcel);
            UITheme.StyleButton(btnPreviewExcelUpdate, AppButtonStyle.PreviewUpdate);
            UITheme.StyleButton(btnCommitToDb, AppButtonStyle.ApproveAndSave);
            UITheme.StyleButton(btnSearch, AppButtonStyle.Search);

            UITheme.StyleGrid(dgvWipDetails, isValid: true);
        }

        // Apply exactly the same colors as in ItemsCatalogueForm
        private void DataGridView_ItemStatus_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            var dgv = sender as DataGridView;
            if (dgv != null && e.ColumnIndex >= 0 && e.RowIndex >= 0 && e.Value != null && e.Value != DBNull.Value)
            {
                string colName = dgv.Columns[e.ColumnIndex].Name;

                if (colName == "ItemStatus")
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
                        // Fallback safely just in case it is already parsed as string
                        if (valStr.Equals("Inactive", StringComparison.OrdinalIgnoreCase))
                        {
                            e.CellStyle.ForeColor = Color.DarkGray;
                        }
                        else if (valStr.Equals("Active", StringComparison.OrdinalIgnoreCase) || valStr.Equals("Valid", StringComparison.OrdinalIgnoreCase))
                        {
                            e.CellStyle.ForeColor = Color.Green;
                        }
                        else if (valStr.Equals("Invalid", StringComparison.OrdinalIgnoreCase) || valStr.Equals("Missing", StringComparison.OrdinalIgnoreCase))
                        {
                            e.CellStyle.ForeColor = Color.Red;
                        }
                    }
                }
            }
        }

        private void UpdateUIState()
        {
            switch (_currentState)
            {
                case AppState.Idle:
                    btnLoadDetails.Visible = true;
                    btnLoadDetails.Text = "View WIPs";
                    UITheme.StyleButton(btnLoadDetails, AppButtonStyle.LoadDbData);

                    btnExport.Visible = false;
                    btnPreviewExcelUpdate.Visible = false;
                    btnCommitToDb.Visible = false;
                    cmbPeriods.Enabled = true;

                    lblTitle.Text = "Calculated WIPs";
                    lblModeIndicator.Visible = false;
                    pnlSearch.Visible = false;
                    break;

                case AppState.ViewingDB:
                    btnLoadDetails.Visible = true;
                    btnLoadDetails.Text = "Refresh WIPs";
                    UITheme.StyleButton(btnLoadDetails, AppButtonStyle.Refresh);

                    btnExport.Visible = true;
                    btnPreviewExcelUpdate.Visible = true;
                    btnCommitToDb.Visible = false;
                    cmbPeriods.Enabled = true;

                    lblTitle.Text = $"Viewing WIPs - {_selectedMonth} {_selectedYear} ({_currentWipData.Count} Items)";

                    lblModeIndicator.BackColor = UITheme.Success_Color;
                    lblModeIndicator.Text = "LIVE VIEW: Showing current WIP records.";
                    lblModeIndicator.Visible = true;
                    pnlSearch.Visible = true;
                    break;

                case AppState.PreviewingUpdate:
                    btnLoadDetails.Visible = true;
                    btnLoadDetails.Text = "Cancel Import";
                    UITheme.StyleButton(btnLoadDetails, AppButtonStyle.Secondary);

                    btnExport.Visible = false;
                    btnPreviewExcelUpdate.Visible = false;
                    btnCommitToDb.Visible = true;
                    cmbPeriods.Enabled = false;

                    lblTitle.Text = $"Import Preview - {_pendingUpdates.Count} updates.";

                    lblModeIndicator.BackColor = UITheme.Refresh_Color;
                    lblModeIndicator.Text = "PREVIEW MODE: Reviewing imported updates. Click 'Review Changes' to continue.";
                    lblModeIndicator.Visible = true;
                    pnlSearch.Visible = false;
                    break;
            }

            int startX = 230;
            int spacing = 10;

            if (btnLoadDetails.Visible)
            {
                btnLoadDetails.Left = startX;
                startX += btnLoadDetails.Width + spacing;
            }
            if (btnExport.Visible)
            {
                btnExport.Left = startX;
                startX += btnExport.Width + spacing;
            }
            if (btnPreviewExcelUpdate.Visible)
            {
                btnPreviewExcelUpdate.Left = startX;
                startX += btnPreviewExcelUpdate.Width + spacing;
            }
            if (btnCommitToDb.Visible)
            {
                btnCommitToDb.Left = startX;
            }
        }
        #endregion

        #region Data Loading & Binding
        private void BindGrid(IEnumerable<WipDetail> data)
        {
            _currentDisplayList = data.ToList();

            var displayData = _currentDisplayList.Select(d => new {
                d.CASIN,
                WIP_Quantity = d.WipQuantity,
                d.ItemStatus // Kept for cell formatting colorization
            }).ToList();

            dgvWipDetails.DataSource = null;
            dgvWipDetails.DataSource = displayData;

            // Update counts reliably based on actual WipDetail items
            UpdateDisplayCounts();
        }

        private void UpdateDisplayCounts()
        {
            if (_currentDisplayList == null)
            {
                lblTotalItems.Text = "Total Items: 0";
                lblActiveItems.Text = "Active: 0";
                lblInactiveItems.Text = "Deactivated: 0";
                lblInvalidItems.Text = "Invalid: 0";
                return;
            }

            int total = _currentDisplayList.Count;
            int active = _currentDisplayList.Count(w => w.ItemStatus == 1);
            int inactive = _currentDisplayList.Count(w => w.ItemStatus == 0);
            int invalid = _currentDisplayList.Count(w => w.ItemStatus == 2);

            lblTotalItems.Text = $"Total Items: {total}";
            lblActiveItems.Text = $"Active: {active}";
            lblInactiveItems.Text = $"Deactivated: {inactive}";
            lblInvalidItems.Text = $"Invalid: {invalid}";
        }

        private int GetMonthIndex(string monthName)
        {
            if (string.IsNullOrWhiteSpace(monthName)) return 0;

            string lowerMonth = monthName.ToLower();
            if (lowerMonth.Contains("january")) return 1;
            if (lowerMonth.Contains("february")) return 2;
            if (lowerMonth.Contains("march")) return 3;
            if (lowerMonth.Contains("april")) return 4;
            if (lowerMonth.Contains("may")) return 5;
            if (lowerMonth.Contains("june")) return 6;
            if (lowerMonth.Contains("july")) return 7;
            if (lowerMonth.Contains("august")) return 8;
            if (lowerMonth.Contains("september")) return 9;
            if (lowerMonth.Contains("october")) return 10;
            if (lowerMonth.Contains("november")) return 11;
            if (lowerMonth.Contains("december")) return 12;

            return 0;
        }

        private async Task LoadPeriodsAsync()
        {
            SetStatus("Loading available periods...", StatusType.Warning);
            try
            {
                var response = _wipRepository.GetForecastsWithCalculatedWip();
                if (response.Success && response.Data != null && response.Data.Any())
                {
                    cmbPeriods.Items.Clear();
                    foreach (var f in response.Data.OrderByDescending(f => f.Year).ThenByDescending(f => GetMonthIndex(f.Month)).ThenByDescending(f => f.Month))
                    {
                        cmbPeriods.Items.Add(new { Text = $"{f.Month} {f.Year}", Month = f.Month, Year = f.Year });
                    }
                    cmbPeriods.DisplayMember = "Text";
                    cmbPeriods.ValueMember = "Text";
                    cmbPeriods.SelectedIndex = 0;
                    SetStatus("Periods loaded.", StatusType.Success);
                }
                else
                {
                    SetStatus("No WIP data available.", StatusType.Warning);
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"An unexpected error occurred while loading available periods: {ex.Message}"
                                  + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                                  + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "");

                SetStatus("Failed to load periods.", StatusType.Error);
                MessageBox.Show(errorMsg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadWipDetailsAsync()
        {
            if (cmbPeriods.SelectedItem == null) return;

            txtSearch.Clear();

            if (_currentState == AppState.PreviewingUpdate)
            {
                _pendingUpdates.Clear();
                _currentState = AppState.Idle;
            }

            dynamic selection = cmbPeriods.SelectedItem;
            _selectedMonth = selection.Month;
            _selectedYear = selection.Year;

            SetStatus($"Loading details for {_selectedMonth} {_selectedYear}...", StatusType.Warning);

            try
            {
                var response = await _wipRepository.GetWipDetailsByPeriodAsync(_selectedMonth, _selectedYear);
                if (response.Success && response.Data != null)
                {
                    _currentWipData = response.Data;
                    BindGrid(_currentWipData);
                    _currentState = AppState.ViewingDB;
                    UpdateUIState();
                    SetStatus("WIP details loaded successfully.", StatusType.Success);
                }
                else
                {
                    SetStatus("Failed to load WIP details.", StatusType.Error);
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"An unexpected error occurred while loading WIP details: {ex.Message}"
                                  + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                                  + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "");

                SetStatus("Error loading details.", StatusType.Error);
                MessageBox.Show(errorMsg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion

        #region Event Handlers
        private void BtnExport_Click(object sender, EventArgs e)
        {
            if (_currentWipData == null || !_currentWipData.Any()) return;

            try
            {
                var exportData = _currentWipData.Select(d => new { d.CASIN, d.WipQuantity }).ToList();
                string periodLabel = $"{_selectedMonth} {_selectedYear}";
                string fileName = $"{periodLabel}-Wip.xlsx";
                string worksheetName = $"{periodLabel}-Wip";

                _excelService.ExportWipDataToExcel(exportData, fileName, worksheetName);
                SetStatus("Data exported successfully.", StatusType.Success);
            }
            catch (Exception ex)
            {
                string errorMsg = $"An unexpected error occurred while exporting data to Excel: {ex.Message}"
                                  + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                                  + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "");

                SetStatus("Export failed.", StatusType.Error);
                MessageBox.Show(errorMsg, "Export Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnPreviewExcelUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                using (var ofd = new OpenFileDialog { Filter = "Excel Files (*.xlsx)|*.xlsx", Title = "Upload updated WIP" })
                {
                    if (ofd.ShowDialog() != DialogResult.OK) return;

                    var parseResult = _excelService.ReadEditWipExcel(ofd.FileName);
                    if (!parseResult.Success)
                    {
                        MessageBox.Show(parseResult.Message, "Invalid File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    _pendingUpdates = parseResult.Data;
                    if (_pendingUpdates == null || _pendingUpdates.Count == 0) return;

                    var table = new DataTable();
                    table.Columns.Add("CASIN", typeof(string));
                    table.Columns.Add("CurrentWip", typeof(int));
                    table.Columns.Add("ProposedWip", typeof(int));
                    table.Columns.Add("Delta", typeof(int));

                    var currentMap = _currentWipData.ToDictionary(x => x.CASIN, x => x.WipQuantity ?? 0, StringComparer.OrdinalIgnoreCase);

                    foreach (var update in _pendingUpdates)
                    {
                        int current = currentMap.ContainsKey(update.CASIN) ? currentMap[update.CASIN] : 0;
                        int proposed = update.UserWipQty ?? 0;
                        table.Rows.Add(update.CASIN, current, proposed, proposed - current);
                    }

                    dgvWipDetails.DataSource = table;
                    _currentState = AppState.PreviewingUpdate;
                    UpdateUIState();

                    SetStatus("Excel loaded. Review changes and proceed to approve.", StatusType.Success);
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"An unexpected error occurred while previewing the Excel update: {ex.Message}"
                                  + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                                  + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "");

                SetStatus("Failed to read Excel.", StatusType.Error);
                MessageBox.Show(errorMsg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task CommitUpdatesAsync()
        {
            // 1. Validate all pending updates against their respective CasePack values immediately
            var currentWipMap = _currentWipData.ToDictionary(w => w.CASIN, w => w.CasePack ?? 0, StringComparer.OrdinalIgnoreCase);

            foreach (var update in _pendingUpdates)
            {
                if (currentWipMap.TryGetValue(update.CASIN, out int casePack) && casePack > 0)
                {
                    int proposedQty = update.UserWipQty ?? 0;
                    if (proposedQty % casePack != 0)
                    {
                        MessageBox.Show(
                            $"Validation Error: The proposed WIP quantity ({proposedQty}) for CASIN {update.CASIN} is not a multiple of its defined Case Pack ({casePack}). The import process has been aborted.",
                            "Case Pack Validation Failed",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);

                        SetStatus("Validation failed: Case Pack mismatch.", StatusType.Error);
                        return; // Abort immediately, do not proceed to review or commit
                    }
                }
            }

            // 2. Proceed with confirmation and database commit if validation passes
            var confirm = MessageBox.Show(
                $"Proceed to review {_pendingUpdates.Count} updates for {_selectedMonth} {_selectedYear}?",
                "Review Updates", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            SetStatus("Preparing updates for review...", StatusType.Warning);

            try
            {
                var resp = await _wipRepository.AddUserWipQtyForPeriodAsync(_selectedMonth, _selectedYear, _pendingUpdates, _loggedInUserId);

                if (resp.Success)
                {
                    SetStatus("Updates ready for review.", StatusType.Success);

                    var stagedDataResponse = await _wipRepository.GetWipDetailsByPeriodAsync(_selectedMonth, _selectedYear);

                    if (stagedDataResponse.Success && stagedDataResponse.Data != null)
                    {
                        ShowWipApprovalForm(_selectedMonth, _selectedYear, stagedDataResponse.Data);
                    }

                    _pendingUpdates.Clear();
                    await LoadWipDetailsAsync();
                }
                else
                {
                    SetStatus("Failed to prepare updates.", StatusType.Error);
                    MessageBox.Show(resp.Message, "Preparation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"An unexpected error occurred while preparing updates for review: {ex.Message}"
                    + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                    + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "");

                SetStatus("Unexpected error while preparing updates.", StatusType.Error);
                MessageBox.Show(errorMsg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            if (_currentState != AppState.ViewingDB) return;

            string keyword = txtSearch.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(keyword))
            {
                BindGrid(_currentWipData);
                SetStatus("Filter cleared.", StatusType.Success);
                return;
            }

            var filtered = _currentWipData.Where(x =>
                !string.IsNullOrEmpty(x.CASIN) && x.CASIN.ToLower().Contains(keyword)
            ).ToList();

            BindGrid(filtered);
            SetStatus($"Found {filtered.Count} matching WIPs.", StatusType.Success);
        }
        #endregion

        #region Helpers & Themed Status
        private void SetStatus(string message, StatusType statusType)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => SetStatus(message, statusType)));
                return;
            }

            statusLabel.Text = string.IsNullOrEmpty(message) ? "Ready" : message;
            statusLabel.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            statusStrip.BackColor = UITheme.BackgroundCanvas;

            switch (statusType)
            {
                case StatusType.Success:
                    statusLabel.BackColor = UITheme.Success_Color;
                    statusLabel.ForeColor = Color.White;
                    break;
                case StatusType.Error:
                    statusLabel.BackColor = UITheme.GridErrorHeader;
                    statusLabel.ForeColor = Color.White;
                    break;
                case StatusType.Warning:
                    statusLabel.BackColor = UITheme.WarningColor;
                    statusLabel.ForeColor = Color.Black;
                    break;
                default:
                    statusLabel.BackColor = Color.Transparent;
                    statusLabel.ForeColor = Color.DimGray;
                    break;
            }
            statusStrip.Refresh();
        }

        #endregion

        #region Wip Approval Form 
        public void ShowWipApprovalForm(string month, string year, List<WipDetail> details)
        {
            try
            {
                var table = new DataTable();
                table.Columns.Add("CASIN", typeof(string));
                table.Columns.Add("ItemStatus", typeof(string)); // Make ItemStatus visible here
                table.Columns.Add("CurrentWip", typeof(int));
                table.Columns.Add("ProposedWip", typeof(int));
                table.Columns.Add("Delta", typeof(int));

                bool hasChangesToApprove = false;

                foreach (var d in details)
                {
                    int current = d.WipQuantity ?? 0;
                    int proposed = d.UserWipQty ?? current;

                    if (current == proposed) continue;

                    int delta = proposed - current;
                    table.Rows.Add(d.CASIN, d.ItemStatus, current, proposed, delta);
                    hasChangesToApprove = true;
                }

                if (!hasChangesToApprove)
                {
                    MessageBox.Show("There are no pending changes requiring approval.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var form = new Form
                {
                    Text = $"Finalize Updates for {month} {year}",
                    Width = 550,
                    Height = 650,
                    StartPosition = FormStartPosition.CenterParent,
                    BackColor = UITheme.BackgroundCanvas
                };

                var dgv = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    AllowUserToOrderColumns = false,
                    MultiSelect = true,
                    DataSource = table
                };

                // Colorize the approval form too!
                dgv.CellFormatting += DataGridView_ItemStatus_CellFormatting;

                UITheme.StyleGrid(dgv);

                var panel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Top,
                    Height = 55,
                    FlowDirection = FlowDirection.LeftToRight,
                    Padding = new Padding(8),
                    BackColor = UITheme.GridHeaderNavy
                };

                var btnApproveAll = new Button
                {
                    Text = "Approve & Apply",
                    Width = 140,
                    Height = 35
                };
                UITheme.StyleButton(btnApproveAll, AppButtonStyle.ApproveAndSave);
                panel.Controls.Add(btnApproveAll);

                var lblSearch = new Label
                {
                    Text = "Search CASIN:",
                    Width = 100,
                    TextAlign = ContentAlignment.MiddleRight,
                    ForeColor = Color.White,
                    Padding = new Padding(0, 10, 0, 0),
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold)
                };
                var txtSearchPopup = new TextBox { Width = 150, Margin = new Padding(0, 8, 0, 0), Font = new Font("Segoe UI", 9.5F) };

                var btnSearchPopup = new Button
                {
                    Text = "Search",
                    Width = 75,
                    Height = 30,
                    Margin = new Padding(10, 3, 0, 0)
                };
                UITheme.StyleButton(btnSearchPopup, AppButtonStyle.Search);

                panel.Controls.Add(lblSearch);
                panel.Controls.Add(txtSearchPopup);
                panel.Controls.Add(btnSearchPopup);

                async Task Approve(Func<DataRow, bool> rowFilter)
                {
                    try
                    {
                        var updates = new List<WipDetail>();
                        foreach (DataRow r in table.Rows)
                        {
                            if (!rowFilter(r)) continue;

                            string casin = r.Field<string>("CASIN");
                            int current = r.Field<int>("CurrentWip");
                            int proposed = r.Field<int>("ProposedWip");
                            if (current == proposed) continue;

                            // Validate against CasePack
                            var detailItem = details.FirstOrDefault(d => d.CASIN.Equals(casin, StringComparison.OrdinalIgnoreCase));
                            if (detailItem != null && detailItem.CasePack.HasValue && detailItem.CasePack.Value > 0)
                            {
                                if (proposed % detailItem.CasePack.Value != 0)
                                {
                                    MessageBox.Show(
                                        $"Validation Failed: The proposed WIP quantity ({proposed}) for CASIN {casin} is not a multiple of its Case Pack ({detailItem.CasePack.Value}). No records will be updated.",
                                        "Invalid Quantity - Case Pack Mismatch",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Error);
                                    return; // Abort entire batch immediately
                                }
                            }

                            updates.Add(new WipDetail
                            {
                                CASIN = casin,
                                UserWipQty = proposed
                            });
                        }

                        if (updates.Count == 0)
                        {
                            MessageBox.Show("No changed values found to approve.", "Information",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }

                        var confirm = MessageBox.Show(
                            $"Approve and apply {updates.Count} updates for {month} {year}?\n\n" +
                            "This will update the official WIP quantities and synchronize the forecast.",
                            "Confirm Final Approval",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                        if (confirm != DialogResult.Yes) return;

                        var resp = await _wipRepository.UpdateWipForPeriodAsync(month, year, updates, _loggedInUserId);
                        if (resp.Success)
                        {
                            MessageBox.Show("WIP changes approved and forecast synchronized successfully!",
                                                "Approval Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            form.DialogResult = DialogResult.OK;
                            form.Close();
                        }
                        else
                        {
                            MessageBox.Show($"Update failed:\n{resp.Message}", "Database Abort",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        string errorMsg = $"An unexpected error occurred while saving the approvals: {ex.Message}";
                        MessageBox.Show(errorMsg, "Critical Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                btnApproveAll.Click += async (s, e) =>
                {
                    await Approve(r =>
                    {
                        int cur = r.Field<int>("CurrentWip");
                        int prop = r.Field<int>("ProposedWip");
                        return cur != prop;
                    });
                };

                btnSearchPopup.Click += (s, e) =>
                {
                    string searchValue = txtSearchPopup.Text.Trim();
                    if (string.IsNullOrEmpty(searchValue))
                    {
                        dgv.DataSource = table;
                    }
                    else
                    {
                        var filteredRows = table.AsEnumerable()
                                                 .Where(r => r.Field<string>("CASIN")
                                                 .IndexOf(searchValue, StringComparison.OrdinalIgnoreCase) >= 0);

                        if (!filteredRows.Any())
                        {
                            MessageBox.Show("No CASIN found matching the search criteria.", "Search Result",
                                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }

                        dgv.DataSource = filteredRows.CopyToDataTable();
                    }
                };

                form.Controls.Add(dgv);
                form.Controls.Add(panel);
                form.ShowDialog();
            }
            catch (Exception ex)
            {
                string errorMsg = $"An unexpected error occurred while displaying the approval UI: {ex.Message}"
                                  + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                                  + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "");

                MessageBox.Show(errorMsg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion
    }
}