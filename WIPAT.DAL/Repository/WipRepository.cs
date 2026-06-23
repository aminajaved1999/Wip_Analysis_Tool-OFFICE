using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using WIPAT.DAL.Interfaces;
using WIPAT.Entities;
using WIPAT.Entities.Dto;
using WIPAT.Entities.Enum;

namespace WIPAT.DAL
{
    public class WipRepository : IWipRepository
    {
        private readonly WIPATContext _context;
        private readonly IStockRepository _stockRepository;


        #region Constructor

        public WipRepository(WIPATContext context, IStockRepository stockRepository)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _stockRepository = stockRepository;
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

        public Response<List<WipMaster>> GetAvailableWipPeriods()
        {
            try
            {
                var wips = _context.WipMasters
                .AsNoTracking()
                .OrderByDescending(fm => fm.IssuedYear)
                .ThenByDescending(fm => fm.IssuedMonth)
                .ToList();

                if (wips.Any())
                {
                    return new Response<List<WipMaster>>
                    {
                        Success = true,
                        Message = "WIP periods retrieved successfully.",
                        Data = wips
                    };
                }

                return new Response<List<WipMaster>>
                {
                    Success = true,
                    Message = "No WIP periods available.",
                };

            }
            catch (Exception ex)
            {
                return new Response<List<WipMaster>>
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
                        Message = $"WIP details for {month} {year} loaded successfully. Total records: {details.Count}.",
                        Data = details
                    };
                }

                return new Response<List<WipDetail>>
                {
                    Success = false,
                    Message = $"No WIP details were found for {month} {year}.",
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

        #region Write Operations
        public async Task<Response<bool>> SaveWipRecordsTransactionAsync(
            string fileName, string issuedMonthName, int issuedYear, string targetMonthName, int targetYear,
            string wipType, string capacity, int? globalMoq, bool globalIsCasePack, int loggedInUserId,
            List<WipDetail> dedupedDetails, DataTable stockDataTable, string wipColName, string sessionMonth, string sessionYear)
        {
            using (var tx = _context.Database.BeginTransaction())
            {
                _context.Configuration.AutoDetectChangesEnabled = false;
                _context.Configuration.ValidateOnSaveEnabled = false;
                _context.Database.CommandTimeout = 300;

                try
                {
                    var distinctCasins = dedupedDetails.Select(d => d.CASIN).Distinct().ToList();

                    var itemCatalogueMap = await _context.ItemCatalogues
                        .Where(ic => distinctCasins.Contains(ic.Casin))
                        .Select(ic => new { ic.Casin, ic.Id })
                        .AsNoTracking()
                        .ToDictionaryAsync(k => k.Casin, v => v.Id);

                    var missingCasins = new List<string>();
                    foreach (var detail in dedupedDetails)
                    {
                        if (itemCatalogueMap.TryGetValue(detail.CASIN, out int catalogueId))
                            detail.ItemCatalogueId = catalogueId;
                        else
                            missingCasins.Add(detail.CASIN);
                    }

                    if (missingCasins.Any())
                        throw new Exception($"Failed to map ItemCatalogueId for Casins: {string.Join(", ", missingCasins.Distinct())}");

                    var forecastMaster = await _context.ForecastMasters.AsNoTracking().FirstOrDefaultAsync(fm => fm.FileName == fileName);
                    if (forecastMaster == null) throw new Exception($"POForecastMaster not found for file '{fileName}'");

                    var wipMaster = await _context.WipMasters
                        .FirstOrDefaultAsync(wm => wm.FileName == fileName && wm.TargetMonth == targetMonthName);

                    if (wipMaster == null)
                    {
                        wipMaster = new WipMaster
                        {
                            FileName = fileName,
                            IssuedMonth = issuedMonthName,
                            IssuedYear = issuedYear.ToString(),
                            TargetMonth = targetMonthName,
                            TargetYear = targetYear,
                            Type = wipType,
                            WipProcessingType = capacity,
                            MOQ = globalMoq,
                            IsCasePackChecked = globalIsCasePack,
                            CreatedAt = DateTime.Now,
                            CreatedById = loggedInUserId
                        };
                        _context.WipMasters.Add(wipMaster);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        wipMaster.UpdatedAt = DateTime.Now;
                        wipMaster.UpdatedById = loggedInUserId;
                        wipMaster.IsCasePackChecked = globalIsCasePack;
                        wipMaster.WipProcessingType = capacity;
                        wipMaster.MOQ = globalMoq;
                    }

                    var existingWipDetails = await _context.WipDetails
                        .Where(d => d.WipMaster_Id == wipMaster.Id)
                        .Select(d => new { d.Id, Key = d.CASIN + "|" + d.CommitmentPeriod })
                        .AsNoTracking()
                        .ToDictionaryAsync(k => k.Key, v => v.Id);

                    var detailsToInsert = new List<WipDetail>();
                    int iterationCount = 0;

                    foreach (var item in dedupedDetails)
                    {
                        string key = item.CASIN + "|" + item.CommitmentPeriod;
                        item.WipMaster_Id = wipMaster.Id;

                        if (existingWipDetails.TryGetValue(key, out int existingId))
                        {
                            item.Id = existingId;
                            _context.WipDetails.Attach(item);
                            _context.Entry(item).State = EntityState.Modified;
                            _context.Entry(item).Property(x => x.CreatedAt).IsModified = false;
                            _context.Entry(item).Property(x => x.CreatedById).IsModified = false;
                        }
                        else
                        {
                            detailsToInsert.Add(item);
                        }

                        iterationCount++;
                        if (iterationCount % 1000 == 0)
                        {
                            if (detailsToInsert.Any())
                            {
                                _context.WipDetails.AddRange(detailsToInsert);
                                detailsToInsert.Clear();
                            }
                            await _context.SaveChangesAsync();
                        }
                    }

                    if (detailsToInsert.Any())
                    {
                        _context.WipDetails.AddRange(detailsToInsert);
                    }

                    var masterStub = new ForecastMaster { Id = forecastMaster.Id, IsWipCalculated = true };
                    _context.ForecastMasters.Attach(masterStub);
                    _context.Entry(masterStub).Property(x => x.IsWipCalculated).IsModified = true;

                    // Note: Ensure _stockRepository is injected in WipRepository or its logic is handled here
                    var stockResult = await _stockRepository.UpdateStockQtyInStockTable(stockDataTable, wipColName, sessionMonth, sessionYear);
                    if (!stockResult.Success) throw new Exception(stockResult.Message);

                    await _context.SaveChangesAsync();
                    tx.Commit();

                    return new Response<bool> { Success = true, Data = true, Status = StatusType.Success, Message = "WIP saved successfully." };
                }
                catch (Exception ex)
                {
                    tx.Rollback();

                    // Capture the exception and return it via the Response object instead of throwing
                    return new Response<bool>
                    {
                        Success = false,
                        Data = false,
                        Status = StatusType.Error,
                        Message = $"Transaction failed: {ex.Message}" + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}" : "")
                    };
                }
                finally
                {
                    _context.Configuration.AutoDetectChangesEnabled = true;
                    _context.Configuration.ValidateOnSaveEnabled = true;
                }
            }
        }
        #endregion Write Operations
    }
}