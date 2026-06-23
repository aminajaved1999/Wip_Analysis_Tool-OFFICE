using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using WIPAT.Entities.ExcelTemplateDefinitions;

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

        #region validate excel file

        public async Task<Response<bool>> ValidateExcelFile(string filePath, string fileType, string requiredWorkSheetName, List<string> requiredExcelColumns,
            string requiredMonth = null,
            string requiredYear = null
            )
        {
            var response = new Response<bool>();
            var allowedExtensions = new[] { ".xls", ".xlsx" };
            List<string> casinList = new List<string>();

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

                    var missingColumns = requiredExcelColumns.Where(rc => !excelHeaders.ContainsKey(rc)).ToList();

                    if (missingColumns.Any())
                    {
                        return CreateErrorResponse($"Missing required columns: {string.Join(", ", missingColumns)}.");
                    }
                    #endregion

                    #region Data Validation

                    bool IsNumeric(string s) => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
                    bool IsEmpty(string s) => string.IsNullOrWhiteSpace(s);

                    HashSet<string> distinctProjMonths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    HashSet<string> distinctProjYears = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    bool isForecastFile = fileType == FileType.Forecast.ToString();

                    for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                    {
                        foreach (var colName in requiredExcelColumns)
                        {
                            int colIndex = excelHeaders[colName];
                            string cellValue = worksheet.Cells[row, colIndex].Text?.Trim();

                            if (string.IsNullOrWhiteSpace(cellValue))
                            {
                                return CreateErrorResponse($"Column '{colName}' at row {row} cannot be empty.");
                            }

                            if (isForecastFile)
                            {
                                if (colName == ForecastExcelColumns.ProjectionMonth.ToString())
                                    distinctProjMonths.Add(cellValue);
                                else if (colName == ForecastExcelColumns.ProjectionYear.ToString())
                                    distinctProjYears.Add(cellValue);
                            }

                            if (colName == AllColumnNames.CAsin || colName == "CASIN" && !string.IsNullOrWhiteSpace(cellValue))
                            {
                                casinList.Add(cellValue);
                            }

                            // STRICT VALIDATION FOR ORDER QUANTITY: No decimals, no negatives, no text.
                            if (colName == StockOrderExcelColumns.Quantity.ToString())
                            {
                                if (IsEmpty(cellValue))
                                {
                                    return CreateErrorResponse($"Column '{colName}' at row {row} is required.");
                                }
                                if (!int.TryParse(cellValue, out int qty) || qty < 0)
                                {
                                    return CreateErrorResponse($"Validation Error: Column '{colName}' at row {row} must be a valid positive whole number. Decimals, text, and negative values are strictly prohibited. Found: '{cellValue}'.");
                                }
                            }
                            // GENERAL NUMERIC VALIDATION FOR OTHERS
                            else if (colName == AllColumnNames.CasePackQty ||
                                     colName == StockOrderExcelColumns.Month.ToString() ||
                                     colName == StockOrderExcelColumns.Year.ToString())
                            {
                                if (!IsEmpty(cellValue) && !IsNumeric(cellValue))
                                {
                                    return CreateErrorResponse($"Column '{colName}' at row {row} must be numeric if provided. Found: '{cellValue}'.");
                                }
                            }
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

                    if (isForecastFile)
                    {
                        if (distinctProjMonths.Count > 1 || distinctProjYears.Count > 1)
                        {
                            return CreateErrorResponse("Upload Failed: Multiple Projection Months or Years detected in the file. All rows must have the exact same Projection Month and Year.");
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
                            // Returns INT now
                            int? status = await _itemsRepository.CheckCAsinStatus(c);

                            if (status == null)
                            {
                                missing.Add(c);
                            }
                            else if (status == (int)CatalogueItemStatus.Inactive || status == (int)CatalogueItemStatus.Invalid)
                            {
                                deactivated.Add(c);
                            }
                        }

                        response.MissingItems = missing;
                        response.DeactivatedItems = deactivated;

                        if (missing.Any() || deactivated.Any())
                        {

                            var problemTableResponse = new DataTableFactory().CreateProblemItemsDataTable(missing, deactivated, filePath);
                            if (!problemTableResponse.Success)
                            {
                                response.Success = false;
                                response.Message = problemTableResponse.Message;
                                return response;
                            }
                            var problemTable = problemTableResponse.Data;
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
                                dialogText.AppendLine($"Inactive or Invalid in Catalogue ({deactivated.Count}):");
                                dialogText.AppendLine(string.Join(", ", deactivated));
                                dialogText.AppendLine();
                            }

                            int totalProblemItems = missing.Count + deactivated.Count;
                            if (totalProblemItems > 10)
                            {
                                dialogText.AppendLine("(Tip: Press Ctrl+C to copy this list)\n");
                            }

                            // SCENARIO 1: MISSING ITEMS EXIST.
                            if (missing.Any())
                            {
                                dialogText.AppendLine("Process cancelled. You must add the missing items to the catalogue first.");
                                MessageBox.Show(dialogText.ToString(), "Problem Items - Action Required", MessageBoxButtons.OK, MessageBoxIcon.Error);

                                string message = $"Process cancelled. Found {missing.Count} missing item{(missing.Count > 1 ? "s" : "")}";
                                if (deactivated.Any())
                                {
                                    message += $" and {deactivated.Count} inactive item{(deactivated.Count > 1 ? "s" : "")}";
                                }
                                message += ". Please update the catalogue first.";

                                var errorResponse = CreateErrorResponse(message);
                                errorResponse.MissingItems = missing;
                                errorResponse.DeactivatedItems = deactivated;
                                errorResponse.ProblemItemsTable = problemTable;
                                return errorResponse;
                            }
                            // SCENARIO 2: ONLY DEACTIVATED ITEMS.
                            else
                            {
                                if (fileType == FileType.Forecast.ToString())
                                {
                                    dialogText.AppendLine("Do you want to create the WIP and ignore calculating WIP for these CASINs?\n");
                                    dialogText.AppendLine("• Click 'Yes' to ignore them and continue.");
                                    dialogText.AppendLine("• Click 'No' to cancel so you can activate them in the catalogue.");

                                    DialogResult result = MessageBox.Show(dialogText.ToString(), "Problem Items - Action Required", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                                    if (result == DialogResult.No)
                                    {
                                        var errorResponse = CreateErrorResponse($"Process cancelled. Found {deactivated.Count} inactive items. Please update the catalogue first.");
                                        errorResponse.MissingItems = missing;
                                        errorResponse.DeactivatedItems = deactivated;
                                        errorResponse.ProblemItemsTable = problemTable;
                                        return errorResponse;
                                    }
                                    else
                                    {
                                        response.Message = $"File validated successfully (Ignored {deactivated.Count} problem CASINs).";
                                        response.IsContinueWithInactiveItems = true;
                                    }
                                }
                                else
                                {
                                    string message = $"Process cancelled. Found {missing.Count} missing item{(missing.Count > 1 ? "s" : "")}";
                                    if (deactivated.Any())
                                    {
                                        message += $" and {deactivated.Count} inactive item{(deactivated.Count > 1 ? "s" : "")}";
                                    }
                                    message += ". Please update the catalogue first.";


                                    dialogText.AppendLine(message);

                                    MessageBox.Show(dialogText.ToString(), "Problem Items - Action Required", MessageBoxButtons.OK, MessageBoxIcon.Error);

                                    var errorResponse = CreateErrorResponse($"{message}");
                                    errorResponse.MissingItems = missing;
                                    errorResponse.DeactivatedItems = deactivated;
                                    errorResponse.ProblemItemsTable = problemTable;
                                    return errorResponse;
                                }
                            }
                        }
                    }
                    #endregion 

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


        public async Task<Response<string>> ValidateItemCatalogueExcelFile(string filePath, bool isUpdate = false)
        {
            var response = new Response<string>();

            // ✅ 1. Get exact rules based on whether this is an Insert or Update
            var templateType = isUpdate ? ImportExcelFileType.UpdateExistingCatalogue : ImportExcelFileType.AddNewItemsToCatalogue;
            var templateRules = FileTemplateFactory.GetImportTemplate(templateType);

            string requiredWorkSheetName = ConfigurationManager.AppSettings["ItemCatalogueWorksheetName"];
            var allowedExtensions = new[] { ".xls", ".xlsx" };

            #region Input Validation
            if (string.IsNullOrWhiteSpace(requiredWorkSheetName))
            {
                response.Success = false;
                response.Message = "The worksheet name configuration ('ItemCatalogueWorksheetName') is missing or empty in the App.config file.";
                return response;
            }

            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                response.Success = false;
                response.Message = "File path is empty or the selected file does not exist.";
                return response;
            }

            if (!allowedExtensions.Contains(Path.GetExtension(filePath).ToLower()))
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
                    #region Worksheet Validation

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
                    #endregion Worksheet Validation

                    #region Header Validation

                    var rawHeaders = Enumerable.Range(1, worksheet.Dimension.End.Column)
                        .Select(col => worksheet.Cells[1, col].Text.Trim())
                        .Where(header => !string.IsNullOrWhiteSpace(header))
                        .ToList();

                    var cleanHeadersInExcel = rawHeaders
                        .Select(header => header.Replace(" ", "").ToLower())
                        .ToList();

                    // Find required columns missing from the Excel file
                    var missingColumns = templateRules
                        .Where(rule => rule.IsHeaderRequired)
                        .Where(rule => !cleanHeadersInExcel.Contains(rule.Definition.Name.Replace(" ", "").ToLower()))
                        .Select(rule => rule.Definition.Name)
                        .ToList();

                    var templateNamesCleaned = templateRules
                        .Select(rule => rule.Definition.Name.Replace(" ", "").ToLower())
                        .ToList();

                    // Find columns in Excel that aren't defined in our rules
                    var extraColumns = rawHeaders
                        .Where(header => !templateNamesCleaned.Contains(header.Replace(" ", "").ToLower()))
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

                    #region Data Validation Engine 

                    // Map the actual Excel column indexes to our sanitized column names
                    var columnMappings = Enumerable.Range(1, worksheet.Dimension.End.Column)
                        .Where(col => !string.IsNullOrWhiteSpace(worksheet.Cells[1, col].Text))
                        .ToDictionary(
                            col => worksheet.Cells[1, col].Text.Replace(" ", "").ToLower(),
                            col => col
                        );

                    for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                    {
                        foreach (var rule in templateRules)
                        {
                            string strippedColumnKey = rule.Definition.Name.Replace(" ", "").ToLower();

                            if (!columnMappings.ContainsKey(strippedColumnKey))
                                continue; // Skip if it's an optional column that wasn't provided

                            int colIndex = columnMappings[strippedColumnKey];

                            // Check both Text and Value to account for formula results or formatting
                            var cellText = worksheet.Cells[row, colIndex].Text?.Trim();
                            var cellValueStr = worksheet.Cells[row, colIndex].Value?.ToString()?.Trim() ?? "";

                            // Prefer raw value for validation if it exists, otherwise use text
                            var cellToValidate = string.IsNullOrWhiteSpace(cellValueStr) ? cellText : cellValueStr;
                            bool isEmpty = string.IsNullOrWhiteSpace(cellToValidate);

                            // ✅ 1. Check if Required
                            if (rule.IsValueRequired && isEmpty)
                            {
                                response.Success = false;
                                response.Message = $"Upload Failed: '{rule.Definition.Name}' at row {row} cannot be empty.";
                                return response;
                            }

                            // ✅ 2. Validate ItemStatus specifically via Enum
                            if (!isEmpty && rule.Definition.Name.Equals(MasterColumnCatalogue.ItemStatus.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                if (!Enum.TryParse<CatalogueItemStatus>(cellToValidate, true, out _))
                                {
                                    response.Success = false;
                                    response.Message = $"'ItemStatus' must be 'Active', 'Inactive', or 'Invalid'. Found: '{cellToValidate}'";
                                    return response;
                                }
                            }
                            // ✅ 3. Validate generic format/data type (only if the cell is not empty)
                            else if (!isEmpty)
                            {
                                bool isDataTypeValid = true;

                                switch (rule.Definition.DataType)
                                {
                                    case ExcelDataType.Int:
                                        isDataTypeValid = int.TryParse(cellToValidate, out _);
                                        break;
                                    case ExcelDataType.Decimal:
                                        isDataTypeValid = decimal.TryParse(cellToValidate, out _) || double.TryParse(cellToValidate, out _);
                                        break;
                                    case ExcelDataType.DateTime:
                                        isDataTypeValid = DateTime.TryParse(cellToValidate, out _) || double.TryParse(cellToValidate, out _); // Excel stores dates as doubles
                                        break;
                                    case ExcelDataType.Boolean:
                                        isDataTypeValid = bool.TryParse(cellToValidate, out _) || cellToValidate == "1" || cellToValidate == "0";
                                        break;
                                    case ExcelDataType.String:
                                    default:
                                        isDataTypeValid = true; // Strings are universally valid
                                        break;
                                }

                                if (!isDataTypeValid)
                                {
                                    response.Success = false;
                                    response.Message = $"Invalid value '{cellToValidate}' at row {row} in column '{rule.Definition.Name}'. Expected type: {rule.Definition.DataType}.";
                                    return response;
                                }
                            }
                        }
                    }

                    #endregion

                    response.Success = true;
                    response.Message = "File is valid.";
                    response.Data = requiredWorkSheetName;
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error processing file: {ex.Message}";
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

        #endregion validate excel file

        #region Read Excel
        public Response<(string Month, string Year)> PeekForecastProjectionDate(string filePath, string sheetName)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                using (var package = new ExcelPackage(fileInfo))
                {
                    var ws = package.Workbook.Worksheets[sheetName];
                    if (ws == null || ws.Dimension == null)
                        return new Response<(string, string)> { Success = false };

                    int monthCol = -1;
                    int yearCol = -1;

                    // Find the columns dynamically
                    for (int col = 1; col <= ws.Dimension.End.Column; col++)
                    {
                        var header = ws.Cells[1, col].Text?.Trim();
                        if (header == ForecastExcelColumns.ProjectionMonth.ToString()) monthCol = col;
                        if (header == ForecastExcelColumns.ProjectionYear.ToString()) yearCol = col;
                    }

                    if (monthCol == -1 || yearCol == -1)
                        return new Response<(string, string)> { Success = false };

                    // Grab the values from the first data row (Row 2)
                    string monthStr = ws.Cells[2, monthCol].Text?.Trim();
                    string yearStr = ws.Cells[2, yearCol].Text?.Trim();

                    return new Response<(string, string)> { Success = true, Data = (monthStr, yearStr) };
                }
            }
            catch
            {
                return new Response<(string, string)> { Success = false };
            }
        }

        public async Task<Response<List<DataTable>>> ReadCatalogDataTableFromExcel(string filePath, bool isUpdate = false)
        {
            var response = new Response<List<DataTable>>();
            try
            {

                var validationResponse = await ValidateItemCatalogueExcelFile(filePath, isUpdate);
                if (!validationResponse.Success)
                {
                    response.Success = false;
                    response.Message = validationResponse.Message;
                    return response;
                }

                string workSheetName = validationResponse.Data;
                Response<DataTable> resItemCatalogues = await new DataTableFactory().GetItemCataloguesDataTableFromExcel(filePath, workSheetName, _session.LoggedInUser.Id, isUpdate);
                if (resItemCatalogues.Success == false)
                {
                    response.Success = false;
                    response.Message = resItemCatalogues.Message;
                    return response;
                }

                Response<DataTable> resInitialStock = await new DataTableFactory().GetStockDataTableFromExcel(filePath, workSheetName, _session.LoggedInUser.Id, isUpdate);
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
                    var ws = package.Workbook.Worksheets.FirstOrDefault(w => w.Name.Equals("Wip", StringComparison.OrdinalIgnoreCase));
                    if (ws == null || ws.Dimension == null)
                        return new Response<List<WipDetail>> { Success = false, Message = "No worksheet named 'Wip' or no data found." };

                    int FindCol(string headerName)
                    {
                        for (int col = 1; col <= ws.Dimension.End.Column; col++)
                        {
                            var header = ws.Cells[1, col].Text?.Trim();
                            if (string.Equals(header, headerName, StringComparison.OrdinalIgnoreCase))
                                return col;
                        }
                        return -1;
                    }

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
                            continue;
                        }

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

      
        #region helpers 

        public string GetEnumValue(Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = (DescriptionAttribute)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
            return attribute == null ? value.ToString() : attribute.Description;
        }
        #endregion helpers   

    }
}