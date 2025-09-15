using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
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
    public partial class UploadForm : Form
    {
        // ==== NEW: notify MainForm when inputs change (so it can relock Step 3) ====
        public event Action InputsChanged;

        private void NotifyInputsChanged()
        {
            try { InputsChanged?.Invoke(); } catch { /* swallow */ }
            RefreshMainStepper();
        }

        private readonly WipSession _session;
        private readonly Action<string, StatusType> _setStatus;

        private int commitmentPeriod = 0;
        private int forecastCount = 0;
        private bool stockUploaded = false;
        private bool orderUploaded = false;

        private List<ForecastFileData> _ForecastFiles = new List<ForecastFileData>();
        private FilesManager _FilesManager;
        private ExcelHelper _ExcelHelper;
        private BusyOverlayHelper _busyHelper;

        public UploadForm(WipSession session, Action<string, StatusType> setStatus)
        {
            InitializeComponent();

            _session = session;
            _setStatus = setStatus;

            _ForecastFiles = new List<ForecastFileData>();
            _busyHelper = new BusyOverlayHelper(this, progressBar1, SetStatusThreadSafe);
            _FilesManager = new FilesManager(
                showBusy: msg => _busyHelper.ShowBusy(msg),
                hideBusy: () => _busyHelper.HideBusy(),
                setStatus: (m, t) => SetStatus(m, t)
            );

            _ExcelHelper = new ExcelHelper();

            // Seed commitment from helper → persist to session
            commitmentPeriod = ExcelHelper.GetCommitmentPeriod();
            _session.CommitmentPeriod = commitmentPeriod;

            ApplyModernStyleToDataGrids();

            // IMPORTANT: keep state if user tries to close this form (hide instead)
            this.FormClosing += UploadForm_FormClosing;
        }

        // ==== NEW: Hide instead of close to preserve state ====
        private void UploadForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        // ==== NEW: when the form is re-shown, rebuild UI from session (no recompute needed) ====
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            RebindFromSession();
        }

        // Restore UI (grids/labels) from whatever is already in _session
        private void RebindFromSession()
        {
            try
            {
                // Forecasts
                if (_session.ForecastFiles != null && _session.ForecastFiles.Count > 0)
                {
                    _ForecastFiles = new List<ForecastFileData>(_session.ForecastFiles);
                    forecastCount = _ForecastFiles.Count;
                    ApplyForecastDataToControls();
                }

                // Stock
                if (_session.Stock != null)
                {
                    LoadDataTableIntoGrid(_session.Stock, dataGridViewStock, lblStock);
                    stockUploaded = true;
                }

                // Orders
                if (_session.Orders != null)
                {
                    LoadDataTableIntoGrid(_session.Orders, dataGridViewOrder, lblOrder);
                    orderUploaded = true;
                }

                // If everything is present, show success status (optional)
                if ((_session.ForecastFiles?.Count ?? 0) >= 2 && _session.Stock != null && _session.Orders != null)
                {
                    SetStatus("All required files are uploaded.", StatusType.Success);
                }

                RefreshMainStepper();
            }
            catch (Exception ex)
            {
                SetStatus($"Error while restoring UI from session: {ex.Message}", StatusType.Error);
            }
        }

        private void SetStatus(string msg, StatusType type) => _setStatus?.Invoke(msg, type);

        /// <summary>
        /// Ask MainForm to re-evaluate whether Step 2/3 should be enabled.
        /// </summary>
        private void RefreshMainStepper()
        {
            if (this.ParentForm is MainForm mf)
                mf.UpdateStepperEnabledState();
        }

        #region BROWSE BUTTONS

        private async void HandleFileSelection(FileType fileType)
        {
            var res = new Response<string>();
            try
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Filter = "Excel Files|*.xlsx;*.xls";

                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        string filePath = ofd.FileName;

                        if (string.IsNullOrEmpty(filePath))
                        {
                            res.Success = false;
                            res.Message = "No file selected.";
                            res.Data = null;
                        }
                        else
                        {
                            switch (fileType)
                            {
                                case FileType.Forecast:
                                    {
                                        _busyHelper.ShowBusy("Loading forecast file...");
                                        try
                                        {
                                            var forecastRes = await Task.Run(() =>
                                                _FilesManager.HandleForecastFile(filePath, _ForecastFiles, commitmentPeriod));

                                            if (forecastRes.Success)
                                            {
                                                _ForecastFiles = forecastRes.Data;
                                                forecastCount = _ForecastFiles.Count;

                                                if (!ApplyForecastDataToControls())
                                                {
                                                    SetStatus("Failed to assign Forecast UI controls.", StatusType.Error);
                                                }

                                                // Persist to session (so re-show restores instantly)
                                                _session.ForecastFiles = new List<ForecastFileData>(_ForecastFiles);
                                                _session.AsinList = ExtractAsinsFromForecasts(_ForecastFiles);

                                                // Also seed prev/curr + months
                                                SeedSessionFromForecasts();

                                                SetStatus($"{forecastCount} forecast files loaded.", StatusType.Success);

                                                // ==== NEW: notify navigation logic that inputs changed ====
                                                NotifyInputsChanged();
                                            }
                                            else
                                            {
                                                SetStatus(forecastRes.Message, StatusType.Error);
                                            }
                                        }
                                        finally { _busyHelper.HideBusy(); }

                                        break;
                                    }

                                case FileType.Stock:
                                    {
                                        _busyHelper.ShowBusy("Loading Stock file...");

                                        try
                                        {
                                            var stockRes = await Task.Run(() =>
                                                _FilesManager.HandleStockFile(filePath, stockUploaded));

                                            if (stockRes.Success)
                                            {
                                                stockUploaded = true;
                                                LoadDataTableIntoGrid(stockRes.Data, dataGridViewStock, lblStock);

                                                _session.Stock = stockRes.Data;
                                                SetStatus(stockRes.Message, StatusType.Success);

                                                // ==== NEW: notify inputs changed ====
                                                NotifyInputsChanged();
                                            }
                                            else
                                            {
                                                SetStatus(stockRes.Message, StatusType.Error);
                                            }
                                        }
                                        finally { _busyHelper.HideBusy(); }

                                        break;
                                    }

                                case FileType.Order:
                                    {
                                        _busyHelper.ShowBusy("Loading Order file...");
                                        try
                                        {
                                            var orderRes = await Task.Run(() =>
                                                _FilesManager.HandleOrderFile(filePath, orderUploaded));

                                            if (orderRes.Success)
                                            {
                                                orderUploaded = true;
                                                LoadDataTableIntoGrid(orderRes.Data, dataGridViewOrder, lblOrder);

                                                _session.Orders = orderRes.Data;
                                                SetStatus(orderRes.Message, StatusType.Success);

                                                // ==== NEW: notify inputs changed ====
                                                NotifyInputsChanged();
                                            }
                                            else
                                            {
                                                SetStatus(orderRes.Message, StatusType.Error);
                                            }
                                        }
                                        finally { _busyHelper.HideBusy(); }

                                        break;
                                    }

                                default:
                                    res.Success = false;
                                    res.Message = "Unknown file type.";
                                    break;
                            }
                        }
                    }
                    else
                    {
                        res.Success = false;
                        res.Message = "File selection was canceled.";
                        res.Data = null;
                    }
                }
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Message = $"Exception occurred: {ex.Message}" + (ex.InnerException != null ? $" | Inner Exception: {ex.InnerException.Message}" : "");
                SetStatus(res.Message, StatusType.Error);
            }
        }

        private void btnBrowseForecast_Click(object sender, EventArgs e)
        {
            HandleFileSelection(FileType.Forecast);
        }

        private void btnBrowseStock_Click(object sender, EventArgs e)
        {
            HandleFileSelection(FileType.Stock);
        }

        private void btnBrowseOrder_Click(object sender, EventArgs e)
        {
            HandleFileSelection(FileType.Order);
        }

        private void LoadDataTableIntoGrid(DataTable dt, DataGridView dgv, Label lbl)
        {
            dgv.DataSource = dt;
            dgv.Visible = true;
            lbl.Visible = true;
        }

        private bool ApplyForecastDataToControls() // Assign + Bind + Load + Show
        {
            try
            {
                DataGridView[] dataGrids = { dataGridView1, dataGridView2, dataGridView3, dataGridView4 };
                Label[] labels = { lblDGV1, lblDGV2, lblDGV3, lblDGV4 };

                if (_ForecastFiles == null || _ForecastFiles.Count == 0)
                    return false;

                bool assignedAtLeastOne = false;

                for (int i = 0; i < _ForecastFiles.Count && i < dataGrids.Length; i++)
                {
                    var forecastFile = _ForecastFiles[i];

                    // Assign controls to the in-memory object (handy if reused elsewhere)
                    forecastFile.BoundGrid = dataGrids[i];
                    forecastFile.BoundLabel = labels[i];

                    // Set label text
                    if (forecastFile.BoundLabel != null)
                    {
                        forecastFile.BoundLabel.Text = $"{forecastFile.ProjectionMonth} {forecastFile.ProjectionYear}";
                        forecastFile.BoundLabel.Visible = true;
                    }

                    // Load data into grid
                    if (forecastFile.BoundGrid != null)
                    {
                        forecastFile.BoundGrid.DataSource = forecastFile.FullTable;
                        forecastFile.BoundGrid.Visible = true;
                    }

                    assignedAtLeastOne = true;
                }

                return assignedAtLeastOne;
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error applying forecast data to controls: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $" | Inner Exception: {ex.InnerException.Message}";
                }

                MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        #endregion BROWSE BUTTONS

        // "Upload" button (logical validation only)
        private void btnUpload_Click(object sender, EventArgs e)
        {
            if (_session.ForecastFiles == null || _session.ForecastFiles.Count < 2)
            {
                SetStatus("Please upload at least 2 forecast files.", StatusType.Error);
                return;
            }
            if (_session.Stock == null)
            {
                SetStatus("Please upload the stock file.", StatusType.Error);
                return;
            }
            if (_session.Orders == null)
            {
                SetStatus("Please upload the order file.", StatusType.Error);
                return;
            }

            SetStatus("All required files are uploaded.", StatusType.Success);

            // Let MainForm re-check gating (and keep Step 3 locked until Step 2 is run again)
            NotifyInputsChanged();
        }

        #region helpers

        // Build a distinct ASIN list from all forecast tables (if column names differ, adjust here)
        private List<string> ExtractAsinsFromForecasts(IEnumerable<ForecastFileData> files)
        {
            var list = new List<string>();
            foreach (var f in files)
            {
                if (f?.FullTable == null || !f.FullTable.Columns.Contains("C-ASIN"))
                    continue;

                foreach (DataRow row in f.FullTable.Rows)
                {
                    var asin = row["C-ASIN"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(asin))
                        list.Add(asin);
                }
            }
            return list.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        // After forecasts are loaded, seed session helpers (Prev/Curr and month fields)
        private void SeedSessionFromForecasts()
        {
            try
            {
                _session.WipType = WipType.Analyst.ToString();

                if (_session?.ForecastFiles == null || _session.ForecastFiles.Count < 2)
                    return;

                var orderedFiles = _session.ForecastFiles
                    .OrderBy(f => f.ProjectionYear)
                    .ThenBy(f => MonthNameToNumber(f.ProjectionMonth))
                    .ToList();

                var prevF = orderedFiles[orderedFiles.Count - 2]; // second-to-last
                var currF = orderedFiles[orderedFiles.Count - 1]; // last

                _session.Prev = prevF.Forecast;
                _session.Curr = currF.Forecast;

                // Month strings
                _session.CurrentMonth = _session.Curr.Month;
                _session.CurrentMonthWithYear = $"{_session.Curr.Month} {_session.Curr.Year}";
                _session.TargetMonth = $"{_session.Curr.ForecastingFor}";
                _session.TargetMonthWithYear = $"{currF.ForecastFor}";
            }
            catch (Exception ex)
            {
                string errorMessage = $"Exception occurred: {ex.Message}" +
                    (ex.InnerException != null ? $" | Inner Exception: {ex.InnerException.Message}" : "");
                MessageBox.Show(errorMessage, "Session Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static int MonthNameToNumber(string month)
        {
            if (string.IsNullOrWhiteSpace(month)) return 0;
            return Array.FindIndex(new[]
            { "JANUARY","FEBRUARY","MARCH","APRIL","MAY","JUNE","JULY","AUGUST","SEPTEMBER","OCTOBER","NOVEMBER","DECEMBER" },
                m => m == month.Trim().ToUpperInvariant()) + 1;
        }

        private void SetStatusThreadSafe(string msg, StatusType type)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
                BeginInvoke(new Action(() => _setStatus?.Invoke(msg, type)));
            else
                _setStatus?.Invoke(msg, type);
        }

        #endregion helpers


        #region styling

        private void StyleDataGridView(DataGridView dgv)
        {
            float fontSize = 8.5f;

            dgv.BackgroundColor = Color.White;
            dgv.BorderStyle = BorderStyle.None;
            dgv.EnableHeadersVisualStyles = false;

            dgv.DefaultCellStyle.BackColor = Color.White;
            dgv.DefaultCellStyle.ForeColor = Color.Black;
            dgv.DefaultCellStyle.Font = new Font("Segoe UI", fontSize, FontStyle.Regular);
            dgv.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.GridColor = Color.LightGray;

            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(0, 155, 255);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", fontSize, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.ColumnHeadersHeight = 40;
            dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(10, 5, 10, 5);

            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);

            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 123, 255);
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;

            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.ColumnHeader;
            dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

            dgv.AllowUserToOrderColumns = false;
            dgv.AllowUserToResizeRows = false;
            dgv.AllowUserToResizeColumns = false;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.ReadOnly = true;

            dgv.ScrollBars = ScrollBars.Vertical;
        }

        private void ApplyModernStyleToDataGrids()
        {
            StyleDataGridView(dataGridView1);
            StyleDataGridView(dataGridView2);
            StyleDataGridView(dataGridView3);
            StyleDataGridView(dataGridView4);
            StyleDataGridView(dataGridViewStock);
            StyleDataGridView(dataGridViewOrder);
        }

        #endregion styling
    }
}
