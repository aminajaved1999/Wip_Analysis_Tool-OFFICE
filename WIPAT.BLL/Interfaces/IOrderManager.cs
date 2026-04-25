using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using WIPAT.Entities;
using WIPAT.Entities.BO;
using WIPAT.Entities.Dto;
using WIPAT.Entities.Entities;

namespace WIPAT.BLL.Interfaces
{
    public interface IOrderManager
    {
        Task<Response<OrderFileResponse>> HandleOrderFileAsync(string filePath, WipSession session);
        Task<Response<DataTable>> LoadExistingOrderAsync(string month, string year);

        Task<Response<OrderFileResponse>> ValidateOrderAsync(string filePath, OrderMasterDto masterInfo);
        Task<Response<bool>> ConfirmOrderAsync(OrderMasterDto masterInfo, List<ValidOrder> validItems, WipSession session);

        Task<Response<bool>> RunFillAndKillAsync(OrderMasterDto masterInfo, List<ValidOrder> validItems, WipSession session);

    }
}