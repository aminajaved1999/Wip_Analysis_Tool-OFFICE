using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public class NewWorkingWipManager : INewWorkingWipManager
    {
        private readonly IWipRepository _wipRepository;
        private readonly IForecastRepository _forecastRepository;
        private readonly IStockRepository _stockRepository;
        private readonly WipSession _session;

        private static readonly string[] Months = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };

        private class CachedWipData
        {
            public int? WipQuantity { get; set; }
            public double? ForecastData { get; set; }
        }

        public NewWorkingWipManager(
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
                AddDataTableColumns(resultTable, previousForecast, currentForecast, wipType, isCasePackEnabled, moq);

                using (var context = new WIPATContext())
                {
                    var productionData = FetchProductionData(context, itemsCatalogueData, currentForecast.Month);
                    var prevForecastMap = FetchForecastsBulk(context, asinList, previousForecast.Month, previousForecast.Year);
                    var currForecastMap = FetchForecastsBulk(context, asinList, currentForecast.Month, currentForecast.Year);

                    var targetYears = new List<string> { previousForecast.Year.ToString(), currentForecast.Year.ToString() }.Distinct().ToList();
                    var wipDataMap = FetchWipDetailsBulk(context, asinList, targetYears);

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
                            percentage,
                            prevForecastMap,
                            currForecastMap,
                            wipDataMap
                        );
                    }
                }

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
                    response.Data = resultTable;
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
        #endregion Public Methods

        #region Bulk Fetch Data Strategies

        private IEnumerable<List<string>> ChunkList(List<string> source, int chunkSize = 1000)
        {
            for (int i = 0; i < source.Count; i += chunkSize)
            {
                yield return source.GetRange(i, Math.Min(chunkSize, source.Count - i));
            }
        }

        private Dictionary<string, List<ForecastDetail>> FetchForecastsBulk(WIPATContext context, List<string> asinList, string month, string year)
        {
            var result = new Dictionary<string, List<ForecastDetail>>();

            foreach (var chunk in ChunkList(asinList, 1500))
            {
                var data = context.ForecastDetails
                    .Join(context.ForecastMasters, d => d.POForecastMasterId, m => m.Id, (d, m) => new { ForecastDetail = d, ForecastMaster = m })
                    .Where(joined => joined.ForecastMaster.Year == year
                                  && joined.ForecastMaster.Month == month
                                  && chunk.Contains(joined.ForecastDetail.CASIN))
                    .Select(joined => joined.ForecastDetail)
                    .ToList();

                var grouped = data.GroupBy(d => d.CASIN);
                foreach (var g in grouped)
                {
                    if (!result.ContainsKey(g.Key)) result[g.Key] = new List<ForecastDetail>();
                    result[g.Key].AddRange(g.ToList());
                }
            }
            return result;
        }

        private Dictionary<string, CachedWipData> FetchWipDetailsBulk(WIPATContext context, List<string> asinList, List<string> targetYears)
        {
            var result = new Dictionary<string, CachedWipData>();

            foreach (var chunk in ChunkList(asinList, 1500))
            {
                var data = context.WipDetails
                    .Join(context.WipMasters, d => d.WipMaster_Id, m => m.Id, (d, m) => new { d, m })
                    .Where(joined => joined.d.CommitmentPeriod == "3"
                                  && chunk.Contains(joined.d.CASIN)
                                  && targetYears.Contains(joined.m.TargetYear.ToString()))
                    .Select(joined => new
                    {
                        joined.d.CASIN,
                        joined.m.TargetMonth,
                        TargetYear = joined.m.TargetYear.ToString(),
                        joined.d.WipQuantity,
                        joined.d.ForecastData
                    })
                    .ToList();

                foreach (var item in data)
                {
                    string key = $"{item.CASIN}|{item.TargetMonth}|{item.TargetYear}";
                    if (!result.ContainsKey(key))
                    {
                        result[key] = new CachedWipData
                        {
                            WipQuantity = item.WipQuantity,
                            ForecastData = item.ForecastData
                        };
                    }
                }
            }
            return result;
        }

        private List<ForecastDetail> GetForecastDetailsFromMap(Dictionary<string, List<ForecastDetail>> map, string casin)
        {
            if (map.TryGetValue(casin, out var details) && details.Count > 0)
            {
                return details;
            }

            var defaultforecastDetails = new List<ForecastDetail>();
            int NoOfCommitmentRecords = 6;
            for (int i = 0; i <= NoOfCommitmentRecords; i++)
            {
                defaultforecastDetails.Add(new ForecastDetail
                {
                    CASIN = casin,
                    CommitmentPeriod = i,
                    RequestedQuantity = 0,
                    PODate = default
                });
            }
            return defaultforecastDetails;
        }

        private string GetTargetMonth(string currentMonth, int offset)
        {
            if (string.IsNullOrEmpty(currentMonth)) return string.Empty;
            int index = Array.IndexOf(Months, currentMonth);
            if (index == -1) return string.Empty;
            return Months[(index + offset) % 12];
        }

        #endregion

        #region WIP & Stock Calculation Logic

        private object CalculateWip(string wipType, int currentCommitmentPeriod, int targetCommitmentPeriod, int qty2, int? remainingLayman, int? previousRemainingStock, int initialStock, ref bool hasStockShortfallOccurred, string capacity, List<ForecastDetail> forecast_current_monthData, int? percentage, int previousRemainingLaymanStock, object remainingLaymanValue, int currentStock, out int? outRemainingLayman,
            double? arriving133percent,
            out double? grossRequirement
            )
        {
            object wip = DBNull.Value;
            outRemainingLayman = remainingLayman;
            grossRequirement = null;
            if (wipType == WipType.Analyst.ToString())
            {
                int? analystWip = CalculateAnalystWip(capacity, forecast_current_monthData, targetCommitmentPeriod, currentStock, qty2, currentCommitmentPeriod, percentage
                    , arriving133percent
                    , out grossRequirement
                    );
                wip = analystWip.HasValue ? (object)analystWip.Value : DBNull.Value;
            }

            return wip;
        }

        #endregion

        #region Analyst WIP Strategies

        private int? CalculateAnalystWip(string capacity, List<ForecastDetail> forecast_current_monthData, int targetCommitmentPeriod, int currentStock, int qty2, int currentCommitmentPeriod, int? percentage
            , double? arriving133percent
            , out double? grossRequirement
            )
        {
            int? analystWip = null;
            grossRequirement = null;

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
                else if (capacity == ProcessingWipType.WipWorking.ToString())
                {
                    analystWip = NEWCalculateMediumCapacityWip(targetCommitmentPeriod, currentStock, qty2, currentCommitmentPeriod, 33, arriving133percent, out grossRequirement);
                }
                else
                {
                    analystWip = 0;
                }
            }
            return analystWip;
        }

        private int CalculateHighCapacityWip(List<ForecastDetail> forecast_current_monthData, int targetCommitmentPeriod, int initialStock, int qty2, int currentCommitmentPeriod)
        {
            int analystWip = 0;
            int qty2CurrentPeriod = qty2;
            int qty2NextToTargetPeriod = forecast_current_monthData.FirstOrDefault(f => f.CommitmentPeriod == targetCommitmentPeriod + 1)?.RequestedQuantity ?? 0;
            int value = (qty2CurrentPeriod + qty2NextToTargetPeriod) - initialStock;

            if (value > 0)
            {
                if (currentCommitmentPeriod == targetCommitmentPeriod)
                {
                    analystWip = value;
                }
                else if (currentCommitmentPeriod == targetCommitmentPeriod + 1)
                {
                    analystWip = 0;
                }
                else
                {
                    analystWip = qty2;
                }
            }
            else
            {
                if (currentCommitmentPeriod == targetCommitmentPeriod || currentCommitmentPeriod == targetCommitmentPeriod + 1)
                {
                    analystWip = 0;
                }
                else
                {
                    analystWip = qty2;
                }
            }

            return analystWip;
        }

        private int CalculateMediumCapacityWip(int targetCommitmentPeriod, int initialStock, int qty2, int currentCommitmentPeriod, int percentage)
        {
            int analystWip = 0;
            double requiredQty = qty2 + ((percentage / 100.0) * qty2);
            double shortfall = requiredQty - initialStock;
            var roundedShortfall = (shortfall - Math.Floor(shortfall) < 0.5) ? Math.Floor(shortfall) : Math.Ceiling(shortfall);
            int intShortfall = (int)roundedShortfall;

            if (intShortfall > 0)
            {
                analystWip = intShortfall;
            }

            return analystWip;
        }

        private int CalculateLowCapacityWip(List<ForecastDetail> forecastData, int targetCommitmentPeriod, int initialStock, int qty2, int currentCommitmentPeriod)
        {
            int analystWip = 0;
            int qty2TargetMonth = forecastData.FirstOrDefault(f => f.CommitmentPeriod == targetCommitmentPeriod)?.RequestedQuantity ?? 0;
            int qty2NextMonth = forecastData.FirstOrDefault(f => f.CommitmentPeriod == (targetCommitmentPeriod + 1))?.RequestedQuantity ?? 0;

            if (currentCommitmentPeriod == targetCommitmentPeriod)
            {
                if (qty2TargetMonth > initialStock)
                {
                    analystWip = qty2TargetMonth - initialStock;
                    initialStock = 0;
                }
                else
                {
                    analystWip = 0;
                }
            }
            else
            {
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

        private int NEWCalculateMediumCapacityWip(
        int targetCommitmentPeriod,
        int initialStock,
        int qty2,
        int currentCommitmentPeriod,
        int percentage,
        double? arriving133percent,
        out double? grossRequirement)
        {
            int analystWip = 0;

            double baseQty = ((currentCommitmentPeriod == 1 || currentCommitmentPeriod == 2) && arriving133percent.HasValue)
                             ? arriving133percent.Value
                             : qty2;

            double _requiredQty = baseQty + ((percentage / 100.0) * baseQty);
            double requiredQty = Math.Round(_requiredQty, MidpointRounding.AwayFromZero);

            if (currentCommitmentPeriod == 3)
            {
                grossRequirement = (int)requiredQty;
            }
            else
            {
                grossRequirement = null;
            }

            double shortfall = requiredQty - initialStock;
            double roundedShortfall = Math.Round(shortfall, MidpointRounding.AwayFromZero);
            int intShortfall = (int)roundedShortfall;

            if (intShortfall > 0)
            {
                analystWip = intShortfall;
            }

            return analystWip;
        }
        #endregion

        #region Helper Methods

        private void AddDataTableColumns(DataTable result, ForecastMaster forecast_last_month, ForecastMaster forecast_current_month, string wipType, bool checkBoxCasePack, int? MOQ)
        {
            result.Columns.Add("C-ASIN", typeof(string));
            // Removed IsActive, using ItemStatus int
            result.Columns.Add("ItemStatus", typeof(int));

            result.Columns.Add("Month", typeof(string));
            result.Columns.Add("Year", typeof(string));
            result.Columns.Add("PO_Date", typeof(DateTime));
            result.Columns.Add($"Requested_Quantity ({forecast_last_month.Month})", typeof(int));
            result.Columns.Add($"Wip ({forecast_last_month.Month})", typeof(int));
            result.Columns.Add($"Requested_Quantity ({forecast_current_month.Month})", typeof(int));
            result.Columns.Add("Arriving_133%", typeof(double));

            result.Columns.Add($"CommitmentPeriod ({forecast_current_month.Month})", typeof(string));
            result.Columns.Add("Actual_Order", typeof(int));
            result.Columns.Add("Initial_Stock", typeof(int));
            result.Columns.Add("Stock", typeof(int));

            if (wipType == WipType.LaymanFormula.ToString() || wipType == WipType.Layman.ToString())
            {
                result.Columns.Add("Delta", typeof(int));
                result.Columns.Add("Stock_Layman", typeof(string));
            }
            result.Columns.Add("grossRequirement", typeof(double));

            result.Columns.Add($"Review_Wip", typeof(int));

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

        private void AddRowToDataTable(
       DataTable result, string asin, DateTime pODate, int qty1, object wipOfForecast_last_month,
       int qty2, int commitmentPeriod, int? actualOrderVal, int currentStock, int remainingStock,
       object remainingLaymanValue, object finalWip,
       object moqWip, int? moq, object casePackWip, int? casePack,
       string wipType, ForecastMaster forecast_last_month, ForecastMaster forecast_current_month, int? rawCalculatedWip
            , double? grossRequirement
            , double? arriving133percent
            , int itemStatus
            )
        {
            int delta = qty2 - qty1;

            DataRow newRow = result.NewRow();
            newRow["C-ASIN"] = asin;
            newRow["ItemStatus"] = itemStatus;
            newRow["Month"] = pODate.ToString("MMMM");
            newRow["Year"] = pODate.ToString("yyyy");
            newRow["PO_Date"] = pODate;
            newRow[$"Requested_Quantity ({forecast_last_month.Month})"] = qty1;
            newRow[$"Wip ({forecast_last_month.Month})"] = wipOfForecast_last_month ?? DBNull.Value;
            newRow[$"Requested_Quantity ({forecast_current_month.Month})"] = qty2;
            newRow["Arriving_133%"] = arriving133percent.HasValue ? (object)arriving133percent.Value : DBNull.Value;
            newRow[$"CommitmentPeriod ({forecast_current_month.Month})"] = commitmentPeriod;
            newRow["Actual_Order"] = actualOrderVal.HasValue ? (object)actualOrderVal.Value : DBNull.Value;
            newRow["Initial_Stock"] = currentStock;
            newRow["Stock"] = remainingStock;

            if (wipType == WipType.LaymanFormula.ToString() || wipType == WipType.Layman.ToString())
            {
                newRow["Delta"] = delta;
                newRow["Stock_Layman"] = remainingLaymanValue ?? DBNull.Value;
            }
            newRow["grossRequirement"] = grossRequirement.HasValue ? (object)Math.Round(grossRequirement.Value, 2) : DBNull.Value;
            newRow[$"Review_Wip"] = rawCalculatedWip;

            if (result.Columns.Contains("MOQ"))
            {
                newRow["MOQ_Wip"] = moqWip ?? DBNull.Value;
                newRow["MOQ"] = moq.HasValue ? (object)moq.Value : DBNull.Value;
            }
            if (result.Columns.Contains("CasePack"))
            {
                newRow["CasePack_Wip"] = casePackWip ?? DBNull.Value;
                newRow["CasePack"] = casePack.HasValue ? (object)casePack.Value : DBNull.Value;
            }

            result.Rows.Add(newRow);
        }

        private List<SimulationInputData> _FetchProductionData(WIPATContext context, List<string> asinList, string currentMonth)
        {
            bool includeInactive = _session.IsContinueWithInactiveItems;

            var itemCatalogueMap = context.ItemCatalogues
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
        private List<SimulationInputData> FetchProductionData(WIPATContext context, List<ItemCatalogue> asinList, string currentMonth)
        {
            var itemIds = asinList.Select(c => c.Id).ToList();

            var actualOrderMap = context.ActualOrders
                .Where(a => itemIds.Contains(a.ItemCatalogueId) && a.Month == currentMonth)
                .ToDictionary(a => a.ItemCatalogueId, a => (int?)a.Quantity ?? 0);

            return asinList.Select(item => new SimulationInputData
            {
                Asin = item.Casin,
                ItemId = item.Id,
                ActualOrderQty = actualOrderMap.ContainsKey(item.Id) ? actualOrderMap[item.Id] : 0,
                InitialStock = _stockRepository.GetInitialStockValue(item.Id)
            }).ToList();
        }
        #endregion

        #region Process Single Asin
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
        int? percentage,
        Dictionary<string, List<ForecastDetail>> prevForecastMap,
        Dictionary<string, List<ForecastDetail>> currForecastMap,
        Dictionary<string, CachedWipData> wipDataMap)
        {
            var prevDetails = GetForecastDetailsFromMap(prevForecastMap, data.Asin);
            var currDetails = GetForecastDetailsFromMap(currForecastMap, data.Asin);

            var catalogueItem = catalogue?.FirstOrDefault(i => i.Casin == data.Asin);
            int? casePackQty = catalogueItem?.CasePackQty;

            int itemStatus = catalogueItem != null ? catalogueItem.ItemStatus : (int)CatalogueItemStatus.Invalid;
            bool isActive = itemStatus == (int)CatalogueItemStatus.Active;

            var periods = prevDetails.Select(f => f.CommitmentPeriod)
                .Union(currDetails.Select(f => f.CommitmentPeriod))
                .Prepend(0)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            int currentStock = data.InitialStock;
            int? remainingLayman = 0;
            bool hasStockShortfall = false;

            int? prevPeriodRemainingStock = null;
            int prevPeriodRemainingLayman = data.InitialStock;

            foreach (int period in periods)
            {
                int prevForecastQty = 0;
                int currForecastQty = 0;
                DateTime poDate = default;
                int? arrivingWip = null;
                double? arriving133percent = null;
                int demand = 0;
                int? actualOrder = null;

                if (period == 0) // P1 Logic
                {
                    int cpLookup = period + 1;
                    prevForecastQty = prevDetails.FirstOrDefault(f => f.CommitmentPeriod == cpLookup)?.RequestedQuantity ?? 0;
                    poDate = prevDetails.FirstOrDefault(f => f.CommitmentPeriod == cpLookup)?.PODate ?? default;

                    // Fetch Wip from Dictionary
                    string mon = GetTargetMonth(currForecast.Month, cpLookup - 1);
                    string key = $"{data.Asin}|{mon}|{currForecast.Year}";
                    wipDataMap.TryGetValue(key, out var wipCache);
                    arrivingWip = wipCache?.WipQuantity ?? 0;

                    actualOrder = data.ActualOrderQty;
                    demand = actualOrder.Value;
                }
                else // P2+ Logic
                {
                    prevForecastQty = prevDetails.FirstOrDefault(f => f.CommitmentPeriod == period)?.RequestedQuantity ?? 0;
                    currForecastQty = currDetails.FirstOrDefault(f => f.CommitmentPeriod == period)?.RequestedQuantity ?? 0;
                    poDate = currDetails.FirstOrDefault(f => f.CommitmentPeriod == period)?.PODate ?? default;

                    demand = currForecastQty;

                    if (period < targetPeriod)
                    {
                        // Wip Dictionary Fetch
                        string wipMon = GetTargetMonth(currForecast.Month, period);
                        string wipKey = $"{data.Asin}|{wipMon}|{currForecast.Year}";
                        wipDataMap.TryGetValue(wipKey, out var wCache);
                        arrivingWip = wCache?.WipQuantity ?? 0;

                        // Arriving 133% Dictionary Fetch
                        string pctMon = GetTargetMonth(currForecast.Month, period);
                        string pctKey = $"{data.Asin}|{pctMon}|{currForecast.Year}";
                        wipDataMap.TryGetValue(pctKey, out var pCache);
                        arriving133percent = pCache?.ForecastData ?? 0;
                    }
                }

                if (period == 1 || period == 2)
                {
                    if (arriving133percent != null)
                    {
                        demand = (int)arriving133percent;
                    }
                    else
                    {
                        demand = (int)currForecastQty;
                    }
                }
                else if (period > 2)
                {
                    demand = (int)currForecastQty;
                }

                double? grossRequirement;
                int? rawCalculatedWip = CalculateNewWipLogic(
                    period, targetPeriod, currentStock, demand, arrivingWip, wipType,
                    currForecastQty, remainingLayman, prevPeriodRemainingStock, data.InitialStock,
                    ref hasStockShortfall, capacity, currDetails, percentage, prevPeriodRemainingLayman,
                    arriving133percent,
                    out grossRequirement
                );

                if (period < targetPeriod) rawCalculatedWip = arrivingWip;

                if (!isActive || itemStatus == (int)CatalogueItemStatus.Invalid)
                {
                    rawCalculatedWip = 0;
                }

                int? finalWip = ApplyMoqAndCasePack(rawCalculatedWip, moq, isCasePackEnabled, casePackQty, out int? moqWip, out int? casePackWip);

                if (rawCalculatedWip == 0 && isCasePackEnabled && casePackWip == null)
                {
                    casePackWip = 0;
                }

                int remainingStock = CalculateRemainingStock(period, targetPeriod, currentStock, demand, prevForecastQty, arrivingWip, currForecastQty, wipType, arriving133percent);

                AddRowToDataTable(
                    table, data.Asin, poDate, prevForecastQty, arrivingWip, currForecastQty,
                    period, actualOrder, currentStock, remainingStock, remainingLayman,
                    (object)finalWip ?? DBNull.Value,
                    (object)moqWip ?? DBNull.Value,
                    moq,
                    (object)casePackWip ?? DBNull.Value,
                    casePackQty,
                    wipType, prevForecast, currForecast, rawCalculatedWip,
                     grossRequirement,
                     arriving133percent,
                    itemStatus
                );

                currentStock = remainingStock;
                prevPeriodRemainingStock = remainingStock;
                prevPeriodRemainingLayman = remainingLayman ?? 0;
            }
        }

        private int? CalculateNewWipLogic(int period, int targetPeriod, int currentStock, int demand, int? arrivingWip, string wipType, int currForecastQty, int? remainingLayman, int? prevStock, int initialStock, ref bool hasStockShortfall, string capacity, List<ForecastDetail> currDetails, int? percentage, int prevLaymanStock
            , double? arriving133percent
            , out double? grossRequirement
            )
        {
            if (period < targetPeriod)
            {
                grossRequirement = null;
                return null;
            }

            int stockBeforeNewWip = currentStock;

            if ((wipType == WipType.Layman.ToString() || wipType == WipType.Analyst.ToString()) && stockBeforeNewWip < 0)
            {
                stockBeforeNewWip = 0;
            }

            int? newRemLayman;

            var wipObj = CalculateWip(
                                        wipType,             // 1
                                        period,              // 2
                                        targetPeriod,        // 3
                                        currForecastQty,     // 4
                                        remainingLayman,     // 5
                                        prevStock,           // 6
                                        initialStock,        // 7
                                        ref hasStockShortfall, // 8
                                        capacity,            // 9
                                        currDetails,         // 10
                                        percentage,          // 11
                                        prevLaymanStock,     // 12
                                        DBNull.Value,        // 13
                                        stockBeforeNewWip,   // 14
                                        out newRemLayman,    // 15  
                                        arriving133percent,  // 16 
                                        out grossRequirement // 17 
                                        );

            return (wipObj == DBNull.Value) ? (int?)null : Convert.ToInt32(wipObj);
        }

        private int? ApplyMoqAndCasePack(int? inputWip, int? moq, bool useCasePack, int? casePackQty, out int? moqWip, out int? casePackWip)
        {
            int? runningWip = inputWip;
            moqWip = null;
            casePackWip = null;

            if (!runningWip.HasValue) return null;

            if (moq.HasValue)
            {
                moqWip = (moq.Value > runningWip.Value) ? 0 : runningWip.Value;
                runningWip = moqWip;
            }

            if (useCasePack && casePackQty.HasValue && casePackQty.Value > 0)
            {
                if (runningWip.Value == 0)
                {
                    casePackWip = 0;
                }
                else
                {
                    double ratio = (double)runningWip.Value / casePackQty.Value;
                    double rounded = (ratio % 1 < 0.5) ? Math.Floor(ratio) : Math.Ceiling(ratio);
                    casePackWip = (int)rounded * casePackQty.Value;
                }
                runningWip = casePackWip;
            }

            return runningWip;
        }

        private int CalculateRemainingStock(int period, int targetPeriod, int currentStock, int demand, int prevForecastQty, int? arrivingWip, int currForecastQty, string wipType
                , double? arriving133percent
            )
        {
            int remaining = 0;

            if (period == 0)
            {
                int delta = currForecastQty - prevForecastQty;
                if (arrivingWip == null)
                    remaining = currentStock - demand - delta;
                else
                    remaining = currentStock - demand + arrivingWip.Value;
            }
            else
            {
                if (period < targetPeriod && arrivingWip.HasValue)
                {
                    remaining = currentStock + arrivingWip.Value - demand;
                }
                else
                {
                    remaining = currentStock - (demand - prevForecastQty);
                }
            }

            if ((wipType == WipType.Layman.ToString() || wipType == WipType.Analyst.ToString()) && remaining < 0)
            {
                remaining = 0;
            }

            return remaining;
        }

        #endregion process single asin

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

        #region save
        /// <summary>
        /// Validates session data, parses input tables, and persists Work-in-Progress (WIP) records.
        /// </summary>
        /// <remarks>
        /// <para><b>Operations:</b> Processes the data table, de-duplicates records, inserts/updates WIP Master and Detail records, 
        /// marks the forecast as calculated, and updates stock via repository, all within a single database transaction.</para>
        /// <para><b>Tables/Entities Affected:</b> WipMasters, WipDetails, ForecastMasters, 
        /// and externally updates the Stock table via repository.</para>
        /// </remarks>
        public async Task<Response<bool>> SaveWipRecordsAsync(DataTable finalDataTable, string capacity, string wipColName, DataTable stockDataTable, WipSession wipSession)
        {
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

                var (issuedMonthName, issuedYear) = ParseTargetMonthAndYear(wipSession.CurrentMonthWithYear);

                #endregion

                #region 2. Parse DataTable & Prepare Memory Objects
                var newDetails = new List<WipDetail>(finalDataTable.Rows.Count);

                int? GetInt(DataRow r, string col)
                {
                    if (finalDataTable.Columns.Contains(col) && r[col] != DBNull.Value && int.TryParse(r[col].ToString(), out int v))
                        return v;
                    return null;
                }

                int? globalMoq = null;
                if (finalDataTable.Rows.Count > 0) globalMoq = GetInt(finalDataTable.Rows[0], "MOQ");

                bool globalIsCasePack = finalDataTable.Columns.Contains("CasePack") && finalDataTable.AsEnumerable().Any(r => r["CasePack"] != DBNull.Value);

                foreach (DataRow row in finalDataTable.Rows)
                {
                    string casin = row["C-Asin"]?.ToString()?.Trim();
                    string cPeriod = row[$"CommitmentPeriod ({session.Month})"]?.ToString()?.Trim();

                    if (string.IsNullOrEmpty(casin) || string.IsNullOrEmpty(cPeriod))
                        continue;

                    int? reviewWip = GetInt(row, "Review_Wip");
                    int? moqWip = GetInt(row, "MOQ_Wip");
                    int? cpWip = GetInt(row, "CasePack_Wip");
                    double? grossReq = row["grossRequirement"] as double?;

                    // Extract ItemStatus int enum
                    int itemStatus = (int)CatalogueItemStatus.Invalid;
                    if (finalDataTable.Columns.Contains("ItemStatus") && row["ItemStatus"] != DBNull.Value)
                    {
                        if (int.TryParse(row["ItemStatus"].ToString(), out int parsedStatus))
                        {
                            itemStatus = parsedStatus;
                        }
                    }

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

                        // Assign INT
                        ItemStatus = itemStatus,

                        LaymanFormula = wipType == WipType.LaymanFormula.ToString() ? finalWip : null,
                        Layman = wipType == WipType.Layman.ToString() ? finalWip : null,
                        Analyst = wipType == WipType.Analyst.ToString() ? finalWip : null,
                        ForecastData = grossReq,
                        Review_Wip = reviewWip,
                        MOQ_Wip = moqWip,
                        CasePack_Wip = cpWip,
                        CasePack = GetInt(row, "CasePack"),
                        PODate = DateTime.Now
                    };
                    newDetails.Add(detail);
                }

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
                    context.Configuration.ValidateOnSaveEnabled = false;
                    context.Database.CommandTimeout = 300;

                    try
                    {
                        var itemCatalogueMap = await context.ItemCatalogues
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

                        var forecastMaster = await context.ForecastMasters.AsNoTracking().FirstOrDefaultAsync(fm => fm.FileName == fileName);
                        if (forecastMaster == null) throw new Exception($"POForecastMaster not found for file '{fileName}'");

                        var wipMaster = await context.WipMasters
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
                                CreatedById = _session.LoggedInUser.Id
                            };
                            context.WipMasters.Add(wipMaster);
                            await context.SaveChangesAsync();
                        }
                        else
                        {
                            wipMaster.UpdatedAt = DateTime.Now;
                            wipMaster.UpdatedById = _session.LoggedInUser.Id;
                            wipMaster.IsCasePackChecked = globalIsCasePack;
                            wipMaster.WipProcessingType = capacity;
                            wipMaster.MOQ = globalMoq;
                        }

                        var existingWipDetails = await context.WipDetails
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
                                context.WipDetails.Attach(item);
                                context.Entry(item).State = EntityState.Modified;
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
                                    context.WipDetails.AddRange(detailsToInsert);
                                    detailsToInsert.Clear();
                                }
                                await context.SaveChangesAsync();
                            }
                        }

                        if (detailsToInsert.Any())
                        {
                            context.WipDetails.AddRange(detailsToInsert);
                        }

                        var masterStub = new ForecastMaster { Id = forecastMaster.Id, IsWipCalculated = true };
                        context.ForecastMasters.Attach(masterStub);
                        context.Entry(masterStub).Property(x => x.IsWipCalculated).IsModified = true;

                        var stockResult = await _stockRepository.UpdateStockQtyInStockTable(stockDataTable, wipColName, session.Month, session.Year);
                        if (!stockResult.Success) throw new Exception(stockResult.Message);

                        await context.SaveChangesAsync();
                        tx.Commit();
                        #endregion

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
                        context.Configuration.ValidateOnSaveEnabled = true;
                    }
                }
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

        private (string, int) ParseTargetMonthAndYear(string targetMonth)
        {
            string[] parts = targetMonth.Split(' ');
            if (parts.Length != 2)
            {
                return (null, 0);
            }

            string targetMonthName = parts[0];
            string targetYearStr = parts[1];

            if (int.TryParse(targetYearStr, out int targetYear))
            {
                return (targetMonthName, targetYear);
            }
            else
            {
                return (null, 0);
            }
        }
        #endregion save
    }
}