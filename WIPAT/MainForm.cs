using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WIPAT.BLL;
using WIPAT.BLL.Interfaces;
using WIPAT.BLL.Manager;
using WIPAT.DAL;
using WIPAT.DAL.Interfaces;
using WIPAT.Entities;
using WIPAT.Entities.Dto;
using WIPAT.Entities.Enum;

namespace WIPAT
{
    public partial class MainForm : Form
    {
        #region start 

        #region UI Colors and Step Management

        private readonly Color StepDefaultBack = Color.FromArgb(0, 0, 64);
        private readonly Color StepHoverBack = Color.FromArgb(20, 20, 100);
        private readonly Color StepActiveBack = Color.DodgerBlue;
        private readonly Color StepDefaultFore = Color.LightGray;
        private readonly Color StepActiveFore = Color.White;

        //steps
        public enum Step { Upload = 1, Calculate = 2, Export = 3 }
        private bool _calcDone;
        private bool IsUploadDone = false;  // Track if upload is done
        private bool IsCalculateDone = false;  // Track if calculation is done
        private Label[] steps;
        private Form activeForm = null;
        #endregion UI Colors and Step Management


        #region Fields and Session Management
        //session
        private readonly WipSession _session = new WipSession();
        private readonly ToolTip _stepperTip = new ToolTip();

        // Declarative routing and tiny cache keyed by Step
        private readonly Dictionary<Step, Func<Form>> _routes = new Dictionary<Step, Func<Form>>();
        private readonly Dictionary<Step, Form> _cache = new Dictionary<Step, Form>();

        // Injected Services
        private readonly IForecastManager _forecastManager;
        private readonly IStockManager _stockManager;
        private readonly IOrderManager _orderManager;
        private readonly IExcelService _excelService;   // Handles Exports
        private readonly IWipManager _wipManager;       // Handles Calculations/History
        private readonly IItemsRepository _itemsRepository;
        private readonly IStockRepository _stockRepository;
        private readonly IWipRepository _wipRepository; // For editing logic
        private readonly INewWorkingWipManager _newWorkingWipManager;

        #endregion Fields and Session Management
        #endregion start

        #region Constructor
        public MainForm(
             User loggedInUser,
             IForecastManager forecastManager,
             IStockManager stockManager,
             IOrderManager orderManager,
             IExcelService excelService,
             IWipManager wipManager,
             IItemsRepository itemsRepository,
             IStockRepository stockRepository,
             IWipRepository wipRepository,
             INewWorkingWipManager newWorkingWipManager
             )
        {
            InitializeComponent();

            // Repositories / session
            _session.LoggedInUser = loggedInUser ?? throw new ArgumentNullException(nameof(loggedInUser));

            // Assign injected services
            _forecastManager = forecastManager;
            _stockManager = stockManager;
            _orderManager = orderManager;
            //_importManager = importManager;
            _excelService = excelService;
            _wipManager = wipManager;
            _itemsRepository = itemsRepository;
            _stockRepository = stockRepository;
            _wipRepository = wipRepository;
            _newWorkingWipManager = newWorkingWipManager;


            // Stepper wires
            steps = new[] { step1, step2, step3 };
            HookStepper();

            // Base styling
            stepperPanel.BackColor = StepDefaultBack;

            // Declarative routing → no switch forest
            _routes[Step.Upload] = () => new UploadForm(
                _session,
                _excelService,
                _forecastManager,
                _orderManager,
                new ForecastRepository(new WIPATContext()),
                new OrderRepository(new WIPATContext(), _session), 
                _itemsRepository,
                SetStatus
            );


            // 2. Calculate Form needs WipManager and Repos
            _routes[Step.Calculate] = () =>
            {
                var calc = new CalculateWIPForm(
                    _session,
                    SetStatus,
                    _wipManager,
                    _wipRepository,
                    _stockRepository,
                    _newWorkingWipManager
                );

                calc.CalculationCompleted -= OnStep2Completed;
                calc.CalculationCompleted += OnStep2Completed;
                return calc;
            };
            _routes[Step.Export] = () => new ExportForm(_session, SetStatus);

            // Ensure cached children are disposed on exit
            this.FormClosing += MainForm_FormClosing;

            // First paint
            GoTo(Step.Upload);
            UpdateStepperEnabledState();
        }

