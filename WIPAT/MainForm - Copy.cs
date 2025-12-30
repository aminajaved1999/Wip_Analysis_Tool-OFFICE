#region new
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WIPAT.BLL;
using WIPAT.DAL;
using WIPAT.Entities;
using WIPAT.Entities.Dto;
using WIPAT.Entities.Enum;

namespace WIPAT
{
    public partial class MainForm : Form
    {
        // ==== Step enum & gate interface/impl (kept in same file for simplicity) ====
        public enum Step { Upload = 1, Calculate = 2, Export = 3 }

        public interface IStepGate
        {
            bool CanEnter(Step step, out string reason);
            void MarkCalcDone();
            void ResetCalc();
        }

        /// <summary>
        /// Holds all "can I go to step X?" rules so MainForm stays tiny.
        /// </summary>
        public sealed class StepGate : IStepGate
        {
            private readonly WipSession _session;
            private bool _calcDone;

            public StepGate(WipSession session) => _session = session ?? throw new ArgumentNullException(nameof(session));

            public bool CanEnter(Step step, out string reason)
            {
                reason = string.Empty;

                if (step == Step.Upload)
                    return true;

                // minimal readiness used by Step 2 & 3
                bool baseReady = IsReadyForCalc(out var missingMsg);

                if (step == Step.Calculate)
                {
                    if (!baseReady) { reason = missingMsg; return false; }

                    // Prevent recalculation if current file is locked
                    var current = _session.ForecastFiles?
                        .FirstOrDefault(f => $"{f.ProjectionMonth} {f.ProjectionYear}" == _session.CurrentMonthWithYear);

                    if (current?.IsWipAlreadyCalculated == true)
                    {
                        reason = "WIP for the current file is already calculated. Modification is not allowed.";
                        return false;
                    }

                    return true;
                }

                if (step == Step.Export)
                {
                    if (!baseReady) { reason = missingMsg; return false; }
                    if (!_calcDone) { reason = "Complete Step 2 (Calculate WIP) before proceeding to Export."; return false; }
                    return true;
                }

                return false;
            }

            public void MarkCalcDone() => _calcDone = true;
            public void ResetCalc() => _calcDone = false;

            private bool IsReadyForCalc(out string message)
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

                // Orders
                if (_session.Orders == null)
                    missing.Add("Order file");

                // Commitment period
                if (_session.CommitmentPeriod <= 0)
                    missing.Add("Commitment period");

                message = missing.Count > 0
                    ? "Please Provide Following:\n• " + string.Join("\n• ", missing)
                    : string.Empty;

                return missing.Count == 0;
            }
        }

        // ==== UI Colors ====
        private readonly Color StepDefaultBack = Color.FromArgb(0, 0, 64);
        private readonly Color StepHoverBack = Color.FromArgb(20, 20, 100);
        private readonly Color StepActiveBack = Color.DodgerBlue;

        private readonly Color StepDefaultFore = Color.LightGray;
        private readonly Color StepActiveFore = Color.White;

        // ==== Fields ====
        private Label[] steps;
        private Form activeForm = null;

        private readonly WipSession _session = new WipSession();
        private readonly ToolTip _stepperTip = new ToolTip();

        private ItemsRepository itemsRepository;
        private IStepGate _gate;

        // Declarative routing and tiny cache keyed by Step
        private readonly Dictionary<Step, Func<Form>> _routes = new Dictionary<Step, Func<Form>>();
        private readonly Dictionary<Step, Form> _cache = new Dictionary<Step, Form>();


