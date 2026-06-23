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
using WIPAT.Forms;
using WIPAT.Helpers;

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
            UITheme.SetFormIcon(this);

            // Repositories / session
            _session.LoggedInUser = loggedInUser ?? throw new ArgumentNullException(nameof(loggedInUser));

            // Assign injected services
            _forecastManager = forecastManager;
            _stockManager = stockManager;
            _orderManager = orderManager;
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

            // Declarative routing → LAZY LOADING instructions
            _routes[Step.Upload] = () =>
            {
                var uploadForm = new UploadForm(
                    _session,
                    _excelService,
                    _forecastManager,
                    _orderManager,
                    new ForecastRepository(new WIPATContext()),
                    new OrderRepository(new WIPATContext(), _session),
                    _itemsRepository,
                    SetStatus
                );

                // THE FIX: Listen to the Upload form so the Main form knows when files are ready!
                uploadForm.InputsChanged -= UpdateStepperEnabledState; // Prevent double-firing
                uploadForm.InputsChanged += UpdateStepperEnabledState;

                return uploadForm;
            };

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

            _routes[Step.Export] = () => new ExportForm(_session, SetStatus, _session.LoggedInUser);

            // Ensure cached children are disposed on exit
            this.FormClosing += MainForm_FormClosing;

            // --- THE FIX: WAIT UNTIL FORM LOADS TO RENDER CHILD COMPONENTS ---
            this.Load += MainForm_Load;
        }
        private void MainForm_Load(object sender, EventArgs e)
        {
            // First paint (Only executed AFTER the main screen appears)
            GoTo(Step.Upload);
        }
        #endregion Constructor

        #region Step Validation and Gatekeeping
        public bool CanEnter(Step step, out string reason)
        {
            reason = string.Empty;
            switch (step)
            {
                case Step.Upload:
                    return true;

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

            // REFACTORED: Now only checks for 1 forecast file
            if (_session.ForecastFiles == null || _session.ForecastFiles.Count == 0)
                missing.Add("Current Forecast file (none loaded)");

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
            bool isAllowed = CanEnter(desired, out var reason);

            // Check gate; if blocked, show reason and smart fallback
            if (!isAllowed)
            {
                SetStatus(string.IsNullOrWhiteSpace(reason) ? "Not allowed yet." : reason, StatusType.Warning);
                desired = desired == Step.Export ? Step.Calculate : Step.Upload;
            }

            // 1. Update enabled tooltips and states FIRST
            UpdateStepperEnabledState();

            // 2. Update visuals (Colors the active step blue)
            SetActiveStep((int)desired);

            // 3. Clear the status safely (removes stuck yellow background)
            if (isAllowed)
            {
                SetStatus($"Step {(int)desired}: {steps[(int)desired - 1].Text}", StatusType.Transparent);
            }

            // Show form
            ShowForm(desired);
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

            // Step 2: Calculate - Let CanEnter do all the thinking
            bool canCalculate = CanEnter(Step.Calculate, out var calcReason);

            step2.Enabled = canCalculate;
            step2.Cursor = canCalculate ? Cursors.Hand : Cursors.No;
            _stepperTip.SetToolTip(step2, canCalculate ? "Proceed to Calculate WIP" : (string.IsNullOrWhiteSpace(calcReason) ? "Not ready" : calcReason));

            // Step 3: Export 
            bool canExport = CanEnter(Step.Export, out var exportReason);

            step3.Enabled = canExport;
            step3.Cursor = canExport ? Cursors.Hand : Cursors.No;
            _stepperTip.SetToolTip(step3, canExport ? "Proceed to Export" : (string.IsNullOrWhiteSpace(exportReason) ? "Not ready" : exportReason));
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

        #region Items Catalogue Management

        private async void ItemsCatalogueMenuItem_Click(object sender, EventArgs e)
        {
            //using (var catalogueForm = new ItemsCatalogueForm(_session, _itemsRepository, _stockRepository, _excelService))
            using (var catalogueForm = new ItemsCatalogueForm(_session, _itemsRepository, _stockRepository))
            {
                catalogueForm.ShowDialog();

                try
                {
                    var res = await _itemsRepository.GetActiveItemCatalogues();
                    if (res.Success)
                    {
                        _session.ItemCatalogue = res.Data;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to update session item list: {ex.Message}",
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        #endregion Items Catalogue Management

        #region Calulated WIP Management
        private void calculatedWipsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var form = new CalculatedWipsForm(_session, _wipRepository, _excelService, _session.LoggedInUser.Id, _itemsRepository))
            {
                form.ShowDialog();
            }
        }

        #endregion Calulated WIP Management

        #region Template Download Management

        private void downloadTemplatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var form = new TemplateDownloadForm())
            {
                form.ShowDialog(this);
            }
        }
        #endregion

        private void templateUploadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var form = new TestTemplateUploadForm(_session, _itemsRepository))
            {
                form.ShowDialog(this);
            }
        }
    }
}