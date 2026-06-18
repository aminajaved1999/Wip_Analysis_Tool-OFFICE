using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WIPAT.BLL;
using WIPAT.BLL.Interfaces;
using WIPAT.BLL.Managers;
using WIPAT.BLL.Services;
using WIPAT.DAL;
using WIPAT.DAL.Interfaces;
using WIPAT.Entities;
using WIPAT.Entities.Dto;
using WIPAT.Entities.Enum;
using WIPAT.Entities.ExcelTemplateDefinitions;
using WIPAT.Helpers;

namespace WIPAT
{
    public partial class UploadForm : Form
    {
        public event Action InputsChanged;

        #region Fields & Dependencies
        private readonly WipSession _session;
        private readonly Action<string, StatusType> _setStatus;
        private readonly BusyOverlayHelper _busyHelper;

        private List<ForecastFileData> _forecastFiles;

        private readonly IExcelService _excelSerice;
        private readonly IForecastManager _forecastManager;
        private readonly IOrderManager _orderManager;
        private readonly IForecastRepository _forecastRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IItemsRepository _itemsRepository;

        private int _commitmentPeriod = 0;
        private bool _orderUploaded = false;
        #endregion

        #region Constructor & Initialization
        public UploadForm(
                    WipSession session,
                    IExcelService excelSerice,
                    IForecastManager forecastManager,
                    IOrderManager orderManager,
                    IForecastRepository forecastRepository,
                    IOrderRepository orderRepository,
                    IItemsRepository itemsRepository,
                    Action<string, StatusType> setStatus)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _excelSerice = excelSerice ?? throw new ArgumentNullException(nameof(excelSerice));
            _forecastManager = forecastManager ?? throw new ArgumentNullException(nameof(forecastManager));
            _orderManager = orderManager ?? throw new ArgumentNullException(nameof(orderManager));
            _forecastRepository = forecastRepository ?? throw new ArgumentNullException(nameof(forecastRepository));
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _itemsRepository = itemsRepository ?? throw new ArgumentNullException(nameof(itemsRepository));
            _setStatus = setStatus;

            _forecastFiles = new List<ForecastFileData>();

            InitializeComponent();
            ApplyTheme();
            SetupModernSearchBars();

            _busyHelper = new BusyOverlayHelper(this, progressBarTop, SetStatusThreadSafe);

            _commitmentPeriod = GetCommitmentPeriodFromConfig();
            _session.CommitmentPeriod = _commitmentPeriod;
        }

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Load local memory bindings instantly so the grid populates if data exists
            RebindFromSession();