        public MainForm(User loggedInUser)
        {
            InitializeComponent();

            // Repositories / session
            itemsRepository = new ItemsRepository();
            _session.LoggedInUser = loggedInUser ?? throw new ArgumentNullException(nameof(loggedInUser));
            _gate = new StepGate(_session);

            // Stepper wires
            steps = new[] { step1, step2, step3 };
            HookStepper();

            // Base styling
            stepperPanel.BackColor = StepDefaultBack;

            // Declarative routing → no switch forest
            _routes[Step.Upload] = () => new UploadForm(_session, SetStatus);
            _routes[Step.Calculate] = () =>
            {
                var calc = new CalculateWIPForm(_session, SetStatus);
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

        // ==== Stepper wiring ====
        private void HookStepper()
        {
            var map = new (Label lbl, Step step)[]
            {
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

        // ==== Navigation ====
        private void GoTo(Step desired)
        {
            // Check gate; if blocked, show reason and smart fallback
            if (!_gate.CanEnter(desired, out var reason))
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

        // ==== UI helpers ====
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

        /// <summary>
        /// Enable/disable steps + tooltips (visual gating only).
        /// </summary>
        public void UpdateStepperEnabledState()
        {
            // Step 2
            bool canCalc = _gate.CanEnter(Step.Calculate, out var calcReason);



             canCalc = true; 
             calcReason = string.Empty;



            step2.Enabled = canCalc;
            step2.Cursor = canCalc ? Cursors.Hand : Cursors.No;
            _stepperTip.SetToolTip(step2, canCalc ? "Proceed to Calculate WIP" : (string.IsNullOrWhiteSpace(calcReason) ? "Not ready" : calcReason));
            if (!canCalc && step2.BackColor != StepActiveBack) { step2.BackColor = StepDefaultBack; step2.ForeColor = Color.Gray; }
            if (canCalc && step2.BackColor != StepActiveBack) { step2.ForeColor = StepDefaultFore; }

            // Step 3
            bool canExport = _gate.CanEnter(Step.Export, out var exportReason);
            step3.Enabled = canExport;
            step3.Cursor = canExport ? Cursors.Hand : Cursors.No;
            _stepperTip.SetToolTip(step3, canExport ? "Proceed to Export" : (string.IsNullOrWhiteSpace(exportReason) ? "Not ready" : exportReason));
            if (!canExport && step3.BackColor != StepActiveBack) { step3.BackColor = StepDefaultBack; step3.ForeColor = Color.Gray; }
            if (canExport && step3.BackColor != StepActiveBack) { step3.ForeColor = StepDefaultFore; }
        }

        /// <summary>
        /// Thread-safe status bar helper.
        /// </summary>
        public void SetStatus(string message, StatusType statusType)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => SetStatus(message, statusType)));
                return;
            }

            statusLabel.Text = message;

            switch (statusType)
            {
                case StatusType.Success:
                    statusLabel.BackColor = Color.Green;
                    statusLabel.ForeColor = Color.White;
                    break;
                case StatusType.Error:
                    statusLabel.BackColor = Color.Red;
                    statusLabel.ForeColor = Color.White;
                    break;
                case StatusType.Reset:
                case StatusType.Transparent:
                    statusLabel.Text = statusType == StatusType.Reset ? string.Empty : message;
                    statusLabel.BackColor = Color.Transparent;
                    statusLabel.ForeColor = Color.Black;
                    break;
                case StatusType.Warning:
                    statusLabel.BackColor = Color.Yellow;
                    statusLabel.ForeColor = Color.Black;
                    break;
                default:
                    statusLabel.BackColor = Color.Transparent;
                    statusLabel.ForeColor = Color.Black;
                    break;
            }
        }

        // ==== Calc done / reset hooks (called by children) ====
        private void OnStep2Completed()
        {
            _gate.MarkCalcDone();
            UpdateStepperEnabledState();
            SetStatus("WIP calculation completed. You can proceed to Export.", StatusType.Success);
        }

        /// <summary>
        /// Call this from UploadForm when Step 1 inputs change
        /// (re-uploads, month change, etc.) to relock Step 3.
        /// </summary>
        public void ResetStep2Progress()
        {
            _gate.ResetCalc();
            UpdateStepperEnabledState();
        }

        #region items
        #region Show Items Catalogue

        private async void ItemsCatalogueMenuItem_Click(object sender, EventArgs e)
        {
            Response<List<ItemCatalogue>> resItemsCatalogue = await itemsRepository.GetItemCatalogues();
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

        #endregion Show Items Catalogue

        #region ADD ITEMS + Opening Stock
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

                        var resItemCatalogues = await ValidateAndGetCatalogueDataTable(filePath);
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


                    var resBulkInsert = BulkInsertToDatabase(catalogue, stock);
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


        #region validate excel  
        public async Task<Response<List<DataTable>>> ValidateAndGetCatalogueDataTable(string filePath)
        {
            var response = new Response<List<DataTable>>();
            try
            {

                // Validate the Excel file
                var validationResponse = await ValidateExcelFile(filePath);
                if (!validationResponse.Success)
                {
                    response.Success = false;
                    response.Message = validationResponse.Message;
                    return response;
                }

                string workSheetName = validationResponse.Data;
                // Get Item Catalogues DataTable
                Response<DataTable> resItemCatalogues = await GetItemCataloguesDataTableFromExcel(filePath, workSheetName);
                if (resItemCatalogues.Success == false)
                {
                    response.Success = false;
                    response.Message = resItemCatalogues.Message;
                    return response;
                }

                // Get Stock DataTable
                Response<DataTable> resInitialStock = await GetStockDataTableFromExcel(filePath, workSheetName);
                if (resInitialStock.Success == false)
                {
                    response.Success = false;
                    response.Message = resInitialStock.Message;
                    return response;
                }

                DataTable catalogueTable = resItemCatalogues.Data;
                DataTable stockTable = resInitialStock.Data;

                response.Data = new List<DataTable>();
                response.Data.Add(catalogueTable);
                response.Data.Add(stockTable);

                response.Success = true;

            }
            catch (Exception ex)
            {
                // Handle exceptions and return a failure response with the error message
                response.Success = false;
                response.Message = $"An error occurred: {ex.Message}";
            }

            return response;
        }

        public async Task<Response<string>> ValidateExcelFile(string filePath)
        {
            var response = new Response<string>();
            var requiredExcelColumns = AllColumnNames.ExcelColumnNames.ToList();
            string requiredWorkSheetName = "ItemCatalogues";
            var allowedExtensions = new[] { ".xls", ".xlsx" };

            #region Input Validation
            if (string.IsNullOrWhiteSpace(filePath))
            {
                response.Success = false;
                response.Message = "File path is empty.";
                return response;
            }

            if (!File.Exists(filePath))
            {
                response.Success = false;
                response.Message = "The selected file does not exist.";
                return response;
            }

            var fileExtension = Path.GetExtension(filePath).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
            {
                response.Success = false;
                response.Message = "Invalid file type. Please select a valid Excel file (.xls or .xlsx).";
                return response;
            }
            #endregion

            try
            {
                // Open the Excel file using EPPlus asynchronously
                var fileInfo = new FileInfo(filePath);

                using (var package = await Task.Run(() => new ExcelPackage(fileInfo)))
                {
                    #region File Processing (Opening and Reading Excel File)

                    if (package.Workbook.Worksheets.Count == 0)
                    {
                        response.Success = false;
                        response.Message = "The workbook does not contain any worksheets.";
                        return response;
                    }

                    var worksheet = package.Workbook.Worksheets[requiredWorkSheetName];

                    // Check if worksheet exists
                    if (worksheet == null || worksheet.Dimension == null)
                    {
                        response.Success = false;
                        response.Message = $"Worksheet '{requiredWorkSheetName}' is missing or empty.";
                        return response;
                    }

                    #endregion

                    #region Header Validation
                    // Get the header row (first row in the worksheet)
                    var headerRow = Enumerable.Range(1, worksheet.Dimension.End.Column)
                                              .Select(col => worksheet.Cells[1, col].Text)
                                              .ToList();

                    // Check which columns match or are missing
                    var missingColumns = requiredExcelColumns.Except(headerRow).ToList();
                    var extraColumns = headerRow.Except(requiredExcelColumns).ToList();

                    if (missingColumns.Any() || extraColumns.Any())
                    {
                        string missingMessage = missingColumns.Any() ? $"Missing columns: {string.Join(", ", missingColumns)}." : string.Empty;
                        string extraMessage = extraColumns.Any() ? $"Extra columns: {string.Join(", ", extraColumns)}." : string.Empty;

                        response.Success = false;
                        response.Message = $"{missingMessage} {extraMessage}".Trim();
                    }

                    #endregion

                    #region Data Validation
                    // Validate column data types
                    bool dataTypesValid = true;

                    for (int row = 2; row <= worksheet.Dimension.End.Row; row++) // Start from row 2 to skip the header
                    {
                        foreach (var column in AllColumnNames.ExcelColumnIndexes)
                        {
                            var columnName = column.Key;
                            var columnIndex = column.Value;
                            var cellValue = worksheet.Cells[row, columnIndex].Text;

                            #region validation
                            bool IsNumeric(string s) => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
                            bool IsEmpty(string s) => string.IsNullOrWhiteSpace(s);

                            if (columnName == AllColumnNames.CasePackQty) // optional numeric
                            {
                                if (!IsEmpty(cellValue) && !IsNumeric(cellValue))
                                {
                                    response.Success = false;
                                    response.Message = $"Column '{columnName}' at row {row} must be numeric if provided. Found: '{cellValue}'.";
                                    dataTypesValid = false;
                                }
                            }
                            else if (columnName == AllColumnNames.PCPK || columnName == AllColumnNames.OpeningStock) // required numeric
                            {
                                if (IsEmpty(cellValue))
                                {
                                    response.Success = false;
                                    response.Message = $"Column '{columnName}' at row {row} is required and cannot be empty.";
                                    dataTypesValid = false;
                                }
                                else if (!IsNumeric(cellValue))
                                {
                                    response.Success = false;
                                    response.Message = $"Column '{columnName}' at row {row} must be numeric. Found: '{cellValue}'.";
                                    dataTypesValid = false;
                                }
                            }
                            else if (columnName == AllColumnNames.CAsin || columnName == AllColumnNames.Model || columnName == AllColumnNames.Description || columnName == AllColumnNames.ColorName || columnName == AllColumnNames.Size) // required text
                            {
                                if (IsEmpty(cellValue))
                                {
                                    response.Success = false;
                                    response.Message = $"Column '{columnName}' at row {row} is required and cannot be empty.";
                                    dataTypesValid = false;
                                }
                                else if (IsNumeric(cellValue))
                                {
                                    response.Success = false;
                                    response.Message = $"Column '{columnName}' at row {row} must be text, but a numeric value was found: '{cellValue}'.";
                                    dataTypesValid = false;
                                }
                            }
                            #endregion validation
                        }

                        if (!dataTypesValid)
                            break;
                    }
                    #endregion

                    #region Final Response
                    if (dataTypesValid)
                    {
                        response.Success = true;
                        response.Message = "Columns match the required ones, and the data types are correct.";
                        response.Data = requiredWorkSheetName;
                    }
                    #endregion
                }
            }
            catch (FileNotFoundException)
            {
                response.Success = false;
                response.Message = "The file was not found.";
            }
            catch (UnauthorizedAccessException)
            {
                response.Success = false;
                response.Message = "You do not have permission to access the file.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Cannot open file. It may be in use or corrupted. Error: {ex.Message}";
            }

            return response;
        }


        #region get datatable
        //items catalogue 
        private async Task<Response<DataTable>> GetItemCataloguesDataTableFromExcel(string filePath, string requiredWorkSheetName)
        {
            var response = new Response<DataTable>();
            try
            {
                List<string> requiredCatalogueTableColumns = AllColumnNames.CatalogueTableColumns.ToList();

                #region add columns to DataTable
                DataTable dt = new DataTable();

                // Loop through the column names and add columns to the DataTable with the correct types
                foreach (var columnName in requiredCatalogueTableColumns)
                {
                    Type columnType = AllColumnNames.GetColumnType(columnName);
                    dt.Columns.Add(columnName, columnType);
                }
                #endregion add columns to DataTable

                // Open the Excel file using EPPlus asynchronously
                var fileInfo = new FileInfo(filePath);

                using (var package = await Task.Run(() => new ExcelPackage(fileInfo)))
                {

                    if (package.Workbook.Worksheets.Count == 0)
                    {
                        response.Success = false;
                        response.Message = "The workbook does not contain any worksheets.";
                        return response;
                    }

                    var worksheet = package.Workbook.Worksheets[requiredWorkSheetName];


                    // Ensure worksheet is not disposed
                    if (worksheet == null || worksheet.Dimension == null)
                    {
                        response.Success = false;
                        response.Message = "Worksheet is not valid or has been disposed.";
                        response.Data = null;
                        return response;
                    }

                    int rowCount = worksheet.Dimension.Rows;

                    // Assumes first row is header → start from row 2
                    for (int row = 2; row <= rowCount; row++)
                    {
                        DataRow dr = dt.NewRow();

                        // Iterate through required columns and map values dynamically using columnIndexMap
                        foreach (var column in requiredCatalogueTableColumns)
                        {

                            if (column == AllColumnNames.CreatedAt)
                            {
                                dr[column] = DateTime.Now;
                            }
                            else if (column == AllColumnNames.CreatedById)
                            {
                                dr[column] = _session.LoggedInUser.Id;
                            }
                            else
                            {
                                string cellValue = worksheet.Cells[row, AllColumnNames.ExcelColumnIndexes[column]].Text;

                                //map values
                                var drRes = MapColumnValues(column, cellValue, dr, row);
                                if (!drRes.Success)
                                {
                                    response.Success = drRes.Success;
                                    response.Message = drRes.Message;
                                    return response;
                                }

                                dr = drRes.Data;
                            }

                        }

                        dt.Rows.Add(dr);
                    }

                    response.Success = true;
                    response.Message = "Items Catalogue Data read successfully.";
                    response.Data = dt;
                    return response;
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error reading Items Catalogue Data from Excel file: {ex.Message}";
                response.Data = null;
                return response;
            }

        }

        //stock
        public async Task<Response<DataTable>> GetStockDataTableFromExcel(string filePath, string requiredWorkSheetName)
        {
            var response = new Response<DataTable>();
            try
            {
                List<string> requiredStockTableColumns = AllColumnNames.StockTableColumns.ToList();

                #region add columns to DataTable
                DataTable dt = new DataTable();

                // Loop through the column names and add columns to the DataTable with the correct types
                foreach (var columnName in requiredStockTableColumns)
                {
                    Type columnType = AllColumnNames.GetColumnType(columnName);
                    dt.Columns.Add(columnName, columnType);
                }
                #endregion add columns to DataTable

                // Open the Excel file using EPPlus asynchronously
                var fileInfo = new FileInfo(filePath);

                using (var package = await Task.Run(() => new ExcelPackage(fileInfo)))
                {

                    if (package.Workbook.Worksheets.Count == 0)
                    {
                        response.Success = false;
                        response.Message = "The workbook does not contain any worksheets.";
                        return response;
                    }

                    var worksheet = package.Workbook.Worksheets[requiredWorkSheetName];


                    // Ensure worksheet is not disposed
                    if (worksheet == null || worksheet.Dimension == null)
                    {
                        response.Success = false;
                        response.Message = "Worksheet is not valid or has been disposed.";
                        response.Data = null;
                        return response;
                    }

                    int rowCount = worksheet.Dimension.Rows;

                    // Assumes first row is header → start from row 2
                    for (int row = 2; row <= rowCount; row++)
                    {
                        DataRow dr = dt.NewRow();

                        // Iterate through the requiredColumns and map values dynamically using columnIndexMap
                        foreach (var column in requiredStockTableColumns)
                        {
                            if (column == AllColumnNames.CreatedAt)
                            {
                                dr[column] = DateTime.Now;
                            }
                            else if (column == AllColumnNames.CreatedById)
                            {
                                dr[column] = _session.LoggedInUser.Id;
                            }
                            else if (column == AllColumnNames.ItemCatalogueId)
                            {
                                dr[column] = 0;
                            }
                            else
                            {
                                string cellValue = worksheet.Cells[row, AllColumnNames.ExcelColumnIndexes[column]].Text;

                                //map values
                                var drRes = MapColumnValues(column, cellValue, dr, row);
                                if (!drRes.Success)
                                {
                                    response.Success = drRes.Success;
                                    response.Message = drRes.Message;
                                    return response;
                                }

                                dr = drRes.Data;
                            }
                        }

                        dt.Rows.Add(dr);  // Add the row to the DataTable
                    }

                    response.Success = true;
                    response.Message = "Initial stock data loaded successfully.";
                    response.Data = dt;
                }

            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error reading initial stock Excel file: {ex.Message}";
                response.Data = null;
            }

            return response;
        }

        private Response<DataRow> MapColumnValues(string column, string cellValue, DataRow dr, int row)
        {
            var response = new Response<DataRow>();

            try
            {
                bool IsNumeric(string s) => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
                bool IsEmpty(string s) => string.IsNullOrWhiteSpace(s);

                if (column == AllColumnNames.CasePackQty)
                {
                    if (!IsEmpty(cellValue) && !IsNumeric(cellValue))
                    {
                        var parseRes = TryParseIntegerColumn(column, cellValue, row, dr);
                        if (!parseRes.Success)
                        {
                            response.Success = parseRes.Success;
                            response.Message = parseRes.Message;
                            return response;
                        }
                    }

                }
                else if (column == AllColumnNames.PCPK || column == AllColumnNames.OpeningStock)
                {
                    var parseRes = TryParseIntegerColumn(column, cellValue, row, dr);
                    if (!parseRes.Success)
                    {
                        response.Success = parseRes.Success;
                        response.Message = parseRes.Message;
                        return response;
                    }
                }
                else if (column == AllColumnNames.CreatedById)
                {
                    dr[column] = _session.LoggedInUser.Id;
                }
                else if (column == AllColumnNames.CreatedAt)
                {
                    dr[column] = DateTime.Now;
                }
                else if (column == AllColumnNames.CAsin)
                {
                    dr[column] = cellValue;
                }
                else if (column == AllColumnNames.ItemCatalogueId)
                {
                    dr[column] = 0;
                }
                else
                {
                    dr[column] = cellValue;
                }

                // If no exception occurs and all columns are mapped correctly, we set success to true
                response.Success = true;
                response.Message = "Column values mapped successfully.";
                response.Data = dr;
            }
            catch (Exception ex)
            {
                response.Message = $"An error occurred while mapping column '{column}' for row {row}: {ex.Message}";
                response.Success = false;
            }

            return response;
        }


        private Response<DataRow> TryParseIntegerColumn(string column, string cellValue, int row, DataRow dr)
        {
            var response = new Response<DataRow>();

            try
            {
                if (int.TryParse(cellValue, out int parsedValue))
                {
                    dr[column] = parsedValue;
                    response.Success = true;
                    response.Message = "Success";
                    response.Data = dr;
                    return response;
                }
                else
                {
                    // Return a meaningful error response when parsing fails
                    response.Success = false;
                    response.Message = $"Invalid {column} value at row {row}. Could not parse '{cellValue}' as an integer.";
                    response.Data = null;
                    return response;
                }
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                response.Success = false;
                response.Message = $"An unexpected error occurred while processing the {column} value at row {row}: {ex.Message}";
                response.Data = null;
                return response;
            }
        }




        #endregion get datatable


        #endregion validate excel  


        #region bulk insert
        public Response<bool> BulkInsertToDatabase(DataTable dtItemCatalogues, DataTable dtInitialStock)
        {
            var response = new Response<bool>();

            // Database connection string
            string connectionString = ConfigurationManager.ConnectionStrings["dbContext"].ConnectionString;

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Step 3: bulk insert to ItemCatalogues
                        var resSaveCatalogue = BulkInsertToItemsCatalogue(dtItemCatalogues, conn, transaction);
                        if (!resSaveCatalogue.Success)
                        {
                            transaction.Rollback();
                            response.Success = false;
                            response.Message = $"Failed to bulk insert ItemsCatalogue: {resSaveCatalogue.Message}";
                            response.Data = false;
                            return response;
                        }

                        // Step 4: Retrieve the generated ItemCatalogueId values and update InitialStock
                        var resUpdateStockTable = MapItemCatalogueIds(dtItemCatalogues, dtInitialStock, conn, transaction);
                        if (!resUpdateStockTable.Success)
                        {
                            transaction.Rollback();
                            response.Success = false;
                            response.Message = "Failed to Map ItemCatalogue Ids.";
                            response.Data = false;
                            return response;
                        }

                        // Step 5: bulk insert to InitialStock
                        var resSaveStock = BulkInsertInitialStock(resUpdateStockTable.Data, conn, transaction);
                        if (!resSaveStock.Success)
                        {
                            transaction.Rollback();
                            response.Success = false;
                            response.Message = $"Failed to bulk insert Stock: {resSaveStock.Message}";
                            response.Data = false;
                            return response;
                        }

                        // Commit if everything succeeds
                        transaction.Commit();
                        response.Success = true;
                        response.Message = "Bulk insert completed successfully.";
                        response.Data = true;
                        return response;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        response.Success = false;
                        response.Message = $"Bulk insert Failed: {ex.Message}";
                        response.Data = false;
                        return response;
                    }
                }
            }
        }


