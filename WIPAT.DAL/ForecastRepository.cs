using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
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
                            if (forecastRepository.IsFileAlreadyImported(forecastData.FileName))
                            {
                                return new Response<string> { Success = false, Message = $"Data with filename '{forecastData.FileName}' already exists. Please use a different file." };
                            }
                            #endregion

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
        public bool IsFileAlreadyImported(string fileName)
        {
            using (var context = new WIPATContext())
            {
                return context.ForecastMasters.Any(m => m.FileName == fileName);
            }
        }
        #endregion save imported files

        public ForecastFileData ForecastFileExists(string fileName) 
        { using (var context = new WIPATContext()) 
            { 
                return context.ForecastMasters.Where(f => f.FileName == fileName).
                    Select(f => new ForecastFileData { FileName = f.FileName, ProjectionMonth = f.Month, ProjectionYear =f.Year})
                    .FirstOrDefault(); 
            } 
        }
        public Response<DataTable> xGetForecastDataInDataTable(string fileName, string month, string year)
        {
            var response = new Response<DataTable>();
            try
            {
                using (var context = new WIPATContext())
                {
                    var forecast = context.ForecastMasters
                        .Include(m => m.Details)
                        .FirstOrDefault(m => m.FileName == fileName && m.Month == month && m.Year == year);

                    if (forecast == null || forecast.Details == null || !forecast.Details.Any())
                    {
                        response.Message = $"No forecast data found for file '{fileName}', month '{month}', and year '{year}'.";
                        response.Status = StatusType.Warning;
                        return response;
                    }

                    // Convert details to DataTable
                    DataTable table = new DataTable();
                    table.Columns.Add("C-ASIN", typeof(string));
                    //table.Columns.Add("Model Number", typeof(string));
                    table.Columns.Add("Requested Quantity", typeof(int));
                    table.Columns.Add("WIP", typeof(int));
                    table.Columns.Add("Commitment period", typeof(string));
                    table.Columns.Add("PO Date", typeof(DateTime));
                    table.Columns.Add("Month", typeof(string));
                    table.Columns.Add("Year", typeof(string));

                    // Iterate over the forecast details
                    foreach (var detail in forecast.Details)
                    {
                        table.Rows.Add(
                            detail.CASIN,
                            //detail.ModelNumber, 
                            detail.RequestedQuantity,
                             detail.Wip ?? null,
                            detail.CommitmentPeriod,
                            detail.PODate,
                            detail.Month,
                            detail.Year
                        );
                    }

                    // Success
                    response.Success = true;
                    response.Message = $"Forecast data for file '{fileName}' retrieved successfully.";
                    response.Data = table;

                    return response;
                }
            }
            catch (Exception ex)
            {
                response.Message = $"Exception occurred: {ex.Message}" + (ex.InnerException != null ? $" | Inner Exception: {ex.InnerException.Message}" : "");
                return response;
            }
        }
        public Response<DataTable> GetForecastDataInDataTable(string fileName, string month, string year)
        {
            var response = new Response<DataTable>();
            try
            {
                using (var context = new WIPATContext())
                {
                    // Query only what you need
                    var masterIds = context.ForecastMasters.AsNoTracking()
                                    .Where(m => m.FileName == fileName && m.Month == month && m.Year == year)
                                    .Select(m => m.Id);

                    var rows = context.ForecastDetails.AsNoTracking()
                        .Where(d => masterIds.Contains(d.POForecastMasterId))   // swap FK name if different
                        .Select(d => new
                        {
                            d.CASIN,
                            d.RequestedQuantity,
                            d.Wip,
                            d.CommitmentPeriod,
                            d.PODate,
                            d.Month,
                            d.Year
                        })
                        .ToList();
                    if (rows == null)
                    {
                        response.Message = $"No forecast data found for file '{fileName}', month '{month}', and year '{year}'.";
                        response.Status = StatusType.Warning;
                        return response;
                    }

                    // Build DataTable
                    DataTable table = new DataTable();
                    table.Columns.Add("C-ASIN", typeof(string));
                    table.Columns.Add("Requested Quantity", typeof(int));
                    table.Columns.Add("WIP", typeof(int));
                    table.Columns.Add("Commitment period", typeof(string));
                    table.Columns.Add("PO Date", typeof(DateTime));
                    table.Columns.Add("Month", typeof(string));
                    table.Columns.Add("Year", typeof(string));

                    foreach (var d in rows)
                    {
                        table.Rows.Add(
                            d.CASIN ?? (object)DBNull.Value,
                            d.RequestedQuantity,
                            d.Wip.HasValue ? (object)d.Wip.Value : DBNull.Value,
                            d.CommitmentPeriod ?? (object)DBNull.Value,
                            d.PODate,
                            d.Month ?? (object)DBNull.Value,
                            d.Year ?? (object)DBNull.Value
                        );
                    }

                    response.Success = true;
                    response.Message = $"Forecast data for file '{fileName}' retrieved successfully.";
                    response.Data = table;
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


        public Response<ForecastMaster> xGetForecastDataInObject(string fileName, string month, string year)
        {
            var response = new Response<ForecastMaster>();
            try
            {
                using (var context = new WIPATContext())
                {
                    var forecast = context.ForecastMasters
                        .Include(m => m.Details)
                        .FirstOrDefault(m => m.FileName == fileName && m.Month == month && m.Year == year);

                    if (forecast == null || forecast.Details == null || !forecast.Details.Any())
                    {
                        response.Success = false;
                        response.Status = StatusType.Warning;
                        response.Message = $"No forecast data found for file '{fileName}', month '{month}', and year '{year}'.";
                        return response;
                    }

                    response.Success = true;
                    response.Status = StatusType.Success;
                    response.Message = $"Forecast object for file '{fileName}' retrieved successfully.";
                    response.Data = forecast;
                    return response;
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Status = StatusType.Error;
                response.Message = $"An error occurred while retrieving the forecast object: {ex.Message}" +
                    (ex.InnerException != null ? $" | Inner Exception: {ex.InnerException.Message}" : "");
                return response;
            }
        }
        public Response<ForecastMaster> GetForecastDataInObject(string fileName, string month, string year)
        {
            var response = new Response<ForecastMaster>();
            try
            {
                using (var context = new WIPATContext())
                {

                    context.Database.CommandTimeout = 60;

                    var master = context.ForecastMasters
                                .AsNoTracking()
                                .Where(m => m.FileName == fileName && m.Month == month && m.Year == year)
                                .Include(m => m.Details)
                                .FirstOrDefault();


                    if (master == null)
                    {
                        response.Success = false;
                        response.Status = StatusType.Warning;
                        response.Message = $"No forecast found for file '{fileName}', month '{month}', and year '{year}'.";
                        return response;
                    }

                    if (master.Details == null || !master.Details.Any())
                    {
                        response.Success = false;
                        response.Status = StatusType.Warning;
                        response.Message = $"No forecast details found for file '{fileName}', month '{month}', and year '{year}'.";
                        return response;
                    }

                    response.Success = true;
                    response.Status = StatusType.Success;
                    response.Message = $"Forecast object for file '{fileName}' retrieved successfully.";
                    response.Data = master;
                    return response;
                }
            }
            catch (Exception ex)
            {
                response.Success = false; response.Status = StatusType.Error; 
                response.Message = $"An error occurred while retrieving the forecast object: {ex.Message}" + 
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


        // using System.Data.Entity;   // already present above

        public ForecastMaster GetForecastMasterByFile(string fileName, string month, string year)
        {
            using (var context = new WIPATContext())
            {
                return context.ForecastMasters
                    .Include(m => m.Details)
                    .FirstOrDefault(m => m.FileName == fileName && m.Month == month && m.Year == year);
            }
        }


    }
}
