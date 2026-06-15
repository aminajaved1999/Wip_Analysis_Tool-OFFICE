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
    public class ItemsRepository : IItemsRepository
    {
        private readonly WIPATContext _context;

        #region Constructor

        public ItemsRepository(WIPATContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        #endregion

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
                                         isActive = c.isActive,
                                         Notes = c.Notes,
                                         OpeningStock = s.OpeningStock,
                                         ItemStatus = c.ItemStatus
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

                return new Response<List<ItemCatalogueDto>>
                {
                    Success = false,
                    Data = null,
                    Message = "No ItemCatalogues found"
                };
            }
            catch (Exception ex)
            {
                return new Response<List<ItemCatalogueDto>>
                {
                    Success = false,
                    Data = null,
                    Message = $"An unexpected error occurred while retrieving item catalogues with stock: {ex.Message}"
                               + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                               + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "")
                };
            }
        }

        public async Task<Response<List<ItemCatalogue>>> GetActiveItemCatalogues(bool includeInactive = false)
        {
            try
            {
                var query = _context.ItemCatalogues.AsQueryable();

                if (!includeInactive)
                {
                    query = query.Where(i => i.isActive == true);
                }

                var items = await query.ToListAsync();

                if (items != null && items.Count > 0)
                {
                    return new Response<List<ItemCatalogue>>
                    {
                        Success = true,
                        Data = items,
                        Message = "ItemCatalogues retrieved successfully"
                    };
                }

                return new Response<List<ItemCatalogue>>
                {
                    Success = false,
                    Data = null,
                    Message = "No ItemCatalogues found"
                };
            }
            catch (Exception ex)
            {
                return new Response<List<ItemCatalogue>>
                {
                    Success = false,
                    Message = $"An unexpected error occurred while retrieving active item catalogues: {ex.Message}"
                               + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                               + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "")
                };
            }
        }

        public int GetItemIdByAsin(string asin)
        {
            var item = _context.ItemCatalogues
                               .AsNoTracking()
                               .Where(i => i.isActive == true)
                               .FirstOrDefault(i => i.Casin == asin);
            return item?.Id ?? 0;
        }

        public ItemCatalogue GetItemByCAsin(string casin)
        {
            return _context.ItemCatalogues
                           .AsNoTracking()
                           .FirstOrDefault(item => item.Casin == casin && item.isActive == true);
        }

        public async Task<Dictionary<string, int>> GetCatalogueIdsByCasinsAsync(List<string> casins)
        {
            if (casins == null || !casins.Any())
            {
                return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            }

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
                    {
                        dictionary.Add(item.Casin, item.Id);
                    }
                }

                return dictionary;
            }
            catch (Exception ex)
            {
                string errorMsg = $"An unexpected error occurred while fetching catalogue IDs by CASINs: {ex.Message}"
                                  + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                                  + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "");
                throw new Exception(errorMsg, ex);
            }
        }

        #endregion

        #region Verification Operations

        public bool IsCAsinExistInCatalogue(string casin)
        {
            return _context.ItemCatalogues.Any(item => item.Casin == casin && item.isActive == true);
        }

        public async Task<bool?> CheckCAsinStatus(string casin)
        {
            return await _context.ItemCatalogues
                               .Where(item => item.Casin == casin)
                               .Select(item => (bool?)item.isActive)
                               .FirstOrDefaultAsync();
        }

        public async Task<Response<bool>> IsCasinExistInCatalogueAndInitialStock(string casin)
        {
            try
            {
                bool existsInCatalogue = await _context.ItemCatalogues.AnyAsync(item => item.Casin == casin && item.isActive == true);
                bool existsInInitialStock = await _context.InitialStocks.AnyAsync(stock => stock.ItemCatalogue.Casin == casin && stock.ItemCatalogue.isActive);

                if (existsInCatalogue || existsInInitialStock)
                {
                    string msg;
                    if (existsInCatalogue && existsInInitialStock)
                    {
                        msg = $"The Casin '{casin}' exists in both the Item Catalogue and the Initial Stock.";
                    }
                    else if (existsInCatalogue)
                    {
                        msg = $"The Casin '{casin}' exists in the Item Catalogue, but not in the Initial Stock.";
                    }
                    else
                    {
                        msg = $"The Casin '{casin}' exists in the Initial Stock, but not in the Item Catalogue.";
                    }

                    return new Response<bool> { Success = true, Data = true, Message = msg };
                }

                return new Response<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"The Casin '{casin}' was not found in either the Item Catalogue or the Initial Stock."
                };
            }
            catch (Exception ex)
            {
                return new Response<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"An unexpected error occurred while checking Casin existence in catalogue and stock: {ex.Message}"
                               + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                               + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "")
                };
            }
        }

        public async Task<Response<bool>> IsItemExistInCatalogue(string asin)
        {
            try
            {
                bool existsInCatalogue = await _context.ItemCatalogues.AnyAsync(item => item.Casin == asin && item.isActive == true);

                if (existsInCatalogue)
                {
                    return new Response<bool>
                    {
                        Success = true,
                        Data = true,
                        Message = $"The CASIN '{asin}' exists in the Item Catalogue."
                    };
                }

                return new Response<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"The CASIN '{asin}' was not found in the Item Catalogue."
                };
            }
            catch (Exception ex)
            {
                return new Response<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"An unexpected error occurred while checking item existence in catalogue: {ex.Message}"
                               + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                               + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "")
                };
            }
        }

        #endregion

        #region Write Operations

        public bool AddItemToCatalogue(string casin, string itemName = null, string description = null)
        {
            try
            {
                bool exists = _context.ItemCatalogues.Any(item => item.Casin == casin && item.isActive == true);
                if (exists)
                {
                    return true;
                }

                var newItem = new ItemCatalogue
                {
                    Casin = casin,
                    Description = description ?? string.Empty,
                    CreatedAt = DateTime.Now,
                };

                _context.ItemCatalogues.Add(newItem);
                _context.SaveChanges();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public Response<bool> BulkUpdateCatalogueAndStatusImport(DataTable catalogueTable, DataTable stockTable)
        {
            if (catalogueTable == null || catalogueTable.Rows.Count == 0)
            {
                return new Response<bool> { Success = false, Data = false, Message = "Import failed: The provided catalogue table is empty or null." };
            }

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    if (!catalogueTable.Columns.Contains("C-ASIN"))
                    {
                        return new Response<bool> { Success = false, Data = false, Message = "Import failed: The required 'C-ASIN' identifier column is missing from the file." };
                    }

                    var incomingCasins = catalogueTable.AsEnumerable()
                        .Select(r => r["C-ASIN"]?.ToString()?.Trim())
                        .Where(c => !string.IsNullOrEmpty(c))
                        .Distinct()
                        .ToList();

                    if (!incomingCasins.Any())
                    {
                        return new Response<bool> { Success = false, Data = false, Message = "Import failed: No valid CASINs were found in the uploaded file." };
                    }

                    var existingItems = _context.ItemCatalogues
                        .Where(i => incomingCasins.Contains(i.Casin))
                        .ToDictionary(i => i.Casin, StringComparer.OrdinalIgnoreCase);

                    var missingCasins = incomingCasins.Where(c => !existingItems.ContainsKey(c)).ToList();

                    if (missingCasins.Any())
                    {
                        return new Response<bool>
                        {
                            Success = false,
                            Data = false,
                            Message = $"Import failed: The following CASIN(s) were not found in the database: {string.Join(", ", missingCasins)}"
                        };
                    }

                    var existingItemIds = existingItems.Values.Select(i => i.Id).ToList();
                    var existingStocks = _context.InitialStocks
                        .Where(s => existingItemIds.Contains(s.ItemCatalogueId))
                        .ToDictionary(s => s.ItemCatalogueId);

                    var externalStockDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                    if (stockTable != null && stockTable.Columns.Contains("C-ASIN") && stockTable.Columns.Contains("OpeningStock"))
                    {
                        foreach (DataRow row in stockTable.Rows)
                        {
                            string sCasin = row["C-ASIN"]?.ToString()?.Trim();
                            if (!string.IsNullOrEmpty(sCasin) && int.TryParse(row["OpeningStock"]?.ToString(), out int stockVal))
                            {
                                externalStockDict[sCasin] = stockVal;
                            }
                        }
                    }

                    bool hasModel = catalogueTable.Columns.Contains("Model");
                    bool hasDesc = catalogueTable.Columns.Contains("Description");
                    bool hasColor = catalogueTable.Columns.Contains("Color Name");
                    bool hasSize = catalogueTable.Columns.Contains("Size");
                    bool hasPcpk = catalogueTable.Columns.Contains("PC/PK");
                    bool hasCasePack = catalogueTable.Columns.Contains("Case Pack Qty");
                    bool hasNotes = catalogueTable.Columns.Contains("Notes");
                    bool hasIsActive = catalogueTable.Columns.Contains("IsActive");
                    bool hasCatStock = catalogueTable.Columns.Contains("OpeningStock");
                    bool hasUpdatedAt = catalogueTable.Columns.Contains("UpdatedAt");
                    bool hasUpdatedById = catalogueTable.Columns.Contains("UpdatedById");

                    foreach (DataRow row in catalogueTable.Rows)
                    {
                        string casin = row["C-ASIN"]?.ToString()?.Trim();
                        var item = existingItems[casin];

                        if (hasUpdatedAt && !row.IsNull("UpdatedAt"))
                        {
                            item.UpdatedAt = Convert.ToDateTime(row["UpdatedAt"]);
                        }

                        if (hasUpdatedById && !row.IsNull("UpdatedById"))
                        {
                            item.UpdatedById = Convert.ToInt32(row["UpdatedById"]);
                        }

                        if (hasModel && !row.IsNull("Model"))
                        {
                            item.Model = row["Model"].ToString();
                        }

                        if (hasDesc && !row.IsNull("Description"))
                        {
                            item.Description = row["Description"].ToString();
                        }

                        if (hasColor && !row.IsNull("Color Name"))
                        {
                            item.ColorName = row["Color Name"].ToString();
                        }

                        if (hasSize && !row.IsNull("Size"))
                        {
                            item.Size = row["Size"].ToString();
                        }

                        if (hasPcpk && !row.IsNull("PC/PK"))
                        {
                            item.PCPK = row["PC/PK"].ToString();
                        }

                        if (hasCasePack && !row.IsNull("Case Pack Qty") && int.TryParse(row["Case Pack Qty"].ToString(), out int cpQty))
                        {
                            item.CasePackQty = cpQty;
                        }

                        if (hasNotes && !row.IsNull("Notes"))
                        {
                            item.Notes = row["Notes"].ToString();
                        }

                        if (hasIsActive && !row.IsNull("IsActive"))
                        {
                            item.isActive = bool.TryParse(row["IsActive"].ToString().Trim(), out bool isActive) && isActive;
                        }

                        if (existingStocks.TryGetValue(item.Id, out var stockRecord))
                        {
                            bool isStockUpdated = false;

                            if (hasCatStock && !row.IsNull("OpeningStock") && int.TryParse(row["OpeningStock"].ToString(), out int catStockVal))
                            {
                                stockRecord.OpeningStock = catStockVal;
                                isStockUpdated = true;
                            }
                            else if (externalStockDict.TryGetValue(casin, out int extStockVal))
                            {
                                stockRecord.OpeningStock = extStockVal;
                                isStockUpdated = true;
                            }

                            if (isStockUpdated)
                            {
                                if (hasUpdatedAt && !row.IsNull("UpdatedAt"))
                                {
                                    stockRecord.UpdatedAt = Convert.ToDateTime(row["UpdatedAt"]);
                                }

                                if (hasUpdatedById && !row.IsNull("UpdatedById"))
                                {
                                    stockRecord.UpdatedById = Convert.ToInt32(row["UpdatedById"]);
                                }
                            }
                        }
                    }

                    _context.SaveChanges();
                    transaction.Commit();

                    return new Response<bool>
                    {
                        Success = true,
                        Data = true,
                        Message = $"Successfully updated {catalogueTable.Rows.Count} items."
                    };
                }
                catch (System.Data.DataException dataEx)
                {
                    transaction.Rollback();
                    return new Response<bool>
                    {
                        Success = false,
                        Data = false,
                        Message = $"An unexpected error occurred while reading the table structures: {dataEx.Message}"
                                   + (dataEx.InnerException != null ? $" Inner Exception: {dataEx.InnerException.Message}"
                                   + (dataEx.InnerException.InnerException != null ? $" Inner Inner Exception: {dataEx.InnerException.InnerException.Message}" : "") : "")
                    };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new Response<bool>
                    {
                        Success = false,
                        Data = false,
                        Message = $"An unexpected error occurred during the bulk update: {ex.Message}"
                                   + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                                   + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "")
                    };
                }
            }
        }

        #endregion

        #region Bulk Insert Invalid Catalogue Along With Stock

        public Response<bool> BulkInsertInvalidCatalogueImport(DataTable dtInvalidItems, DataTable dtInitialStock)
        {
            var response = new Response<bool>();
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
                            response.Success = false;
                            response.Message = $"Failed to bulk insert Invalid ItemsCatalogue: {resSaveCatalogue.Message}";
                            response.Data = false;
                            return response;
                        }

                        var resUpdateStockTable = MapItemCatalogueIds(dtInvalidItems, dtInitialStock, conn, transaction);
                        if (!resUpdateStockTable.Success)
                        {
                            transaction.Rollback();
                            response.Success = false;
                            response.Message = "Failed to Map ItemCatalogue Ids for invalid items.";
                            response.Data = false;
                            return response;
                        }

                        var resSaveStock = BulkInsertInvalidInitialStock(resUpdateStockTable.Data, conn, transaction);
                        if (!resSaveStock.Success)
                        {
                            transaction.Rollback();
                            response.Success = false;
                            response.Message = $"Failed to bulk insert Stock for invalid items: {resSaveStock.Message}";
                            response.Data = false;
                            return response;
                        }

                        transaction.Commit();
                        response.Success = true;
                        response.Message = "Invalid catalogue import with stock completed successfully.";
                        response.Data = true;
                        return response;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        response.Success = false;
                        response.Message = $"An unexpected error occurred while performing bulk insert invalid catalogue import: {ex.Message}"
                                           + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                                           + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "");
                        response.Data = false;
                        return response;
                    }
                }
            }
        }

        public Response<bool> BulkInsertInvalidCatalogue(DataTable dtInvalidItems, SqlConnection conn, SqlTransaction transaction)
        {
            var response = new Response<bool>();

            try
            {
                var existingCasins = new HashSet<string>();
                string query = "SELECT Casin FROM dbo.ItemCatalogues";

                using (var cmd = new SqlCommand(query, conn, transaction))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        existingCasins.Add(reader["Casin"].ToString());
                    }
                }

                var seenInFile = new HashSet<string>();
                var duplicateCasins = new List<string>();

                foreach (DataRow row in dtInvalidItems.Rows)
                {
                    string casin = row["Casin"].ToString();

                    if (existingCasins.Contains(casin) || seenInFile.Contains(casin))
                    {
                        duplicateCasins.Add(casin);
                    }
                    else
                    {
                        seenInFile.Add(casin);
                    }
                }

                if (duplicateCasins.Any())
                {
                    response.Success = false;
                    response.Message = $"Duplicate Casins found: {string.Join(", ", duplicateCasins.Distinct())}";
                    return response;
                }

                if (!dtInvalidItems.Columns.Contains("IsActive"))
                    dtInvalidItems.Columns.Add("IsActive", typeof(bool));

                if (!dtInvalidItems.Columns.Contains("ItemStatus"))
                    dtInvalidItems.Columns.Add("ItemStatus", typeof(string));

                foreach (DataRow row in dtInvalidItems.Rows)
                {
                    row["IsActive"] = false;
                    row["ItemStatus"] = ItemStatus.Invalid.ToString();

                    if (row.Table.Columns.Contains("CreatedAt"))
                        row["CreatedAt"] = DateTime.Now;

                    if (row.Table.Columns.Contains("CasePackQty") && row["CasePackQty"] == DBNull.Value)
                        row["CasePackQty"] = 0;

                    if (row.Table.Columns.Contains("PCPK"))
                        row["PCPK"] = row["PCPK"] ?? DBNull.Value;
                }

                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.KeepIdentity, transaction))
                {
                    bulkCopy.DestinationTableName = "dbo.ItemCatalogues";
                    bulkCopy.ColumnMappings.Clear();

                    bulkCopy.ColumnMappings.Add("Casin", "Casin");
                    bulkCopy.ColumnMappings.Add("Model", "Model");
                    bulkCopy.ColumnMappings.Add("Description", "Description");
                    bulkCopy.ColumnMappings.Add("ColorName", "ColorName");
                    bulkCopy.ColumnMappings.Add("Size", "Size");
                    bulkCopy.ColumnMappings.Add("PCPK", "PCPK");
                    bulkCopy.ColumnMappings.Add("CasePackQty", "CasePackQty");
                    bulkCopy.ColumnMappings.Add("CreatedAt", "CreatedAt");
                    bulkCopy.ColumnMappings.Add("CreatedById", "CreatedById");
                    bulkCopy.ColumnMappings.Add("Notes", "Notes");
                    bulkCopy.ColumnMappings.Add("IsActive", "isActive");
                    bulkCopy.ColumnMappings.Add("ItemStatus", "ItemStatus");

                    bulkCopy.WriteToServer(dtInvalidItems);
                }

                response.Success = true;
                response.Message = "Invalid catalogue items inserted successfully.";
                response.Data = true;
            }
            catch (SqlException sqlEx)
            {
                if (sqlEx.Number == 2627 || sqlEx.Number == 2601)
                {
                    response.Success = false;
                    response.Message = "Duplicate Casin detected while inserting invalid items.";
                }
                else
                {
                    response.Success = false;
                    response.Message = $"An unexpected error occurred during database operation: {sqlEx.Message}"
                                       + (sqlEx.InnerException != null ? $" Inner Exception: {sqlEx.InnerException.Message}"
                                       + (sqlEx.InnerException.InnerException != null ? $" Inner Inner Exception: {sqlEx.InnerException.InnerException.Message}" : "") : "");
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"An unexpected error occurred while inserting invalid catalogue: {ex.Message}"
                                   + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                                   + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "");
            }

            return response;
        }

        public Response<bool> BulkInsertInvalidInitialStock(DataTable dt, SqlConnection conn, SqlTransaction transaction)
        {
            var response = new Response<bool>();

            try
            {
                if (!dt.Columns.Contains("OrderQty"))
                    dt.Columns.Add("OrderQty", typeof(int));
                if (!dt.Columns.Contains("ProductionQty"))
                    dt.Columns.Add("ProductionQty", typeof(int));

                foreach (DataRow row in dt.Rows)
                {
                    if (row["OrderQty"] == DBNull.Value) row["OrderQty"] = 0;
                    if (row["ProductionQty"] == DBNull.Value) row["ProductionQty"] = 0;
                    if (row["OpeningStock"] == DBNull.Value) row["OpeningStock"] = 0;
                }

                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.KeepIdentity, transaction))
                {
                    bulkCopy.DestinationTableName = "dbo.InitialStocks";
                    bulkCopy.ColumnMappings.Clear();

                    bulkCopy.ColumnMappings.Add("ItemCatalogueId", "ItemCatalogueId");
                    bulkCopy.ColumnMappings.Add("OpeningStock", "OpeningStock");
                    bulkCopy.ColumnMappings.Add("CreatedAt", "CreatedAt");
                    bulkCopy.ColumnMappings.Add("CreatedById", "CreatedById");
                    bulkCopy.ColumnMappings.Add("OrderQty", "OrderQty");
                    bulkCopy.ColumnMappings.Add("ProductionQty", "ProductionQty");

                    bulkCopy.WriteToServer(dt);
                }

                response.Success = true;
                response.Message = "Bulk insert into InitialStocks completed successfully.";
                response.Data = true;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"An unexpected error occurred while bulk inserting initial stock: {ex.Message}"
                                   + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                                   + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "");
                response.Data = false;
            }

            return response;
        }

        public Response<DataTable> MapItemCatalogueIds(DataTable dtItemCatalogues, DataTable dtInitialStock, SqlConnection conn, SqlTransaction transaction)
        {
            var response = new Response<DataTable>();

            try
            {
                var itemCatalogueIdMap = new Dictionary<string, int>();
                string query = "SELECT Casin, Id FROM dbo.ItemCatalogues";

                using (var cmd = new SqlCommand(query, conn, transaction))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string casin = reader["Casin"].ToString();
                        int itemCatalogueId = Convert.ToInt32(reader["Id"]);
                        itemCatalogueIdMap[casin] = itemCatalogueId;
                    }
                }

                var missingCasins = new List<string>();

                foreach (DataRow row in dtInitialStock.Rows)
                {
                    string casin = row["Casin"].ToString();
                    if (itemCatalogueIdMap.ContainsKey(casin))
                    {
                        row["ItemCatalogueId"] = itemCatalogueIdMap[casin];
                    }
                    else
                    {
                        missingCasins.Add(casin);
                    }
                }

                if (missingCasins.Any())
                {
                    response.Success = false;
                    response.Message = $"Failed to map ItemCatalogueId for Casin(s): {string.Join(", ", missingCasins)}";
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
                response.Message = $"An unexpected error occurred while mapping ItemCatalogueIds: {ex.Message}"
                                   + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                                   + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "");
                response.Data = null;
                return response;
            }
        }

        #endregion
    }
}