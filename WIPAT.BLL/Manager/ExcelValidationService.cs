using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WIPAT.Entities.ExcelTemplateDefinitions;
using WIPAT.DAL.Interfaces;
using WIPAT.Entities;
using WIPAT.Entities.Dto;
using WIPAT.Entities.Enum;

namespace WIPAT.BLL.Services
{
    public class ExcelValidationService
    {
        private readonly IItemsRepository _itemsRepository;
        private readonly WipSession _session;

        public ExcelValidationService(WipSession session, IItemsRepository itemsRepo)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _itemsRepository = itemsRepo ?? throw new ArgumentNullException(nameof(itemsRepo));
        }

        public async Task<Response<ExcelValidationResult>> ValidateAndLoadExcelAsync(
            string filePath,
            ImportExcelFileType fileType,
            string requiredWorkSheetName,
            IReadOnlyList<ColumnRule> requiredExcelColumns)
        {
            var response = new Response<ExcelValidationResult>
            {
                Data = new ExcelValidationResult(),
                Status = StatusType.Warning
            };

            #region 1. File Validation

            if (string.IsNullOrWhiteSpace(filePath))
                return BuildErrorResponse("File path cannot be empty.");

            if (!File.Exists(filePath))
                return BuildErrorResponse($"File does not exist at path: {filePath}");

            string fileExtension = Path.GetExtension(filePath).ToLower();

            if (fileExtension != ".xls" && fileExtension != ".xlsx")
                return BuildErrorResponse("Invalid file format. Only .xls or .xlsx allowed.");

            var errors = new List<string>();
            var result = new ExcelValidationResult();
            var ValidatedData = new DataTable();

            var uniqueMonths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var uniqueYears = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var collectedCasins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            #endregion

            try
            {
                await Task.Run(() =>
                {
                    using (var package = new ExcelPackage(new FileInfo(filePath)))
                    {
                        #region 2. Worksheet Validation
                        if (package.Workbook.Worksheets.Count == 0)
                        {
                            errors.Add("Workbook must contain at least one worksheet.");
                            return;
                        }

                        var ws = package.Workbook.Worksheets[requiredWorkSheetName];

                        if (ws == null)
                        {
                            errors.Add($"Worksheet '{requiredWorkSheetName}' not found.");
                            return;
                        }

                        int totalRows = ws.Dimension?.Rows ?? 0;
                        int totalCols = ws.Dimension?.Columns ?? 0;

                        if (totalRows < 2)
                        {
                            errors.Add($"Worksheet '{requiredWorkSheetName}' contains no data.");
                            return;
                        }
                        #endregion 2. Worksheet Validation

                        #region 3. Header Mapping & Validation

                        var headerLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                        for (int c = 1; c <= totalCols; c++)
                        {
                            var headerText = ws.Cells[1, c].Text?.Trim();
                            if (!string.IsNullOrEmpty(headerText) && !headerLookup.ContainsKey(headerText))
                            {
                                headerLookup[headerText] = c;
                            }
                        }

                        foreach (var rule in requiredExcelColumns)
                        {
                            bool exists = headerLookup.ContainsKey(rule.Definition.Name);

                            if (rule.IsHeaderRequired && !exists)
                            {
                                errors.Add($"Missing required header: '{rule.Definition.Name}'.");
                            }

                            ValidatedData.Columns.Add(rule.Definition.Name, typeof(string));
                        }

                        // Add ItemStatus column ONLY for Forecast and Order files
                        if (fileType == ImportExcelFileType.ForecastFile || fileType == ImportExcelFileType.OrderFile)
                        {
                            if (!ValidatedData.Columns.Contains("ItemStatus"))
                            {
                                ValidatedData.Columns.Add("ItemStatus", typeof(object));
                            }
                        }

                        if (errors.Any()) return;

                        #endregion

                        #region 4. Validate Rows

                        for (int r = 2; r <= totalRows; r++)
                        {
                            var dr = ValidatedData.NewRow();
                            bool isEmpty = true;

                            foreach (var rule in requiredExcelColumns)
                            {
                                if (!headerLookup.TryGetValue(rule.Definition.Name, out int colIndex))
                                {
                                    dr[rule.Definition.Name] = DBNull.Value;
                                    continue;
                                }

                                string value = ws.Cells[r, colIndex].Text?.Trim();
                                bool hasValue = !string.IsNullOrEmpty(value);

                                if (hasValue) isEmpty = false;

                                if (rule.IsValueRequired && !hasValue)
                                {
                                    errors.Add($"Row {r}: '{rule.Definition.Name}' is required.");
                                }

                                if (hasValue && !IsValidDataType(value, rule.Definition.DataType))
                                {
                                    errors.Add($"Row {r}: '{rule.Definition.Name}' has invalid data type. Expected {rule.Definition.DataType}.");
                                }

                                // --- Validation: Strict check for ItemStatus ---
                                if (rule.Definition.Name.Equals("ItemStatus", StringComparison.OrdinalIgnoreCase))
                                {
                                    string[] allowedStatuses = { "active", "inactive", "invalid" };
                                    if (!allowedStatuses.Contains(value.Trim().ToLower()))
                                    {
                                        errors.Add($"Row {r}: 'ItemStatus' must be 'Active', 'Inactive', or 'Invalid'. Found: '{value}'");
                                    }
                                }

                                dr[rule.Definition.Name] = value;

                                if (hasValue)
                                {
                                    string currentColumnName = rule.Definition.Name;

                                    if (currentColumnName.Equals(MasterColumnCatalogue.Casin.Name, StringComparison.OrdinalIgnoreCase))
                                    {
                                        collectedCasins.Add(value);
                                    }

                                    if (fileType == ImportExcelFileType.ForecastFile || fileType == ImportExcelFileType.OrderFile)
                                    {
                                        if (currentColumnName.Equals(MasterColumnCatalogue.MonthInteger.Name, StringComparison.OrdinalIgnoreCase) ||
                                            currentColumnName.Equals(MasterColumnCatalogue.ProjectionMonth.Name, StringComparison.OrdinalIgnoreCase))
                                        {
                                            uniqueMonths.Add(value);
                                        }

                                        if (currentColumnName.Equals(MasterColumnCatalogue.Year.Name, StringComparison.OrdinalIgnoreCase) ||
                                            currentColumnName.Equals(MasterColumnCatalogue.ProjectionYear.Name, StringComparison.OrdinalIgnoreCase))
                                        {
                                            uniqueYears.Add(value);
                                        }
                                    }
                                }
                            }

                            if (!isEmpty)
                            {
                                ValidatedData.Rows.Add(dr);
                            }
                        }

                        // --- VALIDATION: Ensure single projection month and year ---
                        if (uniqueMonths.Count > 1)
                        {
                            errors.Add($"Validation failed: Multiple projection months found ({string.Join(", ", uniqueMonths)}). The entire file must have the exact same projection month (e.g., all must be '{uniqueMonths.First()}').");
                        }

                        if (uniqueYears.Count > 1)
                        {
                            errors.Add($"Validation failed: Multiple projection years found ({string.Join(", ", uniqueYears)}). The entire file must have the exact same projection year (e.g., all must be '{uniqueYears.First()}').");
                        }
                        // -------------------------------------------------------------

                        #endregion
                    }
                });

                if (errors.Any())
                {
                    return BuildErrorResponse(errors);
                }

                #region 5. Consistency & DB Validation

                var dbCheckResponse = await ValidateItemsFromDbAsync(fileType, collectedCasins, filePath);

                if (!dbCheckResponse.Success)
                {
                    return dbCheckResponse;
                }

                // Append DB Item Statuses to ValidatedData ONLY for Forecast and Order files
                if (fileType == ImportExcelFileType.ForecastFile || fileType == ImportExcelFileType.OrderFile)
                {
                    if (dbCheckResponse.Data?.CasinStatuses != null &&
                        ValidatedData.Columns.Contains(MasterColumnCatalogue.Casin.Name) &&
                        ValidatedData.Columns.Contains("ItemStatus"))
                    {
                        foreach (DataRow row in ValidatedData.Rows)
                        {
                            string casinVal = row[MasterColumnCatalogue.Casin.Name]?.ToString();

                            if (!string.IsNullOrWhiteSpace(casinVal) &&
                                dbCheckResponse.Data.CasinStatuses.TryGetValue(casinVal.Trim(), out int? dbStatus) &&
                                dbStatus.HasValue)
                            {
                                row["ItemStatus"] = dbStatus.Value;
                            }
                            else
                            {
                                row["ItemStatus"] = DBNull.Value;
                            }
                        }
                    }
                }

                #endregion

                result.ValidatedData = ValidatedData;

                // Read from our updated structure within Data
                if (dbCheckResponse.Data != null)
                {
                    result.MissingCasins = dbCheckResponse.Data.MissingCasins ?? new List<string>();
                    result.InactiveCasins = dbCheckResponse.Data.InactiveCasins ?? new List<string>();
                    result.InvalidCasins = dbCheckResponse.Data.InvalidCasins ?? new List<string>();
                    result.ProblemItemsTable = dbCheckResponse.Data.ProblemItemsTable;
                    result.CasinStatuses = dbCheckResponse.Data.CasinStatuses;

                    // NEW: 1. Map the boolean flag to the final returned result
                    result.HasIgnoredInactiveOrInvalidItems = dbCheckResponse.Data.HasIgnoredInactiveOrInvalidItems;
                }

                return new Response<ExcelValidationResult>
                {
                    Success = true,
                    Status = StatusType.Success,
                    Data = result,
                    Message = dbCheckResponse.Message ?? "File validated successfully."
                };
            }
            catch (Exception ex)
            {
                return BuildErrorResponse("Exception occurred during validation: " + ex.Message);
            }
        }

