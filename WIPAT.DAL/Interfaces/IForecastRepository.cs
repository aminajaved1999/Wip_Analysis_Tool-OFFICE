using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WIPAT.Entities;
using WIPAT.Entities.Dto;

namespace WIPAT.DAL.Interfaces
{
    public interface IForecastRepository
    {
        Response<string> SaveForecastDataToDatabase(ForecastFileData forecastData, bool isFirstFile);
        Response<ForecastFileData> IsFileAlreadyImported(string fileName);
        bool IsProjectionAlreadyExists(string month, string year);
        bool IsWipAlreadyCalculated(string month, string year);
        Response<ForecastCheckResult> PerformForecastChecks2(string fileName, string month, string year);
        Response<bool> CheckIfWipCalculated(string month, string year);
        Response<Tuple<DataTable, ForecastMaster>> GetForecastDataFromDB(string month, string year);
        Response<bool> MarkForecastMasterAsWIPCalculated(string fileName);
        ForecastMaster GetForecastMasterByFile(string fileName, string month, string year);
        Response<List<ForecastMaster>> GetAvailableForecastsFromDB();
    }
}