        //// Step 1: Bulk Insert → Items Catalogue
        public Response<bool> BulkInsertToItemsCatalogue(DataTable dt, SqlConnection conn, SqlTransaction transaction)
        {
            var response = new Response<bool>();


            try
            {
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.KeepIdentity, transaction))
                {
                    bulkCopy.DestinationTableName = "dbo.ItemCatalogues";

                    // Column mappings (Excel → DB)
                    bulkCopy.ColumnMappings.Add(AllColumnNames.CAsin, "Casin");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.Model, "Model");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.Description, "Description");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.ColorName, "ColorName");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.Size, "Size");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.PCPK, "PCPK");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.CasePackQty, "CasePackQty");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.CreatedAt, "CreatedAt");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.CreatedById, "CreatedById");

                    // Perform the bulk insert within the transaction
                    bulkCopy.WriteToServer(dt);

                    // Add success result
                    response.Success = true;
                    response.Message = "Bulk insert completed successfully.";
                }
            }
            catch (SqlException sqlEx)
            {
                // Handle unique constraint violation
                if (sqlEx.Number == 2627 || sqlEx.Number == 2601)
                {
                    response.Success = false;
                    response.Message = "Some items already exist in the catalogue. Please check your file for duplicates.";
                }
                else
                {
                    response.Success = false;
                    response.Message = "A database error occurred while inserting items catalogue.";
                }
            }

            catch (Exception ex)
            {
                // Add failure result
                response.Success = false;
                response.Message = $"Error in BulkInsertToItemsCatalogue: {ex.Message}";
            }

            return response;
        }

        // Step 2: Map ItemCatalogueIds to InitialStock (after inserting ItemCatalogues)
        public Response<DataTable> MapItemCatalogueIds(DataTable dtItemCatalogues, DataTable dtInitialStock, SqlConnection conn, SqlTransaction transaction)
        {
            var response = new Response<DataTable>();

            try
            {
                // Create a dictionary to map "Casin" from dtItemCatalogues to ItemCatalogueId
                var itemCatalogueIdMap = new Dictionary<string, int>(); // Assuming "Casin" is unique in ItemCatalogues

                // Query to fetch ItemCatalogueId values from the database
                string query = "SELECT Casin, Id FROM dbo.ItemCatalogues";
                using (var cmd = new SqlCommand(query, conn, transaction))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string casin = reader["Casin"].ToString();
                            int itemCatalogueId = Convert.ToInt32(reader["Id"]);

                            // Add to the map
                            itemCatalogueIdMap[casin] = itemCatalogueId;
                        }
                    }
                }

                // Track missing mappings
                var missingCasins = new List<string>();

                // Now, update the dtInitialStock DataTable with the correct ItemCatalogueId
                foreach (DataRow row in dtInitialStock.Rows)
                {
                    string casin = row[AllColumnNames.CAsin].ToString();
                    if (itemCatalogueIdMap.ContainsKey(casin))
                    {
                        row[AllColumnNames.ItemCatalogueId] = itemCatalogueIdMap[casin]; // Set the correct ItemCatalogueId
                    }
                    else
                    {
                        missingCasins.Add(casin);
                    }
                }

                if (missingCasins.Any())
                {
                    response.Success = false;
                    response.Message = $"Failed to map ItemCatalogueId for the following Casin(s): {string.Join(", ", missingCasins)}";
                    response.Data = dtInitialStock;
                    return response;
                }

                response.Success = true;
                response.Message = "ItemCatalogueId mapping completed successfully.";
                response.Data = dtInitialStock;
                return response;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error occurred while mapping ItemCatalogueIds: {ex.Message}";
                response.Data = null;
                return response;
            }
        }

        // Step 3: Bulk Insert → InitialStock Table
        public Response<List<bool>> BulkInsertInitialStock(DataTable dt, SqlConnection conn, SqlTransaction transaction)
        {
            var response = new Response<List<bool>>();


            try
            {
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.KeepIdentity, transaction))
                {
                    bulkCopy.DestinationTableName = "dbo.InitialStocks";

                    // Column mappings (DataTable → DB)
                    bulkCopy.ColumnMappings.Add(AllColumnNames.ItemCatalogueId, "ItemCatalogueId");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.OpeningStock, "OpeningStock");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.CreatedAt, "CreatedAt");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.CreatedById, "CreatedById");

                    // Perform bulk insert
                    bulkCopy.WriteToServer(dt);

                    // Success
                    response.Success = true;
                    response.Message = "Bulk insert into InitialStocks completed successfully.";
                }
            }
            catch (SqlException sqlEx)
            {
                // Handle unique constraint violation
                if (sqlEx.Number == 2627 || sqlEx.Number == 2601)
                {
                    response.Success = false;
                    response.Message = "Some items already exist in the catalogue. Please check your file for duplicates.";
                }
                else
                {
                    response.Success = false;
                    response.Message = "A database error occurred while inserting items catalogue.";
                }
            }

            catch (Exception ex)
            {
                // Failure
                response.Data.Add(false);
                response.Success = false;
                response.Message = $"Error in BulkInsertInitialStock: {ex.Message}";
            }

            return response;
        }



        #endregion bulk insert

        #endregion ADD ITEMS + Opening Stock
        #endregion items
    }
}

