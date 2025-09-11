using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WIPAT.Entities;
using WIPAT.Entities.Dto;

namespace WIPAT.DAL
{
    public class StockRepository
    {
        private ItemsRepository itemsRepository;

        public StockRepository()
        {
            itemsRepository = new ItemsRepository();
        }




        #region stock
        public bool SaveInitialStocksToDatabase(List<InitialStock> stocks)
        {
            try
            {
                using (var context = new WIPATContext())
                {
                    // Delete existing records first
                    context.InitialStocks.RemoveRange(context.InitialStocks);
                    context.SaveChanges();

                    // Add the new records
                    context.InitialStocks.AddRange(stocks);
                    context.SaveChanges();
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }


        public async Task<Response<bool>> SaveStockDataTableToDatabase(DataTable stockTable)
        {
            var response = new Response<bool>();

            try
            {
                using (var context = new WIPATContext())
                {
                    if (stockTable == null || stockTable.Rows.Count == 0)
                    {
                        response.Success = false;
                        response.Message = "No data to save.";
                        response.Data = false;
                        return response;
                    }

                    var stockList = new List<InitialStock>();

                    foreach (DataRow row in stockTable.Rows)
                    {
                        string casin = row["C-ASIN"].ToString().Trim();
                        string quantityStr = row["Initial Stock"].ToString().Trim();

                        // Skip rows where C-ASIN or Quantity is missing
                        if (string.IsNullOrEmpty(casin) || string.IsNullOrEmpty(quantityStr))
                            continue;

                        bool exists = itemsRepository.IsCAsinExistInCatalogue(casin);
                        if (!exists)
                            continue;

                        var item = itemsRepository.GetItemByCAsin(casin);
                        if (item == null)
                            continue;

                        int.TryParse(quantityStr, out int quantity);

                        stockList.Add(new InitialStock
                        {
                            ItemCatalogueId = item.Id,
                            Quantity = quantity
                        });
                    }

                    if (stockList.Any())
                    {
                         context.InitialStocks.AddRange(stockList);
                        await context.SaveChangesAsync();

                        response.Success = true;
                        response.Message = $"{stockList.Count} stock record(s) saved successfully.";
                        response.Data = true;
                    }
                    else
                    {
                        response.Success = false;
                        response.Message = "No valid stock data found to save.";
                        response.Data = false;
                    }
                }
                    
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error saving stock data: {ex.Message}";
                response.Data = false;
            }

            return response;
        }


        public Response<List<InitialStock>> GetInitialStocksFromDatabase()
        {
            try
            {
                using (var context = new WIPATContext())
                {
                    var stocks = context.InitialStocks.ToList();
                    return new Response<List<InitialStock>>()
                    {
                        Success = true,
                        Message = "Initial stocks retrieved successfully.",
                        Data = stocks
                    };
                }
            }
            catch (Exception ex)
            {
                return new Response<List<InitialStock>>()
                {
                    Success = false,
                    Message = $"Error retrieving initial stocks: {ex.Message}",
                    Data = null
                };
            }
        }
        #endregion stock

    }
}
