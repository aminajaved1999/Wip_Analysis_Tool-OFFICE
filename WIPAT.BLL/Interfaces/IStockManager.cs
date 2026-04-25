using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using WIPAT.Entities;
using WIPAT.Entities.BO;
using WIPAT.Entities.Dto;

namespace WIPAT.BLL.Interfaces
{
    public interface IStockManager
    {
        Task<Response<StockFileResponse>> HandleStockFileAsync(string filePath, WipSession session);
    }
}