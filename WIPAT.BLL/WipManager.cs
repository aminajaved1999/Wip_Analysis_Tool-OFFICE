using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WIPAT.DAL;
using WIPAT.Entities;
using WIPAT.Entities.Dto;
using WIPAT.Entities.Enum;

namespace WIPAT.BLL
{
    public class WipManager
    {
        WipRepository wipRepository;
        ForecastRepository forecastRepository;

        public WipManager()
        {

            wipRepository = new WipRepository();
            forecastRepository = new ForecastRepository();
        }


      
        #region wip table
        public Response<DataTable> BuildCommonWipDataTable(List<string> asinList, ForecastMaster forecast1, ForecastMaster forecast2, string targetMonth, string wipType, string capacity = null, int? percentage = null)
        {
            var response = new Response<DataTable>();
            DateTime? firstShortfallMonth = null;
            bool firstShortfallHandled = false;
            bool shouldSetWipToRequestedQuantity = false;
            bool hasStockShortfallOccurred = false;
            var context = new WIPATContext();
            DataTable result = new DataTable();
            bool isCurrentMonthFirstShortfall = false;
            object wip = DBNull.Value;
            object remainingLaymanValue = DBNull.Value;
            int? remainingLayman = 0;
            try
            {
                // Add columns for the data table
                xAddDataTableColumns(result, forecast1, forecast2, wipType);

                DateTime targetMonthDate = DateTime.ParseExact(targetMonth, "MMMM yyyy", CultureInfo.InvariantCulture);

                foreach (string asin in asinList)
                {
                    #region Retrieve Item and Actual Order 
                    var item = context.ItemCatalogues.FirstOrDefault(i => i.Casin == asin);
                    if (item == null) continue;

                    int itemCatalogueId = item.Id;
                    int actualOrder = context.ActualOrders
                        .Where(a => a.ItemCatalogueId == itemCatalogueId)
                        .Select(a => (int?)a.Quantity)
                        .FirstOrDefault() ?? 0;

                    int initialStock = context.InitialStocks
                        .Where(s => s.ItemCatalogueId == itemCatalogueId)
                        .Select(s => (int?)s.Quantity)
                        .FirstOrDefault() ?? 0;
                    #endregion Retrieve Item and Actual Order 

                    #region Forecast Data Preparation

                    var forecastDict1 = GetForecastData(forecast1, asin);
                    var forecastDict2 = GetForecastData(forecast2, asin);

                    //var allMonths = forecastDict1.Keys
                    //    .Union(forecastDict2.Keys)
                    //    .Select(m => DateTime.ParseExact(m, "MMMM yyyy", CultureInfo.InvariantCulture))
                    //    .OrderBy(d => d)
                    //    .ToList();

                    var allMonths = forecastDict1.Keys
                                    .Union(forecastDict2.Keys)
                                    .Select(m => DateTime.ParseExact(m, "MMMM yyyy", CultureInfo.InvariantCulture))
                                    .OrderBy(d => d)
                                    .ToList();

                    #endregion Forecast Data Preparation

                    int currentStock = initialStock;
                    bool isFirstMonth = true;
                    int? previousRemainingStock = null; // Track previous month's remaining stock for next month calculation
                    int previousRemainingLaymanStock = initialStock; // Track the previous month's remaining layman stock for subsequent months

                    foreach (var monthDate in allMonths)
                    {
                        string monthName = monthDate.ToString("MMMM yyyy");

                        string nextMonthName = monthDate.AddMonths(1).ToString("MMMM yyyy");


                        // Fetch CommitmentPeriod from forecast2.Details
                        string commitmentPeriod = GetCommitmentPeriod(forecast2, asin, monthName);

                        // Fetch Wip of forecast1
                        int? wipOfForecast1 = null;
                        if (monthDate < targetMonthDate)
                        {
                            wipOfForecast1 = GetWipFromForecast(forecast1, asin, monthName, context);
                        }


                        // If WIP Of Forecast1's targetmonth is missing for the target month, immediately return an error
                        if (monthDate == targetMonthDate.AddMonths(-1))
                        {
                            if (!wipOfForecast1.HasValue)
                            {
                                response.Success = false;
                                response.Message = $"WIP value is missing for ASIN: {asin} in the target month ({targetMonthDate.AddMonths(-1).ToString("MMMM yyyy")}). Calculate its Wip first.";
                                return response;
                            }
                        }

                        int qty1 = forecastDict1.TryGetValue(monthName, out int q1) ? q1 : 0;
                        int qty2 = forecastDict2.TryGetValue(monthName, out int q2) ? q2 : 0;
                        int delta = qty2 - qty1;

                        object actualOrderVal = DBNull.Value;
                        int remainingStock;

                        //Remaining Stock Calculation
                        remainingStock = CalculateRemainingStock(monthDate, targetMonthDate, currentStock, qty2, wipOfForecast1, delta, isFirstMonth, actualOrder, ref isFirstMonth, wipType);

                        //wip calculation
                        if (wipType == WipType.LaymanFormula.ToString() || wipType == WipType.Layman.ToString())
                        {

                            // Remaining Layman Calculation
                            remainingLayman = CalculateRemainingLayman(monthDate, targetMonthDate, qty2, previousRemainingStock, previousRemainingLaymanStock, initialStock);
                            remainingLaymanValue = remainingLayman.HasValue ? (object)remainingLayman.Value.ToString() : DBNull.Value;


                            if (wipType == WipType.LaymanFormula.ToString())
                            {
                                //Calculate Layman Formula Wip
                                int? LaymanFormulaWip = CalculateLaymanFormulaWip(monthDate, targetMonthDate, qty2, remainingLayman, ref hasStockShortfallOccurred);
                                var laymanFormulaWipValue = LaymanFormulaWip.HasValue ? (object)LaymanFormulaWip.Value : DBNull.Value;
                                wip = laymanFormulaWipValue;
                            }
                            else if (wipType == WipType.Layman.ToString())
                            {
                                //Calculate Layman Formula Wip
                                int? LaymanWip = CalculateLaymanWip(monthDate, targetMonthDate, qty2, remainingLayman, ref hasStockShortfallOccurred);
                                var laymanWipValue = LaymanWip.HasValue ? (object)LaymanWip.Value : DBNull.Value;
                                wip = laymanWipValue;
                            }
                        }
                        else if (wipType == WipType.Analyst.ToString())
                        {
                            //Calculate Analyst Wip
                            int? analystWip = CalculateAnalystWip(capacity, forecastDict2, targetMonthDate, currentStock, qty2, monthDate, percentage);
                            var analystWipValue = analystWip.HasValue ? (object)analystWip.Value : DBNull.Value;
                            wip = analystWipValue;
                        }
                        //Add Row to DataTable
                        xAddRowToDataTable(result, asin, monthName, qty1, wipOfForecast1, qty2, commitmentPeriod, actualOrderVal, delta, currentStock, remainingStock, remainingLaymanValue, wip, wipType);

                        #region Update Stocks for Next Month
                        currentStock = remainingStock;

                        // Store the current remaining layman for use in the next month
                        previousRemainingLaymanStock = remainingLayman ?? 0;

                        // Store the current remaining stock for use in the next month
                        previousRemainingStock = remainingStock;
                        #endregion Update Stocks for Next Month
                    }
                }

                // success response
                response.Success = true;
                response.Message = "Data table built successfully.";
                response.Data = result;
                response.Status = StatusType.Success;
                return response;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"An error occurred: {ex.Message}";
                response.Data = null;
                response.Status = StatusType.Error;
                return response;

            }
        }

