using System;
using System.Collections.Generic;
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
        Response<List<ForecastMaster>> GetForecastsWithCalculatedWip();
        Task<Response<List<WipDetail>>> GetWipDetailsByPeriodAsync(string month, string yearString);
        Task<Response<object>> AddUserWipQtyForPeriodAsync(string month, string year, List<WipDetail> updates);
        Task<Response<object>> UpdateWipForPeriodAsync(string month, string year, List<WipDetail> updates);
    }
}
