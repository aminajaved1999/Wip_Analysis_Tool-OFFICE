using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using WIPAT.DAL;
using WIPAT.Entities;
using WIPAT.Entities.Dto;
using WIPAT.Entities.Enum;
using static System.Data.Entity.Infrastructure.Design.Executor;

namespace WIPAT.BLL
{
    /// <summary>
    /// Manages the business logic for calculating and saving new Work in Progress (WIP)
    /// recommendations. This class orchestrates fetching forecast and stock data,
    /// running a simulation to determine WIP needs based on various strategies
    /// (e.g., Layman, Analyst), and persisting those results to the database.
    /// </summary>
    public class NewWipManager
    {
        #region constructor and properties

        // Repositories for data access
        private readonly WipRepository wipRepository;
        private readonly ForecastRepository forecastRepository;
        private readonly StockRepository stockRepository;

        // Session object to hold user/contextual data
        private readonly WipSession _session;

        public NewWipManager(WipSession session)
        {
            _session = session;
            wipRepository = new WipRepository();
            forecastRepository = new ForecastRepository();
            stockRepository = new StockRepository(session);
        }

        #endregion constructor and properties

        #region CALCULATE
        public Response<DataTable> NEWBuildCommonWipDataTable(List<string> asinList, ForecastMaster forecast1, ForecastMaster forecast2, string targetMonth, string wipType, string capacity = null, int? percentage = null)
        {
            var response = new Response<DataTable>();
            var context = new WIPATContext();
            DataTable result = new DataTable();

            try
            {
                #region Setup DataTable and Parse Target Month
                // Set up the structure of the output DataTable
                AddDataTableColumns(result, forecast1, forecast2, wipType);

                // Parse the target month to DateTime
                DateTime targetMonthDate = DateTime.ParseExact(targetMonth, "MMMM yyyy", CultureInfo.InvariantCulture);
                #endregion

                #region Data Pre-Fetching (Fetching all necessary data in bulk)
                // Fetch Item Catalogues (get all Item IDs for the given ASINs)
                Dictionary<string, int> itemCatalogueMap = context.ItemCatalogues
                    .Where(i => asinList.Contains(i.Casin))
                    .ToDictionary(i => i.Casin, i => i.Id);
                var itemIds = itemCatalogueMap.Values.ToList();

                // Fetch Actual Orders for these items
                Dictionary<int, int> actualOrderMap = context.ActualOrders
                    .Where(a => itemIds.Contains(a.ItemCatalogueId) && a.Month == forecast2.Month)
                    .ToDictionary(a => a.ItemCatalogueId, a => (int?)a.Quantity ?? 0);

                // Fetch Initial Stock for all items
                var initialStockMap = new Dictionary<int, int>();
                foreach (var itemId in itemIds)
                {
                    initialStockMap[itemId] = stockRepository.GetInitialStockValue(itemId);
                }
                #endregion

                #region Prepare Combined Data for ASIN Processing
                // Combine all necessary data into a single structure for easy access during the ASIN processing
                var combinedResult = itemCatalogueMap.Select(kv => new
                {
                    Asin = kv.Key,
                    ItemId = kv.Value,
                    OrderQuantity = actualOrderMap.ContainsKey(kv.Value) ? actualOrderMap[kv.Value] : 0,
                    InitialStock = initialStockMap.ContainsKey(kv.Value) ? initialStockMap[kv.Value] : 0,
                    forecast1Details = forecast1.Details,
                    forecast2Details = forecast2.Details,
                }).ToList();
                #endregion

                #region Process Each ASIN and Calculate WIP Data
                foreach (var record in combinedResult)
                {
                    var item = new ItemDetail();
                    item.ItemCatalogueId = record.ItemId;
                    item.Casin = record.Asin;
                    item.InitalStock = initialStockMap.TryGetValue(item.ItemCatalogueId, out int stock) ? stock : 0;
                    item.AcutalOrder = actualOrderMap.TryGetValue(item.ItemCatalogueId, out int order) ? order : 0;
                    item.Forecast1Data = new List<ForecastDetail>();
                    item.Forecast2Data = new List<ForecastDetail>();
                    // Get Data for Last Month
                    item.Forecast1Data = GetForecastDetails(item.Casin, forecast1.Month, forecast1.Year);
                    // Get Data for Current Month
                    item.Forecast2Data = GetForecastDetails(item.Casin, forecast2.Month, forecast2.Year);


                    if (item.Casin == "B06XW578NR")
                    {
                        var x = 1;
                    }

                    if (item.Casin == "B09KP2R32K")
                    {
                        var x = 2;

                    }

                    if (item.Casin == "B08XQWX7C4")
                    {
                        var x = 2;
                    }


                    #region update default podates
                    #region Update Forecast1 PODate for Default Entries
                    IEnumerable<ForecastDetail> defaultOfForecast1 = item.Forecast1Data.Where(d => d.PODate == default);
                    if (defaultOfForecast1.Any())
                    {
                        var singleCasin = forecast1.Details.FirstOrDefault();
                        var validOfForecast1 = forecast1.Details.Where(d => d.PODate != default && d.CASIN == singleCasin.CASIN).ToList();

                        // Assuming both lists have the same length
                        foreach (var (def, valid) in defaultOfForecast1.Zip(validOfForecast1, (def, valid) => (def, valid)))
                        {
                            def.PODate = valid.PODate;
                        }
                    }
                    #endregion

                    #region  add zero 0 wip foreach
                    IEnumerable<ForecastDetail> defaultOfForecast2 = item.Forecast2Data.Where(d => d.PODate == default);
                    if (defaultOfForecast2.Any())
                    {
                        #region add zero 0 wip foreach

                        foreach (var def in defaultOfForecast2)
                        {
                            int year = int.Parse(forecast2.Year);  

                            DateTime adjustedPODate = new DateTime(year, def.PODate.Month, def.PODate.Day);

                            int production = context.ForecastDetails
                                                    .Where(fd => fd.CASIN == item.Casin
                                                                 && fd.CommitmentPeriod ==1
                                                                 && fd.POForecastMasterId == context.ForecastMasters
                                                                     .Where(fm => fm.Month == forecast1.Month && fm.Year == forecast1.Year)
                                                                     .Select(fm => fm.Id)
                                                                     .FirstOrDefault())
                                                    .Select(fd => fd.Wip)
                                                    .FirstOrDefault() ?? 0; // Default to 0 if production is null


                            string monthName = adjustedPODate.ToString("MMMM yyyy");
                            AddRowToDataTable(result,
                                record.Asin, monthName, default, 0, production, 0, def.CommitmentPeriod, item.AcutalOrder, 0,
                                item.InitalStock,
                                0,
                                0,
                                0,
                                wipType);

                        }
                        #endregion add zero 0 wip foreach

                        // <-- FIX ADDED HERE
                        // This skips the main simulation logic for this item
                        // and moves to the next 'record' in the 'combinedResult' loop.
                        continue;
                    }
                    #endregion  add zero 0 wip foreach

                    #endregion update default podates

                    List<DateTime> allPoDates = item.Forecast1Data.Select(f => f.PODate).Union(item.Forecast2Data.Select(f => f.PODate)).ToList();


                    #region ProcessSingleAsin Logic
                    #region variables
                    // --- State variables for this ASIN's simulation ---
                    bool hasStockShortfallOccurred = false; // Flag for Layman logic
                    object wip = DBNull.Value;
                    object remainingLaymanValue = DBNull.Value;
                    int? remainingLayman = 0;

                    // --- Simulation Start ---
                    int currentStock = item.InitalStock;
                    bool isFirstMonth = true;
                    int? previousRemainingStock = null;
                    int previousRemainingLaymanStock = item.InitalStock;
                    #endregion variables

                    // Iterate through each unique PO date from the master list
                    foreach (var podate in allPoDates)
                    {
                        #region Prepare Data for Each Month
                        string monthName = podate.ToString("MMMM yyyy");
                        var monthDate = podate;

                        int commitmentPeriod = GetCommitmentPeriod(forecast2, record.Asin, podate);

                        // Look up the WIP from Forecast 1. This is only used for
                        // months *before* the new targetMonth.
                        int? wipOfForecast1 = null;  // Fetch Wip of forecast1
                        if (monthDate < targetMonthDate)
                        {
                            wipOfForecast1 = GetWipFromForecast(forecast1, item.Casin, monthName);
                            wip = wipOfForecast1; // last month wip
                        }
                        // If WIP Of Forecast1's targetmonth is missing for the target month, immediately return an error
                        if (monthDate == targetMonthDate.AddMonths(-1))
                        {
                            if (!wipOfForecast1.HasValue)
                            {
                                response.Success = false;
                                response.Message = $"WIP value is missing for ASIN: {item.Casin} in the target month ({targetMonthDate.AddMonths(-1).ToString("MMMM yyyy")}). Calculate its Wip first.";
                                return response;
                            }
                        }
                        #endregion Prepare Data for Each Month

                        #region Forecast Calculation

                        // Get the requested quantities from our lookups.
                        int qty1 = item.Forecast1Data.FirstOrDefault(f => f.PODate == podate)?.RequestedQuantity ?? 0;
                        int qty2 = item.Forecast2Data.FirstOrDefault(f => f.PODate == podate)?.RequestedQuantity ?? 0;
                        int delta = qty2 - qty1; // The change between forecasts

                        // The "Actual Order" is only applied to the very first month
                        object actualOrderVal = DBNull.Value;
                        if (isFirstMonth)
                        {
                            actualOrderVal = item.AcutalOrder;
                        }
                        #endregion Forecast Calculation

                        #region Stock Calculation
                        // Delegate remaining stock calculation, which has complex rules
                        int remainingStock = CalculateRemainingStock(
                            monthDate, targetMonthDate, currentStock, qty2, wipOfForecast1,
                            delta, isFirstMonth, item.AcutalOrder, ref isFirstMonth, wipType
                        );
                        #endregion Stock Calculation

                        // --- WIP Calculation ---
                        // We only calculate *new* WIP for months on or after the target month.
                        if (monthDate >= targetMonthDate)
                        {
                            // Delegate to the main WIP "router" method, which will
                            // select the correct strategy (Layman, Analyst, etc.)
                            wip = CalculateWip(
                                wipType, monthDate, targetMonthDate, qty2, remainingLayman,
                                previousRemainingStock, item.InitalStock, ref hasStockShortfallOccurred,
                                capacity, item.Forecast2Data, percentage, previousRemainingLaymanStock,
                                remainingLaymanValue, currentStock, out remainingLayman
                            );

                            // Update the "Remaining Layman" value for the DataTable
                            remainingLaymanValue = remainingLayman.HasValue
                                ? (object)remainingLayman.Value.ToString()
                                : DBNull.Value;
                        }

                        #region Add Data Row to DataTable
                        // Add the results of this month's simulation as a new row
                        AddRowToDataTable(result, record.Asin, monthName, podate, qty1, wipOfForecast1, qty2, commitmentPeriod, actualOrderVal, delta, currentStock, remainingStock, remainingLaymanValue, wip, wipType);
                        #endregion Add Data Row to DataTable

                        #region Update Stocks for Next Month
                        // The remaining stock from this month becomes the
                        // initial stock for the *next* month's loop.
                        currentStock = remainingStock;
                        previousRemainingLaymanStock = remainingLayman ?? 0;
                        previousRemainingStock = remainingStock;
                        #endregion Update Stocks for Next Month
                    }
                    #endregion ProcessSingleAsin Logic
                }
                #endregion

                #region Validate Results and Build Final Response
                // Validation: Check if the number of processed ASINs matches the input list
                var countOfCP3 = result.AsEnumerable().Count(row => row.Field<string>($"CommitmentPeriod ({forecast2.Month})") == "3");
                var countOfAsinList = asinList.Count();

                if (countOfAsinList == countOfCP3)
                {
                    // Success response
                    response.Success = true;
                    response.Message = "Data table built successfully for all ASINs.";
                    response.Data = result;
                    response.Status = StatusType.Success;
                    return response;
                }
                else
                {
                    // Error response
                    response.Success = false;
                    response.Message = $"Error: Expected {countOfAsinList} ASINs but only {countOfCP3} were processed.";
                    response.Data = result;
                    response.Status = StatusType.Error;
                    return response;
                }
                #endregion
            }
            catch (Exception ex)
            {
                #region Handle Exceptions
                // Global error handler
                response.Success = false;
                response.Message = $"An error occurred: {ex.Message}";
                response.Data = null;
                response.Status = StatusType.Error;
                return response;
                #endregion
            }
        }

