using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using WIPAT.Entities;
using WIPAT.Entities.Dto;

namespace WIPAT.DAL
{
    public class WipRepository
    {

        public Response<bool> CheckIfWipCalculated(string month, string year)
        {
            var response = new Response<bool>();

            try
            {
                using (var context = new WIPATContext())
                {
                    var isWipCalculated = context.ForecastMasters
                        .Any(fm => fm.Month == month && fm.Year == year && fm.IsWipCalculated);

                    if (isWipCalculated)
                    {
                        response.Success = false;
                        response.Message = $"WIP is already calculated for {month} {year}. You cannot recalculate it.";
                        response.Data = true;
                    }
                    else
                    {
                        response.Success = true;
                        response.Message = $"No existing WIP calculation found for {month} {year}.";
                        response.Data = false;
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error checking WIP status: {ex.Message}";
                response.Data = false;
            }

            return response;
        }

        public Response<List<ForecastMaster>> GetForecastsWithCalculatedWip()
        {
            var response = new Response<List<ForecastMaster>>();

            try
            {
                using (var context = new WIPATContext())
                {
                    var forecastsWithCalculatedWip = context.ForecastMasters
                        .Where(fm => fm.IsWipCalculated)
                        .OrderByDescending(fm => fm.Year)
                        .ThenByDescending(fm => fm.Month)
                        .ToList();


                    if (forecastsWithCalculatedWip.Any())
                    {
                        response.Success = true;
                        response.Message = "Successfully retrieved all calculated WIPs.";
                        response.Data = forecastsWithCalculatedWip;
                    }
                    else
                    {
                        response.Success = true;
                        response.Message = "No calculated WIPs found.";
                        response.Data = new List<ForecastMaster>();
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error retrieving calculated WIPs: {ex.Message}";
                response.Data = null;
            }

            return response;
        }

        

        // Add this method to your WipRepository class, for example, after GetForecastsWithCalculatedWip()

        public async Task<Response<List<WipDetail>>> GetWipDetailsByPeriodAsync(string month, string yearString)
        {
            var response = new Response<List<WipDetail>>();

            // Validate and parse the year string to an integer
            if (!int.TryParse(yearString, out int year))
            {
                response.Success = false;
                response.Message = $"Invalid year format: {yearString}.";
                return response;
            }

            try
            {
                using (var context = new WIPATContext())
                {
                    var details = await context.WipDetails
                                           .Include(d => d.Master) // Include WipMaster navigation property
                                            .Where(d => d.Master.IssuedMonth == month.ToString() && d.Master.IssuedYear == year.ToString() && d.CommitmentPeriod == "3")
                                           .ToListAsync();

                    if (details.Any())
                    {
                        response.Success = true;
                        response.Message = $"Successfully retrieved {details.Count} WIP records for {month} {year}.";
                        response.Data = details;
                    }
                    else
                    {
                        response.Success = true; // Not an error, just no data
                        response.Message = $"No WIP records found for {month} {year}.";
                        response.Data = new List<WipDetail>();
                    }
                }
            }
            catch (Exception ex)
            {
                var innerException = ex.InnerException != null ? ex.InnerException.ToString() : "No inner exception.";
                response.Success = false;

                // Add a check for a common EF error if the assumed properties are wrong
                if (ex.Message.Contains("Invalid column name"))
                {
                    response.Message = $"Database schema error: Could not find properties 'issuedMonth' or 'issuedYear' on WipMasters table. Details: {ex.Message}";
                }
                else
                {
                    response.Message = $"Error retrieving WIP details: {ex.Message}. Inner Exception: {innerException}";
                }
                response.Data = null;
            }
            return response;
        }

        public async Task<Response<object>> AddUserWipQtyForPeriodAsync(string month, string year, List<WipDetail> updates)
        {
            var response = new Response<object>();
            #region Input Validation
            if (string.IsNullOrWhiteSpace(month))
            {
                response.Success = false;
                response.Message = "Month is required.";
                return response;
            }

            if (updates == null || !updates.Any())
            {
                response.Success = false;
                response.Message = "No WIP data provided to update.";
                return response;
            }
            #endregion Input Validation

            try
            {
                using (var context = new WIPATContext())
                using (var transaction = context.Database.BeginTransaction())
                {
                    #region Prepare CASIN List

                    // Get CASIN list
                    var casinList = updates.Select(u => u.CASIN.Trim()).Where(c => !string.IsNullOrEmpty(c)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    #endregion

                    #region Fetch Matching Records
                    // Fetch matching records
                    var existingWips = await context.WipDetails
                        .Include(w => w.Master)
                        .Where(w => w.Master.IssuedMonth == month
                                 && w.Master.IssuedYear == year.ToString()
                                 && w.CommitmentPeriod == "3"
                                 && casinList.Contains(w.CASIN))
                        .ToListAsync();
                    #endregion

                    #region Find Missing CASINs
                    // Find missing CASINs
                    var foundCASINs = existingWips.Select(w => w.CASIN).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var missingCASINs = casinList
                        .Where(c => !foundCASINs.Contains(c))
                        .ToList();

                    // ❌ If any CASIN is missing, throw an error and stop processing
                    if (missingCASINs.Any())
                    {
                        transaction.Rollback();

                        response.Success = false;
                        response.Message = $"Error: The following CASINs were not found in the database for {month} {year}: " +
                                           $"{string.Join(", ", missingCASINs.Take(10))}" +
                                           (missingCASINs.Count > 10 ? $" ... (+{missingCASINs.Count - 10} more)" : "");
                        response.Data = new
                        {
                            MissingCount = missingCASINs.Count,
                            MissingCASINs = missingCASINs
                        };

                        return response;
                    }
                    #endregion

                    #region Apply Updates
                    //  All CASINs exist — update UserWipQty
                    int updated = 0;
                    foreach (var update in updates)
                    {
                        var current = existingWips.First(w => w.CASIN.Equals(update.CASIN, StringComparison.OrdinalIgnoreCase));

                        current.UserWipQty = update.UserWipQty;
                        await context.SaveChangesAsync();

                        updated++;
                    }

                    transaction.Commit();
                    #endregion Apply Updates

                    response.Success = true;
                    response.Message = $"Successfully updated {updated} records for {month} {year}.";
                    response.Data = new
                    {
                        UpdatedCount = updated
                    };
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error updating WIP for {month} {year}: {ex.Message}";
                response.Data = null;
            }

            return response;
        }

        public async Task<Response<object>> UpdateWipForPeriodAsync(string month, string year, List<WipDetail> updates)
        {
            var response = new Response<object>();
            #region Input Validation
            if (string.IsNullOrWhiteSpace(month))
            {
                response.Success = false;
                response.Message = "Month is required.";
                return response;
            }

            if (updates == null || !updates.Any())
            {
                response.Success = false;
                response.Message = "No WIP data provided to update.";
                return response;
            }
            #endregion Input Validation

            try
            {
                using (var context = new WIPATContext())
                using (var transaction = context.Database.BeginTransaction())
                {
                    #region Prepare CASIN List

                    // Get CASIN list
                    var casinList = updates.Select(u => u.CASIN.Trim())
                                            .Where(c => !string.IsNullOrEmpty(c))
                                            .Distinct(StringComparer.OrdinalIgnoreCase)
                                            .ToList();

                    #endregion

                    #region Fetch Matching Records
                    // Fetch matching records
                    var existingWips = await context.WipDetails
                        .Include(w => w.Master)
                        .Where(w => w.Master.IssuedMonth == month
                                 && w.Master.IssuedYear == year.ToString()
                                 && w.CommitmentPeriod == "3"
                                 && casinList.Contains(w.CASIN))
                        .ToListAsync();

                    #endregion

                    #region Find Missing CASINs
                    // Find missing CASINs
                    var foundCASINs = existingWips.Select(w => w.CASIN).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var missingCASINs = casinList
                        .Where(c => !foundCASINs.Contains(c))
                        .ToList();

                    // ❌ If any CASIN is missing, throw an error and stop processing
                    if (missingCASINs.Any())
                    {
                        transaction.Rollback();
                        response.Success = false;
                        response.Message = $"Error: The following CASINs were not found in the database for {month} {year}: " +
                                           $"{string.Join(", ", missingCASINs.Take(10))}" +
                                           (missingCASINs.Count > 10 ? $" ... (+{missingCASINs.Count - 10} more)" : "");
                        response.Data = new
                        {
                            MissingCount = missingCASINs.Count,
                            MissingCASINs = missingCASINs
                        };
                        return response;
                    }

                    #endregion

                    #region Apply Updates

                    #region Update WIP Master
                    var wipMaster = await context.WipMasters
                        .Where(w => w.IssuedMonth == month && w.IssuedYear == year.ToString())
                        .FirstOrDefaultAsync();

                    if (wipMaster == null)
                    {
                        transaction.Rollback();
                        response.Success = false;
                        response.Message = $"Error: No WIP Master record found for {month} {year}.";
                        return response;
                    }

                    wipMaster.IsWipModifiedByUser = true;
                    wipMaster.UpdatedAt = DateTime.Now;
                    #endregion

                    #region Update WIP Details
                    int updatedWipCount = 0;

                    foreach (var update in updates)
                    {
                        var current = existingWips.First(w => w.CASIN.Equals(update.CASIN, StringComparison.OrdinalIgnoreCase));
                        current.WipQuantity = update.UserWipQty;
                        updatedWipCount++;
                    }

                    // Save all changes to WIP details at once after the loop
                    if (updatedWipCount > 0)
                    {
                        await context.SaveChangesAsync();
                    }

                    #endregion

                    #region Update Forecast Tables
                    int updatedForecastCount = 0;

                    // Update Forecast Master
                    var forecastMaster = await context.ForecastMasters
                        .Where(f => f.Month == month && f.Year == year.ToString())
                        .FirstOrDefaultAsync();

                    if (forecastMaster == null)
                    {
                        transaction.Rollback();
                        response.Success = false;
                        response.Message = $"Error: No Forecast record found for {month} {year}.";
                        return response;
                    }

                    forecastMaster.IsWipModifiedByUser = true;
                    forecastMaster.UpdatedAt = DateTime.Now;

                    // Update Forecast Details
                    foreach (var update in updates)
                    {
                        var forecastDetail = await context.ForecastDetails
                        .Include(f => f.Master)
                            .Where(f => f.CASIN ==update.CASIN 
                                && f.Master.Month == month && f.Master.Year == year.ToString() 
                                        && f.CommitmentPeriod == 3)
                            .FirstOrDefaultAsync();

                        if (forecastDetail != null)
                        {
                            forecastDetail.Wip = update.UserWipQty;
                            updatedForecastCount++;
                        }
                    }

                    // Save all changes to Forecast details at once after the loop
                    if (updatedForecastCount > 0)
                    {
                        await context.SaveChangesAsync();
                    }

                    #endregion

                    // Commit the transaction
                    transaction.Commit();

                    #endregion Apply Updates

                    // Return success response
                    response.Success = true;
                    response.Message = $"Successfully updated {updatedWipCount}  records for {month} {year}.";
                    response.Data = new
                    {
                        UpdatedWipCount = updatedWipCount,
                        UpdatedForecastCount = updatedForecastCount
                    };
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error updating WIP for {month} {year}: {ex.Message}";
                response.Data = null;
            }

            return response;
        }




    }
}
