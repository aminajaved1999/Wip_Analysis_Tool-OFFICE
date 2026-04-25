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
using WIPAT.Helpers;
using static System.Collections.Specialized.BitVector32;

namespace WIPAT
{
    public partial class UploadForm : Form
    {
        public event Action InputsChanged;

        #region State & Dependencies
        private readonly WipSession _session;
        private readonly Action<string, StatusType> _setStatus;
        private readonly BusyOverlayHelper _busyHelper;

        private List<ForecastFileData> _forecastFiles;

        // Injected Managers (BLL)
        private readonly IExcelService _excelSerice;
        private readonly IForecastManager _forecastManager;
        private readonly IOrderManager _orderManager;
        // Repositories
        private readonly IForecastRepository _forecastRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IItemsRepository _itemsRepository;

        private int _commitmentPeriod = 0;
        private bool _orderUploaded = false;

        // Custom Search Boxes
        private TextBox _txtSearchF1_Real;
        private TextBox _txtSearchF2_Real;
        private TextBox _txtSearchOrder_Real;
        #endregion

        #region Constructor
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
            // Validate dependencies
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _excelSerice = excelSerice ?? throw new ArgumentNullException(nameof(excelSerice));
            _forecastManager = forecastManager ?? throw new ArgumentNullException(nameof(forecastManager));
            _orderManager = orderManager ?? throw new ArgumentNullException(nameof(orderManager));
            _forecastRepository = forecastRepository ?? throw new ArgumentNullException(nameof(forecastRepository));
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _itemsRepository = itemsRepository ?? throw new ArgumentNullException(nameof(itemsRepository));
            _setStatus = setStatus;

            // Initialize State
            _forecastFiles = new List<ForecastFileData>();

            InitializeComponent();
            _session = session;
            _setStatus = setStatus;

            // --- THEME UPDATE START ---
            this.BackColor = UITheme.BackgroundCanvas; // The gray background

            // Setup Tabs background
            tabForecast.BackColor = UITheme.BackgroundCanvas;
            tabOrder.BackColor = UITheme.BackgroundCanvas;

            // Create "Card" effect for grids by making their wrappers White
            pnlGrid1Wrapper.BackColor = UITheme.SurfaceWhite;
            pnlGrid2Wrapper.BackColor = UITheme.SurfaceWhite;
            // Note: If pnlGrid1Wrapper doesn't have Padding in designer, add: pnlGrid1Wrapper.Padding = new Padding(1);

            // Apply Button Themes
            UITheme.ApplyButtonTheme(btnBrowseForecast);
            UITheme.ApplyButtonTheme(btnBrowseOrder);
            UITheme.ApplyButtonTheme(btnExportErrors, isPrimary: false); // Green button
                                                                         // --- THEME UPDATE END ---

            SetupModernSearchBars(); // We will update this method next

            _busyHelper = new BusyOverlayHelper(this, progressBarTop, SetStatusThreadSafe);
            
            _commitmentPeriod = GetCommitmentPeriodFromConfig();
            _session.CommitmentPeriod = _commitmentPeriod;

            ApplyModernStyleToGrids(); // We will update this method next

