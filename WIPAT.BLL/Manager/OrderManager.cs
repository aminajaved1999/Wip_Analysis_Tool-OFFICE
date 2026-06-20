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
using WIPAT.Entities.ExcelTemplateDefinitions;

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

                // FETCH REQUIRED EXCEL IMPORT COLUMNS DIRECTLY FROM DEFINITIONS
                var importRules = FileTemplateFactory.GetImportTemplate(ImportExcelFileType.OrderFile);
                var requiredCols = importRules.Where(r => r.IsHeaderRequired).Select(r => r.Definition.Name).ToList();

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

                var readRes = new DataTableFactory().ReadExcelToDataTable(filePath, sheetName, requiredCols);
                if (!readRes.Success)
                {
                    return new Response<OrderFileResponse> { Success = false, Message = readRes.Message };
                }

                DataTable rawData = readRes.Data;

                var allDbItemsRes = await _itemsRepository.GetActiveItemCatalogues(true);
                var allDbItems = allDbItemsRes.Success && allDbItemsRes.Data != null
                    ? allDbItemsRes.Data.GroupBy(x => x.Casin.Trim(), StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, ItemCatalogue>(StringComparer.OrdinalIgnoreCase);

                #region Process Order Data

                var validOrders = new List<ActualOrder>();
                var missingCasins = new List<string>();
                var deactivatedCasins = new List<string>();

                // Assuming the session object contains a UserId property for the logged-in user
                int loggedInUserId = session.LoggedInUser.Id;

                // 1. Create and populate the ProcessedTable via the separate method
                DataTable processedTable = new DataTableFactory().CreateProcessedOrderDataTable(rawData, allDbItems);

                // 2. Process business logic for Valid Orders and Problem Items
                foreach (DataRow row in rawData.Rows)
                {
                    string casin = row[MasterColumnCatalogue.Casin.Name].ToString().Trim();

                    if (allDbItems.TryGetValue(casin, out var dbItem))
                    {
                        if (dbItem.ItemStatus != (int)CatalogueItemStatus.Active && !valRes.IsContinueWithInactiveItems)
                        {
                            deactivatedCasins.Add(casin);
                        }
                        else
                        {
                            // Only parse strings to integers when we actually need to create an order
                            string qtyStr = row[MasterColumnCatalogue.Quantity.Name].ToString();
                            string monthStr = row[MasterColumnCatalogue.MonthInteger.Name].ToString();
                            string yearStr = row[MasterColumnCatalogue.Year.Name].ToString();

                            int.TryParse(qtyStr, NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out int qty);
                            int.TryParse(monthStr, out int monthNum);

                            string monthName = monthNum >= 1 && monthNum <= 12
                                ? CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(monthNum)
                                : "Invalid";

                            validOrders.Add(new ActualOrder
                            {
                                ItemCatalogueId = dbItem.Id,
                                Quantity = qty,
                                Month = monthName,
                                Year = yearStr,
                                FileName = fileName,
                                CreatedById = loggedInUserId,
                                CreatedAt = DateTime.Now
                            });
                        }
                    }
                    else
                    {
                        missingCasins.Add(casin);
                    }
                }

                // 3. Generate the Problem Items DataTable
                DataTable problemItemsTable = new DataTableFactory().CreateProblemItemsDataTable(missingCasins, deactivatedCasins, fileName);

                #endregion Process Order Data

                // Map local variables to the response object instead of the previous 'processingResult' tuple
                response.Data.DataTable = processedTable;
                response.Data.ValidOrders = validOrders;
                //response.Data.MissingOrders = processingResult.MissingOrders;
                // -----------------------------

                if (response.Data.ValidOrders.Count == 0)
                {
                    return new Response<OrderFileResponse> { Success = false, Message = "No valid orders found in file." };
                }

                //var saveRes = await _orderRepository.SaveOrders(response.Data.ValidOrders);
                var saveRes = await _orderRepository.BulkInsertOrders(processedTable);

                if (!saveRes.Success)
                {
                    return new Response<OrderFileResponse> { Success = false, Message = saveRes.Message };
                }

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

                // FETCH REQUIRED EXCEL IMPORT COLUMNS DIRECTLY FROM DEFINITIONS
                var importRules = FileTemplateFactory.GetImportTemplate(ImportExcelFileType.OrderFile);
                var requiredCols = importRules.Where(r => r.IsHeaderRequired).Select(r => r.Definition.Name).ToList();

                var valRes = await _excelService.ValidateExcelFile(filePath, FileType.Order.ToString(), sheetName, requiredCols);
                if (!valRes.Success)
                {
                    return new Response<OrderFileResponse> { Success = false, Message = valRes.Message };
                }

                var readRes = new DataTableFactory().ReadExcelToDataTable(filePath, sheetName, requiredCols);
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
                    string casin = row[MasterColumnCatalogue.Casin.Name]?.ToString().Trim();
                    string qtyStr = row[MasterColumnCatalogue.Quantity.Name]?.ToString().Trim();
                    string monthStr = row[MasterColumnCatalogue.MonthInteger.Name]?.ToString().Trim();
                    string yearStr = row[MasterColumnCatalogue.Year.Name]?.ToString().Trim();

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