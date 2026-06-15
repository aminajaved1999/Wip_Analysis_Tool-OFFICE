using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using WIPAT.DAL.Interfaces;
using WIPAT.Entities;
using WIPAT.Entities.Dto;

namespace WIPAT.DAL
{
    public class WipRepository : IWipRepository
    {
        private readonly WIPATContext _context;

        #region Constructor

        public WipRepository(WIPATContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        #endregion

        #region Read Operations

        public Response<bool> CheckIfWipCalculated(string month, string year)
        {
            try
            {
                var isWipCalculated = _context.ForecastMasters
                    .AsNoTracking()
                    .Any(fm => fm.Month == month && fm.Year == year && fm.IsWipCalculated);

                if (isWipCalculated)
                {
                    return new Response<bool>
                    {
                        Success = false,
                        Message = $"WIP is already calculated for {month} {year}. You cannot recalculate it.",
                        Data = true
                    };
                }

                return new Response<bool>
                {
                    Success = true,
                    Message = $"No existing WIP calculation found for {month} {year}.",
                    Data = false
                };
            }
            catch (Exception ex)
            {
                return new Response<bool>
                {
                    Success = false,
                    Message = $"An unexpected error occurred while checking WIP status: {ex.Message}"
                               + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                               + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : ""),
                    Data = false
                };
            }
        }

        public Response<List<ForecastMaster>> GetForecastsWithCalculatedWip()
        {
            try
            {
                var forecasts = _context.ForecastMasters
                    .AsNoTracking()
                    .Where(fm => fm.IsWipCalculated)
                    .OrderByDescending(fm => fm.Year)
                    .ThenByDescending(fm => fm.Month)
                    .ToList();

                if (forecasts.Any())
                {
                    return new Response<List<ForecastMaster>>
                    {
                        Success = true,
                        Message = "Successfully retrieved calculated WIPs.",
                        Data = forecasts
                    };
                }

                return new Response<List<ForecastMaster>>
                {
                    Success = true,
                    Message = "No calculated WIPs found.",
                    Data = new List<ForecastMaster>()
                };
            }
            catch (Exception ex)
            {
                return new Response<List<ForecastMaster>>
                {
                    Success = false,
                    Message = $"An unexpected error occurred while retrieving calculated WIPs: {ex.Message}"
                               + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                               + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "")
                };
            }
        }