        public Response<DataTable> nBuildCommonWipDataTable(List<string> asinList, ForecastMaster forecast1, ForecastMaster forecast2, string targetMonth, string wipType, string capacity = null, int? percentage = null)
        {
            var response = new Response<DataTable>();
            DateTime? firstShortfallMonth = null;
            bool firstShortfallHandled = false;
            bool shouldSetWipToRequestedQuantity = false;
            bool hasStockShortfallOccurred = false;
            var context = new WIPATContext();
            DataTable result = new DataTable();
            bool isCurrentMonthFirstShortfall = false;
            object wip = DBNull.Value;
            object remainingLaymanValue = DBNull.Value;
            int? remainingLayman = 0;
            try
            {
                // Add columns for the data table
                xAddDataTableColumns(result, forecast1, forecast2, wipType);

                DateTime targetMonthDate = DateTime.ParseExact(targetMonth, "MMMM yyyy", CultureInfo.InvariantCulture);

                foreach (string asin in asinList)
                {
                    #region Retrieve Item and Actual Order 
                    var item = context.ItemCatalogues.FirstOrDefault(i => i.Casin == asin);
                    if (item == null) continue;

                    int itemCatalogueId = item.Id;
                    int actualOrder = context.ActualOrders
                        .Where(a => a.ItemCatalogueId == itemCatalogueId)
                        .Select(a => (int?)a.Quantity)
                        .FirstOrDefault() ?? 0;

                    int initialStock = context.InitialStocks
                        .Where(s => s.ItemCatalogueId == itemCatalogueId)
                        .Select(s => (int?)s.Quantity)
                        .FirstOrDefault() ?? 0;
                    #endregion Retrieve Item and Actual Order 

                    #region Forecast Data Preparation

                    Dictionary<DateTime, int> forecastDict1 = nGetForecastData(forecast1, asin);
                    Dictionary<DateTime, int> forecastDict2 = nGetForecastData(forecast2, asin);


                    List<DateTime> poDateList = forecastDict1.Keys
                                            .Union(forecastDict2.Keys)
                                            .ToList();




                    #endregion Forecast Data Preparation

                    int currentStock = initialStock;
                    bool isFirstMonth = true;
                    int? previousRemainingStock = null; // Track previous month's remaining stock for next month calculation
                    int previousRemainingLaymanStock = initialStock; // Track the previous month's remaining layman stock for subsequent months

                    foreach (var podate in poDateList)
                    {
                        string monthName = podate.ToString("MMMM yyyy");
                        var monthDate = podate;

                        // Fetch CommitmentPeriod from forecast2.Details
                        string commitmentPeriod = GetCommitmentPeriod2(forecast2, asin, podate);

                        // Fetch Wip of forecast1
                        int? wipOfForecast1 = null;

                        if (monthDate < targetMonthDate)
                        {
                            wipOfForecast1 = GetWipFromForecast(forecast1, asin, monthName, context);
                        }


                        // If WIP Of Forecast1's targetmonth is missing for the target month, immediately return an error
                        if (monthDate == targetMonthDate.AddMonths(-1))
                        {
                            if (!wipOfForecast1.HasValue)
                            {
                                response.Success = false;
                                response.Message = $"WIP value is missing for ASIN: {asin} in the target month ({targetMonthDate.AddMonths(-1).ToString("MMMM yyyy")}). Calculate its Wip first.";
                                return response;
                            }
                        }

                        int qty1 = forecastDict1.TryGetValue(podate, out int q1) ? q1 : 0;
                        int qty2 = forecastDict2.TryGetValue(podate, out int q2) ? q2 : 0;
                        int delta = qty2 - qty1;

                        object actualOrderVal = DBNull.Value;
                        int remainingStock;

                        //Remaining Stock Calculation
                        remainingStock = CalculateRemainingStock(monthDate, targetMonthDate, currentStock, qty2, wipOfForecast1, delta, isFirstMonth, actualOrder, ref isFirstMonth, wipType);

                        //wip calculation
                        if (wipType == WipType.LaymanFormula.ToString() || wipType == WipType.Layman.ToString())
                        {

                            // Remaining Layman Calculation
                            remainingLayman = CalculateRemainingLayman(monthDate, targetMonthDate, qty2, previousRemainingStock, previousRemainingLaymanStock, initialStock);
                            remainingLaymanValue = remainingLayman.HasValue ? (object)remainingLayman.Value.ToString() : DBNull.Value;


                            if (wipType == WipType.LaymanFormula.ToString())
                            {
                                //Calculate Layman Formula Wip
                                int? LaymanFormulaWip = CalculateLaymanFormulaWip(monthDate, targetMonthDate, qty2, remainingLayman, ref hasStockShortfallOccurred);
                                var laymanFormulaWipValue = LaymanFormulaWip.HasValue ? (object)LaymanFormulaWip.Value : DBNull.Value;
                                wip = laymanFormulaWipValue;
                            }
                            else if (wipType == WipType.Layman.ToString())
                            {
                                //Calculate Layman Formula Wip
                                int? LaymanWip = CalculateLaymanWip(monthDate, targetMonthDate, qty2, remainingLayman, ref hasStockShortfallOccurred);
                                var laymanWipValue = LaymanWip.HasValue ? (object)LaymanWip.Value : DBNull.Value;
                                wip = laymanWipValue;
                            }
                        }
                        else if (wipType == WipType.Analyst.ToString())
                        {
                            //Calculate Analyst Wip
                            int? analystWip = CalculateAnalystWip2(capacity, forecastDict2, targetMonthDate, currentStock, qty2, monthDate, percentage);
                            var analystWipValue = analystWip.HasValue ? (object)analystWip.Value : DBNull.Value;
                            wip = analystWipValue;
                        }
                        //Add Row to DataTable
                        xAddRowToDataTable(result, asin, monthName, qty1, wipOfForecast1, qty2, commitmentPeriod, actualOrderVal, delta, currentStock, remainingStock, remainingLaymanValue, wip, wipType);

                        #region Update Stocks for Next Month
                        currentStock = remainingStock;

                        // Store the current remaining layman for use in the next month
                        previousRemainingLaymanStock = remainingLayman ?? 0;

                        // Store the current remaining stock for use in the next month
                        previousRemainingStock = remainingStock;
                        #endregion Update Stocks for Next Month
                    }
                }

                // success response
                response.Success = true;
                response.Message = "Data table built successfully.";
                response.Data = result;
                response.Status = StatusType.Success;
                return response;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"An error occurred: {ex.Message}";
                response.Data = null;
                response.Status = StatusType.Error;
                return response;

            }
        }


