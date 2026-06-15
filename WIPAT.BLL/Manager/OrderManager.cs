using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WIPAT.BLL.Interfaces;
using WIPAT.DAL; // Interfaces
using WIPAT.DAL.Interfaces;
using WIPAT.Entities;
using WIPAT.Entities.BO;
using WIPAT.Entities.Dto;
using WIPAT.Entities.Entities;
using WIPAT.Entities.Enum;

namespace WIPAT.BLL.Managers
{
    public class OrderManager : IOrderManager
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IItemsRepository _itemsRepository;
        private readonly IExcelService _excelService;

        public OrderManager(IOrderRepository orderRepo, IItemsRepository itemsRepo, IExcelService excelService)
        {
            _orderRepository = orderRepo ?? throw new ArgumentNullException(nameof(orderRepo));
            _itemsRepository = itemsRepo ?? throw new ArgumentNullException(nameof(itemsRepo));
            _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));
        }

        public async Task<Response<OrderFileResponse>> HandleOrderFileAsync(string filePath, WipSession session)
        {
            var response = new Response<OrderFileResponse>
            {
                Data = new OrderFileResponse
                {
                    ValidOrders = new List<ActualOrder>(),
                    MissingOrders = new List<InvalidOrder>(),
                    DataTable = new DataTable()
                }
            };

            string fileName = Path.GetFileName(filePath);

            var requiredMonthYear = session.CurrentMonthWithYear;
            if (string.IsNullOrEmpty(requiredMonthYear))
                return new Response<OrderFileResponse> { Success = false, Message = "Session month is not set. Please upload forecasts first." };

            var parts = requiredMonthYear.Split(' ');
            if (parts.Length < 2)
                return new Response<OrderFileResponse> { Success = false, Message = "Invalid Session Month format." };

            string requiredMonth = parts[0];
            string requiredYear = parts[1];

            try
            {
                var existingFileRes = await _orderRepository.OrderFileExists(fileName, requiredMonth, requiredYear);

                if (existingFileRes.Success)
                {
                    var dbData = _orderRepository.GetExistingOrderData(fileName, requiredMonth, requiredYear);

                    if (!dbData.Success)
                        return new Response<OrderFileResponse> { Success = false, Message = dbData.Message };

                    response.Success = true;
                    response.Data.DataTable = dbData.Data.Item1;
                    response.Data.ValidOrders = dbData.Data.Item2;
                    response.Message = $"⚠️ Orders for {requiredMonth} {requiredYear} already exist. Loaded from Database.";
                    return response;
                }

                string sheetName = ConfigurationManager.AppSettings["OrderWorksheetName"];
                if (string.IsNullOrWhiteSpace(sheetName))
                {
                    return new Response<OrderFileResponse> { Success = false, Message = "The worksheet name configuration ('OrderWorksheetName') is missing or empty in the App.config file." };
                }

                var requiredCols = new List<string> { "CASIN", "Quantity", "Month", "Year" };

                var valRes = await _excelService.ValidateExcelFile(filePath, FileType.Order.ToString(), sheetName, requiredCols, requiredMonth, requiredYear);

                if (!valRes.Success)
                {
                    response.Success = false;
                    response.Message = valRes.Message;
                    response.Data = new OrderFileResponse();
                    response.Data.ProblemItemsTable = valRes.ProblemItemsTable;
                    response.Data.DeactivatedItems = valRes.DeactivatedItems;
                    response.Data.MissingItems = valRes.MissingItems;
                    return response;
                }

                var readRes = _excelService.ReadExcelToDataTable(filePath, sheetName, requiredCols);
                if (!readRes.Success) return new Response<OrderFileResponse> { Success = false, Message = readRes.Message };

                DataTable rawData = readRes.Data;

                foreach (var col in requiredCols) response.Data.DataTable.Columns.Add(col);

                response.Data.DataTable.Columns.Add("IsActive", typeof(bool));
                response.Data.DataTable.Columns.Add("ItemStatus", typeof(string));
                response.Data.DataTable.Columns.Add("Status");

                // ---> UPDATED: Fetch full DB Items instead of just IDs <---
                var allDbItemsRes = await _itemsRepository.GetActiveItemCatalogues(true);
                var allDbItems = allDbItemsRes.Success && allDbItemsRes.Data != null
                    ? allDbItemsRes.Data.GroupBy(x => x.Casin.Trim(), StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, ItemCatalogue>(StringComparer.OrdinalIgnoreCase);
                // ----------------------------------------------------------

                foreach (DataRow row in rawData.Rows)
                {
                    string casin = row["CASIN"].ToString().Trim();
                    string qtyStr = row["Quantity"].ToString();
                    string monthStr = row["Month"].ToString();
                    string yearStr = row["Year"].ToString();

                    DataRow uiRow = response.Data.DataTable.NewRow();
                    uiRow["CASIN"] = casin;
                    uiRow["Quantity"] = qtyStr;
                    uiRow["Month"] = monthStr;
                    uiRow["Year"] = yearStr;

                    int.TryParse(qtyStr, NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out int qty);
                    int.TryParse(monthStr, out int monthNum);

                    string monthName = monthNum >= 1 && monthNum <= 12
                        ? CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(monthNum)
                        : "Invalid";

                    // ---> UPDATED: Directly map from the DB entity <---
                    if (allDbItems.TryGetValue(casin, out var dbItem))
                    {
                        uiRow["IsActive"] = dbItem.isActive;
                        uiRow["ItemStatus"] = dbItem.ItemStatus;

                        if (!dbItem.isActive && !valRes.IsContinueWithInactiveItems)
                        {
                            // Item is deactivated, and user did NOT explicitly choose to ignore it
                            uiRow["Status"] = "Deactivated ⚠️";
                            response.Data.MissingOrders.Add(new InvalidOrder
                            {
                                Casin = casin,
                                Quantity = qtyStr,
                                Month = monthName,
                                Year = yearStr,
                                FileName = fileName,
                                Reason = "Deactivated in Catalogue"
                            });
                        }
                        else
                        {
                            // Valid Item (or Deactivated item that user opted to Continue/Ignore)
                            uiRow["Status"] = "Valid ✔";
                            response.Data.ValidOrders.Add(new ActualOrder
                            {
                                ItemCatalogueId = dbItem.Id,
                                Quantity = qty,
                                Month = monthName,
                                Year = yearStr,
                                FileName = fileName,
                                CreatedById = session.LoggedInUser.Id,
                                CreatedAt = DateTime.Now
                            });
                        }
                    }
                    else
                    {
                        // Item is totally missing from the DB
                        uiRow["IsActive"] = false;
                        uiRow["ItemStatus"] = "Missing";
                        uiRow["Status"] = "Missing ❌";
                        response.Data.MissingOrders.Add(new InvalidOrder
                        {
                            Casin = casin,
                            Quantity = qtyStr,
                            Month = monthName,
                            Year = yearStr,
                            FileName = fileName,
                            Reason = "Missing in Catalogue"
                        });
                    }
                    // --------------------------------------------------

                    response.Data.DataTable.Rows.Add(uiRow);
                }

                if (response.Data.ValidOrders.Count == 0)
                {
                    return new Response<OrderFileResponse> { Success = false, Message = "No valid orders found in file." };
                }

                var saveRes = await _orderRepository.SaveOrdersAndUpdateStock(response.Data.ValidOrders);

                if (!saveRes.Success)
                    return new Response<OrderFileResponse> { Success = false, Message = saveRes.Message };

                response.Success = true;
                string ignoreNote = valRes.IsContinueWithInactiveItems ? $" (Ignored {valRes.DeactivatedItems.Count} deactivated items)" : "";
                response.Message = $"Orders uploaded and saved successfully.{ignoreNote}";
                return response;
            }
            catch (Exception ex)
            {
                return new Response<OrderFileResponse> { Success = false, Message = $"Unexpected error: {ex.Message}" };
            }
        }
        public async Task<Response<OrderFileResponse>> __HandleOrderFileAsync(string filePath, WipSession session)
        {
            var response = new Response<OrderFileResponse>
            {
                Data = new OrderFileResponse
                {
                    ValidOrders = new List<ActualOrder>(),
                    MissingOrders = new List<InvalidOrder>(), // Now populated with both Missing & Deactivated
                    DataTable = new DataTable()
                }
            };

            string fileName = Path.GetFileName(filePath);

            #region 1. Pre-Check: Session Month
            var requiredMonthYear = session.CurrentMonthWithYear;
            if (string.IsNullOrEmpty(requiredMonthYear))
                return new Response<OrderFileResponse> { Success = false, Message = "Session month is not set. Please upload forecasts first." };

            var parts = requiredMonthYear.Split(' ');
            if (parts.Length < 2)
                return new Response<OrderFileResponse> { Success = false, Message = "Invalid Session Month format." };

            string requiredMonth = parts[0];
            string requiredYear = parts[1];
            #endregion

            try
            {
                #region 2. Check Database for Existing Data
                var existingFileRes = await _orderRepository.OrderFileExists(fileName, requiredMonth, requiredYear);

                if (existingFileRes.Success)
                {
                    // DATA EXISTS -> Load from DB
                    var dbData = _orderRepository.GetExistingOrderData(fileName, requiredMonth, requiredYear);

                    if (!dbData.Success)
                        return new Response<OrderFileResponse> { Success = false, Message = dbData.Message };

                    response.Success = true;
                    response.Data.DataTable = dbData.Data.Item1;
                    response.Data.ValidOrders = dbData.Data.Item2;
                    response.Message = $"⚠️ Orders for {requiredMonth} {requiredYear} already exist. Loaded from Database.";
                    return response;
                }
                #endregion

                #region 3. Parse Excel File
                // Define Requirements
                string sheetName = ConfigurationManager.AppSettings["OrderWorksheetName"];
                if (string.IsNullOrWhiteSpace(sheetName))
                {
                    return new Response<OrderFileResponse> { Success = false, Message = "The worksheet name configuration ('OrderWorksheetName') is missing or empty in the App.config file." };
                }
                var requiredCols = new List<string> { "CASIN", "Quantity", "Month", "Year" };

                // Validate Structure
                var valRes = await _excelService.ValidateExcelFile(filePath, FileType.Order.ToString(), sheetName, requiredCols);

                // Check if the error is structural (e.g. missing columns) vs catalogue logic (missing/deactivated items)
                bool hasCatalogueIssues = (valRes.MissingItems != null && valRes.MissingItems.Any()) ||
                                          (valRes.DeactivatedItems != null && valRes.DeactivatedItems.Any());

                // ONLY abort immediately if it's a hard structural error. Let catalogue issues pass so we can build the GridView.
                if (!valRes.Success && !hasCatalogueIssues)
                {
                    return new Response<OrderFileResponse> { Success = false, Message = valRes.Message };
                }

                // Read Data
                var readRes = _excelService.ReadExcelToDataTable(filePath, sheetName, requiredCols);
                if (!readRes.Success) return new Response<OrderFileResponse> { Success = false, Message = readRes.Message };

                DataTable rawData = readRes.Data;
                #endregion

                #region 4. Process Logic (Validate Items & Create Objects)

                // Prepare UI DataTable columns
                foreach (var col in requiredCols) response.Data.DataTable.Columns.Add(col);
                response.Data.DataTable.Columns.Add("IsActive", typeof(bool));    // <--- ADD THIS
                response.Data.DataTable.Columns.Add("ItemStatus", typeof(string)); // <--- ADD THIS
                response.Data.DataTable.Columns.Add("Status"); // Existing GridView visual status

                // --- BULK FETCH LOGIC ---
                var distinctCasins = rawData.AsEnumerable()
                    .Select(row => row.Field<string>("CASIN")?.Trim())
                    .Where(c => !string.IsNullOrEmpty(c))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                // Fetch all required IDs in one database call to prevent N+1 queries
                Dictionary<string, int> catalogueItemsDict = await _itemsRepository.GetCatalogueIdsByCasinsAsync(distinctCasins);

                foreach (DataRow row in rawData.Rows)
                {
                    string casin = row["CASIN"].ToString().Trim();
                    string qtyStr = row["Quantity"].ToString();
                    string monthStr = row["Month"].ToString();
                    string yearStr = row["Year"].ToString();

                    int.TryParse(qtyStr, NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out int qty);
                    int.TryParse(monthStr, out int monthNum);

                    // Convert numeric month to Name (e.g., 1 -> January)
                    string monthName = monthNum >= 1 && monthNum <= 12
                        ? CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(monthNum)
                        : "Invalid";

                    DataRow uiRow = response.Data.DataTable.NewRow();
                    uiRow["CASIN"] = casin;
                    uiRow["Quantity"] = qtyStr;
                    uiRow["Month"] = monthStr;
                    uiRow["Year"] = yearStr;

                    // --- CHECK STATUS USING valRes LISTS ---
                    bool isMissing = valRes.MissingItems?.Contains(casin, StringComparer.OrdinalIgnoreCase) == true;
                    bool isDeactivated = valRes.DeactivatedItems?.Contains(casin, StringComparer.OrdinalIgnoreCase) == true;

                    if (isMissing)
                    {
                        uiRow["Status"] = "Missing ❌";
                        response.Data.MissingOrders.Add(new InvalidOrder
                        {
                            Casin = casin,
                            Quantity = qtyStr,
                            Month = monthName,
                            Year = yearStr,
                            FileName = fileName,
                            Reason = "Missing in Catalogue"
                        });
                    }
                    else if (isDeactivated)
                    {
                        uiRow["Status"] = "Deactivated ⚠️";
                        response.Data.MissingOrders.Add(new InvalidOrder
                        {
                            Casin = casin,
                            Quantity = qtyStr,
                            Month = monthName,
                            Year = yearStr,
                            FileName = fileName,
                            Reason = "Deactivated in Catalogue"
                        });
                    }
                    else if (catalogueItemsDict.TryGetValue(casin, out int itemId))
                    {
                        uiRow["Status"] = "Valid ✔";
                        response.Data.ValidOrders.Add(new ActualOrder
                        {
                            ItemCatalogueId = itemId,
                            Quantity = qty,
                            Month = monthName,
                            Year = yearStr,
                            FileName = fileName,
                            CreatedById = session.LoggedInUser.Id,
                            CreatedAt = DateTime.Now
                        });
                    }
                    else
                    {
                        // Fallback catch-all
                        uiRow["Status"] = "Error ❌";
                    }

                    response.Data.DataTable.Rows.Add(uiRow);
                }

                // --- DEFERRED ERROR RETURN ---
                // If there were catalogue issues, we return failure NOW, but with the populated DataTable included
                if (!valRes.Success)
                {
                    response.Success = false;
                    response.Message = valRes.Message;
                    return response;
                }

                if (response.Data.ValidOrders.Count == 0)
                {
                    response.Success = false;
                    response.Message = "No valid orders found in file.";
                    return response;
                }
                #endregion

                #region 5. Save to Database
                var saveRes = await _orderRepository.SaveOrdersAndUpdateStock(response.Data.ValidOrders);

                if (!saveRes.Success)
                    return new Response<OrderFileResponse> { Success = false, Message = saveRes.Message };

                // Success!
                response.Success = true;
                string ignoreNote = valRes.DeactivatedItems?.Any() == true ? $" (Ignored {valRes.DeactivatedItems.Count} deactivated items)" : "";
                response.Message = $"Orders uploaded and saved successfully.{ignoreNote}";
                return response;
                #endregion
            }
            catch (Exception ex)
            {
                return new Response<OrderFileResponse> { Success = false, Message = $"Unexpected error: {ex.Message}" };
            }
        }
        public async Task<Response<OrderFileResponse>> _HandleOrderFileAsync(string filePath, WipSession session)
        {
            var response = new Response<OrderFileResponse>
            {
                Data = new OrderFileResponse
                {
                    ValidOrders = new List<ActualOrder>(),
                    MissingOrders = new List<InvalidOrder>(),
                    DataTable = new DataTable()
                }
            };

            string fileName = Path.GetFileName(filePath);

            #region 1. Pre-Check: Session Month
            var requiredMonthYear = session.CurrentMonthWithYear;
            if (string.IsNullOrEmpty(requiredMonthYear))
                return new Response<OrderFileResponse> { Success = false, Message = "Session month is not set. Please upload forecasts first." };

            var parts = requiredMonthYear.Split(' ');
            if (parts.Length < 2)
                return new Response<OrderFileResponse> { Success = false, Message = "Invalid Session Month format." };

            string requiredMonth = parts[0];
            string requiredYear = parts[1];
            #endregion

            try
            {
                #region 2. Check Database for Existing Data
                var existingFileRes = await _orderRepository.OrderFileExists(fileName, requiredMonth, requiredYear);

                if (existingFileRes.Success)
                {
                    // DATA EXISTS -> Load from DB
                    var dbData = _orderRepository.GetExistingOrderData(fileName, requiredMonth, requiredYear);

                    if (!dbData.Success)
                        return new Response<OrderFileResponse> { Success = false, Message = dbData.Message };

                    response.Success = true;
                    response.Data.DataTable = dbData.Data.Item1;
                    response.Data.ValidOrders = dbData.Data.Item2;
                    response.Message = $"⚠️ Orders for {requiredMonth} {requiredYear} already exist. Loaded from Database.";
                    return response;
                }
                #endregion

                #region 3. Parse Excel File
                // Define Requirements



                //string sheetName = "Order"; // Or ExcelSheetNames.Order.ToString
                string sheetName = ConfigurationManager.AppSettings["OrderWorksheetName"];
                if (string.IsNullOrWhiteSpace(sheetName))
                {
                    response.Success = false;
                    response.Message = "The worksheet name configuration ('OrderWorksheetName') is missing or empty in the App.config file.";
                    return response;
                }
                var requiredCols = new List<string> { "CASIN", "Quantity", "Month", "Year" }; // StockOrderExcelColumns

                // Validate Structure
                var valRes = await _excelService.ValidateExcelFile(filePath,FileType.Order.ToString(), sheetName, requiredCols);
                if (!valRes.Success) return new Response<OrderFileResponse> { Success = false, Message = valRes.Message };

                // Read Data
                var readRes = _excelService.ReadExcelToDataTable(filePath, sheetName, requiredCols);
                if (!readRes.Success) return new Response<OrderFileResponse> { Success = false, Message = readRes.Message };

                DataTable rawData = readRes.Data;
                #endregion

                #region 4. Process Logic (Validate Items & Create Objects)

                // Prepare UI DataTable columns
                foreach (var col in requiredCols) response.Data.DataTable.Columns.Add(col);
                response.Data.DataTable.Columns.Add("Status");

                foreach (DataRow row in rawData.Rows)
                {
                    string casin = row["CASIN"].ToString();
                    string qtyStr = row["Quantity"].ToString();
                    string monthStr = row["Month"].ToString();
                    string yearStr = row["Year"].ToString();

                    int.TryParse(qtyStr, NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out int qty);
                    int.TryParse(monthStr, out int monthNum);

                    // Convert numeric month to Name (e.g., 1 -> January)
                    string monthName = monthNum >= 1 && monthNum <= 12
                        ? CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(monthNum)
                        : "Invalid";

                    // Validate Month Match with Session
                    if (!string.Equals(monthName, requiredMonth, StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(yearStr, requiredYear, StringComparison.OrdinalIgnoreCase))
                    {
                        // Optional: Fail immediately or skip? 
                        // For now, let's mark as invalid row but continue processing
                    }

                    // Check Item Existence
                    var itemExist = await _itemsRepository.IsItemExistInCatalogue(casin);

                    DataRow uiRow = response.Data.DataTable.NewRow();
                    uiRow["CASIN"] = casin;
                    uiRow["Quantity"] = qtyStr;
                    uiRow["Month"] = monthStr;
                    uiRow["Year"] = yearStr;

                    if (itemExist.Success)
                    {
                        var item = _itemsRepository.GetItemByCAsin(casin);
                        response.Data.ValidOrders.Add(new ActualOrder
                        {
                            ItemCatalogueId = item.Id,
                            Quantity = qty,
                            Month = monthName,
                            Year = yearStr,
                            FileName = fileName,
                            CreatedById = session.LoggedInUser.Id,
                            CreatedAt = DateTime.Now
                        });
                        uiRow["Status"] = "✔";
                    }
                    else
                    {
                        response.Data.MissingOrders.Add(new InvalidOrder
                        {
                            Casin = casin,
                            Quantity = qtyStr,
                            Month = monthName,
                            Year = yearStr,
                            FileName = fileName
                        });
                        uiRow["Status"] = "❌";
                    }
                    response.Data.DataTable.Rows.Add(uiRow);
                }

                if (response.Data.MissingOrders.Count > 0)
                {
                    response.Success = false;
                    response.Message = $"Found {response.Data.MissingOrders.Count} invalid orders (missing items). Please fix and retry.";
                    return response;
                }

                if (response.Data.ValidOrders.Count == 0)
                {
                    response.Success = false;
                    response.Message = "No valid orders found in file.";
                    return response;
                }
                #endregion

                #region 5. Save to Database
                var saveRes = await _orderRepository.SaveOrdersAndUpdateStock(response.Data.ValidOrders);

                if (!saveRes.Success)
                    return new Response<OrderFileResponse> { Success = false, Message = saveRes.Message };

                // Success!
                response.Success = true;
                response.Message = "Orders uploaded and saved successfully.";
                return response;
                #endregion
            }
            catch (Exception ex)
            {
                return new Response<OrderFileResponse> { Success = false, Message = $"Unexpected error: {ex.Message}" };
            }
        }

        public async Task<Response<DataTable>> LoadExistingOrderAsync(string month, string year)
        {
            return await Task.Run(() => _orderRepository.GetOrderDataByMonthYear(month, year));
        }


        public async Task<Response<OrderFileResponse>> ValidateOrderAsync(string filePath, OrderMasterDto masterInfo)
        {
            var response = new Response<OrderFileResponse>
            {
                Success = true,
                Data = new OrderFileResponse
                {
                    ValidOrderItems = new List<ValidOrder>(),
                    InvalidOrderItems = new List<InvalidOrder>()
                }
            };

            try
            {
                // 1. DUPLICATE CHECK
                var duplicateCheck = _orderRepository.IsDocNoExists(masterInfo.DocNo, masterInfo.DocType);
                if (!duplicateCheck.Success || duplicateCheck.Data)
                {
                    response.Success = false;
                    response.Message = duplicateCheck.Data
                        ? $"Document Number '{masterInfo.DocNo}' already exists."
                        : duplicateCheck.Message;
                    return response;
                }

                // 2. VALIDATE & READ EXCEL
                string sheetName = ExcelSheetNames.Order.ToString();
                var requiredCols = new List<string>
                {
                    StockOrderExcelColumns.CASIN.ToString(),
                    StockOrderExcelColumns.Quantity.ToString(),
                    StockOrderExcelColumns.Month.ToString(),
                    StockOrderExcelColumns.Year.ToString()
                };

                var valRes = await _excelService.ValidateExcelFile(filePath, FileType.Order.ToString(), sheetName, requiredCols);
                if (!valRes.Success)
                {
                    return new Response<OrderFileResponse> { Success = false, Message = valRes.Message };
                }

                var readRes = _excelService.ReadExcelToDataTable(filePath, sheetName, requiredCols);
                if (!readRes.Success)
                {
                    return new Response<OrderFileResponse> { Success = false, Message = readRes.Message };
                }

                // 3. BATCH FETCH & CACHE
                var allItemsResponse = await _itemsRepository.GetActiveItemCatalogues();
                var allDbItems = (allItemsResponse.Success && allItemsResponse.Data != null) ? allItemsResponse.Data : new List<ItemCatalogue>();
                var itemsCache = allDbItems.GroupBy(x => x.Casin).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                // 4. VALIDATION LOOP
                foreach (DataRow row in readRes.Data.Rows)
                {
                    string casin = row[StockOrderExcelColumns.CASIN.ToString()]?.ToString().Trim();
                    string qtyStr = row[StockOrderExcelColumns.Quantity.ToString()]?.ToString().Trim();
                    string monthStr = row[StockOrderExcelColumns.Month.ToString()]?.ToString().Trim();
                    string yearStr = row[StockOrderExcelColumns.Year.ToString()]?.ToString().Trim();

                    if (string.IsNullOrWhiteSpace(casin))
                    {
                        continue;
                    }

                    bool itemExists = itemsCache.TryGetValue(casin, out var item);
                    bool isQtyValid = int.TryParse(qtyStr, out int qty) && qty > 0;
                    bool isMonthValid = int.TryParse(monthStr, out int month) && month >= 1 && month <= 12;
                    bool isYearValid = int.TryParse(yearStr, out int year) && year > 0;

                    if (!itemExists || !isQtyValid || !isMonthValid || !isYearValid)
                    {
                        response.Data.InvalidOrderItems.Add(new InvalidOrder
                        {
                            Casin = casin,
                            Quantity = qtyStr,
                            Month = masterInfo.Month,
                            Year = masterInfo.Year,
                            FileName = masterInfo.FileName
                        });
                    }
                    else
                    {
                        response.Data.ValidOrderItems.Add(new ValidOrder
                        {
                            ItemCatalogueId = item.Id,
                            Casin = casin,
                            Quantity = qtyStr,
                            Month = monthStr,
                            Year = yearStr,
                            FileName = masterInfo.FileName
                        });
                    }
                }

                // Summary Counts
                response.Data.ValidOrderCount = response.Data.ValidOrderItems.Count;
                response.Data.InvalidOrderCount = response.Data.InvalidOrderItems.Count;
                response.Data.TotalOrderCount = response.Data.ValidOrderCount + response.Data.InvalidOrderCount;

                return response;
            }
            catch (Exception ex)
            {
                return new Response<OrderFileResponse> { Success = false, Message = $"Validation Error: {ex.Message}" };
            }
        }

        public async Task<Response<bool>> ConfirmOrderAsync(OrderMasterDto masterInfo, List<ValidOrder> validItems, WipSession session)
        {
            var orderDetailEnities = new List<OrderDetail>();

            try
            {
                // Final sanity check
                if (validItems == null || validItems.Count == 0)
                    return new Response<bool> { Success = false, Message = "No valid items to save." };

                var orderMasterEntity = new OrderMaster
                {
                    Month = masterInfo.Month,
                    Year = masterInfo.Year,
                    DocType = masterInfo.DocType,
                    DocNo = masterInfo.DocNo,
                    FileName = masterInfo.FileName,
                    CreatedAt = DateTime.Now,
                    CreatedById = session.LoggedInUser.Id
                };

                // Ensure detail items have metadata
                foreach (var item in validItems)
                {
                    var orderDetail = new OrderDetail
                    {
                        ItemCatalogueId = item.ItemCatalogueId,
                        Quantity = int.Parse(item.Quantity),
                        DocType = masterInfo.DocType,
                        DocNo = masterInfo.DocNo,
                        CreatedAt = DateTime.Now,
                        CreatedById = session.LoggedInUser.Id
                    };
                    orderDetailEnities.Add(orderDetail);
                }

                var saveResult = await _orderRepository.ExecuteOrderInsertion(orderMasterEntity, orderDetailEnities);
                return new Response<bool> { Success = saveResult.Success, Message = saveResult.Message };
            }
            catch (Exception ex)
            {
                return new Response<bool> { Success = false, Message = $"Save Error: {ex.Message}" };
            }
        }

        public async Task<Response<bool>> RunFillAndKillAsync(OrderMasterDto masterInfo, List<ValidOrder> validItems, WipSession session)
        {
            var orderDetailEnities = new List<OrderDetail>();

            try
            {
                // Final sanity check
                if (validItems == null || validItems.Count == 0)
                    return new Response<bool> { Success = false, Message = "No valid items to save." };

                var orderMasterEntity = new OrderMaster
                {
                    Month = masterInfo.Month,
                    Year = masterInfo.Year,
                    DocType = masterInfo.DocType,
                    DocNo = masterInfo.DocNo,
                    FileName = masterInfo.FileName,
                    CreatedAt = DateTime.Now,
                    CreatedById = session.LoggedInUser.Id
                };

                // Ensure detail items have metadata
                foreach (var item in validItems)
                {
                    var orderDetail = new OrderDetail
                    {
                        DocType = masterInfo.DocType,
                        DocNo = masterInfo.DocNo,
                        ItemCatalogueId = item.ItemCatalogueId,
                        Quantity = int.Parse(item.Quantity),
                        CreatedAt = DateTime.Now,
                        CreatedById = session.LoggedInUser.Id
                    };
                    orderDetailEnities.Add(orderDetail);
                }

                var saveResult = await _orderRepository.ExecuteOrderInsertion(orderMasterEntity, orderDetailEnities);
                return new Response<bool> { Success = saveResult.Success, Message = saveResult.Message };
            }
            catch (Exception ex)
            {
                return new Response<bool> { Success = false, Message = $"Save Error: {ex.Message}" };
            }
        }


    }
}