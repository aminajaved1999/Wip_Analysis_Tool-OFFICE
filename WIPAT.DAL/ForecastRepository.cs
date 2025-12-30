using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using WIPAT.Entities;
using WIPAT.Entities.Dto;
using WIPAT.Entities.Enum;

namespace WIPAT.DAL
{
    public class ForecastRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["dbContext"].ConnectionString;

        #region save imported files
        public Response<string> SaveForecastDataToDatabase(ForecastFileData forecastData, bool isFirstFile)
        {
            var forecastRepository = new ForecastRepository();

            try
            {
                using (var context = new WIPATContext())
                {
                    // Open the EF database connection explicitly
                    var dbConnection = context.Database.Connection;
                    if (dbConnection.State != ConnectionState.Open)
                        dbConnection.Open();

                    using (var transaction = dbConnection.BeginTransaction())
                    {
                        try
                        {
                            // Assign the transaction to EF context
                            context.Database.UseTransaction(transaction);

                            #region Check for Duplicate File
                            var existingFileRes = forecastRepository.IsFileAlreadyImported(forecastData.FileName);
                            if (existingFileRes.Success)
                            {
                                return new Response<string> { Success = false, Message = $"Data with filename '{forecastData.FileName}' already exists. Please use a different file." };
                            }
                            #endregion

                            #region Check If same ProjectionMonth, ProjectionYear already exist
                            if (forecastRepository.IsProjectionAlreadyExists(forecastData.ProjectionMonth, forecastData.ProjectionYear))
                            {
                                return new Response<string>
                                {
                                    Success = false,
                                    Message = $"Forecast data for {forecastData.ProjectionMonth}/{forecastData.ProjectionYear} already exists."
                                };
                            }
                            #endregion Check If same ProjectionMonth, ProjectionYear already exist

                            #region Check If WIP for ProjectionMonth, ProjectionYear already calculated
                            if (forecastRepository.IsWipAlreadyCalculated(forecastData.ProjectionMonth, forecastData.ProjectionYear))
                            {
                                return new Response<string>
                                {
                                    Success = false,
                                    Message = $"WIP has already been calculated for {forecastData.ProjectionMonth}/{forecastData.ProjectionYear}. Duplicate calculation is not allowed."
                                };
                            }
                            #endregion Check If WIP for ProjectionMonth, ProjectionYear already calculated

                            #region Save Master Record
                            var master = new ForecastMaster
                            {
                                Month = forecastData.ProjectionMonth,
                                Year = forecastData.ProjectionYear,
                                ForecastingFor = forecastData.ForecastFor,
                                FileName = forecastData.FileName,
                                CreatedBy = Environment.UserName,
                                CreatedAt = DateTime.Now
                            };

                            context.ForecastMasters.Add(master);
                            context.SaveChanges();
                            int masterId = master.Id;
                            #endregion

                            #region Prepare DataTable
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

                            #region Bulk Insert with Transaction
                            using (var sqlBulkCopy = new SqlBulkCopy((SqlConnection)dbConnection, SqlBulkCopyOptions.Default, (SqlTransaction)transaction))
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

                            // Commit the transaction if all goes well
                            transaction.Commit();

                            return new Response<string> { Success = true, Message = "Data saved successfully!" };
                        }
                        catch (Exception innerEx)
                        {
                            transaction.Rollback();
                            return new Response<string> { Success = false, Message = "Transaction rolled back due to error: " + innerEx.Message };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new Response<string> { Success = false, Message = "Error initializing database operation: " + ex.Message };
            }
        }

        #region checks
        public Response<ForecastFileData> IsFileAlreadyImported(string fileName)
        {
            var response = new Response<ForecastFileData>();
            try
            {
                using (var context = new WIPATContext())
                {
                    // First, try to get the ForecastMaster by FileName
                    var forecastMaster = context.ForecastMasters.Include(m => m.Details).FirstOrDefault(m => m.FileName == fileName);
                    if (forecastMaster == null)
                    {
                        response.Message = $"No forecast file found with the name '{fileName}'.";
                        response.Status = StatusType.Warning;
                    }
                    else
                    {
                        // return the basic forecast file info
                        var forecastFileData = new ForecastFileData
                        {
                            FileName = forecastMaster.FileName,
                            ProjectionMonth = forecastMaster.Month,
                            ProjectionYear = forecastMaster.Year
                        };

                        response.Message = $"Forecast file '{fileName}' found successfully.";
                        response.Status = StatusType.Success;
                        response.Data = forecastFileData;
                    }

                    return response;
                }
            }
            catch (Exception ex)
            {
                response.Message = $"Exception occurred: {ex.Message}" +
                    (ex.InnerException != null ? $" | Inner Exception: {ex.InnerException.Message}" : "");
                return response;
            }
        }

        public bool IsProjectionAlreadyExists(string month, string year)
        {
            using (var context = new WIPATContext())
            {
                return context.ForecastMasters.Any(fm =>
                fm.Month == month &&
                fm.Year == year);

            }
        }

        public bool IsWipAlreadyCalculated(string month, string year)
        {
            using (var context = new WIPATContext())
            {
                return context.ForecastMasters.Any(fm =>
                fm.Month == month &&
                fm.Year == year &&
                fm.IsWipCalculated == true);
            }

        }

        public Response<ForecastCheckResult> PerformForecastChecks2(string fileName, string month, string year)
        {
            var result = new Response<ForecastCheckResult>();
            result.Data = new ForecastCheckResult();
            result.Data.FileData = new ForecastFileData();
            result.Data.FileData.FullTable = new DataTable();
            result.Data.FileData.Forecast = new ForecastMaster();

            #region input validation
            if (string.IsNullOrEmpty(fileName))
            {
                result.Success = false;
                result.Message = $"FileName Required";
                return result;
            }

            if (string.IsNullOrEmpty(month))
            {
                result.Success = false;
                result.Message = $"Projection Month Required";
                return result;
            }

            if (string.IsNullOrEmpty(year))
            {
                result.Success = false;
                result.Message = $"Projection Year Required";
                return result;
            }
            #endregion input validation

            try
            {
                using (var context = new WIPATContext())
                {
                    var ForecastbyFileName = context.ForecastMasters.FirstOrDefault(fm => fm.FileName == fileName);
                    var ForecastbyFileData = context.ForecastMasters.FirstOrDefault(fm => fm.Month == month && fm.Year == year);
                    if (ForecastbyFileName != null)
                    {
                        result.Data.FileExists = true;
                        if (ForecastbyFileName.IsWipCalculated)
                        {
                            result.Data.IsWipCalculated = true;
                        }
                    }


                    if (ForecastbyFileData != null)
                    {
                        result.Data.ProjectionExists = true;
                        if (ForecastbyFileData.IsWipCalculated)
                        {
                            result.Data.IsWipCalculated = true;
                        }
                    }


                    if (result.Data.ProjectionExists || result.Data.FileExists)
                    {
                        var existingMessage = $"A forecast for {month} {year} already exists";
                        if (result.Data.IsWipCalculated)
                        {
                            existingMessage = $"{existingMessage}, with WIP already calculated.\nYou cannot recalculate the WIP for {month} {year}.";
                        }

                        #region Load existing data
                        var existingForecast = GetForecastDataFromDB(month, year);
                        if (!existingForecast.Success)
                        {
                            result.Success = false;
                            result.Message = $"{existingMessage} However, failed to load the existing forecast data: {existingForecast.Message}.";
                            return result;
                        }

                        result.Success = false;

                        //data
                        result.Data.FileData.FullTable = existingForecast.Data.Item1;
                        result.Data.FileData.Forecast = existingForecast.Data.Item2;

                        //message
                        result.Message = $"{existingMessage} and loaded.";

                        if (result.Data.FileData.IsWipAlreadyCalculated)
                        {
                            result.Message = $"{existingMessage}, You can't calculate its wip";
                        }
                        else
                        {
                            result.Message = $"{existingMessage}";
                        }

                        //return
                        return result;

                        #endregion Load existing data
                    }
                    else
                    {
                        #region when no data found
                        result.Success = true;
                        result.Message = "No existing forecast with calculated WIP found. Ready to create new Wip.";

                        // Set flags explicitly
                        result.Data.FileExists = false;
                        result.Data.ProjectionExists = false;
                        result.Data.IsWipCalculated = false;

                        result.Data.FileData.FullTable = new DataTable();  // empty table
                        result.Data.FileData.Forecast = new ForecastMaster(); // empty forecast

                        return result;
                        #endregion
                    }



                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions and return meaningful message
                result.Data = new ForecastCheckResult();
                result.Success = false;
                result.Message = $"Exception occurred: {ex.Message}" +
                                 (ex.InnerException != null ? $" | Inner Exception: {ex.InnerException.Message}" : "");
            }

            return result;
        }

        #endregion checks


        #endregion save imported files


        public Response<bool> IsWipCalculated(string month, string year)
        {
            var response = new Response<bool>();
            try
            {
                using (var context = new WIPATContext())
                {
                    // Attempt to retrieve the ForecastMaster by file name
                    var forecastMaster = context.ForecastMasters.FirstOrDefault(m => m.Month == month && m.Year == year);
                    if (forecastMaster == null)
                    {
                        response.Success = false;
                        response.Message = $"No forecast file found with the for month '{month} {year}'.";
                        response.Data = false;
                        return response;
                    }

                    if (forecastMaster.IsWipCalculated == true)
                    {
                        response.Success = false;
                        response.Message = $"WIP has already been calculated for {month} {year}. You cannot calculate it again.";
                        response.Data = true;
                        return response;
                    }

                    response.Success = true;
                    response.Message = $"WIP has not been calculated yet for {month} {year}. You may proceed with the calculation.";
                    response.Data = false;
                    return response;
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"An exception occurred: {ex.Message}" +
                                   (ex.InnerException != null ? $" | Inner Exception: {ex.InnerException.Message}" : "");
                response.Data = false;
                return response;
            }
        }

        public Response<Tuple<DataTable, ForecastMaster>> GetForecastDataFromDB(string month, string year)
        {
            var response = new Response<Tuple<DataTable, ForecastMaster>>();

            try
            {
                using (var context = new WIPATContext())
                {
                    context.Database.CommandTimeout = 60;

                    // Get ForecastMaster with Details
                    var master = context.ForecastMasters
                                .AsNoTracking()
                                .Where(m => m.Month == month && m.Year == year)
                                .Include(m => m.Details)
                                .FirstOrDefault();

                    if (master == null)
                    {
                        response.Success = false;
                        response.Status = StatusType.Warning;
                        response.Message = $"No forecast found for '{month} {year}'.";
                        return response;
                    }

                    if (master.Details == null || !master.Details.Any())
                    {
                        response.Success = false;
                        response.Status = StatusType.Warning;
                        response.Message = $"No forecast details found for '{month} {year}'.";
                        return response;
                    }

                    // Order the details by CASIN before adding them to the DataTable
                    var orderedDetails = master.Details.OrderBy(d => d.CASIN).ToList();


                    // Build DataTable
                    DataTable table = new DataTable();
                    table.Columns.Add("C-ASIN", typeof(string));
                    table.Columns.Add("Requested Quantity", typeof(int));
                    table.Columns.Add("WIP", typeof(int));
                    table.Columns.Add("Commitment period", typeof(int));
                    table.Columns.Add("PO Date", typeof(DateTime));
                    table.Columns.Add("Month", typeof(string));
                    table.Columns.Add("Year", typeof(string));

                    //foreach (var d in master.Details)
                    foreach (var d in orderedDetails)
                    {
                        table.Rows.Add(
                            d.CASIN ?? (object)DBNull.Value,
                            d.RequestedQuantity,
                            d.Wip.HasValue ? (object)d.Wip.Value : DBNull.Value,
                            d.CommitmentPeriod,
                            d.PODate,
                            d.Month ?? (object)DBNull.Value,
                            d.Year ?? (object)DBNull.Value
                        );
                    }

                    // Build response
                    response.Success = true;
                    response.Status = StatusType.Success;
                    response.Message = $"Forecast data for '{month} {year}' retrieved successfully.";
                    response.Data = new Tuple<DataTable, ForecastMaster>(table, master);
                    return response;
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Status = StatusType.Error;
                response.Message = $"An exception occurred: {ex.Message}" +
                                   (ex.InnerException != null ? $" | Inner Exception: {ex.InnerException.Message}" : "");
                return response;
            }
        }


        #region wip helper
        public async Task<Response<bool>> NewUpdateWipInPOForecastDetailAsync(string asin, string month, string year, int? wipQty, string fileName, string targetMonth)
        {
            var response = new Response<bool>();

            try
            {
                using (var context = new WIPATContext())
                {
                    // Step 1: Find the ForecastMaster by FileName
                    var forecastMaster = await context.ForecastMasters.FirstOrDefaultAsync(fm => fm.FileName == fileName);

                    if (forecastMaster == null)
                    {
                        response.Success = false;
                        response.Message = $"POForecastMaster not found for file '{fileName}'.";
                        return response;
                    }

                    // Step 2: Split targetMonth (e.g., "November 2025") into Month and Year
                    var targetMonthParts = targetMonth.Split(' ');
                    string targetMonthName = targetMonthParts[0];  // "November"
                    int targetMonthYear = int.Parse(targetMonthParts[1]);  // 2025



                    // Step 3: Find the relevant POForecastDetail record
                    var forecastDetail = await context.ForecastDetails
                        .FirstOrDefaultAsync(fd =>
                            fd.CASIN == asin &&
                            fd.Month == month &&
                            fd.Year == year &&
                            fd.POForecastMasterId == forecastMaster.Id);

                    if (forecastDetail != null)
                    {
                        forecastDetail.Wip = wipQty;
                        await context.SaveChangesAsync();

                        response.Success = true;
                        response.Message = "WIP successfully updated in POForecastDetail.";
                    }
                    else
                    {
                        response.Success = false;
                        response.Message = $"POForecastDetail not found for {asin}, Month: {month}, Year: {year}.";
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Exception while updating WIP in POForecastDetail: {ex.Message}";
            }

            return response;
        }
        public Response<bool> MarkForecastMasterAsWIPCalculated(string fileName)
        {
            var response = new Response<bool>();

            try
            {
                using (var context = new WIPATContext())
                {
                    // Get the forecast master using the provided file name
                    var forecastMaster = context.ForecastMasters.FirstOrDefault(f => f.FileName == fileName);

                    if (forecastMaster == null)
                    {
                        response.Success = false;
                        response.Message = $"Forecast master not found for file: {fileName}";
                        response.Data = false;
                        return response;
                    }

                    // Update the IsWipCalculated flag on the master
                    forecastMaster.IsWipCalculated = true;
                    context.SaveChanges();

                    response.Success = true;
                    response.Message = "WIP status marked successfully on master.";
                    response.Data = true;
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error marking forecast master as WIP: {ex.Message}";
                response.Data = false;
            }

            return response;
        }
        #endregion wip helper

        public ForecastMaster GetForecastMasterByFile(string fileName, string month, string year)
        {
            using (var context = new WIPATContext())
            {
                return context.ForecastMasters
                    .Include(m => m.Details)
                    .FirstOrDefault(m => m.FileName == fileName && m.Month == month && m.Year == year);
            }
        }
        public Response<List<ForecastMaster>> GetAvailableForecastsFromDB()
        {
            var response = new Response<List<ForecastMaster>>();

            try
            {
                using (var context = new WIPATContext())
                {
                    var forecasts = context.ForecastMasters
                          .Include(f => f.Details)
                          .ToList();

                    if (forecasts.Any())
                    {
                        response.Success = true;
                        response.Message = "Successfully retrieved all forecasts.";
                        response.Data = forecasts;
                    }
                    else
                    {
                        response.Success = true;
                        response.Message = "No forecasts found.";
                        response.Data = new List<ForecastMaster>();
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                // This will now show up in your MessageBox if it fails again
                response.Message = $"Error retrieving forecasts: {ex.Message}";
                response.Data = null;
            }

            return response;
        }


    }
}
