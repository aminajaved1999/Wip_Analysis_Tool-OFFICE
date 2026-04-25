using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WIPAT.Entities;
using WIPAT.Entities.Dto;

namespace WIPAT.BLL.Interfaces
{
    public interface IForecastManager
    {
        Task<Response<List<ForecastFileData>>> HandleForecastFileAsync(string filePath, List<ForecastFileData> currentSessionFiles, int commitmentPeriod, WipSession session);
        Task<Response<ForecastFileData>> GetForecastFilePreviewAsync(string filePath, int commitmentPeriod);
        Task<Response<ForecastFileData>> LoadExistingForecastAsync(string month, string year);
    }
}
