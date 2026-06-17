using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using WIPAT.Entities.ExcelTemplateDefinitions;
using WIPAT.BLL.Services;
using WIPAT.DAL.Interfaces;
using WIPAT.Entities;
using WIPAT.Entities.Dto;
using WIPAT.Entities.Enum;
using WIPAT.Helpers;

namespace WIPAT.Forms
{
    public partial class TestTemplateUploadForm : Form
    {
        // 1. Store dependencies as fields so we can instantiate a fresh service on each upload
        private readonly WipSession _session;
        private readonly IItemsRepository _itemsRepo;

        public TestTemplateUploadForm(WipSession session, IItemsRepository itemsRepo)
        {
            InitializeComponent();
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

            // Save dependencies instead of initializing the service once globally
            _session = session;
            _itemsRepo = itemsRepo;

            ApplyTheme();
            SetStatus("Ready", StatusType.Reset);

            // Hook up events for DataGridView validation and styling
            dgvErrorItems.DataBindingComplete += DgvErrorItems_DataBindingComplete;
            dgvErrorItems.CellBeginEdit += DgvErrorItems_CellBeginEdit;
        }

        #region UI & Theme Setup
        private void ApplyTheme()
        {
            UITheme.SetFormIcon(this);
            this.BackColor = UITheme.BackgroundCanvas;

            // Header Styling
            headerPanel.BackColor = UITheme.MainColor;
            lblTitle.ForeColor = UITheme.SurfaceWhite;

            // Toolbar Styling
            toolbarPanel.BackColor = Color.FromArgb(245, 246, 250);

            // Grid & Button Styling
            UITheme.StyleGrid(dataGridView);
            UITheme.StyleButton(btnBrowse, AppButtonStyle.Search);
        }

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

        #region Events
        private void UploadValidationForm_Load(object sender, EventArgs e)
        {
            var dataSource = Enum.GetValues(typeof(ImportExcelFileType))
                                 .Cast<ImportExcelFileType>()
                                 .Select(fileType => new
                                 {
                                     Value = fileType,
                                     Display = Regex.Replace(fileType.ToString(), "([A-Z])", " $1").Trim()
                                 })
                                 .ToList();

            cmbFileType.DataSource = dataSource;
            cmbFileType.DisplayMember = "Display";
            cmbFileType.ValueMember = "Value";
        }

        private async void btnBrowse_Click(object sender, EventArgs e)
        {
            if (cmbFileType.SelectedValue == null)
            {
                MessageBox.Show("Please select a template type first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls";
                ofd.Title = "Select Excel File to Validate";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtFilePath.Text = ofd.FileName;
                    ImportExcelFileType selectedType = (ImportExcelFileType)cmbFileType.SelectedValue;

                    ResetGrids();
                    SetStatus("Validating...", StatusType.Warning);

                    #region get required ExcelColumns
                    var requiredExcelColumns = FileTemplateFactory.GetImportTemplate(selectedType);
                    #endregion get required ExcelColumns

                    #region get required WorkSheetName
                    string requiredWorkSheetName;

                    switch (selectedType)
                    {
                        case ImportExcelFileType.AddNewItemsToCatalogue:
                        case ImportExcelFileType.UpdateExistingCatalogue:
                            requiredWorkSheetName = ConfigurationManager.AppSettings["ItemCatalogueWorksheetName"] ?? "ItemCatalogues";
                            break;

                        case ImportExcelFileType.ForecastFile:
                            requiredWorkSheetName = ConfigurationManager.AppSettings["ForecastWorksheetName"] ?? "Vendor Central Excel Output";
                            break;

                        case ImportExcelFileType.OrderFile:
                            requiredWorkSheetName = ConfigurationManager.AppSettings["OrderWorksheetName"] ?? "Order";
                            break;

                        default:
                            requiredWorkSheetName = selectedType.ToString();
                            break;
                    }
                    #endregion get required WorkSheetName

                    // 3. Instantiate a fresh service strictly for this upload. 
                    // This prevents the service from returning a cached ProblemItemsTable from the previous upload.
                    var freshValidationService = new ExcelValidationService(_session, _itemsRepo);

                    // Execute Service Validation
                    var response = await freshValidationService.ValidateAndLoadExcelAsync(
                        ofd.FileName, //filepath
                        selectedType, //filetype
                        requiredWorkSheetName, //worksheet name
                        requiredExcelColumns //column definitions
                    );

                    // Handle Generic Problem Items Panel
                    bool hasProblemItems = response.Data?.ProblemItemsTable != null && response.Data.ProblemItemsTable.Rows.Count > 0;

                    if (hasProblemItems)
                    {
                        // Show Panel and Bind Data
                        pnlErrorItems.Visible = true;
                        dgvErrorItems.AutoGenerateColumns = true;
                        dgvErrorItems.DataSource = response.Data.ProblemItemsTable;

                        // Set Dynamic Header Text
                        string displayTypeName = Regex.Replace(selectedType.ToString(), "([A-Z])", " $1").Trim();
                        lblErrorHeader.Text = $"Invalid Items in {displayTypeName}";

                        // Requirement: Only show Mark Invalid and Checkbox column if ForecastFile is selected
                        bool isForecastFile = selectedType == ImportExcelFileType.ForecastFile;
                        bool hasMissingItems = response.Data.MissingCasins != null && response.Data.MissingCasins.Count > 0;

                        btnMarkInvalid.Visible = isForecastFile && hasMissingItems;
                        chkSelect.Visible = isForecastFile;
                    }
                    else
                    {
                        pnlErrorItems.Visible = false;
                        dgvErrorItems.DataSource = null;
                    }

                    // Handle Service Output
                    if (response.Success)
                    {
                        ResetGrids();
                        SetStatus(response.Message, StatusType.Success);
                        dataGridView.DataSource = response.Data?.ValidatedData;
                        dataGridView.Visible = true;

                        if (dataGridView.Columns.Count > 0)
                        {
                            dataGridView.AutoResizeColumns();
                        }
                    }
                    else
                    {
                        ShowErrors(response.MissingItems ?? new List<string> { response.Message });
                    }
                }
            }
        }
        #endregion