        public async Task<Response<List<WipDetail>>> GetWipDetailsByPeriodAsync(string month, string yearString)
        {
            if (!int.TryParse(yearString, out int year))
            {
                return new Response<List<WipDetail>> { Success = false, Message = $"Invalid year format: {yearString}." };
            }

            try
            {
                var details = await _context.WipDetails
                    .AsNoTracking()
                    .Include(d => d.Master)
                    .Where(d => d.Master.IssuedMonth == month
                             && d.Master.IssuedYear == year.ToString()
                             && d.CommitmentPeriod == "3")
                    .ToListAsync();

                if (details.Any())
                {
                    return new Response<List<WipDetail>>
                    {
                        Success = true,
                        Message = $"Retrieved {details.Count} WIP records.",
                        Data = details
                    };
                }

                return new Response<List<WipDetail>>
                {
                    Success = true,
                    Message = $"No WIP records found for {month} {year}.",
                    Data = new List<WipDetail>()
                };
            }
            catch (Exception ex)
            {
                return new Response<List<WipDetail>>
                {
                    Success = false,
                    Message = $"An unexpected error occurred while retrieving WIP details by period: {ex.Message}"
                               + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                               + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "")
                };
            }
        }

        #endregion

        #region Update Operations

        public async Task<Response<object>> AddUserWipQtyForPeriodAsync(string month, string year, List<WipDetail> updates, int loggedInUserId)
        {
            if (string.IsNullOrWhiteSpace(month)) return new Response<object> { Success = false, Message = "Month is required." };
            if (updates == null || !updates.Any()) return new Response<object> { Success = false, Message = "No updates provided." };

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var casinList = updates.Select(u => u.CASIN.Trim())
                                           .Where(c => !string.IsNullOrEmpty(c))
                                           .Distinct(StringComparer.OrdinalIgnoreCase)
                                           .ToList();

                    var existingWips = await _context.WipDetails
                        .Include(w => w.Master)
                        .Where(w => w.Master.IssuedMonth == month
                                 && w.Master.IssuedYear == year
                                 && w.CommitmentPeriod == "3"
                                 && casinList.Contains(w.CASIN))
                        .ToListAsync();

                    var foundCASINs = existingWips.Select(w => w.CASIN).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var missingCASINs = casinList.Where(c => !foundCASINs.Contains(c)).ToList();

                    if (missingCASINs.Any())
                    {
                        transaction.Rollback();
                        return new Response<object>
                        {
                            Success = false,
                            Message = $"Error: Missing CASINs in DB: {string.Join(", ", missingCASINs.Take(5))}...",
                            Data = new { MissingCount = missingCASINs.Count, MissingCASINs = missingCASINs }
                        };
                    }

                    int updatedCount = 0;
                    foreach (var update in updates)
                    {
                        var current = existingWips.First(w => w.CASIN.Equals(update.CASIN, StringComparison.OrdinalIgnoreCase));
                        current.UserWipQty = update.UserWipQty;
                        current.UpdatedById = loggedInUserId;
                        current.UpdatedAt = DateTime.Now;

                        updatedCount++;
                    }

                    await _context.SaveChangesAsync();
                    transaction.Commit();

                    return new Response<object>
                    {
                        Success = true,
                        Message = $"Updated {updatedCount} records.",
                        Data = new { UpdatedCount = updatedCount }
                    };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new Response<object>
                    {
                        Success = false,
                        Message = $"An unexpected error occurred while adding user WIP quantity: {ex.Message}"
                                   + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                                   + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "")
                    };
                }
            }
        }

        public async Task<Response<object>> UpdateWipForPeriodAsync(string month, string year, List<WipDetail> updates, int loggedInUserId)
        {
            if (string.IsNullOrWhiteSpace(month)) return new Response<object> { Success = false, Message = "Month is required." };
            if (updates == null || !updates.Any()) return new Response<object> { Success = false, Message = "No updates provided." };

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var casinList = updates.Select(u => u.CASIN.Trim())
                                           .Where(c => !string.IsNullOrEmpty(c))
                                           .Distinct(StringComparer.OrdinalIgnoreCase)
                                           .ToList();

                    var existingWips = await _context.WipDetails
                        .Include(w => w.Master)
                        .Where(w => w.Master.IssuedMonth == month
                                 && w.Master.IssuedYear == year
                                 && w.CommitmentPeriod == "3"
                                 && casinList.Contains(w.CASIN))
                        .ToListAsync();

                    var foundCASINs = existingWips.Select(w => w.CASIN).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var missingCASINs = casinList.Where(c => !foundCASINs.Contains(c)).ToList();

                    if (missingCASINs.Any())
                    {
                        transaction.Rollback();
                        return new Response<object> { Success = false, Message = $"Missing CASINs: {string.Join(",", missingCASINs.Take(5))}" };
                    }

                    // Update WIP Master record
                    var wipMaster = await _context.WipMasters
                        .FirstOrDefaultAsync(w => w.IssuedMonth == month && w.IssuedYear == year);

                    if (wipMaster == null)
                    {
                        transaction.Rollback();
                        return new Response<object> { Success = false, Message = $"WIP Master not found for {month} {year}." };
                    }

                    wipMaster.IsWipModifiedByUser = true;
                    wipMaster.UpdatedAt = DateTime.Now;
                    wipMaster.UpdatedById = loggedInUserId;

                    // Update WIP detail quantities
                    int updatedWipCount = 0;
                    foreach (var update in updates)
                    {
                        var current = existingWips.First(w => w.CASIN.Equals(update.CASIN, StringComparison.OrdinalIgnoreCase));
                        current.WipQuantity = update.UserWipQty;
                        updatedWipCount++;
                    }

                    // Update corresponding Forecast tables
                    var forecastMaster = await _context.ForecastMasters
                        .FirstOrDefaultAsync(f => f.Month == month && f.Year == year);

                    if (forecastMaster == null)
                    {
                        transaction.Rollback();
                        return new Response<object> { Success = false, Message = $"Forecast Master not found for {month} {year}." };
                    }

                    forecastMaster.IsWipModifiedByUser = true;
                    forecastMaster.UpdatedAt = DateTime.Now;
                    forecastMaster.UpdatedById = loggedInUserId;

                    int updatedForecastCount = 0;
                    var forecastDetails = await _context.ForecastDetails
                        .Include(f => f.Master)
                        .Where(f => f.Master.Month == month
                                 && f.Master.Year == year
                                 && f.CommitmentPeriod == 3
                                 && casinList.Contains(f.CASIN))
                        .ToListAsync();

                    foreach (var update in updates)
                    {
                        var detail = forecastDetails.FirstOrDefault(f => f.CASIN.Equals(update.CASIN, StringComparison.OrdinalIgnoreCase));
                        if (detail != null)
                        {
                            detail.Wip = update.UserWipQty;
                            detail.UpdatedAt = DateTime.Now;
                            detail.UpdatedById = loggedInUserId;
                            updatedForecastCount++;
                        }
                    }

                    await _context.SaveChangesAsync();
                    transaction.Commit();

                    return new Response<object>
                    {
                        Success = true,
                        Message = $"Successfully updated {updatedWipCount} records.",
                        Data = new { UpdatedWipCount = updatedWipCount, UpdatedForecastCount = updatedForecastCount }
                    };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new Response<object>
                    {
                        Success = false,
                        Message = $"An unexpected error occurred while updating WIP records: {ex.Message}"
                                   + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                                   + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "")
                    };
                }
            }
        }

        #endregion
    }
}