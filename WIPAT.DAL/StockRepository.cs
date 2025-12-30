using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using WIPAT.Entities;
using WIPAT.Entities.Dto;

namespace WIPAT.DAL
{
    public class StockRepository
    {
        private ItemsRepository itemsRepository;
        private WipSession _session;
        public StockRepository(WipSession session)
        {
            itemsRepository = new ItemsRepository();
            _session = session;
        }


        #region stock
        public bool SaveInitialStocksToDatabase(List<InitialStock> stocks)
        {
            try
            {
                using (var context = new WIPATContext())
                {

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

        #endregion stock


        // update Quantity Column
        public async Task<Response<bool>> UpdateStockQuantitiesAsync(List<InitialStock> stocks)
        {
            var response = new Response<bool>();
            try
            {
                int isSaved = 0;

                using (var context = new WIPATContext())
                {
                    using (var transaction = context.Database.BeginTransaction())
                    {
                        try
                        {
                            foreach (var stock in stocks)
                            {
                                var existingStock = await context.InitialStocks.FirstOrDefaultAsync(s => s.ItemCatalogueId == stock.ItemCatalogueId);

                                if (existingStock != null)
                                {
                                    existingStock.OpeningStock = stock.OpeningStock;
                                    existingStock.UpdatedAt = DateTime.Now;
                                    existingStock.UpdatedById = _session.LoggedInUser.Id;

                                    isSaved = await context.SaveChangesAsync();

                                    if (isSaved <= 0)
                                    {
                                        transaction.Rollback();
                                        response.Success = false;
                                        response.Data = false;
                                        response.Message = $"Failed to update record with ItemCatalogueId: '{existingStock.ItemCatalogueId}'";
                                        return response;
                                    }
                                }
                                else
                                {
                                    transaction.Rollback();
                                    response.Success = false;
                                    response.Data = false;
                                    response.Message = $"ItemCatalogueId {stock.ItemCatalogueId} not found.";
                                    return response;
                                }
                            }

                            transaction.Commit();

                            response.Success = true;
                            response.Data = true;
                            response.Message = "Stock quantities updated successfully.";
                            return response;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();

                            response.Success = false;
                            response.Data = false;
                            response.Message = $"An error occurred while updating stock quantities: {ex.Message}. Please try again or contact support if the issue persists.";
                            return response;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Data = false;
                response.Message = $"An error occurred while processing your request: {ex.Message}. Please try again or contact support if the issue persists.";
                return response;
            }
        }
      
        public async Task<Response<bool>> UpdateStockQtyInStockTable(DataTable stockDataTable, string wipColName, string month, string year)
        {
            var response = new Response<bool>();
            
            DateTime currentDate = DateTime.Parse($"01 {month} {year}");
            DateTime previousDate = currentDate.AddMonths(-1);
            string previousMonth = previousDate.ToString("MMMM");
            string previousYear = previousDate.Year.ToString();


            //required col
            string casinCol = "C-ASIN";
            string stockCol = "Initial_Stock";
            string orderCol = "Actual_Order";
            string productionCol = $"Wip ({previousMonth})";
            string commitmentPeriodCol = $"CommitmentPeriod ({month})";


            string commitmentPeriod = "0";

            try
            {
                #region Input Validation
                // Validate inputs
                if (stockDataTable == null || stockDataTable.Rows.Count == 0)
                {
                    response.Success = false;
                    response.Data = false;
                    response.Message = "The input data table is null or empty. Please provide a valid data table.";
                    return response;
                }

                // Check if required columns exist
                if (!stockDataTable.Columns.Contains(casinCol))
                {
                    response.Success = false;
                    response.Data = false;
                    response.Message = $"The input data table is missing the required column '{casinCol}'. " +
                                        "Please ensure the column exists in the data table.";
                    return response;
                }

                if (!stockDataTable.Columns.Contains(stockCol))
                {
                    response.Success = false;
                    response.Data = false;
                    response.Message = $"The input data table is missing the required column '{stockCol}'. " +
                                        "Please ensure the column exists in the data table.";
                    return response;
                }


                if (!stockDataTable.Columns.Contains(orderCol))
                {
                    response.Success = false;
                    response.Data = false;
                    response.Message = $"The input data table is missing the required column '{orderCol}'. " +
                                        "Please ensure the column exists in the data table.";
                    return response;
                }

                if (!stockDataTable.Columns.Contains(productionCol))
                {
                    response.Success = false;
                    response.Data = false;
                    response.Message = $"The input data table is missing the required column '{productionCol}'. " +
                                        "Please ensure the column exists in the data table.";
                    return response;
                }

                if (!stockDataTable.Columns.Contains(commitmentPeriodCol))
                {
                    response.Success = false;
                    response.Data = false;
                    response.Message = $"The input data table is missing the required column '{commitmentPeriodCol}'. " +
                                        "Please ensure the column exists in the data table.";
                    return response;
                }
                #endregion Input Validation

                #region Database Operations
                int isSaved = 0;
                using (var context = new WIPATContext())
                {
                    using (var transaction = context.Database.BeginTransaction())
                    {
                        try
                        {
                            DataRow[] distinctCASIN = stockDataTable.DefaultView.ToTable(true, casinCol).Select();
                            foreach (var item in distinctCASIN)
                            {
                                var Casin = item[casinCol]?.ToString();
                                if (string.IsNullOrEmpty(Casin))
                                {
                                    transaction.Rollback();
                                    response.Success = false;
                                    response.Data = false;
                                    response.Message = "A null or empty 'Casin' value was found in the input data. " +
                                                        "Please ensure all rows have a valid 'Casin' value.";
                                    return response;
                                }

                                #region Filter Rows for Specific CASIN and Commitment Period

                                string filterExpression = $"[{casinCol}] = '{Casin.Replace("'", "''")}' and [{commitmentPeriodCol}] = {commitmentPeriod}";
                                DataRow[] filteredRows = stockDataTable.Select(filterExpression);

                                if (filteredRows == null || filteredRows.Length == 0)
                                {
                                    string commitmentPeriod_ = "1";
                                    filterExpression = $"[{casinCol}] = '{Casin.Replace("'", "''")}' and [{commitmentPeriodCol}] = {commitmentPeriod_}";
                                    filteredRows = stockDataTable.Select(filterExpression);
                                    if (filteredRows == null || filteredRows.Length == 0)
                                    {
                                        transaction.Rollback();
                                        response.Success = false;
                                        response.Data = false;
                                        response.Message = $"No matching rows found for 'Casin' '{Casin}' with {commitmentPeriodCol}) '{commitmentPeriod}'. " +
                                                            $"Please check the input data for consistency.";
                                        return response;
                                    }
                                }
                                #endregion Filter Rows for Specific CASIN and Commitment Period


                                if (Casin == "B06XW578NR")
                                {
                                    var x = 1;
                                }

                                if (Casin == "B09KP2R32K")
                                {
                                    var x = 2;

                                }

                                if (Casin == "B09TPL48K8")
                                {
                                    var x = 2;
                                }

                                #region Parse Stock Quantity
                                // Attempt to parse stock quantity safely
                                var orderValue = filteredRows[0][orderCol]?.ToString();
                                var productionValue = filteredRows[0][productionCol]?.ToString();
                                var stockValue = filteredRows[0][stockCol]?.ToString();

                                int orderQty, productionQty, stockQty;

                                #region convert to int
                                // --- Validate orderQty ---
                                if (string.IsNullOrWhiteSpace(orderValue) || !int.TryParse(orderValue, out orderQty))
                                {
                                    transaction.Rollback();
                                    response.Success = false;
                                    response.Data = false;
                                    response.Message =
                                        $"The order quantity for 'Casin' '{Casin}' is invalid or missing. Please provide valid order quantity.";
                                    return response;
                                }

                                //---Validate productionQty-- -
                                if (string.IsNullOrWhiteSpace(productionValue) || !int.TryParse(productionValue, out productionQty))
                                {
                                    // If invalid or missing → consider it as 0
                                    productionQty = 0;
                                }
                                //// --- Validate productionQty ---
                                //if (string.IsNullOrWhiteSpace(productionValue) || !int.TryParse(productionValue, out productionQty))
                                //{
                                //    transaction.Rollback();
                                //    response.Success = false;
                                //    response.Data = false;
                                //    response.Message =
                                //        $"The production quantity for 'Casin' '{Casin}' is invalid or missing. Please provide valid production quantity.";
                                //    return response;
                                //}

                                // --- Validate stockQty ---
                                if (string.IsNullOrWhiteSpace(stockValue) || !int.TryParse(stockValue, out stockQty))
                                {
                                    transaction.Rollback();
                                    response.Success = false;
                                    response.Data = false;
                                    response.Message =
                                        $"The stock quantity for 'Casin' '{Casin}' is invalid or missing. Please provide valid stock quantity.";
                                    return response;
                                }
                                #endregion convert to int

                                #endregion

                                int newStock = (productionQty + stockQty) - orderQty;

                                if (newStock<0)
                                {
                                    newStock = 0;
                                }

                                #region Fetch ItemCatalogue and InitialStock
                                InitialStock initialStock = await context.InitialStocks
                                                    .Include(s => s.ItemCatalogue)
                                                    .FirstOrDefaultAsync(s => s.ItemCatalogue.Casin == Casin);

                                if (initialStock == null)
                                {
                                    transaction.Rollback();
                                    response.Success = false;
                                    response.Data = false;
                                    response.Message = $"Initial stock not found for 'Casin' '{Casin}'. " +
                                                        $"Please ensure the initial stock is set for this item.";
                                    return response;
                                }
                                #endregion Fetch ItemCatalogue and InitialStock

                                #region Update Stock Quantity
                                

                                // Update quantities
                                //productionQty
                                initialStock.OpeningStock = newStock;
                                initialStock.UpdatedAt = DateTime.Now;
                                initialStock.UpdatedById = _session.LoggedInUser.Id;

                                // Save changes
                                isSaved = await context.SaveChangesAsync();

                                if (isSaved <= 0)
                                {
                                    transaction.Rollback();
                                    response.Success = false;
                                    response.Data = false;
                                    response.Message = $"Failed to update the stock record for 'Casin' '{Casin}'. " +
                                                        $"Please try again or contact support if the issue persists.";
                                    return response;
                                }
                                #endregion Update Stock Quantity
                            }

                            // Commit transaction
                            transaction.Commit();

                            response.Success = true;
                            response.Data = true;
                            response.Message = "Stock quantities updated successfully.";
                            return response;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            response.Success = false;
                            response.Data = false;
                            response.Message = $"An error occurred while updating the stock quantity for 'Casin'. " +
                                                $"Error: {ex.Message}. Please try again or contact support if the issue persists.";
                            return response;
                        }
                    }
                }
                #endregion Database Operations

            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Data = false;
                response.Message = $"An unexpected error occurred while processing your request. " +
                                    $"Error: {ex.Message}. Please try again or contact support if the issue persists.";
                return response;
            }
        }

        public int GetInitialStockValue(int itemCatalogueId)
        {
            try
            {
                using (var context = new WIPATContext())
                {
                    InitialStock stock = context.InitialStocks
                                                 .Where(s => s.ItemCatalogueId == itemCatalogueId)
                                                 .FirstOrDefault();

                    if (stock == null)
                    {
                        throw new InvalidOperationException($"Stock not found for ItemCatalogueId: {itemCatalogueId}");
                    }

                    var currentStock = stock.OpeningStock + stock.ProductionQty - stock.OrderQty;

                    return currentStock;
                }
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                throw;
            }
        }

    }

}