            this.FormClosing += (s, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; this.Hide(); }
            };

        }

        #endregion Constructor

        #region 1. UI Setup & Styling
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // 1. Load the Dropdown Data from DB
            LoadForecastDropdown();
            LoadOrderDropdown();

            // 2. Restore any existing session state
            RebindFromSession();
        }

        private void SetupModernSearchBars()
        {
            // --- GRID 1 (Left Aligned) ---
            lblForecast1.AutoSize = true;
            lblForecast1.Dock = DockStyle.Left;
            lblForecast1.TextAlign = ContentAlignment.MiddleLeft;

            Panel pnlSearch1 = CreateModernSearchBar(out _txtSearchF1_Real);
            pnlSearch1.Dock = DockStyle.Left;
            pnlSearch1.Width = 300;
            pnlSearch1.Padding = new Padding(20, 7, 0, 5);

            this.pnlHeader1.Controls.Clear();
            this.pnlHeader1.Controls.Add(pnlSearch1);
            this.pnlHeader1.Controls.Add(lblForecast1);

            _txtSearchF1_Real.TextChanged += (s, e) => FilterGrid(dgvForecast1, _txtSearchF1_Real.Text);

            // --- GRID 2 (Left Aligned) ---
            lblForecast2.AutoSize = true;
            lblForecast2.Dock = DockStyle.Left;
            lblForecast2.TextAlign = ContentAlignment.MiddleLeft;

            Panel pnlSearch2 = CreateModernSearchBar(out _txtSearchF2_Real);
            pnlSearch2.Dock = DockStyle.Left;
            pnlSearch2.Width = 300;
            pnlSearch2.Padding = new Padding(20, 7, 0, 5);

            this.pnlHeader2.Controls.Clear();
            this.pnlHeader2.Controls.Add(pnlSearch2);
            this.pnlHeader2.Controls.Add(lblForecast2);

            _txtSearchF2_Real.TextChanged += (s, e) => FilterGrid(dgvForecast2, _txtSearchF2_Real.Text);

            // --- ORDER GRID ---
            Panel pnlSearchOrder = CreateModernSearchBar(out _txtSearchOrder_Real);

            // Move Search Bar to the right to make room for the new Dropdown
            pnlSearchOrder.Location = new Point(380, 13);

            this.pnlOrderHeader.Controls.Add(pnlSearchOrder);
            _txtSearchOrder_Real.TextChanged += (s, e) => FilterGrid(dgvOrder, _txtSearchOrder_Real.Text);
        }

        private void ApplyModernStyleToGrids()
        {
            // Use the central theme logic
            UITheme.StyleGrid(dgvForecast1, true);
            UITheme.StyleGrid(dgvForecast2, true);
            UITheme.StyleGrid(dgvOrder, true);
            UITheme.StyleGrid(dgvOrderErrors, false);
        }

        private Panel CreateModernSearchBar(out TextBox refTextBox)
        {
            Panel container = new Panel();
            container.Size = new Size(240, 32);

            // UPDATE: Use BackgroundCanvas so it blends with the gray form
            container.BackColor = UITheme.BackgroundCanvas;
            container.Padding = new Padding(10, 7, 10, 5);
            container.Cursor = Cursors.IBeam;

            container.Paint += (s, e) =>
            {
                // Draw a simple border
                ControlPaint.DrawBorder(e.Graphics, container.ClientRectangle,
                    Color.Silver, 1, ButtonBorderStyle.Solid, // Left
                    Color.Silver, 1, ButtonBorderStyle.Solid, // Top
                    Color.Silver, 1, ButtonBorderStyle.Solid, // Right
                    Color.Silver, 1, ButtonBorderStyle.Solid  // Bottom
                );
            };

            Label lblIcon = new Label();
            lblIcon.Text = "🔍";
            lblIcon.Font = new Font("Segoe UI Symbol", 9);
            lblIcon.AutoSize = true;
            lblIcon.Dock = DockStyle.Left;
            lblIcon.ForeColor = Color.Gray;
            lblIcon.BackColor = Color.Transparent;
            lblIcon.Padding = new Padding(0, 2, 0, 0);

            TextBox txt = new TextBox();
            txt.BorderStyle = BorderStyle.None;
            // UPDATE: Match container background
            txt.BackColor = UITheme.BackgroundCanvas;
            txt.Dock = DockStyle.Fill;
            txt.Font = new Font("Segoe UI", 9.5f);
            txt.ForeColor = Color.Gray;
            txt.Text = "Search C-ASIN...";

            txt.GotFocus += (s, e) =>
            {
                if (txt.Text == "Search C-ASIN...") { txt.Text = ""; txt.ForeColor = Color.Black; }
                // On Focus: Turn White to look active
                container.BackColor = UITheme.SurfaceWhite;
                txt.BackColor = UITheme.SurfaceWhite;
            };
            txt.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txt.Text)) { txt.Text = "Search C-ASIN..."; txt.ForeColor = Color.Gray; }
                // Lost Focus: Go back to Gray
                container.BackColor = UITheme.BackgroundCanvas;
                txt.BackColor = UITheme.BackgroundCanvas;
            };

            container.Click += (s, e) => txt.Focus();
            lblIcon.Click += (s, e) => txt.Focus();

            container.Controls.Add(txt);
            container.Controls.Add(lblIcon);

            refTextBox = txt;
            return container;
        }

        #endregion

        #region 2. Unified Logic (Dropdowns & Browsing)

        // ---------------------------------------------------------
        // FORECAST LOGIC
        // ---------------------------------------------------------

        private void LoadForecastDropdown()
        {
            try
            {
                var dbResult = _forecastRepository.GetAvailableForecastsFromDB();
                var items = new List<string> { "Load from DB..." };
                if (dbResult.Success && dbResult.Data != null)
                {
                    items.AddRange(dbResult.Data.Select(f => $"{f.Month}_{f.Year}").Distinct());
                }
                cmbDbForecasts.DataSource = items;
            }
            catch { /* Handle safely */ }
        }

        private async void cmbDbForecasts_SelectionChangeCommitted(object sender, EventArgs e)
        {
            string sel = cmbDbForecasts.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(sel) || sel.StartsWith("Load")) return;

            var parts = sel.Split('_');
            string selectedMonth = parts[0];
            string selectedYear = parts[1];


            // Scenario A: Rolling Forward (User has Month 1 & 2, selects Month 3)
            if (_forecastFiles.Count == 2)
            {
                var f1 = _forecastFiles[0];
                var f2 = _forecastFiles[1];

                bool isConsecutiveToSecond = IsConsecutiveMonth(f2, selectedMonth, selectedYear);
                bool isReplacingSecond = IsConsecutiveMonth(f1, selectedMonth, selectedYear);

                if (isConsecutiveToSecond)
                {
                    // Action: Roll Forward
                    bool confirm = ShowSequenceConfirmation(
                        title: "Timeline Update (Roll Forward)",
                        mainMessage: $"You are loading {selectedMonth} {selectedYear}.",
                        subMessage: $"This follows the current timeline.\nTo proceed, {f1.ProjectionMonth} {f1.ProjectionYear} will be removed to make room.",
                        actionButton: "Update Timeline");

                    if (!confirm) { ResetForecastDropdown(); return; }

                    // Logic handled in Load: We will remove index 0 after successful load
                }
                else if (isReplacingSecond)
                {
                    // Action: Replace Last
                    bool confirm = ShowSequenceConfirmation(
                        title: "Replace Forecast",
                        mainMessage: $"You are replacing {f2.ProjectionMonth} with {selectedMonth}.",
                        subMessage: $"This will maintain the start date of {f1.ProjectionMonth}.",
                        actionButton: "Replace File");

                    if (!confirm) { ResetForecastDropdown(); return; }

                    _forecastFiles.RemoveAt(1); // Remove immediately to make room
                }
                else
                {
                    ShowErrorDialog("Sequence Error",
                        $"The selected file ({selectedMonth}) does not fit the current timeline sequence.");
                    ResetForecastDropdown();
                    return;
                }
            }
            // Scenario B: User has Month 1, selects random Month (Non-consecutive)
            else if (_forecastFiles.Count == 1)
            {
                var existing = _forecastFiles[0];
                if (!IsConsecutiveMonth(existing, selectedMonth, selectedYear))
                {
                    bool confirm = ShowSequenceConfirmation(
                       title: "New Timeline Start",
                       mainMessage: $"Start new timeline with {selectedMonth} {selectedYear}?",
                       subMessage: $"The existing file ({existing.ProjectionMonth}) is not consecutive and will be cleared.",
                       actionButton: "Start New");

                    if (confirm)
                    {
                        ClearForecastSession();
                    }
                    else
                    {
                        ResetForecastDropdown();
                        return;
                    }
                }
            }

            // --- LOAD DATA ---
            await LoadForecastDataCommonAsync(selectedMonth, selectedYear);
            ResetForecastDropdown();
        }

        private void btnBrowseForecast_Click(object sender, EventArgs e) => HandleFileSelection(FileType.Forecast);

        private async Task ProcessForecastFile(string filePath)
        {
            // 1. Check Session Limits
            if (_forecastFiles.Count >= 2)
            {
                ShowErrorDialog("Session Full", "Maximum 2 forecast files allowed.\nPlease clear the session or use the dropdown to Roll Forward.");
                return;
            }

            _busyHelper.ShowBusy("Verifying File...");
            try
            {
                // 2. Preview File (Get Dates)
                var previewResponse = await _forecastManager.GetForecastFilePreviewAsync(filePath, _commitmentPeriod);
                if (!previewResponse.Success)
                {
                    _busyHelper.HideBusy(); // Hide early if failing here
                    SetStatusThreadSafe(previewResponse.Message, StatusType.Error);
                    return;
                }

                var newFile = previewResponse.Data;
                _busyHelper.HideBusy(); // Hide to show dialogs safely

                // 3. Consecutive Logic
                if (_forecastFiles.Count == 1)
                {
                    var existing = _forecastFiles[0];
                    if (!IsConsecutiveMonth(existing, newFile.ProjectionMonth, newFile.ProjectionYear))
                    {
                        bool confirm = ShowSequenceConfirmation(
                            title: "Non-Consecutive Month",
                            mainMessage: $"This file ({newFile.ProjectionMonth}) does not follow {existing.ProjectionMonth}.",
                            subMessage: "Do you want to clear the current session and start a new timeline?",
                            actionButton: "Start New Timeline");

                        if (confirm)
                        {
                            ClearForecastSession();
                        }
                        else
                        {
                            SetStatusThreadSafe("Upload cancelled.", StatusType.Warning);
                            return;
                        }
                    }
                }

                // 4. Process & Save (This handles the DB Check)
                _busyHelper.ShowBusy("Processing & Saving...");

                // Create a copy to pass to manager
                var currentListCopy = new List<ForecastFileData>(_forecastFiles);
                var result = await Task.Run(() => _forecastManager.HandleForecastFileAsync(filePath, currentListCopy, _commitmentPeriod, _session));

                if (result.Success)
                {
                    _forecastFiles = result.Data;
                    BindForecastGrids();
                    await UpdateSessionAsync();
                    InputsChanged?.Invoke();

                    // CHECK MESSAGE FOR "IGNORED" (Meaning loaded from DB)
                    if (result.Message.Contains("IGNORED") || result.Message.Contains("already exists"))
                    {
                        MessageBox.Show(result.Message, "Existing Record Loaded", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        SetStatusThreadSafe($"Existing Forecast loaded: {newFile.ProjectionMonth}", StatusType.Warning);
                    }
                    else
                    {
                        SetStatusThreadSafe($"Forecast Loaded: {newFile.ProjectionMonth} {newFile.ProjectionYear}", StatusType.Success);
                    }
                }
                else
                {
                    SetStatusThreadSafe(result.Message, StatusType.Error);
                    MessageBox.Show(result.Message, "Upload Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                SetStatusThreadSafe($"Error: {ex.Message}", StatusType.Error);
            }
            finally
            {
                _busyHelper.HideBusy();
            }
        }


        // ---------------------------------------------------------
        // ORDER LOGIC
        // ---------------------------------------------------------

        private void LoadOrderDropdown()
        {
            try
            {
                Response<List<ActualOrder>> dbResult = _orderRepository.GetActualOrdersFromDatabase();
                var items = new List<string> { "Load Order from DB..." };

                if (dbResult.Success && dbResult.Data != null)
                {
                    var distinctOrders = dbResult.Data
                        .Select(o => new { o.Month, o.Year })
                        .Distinct()
                        .Select(o => $"{o.Month}_{o.Year}");

                    items.AddRange(distinctOrders);
                }
                cmbDbOrders.DataSource = items;
            }
            catch { /* Handle safely */ }
        }

        // Common Validation for Orders (Used by both Dropdown and Browse)
        private bool ValidateOrderPrerequisites(string targetMonth, string targetYear)
        {
            // Check 1: Must have 2 forecasts
            if (_forecastFiles.Count < 2)
            {
                ShowErrorDialog("Prerequisite Missing",
                    "You must establish a valid Forecast timeline (2 files) before loading Orders.");
                return false;
            }

            // Check 2: Date Match
            var targetForecast = _forecastFiles[1]; // The "Current" month
            bool match = string.Equals(targetMonth, targetForecast.ProjectionMonth, StringComparison.OrdinalIgnoreCase) &&
                         string.Equals(targetYear, targetForecast.ProjectionYear, StringComparison.OrdinalIgnoreCase);

            if (!match)
            {
                ShowErrorDialog("Date Mismatch",
                    $"Target Forecast: {targetForecast.ProjectionMonth} {targetForecast.ProjectionYear}\n" +
                    $"Selected Order:  {targetMonth} {targetYear}\n\n" +
                    "The Order file must match the Current Forecast month.");
                return false;
            }

            return true;
        }

        private async void cmbDbOrders_SelectionChangeCommitted(object sender, EventArgs e)
        {
            string sel = cmbDbOrders.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(sel) || sel.StartsWith("Load")) return;

            var parts = sel.Split('_');
            if (!ValidateOrderPrerequisites(parts[0], parts[1]))
            {
                ResetOrderDropdown();
                return;
            }

            _busyHelper.ShowBusy($"Loading Orders {sel}...");
            try
            {
                var result = await Task.Run(() => _orderManager.LoadExistingOrderAsync(parts[0], parts[1]));
                HandleOrderLoadResult(result.Success, result.Data, result.Message);
            }
            finally
            {
                _busyHelper.HideBusy();
                ResetOrderDropdown();
            }
        }

        private void btnBrowseOrder_Click(object sender, EventArgs e) => HandleFileSelection(FileType.Order);

        private async Task ProcessOrderFile(string filePath)
        {
            // Note: We can't validate dates yet because we haven't read the file.
            // But we CAN check if forecasts exist.
            if (_forecastFiles.Count < 2)
            {
                ShowErrorDialog("Prerequisite Missing", "You must upload 2 Forecast files before uploading Orders.");
                return;
            }

            _busyHelper.ShowBusy("Processing Order File...");
            try
            {
                var result = await Task.Run(() => _orderManager.HandleOrderFileAsync(filePath, _session));
                if (!result.Success)
                {
                    // Handle specifically if validation failed inside Manager (Missing Orders)
                    if (result.Data != null && result.Data.MissingOrders != null && result.Data.MissingOrders.Any())
                    {
                        SetStatus("Order file contains invalid items.", StatusType.Error);
                        dgvOrderErrors.DataSource = result.Data.MissingOrders;
                        pnlOrderErrors.Visible = true;
                        return;
                    }
                    SetStatus(result?.Message ?? "Unknown Error", StatusType.Error);
                    return;
                }

                // Check Dates NOW that we have data
                if (result.Data.ValidOrders.Count > 0)
                {
                    var r = result.Data.ValidOrders.FirstOrDefault();
                    string m = r.Month;
                    string y = r.Year;

                    if (!ValidateOrderPrerequisites(m, y)) return;

                    // CHECK MESSAGE FOR "IGNORED" (Meaning loaded from DB)
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
            finally { _busyHelper.HideBusy(); }
        }

        #endregion

        #region 3. Common Helpers (Date, Grid, File I/O)

        private async Task LoadForecastDataCommonAsync(string month, string year)
        {
            _busyHelper.ShowBusy($"Loading {month} {year}...");
            try
            {
                var result = await Task.Run(() => _forecastManager.LoadExistingForecastAsync(month, year));

                // HIDE BUSY FIRST
                _busyHelper.HideBusy();

                if (result.Success)
                {
                    var loadedFile = result.Data;

                    if (loadedFile.IsWipAlreadyCalculated)
                    {
                        MessageBox.Show(
                            $"WIP for {month} {year} is already calculated.\nYou cannot calculate it again.",
                            "WIP Already Calculated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    if (_forecastFiles.Count >= 2)
                    {
                        _forecastFiles.RemoveAt(0);
                    }

                    _forecastFiles.Add(loadedFile);
                    BindForecastGrids();
                    await UpdateSessionAsync();
                    InputsChanged?.Invoke();

                    // FIX: Queue Success message safely behind the HideBusy reset
                    SetStatusThreadSafe($"Loaded {month} {year} successfully.", StatusType.Success);
                }
                else
                {
                    // FIX
                    SetStatusThreadSafe(result.Message, StatusType.Error);
                }
            }
            catch (Exception ex)
            {
                _busyHelper.HideBusy();
                // FIX
                SetStatusThreadSafe($"Error: {ex.Message}", StatusType.Error);
            }
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
            var resItemsCatalogue = await _itemsRepository.GetItemCatalogues();
            if (resItemsCatalogue.Success)
            {
                _session.ItemCatalogue = resItemsCatalogue.Data;
            }

            _session.WipType = WipType.Analyst.ToString();
            _session.ForecastFiles = new List<ForecastFileData>(_forecastFiles);
            var asins = _forecastFiles
                .SelectMany(f => f.FullTable.AsEnumerable())
                .Select(r => r["C-ASIN"]?.ToString())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();
            _session.AsinList = asins;

            if (_forecastFiles.Count >= 2)
            {
                var sorted = _forecastFiles.OrderBy(f => f.ProjectionYear).ThenBy(f => GetMonthNumber(f.ProjectionMonth)).ToList();
                _session.Prev = sorted[0].Forecast;
                _session.Curr = sorted[1].Forecast;
                _session.CurrentMonthWithYear = $"{_session.Curr.Month} {_session.Curr.Year}";
            }
            else if (_forecastFiles.Count == 1)
            {
                _session.Curr = _forecastFiles[0].Forecast;
            }

            _session.TargetMonth = $"{_session.Curr.ForecastingFor}";
            await Task.CompletedTask;
        }

        private void ClearForecastSession()
        {
            _forecastFiles.Clear();
            dgvForecast1.DataSource = null;
            lblForecast1.Text = "Empty Slot";
            if (_txtSearchF1_Real != null) _txtSearchF1_Real.Text = "";

            // Also clear second grid if exists
            dgvForecast2.DataSource = null;
            lblForecast2.Text = "Empty Slot";
            if (_txtSearchF2_Real != null) _txtSearchF2_Real.Text = "";
        }

        private void ResetForecastDropdown() => cmbDbForecasts.SelectedIndex = 0;
        private void ResetOrderDropdown() => cmbDbOrders.SelectedIndex = 0;

        // --- DIALOG HELPERS ---

        private bool ShowSequenceConfirmation(string title, string mainMessage, string subMessage, string actionButton)
        {
            string fullMessage = $"{mainMessage}\n\n{subMessage}";
            // Using standard MessageBox options, but constructing clarity
            return MessageBox.Show(fullMessage, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        private void ShowErrorDialog(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        // --- GRID & DATE HELPERS ---

        private void BindForecastGrids()
        {
            if (_txtSearchF1_Real != null) _txtSearchF1_Real.Text = "";
            if (_txtSearchF2_Real != null) _txtSearchF2_Real.Text = "";

            dgvForecast1.DataSource = null;
            dgvForecast2.DataSource = null;
            lblForecast1.Text = "Empty Slot";
            lblForecast2.Text = "Empty Slot";

            if (_forecastFiles.Count > 0)
            {
                var f1 = _forecastFiles[0];
                dgvForecast1.DataSource = f1.FullTable;
                lblForecast1.Text = $"{f1.ProjectionMonth} {f1.ProjectionYear}";
                lblForecast1.ForeColor = Color.Black;
                ColorRowsByGroup(dgvForecast1, "C-ASIN");
            }

            if (_forecastFiles.Count > 1)
            {
                var f2 = _forecastFiles[1];
                dgvForecast2.DataSource = f2.FullTable;
                lblForecast2.Text = $"{f2.ProjectionMonth} {f2.ProjectionYear}";
                lblForecast2.ForeColor = Color.Black;
                ColorRowsByGroup(dgvForecast2, "C-ASIN");
            }
        }

        private void BindOrderGrid(DataTable dt)
        {
            if (_txtSearchOrder_Real != null) _txtSearchOrder_Real.Text = "";
            dgvOrder.DataSource = dt;
            dgvOrder.Visible = true;
        }

        private void FilterGrid(DataGridView dgv, string searchText)
        {
            if (dgv.DataSource is DataTable dt)
            {
                try
                {
                    string targetColumn = dt.Columns.Contains("C-ASIN") ? "C-ASIN" : (dt.Columns.Contains("CASIN") ? "CASIN" : "");
                    if (string.IsNullOrEmpty(targetColumn)) return;

                    string filter = (string.IsNullOrWhiteSpace(searchText) || searchText.StartsWith("Search"))
                        ? "" : $"[{targetColumn}] LIKE '%{searchText}%'";

                    dt.DefaultView.RowFilter = filter;
                    ColorRowsByGroup(dgv, targetColumn);
                }
                catch { /* logging */ }
            }
        }

        private void ColorRowsByGroup(DataGridView dgv, string columnName)
        {
            if (dgv.Rows.Count == 0) return;
            Color color1 = Color.White;
            Color color2 = Color.FromArgb(245, 247, 250);
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

        private int GetMonthNumber(string monthName)
        {
            try { return DateTime.ParseExact(monthName.Trim(), "MMMM", CultureInfo.InvariantCulture).Month; }
            catch { return 0; }
        }

        private bool IsConsecutiveMonth(ForecastFileData existingFile, string newMonthName, string newYearStr)
        {
            try
            {
                int m1 = GetMonthNumber(existingFile.ProjectionMonth);
                int y1 = int.Parse(existingFile.ProjectionYear);
                DateTime date1 = new DateTime(y1, m1, 1);
                int m2 = GetMonthNumber(newMonthName);
                int y2 = int.Parse(newYearStr);
                DateTime date2 = new DateTime(y2, m2, 1);
                return date1.AddMonths(1) == date2;
            }
            catch { return false; }
        }

        private void SetStatus(string msg, StatusType type) => _setStatus?.Invoke(msg, type);
        private void SetStatusThreadSafe(string msg, StatusType type) => BeginInvoke(new Action(() => SetStatus(msg, type)));
        private void btnExportErrors_Click(object sender, EventArgs e)
        {
            if (dgvOrderErrors.Rows.Count == 0)
            {
                MessageBox.Show("No error records to export.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Excel Workbook|*.xlsx";
                sfd.Title = "Save Error Report";
                sfd.FileName = $"Upload_Errors_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _busyHelper.ShowBusy("Exporting Error Report...");
                        _excelSerice.ExportGridToExcel(dgvOrderErrors, sfd.FileName, "Invalid Orders");

                        MessageBox.Show("Error report exported successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Export Failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        _busyHelper.HideBusy();
                    }
                }
            }
        }

        #endregion

        private void tabControlMain_Selecting(object sender, TabControlCancelEventArgs e)
        {
            // Check if the clicked tab is our special "New Form" tab
            if (e.TabPage == tabOpenNewForm)
            {
                // 1. Cancel the navigation so we don't go to the blank tab
                e.Cancel = true;

                // 2. Open your new form
                var myNewForm = new OrderEntryForm(_session, _orderManager, _excelSerice); 
                myNewForm.Show();
            }
        }

        public static int GetCommitmentPeriodFromConfig()
        {
            string setting = ConfigurationManager.AppSettings["CommitmentPeriod"];

            if (string.IsNullOrEmpty(setting))
            {
                throw new ConfigurationErrorsException("AppSetting 'CommitmentPeriod' is missing or empty.");
            }

            if (!int.TryParse(setting, out int parsedValue))
            {
                throw new ConfigurationErrorsException($"AppSetting 'CommitmentPeriod' is not a valid integer: '{setting}'.");
            }

            return parsedValue;
        }
    }
}