        private int? GetWipFromForecast(ForecastMaster forecast, string asin, string monthName, WIPATContext context)
        {
            // Query the WipMaster table first, then access WipDetails via the navigation property
            var forecastMaster = context.ForecastMasters
                .Where(fm => fm.FileName == forecast.FileName)
                .FirstOrDefault();

            if (forecastMaster == null) return null;

            // Now, query the WipDetails related to that WipMaster
            return forecastMaster.Details
                .Where(d => d.CASIN == asin &&
                            (d.Month.ToString() + " " + d.Year.ToString()) == monthName)
                .Select(d => d.Wip)
                .FirstOrDefault();
        }
        private int? GetWipFromForecast2(ForecastMaster forecast, string asin, DateTime podate, WIPATContext context)
        {
            // Query the WipMaster table first, then access WipDetails via the navigation property
            var forecastMaster = context.ForecastMasters
                .Where(fm => fm.FileName == forecast.FileName)
                .FirstOrDefault();

            if (forecastMaster == null) return null;

            // Now, query the WipDetails related to that WipMaster
            return forecastMaster.Details
                .Where(d => d.CASIN == asin && d.PODate == podate)
                .Select(d => d.Wip)
                .FirstOrDefault();
        }


        #region wip helpers
        private void xAddDataTableColumns(DataTable result, ForecastMaster forecast1, ForecastMaster forecast2, string wipType)
        {
            if (wipType == WipType.LaymanFormula.ToString() || wipType == WipType.Layman.ToString())
            {
                // Add columns with appropriate types
                result.Columns.Add("C-ASIN", typeof(string));
                result.Columns.Add("Month", typeof(string));
                result.Columns.Add($"Requested_Quantity ({forecast1.Month})", typeof(int));
                result.Columns.Add($"Wip ({forecast1.Month})", typeof(int));
                result.Columns.Add($"Requested_Quantity ({forecast2.Month})", typeof(int));
                result.Columns.Add($"CommitmentPeriod ({forecast2.Month})", typeof(string));
                result.Columns.Add("Actual_Order", typeof(int));
                result.Columns.Add("Delta", typeof(int));
                result.Columns.Add("Initial_Stock", typeof(int));
                result.Columns.Add("Remaining", typeof(int));
                result.Columns.Add("Remaining_Layman", typeof(string));
                result.Columns.Add($"{wipType}({forecast2.Month})", typeof(int));
            }
            else if (wipType == WipType.Analyst.ToString())
            {
                // Add columns for the data table
                result.Columns.Add("C-ASIN", typeof(string));
                result.Columns.Add("Month", typeof(string));
                result.Columns.Add($"Requested_Quantity ({forecast1.Month})", typeof(int));
                result.Columns.Add($"Wip ({forecast1.Month})", typeof(int));
                result.Columns.Add($"Requested_Quantity ({forecast2.Month})", typeof(int));
                result.Columns.Add($"CommitmentPeriod ({forecast2.Month})", typeof(string));
                result.Columns.Add("Actual_Order", typeof(int));
                //result.Columns.Add("Delta", typeof(int));
                result.Columns.Add("Initial_Stock", typeof(int));
                result.Columns.Add("Remaining", typeof(int));
                result.Columns.Add($"{wipType}({forecast2.Month})", typeof(int));
            }

        }
        private Dictionary<string, int> GetForecastData(ForecastMaster forecast, string asin)
        {
            return forecast.Details
                .Where(d => d.CASIN == asin)
                .GroupBy(d => $"{d.Month} {d.Year}")
                .ToDictionary(g => g.Key, g => g.Sum(d => d.RequestedQuantity));
        }
        private Dictionary<DateTime, int> nGetForecastData(ForecastMaster forecast, string asin)
        {
            return forecast.Details
                           .Where(d => d.CASIN == asin)
                           .GroupBy(d => d.PODate)
                           .ToDictionary(
                               g => g.Key,
                               g => g.Sum(d => d.RequestedQuantity)
                           );
        }






        private string GetCommitmentPeriod(ForecastMaster forecast, string asin, string monthName)
        {
            //return forecast.Details.FirstOrDefault(d => d.CASIN == asin && $"{d.Month} {d.Year}" == monthName)?.CommitmentPeriod ?? "0";
            
            var CP = forecast.Details .FirstOrDefault(d => d.CASIN == asin && $"{d.Month} {d.Year}" == monthName)?.CommitmentPeriod;
            if (string.IsNullOrEmpty(CP))
            {
                CP = "0";
            }

            return CP;
        }

        private string GetCommitmentPeriod2(ForecastMaster forecast, string asin, DateTime podate)
        {
            //return forecast.Details.FirstOrDefault(d => d.CASIN == asin && $"{d.Month} {d.Year}" == monthName)?.CommitmentPeriod ?? "0";

            var CP = forecast.Details.FirstOrDefault(d => d.CASIN == asin && d.PODate == podate)?.CommitmentPeriod;
            if (string.IsNullOrEmpty(CP))
            {
                CP = "0";
            }

            return CP;
        }

        public int? CalculateRemainingLayman(DateTime monthDate, DateTime targetMonthDate, int qty2, int? previousRemainingStock, int previousRemainingLaymanStock, int initialStock)
        {
            int? remainingLayman = null;

            if (monthDate < targetMonthDate)
            {
                remainingLayman = null; // No remaining layman before the target month
            }
            else if (monthDate == targetMonthDate)
            {
                // Calculate remaining layman for the target month using previous remaining stock
                remainingLayman = previousRemainingStock.HasValue ? previousRemainingStock.Value - qty2 : initialStock - qty2;
            }
            else
            {
                // For subsequent months after the target month, subtract the requested quantity from the previous remaining layman stock
                remainingLayman = previousRemainingLaymanStock - qty2;
            }

            return remainingLayman;
        }
        private void xAddRowToDataTable(DataTable result, string asin, string monthName, int qty1, object wipOfForecast1, int qty2, string commitmentPeriod, object actualOrderVal, int delta, int currentStock, int remainingStock, object remainingLaymanValue, object wip, string wipType)
        {
            if (wipType == WipType.LaymanFormula.ToString() || wipType == WipType.Layman.ToString())
            {
                result.Rows.Add(asin, monthName, qty1, wipOfForecast1, qty2, commitmentPeriod, actualOrderVal, delta, currentStock, remainingStock, remainingLaymanValue, wip);
            }
            else if (wipType == WipType.Analyst.ToString())
            {
                //result.Rows.Add(asin, monthName, qty1, wipOfForecast1, qty2, commitmentPeriod, actualOrderVal, delta, currentStock, remainingStock, wip);
                result.Rows.Add(asin, monthName, qty1, wipOfForecast1, qty2, commitmentPeriod, actualOrderVal, currentStock, remainingStock, wip);
            }
        }

        #endregion wip helpers

