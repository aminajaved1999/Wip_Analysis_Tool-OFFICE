using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WIPAT.BLL.Interfaces;
using WIPAT.BLL.Services;
using WIPAT.DAL.Interfaces;
using WIPAT.Entities;
using WIPAT.Entities.Dto;
using WIPAT.Entities.Enum;
using WIPAT.Entities.ExcelTemplateDefinitions;
using WIPAT.Helpers;

namespace WIPAT
{
    public partial class ItemsCatalogueForm : Form
    {
        #region Fields & Enums
        private readonly WipSession _session;
        private readonly ExcelValidationService excelValidationService;
        private readonly IItemsRepository _itemsRepository;
        private readonly IStockRepository _stockRepository;

        private enum AppState { Idle, ViewingDB, PreviewingExcelAdd, PreviewingExcelUpdate }
        private AppState _currentState = AppState.Idle;

        private List<ItemCatalogueDto> _allItems = new List<ItemCatalogueDto>();
        private BindingSource _bindingSource = new BindingSource();

        private DataTable _pendingCatalogueTable;
        private DataTable _pendingStockTable;
        #endregion

        #region Constructor & Initialization
        public ItemsCatalogueForm(
        WipSession session,
        IItemsRepository itemsRepository,
        IStockRepository stockRepository)
        {
            InitializeComponent();

            // Required for EPPlus operations within this form
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

            ApplyTheme();

            _session = session;
            _itemsRepository = itemsRepository;
            _stockRepository = stockRepository;
            excelValidationService = new ExcelValidationService(_session, _itemsRepository);

            UpdateUIState();
        }
        #endregion

        #region UI & Theme Setup
        private void ApplyTheme()
        {
            UITheme.SetFormIcon(this);
            this.BackColor = UITheme.BackgroundCanvas;

            headerPanel.BackColor = UITheme.MainColor;
            lblTitle.ForeColor = UITheme.SurfaceWhite;

            UITheme.StyleGrid(dgvItems);

            UITheme.StyleButton(btnLoadDb, AppButtonStyle.LoadDbData);
            UITheme.StyleButton(btnPreviewExcelAdd, AppButtonStyle.PreviewAdd);
            UITheme.StyleButton(btnPreviewExcelUpdate, AppButtonStyle.PreviewUpdate);
            UITheme.StyleButton(btnExport, AppButtonStyle.ExportToExcel);
            UITheme.StyleButton(btnCommitToDb, AppButtonStyle.ApproveAndSave);
            UITheme.StyleButton(btnSearch, AppButtonStyle.Search);
        }

        private void UpdateUIState()
        {
            switch (_currentState)
            {
                case AppState.Idle:
                    btnLoadDb.Visible = true;
                    btnLoadDb.Text = "View Catalog";
                    btnPreviewExcelAdd.Visible = true;
                    btnPreviewExcelUpdate.Visible = true;
                    btnExport.Visible = false;
                    btnCommitToDb.Visible = false;
                    pnlSearch.Visible = false;

                    if (pnlStatusBar != null) pnlStatusBar.Visible = false;

                    lblTitle.Text = "Items Catalog Manager";
                    lblModeIndicator.Visible = false;
                    break;

                case AppState.ViewingDB:
                    btnLoadDb.Visible = true;
                    btnLoadDb.Text = "Refresh Catalog";
                    btnPreviewExcelAdd.Visible = true;
                    btnPreviewExcelUpdate.Visible = true;
                    btnExport.Visible = true;
                    pnlSearch.Visible = true;
                    btnCommitToDb.Visible = false;

                    if (pnlStatusBar != null) pnlStatusBar.Visible = true;

                    lblTitle.Text = "System Catalog";

                    lblModeIndicator.BackColor = UITheme.Success_Color;
                    lblModeIndicator.Text = "LIVE CATALOG: You are viewing current system records.";
                    lblModeIndicator.Visible = true;
                    break;

                case AppState.PreviewingExcelAdd:
                case AppState.PreviewingExcelUpdate:
                    btnCommitToDb.Visible = true;
                    btnCommitToDb.Text = "Save Changes";
                    btnLoadDb.Visible = true;
                    btnLoadDb.Text = "Cancel Import";

                    btnPreviewExcelAdd.Visible = false;
                    btnPreviewExcelUpdate.Visible = false;
                    btnExport.Visible = false;
                    pnlSearch.Visible = false;

                    if (pnlStatusBar != null) pnlStatusBar.Visible = true;

                    var actionText = _currentState == AppState.PreviewingExcelAdd ? "New Items" : "Updates";

                    lblTitle.Text = $"Import Preview ({actionText})";

                    lblModeIndicator.BackColor = UITheme.WarningColor;
                    lblModeIndicator.Text = $"PREVIEW MODE: Reviewing imported items. Click 'Save Changes' to apply.";
                    lblModeIndicator.Visible = true;
                    break;
            }
        }