#endregion new

#region old
//using OfficeOpenXml;
//using System;
//using System.Collections.Generic;
//using System.Configuration;
//using System.Data;
//using System.Data.SqlClient;
//using System.Data.SqlTypes;
//using System.Drawing;
//using System.Globalization;
//using System.IO;
//using System.Linq;
//using System.Runtime.Remoting.Messaging;
//using System.Runtime.Serialization;
//using System.Threading.Tasks;
//using System.Windows.Forms;
//using WIPAT.BLL;
//using WIPAT.DAL;
//using WIPAT.Entities;
//using WIPAT.Entities.Dto;
//using WIPAT.Entities.Enum;

//namespace WIPAT
//{
//    public partial class MainForm : Form
//    {
//        private Label[] steps;

//        private readonly Color StepDefaultBack = Color.FromArgb(0, 0, 64);
//        private readonly Color StepHoverBack = Color.FromArgb(20, 20, 100);
//        private readonly Color StepActiveBack = Color.DodgerBlue;

//        private readonly Color StepDefaultFore = Color.LightGray;
//        private readonly Color StepActiveFore = Color.White;

//        private Form activeForm = null;
//        private ItemsRepository itemsRepository;

//        private readonly WipSession _session = new WipSession();
//        private readonly ToolTip _stepperTip = new ToolTip();

//        // Track whether Step 2 (Calculate WIP) has finished successfully
//        private bool _step2Done = false;

//        // NEW: cache child forms so UI/data state persists across navigation
//        private readonly Dictionary<Type, Form> _forms = new Dictionary<Type, Form>();

//        public MainForm(User loggedInUser)
//        {
//            InitializeComponent();
//            itemsRepository = new ItemsRepository();

//            // If you add more steps later, extend this array and the switch below.
//            steps = new[] { step1, step2, step3 };

//            foreach (var step in steps)
//            {
//                step.Click += Step_Click;
//                step.MouseEnter += Step_MouseEnter;
//                step.MouseLeave += Step_MouseLeave;
//            }

//            stepperPanel.BackColor = StepDefaultBack;

//            _session.LoggedInUser = loggedInUser;
//            // Start at Step 1 (Upload)
//            SetActiveStep(1);
//            ShowForm(() => new UploadForm(_session, SetStatus));

//            // Initial gating (locks Step 2 & 3 until Step 1 is ready; Step 3 until Step 2 is done)
//            //UpdateStepperEnabledState();

//            // Ensure all cached children get disposed when app exits
//            this.FormClosing += MainForm_FormClosing;
//        }

//        #region Child form caching / navigation

//        private T GetOrCreate<T>(Func<T> factory) where T : Form
//        {
//            if (_forms.TryGetValue(typeof(T), out var existing) && !existing.IsDisposed)
//                return (T)existing;

//            var created = factory();
//            created.TopLevel = false;
//            created.FormBorderStyle = FormBorderStyle.None;
//            created.Dock = DockStyle.Fill;

//            _forms[typeof(T)] = created;
//            return created;
//        }

//        /// Show a cached child form
//        private void ShowForm<T>(Func<T> factory) where T : Form
//        {
//            var form = GetOrCreate(factory);

//            if (activeForm != null && !activeForm.IsDisposed)
//                activeForm.Hide(); // keep state

//            activeForm = form;

//            if (!mainPanel.Controls.Contains(form))
//                mainPanel.Controls.Add(form);

//            form.BringToFront();
//            form.Show();
//        }

//        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
//        {
//            // Dispose cached children on app exit to free resources
//            foreach (var kv in _forms.ToList())
//            {
//                if (kv.Value != null && !kv.Value.IsDisposed)
//                    kv.Value.Dispose();
//            }
//            _forms.Clear();
//        }

//        #endregion

//        #region Step UI Management

//        private void Step_Click(object sender, EventArgs e)
//        {
//            var clickedStep = sender as Label;
//            if (clickedStep == null) return;

//            int index = Array.IndexOf(steps, clickedStep) + 1;
//            statusLabel.Text = $"Step {index}: {clickedStep.Text}";

//            switch (index)
//            {
//                case 1:
//                    SetActiveStep(1);
//                    ShowForm(() => new UploadForm(_session, SetStatus));
//                    break;

//                case 2:
//                    if (!IsReadyForCalc(out var missingMsgFor2))
//                    {
//                        var currentFile = _session.ForecastFiles.Where(f => $"{f.ProjectionMonth} {f.ProjectionYear}" == _session.CurrentMonthWithYear).FirstOrDefault();
//                        if (currentFile != null && currentFile.IsWipAlreadyCalculated)
//                        {
//                            var MESSAGE = "WIP for the current file is already calculated. Modification is not allowed.";
//                            SetStatus(MESSAGE, StatusType.Warning);
//                            return;
//                        }



//                        SetStatus(string.IsNullOrWhiteSpace(missingMsgFor2) ? "Complete Step 1 first." : missingMsgFor2, StatusType.Warning);
//                        SetActiveStep(1);
//                        ShowForm(() => new UploadForm(_session, SetStatus));
//                        return;
//                    }

//                    SetActiveStep(2);
//                    ShowForm(() =>
//                    {
//                        var calc = new CalculateWIPForm(_session, SetStatus);
//                        // Only add once per instance
//                        calc.CalculationCompleted -= OnStep2Completed;
//                        calc.CalculationCompleted += OnStep2Completed;
//                        return calc;
//                    });
//                    break;

//                case 3:
//                    var step1Ready = IsReadyForCalc(out var missingMsgFor3);
//                    if (!step1Ready)
//                    {
//                        SetStatus(string.IsNullOrWhiteSpace(missingMsgFor3) ? "Complete Step 1 first." : missingMsgFor3, StatusType.Warning);
//                        SetActiveStep(1);
//                        ShowForm(() => new UploadForm(_session, SetStatus));
//                        return;
//                    }

//                    if (!_step2Done)
//                    {
//                        SetStatus("Complete Step 2 (Calculate WIP) before proceeding to Export.", StatusType.Warning);
//                        SetActiveStep(2);
//                        ShowForm(() =>
//                        {
//                            var calc2 = new CalculateWIPForm(_session, SetStatus);
//                            calc2.CalculationCompleted -= OnStep2Completed;
//                            calc2.CalculationCompleted += OnStep2Completed;
//                            return calc2;
//                        });
//                        return;
//                    }

//                    SetActiveStep(3);
//                    ShowForm(() => new ExportForm(_session, SetStatus));
//                    break;
//            }
//        }


//        private void Step_MouseEnter(object sender, EventArgs e)
//        {
//            var step = sender as Label;
//            if (step == null || !step.Enabled) return;
//            if (step.BackColor != StepActiveBack)
//                step.BackColor = StepHoverBack;
//        }

