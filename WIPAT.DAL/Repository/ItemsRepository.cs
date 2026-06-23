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
using WIPAT.Entities.ExcelTemplateDefinitions;

namespace WIPAT.DAL
{
    public class ItemsRepository : IItemsRepository
    {
        private readonly WIPATContext _context;

        public ItemsRepository(WIPATContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        #region Retrieval Operations

        public async Task<Response<List<ItemCatalogueDto>>> GetItemCataloguesWithStock()
        {
            try
            {
                var results = await (from c in _context.ItemCatalogues
                                     join s in _context.InitialStocks
                                     on c.Id equals s.ItemCatalogueId
                                     select new ItemCatalogueDto
                                     {
                                         Id = c.Id,
                                         Casin = c.Casin,
                                         Model = c.Model,
                                         Description = c.Description,
                                         ColorName = c.ColorName,
                                         Size = c.Size,
                                         PCPK = c.PCPK,
                                         MOQ = c.MOQ,
                                         CasePackQty = c.CasePackQty,
                                         ItemStatus = c.ItemStatus,
                                         Notes = c.Notes,
                                         OpeningStock = s.OpeningStock
                                     }).ToListAsync();

                if (results != null && results.Count > 0)
                {
                    return new Response<List<ItemCatalogueDto>>
                    {
                        Success = true,
                        Data = results,
                        Message = "ItemCatalogues and associated stocks retrieved successfully"
                    };
                }

                return new Response<List<ItemCatalogueDto>> { Success = false, Message = "No ItemCatalogues found" };
            }
            catch (Exception ex)
            {
                return new Response<List<ItemCatalogueDto>> { Success = false, Message = $"An error occurred: {ex.Message}" };
            }
        }

        public async Task<Response<List<ItemCatalogue>>> GetActiveItemCatalogues(bool includeInactive = false)
        {
            try
            {
                var query = _context.ItemCatalogues.AsQueryable();

                if (!includeInactive)
                {
                    query = query.Where(i => i.ItemStatus == (int)CatalogueItemStatus.Active);
                }

                var items = await query.ToListAsync();

                if (items != null && items.Count > 0)
                {
                    return new Response<List<ItemCatalogue>> { Success = true, Data = items };
                }

                return new Response<List<ItemCatalogue>> { Success = false, Message = "No ItemCatalogues found" };
            }
            catch (Exception ex)
            {
                return new Response<List<ItemCatalogue>> { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public int GetItemIdByAsin(string asin)
        {
            try
            {
                var item = _context.ItemCatalogues
                                   .AsNoTracking()
                                   .Where(i => i.ItemStatus == (int)CatalogueItemStatus.Active)
                                   .FirstOrDefault(i => i.Casin == asin);

                return item?.Id ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        public ItemCatalogue GetItemByCAsin(string casin)
        {
            try
            {
                var item = _context.ItemCatalogues
                                   .AsNoTracking()
                                   .FirstOrDefault(i => i.Casin == casin && i.ItemStatus == (int)CatalogueItemStatus.Active);

                return item;
            }
            catch
            {
                return null;
            }
        }

        public async Task<Dictionary<string, int>> GetCatalogueIdsByCasinsAsync(List<string> casins)
        {
            if (casins == null || !casins.Any()) return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var items = await _context.ItemCatalogues
                    .AsNoTracking()
                    .Where(i => casins.Contains(i.Casin))
                    .Select(i => new { i.Casin, i.Id })
                    .ToListAsync();

                var dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                foreach (var item in items)
                {
                    if (!string.IsNullOrWhiteSpace(item.Casin) && !dictionary.ContainsKey(item.Casin))
                        dictionary.Add(item.Casin, item.Id);
                }

                return dictionary;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error: {ex.Message}", ex);
            }
        }

        #endregion

        #region Verification Operations

        public bool IsCAsinExistInCatalogue(string casin)
        {
            try
            {
                return _context.ItemCatalogues.Any(item => item.Casin == casin && item.ItemStatus == (int)CatalogueItemStatus.Active);
            }
            catch
            {
                return false;
            }
        }

        public async Task<int?> CheckCAsinStatus(string casin)
        {
            try
            {
                return await _context.ItemCatalogues
                                           .Where(item => item.Casin == casin)
                                           .Select(item => (int?)item.ItemStatus)
                                           .FirstOrDefaultAsync();
            }
            catch
            {
                return null;
            }
        }

        public async Task<Dictionary<string, int>> GetCasinStatusesBatchAsync(IEnumerable<string> casins)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            var casinList = casins.ToList();
            int batchSize = 1000;
            int total = casinList.Count;

            try
            {
                for (int i = 0; i < total; i += batchSize)
                {
                    var chunk = casinList.Skip(i).Take(batchSize).ToList();

                    var chunkResult = await _context.ItemCatalogues
                        .Where(item => chunk.Contains(item.Casin))
                        .Select(item => new { item.Casin, item.ItemStatus })
                        .ToDictionaryAsync(x => x.Casin, x => (int)x.ItemStatus, StringComparer.OrdinalIgnoreCase);

                    foreach (var kvp in chunkResult)
                    {
                        result[kvp.Key] = kvp.Value;
                    }
                }

                return result;
            }
            catch
            {
                throw;
            }
        }

        public async Task<Response<bool>> IsCasinExistInCatalogueAndInitialStock(string casin)
        {
            try
            {
                bool existsInCatalogue = await _context.ItemCatalogues.AnyAsync(item => item.Casin == casin && item.ItemStatus == (int)CatalogueItemStatus.Active);
                bool existsInInitialStock = await _context.InitialStocks.AnyAsync(stock => stock.ItemCatalogue.Casin == casin && stock.ItemCatalogue.ItemStatus == (int)CatalogueItemStatus.Active);

                if (existsInCatalogue || existsInInitialStock)
                {
                    string msg = existsInCatalogue && existsInInitialStock ? $"Casin '{casin}' exists in both." :
                                 existsInCatalogue ? $"Exists in Catalogue only." : $"Exists in Stock only.";
                    return new Response<bool> { Success = true, Data = true, Message = msg };
                }

                return new Response<bool> { Success = false, Data = false, Message = $"Casin '{casin}' not found." };
            }
            catch (Exception ex)
            {
                return new Response<bool> { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<Response<bool>> IsItemExistInCatalogue(string asin)
        {
            try
            {
                bool existsInCatalogue = await _context.ItemCatalogues.AnyAsync(item => item.Casin == asin && item.ItemStatus == (int)CatalogueItemStatus.Active);

                if (existsInCatalogue) return new Response<bool> { Success = true, Data = true };

                return new Response<bool> { Success = false, Data = false, Message = $"CASIN '{asin}' not found." };
            }
            catch (Exception ex)
            {
                return new Response<bool> { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        #endregion

        #region Write Operations

        public bool AddItemToCatalogue(string casin, string itemName = null, string description = null)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    if (_context.ItemCatalogues.Any(item => item.Casin == casin && item.ItemStatus == (int)CatalogueItemStatus.Active))
                    {
                        transaction.Commit();
                        return true;
                    }

                    var newItem = new ItemCatalogue
                    {
                        Casin = casin,
                        Description = description ?? string.Empty,
                        ItemStatus = (int)CatalogueItemStatus.Active,
                        CreatedAt = DateTime.Now,
                    };

                    _context.ItemCatalogues.Add(newItem);
                    _context.SaveChanges();

                    transaction.Commit();
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    return false;
                }
            }
        }

        public Response<bool> BulkUpdateCatalogueAndStatusImport(DataTable catalogueTable, DataTable stockTable)
        {
            if (catalogueTable == null || catalogueTable.Rows.Count == 0)
                return new Response<bool> { Success = false, Message = "Import failed: empty table." };

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    if (!catalogueTable.Columns.Contains("CASIN"))
                    {
                        transaction.Commit();
                        return new Response<bool> { Success = false, Message = "Missing C-ASIN column." };
                    }

                    var incomingCasins = catalogueTable.AsEnumerable()
                        .Select(r => r["CASIN"]?.ToString()?.Trim())
                        .Where(c => !string.IsNullOrEmpty(c))
                        .Distinct().ToList();

                    if (!incomingCasins.Any())
                    {
                        transaction.Commit();
                        return new Response<bool> { Success = false, Message = "No valid CASINs." };
                    }

                    var existingItems = _context.ItemCatalogues.Where(i => incomingCasins.Contains(i.Casin)).ToDictionary(i => i.Casin, StringComparer.OrdinalIgnoreCase);
                    var missingCasins = incomingCasins.Where(c => !existingItems.ContainsKey(c)).ToList();

                    if (missingCasins.Any())
                    {
                        transaction.Commit();
                        return new Response<bool> { Success = false, Message = $"Not found in DB: {string.Join(", ", missingCasins)}" };
                    }

                    var existingItemIds = existingItems.Values.Select(i => i.Id).ToList();
                    var existingStocks = _context.InitialStocks.Where(s => existingItemIds.Contains(s.ItemCatalogueId)).ToDictionary(s => s.ItemCatalogueId);
                    var externalStockDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                    if (stockTable != null && stockTable.Columns.Contains("CASIN") && stockTable.Columns.Contains("OpeningStock"))
                    {
                        foreach (DataRow row in stockTable.Rows)
                        {
                            string sCasin = row["CASIN"]?.ToString()?.Trim();
                            if (!string.IsNullOrEmpty(sCasin) && int.TryParse(row["OpeningStock"]?.ToString(), out int stockVal))
                                externalStockDict[sCasin] = stockVal;
                        }
                    }

                    bool hasItemStatus = catalogueTable.Columns.Contains("ItemStatus");

                    foreach (DataRow row in catalogueTable.Rows)
                    {
                        string casin = row["CASIN"]?.ToString()?.Trim();
                        var item = existingItems[casin];

                        if (catalogueTable.Columns.Contains("Model") && !row.IsNull("Model")) item.Model = row["Model"].ToString();
                        if (catalogueTable.Columns.Contains("Description") && !row.IsNull("Description")) item.Description = row["Description"].ToString();
                        if (catalogueTable.Columns.Contains("Color Name") && !row.IsNull("Color Name")) item.ColorName = row["Color Name"].ToString();
                        if (catalogueTable.Columns.Contains("Size") && !row.IsNull("Size")) item.Size = row["Size"].ToString();
                        if (catalogueTable.Columns.Contains("PC/PK") && !row.IsNull("PC/PK")) item.PCPK = row["PC/PK"].ToString();
                        if (catalogueTable.Columns.Contains("Case Pack Qty") && !row.IsNull("Case Pack Qty") && int.TryParse(row["Case Pack Qty"].ToString(), out int cpQty))
                            item.CasePackQty = cpQty;
                        if (catalogueTable.Columns.Contains("Notes") && !row.IsNull("Notes")) item.Notes = row["Notes"].ToString();

                        if (hasItemStatus && !row.IsNull("ItemStatus"))
                        {
                            string statusStr = row["ItemStatus"].ToString().Trim();
                            if (int.TryParse(statusStr, out int statInt))
                            {
                                item.ItemStatus = statInt;
                            }
                            else if (Enum.TryParse<CatalogueItemStatus>(statusStr, true, out var statEnum))
                            {
                                item.ItemStatus = (int)statEnum;
                            }
                        }

                        if (existingStocks.TryGetValue(item.Id, out var stockRecord))
                        {
                            if (catalogueTable.Columns.Contains("OpeningStock") && !row.IsNull("OpeningStock") && int.TryParse(row["OpeningStock"].ToString(), out int catStockVal))
                                stockRecord.OpeningStock = catStockVal;
                            else if (externalStockDict.TryGetValue(casin, out int extStockVal))
                                stockRecord.OpeningStock = extStockVal;
                        }
                    }

                    _context.SaveChanges();
                    transaction.Commit();
                    return new Response<bool> { Success = true, Data = true, Message = $"Successfully updated {catalogueTable.Rows.Count} items." };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new Response<bool> { Success = false, Message = $"Error: {ex.Message}" };
                }
            }
        }

        #endregion

        #region Bulk Insert Invalid Catalogue Along With Stock

        public Response<bool> BulkInsertInvalidCatalogueImport(DataTable dtInvalidItems, DataTable dtInitialStock)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["dbContext"].ConnectionString;

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        var resSaveCatalogue = BulkInsertInvalidCatalogue(dtInvalidItems, conn, transaction);
                        if (!resSaveCatalogue.Success)
                        {
                            transaction.Rollback();
                            return new Response<bool> { Success = false, Message = resSaveCatalogue.Message };
                        }

                        var resUpdateStockTable = MapItemCatalogueIds(dtInvalidItems, dtInitialStock, conn, transaction);
                        if (!resUpdateStockTable.Success)
                        {
                            transaction.Rollback();
                            return new Response<bool> { Success = false, Message = "Failed to Map ItemCatalogue Ids." };
                        }

                        var resSaveStock = BulkInsertInvalidInitialStock(resUpdateStockTable.Data, conn, transaction);
                        if (!resSaveStock.Success)
                        {
                            transaction.Rollback();
                            return new Response<bool> { Success = false, Message = resSaveStock.Message };
                        }

                        transaction.Commit();
                        return new Response<bool> { Success = true, Data = true, Message = "Import completed successfully." };
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return new Response<bool> { Success = false, Message = $"Error: {ex.Message}" };
                    }
                }
            }
        }

        public Response<bool> BulkInsertInvalidCatalogue(DataTable dtInvalidItems, SqlConnection conn, SqlTransaction transaction)
        {
            try
            {
                var existingCasins = new HashSet<string>();
                string query = "SELECT Casin FROM dbo.ItemCatalogues";

                using (var cmd = new SqlCommand(query, conn, transaction))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read()) existingCasins.Add(reader["Casin"].ToString());
                }

                var seenInFile = new HashSet<string>();
                var duplicateCasins = new List<string>();

                foreach (DataRow row in dtInvalidItems.Rows)
                {
                    string casin = row["Casin"].ToString();
                    if (existingCasins.Contains(casin) || seenInFile.Contains(casin)) duplicateCasins.Add(casin);
                    else seenInFile.Add(casin);
                }

                if (duplicateCasins.Any()) return new Response<bool> { Success = false, Message = $"Duplicate Casins found: {string.Join(", ", duplicateCasins.Distinct())}" };

                if (!dtInvalidItems.Columns.Contains("ItemStatus"))
                    dtInvalidItems.Columns.Add("ItemStatus", typeof(int));

                foreach (DataRow row in dtInvalidItems.Rows)
                {
                    row["ItemStatus"] = (int)CatalogueItemStatus.Invalid; // Set 2 for invalid items
                    if (row.Table.Columns.Contains("CreatedAt")) row["CreatedAt"] = DateTime.Now;
                    if (row.Table.Columns.Contains("CasePackQty") && row["CasePackQty"] == DBNull.Value) row["CasePackQty"] = 0;
                    if (row.Table.Columns.Contains("PCPK")) row["PCPK"] = row["PCPK"] ?? DBNull.Value;
                }

                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.KeepIdentity, transaction))
                {
                    bulkCopy.DestinationTableName = "dbo.ItemCatalogues";
                    bulkCopy.ColumnMappings.Add(MasterColumnCatalogue.Casin.Name, "Casin");
                    bulkCopy.ColumnMappings.Add(MasterColumnCatalogue.Model.Name, "Model");
                    bulkCopy.ColumnMappings.Add(MasterColumnCatalogue.Description.Name, "Description");
                    bulkCopy.ColumnMappings.Add(MasterColumnCatalogue.ColorName.Name, "ColorName");
                    bulkCopy.ColumnMappings.Add(MasterColumnCatalogue.Size.Name, "Size");
                    bulkCopy.ColumnMappings.Add(MasterColumnCatalogue.PCPK.Name, "PCPK");
                    bulkCopy.ColumnMappings.Add(MasterColumnCatalogue.CasePackQty.Name, "CasePackQty");
                    bulkCopy.ColumnMappings.Add(MasterColumnCatalogue.CreatedAt.Name, "CreatedAt");
                    bulkCopy.ColumnMappings.Add(MasterColumnCatalogue.CreatedById.Name, "CreatedById");
                    bulkCopy.ColumnMappings.Add(MasterColumnCatalogue.Notes.Name, "Notes");
                    bulkCopy.ColumnMappings.Add(MasterColumnCatalogue.ItemStatusInt.Name, "ItemStatus");

                    bulkCopy.WriteToServer(dtInvalidItems);
                }

