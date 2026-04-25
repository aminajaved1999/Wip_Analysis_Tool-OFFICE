using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WIPAT.BLL.Interfaces;
using WIPAT.DAL; // Interfaces
using WIPAT.DAL.Interfaces;
using WIPAT.Entities;
using WIPAT.Entities.BO;
using WIPAT.Entities.Dto;
using WIPAT.Entities.Enum;

namespace WIPAT.BLL.Managers
{
    public class StockManager : IStockManager
    {
        private readonly IStockRepository _stockRepository;
        private readonly IItemsRepository _itemsRepository;
        private readonly IExcelService _excelService;

        public StockManager(IStockRepository stockRepo, IItemsRepository itemsRepo, IExcelService excelService)
        {
            _stockRepository = stockRepo ?? throw new ArgumentNullException(nameof(stockRepo));
            _itemsRepository = itemsRepo ?? throw new ArgumentNullException(nameof(itemsRepo));
            _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));
        }

        public async Task<Response<StockFileResponse>> HandleStockFileAsync(string filePath, WipSession session)
        {
            var response = new Response<StockFileResponse>
            {
                Data = new StockFileResponse
                {
                    DataTable = new DataTable(),
                    MissingStocks = new List<InvalidStock>(),
                    ValidStocks = new List<InitialStock>()
                }
            };

            string fileName = Path.GetFileName(filePath);

            // 1. Setup DataTable for UI Display
            var uiTable = response.Data.DataTable;
            uiTable.Columns.Add("C-ASIN");
            uiTable.Columns.Add("Quantity");
            uiTable.Columns.Add("Status");

            try
            {
                // 2. Validate Excel Structure
                string sheetName = "Stock"; // Or use Enum: ExcelSheetNames.Stock.ToString()
                var requiredCols = new List<string> { "C-ASIN", "Quantity" }; // Or Enum: StockOrderExcelColumns

                var valRes = _excelService.ValidateColumns(filePath, sheetName, requiredCols);
                if (!valRes.Success) return new Response<StockFileResponse> { Success = false, Message = valRes.Message };

                // 3. Read Data
                var readRes = _excelService.ReadExcelToDataTable(filePath, sheetName, requiredCols);
                if (!readRes.Success) return new Response<StockFileResponse> { Success = false, Message = readRes.Message };

                DataTable rawData = readRes.Data;

                // 4. Process Logic (Pure C#)
                var validStocksToSave = new List<InitialStock>();

                foreach (DataRow row in rawData.Rows)
                {
                    string casin = row["C-ASIN"].ToString();
                    string qtyStr = row["Quantity"].ToString();
                    int.TryParse(qtyStr, out int qty);

                    // Check DB
                    var existsRes = await _itemsRepository.IsCasinExistInCatalogueAndInitialStock(casin);

                    if (existsRes.Success)
                    {
                        // Found in DB
                        var item = _itemsRepository.GetItemByCAsin(casin);

                        validStocksToSave.Add(new InitialStock
                        {
                            ItemCatalogueId = item.Id,
                            OpeningStock = qty,
                            CreatedById = session.LoggedInUser.Id,
                            UpdatedAt = DateTime.Now
                        });

                        uiTable.Rows.Add(casin, qtyStr, "✔");
                    }
                    else
                    {
                        // Missing
                        response.Data.MissingStocks.Add(new InvalidStock
                        {
                            Casin = casin,
                            Quantity = qtyStr,
                            FileName = fileName
                        });

                        uiTable.Rows.Add(casin, qtyStr, "❌");
                    }
                }

                // 5. Update Database
                if (validStocksToSave.Any())
                {
                    var updateRes = await _stockRepository.UpdateStockQuantitiesAsync(validStocksToSave);
                    if (!updateRes.Success)
                    {
                        response.Success = false;
                        response.Message = updateRes.Message;
                        return response;
                    }
                }

                // 6. Final Response Construction
                response.Success = true;

                var msgParts = new List<string> { "Stock file processed." };
                if (validStocksToSave.Any()) msgParts.Add($"{validStocksToSave.Count} updated successfully.");
                if (response.Data.MissingStocks.Any()) msgParts.Add($"{response.Data.MissingStocks.Count} invalid items found.");

                response.Message = string.Join(" ", msgParts);
                return response;

            }
            catch (Exception ex)
            {
                return new Response<StockFileResponse> { Success = false, Message = $"Unexpected error: {ex.Message}" };
            }
        }



     
    }
}