        // Helper to prevent concurrent user actions
        private void SetUIEnabled(bool isEnabled)
        {
            flowLayoutPanelActions.Enabled = isEnabled;
            pnlSearch.Enabled = isEnabled;
            dgvItems.Enabled = isEnabled;
        }
        #endregion

        #region Data Loading & Binding
        private async Task LoadDataAsync()
        {
            txtSearch.Clear();

            if (_currentState == AppState.PreviewingExcelAdd || _currentState == AppState.PreviewingExcelUpdate)
            {
                _pendingCatalogueTable = null;
                _pendingStockTable = null;
                _currentState = AppState.Idle;
            }

            SetStatus("Loading catalog...", StatusType.Warning);

            try
            {
                Response<List<ItemCatalogueDto>> res = await _itemsRepository.GetItemCataloguesWithStock();
                if (!res.Success)
                {
                    SetStatus("Failed to load catalog.", StatusType.Error);
                    MessageBox.Show("Failed to load item catalog:\n" + res.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                _allItems = res.Data;
                _currentState = AppState.ViewingDB;
                BindGrid(_allItems);
                UpdateUIState();
                SetStatus("Catalog loaded successfully.", StatusType.Success);
            }
            catch (Exception ex)
            {
                string errorMsg = $"An unexpected error occurred while loading the catalog: {ex.Message}"
                                  + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                                  + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "");

                SetStatus("Failed to load catalog.", StatusType.Error);
                MessageBox.Show(errorMsg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BindGrid(IEnumerable<ItemCatalogueDto> data)
        {
            DataTable table = new DataTable();

            // 1. Fetch dynamic template definitions
            var exportTemplate = FileTemplateFactory.GetExportTemplate(ExportExcelFileType.ExportItemsCatalogue);

            // 2. Build columns dynamically based on the master catalogue
            foreach (var rule in exportTemplate)
            {
                Type dotNetType = GetDotNetType(rule.Definition.DataType);
                table.Columns.Add(rule.Definition.Name, dotNetType);
            }

            // 3. Populate rows matching the template layout
            foreach (var item in data)
            {
                DataRow row = table.NewRow();
                foreach (var rule in exportTemplate)
                {
                    string colName = rule.Definition.Name;
                    row[colName] = GetColumnValue(item, colName) ?? DBNull.Value;
                }
                table.Rows.Add(row);
            }

            _bindingSource.DataSource = table;
            dgvItems.DataSource = _bindingSource;

            UpdateDisplayCounts();
        }

        // Helper mapping: Convert System Vocabulary Enum to .NET Type
        private Type GetDotNetType(ExcelDataType dataType)
        {
            switch (dataType)
            {
                case ExcelDataType.Int: return typeof(int);
                case ExcelDataType.Decimal: return typeof(decimal);
                case ExcelDataType.DateTime: return typeof(DateTime);
                case ExcelDataType.Boolean: return typeof(bool);
                case ExcelDataType.String:
                default: return typeof(string);
            }
        }

        // Helper mapping: Extract property values dynamically
        private object GetColumnValue(ItemCatalogueDto item, string columnName)
        {
            switch (columnName)
            {
                case "CASIN": return item.Casin;
                case "Model": return item.Model;
                case "Description": return item.Description;
                case "ColorName": return item.ColorName;
                case "Size": return item.Size;
                case "PC/PK": return item.PCPK;
                case "CasePackQty": return item.CasePackQty;
                case "OpeningStock": return item.OpeningStock;
                case "Notes": return item.Notes;
                case "ItemStatus": return item.ItemStatus.ToString(); // Converted to string per the Master Catalogue definition
                default: return null;
            }
        }

        private void UpdateDisplayCounts()
        {
            if (_bindingSource == null || _bindingSource.List.Count == 0)
            {
                lblTotalItems.Text = "Total: 0";
                lblActiveItems.Text = "Active: 0";
                lblInactiveItems.Text = "Inactive: 0";
                lblInvalidItems.Text = "Invalid: 0";
                return;
            }

            int total = 0, active = 0, inactive = 0, invalid = 0;

            foreach (DataRowView rowView in _bindingSource.List)
            {
                total++;
                if (int.TryParse(rowView["ItemStatus"].ToString(), out int status))
                {
                    if (status == 1) active++;
                    else if (status == 0) inactive++;
                    else if (status == 2) invalid++;
                }
            }

            lblTotalItems.Text = $"Total: {total}";
            lblActiveItems.Text = $"Active: {active}";
            lblInactiveItems.Text = $"Inactive: {inactive}";
            lblInvalidItems.Text = $"Invalid: {invalid}";
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
                    statusLabel.ForeColor = UITheme.SurfaceWhite;
                    break;
                case StatusType.Error:
                    statusLabel.BackColor = UITheme.GridErrorHeader;
                    statusLabel.ForeColor = UITheme.SurfaceWhite;
                    break;
                case StatusType.Warning:
                    statusLabel.BackColor = UITheme.WarningColor;
                    statusLabel.ForeColor = UITheme.GridRowText;
                    break;
                default:
                    statusLabel.BackColor = Color.Transparent;
                    statusLabel.ForeColor = UITheme.TextSecondaryColor;
                    break;
            }
            statusStrip.Refresh();
        }

        private void ShowErrors(List<string> errors)
        {
            var validErrors = errors?.Where(e => !string.IsNullOrWhiteSpace(e)).ToList() ?? new List<string>();

            if (validErrors.Count == 0) return;

            int displayLimit = 15;
            var errorsToDisplay = validErrors.Take(displayLimit).ToList();

            string errorMessage = string.Join(Environment.NewLine, errorsToDisplay);

            if (validErrors.Count > displayLimit)
            {
                errorMessage += $"{Environment.NewLine}{Environment.NewLine}... and {validErrors.Count - displayLimit} more error(s).";
            }

            MessageBox.Show(errorMessage, "Validation Errors", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        #endregion

        #region Event Handlers
        private async void BtnLoadDb_Click(object sender, EventArgs e)
        {
            try
            {
                SetUIEnabled(false);
                await LoadDataAsync();
            }
            finally
            {
                SetUIEnabled(true);
            }
        }

        private async void BtnPreviewExcelAdd_Click(object sender, EventArgs e)
        {
            try
            {
                SetUIEnabled(false);
                await LoadExcelForPreviewAsync(AppState.PreviewingExcelAdd);
            }
            finally
            {
                SetUIEnabled(true);
            }
        }

        private async void BtnPreviewExcelUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                SetUIEnabled(false);
                await LoadExcelForPreviewAsync(AppState.PreviewingExcelUpdate);
            }
            finally
            {
                SetUIEnabled(true);
            }
        }

        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                BtnSearch_Click(sender, e);
            }
        }

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            if (_currentState != AppState.ViewingDB || _bindingSource.DataSource == null) return;

            string keyword = txtSearch.Text.Trim().Replace("'", "''");

            if (string.IsNullOrEmpty(keyword))
            {
                _bindingSource.RemoveFilter();
                SetStatus("Filter cleared.", StatusType.Success);
            }
            else
            {
                _bindingSource.Filter = $"CASIN LIKE '%{keyword}%'";
                SetStatus($"Applied search filter.", StatusType.Success);
            }

            UpdateDisplayCounts();
        }