                return new Response<bool> { Success = true, Data = true, Message = "Inserted successfully." };
            }
            catch (Exception ex)
            {
                return new Response<bool> { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public Response<bool> BulkInsertInitialStock(DataTable dt, SqlConnection conn, SqlTransaction transaction)
        {
            try
            {
                if (!dt.Columns.Contains("OrderQty")) dt.Columns.Add("OrderQty", typeof(int));
                if (!dt.Columns.Contains("ProductionQty")) dt.Columns.Add("ProductionQty", typeof(int));

                foreach (DataRow row in dt.Rows)
                {
                    if (row["OrderQty"] == DBNull.Value) row["OrderQty"] = 0;
                    if (row["ProductionQty"] == DBNull.Value) row["ProductionQty"] = 0;
                }

                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.KeepIdentity, transaction))
                {
                    bulkCopy.DestinationTableName = "dbo.InitialStocks";
                    bulkCopy.ColumnMappings.Add("ItemCatalogueId", "ItemCatalogueId");
                    bulkCopy.ColumnMappings.Add("OpeningStock", "OpeningStock");
                    bulkCopy.ColumnMappings.Add("CreatedAt", "CreatedAt");
                    bulkCopy.ColumnMappings.Add("CreatedById", "CreatedById");
                    bulkCopy.ColumnMappings.Add("OrderQty", "OrderQty");
                    bulkCopy.ColumnMappings.Add("ProductionQty", "ProductionQty");

                    bulkCopy.WriteToServer(dt);
                }
                return new Response<bool> { Success = true, Data = true };
            }
            catch (Exception ex)
            {
                return new Response<bool> { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public Response<bool> BulkInsertInvalidInitialStock(DataTable dt, SqlConnection conn, SqlTransaction transaction)
        {
            return BulkInsertInitialStock(dt, conn, transaction);
        }

        public Response<DataTable> MapItemCatalogueIds(DataTable dtItemCatalogues, DataTable dtInitialStock, SqlConnection conn, SqlTransaction transaction)
        {
            try
            {
                var itemCatalogueIdMap = new Dictionary<string, int>();
                string query = "SELECT Casin, Id FROM dbo.ItemCatalogues";

                using (var cmd = new SqlCommand(query, conn, transaction))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read()) itemCatalogueIdMap[reader["Casin"].ToString()] = Convert.ToInt32(reader["Id"]);
                }

                var missingCasins = new List<string>();

                foreach (DataRow row in dtInitialStock.Rows)
                {
                    string casin = row["CASIN"].ToString();
                    if (itemCatalogueIdMap.ContainsKey(casin)) row["ItemCatalogueId"] = itemCatalogueIdMap[casin];
                    else missingCasins.Add(casin);
                }

                if (missingCasins.Any()) return new Response<DataTable> { Success = false, Message = $"Failed to map Casins: {string.Join(", ", missingCasins)}" };

                return new Response<DataTable> { Success = true, Data = dtInitialStock };
            }
            catch (Exception ex)
            {
                return new Response<DataTable> { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public Response<bool> BulkInsertToItemsCatalogue(DataTable dt, SqlConnection conn, SqlTransaction transaction)
        {
            try
            {
                var existingCasins = new HashSet<string>();
                string query = "SELECT Casin FROM dbo.ItemCatalogues";
                using (var cmd = new SqlCommand(query, conn, transaction))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read()) existingCasins.Add(reader["Casin"].ToString());
                }

                var duplicateCasins = new List<string>();
                var seenInFile = new HashSet<string>();

                foreach (DataRow row in dt.Rows)
                {
                    string currentCasin = row["C-ASIN"].ToString();
                    if (existingCasins.Contains(currentCasin) || seenInFile.Contains(currentCasin)) duplicateCasins.Add(currentCasin);
                    else seenInFile.Add(currentCasin);
                }

                if (duplicateCasins.Any()) return new Response<bool> { Success = false, Message = $"Duplicate Casins found: {string.Join(", ", duplicateCasins.Distinct())}" };

                if (!dt.Columns.Contains("ItemStatus")) dt.Columns.Add("ItemStatus", typeof(int));

                foreach (DataRow row in dt.Rows)
                {
                    row["ItemStatus"] = (int)CatalogueItemStatus.Active;
                }

                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.KeepIdentity, transaction))
                {
                    bulkCopy.DestinationTableName = "dbo.ItemCatalogues";
                    bulkCopy.ColumnMappings.Add("C-ASIN", "Casin");
                    bulkCopy.ColumnMappings.Add("Model", "Model");
                    bulkCopy.ColumnMappings.Add("Description", "Description");
                    bulkCopy.ColumnMappings.Add("Color Name", "ColorName");
                    bulkCopy.ColumnMappings.Add("Size", "Size");
                    bulkCopy.ColumnMappings.Add("PC/PK", "PCPK");
                    bulkCopy.ColumnMappings.Add("Case Pack Qty", "CasePackQty");
                    bulkCopy.ColumnMappings.Add("CreatedAt", "CreatedAt");
                    bulkCopy.ColumnMappings.Add("CreatedById", "CreatedById");
                    bulkCopy.ColumnMappings.Add("Notes", "Notes");
                    bulkCopy.ColumnMappings.Add("ItemStatus", "ItemStatus");

                    bulkCopy.WriteToServer(dt);
                }
                return new Response<bool> { Success = true, Data = true };
            }
            catch (Exception ex)
            {
                return new Response<bool> { Success = false, Message = $"Error: {ex.Message}" };
            }
        }
        #endregion
    }
}