        public List<ForecastDetail> GetForecastDetails(string casin,string month, string year)
        {
            using (var context = new WIPATContext())
            {
                var forecastDetails = context.ForecastDetails
                    .Join(context.ForecastMasters,
                        d => d.POForecastMasterId,
                        m => m.Id,
                        (d, m) => new { ForecastDetail = d, ForecastMaster = m })
                    .Where(joined => joined.ForecastMaster.Year == year
                                     && joined.ForecastMaster.Month == month
                                     && joined.ForecastDetail.CASIN == casin)
                    .Select(joined => joined.ForecastDetail)
                    .ToList();

                if (forecastDetails == null || forecastDetails.Count == 0)
                {
                    #region default forecast details
                    var defaultforecastDetails = new List<ForecastDetail>();
                    var NoOfCommitmentRecords = 6;
                    for (int i = 0; i <= NoOfCommitmentRecords; i++) 
                    {
                        var defaultDetail = new ForecastDetail
                        {
                            CASIN = casin,
                            CommitmentPeriod = i, 
                            RequestedQuantity = 0,
                            PODate = default
                        };
                        defaultforecastDetails.Add(defaultDetail);
                    }
                    #endregion default forecast details

                    return defaultforecastDetails; 
                }

                return forecastDetails;
            }
        }
      