        #endregion Constructor

        #region Step Validation and Gatekeeping
        public bool CanEnter(Step step, out string reason)
        {
            reason = string.Empty;
            switch (step)
            {
                case Step.Calculate:
                    var (isReady, message) = IsReadyForCalc();
                    if (!isReady)
                    {
                        IsUploadDone = false;
                        reason = message;
                        return false;
                    }
                    else
                    {
                        IsUploadDone = true;
                        return true;
                    }

                case Step.Export:
                    // Ensure that calculation is done before export
                    if (!IsCalculateDone)
                    {
                        reason = "Complete Step 2 (Calculate WIP) before proceeding to Export.";
                        return false;
                    }
                    return true;

                default:
                    return false;
            }
        }

        // Reset Step 1 (Upload) progress
        public void ResetUpload() => IsUploadDone = false;

        // Reset Step 2 (Calculate) progress
        public void ResetCalc() => IsCalculateDone = false;

        // Check if the system is ready for calculation (Step 2)
        private (bool, string) IsReadyForCalc()
        {
            var missing = new List<string>();

            // Ensure session is initialized
            if (_session == null)
            {
                return (false, "Internal error: session is null.");
            }

            // Check for missing forecast files
            if (_session.ForecastFiles == null || _session.ForecastFiles.Count == 0)
                missing.Add("Forecast files (none loaded)");
            else if (_session.ForecastFiles.Count < 2)
                missing.Add($"At least 2 forecast files (currently {_session.ForecastFiles.Count})");

            // Ensure order file is loaded
            if (_session.Orders == null)
                missing.Add("Order file");

            // Ensure commitment period is valid
            if (_session.CommitmentPeriod <= 0)
                missing.Add("Commitment period");

            // Build error message if any missing data
            string message = missing.Count > 0
                ? "Please Provide Following:\n• " + string.Join("\n• ", missing)
                : string.Empty;

            return (missing.Count == 0, message);
        }
        #endregion Step Validation and Gatekeeping

        #region Stepper UI and Navigation
        private void HookStepper()
        {
            var map = new (Label lbl, Step step)[] {
                (step1, Step.Upload),
                (step2, Step.Calculate),
                (step3, Step.Export)
            };

            foreach (var (lbl, s) in map)
            {
                lbl.Tag = s;

                lbl.MouseEnter += (_, __) =>
                {
                    if (!lbl.Enabled) return;
                    if (lbl.BackColor != StepActiveBack)
                        lbl.BackColor = StepHoverBack;
                };

                lbl.MouseLeave += (_, __) =>
                {
                    if (!lbl.Enabled) return;
                    if (lbl.BackColor != StepActiveBack)
                        lbl.BackColor = StepDefaultBack;
                };

                lbl.Click += (_, __) => GoTo((Step)lbl.Tag);
            }
        }
        private void GoTo(Step desired)
        {
            // Check gate; if blocked, show reason and smart fallback
            if (!CanEnter(desired, out var reason))
            {
                SetStatus(string.IsNullOrWhiteSpace(reason) ? "Not allowed yet." : reason, StatusType.Warning);
                desired = desired == Step.Export ? Step.Calculate : Step.Upload;
            }

            // Update visuals
            SetActiveStep((int)desired);
            statusLabel.Text = $"Step {(int)desired}: {steps[(int)desired - 1].Text}";

            // Show form
            ShowForm(desired);

            // Update enabled tooltips for other steps
            UpdateStepperEnabledState();
        }
        private void ShowForm(Step step)
        {
            // get or create (persist state across nav)
            if (!_cache.TryGetValue(step, out var form) || form.IsDisposed)
            {
                form = _routes[step]();
                form.TopLevel = false;
                form.FormBorderStyle = FormBorderStyle.None;
                form.Dock = DockStyle.Fill;
                _cache[step] = form;
            }

            if (activeForm != null && !activeForm.IsDisposed)
                activeForm.Hide(); // keep state alive

            activeForm = form;

            if (!mainPanel.Controls.Contains(form))
                mainPanel.Controls.Add(form);

            form.BringToFront();
            form.Show();
        }
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (var kv in _cache.ToList())
            {
                if (kv.Value != null && !kv.Value.IsDisposed)
                    kv.Value.Dispose();
            }
            _cache.Clear();
        }
        private void SetActiveStep(int stepNumber)
        {
            for (int i = 0; i < steps.Length; i++)
            {
                if (i == stepNumber - 1)
                {
                    steps[i].BackColor = StepActiveBack;
                    steps[i].ForeColor = StepActiveFore;
                }
                else
                {
                    steps[i].BackColor = StepDefaultBack;
                    steps[i].ForeColor = steps[i].Enabled ? StepDefaultFore : Color.Gray;
                }
            }
        }

