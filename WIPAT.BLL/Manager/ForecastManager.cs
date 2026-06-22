using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WIPAT.BLL.Interfaces;
using WIPAT.DAL.Interfaces;
using WIPAT.Entities;
using WIPAT.Entities.Dto;
using WIPAT.Entities.Enum;
using WIPAT.Entities.ExcelTemplateDefinitions;

namespace WIPAT.BLL.Managers
{
    public class ForecastManager : IForecastManager
    {
        private readonly IForecastRepository _forecastRepository;
        private readonly IItemsRepository _itemsRepository;
        private readonly IExcelService _excelService;

        // Optional: Cache the last successfully previewed file data to avoid re-reading/re-validating on final save
        private ForecastFileData _lastPreviewedData = null;
        private readonly WipSession _session;

        public ForecastManager(
            IForecastRepository forecastRepository,
            IItemsRepository itemsRepository,
            IExcelService excelService,
            WipSession session)
        {
            _forecastRepository = forecastRepository ?? throw new ArgumentNullException(nameof(forecastRepository));
            _itemsRepository = itemsRepository ?? throw new ArgumentNullException(nameof(itemsRepository));
            _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        #region Public Methods

        public async Task<Response<List<ForecastFileData>>> HandleForecastFileAsync(string filePath, List<ForecastFileData> currentSessionFiles, int commitmentPeriod, WipSession session)
        {
            var response = new Response<List<ForecastFileData>>();
            string fileName = Path.GetFileName(filePath);
            bool isFirstFile = currentSessionFiles.Count == 0;

            try
            {
                if (currentSessionFiles.Count >= 4)
                {
                    return new Response<List<ForecastFileData>> { Success = false, Message = "Maximum 4 forecast files allowed." };
                }

                if (currentSessionFiles.Any(f => f.FileName == fileName))
                {
                    return new Response<List<ForecastFileData>> { Success = false, Message = $"File '{fileName}' is already in the current session." };
                }

                // Declare the variable here so it is accessible throughout the method
                bool isContinueWithInactive = false;

                // Use cached preview data if it matches the current file path to avoid duplicate validation/reading
                ForecastFileData newForecastData;

                if (_lastPreviewedData != null && _lastPreviewedData.FilePath == filePath)
                {
                    newForecastData = _lastPreviewedData;
                    isContinueWithInactive = newForecastData.IsContinueWithInactiveItems;
                    // Clear cache after consumption
                    _lastPreviewedData = null;
                }
                else
                {
                    // Fallback execution if preview was skipped
                    var importResponse = await ProcessForecastFileInternal(filePath, commitmentPeriod, isFirstFile);
                    if (!importResponse.Success)
                    {
                        return new Response<List<ForecastFileData>> { Success = false, Message = importResponse.Message };
                    }
                    newForecastData = importResponse.Data;
                    isContinueWithInactive = importResponse.Data.IsContinueWithInactiveItems;
                }

                if (currentSessionFiles.Any(f => f.ProjectionMonth == newForecastData.ProjectionMonth && f.ProjectionYear == newForecastData.ProjectionYear))
                    return new Response<List<ForecastFileData>> { Success = false, Message = $"A file for {newForecastData.ProjectionMonth}/{newForecastData.ProjectionYear} is already in the current session." };

                var dbCheck = _forecastRepository.PerformForecastChecks2(newForecastData.FileName, newForecastData.ProjectionMonth, newForecastData.ProjectionYear);

                if (dbCheck.Success)
                {
                    var saveResponse = _forecastRepository.SaveForecastDataToDatabase(newForecastData, isFirstFile, isContinueWithInactive, _session.LoggedInUser.Id);

                    if (!saveResponse.Success)
                    {
                        return new Response<List<ForecastFileData>> { Success = false, Message = saveResponse.Message };
                    }

                    var refreshedData = _forecastRepository.GetForecastDataFromDB(newForecastData.ProjectionMonth, newForecastData.ProjectionYear);
                    if (refreshedData.Success)
                    {
                        newForecastData.Forecast = refreshedData.Data.Item2;
                    }

                    currentSessionFiles.Add(newForecastData);
                    response.Success = true;
                    response.Message = "Forecast file imported and saved successfully.";
                    response.IsContinueWithInactiveItems = isContinueWithInactive;
                }
                else
                {
                    if (dbCheck.Data?.FileData?.FullTable != null)
                    {
                        newForecastData.FullTable = dbCheck.Data.FileData.FullTable;
                        newForecastData.Forecast = dbCheck.Data.FileData.Forecast;
                        newForecastData.IsWipAlreadyCalculated = true;

                        currentSessionFiles.Add(newForecastData);

                        response.Success = true;
                        response.Message = $"⚠️ Data for {newForecastData.ProjectionMonth} {newForecastData.ProjectionYear} already exists.\nLoaded from Database.";
                    }
                    else
                    {
                        return new Response<List<ForecastFileData>> { Success = false, Message = "Data exists but failed to load from DB." };
                    }
                }

                response.Data = currentSessionFiles;
                return response;
            }
            catch (Exception ex)
            {
                return new Response<List<ForecastFileData>> { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<Response<ForecastFileData>> GetForecastFilePreviewAsync(string filePath, int commitmentPeriod)
        {
            string fileName = Path.GetFileName(filePath);
            string sheetName = _excelService.GetEnumValue(ExcelSheetNames.Forecast);

            // 1. FAST PEEK & DB CHECK (Bypass heavy validation if already in DB)
            var peekRes = await Task.Run(() => _excelService.PeekForecastProjectionDate(filePath, sheetName));

            if (peekRes.Success)
            {
                if (int.TryParse(peekRes.Data.Month, out int pMonth) && int.TryParse(peekRes.Data.Year, out int pYear))
                {
                    DateTime projDate = new DateTime(pYear, pMonth, 1);
                    string monthName = projDate.ToString("MMMM");
                    string yearStr = projDate.ToString("yyyy");

                    var dbCheck = _forecastRepository.PerformForecastChecks2(fileName, monthName, yearStr);

                    // In your repository, Success = false means the record ALREADY EXISTS
                    if (!dbCheck.Success && dbCheck.Data != null && (dbCheck.Data.FileExists || dbCheck.Data.ProjectionExists))
                    {
                        // Bypass validation completely. Create a dummy object for HandleForecastFileAsync.
                        var existingFileData = new ForecastFileData
                        {
                            FileName = fileName,
                            FilePath = filePath,
                            ProjectionMonth = monthName,
                            ProjectionYear = yearStr
                        };

                        _lastPreviewedData = existingFileData; // Cache it so HandleForecastFileAsync catches it

                        return new Response<ForecastFileData>
                        {
                            Success = true, // Force success so the UI moves to HandleForecastFileAsync seamlessly
                            Message = dbCheck.Message,
                            Data = existingFileData
                        };
                    }
                }
            }

            // 2. HEAVY VALIDATION (Only runs if the file is genuinely new)
            var res = await ProcessForecastFileInternal(filePath, commitmentPeriod, true);
            if (res.Success)
            {
                _lastPreviewedData = res.Data;
            }
            return res;
        }

        public async Task<Response<ForecastFileData>> LoadExistingForecastAsync(string month, string year)
        {
            return await Task.Run(() =>
            {
                var response = new Response<ForecastFileData>();
                var dbResult = _forecastRepository.GetForecastDataFromDB(month, year);

                if (!dbResult.Success)
                {
                    response.Success = false;
                    response.Message = dbResult.Message;
                    return response;
                }

                response.Success = true;
                response.Data = new ForecastFileData
                {
                    FileName = $"DB_{month}_{year}",
                    FilePath = "Database Source",
                    ProjectionMonth = month,
                    ProjectionYear = year,
                    FullTable = dbResult.Data.Item1,
                    FilteredTable = dbResult.Data.Item1,
                    Forecast = dbResult.Data.Item2,
                    ForecastFor = dbResult.Data.Item2?.ForecastingFor ?? $"{month} {year}",
                    IsWipAlreadyCalculated = dbResult.Data.Item2.IsWipCalculated,
                    IsContinueWithInactiveItems = dbResult.Data.Item2.IsContinueWithInactiveItems
                };

                return response;
            });
        }

        #endregion

        #region Private Helper Methods (The Core Logic)

        private async Task<Response<ForecastFileData>> ProcessForecastFileInternal(string filePath, int commitmentPeriod, bool isFirstFile)
        {
            var response = new Response<ForecastFileData>();

            // 1. Get the list of ColumnRule objects
            var requiredExcelColumns = FileTemplateFactory.GetImportTemplate(ImportExcelFileType.ForecastFile);

            // Define the columns you want to ignore for validation
            var excludedColumns = new[] { "Month", "Year", "ItemStatus" };

            // Extract the Name, filter out the unwanted ones, and convert to a List<string>
            List<string> requiredColumns = requiredExcelColumns
                .Select(rule => rule.Definition.Name)
                .Where(name => !excludedColumns.Contains(name))
                .ToList();

            // 3. Determine worksheet name dynamically based on App.config keys
            string sheetName = ConfigurationManager.AppSettings["ForecastWorksheetName"] ?? "Vendor Central Excel Output";

            // 4. validate the excel file
            var valRes = await _excelService.ValidateExcelFile(filePath, FileType.Forecast.ToString(), sheetName, requiredColumns);
            if (!valRes.Success)
            {
                response.Success = false;
                response.Message = valRes.Message;
                response.Data = new ForecastFileData
                {
                    DeactivatedItems = valRes.DeactivatedItems,
                    MissingItems = valRes.MissingItems,
                    ProblemItemsTable = valRes.ProblemItemsTable
                };
                return response;
            }

            var readRes = new DataTableFactory().ReadExcelToDataTable(filePath, sheetName, requiredColumns);
            if (!readRes.Success)
            {
                return new Response<ForecastFileData> { Success = false, Message = readRes.Message };
            }

            DataTable rawTable = readRes.Data;

            if (rawTable.Rows.Count == 0)
            {
                return new Response<ForecastFileData> { Success = false, Message = "File is empty." };
            }

            // 5. Fetch DB Items for Lookup
            var allDbItemsRes = await _itemsRepository.GetActiveItemCatalogues(true);
            var allDbItems = allDbItemsRes.Success && allDbItemsRes.Data != null
                ? allDbItemsRes.Data.GroupBy(x => x.Casin.Trim(), StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, ItemCatalogue>(StringComparer.OrdinalIgnoreCase);

            // 6. Header Validation & Meta Data Extraction
            string projMonthStr = rawTable.Rows[0][MasterColumnCatalogue.ProjectionMonth.Name]?.ToString();
            string projYearStr = rawTable.Rows[0][MasterColumnCatalogue.ProjectionYear.Name]?.ToString();

            if (!int.TryParse(projMonthStr, out int pMonth) || !int.TryParse(projYearStr, out int pYear))
            {
                return new Response<ForecastFileData> { Success = false, Message = "Invalid Projection Month/Year in file header." };
            }

            DateTime projectionDate = new DateTime(pYear, pMonth, 1);
            string ProjectionMonth = projectionDate.ToString("MMMM");
            string ProjectionYear = projectionDate.ToString("yyyy");
            string forecastFor = projectionDate.AddMonths(commitmentPeriod + 1).ToString("MMMM yyyy");
            
            // 7. Get Processed DataTable via Factory
            var processedTableResponse = new DataTableFactory().CreateProcessedForecastTable(rawTable, requiredColumns, allDbItems);

            if (!processedTableResponse.Success)
            {
                return new Response<ForecastFileData> { Success = false, Message = processedTableResponse.Message };
            }

            // Extract the actual DataTable to proceed with your logic
            var processedTable = processedTableResponse.Data;

            // 8. Generate Missing Items
            await GenerateMissingItemsAsync(processedTable, commitmentPeriod, isFirstFile, valRes.IsContinueWithInactiveItems);

            response.Success = true;
            response.Data = new ForecastFileData
            {
                FileName = Path.GetFileName(filePath),
                FilePath = filePath,
                FullTable = processedTable,
                FilteredTable = processedTable.Copy(),
                ProjectionMonth = ProjectionMonth,
                ProjectionYear = ProjectionYear,
                ForecastFor = forecastFor,
                IsContinueWithInactiveItems = valRes.IsContinueWithInactiveItems,
                ProblemItemsTable = valRes.ProblemItemsTable,
            };

            return response;
        }

        private async Task GenerateMissingItemsAsync(DataTable table, int commitmentPeriod, bool isFirstFile, bool includeInactiveItems)
        {
            var distinctSchedules = table.AsEnumerable()
                .Select(r => new
                {
                    Period = r[MasterColumnCatalogue.CommitmentPeriod.Name].ToString(),
                    PODate = r[MasterColumnCatalogue.PODate.Name].ToString(),
                    Month = r[MasterColumnCatalogue.MonthString.Name].ToString(),
                    Year = r[MasterColumnCatalogue.Year.Name].ToString()
                })
                .Distinct()
                .OrderBy(x => int.TryParse(x.Period, out int p) ? p : 999)
                .ToList();

            if (!distinctSchedules.Any()) return;

            var resItems = await _itemsRepository.GetActiveItemCatalogues(includeInactiveItems);
            if (!resItems.Success || resItems.Data == null) return;

            var dbCasinDict = resItems.Data
                .GroupBy(x => x.Casin.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var fileCasins = table.AsEnumerable()
                .Select(r => r[MasterColumnCatalogue.Casin.Name].ToString().Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var missingCasins = dbCasinDict.Keys.Except(fileCasins, StringComparer.OrdinalIgnoreCase).ToList();

            foreach (var missingCasin in missingCasins)
            {
                int periodIndex = 0;
                foreach (var schedule in distinctSchedules)
                {
                    var newRow = table.NewRow();
                    newRow[MasterColumnCatalogue.Casin.Name] = missingCasin;
                    newRow[MasterColumnCatalogue.RequestedQuantity.Name] = "0";
                    newRow[MasterColumnCatalogue.CommitmentPeriod.Name] = schedule.Period;
                    newRow[MasterColumnCatalogue.PODate.Name] = schedule.PODate;
                    newRow[MasterColumnCatalogue.MonthString.Name] = schedule.Month;
                    newRow[MasterColumnCatalogue.Year.Name] = schedule.Year;

                    var dbItem = dbCasinDict[missingCasin];
                    newRow[MasterColumnCatalogue.ItemStatus.Name] = dbItem.ItemStatus;

                    newRow[MasterColumnCatalogue.ProjectionMonth.Name] = "";
                    newRow[MasterColumnCatalogue.ProjectionYear.Name] = "";

                    table.Rows.Add(newRow);
                    periodIndex++;
                }
            }
        }
        
        #endregion
    }
}