        private int? GetWipFromForecast(ForecastMaster forecast, string asin, string monthName)
        {
            // Now, query the WipDetails related to that WipMaster
            return forecast.Details
                .Where(d => d.CASIN == asin &&
                            (d.Month.ToString() + " " + d.Year.ToString()) == monthName)
                .Select(d => d.Wip)
                .FirstOrDefault();
        }
        #region helpers to calculate WIP DataTable

        /// <summary>
        /// Defines the column structure for the output DataTable based on the WIP type.
        /// </summary>
        /// <param name="result">The DataTable to add columns to.</param>
        /// <param name="forecast1">The first forecast, used for column naming.</param>
        /// <param name="forecast2">The second forecast, used for column naming.</param>
        /// <param name="wipType">The type of WIP calculation.</param>
        private void AddDataTableColumns(DataTable result, ForecastMaster forecast1, ForecastMaster forecast2, string wipType)
        {
            if (wipType == WipType.LaymanFormula.ToString() || wipType == WipType.Layman.ToString())
            {
                // Layman calculations require a "Remaining_Layman" column
                result.Columns.Add("C-ASIN", typeof(string));
                result.Columns.Add("Month", typeof(string));
                result.Columns.Add("PODate", typeof(DateTime));
                result.Columns.Add($"Requested_Quantity ({forecast1.Month})", typeof(int));
                result.Columns.Add($"Wip ({forecast1.Month})", typeof(int));
                result.Columns.Add($"Requested_Quantity ({forecast2.Month})", typeof(int));
                result.Columns.Add($"CommitmentPeriod ({forecast2.Month})", typeof(int));
                result.Columns.Add("Actual_Order", typeof(int));
                result.Columns.Add("Delta", typeof(int));
                result.Columns.Add("Initial_Stock", typeof(int));
                result.Columns.Add("Remaining", typeof(int));
                result.Columns.Add("Remaining_Layman", typeof(string)); // Specific to Layman
                result.Columns.Add($"{wipType}({forecast2.Month})", typeof(int));
            }
            else if (wipType == WipType.Analyst.ToString())
            {
                // Analyst does not need "Delta" or "Remaining_Layman"
                result.Columns.Add("C-ASIN", typeof(string));
                result.Columns.Add("Month", typeof(string));
                result.Columns.Add("PODate", typeof(DateTime));
                result.Columns.Add($"Requested_Quantity ({forecast1.Month})", typeof(int));
                result.Columns.Add($"Wip ({forecast1.Month})", typeof(int));
                result.Columns.Add($"Requested_Quantity ({forecast2.Month})", typeof(int));
                result.Columns.Add($"CommitmentPeriod ({forecast2.Month})", typeof(string));
                result.Columns.Add("Actual_Order", typeof(int));
                result.Columns.Add("Initial_Stock", typeof(int));
                result.Columns.Add("Remaining", typeof(int));
                result.Columns.Add($"{wipType}({forecast2.Month})", typeof(int));
            }
        }


        /// <summary>
        /// Gets the CommitmentPeriod string (e.g., "P1") for a specific ASIN and date.
        /// </summary>
        /// <param name="forecast">The forecast master object to query.</param>
        /// <param name="asin">The ASIN to filter by.</param>
        /// <param name="podate">The PO Date to filter by.</param>
        /// <returns>The CommitmentPeriod string, or "0" if not found.</returns>
        private int GetCommitmentPeriod(ForecastMaster forecast, string asin, DateTime podate)
        {
            var CP = forecast.Details.FirstOrDefault(d => d.CASIN == asin && d.PODate == podate)?.CommitmentPeriod;
            if (CP == null)
            {
                CP = 0; // Default to "0" if null or empty
            }
            return CP.Value;
        }

