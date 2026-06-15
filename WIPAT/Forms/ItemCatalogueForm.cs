using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
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
    public partial class ItemsCatalogueForm : Form
    {
        #region Fields & Enums
        private readonly IItemsRepository _itemsRepository;
        private readonly IStockRepository _stockRepository;
        private readonly IExcelService _excelService;

        private enum AppState { Idle, ViewingDB, PreviewingExcelAdd, PreviewingExcelUpdate }
        private AppState _currentState = AppState.Idle;

        private List<ItemCatalogueDto> _allItems = new List<ItemCatalogueDto>();
        private List<ItemCatalogueDto> _currentDisplayList = new List<ItemCatalogueDto>();

        private DataTable _pendingCatalogueTable;
        private DataTable _pendingStockTable;

        private string _sortColumn = string.Empty;
        private bool _sortAscending = true;
        #endregion

        #region Constructor & Initialization
        public ItemsCatalogueForm(
            IItemsRepository itemsRepository,
            IStockRepository stockRepository,
            IExcelService excelService)
        {
            InitializeComponent();
            ApplyTheme();

            _itemsRepository = itemsRepository;
            _stockRepository = stockRepository;
            _excelService = excelService;

            btnLoadDb.Click += async (s, e) => await LoadDataAsync();
            btnSearch.Click += BtnSearch_Click;
            btnPreviewExcelAdd.Click += async (s, e) => await LoadExcelForPreviewAsync(AppState.PreviewingExcelAdd);
            btnPreviewExcelUpdate.Click += async (s, e) => await LoadExcelForPreviewAsync(AppState.PreviewingExcelUpdate);
            btnExport.Click += BtnExport_Click;
            btnCommitToDb.Click += async (s, e) => await BtnCommitToDb_Click(s, e);
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) BtnSearch_Click(s, e); };
            dgvItems.ColumnHeaderMouseClick += DgvItems_ColumnHeaderMouseClick;

            // Wire up the new global UITheme method to handle counts automatically
            dgvItems.DataBindingComplete += (s, e) =>
                UITheme.UpdateGridSummaryCounts(dgvItems, lblTotalItems, lblActiveItems, lblInactiveItems, lblInvalidItems);

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

                    // Hide the new summary bar when there is no data
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

                    // Show the summary bar
                    if (pnlStatusBar != null) pnlStatusBar.Visible = true;

                    // Simplified title! The bottom status bar handles the counts now.
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

                    // Show the summary bar to count the preview items
                    if (pnlStatusBar != null) pnlStatusBar.Visible = true;

                    var actionText = _currentState == AppState.PreviewingExcelAdd ? "New Items" : "Updates";

                    // Simplified title here as well
                    lblTitle.Text = $"Import Preview ({actionText})";

                    lblModeIndicator.BackColor = UITheme.WarningColor;
                    lblModeIndicator.Text = $"PREVIEW MODE: Reviewing imported items. Click 'Save Changes' to apply.";
                    lblModeIndicator.Visible = true;
                    break;
            }
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
            _currentDisplayList = data.ToList();
            IEnumerable<ItemCatalogueDto> dataToDisplay = _currentDisplayList;

            if (!string.IsNullOrEmpty(_sortColumn))
            {
                PropertyInfo propertyInfo = typeof(ItemCatalogueDto).GetProperty(_sortColumn);
                if (propertyInfo != null)
                {
                    dataToDisplay = _sortAscending
                        ? _currentDisplayList.OrderBy(x => propertyInfo.GetValue(x))
                        : _currentDisplayList.OrderByDescending(x => propertyInfo.GetValue(x));
                }
            }

            var displayData = dataToDisplay.Select(x => new
            {
                x.Casin,
                x.Model,
                x.Description,
                x.ColorName,
                x.Size,
                x.PCPK,
                x.CasePackQty,
                x.isActive,
                x.OpeningStock,
                x.ItemStatus,

            }).ToList();

            dgvItems.DataSource = null;
            dgvItems.DataSource = displayData;
            UpdateSortArrows();
        }

        private void DgvItems_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (_currentState != AppState.ViewingDB) return;

            string clickedColumn = dgvItems.Columns[e.ColumnIndex].Name;
            if (_sortColumn == clickedColumn)
                _sortAscending = !_sortAscending;
            else
            {
                _sortColumn = clickedColumn;
                _sortAscending = true;
            }
            BindGrid(_currentDisplayList);
        }

        private void UpdateSortArrows()
        {
            if (_currentState != AppState.ViewingDB) return;

            foreach (DataGridViewColumn col in dgvItems.Columns)
            {
                col.SortMode = DataGridViewColumnSortMode.Programmatic;
                col.HeaderText = col.HeaderText.Replace(" ▲", "").Replace(" ▼", "");
                if (col.Name == _sortColumn)
                    col.HeaderText += _sortAscending ? " ▲" : " ▼";
            }
        }
        #endregion

        #region Search Logic
        private void BtnSearch_Click(object sender, EventArgs e)
        {
            if (_currentState != AppState.ViewingDB) return;

            string keyword = txtSearch.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(keyword))
            {
                BindGrid(_allItems);
                SetStatus("Filter cleared.", StatusType.Success);
                return;
            }

            var filtered = _allItems.Where(x =>
                (!string.IsNullOrEmpty(x.Casin) && x.Casin.ToLower().Contains(keyword)) ||
                (!string.IsNullOrEmpty(x.Model) && x.Model.ToLower().Contains(keyword)) ||
                (!string.IsNullOrEmpty(x.Description) && x.Description.ToLower().Contains(keyword))
            ).ToList();

            BindGrid(filtered);
            SetStatus($"Found {filtered.Count} matching items.", StatusType.Success);
        }
        #endregion

        #region Actions (Export, Import, Save)
        private void BtnExport_Click(object sender, EventArgs e)
        {
            if (dgvItems.Rows.Count == 0)
            {
                SetStatus("No data available to export.", StatusType.Warning);
                MessageBox.Show("There is no data in the grid to export.", "Empty Grid", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "Excel Files (*.xlsx)|*.xlsx";
                    sfd.FileName = $"ItemCatalog_{DateTime.Now:yyyyMMdd}.xlsx";
                    sfd.Title = "Save Excel Export";

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        SetStatus("Exporting data to Excel...", StatusType.Warning);

                        Response<string> response = _excelService.ExportGridToExcel(dgvItems, sfd.FileName, "Items");

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
        }

        private async Task LoadExcelForPreviewAsync(AppState previewState)
        {
            try
            {
                using (OpenFileDialog ofd = new OpenFileDialog { Filter = "Excel Files|*.xlsx;*.xls", Title = "Select Excel File" })
                {
                    if (ofd.ShowDialog() != DialogResult.OK) return;

                    SetStatus("Reading Excel file...", StatusType.Warning);

                    bool isUpdate = (previewState == AppState.PreviewingExcelUpdate);
                    var resExcel = await _excelService.ReadCatalogDataTableFromExcel(ofd.FileName, isUpdate);

                    if (!resExcel.Success || resExcel.Data.Count < 2)
                    {
                        SetStatus("Failed to read Excel file.", StatusType.Error);
                        MessageBox.Show($"Error reading Excel file:\n{resExcel.Message}", "Import Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    _pendingCatalogueTable = resExcel.Data[0];
                    _pendingStockTable = resExcel.Data[1];

                    if ((_pendingCatalogueTable == null || _pendingCatalogueTable.Rows.Count == 0))
                    {
                        SetStatus("No data found in Excel file.", StatusType.Warning);
                        MessageBox.Show("The selected Excel sheet appears to be empty.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

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

        private async Task BtnCommitToDb_Click(object sender, EventArgs e)
        {
            var actionText = _currentState == AppState.PreviewingExcelAdd ? "add" : "update";
            var confirm = MessageBox.Show(
                $"Are you sure you want to {actionText} {_pendingCatalogueTable.Rows.Count} items in the system?",
                "Confirm Save", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            SetStatus("Saving changes...", StatusType.Warning);

            try
            {
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
        #endregion
    }
}