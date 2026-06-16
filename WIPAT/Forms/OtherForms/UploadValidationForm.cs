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
using WIPAT.BLL.Manager.ExcelTemplateDefinitions;
using WIPAT.BLL.Services;
using WIPAT.DAL.Interfaces;
using WIPAT.Entities;
using WIPAT.Entities.Dto;
using WIPAT.Entities.Enum;
using WIPAT.Helpers;
using static System.Collections.Specialized.BitVector32;

namespace WIPAT.Forms
{
    public partial class UploadValidationForm : Form
    {
        private readonly ExcelValidationService _excelValidationService;

        public UploadValidationForm(WipSession session, IItemsRepository itemsRepo)
        {
            InitializeComponent();
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            _excelValidationService = new ExcelValidationService(session, itemsRepo);


            ApplyTheme();
            SetStatus("Ready", StatusType.Reset);
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
            var dataSource = Enum.GetValues(typeof(ExcelFileType))
                                 .Cast<ExcelFileType>()
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
                    ExcelFileType selectedType = (ExcelFileType)cmbFileType.SelectedValue;

                    // Clear previous data
                    dataGridView.DataSource = null;
                    dataGridView.Visible = true;
                    SetStatus("Validating...", StatusType.Warning);

                    #region get required ExcelColumns
                    var requiredExcelColumns = FileTemplateFactory.GetTemplate(selectedType);
                    #endregion get required ExcelColumns

                    #region get required WorkSheetName
                    string requiredWorkSheetName;

                    switch (selectedType)
                    {
                        case ExcelFileType.AddNewItemsToCatalogue:
                        case ExcelFileType.UpdateExistingCatalogue:
                            requiredWorkSheetName = ConfigurationManager.AppSettings["ItemCatalogueWorksheetName"] ?? "ItemCatalogues";
                            break;

                        case ExcelFileType.ForecastFile:
                            requiredWorkSheetName = ConfigurationManager.AppSettings["ForecastWorksheetName"] ?? "Vendor Central Excel Output";
                            break;

                        case ExcelFileType.OrderFile:
                            requiredWorkSheetName = ConfigurationManager.AppSettings["OrderWorksheetName"] ?? "Order";
                            break;

                        default:
                            requiredWorkSheetName = selectedType.ToString();
                            break;
                    }
                    #endregion get required WorkSheetName


                    // Execute Service Validation
                    var response = await _excelValidationService.ValidateAndLoadExcelAsync(
                        ofd.FileName, //filepath
                        selectedType, //filetype
                        requiredWorkSheetName, //worksheet name
                        requiredExcelColumns, //column definitions
                        PromptForInactiveItems // Pass the delegate for MessageBox logic
                    );

                    // Handle Service Output
                    if (response.Success)
                    {
                        SetStatus(response.Message, StatusType.Success);
                        dataGridView.DataSource = response.Data;
                        dataGridView.AutoResizeColumns();
                    }
                    else
                    {
                        ShowErrors(response.MissingItems ?? new List<string> { response.Message });
                    }
                }
            }
        }
        #endregion

        #region UI Helpers
        /// <summary>
        /// This delegate is invoked by the service so the form can handle UI prompting 
        /// while keeping the service layer purely logic-driven.
        /// </summary>
        private bool PromptForInactiveItems(int inactiveCount)
        {
            // Ensure UI prompts happen on the UI thread
            if (this.InvokeRequired)
            {
                return (bool)this.Invoke(new Func<int, bool>(PromptForInactiveItems), inactiveCount);
            }

            var dialogResult = MessageBox.Show(
                $"The file contains {inactiveCount} inactive CASIN(s).\n\nDo you want to ignore the inactive items and continue loading the rest?",
                "Inactive Items Detected",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (dialogResult == DialogResult.Yes)
            {
                SetStatus("Inactive items ignored.", StatusType.Warning);
                return true;
            }

            return false;
        }

        private void ShowErrors(List<string> errors)
        {
            SetStatus($"Validation Failed: Found {errors.Count} errors.", StatusType.Error);
            dataGridView.Visible = false; // Hide grid

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