        /// <summary>
        /// Calculates the *physical* remaining stock for a given month.
        /// This calculation is complex and depends on the simulation's state.
        /// </summary>
        /// <param name="monthDate">The date of the current simulation month.</param>
        /// <param name="targetMonthDate">The start date for calculations.</param>
        /// <param name="currentStock">The starting stock for the *current* month.</param>
        /// <param name="qty2">The requested quantity for the current month.</param>
        /// <param name="wipOfForecast1">The pre-calculated WIP from Forecast 1 (if before target month).</param>
        /// <param name="delta">The change in forecast (qty2 - qty1).</param>
        /// <param name="isFirstMonth">Flag indicating if this is the first month of the simulation.</param>
        /// <param name="actualOrder">The actual order quantity (only used in first month).</param>
        /// <param name="isFirstMonthFlag">A ref flag that is set to false after the first pass.</param>
        /// <param name="wipType">The WIP calculation strategy.</param>
        /// <returns>The calculated physical remaining stock.</returns>
        private int CalculateRemainingStock(DateTime monthDate, DateTime targetMonthDate, int currentStock, int qty2, int? wipOfForecast1, int delta, bool isFirstMonth, int actualOrder, ref bool isFirstMonthFlag, string wipType)
        {
            int remainingStock;

            if (isFirstMonth) // --- Logic for the very first month ---
            {
                if (wipOfForecast1 == null)
                {
                    // If no historical WIP, remaining = stock - order - delta
                    remainingStock = currentStock - actualOrder - delta;
                }
                else
                {
                    // If historical WIP exists, remaining = stock - order + historical_wip
                    remainingStock = currentStock - actualOrder + wipOfForecast1.Value;
                }
                isFirstMonthFlag = false; // Unset the flag for all future loops
            }
            else // --- Logic for all subsequent months ---
            {
                if (monthDate < targetMonthDate && wipOfForecast1.HasValue)
                {
                    // Before target month: remaining = stock + historical_wip - forecast_qty
                    remainingStock = currentStock + wipOfForecast1.Value - qty2;
                 }
                else
                {
                    // On or after target month: remaining = stock - delta
                    // (The 'wip' portion is handled by the main 'CalculateWip' method,
                    // not this 'RemainingStock' calculator)
                    remainingStock = currentStock - delta;
                }
            }

            // Business Rule: For Layman and Analyst types, physical stock
            // cannot be negative. It's capped at 0.
            if (wipType == WipType.Layman.ToString() || wipType == WipType.Analyst.ToString())
            {
                if (remainingStock < 0)
                {
                    remainingStock = 0;
                }
            }

            return remainingStock;
        }

        /// <summary>
        /// A "router" method that selects the correct WIP calculation strategy.
        /// It also calculates and returns the `remainingLayman` via an 'out' parameter.
        /// </summary>
        /// <param name="outRemainingLayman">The calculated remaining layman stock (output).</param>
        /// <returns>The calculated WIP quantity as an object (or DBNull.Value).</returns>
        private object CalculateWip(string wipType, DateTime monthDate, DateTime targetMonthDate, int qty2, int? remainingLayman, int? previousRemainingStock, int initialStock, ref bool hasStockShortfallOccurred, string capacity, 
            List<ForecastDetail> forecast2Data, int? percentage, int previousRemainingLaymanStock, object remainingLaymanValue, int currentStock, out int? outRemainingLayman)
        {
            object wip = DBNull.Value;
            outRemainingLayman = remainingLayman; // Initialize out-parameter

            // --- Strategy Selection ---
            if (wipType == WipType.Analyst.ToString())
            {
                // "Analyst" type is a router itself, branching by "capacity".
                int? analystWip = CalculateAnalystWip2(capacity, forecast2Data, targetMonthDate, currentStock, qty2, monthDate, percentage);

                wip = analystWip.HasValue ? (object)analystWip.Value : DBNull.Value;
            }

            return wip;
        }

        #region Calculate wip helpers

        /// <summary>
        /// Router method for the "Analyst" strategy. Selects a sub-method
        /// based on the `capacity` setting.
        /// </summary>
        /// <returns>The calculated WIP quantity.</returns>
        private int? CalculateAnalystWip2(string capacity, List<ForecastDetail> forecast2Data, DateTime targetMonthDate, int currentStock, int qty2, DateTime monthDate, int? percentage)
        {
            int? analystWip = null;

            // Only calculate for months on or after the target date
            if (monthDate >= targetMonthDate)
            {
                if (capacity == ProcessingWipType.MonthOfSupply.ToString())
                {
                    analystWip = CalculateHighCapacityWip2(forecast2Data, targetMonthDate, currentStock, qty2, monthDate);
                }
                else if (capacity == ProcessingWipType.Percentage.ToString())
                {
                    analystWip = CalculateMediumCapacityWip2(forecast2Data, targetMonthDate, currentStock, qty2, monthDate, percentage.Value);
                }
                else if (capacity == ProcessingWipType.System.ToString())
                {
                    analystWip = CalculateLowCapacityWip2(forecast2Data, targetMonthDate, currentStock, qty2, monthDate);
                }
                else
                {
                    // Default to 0 if capacity type is unknown
                    analystWip = 0;
                }
            }
            return analystWip;
        }

        #region analyst helpers