        private async void BtnExport_Click(object sender, EventArgs e)
        {
            if (dgvItems.Rows.Count == 0)
            {
                SetStatus("No data available to export.", StatusType.Warning);
                MessageBox.Show("There is no data in the grid to export.", "Empty Grid", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                SetUIEnabled(false);

                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "Excel Files (*.xlsx)|*.xlsx";
                    sfd.FileName = $"ItemCatalog_{DateTime.Now:yyyyMMdd}.xlsx";
                    sfd.Title = "Save Excel Export";

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        SetStatus("Exporting data to Excel...", StatusType.Warning);

                        Response<string> response = await Task.Run(() => excelValidationService.ExportGridToExcel(dgvItems, sfd.FileName, "Items"));

                        if (response.Success)
                        {
                            SetStatus("Catalog exported successfully.", StatusType.Success);
                            MessageBox.Show("Data successfully exported to Excel.", "Export Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            SetStatus("Export failed.", StatusType.Error);
                            MessageBox.Show($"Failed to export data:\n\n{response.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"An unexpected error occurred while exporting data to Excel: {ex.Message}"
                                  + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                                  + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "");

                SetStatus("Export failed due to a critical error.", StatusType.Error);
                MessageBox.Show(errorMsg, "Export Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetUIEnabled(true);
            }
        }

        private async Task LoadExcelForPreviewAsync(AppState previewState)
        {
            try
            {
                using (OpenFileDialog ofd = new OpenFileDialog { Filter = "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls", Title = "Select Excel File to Validate" })
                {
                    if (ofd.ShowDialog() != DialogResult.OK) return;

                    SetStatus("Validating and reading Excel file...", StatusType.Warning);

                    ImportExcelFileType selectedType = (previewState == AppState.PreviewingExcelUpdate)
                        ? ImportExcelFileType.UpdateExistingCatalogue
                        : ImportExcelFileType.AddNewItemsToCatalogue;

                    var requiredExcelColumns = FileTemplateFactory.GetImportTemplate(selectedType);
                    string requiredWorkSheetName = ConfigurationManager.AppSettings["ItemCatalogueWorksheetName"] ?? "ItemCatalogues";

                    // Instantiate a fresh service strictly for this upload. 
                    var freshValidationService = new ExcelValidationService(_session, _itemsRepository);

                    var response = await freshValidationService.ValidateAndLoadExcelAsync(
                        ofd.FileName,
                        selectedType,
                        requiredWorkSheetName,
                        requiredExcelColumns
                    );

                    if (!response.Success)
                    {
                        var errorsToDisplay = response.MissingItems != null && response.MissingItems.Count > 0
                                            ? response.MissingItems
                                            : (!string.IsNullOrWhiteSpace(response.Message) ? new List<string> { response.Message } : new List<string>());

                        ShowErrors(errorsToDisplay);
                        SetStatus("Validation Failed.", StatusType.Error);
                        return;
                    }

                    if (response.Data?.ValidatedData == null || response.Data.ValidatedData.Rows.Count == 0)
                    {
                        SetStatus("No data found in Excel file.", StatusType.Warning);
                        MessageBox.Show("The selected Excel sheet appears to be empty or contains no valid rows.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    _pendingCatalogueTable = response.Data.ValidatedData;
                    _pendingStockTable = response.Data.ValidatedData;

                    dgvItems.DataSource = null;
                    dgvItems.DataSource = _pendingCatalogueTable;

                    _currentState = previewState;
                    UpdateUIState();
                    SetStatus("Preview ready. Click 'Save Changes' to complete the import.", StatusType.Success);
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"An unexpected error occurred while reading the Excel file: {ex.Message}"
                                  + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                                  + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "");

                SetStatus("An unexpected error occurred while reading the file.", StatusType.Error);
                MessageBox.Show(errorMsg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnCommitToDb_Click(object sender, EventArgs e)
        {
            var actionText = _currentState == AppState.PreviewingExcelAdd ? "add" : "update";
            var confirm = MessageBox.Show(
                $"Are you sure you want to {actionText} {_pendingCatalogueTable.Rows.Count} items in the system?",
                "Confirm Save", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                SetUIEnabled(false);
                SetStatus("Saving changes...", StatusType.Warning);

                Response<bool> resBulk;
                if (_currentState == AppState.PreviewingExcelAdd)
                {
                    resBulk = _stockRepository.BulkInsertCatalogueImport(_pendingCatalogueTable, _pendingStockTable);
                }
                else
                {
                    resBulk = _itemsRepository.BulkUpdateCatalogueAndStatusImport(_pendingCatalogueTable, _pendingStockTable);
                }

                if (!resBulk.Success)
                {
                    SetStatus("Save failed.", StatusType.Error);
                    MessageBox.Show($"Error saving records to the system:\n{resBulk.Message}", "Save Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show($"Successfully applied {actionText}s to the catalog.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _pendingCatalogueTable = null;
                    _pendingStockTable = null;
                    await LoadDataAsync();
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"An unexpected error occurred while saving records to the system: {ex.Message}"
                                  + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                                  + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "");

                SetStatus("Save failed.", StatusType.Error);
                MessageBox.Show(errorMsg, "Save Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetUIEnabled(true);
            }
        }
        #endregion
    }
}