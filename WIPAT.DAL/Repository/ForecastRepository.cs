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
                    #region 1. Validation Checks
                    var existingFileRes = IsFileAlreadyImported(forecastData.FileName);
                    if (existingFileRes.Success)
                    {
                        return new Response<string> { Success = false, Message = $"Data with filename '{forecastData.FileName}' already exists. Please use a different file." };
                    }

                    if (IsProjectionAlreadyExists(forecastData.ProjectionMonth, forecastData.ProjectionYear))
                    {
                        return new Response<string>
                        {
                            Success = false,
                            Message = $"Forecast data for {forecastData.ProjectionMonth}/{forecastData.ProjectionYear} already exists."
                        };
                    }

                    if (IsWipAlreadyCalculated(forecastData.ProjectionMonth, forecastData.ProjectionYear))
                    {
                        return new Response<string>
                        {
                            Success = false,
                            Message = $"WIP has already been calculated for {forecastData.ProjectionMonth}/{forecastData.ProjectionYear}. Duplicate calculation is not allowed."
                        };
                    }
                    #endregion

                    #region 2. Save Master Record
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
                    _context.SaveChanges(); // Generates master.Id
                    int masterId = master.Id;
                    #endregion

                    #region 3. Prepare DataTable for Bulk Insert
                    DataTable bulkTable = new DataTable();
                    bulkTable.Columns.Add("CASIN", typeof(string));
                    bulkTable.Columns.Add("RequestedQuantity", typeof(int));
                    bulkTable.Columns.Add("Wip", typeof(int));
                    bulkTable.Columns.Add("CommitmentPeriod", typeof(string));
                    bulkTable.Columns.Add("PODate", typeof(DateTime));
                    bulkTable.Columns.Add("Month", typeof(string));
                    bulkTable.Columns.Add("Year", typeof(string));
                    bulkTable.Columns.Add("POForecastMasterId", typeof(int));
                    bulkTable.Columns.Add("IsSystemGenerated", typeof(bool));

                    foreach (DataRow row in forecastData.FullTable.Rows)
                    {
                        var newRow = bulkTable.NewRow();
                        newRow["CASIN"] = row["C-ASIN"].ToString();
                        newRow["RequestedQuantity"] = int.TryParse(row["Requested Quantity"].ToString(), out int qty) ? qty : 0;
                        newRow["Wip"] = isFirstFile && int.TryParse(row["Wip"].ToString(), out int wipVal) ? wipVal : (object)DBNull.Value;
                        newRow["CommitmentPeriod"] = row["Commitment period"].ToString();
                        newRow["PODate"] = DateTime.TryParse(row["PO date"].ToString(), out DateTime poDate) ? poDate : DateTime.MinValue;
                        newRow["Month"] = row["Month"].ToString();
                        newRow["Year"] = row["Year"].ToString();
                        newRow["POForecastMasterId"] = masterId;

                        if (row.Table.Columns.Contains("IsSystemGenerated") && row["IsSystemGenerated"] != DBNull.Value)
                        {
                            newRow["IsSystemGenerated"] = Convert.ToBoolean(row["IsSystemGenerated"]);
                        }
                        else
                        {
                            newRow["IsSystemGenerated"] = false;
                        }

                        bulkTable.Rows.Add(newRow);
                    }
                    #endregion

                    #region 4. Execute SQL Bulk Copy
                    var sqlConnection = (SqlConnection)_context.Database.Connection;
                    var sqlTransaction = (SqlTransaction)transaction.UnderlyingTransaction;
                    if (sqlConnection.State != ConnectionState.Open) 
                    {
                        sqlConnection.Open(); 
                    }

                    using (var sqlBulkCopy = new SqlBulkCopy(sqlConnection, SqlBulkCopyOptions.Default, sqlTransaction))
                    {
                        sqlBulkCopy.DestinationTableName = "dbo.ForecastDetails";
                        sqlBulkCopy.ColumnMappings.Add("CASIN", "CASIN");
                        sqlBulkCopy.ColumnMappings.Add("RequestedQuantity", "RequestedQuantity");
                        sqlBulkCopy.ColumnMappings.Add("Wip", "Wip");
                        sqlBulkCopy.ColumnMappings.Add("CommitmentPeriod", "CommitmentPeriod");
                        sqlBulkCopy.ColumnMappings.Add("PODate", "PODate");
                        sqlBulkCopy.ColumnMappings.Add("Month", "Month");
                        sqlBulkCopy.ColumnMappings.Add("Year", "Year");
                        sqlBulkCopy.ColumnMappings.Add("POForecastMasterId", "POForecastMasterId");
                        sqlBulkCopy.ColumnMappings.Add("IsSystemGenerated", "IsSystemGenerated");

                        sqlBulkCopy.WriteToServer(bulkTable);
                    }
                    #endregion

                    transaction.Commit();
                    return new Response<string> { Success = true, Message = "Data saved successfully!" };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new Response<string> { Success = false, Message = "Transaction rolled back due to error: " + ex.Message };
                }
            }
        }

        public Response<string> SaveForecastDataToDatabase(ForecastFileData forecastData, bool isFirstFile)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    #region 1. Validation Checks
                    var existingFileRes = IsFileAlreadyImported(forecastData.FileName);
                    if (existingFileRes.Success)
                    {
                        return new Response<string> { Success = false, Message = $"Data with filename '{forecastData.FileName}' already exists. Please use a different file." };
                    }

                    if (IsProjectionAlreadyExists(forecastData.ProjectionMonth, forecastData.ProjectionYear))
                    {
                        return new Response<string>
                        {
                            Success = false,
                            Message = $"Forecast data for {forecastData.ProjectionMonth}/{forecastData.ProjectionYear} already exists."
                        };
                    }

                    if (IsWipAlreadyCalculated(forecastData.ProjectionMonth, forecastData.ProjectionYear))
                    {
                        return new Response<string>
                        {
                            Success = false,
                            Message = $"WIP has already been calculated for {forecastData.ProjectionMonth}/{forecastData.ProjectionYear}. Duplicate calculation is not allowed."
                        };
                    }
                    #endregion

                    #region 2. Save Master Record
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
                    #endregion

                    #region 3. Prepare DataTable for Bulk Insert
                    // Fetch a lookup dictionary to map CASIN to ItemCatalogueId efficiently
                    var catalogueLookup = _context.ItemCatalogues
                        .Select(x => new { x.Id, x.Casin })
                        .ToDictionary(x => x.Casin, x => x.Id);

                    DataTable bulkTable = new DataTable();
                    bulkTable.Columns.Add("ItemCatalogueId", typeof(int)); // Added this
                    bulkTable.Columns.Add("CASIN", typeof(string));
                    bulkTable.Columns.Add("RequestedQuantity", typeof(int));
                    bulkTable.Columns.Add("Wip", typeof(int));
                    bulkTable.Columns.Add("CommitmentPeriod", typeof(string));
                    bulkTable.Columns.Add("PODate", typeof(DateTime));
                    bulkTable.Columns.Add("Month", typeof(string));
                    bulkTable.Columns.Add("Year", typeof(string));
                    bulkTable.Columns.Add("POForecastMasterId", typeof(int));
                    bulkTable.Columns.Add("IsSystemGenerated", typeof(bool));

                    foreach (DataRow row in forecastData.FullTable.Rows)
                    {
                        var casinValue = row["C-ASIN"].ToString();
                        var newRow = bulkTable.NewRow();

                        // Assign the FK by looking it up in the dictionary
                        if (catalogueLookup.TryGetValue(casinValue, out int catId))
                        {
                            newRow["ItemCatalogueId"] = catId;
                        }
                        else
                        {
                            newRow["ItemCatalogueId"] = DBNull.Value;
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
                    #endregion

                    #region 4. Execute SQL Bulk Copy
                    var sqlConnection = (SqlConnection)_context.Database.Connection;
                    var sqlTransaction = (SqlTransaction)transaction.UnderlyingTransaction;
                    if (sqlConnection.State != ConnectionState.Open) sqlConnection.Open();

                    using (var sqlBulkCopy = new SqlBulkCopy(sqlConnection, SqlBulkCopyOptions.Default, sqlTransaction))
                    {
                        sqlBulkCopy.DestinationTableName = "dbo.ForecastDetails";

                        // Add the new mapping here
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

                        sqlBulkCopy.WriteToServer(bulkTable);
                    }
                    #endregion

                    transaction.Commit();
                    return new Response<string> { Success = true, Message = "Data saved successfully!" };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new Response<string> { Success = false, Message = "Transaction rolled back: " + ex.Message };
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
                    Message = $"Exception: {ex.Message}"
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

            #region Input Validation
            if (string.IsNullOrEmpty(fileName)) return new Response<ForecastCheckResult> { Success = false, Message = "FileName Required" };
            if (string.IsNullOrEmpty(month)) return new Response<ForecastCheckResult> { Success = false, Message = "Projection Month Required" };
            if (string.IsNullOrEmpty(year)) return new Response<ForecastCheckResult> { Success = false, Message = "Projection Year Required" };
            #endregion

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

                    result.Success = false; // Indicates "Don't process new file, use DB"
                    result.Data.FileData.FullTable = existingForecast.Data.Item1;
                    result.Data.FileData.Forecast = existingForecast.Data.Item2;
                    result.Message = msg;
                    return result;
                }
                else
                {
                    result.Success = true;
                    result.Message = "No existing forecast found. Ready to create new.";
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Exception: {ex.Message}";
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
                return new Response<bool> { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public Response<Tuple<DataTable, ForecastMaster>> GetForecastDataFromDB(string month, string year)
        {
            try
            {
                // Note: No "using" block here, shared context
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
                return new Response<Tuple<DataTable, ForecastMaster>> { Success = false, Status = StatusType.Error, Message = $"Error: {ex.Message}" };
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
                return new Response<bool> { Success = false, Message = $"Error: {ex.Message}" };
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
                return new Response<List<ForecastMaster>> { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        #endregion
    }
}