        /// <summary>
        /// Analyst "High Capacity" (MonthOfSupply) logic.
        /// Covers the needs for the target month AND the next month, all in the target month.
        /// </summary>
        private int CalculateHighCapacityWip2(List<ForecastDetail> forecast2Data, DateTime targetMonthDate, int initialStock, int qty2, DateTime monthDate)
        {
            int analystWip = 0;

            // 1. Get demand for target month (e.g., Nov) and next month (e.g., Dec)
            // For the target month (e.g., November)
            int qty2TargetMonth = forecast2Data.FirstOrDefault(f => f.PODate == targetMonthDate)?.RequestedQuantity ?? 0;

            // For the month after the target (e.g., December)
            int qty2NextToTargetMonth = forecast2Data.FirstOrDefault(f => f.PODate == targetMonthDate.AddMonths(1))?.RequestedQuantity ?? 0;


            // 2. Calculate the total shortfall for *both months*
            int value = (qty2TargetMonth + qty2NextToTargetMonth) - initialStock;

            if (value > 0)
            {
                // --- Shortfall exists ---
                if (monthDate == targetMonthDate)
                {
                    // 3. On Target Month (Nov): WIP = total shortfall for Nov + Dec
                    analystWip = value;
                }
                else if (monthDate == targetMonthDate.AddMonths(1))
                {
                    // 4. On Next Month (Dec): WIP = 0 (it was already covered in Nov)
                    analystWip = 0;
                }
                else
                {
                    // 5. On all other months (Jan+): WIP = that month's quantity
                    analystWip = qty2;
                }
            }
            else
            {
                // --- No shortfall ---
                if (monthDate == targetMonthDate || monthDate == targetMonthDate.AddMonths(1))
                {
                    // 6. Nov/Dec: WIP = 0 (stock is sufficient)
                    analystWip = 0;
                }
                else
                {
                    // 7. Jan+: WIP = that month's quantity
                    analystWip = qty2;
                }
            }

            return analystWip;
        }

        /// <summary>
        /// Analyst "Medium Capacity" (Percentage) logic.
        /// Covers the target month's demand plus a percentage buffer.
        /// </summary>
        private int CalculateMediumCapacityWip2(List<ForecastDetail> forecast2Data, DateTime targetMonthDate, int initialStock, int qty2, DateTime monthDate, int percentage)
        {
            int analystWip = 0;

            // 1. Get demand for the target month
            int qty2TargetMonth = forecast2Data
                                    .Where(f => f.PODate.ToString("MMMM yyyy") == targetMonthDate.ToString("MMMM yyyy"))
                                    .Select(f => f.RequestedQuantity)
                                    .FirstOrDefault();


            // 2. Calculate demand + percentage buffer
            double value = (qty2TargetMonth + ((percentage / 100.0) * qty2TargetMonth)) - initialStock;

            // 3. Round the result (standard rounding)
            var result = (value - Math.Floor(value) < 0.5) ? Math.Floor(value) : Math.Ceiling(value);
            int IntValue = (int)result;

            if (result > 0)
            {
                // 4. If there's a shortfall, assign it as WIP.
                // This logic appears to assign the *same* WIP value to all months,
                // which might be a bug.
                // The commented-out code suggests it should only be for the target month.
                analystWip = IntValue;
            }

            return analystWip;
        }

        /// <summary>
        /// Analyst "Low Capacity" (System) logic.
        /// Calculates WIP month-by-month based on simple shortfall.
        /// </summary>
        private int CalculateLowCapacityWip2(List<ForecastDetail> forecastData, DateTime targetMonthDate, int initialStock, int qty2, DateTime monthDate)
        {
            int analystWip = 0;

            // Note: This function seems to re-use 'initialStock' in a way that
            // implies it's a *running* stock, but it's passed in as a fixed value.
            // This logic may be flawed, as it doesn't use 'currentStock'.
            // The comments below describe what the *code* does, not what it *should* do.

            // 1. Get quantities
            // Find the forecast data for the target month (e.g., November)
            int qty2TargetMonth = forecastData.FirstOrDefault(f => f.PODate == targetMonthDate)?.RequestedQuantity ?? 0;

            // Find the forecast data for the next month (e.g., December)
            int qty2NextMonth = forecastData.FirstOrDefault(f => f.PODate == targetMonthDate.AddMonths(1))?.RequestedQuantity ?? 0;

            // 2. Logic for Target Month
            if (monthDate == targetMonthDate)
            {
                if (qty2TargetMonth > initialStock)
                {
                    // If demand > stock, WIP = the difference
                    analystWip = qty2TargetMonth - initialStock;
                    initialStock = 0; // This state change is lost after the function exits
                }
                else
                {
                    // If demand <= stock, WIP = 0
                    analystWip = 0;
                }
            }
            // 3. Logic for all other months
            else
            {
                if (qty2NextMonth > initialStock) // This uses 'qty2NextMonth' for all other months (Jan, Feb...)
                {
                    analystWip = qty2NextMonth - initialStock;
                }
                else
                {
                    if (initialStock == 0)
                    {
                        analystWip = qty2; // WIP = full request if stock is 0
                    }
                    else
                    {
                        analystWip = 0; // No WIP if stock > 0
                    }
                }
            }

            return analystWip;
        }

        #endregion analyst helpers

        #endregion Calculate wip helpers

        /// <summary>
        /// Adds a single, calculated row to the results DataTable.
        /// </summary>
        private void AddRowToDataTable(DataTable result, string asin, string monthName, DateTime pODate, int qty1, object wipOfForecast1, int qty2, int commitmentPeriod, object actualOrderVal, int delta, int currentStock, int remainingStock, object remainingLaymanValue, object wip, string wipType)
        {
            // Select the correct 'Rows.Add' signature based on the table structure
            if (wipType == WipType.LaymanFormula.ToString() || wipType == WipType.Layman.ToString())
            {
                result.Rows.Add(asin, monthName, pODate, qty1, wipOfForecast1, qty2, commitmentPeriod, actualOrderVal, delta, currentStock, remainingStock, remainingLaymanValue, wip);
            }
            else if (wipType == WipType.Analyst.ToString())
            {
                result.Rows.Add(asin, monthName, pODate, qty1, wipOfForecast1, qty2, commitmentPeriod, actualOrderVal, currentStock, remainingStock, wip);
            }
        }

        #endregion helpers to calculate WIP DataTable
        #endregion CALCULATE


        #region save calculated WIP to DB

