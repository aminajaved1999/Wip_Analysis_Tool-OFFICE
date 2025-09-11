using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;
using WIPAT.DAL;
using WIPAT.Entities;
using WIPAT.Entities.Dto;
using WIPAT.Entities.Enum;

namespace WIPAT.BLL
{
    public class FilesManager
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

        public FilesManager( Action<string> showBusy = null, Action hideBusy = null, Action<string, StatusType> setStatus = null)
        {
            forecastRepository = new ForecastRepository();
            itemsRepository = new ItemsRepository();
            orderRepository = new OrderRepository();
            stockRepository = new StockRepository();
            miscellaneousRepository = new MiscellaneousRepository();

            _showBusy = showBusy;
            _hideBusy = hideBusy;
            _setStatus = setStatus;
        }

        #region Handle Forecast File
        public Response<List<ForecastFileData>> HandleForecastFile(string filePath, List<ForecastFileData> forecastFiles, int commitmentPeriod)
        {
            var res = new Response<List<ForecastFileData>>();
            try
            {
                bool isFirstFile = false;
                if (filePath.ToLower().Contains("forecast"))
                {
                    if (_forecastCount >= 4)
                    {
                        res.Success = false;
                        res.Message = "Maximum 4 forecast files allowed.";
                        return res;
                    }

                    if (forecastFiles.Count == 0)
                    {
                        isFirstFile = true;
                    }
                    else
                    {
                        isFirstFile = false;
                    }

                    //  Check if the file exists in DB 
                    string fileName = Path.GetFileName(filePath);

                    var existingFileData = forecastRepository.ForecastFileExists(fileName);
                    if (existingFileData != null)
                    {
                        // Ask user
                        var load = MessageBox.Show($"'{fileName}'  exists but no WIP yet. Load existing instead?", "WIP Not Calculated",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question
                        );

                        if (load == DialogResult.Yes)
                        {
                            _showBusy?.Invoke("Get Forecast Data in DataTable...");

                            var existingDataTable = forecastRepository.GetForecastDataInDataTable(existingFileData.FileName, existingFileData.ProjectionMonth, existingFileData.ProjectionYear);
                            if (!existingDataTable.Success)
                            {
                                res.Success = false;
                                res.Message = $"Failed to load forecast data table for file '{fileName}':{existingDataTable.Message}.";
                                return res;
                            }
                            existingFileData.FullTable = existingDataTable.Data;



                            _showBusy?.Invoke("Get Forecast Data in Object...");

                            var existingForecast = forecastRepository.GetForecastDataInObject(existingFileData.FileName, existingFileData.ProjectionMonth, existingFileData.ProjectionYear);
                            if (!existingForecast.Success)
                            {
                                res.Success = false;
                                res.Message = $"Failed to load forecast object data for file '{fileName}':{existingForecast.Message}.";
                                return res;
                            }
                            existingFileData.Forecast = existingForecast.Data;
                            forecastFiles.Add(existingFileData);
                            _forecastCount++;

                            res.Success = true;
                            res.Message = $"{forecastFiles.Count} forecast files loaded (from existing DB).";
                            res.Data = forecastFiles;
                            return res;
                        }
                        // Else continue with import
                    }



                    // import
                    _showBusy?.Invoke("Import Forecast File...");
                    var importResponse = ImportForecastFile(filePath, commitmentPeriod, isFirstFile);
                    if (!importResponse.Success)
                    {
                        res.Success = false;
                        res.Message = $"Error importing forecast file: {importResponse.Message}";
                        return res;
                    }

                    //save
                    _showBusy?.Invoke("Save Forecast Data To Database...");
                    var saveResponse = forecastRepository.SaveForecastDataToDatabase(importResponse.Data, isFirstFile);
                    if (!saveResponse.Success)
                    {
                        res.Success = false;
                        res.Message = $"Error saving forecast file: {saveResponse.Message}";
                        return res;
                    }

                    _showBusy?.Invoke("Get Forecast Data in Object...");
                    var forecast = forecastRepository.GetForecastDataInObject(importResponse.Data.FileName, importResponse.Data.ProjectionMonth, importResponse.Data.ProjectionYear);
                    if (!forecast.Success)
                    {
                        res.Success = false;
                        res.Message = $"Failed to load forecast object data for file '{fileName}':{forecast.Message}.";
                        return res;
                    }
                    importResponse.Data.Forecast = forecast.Data;

                    forecastFiles.Add(importResponse.Data);
                    _forecastCount++;

                    res.Success = true;
                    res.Message = $"{forecastFiles.Count} forecast files loaded.";
                    res.Data = forecastFiles;
                    return res;
                }
                else
                {
                    res.Success = false;
                    res.Message = "Selected file is not a forecast file.";
                    return res;
                }
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Message = $"Exception occurred: {ex.Message}" + (ex.InnerException != null ? $" | Inner Exception: {ex.InnerException.Message}" : "");
                return res;
            }

        }
        public Response<ForecastFileData> ImportForecastFile(string filePath, int commitmentPeriod, bool isFirstFile)
        {
            var response = new Response<ForecastFileData>();
            try
            {
                string[] requiredHeaders = new[]
                {
                    //"C-ASIN", "Model number", "Requested Quantity", "Commitment period", "PO date"
                    "C-ASIN",  "Requested Quantity", "Commitment period", "PO date"
                };
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets["Vendor Central Excel Output"];
                    if (worksheet == null)
                    {
                        response.Success = false;
                        response.Message = "Worksheet 'Vendor Central Excel Output' not found.";
                        return response;
                    }

                    // Find required columns (your existing logic)
                    var columnIndexes = requiredHeaders.ToDictionary(h => h, h => -1);
                    for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                    {
                        string header = worksheet.Cells[2, col].Text?.Trim();
                        if (columnIndexes.ContainsKey(header))
                            columnIndexes[header] = col;
                    }

                    if (columnIndexes.Values.Any(v => v == -1))
                    {
                        response.Success = false;
                        response.Message = "Missing required columns.";
                        return response;
                    }

                    DataTable table = new DataTable();
                    foreach (var header in requiredHeaders) table.Columns.Add(header);
                    table.Columns.Add("Month");
                    table.Columns.Add("Year");
                    table.Columns.Add("Wip");

                    var asinRowIndex = new Dictionary<string, int>();

                    int row = 3;
                    while (!string.IsNullOrWhiteSpace(worksheet.Cells[row, columnIndexes["C-ASIN"]].Text))
                    {
                        var newRow = table.NewRow();
                        foreach (var header in requiredHeaders)
                            newRow[header] = worksheet.Cells[row, columnIndexes[header]].Text?.Trim();

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

                    DataTable filteredTable = table.Copy();

                    if (!TryExtractProjectionMonth(filePath, out DateTime projMonth, out string label, out string err))
                    {
                        response.Success = false;
                        response.Message = err;
                        return response;
                    }

                    string forecastFor = projMonth.AddMonths(commitmentPeriod + 1).ToString("MMMM yyyy");

                    response.Success = true;
                    response.Data = new ForecastFileData
                    {
                        FileName = Path.GetFileName(filePath),
                        FilePath = filePath,
                        FullTable = table,
                        FilteredTable = filteredTable,
                        ProjectionMonth = projMonth.ToString("MMMM"),
                        ProjectionYear = projMonth.ToString("yyyy"),
                        ForecastFor = forecastFor
                    };
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
        public bool TryExtractProjectionMonth(string fileName, out DateTime projectionMonth, out string label, out string error)
        {
            error = "";
            projectionMonth = default;
            label = "";

            var parts = Path.GetFileNameWithoutExtension(fileName).Split('_');
            if (parts.Length < 3)
            {
                error = "Could not extract Projection Month from file name.";
                return false;
            }

            string monthStr = parts[parts.Length - 2];
            string yearStr = parts[parts.Length - 1];

            if (DateTime.TryParseExact($"{monthStr} {yearStr}", "MMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out projectionMonth))
            {
                label = $"{monthStr} {yearStr}";
                return true;
            }

            error = "Invalid month/year format in file name.";
            return false;
        }

        #endregion Handle Forecast File

        #region Handle Stock File Upload
        public async Task<Response<DataTable>> HandleStockFile(string filePath, bool stockUploaded)
        {
            var res = new Response<DataTable>();
            if (stockUploaded)
            {
                res.Success = false;
                res.Message = "Stock file already uploaded.";
                res.Data = null;
                return res;
            }

            // import the stock file into datatable
            var loadResponse = await LoadStockDataFromExcelIntoDataTable(filePath);
            if (!loadResponse.Success)
            {
                res.Success = false;
                res.Message = loadResponse.Message;
                return res;
            }

            res.Success = true;
            res.Message = $"Stock file loaded and saved: {filePath}";
            res.Data = loadResponse.Data;
            return res;
        }
        public async Task<Response<DataTable>> LoadStockDataFromExcelIntoDataTable(string filePath)
        {
            var response = new Response<DataTable>();
            var missingItems = new List<Miscellaneous>();
            var matchedStocks = new List<InitialStock>();

            try
            {
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    ExcelWorksheet ws = package.Workbook.Worksheets[0];

                    if (ws == null || ws.Dimension == null)
                    {
                        response.Success = false;
                        response.Message = "The selected Excel sheet is empty.";
                        return response;
                    }

                    DataTable dt = new DataTable();

                    int colCAsin = -1;
                    int colInitialStock = -1;

                    for (int col = 1; col <= ws.Dimension.End.Column; col++)
                    {
                        string header = ws.Cells[1, col].Text.Trim();
                        if (header.Equals("C-ASIN", StringComparison.OrdinalIgnoreCase))
                            colCAsin = col;
                        else if (header.Equals("Initial Stock", StringComparison.OrdinalIgnoreCase))
                            colInitialStock = col;
                    }

                    if (colCAsin == -1 || colInitialStock == -1)
                    {
                        string missingCols = "";
                        if (colCAsin == -1) missingCols += "'C-ASIN' ";
                        if (colInitialStock == -1) missingCols += "'Initial Stock' ";

                        response.Success = false;
                        response.Message = $"Required column(s) {missingCols.Trim()} not found.";
                        return response;
                    }

                    dt.Columns.Add("C-ASIN");
                    dt.Columns.Add("Initial Stock");
                    dt.Columns.Add("Status");

                    for (int row = 2; row <= ws.Dimension.End.Row; row++)
                    {
                        string casin = ws.Cells[row, colCAsin].Text.Trim();
                        string initialStock = ws.Cells[row, colInitialStock].Text.Trim();

                        bool exists = itemsRepository.IsCAsinExistInCatalogue(casin);

                        if (exists)
                        {
                            var item = itemsRepository.GetItemByCAsin(casin);

                            matchedStocks.Add(new InitialStock
                            {
                                ItemCatalogueId = item.Id,
                                Quantity = int.TryParse(initialStock, out var qty) ? qty : 0,
                                Year = DateTime.Now.Year.ToString(),
                                Month = DateTime.Now.ToString("MMMM")
                            });
                        }
                        else
                        {
                            missingItems.Add(new Miscellaneous
                            {
                                FileName = Path.GetFileName(filePath),
                                DetectedAt = DateTime.Now,
                                Type = MiscellaneousType.Stock.ToString(),
                                Casin = casin,
                                Year = DateTime.Now.Year.ToString(),
                                Month = DateTime.Now.ToString("MMMM"),
                                Quantity = initialStock
                            });
                        }

                        DataRow newRow = dt.NewRow();
                        newRow["C-ASIN"] = casin;
                        newRow["Initial Stock"] = initialStock;
                        newRow["Status"] = exists ? "✔" : "❌";
                        dt.Rows.Add(newRow);
                    }

                    // Variables to track save results
                    int savedMiscCount = 0;
                    int savedStocksCount = 0;
                    bool missingItemsSaveSuccess = true;
                    bool stockSaveSuccess = true;

                    // Save missing items if any
                    if (missingItems.Any())
                    {
                        missingItemsSaveSuccess = await miscellaneousRepository.addMiscellaneousAsin(missingItems);
                        if (missingItemsSaveSuccess)
                        {
                            savedMiscCount = missingItems.Count;
                        }
                    }

                    // Save matched stock if any
                    if (matchedStocks.Any())
                    {
                        stockSaveSuccess = stockRepository.SaveInitialStocksToDatabase(matchedStocks);
                        if (stockSaveSuccess)
                        {
                            savedStocksCount = matchedStocks.Count;
                        }
                    }

                    // Build the response message
                    var messages = new List<string> { "Excel loaded successfully." };

                    if (matchedStocks.Any())
                    {
                        if (stockSaveSuccess)
                        {
                            messages.Add($"{savedStocksCount} stock(s) saved successfully.");
                        }
                        else
                        {
                            messages.Add("Failed to save stock(s).");
                        }
                    }

                    if (missingItems.Any())
                    {
                        if (missingItemsSaveSuccess)
                        {
                            messages.Add($"{savedMiscCount} missing item(s) saved.");
                        }
                        else
                        {
                            messages.Add("Failed to save missing item(s).");
                        }
                    }

                    response.Success = (stockSaveSuccess && missingItemsSaveSuccess);
                    response.Message = string.Join(" ", messages);
                    response.Data = dt; // Return the DataTable here

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
        public async Task<Response<DataTable>> HandleOrderFile(string filePath, bool orderUploaded)
        {
            var res = new Response<DataTable>();
            //if (orderUploaded)
            //{
            //    res.Success = false;
            //    res.Message = "Order file already uploaded.";
            //    res.Data = null;
            //    return res;
            //}

            var loadResponse = await LoadOrderDataFromExcelIntoDataTable(filePath);
            if (!loadResponse.Success)
            {
                res.Success = false;
                res.Message = loadResponse.Message;
                return res;
            }

            res.Success = true;
            res.Message = $"Order file loaded and saved: {filePath}";
            res.Data = loadResponse.Data;
            return res;
        }
        public async Task<Response<DataTable>> LoadOrderDataFromExcelIntoDataTable(string filePath)
        {
            var response = new Response<DataTable>();
            var missingItems = new List<Miscellaneous>();
            var matchedOrders = new List<ActualOrder>();

            try
            {
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    ExcelWorksheet ws = package.Workbook.Worksheets[0];

                    if (ws == null || ws.Dimension == null)
                    {
                        response.Success = false;
                        response.Message = "The selected Excel sheet is empty.";
                        return response;
                    }

                    DataTable dt = new DataTable();

                    int colCAsin = -1;
                    int colActualOrder = -1;
                    int colMonth = -1;
                    int colYear = -1;

                    // Identify column indices
                    for (int col = 1; col <= ws.Dimension.End.Column; col++)
                    {
                        string header = ws.Cells[1, col].Text.Trim();
                        if (header.Equals("C-ASIN", StringComparison.OrdinalIgnoreCase))
                            colCAsin = col;
                        else if (header.Equals("Actual Order", StringComparison.OrdinalIgnoreCase))
                            colActualOrder = col;
                        else if (header.Equals("Month", StringComparison.OrdinalIgnoreCase))
                            colMonth = col;
                        else if (header.Equals("Year", StringComparison.OrdinalIgnoreCase))
                            colYear = col;
                    }

                    // Check for missing columns
                    if (colCAsin == -1 || colActualOrder == -1 || colMonth == -1 || colYear == -1)
                    {
                        string missingCols = "";
                        if (colCAsin == -1) missingCols += "'C-ASIN' ";
                        if (colActualOrder == -1) missingCols += "'Actual Order' ";
                        if (colMonth == -1) missingCols += "'Month' ";
                        if (colYear == -1) missingCols += "'Year' ";

                        response.Success = false;
                        response.Message = $"Required column(s) {missingCols.Trim()} not found.";
                        return response;
                    }

                    // Create columns for the DataTable
                    dt.Columns.Add("C-ASIN");
                    dt.Columns.Add("Actual Order");
                    dt.Columns.Add("Month");
                    dt.Columns.Add("Year");
                    dt.Columns.Add("Status");

                    var monthsList = new List<string>();
                    var yearsList = new List<string>();

                    // Process the rows
                    for (int row = 2; row <= ws.Dimension.End.Row; row++)
                    {
                        // Check if the row is completely empty (i.e., no value in critical columns)
                        string casin = ws.Cells[row, colCAsin].Text.Trim();
                        string actualOrderStr = ws.Cells[row, colActualOrder].Text.Trim();
                        string month = ws.Cells[row, colMonth].Text.Trim();
                        string year = ws.Cells[row, colYear].Text.Trim();

                        // Skip the row if all the important columns are empty
                        if (string.IsNullOrWhiteSpace(casin) && string.IsNullOrWhiteSpace(actualOrderStr) &&
                            string.IsNullOrWhiteSpace(month) && string.IsNullOrWhiteSpace(year))
                        {
                            continue; // Skip this row and go to the next one
                        }

                        // Add to a list for validation
                        monthsList.Add(month);
                        yearsList.Add(year);

                        bool exists = itemsRepository.IsCAsinExistInCatalogue(casin);

                        if (exists)
                        {
                            var item = itemsRepository.GetItemByCAsin(casin);

                            matchedOrders.Add(new ActualOrder
                            {
                                ItemCatalogueId = item.Id,
                                Quantity = int.TryParse(actualOrderStr, out var qty) ? qty : 0,
                                Month = month,
                                Year = year
                            });
                        }
                        else
                        {
                            missingItems.Add(new Miscellaneous
                            {
                                FileName = Path.GetFileName(filePath),
                                DetectedAt = DateTime.Now,
                                Type = MiscellaneousType.Order.ToString(),
                                Casin = casin,
                                Month = month,
                                Year = year,
                                Quantity = actualOrderStr
                            });
                        }

                        DataRow newRow = dt.NewRow();
                        newRow["C-ASIN"] = casin;
                        newRow["Actual Order"] = actualOrderStr;
                        newRow["Month"] = month;
                        newRow["Year"] = year;
                        newRow["Status"] = exists ? "✔" : "❌";
                        dt.Rows.Add(newRow);
                    }


                    // Validate months and years
                    // Validate months and years
                    var distinctMonths = monthsList.Distinct().ToList();
                    var distinctYears = yearsList.Distinct().ToList();

                    if (distinctMonths.Count > 1 || distinctYears.Count > 1)
                    {
                        response.Success = false;
                        response.Message = "The Excel file contains multiple months or years. Please ensure the file contains data for only one month and one year.";
                        return response;
                    }


                    // Variables to track save results
                    int missingItemsSavedCount = 0;
                    int ordersSavedCount = 0;
                    bool missingItemsSaveSuccess = true;
                    bool ordersSaveSuccess = true;

                    // Save missing items if any
                    if (missingItems.Any())
                    {
                        missingItemsSaveSuccess = await miscellaneousRepository.addMiscellaneousAsin(missingItems);
                        if (missingItemsSaveSuccess)
                        {
                            missingItemsSavedCount = missingItems.Count;
                        }
                    }

                    // Save matched orders if any
                    if (matchedOrders.Any())
                    {
                        ordersSaveSuccess = await orderRepository.SaveActualOrdersToDatabase(matchedOrders);
                        if (ordersSaveSuccess)
                        {
                            ordersSavedCount = matchedOrders.Count;
                        }
                    }

                    // Build the response message
                    var messages = new List<string> { "Excel loaded successfully." };

                    if (matchedOrders.Any())
                    {
                        if (ordersSaveSuccess)
                        {
                            messages.Add($"{ordersSavedCount} order(s) saved successfully.");
                        }
                        else
                        {
                            messages.Add("Failed to save order(s).");
                        }
                    }

                    if (missingItems.Any())
                    {
                        if (missingItemsSaveSuccess)
                        {
                            messages.Add($"{missingItemsSavedCount} missing item(s) saved.");
                        }
                        else
                        {
                            messages.Add("Failed to save missing item(s).");
                        }
                    }

                    response.Success = (ordersSaveSuccess && missingItemsSaveSuccess);
                    response.Message = string.Join(" ", messages);
                    response.Data = dt; // Return the DataTable here

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
        #endregion Handle Order File Upload


        public ForecastMaster ResolveForecastMaster(ForecastFileData f)
        {
            try
            {
                if (f == null) return null;

                return forecastRepository.GetForecastMasterByFile(f.FileName, f.ProjectionMonth, f.ProjectionYear);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Exception occurred: {ex.Message}" + (ex.InnerException != null ? $" | Inner Exception: {ex.InnerException.Message}" : "");
                MessageBox.Show(errorMessage, "Forecast Resolution Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }


    }

}