using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Configuration; 
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WIPAT.BLL.Interfaces;
using WIPAT.DAL;
using WIPAT.DAL.Interfaces;
using WIPAT.Entities;
using WIPAT.Entities.Dto;
using WIPAT.Entities.Enum;
using static System.Data.Entity.Infrastructure.Design.Executor;

namespace WIPAT.BLL.Services
{
    public class ExcelService : IExcelService
    {
        private readonly WipSession _session;
        private readonly IItemsRepository _itemsRepository;

        public ExcelService(WipSession session, IItemsRepository itemsRepo)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _itemsRepository = itemsRepo ?? throw new ArgumentNullException(nameof(itemsRepo));

        }

        #region  validate excel file

        public async Task<Response<bool>> ValidateExcelFile(string filePath, string fileType, string requiredWorkSheetName, List<string> requiredExcelColumns,
            string requiredMonth = null,
            string requiredYear = null
            )
        {
            var response = new Response<bool>();
            var allowedExtensions = new[] { ".xls", ".xlsx" };
            List<string> casinList = new List<string>();

            // Local helper function to quickly return errors
            Response<bool> CreateErrorResponse(string errorMessage)
            {
                return new Response<bool> { Success = false, Message = errorMessage };
            }

            #region Input Validation
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return CreateErrorResponse("File path is empty.");
            }

            if (!File.Exists(filePath))
            {
                return CreateErrorResponse("The selected file does not exist.");
            }