        /// <summary>
        /// Saves the calculated WIP data from the DataTable into the database.
        /// This method performs a complete "upsert" (Insert/Update) operation
        /// for WipMaster, WipDetails, and updates related ForecastDetails and Stock.
        /// </summary>
        /// <param name="finalDataTable">The DataTable from the UI (e.g., a grid) containing the *final* WIP values.</param>
        /// <param name="capacity">The capacity setting (e.g., "MonthOfSupply") to save to the master record.</param>
        /// <param name="wipColName">The specific column name (e.g., "Review_Wip") to use as the final WIP quantity.</param>
        /// <param name="stockDataTable">A DataTable of stock values to be updated.</param>
        /// <returns>A Response object indicating success or failure.</returns>
        public async Task<Response<bool>> SaveWipRecordsAsync(DataTable finalDataTable, string capacity, string wipColName, DataTable stockDataTable)
        {
            try
            {
                #region 1. Prep Data
                // Get all necessary context from the user's session
                string fileName = _session.Curr.FileName;
                string wipType = _session.WipType;
                string targetMonth = _session.TargetMonth;
                string currentMonth = _session.Curr.Month;
                string currentYear = _session.Curr.Year;

                var (targetMonthName, targetYear) = ParseTargetMonthAndYear(targetMonth);
                if (targetYear == 0)
                {
                    return new Response<bool> { Success = false, Status = StatusType.Error, Message = "Invalid year format in targetMonth.", Data = false };
                }
                #endregion

                #region 2. Optional Global Flags
                // Read global values *once* from the table instead of in the loop.
                // This is an optimization.
                int? globalMoqQty = null;
                bool globalIsCasePack = false;

                if (finalDataTable.Columns.Contains("MOQ"))
                {
                    var rowWithMoq = finalDataTable.AsEnumerable().FirstOrDefault(r => r["MOQ"] != DBNull.Value);
                    if (rowWithMoq != null && int.TryParse(rowWithMoq["MOQ"]?.ToString()?.Trim(), out var parsedMoq))
                        globalMoqQty = parsedMoq;
                }

                if (finalDataTable.Columns.Contains("CasePack"))
                    globalIsCasePack = finalDataTable.AsEnumerable().Any(r => r["CasePack"] != DBNull.Value);
                #endregion

                #region 3. Process DataTable Rows into WipDetail Entities
                var newDetails = new List<WipDetail>(finalDataTable.Rows.Count);
                foreach (DataRow row in finalDataTable.Rows)
                {
                    // --- Validate Row ---
                    string casin = row["C-Asin"]?.ToString()?.Trim();
                    string month = row["Month"]?.ToString()?.Trim();
                    string year = row["Year"]?.ToString()?.Trim();
                    string stockS = row["Stock"]?.ToString()?.Trim();
                    string cPeriod = row[$"CommitmentPeriod ({_session.Curr.Month})"]?.ToString()?.Trim();

                    if (string.IsNullOrEmpty(month) || string.IsNullOrEmpty(year) ||
                        string.IsNullOrEmpty(casin) || string.IsNullOrEmpty(stockS))
                    {
                        return new Response<bool> { Success = false, Status = StatusType.Error, Message = "Invalid data: One or more required fields are missing.", Data = false };
                    }

                    // --- Parse Quantities ---
                    int? stockQty = int.TryParse(stockS, out var parsedStock) ? (int?)parsedStock : null;
                    int? reviewWipQty = finalDataTable.Columns.Contains("Review_Wip") ? TryParseNullableInt(row["Review_Wip"]) : null;
                    int? moqWipQty = finalDataTable.Columns.Contains("MOQ_Wip") ? TryParseNullableInt(row["MOQ_Wip"]) : null;
                    int? casePackWipQty = finalDataTable.Columns.Contains("CasePack_Wip") ? TryParseNullableInt(row["CasePack_Wip"]) : null;
                    int? casePackQty = finalDataTable.Columns.Contains("CasePack") ? TryParseNullableInt(row["CasePack"]) : null;

                    // --- Determine Final WIP Quantity ---
                    // Select the correct WIP value based on the column name provided
                    int? wipQty = null;
                    switch (wipColName)
                    {
                        case "CasePack_Wip": wipQty = casePackWipQty; break;
                        case "MOQ_Wip": wipQty = moqWipQty; break;
                        case "Review_Wip": wipQty = reviewWipQty; break;
                    }

                    // --- Create WipDetail Entity ---
                    var detail = new WipDetail
                    {
                        CASIN = casin,
                        Month = month,
                        Year = year,
                        WipQuantity = wipQty, // This is the final, chosen WIP
                        SystemWip = wipQty,   // This is also the final, chosen WIP
                        Stock = stockQty,
                        CommitmentPeriod = cPeriod,
                        // Store the *original* calculated value in its respective column
                        LaymanFormula = wipType == WipType.LaymanFormula.ToString() ? wipQty : (int?)null,
                        Layman = wipType == WipType.Layman.ToString() ? wipQty : (int?)null,
                        Analyst = wipType == WipType.Analyst.ToString() ? wipQty : (int?)null,
                        // Store the post-processing step values
                        Review_Wip = reviewWipQty,
                        MOQ_Wip = moqWipQty,
                        CasePack_Wip = casePackWipQty,
                        CasePack = casePackQty,
                        PODate = DateTime.Now // TODO: This should likely be the PODate from the row
                    };
                    newDetails.Add(detail);
                }
                #endregion

                #region 4. Deduplicate by Business Key
                // The DataTable from the UI may have multiple rows for the same
                // ASIN + CommitmentPeriod. We only want to save the *last* one.
                var newByKey = newDetails.GroupBy(d => d.CASIN + "|" + d.CommitmentPeriod)
                                         .ToDictionary(g => g.Key, g => g.Last());
                var dedupedDetails = newByKey.Values.ToList();

                // Get the distinct keys for our database queries
                var casins = dedupedDetails.Select(d => d.CASIN).Distinct().ToList();
                var cps = dedupedDetails.Select(d => d.CommitmentPeriod).Distinct().ToList();
                #endregion

                #region 5. Database Context Operations (Transaction)
                using (var context = new WIPATContext())
                using (var tx = context.Database.BeginTransaction())
                {
                    // --- EF6 Performance Optimizations ---
                    // Set a longer timeout for this potentially large transaction
                    context.Database.CommandTimeout = 180; // 3 minutes
                    // Turn off auto-detection of changes. This is the *most important*
                    // performance gain for bulk operations in EF6.
                    var previousDetect = context.Configuration.AutoDetectChangesEnabled;
                    context.Configuration.AutoDetectChangesEnabled = false;

                    try
                    {
                        #region 5a. Get ForecastMaster
                        var forecastMaster = await context.ForecastMasters.AsNoTracking().FirstOrDefaultAsync(fm => fm.FileName == fileName);

                        if (forecastMaster == null)
                        {
                            tx.Rollback();
                            context.Configuration.AutoDetectChangesEnabled = previousDetect;
                            return new Response<bool> { Success = false, Status = StatusType.Error, Message = $"POForecastMaster not found for file '{fileName}'.", Data = false };
                        }
                        var forecastMasterId = forecastMaster.Id;
                        #endregion

                        #region 5b. Update ForecastDetails.Wip
                        // Get all matching ForecastDetail records from the DB
                        var forecastDetails = await context.ForecastDetails
                            .Where(fd => fd.POForecastMasterId == forecastMasterId
                                         && casins.Contains(fd.CASIN)
                                         && cps.Contains(fd.CommitmentPeriod.ToString()))
                            .Select(fd => new { fd.Id, fd.CASIN, fd.CommitmentPeriod })
                            .AsNoTracking()
                            .ToListAsync();

                        // Loop through the DB records and update them from our in-memory list
                        foreach (var fd in forecastDetails)
                        {
                            var key = fd.CASIN + "|" + fd.CommitmentPeriod;
                            if (newByKey.TryGetValue(key, out var nd))
                            {
                                // --- High-Performance "Stub" Update ---
                                // 1. Create a "stub" entity with only the Primary Key
                                var stub = new ForecastDetail { Id = fd.Id };
                                // 2. Attach it to the context. EF knows it exists.
                                context.ForecastDetails.Attach(stub);
                                // 3. Update *only* the 'Wip' property
                                stub.Wip = nd.WipQuantity;
                                // 4. Tell EF *only* this one property has changed
                                context.Entry(stub).Property(x => x.Wip).IsModified = true;
                            }
                        }
                        #endregion

                        #region 5c. Upsert WipMaster
                        // Find or create the WipMaster record for this calculation
                        var existingWipMaster = await context.WipMasters.FirstOrDefaultAsync(wm => wm.FileName == fileName && wm.TargetMonth == targetMonthName);

                        if (existingWipMaster == null)
                        {
                            // --- Case 1: Create New WipMaster ---
                            existingWipMaster = new WipMaster
                            {
                                FileName = fileName,
                                IssuedMonth = dedupedDetails.First().Month,
                                IssuedYear = dedupedDetails.First().Year,
                                TargetMonth = targetMonthName,
                                TargetYear = targetYear,
                                Type = wipType,
                                WipProcessingType = capacity,
                                MOQ = globalMoqQty,
                                IsCasePackChecked = globalIsCasePack,
                                CreatedAt = DateTime.Now,
                                CreatedById = _session.LoggedInUser.Id,
                            };
                            context.WipMasters.Add(existingWipMaster);

                            // We *must* save here to get the new Master.Id for the Details
                            await context.SaveChangesAsync();

                            // Assign the new FK to all detail records
                            foreach (var nd in dedupedDetails)
                                nd.WipMaster_Id = existingWipMaster.Id;

                            // Add all new details in one bulk operation
                            context.WipDetails.AddRange(dedupedDetails);
                        }
                        else
                        {
                            // --- Case 2: Update Existing WipMaster ---

                            // 2a. Update master properties
                            existingWipMaster.MOQ = globalMoqQty;
                            existingWipMaster.IsCasePackChecked = globalIsCasePack;
                            existingWipMaster.WipProcessingType = capacity;
                            existingWipMaster.Type = wipType;
                            existingWipMaster.IssuedMonth = dedupedDetails.First().Month;
                            existingWipMaster.IssuedYear = dedupedDetails.First().Year;
                            existingWipMaster.TargetMonth = targetMonthName;
                            existingWipMaster.TargetYear = targetYear;
                            existingWipMaster.UpdatedAt = DateTime.Now;
                            existingWipMaster.UpdatedById = _session.LoggedInUser.Id;
                            // Note: We don't need context.Entry(existingWipMaster).State = ...
                            // because we fetched it *with* tracking.

                            // 2b. Get all existing detail keys for comparison
                            var existingDetailKeys = await context.WipDetails
                                .Where(d => d.WipMaster_Id == existingWipMaster.Id
                                             && casins.Contains(d.CASIN)
                                             && cps.Contains(d.CommitmentPeriod))
                                .Select(d => new { d.Id, d.CASIN, d.CommitmentPeriod })
                                .AsNoTracking()
                                .ToListAsync();

                            // 2c. Create a fast lookup map of existing details
                            var existingMap = existingDetailKeys.ToDictionary(
                                k => k.CASIN + "|" + k.CommitmentPeriod,
                                v => v.Id);

                            var toInsert = new List<WipDetail>();
                            // This guard prevents attaching two stubs with the same PK
                            // if the input data had duplicates.
                            var processedExistingIds = new HashSet<int>();

                            // 2d. Loop through *new* data and decide to INSERT or UPDATE
                            foreach (var nd in dedupedDetails)
                            {
                                var k = nd.CASIN + "|" + nd.CommitmentPeriod;
                                if (existingMap.TryGetValue(k, out var existingId))
                                {
                                    // --- UPDATE Existing Detail ---
                                    if (processedExistingIds.Add(existingId))
                                    {
                                        // Use the high-performance stub pattern again
                                        var stub = new WipDetail { Id = existingId };
                                        context.WipDetails.Attach(stub);

                                        // Set all fields to update
                                        stub.WipQuantity = nd.WipQuantity;
                                        stub.SystemWip = nd.WipQuantity;
                                        stub.Stock = nd.Stock;
                                        stub.CommitmentPeriod = nd.CommitmentPeriod;
                                        stub.LaymanFormula = nd.LaymanFormula;
                                        stub.Layman = nd.Layman;
                                        stub.Analyst = nd.Analyst;
                                        stub.Review_Wip = nd.Review_Wip;
                                        stub.MOQ_Wip = nd.MOQ_Wip;
                                        stub.CasePack_Wip = nd.CasePack_Wip;
                                        stub.CasePack = nd.CasePack;
                                        stub.PODate = nd.PODate;

                                        // Mark all properties as modified
                                        var e = context.Entry(stub);
                                        e.Property(x => x.WipQuantity).IsModified = true;
                                        e.Property(x => x.Stock).IsModified = true;
                                        // ... (mark all other properties as modified)
                                        e.Property(x => x.CommitmentPeriod).IsModified = true;
                                        e.Property(x => x.LaymanFormula).IsModified = true;
                                        e.Property(x => x.Layman).IsModified = true;
                                        e.Property(x => x.Analyst).IsModified = true;
                                        e.Property(x => x.Review_Wip).IsModified = true;
                                        e.Property(x => x.MOQ_Wip).IsModified = true;
                                        e.Property(x => x.CasePack_Wip).IsModified = true;
                                        e.Property(x => x.CasePack).IsModified = true;
                                        e.Property(x => x.PODate).IsModified = true;
                                    }
                                }
                                else
                                {
                                    // --- INSERT New Detail ---
                                    nd.WipMaster_Id = existingWipMaster.Id;
                                    toInsert.Add(nd);
                                }
                            }

                            // 2e. Add all new records in bulk
                            if (toInsert.Count > 0)
                                context.WipDetails.AddRange(toInsert);
                        }
                        #endregion

                        #region 5d. Mark ForecastMaster as WIP calculated
                        // Use a stub update to mark the master file as processed
                        var fmStub = new ForecastMaster { Id = forecastMasterId, IsWipCalculated = true };
                        context.ForecastMasters.Attach(fmStub);
                        context.Entry(fmStub).Property(x => x.IsWipCalculated).IsModified = true;
                        #endregion

                        #region 5e. Update Stock Qty In StockTable
                        // This region for updating *production* qty is commented out.
                        //var updateProdQtyRes = await stockRepository.UpdateProductionQtyInStockTable(dataTable, wipColName, currentMonth, currentYear);
                        //if (!updateProdQtyRes.Success)
                        //{
                        //    tx.Rollback(); ...
                        //}

                        // This call updates the *stock* quantity.
                        var updateStockQtyRes = await stockRepository.UpdateStockQtyInStockTable(stockDataTable, wipColName, currentMonth, currentYear);
                        if (!updateStockQtyRes.Success)
                        {
                            // If stock update fails, roll back the *entire* transaction
                            tx.Rollback();
                            context.Configuration.AutoDetectChangesEnabled = previousDetect;
                            return new Response<bool> { Success = false, Data = false, Status = StatusType.Error, Message = updateStockQtyRes.Message };
                        }
                        #endregion

                        // 6. All database operations succeeded.
                        await context.SaveChangesAsync();
                        tx.Commit();
                        context.Configuration.AutoDetectChangesEnabled = previousDetect; // Restore setting

                        // --- Success ---
                        return new Response<bool>
                        {
                            Success = true,
                            Data = true,
                            Status = StatusType.Success,
                            Message = "WIP Master & details saved, and POForecastDetail WIP updated."
                        };
                    }
                    catch (Exception)
                    {
                        // Something failed *inside* the transaction.
                        tx.Rollback(); // Roll back all changes
                        context.Configuration.AutoDetectChangesEnabled = previousDetect; // Restore setting
                        throw; // Re-throw the exception to be caught by the outer try-catch
                    }
                }
                #endregion 
            }
            catch (Exception ex)
            {
                // This is the global catch block for the entire save operation
                return new Response<bool>
                {
                    Success = false,
                    Data = false,
                    Status = StatusType.Error,
                    Message = $"Exception: {ex.Message}"
                     + (ex.InnerException != null ? $" Inner Exception: {ex.InnerException.Message}"
                     + (ex.InnerException.InnerException != null ? $" Inner Inner Exception: {ex.InnerException.InnerException.Message}" : "") : "")
                };
            }
        }