            // Fetch DB data for the dropdowns in the background without freezing the UI
            await Task.WhenAll(
                LoadForecastDropdownAsync(),
                LoadOrderDropdownAsync()
            );
        }

        private void UploadForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
        }
        #endregion

        #region UI Setup & Styling
        private void ApplyTheme()
        {
            this.BackColor = UITheme.BackgroundCanvas;
            tabForecast.BackColor = UITheme.BackgroundCanvas;
            tabOrder.BackColor = UITheme.BackgroundCanvas;

            pnlGrid1Wrapper.BackColor = UITheme.SurfaceWhite;
            pnlOrderGridWrapper.BackColor = UITheme.SurfaceWhite;

            UITheme.SetFormIcon(this);
            UITheme.StyleButton(btnBrowseForecast, AppButtonStyle.Upload);
            UITheme.StyleButton(btnBrowseOrder, AppButtonStyle.Upload);
            UITheme.StyleButton(btnExportForecastErrors, AppButtonStyle.Secondary);
            UITheme.StyleButton(btnExportOrderErrors, AppButtonStyle.Secondary);
            UITheme.StyleButton(btnMarkInvalid, AppButtonStyle.Danger);

            UITheme.StyleGrid(dgvForecast1, true);
            UITheme.StyleGrid(dgvOrder, true);
            UITheme.StyleGrid(dgvForecastErrors, false);
            UITheme.StyleGrid(dgvOrderErrors, false);
        }

        private void SetupModernSearchBars()
        {
            BindSearchBarVisuals(pnlSearch1, txtSearchF1_Real, lblIcon1);
            BindSearchBarVisuals(pnlSearchOrder, txtSearchOrder_Real, lblIconOrder);

            txtSearchF1_Real.TextChanged += (s, e) => FilterGrid(dgvForecast1, txtSearchF1_Real.Text);
            txtSearchOrder_Real.TextChanged += (s, e) => FilterGrid(dgvOrder, txtSearchOrder_Real.Text);
        }

        private void BindSearchBarVisuals(Panel container, TextBox txt, Label lblIcon)
        {
            container.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, container.ClientRectangle,
                    UITheme.GridBorder, 1, ButtonBorderStyle.Solid,
                    UITheme.GridBorder, 1, ButtonBorderStyle.Solid,
                    UITheme.GridBorder, 1, ButtonBorderStyle.Solid,
                    UITheme.GridBorder, 1, ButtonBorderStyle.Solid
                );
            };

            txt.GotFocus += (s, e) =>
            {
                if (txt.Text.StartsWith("Search")) { txt.Text = ""; txt.ForeColor = UITheme.GridRowText; }
                container.BackColor = UITheme.SurfaceWhite;
                txt.BackColor = UITheme.SurfaceWhite;
            };

            txt.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txt.Text)) { txt.Text = "Search CASIN..."; txt.ForeColor = UITheme.TextSecondaryColor; }
                container.BackColor = UITheme.BackgroundCanvas;
                txt.BackColor = UITheme.BackgroundCanvas;
            };

            container.Click += (s, e) => txt.Focus();
            lblIcon.Click += (s, e) => txt.Focus();
        }

        private void tabControlMain_DrawItem(object sender, DrawItemEventArgs e)
        {
            Graphics g = e.Graphics;
            TabPage tabPage = tabControlMain.TabPages[e.Index];
            Rectangle tabBounds = tabControlMain.GetTabRect(e.Index);
            bool isSelected = (e.State == DrawItemState.Selected);

            Color backColor = isSelected ? UITheme.SurfaceWhite : UITheme.BackgroundCanvas;
            Color textColor = isSelected ? UITheme.MainColor : UITheme.TextSecondaryColor;
            Color bottomLineColor = isSelected ? UITheme.MainColor : UITheme.GridBorder;

            using (SolidBrush brush = new SolidBrush(backColor))
            {
                g.FillRectangle(brush, tabBounds);
            }

            Font font = new Font("Segoe UI", 11F, isSelected ? FontStyle.Bold : FontStyle.Regular);
            TextRenderer.DrawText(g, tabPage.Text, font, tabBounds, textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            int lineThickness = isSelected ? 3 : 1;
            using (Pen pen = new Pen(bottomLineColor, lineThickness))
            {
                g.DrawLine(pen, tabBounds.Left, tabBounds.Bottom - 1, tabBounds.Right, tabBounds.Bottom - 1);
            }
        }

        private void ConfigureErrorGrid(DataGridView dgv)
        {
            dgv.ReadOnly = false;

            foreach (DataGridViewColumn col in dgv.Columns)
            {
                if (col.Name != "chkSelect")
                {
                    col.ReadOnly = true;
                }

                // Hide redundant IsActive column so UI looks clean
                if (col.Name == "IsActive")
                {
                    col.Visible = false;
                }
            }
        }
        #endregion

        #region Forecast Logic
        private async Task LoadForecastDropdownAsync()
        {
            try
            {
                // Show visual feedback instantly
                cmbDbForecasts.DataSource = new List<string> { "Loading..." };

                // Offload the heavy database call to a background thread
                var dbResult = await Task.Run(() => _forecastRepository.GetAvailableForecastsFromDB());

                var items = new List<string> { "-- Select Saved Forecast --" };
                if (dbResult.Success && dbResult.Data != null)
                {
                    items.AddRange(dbResult.Data.Select(f => $"{f.Month}_{f.Year}").Distinct());
                }

                // Once the background task is done, update the UI
                cmbDbForecasts.DataSource = items;
            }
            catch (Exception ex)
            {
                string msg = $"An unexpected error occurred while loading the forecast dropdown: {ex.Message}"
                             + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                             + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "");
                SetStatusThreadSafe(msg, StatusType.Error);
                cmbDbForecasts.DataSource = new List<string> { "-- Select Saved Forecast --" };
            }
        }
        private async void cmbDbForecasts_SelectionChangeCommitted(object sender, EventArgs e)
        {
            try
            {
                string sel = cmbDbForecasts.SelectedItem?.ToString();

                // Ignore the placeholder item gracefully
                if (string.IsNullOrEmpty(sel) || sel.StartsWith("--")) return;

                var parts = sel.Split('_');
                string selectedMonth = parts[0];
                string selectedYear = parts[1];

                await LoadForecastDataCommonAsync(selectedMonth, selectedYear);
                ResetForecastDropdown();
            }
            catch (Exception ex)
            {
                string msg = $"An unexpected error occurred while processing the selected dropdown forecast: {ex.Message}"
                             + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                             + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "");
                SetStatusThreadSafe(msg, StatusType.Error);
                ResetForecastDropdown();
            }
        }

        private async Task ProcessForecastFile(string filePath)
        {
            _busyHelper.ShowBusy("Verifying File...");
            try
            {
                var previewResponse = await _forecastManager.GetForecastFilePreviewAsync(filePath, _commitmentPeriod);
                if (!previewResponse.Success)
                {
                    if (previewResponse.Data != null && previewResponse.Data.ProblemItemsTable != null && previewResponse.Data.ProblemItemsTable.Rows.Count > 0)
                    {
                        dgvForecastErrors.DataSource = previewResponse.Data.ProblemItemsTable;
                        ConfigureErrorGrid(dgvForecastErrors);
                        pnlForecastErrors.Visible = true;
                    }
                    _busyHelper.HideBusy();
                    SetStatusThreadSafe(previewResponse.Message, StatusType.Error);
                    MessageBox.Show(previewResponse.Message, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var newFile = previewResponse.Data;
                _busyHelper.HideBusy();
                _busyHelper.ShowBusy("Processing & Saving...");

                var currentListCopy = new List<ForecastFileData>();
                var result = await Task.Run(() => _forecastManager.HandleForecastFileAsync(filePath, currentListCopy, _commitmentPeriod, _session));

                if (result.Success)
                {
                    _forecastFiles.Clear();
                    _forecastFiles.Add(result.Data.First());

                    _session.IsContinueWithInactiveItems = result.IsContinueWithInactiveItems;

                    // Show the message box BEFORE we bind and update the UI grids
                    if (result.Message.Contains("IGNORED") || result.Message.Contains("already exists"))
                    {
                        MessageBox.Show(result.Message, "Existing Record Loaded", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        SetStatusThreadSafe($"Existing Forecast loaded: {newFile.ProjectionMonth}", StatusType.Warning);
                    }
                    else
                    {
                        SetStatusThreadSafe($"Forecast Loaded: {newFile.ProjectionMonth} {newFile.ProjectionYear}", StatusType.Success);
                    }

                    // NOW update the grids after the user has acknowledged the popup
                    BindForecastGrids();
                    await UpdateSessionAsync();
                    InputsChanged?.Invoke();

                    pnlForecastErrors.Visible = false;
                }
                else
                {
                    if (result.ProblemItemsTable != null && result.ProblemItemsTable.Rows.Count > 0)
                    {
                        dgvForecastErrors.DataSource = result.ProblemItemsTable;
                        ConfigureErrorGrid(dgvForecastErrors);
                        pnlForecastErrors.Visible = true;
                    }
                    SetStatusThreadSafe(result.Message, StatusType.Error);
                    MessageBox.Show(result.Message, "Upload Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                SetStatusThreadSafe($"Error: {ex.Message}", StatusType.Error);
                MessageBox.Show(ex.Message, "System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _busyHelper.HideBusy();
            }
        }

        private async Task LoadForecastDataCommonAsync(string month, string year)
        {
            _busyHelper.ShowBusy($"Loading {month} {year}...");
            try
            {
                var result = await Task.Run(() => _forecastManager.LoadExistingForecastAsync(month, year));
                _busyHelper.HideBusy();

                if (result.Success)
                {
                    var loadedFile = result.Data;

                    if (loadedFile.IsWipAlreadyCalculated)
                    {
                        MessageBox.Show($"WIP for {month} {year} is already calculated.\nYou cannot calculate it again.",
                            "WIP Already Calculated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    _forecastFiles.Clear();
                    _forecastFiles.Add(loadedFile);

                    BindForecastGrids();
                    await UpdateSessionAsync();
                    InputsChanged?.Invoke();

                    pnlForecastErrors.Visible = false;
                    SetStatusThreadSafe($"Loaded {month} {year} successfully.", StatusType.Success);
                }
                else
                {
                    SetStatusThreadSafe(result.Message, StatusType.Error);
                }
            }
            catch (Exception ex)
            {
                _busyHelper.HideBusy();
                SetStatusThreadSafe($"Error: {ex.Message}", StatusType.Error);
            }
        }
        #endregion

        #region Order Logic
        private async Task LoadOrderDropdownAsync()
        {
            try
            {
                // Show visual feedback instantly
                cmbDbOrders.DataSource = new List<string> { "Loading..." };

                // Offload the heavy database call to a background thread
                var dbResult = await Task.Run(() => _orderRepository.GetActualOrdersFromDatabase());

                var items = new List<string> { "-- Select Saved Order --" };

                if (dbResult.Success && dbResult.Data != null)
                {
                    var distinctOrders = dbResult.Data
                        .Select(o => new { o.Month, o.Year })
                        .Distinct()
                        .Select(o => $"{o.Month}_{o.Year}");

                    items.AddRange(distinctOrders);
                }

                // Once the background task is done, update the UI
                cmbDbOrders.DataSource = items;
            }
            catch (Exception ex)
            {
                string msg = $"An unexpected error occurred while loading the order dropdown: {ex.Message}"
                             + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                             + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "");
                SetStatusThreadSafe(msg, StatusType.Error);
                cmbDbOrders.DataSource = new List<string> { "-- Select Saved Order --" };
            }
        }

        private bool ValidateOrderPrerequisites(string targetMonth, string targetYear)
        {
            if (_forecastFiles.Count == 0)
            {
                ShowErrorDialog("Prerequisite Missing", "You must load the Current Forecast before loading Orders.");
                return false;
            }

            var targetForecast = _forecastFiles[0];
            bool match = string.Equals(targetMonth, targetForecast.ProjectionMonth, StringComparison.OrdinalIgnoreCase) &&
                         string.Equals(targetYear, targetForecast.ProjectionYear, StringComparison.OrdinalIgnoreCase);

            if (!match)
            {
                ShowErrorDialog("Date Mismatch",
                    $"Current Forecast: {targetForecast.ProjectionMonth} {targetForecast.ProjectionYear}\n" +
                    $"Selected Order:  {targetMonth} {targetYear}\n\n" +
                    "The Order file must match the Current Forecast month.");
                return false;
            }

            return true;
        }

        private async void cmbDbOrders_SelectionChangeCommitted(object sender, EventArgs e)
        {
            try
            {
                string sel = cmbDbOrders.SelectedItem?.ToString();

                // Ignore the placeholder item gracefully
                if (string.IsNullOrEmpty(sel) || sel.StartsWith("--")) return;

                var parts = sel.Split('_');
                if (!ValidateOrderPrerequisites(parts[0], parts[1]))
                {
                    ResetOrderDropdown();
                    return;
                }

                _busyHelper.ShowBusy($"Loading Orders {sel}...");
                var result = await Task.Run(() => _orderManager.LoadExistingOrderAsync(parts[0], parts[1]));
                HandleOrderLoadResult(result.Success, result.Data, result.Message);
            }
            catch (Exception ex)
            {
                string msg = $"An unexpected error occurred while loading the selected order: {ex.Message}"
                             + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                             + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "");
                SetStatusThreadSafe(msg, StatusType.Error);
            }
            finally
            {
                _busyHelper.HideBusy();
                ResetOrderDropdown();
            }
        }

        private async Task ProcessOrderFile(string filePath)
        {
            if (_forecastFiles.Count < 1)
            {
                ShowErrorDialog("Prerequisite Missing", "You must load the Current Forecast before uploading Orders.");
                return;
            }

            _busyHelper.ShowBusy("Processing Order File...");
            try
            {
                var result = await Task.Run(() => _orderManager.HandleOrderFileAsync(filePath, _session));
                if (!result.Success)
                {
                    if (result.Data != null && result.Data.ProblemItemsTable != null && result.Data.ProblemItemsTable.Rows.Count > 0)
                    {
                        SetStatus(result.Message, StatusType.Error);
                        dgvOrderErrors.DataSource = result.Data.ProblemItemsTable;
                        ConfigureErrorGrid(dgvOrderErrors);
                        pnlOrderErrors.Visible = true;
                        return;
                    }
                    SetStatus(result?.Message ?? "Unknown Error", StatusType.Error);
                    return;
                }

                if (result.Data.ValidOrders.Count > 0)
                {
                    var r = result.Data.ValidOrders.FirstOrDefault();
                    string m = r.Month;
                    string y = r.Year;

                    if (!ValidateOrderPrerequisites(m, y)) return;

                    if (result.Message.Contains("IGNORED") || result.Message.Contains("already exist"))
                    {
                        HandleOrderLoadResult(true, result.Data.DataTable, "Existing Orders loaded from Database.");
                        MessageBox.Show(result.Message, "Existing Record Loaded", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        HandleOrderLoadResult(true, result.Data.DataTable, "Orders uploaded successfully.");
                    }
                }
                else
                {
                    SetStatus("Order file is empty.", StatusType.Warning);
                }
            }
            catch (Exception ex)
            {
                string msg = $"An unexpected error occurred while processing the order file: {ex.Message}"
                             + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                             + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "");
                SetStatusThreadSafe(msg, StatusType.Error);
            }
            finally { _busyHelper.HideBusy(); }
        }

        private void HandleOrderLoadResult(bool success, DataTable data, string msg)
        {
            if (success)
            {
                _orderUploaded = true;
                _session.Orders = data;
                BindOrderGrid(_session.Orders);
                pnlOrderErrors.Visible = false;
                SetStatus("Orders loaded successfully.", StatusType.Success);
                InputsChanged?.Invoke();
            }
            else
            {
                SetStatus($"Failed: {msg}", StatusType.Error);
            }
        }
        #endregion

        #region Data Binding & Search Logic
        private (int Total, int Active, int Inactive, int Invalid) CalculateGridStats(DataTable dt)
        {
            if (dt == null) return (0, 0, 0, 0);

            string casinColName = MasterColumnCatalogue.Casin.Name;
            string itemStatusColName = MasterColumnCatalogue.ItemStatus.Name;

            // If the exact CASIN column isn't there, we can't group by it
            if (!dt.Columns.Contains(casinColName)) return (dt.Rows.Count, dt.Rows.Count, 0, 0);

            var groups = dt.AsEnumerable()
                .Where(r => r[casinColName] != DBNull.Value && !string.IsNullOrWhiteSpace(r[casinColName].ToString()))
                .GroupBy(r => r[casinColName].ToString().Trim(), StringComparer.OrdinalIgnoreCase);

            int total = groups.Count();
            int active = 0;
            int inactive = 0;
            int invalid = 0;

            bool hasItemStatus = dt.Columns.Contains(itemStatusColName);
            bool hasUiStatus = dt.Columns.Contains("Status");

            foreach (var group in groups)
            {
                var row = group.First();

                // Order Manager UI Status format (e.g. "Valid ✔")
                if (hasUiStatus && row["Status"] != DBNull.Value)
                {
                    string s = row["Status"].ToString();
                    if (s.Contains("Valid")) active++;
                    else if (s.Contains("Deactivated")) inactive++;
                    else if (s.Contains("Missing") || s.Contains("Error")) invalid++;
                }
                // Forecast Manager format - handles both string ("Active") and integer ("1") representations
                else if (hasItemStatus && row[itemStatusColName] != DBNull.Value)
                {
                    string statusValue = row[itemStatusColName].ToString().Trim();

                    // Try to parse as integer first (0 = Inactive, 1 = Active, 2 = Invalid)
                    if (int.TryParse(statusValue, out int stat))
                    {
                        if (stat == 1) active++;
                        else if (stat == 0) inactive++;
                        else if (stat == 2) invalid++;
                    }
                    else
                    {
                        // Fallback to string comparison for the data shown in the visualizer
                        if (statusValue.Equals("Active", StringComparison.OrdinalIgnoreCase)) active++;
                        else if (statusValue.Equals("Inactive", StringComparison.OrdinalIgnoreCase)) inactive++;
                        else if (statusValue.Equals("Invalid", StringComparison.OrdinalIgnoreCase)) invalid++;
                    }
                }
            }

            return (total, active, inactive, invalid);
        }


        private void UpdateGridStats(int gridIndex, int total, int active, int inactive, int invalid)
        {
            if (gridIndex == 1)
            {
                lblTotal1.Text = $"Total: {total}";
                lblActive1.Text = $"Active: {active}";
                lblInactive1.Text = $"Inactive: {inactive}";
                lblInvalid1.Text = $"Invalid: {invalid}";
                pnlStatsBar1.Visible = true;
            }
            else if (gridIndex == 3)
            {
                lblTotalOrder.Text = $"Total: {total}";
                lblActiveOrder.Text = $"Active: {active}";
                lblInactiveOrder.Text = $"Inactive: {inactive}";
                lblInvalidOrder.Text = $"Invalid: {invalid}";
                pnlStatsBarOrder.Visible = true;
            }
        }

        private void BindForecastGrids()
        {
            if (txtSearchF1_Real != null) txtSearchF1_Real.Text = "";

            dgvForecast1.DataSource = null;
            lblForecast1.Text = "Empty Slot";

            if (pnlSearch1 != null) pnlSearch1.Visible = false;
            if (pnlStatsBar1 != null) pnlStatsBar1.Visible = false;

            if (_forecastFiles.Count > 0)
            {
                var f1 = _forecastFiles[0];
                dgvForecast1.DataSource = f1.FullTable;

                if (dgvForecast1.Columns.Contains("IsActive")) dgvForecast1.Columns["IsActive"].Visible = false;

                lblForecast1.Text = $"{f1.ProjectionMonth} {f1.ProjectionYear} (Current)";
                lblForecast1.ForeColor = UITheme.GridRowText;

                var stats1 = CalculateGridStats(f1.FullTable);
                UpdateGridStats(1, stats1.Total, stats1.Active, stats1.Inactive, stats1.Invalid);

                string colName1 = f1.FullTable.Columns.Contains("CASIN") ? "CASIN" : "CASIN";
                ColorRowsByGroup(dgvForecast1, colName1);

                if (pnlSearch1 != null) pnlSearch1.Visible = true;
            }
        }

        private void BindOrderGrid(DataTable dt)
        {
            if (pnlSearchOrder != null)
            {
                txtSearchOrder_Real.Text = "";
                pnlSearchOrder.Visible = true;
            }
            dgvOrder.DataSource = dt;
            dgvOrder.Visible = true;

            // Hide redundant IsActive column so UI looks clean
            if (dgvOrder.Columns.Contains("IsActive")) dgvOrder.Columns["IsActive"].Visible = false;

            if (dt != null && dt.Rows.Count > 0)
            {
                string month = dt.Columns.Contains("Month") ? dt.Rows[0]["Month"]?.ToString() : "";
                string year = dt.Columns.Contains("Year") ? dt.Rows[0]["Year"]?.ToString() : "";

                if (!string.IsNullOrEmpty(month) || !string.IsNullOrEmpty(year))
                {
                    lblOrderMonth.Text = $"Order Data: {month} {year}".Trim();
                    lblOrderMonth.ForeColor = UITheme.GridRowText;
                }
                else
                {
                    lblOrderMonth.Text = "Order Data Loaded";
                    lblOrderMonth.ForeColor = UITheme.GridRowText;
                }
            }
            else
            {
                lblOrderMonth.Text = "No Order Loaded";
                lblOrderMonth.ForeColor = Color.Silver;
            }

            var stats = CalculateGridStats(dt);
            UpdateGridStats(3, stats.Total, stats.Active, stats.Inactive, stats.Invalid);

            string targetColumn = dt != null && dt.Columns.Contains("CASIN") ? "CASIN" : (dt != null && dt.Columns.Contains("CASIN") ? "CASIN" : "");
            if (!string.IsNullOrEmpty(targetColumn))
            {
                ColorRowsByGroup(dgvOrder, targetColumn);
            }
        }

        private void FilterGrid(DataGridView dgv, string searchText)
        {
            if (dgv.DataSource is DataTable dt)
            {
                try
                {
                    string targetColumn = dt.Columns.Contains("CASIN") ? "CASIN" : (dt.Columns.Contains("CASIN") ? "CASIN" : "");
                    if (string.IsNullOrEmpty(targetColumn)) return;

                    string filter = (string.IsNullOrWhiteSpace(searchText) || searchText.StartsWith("Search"))
                        ? "" : $"[{targetColumn}] LIKE '%{searchText}%'";

                    dt.DefaultView.RowFilter = filter;
                    ColorRowsByGroup(dgv, targetColumn);
                }
                catch (Exception ex)
                {
                    string msg = $"An unexpected error occurred while filtering the grid: {ex.Message}"
                                 + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                                 + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "");
                    SetStatusThreadSafe(msg, StatusType.Error);
                }
            }
        }

        private void ColorRowsByGroup(DataGridView dgv, string columnName)
        {
            if (dgv.Rows.Count == 0) return;
            Color color1 = UITheme.SurfaceWhite;
            Color color2 = UITheme.BackgroundCanvas;
            Color currentColor = color1;
            string previousValue = null;

            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (!dgv.Columns.Contains(columnName)) return;
                var cellValue = row.Cells[columnName].Value?.ToString();
                if (previousValue != null && cellValue != previousValue)
                    currentColor = (currentColor == color1) ? color2 : color1;

                row.DefaultCellStyle.BackColor = currentColor;
                previousValue = cellValue;
            }
        }

        private void RebindFromSession()
        {
            if (_session.ForecastFiles != null && _session.ForecastFiles.Count > 0)
            {
                _forecastFiles = new List<ForecastFileData>(_session.ForecastFiles);
                BindForecastGrids();
            }

            if (_session.Orders != null)
            {
                BindOrderGrid(_session.Orders);
                _orderUploaded = true;
            }
        }

        private async Task UpdateSessionAsync()
        {
            if (_forecastFiles.Count == 0) return;

            var currentFile = _forecastFiles[0];
            _session.Curr = currentFile.Forecast;
            _session.CurrentMonthWithYear = $"{_session.Curr.Month} {_session.Curr.Year}";
            _session.TargetMonth = $"{_session.Curr.ForecastingFor}";

            // 1. Calculate Previous Month
            int m = GetMonthNumber(currentFile.ProjectionMonth);
            int y = int.Parse(currentFile.ProjectionYear);

            DateTime prevDate = new DateTime(y, m, 1).AddMonths(-1);
            string prevMonthName = prevDate.ToString("MMMM");
            string prevYearStr = prevDate.Year.ToString();

            // 2. Auto-Fetch Previous Forecast from DB
            var prevDbResult = await Task.Run(() => _forecastManager.LoadExistingForecastAsync(prevMonthName, prevYearStr));

            if (prevDbResult.Success && prevDbResult.Data != null)
            {
                _session.Prev = prevDbResult.Data.Forecast;
                // Removed confusing UI status notification here
            }
            else
            {
                _session.Prev = null;
                // Removed confusing UI status notification here
            }

            // 3. Finalize Session Setup
            bool includeInactive = currentFile.IsContinueWithInactiveItems || _session.IsContinueWithInactiveItems;
            _session.IsContinueWithInactiveItems = includeInactive;

            var resItemsCatalogue = await _itemsRepository.GetActiveItemCatalogues(includeInactive);
            if (resItemsCatalogue.Success)
            {
                _session.ItemCatalogue = resItemsCatalogue.Data;
            }

            _session.WipType = WipType.Analyst.ToString();
            _session.ForecastFiles = new List<ForecastFileData> { currentFile };

            _session.AsinList = currentFile.FullTable.AsEnumerable()
                .Select(r => r["CASIN"]?.ToString())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();
        }

        private void ClearForecastSession()
        {
            _forecastFiles.Clear();
            dgvForecast1.DataSource = null;
            lblForecast1.Text = "Empty Slot";

            if (pnlSearch1 != null)
            {
                txtSearchF1_Real.Text = "";
                pnlSearch1.Visible = false;
            }
            if (pnlStatsBar1 != null) pnlStatsBar1.Visible = false;
        }

        private void ResetForecastDropdown() => cmbDbForecasts.SelectedIndex = 0;
        private void ResetOrderDropdown() => cmbDbOrders.SelectedIndex = 0;
        #endregion

        #region Action Handlers
        private void btnBrowseForecast_Click(object sender, EventArgs e) => HandleFileSelection(FileType.Forecast);
        private void btnBrowseOrder_Click(object sender, EventArgs e) => HandleFileSelection(FileType.Order);

        private async void HandleFileSelection(FileType fileType)
        {
            using (OpenFileDialog ofd = new OpenFileDialog { Filter = "Excel Files|*.xlsx;*.xls" })
            {
                if (ofd.ShowDialog() != DialogResult.OK) return;
                switch (fileType)
                {
                    case FileType.Forecast: await ProcessForecastFile(ofd.FileName); break;
                    case FileType.Order: await ProcessOrderFile(ofd.FileName); break;
                }
            }
        }

        private void tabControlMain_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (e.TabPage == tabOpenNewForm)
            {
                e.Cancel = true;
                var myNewForm = new OrderEntryForm(_session, _orderManager, _excelSerice);
                myNewForm.Show();
            }
        }

        private void btnExportForecastErrors_Click(object sender, EventArgs e) => ExportErrors(dgvForecastErrors, "Forecast_Errors");
        private void btnExportOrderErrors_Click(object sender, EventArgs e) => ExportErrors(dgvOrderErrors, "Order_Errors");

        private void ExportErrors(DataGridView targetGrid, string filePrefix)
        {
            if (targetGrid.Rows.Count == 0)
            {
                MessageBox.Show("No error records to export.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Excel Workbook|*.xlsx";
                sfd.Title = "Save Error Report";
                sfd.FileName = $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _busyHelper.ShowBusy("Exporting Error Report...");
                        _excelSerice.ExportGridToExcel(targetGrid, sfd.FileName, "Invalid Items");
                        MessageBox.Show("Error report exported successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        string msg = $"An unexpected error occurred while exporting the error report: {ex.Message}"
                                     + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                                     + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "");
                        MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        _busyHelper.HideBusy();
                    }
                }
            }
        }

        private void btnMarkInvalid_Click(object sender, EventArgs e)
        {
            string asinColName = dgvForecastErrors.Columns.Contains("CASIN") ? "CASIN" :
                               (dgvForecastErrors.Columns.Contains("CASIN") ? "CASIN" : null);

            if (string.IsNullOrEmpty(asinColName))
            {
                MessageBox.Show("Could not find a valid ASIN column in the error grid.", "Data Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            List<string> selectedAsins = new List<string>();
            foreach (DataGridViewRow row in dgvForecastErrors.Rows)
            {
                var cell = row.Cells["chkSelect"]?.Value;
                if (cell != null && (bool)cell)
                {
                    string asin = row.Cells[asinColName]?.Value?.ToString();
                    if (!string.IsNullOrEmpty(asin))
                        selectedAsins.Add(asin);
                }
            }

            if (selectedAsins.Count == 0)
            {
                MessageBox.Show("No items selected.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _busyHelper.ShowBusy("Marking items as invalid with stock...");

            try
            {
                int userId = _session.LoggedInUser.Id;
                DataTable dtInvalidItems = new DataTableFactory().CreateInvalidItemDataTable(selectedAsins, userId);
                DataTable dtInitialStock = new DataTableFactory().CreateInvalidStockDataTable(selectedAsins, userId);

                var response = _itemsRepository.BulkInsertInvalidCatalogueImport(dtInvalidItems, dtInitialStock);

                if (response.Success)
                {
                    MessageBox.Show($"{selectedAsins.Count} items have been marked as invalid along with their stock.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"Failed to mark items as invalid: {response.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                dgvForecastErrors.DataSource = null;
                pnlForecastErrors.Visible = false;
            }
            catch (Exception ex)
            {
                string msg = $"An unexpected error occurred while marking items as invalid: {ex.Message}"
                           + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                           + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "");
                MessageBox.Show(msg, "System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _busyHelper.HideBusy();
            }
        }

       
        private void dgvForecastErrors_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == dgvForecastErrors.Columns["chkSelect"].Index && e.RowIndex >= 0)
            {
                dgvForecastErrors.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void dgvForecastErrors_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (!dgvForecastErrors.Columns.Contains("chkSelect"))
                return;

            foreach (DataGridViewRow row in dgvForecastErrors.Rows)
            {
                var reason = row.Cells["Reason"].Value?.ToString();
                var chkCell = row.Cells["chkSelect"];

                if (reason != "Missing")
                {
                    chkCell.ReadOnly = true;
                    chkCell.Style.BackColor = UITheme.GridBorder;
                    chkCell.Value = false;
                }
                else
                {
                    chkCell.ReadOnly = false;
                    chkCell.Style.BackColor = UITheme.SurfaceWhite;
                }
            }
        }
        #endregion

        #region Helpers
        public static int GetCommitmentPeriodFromConfig()
        {
            string setting = ConfigurationManager.AppSettings["CommitmentPeriod"];
            if (string.IsNullOrEmpty(setting)) throw new ConfigurationErrorsException("AppSetting 'CommitmentPeriod' is missing or empty.");
            if (!int.TryParse(setting, out int parsedValue)) throw new ConfigurationErrorsException($"AppSetting 'CommitmentPeriod' is not a valid integer: '{setting}'.");
            return parsedValue;
        }

        private int GetMonthNumber(string monthName)
        {
            try { return DateTime.ParseExact(monthName.Trim(), "MMMM", CultureInfo.InvariantCulture).Month; }
            catch (Exception ex)
            {
                string msg = $"An unexpected error occurred while parsing the month number: {ex.Message}"
                             + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                             + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "");
                SetStatusThreadSafe(msg, StatusType.Warning);
                return 0;
            }
        }

        private void ShowErrorDialog(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void SetStatus(string msg, StatusType type) => _setStatus?.Invoke(msg, type);
        private void SetStatusThreadSafe(string msg, StatusType type) => BeginInvoke(new Action(() => SetStatus(msg, type)));
        #endregion
    }
}