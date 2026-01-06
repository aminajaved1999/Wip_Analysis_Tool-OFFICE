using OfficeOpenXml;
using OfficeOpenXml.Drawing.Slicer.Style;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Data.Entity.Core.Metadata.Edm;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;
using WIPAT.DAL;
using WIPAT.Entities;
using WIPAT.Entities.BO;
using WIPAT.Entities.Dto;
using WIPAT.Entities.Enum;
using static System.Collections.Specialized.BitVector32;
using Color = System.Drawing.Color;

namespace WIPAT.BLL
{
    public class NewFilesManager
    {
        private int _forecastCount;
        private ForecastRepository forecastRepository;
        private ItemsRepository itemsRepository;
        private OrderRepository orderRepository;
        private StockRepository stockRepository;
        private MiscellaneousRepository miscellaneousRepository;

        // ✅ Add these callbacks
        private readonly Action<string> _showBusy;                 // update/show busy with a message
        private readonly Action _hideBusy;                         // hide busy
        private readonly Action<string, StatusType> _setStatus;    // optional status updates
        private readonly WipSession _session;
        public NewFilesManager(WipSession session, Action<string> showBusy = null, Action hideBusy = null, Action<string, StatusType> setStatus = null)
        {
            forecastRepository = new ForecastRepository();
            itemsRepository = new ItemsRepository();
            orderRepository = new OrderRepository(session);
            stockRepository = new StockRepository(session);
            miscellaneousRepository = new MiscellaneousRepository();

            _session = session;
            _showBusy = showBusy;
            _hideBusy = hideBusy;
            _setStatus = setStatus;
        }

