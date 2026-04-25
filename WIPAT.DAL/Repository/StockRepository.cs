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
            try
            {
                _context.InitialStocks.AddRange(stocks);
                _context.SaveChanges();
                return true;
            }
            catch (Exception)
            {
                return false;
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

        public async Task<Response<bool>> UpdateStockQtyInStockTable(DataTable stockDataTable, string wipColName, string month, string year)
        {
            var response = new Response<bool>();

            // Date parsing logic
            if (!DateTime.TryParse($"01 {month} {year}", out DateTime currentDate))
            {
                return new Response<bool> { Success = false, Message = "Invalid Month/Year format." };
            }

            DateTime previousDate = currentDate.AddMonths(-1);
            string previousMonth = previousDate.ToString("MMMM");

            // Required columns
            string casinCol = "C-ASIN";
            string stockCol = "Initial_Stock";
            string orderCol = "Actual_Order";
            string productionCol = $"Wip ({previousMonth})";
            string commitmentPeriodCol = $"CommitmentPeriod ({month})";
            string commitmentPeriod = "0";

            try
            {
                #region Input Validation
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
                #endregion Input Validation

                #region Database Operations
                using (var transaction = _context.Database.BeginTransaction())
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
                                return new Response<bool> { Success = false, Message = "A null or empty 'Casin' value was found." };
                            }

                            #region Filter Rows
                            string filterExpression = $"[{casinCol}] = '{Casin.Replace("'", "''")}' and [{commitmentPeriodCol}] = {commitmentPeriod}";
                            DataRow[] filteredRows = stockDataTable.Select(filterExpression);

                            if (filteredRows == null || filteredRows.Length == 0)
                            {
                                // Fallback check for period 1
                                string commitmentPeriod_ = "1";
                                filterExpression = $"[{casinCol}] = '{Casin.Replace("'", "''")}' and [{commitmentPeriodCol}] = {commitmentPeriod_}";
                                filteredRows = stockDataTable.Select(filterExpression);

                                if (filteredRows == null || filteredRows.Length == 0)
                                {
                                    transaction.Rollback();
                                    return new Response<bool>
                                    {
                                        Success = false,
                                        Message = $"No matching rows found for Casin '{Casin}'."
                                    };
                                }
                            }
                            #endregion

                            #region Parse & Calculate
                            var orderValue = filteredRows[0][orderCol]?.ToString();
                            var productionValue = filteredRows[0][productionCol]?.ToString();
                            var stockValue = filteredRows[0][stockCol]?.ToString();

                            if (!int.TryParse(orderValue, out int orderQty) || !int.TryParse(stockValue, out int stockQty))
                            {
                                transaction.Rollback();
                                return new Response<bool>
                                {
                                    Success = false,
                                    Message = $"Invalid numeric data for Casin '{Casin}'."
                                };
                            }

                            if (!int.TryParse(productionValue, out int productionQty))
                            {
                                productionQty = 0; // Default to 0 if missing/invalid
                            }

                            int newStock = (productionQty + stockQty) - orderQty;
                            if (newStock < 0) newStock = 0;
                            #endregion

                            #region Fetch & Update
                            InitialStock initialStock = await _context.InitialStocks
                                                .Include(s => s.ItemCatalogue)
                                                .FirstOrDefaultAsync(s => s.ItemCatalogue.Casin == Casin);

                            if (initialStock == null)
                            {
                                transaction.Rollback();
                                return new Response<bool> { Success = false, Message = $"Initial stock not found for '{Casin}'." };
                            }

                            initialStock.OpeningStock = newStock;
                            initialStock.UpdatedAt = DateTime.Now;
                            initialStock.UpdatedById = _session.LoggedInUser.Id;

                            int isSaved = await _context.SaveChangesAsync();

                            if (isSaved <= 0)
                            {
                                transaction.Rollback();
                                return new Response<bool> { Success = false, Message = $"Failed to update DB for '{Casin}'." };
                            }
                            #endregion
                        }

                        transaction.Commit();

                        return new Response<bool>
                        {
                            Success = true,
                            Data = true,
                            Message = "Stock quantities updated successfully."
                        };
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return new Response<bool>
                        {
                            Success = false,
                            Message = $"An error occurred: {ex.Message}."
                        };
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                return new Response<bool>
                {
                    Success = false,
                    Message = $"Unexpected error: {ex.Message}."
                };
            }
        }

        public int GetInitialStockValue(int itemCatalogueId)
        {
            var stock = _context.InitialStocks.FirstOrDefault(s => s.ItemCatalogueId == itemCatalogueId);

            if (stock == null)
            {
                throw new InvalidOperationException($"Stock not found for ItemCatalogueId: {itemCatalogueId}");
            }

            return stock.OpeningStock + stock.ProductionQty - stock.OrderQty;
        }


        #region bulk insert item catalogue along with stock
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
        public Response<bool> xBulkInsertToItemsCatalogue(DataTable dt, SqlConnection conn, SqlTransaction transaction)
        {
            var response = new Response<bool>();


            try
            {
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.KeepIdentity, transaction))
                {
                    bulkCopy.DestinationTableName = "dbo.ItemCatalogues";

                    // Column mappings (Excel → DB)
                    bulkCopy.ColumnMappings.Add(AllColumnNames.CAsin, "Casin");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.Model, "Model");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.Description, "Description");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.ColorName, "ColorName");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.Size, "Size");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.PCPK, "PCPK");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.CasePackQty, "CasePackQty");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.CreatedAt, "CreatedAt");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.CreatedById, "CreatedById");

                    // Perform the bulk insert within the transaction
                    bulkCopy.WriteToServer(dt);

                    // Add success result
                    response.Success = true;
                    response.Message = "Bulk insert completed successfully.";
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
                // Add failure result
                response.Success = false;
                response.Message = $"Error in BulkInsertToItemsCatalogue: {ex.Message}";
            }

            return response;
        }
        public Response<bool> BulkInsertToItemsCatalogue(DataTable dt, SqlConnection conn, SqlTransaction transaction)
        {
            var response = new Response<bool>();

            try
            {
                // 1. Ensure IsActive exists in the DataTable
                if (!dt.Columns.Contains("IsActive"))
                {
                    dt.Columns.Add("IsActive", typeof(bool));
                }

                // 2. Populate the data
                foreach (DataRow row in dt.Rows)
                {
                    row["IsActive"] = true;
                }

                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.KeepIdentity, transaction))
                {
                    bulkCopy.DestinationTableName = "dbo.ItemCatalogues";

                    // Clear any existing mappings to avoid duplicates if the method is retried
                    bulkCopy.ColumnMappings.Clear();

                    // 3. Define Mappings (Source Column Name, Destination Column Name)
                    // IMPORTANT: Ensure the second parameter matches your SQL Table column names EXACTLY
                    bulkCopy.ColumnMappings.Add(AllColumnNames.CAsin, "Casin");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.Model, "Model");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.Description, "Description");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.ColorName, "ColorName");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.Size, "Size");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.PCPK, "PCPK");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.CasePackQty, "CasePackQty");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.CreatedAt, "CreatedAt");
                    bulkCopy.ColumnMappings.Add(AllColumnNames.CreatedById, "CreatedById");

                    // Matches 'public bool isActive' in your entity (assuming SQL column is isActive)
                    bulkCopy.ColumnMappings.Add("IsActive", "isActive");

                    // 4. Validate mappings before execution to catch typos early
                    foreach (SqlBulkCopyColumnMapping map in bulkCopy.ColumnMappings)
                    {
                        if (!dt.Columns.Contains(map.SourceColumn))
                        {
                            throw new Exception($"The source column '{map.SourceColumn}' was not found in the DataTable. Check your Excel/CSV headers.");
                        }
                    }

                    bulkCopy.WriteToServer(dt);

                    response.Success = true;
                    response.Message = "Bulk insert completed successfully.";
                }
            }
            catch (SqlException sqlEx)
            {
                if (sqlEx.Number == 2627 || sqlEx.Number == 2601)
                {
                    response.Success = false;
                    response.Message = "Some items already exist. Please check for duplicate Casins.";
                }
                else
                {
                    response.Success = false;
                    response.Message = $"Database Error: {sqlEx.Message}";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Mapping Error: {ex.Message}";
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
            var response = new Response<List<bool>>();


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
     
        #endregion bulk insert item catalogue along with stock
    }
}