        /// Enable/disable steps + tooltips (visual gating only).
        public void UpdateStepperEnabledState()
        {
            // Step 1: Upload - Always enabled
            step1.Enabled = true;
            step1.Cursor = Cursors.Hand;
            _stepperTip.SetToolTip(step1, "Proceed to Upload");
            step1.BackColor = StepDefaultBack;
            step1.ForeColor = StepDefaultFore;

            // Step 2: Calculate - Only enabled if Step 1 is done and other conditions are met
            bool canCalculate = CanEnter(Step.Calculate, out var calcReason);
            if (canCalculate)
            {
                step2.Enabled = canCalculate;
                step2.Cursor = canCalculate ? Cursors.Hand : Cursors.No;
                _stepperTip.SetToolTip(step2, canCalculate ? "Proceed to Calculate WIP" : (string.IsNullOrWhiteSpace(calcReason) ? "Not ready" : calcReason));

            }
            if (!IsUploadDone || (_session.ForecastFiles?.Any() ?? false) == false || _session.Orders == null || _session.CommitmentPeriod <= 0)
            {
                canCalculate = false;
                calcReason = "Complete Step 1 (Upload) first.";
            }


            // Visual updates for Step 2
            if (canCalculate)
            {
                step2.BackColor = StepDefaultBack;
                step2.ForeColor = StepDefaultFore;
            }
            else
            {
                step2.BackColor = StepDefaultBack;
                step2.ForeColor = Color.Gray;
            }

            // Step 3: Export - Only enabled if Step 2 is done and other conditions are met
            bool canExport = CanEnter(Step.Export, out var exportReason);

            step3.Enabled = canExport;
            step3.Cursor = canExport ? Cursors.Hand : Cursors.No;
            _stepperTip.SetToolTip(step3, canExport ? "Proceed to Export" : (string.IsNullOrWhiteSpace(exportReason) ? "Not ready" : exportReason));

            // Visual updates for Step 3
            if (canExport)
            {
                step3.BackColor = StepDefaultBack;
                step3.ForeColor = StepDefaultFore;
            }
            else
            {
                step3.BackColor = StepDefaultBack;
                step3.ForeColor = Color.Gray;
            }
        }

        // ==== Calc done / reset hooks (called by children) ====
        private void OnStep2Completed()
        {
            IsCalculateDone = true;
            UpdateStepperEnabledState();
            SetStatus("WIP calculation completed. You can proceed to Export.", StatusType.Success);
        }

        /// <summary>
        /// Call this from UploadForm when Step 1 inputs change
        /// (re-uploads, month change, etc.) to relock Step 3.
        /// </summary>
        public void ResetStep2Progress()
        {
            ResetCalc();
            UpdateStepperEnabledState();
        }
        #endregion Stepper UI and Navigation