        public async Task<Response<ForecastFileData>> GetForecastFilePreviewAsync(string filePath, int commitmentPeriod)
        {
            return await Task.Run(async () =>
            {
                // 1. Define requirements 
                string requiredWorkSheetName = GetEnumValue(ExcelSheetNames.Forecast);
                List<string> requiredExcelColumns = new List<string> {
            GetEnumValue(ForecastExcelColumns.CASIN),
            GetEnumValue(ForecastExcelColumns.Requested_Quantity),
            GetEnumValue(ForecastExcelColumns.Commitment_Period),
            GetEnumValue(ForecastExcelColumns.PO_Date),
            ForecastExcelColumns.ProjectionMonth.ToString(),
            ForecastExcelColumns.ProjectionYear.ToString()
        };

                // 2. Run Import (Now awaited)
                return await ImportForecastFile(filePath, commitmentPeriod, true, requiredWorkSheetName, requiredExcelColumns);
            });
        }
        #region Handle Forecast File
        public async Task<Response<List<ForecastFileData>>> HandleForecastFileAsync(string filePath, List<ForecastFileData> forecastFiles, int commitmentPeriod, WipSession session)
        {
            var res = new Response<List<ForecastFileData>>();
            bool isFirstFile = false;
            string fileName = Path.GetFileName(filePath);

            try
            {
                #region Initial Validations & Setup
                if (_forecastCount >= 4)
                {
                    res.Success = false;
                    res.Message = "Maximum 4 forecast files allowed.";
                    return res;
                }

                isFirstFile = forecastFiles.Count == 0;
                #endregion

                #region check if with same name already uploaded (Session Check)
                if (forecastFiles.Count >= 1)
                {
                    if (forecastFiles.Any(f => f.FileName == fileName))
                    {
                        res.Success = false;
                        res.Message = $"File with name '{fileName}' is already in the current session.";
                        return res;
                    }
                }
                #endregion

                #region Validate Excel File
                _showBusy?.Invoke("Validating Forecast File...");

                string requiredWorkSheetName = GetEnumValue(ExcelSheetNames.Forecast);
                List<string> requiredExcelColumns = new List<string> 
                {
                    GetEnumValue(ForecastExcelColumns.CASIN),
                    GetEnumValue(ForecastExcelColumns.Requested_Quantity),
                    GetEnumValue(ForecastExcelColumns.Commitment_Period),
                    GetEnumValue(ForecastExcelColumns.PO_Date),
                    ForecastExcelColumns.ProjectionMonth.ToString(),
                    ForecastExcelColumns.ProjectionYear.ToString()
                };

                var fileType = FileType.Forecast.ToString();
                var validateResponse = await ValidateUploadFormExcelFiles(filePath, fileType, requiredWorkSheetName, requiredExcelColumns);
                if (!validateResponse.Success)
                {
                    res.Success = false;
                    res.Message = validateResponse.Message;
                    return res;
                }
                #endregion

                #region Import Excel (To get Dates for DB Check)
                _showBusy?.Invoke("Reading File Header...");

                // 🟢 UPDATED: Added 'await' here because ImportForecastFile is now async
                var importResponse = await ImportForecastFile(filePath, commitmentPeriod, isFirstFile, requiredWorkSheetName, requiredExcelColumns);

                if (!importResponse.Success)
                {
                    return new Response<List<ForecastFileData>> { Success = false, Message = $"Error importing forecast file: {importResponse.Message}" };
                }
                var forecastFileData = importResponse.Data;
                #endregion

                #region Check If File Data Already Uploaded (Session Check by Date)
                if (forecastFiles.Count >= 1)
                {
                    if (forecastFiles.Any(f => f.ProjectionMonth == forecastFileData.ProjectionMonth && f.ProjectionYear == forecastFileData.ProjectionYear))
                    {
                        res.Success = false;
                        res.Message = $"A file for {forecastFileData.ProjectionMonth}/{forecastFileData.ProjectionYear} is already in the current session.";
                        return res;
                    }
                }
                #endregion

                bool IsAlreadyExist = false;

                #region Check If Data Already Exist in DB
                var checksRes = forecastRepository.PerformForecastChecks2(forecastFileData.FileName, forecastFileData.ProjectionMonth, forecastFileData.ProjectionYear);

                if (checksRes.Success)
                {
                    IsAlreadyExist = false; // New Data
                }
                else
                {
                    IsAlreadyExist = true; // Data Exists
                    if (checksRes?.Data?.FileData?.FullTable?.Rows.Count <= 0)
                    {
                        res.Success = false;
                        res.Message = $"Failed to load the existing forecast data for verification.";
                        return res;
                    }
                }
                #endregion

                if (IsAlreadyExist)
                {
                    #region Case: Data Exists - IGNORE FILE, LOAD DB
                    if (checksRes.Data.FileData != null && checksRes.Data.FileData.FullTable != null && checksRes.Data.FileData.Forecast != null)
                    {
                        forecastFileData.FullTable = checksRes.Data.FileData.FullTable;
                        forecastFileData.Forecast = checksRes.Data.FileData.Forecast;
                        forecastFileData.IsWipAlreadyCalculated = true;

                        forecastFiles.Add(forecastFileData);
                        _forecastCount++;

                        res.Success = true;
                        res.Data = forecastFiles;
                        res.Message = $"⚠️ Data for {forecastFileData.ProjectionMonth} {forecastFileData.ProjectionYear} already exists in the database.\n\n" +
                                      "To prevent manipulation, the uploaded file was IGNORED.\n" +
                                      "The official record has been loaded instead.";

                        return res;
                    }
                    #endregion
                }
                else
                {
                    #region Case: New Forecast - SAVE FILE

                    // 1. Save to DB
                    _showBusy?.Invoke("Saving Forecast Data...");
                    var saveResponse = forecastRepository.SaveForecastDataToDatabase(forecastFileData, isFirstFile);
                    if (!saveResponse.Success)
                    {
                        return new Response<List<ForecastFileData>> { Success = false, Message = $"Error saving forecast file: {saveResponse.Message}" };
                    }

                    // 2. Fetch back to ensure ID consistency
                    _showBusy?.Invoke("Verifying Saved Data...");
                    var forecastDataRes = forecastRepository.GetForecastDataFromDB(forecastFileData.ProjectionMonth, forecastFileData.ProjectionYear);
                    if (!forecastDataRes.Success)
                    {
                        return new Response<List<ForecastFileData>> { Success = false, Message = $"Failed to load forecast data: {forecastDataRes.Message}" };
                    }

                    forecastFileData.Forecast = forecastDataRes.Data.Item2;
                    forecastFiles.Add(forecastFileData);
                    _forecastCount++;

                    res.Success = true;
                    res.Message = $"{forecastFiles.Count} forecast files loaded successfully.";
                    res.Data = forecastFiles;

                    #endregion
                }

                return res;
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Message = $"Exception occurred: {ex.Message}";
                return res;
            }
        }
        public async Task<Response<ForecastFileData>> ImportForecastFile(string filePath, int commitmentPeriod, bool isFirstFile, string requiredWorkSheetName, List<string> requiredExcelColumns)
        {
            var response = new Response<ForecastFileData>();
            try
            {
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    #region Validate Worksheet & Columns

                    var worksheet = package.Workbook.Worksheets[requiredWorkSheetName];
                    if (worksheet == null)
                    {
                        response.Success = false;
                        response.Message = $"Worksheet '{requiredWorkSheetName}' not found.";
                        return response;
                    }

                    var columnIndexes = requiredExcelColumns.ToDictionary(h => h, h => -1);
                    for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                    {
                        string header = worksheet.Cells[1, col].Text?.Trim();
                        if (columnIndexes.ContainsKey(header))
                            columnIndexes[header] = col;
                    }

                    var missingColumns = columnIndexes.Where(kvp => kvp.Value == -1).Select(kvp => kvp.Key).ToList();
                    if (missingColumns.Any())
                    {
                        response.Success = false;
                        response.Message = "Missing required columns: " + string.Join(", ", missingColumns);
                        return response;
                    }

                    #endregion

                    #region Build DataTable
                    DataTable table = new DataTable();
                    foreach (var header in requiredExcelColumns) table.Columns.Add(header);
                    table.Columns.Add("Month");
                    table.Columns.Add("Year");
                    table.Columns.Add("Wip");
                    table.Columns.Add("IsSystemGenerated", typeof(bool));

                    var asinRowIndex = new Dictionary<string, int>();

                    int row = 2;
                    while (row <= worksheet.Dimension.End.Row && !string.IsNullOrWhiteSpace(worksheet.Cells[row, columnIndexes["C-ASIN"]].Text))
                    {
                        var newRow = table.NewRow();
                        foreach (var header in requiredExcelColumns)
                        {
                            newRow[header] = worksheet.Cells[row, columnIndexes[header]].Text?.Trim();
                        }

                        string asin = newRow["C-ASIN"].ToString();

                        if (!asinRowIndex.ContainsKey(asin))
                            asinRowIndex[asin] = 0;

                        if (DateTime.TryParse(newRow["PO date"].ToString(), out DateTime poDate))
                        {
                            newRow["Month"] = poDate.ToString("MMMM");
                            newRow["Year"] = poDate.Year.ToString();
                        }
                        else
                        {
                            newRow["Month"] = "Invalid Date";
                            newRow["Year"] = "";
                        }
                        // Mark as NOT system generated (came from file)
                        newRow["IsSystemGenerated"] = false;

                        if (isFirstFile && asinRowIndex[asin] == commitmentPeriod)
                        {
                            if (decimal.TryParse(newRow["Requested Quantity"]?.ToString(), out decimal requestedQty))
                                newRow["Wip"] = requestedQty;
                            else
                                newRow["Wip"] = DBNull.Value;
                        }
                        else
                        {
                            newRow["Wip"] = DBNull.Value;
                        }

                        table.Rows.Add(newRow);
                        asinRowIndex[asin]++;
                        row++;
                    }
                    #endregion

                    #region Insert Missing Items with 0 Quantity

                    // 1. Extract the "Master Schedule" (Date info per period) from valid rows
                    // This groups by the numeric commitment period found in the file (e.g., 1, 2, 3...)
                    var distinctSchedules = table.AsEnumerable()
                        .Select(r => new
                        {
                            Period = r[GetEnumValue(ForecastExcelColumns.Commitment_Period)].ToString(),
                            PODate = r[GetEnumValue(ForecastExcelColumns.PO_Date)].ToString(),
                            Month = r["Month"].ToString(),
                            Year = r["Year"].ToString()
                        })
                        .Distinct()
                        // Order them to ensure we iterate 1, 2, 3, 4, 5, 6 properly for the index check
                        .OrderBy(x => int.TryParse(x.Period, out int p) ? p : 999)
                        .ToList();

                    if (distinctSchedules.Any())
                    {
                        // 2. Get All Active Items from Database
                        var resItems = await itemsRepository.GetItemCatalogues();
                        if (resItems.Success && resItems.Data != null)
                        {
                            var dbCasins = resItems.Data.Select(x => x.Casin.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                            // 3. Find items in DB but NOT in File
                            var excelCasins = table.AsEnumerable()
                                .Select(r => r["C-ASIN"].ToString().Trim())
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .ToList();

                            var missingCasins = dbCasins.Except(excelCasins, StringComparer.OrdinalIgnoreCase).ToList();

                            // 4. Generate rows for missing items
                            foreach (var missingCasin in missingCasins)
                            {
                                int periodIndex = 0;
                                foreach (var schedule in distinctSchedules)
                                {
                                    var newRow = table.NewRow();

                                    newRow["C-ASIN"] = missingCasin;
                                    newRow["Requested Quantity"] = "0"; // Force 0 for missing items
                                    newRow[GetEnumValue(ForecastExcelColumns.Commitment_Period)] = schedule.Period;
                                    newRow[GetEnumValue(ForecastExcelColumns.PO_Date)] = schedule.PODate;
                                    newRow["Month"] = schedule.Month;
                                    newRow["Year"] = schedule.Year;
                                    newRow["IsSystemGenerated"] = true; //Mark as System Generated

                                    // Apply WIP logic: If this period index matches the 'commitmentPeriod' passed to the method, set WIP=0
                                    if (isFirstFile && periodIndex == commitmentPeriod)
                                    {
                                        newRow["Wip"] = 0;
                                    }
                                    else
                                    {
                                        newRow["Wip"] = DBNull.Value;
                                    }

                                    // Fill other columns to avoid nulls
                                    foreach (DataColumn col in table.Columns)
                                    {
                                        if (newRow[col] == DBNull.Value && col.ColumnName != "Wip")
                                        {
                                            newRow[col] = "";
                                        }
                                    }

                                    table.Rows.Add(newRow);
                                    periodIndex++;
                                }
                            }
                        }
                    }
                    #endregion

                    DataTable filteredTable = table.Copy();

                    #region Extract Projection Month and year
                    if (!int.TryParse(worksheet.Cells[2, columnIndexes["ProjectionMonth"]]?.Text?.Trim(), out int projectionMonth)
                        || !int.TryParse(worksheet.Cells[2, columnIndexes["ProjectionYear"]]?.Text?.Trim(), out int projectionYear))
                    {
                        response.Success = false;
                        response.Message = "Projection Month or Projection Year not found or invalid.";
                        return response;
                    }

                    DateTime projectionDate = new DateTime(projectionYear, projectionMonth, 1);
                    string ProjectionMonth = projectionDate.ToString("MMMM");
                    string ProjectionYear = projectionDate.ToString("yyyy");
                    string forecastFor = projectionDate.AddMonths(commitmentPeriod + 1).ToString("MMMM yyyy");
                    #endregion

                    #region success response 
                    response.Success = true;
                    response.Data = new ForecastFileData
                    {
                        FileName = Path.GetFileName(filePath),
                        FilePath = filePath,
                        FullTable = table,
                        FilteredTable = filteredTable,
                        ProjectionMonth = ProjectionMonth,
                        ProjectionYear = ProjectionYear,
                        ForecastFor = forecastFor
                    };
                    #endregion success response 

                    return response;
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                return response;
            }
        }
        #endregion Handle Forecast File

        #region Handle Stock File Upload
        public async Task<Response<StockFileResponse>> HandleStockFile(string filePath, bool StockUploaded, WipSession session)
        {
            var response = new Response<StockFileResponse>();
            string fileName = Path.GetFileName(filePath);

            try
            {

                #region Update Stock Data..
                var importResponse = await UpdateStockData(filePath, session);
                if (!importResponse.Success)
                {
                    response.Success = importResponse.Success;
                    response.Message = importResponse.Message;
                    return response;
                }

                response.Success = true;
                response.Data.DataTable = importResponse.Data.DataTable;
                response.Data.MissingStocks = importResponse.Data.MissingStocks;
                return response;
                #endregion Update Stock Data...
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "An unexpected error occurred while handling the Stock file.";
                response.Data = null;

                return response;
            }
        }
        public async Task<Response<StockFileResponse>> UpdateStockData(string filePath, WipSession session)
        {
            var response = new Response<StockFileResponse>();
            response.Data.DataTable = new DataTable();
            response.Data.MissingStocks = new List<InvalidStock>();
            response.Data.ValidStocks = new List<InitialStock>();
            response.Message = string.Empty;

            #region Initialize Response and Variables
            var missingItems = new List<InvalidStock>();
            var existingItemsInCatalogue = new List<InitialStock>();
            var dt = new DataTable();
            // Add necessary columns to the DataTable
            dt.Columns.Add(StockOrderExcelColumns.CASIN.ToString());
            dt.Columns.Add(StockOrderExcelColumns.Quantity.ToString());
            dt.Columns.Add("Status"); // New column for row status
            var requiredWorkSheetName = ExcelSheetNames.Stock.ToString();

            var requiredExcelColumns = new List<string> { StockOrderExcelColumns.CASIN.ToString(), StockOrderExcelColumns.Quantity.ToString() };

            #endregion


            #region Validate Excel File
            var fileType = FileType.Stock.ToString();
            var validateResponse = await ValidateUploadFormExcelFiles(filePath, fileType, requiredWorkSheetName, requiredExcelColumns);
            if (!validateResponse.Success)
            {
                response.Success = false;
                response.Message = validateResponse.Message;
                return response;
            }
            #endregion Validate Excel File

            try
            {
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var ws = package.Workbook.Worksheets[requiredWorkSheetName];

                    // Ensure the worksheet is valid
                    if (ws == null || ws.Dimension == null)
                    {
                        response.Success = false;
                        response.Message = "The selected Excel sheet is empty.";
                        return response;
                    }

                    #region Process Rows
                    int colCAsin = Array.IndexOf(ws.Cells[1, 1, 1, ws.Dimension.End.Column].Select(c => c.Text).ToArray(), StockOrderExcelColumns.CASIN.ToString()) + 1;
                    int colInitialStock = Array.IndexOf(ws.Cells[1, 1, 1, ws.Dimension.End.Column].Select(c => c.Text).ToArray(), StockOrderExcelColumns.Quantity.ToString()) + 1;

                    for (int row = 2; row <= ws.Dimension.End.Row; row++)
                    {
                        string rowStatus = string.Empty;

                        string casin = ws.Cells[row, colCAsin].Text.Trim();
                        string initialStockStr = ws.Cells[row, colInitialStock].Text.Trim();
                        int.TryParse(initialStockStr, out int stockQty);

                        //bool exists = itemsRepository.IsCAsinExistInCatalogue(casin);
                        var casinExistResponse = await itemsRepository.IsCasinExistInCatalogueAndInitialStock(casin);
                        if (casinExistResponse.Success)
                        {
                            rowStatus = "✔";

                            //get item details
                            var item = itemsRepository.GetItemByCAsin(casin);

                            //add to list for saving later
                            var initialStock = new InitialStock();
                            initialStock.ItemCatalogueId = item.Id;
                            initialStock.OpeningStock = stockQty;
                            initialStock.CreatedById = session.LoggedInUser.Id;
                            existingItemsInCatalogue.Add(initialStock);
                            dt.Rows.Add(casin, initialStockStr, rowStatus);

                        }
                        else
                        {
                            rowStatus = "❌";

                            //add to missing list
                            var invalidStock = new InvalidStock();
                            invalidStock.Casin = casin;
                            invalidStock.Quantity = initialStockStr;
                            invalidStock.FileName = Path.GetFileName(filePath);
                            missingItems.Add(invalidStock);
                        }

                        //dt.Rows.Add(casin, initialStockStr, rowStatus);
                    }
                    #endregion Process Rows

                    #region update Stocks
                    var updateStockResponse = await stockRepository.UpdateStockQuantitiesAsync(existingItemsInCatalogue);
                    if (!updateStockResponse.Success)
                    {
                        response.Success = false;
                        response.Message = updateStockResponse.Message;
                        return response;
                    }

                    #endregion

                    #region Build Response Message
                    var messageParts = new List<string> { "Excel loaded successfully." };

                    if (existingItemsInCatalogue.Any())
                    {
                        messageParts.Add(updateStockResponse.Success ? $"{existingItemsInCatalogue.Count} stock(s) update successfully." : "Failed to update stock(s).");
                    }

                    if (missingItems.Any())
                    {
                        messageParts.Add($"{missingItems.Count} invalid stock item(s) found and listed below.");
                    }

                    response.Success = updateStockResponse.Success;
                    response.Message = string.Join(" ", messageParts);
                    response.Data.DataTable = dt;
                    response.Data.MissingStocks = missingItems;
                    #endregion

                    return response;
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error reading Excel file: " + ex.Message;
                return response;
            }
        }
        #endregion Handle Stock File Upload

        #region Handle Order File Upload
        public async Task<Response<OrderFileResponse>> HandleOrderFile(string filePath, bool orderUploaded, WipSession session)
        {
            var response = new Response<OrderFileResponse>();
            response.Data = new OrderFileResponse(); // Initialize to avoid nulls

            string fileName = Path.GetFileName(filePath);
            var requiredMonthYear = session.CurrentMonthWithYear;

            if (string.IsNullOrEmpty(requiredMonthYear))
            {
                response.Success = false;
                response.Message = "Session month is not set. Please upload forecasts first.";
                return response;
            }

            var parts = requiredMonthYear.Split(' ');
            string requiredMonth = parts[0];
            string requiredYear = parts[1];

            try
            {
                #region 1. Check if Order Already Exists (DB Check)
                // We check this BEFORE processing the excel file to save time
                var existingFileRes = await orderRepository.OrderFileExists(fileName, requiredMonth, requiredYear);

                if (existingFileRes.Success)
                {
                    // CASE: DATA EXISTS -> IGNORE FILE, LOAD DB
                    _showBusy?.Invoke("Fetching existing orders...");

                    var orderDataResponse = orderRepository.GetExstingOrderData(fileName, requiredMonth, requiredYear);

                    if (!orderDataResponse.Success)
                    {
                        response.Success = false;
                        response.Message = orderDataResponse.Message;
                        return response;
                    }
                    else
                    {
                        response.Success = true;
                        response.Data.DataTable = orderDataResponse.Data.Item1;
                        response.Data.ValidOrders = orderDataResponse.Data.Item2;

                        // Specific message for UI
                        response.Message = $"⚠️ Orders for {requiredMonth} {requiredYear} already exist.\n\n" +
                                           "The uploaded file was IGNORED to prevent manipulation.\n" +
                                           "Existing data has been loaded from the database.";
                        return response;
                    }
                }
                #endregion

                #region 2. Process New File

                // Setup Requirements
                var requiredWorkSheetName = ExcelSheetNames.Order.ToString();
                var requiredExcelColumns = new List<string> 
                {
                    StockOrderExcelColumns.CASIN.ToString(),
                    StockOrderExcelColumns.Quantity.ToString(),
                    StockOrderExcelColumns.Month.ToString(),
                    StockOrderExcelColumns.Year.ToString()
                };

                // Validate
                var validateResponse = await ValidateUploadFormExcelFiles(filePath, FileType.Order.ToString(), requiredWorkSheetName, requiredExcelColumns, requiredMonthYear);
                if (!validateResponse.Success)
                {
                    response.Success = false;
                    response.Message = validateResponse.Message;
                    return response;
                }

                // Import
                var importResponse = await ImportOrderFile(filePath, requiredWorkSheetName, requiredExcelColumns, session);

                if (!importResponse.Success)
                {
                    response.Success = false;
                    response.Data = importResponse.Data;
                    response.Message = importResponse.Message;
                    return response;
                }
                else if (importResponse.Data.MissingOrders.Count > 0)
                {
                    response.Success = false; // Treated as failure if there are invalid items
                    response.Data = importResponse.Data;
                    response.Message = $"Invalid Orders Found: {importResponse.Data.InvalidOrderCount}";
                    return response;
                }
                else if (importResponse.Data.ValidOrders.Count <= 0)
                {
                    response.Success = false;
                    response.Message = "No valid orders found in the file.";
                    return response;
                }

                // Save New Data
                var orderUpdateResponse = await orderRepository.SaveOrdersAndUpdateStock(importResponse.Data.ValidOrders);
                if (!orderUpdateResponse.Success)
                {
                    response.Success = false;
                    response.Message = orderUpdateResponse.Message;
                    return response;
                }

                // Fetch back to display
                var freshDataResponse = orderRepository.GetExstingOrderData(fileName, requiredMonth, requiredYear);
                if (!freshDataResponse.Success)
                {
                    // Fallback to imported data if fetch fails, though saving succeeded
                    response.Success = true;
                    response.Message = "Orders saved successfully.";
                    response.Data = importResponse.Data;
                }
                else
                {
                    response.Success = true;
                    response.Message = "Orders uploaded and saved successfully.";
                    response.Data.DataTable = freshDataResponse.Data.Item1;
                    response.Data.ValidOrders = freshDataResponse.Data.Item2;
                }

                return response;
                #endregion
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "An unexpected error occurred: " + ex.Message;
                return response;
            }
        }

        public async Task<Response<OrderFileResponse>> ImportOrderFile(string filePath, string requiredWorkSheetName, List<string> requiredExcelColumns, WipSession session)
        {
            var response = new Response<OrderFileResponse>
            {
                Data = new OrderFileResponse
                {
                    ValidOrders = new List<ActualOrder>(),
                    MissingOrders = new List<InvalidOrder>(),
                    DataTable = new DataTable(),
                    ValidOrderCount = 0,
                    InvalidOrderCount = 0,
                    TotalOrderCount = 0
                }
            };

            try
            {
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets[requiredWorkSheetName];
                    if (worksheet == null)
                    {
                        response.Success = false;
                        response.Message = $"Worksheet '{requiredWorkSheetName}' not found.";
                        return response;
                    }

                    // Map column indexes
                    var columnIndexes = requiredExcelColumns.ToDictionary(h => h, h => -1);
                    for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                    {
                        string header = worksheet.Cells[1, col].Text?.Trim();
                        if (columnIndexes.ContainsKey(header))
                            columnIndexes[header] = col;
                    }

                    // Prepare DataTable
                    foreach (var header in requiredExcelColumns)
                        response.Data.DataTable.Columns.Add(header);
                    response.Data.DataTable.Columns.Add("Status");

                    int row = 2;
                    while (true)
                    {
                        bool isRowEmpty = true;
                        var newRow = response.Data.DataTable.NewRow();

                        // Read cell values
                        var rowData = new Dictionary<string, string>();
                        foreach (var header in requiredExcelColumns)
                        {
                            var value = worksheet.Cells[row, columnIndexes[header]].Text?.Trim();
                            rowData[header] = value;
                            newRow[header] = value;
                            if (!string.IsNullOrEmpty(value))
                                isRowEmpty = false;
                        }

                        if (isRowEmpty)
                            break;

                        // Extract specific fields (assuming standard column names)
                        string casin = rowData.ContainsKey("CASIN") ? rowData["CASIN"] : string.Empty;
                        string actualOrderStr = rowData.ContainsKey("Quantity") ? rowData["Quantity"] : string.Empty;
                        string monthStr = rowData.ContainsKey("Month") ? rowData["Month"] : string.Empty;
                        string yearStr = rowData.ContainsKey("Year") ? rowData["Year"] : string.Empty;

                        int.TryParse(monthStr, out int month);
                        int.TryParse(yearStr, out int year);

                        // Use DateTime to convert month number to month name
                        string monthName = new DateTime(year, month, 1).ToString("MMMM");

                        // Validate item in the catalogue
                        var itemExistResponse = await itemsRepository.IsItemExistInCatalogue(casin);
                        string rowStatus;

                        if (itemExistResponse.Success)
                        {
                            rowStatus = "✔";

                            var item = itemsRepository.GetItemByCAsin(casin); // assuming this is not async
                            var actualOrder = new ActualOrder
                            {
                                ItemCatalogueId = item.Id,
                                Quantity = int.TryParse(actualOrderStr, out var qty) ? qty : 0,
                                Month = monthName,
                                Year = yearStr,
                                CreatedAt = DateTime.Now,
                                CreatedById = session.LoggedInUser.Id,
                                FileName = Path.GetFileName(filePath)
                            };
                            response.Data.ValidOrders.Add(actualOrder);
                            response.Data.ValidOrderCount++; // Increment valid orders count
                        }
                        else
                        {
                            rowStatus = "❌";
                            var invalidOrder = new InvalidOrder
                            {
                                Casin = casin,
                                Quantity = actualOrderStr,
                                Month = monthName,
                                Year = yearStr,
                                FileName = Path.GetFileName(filePath)
                            };
                            response.Data.MissingOrders.Add(invalidOrder);
                            response.Data.InvalidOrderCount++; // Increment invalid orders count
                        }

                        response.Data.TotalOrderCount++; // Increment total orders count
                        newRow["Status"] = rowStatus;
                        response.Data.DataTable.Rows.Add(newRow);
                        row++;
                    }

                    response.Success = true;
                    response.Message = "File imported successfully.";
                    return response;
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error reading Excel file: {ex.Message}";
                return response;
            }
        }

        #endregion Handle Order File Upload

        #region validate Excel File

        public async Task<Response<string>> ValidateUploadFormExcelFiles(string filePath, string fileType, string requiredWorkSheetName, List<string> requiredExcelColumns, string requiredOrderMonthYear = null)
        {
            var response = new Response<string>();
            var allowedExtensions = new[] { ".xls", ".xlsx" };
            List<string> casinList = new List<string>();

            #region Input Validation
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

            if (fileType == FileType.Order.ToString())
            {
                if (string.IsNullOrEmpty(requiredOrderMonthYear))
                {
                    response.Success = false;
                    response.Message = "value if 'requiredOrderMonthYear' is required";
                    return response;
                }
            }
            #endregion

            try
            {
                var fileInfo = new FileInfo(filePath);
                using (var package = await Task.Run(() => new ExcelPackage(fileInfo)))
                {
                    #region File Processing (Opening and Reading Excel File)
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
                    #endregion

                    #region Header Validation
                    var headerRow = Enumerable.Range(1, worksheet.Dimension.End.Column)
                                                .Select(col => worksheet.Cells[1, col].Text)
                                                .ToList();

                    var missingColumns = requiredExcelColumns.Except(headerRow).ToList();

                    if (missingColumns.Any())
                    {
                        string missingMessage = missingColumns.Any() ? $"Missing columns: {string.Join(", ", missingColumns)}." : string.Empty;
                        response.Success = false;
                        response.Message = $"{missingMessage}".Trim();
                        return response;
                    }
                    #endregion

                    #region Data Validation
                    // Helper functions
                    bool IsNumeric(string s) => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
                    bool IsInteger(string s) => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
                    bool IsNegative(string s) => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) && number < 0;
                    bool IsEmpty(string s) => string.IsNullOrWhiteSpace(s);
                    bool IsValidMonth(string s) => IsNumeric(s) && int.TryParse(s, out int month) && month >= 1 && month <= 12;
                    bool IsValidCommitmentPeriod(string s) => IsNumeric(s) && int.TryParse(s, out int month) && month >= 1 && month <= 6;
                    bool IsValidYear(string s) => IsNumeric(s) && int.TryParse(s, out int year) && year >= 1900 && year <= DateTime.Now.Year;
                    bool IsValidDate(string s) => DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

                    List<string> monthsList = new List<string>();
                    List<string> yearsList = new List<string>();

                    for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                    {
                        foreach (var column in requiredExcelColumns)
                        {
                            var columnIndex = headerRow.IndexOf(column) + 1;
                            var cellValue = worksheet.Cells[row, columnIndex].Text;

                            #region Forecast File columns validation
                            if (column == ForecastExcelColumns.ProjectionMonth.ToString())
                            {
                                if (IsEmpty(cellValue)) { response.Success = false; response.Message = $"Column '{column}' at row {row} is required."; return response; }
                                else if (!IsValidMonth(cellValue)) { response.Success = false; response.Message = $"Column '{column}' at row {row} must be 1-12."; return response; }
                                monthsList.Add(cellValue);
                            }
                            else if (column == ForecastExcelColumns.ProjectionYear.ToString())
                            {
                                if (IsEmpty(cellValue)) { response.Success = false; response.Message = $"Column '{column}' at row {row} is required."; return response; }
                                else if (!IsValidYear(cellValue)) { response.Success = false; response.Message = $"Column '{column}' at row {row} must be a valid year."; return response; }
                                yearsList.Add(cellValue);
                            }
                            else if (column == GetEnumValue(ForecastExcelColumns.Requested_Quantity))
                            {
                                if (IsEmpty(cellValue)) { response.Success = false; response.Message = $"Column '{column}' at row {row} is required."; return response; }
                                else if (!IsNumeric(cellValue) || IsNegative(cellValue)) { response.Success = false; response.Message = $"Column '{column}' at row {row} must be positive numeric."; return response; }
                            }
                            else if (column == GetEnumValue(ForecastExcelColumns.Commitment_Period))
                            {
                                if (IsEmpty(cellValue)) { response.Success = false; response.Message = $"Column '{column}' at row {row} is required."; return response; }
                                else if (!IsValidCommitmentPeriod(cellValue)) { response.Success = false; response.Message = $"Column '{column}' at row {row} must be 1-6."; return response; }
                            }
                            else if (column == GetEnumValue(ForecastExcelColumns.PO_Date))
                            {
                                if (IsEmpty(cellValue)) { response.Success = false; response.Message = $"Column '{column}' at row {row} is required."; return response; }
                                else if (!IsValidDate(cellValue)) { response.Success = false; response.Message = $"Column '{column}' at row {row} must be valid date."; return response; }
                            }
                            else if (column == GetEnumValue(ForecastExcelColumns.CASIN))
                            {
                                if (IsEmpty(cellValue)) { response.Success = false; response.Message = $"Column '{column}' at row {row} is required."; return response; }
                                casinList.Add(cellValue.Trim());
                            }
                            #endregion

                            #region stock / order columns validation
                            if (column == StockOrderExcelColumns.Quantity.ToString())
                            {
                                if (IsEmpty(cellValue)) { response.Success = false; response.Message = $"Column '{column}' at row {row} is required."; return response; }
                                else if (!IsNumeric(cellValue) || IsNegative(cellValue)) { response.Success = false; response.Message = $"Column '{column}' at row {row} must be positive numeric."; return response; }
                            }
                            else if (column == StockOrderExcelColumns.CASIN.ToString())
                            {
                                if (IsEmpty(cellValue)) { response.Success = false; response.Message = $"Column '{column}' at row {row} is required."; return response; }
                                casinList.Add(cellValue);
                            }
                            else if (column == StockOrderExcelColumns.Month.ToString())
                            {
                                if (IsEmpty(cellValue)) { response.Success = false; response.Message = $"Column '{column}' at row {row} is required."; return response; }
                                monthsList.Add(cellValue);
                            }
                            else if (column == StockOrderExcelColumns.Year.ToString())
                            {
                                if (IsEmpty(cellValue)) { response.Success = false; response.Message = $"Column '{column}' at row {row} is required."; return response; }
                                yearsList.Add(cellValue);
                            }

                            if (fileType == FileType.Order.ToString() || fileType == FileType.Stock.ToString())
                            {
                                var duplicateCASINs = casinList
                                    .Where(s => !string.IsNullOrWhiteSpace(s))
                                    .GroupBy(x => x.Trim(), StringComparer.OrdinalIgnoreCase)
                                    .Where(g => g.Count() > 1)
                                    .Select(g => g.Key)
                                    .ToList();

                                if (duplicateCASINs.Any())
                                {
                                    response.Success = false;
                                    response.Message = $"Duplicate CASIN values found: {string.Join(", ", duplicateCASINs)}.";
                                    return response;
                                }
                            }
                            #endregion
                        }
                    }

                    if (fileType == FileType.Order.ToString())
                    {
                        var distinctMonths = monthsList.Distinct().ToList();
                        var distinctYears = yearsList.Distinct().ToList();

                        if (distinctMonths.Count > 1 || distinctYears.Count > 1)
                        {
                            response.Success = false;
                            response.Message = "The Excel file contains multiple months or years. Please ensure the file contains data for only one month and one year.";
                            return response;
                        }

                        int month = int.Parse(distinctMonths.FirstOrDefault());
                        int yearInt = int.Parse(distinctYears.FirstOrDefault());
                        string monthName = new DateTime(yearInt, month, 1).ToString("MMMM");

                        var parts = requiredOrderMonthYear.Split(' ');
                        string requiredMonth = parts[0];
                        string requiredYear = parts[1];

                        if (requiredMonth != monthName)
                        {
                            response.Success = false;
                            response.Message = $"The Excel file contains data for {monthName}. Please ensure the file contains data for the required month ({requiredMonth}).";
                            return response;
                        }

                        if (requiredYear != yearInt.ToString())
                        {
                            response.Success = false;
                            response.Message = $"The Excel file contains data for {yearInt}. Please ensure the file contains data for the required year ({requiredYear}).";
                            return response;
                        }
                    }

                    // Items in File that are MISSING in DB 
                    if (fileType == FileType.Forecast.ToString() || fileType == FileType.Order.ToString() || fileType == FileType.Stock.ToString())
                    {
                        var distinctCASINs = casinList
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .Select(s => s.Trim())
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList();

                        var missing = new List<string>();

                        foreach (var c in distinctCASINs)
                        {
                            if (!itemsRepository.IsCAsinExistInCatalogue(c))
                                missing.Add(c);
                        }

                        if (missing.Any())
                        {
                            response.Success = false;
                            response.Message = "The following CASIN values do not exist in the catalogue:\r\n\r\n" +
                                               string.Join("\r\n", missing) +
                                               "\r\n\r\nPlease add these items in the catalogue first to continue.";

                            return response;
                        }
                    }
                    #endregion

                    response.Success = true;
                    response.Message = "Excel file validated successfully.";
                    response.Data = "Validation passed.";
                    return response;
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error: {ex.Message}";
            }
            return response;
        }
        #endregion validate Excel File



        // Helper method to get the enum value
        public static string GetEnumValue(Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = (DescriptionAttribute)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
            return attribute == null ? value.ToString() : attribute.Description;
        }


        public async Task<Response<ForecastFileData>> LoadExistingForecastAsync(string month, string year)
        {
            var response = new Response<ForecastFileData>();

            try
            {
                // 1. Fetch data from Repository
                // Wrapping in Task.Run ensures the UI doesn't freeze if the repo call is synchronous
                var forecastDataRes = await Task.Run(() => forecastRepository.GetForecastDataFromDB(month, year));

                if (!forecastDataRes.Success)
                {
                    response.Success = false;
                    response.Message = forecastDataRes.Message;
                    return response;
                }

                // 2. Extract Data
                var dataTable = forecastDataRes.Data.Item1;
                var forecastEntity = forecastDataRes.Data.Item2;

                // 3. Map to ForecastFileData DTO
                var fileData = new ForecastFileData
                {
                    // Set identifier properties
                    FileName = $"DB_{month}_{year}", // Artificial name to indicate DB source
                    FilePath = "Database Source",    // No physical path needed

                    // Set Month/Year keys
                    ProjectionMonth = month,
                    ProjectionYear = year,

                    // Bind the actual Data
                    FullTable = dataTable,
                    FilteredTable = dataTable, // Usually same as Full for DB loads
                    Forecast = forecastEntity,

                    // Important: Set the display text for the target month
                    // We use the entity's property if available, otherwise fallback to current month
                    ForecastFor = forecastEntity?.ForecastingFor ?? $"{month} {year}",

                    // CRITICAL: Mark as already calculated so the app doesn't try to re-process it as a new Excel upload
                    IsWipAlreadyCalculated = forecastEntity.IsWipCalculated
                };

                response.Success = true;
                response.Data = fileData;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error loading forecast from DB: {ex.Message}";
            }

            return response;
        }

        public async Task<Response<DataTable>> LoadExistingOrderAsync(string month, string year)
        {
            var response = new Response<DataTable>();

            try
            {
                // 1. Fetch data from Repository
                // Wrapping in Task.Run ensures the UI doesn't freeze if the repo call is synchronous
                var OrderDataRes = await Task.Run(() => orderRepository.GetOrderDataByMonthYear(month, year));

                if (!OrderDataRes.Success)
                {
                    response.Success = false;
                    response.Message = OrderDataRes.Message;
                    return response;
                }

                // 2. Extract Data
                // 3. Map to OrderFileData DTO
                response.Data = OrderDataRes.Data;
                response.Success = true;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error loading Order from DB: {ex.Message}";
            }

            return response;
        }
    }


}