        #region Helpers
        private bool IsValidDataType(string value, ExcelDataType type)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            string val = value.Trim();

            switch (type)
            {
                case ExcelDataType.String:
                    return true;
                case ExcelDataType.Int:
                    // Parse as int and ensure it is not negative
                    return int.TryParse(val, out int result) && result >= 0;
                case ExcelDataType.Decimal:
                    // Parse as decimals and ensure it is not negative
                    return decimal.TryParse(val, out decimal dResult) && dResult >= 0;
                case ExcelDataType.DateTime:
                    return DateTime.TryParse(val, out _) || double.TryParse(val, out _);
                case ExcelDataType.Boolean:
                    string bVal = val.ToLower();
                    return bVal == "true" || bVal == "false" || bVal == "1" || bVal == "0";
                default:
                    return false;
            }
        }

        private Response<ExcelValidationResult> BuildErrorResponse(List<string> errors)
        {
            return new Response<ExcelValidationResult>
            {
                Success = false,
                Status = StatusType.Error,
                Message = $"Validation failed with {errors.Count} error(s).",
                MissingItems = errors
            };
        }

        private Response<ExcelValidationResult> BuildErrorResponse(string error) => BuildErrorResponse(new List<string> { error });
        #endregion

        private async Task<Response<ExcelValidationResult>> ValidateItemsFromDbAsync(ImportExcelFileType fileType,IEnumerable<string> casinList,string filePath)
        {
            #region 1. Initialization & Deduplication

            var resultData = new ExcelValidationResult();
            var response = new Response<ExcelValidationResult>
            {
                Success = true,
                Status = StatusType.Success,
                Data = resultData,
                MissingItems = new List<string>(),
                DeactivatedItems = new List<string>()
            };

            // If it's not a Forecast or Order file, we don't need to validate CASINs against the DB
            if (fileType != ImportExcelFileType.ForecastFile && fileType != ImportExcelFileType.OrderFile)
            {
                return response;
            }

            var distinctCASINs = casinList
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            #endregion

            #region 2. Database Batch Verification

            var dbStatuses = await _itemsRepository.GetCasinStatusesBatchAsync(distinctCASINs);

            #endregion

            #region 3. Results Categorization & Mapping

            var missing = new List<string>();
            var inactive = new List<string>();
            var invalid = new List<string>();
            var casinStatuses = new Dictionary<string, int?>(StringComparer.OrdinalIgnoreCase);

            // Loop through our fast in-memory list instead of hitting the database
            foreach (var c in distinctCASINs)
            {
                if (dbStatuses.TryGetValue(c, out int status))
                {
                    casinStatuses[c] = status; // Map it for later injection

                    if (status == (int)CatalogueItemStatus.Inactive) // status = 0
                        inactive.Add(c);
                    else if (status == (int)CatalogueItemStatus.Invalid)  // status = 2
                        invalid.Add(c);
                }
                else
                {
                    // If the key wasn't in the returned dictionary, it doesn't exist in the DB
                    casinStatuses[c] = null;
                    missing.Add(c);
                }
            }

            resultData.CasinStatuses = casinStatuses; // Store map for the main Validation method

            #endregion

            #region 4. Problem Items Handling & User Prompts

            if (missing.Any() || inactive.Any() || invalid.Any())
            {
                DataTable problemTable = CreateProblemItemsDataTable(inactive, invalid, missing, filePath);

                // Update our specialized data class
                resultData.MissingCasins = missing;
                resultData.InactiveCasins = inactive;
                resultData.InvalidCasins = invalid;
                resultData.ProblemItemsTable = problemTable;

                // Update wrapper response to preserve backward compatibility for base class Error properties
                response.MissingItems = missing;
                response.DeactivatedItems = inactive.Concat(invalid).ToList();

                StringBuilder dialogText = new StringBuilder();
                dialogText.AppendLine("The following items need your attention:\n");

                if (missing.Any())
                {
                    dialogText.AppendLine($"Missing in Catalogue ({missing.Count}):");
                    dialogText.AppendLine(string.Join(", ", missing));
                    dialogText.AppendLine();
                }

                if (inactive.Any())
                {
                    dialogText.AppendLine($"Inactive in Catalogue ({inactive.Count}):");
                    dialogText.AppendLine(string.Join(", ", inactive));
                    dialogText.AppendLine();
                }

                if (invalid.Any())
                {
                    dialogText.AppendLine($"Invalid in Catalogue ({invalid.Count}):");
                    dialogText.AppendLine(string.Join(", ", invalid));
                    dialogText.AppendLine();
                }

                // --- 4a. Hard Fail on Missing Items ---
                if (missing.Any())
                {
                    dialogText.AppendLine("Process cancelled. You must add the missing items to the catalogue first.");

                    MessageBox.Show(dialogText.ToString(),
                        "Problem Items - Action Required",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    response.Success = false;
                    response.Status = StatusType.Error;
                    response.Message = $"Process cancelled. Found {missing.Count} missing item(s).";
                    return response;
                }

                // --- 4b. Handle Inactive and Invalid Items Based on File Type ---
                if (inactive.Any() || invalid.Any())
                {
                    int totalProblematic = inactive.Count + invalid.Count;

                    if (fileType == ImportExcelFileType.ForecastFile)
                    {
                        dialogText.AppendLine("Do you want to create the WIP and ignore calculating WIP for these CASINs?\n");
                        dialogText.AppendLine("• Click 'Yes' to ignore them and continue.");
                        dialogText.AppendLine("• Click 'No' to cancel.");

                        DialogResult result = MessageBox.Show(dialogText.ToString(),
                            "Problem Items - Action Required",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning);

                        if (result == DialogResult.No)
                        {
                            response.Success = false;
                            response.Status = StatusType.Error;
                            response.Message = $"Process cancelled by user. Found {totalProblematic} inactive/invalid items.";
                        }
                        else
                        {
                            // User opted to ignore: Set flag to true to inform the calling method
                            resultData.HasIgnoredInactiveOrInvalidItems = true;
                            response.Message = $"File validated successfully (Ignored {totalProblematic} inactive/invalid CASINs).";
                        }
                    }
                    else
                    {
                        dialogText.AppendLine("Process cancelled. Order files cannot contain inactive or invalid items.");

                        MessageBox.Show(dialogText.ToString(),
                            "Problem Items - Action Required",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);

                        response.Success = false;
                        response.Status = StatusType.Error;
                        response.Message = $"Process cancelled. Found {totalProblematic} inactive/invalid item(s).";
                    }
                }
            }

            #endregion

            return response;
        }

        private DataTable CreateProblemItemsDataTable(List<string> inactiveItems, List<string> invalidItems, List<string> missingItems, string filePath)
        {
            DataTable dt = new DataTable("ProblemItems");

            const string FileNameColumn = "FileName";
            const string ReasonColumn = "Reason";

            dt.Columns.Add(MasterColumnCatalogue.Casin.Name, typeof(string));
            dt.Columns.Add(FileNameColumn, typeof(string));
            dt.Columns.Add(ReasonColumn, typeof(string));

            string fileName = Path.GetFileName(filePath);

            if (inactiveItems != null)
            {
                foreach (var casin in inactiveItems)
                {
                    dt.Rows.Add(casin, fileName, "Inactive");
                }
            }

            if (invalidItems != null)
            {
                foreach (var casin in invalidItems)
                {
                    dt.Rows.Add(casin, fileName, "Invalid");
                }
            }

            if (missingItems != null)
            {
                foreach (var casin in missingItems)
                {
                    dt.Rows.Add(casin, fileName, "Missing");
                }
            }

            return dt;
        }
        
        public class ExcelValidationResult
        {
            public DataTable ValidatedData { get; set; }
            public List<string> MissingCasins { get; set; } = new List<string>();
            public List<string> InactiveCasins { get; set; } = new List<string>();
            public List<string> InvalidCasins { get; set; } = new List<string>();
            public DataTable ProblemItemsTable { get; set; }
            public Dictionary<string, int?> CasinStatuses { get; set; } = new Dictionary<string, int?>(StringComparer.OrdinalIgnoreCase);
            public bool HasIgnoredInactiveOrInvalidItems { get; set; }
        }

        
    }
}