        #region Error Grid Specific Events

        /// <summary>
        /// Visually grays out and locks the checkboxes for rows where the reason is not "missing".
        /// </summary>
        private void DgvErrorItems_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            var reasonCol = dgvErrorItems.Columns.Cast<DataGridViewColumn>()
                .FirstOrDefault(c => c.Name.Equals("Reason", StringComparison.OrdinalIgnoreCase));

            if (reasonCol != null && dgvErrorItems.Columns.Contains("chkSelect"))
            {
                foreach (DataGridViewRow row in dgvErrorItems.Rows)
                {
                    var reasonText = row.Cells[reasonCol.Index].Value?.ToString() ?? string.Empty;

                    if (!reasonText.Trim().Equals("missing", StringComparison.OrdinalIgnoreCase))
                    {
                        row.Cells["chkSelect"].ReadOnly = true;
                        row.Cells["chkSelect"].Style.BackColor = Color.LightGray; // Visual cue that it's disabled
                    }
                }
            }
        }

        /// <summary>
        /// Enforces the rule that prevents user clicks/edits on the checkbox cell if the reason is not "missing".
        /// </summary>
        private void DgvErrorItems_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.RowIndex >= 0 && dgvErrorItems.Columns[e.ColumnIndex].Name == "chkSelect")
            {
                var reasonCol = dgvErrorItems.Columns.Cast<DataGridViewColumn>()
                    .FirstOrDefault(c => c.Name.Equals("Reason", StringComparison.OrdinalIgnoreCase));

                if (reasonCol != null)
                {
                    var reasonText = dgvErrorItems.Rows[e.RowIndex].Cells[reasonCol.Index].Value?.ToString() ?? string.Empty;

                    // If reason is not strictly "missing", cancel the edit action
                    if (!reasonText.Trim().Equals("missing", StringComparison.OrdinalIgnoreCase))
                    {
                        e.Cancel = true;
                    }
                }
                else
                {
                    // If the Reason column couldn't be found in the template, prevent editing entirely
                    e.Cancel = true;
                }
            }
        }

        #endregion

        #region UI Helpers

        /// <summary>
        /// Safely tears down the grids, ensuring auto-generated columns from previous
        /// uploads don't persist visually while keeping custom design-time columns intact.
        /// </summary>
        private void ResetGrids()
        {
            // Clear main data grid
            dataGridView.DataSource = null;
            dataGridView.Visible = true;

            // Clear error grid data & hide panel
            dgvErrorItems.DataSource = null;
            pnlErrorItems.Visible = false;

            // Iterate backwards to safely remove auto-generated schema columns
            // while preserving your 'chkSelect' CheckBox column.
            for (int i = dgvErrorItems.Columns.Count - 1; i >= 0; i--)
            {
                if (dgvErrorItems.Columns[i].Name != "chkSelect")
                {
                    dgvErrorItems.Columns.RemoveAt(i);
                }
            }
        }

        private void ShowErrors(List<string> errors)
        {
            SetStatus($"Validation Failed: Found {errors.Count} errors.", StatusType.Error);
            dataGridView.Visible = false; // Hide main grid

            int displayLimit = 15;
            var errorsToDisplay = errors.Take(displayLimit).ToList();

            string errorMessage = string.Join(Environment.NewLine, errorsToDisplay);

            if (errors.Count > displayLimit)
            {
                errorMessage += $"{Environment.NewLine}{Environment.NewLine}... and {errors.Count - displayLimit} more error(s).";
            }

            MessageBox.Show(errorMessage, "Validation Errors", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        #endregion
    }
}