//        private void Step_MouseLeave(object sender, EventArgs e)
//        {
//            var step = sender as Label;
//            if (step == null || !step.Enabled) return;
//            if (step.BackColor != StepActiveBack)
//                step.BackColor = StepDefaultBack;
//        }

//        private void SetActiveStep(int stepNumber)
//        {
//            for (int i = 0; i < steps.Length; i++)
//            {
//                if (i == stepNumber - 1)
//                {
//                    steps[i].BackColor = StepActiveBack;
//                    steps[i].ForeColor = StepActiveFore;
//                }
//                else
//                {
//                    steps[i].BackColor = StepDefaultBack;
//                    steps[i].ForeColor = steps[i].Enabled ? StepDefaultFore : Color.Gray;
//                }
//            }
//        }

//        /// <summary>
//        /// Enable/disable steps based on readiness.
//        /// Step 2: only if Step 1 ready.
//        /// Step 3: only if Step 1 ready AND Step 2 done.
//        /// </summary>
//        public void UpdateStepperEnabledState()
//        {
//            bool step1Ready = IsReadyForCalc(out var missingMsg);
//            bool step2Ready = _step2Done;

//            if (!step1Ready)
//                //SetStatus(string.IsNullOrWhiteSpace(missingMsg) ? "Complete Step 1 first." : missingMsg, StatusType.Warning);

//                // Step 2 enabled only if Step 1 ready
//                step2.Enabled = step1Ready;
//            step2.Cursor = step1Ready ? Cursors.Hand : Cursors.No;
//            _stepperTip.SetToolTip(step2, step1Ready
//                ? "Proceed to Calculate WIP"
//                : "Complete Step 1 first (Forecast ×2, Stock, Orders, Prev, Curr, metadata...).");

//            // Step 3 enabled only if Step 1 ready AND Step 2 done
//            bool step3Enabled = step1Ready && step2Ready;
//            step3.Enabled = step3Enabled;
//            step3.Cursor = step3Enabled ? Cursors.Hand : Cursors.No;
//            _stepperTip.SetToolTip(step3, step3Enabled
//                ? "Proceed to Export"
//                : (step1Ready ? "Run Calculate WIP in Step 2 first." : "Complete Step 1 first."));

//            // Visually dim disabled steps (unless active)
//            if (!step1Ready && step2.BackColor != StepActiveBack)
//            {
//                step2.BackColor = StepDefaultBack;
//                step2.ForeColor = Color.Gray;
//            }
//            else if (step1Ready && step2.BackColor != StepActiveBack)
//            {
//                step2.ForeColor = StepDefaultFore;
//            }

//            if (!step3Enabled && step3.BackColor != StepActiveBack)
//            {
//                step3.BackColor = StepDefaultBack;
//                step3.ForeColor = Color.Gray;
//            }
//            else if (step3Enabled && step3.BackColor != StepActiveBack)
//            {
//                step3.ForeColor = StepDefaultFore;
//            }
//        }

//        /// <summary>
//        /// Minimal requirement to enter Step 2 (what you already defined).
//        /// </summary>
//        private bool IsReadyForCalc(out string message)
//        {
//            message = string.Empty;
//            var missing = new List<string>();

//            if (_session == null)
//            {
//                message = "Internal error: session is null.";
//                return false;
//            }

//            // Forecasts
//            if (_session.ForecastFiles == null || _session.ForecastFiles.Count == 0)
//                missing.Add("Forecast files (none loaded)");
//            else if (_session.ForecastFiles.Count < 2)
//                missing.Add($"At least 2 forecast files (currently {_session.ForecastFiles.Count})");

//            // Optional but useful (kept as a required check per your current logic)
//            //if (_session.AsinList == null || _session.AsinList.Count == 0)
//            //missing.Add("ASIN list (not extracted from forecasts)");

//            // Stock / Orders
//            //if (_session.Stock == null)
//            //    missing.Add("Stock file");
//            if (_session.Orders == null)
//                missing.Add("Order file");

//            // Forecast masters
//            //if (_session.Prev == null)
//            //missing.Add("Previous forecast (Prev)");
//            //if (_session.Curr == null)
//            //missing.Add("Current forecast (Curr)");

//            // Metadata / selections
//            //if (string.IsNullOrWhiteSpace(_session.WipType))
//            //    missing.Add("WIP Type (Analyst / Layman / LaymanFormula)");
//            //if (string.IsNullOrWhiteSpace(_session.TargetMonth))
//            //    missing.Add("Target month (e.g., \"August 2025\")");
//            //if (string.IsNullOrWhiteSpace(_session.CurrentMonth))
//            //    missing.Add("Current month (e.g., \"July 2025\")");
//            //if (string.IsNullOrWhiteSpace(_session.CurrentMonthWithYear))
//            //    missing.Add("Current month with year (e.g., \"July 2025\")");

//            // CommitmentPeriod (used during export filtering)
//            if (_session.CommitmentPeriod <= 0)
//                missing.Add("Commitment period");

//            if (missing.Count > 0)
//            {
//                message = "Please Provide Following:\n• " + string.Join("\n• ", missing);
//                return false;
//            }

//            message = string.Empty;
//            return true;
//        }

//        /// <summary>
//        /// Status bar helper (colors + text).
//        /// </summary>
//        public void SetStatus(string message, StatusType statusType)
//        {
//            if (InvokeRequired)
//            {
//                Invoke(new Action(() => SetStatus(message, statusType)));
//                return;
//            }

//            statusLabel.Text = message;

//            switch (statusType)
//            {
//                case StatusType.Success:
//                    statusLabel.BackColor = Color.Green;
//                    statusLabel.ForeColor = Color.White;
//                    break;
//                case StatusType.Error:
//                    statusLabel.BackColor = Color.Red;
//                    statusLabel.ForeColor = Color.White;
//                    break;
//                case StatusType.Reset:
//                    statusLabel.Text = string.Empty;
//                    statusLabel.BackColor = Color.Transparent;
//                    statusLabel.ForeColor = Color.Black;
//                    break;
//                case StatusType.Warning:
//                    statusLabel.BackColor = Color.Yellow;
//                    statusLabel.ForeColor = Color.Black;
//                    break;
//                case StatusType.Transparent:
//                    statusLabel.BackColor = Color.Transparent;
//                    statusLabel.ForeColor = Color.Black;
//                    break;
//            }
//        }

//        /// <summary>
//        /// Called by CalculateWIPForm when the calculation is finished.
//        /// Unlocks Step 3.
//        /// </summary>
//        private void OnStep2Completed()
//        {
//            _step2Done = true;
//            UpdateStepperEnabledState();
//            SetStatus("WIP calculation completed. You can proceed to Export.", StatusType.Success);
//        }

//        /// <summary>
//        /// If user changes Step 1 inputs (re-uploads, changes months, etc.), call this
//        /// from UploadForm to relock Step 3 until Step 2 is rerun.
//        /// </summary>
//        public void ResetStep2Progress()
//        {
//            _step2Done = false;
//            UpdateStepperEnabledState();
//        }

//        #endregion Step UI Management


//        #region items
//        #region Show Items Catalogue

//        private async void ItemsCatalogueMenuItem_Click(object sender, EventArgs e)
//        {
//            Response<List<ItemCatalogue>> resItemsCatalogue = await itemsRepository.GetItemCatalogues();
//            if (!resItemsCatalogue.Success)
//            {
//                MessageBox.Show("Failed to load item catalogue:\n" + resItemsCatalogue.Message,
//                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
//                return;
//            }

//            _session.ItemCatalogue = resItemsCatalogue.Data;

//            var filtered = resItemsCatalogue.Data
//         .Select(x => new
//         {
//             x.Casin,
//             x.Model,
//             x.Description,
//             x.ColorName,
//             x.Size,
//             x.PCPK,
//             x.CasePackQty
//         })
//         .ToList();



//            var form = new Form
//            {
//                Text = "Item Catalogue",
//                Width = 800,
//                Height = 500,
//                StartPosition = FormStartPosition.CenterParent
//            };

//            var dgv = new DataGridView
//            {
//                Dock = DockStyle.Fill,
//                ReadOnly = true,
//                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
//                AllowUserToOrderColumns = false,
//                DataSource = filtered
//            };

//            form.Controls.Add(dgv);
//            form.ShowDialog();
//        }

//        #endregion Show Items Catalogue

//        #region ADD ITEMS + Opening Stock
//        private void addItemsToCatalogueToolStripMenuItem_Click(object sender, EventArgs e)
//        {
//            var form = new Form
//            {
//                Text = "Import Item Catalogue",
//                Width = 800,
//                Height = 500,
//                StartPosition = FormStartPosition.CenterParent
//            };

//            var btnSelectFile = new Button
//            {
//                Text = "Select Excel File",
//                Dock = DockStyle.Top,
//                Height = 40
//            };

//            var dgv = new DataGridView
//            {
//                Dock = DockStyle.Fill,
//                ReadOnly = true,
//                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
//            };

//            var btnSave = new Button
//            {
//                Text = "Save to Database",
//                Dock = DockStyle.Bottom,
//                Height = 40,
//                Enabled = false
//            };