        #region Local helpers

        /// <summary>
        /// Safely parses an object from a DataRow into a nullable integer.
        /// </summary>
        private int? TryParseNullableInt(object o)
        {
            if (o == null || o == DBNull.Value) return null;
            return int.TryParse(o.ToString()?.Trim(), out var v) ? (int?)v : null;
        }

        /// <summary>
        /// Parses a target month string (e.g., "May 2025") into its name and year.
        /// </summary>
        /// <param name="targetMonth">The string to parse.</param>
        /// <returns>A tuple of (string MonthName, int Year). Year is 0 if parse fails.</returns>
        private (string, int) ParseTargetMonthAndYear(string targetMonth)
        {
            string[] parts = targetMonth.Split(' ');
            if (parts.Length != 2)
            {
                return (null, 0); // Invalid format
            }

            string targetMonthName = parts[0]; // e.g., "May"
            string targetYearStr = parts[1];   // e.g., "2025"

            if (int.TryParse(targetYearStr, out int targetYear))
            {
                return (targetMonthName, targetYear);
            }
            else
            {
                return (null, 0); // Invalid year format
            }
        }
        #endregion

        #endregion save calculated WIP to DB
    }

    public class ItemDetail
    {
        public int ItemCatalogueId { get; set; }
        public string Casin { get; set; }
        public int InitalStock { get; set; }
        public int AcutalOrder { get; set; }
        public Dictionary<string, int> forecast1WipMap { get; set; }
        public List<ForecastDetail> Forecast1Data { get; set; }
        
        public List<ForecastDetail> Forecast2Data { get; set; }
    }

}