using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WIPAT.BLL.Interfaces;
using WIPAT.DAL;
using WIPAT.DAL.Interfaces;
using WIPAT.Entities;
using WIPAT.Entities.Dto;
using WIPAT.Entities.Enum;

namespace WIPAT.BLL.Manager
{
    public class WipManager : IWipManager
    {
        private readonly IWipRepository _wipRepository;
        private readonly IForecastRepository _forecastRepository;
        private readonly IStockRepository _stockRepository;
        private readonly WipSession _session;

        // Constructor Injection
        public WipManager(
            IWipRepository wipRepository,
            IForecastRepository forecastRepository,
            IStockRepository stockRepository,
            WipSession session)
        {
            _wipRepository = wipRepository;
            _forecastRepository = forecastRepository;
            _stockRepository = stockRepository;
            _session = session;
        }

        #region Public Methods
        /// <summary>
        /// Builds the main Work-in-Progress (WIP) data table for a list of ASINs.
        /// It simulates stock, orders, and forecasts month-by-month
        /// and applies final MOQ (Minimum Order Quantity) and CasePack adjustments.
        /// </summary>
        public Response<DataTable> BuildCommonWipDataTable(
         List<string> asinList,
         ForecastMaster previousForecast,
         ForecastMaster currentForecast,
         string targetMonth,
         string wipType,
         List<ItemCatalogue> itemsCatalogueData,
         int? moq,
         bool isCasePackEnabled,
         string capacity = null,
         int? percentage = null)
        {
            var response = new Response<DataTable>();
            var resultTable = new DataTable();
            const int TargetCommitmentPeriod = 3;

            try
            {
                // 1. Setup Output Table
                AddDataTableColumns(resultTable, previousForecast, currentForecast, wipType, isCasePackEnabled, moq);

                // 2. Fetch Data
                using (var context = new WIPATContext())
                {
                    var productionData = FetchProductionData(context, asinList, currentForecast.Month);

                    // 3. Process Each ASIN
                    foreach (var dataItem in productionData)
                    {
                        ProcessSingleAsin(
                            dataItem,
                            resultTable,
                            previousForecast,
                            currentForecast,
                            itemsCatalogueData,
                            wipType,
                            TargetCommitmentPeriod,
                            moq,
                            isCasePackEnabled,
                            capacity,
                            percentage
                        );
                    }
                }

                // 4. Validation
                var countProcessed = resultTable.AsEnumerable()
                    .Count(row => row.Field<string>($"CommitmentPeriod ({currentForecast.Month})") == "3");

                if (asinList.Count == countProcessed)
                {
                    response.Success = true;
                    response.Message = "Data table built successfully.";
                    response.Data = resultTable;
                    response.Status = StatusType.Success;
                }
                else
                {
                    response.Success = false;
                    response.Message = $"Error: Expected {asinList.Count} ASINs but processed {countProcessed}.";
                    response.Data = resultTable; // Return partial data for debugging
                    response.Status = StatusType.Error;
                }

                return response;
            }
            catch (Exception ex)
            {
                return new Response<DataTable>
                {
                    Success = false,
                    Message = $"An error occurred: {ex.Message}",
                    Status = StatusType.Error
                };
            }
        }

        /// <summary>
        /// Retrieves all forecast details for a specific ASIN, month, and year.
        /// If no details are found, it generates a list of "default" details (CP 0-6) with zero values.
        /// </summary>
        /// <returns>A list of <see cref="ForecastDetail"/> objects.</returns>
        public List<ForecastDetail> GetForecastDetails(string casin, string month, string year)
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

                // If no records exist for this item, create a default set of 0-value records.
                if (forecastDetails == null || forecastDetails.Count == 0)
                {
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
                    return defaultforecastDetails;
                }

