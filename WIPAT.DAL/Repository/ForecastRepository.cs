using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using WIPAT.DAL.Interfaces;
using WIPAT.Entities;
using WIPAT.Entities.Dto;
using WIPAT.Entities.Enum;

namespace WIPAT.DAL
{
    public class ForecastRepository : IForecastRepository
    {
        private readonly WIPATContext _context;

        public ForecastRepository(WIPATContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        #region Save Imported Files

        public Response<string> _SaveForecastDataToDatabase(ForecastFileData forecastData, bool isFirstFile)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // 1. Validation Checks
                    var existingFileRes = IsFileAlreadyImported(forecastData.FileName);
                    if (existingFileRes.Success)
                    {
                        return new Response<string> { Success = false, Message = $"Data with filename '{forecastData.FileName}' already exists. Please use a different file." };
                    }

                    if (IsProjectionAlreadyExists(forecastData.ProjectionMonth, forecastData.ProjectionYear))
                    {
                        return new Response<string> { Success = false, Message = $"Forecast data for {forecastData.ProjectionMonth}/{forecastData.ProjectionYear} already exists." };
                    }

                    if (IsWipAlreadyCalculated(forecastData.ProjectionMonth, forecastData.ProjectionYear))
                    {
                        return new Response<string> { Success = false, Message = $"WIP has already been calculated for {forecastData.ProjectionMonth}/{forecastData.ProjectionYear}. Duplicate calculation is not allowed." };
                    }

                    // 2. Save Master Record
                    var master = new ForecastMaster
                    {
                        Month = forecastData.ProjectionMonth,
                        Year = forecastData.ProjectionYear,
                        ForecastingFor = forecastData.ForecastFor,
                        FileName = forecastData.FileName,
                        CreatedBy = Environment.UserName,
                        CreatedAt = DateTime.Now
                    };

                    _context.ForecastMasters.Add(master);
                    _context.SaveChanges();
                    int masterId = master.Id;

                    // 3. Prepare DataTable for Bulk Insert
                    var catalogueLookup = _context.ItemCatalogues
                        .Select(x => new { x.Id, x.Casin, x.isActive })
                        .ToDictionary(x => x.Casin, x => new { x.Id, x.isActive });

                    DataTable bulkTable = new DataTable();
                    bulkTable.Columns.Add("ItemCatalogueId", typeof(int));
                    bulkTable.Columns.Add("CASIN", typeof(string));
                    bulkTable.Columns.Add("RequestedQuantity", typeof(int));
                    bulkTable.Columns.Add("Wip", typeof(int));
                    bulkTable.Columns.Add("CommitmentPeriod", typeof(string));
                    bulkTable.Columns.Add("PODate", typeof(DateTime));
                    bulkTable.Columns.Add("Month", typeof(string));
                    bulkTable.Columns.Add("Year", typeof(string));
                    bulkTable.Columns.Add("POForecastMasterId", typeof(int));
                    bulkTable.Columns.Add("IsSystemGenerated", typeof(bool));
                    bulkTable.Columns.Add("IsActive", typeof(bool));

                    foreach (DataRow row in forecastData.FullTable.Rows)
                    {
                        var casinValue = row["C-ASIN"].ToString();
                        var newRow = bulkTable.NewRow();

                        if (catalogueLookup.TryGetValue(casinValue, out var catInfo))
                        {
                            newRow["ItemCatalogueId"] = catInfo.Id;
                            newRow["IsActive"] = catInfo.isActive;
                        }
                        else
                        {
                            return new Response<string> { Success = false, Message = $"Import failed: The CASIN '{casinValue}' does not exist in the Item Catalogue. Please register it before uploading." };
                        }

                        newRow["CASIN"] = casinValue;
                        newRow["RequestedQuantity"] = int.TryParse(row["Requested Quantity"].ToString(), out int qty) ? qty : 0;
                        newRow["Wip"] = isFirstFile && int.TryParse(row["Wip"].ToString(), out int wipVal) ? wipVal : (object)DBNull.Value;
                        newRow["CommitmentPeriod"] = row["Commitment period"].ToString();
                        newRow["PODate"] = DateTime.TryParse(row["PO date"].ToString(), out DateTime poDate) ? poDate : DateTime.MinValue;
                        newRow["Month"] = row["Month"].ToString();
                        newRow["Year"] = row["Year"].ToString();
                        newRow["POForecastMasterId"] = masterId;
                        newRow["IsSystemGenerated"] = row.Table.Columns.Contains("IsSystemGenerated") && row["IsSystemGenerated"] != DBNull.Value
                                                      ? Convert.ToBoolean(row["IsSystemGenerated"])
                                                      : false;

                        bulkTable.Rows.Add(newRow);
                    }

                    // 4. Execute SQL Bulk Copy
                    var sqlConnection = (SqlConnection)_context.Database.Connection;
                    var sqlTransaction = (SqlTransaction)transaction.UnderlyingTransaction;
                    if (sqlConnection.State != ConnectionState.Open) sqlConnection.Open();

                    using (var sqlBulkCopy = new SqlBulkCopy(sqlConnection, SqlBulkCopyOptions.Default, sqlTransaction))
                    {
                        sqlBulkCopy.DestinationTableName = "dbo.ForecastDetails";

                        sqlBulkCopy.ColumnMappings.Add("ItemCatalogueId", "ItemCatalogueId");
                        sqlBulkCopy.ColumnMappings.Add("CASIN", "CASIN");
                        sqlBulkCopy.ColumnMappings.Add("RequestedQuantity", "RequestedQuantity");
                        sqlBulkCopy.ColumnMappings.Add("Wip", "Wip");
                        sqlBulkCopy.ColumnMappings.Add("CommitmentPeriod", "CommitmentPeriod");
                        sqlBulkCopy.ColumnMappings.Add("PODate", "PODate");
                        sqlBulkCopy.ColumnMappings.Add("Month", "Month");
                        sqlBulkCopy.ColumnMappings.Add("Year", "Year");
                        sqlBulkCopy.ColumnMappings.Add("POForecastMasterId", "POForecastMasterId");
                        sqlBulkCopy.ColumnMappings.Add("IsSystemGenerated", "IsSystemGenerated");
                        sqlBulkCopy.ColumnMappings.Add("IsActive", "IsActive");

                        sqlBulkCopy.WriteToServer(bulkTable);
                    }

                    transaction.Commit();
                    return new Response<string> { Success = true, Message = "Data saved successfully!" };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new Response<string>
                    {
                        Success = false,
                        Message = $"An unexpected error occurred while saving the initial forecast data to the database: {ex.Message}"
                                  + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                                  + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "")
                    };
                }
            }
        }

        public Response<string> SaveForecastDataToDatabase(ForecastFileData forecastData, bool isFirstFile, bool IsContinueWithInactiveItems)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // 1. Validation Checks
                    var existingFileRes = IsFileAlreadyImported(forecastData.FileName);
                    if (existingFileRes.Success)
                    {
                        return new Response<string> { Success = false, Message = $"Data with filename '{forecastData.FileName}' already exists. Please use a different file." };
                    }

                    if (IsProjectionAlreadyExists(forecastData.ProjectionMonth, forecastData.ProjectionYear))
                    {
                        return new Response<string> { Success = false, Message = $"Forecast data for {forecastData.ProjectionMonth}/{forecastData.ProjectionYear} already exists." };
                    }

                    if (IsWipAlreadyCalculated(forecastData.ProjectionMonth, forecastData.ProjectionYear))
                    {
                        return new Response<string> { Success = false, Message = $"WIP has already been calculated for {forecastData.ProjectionMonth}/{forecastData.ProjectionYear}. Duplicate calculation is not allowed." };
                    }

                    // 2. Save Master Record
                    var master = new ForecastMaster
                    {
                        Month = forecastData.ProjectionMonth,
                        Year = forecastData.ProjectionYear,
                        ForecastingFor = forecastData.ForecastFor,
                        FileName = forecastData.FileName,
                        CreatedBy = Environment.UserName,
                        CreatedAt = DateTime.Now,
                        IsContinueWithInactiveItems = IsContinueWithInactiveItems
                    };

                    _context.ForecastMasters.Add(master);
                    _context.SaveChanges();
                    int masterId = master.Id;

                    // 3. Prepare DataTable for Bulk Insert
                    var catalogueLookup = _context.ItemCatalogues
                        .Select(x => new { x.Id, x.Casin, x.isActive })
                        .ToDictionary(x => x.Casin, x => new { x.Id, x.isActive });

                    DataTable bulkTable = new DataTable();
                    bulkTable.Columns.Add("ItemCatalogueId", typeof(int));
                    bulkTable.Columns.Add("CASIN", typeof(string));
                    bulkTable.Columns.Add("RequestedQuantity", typeof(int));
                    bulkTable.Columns.Add("Wip", typeof(int));
                    bulkTable.Columns.Add("CommitmentPeriod", typeof(string));
                    bulkTable.Columns.Add("PODate", typeof(DateTime));
                    bulkTable.Columns.Add("Month", typeof(string));
                    bulkTable.Columns.Add("Year", typeof(string));
                    bulkTable.Columns.Add("POForecastMasterId", typeof(int));
                    bulkTable.Columns.Add("IsSystemGenerated", typeof(bool));
                    bulkTable.Columns.Add("IsActive", typeof(bool));

                    foreach (DataRow row in forecastData.FullTable.Rows)
                    {
                        var casinValue = row["C-ASIN"].ToString();
                        var newRow = bulkTable.NewRow();

                        if (catalogueLookup.TryGetValue(casinValue, out var catInfo))
                        {
                            newRow["ItemCatalogueId"] = catInfo.Id;
                            newRow["IsActive"] = catInfo.isActive;
                        }
                        else
                        {
                            return new Response<string> { Success = false, Message = $"Import failed: The CASIN '{casinValue}' does not exist in the Item Catalogue. Please register it before uploading." };
                        }

                        newRow["CASIN"] = casinValue;
                        newRow["RequestedQuantity"] = int.TryParse(row["Requested Quantity"].ToString(), out int qty) ? qty : 0;
                        newRow["Wip"] = isFirstFile && int.TryParse(row["Wip"].ToString(), out int wipVal) ? wipVal : (object)DBNull.Value;
                        newRow["CommitmentPeriod"] = row["Commitment period"].ToString();
                        newRow["PODate"] = DateTime.TryParse(row["PO date"].ToString(), out DateTime poDate) ? poDate : DateTime.MinValue;
                        newRow["Month"] = row["Month"].ToString();
                        newRow["Year"] = row["Year"].ToString();
                        newRow["POForecastMasterId"] = masterId;
                        newRow["IsSystemGenerated"] = row.Table.Columns.Contains("IsSystemGenerated") && row["IsSystemGenerated"] != DBNull.Value
                                                      ? Convert.ToBoolean(row["IsSystemGenerated"])
                                                      : false;

                        bulkTable.Rows.Add(newRow);
                    }

                    // 4. Execute SQL Bulk Copy
                    var sqlConnection = (SqlConnection)_context.Database.Connection;
                    var sqlTransaction = (SqlTransaction)transaction.UnderlyingTransaction;
                    if (sqlConnection.State != ConnectionState.Open) sqlConnection.Open();

                    using (var sqlBulkCopy = new SqlBulkCopy(sqlConnection, SqlBulkCopyOptions.Default, sqlTransaction))
                    {
                        sqlBulkCopy.DestinationTableName = "dbo.ForecastDetails";

                        sqlBulkCopy.ColumnMappings.Add("ItemCatalogueId", "ItemCatalogueId");
                        sqlBulkCopy.ColumnMappings.Add("CASIN", "CASIN");
                        sqlBulkCopy.ColumnMappings.Add("RequestedQuantity", "RequestedQuantity");
                        sqlBulkCopy.ColumnMappings.Add("Wip", "Wip");
                        sqlBulkCopy.ColumnMappings.Add("CommitmentPeriod", "CommitmentPeriod");
                        sqlBulkCopy.ColumnMappings.Add("PODate", "PODate");
                        sqlBulkCopy.ColumnMappings.Add("Month", "Month");
                        sqlBulkCopy.ColumnMappings.Add("Year", "Year");
                        sqlBulkCopy.ColumnMappings.Add("POForecastMasterId", "POForecastMasterId");
                        sqlBulkCopy.ColumnMappings.Add("IsSystemGenerated", "IsSystemGenerated");
                        sqlBulkCopy.ColumnMappings.Add("IsActive", "IsActive");

                        sqlBulkCopy.WriteToServer(bulkTable);
                    }

                    transaction.Commit();
                    return new Response<string> { Success = true, Message = "Data saved successfully!" };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new Response<string>
                    {
                        Success = false,
                        Message = $"An unexpected error occurred while saving forecast data to the database: {ex.Message}"
                                  + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                                  + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "")
                    };
                }
            }
        }

        #endregion

        #region Checks

        public Response<ForecastFileData> IsFileAlreadyImported(string fileName)
        {
            var response = new Response<ForecastFileData>();
            try
            {
                var forecastMaster = _context.ForecastMasters
                    .Include(m => m.Details)
                    .FirstOrDefault(m => m.FileName == fileName);

                if (forecastMaster == null)
                {
                    response.Message = $"No forecast file found with the name '{fileName}'.";
                    response.Status = StatusType.Warning;
                }
                else
                {
                    response.Message = $"Forecast file '{fileName}' found successfully.";
                    response.Status = StatusType.Success;
                    response.Data = new ForecastFileData
                    {
                        FileName = forecastMaster.FileName,
                        ProjectionMonth = forecastMaster.Month,
                        ProjectionYear = forecastMaster.Year
                    };
                }
                return response;
            }
            catch (Exception ex)
            {
                return new Response<ForecastFileData>
                {
                    Success = false,
                    Message = $"An unexpected error occurred while checking if the file is already imported: {ex.Message}"
                              + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                              + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "")
                };
            }
        }

        public bool IsProjectionAlreadyExists(string month, string year)
        {
            return _context.ForecastMasters.Any(fm => fm.Month == month && fm.Year == year);
        }

        public bool IsWipAlreadyCalculated(string month, string year)
        {
            return _context.ForecastMasters.Any(fm => fm.Month == month && fm.Year == year && fm.IsWipCalculated);
        }

        public Response<ForecastCheckResult> PerformForecastChecks2(string fileName, string month, string year)
        {
            var result = new Response<ForecastCheckResult>
            {
                Data = new ForecastCheckResult
                {
                    FileData = new ForecastFileData
                    {
                        FullTable = new DataTable(),
                        Forecast = new ForecastMaster()
                    }
                }
            };

            if (string.IsNullOrEmpty(fileName)) return new Response<ForecastCheckResult> { Success = false, Message = "FileName Required" };
            if (string.IsNullOrEmpty(month)) return new Response<ForecastCheckResult> { Success = false, Message = "Projection Month Required" };
            if (string.IsNullOrEmpty(year)) return new Response<ForecastCheckResult> { Success = false, Message = "Projection Year Required" };

            try
            {
                var forecastByFileName = _context.ForecastMasters.FirstOrDefault(fm => fm.FileName == fileName);
                var forecastByFileData = _context.ForecastMasters.FirstOrDefault(fm => fm.Month == month && fm.Year == year);

                if (forecastByFileName != null)
                {
                    result.Data.FileExists = true;
                    if (forecastByFileName.IsWipCalculated)
                    {
                        result.Data.IsWipCalculated = true;
                    }
                }

                if (forecastByFileData != null)
                {
                    result.Data.ProjectionExists = true;
                    if (forecastByFileData.IsWipCalculated)
                    {
                        result.Data.IsWipCalculated = true;
                    }
                }

                if (result.Data.ProjectionExists || result.Data.FileExists)
                {
                    string msg = $"A forecast for {month} {year} already exists";
                    if (result.Data.IsWipCalculated) msg += ", with WIP already calculated.";

                    var existingForecast = GetForecastDataFromDB(month, year);
                    if (!existingForecast.Success)
                    {
                        result.Success = false;
                        result.Message = $"{msg}. Failed to load existing data: {existingForecast.Message}";
                        return result;
                    }

                    // Indicates "Don't process new file, use DB"
                    result.Success = false;
                    result.Data.FileData.FullTable = existingForecast.Data.Item1;
                    result.Data.FileData.Forecast = existingForecast.Data.Item2;
                    result.Message = msg;
                    return result;
                }

                result.Success = true;
                result.Message = "No existing forecast found. Ready to create new.";
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"An unexpected error occurred while performing forecast checks: {ex.Message}"
                                 + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                                 + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "");
                return result;
            }
        }

        #endregion

        #region Retrieval Methods

        public Response<bool> CheckIfWipCalculated(string month, string year)
        {
            try
            {
                var forecastMaster = _context.ForecastMasters.FirstOrDefault(m => m.Month == month && m.Year == year);

                if (forecastMaster == null)
                {
                    return new Response<bool> { Success = false, Message = $"No forecast found for {month} {year}." };
                }

                if (forecastMaster.IsWipCalculated)
                {
                    return new Response<bool> { Success = false, Message = $"WIP already calculated for {month} {year}.", Data = true };
                }

                return new Response<bool> { Success = true, Message = "WIP not yet calculated.", Data = false };
            }
            catch (Exception ex)
            {
                return new Response<bool>
                {
                    Success = false,
                    Message = $"An unexpected error occurred while checking if WIP is calculated: {ex.Message}"
                              + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                              + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "")
                };
            }
        }

        public Response<Tuple<DataTable, ForecastMaster>> xGetForecastDataFromDB(string month, string year)
        {
            try
            {
                var master = _context.ForecastMasters
                            .AsNoTracking()
                            .Include(m => m.Details)
                            .FirstOrDefault(m => m.Month == month && m.Year == year);

                if (master == null)
                {
                    return new Response<Tuple<DataTable, ForecastMaster>> { Success = false, Status = StatusType.Warning, Message = $"No forecast found for {month} {year}." };
                }

                if (master.Details == null || !master.Details.Any())
                {
                    return new Response<Tuple<DataTable, ForecastMaster>> { Success = false, Status = StatusType.Warning, Message = $"No details found for {month} {year}." };
                }

                var orderedDetails = master.Details.OrderBy(d => d.CASIN).ToList();

                DataTable table = new DataTable();
                table.Columns.Add("C-ASIN", typeof(string));
                table.Columns.Add("Requested Quantity", typeof(int));
                table.Columns.Add("WIP", typeof(int));
                table.Columns.Add("Commitment period", typeof(int));
                table.Columns.Add("PO Date", typeof(DateTime));
                table.Columns.Add("Month", typeof(string));
                table.Columns.Add("Year", typeof(string));

                foreach (var d in orderedDetails)
                {
                    table.Rows.Add(
                        d.CASIN ?? (object)DBNull.Value,
                        d.RequestedQuantity,
                        d.Wip ?? (object)DBNull.Value,
                        d.CommitmentPeriod,
                        d.PODate,
                        d.Month ?? (object)DBNull.Value,
                        d.Year ?? (object)DBNull.Value
                    );
                }

                return new Response<Tuple<DataTable, ForecastMaster>>
                {
                    Success = true,
                    Status = StatusType.Success,
                    Data = new Tuple<DataTable, ForecastMaster>(table, master)
                };
            }
            catch (Exception ex)
            {
                return new Response<Tuple<DataTable, ForecastMaster>>
                {
                    Success = false,
                    Status = StatusType.Error,
                    Message = $"An unexpected error occurred while retrieving legacy forecast data from the database: {ex.Message}"
                              + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                              + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "")
                };
            }
        }

        public Response<Tuple<DataTable, ForecastMaster>> GetForecastDataFromDB(string month, string year)
        {
            try
            {
                var master = _context.ForecastMasters
                            .AsNoTracking()
                            .Include(m => m.Details.Select(d => d.ItemCatalogue))
                            .FirstOrDefault(m => m.Month == month && m.Year == year);

                if (master == null)
                {
                    return new Response<Tuple<DataTable, ForecastMaster>> { Success = false, Status = StatusType.Warning, Message = $"No forecast found for {month} {year}." };
                }

                var details = master.Details
                        .Where(d => d.ItemCatalogue != null && (d.ItemCatalogue.isActive || master.IsContinueWithInactiveItems))
                        .OrderBy(d => d.CASIN)
                        .ToList();

                if (!details.Any())
                {
                    return new Response<Tuple<DataTable, ForecastMaster>> { Success = false, Status = StatusType.Warning, Message = $"No active items found for {month} {year}." };
                }

                DataTable table = new DataTable();
                table.Columns.Add("C-ASIN", typeof(string));
                table.Columns.Add("Requested Quantity", typeof(int));
                table.Columns.Add("WIP", typeof(int));
                table.Columns.Add("Commitment period", typeof(int));
                table.Columns.Add("PO Date", typeof(DateTime));
                table.Columns.Add("Month", typeof(string));
                table.Columns.Add("Year", typeof(string));

                // ---> ADDED: Required for UI Stats <---
                table.Columns.Add("IsActive", typeof(bool));
                table.Columns.Add("ItemStatus", typeof(string));

                foreach (var d in details)
                {
                    bool isActive = d.ItemCatalogue != null ? d.ItemCatalogue.isActive : true;
                    string itemStatus = d.ItemCatalogue != null ? (d.ItemCatalogue.ItemStatus ?? (isActive ? "Valid" : "Inactive")) : "Missing";

                    table.Rows.Add(
                        d.CASIN ?? (object)DBNull.Value,
                        d.RequestedQuantity,
                        d.Wip ?? (object)DBNull.Value,
                        d.CommitmentPeriod,
                        d.PODate,
                        d.Month ?? (object)DBNull.Value,
                        d.Year ?? (object)DBNull.Value,
                        isActive,
                        itemStatus
                    );
                }

                return new Response<Tuple<DataTable, ForecastMaster>>
                {
                    Success = true,
                    Status = StatusType.Success,
                    Data = new Tuple<DataTable, ForecastMaster>(table, master)
                };
            }
            catch (Exception ex)
            {
                return new Response<Tuple<DataTable, ForecastMaster>>
                {
                    Success = false,
                    Status = StatusType.Error,
                    Message = $"An unexpected error occurred while retrieving forecast data from the database: {ex.Message}"
                                + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                                + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "")
                };
            }
        }
        public Response<bool> MarkForecastMasterAsWIPCalculated(string fileName)
        {
            try
            {
                var forecastMaster = _context.ForecastMasters.FirstOrDefault(f => f.FileName == fileName);

                if (forecastMaster == null)
                {
                    return new Response<bool> { Success = false, Message = $"Forecast master not found for: {fileName}" };
                }

                forecastMaster.IsWipCalculated = true;
                _context.SaveChanges();

                return new Response<bool> { Success = true, Message = "Marked as WIP calculated.", Data = true };
            }
            catch (Exception ex)
            {
                return new Response<bool>
                {
                    Success = false,
                    Message = $"An unexpected error occurred while marking the forecast master as WIP calculated: {ex.Message}"
                              + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                              + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "")
                };
            }
        }

        public ForecastMaster GetForecastMasterByFile(string fileName, string month, string year)
        {
            return _context.ForecastMasters
                .Include(m => m.Details)
                .FirstOrDefault(m => m.FileName == fileName && m.Month == month && m.Year == year);
        }

        public Response<List<ForecastMaster>> GetAvailableForecastsFromDB()
        {
            try
            {
                var forecasts = _context.ForecastMasters.Include(f => f.Details).ToList();
                return new Response<List<ForecastMaster>> { Success = true, Data = forecasts };
            }
            catch (Exception ex)
            {
                return new Response<List<ForecastMaster>>
                {
                    Success = false,
                    Message = $"An unexpected error occurred while retrieving available forecasts from the database: {ex.Message}"
                              + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                              + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "")
                };
            }
        }

        #endregion
    }
}