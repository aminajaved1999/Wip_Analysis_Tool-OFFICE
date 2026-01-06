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
using WIPAT.DAL;
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
        #endregion UI Colors and Step Management


        #region Fields and Session Management
        //steps
        public enum Step { Upload = 1, Calculate = 2, Export = 3 }
        private bool _calcDone;
        private bool IsUploadDone = false;  // Track if upload is done
        private bool IsCalculateDone = false;  // Track if calculation is done
        private Label[] steps;
        private Form activeForm = null;

        //session
        private readonly WipSession _session = new WipSession();
        private readonly System.Windows.Forms.ToolTip _stepperTip = new System.Windows.Forms.ToolTip();

        //
        private ItemsRepository itemsRepository;
        private WipRepository wipRepository;

        // Declarative routing and tiny cache keyed by Step
        private readonly Dictionary<Step, Func<Form>> _routes = new Dictionary<Step, Func<Form>>();
        private readonly Dictionary<Step, Form> _cache = new Dictionary<Step, Form>();
        #endregion Fields and Session Management
        #endregion start

        #region Constructor
        public MainForm(User loggedInUser)
        {
            InitializeComponent();

            // Repositories / session
            itemsRepository = new ItemsRepository();
            wipRepository = new WipRepository();
            _session.LoggedInUser = loggedInUser ?? throw new ArgumentNullException(nameof(loggedInUser));

            // Stepper wires
            steps = new[] { step1, step2, step3 };
            HookStepper();

            // Base styling
            stepperPanel.BackColor = StepDefaultBack;

            // Declarative routing → no switch forest
            _routes[Step.Upload] = () => new NewUploadForm(_session, SetStatus);
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
        #endregion status 

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
                    //if (!IsEmpty(cellValue) && !IsNumeric(cellValue))
                    if (!IsEmpty(cellValue) )
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

                // Ensure required column 'OrderQty' exists in DataTable
                    dt.Columns.Add("OrderQty", typeof(int));
                    dt.Columns.Add("ProductionQty", typeof(int));

                // Deliberately set OrderQty = 0 for all rows
                foreach (DataRow row in dt.Rows)
                {
                    row["OrderQty"] = 0;
                    row["ProductionQty"] = 0;
                }

                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.KeepIdentity, transaction))
                {
                    bulkCopy.DestinationTableName = "dbo.InitialStocks";

                    // Column mappings (DataTable → DB)
                    bulkCopy.ColumnMappings.Add(AllColumnNames.ItemCatalogueId, "ItemCatalogueId");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.OpeningStock, "OpeningStock");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.CreatedAt, "CreatedAt");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.CreatedById, "CreatedById");

                    // Add missing column mapping for OrderQty
                    bulkCopy.ColumnMappings.Add("OrderQty", "OrderQty");
                    bulkCopy.ColumnMappings.Add("ProductionQty", "ProductionQty");


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

        #region calculated Wip

        private void calculatedWipsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Clear previous dropdown items to avoid duplicates
            calculatedWipsToolStripMenuItem.DropDownItems.Clear();

            try
            {

                #region Load Calculated WIPs 
                // Get the list of forecasts that have calculated WIPs
                var forecastsWithCalculatedWipResponse = wipRepository.GetForecastsWithCalculatedWip();
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
                                Response<List<WipDetail>> detailsResponse = await wipRepository.GetWipDetailsByPeriodAsync(forecast.Month, forecast.Year);

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
                                                ExportToExcel_EPPlus(selectedData, fileName, worksheetName);
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

        private void ExportToExcel_EPPlus<T>(List<T> data, string fileName, string worksheetName)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    FileName = fileName,
                    Filter = "Excel Files (*.xlsx)|*.xlsx",
                    Title = "Save WIP Data to Excel"
                };

                if (saveFileDialog.ShowDialog() != DialogResult.OK) return;

                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add(worksheetName);

                    if (!data.Any())
                    {
                        MessageBox.Show("No data to export.", "Export Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Use reflection to get property names
                    var props = typeof(T).GetProperties();

                    // Headers
                    for (int i = 0; i < props.Length; i++)
                        worksheet.Cells[1, i + 1].Value = props[i].Name;

                    // Data
                    for (int row = 0; row < data.Count; row++)
                    {
                        for (int col = 0; col < props.Length; col++)
                        {
                            worksheet.Cells[row + 2, col + 1].Value = props[col].GetValue(data[row]);
                        }
                    }

                    // Style header
                    using (var range = worksheet.Cells[1, 1, 1, props.Length])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                        range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    }

                    if (worksheet.Dimension != null)
                        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    package.SaveAs(new FileInfo(saveFileDialog.FileName));
                }

                MessageBox.Show($"WIP data successfully exported to:\n{saveFileDialog.FileName}",
                                "Export Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while exporting to Excel:\n{ex.Message}",
                                "Export Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        #endregion calculated Wip

        #region edit - calculated wip

        private void editCalculatedWipsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Reset dropdown 
            editCalculatedWipsToolStripMenuItem.DropDownItems.Clear();

            try
            {
                #region FETCH: Forecast periods with calculated WIP
                var forecastsWithCalculatedWipResponse = wipRepository.GetForecastsWithCalculatedWip();
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
                                var parseResult = ParseWipExcel(ofd.FileName);

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
                                var updateResponse = await wipRepository.AddUserWipQtyForPeriodAsync(periodMonth, periodYear, updates);
                                #endregion

                                #region Show result and refresh WIP details
                                if (updateResponse.Success)
                                {
                                    // Re-fetch the updated WIP details and display them
                                    var refreshedDetails = await wipRepository.GetWipDetailsByPeriodAsync(periodMonth, periodYear);
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

        private Response<List<WipDetail>> ParseWipExcel(string filePath)
        {
            var response = new Response<List<WipDetail>>();

            try
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    response.Success = false;
                    response.Message = "File not found.";
                    return response;
                }

                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    // Check for the "Wip" sheet name
                    var ws = package.Workbook.Worksheets.FirstOrDefault(w => w.Name.Equals("Wip", StringComparison.OrdinalIgnoreCase));
                    if (ws == null || ws.Dimension == null)
                        return new Response<List<WipDetail>> { Success = false, Message = "No worksheet named 'Wip' or no data found." };

                    // Locate the exact columns by header text
                    int FindCol(string headerName)
                    {
                        for (int col = 1; col <= ws.Dimension.End.Column; col++)
                        {
                            var header = ws.Cells[1, col].Text?.Trim();
                            if (string.Equals(header, headerName, StringComparison.OrdinalIgnoreCase))
                                return col;
                        }
                        return -1;  // Return -1 if not found
                    }

                    // Look for the exact headers for CASIN and WipQuantity
                    var casinCol = FindCol("CASIN");
                    var wipCol = FindCol("WipQuantity");

                    if (casinCol == -1 || wipCol == -1)
                        return new Response<List<WipDetail>> { Success = false, Message = "Required columns not found. Expecting headers: 'CASIN' and 'WipQuantity'." };

                    var list = new List<WipDetail>();
                    var errors = new List<string>();
                    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    for (int r = 2; r <= ws.Dimension.End.Row; r++)
                    {
                        var casin = ws.Cells[r, casinCol].Text?.Trim();
                        var wipStr = ws.Cells[r, wipCol].Text?.Trim();

                        if (string.IsNullOrWhiteSpace(casin))
                        {
                            // Skip blank/empty rows
                            continue;
                        }

                        // Parse quantity using invariant first, then current culture as fallback
                        if (!int.TryParse(wipStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var qty) &&
                            !int.TryParse(wipStr, NumberStyles.Any, CultureInfo.CurrentCulture, out qty))
                        {
                            errors.Add($"Row {r}: invalid WipQuantity '{wipStr}' at CASIN '{casin}'.");
                            continue;
                        }

                        if (qty < 0)
                        {
                            errors.Add($"Row {r}: negative WipQuantity '{qty}' at CASIN '{casin}' not allowed.");
                            continue;
                        }

                        // If duplicate CASIN appears, keep the last occurrence
                        if (seen.Contains(casin))
                        {
                            var idx = list.FindIndex(x => string.Equals(x.CASIN, casin, StringComparison.OrdinalIgnoreCase));
                            list[idx] = new WipDetail { CASIN = casin, UserWipQty = qty };
                        }
                        else
                        {
                            list.Add(new WipDetail { CASIN = casin, UserWipQty = qty });
                            seen.Add(casin);
                        }
                    }

                    if (errors.Any())
                    {
                        var message = string.Join("\n", errors.Take(25)) +
                            (errors.Count > 25 ? $"\n...and {errors.Count - 25} more." : "");
                        return new Response<List<WipDetail>> { Success = false, Message = message };
                    }

                    return new Response<List<WipDetail>> { Success = true, Data = list };
                }
            }
            catch (Exception ex)
            {
                return new Response<List<WipDetail>> { Success = false, Message = $"Failed to read Excel: {ex.Message}" };
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

                        var resp = await wipRepository.UpdateWipForPeriodAsync(month, year, updates);
                        if (resp.Success)
                        {
                            MessageBox.Show(string.IsNullOrWhiteSpace(resp.Message)
                                ? "Approval applied successfully."
                                : resp.Message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            // Refresh the grid after approval
                            var refreshed = await wipRepository.GetWipDetailsByPeriodAsync(month, year);
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

        #endregion edit - calculated wip


    }
}