//            form.Controls.Add(dgv);
//            form.Controls.Add(btnSave);
//            form.Controls.Add(btnSelectFile);

//            // Access
//            DataTable catalogue = new DataTable();
//            DataTable stock = new DataTable();

//            // File selection + load
//            btnSelectFile.Click += async (s, ev) =>
//            {
//                try
//                {
//                    OpenFileDialog ofd = new OpenFileDialog
//                    {
//                        Filter = "Excel Files|*.xlsx;*.xls",
//                        Title = "Select Excel File"
//                    };

//                    if (ofd.ShowDialog() == DialogResult.OK)
//                    {
//                        string filePath = ofd.FileName;

//                        var resItemCatalogues = await ValidateAndGetCatalogueDataTable(filePath);
//                        if (!resItemCatalogues.Success)
//                        {
//                            MessageBox.Show($"Error: {resItemCatalogues.Message}", "Import Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
//                            return;
//                        }

//                        // Access
//                        catalogue = resItemCatalogues.Data[0];
//                        stock = resItemCatalogues.Data[1];


//                        dgv.DataSource = catalogue;
//                        btnSave.Enabled = true;
//                    }
//                }
//                catch (Exception ex)
//                {
//                    MessageBox.Show("Error: " + ex.Message, "Import Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
//                }
//            };

//            // Save to DB
//            btnSave.Click += (s, ev) =>
//            {
//                try
//                {

//                    if (catalogue == null && catalogue.Rows.Count <= 0)
//                    {
//                        MessageBox.Show("No data for ItemCatalogues to save.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                        return;
//                    }

//                    if (stock == null && stock.Rows.Count <= 0)
//                    {
//                        MessageBox.Show("No data for InitialStock to save.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
//                        return;
//                    }


//                    var resBulkInsert = BulkInsertToDatabase(catalogue, stock);
//                    if (!resBulkInsert.Success)
//                    {
//                        MessageBox.Show($"Error: {resBulkInsert.Message}", "Save Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);

//                    }
//                    else
//                    {
//                        MessageBox.Show($"{resBulkInsert.Message}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
//                    }

//                    form.Close();
//                }
//                catch (Exception ex)
//                {
//                    MessageBox.Show("Error: " + ex.Message, "Save Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
//                }
//            };

//            form.ShowDialog();
//        }


//        #region validate excel  
//        public async Task<Response<List<DataTable>>> ValidateAndGetCatalogueDataTable(string filePath)
//        {
//            var response = new Response<List<DataTable>>();
//            try
//            {

//                // Validate the Excel file
//                var validationResponse = await ValidateExcelFile(filePath);
//                if (!validationResponse.Success)
//                {
//                    response.Success = false;
//                    response.Message = validationResponse.Message;
//                    return response;
//                }

//                string workSheetName = validationResponse.Data;
//                // Get Item Catalogues DataTable
//                Response<DataTable> resItemCatalogues = await GetItemCataloguesDataTableFromExcel(filePath, workSheetName);
//                if (resItemCatalogues.Success == false)
//                {
//                    response.Success = false;
//                    response.Message = resItemCatalogues.Message;
//                    return response;
//                }

//                // Get Stock DataTable
//                Response<DataTable> resInitialStock = await GetStockDataTableFromExcel(filePath, workSheetName);
//                if (resInitialStock.Success == false)
//                {
//                    response.Success = false;
//                    response.Message = resInitialStock.Message;
//                    return response;
//                }

//                DataTable catalogueTable = resItemCatalogues.Data;
//                DataTable stockTable = resInitialStock.Data;

//                response.Data = new List<DataTable>();
//                response.Data.Add(catalogueTable);
//                response.Data.Add(stockTable);

//                response.Success = true;

//            }
//            catch (Exception ex)
//            {
//                // Handle exceptions and return a failure response with the error message
//                response.Success = false;
//                response.Message = $"An error occurred: {ex.Message}";
//            }

//            return response;
//        }

//        public async Task<Response<string>> ValidateExcelFile(string filePath)
//        {
//            var response = new Response<string>();
//            var requiredExcelColumns = AllColumnNames.ExcelColumnNames.ToList();
//            string requiredWorkSheetName = "ItemCatalogues";
//            var allowedExtensions = new[] { ".xls", ".xlsx" };

//            #region Input Validation
//            if (string.IsNullOrWhiteSpace(filePath))
//            {
//                response.Success = false;
//                response.Message = "File path is empty.";
//                return response;
//            }

//            if (!File.Exists(filePath))
//            {
//                response.Success = false;
//                response.Message = "The selected file does not exist.";
//                return response;
//            }

//            var fileExtension = Path.GetExtension(filePath).ToLower();
//            if (!allowedExtensions.Contains(fileExtension))
//            {
//                response.Success = false;
//                response.Message = "Invalid file type. Please select a valid Excel file (.xls or .xlsx).";
//                return response;
//            }
//            #endregion

//            try
//            {
//                // Open the Excel file using EPPlus asynchronously
//                var fileInfo = new FileInfo(filePath);

//                using (var package = await Task.Run(() => new ExcelPackage(fileInfo)))
//                {
//                    #region File Processing (Opening and Reading Excel File)

//                    if (package.Workbook.Worksheets.Count == 0)
//                    {
//                        response.Success = false;
//                        response.Message = "The workbook does not contain any worksheets.";
//                        return response;
//                    }

//                    var worksheet = package.Workbook.Worksheets[requiredWorkSheetName];

//                    // Check if worksheet exists
//                    if (worksheet == null || worksheet.Dimension == null)
//                    {
//                        response.Success = false;
//                        response.Message = $"Worksheet '{requiredWorkSheetName}' is missing or empty.";
//                        return response;
//                    }

//                    #endregion

//                    #region Header Validation
//                    // Get the header row (first row in the worksheet)
//                    var headerRow = Enumerable.Range(1, worksheet.Dimension.End.Column)
//                                              .Select(col => worksheet.Cells[1, col].Text)
//                                              .ToList();

//                    // Check which columns match or are missing
//                    var missingColumns = requiredExcelColumns.Except(headerRow).ToList();
//                    var extraColumns = headerRow.Except(requiredExcelColumns).ToList();

//                    if (missingColumns.Any() || extraColumns.Any())
//                    {
//                        string missingMessage = missingColumns.Any() ? $"Missing columns: {string.Join(", ", missingColumns)}." : string.Empty;
//                        string extraMessage = extraColumns.Any() ? $"Extra columns: {string.Join(", ", extraColumns)}." : string.Empty;

//                        response.Success = false;
//                        response.Message = $"{missingMessage} {extraMessage}".Trim();
//                    }

//                    #endregion

//                    #region Data Validation
//                    // Validate column data types
//                    bool dataTypesValid = true;

//                    for (int row = 2; row <= worksheet.Dimension.End.Row; row++) // Start from row 2 to skip the header
//                    {
//                        foreach (var column in AllColumnNames.ExcelColumnIndexes)
//                        {
//                            var columnName = column.Key;
//                            var columnIndex = column.Value;
//                            var cellValue = worksheet.Cells[row, columnIndex].Text;

//                            #region validation
//                            bool IsNumeric(string s) => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
//                            bool IsEmpty(string s) => string.IsNullOrWhiteSpace(s);

//                            if (columnName == AllColumnNames.CasePackQty) // optional numeric
//                            {
//                                if (!IsEmpty(cellValue) && !IsNumeric(cellValue))
//                                {
//                                    response.Success = false;
//                                    response.Message = $"Column '{columnName}' at row {row} must be numeric if provided. Found: '{cellValue}'.";
//                                    dataTypesValid = false;
//                                }
//                            }
//                            else if (columnName == AllColumnNames.PCPK || columnName == AllColumnNames.OpeningStock) // required numeric
//                            {
//                                if (IsEmpty(cellValue))
//                                {
//                                    response.Success = false;
//                                    response.Message = $"Column '{columnName}' at row {row} is required and cannot be empty.";
//                                    dataTypesValid = false;
//                                }
//                                else if (!IsNumeric(cellValue))
//                                {
//                                    response.Success = false;
//                                    response.Message = $"Column '{columnName}' at row {row} must be numeric. Found: '{cellValue}'.";
//                                    dataTypesValid = false;
//                                }
//                            }
//                            else if (columnName == AllColumnNames.CAsin || columnName == AllColumnNames.Model || columnName == AllColumnNames.Description || columnName == AllColumnNames.ColorName || columnName == AllColumnNames.Size) // required text
//                            {
//                                if (IsEmpty(cellValue))
//                                {
//                                    response.Success = false;
//                                    response.Message = $"Column '{columnName}' at row {row} is required and cannot be empty.";
//                                    dataTypesValid = false;
//                                }
//                                else if (IsNumeric(cellValue))
//                                {
//                                    response.Success = false;
//                                    response.Message = $"Column '{columnName}' at row {row} must be text, but a numeric value was found: '{cellValue}'.";
//                                    dataTypesValid = false;
//                                }
//                            }
//                            #endregion validation
//                        }

//                        if (!dataTypesValid)
//                            break;
//                    }
//                    #endregion

