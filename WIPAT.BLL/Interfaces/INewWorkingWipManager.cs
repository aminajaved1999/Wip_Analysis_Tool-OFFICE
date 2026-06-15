using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using WIPAT.Entities;
using WIPAT.Entities.Dto;

namespace WIPAT.BLL.Interfaces
{
    public interface INewWorkingWipManager
    {

        // <summary>
        /// Builds the main Work-in-Progress (WIP) data table for a list of ASINs.
        /// It simulates stock, orders, and forecasts month-by-month
        /// and applies final MOQ (Minimum Order Quantity) and CasePack adjustments.
        /// </summary>
        Response<DataTable> BuildCommonWipDataTable(
            List<string> asinList,
            ForecastMaster previousForecast,
            ForecastMaster currentForecast,
            string targetMonth,
            string wipType,
            List<ItemCatalogue> itemsCatalogueData,
            int? moq,
            bool isCasePackEnabled,
            string capacity = null,
            int? percentage = null);

        /// <summary>
        /// Retrieves all forecast details for a specific ASIN, month, and year.
        /// If no details are found, it generates a list of "default" details (CP 0-6) with zero values.
        /// </summary>
        /// <returns>A list of <see cref="ForecastDetail"/> objects.</returns>
        List<ForecastDetail> GetForecastDetails(
            string casin,
            string month,
            string year);


        Task<Response<bool>> SaveWipRecordsAsync(DataTable finalDataTable, string capacity, string wipColName, DataTable stockDataTable, WipSession wipSession);

    }
}