using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WIPAT.BLL.Interfaces;
using WIPAT.DAL.Interfaces;
using WIPAT.Entities;
using WIPAT.Entities.Dto;
using WIPAT.Entities.Enum;

namespace WIPAT.BLL.Managers
{
    public class ForecastManager : IForecastManager
    {
        private readonly IForecastRepository _forecastRepository;
        private readonly IItemsRepository _itemsRepository;
        private readonly IExcelService _excelService; 

        public ForecastManager(
            IForecastRepository forecastRepository,
            IItemsRepository itemsRepository,
            IExcelService excelService)
        {
            _forecastRepository = forecastRepository ?? throw new ArgumentNullException(nameof(forecastRepository));
            _itemsRepository = itemsRepository ?? throw new ArgumentNullException(nameof(itemsRepository));
            _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));
        }

        #region Public Methods

        public async Task<Response<List<ForecastFileData>>> HandleForecastFileAsync(string filePath, List<ForecastFileData> currentSessionFiles, int commitmentPeriod, WipSession session)
        {
            var response = new Response<List<ForecastFileData>>();
            string fileName = Path.GetFileName(filePath);
            bool isFirstFile = currentSessionFiles.Count == 0;

            try
            {
                #region 1. Validation Logic
                if (currentSessionFiles.Count >= 4)
                {
                    return new Response<List<ForecastFileData>> { Success = false, Message = "Maximum 4 forecast files allowed." };
                }

                if (currentSessionFiles.Any(f => f.FileName == fileName))
                {
                    return new Response<List<ForecastFileData>> { Success = false, Message = $"File '{fileName}' is already in the current session." };
                }
                #endregion

                #region 2. Process File
                var importResponse = await ProcessForecastFile(filePath, commitmentPeriod, isFirstFile);

                if (!importResponse.Success)
                {
                    return new Response<List<ForecastFileData>> { Success = false, Message = importResponse.Message };
                }

                var newForecastData = importResponse.Data;
                #endregion

                #region 3. Check Session Duplicates (by Date)
                if (currentSessionFiles.Any(f => f.ProjectionMonth == newForecastData.ProjectionMonth && f.ProjectionYear == newForecastData.ProjectionYear))
                    return new Response<List<ForecastFileData>> { Success = false, Message = $"A file for {newForecastData.ProjectionMonth}/{newForecastData.ProjectionYear} is already in the current session." };
                #endregion

                #region 4. Database Checks & Saving
                var dbCheck = _forecastRepository.PerformForecastChecks2(newForecastData.FileName, newForecastData.ProjectionMonth, newForecastData.ProjectionYear);

                if (dbCheck.Success)
                {
                    // Case: NEW DATA -> Save to DB
                    var saveResponse = _forecastRepository.SaveForecastDataToDatabase(newForecastData, isFirstFile);
                    if (!saveResponse.Success)
                        return new Response<List<ForecastFileData>> { Success = false, Message = saveResponse.Message };

                    // Fetch back to ensure consistency
                    var refreshedData = _forecastRepository.GetForecastDataFromDB(newForecastData.ProjectionMonth, newForecastData.ProjectionYear);
                    if (refreshedData.Success)
                    {
                        newForecastData.Forecast = refreshedData.Data.Item2;
                    }

                    currentSessionFiles.Add(newForecastData);
                    response.Success = true;
                    response.Message = "Forecast file imported and saved successfully.";
                }
                else
                {
                    // Case: DATA EXISTS -> Use DB Data
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
                #endregion

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
            // Reuse the same processing logic, just treat it as 'First File' for preview purposes
            return await ProcessForecastFile(filePath, commitmentPeriod, true);
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
                    IsWipAlreadyCalculated = dbResult.Data.Item2.IsWipCalculated
                };

                return response;
            });
        }

        #endregion

        #region Private Helper Methods (The Core Logic)

        private async Task<Response<ForecastFileData>> ProcessForecastFile(string filePath, int commitmentPeriod, bool isFirstFile)
        {
            var response = new Response<ForecastFileData>();

            // 1. Define Columns
            var requiredColumns = new List<string>
            {
                _excelService.GetEnumValue(ForecastExcelColumns.CASIN),
                _excelService.GetEnumValue(ForecastExcelColumns.Requested_Quantity),
                _excelService.GetEnumValue(ForecastExcelColumns.Commitment_Period),
                _excelService.GetEnumValue(ForecastExcelColumns.PO_Date),
                ForecastExcelColumns.ProjectionMonth.ToString(),
                ForecastExcelColumns.ProjectionYear.ToString()
            };

            // 2. Delegate Excel Reading to Service
            string sheetName = _excelService.GetEnumValue(ExcelSheetNames.Forecast);

            // Validation
            var valRes = await _excelService.ValidateExcelFile(filePath, FileType.Forecast.ToString(), sheetName, requiredColumns);
            if (!valRes.Success)
            {
                return new Response<ForecastFileData> { Success = false, Message = valRes.Message };
            }

            // Reading
            //var readRes = _excelService.ReadForecastExcelToDataTable(filePath, sheetName);
            var readRes = _excelService.ReadExcelToDataTable(filePath, sheetName, requiredColumns);
            if (!readRes.Success)
            {
                return new Response<ForecastFileData> { Success = false, Message = readRes.Message };
            }


            DataTable rawTable = readRes.Data;

            // 3. Transform Data (Replaces the loop inside ImportForecastFile)
            var processedTable = new DataTable();
            foreach (var col in requiredColumns) processedTable.Columns.Add(col);
            processedTable.Columns.Add("Month");
            processedTable.Columns.Add("Year");
            processedTable.Columns.Add("Wip");
            processedTable.Columns.Add("IsSystemGenerated", typeof(bool));

            // To track Commitment Period logic
            var asinRowIndex = new Dictionary<string, int>();

            // Get Projection Date from the first row of data (Assuming Row 2 in Excel = Row 0 in DataTable)
            if (rawTable.Rows.Count == 0)
            {
                return new Response<ForecastFileData> { Success = false, Message = "File is empty." };
            }

            string projMonthStr = rawTable.Rows[0][ForecastExcelColumns.ProjectionMonth.ToString()]?.ToString();
            string projYearStr = rawTable.Rows[0][ForecastExcelColumns.ProjectionYear.ToString()]?.ToString();

            if (!int.TryParse(projMonthStr, out int pMonth) || !int.TryParse(projYearStr, out int pYear))
            {
                return new Response<ForecastFileData> { Success = false, Message = "Invalid Projection Month/Year in file header." };
            }

            DateTime projectionDate = new DateTime(pYear, pMonth, 1);
            string ProjectionMonth = projectionDate.ToString("MMMM");
            string ProjectionYear = projectionDate.ToString("yyyy");
            string forecastFor = projectionDate.AddMonths(commitmentPeriod + 1).ToString("MMMM yyyy");

            // 4. Loop Rows & Parse
            foreach (DataRow row in rawTable.Rows)
            {
                string casin = row[_excelService.GetEnumValue(ForecastExcelColumns.CASIN)].ToString().Trim();
                if (string.IsNullOrEmpty(casin))
                {
                    continue;
                }


                var newRow = processedTable.NewRow();

                // Copy basic cols
                foreach (var col in requiredColumns)
                {
                    newRow[col] = row[col];
                }

                // Parse PO Date for Month/Year
                if (DateTime.TryParse(row[_excelService.GetEnumValue(ForecastExcelColumns.PO_Date)].ToString(), out DateTime poDate))
                {
                    newRow["Month"] = poDate.ToString("MMMM");
                    newRow["Year"] = poDate.Year.ToString();
                }
                else
                {
                    newRow["Month"] = "Invalid Date";
                    newRow["Year"] = "";
                }

                newRow["IsSystemGenerated"] = true;

                // WIP Calculation Logic
                if (!asinRowIndex.ContainsKey(casin))
                {
                    asinRowIndex[casin] = 0;
                }


                if (isFirstFile && asinRowIndex[casin] == commitmentPeriod)
                {
                    //if (decimal.TryParse(row[_excelService.GetEnumValue(ForecastExcelColumns.Requested_Quantity)]?.ToString(), out decimal reqQty))
                    //{
                    //    newRow["Wip"] = reqQty;

                    //}
                    //else
                    //{
                    //    newRow["Wip"] = DBNull.Value;
                    //}
                    newRow["Wip"] = 0;

                }
                else
                {
                    newRow["Wip"] = DBNull.Value;
                }

                processedTable.Rows.Add(newRow);
                asinRowIndex[casin]++;
            }

            // 5. Generate Missing Items (Business Logic)
            await GenerateMissingItemsAsync(processedTable, commitmentPeriod, isFirstFile);

            response.Success = true;
            response.Data = new ForecastFileData
            {
                FileName = Path.GetFileName(filePath),
                FilePath = filePath,
                FullTable = processedTable,
                FilteredTable = processedTable.Copy(),
                ProjectionMonth = ProjectionMonth,
                ProjectionYear = ProjectionYear,
                ForecastFor = forecastFor
            };

            return response;
        }

        private async Task GenerateMissingItemsAsync(DataTable table, int commitmentPeriod, bool isFirstFile)
        {
            // 1. Identify distinct Schedules (Periods)
            var distinctSchedules = table.AsEnumerable()
                .Select(r => new
                {
                    Period = r[_excelService.GetEnumValue(ForecastExcelColumns.Commitment_Period)].ToString(),
                    PODate = r[_excelService.GetEnumValue(ForecastExcelColumns.PO_Date)].ToString(),
                    Month = r["Month"].ToString(),
                    Year = r["Year"].ToString()
                })
                .Distinct()
                .OrderBy(x => int.TryParse(x.Period, out int p) ? p : 999)
                .ToList();

            if (!distinctSchedules.Any())
            {
                return;
            }


            // 2. Get DB Items
            var resItems = await _itemsRepository.GetItemCatalogues();
            if (!resItems.Success || resItems.Data == null)
            {
                return;
            }


            var dbCasins = resItems.Data.Select(x => x.Casin.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            // 3. Get File Items
            var fileCasins = table.AsEnumerable()
                .Select(r => r["C-ASIN"].ToString().Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // 4. Find Missing
            var missingCasins = dbCasins.Except(fileCasins, StringComparer.OrdinalIgnoreCase).ToList();

            // 5. Add Rows
            foreach (var missingCasin in missingCasins)
            {
                int periodIndex = 0;
                foreach (var schedule in distinctSchedules)
                {
                    var newRow = table.NewRow();
                    newRow["C-ASIN"] = missingCasin;
                    newRow[_excelService.GetEnumValue(ForecastExcelColumns.Requested_Quantity)] = "0"; // Zero fill
                    newRow[_excelService.GetEnumValue(ForecastExcelColumns.Commitment_Period)] = schedule.Period;
                    newRow[_excelService.GetEnumValue(ForecastExcelColumns.PO_Date)] = schedule.PODate;
                    newRow["Month"] = schedule.Month;
                    newRow["Year"] = schedule.Year;
                    newRow["IsSystemGenerated"] = true;

                    // Fill other required columns with empty strings to prevent null errors
                    newRow[ForecastExcelColumns.ProjectionMonth.ToString()] = "";
                    newRow[ForecastExcelColumns.ProjectionYear.ToString()] = "";

                    // WIP Logic for missing items
                    if (isFirstFile && periodIndex == commitmentPeriod)
                    {
                        newRow["Wip"] = 0;
                    }
                    else
                    {
                        newRow["Wip"] = DBNull.Value;
                    }

                    table.Rows.Add(newRow);
                    periodIndex++;
                }
            }
        }

        #endregion
    }
}