                return forecastDetails;
            }
        }
        #endregion Public Methods

        #region WIP & Stock Calculation Logic

        /// <summary>
        /// A "router" method that selects the correct WIP calculation strategy based on 'wipType'.
        /// </summary>
        private object CalculateWip(string wipType, int currentCommitmentPeriod, int targetCommitmentPeriod, int qty2, int? remainingLayman, int? previousRemainingStock, int initialStock, ref bool hasStockShortfallOccurred, string capacity,
            List<ForecastDetail> forecast_current_monthData, int? percentage, int previousRemainingLaymanStock, object remainingLaymanValue, int currentStock, out int? outRemainingLayman)
        {
            object wip = DBNull.Value;
            outRemainingLayman = remainingLayman; // Pass through the value

            if (wipType == WipType.Analyst.ToString())
            {
                // "Analyst" type is a router itself, branching by "capacity"
                int? analystWip = CalculateAnalystWip(capacity, forecast_current_monthData, targetCommitmentPeriod, currentStock, qty2, currentCommitmentPeriod, percentage);

                wip = analystWip.HasValue ? (object)analystWip.Value : DBNull.Value;
            }
            // else if (wipType == WipType.Layman.ToString())
            // {
            // ... Logic for Layman would go here ...
            // }

            return wip;
        }

        #endregion

        #region Analyst WIP Strategies

        /// <summary>
        /// Router for the "Analyst" strategy. Selects a sub-method based on the 'capacity' setting.
        /// </summary>
        private int? CalculateAnalystWip(string capacity, List<ForecastDetail> forecast_current_monthData, int targetCommitmentPeriod, int currentStock, int qty2, int currentCommitmentPeriod, int? percentage)
        {
            int? analystWip = null;

            // WIP calculations only apply *at or after* the target commitment period (e.g., P3+).
            if (currentCommitmentPeriod >= targetCommitmentPeriod)
            {
                if (capacity == ProcessingWipType.MonthOfSupply.ToString())
                {
                    analystWip = CalculateHighCapacityWip(forecast_current_monthData, targetCommitmentPeriod, currentStock, qty2, currentCommitmentPeriod);
                }
                else if (capacity == ProcessingWipType.Percentage.ToString())
                {
                    analystWip = CalculateMediumCapacityWip(targetCommitmentPeriod, currentStock, qty2, currentCommitmentPeriod, percentage.Value);
                }
                else if (capacity == ProcessingWipType.System.ToString())
                {
                    analystWip = CalculateLowCapacityWip(forecast_current_monthData, targetCommitmentPeriod, currentStock, qty2, currentCommitmentPeriod);
                }
                else
                {
                    // Default to 0 if capacity type is unknown or not applicable
                    analystWip = 0;
                }
            }
            return analystWip;
        }

        /// <summary>
        /// Analyst "High Capacity" (MonthOfSupply) logic.
        /// Calculates WIP to cover demand for the current and target periods (e.g., P3+P3),
        /// then shifts to current-month demand for P5+.
        /// </summary>
        private int CalculateHighCapacityWip(List<ForecastDetail> forecast_current_monthData, int targetCommitmentPeriod, int initialStock, int qty2, int currentCommitmentPeriod)
        {
            int analystWip = 0;

            // Get demand for the *current* period (which is just qty2) and the *target* period (e.g., P3).
            int qty2CurrentPeriod = qty2;
            int qty2NextToTargetPeriod = forecast_current_monthData.FirstOrDefault(f => f.CommitmentPeriod == targetCommitmentPeriod + 1)?.RequestedQuantity ?? 0;

            // Calculate a shortfall 'value' based on the sum of *current* and *target* period demand.
            int value = (qty2CurrentPeriod + qty2NextToTargetPeriod) - initialStock;

            if (value > 0)
            {
                if (currentCommitmentPeriod == targetCommitmentPeriod)
                {
                    // On Target Month (e.g., P3): WIP = the calculated shortfall 'value'.
                    analystWip = value;
                }
                else if (currentCommitmentPeriod == targetCommitmentPeriod + 1)
                {
                    // On Month P4: WIP = 0.
                    analystWip = 0;
                }
                else
                {
                    // On all other months (P5+): WIP = this month's demand.
                    analystWip = qty2;
                }
            }
            else
            {
                // If stock is sufficient for the 'value'...
                if (currentCommitmentPeriod == targetCommitmentPeriod || currentCommitmentPeriod == targetCommitmentPeriod + 1)
                {
                    // ...WIP = 0 for P3 and P4.
                    analystWip = 0;
                }
                else
                {
                    // ...WIP = this month's demand for P5+.
                    analystWip = qty2;
                }
            }

            return analystWip;
        }

        /// <summary>
        /// Analyst "Medium Capacity" (Percentage) logic.
        /// Calculates WIP to meet the current month's demand plus a percentage buffer,
        /// accounting for the current available stock.
        /// </summary>
        private int CalculateMediumCapacityWip(int targetCommitmentPeriod, int initialStock, int qty2, int currentCommitmentPeriod, int percentage)
        {
            int analystWip = 0;

            // 1. Calculate the total required quantity (this month's demand + percentage buffer).
            double requiredQty = qty2 + ((percentage / 100.0) * qty2);

            // 2. Calculate the shortfall against the current stock.
            double shortfall = requiredQty - initialStock;

            // 3. Round the shortfall to the nearest integer.
            var roundedShortfall = (shortfall - Math.Floor(shortfall) < 0.5) ? Math.Floor(shortfall) : Math.Ceiling(shortfall);
            //var roundedShortfall = Math.Ceiling(shortfall);
            int intShortfall = (int)roundedShortfall;

            if (intShortfall > 0)
            {
                // 4. If there is a shortfall, set it as the WIP.
                analystWip = intShortfall;
            }

            return analystWip;
        }

        /// <summary>
        /// Analyst "Low Capacity" (System) logic.
        /// Calculates WIP month-by-month based on simple shortfall against 'initialStock'.
        /// </summary>
        private int CalculateLowCapacityWip(List<ForecastDetail> forecastData, int targetCommitmentPeriod, int initialStock, int qty2, int currentCommitmentPeriod)
        {
            int analystWip = 0;

            // NOTE: This logic does NOT use 'currentStock' as a running calculation.
            // It uses the 'initialStock' passed in for *every* period's calculation.

            int qty2TargetMonth = forecastData.FirstOrDefault(f => f.CommitmentPeriod == targetCommitmentPeriod)?.RequestedQuantity ?? 0;
            int qty2NextMonth = forecastData.FirstOrDefault(f => f.CommitmentPeriod == (targetCommitmentPeriod + 1))?.RequestedQuantity ?? 0;

            if (currentCommitmentPeriod == targetCommitmentPeriod)
            {
                // Logic for Target Month (P3)
                if (qty2TargetMonth > initialStock)
                {
                    analystWip = qty2TargetMonth - initialStock;
                    // WARNING: 'initialStock' is modified locally, but this change does not persist outside this call, which may be a bug.
                    initialStock = 0;
                }
                else
                {
                    analystWip = 0;
                }
            }
            else
            {
                // Logic for all other months (P4, P5, etc.)
                // WARNING: This logic compares all future periods (P4, P5+) against P4's demand ('qty2NextMonth').
                if (qty2NextMonth > initialStock)
                {
                    analystWip = qty2NextMonth - initialStock;
                }
                else
                {
                    if (initialStock == 0)
                    {
                        analystWip = qty2;
                    }
                    else
                    {
                        analystWip = 0;
                    }
                }
            }

            return analystWip;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Helper to get the 'Wip' value from a forecast's details for a specific ASIN and CommitmentPeriod.
        /// </summary>
        private int? GetWipFromForecast(ForecastMaster forecast, string asin, int CP)
        {
            return forecast.Details
                .Where(d => d.CASIN == asin &&
                             (d.CommitmentPeriod) == CP)
                .Select(d => d.Wip)
                .FirstOrDefault();
        }

        /// <summary>
        /// Defines the column structure for the output DataTable based on the WIP type.
        /// </summary>
        private void AddDataTableColumns(DataTable result, ForecastMaster forecast_last_month, ForecastMaster forecast_current_month, string wipType, bool checkBoxCasePack, int? MOQ)
        {
            // Common Columns
            result.Columns.Add("C-ASIN", typeof(string));
            result.Columns.Add("Month", typeof(string));
            result.Columns.Add("Year", typeof(string));
            result.Columns.Add("PO_Date", typeof(DateTime));
            result.Columns.Add($"Requested_Quantity ({forecast_last_month.Month})", typeof(int));
            result.Columns.Add($"Wip ({forecast_last_month.Month})", typeof(int));
            result.Columns.Add($"Requested_Quantity ({forecast_current_month.Month})", typeof(int));
            result.Columns.Add($"CommitmentPeriod ({forecast_current_month.Month})", typeof(string));
            result.Columns.Add("Actual_Order", typeof(int));
            result.Columns.Add("Initial_Stock", typeof(int));
            result.Columns.Add("Stock", typeof(int)); // old name "Remaining"

            // WIP-Specific Columns
            if (wipType == WipType.LaymanFormula.ToString() || wipType == WipType.Layman.ToString())
            {
                result.Columns.Add("Delta", typeof(int));
                result.Columns.Add("Stock", typeof(string)); //old name "Remaining_Layman"
            }

            // Final WIP Column
            result.Columns.Add($"Review_Wip", typeof(int)); //old name  $"{wipType}({forecast_current_month.Month})"

            // Columns added from the merged adjustment logic
            if (MOQ != null)
            {
                result.Columns.Add("MOQ_Wip", typeof(int));
                result.Columns.Add("MOQ", typeof(int));
            }
            if (checkBoxCasePack)
            {
                result.Columns.Add("CasePack_Wip", typeof(int));
                result.Columns.Add("CasePack", typeof(int));
            }
        }

        /// <summary>
        /// Adds a new row to the WIP simulation <see cref="DataTable"/>.
        /// </summary>
        private void AddRowToDataTable(
       DataTable result, string asin, DateTime pODate, int qty1, object wipOfForecast_last_month,
       int qty2, int commitmentPeriod, int? actualOrderVal, int currentStock, int remainingStock,
       object remainingLaymanValue, object finalWip,
       object moqWip, int? moq, object casePackWip, int? casePack,
       string wipType, ForecastMaster forecast_last_month, ForecastMaster forecast_current_month, int? rawCalculatedWip)
        {
            int delta = qty2 - qty1;

            DataRow newRow = result.NewRow();
            newRow["C-ASIN"] = asin;
            newRow["Month"] = pODate.ToString("MMMM");
            newRow["Year"] = pODate.ToString("yyyy");
            newRow["PO_Date"] = pODate;
            newRow[$"Requested_Quantity ({forecast_last_month.Month})"] = qty1;
            newRow[$"Wip ({forecast_last_month.Month})"] = wipOfForecast_last_month ?? DBNull.Value;
            newRow[$"Requested_Quantity ({forecast_current_month.Month})"] = qty2;
            newRow[$"CommitmentPeriod ({forecast_current_month.Month})"] = commitmentPeriod;
            newRow["Actual_Order"] = actualOrderVal.HasValue ? (object)actualOrderVal.Value : DBNull.Value;
            newRow["Initial_Stock"] = currentStock;
            newRow["Stock"] = remainingStock; // old name "Remaining" 

            if (wipType == WipType.LaymanFormula.ToString() || wipType == WipType.Layman.ToString())
            {
                newRow["Delta"] = delta;
                newRow["Stock"] = remainingLaymanValue;  // old name "Remaining_Layman" 
            }

            //newRow[$"{wipType}({result.Columns[6].ColumnName.Split(' ')[1].Trim('(', ')')})"] = finalWip;
            //newRow[$"Review_Wip"] = finalWip;
            newRow[$"Review_Wip"] = rawCalculatedWip;

            // Add Merged Columns, handling nullables
            if (result.Columns.Contains("MOQ"))
            {
                newRow["MOQ_Wip"] = moqWip;
                newRow["MOQ"] = moq.HasValue ? (object)moq.Value : DBNull.Value;
            }
            if (result.Columns.Contains("CasePack"))
            {
                newRow["CasePack_Wip"] = casePackWip;
                newRow["CasePack"] = casePack.HasValue ? (object)casePack.Value : DBNull.Value;
            }

            result.Rows.Add(newRow);
        }

        private List<SimulationInputData> FetchProductionData(WIPATContext context, List<string> asinList, string currentMonth)
        {
            // Check the session flag to see if we should include inactive items
            bool includeInactive = _session.IsContinueWithInactiveItems;

            var itemCatalogueMap = context.ItemCatalogues
                        // ---> UPDATED: Use Enum <---
                        .Where(i => (i.ItemStatus == (int)CatalogueItemStatus.Active || includeInactive) && asinList.Contains(i.Casin))
                        .ToDictionary(i => i.Casin, i => i.Id);

            var itemIds = itemCatalogueMap.Values.ToList();

            var actualOrderMap = context.ActualOrders
                .Where(a => itemIds.Contains(a.ItemCatalogueId) && a.Month == currentMonth)
                .ToDictionary(a => a.ItemCatalogueId, a => (int?)a.Quantity ?? 0);

            return itemCatalogueMap.Select(kv => new SimulationInputData
            {
                Asin = kv.Key,
                ItemId = kv.Value,
                ActualOrderQty = actualOrderMap.ContainsKey(kv.Value) ? actualOrderMap[kv.Value] : 0,
                InitialStock = _stockRepository.GetInitialStockValue(kv.Value)
            }).ToList();
        }
        #endregion

        #region process single asin
        private void ProcessSingleAsin(
        SimulationInputData data,
        DataTable table,
        ForecastMaster prevForecast,
        ForecastMaster currForecast,
        List<ItemCatalogue> catalogue,
        string wipType,
        int targetPeriod,
        int? moq,
        bool isCasePackEnabled,
        string capacity,
        int? percentage)
        {

            if (data.Asin == "B07QDBTDR9")
            {
                var x = 1;
            }
            // Setup Forecast Data
            var prevDetails = GetForecastDetails(data.Asin, prevForecast.Month, prevForecast.Year);
            var currDetails = GetForecastDetails(data.Asin, currForecast.Month, currForecast.Year);

            // Get CasePack
            var catalogueItem = catalogue?.FirstOrDefault(i => i.Casin == data.Asin);
            int? casePackQty = catalogueItem?.CasePackQty;

            // Get Periods (0 to 6)
            var periods = prevDetails.Select(f => f.CommitmentPeriod)
                .Union(currDetails.Select(f => f.CommitmentPeriod))
                .Prepend(0) // Ensure P1/Period 0 is first
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            // Simulation State
            int currentStock = data.InitialStock;
            int? remainingLayman = 0;
            bool hasStockShortfall = false;

            // We keep track of the previous loop's stock for specific calculations
            int? prevPeriodRemainingStock = null;
            int prevPeriodRemainingLayman = data.InitialStock;

            foreach (int period in periods)
            {
                // A. Determine Demands & Inputs
                int prevForecastQty = 0;
                int currForecastQty = 0;
                DateTime poDate = default;
                int? arrivingWip = null;
                int demand = 0;
                int? actualOrder = null;

                if (period == 0) // P1 Logic
                {
                    int cpLookup = period + 1; // P1 looks at CP 1
                    prevForecastQty = prevDetails.FirstOrDefault(f => f.CommitmentPeriod == cpLookup)?.RequestedQuantity ?? 0;
                    poDate = prevDetails.FirstOrDefault(f => f.CommitmentPeriod == cpLookup)?.PODate ?? default;
                    arrivingWip = GetWipFromForecast(prevForecast, data.Asin, cpLookup) ?? 0;

                    actualOrder = data.ActualOrderQty;
                    demand = actualOrder.Value;
                }
                else // P2+ Logic
                {
                    prevForecastQty = prevDetails.FirstOrDefault(f => f.CommitmentPeriod == period)?.RequestedQuantity ?? 0;
                    currForecastQty = currDetails.FirstOrDefault(f => f.CommitmentPeriod == period)?.RequestedQuantity ?? 0;
                    poDate = currDetails.FirstOrDefault(f => f.CommitmentPeriod == period)?.PODate ?? default;

                    demand = currForecastQty;

                    // Arriving WIP exists only before target period
                    if (period < targetPeriod)
                    {
                        arrivingWip = GetWipFromForecast(prevForecast, data.Asin, period + 1) ?? 0;
                    }
                }

                // B. Calculate New WIP
                int? rawCalculatedWip = CalculateNewWipLogic(
                    period, targetPeriod, currentStock, demand, arrivingWip, wipType,
                    currForecastQty, remainingLayman, prevPeriodRemainingStock, data.InitialStock,
                    ref hasStockShortfall, capacity, currDetails, percentage, prevPeriodRemainingLayman
                );

                // P0/P1 specific override: WIP comes from history
                if (period < targetPeriod) rawCalculatedWip = arrivingWip;

                // C. Apply Adjustments (MOQ / CasePack)
                int? finalWip = ApplyMoqAndCasePack(rawCalculatedWip, moq, isCasePackEnabled, casePackQty, out int? moqWip, out int? casePackWip);

                // D. Calculate Remaining Stock
                int remainingStock = CalculateRemainingStock(period, targetPeriod, currentStock, demand, prevForecastQty, arrivingWip, currForecastQty, wipType);

                // E. Add Row
                AddRowToDataTable(
                    table, data.Asin, poDate, prevForecastQty, arrivingWip, currForecastQty,
                    period, actualOrder, currentStock, remainingStock, remainingLayman,
                    (object)finalWip ?? DBNull.Value,
                    (object)moqWip ?? DBNull.Value,
                    moq,
                    (object)casePackWip ?? DBNull.Value,
                    casePackQty,
                    wipType, prevForecast, currForecast, rawCalculatedWip
                );

                // F. Update State for Next Loop
                currentStock = remainingStock;
                prevPeriodRemainingStock = remainingStock;
                prevPeriodRemainingLayman = remainingLayman ?? 0;
            }
        }
        private int? CalculateNewWipLogic(
        int period, int targetPeriod, int currentStock, int demand, int? arrivingWip, string wipType,
        int currForecastQty, int? remainingLayman, int? prevStock, int initialStock,
        ref bool hasStockShortfall, string capacity, List<ForecastDetail> currDetails, int? percentage, int prevLaymanStock)
        {
            if (period < targetPeriod) return null;

            // Logic: Stock before new WIP
            int stockBeforeNewWip = currentStock;

            // Business Rule: Cap negative stock for specific types
            if ((wipType == WipType.Layman.ToString() || wipType == WipType.Analyst.ToString()) && stockBeforeNewWip < 0)
            {
                stockBeforeNewWip = 0;
            }

            // Call external calculation method
            int? newRemLayman;
            var wipObj = CalculateWip(
                wipType, period, targetPeriod, currForecastQty, remainingLayman,
                prevStock, initialStock, ref hasStockShortfall,
                capacity, currDetails, percentage, prevLaymanStock,
                DBNull.Value, stockBeforeNewWip, out newRemLayman
            );

            return (wipObj == DBNull.Value) ? (int?)null : Convert.ToInt32(wipObj);
        }

        private int? ApplyMoqAndCasePack(int? inputWip, int? moq, bool useCasePack, int? casePackQty, out int? moqWip, out int? casePackWip)
        {
            int? runningWip = inputWip;
            moqWip = null;
            casePackWip = null;

            if (!runningWip.HasValue) return null;

            // 1. MOQ
            if (moq.HasValue)
            {
                moqWip = (moq.Value > runningWip.Value) ? 0 : runningWip.Value;
                runningWip = moqWip;
            }

            // 2. CasePack
            if (useCasePack && casePackQty.HasValue && casePackQty.Value > 0)
            {
                if (runningWip.Value == 0)
                {
                    casePackWip = 0;
                }
                else
                {
                    double ratio = (double)runningWip.Value / casePackQty.Value;
                    // Standard rounding: .5 rounds up
                    //double rounded = (ratio % 1 < 0.5) ? Math.Floor(ratio) : Math.Ceiling(ratio);
                    double rounded = Math.Ceiling(ratio);

                    casePackWip = (int)rounded * casePackQty.Value;
                }
                runningWip = casePackWip;
            }

            return runningWip;
        }

        private int CalculateRemainingStock(int period, int targetPeriod, int currentStock, int demand, int prevForecastQty, int? arrivingWip, int currForecastQty, string wipType)
        {
            int remaining = 0;

            if (period == 0)
            {
                // Period 0 Logic
                int delta = currForecastQty - prevForecastQty; // (qty2 - qty1) from original code logic
                if (arrivingWip == null)
                    remaining = currentStock - demand - delta;
                else
                    remaining = currentStock - demand + arrivingWip.Value;
            }
            else
            {
                // Period > 0 Logic
                if (period < targetPeriod && arrivingWip.HasValue)
                {
                    remaining = currentStock + arrivingWip.Value - demand;
                }
                else
                {
                    // Logic for target period and beyond
                    // Original: remainingStock = currentStock - (thisPeriodDemand - qty1);
                    remaining = currentStock - (demand - prevForecastQty);
                }
            }

            // Business Rule Cap
            if ((wipType == WipType.Layman.ToString() || wipType == WipType.Analyst.ToString()) && remaining < 0)
            {
                remaining = 0;
            }

            return remaining;
        }
        #endregion process single asin




        #region save
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
        public async Task<Response<bool>> SaveWipRecordsAsync(DataTable finalDataTable, string capacity, string wipColName, DataTable stockDataTable, WipSession wipSession)
        {
            // Initialize the single response variable
            var response = new Response<bool>();

            try
            {
                #region 1. Validation & Session Setup
                var session = wipSession.Curr;
                string fileName = session.FileName;
                string wipType = wipSession.WipType;

                var (targetMonthName, targetYear) = ParseTargetMonthAndYear(wipSession.TargetMonth);
                if (targetYear == 0)
                {
                    response.Success = false;
                    response.Status = StatusType.Error;
                    response.Message = "Invalid year format in targetMonth.";
                    response.Data = false;
                    return response;
                }
                #endregion

                #region 2. Parse DataTable & Prepare Memory Objects
                var newDetails = new List<WipDetail>(finalDataTable.Rows.Count);

                // Helper local function to safely parse ints
                int? GetInt(DataRow r, string col)
                {
                    if (finalDataTable.Columns.Contains(col) && r[col] != DBNull.Value && int.TryParse(r[col].ToString(), out int v))
                        return v;
                    return null;
                }

                // Check global flags once
                int? globalMoq = null;
                if (finalDataTable.Rows.Count > 0) globalMoq = GetInt(finalDataTable.Rows[0], "MOQ");

                bool globalIsCasePack = finalDataTable.Columns.Contains("CasePack") && finalDataTable.AsEnumerable().Any(r => r["CasePack"] != DBNull.Value);

                foreach (DataRow row in finalDataTable.Rows)
                {
                    string casin = row["C-Asin"]?.ToString()?.Trim();
                    string cPeriod = row[$"CommitmentPeriod ({session.Month})"]?.ToString()?.Trim();

                    if (string.IsNullOrEmpty(casin) || string.IsNullOrEmpty(cPeriod))
                        continue;

                    // Parse columns
                    int? reviewWip = GetInt(row, "Review_Wip");
                    int? moqWip = GetInt(row, "MOQ_Wip");
                    int? cpWip = GetInt(row, "CasePack_Wip");

                    // C# 7.3 compatible switch
                    int? finalWip = null;
                    switch (wipColName)
                    {
                        case "CasePack_Wip": finalWip = cpWip; break;
                        case "MOQ_Wip": finalWip = moqWip; break;
                        case "Review_Wip": finalWip = reviewWip; break;
                    }

                    var detail = new WipDetail
                    {
                        CASIN = casin,
                        Month = row["Month"]?.ToString(),
                        Year = row["Year"]?.ToString(),
                        Stock = GetInt(row, "Stock"),
                        CommitmentPeriod = cPeriod,
                        WipQuantity = finalWip,
                        SystemWip = finalWip,

                        // Specific Logic columns
                        LaymanFormula = wipType == WipType.LaymanFormula.ToString() ? finalWip : null,
                        Layman = wipType == WipType.Layman.ToString() ? finalWip : null,
                        Analyst = wipType == WipType.Analyst.ToString() ? finalWip : null,

                        // Meta data
                        Review_Wip = reviewWip,
                        MOQ_Wip = moqWip,
                        CasePack_Wip = cpWip,
                        CasePack = GetInt(row, "CasePack"),
                        PODate = DateTime.Now
                    };
                    newDetails.Add(detail);
                }

                // Deduplicate
                var dedupedDetails = newDetails
                    .GroupBy(x => x.CASIN + "|" + x.CommitmentPeriod)
                    .Select(g => g.Last())
                    .ToList();

                var distinctCasins = dedupedDetails.Select(d => d.CASIN).Distinct().ToList();
                var distinctPeriods = dedupedDetails.Select(d => d.CommitmentPeriod).Distinct().ToList();
                #endregion

                #region 3. Database Transaction & Execution
                using (var context = new WIPATContext())
                using (var tx = context.Database.BeginTransaction())
                {
                    context.Configuration.AutoDetectChangesEnabled = false;
                    context.Database.CommandTimeout = 180;

                    try
                    {
                        #region 3a. Update ForecastDetails
                        var forecastMaster = await context.ForecastMasters.AsNoTracking().FirstOrDefaultAsync(fm => fm.FileName == fileName);
                        if (forecastMaster == null) throw new Exception($"POForecastMaster not found for file '{fileName}'");

                        var fdIds = await context.ForecastDetails
                            .Where(fd => fd.POForecastMasterId == forecastMaster.Id
                                      && distinctCasins.Contains(fd.CASIN)
                                      && distinctPeriods.Contains(fd.CommitmentPeriod.ToString()))
                            .Select(fd => new { fd.Id, Key = fd.CASIN + "|" + fd.CommitmentPeriod })
                            .AsNoTracking()
                            .ToListAsync();

                        var incomingMap = dedupedDetails.ToDictionary(k => k.CASIN + "|" + k.CommitmentPeriod);

                        foreach (var fd in fdIds)
                        {
                            if (incomingMap.TryGetValue(fd.Key, out var match))
                            {
                                var stub = new ForecastDetail { Id = fd.Id, Wip = match.WipQuantity };
                                context.ForecastDetails.Attach(stub);
                                context.Entry(stub).Property(x => x.Wip).IsModified = true;
                            }
                        }
                        #endregion

                        #region 3b. Upsert WipMaster
                        var wipMaster = await context.WipMasters
                            .FirstOrDefaultAsync(wm => wm.FileName == fileName && wm.TargetMonth == targetMonthName);

                        if (wipMaster == null)
                        {
                            wipMaster = new WipMaster
                            {
                                FileName = fileName,
                                IssuedMonth = dedupedDetails.First().Month,
                                IssuedYear = dedupedDetails.First().Year,
                                TargetMonth = targetMonthName,
                                TargetYear = targetYear,
                                Type = wipType,
                                WipProcessingType = capacity,
                                MOQ = globalMoq,
                                IsCasePackChecked = globalIsCasePack,
                                CreatedAt = DateTime.Now,
                                CreatedById = _session.LoggedInUser.Id
                            };
                            context.WipMasters.Add(wipMaster);
                            await context.SaveChangesAsync();
                        }
                        else
                        {
                            wipMaster.UpdatedAt = DateTime.Now;
                            wipMaster.UpdatedById = _session.LoggedInUser.Id;
                            wipMaster.WipProcessingType = capacity;
                            wipMaster.MOQ = globalMoq;
                            wipMaster.IsCasePackChecked = globalIsCasePack;
                        }
                        #endregion

                        #region 3c. Upsert WipDetails
                        var existingWipDetails = await context.WipDetails
                            .Where(d => d.WipMaster_Id == wipMaster.Id)
                            .Select(d => new { d.Id, Key = d.CASIN + "|" + d.CommitmentPeriod })
                            .AsNoTracking()
                            .ToDictionaryAsync(k => k.Key, v => v.Id);

                        var detailsToInsert = new List<WipDetail>();

                        foreach (var item in dedupedDetails)
                        {
                            string key = item.CASIN + "|" + item.CommitmentPeriod;
                            item.WipMaster_Id = wipMaster.Id;

                            if (existingWipDetails.TryGetValue(key, out int existingId))
                            {
                                item.Id = existingId;
                                context.WipDetails.Attach(item);
                                context.Entry(item).State = EntityState.Modified;
                            }
                            else
                            {
                                detailsToInsert.Add(item);
                            }
                        }

                        if (detailsToInsert.Any())
                        {
                            context.WipDetails.AddRange(detailsToInsert);
                        }
                        #endregion

                        #region 3d. Finalize & Stock Update
                        var masterStub = new ForecastMaster { Id = forecastMaster.Id, IsWipCalculated = true };
                        context.ForecastMasters.Attach(masterStub);
                        context.Entry(masterStub).Property(x => x.IsWipCalculated).IsModified = true;

                        var stockResult = await _stockRepository.UpdateStockQtyInStockTable(stockDataTable, wipColName, session.Month, session.Year);
                        if (!stockResult.Success) throw new Exception(stockResult.Message);

                        await context.SaveChangesAsync();
                        tx.Commit();
                        #endregion

                        // Success Path
                        response.Success = true;
                        response.Data = true;
                        response.Status = StatusType.Success;
                        response.Message = "WIP saved successfully.";
                    }
                    catch (Exception)
                    {
                        tx.Rollback();
                        throw;
                    }
                    finally
                    {
                        context.Configuration.AutoDetectChangesEnabled = true;
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Data = false;
                response.Status = StatusType.Error;
                response.Message = ex.Message + (ex.InnerException != null ? " Inner: " + ex.InnerException.Message : "");
            }

            return response;
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
        #endregion save
    }
}