//                    #region Final Response
//                    if (dataTypesValid)
//                    {
//                        response.Success = true;
//                        response.Message = "Columns match the required ones, and the data types are correct.";
//                        response.Data = requiredWorkSheetName;
//                    }
//                    #endregion
//                }
//            }
//            catch (FileNotFoundException)
//            {
//                response.Success = false;
//                response.Message = "The file was not found.";
//            }
//            catch (UnauthorizedAccessException)
//            {
//                response.Success = false;
//                response.Message = "You do not have permission to access the file.";
//            }
//            catch (Exception ex)
//            {
//                response.Success = false;
//                response.Message = $"Cannot open file. It may be in use or corrupted. Error: {ex.Message}";
//            }

//            return response;
//        }


//        #region get datatable
//        //items catalogue 
//        private async Task<Response<DataTable>> GetItemCataloguesDataTableFromExcel(string filePath, string requiredWorkSheetName)
//        {
//            var response = new Response<DataTable>();
//            try
//            {
//                List<string> requiredCatalogueTableColumns = AllColumnNames.CatalogueTableColumns.ToList();

//                #region add columns to DataTable
//                DataTable dt = new DataTable();

//                // Loop through the column names and add columns to the DataTable with the correct types
//                foreach (var columnName in requiredCatalogueTableColumns)
//                {
//                    Type columnType = AllColumnNames.GetColumnType(columnName);
//                    dt.Columns.Add(columnName, columnType);
//                }
//                #endregion add columns to DataTable

//                // Open the Excel file using EPPlus asynchronously
//                var fileInfo = new FileInfo(filePath);

//                using (var package = await Task.Run(() => new ExcelPackage(fileInfo)))
//                {

//                    if (package.Workbook.Worksheets.Count == 0)
//                    {
//                        response.Success = false;
//                        response.Message = "The workbook does not contain any worksheets.";
//                        return response;
//                    }

//                    var worksheet = package.Workbook.Worksheets[requiredWorkSheetName];


//                    // Ensure worksheet is not disposed
//                    if (worksheet == null || worksheet.Dimension == null)
//                    {
//                        response.Success = false;
//                        response.Message = "Worksheet is not valid or has been disposed.";
//                        response.Data = null;
//                        return response;
//                    }

//                    int rowCount = worksheet.Dimension.Rows;

//                    // Assumes first row is header → start from row 2
//                    for (int row = 2; row <= rowCount; row++)
//                    {
//                        DataRow dr = dt.NewRow();

//                        // Iterate through required columns and map values dynamically using columnIndexMap
//                        foreach (var column in requiredCatalogueTableColumns)
//                        {

//                            if (column == AllColumnNames.CreatedAt)
//                            {
//                                dr[column] = DateTime.Now;
//                            }
//                            else if (column == AllColumnNames.CreatedById)
//                            {
//                                dr[column] = _session.LoggedInUser.Id;
//                            }
//                            else
//                            {
//                                string cellValue = worksheet.Cells[row, AllColumnNames.ExcelColumnIndexes[column]].Text;

//                                //map values
//                                var drRes = MapColumnValues(column, cellValue, dr, row);
//                                if (!drRes.Success)
//                                {
//                                    response.Success = drRes.Success;
//                                    response.Message = drRes.Message;
//                                    return response;
//                                }

//                                dr = drRes.Data;
//                            }

//                        }

//                        dt.Rows.Add(dr);
//                    }

//                    response.Success = true;
//                    response.Message = "Items Catalogue Data read successfully.";
//                    response.Data = dt;
//                    return response;
//                }
//            }
//            catch (Exception ex)
//            {
//                response.Success = false;
//                response.Message = $"Error reading Items Catalogue Data from Excel file: {ex.Message}";
//                response.Data = null;
//                return response;
//            }

//        }

//        //stock
//        public async Task<Response<DataTable>> GetStockDataTableFromExcel(string filePath, string requiredWorkSheetName)
//        {
//            var response = new Response<DataTable>();
//            try
//            {
//                List<string> requiredStockTableColumns = AllColumnNames.StockTableColumns.ToList();

//                #region add columns to DataTable
//                DataTable dt = new DataTable();

//                // Loop through the column names and add columns to the DataTable with the correct types
//                foreach (var columnName in requiredStockTableColumns)
//                {
//                    Type columnType = AllColumnNames.GetColumnType(columnName);
//                    dt.Columns.Add(columnName, columnType);
//                }
//                #endregion add columns to DataTable

//                // Open the Excel file using EPPlus asynchronously
//                var fileInfo = new FileInfo(filePath);

//                using (var package = await Task.Run(() => new ExcelPackage(fileInfo)))
//                {

//                    if (package.Workbook.Worksheets.Count == 0)
//                    {
//                        response.Success = false;
//                        response.Message = "The workbook does not contain any worksheets.";
//                        return response;
//                    }

//                    var worksheet = package.Workbook.Worksheets[requiredWorkSheetName];


//                    // Ensure worksheet is not disposed
//                    if (worksheet == null || worksheet.Dimension == null)
//                    {
//                        response.Success = false;
//                        response.Message = "Worksheet is not valid or has been disposed.";
//                        response.Data = null;
//                        return response;
//                    }

//                    int rowCount = worksheet.Dimension.Rows;

//                    // Assumes first row is header → start from row 2
//                    for (int row = 2; row <= rowCount; row++)
//                    {
//                        DataRow dr = dt.NewRow();

//                        // Iterate through the requiredColumns and map values dynamically using columnIndexMap
//                        foreach (var column in requiredStockTableColumns)
//                        {
//                            if (column == AllColumnNames.CreatedAt)
//                            {
//                                dr[column] = DateTime.Now;
//                            }
//                            else if (column == AllColumnNames.CreatedById)
//                            {
//                                dr[column] = _session.LoggedInUser.Id;
//                            }
//                            else if (column == AllColumnNames.ItemCatalogueId)
//                            {
//                                dr[column] = 0;
//                            }
//                            else
//                            {
//                                string cellValue = worksheet.Cells[row, AllColumnNames.ExcelColumnIndexes[column]].Text;

//                                //map values
//                                var drRes = MapColumnValues(column, cellValue, dr, row);
//                                if (!drRes.Success)
//                                {
//                                    response.Success = drRes.Success;
//                                    response.Message = drRes.Message;
//                                    return response;
//                                }

//                                dr = drRes.Data;
//                            }
//                        }

//                        dt.Rows.Add(dr);  // Add the row to the DataTable
//                    }

//                    response.Success = true;
//                    response.Message = "Initial stock data loaded successfully.";
//                    response.Data = dt;
//                }

//            }
//            catch (Exception ex)
//            {
//                response.Success = false;
//                response.Message = $"Error reading initial stock Excel file: {ex.Message}";
//                response.Data = null;
//            }

//            return response;
//        }

//        private Response<DataRow> MapColumnValues(string column, string cellValue, DataRow dr, int row)
//        {
//            var response = new Response<DataRow>();

//            try
//            {
//                bool IsNumeric(string s) => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
//                bool IsEmpty(string s) => string.IsNullOrWhiteSpace(s);

//                if (column == AllColumnNames.CasePackQty)
//                {
//                    if (!IsEmpty(cellValue) && !IsNumeric(cellValue))
//                    {
//                        var parseRes = TryParseIntegerColumn(column, cellValue, row, dr);
//                        if (!parseRes.Success)
//                        {
//                            response.Success = parseRes.Success;
//                            response.Message = parseRes.Message;
//                            return response;
//                        }
//                    }

//                }
//                else if (column == AllColumnNames.PCPK || column == AllColumnNames.OpeningStock)
//                {
//                    var parseRes = TryParseIntegerColumn(column, cellValue, row, dr);
//                    if (!parseRes.Success)
//                    {
//                        response.Success = parseRes.Success;
//                        response.Message = parseRes.Message;
//                        return response;
//                    }
//                }
//                else if (column == AllColumnNames.CreatedById)
//                {
//                    dr[column] = _session.LoggedInUser.Id;
//                }
//                else if (column == AllColumnNames.CreatedAt)
//                {
//                    dr[column] = DateTime.Now;
//                }
//                else if (column == AllColumnNames.CAsin)
//                {
//                    dr[column] = cellValue;
//                }
//                else if (column == AllColumnNames.ItemCatalogueId)
//                {
//                    dr[column] = 0;
//                }
//                else
//                {
//                    dr[column] = cellValue;
//                }

//                // If no exception occurs and all columns are mapped correctly, we set success to true
//                response.Success = true;
//                response.Message = "Column values mapped successfully.";
//                response.Data = dr;
//            }
//            catch (Exception ex)
//            {
//                response.Message = $"An error occurred while mapping column '{column}' for row {row}: {ex.Message}";
//                response.Success = false;
//            }

//            return response;
//        }


//        private Response<DataRow> TryParseIntegerColumn(string column, string cellValue, int row, DataRow dr)
//        {
//            var response = new Response<DataRow>();

//            try
//            {
//                if (int.TryParse(cellValue, out int parsedValue))
//                {
//                    dr[column] = parsedValue;
//                    response.Success = true;
//                    response.Message = "Success";
//                    response.Data = dr;
//                    return response;
//                }
//                else
//                {
//                    // Return a meaningful error response when parsing fails
//                    response.Success = false;
//                    response.Message = $"Invalid {column} value at row {row}. Could not parse '{cellValue}' as an integer.";
//                    response.Data = null;
//                    return response;
//                }
//            }
//            catch (Exception ex)
//            {
//                // Log the exception or handle it as needed
//                response.Success = false;
//                response.Message = $"An unexpected error occurred while processing the {column} value at row {row}: {ex.Message}";
//                response.Data = null;
//                return response;
//            }
//        }