        #region status 
        public void SetStatus(string message, StatusType statusType)
        {
            // 1. Thread Safety: Ensure UI updates happen on the main thread
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => SetStatus(message, statusType)));
                return;
            }

            // 2. Set the text (Default to "Ready" if empty)
            statusLabel.Text = string.IsNullOrEmpty(message) ? "Ready" : message;
            statusLabel.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            statusLabel.Padding = new Padding(10, 5, 10, 5);
            statusStrip.BackColor = Color.FromArgb(245, 245, 245);
            statusLabel.BackColor = Color.Transparent;

            // 3. Apply the "Beautiful" Modern Colors
            switch (statusType)
            {
                case StatusType.Success:
                    statusLabel.BackColor = Color.FromArgb(40, 167, 69);
                    statusLabel.ForeColor = Color.White;
                    break;
                case StatusType.Error:
                    statusLabel.BackColor = Color.FromArgb(220, 53, 69);
                    statusLabel.ForeColor = Color.White;
                    break;
                case StatusType.Warning:
                    statusLabel.BackColor = Color.FromArgb(255, 193, 7);
                    statusLabel.ForeColor = Color.Black;
                    break;
                case StatusType.Reset:
                case StatusType.Transparent:
                default:
                    statusLabel.BackColor = Color.Transparent;
                    statusLabel.ForeColor = Color.DimGray;
                    break;
            }

            // 4. Force Repaint Immediately
            statusStrip.Refresh();
        }
        #endregion status 

        #region items

        //Show Items Catalogue
        private async void ItemsCatalogueMenuItem_Click(object sender, EventArgs e)
        {
            Response<List<ItemCatalogue>> resItemsCatalogue = await _itemsRepository.GetItemCatalogues();
            if (!resItemsCatalogue.Success)
            {
                MessageBox.Show("Failed to load item catalogue:\n" + resItemsCatalogue.Message,
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _session.ItemCatalogue = resItemsCatalogue.Data;

            var filtered = resItemsCatalogue.Data
             .Select(x => new
             {
                 x.Casin,
                 x.Model,
                 x.Description,
                 x.ColorName,
                 x.Size,
                 x.PCPK,
                 x.CasePackQty
             })
             .ToList();



            var form = new Form
            {
                Text = "Item Catalogue",
                Width = 800,
                Height = 500,
                StartPosition = FormStartPosition.CenterParent
            };

            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToOrderColumns = false,
                DataSource = filtered
            };

            form.Controls.Add(dgv);
            form.ShowDialog();
        }

        // ADD ITEMS + Opening Stock
        private void addItemsToCatalogueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var form = new Form
            {
                Text = "Import Item Catalogue",
                Width = 800,
                Height = 500,
                StartPosition = FormStartPosition.CenterParent
            };

            var btnSelectFile = new Button
            {
                Text = "Select Excel File",
                Dock = DockStyle.Top,
                Height = 40
            };

            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            var btnSave = new Button
            {
                Text = "Save to Database",
                Dock = DockStyle.Bottom,
                Height = 40,
                Enabled = false
            };

            form.Controls.Add(dgv);
            form.Controls.Add(btnSave);
            form.Controls.Add(btnSelectFile);

            // Access
            DataTable catalogue = new DataTable();
            DataTable stock = new DataTable();

            // File selection + load
            btnSelectFile.Click += async (s, ev) =>
            {
                try
                {
                    OpenFileDialog ofd = new OpenFileDialog
                    {
                        Filter = "Excel Files|*.xlsx;*.xls",
                        Title = "Select Excel File"
                    };

                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        string filePath = ofd.FileName;

                        var resItemCatalogues = await _excelService.ReadCatalogDataTableFromExcel(filePath);
                        if (!resItemCatalogues.Success)
                        {
                            MessageBox.Show($"Error: {resItemCatalogues.Message}", "Import Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        // Access
                        catalogue = resItemCatalogues.Data[0];
                        stock = resItemCatalogues.Data[1];


                        dgv.DataSource = catalogue;
                        btnSave.Enabled = true;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message, "Import Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            // Save to DB
            btnSave.Click += (s, ev) =>
            {
                try
                {

                    if (catalogue == null && catalogue.Rows.Count <= 0)
                    {
                        MessageBox.Show("No data for ItemCatalogues to save.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (stock == null && stock.Rows.Count <= 0)
                    {
                        MessageBox.Show("No data for InitialStock to save.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }


                    var resBulkInsert = _stockRepository.BulkInsertCatalogueImport(catalogue, stock);
                    if (!resBulkInsert.Success)
                    {
                        MessageBox.Show($"Error: {resBulkInsert.Message}", "Save Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    }
                    else
                    {
                        MessageBox.Show($"{resBulkInsert.Message}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    form.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message, "Save Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            form.ShowDialog();
        }

        #endregion items

        #region Wip
        //show calculted wip
        private void calculatedWipsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Clear previous dropdown items to avoid duplicates
            calculatedWipsToolStripMenuItem.DropDownItems.Clear();

            try
            {

                #region Load Calculated WIPs 
                // Get the list of forecasts that have calculated WIPs
                var forecastsWithCalculatedWipResponse = _wipRepository.GetForecastsWithCalculatedWip();
                #endregion Load Calculated WIPs

                if (forecastsWithCalculatedWipResponse.Success && forecastsWithCalculatedWipResponse.Data != null && forecastsWithCalculatedWipResponse.Data.Any())
                {
                    int GetMonthNumber(string monthName)
                    {
                        if (string.IsNullOrEmpty(monthName))
                            return 13;

                        switch (monthName.ToLower())
                        {
                            case "january": return 1;
                            case "february": return 2;
                            case "march": return 3;
                            case "april": return 4;
                            case "may": return 5;
                            case "june": return 6;
                            case "july": return 7;
                            case "august": return 8;
                            case "september": return 9;
                            case "october": return 10;
                            case "november": return 11;
                            case "december": return 12;
                            default: return 13; // unknown month last
                        }
                    }

                    var sortedForecasts = forecastsWithCalculatedWipResponse.Data
                        .OrderBy(f => f.Year)
                        .ThenBy(f => GetMonthNumber(f.Month))
                        .ToList();

                    #region Build dropdown items for each period
                    // Loop through each forecast record
                    foreach (var forecast in sortedForecasts)
                    {
                        var period = $"{forecast.Month} {forecast.Year}";
                        // Create a new submenu item for this period
                        var forecastItem = new ToolStripMenuItem(period);
                        // Add item to dropdown
                        calculatedWipsToolStripMenuItem.DropDownItems.Add(forecastItem);

                        #region On click, fetch & display WIP details for the period
                        forecastItem.Click += async (s, args) =>
                        {
                            try
                            {
                                // Fetch WIP details for the selected period
                                Response<List<WipDetail>> detailsResponse = await _wipRepository.GetWipDetailsByPeriodAsync(forecast.Month, forecast.Year);

                                if (detailsResponse.Success && detailsResponse.Data != null)
                                {
                                    if (detailsResponse.Data.Any())
                                    {
                                        var selectedData = detailsResponse.Data
                                                            .Select(d => new { CASIN = d.CASIN, WipQuantity = d.WipQuantity })
                                                            .ToList();

                                        #region Build UI (Form + Grid + Export Button)
                                        // Create a new form to display data
                                        var form = new Form
                                        {
                                            Text = $"WIP Details for {period}",
                                            Width = 1000,
                                            Height = 600,
                                            StartPosition = FormStartPosition.CenterParent
                                        };

                                        // Create DataGridView
                                        var dgv = new DataGridView
                                        {
                                            Dock = DockStyle.Fill,
                                            ReadOnly = true,
                                            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                                            AllowUserToOrderColumns = false,
                                            DataSource = selectedData
                                        };

                                        // Add Export button
                                        var exportButton = new Button
                                        {
                                            Text = "Export to Excel",
                                            Dock = DockStyle.Top,
                                            Height = 35
                                        };
                                        #endregion

                                        #region Export handler
                                        // Export button event
                                        exportButton.Click += (s2, e2) =>
                                        {
                                            try
                                            {
                                                string fileName = $"{period}-Wip.xlsx";
                                                string worksheetName = $"{period}-Wip";
                                                _excelService.ExportWipDataToExcel(selectedData, fileName, worksheetName);
                                            }
                                            catch (Exception ex)
                                            {
                                                MessageBox.Show($"An error occurred while exporting to Excel:\n{ex.Message}",
                                                                "Export Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            }
                                        };
                                        #endregion

                                        #region Show form

                                        form.Controls.Add(dgv);
                                        form.Controls.Add(exportButton);
                                        form.ShowDialog();
                                        #endregion

                                    }
                                    else
                                    {
                                        MessageBox.Show(detailsResponse.Message ?? "No records found.", "No Data",
                                                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    }
                                }
                                else
                                {
                                    MessageBox.Show($"Failed to load WIP details:\n{detailsResponse.Message}",
                                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"An unexpected error occurred while fetching details:\n{ex.Message}",
                                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        };
                        #endregion On click, fetch & display WIP details for the period
                    }
                    #endregion Build dropdown items for each period

                }
                else
                {
                    MessageBox.Show(
                        $"Error: {forecastsWithCalculatedWipResponse.Message ?? "No calculated WIP data found"}",
                        "Load Failed", MessageBoxButtons.OK, MessageBoxIcon.Error
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"An unexpected error occurred while loading calculated WIPs:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error
                );
            }
        }

        //edit - calculated wip
        private void editCalculatedWipsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Reset dropdown 
            editCalculatedWipsToolStripMenuItem.DropDownItems.Clear();

            try
            {
                #region FETCH: Forecast periods with calculated WIP
                var forecastsWithCalculatedWipResponse = _wipRepository.GetForecastsWithCalculatedWip();
                #endregion

                #region  Validate repository response
                if (!(forecastsWithCalculatedWipResponse.Success
                      && forecastsWithCalculatedWipResponse.Data != null
                      && forecastsWithCalculatedWipResponse.Data.Any()))
                {
                    MessageBox.Show(
                        $"Error: {forecastsWithCalculatedWipResponse.Message ?? "No calculated WIP data found"}",
                        "Load Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return;
                }
                #endregion

                #region Month name → month number
                int GetMonthNumber(string monthName)
                {
                    if (string.IsNullOrWhiteSpace(monthName)) return 13;

                    switch (monthName.Trim().ToLowerInvariant())
                    {
                        case "january": return 1;
                        case "february": return 2;
                        case "march": return 3;
                        case "april": return 4;
                        case "may": return 5;
                        case "june": return 6;
                        case "july": return 7;
                        case "august": return 8;
                        case "september": return 9;
                        case "october": return 10;
                        case "november": return 11;
                        case "december": return 12;
                        default: return 13; // Unknown -> push to end
                    }
                }
                #endregion

                #region SORT: Order periods by Year then Month
                var orderedForecasts = forecastsWithCalculatedWipResponse.Data
                    .OrderBy(f => f.Year)
                    .ThenBy(f => GetMonthNumber(f.Month))
                    .ToList();
                #endregion

                #region UI: Build dropdown items (Upload & Update per period)
                foreach (var f in orderedForecasts)
                {
                    // Copy to locals to avoid closure-capture bugs
                    var periodMonth = f.Month;
                    var periodYear = f.Year;
                    var periodLabel = $"{periodMonth} {periodYear}";

                    var periodMenuItem = new ToolStripMenuItem(periodLabel);
                    editCalculatedWipsToolStripMenuItem.DropDownItems.Add(periodMenuItem);

                    #region HANDLER: Upload Excel → Validate → Confirm → Apply bulk update
                    periodMenuItem.Click += async (s, args2) =>
                    {
                        try
                        {
                            #region FILE_PICKER: Choose Excel file with updates
                            using (var ofd = new OpenFileDialog
                            {
                                Title = $"Upload updated WIP for {periodLabel}",
                                Filter = "Excel Files (*.xlsx)|*.xlsx",
                                Multiselect = false
                            })
                            {
                                if (ofd.ShowDialog() != DialogResult.OK) return;
                                #endregion

                                #region PARSE_VALIDATE: Read and validate Excel (CASIN, WipQuantity)
                                var parseResult = _excelService.ReadEditWipExcel(ofd.FileName);

                                if (!parseResult.Success)
                                {
                                    MessageBox.Show(
                                        $"Validation failed:\n{parseResult.Message}",
                                        "Invalid File",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Warning
                                    );
                                    return;
                                }

                                var updates = parseResult.Data;
                                if (updates == null || updates.Count == 0)
                                {
                                    MessageBox.Show(
                                        "No valid rows found in the Excel file.",
                                        "Nothing to Update",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Information
                                    );
                                    return;
                                }
                                #endregion

                                #region CONFIRM: Ask user before applying updates
                                var confirm = MessageBox.Show(
                                    $"You are about to update {updates.Count} WIP entr{(updates.Count == 1 ? "y" : "ies")} for {periodLabel}.\n\nProceed?",
                                    "Confirm Update",
                                    MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Question
                                );
                                if (confirm != DialogResult.Yes) return;
                                #endregion

                                #region APPLY_UPDATES: Bulk update via repository
                                var updateResponse = await _wipRepository.AddUserWipQtyForPeriodAsync(periodMonth, periodYear, updates);
                                #endregion

                                #region Show result and refresh WIP details
                                if (updateResponse.Success)
                                {
                                    // Re-fetch the updated WIP details and display them
                                    var refreshedDetails = await _wipRepository.GetWipDetailsByPeriodAsync(periodMonth, periodYear);
                                    if (refreshedDetails.Success && refreshedDetails.Data != null && refreshedDetails.Data.Any())
                                    {
                                        var updatedData = refreshedDetails.Data
                                            .Select(d => new { d.CASIN, d.WipQuantity, d.UserWipQty })
                                            .ToList();

                                        // Call the reusable method to show updated details
                                        //ShowWipDetailsForm(periodLabel, updatedData);
                                        ShowWipApprovalForm(periodMonth, periodYear, refreshedDetails.Data);
                                    }
                                    else
                                    {
                                        MessageBox.Show("No WIP records found after update.", "No Data",
                                                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    }
                                }
                                else
                                {
                                    MessageBox.Show(
                                        $"Failed to update WIP for {periodLabel}:\n{updateResponse.Message}",
                                        "Update Failed",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Error
                                    );
                                }
                                #endregion

                                #region RESULT: Notify user of outcome
                                if (updateResponse.Success)
                                {
                                    MessageBox.Show(
                                        $"{(string.IsNullOrWhiteSpace(updateResponse.Message) ? "" : updateResponse.Message)}",
                                        "Update Successful",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Information
                                    );
                                }
                                else
                                {
                                    MessageBox.Show(
                                        $"Failed to update WIP for {periodLabel}:\n{updateResponse.Message}",
                                        "Update Failed",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Error
                                    );
                                }
                                #endregion
                            }
                        }
                        catch (Exception exInner)
                        {
                            #region ERROR: Unexpected failure during update flow
                            MessageBox.Show(
                                $"An unexpected error occurred while updating WIP:\n{exInner.Message}",
                                "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error
                            );
                            #endregion
                        }
                    };
                    #endregion
                }
                #endregion



            }
            catch (Exception ex)
            {
                #region ERROR: Unexpected failure while building edit menu
                MessageBox.Show(
                    $"An unexpected error occurred while loading calculated WIPs:\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                #endregion
            }
        }

        private void ShowWipApprovalForm(string month, string year, List<WipDetail> details)
        {
            try
            {
                // Build a DataTable for easy binding & selection
                var table = new DataTable();
                table.Columns.Add("CASIN", typeof(string));
                table.Columns.Add("CurrentWip", typeof(int));
                table.Columns.Add("ProposedWip", typeof(int));
                table.Columns.Add("Delta", typeof(int));

                foreach (var d in details)
                {
                    int current = d.WipQuantity.Value;
                    int proposed = d.UserWipQty.Value;
                    int delta = proposed - current;

                    table.Rows.Add(d.CASIN, current, proposed, delta);
                }

                var form = new Form
                {
                    Text = $"Approve WIP for {month}{year}",
                    Width = 500,
                    Height = 650,
                    StartPosition = FormStartPosition.CenterParent
                };

                var dgv = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    AllowUserToOrderColumns = false,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    MultiSelect = true,
                    DataSource = table,
                };

                // Top panel with actions
                var panel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Top,
                    Height = 48,
                    FlowDirection = FlowDirection.LeftToRight,
                    Padding = new Padding(8)
                };

                var btnApproveAll = new Button { Text = "Approve All", Width = 110, Height = 32 };
                panel.Controls.Add(btnApproveAll);

                // Adding search controls
                var lblSearch = new Label { Text = "Search CASIN:", Width = 100, TextAlign = ContentAlignment.MiddleRight };
                var txtSearch = new TextBox { Width = 150 };
                var btnSearch = new Button { Text = "Search", Width = 75, Height = 32 };

                panel.Controls.Add(lblSearch);
                panel.Controls.Add(txtSearch);
                panel.Controls.Add(btnSearch);

                #region approve
                // Approve helper
                async Task Approve(Func<DataRow, bool> rowFilter)
                {
                    try
                    {
                        // Build updates from rows where Proposed != Current
                        var updates = new List<WipDetail>();
                        foreach (DataRow r in table.Rows)
                        {
                            if (!rowFilter(r)) continue;

                            string casin = r.Field<string>("CASIN");
                            int current = r.Field<int>("CurrentWip");
                            int proposed = r.Field<int>("ProposedWip");
                            if (current == proposed) continue;

                            updates.Add(new WipDetail
                            {
                                CASIN = casin,
                                UserWipQty = proposed   // repository will set WipQuantity = UserWipQty
                            });
                        }

                        if (updates.Count == 0)
                        {
                            MessageBox.Show("Nothing to approve (no changes selected).", "Info",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }

                        var confirm = MessageBox.Show(
                            $"Approve {updates.Count} entr{(updates.Count == 1 ? "y" : "ies")} for {month}{year}?\n" +
                            "This will set WipQuantity = ProposedWip (UserWipQty).",
                            "Confirm Approval",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                        if (confirm != DialogResult.Yes) return;

                        var resp = await _wipRepository.UpdateWipForPeriodAsync(month, year, updates);
                        if (resp.Success)
                        {
                            MessageBox.Show(string.IsNullOrWhiteSpace(resp.Message)
                                ? "Approval applied successfully."
                                : resp.Message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            // Refresh the grid after approval
                            var refreshed = await _wipRepository.GetWipDetailsByPeriodAsync(month, year);
                            if (refreshed.Success && refreshed.Data != null)
                            {
                                // Update rows with new currents (WipQuantity)
                                var map = refreshed.Data.ToDictionary(x => x.CASIN, x => x, StringComparer.OrdinalIgnoreCase);
                                foreach (DataRow r in table.Rows)
                                {
                                    var casin = r.Field<string>("CASIN");
                                    if (map.TryGetValue(casin, out var w))
                                    {
                                        r.SetField("CurrentWip", w.WipQuantity);
                                        r.SetField("Delta", r.Field<int>("ProposedWip") - w.WipQuantity);
                                    }
                                }
                                dgv.Refresh();
                            }
                        }
                        else
                        {
                            MessageBox.Show($"Approval failed:\n{resp.Message}", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Unexpected error while approving:\n{ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                // Approve All (only those that actually change)
                btnApproveAll.Click += async (s, e) =>
                {
                    await Approve(r =>
                    {
                        int cur = r.Field<int>("CurrentWip");
                        int prop = r.Field<int>("ProposedWip");
                        return cur != prop;
                    });
                };
                #endregion approve

                // Search Button Clicked
                btnSearch.Click += (s, e) =>
                {
                    string searchValue = txtSearch.Text.Trim();
                    if (string.IsNullOrEmpty(searchValue))
                    {
                        // Reset the filter if search is empty
                        dgv.DataSource = table;
                    }
                    else
                    {
                        var filteredRows = table.AsEnumerable()
                                                 .Where(r => r.Field<string>("CASIN")
                                                            .IndexOf(searchValue, StringComparison.OrdinalIgnoreCase) >= 0)
                                                 .CopyToDataTable();

                        if (filteredRows.Rows.Count == 0)
                        {
                            MessageBox.Show("No CASIN found matching the search criteria.", "Search Result",
                                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }

                        dgv.DataSource = filteredRows;
                    }
                };

                #region show
                form.Controls.Add(dgv);
                form.Controls.Add(panel);
                form.ShowDialog();
                #endregion show

            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred while displaying approval UI:\n{ex.Message}",
                                 "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion wip

    }
}