        #region Calculate wip
        //LaymanFormula
        private int? CalculateLaymanFormulaWip(DateTime monthDate, DateTime targetMonthDate, int qty2, int? remainingLayman, ref bool hasStockShortfallOccurred)
        {
            int? LaymanFormulaWip = null;

            if (monthDate >= targetMonthDate)
            {
                // Check for the first shortfall occurrence (remainingLayman < 0) 
                if (!hasStockShortfallOccurred && remainingLayman.HasValue && remainingLayman.Value < 0)
                {
                    // First time shortfall occurred → WIP = exact shortfall (absolute)
                    LaymanFormulaWip = -remainingLayman.Value;  // Using absolute shortfall value
                    hasStockShortfallOccurred = true;
                }
                else if (hasStockShortfallOccurred)
                {
                    // After the first shortfall month, WIP = requested quantity for the month
                    if (monthDate > targetMonthDate)  // Starting from the month after the shortfall
                    {
                        LaymanFormulaWip = qty2;
                    }
                    else
                    {
                        // For the shortfall month itself, set WIP to absolute value of shortfall
                        if (remainingLayman.HasValue && remainingLayman.Value < 0)
                        {
                            LaymanFormulaWip = -remainingLayman.Value;  // Ensure it's the absolute shortfall value
                        }
                        else
                        {
                            LaymanFormulaWip = 0;  // Fallback to 0 if no shortfall (just a safety check)
                        }
                    }
                }
                else
                {
                    // No shortfall yet → No WIP
                    LaymanFormulaWip = 0;
                }
            }

            // Additional checks to ensure no unexpected negative values for WIP (optional, depends on your business rules)
            if (LaymanFormulaWip != null)
            {
                if (LaymanFormulaWip.HasValue && LaymanFormulaWip.Value < 0)
                {
                    LaymanFormulaWip = 0;  // Set to 0 if the WIP value becomes negative (business logic may vary)
                }
            }

            return LaymanFormulaWip;
        }
        //Layman
        private int? CalculateLaymanWip(DateTime monthDate, DateTime targetMonthDate, int qty2, int? remainingLayman, ref bool hasStockShortfallOccurred)
        {
            int? LaymanWip = null;

            if (monthDate >= targetMonthDate)
            {
                // Check for the first shortfall occurrence (remainingLayman < 0) 
                if (!hasStockShortfallOccurred && remainingLayman.HasValue && remainingLayman.Value < 0)
                {
                    // First time shortfall occurred → WIP = exact shortfall (absolute)
                    LaymanWip = -remainingLayman.Value;  // Using absolute shortfall value
                    hasStockShortfallOccurred = true;
                }
                else if (hasStockShortfallOccurred)
                {
                    // After the first shortfall month, WIP = requested quantity for the month
                    if (monthDate > targetMonthDate)  // Starting from the month after the shortfall
                    {
                        LaymanWip = qty2;
                    }
                    else
                    {
                        // For the shortfall month itself, set WIP to absolute value of shortfall
                        if (remainingLayman.HasValue && remainingLayman.Value < 0)
                        {
                            LaymanWip = -remainingLayman.Value;  // Ensure it's the absolute shortfall value
                        }
                        else
                        {
                            LaymanWip = 0;  // Fallback to 0 if no shortfall (just a safety check)
                        }
                    }
                }
                else
                {
                    // No shortfall yet → No WIP
                    LaymanWip = 0;
                }
            }

            // Additional checks to ensure no unexpected negative values for WIP (optional, depends on your business rules)
            if (LaymanWip.HasValue && LaymanWip.Value < 0)
            {
                LaymanWip = 0;  // Set to 0 if the WIP value becomes negative (business logic may vary)
            }
            return LaymanWip;
        }
        //Analyst
        private int? CalculateAnalystWip(string capacity, Dictionary<string, int> forecastDict2, DateTime targetMonthDate, int currentStock, int qty2, DateTime monthDate, int? percentage)
        {
            int? analystWip = null;
            if (monthDate >= targetMonthDate)
            {
                if (capacity == ProcessingWipType.MonthOfSupply.ToString())
                {
                    analystWip = CalculateHighCapacityWip(forecastDict2, targetMonthDate, currentStock, qty2, monthDate);
                }
                else if (capacity == ProcessingWipType.Percentage.ToString())
                {
                    analystWip = CalculateMediumCapacityWip(forecastDict2, targetMonthDate, currentStock, qty2, monthDate, percentage.Value);
                }
                else if (capacity == ProcessingWipType.System.ToString())
                {
                    analystWip = CalculateLowCapacityWip(forecastDict2, targetMonthDate, currentStock, qty2, monthDate);
                }
                else
                {
                    analystWip = 0;
                }
            }
            return analystWip;
        }

        private int? CalculateAnalystWip2(string capacity, Dictionary<DateTime, int> forecastDict2, DateTime targetMonthDate, int currentStock, int qty2, DateTime monthDate, int? percentage)
        {
            int? analystWip = null;
            if (monthDate >= targetMonthDate)
            {
                if (capacity == ProcessingWipType.MonthOfSupply.ToString())
                {
                    analystWip = CalculateHighCapacityWip2(forecastDict2, targetMonthDate, currentStock, qty2, monthDate);
                }
                else if (capacity == ProcessingWipType.Percentage.ToString())
                {
                    analystWip = CalculateMediumCapacityWip2(forecastDict2, targetMonthDate, currentStock, qty2, monthDate, percentage.Value);
                }
                else if (capacity == ProcessingWipType.System.ToString())
                {
                    analystWip = CalculateLowCapacityWip2(forecastDict2, targetMonthDate, currentStock, qty2, monthDate);
                }
                else
                {
                    analystWip = 0;
                }
            }
            return analystWip;
        }



        #region analyst helpers
        private int CalculateHighCapacityWip(Dictionary<string, int> forecastDict2, DateTime targetMonthDate, int initialStock, int qty2, DateTime monthDate)
        {
            int analystWip = 0;

            // For High Capacity: calculate value for target month and next month (December)
            int qty2TargetMonth = forecastDict2.TryGetValue(targetMonthDate.ToString("MMMM yyyy"), out int q2Nov) ? q2Nov : 0;
            int qty2NextToTargetMonth = forecastDict2.TryGetValue(targetMonthDate.AddMonths(1).ToString("MMMM yyyy"), out int q2Dec) ? q2Dec : 0;

            // Calculate value for November WIP
            int value = (qty2TargetMonth + qty2NextToTargetMonth) - initialStock;

            if (value > 0)
            {
                // WIP calculations for November and December
                if (monthDate == targetMonthDate)
                {
                    analystWip = value;  // November WIP
                }
                else if (monthDate == targetMonthDate.AddMonths(1))
                {
                    analystWip = 0;  // December WIP is 0
                }
                else
                {
                    analystWip = qty2;  // January WIP is same as qty2 of that month
                }
            }
            else
            {
                // If no shortfall, WIP for Nov and Dec are 0, and January takes qty2 for the month
                if (monthDate == targetMonthDate || monthDate == targetMonthDate.AddMonths(1))
                {
                    analystWip = 0;
                }
                else
                {
                    analystWip = qty2;  // January WIP is same as qty2 of that month
                }
            }

            return analystWip;
        }
        private int CalculateMediumCapacityWip(Dictionary<string, int> forecastDict2, DateTime targetMonthDate, int initialStock, int qty2, DateTime monthDate, int percentage)
        {
            int analystWip = 0;

            // Retrieve forecasted quantity for the target month
            int qty2TargetMonth = forecastDict2.TryGetValue(targetMonthDate.ToString("MMMM yyyy"), out int q2Target) ? q2Target : 0;

            // Special debug/log for qty2TargetMonth == 4540, but calculation stays the same
            if (qty2TargetMonth == 4540)
            {
                Console.WriteLine("Debug: qty2TargetMonth is exactly 4540");
                // You can add more debugging or special handling here if needed
            }

            // Perform the calculation for all cases including qty2TargetMonth == 4540
            int value = (qty2TargetMonth + (int)((percentage / 100.0) * qty2TargetMonth)) - initialStock;

            if (value > 0)
            {
                //if (monthDate == targetMonthDate)
                //{
                    analystWip = value;  // WIP for the target month
                //}
                //else
                //{
                //    analystWip = qty2;  // WIP for the next month
                //}
            }

            return analystWip;
        }
        private int CalculateLowCapacityWip(Dictionary<string, int> forecastDict, DateTime targetMonthDate, int initialStock, int qty2, DateTime monthDate)
        {
            int analystWip = 0;

            // Get the quantities for the target month, next month, and beyond
            int qty2TargetMonth = forecastDict.TryGetValue(targetMonthDate.ToString("MMMM yyyy"), out int q2Target) ? q2Target : 0;
            int qty2NextMonth = forecastDict.TryGetValue(targetMonthDate.AddMonths(1).ToString("MMMM yyyy"), out int q2Next) ? q2Next : 0;

            // Handle the case for the target month
            if (monthDate == targetMonthDate)
            {
                if (qty2TargetMonth > initialStock)
                {

                    analystWip = qty2TargetMonth - initialStock; // WIP is the difference when qty2 is greater than initial stock
                    initialStock = 0;
                }
                else
                {
                    analystWip = 0; // No WIP if qty2 is less than or equal to initial stock
                }
            }
            else
            {
                if (qty2NextMonth > initialStock)
                {
                    analystWip = qty2NextMonth - initialStock; // WIP is the difference when qty2 is greater than initial stock
                }
                else
                {
                    //analystWip = 0; // No WIP if qty2 is less than or equal to initial stock
                    if (initialStock == 0)
                    {
                        analystWip = qty2; // WIP equals requested quantity if there is no stock available
                    }
                    else
                    {
                        analystWip = 0; // No WIP if there is stock
                    }
                }
            }

            return analystWip;
        }
        #endregion analyst helpers