//        #endregion get datatable


//        #endregion validate excel  


//        #region bulk insert
//        public Response<bool> BulkInsertToDatabase(DataTable dtItemCatalogues, DataTable dtInitialStock)
//        {
//            var response = new Response<bool>();

//            // Database connection string
//            string connectionString = ConfigurationManager.ConnectionStrings["dbContext"].ConnectionString;

//            using (var conn = new SqlConnection(connectionString))
//            {
//                conn.Open();
//                using (var transaction = conn.BeginTransaction())
//                {
//                    try
//                    {
//                        // Step 3: bulk insert to ItemCatalogues
//                        var resSaveCatalogue = BulkInsertToItemsCatalogue(dtItemCatalogues, conn, transaction);
//                        if (!resSaveCatalogue.Success)
//                        {
//                            transaction.Rollback();
//                            response.Success = false;
//                            response.Message = $"Failed to bulk insert ItemsCatalogue: {resSaveCatalogue.Message}";
//                            response.Data = false;
//                            return response;
//                        }

//                        // Step 4: Retrieve the generated ItemCatalogueId values and update InitialStock
//                        var resUpdateStockTable = MapItemCatalogueIds(dtItemCatalogues, dtInitialStock, conn, transaction);
//                        if (!resUpdateStockTable.Success)
//                        {
//                            transaction.Rollback();
//                            response.Success = false;
//                            response.Message = "Failed to Map ItemCatalogue Ids.";
//                            response.Data = false;
//                            return response;
//                        }

//                        // Step 5: bulk insert to InitialStock
//                        var resSaveStock = BulkInsertInitialStock(resUpdateStockTable.Data, conn, transaction);
//                        if (!resSaveStock.Success)
//                        {
//                            transaction.Rollback();
//                            response.Success = false;
//                            response.Message = $"Failed to bulk insert Stock: {resSaveStock.Message}";
//                            response.Data = false;
//                            return response;
//                        }

//                        // Commit if everything succeeds
//                        transaction.Commit();
//                        response.Success = true;
//                        response.Message = "Bulk insert completed successfully.";
//                        response.Data = true;
//                        return response;
//                    }
//                    catch (Exception ex)
//                    {
//                        transaction.Rollback();
//                        response.Success = false;
//                        response.Message = $"Bulk insert Failed: {ex.Message}";
//                        response.Data = false;
//                        return response;
//                    }
//                }
//            }
//        }


//        //// Step 1: Bulk Insert → Items Catalogue
//        public Response<bool> BulkInsertToItemsCatalogue(DataTable dt, SqlConnection conn, SqlTransaction transaction)
//        {
//            var response = new Response<bool>();


//            try
//            {
//                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.KeepIdentity, transaction))
//                {
//                    bulkCopy.DestinationTableName = "dbo.ItemCatalogues";

//                    // Column mappings (Excel → DB)
//                    bulkCopy.ColumnMappings.Add(AllColumnNames.CAsin, "Casin");
//                    bulkCopy.ColumnMappings.Add(AllColumnNames.Model, "Model");
//                    bulkCopy.ColumnMappings.Add(AllColumnNames.Description, "Description");
//                    bulkCopy.ColumnMappings.Add(AllColumnNames.ColorName, "ColorName");
//                    bulkCopy.ColumnMappings.Add(AllColumnNames.Size, "Size");
//                    bulkCopy.ColumnMappings.Add(AllColumnNames.PCPK, "PCPK");
//                    bulkCopy.ColumnMappings.Add(AllColumnNames.CasePackQty, "CasePackQty");
//                    bulkCopy.ColumnMappings.Add(AllColumnNames.CreatedAt, "CreatedAt");
//                    bulkCopy.ColumnMappings.Add(AllColumnNames.CreatedById, "CreatedById");

//                    // Perform the bulk insert within the transaction
//                    bulkCopy.WriteToServer(dt);

//                    // Add success result
//                    response.Success = true;
//                    response.Message = "Bulk insert completed successfully.";
//                }
//            }
//            catch (SqlException sqlEx)
//            {
//                // Handle unique constraint violation
//                if (sqlEx.Number == 2627 || sqlEx.Number == 2601)
//                {
//                    response.Success = false;
//                    response.Message = "Some items already exist in the catalogue. Please check your file for duplicates.";
//                }
//                else
//                {
//                    response.Success = false;
//                    response.Message = "A database error occurred while inserting items catalogue.";
//                }
//            }

//            catch (Exception ex)
//            {
//                // Add failure result
//                response.Success = false;
//                response.Message = $"Error in BulkInsertToItemsCatalogue: {ex.Message}";
//            }

//            return response;
//        }

//        // Step 2: Map ItemCatalogueIds to InitialStock (after inserting ItemCatalogues)
//        public Response<DataTable> MapItemCatalogueIds(DataTable dtItemCatalogues, DataTable dtInitialStock, SqlConnection conn, SqlTransaction transaction)
//        {
//            var response = new Response<DataTable>();

//            try
//            {
//                // Create a dictionary to map "Casin" from dtItemCatalogues to ItemCatalogueId
//                var itemCatalogueIdMap = new Dictionary<string, int>(); // Assuming "Casin" is unique in ItemCatalogues

//                // Query to fetch ItemCatalogueId values from the database
//                string query = "SELECT Casin, Id FROM dbo.ItemCatalogues";
//                using (var cmd = new SqlCommand(query, conn, transaction))
//                {
//                    using (var reader = cmd.ExecuteReader())
//                    {
//                        while (reader.Read())
//                        {
//                            string casin = reader["Casin"].ToString();
//                            int itemCatalogueId = Convert.ToInt32(reader["Id"]);

//                            // Add to the map
//                            itemCatalogueIdMap[casin] = itemCatalogueId;
//                        }
//                    }
//                }

//                // Track missing mappings
//                var missingCasins = new List<string>();

//                // Now, update the dtInitialStock DataTable with the correct ItemCatalogueId
//                foreach (DataRow row in dtInitialStock.Rows)
//                {
//                    string casin = row[AllColumnNames.CAsin].ToString();
//                    if (itemCatalogueIdMap.ContainsKey(casin))
//                    {
//                        row[AllColumnNames.ItemCatalogueId] = itemCatalogueIdMap[casin]; // Set the correct ItemCatalogueId
//                    }
//                    else
//                    {
//                        missingCasins.Add(casin);
//                    }
//                }

//                if (missingCasins.Any())
//                {
//                    response.Success = false;
//                    response.Message = $"Failed to map ItemCatalogueId for the following Casin(s): {string.Join(", ", missingCasins)}";
//                    response.Data = dtInitialStock;
//                    return response;
//                }

//                response.Success = true;
//                response.Message = "ItemCatalogueId mapping completed successfully.";
//                response.Data = dtInitialStock;
//                return response;
//            }
//            catch (Exception ex)
//            {
//                response.Success = false;
//                response.Message = $"Error occurred while mapping ItemCatalogueIds: {ex.Message}";
//                response.Data = null;
//                return response;
//            }
//        }

//        // Step 3: Bulk Insert → InitialStock Table
//        public Response<List<bool>> BulkInsertInitialStock(DataTable dt, SqlConnection conn, SqlTransaction transaction)
//        {
//            var response = new Response<List<bool>>();


//            try
//            {
//                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.KeepIdentity, transaction))
//                {
//                    bulkCopy.DestinationTableName = "dbo.InitialStocks";

//                    // Column mappings (DataTable → DB)
//                    bulkCopy.ColumnMappings.Add(AllColumnNames.ItemCatalogueId, "ItemCatalogueId");
//                    bulkCopy.ColumnMappings.Add(AllColumnNames.OpeningStock, "OpeningStock");
//                    bulkCopy.ColumnMappings.Add(AllColumnNames.CreatedAt, "CreatedAt");
//                    bulkCopy.ColumnMappings.Add(AllColumnNames.CreatedById, "CreatedById");

//                    // Perform bulk insert
//                    bulkCopy.WriteToServer(dt);

//                    // Success
//                    response.Success = true;
//                    response.Message = "Bulk insert into InitialStocks completed successfully.";
//                }
//            }
//            catch (SqlException sqlEx)
//            {
//                // Handle unique constraint violation
//                if (sqlEx.Number == 2627 || sqlEx.Number == 2601)
//                {
//                    response.Success = false;
//                    response.Message = "Some items already exist in the catalogue. Please check your file for duplicates.";
//                }
//                else
//                {
//                    response.Success = false;
//                    response.Message = "A database error occurred while inserting items catalogue.";
//                }
//            }

//            catch (Exception ex)
//            {
//                // Failure
//                response.Data.Add(false);
//                response.Success = false;
//                response.Message = $"Error in BulkInsertInitialStock: {ex.Message}";
//            }

//            return response;
//        }



//        #endregion bulk insert

//        #endregion ADD ITEMS + Opening Stock
//        #endregion items
//    }
//}

#endregion old


