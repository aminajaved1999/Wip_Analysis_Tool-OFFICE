using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WIPAT.BLL.Manager.ExcelTemplateDefinitions;
using WIPAT.DAL.Interfaces;
using WIPAT.Entities;
using WIPAT.Entities.Dto;
using WIPAT.Entities.Enum;

namespace WIPAT.BLL.Services
{
    public class ExcelValidationService
    {
        private readonly IItemsRepository _itemsRepository;
        public ExcelValidationService(WipSession session, IItemsRepository itemsRepo)
        {
            _itemsRepository = itemsRepo ?? throw new ArgumentNullException(nameof(itemsRepo));
        }

        public async Task<Response<ExcelValidationResult>> ValidateAndLoadExcelAsync(string filePath, ExcelFileType fileType, string requiredWorkSheetName, IReadOnlyList<ColumnRule> requiredExcelColumns, Func<int, bool> confirmIgnoreInactive)
        {
            var response = new Response<ExcelValidationResult>
            {
                Data = new ExcelValidationResult(),
                Status = StatusType.Warning
            };

            #region 1. File Validation

            if (string.IsNullOrWhiteSpace(filePath))
            {
                return BuildErrorResponse("File path cannot be empty.");
            }

            if (!File.Exists(filePath))
            {
                return BuildErrorResponse($"File does not exist at path: {filePath}");
            }

            string fileExtension = Path.GetExtension(filePath).ToLower();

            if (fileExtension != ".xls" && fileExtension != ".xlsx")
            {
                return BuildErrorResponse("Invalid file format. Only .xls or .xlsx allowed.");
            }

            var errors = new List<string>();
            var result = new ExcelValidationResult();
            var dt = new DataTable();

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

                        #region 2. Header Mapping & Validation

                        var headerLookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                        for (int c = 1; c <= totalCols; c++)
                        {
                            var headerText = ws.Cells[1, c].Text?.Trim();

                            if (!string.IsNullOrEmpty(headerText))
                            {
                                if (!headerLookup.ContainsKey(headerText))
                                {
                                    headerLookup[headerText] = c;
                                }
                            }
                        }

                        foreach (var rule in requiredExcelColumns)
                        {
                            bool exists = headerLookup.ContainsKey(rule.Definition.Name);

                            if (rule.IsHeaderRequired)
                            {
                                if (!exists)
                                {
                                    errors.Add($"Missing required header: '{rule.Definition.Name}'.");
                                }
                            }

                            dt.Columns.Add(rule.Definition.Name, typeof(string));
                        }

                        if (errors.Any())
                        {
                            return;
                        }

                        #endregion

                        #region 3. Validate Rows

                        for (int r = 2; r <= totalRows; r++)
                        {
                            var dr = dt.NewRow();
                            bool isEmpty = true;

                            foreach (var rule in requiredExcelColumns)
                            {
                                if (!headerLookup.TryGetValue(rule.Definition.Name, out int colIndex))
                                {
                                    dr[rule.Definition.Name] = DBNull.Value;
                                    continue;
                                }

                                string value = ws.Cells[r, colIndex].Text?.Trim();

                                if (!string.IsNullOrEmpty(value))
                                {
                                    isEmpty = false;
                                }

                                if (rule.IsValueRequired)
                                {
                                    if (string.IsNullOrEmpty(value))
                                    {
                                        errors.Add($"Row {r}: '{rule.Definition.Name}' is required.");
                                    }
                                }

                                if (!string.IsNullOrEmpty(value))
                                {
                                    if (!IsValidDataType(value, rule.Definition.DataType))
                                    {
                                        errors.Add($"Row {r}: '{rule.Definition.Name}' has invalid data type.");
                                    }
                                }

                                dr[rule.Definition.Name] = value;

                                if (!string.IsNullOrEmpty(value))
                                {
                                    string colName = rule.Definition.Name.ToLower();

                                    if (colName == "casin")
                                    {
                                        collectedCasins.Add(value);
                                    }

                                    if (fileType == ExcelFileType.ForecastFile || fileType == ExcelFileType.OrderFile)
                                    {
                                        if (colName.Contains("month"))
                                        {
                                            uniqueMonths.Add(value);
                                        }

                                        if (colName.Contains("year"))
                                        {
                                            uniqueYears.Add(value);
                                        }
                                    }
                                }
                            }

                            if (!isEmpty)
                            {
                                dt.Rows.Add(dr);
                            }
                        }

                        #endregion
                    }
                });

                if (errors.Any())
                {
                    return BuildErrorResponse(errors);
                }

                #region 4. Consistency & DB Validation

                var DBCHECK = await ValidateCatalogueItemsInDB(fileType.ToString(),collectedCasins, filePath );

                #endregion

                result.Data = dt;

                return new Response<ExcelValidationResult>
                {
                    Success = true,
                    Status = StatusType.Success,
                    Data = result
                };
            }
            catch (Exception ex)
            {
                return BuildErrorResponse("Exception occurred: " + ex.Message);
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
                    return int.TryParse(val, out _);
                case ExcelDataType.Decimal:
                    return decimal.TryParse(val, out _);
                case ExcelDataType.DateTime:
                    // Explicitly allow OADate doubles (Excel format) or standard string dates
                    return DateTime.TryParse(val, out _) || double.TryParse(val, out _);
                case ExcelDataType.Boolean:
                    string bVal = val.ToLower();
                    // Covers "True", "False", "1", "0"
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
                Message = $"Validation failed: {errors.Count} error(s).",
                MissingItems = errors
            };
        }

        private Response<ExcelValidationResult> BuildErrorResponse(string error) => BuildErrorResponse(new List<string> { error });
        #endregion

        private async Task<Response<ExcelValidationResult>> ValidateCatalogueItemsInDB(string fileType, IEnumerable<string> casinList, string filePath)
        {
            var response = new Response<ExcelValidationResult>
            {
                Data = new ExcelValidationResult(),
                Status = StatusType.Warning
            };

            if (fileType == ExcelFileType.ForecastFile.ToString() ||
                fileType == ExcelFileType.OrderFile.ToString())
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
                    int? status = await _itemsRepository.CheckCAsinStatus(c);

                    if (status == null)
                        missing.Add(c);
                    else if (status == (int)CatalogueItemStatus.Inactive ||
                             status == (int)CatalogueItemStatus.Invalid)
                        deactivated.Add(c);
                }

                response.MissingItems = missing;
                response.DeactivatedItems = deactivated;

                if (missing.Any() || deactivated.Any())
                {
                    DataTable problemTable = CreateProblemItemsDataTable(missing, deactivated, filePath);

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

                    if (missing.Any())
                    {
                        dialogText.AppendLine("Process cancelled. You must add the missing items to the catalogue first.");

                        MessageBox.Show(dialogText.ToString(),
                            "Problem Items - Action Required",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);

                        var errorResponse = BuildErrorResponse(
                            $"Process cancelled. Found {missing.Count} missing item(s).");

                        errorResponse.MissingItems = missing;
                        errorResponse.DeactivatedItems = deactivated;
                        errorResponse.ProblemItemsTable = problemTable;

                        return errorResponse;
                    }
                    else
                    {
                        if (fileType == FileType.Forecast.ToString())
                        {
                            dialogText.AppendLine(
                                "Do you want to create the WIP and ignore calculating WIP for these CASINs?\n");
                            dialogText.AppendLine("• Click 'Yes' to ignore them and continue.");
                            dialogText.AppendLine("• Click 'No' to cancel.");

                            DialogResult result = MessageBox.Show(dialogText.ToString(),
                                "Problem Items - Action Required",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Warning);

                            if (result == DialogResult.No)
                            {
                                var errorResponse = BuildErrorResponse(
                                    $"Process cancelled. Found {deactivated.Count} inactive items.");

                                errorResponse.MissingItems = missing;
                                errorResponse.DeactivatedItems = deactivated;
                                errorResponse.ProblemItemsTable = problemTable;

                                return errorResponse;
                            }

                            response.Message =
                                $"File validated successfully (Ignored {deactivated.Count} inactive CASINs).";

                            response.IsContinueWithInactiveItems = true;
                        }
                        else
                        {
                            string message =
                                $"Process cancelled. Found {deactivated.Count} inactive item(s).";

                            dialogText.AppendLine(message);

                            MessageBox.Show(dialogText.ToString(),
                                "Problem Items - Action Required",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);

                            var errorResponse = BuildErrorResponse(message);

                            errorResponse.MissingItems = missing;
                            errorResponse.DeactivatedItems = deactivated;
                            errorResponse.ProblemItemsTable = problemTable;

                            return errorResponse;
                        }
                    }
                }
            }

            return response;
        }

        public class ExcelValidationResult
        {
            public DataTable Data { get; set; }
            public List<string> MissingCasins { get; set; } = new List<string>();
            public List<string> InactiveCasins { get; set; } = new List<string>();
            public DataTable ProblemItemsTable { get; set; }
        }

        private DataTable CreateProblemItemsDataTable(List<string> inactiveItems, List<string> MissingItems, string filePath)
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

            if (MissingItems != null)
            {
                foreach (var casin in MissingItems)
                {
                    dt.Rows.Add(casin, fileName, "Missing");
                }
            }

            return dt;
        }
    }
}