        #region analyst helpers
        private int CalculateHighCapacityWip2(Dictionary<DateTime, int> forecastDict2, DateTime targetMonthDate, int initialStock, int qty2, DateTime monthDate)
        {
            int analystWip = 0;

            // For High Capacity: calculate value for target month and next month (December)
            int qty2TargetMonth = forecastDict2.TryGetValue(targetMonthDate, out int q2Nov) ? q2Nov : 0;
            int qty2NextToTargetMonth = forecastDict2.TryGetValue(targetMonthDate.AddMonths(1), out int q2Dec) ? q2Dec : 0;

            // Calculate value for November WIP
            int value = (qty2TargetMonth + qty2NextToTargetMonth) - initialStock;

            if (value > 0)
            {
                // WIP calculations for November and December
                if (monthDate == targetMonthDate)
                {
                    analystWip = value;  // November WIP
                }
                else if (monthDate == targetMonthDate.AddMonths(1))
                {
                    analystWip = 0;  // December WIP is 0
                }
                else
                {
                    analystWip = qty2;  // January WIP is same as qty2 of that month
                }
            }
            else
            {
                // If no shortfall, WIP for Nov and Dec are 0, and January takes qty2 for the month
                if (monthDate == targetMonthDate || monthDate == targetMonthDate.AddMonths(1))
                {
                    analystWip = 0;
                }
                else
                {
                    analystWip = qty2;  // January WIP is same as qty2 of that month
                }
            }

            return analystWip;
        }
        private int CalculateMediumCapacityWip2(Dictionary<DateTime, int> forecastDict2, DateTime targetMonthDate, int initialStock, int qty2, DateTime monthDate, int percentage)
        {
            int analystWip = 0;

            // Retrieve forecasted quantity for the target month
            //int qty2TargetMonth = forecastDict2.TryGetValue(targetMonthDate, out int q2Nov) ? q2Nov : 0;
            int qty2TargetMonth = forecastDict2
                            .Where(kvp => kvp.Key.ToString("MMMM yyyy") == targetMonthDate.ToString("MMMM yyyy"))
                            .Select(kvp => kvp.Value)
                            .FirstOrDefault();


            


            // Special debug/log for qty2TargetMonth == 4540, but calculation stays the same
            if (qty2TargetMonth == 4540)
            {
                Console.WriteLine("Debug: qty2TargetMonth is exactly 4540");
                // You can add more debugging or special handling here if needed
            }

            // Perform the calculation for all cases including qty2TargetMonth == 4540
            int value = (qty2TargetMonth + (int)((percentage / 100.0) * qty2TargetMonth)) - initialStock;

            if (value > 0)
            {
                //if (monthDate == targetMonthDate)
                //{
                analystWip = value;  // WIP for the target month
                //}
                //else
                //{
                //    analystWip = qty2;  // WIP for the next month
                //}
            }

            return analystWip;
        }
        private int CalculateLowCapacityWip2(Dictionary<DateTime, int> forecastDict, DateTime targetMonthDate, int initialStock, int qty2, DateTime monthDate)
        {
            int analystWip = 0;

            // Get the quantities for the target month, next month, and beyond
            int qty2TargetMonth = forecastDict.TryGetValue(targetMonthDate, out int q2Nov) ? q2Nov : 0;
            int qty2NextMonth = forecastDict.TryGetValue(targetMonthDate.AddMonths(1), out int q2Dec) ? q2Dec : 0;

            // Handle the case for the target month
            if (monthDate == targetMonthDate)
            {
                if (qty2TargetMonth > initialStock)
                {

                    analystWip = qty2TargetMonth - initialStock; // WIP is the difference when qty2 is greater than initial stock
                    initialStock = 0;
                }
                else
                {
                    analystWip = 0; // No WIP if qty2 is less than or equal to initial stock
                }
            }
            else
            {
                if (qty2NextMonth > initialStock)
                {
                    analystWip = qty2NextMonth - initialStock; // WIP is the difference when qty2 is greater than initial stock
                }
                else
                {
                    //analystWip = 0; // No WIP if qty2 is less than or equal to initial stock
                    if (initialStock == 0)
                    {
                        analystWip = qty2; // WIP equals requested quantity if there is no stock available
                    }
                    else
                    {
                        analystWip = 0; // No WIP if there is stock
                    }
                }
            }

            return analystWip;
        }
        #endregion analyst helpers



        #endregion Calculate wip
        #endregion  wip table 

        #region save

