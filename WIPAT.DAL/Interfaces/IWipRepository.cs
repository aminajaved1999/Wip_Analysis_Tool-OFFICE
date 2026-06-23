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
    public interface IWipRepository
    {
        Response<bool> CheckIfWipCalculated(string month, string year);
        Response<List<WipMaster>> GetAvailableWipPeriods();
        Task<Response<List<WipDetail>>> GetWipDetailsByPeriodAsync(string month, string yearString);
        Task<Response<object>> AddUserWipQtyForPeriodAsync(string month, string year, List<WipDetail> updates, int loggedInUserId);
        Task<Response<object>> UpdateWipForPeriodAsync(string month, string year, List<WipDetail> updates, int loggedInUserId);
        Task<Response<bool>> SaveWipRecordsTransactionAsync(
        string fileName, string issuedMonthName, int issuedYear, string targetMonthName, int targetYear,
        string wipType, string capacity, int? globalMoq, bool globalIsCasePack, int loggedInUserId,
        List<WipDetail> dedupedDetails, DataTable stockDataTable, string wipColName, string sessionMonth, string sessionYear);
    }
}
