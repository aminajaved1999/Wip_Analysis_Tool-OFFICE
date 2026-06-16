using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions; 
using System.Threading.Tasks;
using System.Windows.Forms;
using WIPAT.BLL.Manager;
using WIPAT.BLL.Manager.ExcelTemplateDefinitions;
using WIPAT.Helpers;

namespace WIPAT.Forms
{
    public partial class TemplateDownloadForm : Form
    {
        public TemplateDownloadForm()
        {
            InitializeComponent();
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

            // Apply the brand theme upon initialization
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            UITheme.SetFormIcon(this);
            this.BackColor = UITheme.BackgroundCanvas;

            // Header Styling
            headerPanel.BackColor = UITheme.MainColor;
            lblTitle.ForeColor = UITheme.SurfaceWhite;

            // Label Styling
            lblInstruction.ForeColor = UITheme.TextSecondaryColor;

            // Button Styling 
            UITheme.StyleButton(btnDownload, AppButtonStyle.ExportToExcel);
        }

        private void TemplateDownloadForm_Load(object sender, EventArgs e)
        {
            // Bind Enum values but format them nicely with spaces (e.g., "ForecastFile" -> "Forecast File")
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

        private void btnDownload_Click(object sender, EventArgs e)
        {
            // Ensure something is selected
            if (cmbFileType.SelectedValue == null)
            {
                MessageBox.Show("Please select a file type first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Retrieve the selected type using SelectedValue
            ExcelFileType selectedType = (ExcelFileType)cmbFileType.SelectedValue;

            // Dynamically set the default file name before showing the dialog
            saveFileDialog1.FileName = $"{selectedType}_Template.xlsx";

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                GenerateExcelTemplate(selectedType, saveFileDialog1.FileName);
                MessageBox.Show("Template downloaded successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void GenerateExcelTemplate(ExcelFileType fileType, string filePath)
        {
            var rules = FileTemplateFactory.GetTemplate(fileType);

            // Determine worksheet name dynamically based on App.config keys
            string sheetName = fileType.ToString();

            if (fileType == ExcelFileType.AddNewItemsToCatalogue || fileType == ExcelFileType.UpdateExistingCatalogue)
            {
                sheetName = ConfigurationManager.AppSettings["ItemCatalogueWorksheetName"] ?? "ItemCatalogues";
            }
            else if (fileType == ExcelFileType.ForecastFile)
            {
                sheetName = ConfigurationManager.AppSettings["ForecastWorksheetName"] ?? "Vendor Central Excel Output";
            }
            else if (fileType == ExcelFileType.OrderFile)
            {
                sheetName = ConfigurationManager.AppSettings["OrderWorksheetName"] ?? "Order";
            }

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                // Injects the dynamically mapped worksheet name
                var ws = package.Workbook.Worksheets.Add(sheetName);

                // Write Headers
                for (int i = 0; i < rules.Count; i++)
                {
                    int col = i + 1;
                    var cell = ws.Cells[1, col];
                    cell.Value = rules[i].Definition.Name;

                    // Clean contextual styling matching enterprise tokens
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                }

                // Global worksheet column auto-fit standard
                ws.Cells.AutoFitColumns();

                // Save standard changes to physical file info pipeline
                package.Save();
            }
        }
    }
}