        public async Task<Response<bool>> xSaveWipRecordsAsync3(DataTable dataTable,string fileName,string wipType,string capacity, string targetMonth, string wipColName)
        {
            var response = new Response<bool>();

            try
            {
                var targetParts = (targetMonth ?? string.Empty).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                string targetMonthName = targetParts.Length > 0 ? targetParts[0] : DateTime.Now.ToString("MMMM");
                int targetYear = (targetParts.Length > 1 && int.TryParse(targetParts[1], out var ty)) ? ty : DateTime.Now.Year;

                int? globalMoqQty = null;
                bool globalIsCasePack = false;

                if (dataTable.Columns.Contains("MOQ"))
                {
                    var rowWithMoq = dataTable.AsEnumerable().FirstOrDefault(r => r["MOQ"] != DBNull.Value);
                    int parsedMoq;
                    if (rowWithMoq != null && int.TryParse(rowWithMoq["MOQ"]?.ToString()?.Trim(), out parsedMoq))
                        globalMoqQty = parsedMoq;
                }

                if (dataTable.Columns.Contains("CasePack"))
                    globalIsCasePack = dataTable.AsEnumerable().Any(r => r["CasePack"] != DBNull.Value);

                var newDetails = new List<WipDetail>(dataTable.Rows.Count);
                var now = DateTime.Now;

                foreach (DataRow row in dataTable.Rows)
                {
                    string casin = row["C-Asin"]?.ToString()?.Trim();
                    string month = row["Month"]?.ToString()?.Trim();
                    string year = row["Year"]?.ToString()?.Trim();
                    string stockS = row["Stock"]?.ToString()?.Trim();
                    string cPeriod = row["Commitment_Period"]?.ToString()?.Trim();

                    if (string.IsNullOrEmpty(month) || string.IsNullOrEmpty(year) ||
                        string.IsNullOrEmpty(casin) || string.IsNullOrEmpty(stockS))
                    {
                        return new Response<bool>
                        {
                            Success = false,
                            Status = StatusType.Error,
                            Message = "Invalid data: One or more required fields are missing.",
                            Data = false
                        };
                    }

                    int parsedStock;
                    int? stockQty = int.TryParse(stockS, out parsedStock) ? (int?)parsedStock : null;

                    int? reviewWipQty = dataTable.Columns.Contains("Review_Wip") ? TryParseNullableInt(row["Review_Wip"]) : null;
                    int? moqWipQty = dataTable.Columns.Contains("MOQ_Wip") ? TryParseNullableInt(row["MOQ_Wip"]) : null;
                    int? casePackWipQty = dataTable.Columns.Contains("CasePack_Wip") ? TryParseNullableInt(row["CasePack_Wip"]) : null;
                    int? casePackQty = dataTable.Columns.Contains("CasePack") ? TryParseNullableInt(row["CasePack"]) : null;

                    int? wipQty;
                    switch (wipColName)
                    {
                        case "CasePack_Wip": wipQty = casePackWipQty; break;
                        case "MOQ_Wip": wipQty = moqWipQty; break;
                        case "Review_Wip": wipQty = reviewWipQty; break;
                        default: wipQty = null; break;
                    }

                    var detail = new WipDetail
                    {
                        CASIN = casin,
                        Month = month,
                        Year = year,
                        WipQuantity = wipQty,
                        Stock = stockQty,
                        CommitmentPeriod = cPeriod,
                        LaymanFormula = wipType == WipType.LaymanFormula.ToString() ? wipQty : (int?)null,
                        Layman = wipType == WipType.Layman.ToString() ? wipQty : (int?)null,
                        Analyst = wipType == WipType.Analyst.ToString() ? wipQty : (int?)null,
                        Review_Wip = reviewWipQty,
                        MOQ_Wip = moqWipQty,
                        CasePack_Wip = casePackWipQty,
                        CasePack = casePackQty,
                        PODate = now
                    };

                    newDetails.Add(detail);
                }

                var casins = newDetails.Select(d => d.CASIN).Distinct().ToList();
                var months = newDetails.Select(d => d.Month).Distinct().ToList();
                var years = newDetails.Select(d => d.Year).Distinct().ToList();

                var newByKey = newDetails
                    .GroupBy(d => d.CASIN + "|" + d.Month + "|" + d.Year)
                    .ToDictionary(g => g.Key, g => g.Last());

                using (var context = new WIPATContext())
                {
                    // EF6 knobs
                    context.Database.CommandTimeout = 180; // seconds
                    var previousDetect = context.Configuration.AutoDetectChangesEnabled;
                    context.Configuration.AutoDetectChangesEnabled = false;

                    using (var tx = context.Database.BeginTransaction())
                    {
                        try
                        {
                            // Forecast master (no tracking)
                            var forecastMaster = await context.ForecastMasters
                                .AsNoTracking()
                                .FirstOrDefaultAsync(fm => fm.FileName == fileName);

                            if (forecastMaster == null)
                            {
                                context.Configuration.AutoDetectChangesEnabled = previousDetect;
                                return new Response<bool>
                                {
                                    Success = false,
                                    Status = StatusType.Error,
                                    Message = "POForecastMaster not found for file '" + fileName + "'.",
                                    Data = false
                                };
                            }

                            var forecastMasterId = forecastMaster.Id;

                            // Pull relevant forecast detail keys
                            var forecastDetails = await context.ForecastDetails
                                .Where(fd => fd.POForecastMasterId == forecastMasterId
                                             && casins.Contains(fd.CASIN)
                                             && months.Contains(fd.Month)
                                             && years.Contains(fd.Year))
                                .Select(fd => new { fd.Id, fd.CASIN, fd.Month, fd.Year })
                                .AsNoTracking()
                                .ToListAsync();

                            // Update only Wip via stubs (mark property Modified in EF6)
                            foreach (var fd in forecastDetails)
                            {
                                var key = fd.CASIN + "|" + fd.Month + "|" + fd.Year;
                                WipDetail nd;
                                if (newByKey.TryGetValue(key, out nd))
                                {
                                    var stub = new ForecastDetail { Id = fd.Id };
                                    context.ForecastDetails.Attach(stub);
                                    stub.Wip = nd.WipQuantity;
                                    context.Entry(stub).Property(x => x.Wip).IsModified = true;
                                }
                            }

                            // Upsert/merge WipMaster for this TargetMonth
                            var existingWipMaster = await context.WipMasters
                                .FirstOrDefaultAsync(wm => wm.FileName == fileName && wm.TargetMonth == targetMonthName);

                            if (existingWipMaster == null)
                            {
                                existingWipMaster = new WipMaster
                                {
                                    FileName = fileName,
                                    IssuedMonth = newDetails.First().Month,
                                    TargetMonth = targetMonthName,
                                    Type = wipType,
                                    WipProcessingType = capacity,
                                    MOQ = globalMoqQty,
                                    IsCasePackChecked = globalIsCasePack
                                };

                                context.WipMasters.Add(existingWipMaster);
                                await context.SaveChangesAsync(); // to get Id/state

                                // Assign via navigation (works even if no FK property exists)
                                foreach (var nd in newDetails)
                                {
                                    nd.WipMaster_Id = existingWipMaster.Id;
                                }

                                context.WipDetails.AddRange(newDetails);
                            }
                            else
                            {
                                // Update master props
                                existingWipMaster.MOQ = globalMoqQty;
                                existingWipMaster.IsCasePackChecked = globalIsCasePack;
                                existingWipMaster.WipProcessingType = capacity;
                                existingWipMaster.Type = wipType;

                                // Existing detail keys (relevant only)
                                var existingDetailKeys = await context.WipDetails
                                    .Where(d => d.WipMaster_Id == existingWipMaster.Id
                                                && casins.Contains(d.CASIN)
                                                && months.Contains(d.Month)
                                                && years.Contains(d.Year))
                                    .Select(d => new { d.Id, d.CASIN, d.Month, d.Year })
                                    .AsNoTracking()
                                    .ToListAsync();

                                var existingMap = existingDetailKeys.ToDictionary(
                                    k => k.CASIN + "|" + k.Month + "|" + k.Year,
                                    v => v.Id);

                                var toInsert = new List<WipDetail>();

                                foreach (var nd in newDetails)
                                {
                                    var k = nd.CASIN + "|" + nd.Month + "|" + nd.Year;
                                    int existingId;
                                    if (existingMap.TryGetValue(k, out existingId))
                                    {
                                        var stub = new WipDetail { Id = existingId };
                                        context.WipDetails.Attach(stub);

                                        // set fields
                                        stub.WipQuantity = nd.WipQuantity;
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

                                        // mark modified (EF6)
                                        var e = context.Entry(stub);
                                        e.Property(x => x.WipQuantity).IsModified = true;
                                        e.Property(x => x.Stock).IsModified = true;
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
                                    else
                                    {
                                        // new row — link by navigation (no FK needed)
                                        nd.WipMaster_Id = existingWipMaster.Id;      
                                        toInsert.Add(nd);
                                    }
                                }

                                if (toInsert.Count > 0)
                                    context.WipDetails.AddRange(toInsert);
                            }

                            // Mark forecast master as calculated (stub + Modified)
                            var fmStub = new ForecastMaster { Id = forecastMasterId, IsWipCalculated = true };
                            context.ForecastMasters.Attach(fmStub);
                            context.Entry(fmStub).Property(x => x.IsWipCalculated).IsModified = true;

                            await context.SaveChangesAsync();
                            tx.Commit();

                            context.Configuration.AutoDetectChangesEnabled = previousDetect;

                            return new Response<bool>
                            {
                                Success = true,
                                Data = true,
                                Status = StatusType.Success,
                                Message = "WIP Master & details saved, and POForecastDetail WIP updated (bulk, EF6)."
                            };
                        }
                        catch (Exception)
                        {
                            tx.Rollback();
                            context.Configuration.AutoDetectChangesEnabled = previousDetect;
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new Response<bool>
                {
                    Success = false,
                    Data = false,
                    Status = StatusType.Error,
                    Message = "Exception: " + ex.Message
                };
            }

            // local helper (C# 7.3)
            int? TryParseNullableInt(object o)
            {
                if (o == null || o == DBNull.Value) return null;
                int v;
                return int.TryParse(o.ToString()?.Trim(), out v) ? (int?)v : null;
            }
        }
        public async Task<Response<bool>> SaveWipRecordsAsync3(
    DataTable dataTable,
    string fileName,
    string wipType,
    string capacity,
    string targetMonth,
    string wipColName)
        {
            var response = new Response<bool>();

            try
            {
                // ----- Parse target month & year (robust to "May 2025" or just "May") -----
                var targetParts = (targetMonth ?? string.Empty)
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                string targetMonthName = targetParts.Length > 0 ? targetParts[0] : DateTime.Now.ToString("MMMM");
                int targetYear = (targetParts.Length > 1 && int.TryParse(targetParts[1], out var ty)) ? ty : DateTime.Now.Year;

                // ----- Optional global flags read once from table -----
                int? globalMoqQty = null;
                bool globalIsCasePack = false;

                if (dataTable.Columns.Contains("MOQ"))
                {
                    var rowWithMoq = dataTable.AsEnumerable().FirstOrDefault(r => r["MOQ"] != DBNull.Value);
                    if (rowWithMoq != null && int.TryParse(rowWithMoq["MOQ"]?.ToString()?.Trim(), out var parsedMoq))
                        globalMoqQty = parsedMoq;
                }

                if (dataTable.Columns.Contains("CasePack"))
                    globalIsCasePack = dataTable.AsEnumerable().Any(r => r["CasePack"] != DBNull.Value);

                var newDetails = new List<WipDetail>(dataTable.Rows.Count);
                var now = DateTime.Now;

                // ----- Build raw details (may contain duplicates by business key) -----
                foreach (DataRow row in dataTable.Rows)
                {
                    string casin = row["C-Asin"]?.ToString()?.Trim();
                    string month = row["Month"]?.ToString()?.Trim();
                    string year = row["Year"]?.ToString()?.Trim();
                    string stockS = row["Stock"]?.ToString()?.Trim();
                    string cPeriod = row["Commitment_Period"]?.ToString()?.Trim();

                    if (string.IsNullOrEmpty(month) || string.IsNullOrEmpty(year) ||
                        string.IsNullOrEmpty(casin) || string.IsNullOrEmpty(stockS))
                    {
                        return new Response<bool>
                        {
                            Success = false,
                            Status = StatusType.Error,
                            Message = "Invalid data: One or more required fields are missing.",
                            Data = false
                        };
                    }

                    int? stockQty = int.TryParse(stockS, out var parsedStock) ? (int?)parsedStock : null;

                    int? reviewWipQty = dataTable.Columns.Contains("Review_Wip") ? TryParseNullableInt(row["Review_Wip"]) : null;
                    int? moqWipQty = dataTable.Columns.Contains("MOQ_Wip") ? TryParseNullableInt(row["MOQ_Wip"]) : null;
                    int? casePackWipQty = dataTable.Columns.Contains("CasePack_Wip") ? TryParseNullableInt(row["CasePack_Wip"]) : null;
                    int? casePackQty = dataTable.Columns.Contains("CasePack") ? TryParseNullableInt(row["CasePack"]) : null;

                    int? wipQty = null;
                    switch (wipColName)
                    {
                        case "CasePack_Wip": wipQty = casePackWipQty; break;
                        case "MOQ_Wip": wipQty = moqWipQty; break;
                        case "Review_Wip": wipQty = reviewWipQty; break;
                    }

                    var detail = new WipDetail
                    {
                        CASIN = casin,
                        Month = month,
                        Year = year,
                        WipQuantity = wipQty,
                        Stock = stockQty,
                        CommitmentPeriod = cPeriod,
                        LaymanFormula = wipType == WipType.LaymanFormula.ToString() ? wipQty : (int?)null,
                        Layman = wipType == WipType.Layman.ToString() ? wipQty : (int?)null,
                        Analyst = wipType == WipType.Analyst.ToString() ? wipQty : (int?)null,
                        Review_Wip = reviewWipQty,
                        MOQ_Wip = moqWipQty,
                        CasePack_Wip = casePackWipQty,
                        CasePack = casePackQty,
                        PODate = now
                    };

                    newDetails.Add(detail);
                }

                // ----- Deduplicate by business key CASIN|Month|Year (keep last row for that key) -----
                var newByKey = newDetails
                    .GroupBy(d => d.CASIN + "|" + d.Month + "|" + d.Year)
                    .ToDictionary(g => g.Key, g => g.Last());

                var dedupedDetails = newByKey.Values.ToList();

                // These drive all DB queries to keep sets tight & consistent
                var casins = dedupedDetails.Select(d => d.CASIN).Distinct().ToList();
                var months = dedupedDetails.Select(d => d.Month).Distinct().ToList();
                var years = dedupedDetails.Select(d => d.Year).Distinct().ToList();

                using (var context = new WIPATContext())
                {
                    // EF6 knobs
                    context.Database.CommandTimeout = 180; // seconds
                    var previousDetect = context.Configuration.AutoDetectChangesEnabled;
                    context.Configuration.AutoDetectChangesEnabled = false;

                    using (var tx = context.Database.BeginTransaction())
                    {
                        try
                        {
                            // ----- Forecast master (no tracking) -----
                            var forecastMaster = await context.ForecastMasters
                                .AsNoTracking()
                                .FirstOrDefaultAsync(fm => fm.FileName == fileName);

                            if (forecastMaster == null)
                            {
                                context.Configuration.AutoDetectChangesEnabled = previousDetect;
                                return new Response<bool>
                                {
                                    Success = false,
                                    Status = StatusType.Error,
                                    Message = $"POForecastMaster not found for file '{fileName}'.",
                                    Data = false
                                };
                            }

                            var forecastMasterId = forecastMaster.Id;

                            // ----- Update ForecastDetails.Wip in bulk via stubs (limited to relevant keys) -----
                            var forecastDetails = await context.ForecastDetails
                                .Where(fd => fd.POForecastMasterId == forecastMasterId
                                             && casins.Contains(fd.CASIN)
                                             && months.Contains(fd.Month)
                                             && years.Contains(fd.Year))
                                .Select(fd => new { fd.Id, fd.CASIN, fd.Month, fd.Year })
                                .AsNoTracking()
                                .ToListAsync();

                            foreach (var fd in forecastDetails)
                            {
                                var key = fd.CASIN + "|" + fd.Month + "|" + fd.Year;
                                if (newByKey.TryGetValue(key, out var nd))
                                {
                                    var stub = new ForecastDetail { Id = fd.Id };
                                    context.ForecastDetails.Attach(stub);
                                    stub.Wip = nd.WipQuantity;
                                    context.Entry(stub).Property(x => x.Wip).IsModified = true;
                                }
                            }

                            // ----- Upsert/merge WipMaster for this TargetMonth -----
                            var existingWipMaster = await context.WipMasters
                                .FirstOrDefaultAsync(wm => wm.FileName == fileName && wm.TargetMonth == targetMonthName);

                            if (existingWipMaster == null)
                            {
                                existingWipMaster = new WipMaster
                                {
                                    FileName = fileName,
                                    IssuedMonth = dedupedDetails.First().Month,
                                    TargetMonth = targetMonthName,
                                    Type = wipType,
                                    WipProcessingType = capacity,
                                    MOQ = globalMoqQty,
                                    IsCasePackChecked = globalIsCasePack
                                };

                                context.WipMasters.Add(existingWipMaster);
                                await context.SaveChangesAsync(); // get Id for FK

                                foreach (var nd in dedupedDetails)
                                    nd.WipMaster_Id = existingWipMaster.Id;

                                context.WipDetails.AddRange(dedupedDetails);
                            }
                            else
                            {
                                // Update master props
                                existingWipMaster.MOQ = globalMoqQty;
                                existingWipMaster.IsCasePackChecked = globalIsCasePack;
                                existingWipMaster.WipProcessingType = capacity;
                                existingWipMaster.Type = wipType;

                                // Pull existing detail keys (only relevant subset)
                                var existingDetailKeys = await context.WipDetails
                                    .Where(d => d.WipMaster_Id == existingWipMaster.Id
                                                && casins.Contains(d.CASIN)
                                                && months.Contains(d.Month)
                                                && years.Contains(d.Year))
                                    .Select(d => new { d.Id, d.CASIN, d.Month, d.Year })
                                    .AsNoTracking()
                                    .ToListAsync();

                                var existingMap = existingDetailKeys.ToDictionary(
                                    k => k.CASIN + "|" + k.Month + "|" + k.Year,
                                    v => v.Id);

                                var toInsert = new List<WipDetail>();
                                var processedExistingIds = new HashSet<int>(); // guard double Attach on same PK

                                foreach (var nd in dedupedDetails)
                                {
                                    var k = nd.CASIN + "|" + nd.Month + "|" + nd.Year;

                                    if (existingMap.TryGetValue(k, out var existingId))
                                    {
                                        // Update existing row via stub
                                        if (processedExistingIds.Add(existingId))
                                        {
                                            var stub = new WipDetail { Id = existingId };
                                            context.WipDetails.Attach(stub);

                                            // set fields
                                            stub.WipQuantity = nd.WipQuantity;
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

                                            var e = context.Entry(stub);
                                            e.Property(x => x.WipQuantity).IsModified = true;
                                            e.Property(x => x.Stock).IsModified = true;
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
                                        // else: already updated this PK within this loop
                                    }
                                    else
                                    {
                                        // brand-new detail
                                        nd.WipMaster_Id = existingWipMaster.Id;
                                        toInsert.Add(nd);
                                    }
                                }

                                if (toInsert.Count > 0)
                                    context.WipDetails.AddRange(toInsert);
                            }

                            // ----- Mark ForecastMaster as WIP calculated -----
                            var fmStub = new ForecastMaster { Id = forecastMasterId, IsWipCalculated = true };
                            context.ForecastMasters.Attach(fmStub);
                            context.Entry(fmStub).Property(x => x.IsWipCalculated).IsModified = true;

                            await context.SaveChangesAsync();
                            tx.Commit();
                            context.Configuration.AutoDetectChangesEnabled = previousDetect;

                            return new Response<bool>
                            {
                                Success = true,
                                Data = true,
                                Status = StatusType.Success,
                                Message = "WIP Master & details saved, and POForecastDetail WIP updated (bulk, EF6)."
                            };
                        }
                        catch (Exception)
                        {
                            tx.Rollback();
                            context.Configuration.AutoDetectChangesEnabled = previousDetect;
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new Response<bool>
                {
                    Success = false,
                    Data = false,
                    Status = StatusType.Error,
                    Message = "Exception: " + ex.Message
                };
            }

            // ----- Local helper (C# 7.3) -----
            int? TryParseNullableInt(object o)
            {
                if (o == null || o == DBNull.Value) return null;
                return int.TryParse(o.ToString()?.Trim(), out var v) ? (int?)v : null;
            }
        }



        #endregion save

        private int CalculateRemainingStock(DateTime monthDate, DateTime targetMonthDate, int currentStock, int qty2, int? wipOfForecast1, int delta, bool isFirstMonth, int actualOrder, ref bool isFirstMonthFlag, string wipType)
        {
            int remainingStock;

            //if (isFirstMonth) // For the first month
            //{
            //    remainingStock = currentStock - actualOrder - delta;
            //    isFirstMonthFlag = false; // Set flag to false after the first month
            //}
            if (isFirstMonth) // For the first month
            {
                if (wipOfForecast1 == null)
                {
                    remainingStock = currentStock - actualOrder - delta;
                    isFirstMonthFlag = false; // Set flag to false after the first month
                }
                else
                {
                    remainingStock = currentStock - actualOrder + wipOfForecast1.Value;
                    isFirstMonthFlag = false; // Set flag to false after the first month
                }

            }
            else // For subsequent months
            {
                if (monthDate < targetMonthDate && wipOfForecast1.HasValue)
                {
                    // If the month is before the target month, use WIP from forecast
                    remainingStock = currentStock + wipOfForecast1.Value - qty2;
                }
                else
                {
                    // For other months, subtract delta
                    remainingStock = currentStock - delta;
                }
            }

            if (wipType == WipType.Layman.ToString() || wipType == WipType.Analyst.ToString())
            {
                // Cap Remaining at 0 (physical stock cannot be negative)
                if (remainingStock < 0)
                {
                    remainingStock = 0;
                }
            }

            return remainingStock;
        }


    }
}
