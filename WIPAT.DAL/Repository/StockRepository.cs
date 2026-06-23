using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using WIPAT.DAL.Interfaces;
using WIPAT.Entities;
using WIPAT.Entities.Dto;
using WIPAT.Entities.Enum;

namespace WIPAT.DAL
{
    public class StockRepository : IStockRepository
    {
        private readonly WIPATContext _context;
        private readonly WipSession _session;

        public StockRepository(WIPATContext context, WipSession session)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        #region Stock

        public bool SaveInitialStocksToDatabase(List<InitialStock> stocks)
        {
            // Added explicit transaction block to guarantee atomicity
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    _context.InitialStocks.AddRange(stocks);
                    _context.SaveChanges();

                    transaction.Commit();
                    return true;
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    return false;
                }
            }
        }

        #endregion Stock

        // Update Quantity Column
        public async Task<Response<bool>> UpdateStockQuantitiesAsync(List<InitialStock> stocks)
        {
            var response = new Response<bool>();

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    foreach (var stock in stocks)
                    {
                        var existingStock = await _context.InitialStocks
                            .FirstOrDefaultAsync(s => s.ItemCatalogueId == stock.ItemCatalogueId);

                        if (existingStock != null)
                        {
                            existingStock.OpeningStock = stock.OpeningStock;
                            existingStock.UpdatedAt = DateTime.Now;
                            existingStock.UpdatedById = _session.LoggedInUser.Id;

                            int isSaved = await _context.SaveChangesAsync();

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
                    response.Message = $"An error occurred while updating stock quantities: {ex.Message}.";
                    return response;
                }
            }
        }

        // Optimized
        public async Task<Response<bool>> UpdateStockQtyInStockTable(DataTable stockDataTable, string wipColName, string month, string year)
        {
            var response = new Response<bool>();

            if (!DateTime.TryParse($"01 {month} {year}", out DateTime currentDate))
            {
                return new Response<bool> { Success = false, Message = "Invalid Month/Year format." };
            }

            DateTime previousDate = currentDate.AddMonths(-1);
            string previousMonth = previousDate.ToString("MMMM");

            string casinCol = "CASIN";
            string stockCol = "Initial_Stock";
            string orderCol = "Actual_Order";
            string productionCol = $"Wip ({previousMonth})";
            string commitmentPeriodCol = $"CommitmentPeriod ({month})";

            try
            {
                #region 1. Input Validation
                if (stockDataTable == null || stockDataTable.Rows.Count == 0)
                {
                    return new Response<bool> { Success = false, Message = "The input data table is null or empty." };
                }

                var requiredColumns = new[] { casinCol, stockCol, orderCol, productionCol, commitmentPeriodCol };
                foreach (var col in requiredColumns)
                {
                    if (!stockDataTable.Columns.Contains(col))
                    {
                        return new Response<bool> { Success = false, Message = $"Missing required column '{col}'." };
                    }
                }
                #endregion

                #region 2. In-Memory Calculations
                var targetStocks = new Dictionary<string, int>();

                var groupedByCasin = stockDataTable.AsEnumerable()
                    .Where(r => r[casinCol] != DBNull.Value && !string.IsNullOrWhiteSpace(r[casinCol].ToString()))
                    .GroupBy(r => r[casinCol].ToString());

                foreach (var group in groupedByCasin)
                {
                    string casin = group.Key;

                    var row = group.FirstOrDefault(r => r[commitmentPeriodCol]?.ToString() == "0")
                           ?? group.FirstOrDefault(r => r[commitmentPeriodCol]?.ToString() == "1");

                    if (row == null) return new Response<bool> { Success = false, Message = $"No matching rows found for Casin '{casin}'." };

                    var orderValue = row[orderCol]?.ToString();
                    var productionValue = row[productionCol]?.ToString();
                    var stockValue = row[stockCol]?.ToString();

                    if (!int.TryParse(orderValue, out int orderQty) || !int.TryParse(stockValue, out int stockQty))
                    {
                        return new Response<bool> { Success = false, Message = $"Invalid numeric data for Casin '{casin}'." };
                    }

                    if (!int.TryParse(productionValue, out int productionQty)) productionQty = 0;

                    int newStock = (productionQty + stockQty) - orderQty;
                    targetStocks[casin] = Math.Max(0, newStock);
                }
                #endregion

                #region 3. Bulk Database Operations
                try
                {
                    _context.Configuration.AutoDetectChangesEnabled = false;

                    var casinsToFetch = targetStocks.Keys.ToList();
                    var stocksToUpdate = new List<InitialStock>();

                    int chunkSize = 1000;
                    for (int i = 0; i < casinsToFetch.Count; i += chunkSize)
                    {
                        var chunk = casinsToFetch.Skip(i).Take(chunkSize).ToList();
                        var stocks = await _context.InitialStocks
                            .Include(s => s.ItemCatalogue)
                            .Where(s => chunk.Contains(s.ItemCatalogue.Casin))
                            .ToListAsync();

                        stocksToUpdate.AddRange(stocks);
                    }

                    var foundCasins = stocksToUpdate.Select(s => s.ItemCatalogue.Casin).ToHashSet();
                    var missingCasins = casinsToFetch.Where(c => !foundCasins.Contains(c)).ToList();

                    if (missingCasins.Any())
                    {
                        string missingStr = string.Join(", ", missingCasins.Take(5)) + (missingCasins.Count > 5 ? "..." : "");
                        // Return early without saving; no transaction rollback needed because nothing was saved yet
                        return new Response<bool> { Success = false, Message = $"Initial stock not found for: {missingStr}" };
                    }

                    foreach (var stock in stocksToUpdate)
                    {
                        stock.OpeningStock = targetStocks[stock.ItemCatalogue.Casin];
                        stock.UpdatedAt = DateTime.Now;
                        stock.UpdatedById = _session.LoggedInUser.Id;
                        _context.Entry(stock).State = EntityState.Modified;
                    }

                    // EF natively wraps this single call in a transaction automatically
                    int isSaved = await _context.SaveChangesAsync();

                    if (isSaved <= 0 && targetStocks.Count > 0)
                    {
                        return new Response<bool> { Success = false, Message = "Failed to apply bulk update to the database." };
                    }

                    return new Response<bool> { Success = true, Data = true, Message = "Stock quantities updated successfully." };
                }
                finally
                {
                    _context.Configuration.AutoDetectChangesEnabled = true;
                }
                #endregion
            }
            catch (Exception ex)
            {
                return new Response<bool> { Success = false, Message = $"Unexpected error: {ex.Message}." };
            }
        }

        public int GetInitialStockValue(int itemCatalogueId)
        {
            // Note: Explicit transactions are omitted here because this is a single, pure READ operation.
            // Entity Framework wraps individual reads efficiently, and explicit transactions aren't required 
            // unless establishing a specific isolation level for concurrency scenarios.

            // 1. Guard clauses to instantly detect DI or context setup issues
            if (_context == null)
            {
                throw new InvalidOperationException("The database context is not initialized (null). Check your Dependency Injection configuration.");
            }

            if (_context.InitialStocks == null)
            {
                throw new InvalidOperationException("The InitialStocks table/DbSet is not initialized. Verify your DbContext mappings.");
            }

            try
            {
                // 2. Perform the database query
                var stock = _context.InitialStocks.FirstOrDefault(s => s.ItemCatalogueId == itemCatalogueId);

                if (stock == null)
                {
                    throw new InvalidOperationException($"Stock data not found for ItemCatalogueId: {itemCatalogueId}");
                }

                return stock.OpeningStock + stock.ProductionQty - stock.OrderQty;
            }
            catch (ArgumentNullException ex)
            {
                throw new InvalidOperationException("A required argument was unexpectedly null during the database lookup.", ex);
            }
            catch (System.Data.Entity.Core.EntityException ex)
            {
                throw new InvalidOperationException("A connection or data access error occurred while communicating with the database. Check your database server and connection string.", ex);
            }
            catch (NullReferenceException ex)
            {
                // Catches underlying provider failures such as an unresolvable or missing connection string
                throw new InvalidOperationException("A null reference exception occurred during database execution. Verify that your connection string is correctly defined in the configuration file.", ex);
            }
            catch (Exception ex)
            {
                // Wrap and preserve original stack trace/exception details
                throw new Exception($"An unexpected error occurred while fetching the initial stock value for ItemCatalogueId {itemCatalogueId}: {ex.Message}", ex);
            }
        }

        #region bulk insert active item catalogue along with stock

        public Response<bool> BulkInsertCatalogueImport(DataTable dtItemCatalogues, DataTable dtInitialStock)
        {
            var response = new Response<bool>();

            // Database connection string
            string connectionString = ConfigurationManager.ConnectionStrings["dbContext"].ConnectionString;

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Step 3: bulk insert to ItemCatalogues
                        var resSaveCatalogue = BulkInsertToItemsCatalogue(dtItemCatalogues, conn, transaction);
                        if (!resSaveCatalogue.Success)
                        {
                            transaction.Rollback();
                            response.Success = false;
                            response.Message = $"Failed to bulk insert ItemsCatalogue: {resSaveCatalogue.Message}";
                            response.Data = false;
                            return response;
                        }

                        // Step 4: Retrieve the generated ItemCatalogueId values and update InitialStock
                        var resUpdateStockTable = MapItemCatalogueIds(dtItemCatalogues, dtInitialStock, conn, transaction);
                        if (!resUpdateStockTable.Success)
                        {
                            transaction.Rollback();
                            response.Success = false;
                            response.Message = "Failed to Map ItemCatalogue Ids.";
                            response.Data = false;
                            return response;
                        }

                        // Step 5: bulk insert to InitialStock
                        var resSaveStock = BulkInsertInitialStock(resUpdateStockTable.Data, conn, transaction);
                        if (!resSaveStock.Success)
                        {
                            transaction.Rollback();
                            response.Success = false;
                            response.Message = $"Failed to bulk insert Stock: {resSaveStock.Message}";
                            response.Data = false;
                            return response;
                        }

                        // Commit if everything succeeds
                        transaction.Commit();
                        response.Success = true;
                        response.Message = "Bulk insert completed successfully.";
                        response.Data = true;
                        return response;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        response.Success = false;
                        response.Message = $"Bulk insert Failed: {ex.Message}";
                        response.Data = false;
                        return response;
                    }
                }
            }
        }

        //// Step 1: Bulk Insert → Items Catalogue
        public Response<bool> BulkInsertToItemsCatalogue(DataTable dt, SqlConnection conn, SqlTransaction transaction)
        {
            var response = new Response<bool>();

            try
            {
                // 1. Fetch all existing Casins from the database to check for duplicates
                var existingCasins = new HashSet<string>();
                string query = "SELECT Casin FROM dbo.ItemCatalogues";
                using (var cmd = new SqlCommand(query, conn, transaction))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            existingCasins.Add(reader["Casin"].ToString());
                        }
                    }
                }

                // 2. Identify duplicates
                var duplicateCasins = new List<string>();
                var seenInFile = new HashSet<string>(); // To check if the file itself has duplicates

                foreach (DataRow row in dt.Rows)
                {
                    string currentCasin = row[AllColumnNames.CAsin].ToString();

                    // Check if it exists in DB OR if it's a duplicate within the file itself
                    if (existingCasins.Contains(currentCasin) || seenInFile.Contains(currentCasin))
                    {
                        duplicateCasins.Add(currentCasin);
                    }
                    else
                    {
                        seenInFile.Add(currentCasin);
                    }
                }

                // If duplicates found, return them specifically
                if (duplicateCasins.Any())
                {
                    response.Success = false;
                    response.Message = $"Duplicate Casins found: {string.Join(", ", duplicateCasins.Distinct())}";
                    return response;
                }

                // --- Prepare DataTable for Bulk Insert ---
                // Ensure IsActive column exists
                if (!dt.Columns.Contains("IsActive"))
                    dt.Columns.Add("IsActive", typeof(bool));

                // Ensure ItemStatus column exists
                if (!dt.Columns.Contains("ItemStatus"))
                    dt.Columns.Add("ItemStatus", typeof(string));

                // Set default values for new rows
                foreach (DataRow row in dt.Rows)
                {
                    row["IsActive"] = true; // default
                    row["ItemStatus"] = ItemStatus.Valid.ToString(); // default
                }

                // --- Bulk Insert ---
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.KeepIdentity, transaction))
                {
                    bulkCopy.DestinationTableName = "dbo.ItemCatalogues";

                    bulkCopy.ColumnMappings.Add(AllColumnNames.CAsin, "Casin");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.Model, "Model");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.Description, "Description");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.ColorName, "ColorName");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.Size, "Size");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.PCPK, "PCPK");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.CasePackQty, "CasePackQty");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.CreatedAt, "CreatedAt");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.CreatedById, "CreatedById");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.Notes, "Notes");

                    // Add the new columns
                    bulkCopy.ColumnMappings.Add("IsActive", "IsActive");
                    bulkCopy.ColumnMappings.Add("ItemStatus", "ItemStatus");

                    bulkCopy.WriteToServer(dt);
                }

                response.Success = true;
                response.Message = "Bulk insert completed successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error: {ex.Message}";
            }

            return response;
        }

        // Step 2: Map ItemCatalogueIds to InitialStock (after inserting ItemCatalogues)
        public Response<DataTable> MapItemCatalogueIds(DataTable dtItemCatalogues, DataTable dtInitialStock, SqlConnection conn, SqlTransaction transaction)
        {
            var response = new Response<DataTable>();

            try
            {
                // Create a dictionary to map "Casin" from dtItemCatalogues to ItemCatalogueId
                var itemCatalogueIdMap = new Dictionary<string, int>(); // Assuming "Casin" is unique in ItemCatalogues

                // Query to fetch ItemCatalogueId values from the database
                string query = "SELECT Casin, Id FROM dbo.ItemCatalogues";
                using (var cmd = new SqlCommand(query, conn, transaction))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string casin = reader["Casin"].ToString();
                            int itemCatalogueId = Convert.ToInt32(reader["Id"]);

                            // Add to the map
                            itemCatalogueIdMap[casin] = itemCatalogueId;
                        }
                    }
                }

                // Track missing mappings
                var missingCasins = new List<string>();

                // Now, update the dtInitialStock DataTable with the correct ItemCatalogueId
                foreach (DataRow row in dtInitialStock.Rows)
                {
                    string casin = row[AllColumnNames.CAsin].ToString();
                    if (itemCatalogueIdMap.ContainsKey(casin))
                    {
                        row[AllColumnNames.ItemCatalogueId] = itemCatalogueIdMap[casin]; // Set the correct ItemCatalogueId
                    }
                    else
                    {
                        missingCasins.Add(casin);
                    }
                }

                if (missingCasins.Any())
                {
                    response.Success = false;
                    response.Message = $"Failed to map ItemCatalogueId for the following Casin(s): {string.Join(", ", missingCasins)}";
                    response.Data = dtInitialStock;
                    return response;
                }

                response.Success = true;
                response.Message = "ItemCatalogueId mapping completed successfully.";
                response.Data = dtInitialStock;
                return response;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error occurred while mapping ItemCatalogueIds: {ex.Message}";
                response.Data = null;
                return response;
            }
        }

        // Step 3: Bulk Insert → InitialStock Table
        public Response<List<bool>> BulkInsertInitialStock(DataTable dt, SqlConnection conn, SqlTransaction transaction)
        {
            // Initialized the list immediately to avoid NullReferenceException in the catch block 
            var response = new Response<List<bool>> { Data = new List<bool>() };

            try
            {
                // Ensure required column 'OrderQty' exists in DataTable
                dt.Columns.Add("OrderQty", typeof(int));
                dt.Columns.Add("ProductionQty", typeof(int));

                // Deliberately set OrderQty = 0 for all rows
                foreach (DataRow row in dt.Rows)
                {
                    row["OrderQty"] = 0;
                    row["ProductionQty"] = 0;
                }

                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.KeepIdentity, transaction))
                {
                    bulkCopy.DestinationTableName = "dbo.InitialStocks";

                    // Column mappings (DataTable → DB)
                    bulkCopy.ColumnMappings.Add(AllColumnNames.ItemCatalogueId, "ItemCatalogueId");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.OpeningStock, "OpeningStock");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.CreatedAt, "CreatedAt");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.CreatedById, "CreatedById");

                    // Add missing column mapping for OrderQty
                    bulkCopy.ColumnMappings.Add("OrderQty", "OrderQty");
                    bulkCopy.ColumnMappings.Add("ProductionQty", "ProductionQty");


                    // Perform bulk insert
                    bulkCopy.WriteToServer(dt);

                    // Success
                    response.Success = true;
                    response.Message = "Bulk insert into InitialStocks completed successfully.";
                }
            }
            catch (SqlException sqlEx)
            {
                // Handle unique constraint violation
                if (sqlEx.Number == 2627 || sqlEx.Number == 2601)
                {
                    response.Success = false;
                    response.Message = "Some items already exist in the catalogue. Please check your file for duplicates.";
                }
                else
                {
                    response.Success = false;
                    response.Message = "A database error occurred while inserting items catalogue.";
                }
            }
            catch (Exception ex)
            {
                // Failure
                response.Data.Add(false);
                response.Success = false;
                response.Message = $"Error in BulkInsertInitialStock: {ex.Message}";
            }

            return response;
        }

        #endregion bulk insert active item catalogue along with stock
    }
}