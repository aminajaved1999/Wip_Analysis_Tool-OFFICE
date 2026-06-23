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
using WIPAT.Entities.ExcelTemplateDefinitions;
using static System.Data.Entity.Infrastructure.Design.Executor;

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
        public Response<string> SaveForecastDataToDatabase(ForecastFileData forecastData, bool isFirstFile, bool IsContinueWithInactiveItems, int loggedInUserId)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    #region 1. Validation Checks
                    var existingFileRes = IsFileAlreadyImported(forecastData.FileName);
                    if (existingFileRes.Success)
                    {
                        transaction.Commit();
                        return new Response<string> { Success = false, Message = $"Data with filename '{forecastData.FileName}' already exists. Please use a different file." };
                    }

                    if (IsProjectionAlreadyExists(forecastData.ProjectionMonth, forecastData.ProjectionYear))
                    {
                        transaction.Commit();
                        return new Response<string> { Success = false, Message = $"Forecast data for {forecastData.ProjectionMonth}/{forecastData.ProjectionYear} already exists." };
                    }

                    if (IsWipAlreadyCalculated(forecastData.ProjectionMonth, forecastData.ProjectionYear))
                    {
                        transaction.Commit();
                        return new Response<string> { Success = false, Message = $"WIP has already been calculated for {forecastData.ProjectionMonth}/{forecastData.ProjectionYear}. Duplicate calculation is not allowed." };
                    }
                    #endregion 1. Validation Checks

                    #region 2. Save Master Record
                    var master = new ForecastMaster
                    {
                        Month = forecastData.ProjectionMonth,
                        Year = forecastData.ProjectionYear,
                        ForecastingFor = forecastData.ForecastFor,
                        FileName = forecastData.FileName,
                        CreatedById = loggedInUserId,
                        CreatedAt = DateTime.Now,
                        IsContinueWithInactiveItems = IsContinueWithInactiveItems
                    };

                    _context.ForecastMasters.Add(master);
                    _context.SaveChanges();
                    int masterId = master.Id;
                    #endregion 2. Save Master Record

                    #region 3. Build Lookup Data
                    var catalogueLookup = _context.ItemCatalogues
                                    .Select(x => new { x.Id, x.Casin, x.ItemStatus, x.Model })
                                    .ToDictionary(x => x.Casin, x => (x.Id, x.ItemStatus, x.Model));
                    #endregion 3. Build Lookup Data

                    #region 4. Prepare Bulk Insert DataTable
                    var bulkTableResponse = new DataTableFactory().CreateForecastBulkInsertTable(forecastData.ForecastViewTable, masterId, catalogueLookup, loggedInUserId);

                    if (!bulkTableResponse.Success)
                    {
                        transaction.Rollback();
                        return new Response<string> { Success = false, Message = bulkTableResponse.Message };
                    }

                    DataTable bulkTable = bulkTableResponse.Data;
                    #endregion 4. Prepare Bulk Insert DataTable

                    #region 5. set audit columns in Datatable
                    foreach (System.Data.DataRow row in bulkTable.Rows)
                    {
                        row[MasterColumnCatalogue.CreatedById.Name] = loggedInUserId;
                        row[MasterColumnCatalogue.CreatedAt.Name] = DateTime.Now;
                    }
                    #endregion 5. set audit columns in Datatable

                    #region 6. Bulk Insert Forecast Details

                    var sqlConnection = (SqlConnection)_context.Database.Connection;
                    var sqlTransaction = (SqlTransaction)transaction.UnderlyingTransaction;

                    if (sqlConnection.State != ConnectionState.Open) sqlConnection.Open();

                    using (var sqlBulkCopy = new SqlBulkCopy(sqlConnection, SqlBulkCopyOptions.Default, sqlTransaction))
                    {
                        sqlBulkCopy.DestinationTableName = "dbo.ForecastDetails";

                        sqlBulkCopy.ColumnMappings.Add(MasterColumnCatalogue.ItemCatalogueId.Name, "ItemCatalogueId");
                        sqlBulkCopy.ColumnMappings.Add(MasterColumnCatalogue.Casin.Name, "CASIN");
                        sqlBulkCopy.ColumnMappings.Add(MasterColumnCatalogue.Model.Name, "ModelNumber");
                        sqlBulkCopy.ColumnMappings.Add(MasterColumnCatalogue.RequestedQuantity.Name, "RequestedQuantity");
                        sqlBulkCopy.ColumnMappings.Add(MasterColumnCatalogue.CommitmentPeriod.Name, "CommitmentPeriod");
                        sqlBulkCopy.ColumnMappings.Add(MasterColumnCatalogue.PODate.Name, "PODate");
                        sqlBulkCopy.ColumnMappings.Add(MasterColumnCatalogue.MonthString.Name, "Month");
                        sqlBulkCopy.ColumnMappings.Add(MasterColumnCatalogue.Year.Name, "Year");
                        sqlBulkCopy.ColumnMappings.Add(MasterColumnCatalogue.POForecastMasterId.Name, "POForecastMasterId");
                        sqlBulkCopy.ColumnMappings.Add(MasterColumnCatalogue.ItemStatus.Name, "ItemStatus");
                        sqlBulkCopy.ColumnMappings.Add(MasterColumnCatalogue.CreatedById.Name, "CreatedById");
                        sqlBulkCopy.ColumnMappings.Add(MasterColumnCatalogue.CreatedAt.Name, "CreatedAt");

                        sqlBulkCopy.WriteToServer(bulkTable);
                    }
                    #endregion 6. Bulk Insert Forecast Details

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
            try
            {
                return _context.ForecastMasters.Any(fm => fm.Month == month && fm.Year == year);
            }
            catch
            {
                return false;
            }
        }

        public bool IsWipAlreadyCalculated(string month, string year)
        {
            try
            {
                return _context.ForecastMasters.Any(fm => fm.Month == month && fm.Year == year && fm.IsWipCalculated);
            }
            catch
            {
                return false;
            }
        }

        public Response<ForecastCheckResult> PerformForecastChecks2(string fileName, string month, string year)
        {
            var result = new Response<ForecastCheckResult>
            {
                Data = new ForecastCheckResult
                {
                    FileData = new ForecastFileData
                    {
                        ForecastViewTable = new DataTable(),
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

                    result.Success = false;
                    result.Data.FileData.ForecastViewTable = existingForecast.Data.Item1;
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

        public Response<Tuple<DataTable, ForecastMaster>> GetForecastDataFromDB(string month, string year)
        {
            try
            {
                var master = _context.ForecastMasters
                            .AsNoTracking()
                            .Include(m => m.Details.Select(d => d.ItemCatalogue))
                            .FirstOrDefault(m => m.Month == month && m.Year == year);



                //var master = _context.ForecastMasters
                //            .AsNoTracking()
                //            .Include(m => m.Details.Select(d => d.ItemCatalogue))
                //            .FirstOrDefault(m => m.Month == month && m.Year == year);

                if (master == null)
                {
                    return new Response<Tuple<DataTable, ForecastMaster>>
                    {
                        Success = false,
                        Status = StatusType.Warning,
                        Message = $"No forecast found for {month} {year}."
                    };
                }

                //var details = master.Details
                //    .Where(d => d.ItemCatalogue != null &&
                //            (d.ItemCatalogue.ItemStatus == (int)CatalogueItemStatus.Active || master.IsContinueWithInactiveItems))
                //    .OrderBy(d => d.CASIN)
                //    .ToList();

                //if (!details.Any())
                //{
                //    return new Response<Tuple<DataTable, ForecastMaster>>
                //    {
                //        Success = false,
                //        Status = StatusType.Warning,
                //        Message = $"No active items found for {month} {year}."
                //    };
                //}

                var tableResponse = new DataTableFactory().BuildForecastDataTable(master.Details);

                if (!tableResponse.Success)
                {
                    return new Response<Tuple<DataTable, ForecastMaster>>
                    {
                        Success = false,
                        Status = StatusType.Error,
                        Message = tableResponse.Message
                    };
                }

                return new Response<Tuple<DataTable, ForecastMaster>>
                {
                    Success = true,
                    Status = StatusType.Success,
                    Data = new Tuple<DataTable, ForecastMaster>(tableResponse.Data, master)
                };
            }
            catch (Exception ex)
            {
                return new Response<Tuple<DataTable, ForecastMaster>>
                {
                    Success = false,
                    Status = StatusType.Error,
                    Message = $"An unexpected error occurred while retrieving forecast data from the database: {ex.Message}"
                            + (ex.InnerException != null
                                ? $" Inner Exception: {ex.InnerException.Message}"
                                + (ex.InnerException.InnerException != null
                                    ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}"
                                    : "")
                                : "")
                };
            }
        }

        public Response<bool> MarkForecastMasterAsWIPCalculated(string fileName)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var forecastMaster = _context.ForecastMasters.FirstOrDefault(f => f.FileName == fileName);

                    if (forecastMaster == null)
                    {
                        transaction.Commit();
                        return new Response<bool> { Success = false, Message = $"Forecast master not found for: {fileName}" };
                    }

                    forecastMaster.IsWipCalculated = true;
                    _context.SaveChanges();

                    transaction.Commit();
                    return new Response<bool> { Success = true, Message = "Marked as WIP calculated.", Data = true };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new Response<bool>
                    {
                        Success = false,
                        Message = $"An unexpected error occurred while marking the forecast master as WIP calculated: {ex.Message}"
                                  + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                                  + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "")
                    };
                }
            }
        }

        public ForecastMaster GetForecastMasterByFile(string fileName, string month, string year)
        {
            try
            {
                var master = _context.ForecastMasters
                    .Include(m => m.Details)
                    .FirstOrDefault(m => m.FileName == fileName && m.Month == month && m.Year == year);

                return master;
            }
            catch
            {
                return null;
            }
        }

        public Response<List<ForecastMaster>> GetAvailableForecastsFromDB()
        {
            try
            {
                var projected = _context.ForecastMasters
                                        .AsNoTracking()
                                        .Where(f => f.IsWipCalculated == false)
                                        .Select(f => new { f.Month, f.Year })
                                        .Distinct()
                                        .ToList();

                var forecasts = projected.Select(p => new ForecastMaster { Month = p.Month, Year = p.Year }).ToList();

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