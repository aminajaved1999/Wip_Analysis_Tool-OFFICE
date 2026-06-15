using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using WIPAT.Entities;
using WIPAT.Entities.Dto;

namespace WIPAT.BLL.Interfaces
{
    public interface INewWorkingWipManager
    {

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

        List<ForecastDetail> GetForecastDetails(string casin, string month, string year);

        Task<Response<bool>> SaveWipRecordsAsync(DataTable finalDataTable, string capacity, string wipColName, DataTable stockDataTable, WipSession wipSession);

    }
}