            var fileExtension = Path.GetExtension(filePath).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return CreateErrorResponse("Invalid file type. Please select a valid Excel file (.xls or .xlsx).");
            }
            #endregion

            try
            {
                var fileInfo = new FileInfo(filePath);

                using (var package = new ExcelPackage(fileInfo))
                {
                    #region File Processing & Worksheet Check
                    if (package.Workbook.Worksheets.Count == 0)
                    {
                        return CreateErrorResponse("The workbook does not contain any worksheets.");
                    }

                    var worksheet = package.Workbook.Worksheets[requiredWorkSheetName];

                    if (worksheet == null || worksheet.Dimension == null)
                    {
                        return CreateErrorResponse($"Worksheet '{requiredWorkSheetName}' is missing or empty.");
                    }
                    #endregion

                    #region Header Validation & Column Mapping
                    // 1. Get all headers from the first row
                    var totalColumns = worksheet.Dimension.End.Column;
                    var excelHeaders = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                    for (int col = 1; col <= totalColumns; col++)
                    {
                        var headerText = worksheet.Cells[1, col].Text.Trim();
                        if (!string.IsNullOrEmpty(headerText) && !excelHeaders.ContainsKey(headerText))
                        {
                            excelHeaders.Add(headerText, col);
                        }
                    }

                    // 2. Check for Missing Columns
                    var missingColumns = requiredExcelColumns.Where(rc => !excelHeaders.ContainsKey(rc)).ToList();

                    if (missingColumns.Any())
                    {
                        return CreateErrorResponse($"Missing required columns: {string.Join(", ", missingColumns)}.");
                    }
                    #endregion

                    #region Data Validation

                    // Helper functions
                    bool IsNumeric(string s) => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
                    bool IsEmpty(string s) => string.IsNullOrWhiteSpace(s);

                    // Loop through rows
                    for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                    {
                        foreach (var colName in requiredExcelColumns)
                        {
                            // Get the actual column index from our map
                            int colIndex = excelHeaders[colName];
                            string cellValue = worksheet.Cells[row, colIndex].Text?.Trim();

                            // --- Validation Rules ---
                            if (string.IsNullOrWhiteSpace(cellValue))
                            {
                                return CreateErrorResponse($"Column '{colName}' at row {row} cannot be empty.");
                            }

                            if (colName == AllColumnNames.CAsin || colName == "CASIN" && !string.IsNullOrWhiteSpace(cellValue))
                            {
                                casinList.Add(cellValue);
                            }

                            // Case 1: Optional Numeric (CasePackQty)
                            if (colName == AllColumnNames.CasePackQty ||
                                colName == StockOrderExcelColumns.Quantity.ToString() ||
                                colName == StockOrderExcelColumns.Month.ToString() ||
                                colName == StockOrderExcelColumns.Year.ToString())
                            {
                                if (!IsEmpty(cellValue) && !IsNumeric(cellValue))
                                {
                                    return CreateErrorResponse($"Column '{colName}' at row {row} must be numeric if provided. Found: '{cellValue}'.");
                                }
                            }
                            // Case 2: Required Numeric (PCPK, OpeningStock)
                            else if (colName == AllColumnNames.PCPK || colName == AllColumnNames.OpeningStock)
                            {
                                if (IsEmpty(cellValue))
                                {
                                    return CreateErrorResponse($"Column '{colName}' at row {row} is required.");
                                }
                                if (!IsNumeric(cellValue))
                                {
                                    return CreateErrorResponse($"Column '{colName}' at row {row} must be a valid number. Found: '{cellValue}'.");
                                }
                            }
                            // Case 3: Required Text (CAsin, Model, Description, etc.)
                            else if (colName == AllColumnNames.CAsin ||
                                     colName == AllColumnNames.Model ||
                                     colName == AllColumnNames.Description ||
                                     colName == AllColumnNames.ColorName ||
                                     colName == AllColumnNames.Size)
                            {
                                if (IsEmpty(cellValue))
                                {
                                    return CreateErrorResponse($"Column '{colName}' at row {row} is required.");
                                }
                            }
                        }
                    }
                    #endregion

                    #region Items in File that are MISSING or DEACTIVATED in DB 
                    if (fileType == FileType.Forecast.ToString() || fileType == FileType.Order.ToString() || fileType == FileType.Stock.ToString())
                    {
                        var distinctCASINs = casinList
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .Select(s => s.Trim())
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList();

                        var missing = new List<string>();
                        var deactivated = new List<string>();

                        foreach (var c in distinctCASINs)
                        {
                            // Call the combined repository method
                            bool? status = await _itemsRepository.CheckCAsinStatus(c);

                            if (status == null)
                            {
                                // Item doesn't exist at all
                                missing.Add(c);
                            }
                            else if (status == false)
                            {
                                // Item exists but isActive is false
                                deactivated.Add(c);
                            }
                        }

                        // Assign lists to response object immediately so they are available in all return paths
                        response.MissingItems = missing;
                        response.DeactivatedItems = deactivated;

                        if (missing.Any() || deactivated.Any())
                        {
                            // ---> GENERATE THE DATATABLE <---
                            DataTable problemTable = CreateProblemItemsDataTable(missing, deactivated, filePath, requiredMonth, requiredYear);

                            // Attach it to your response object (Uncomment if you added this property)
                            response.ProblemItemsTable = problemTable;

                            StringBuilder dialogText = new StringBuilder();
                            dialogText.AppendLine("The following items need your attention:\n");

                            if (missing.Any())
                            {
                                dialogText.AppendLine($"Missing in Catalogue ({missing.Count}):");
                                dialogText.AppendLine(string.Join(", ", missing));
                                dialogText.AppendLine();
                            }

                            if (deactivated.Any())
                            {
                                dialogText.AppendLine($"Deactivated in Catalogue ({deactivated.Count}):");
                                dialogText.AppendLine(string.Join(", ", deactivated));
                                dialogText.AppendLine();
                            }

                            int totalProblemItems = missing.Count + deactivated.Count;
                            if (totalProblemItems > 10)
                            {
                                dialogText.AppendLine("(Tip: Press Ctrl+C to copy this list)\n");
                            }

                            // SCENARIO 1: MISSING ITEMS EXIST. Show MessageBox with ONLY an 'OK' button.
                            if (missing.Any())
                            {
                                dialogText.AppendLine("Process cancelled. You must add the missing items to the catalogue first.");

                                MessageBox.Show(dialogText.ToString(), "Problem Items - Action Required", MessageBoxButtons.OK, MessageBoxIcon.Error);

                                string message = $"Process cancelled. Found {missing.Count} missing item{(missing.Count > 1 ? "s" : "")}";
                                if (deactivated.Any())
                                {
                                    message += $" and {deactivated.Count} deactivated item{(deactivated.Count > 1 ? "s" : "")}";
                                }
                                message += ". Please update the catalogue first.";

                                var errorResponse = CreateErrorResponse(message);
                                errorResponse.MissingItems = missing;
                                errorResponse.DeactivatedItems = deactivated;
                                errorResponse.ProblemItemsTable = problemTable; // Attach here too
                                return errorResponse;
                            }
                            // SCENARIO 2: ONLY DEACTIVATED ITEMS.
                            else
                            {
                                // ONLY allow bypass if the file type is Forecast
                                if (fileType == FileType.Forecast.ToString())
                                {
                                    dialogText.AppendLine("Do you want to create the WIP and ignore calculating WIP for these CASINs?\n");
                                    dialogText.AppendLine("• Click 'Yes' to ignore them and continue.");
                                    dialogText.AppendLine("• Click 'No' to cancel so you can activate them in the catalogue.");

                                    DialogResult result = MessageBox.Show(dialogText.ToString(), "Problem Items - Action Required", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                                    if (result == DialogResult.No)
                                    {
                                        var errorResponse = CreateErrorResponse($"Process cancelled. Found {deactivated.Count} deactivated items. Please update the catalogue first.");
                                        errorResponse.MissingItems = missing;
                                        errorResponse.DeactivatedItems = deactivated;
                                        errorResponse.ProblemItemsTable = problemTable; // Attach here too
                                        return errorResponse;
                                    }
                                    else
                                    {
                                        // User clicked 'Yes'. Update the success message to reflect ignored items.
                                        response.Message = $"File validated successfully (Ignored {deactivated.Count} problem CASINs).";
                                        response.IsContinueWithInactiveItems = true;
                                    }
                                }
                                // If NOT Forecast, cancel the process with an error
                                else
                                {
                                    string message = $"Process cancelled. Found {missing.Count} missing item{(missing.Count > 1 ? "s" : "")}";
                                    if (deactivated.Any())
                                    {
                                        message += $" and {deactivated.Count} deactivated item{(deactivated.Count > 1 ? "s" : "")}";
                                    }
                                    message += ". Please update the catalogue first.";


                                    dialogText.AppendLine(message);

                                    MessageBox.Show(dialogText.ToString(), "Problem Items - Action Required", MessageBoxButtons.OK, MessageBoxIcon.Error);

                                    var errorResponse = CreateErrorResponse($"{message}");
                                    errorResponse.MissingItems = missing;
                                    errorResponse.DeactivatedItems = deactivated;
                                    errorResponse.ProblemItemsTable = problemTable; // Attach here too
                                    return errorResponse;
                                }
                            }
                        }
                    }
                    #endregion Items in File that are MISSING or DEACTIVATED in DB 

                    // If we get here without returning, the file passed validation or user ignored warnings
                    response.Success = true;
                    if (string.IsNullOrEmpty(response.Message))
                    {
                        response.Message = "File validated successfully.";
                    }
                }
            }
            catch (Exception ex)
            {
                return CreateErrorResponse($"Error validating file: {ex.Message}");
            }

            return response;
        }
        // Helper method to generate the DataTable
        private DataTable CreateProblemItemsDataTable(List<string> missingItems, List<string> deactivatedItems, string filePath, string month, string year)
        {
            DataTable dt = new DataTable("ProblemItems");

            // Define columns
            dt.Columns.Add("Casin", typeof(string));
            dt.Columns.Add("Month", typeof(string));
            dt.Columns.Add("Year", typeof(string));
            dt.Columns.Add("FileName", typeof(string));
            dt.Columns.Add("Reason", typeof(string));

            // Extract just the file name
            string fileName = Path.GetFileName(filePath);

            // Add Deactivated items
            if (deactivatedItems != null)
            {
                foreach (var casin in deactivatedItems)
                {
                    dt.Rows.Add(casin, month, year, fileName, "Deactivated");
                }
            }

            // Add Missing items
            if (missingItems != null)
            {
                foreach (var casin in missingItems)
                {
                    dt.Rows.Add(casin, month, year, fileName, "Missing");
                }
            }

            return dt;
        }

        public async Task<Response<string>> ValidateItemCatalogueExcelFile(string filePath)
        {
            var response = new Response<string>();
            var requiredExcelColumns = AllColumnNames.ExcelColumnNames.ToList();
            string requiredWorkSheetName = ConfigurationManager.AppSettings["ItemCatalogueWorksheetName"];
            var allowedExtensions = new[] { ".xls", ".xlsx" };

            #region Input Validation
            if (string.IsNullOrWhiteSpace(requiredWorkSheetName))
            {
                response.Success = false;
                response.Message = "The worksheet name configuration ('ItemCatalogueWorksheetName') is missing or empty in the App.config file.";
                return response;
            }

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

                    if (worksheet == null || worksheet.Dimension == null)
                    {
                        response.Success = false;
                        response.Message = $"Worksheet '{requiredWorkSheetName}' is missing or empty.";
                        return response;
                    }

                    #region Header Validation (Space-Insensitive Fix)
                    var cleanHeadersInExcel = Enumerable.Range(1, worksheet.Dimension.End.Column)
                                                        .Select(col => worksheet.Cells[1, col].Text.Replace(" ", "").Trim().ToLower())
                                                        .Where(text => !string.IsNullOrEmpty(text))
                                                        .ToList();

                    var missingColumns = requiredExcelColumns
                        .Where(col => col != AllColumnNames.IsActive) // Keep IsActive optional
                        .Where(col => !cleanHeadersInExcel.Contains(col.Replace(" ", "").Trim().ToLower()))
                        .ToList();

                    var cleanRequiredKeys = requiredExcelColumns.Select(col => col.Replace(" ", "").Trim().ToLower()).ToList();
                    var rawHeadersRow = Enumerable.Range(1, worksheet.Dimension.End.Column)
                                                  .Select(col => worksheet.Cells[1, col].Text.Trim())
                                                  .ToList();

                    var extraColumns = rawHeadersRow
                        .Where(header => !string.IsNullOrEmpty(header) &&
                                         !cleanRequiredKeys.Contains(header.Replace(" ", "").Trim().ToLower()))
                        .ToList();

                    if (missingColumns.Any() || extraColumns.Any())
                    {
                        string missingMessage = missingColumns.Any() ? $"Missing columns: {string.Join(", ", missingColumns)}." : string.Empty;
                        string extraMessage = extraColumns.Any() ? $"Extra columns: {string.Join(", ", extraColumns)}." : string.Empty;

                        response.Success = false;
                        response.Message = $"{missingMessage} {extraMessage}".Trim();
                        return response;
                    }
                    #endregion

                    #region Data Validation
                    bool dataTypesValid = true;

                    var actualHeadersInFile = Enumerable.Range(1, worksheet.Dimension.End.Column)
                                                        .Where(col => !string.IsNullOrWhiteSpace(worksheet.Cells[1, col].Text))
                                                        .ToDictionary(
                                                            col => worksheet.Cells[1, col].Text.Replace(" ", "").Trim().ToLower(),
                                                            col => col
                                                        );

                    for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                    {
                        foreach (var column in AllColumnNames.ExcelColumnIndexes)
                        {
                            var columnName = column.Key;
                            string strippedColumnKey = columnName.Replace(" ", "").Trim().ToLower();

                            bool IsNumeric(string s) => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
                            bool IsEmpty(string s) => string.IsNullOrWhiteSpace(s);

                            // --- STRICTOR ISACTIVE IN-LOOP VALIDATION ---
                            if (columnName == AllColumnNames.IsActive)
                            {
                                if (!actualHeadersInFile.ContainsKey(strippedColumnKey))
                                {
                                    continue; // Optional column is completely missing, skip safely
                                }

                                int fileColumnIndex = actualHeadersInFile[strippedColumnKey];

                                // Get raw string text representation
                                var cellText = worksheet.Cells[row, fileColumnIndex].Text.Trim();
                                var cellValueStr = worksheet.Cells[row, fileColumnIndex].Value?.ToString()?.Trim() ?? "";

                                // Check empty states
                                if (string.IsNullOrWhiteSpace(cellText) && string.IsNullOrWhiteSpace(cellValueStr))
                                {
                                    response.Success = false;
                                    response.Message = $"Upload Failed: The column heading '{AllColumnNames.IsActive}' is present, therefore row {row} cannot be left empty. Please provide either TRUE or FALSE.";
                                    dataTypesValid = false;
                                    break;
                                }

                                // Normalize checks to catch TRUE, FALSE, 1, 0, or underlying boolean evaluation types safely
                                string normText = cellText.ToUpper();
                                string normVal = cellValueStr.ToUpper();

                                bool isValidTrue = (normText == "TRUE" || normText == "1" || normVal == "TRUE" || normVal == "1");
                                bool isValidFalse = (normText == "FALSE" || normText == "0" || normVal == "FALSE" || normVal == "0");

                                if (!isValidTrue && !isValidFalse)
                                {
                                    response.Success = false;
                                    response.Message = $"Upload Failed: Invalid value '{cellText}' at row {row} in the '{AllColumnNames.IsActive}' column. Value must be written explicitly as either TRUE or FALSE.";
                                    dataTypesValid = false;
                                    break;
                                }

                                continue;
                            }

                            // --- Existing Data Type Validations ---
                            if (!actualHeadersInFile.ContainsKey(strippedColumnKey)) continue;
                            int dynamicIndex = actualHeadersInFile[strippedColumnKey];
                            var cellValue = worksheet.Cells[row, dynamicIndex].Text;

                            //if (columnName == AllColumnNames.CasePackQty)
                            //{
                            //    if (!IsEmpty(cellValue) && !IsNumeric(cellValue))
                            //    {
                            //        response.Success = false;
                            //        response.Message = $"Column '{columnName}' at row {row} must be numeric if provided. Found: '{cellValue}'.";
                            //        dataTypesValid = false;
                            //    }
                            //}
                            //else 
                            if (columnName == AllColumnNames.PCPK || columnName == AllColumnNames.OpeningStock || columnName == AllColumnNames.CasePackQty)
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
                            else if (columnName == AllColumnNames.CAsin || columnName == AllColumnNames.Model || columnName == AllColumnNames.Description || columnName == AllColumnNames.ColorName || columnName == AllColumnNames.Size)
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
                            else if (columnName == AllColumnNames.Notes)
                            {
                                if (!IsEmpty(cellValue) && IsNumeric(cellValue))
                                {
                                    response.Success = false;
                                    response.Message = $"Column '{columnName}' at row {row} must be text if provided, but a numeric value was found: '{cellValue}'.";
                                    dataTypesValid = false;
                                }
                            }
                        }

                        if (!dataTypesValid)
                            break;
                    }
                    #endregion

                    if (dataTypesValid)
                    {
                        response.Success = true;
                        response.Message = "Columns match the required ones, and the data types are correct.";
                        response.Data = requiredWorkSheetName;
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Cannot open file or process data structures. Error: {ex.Message}";
            }

            return response;
        }

        public Response<bool> ValidateColumns(string filePath, string sheetName, List<string> requiredColumns)
        {
            try
            {
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var ws = package.Workbook.Worksheets[sheetName];
                    if (ws == null) return new Response<bool> { Success = false, Message = $"Sheet {sheetName} not found." };

                    var headerRow = Enumerable.Range(1, ws.Dimension.End.Column)
                                              .Select(col => ws.Cells[1, col].Text.Trim())
                                              .ToList();

                    var missing = requiredColumns.Except(headerRow).ToList();
                    if (missing.Any())
                    {
                        return new Response<bool> { Success = false, Message = $"Missing columns: {string.Join(", ", missing)}" };
                    }

                    return new Response<bool> { Success = true };
                }
            }
            catch (Exception ex)
            {
                return new Response<bool> { Success = false, Message = ex.Message };
            }
        }

        #endregion  validate excel file

        #region Read Excel
        public Response<DataTable> ReadExcelToDataTable(string filePath, string sheetName, List<string> columnsToRead = null)
        {
            var response = new Response<DataTable>();
            var table = new DataTable();

            try
            {
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets[sheetName];
                    if (worksheet == null || worksheet.Dimension == null)
                    {
                        return new Response<DataTable> { Success = false, Message = $"Sheet {sheetName} not found or empty." };
                    }

                    // 1. Map Headers to Column Indexes
                    var columnMapping = new Dictionary<string, int>();
                    for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                    {
                        string header = worksheet.Cells[1, col].Text.Trim();
                        if (!string.IsNullOrEmpty(header))
                        {
                            columnMapping[header] = col;
                        }
                    }

                    // 2. Determine which columns to read
                    var targetColumns = columnsToRead ?? columnMapping.Keys.ToList();

                    // 3. Create DataTable Columns
                    foreach (var colName in targetColumns)
                    {
                        if (columnMapping.ContainsKey(colName))
                        {
                            table.Columns.Add(colName);
                        }
                        else
                        {
                            return new Response<DataTable> { Success = false, Message = $"Column '{colName}' not found in file." };
                        }
                    }

                    // 4. Read Data Rows
                    for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                    {
                        var newRow = table.NewRow();
                        bool rowHasData = false;

                        foreach (var colName in targetColumns)
                        {
                            int colIndex = columnMapping[colName];
                            string value = worksheet.Cells[row, colIndex].Text.Trim();
                            newRow[colName] = value;

                            if (!string.IsNullOrEmpty(value)) 
                            {
                                rowHasData = true;
                            } 
                        }

                        if (rowHasData)
                        {
                            table.Rows.Add(newRow);
                        }
                    }

                    response.Success = true;
                    response.Data = table;
                    return response;
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error reading Excel: {ex.Message}";
                return response;
            }
        }

        public async Task<Response<List<DataTable>>> ReadCatalogDataTableFromExcel(string filePath, bool isUpdate = false)
        {
            var response = new Response<List<DataTable>>();
            try
            {

                // Validate the Excel file
                var validationResponse = await ValidateItemCatalogueExcelFile(filePath);
                if (!validationResponse.Success)
                {
                    response.Success = false;
                    response.Message = validationResponse.Message;
                    return response;
                }

                string workSheetName = validationResponse.Data;
                // Get Item Catalogues DataTable
                //Response<DataTable> resItemCatalogues = await GetItemCataloguesDataTableFromExcel(filePath, workSheetName);
                Response<DataTable> resItemCatalogues = await GetItemCataloguesDataTableFromExcel(filePath, workSheetName, isUpdate);
                if (resItemCatalogues.Success == false)
                {
                    response.Success = false;
                    response.Message = resItemCatalogues.Message;
                    return response;
                }

                // Get Stock DataTable
                //Response<DataTable> resInitialStock = await GetStockDataTableFromExcel(filePath, workSheetName);
                Response<DataTable> resInitialStock = await GetStockDataTableFromExcel(filePath, workSheetName, isUpdate);
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
                response.Success = false;
                response.Message = $"An error occurred: {ex.Message}";
            }

            return response;
        }

        public Response<List<WipDetail>> ReadEditWipExcel(string filePath)
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

        #endregion Read Excel

        #region Get Datatable
        public async Task<Response<DataTable>> GetItemCataloguesDataTableFromExcel(string filePath, string requiredWorkSheetName, bool isUpdate = false)
        {
            var response = new Response<DataTable>();
            try
            {
                List<string> requiredCatalogueTableColumns = AllColumnNames.CatalogueTableColumns.ToList();

                // 1. Swap Creation columns for Update columns if in Update Mode
                if (isUpdate)
                {
                    requiredCatalogueTableColumns.Remove(AllColumnNames.CreatedAt);
                    requiredCatalogueTableColumns.Remove(AllColumnNames.CreatedById);

                    if (!requiredCatalogueTableColumns.Contains(AllColumnNames.UpdatedAt))
                        requiredCatalogueTableColumns.Add(AllColumnNames.UpdatedAt);

                    if (!requiredCatalogueTableColumns.Contains(AllColumnNames.UpdatedById))
                        requiredCatalogueTableColumns.Add(AllColumnNames.UpdatedById);
                }

                #region 2. Add Columns to DataTable
                DataTable dt = new DataTable();
                foreach (var columnName in requiredCatalogueTableColumns)
                {
                    // Now fully handled by your updated AllColumnNames class!
                    Type columnType = AllColumnNames.GetColumnType(columnName);
                    dt.Columns.Add(columnName, columnType);
                }
                #endregion

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

                    if (worksheet == null || worksheet.Dimension == null)
                    {
                        response.Success = false;
                        response.Message = "Worksheet is not valid, disposed, or entirely empty.";
                        return response;
                    }

                    int rowCount = worksheet.Dimension.Rows;
                    int colCount = worksheet.Dimension.Columns;

                    #region 3. Dynamically Map Column Positions (Space-Proof)
                    Dictionary<string, int> actualColumnIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                    for (int col = 1; col <= colCount; col++)
                    {
                        string headerValue = worksheet.Cells[1, col].Value?.ToString();
                        if (!string.IsNullOrWhiteSpace(headerValue))
                        {
                            string strippedHeader = headerValue.Replace(" ", "").Trim();
                            actualColumnIndexes[strippedHeader] = col;
                        }
                    }
                    #endregion

                    #region 4. Validate Required Columns Exist
                    foreach (var requiredCol in requiredCatalogueTableColumns)
                    {
                        if (requiredCol == AllColumnNames.CreatedAt || requiredCol == AllColumnNames.CreatedById ||
                            requiredCol == AllColumnNames.UpdatedAt || requiredCol == AllColumnNames.UpdatedById)
                            continue;

                        if (requiredCol == AllColumnNames.IsActive)
                            continue;

                        string searchKey = requiredCol.Replace(" ", "").Trim();

                        if (!actualColumnIndexes.ContainsKey(searchKey))
                        {
                            response.Success = false;
                            response.Message = $"Upload Failed: Missing required column '{requiredCol}'. Please ensure it exists in the header row.";
                            return response;
                        }
                    }
                    #endregion

                    #region 5. Read and Dynamically Convert Data
                    for (int row = 2; row <= rowCount; row++)
                    {
                        DataRow dr = dt.NewRow();

                        foreach (var column in requiredCatalogueTableColumns)
                        {
                            if (column == AllColumnNames.CreatedAt || column == AllColumnNames.UpdatedAt)
                            {
                                dr[column] = DateTime.Now;
                            }
                            else if (column == AllColumnNames.CreatedById || column == AllColumnNames.UpdatedById)
                            {
                                dr[column] = _session.LoggedInUser.Id;
                            }
                            else
                            {
                                string searchKey = column.Replace(" ", "").Trim();

                                if (column == AllColumnNames.IsActive && !actualColumnIndexes.ContainsKey(searchKey))
                                {
                                    dr[column] = DBNull.Value;
                                    continue;
                                }

                                int dynamicColIndex = actualColumnIndexes[searchKey];
                                string cellValue = worksheet.Cells[row, dynamicColIndex].Value?.ToString()?.Trim();

                                try
                                {
                                    if (string.IsNullOrWhiteSpace(cellValue))
                                    {
                                        dr[column] = DBNull.Value;
                                    }
                                    else
                                    {
                                        Type targetType = dt.Columns[column].DataType;
                                        Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
                                        dr[column] = Convert.ChangeType(cellValue, underlyingType);
                                    }
                                }
                                catch (FormatException)
                                {
                                    response.Success = false;
                                    response.Message = $"Upload Failed: Row {row} contains invalid data in the '{column}' column. Expected a valid {dt.Columns[column].DataType.Name}.";
                                    return response;
                                }
                                catch (Exception ex)
                                {
                                    response.Success = false;
                                    response.Message = $"Upload Failed: Error processing Row {row}, Column '{column}'. Details: {ex.Message}";
                                    return response;
                                }
                            }
                        }

                        dt.Rows.Add(dr);
                    }
                    #endregion

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
        public async Task<Response<DataTable>> GetStockDataTableFromExcel(string filePath, string requiredWorkSheetName, bool isUpdate = false)
        {
            var response = new Response<DataTable>();
            try
            {
                List<string> requiredStockTableColumns = AllColumnNames.StockTableColumns.ToList();

                // 1. Swap Creation columns for Update columns if in Update Mode
                if (isUpdate)
                {
                    requiredStockTableColumns.Remove(AllColumnNames.CreatedAt);
                    requiredStockTableColumns.Remove(AllColumnNames.CreatedById);

                    if (!requiredStockTableColumns.Contains(AllColumnNames.UpdatedAt))
                        requiredStockTableColumns.Add(AllColumnNames.UpdatedAt);

                    if (!requiredStockTableColumns.Contains(AllColumnNames.UpdatedById))
                        requiredStockTableColumns.Add(AllColumnNames.UpdatedById);
                }

                #region 2. Add columns to DataTable
                DataTable dt = new DataTable();

                foreach (var columnName in requiredStockTableColumns)
                {
                    // Handled dynamically by the updated AllColumnNames mapping
                    Type columnType = AllColumnNames.GetColumnType(columnName);
                    dt.Columns.Add(columnName, columnType);
                }
                #endregion

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

                    if (worksheet == null || worksheet.Dimension == null)
                    {
                        response.Success = false;
                        response.Message = "Worksheet is not valid or has been disposed.";
                        response.Data = null;
                        return response;
                    }

                    int rowCount = worksheet.Dimension.Rows;

                    for (int row = 2; row <= rowCount; row++)
                    {
                        DataRow dr = dt.NewRow();

                        foreach (var column in requiredStockTableColumns)
                        {
                            if (column == AllColumnNames.CreatedAt || column == AllColumnNames.UpdatedAt)
                            {
                                dr[column] = DateTime.Now;
                            }
                            else if (column == AllColumnNames.CreatedById || column == AllColumnNames.UpdatedById)
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
        #endregion Get Datatable

        #region Export to excel
        public void ExportWipDataToExcel<T>(List<T> data, string fileName, string worksheetName)
        {
            try
            {

                #region validate inputs
                if (data == null || !data.Any())
                {
                    MessageBox.Show("Data list is null or empty.", "Export Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(fileName))
                {
                    MessageBox.Show("File name is empty.", "Export Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(worksheetName))
                {
                    MessageBox.Show("Worksheet name is empty.", "Export Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                #endregion validate inputs

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

        public Response<string> ExportGridToExcel(DataGridView grid, string filePath, string sheetName)
        {
            var response = new Response<string>();
            #region validate inputs 
            if (grid == null)
            {
                response.Success = false;
                response.Message = "DataGridView is null.";
                return response;
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                response.Success = false;
                response.Message = "File path is empty.";
                return response;
            }

            if (string.IsNullOrWhiteSpace(sheetName))
            {
                response.Success = false;
                response.Message = "Sheet name is empty.";
                return response;
            }
            #endregion validate inputs 

            try
            {
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    // Add Worksheet
                    var ws = package.Workbook.Worksheets.Add(sheetName);

                    // 1. Export Headers
                    for (int i = 0; i < grid.Columns.Count; i++)
                    {
                        var cell = ws.Cells[1, i + 1];
                        cell.Value = grid.Columns[i].HeaderText;

                        // Style Header: Bold + Light Gray Background
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        cell.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    }

                    // 2. Export Data Rows
                    for (int i = 0; i < grid.Rows.Count; i++)
                    {
                        for (int j = 0; j < grid.Columns.Count; j++)
                        {
                            // i+2 (Excel row starts at 1, header is 1)
                            // j+1 (Excel col starts at 1)
                            var value = grid.Rows[i].Cells[j].Value;
                            ws.Cells[i + 2, j + 1].Value = value?.ToString();
                        }
                    }

                    // 3. Auto-fit columns
                    ws.Cells.AutoFitColumns();

                    // 4. Save to disk
                    package.Save();
                }

                // Success Response
                response.Success = true;
                response.Message = "File exported successfully.";
                response.Data = filePath;
                response.Status = StatusType.Success;
            }
            catch (Exception ex)
            {
                // Error Response
                response.Success = false;
                response.Message = $"Export Failed: {ex.Message}";
                response.Data = null;
                response.Status = StatusType.Error;
            }

            return response;
        }

        #endregion Export to excel

        #region helpers 
        //helpers for mapping column values
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
                    if (!IsEmpty(cellValue))
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

        // 
        public string GetEnumValue(Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = (DescriptionAttribute)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
            return attribute == null ? value.ToString() : attribute.Description;
        }
        #endregion helpers   

    }

}