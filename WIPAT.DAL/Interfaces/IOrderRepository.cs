using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WIPAT.Entities;
using WIPAT.Entities.Dto;
using WIPAT.Entities.Entities;

namespace WIPAT.DAL.Interfaces
{
    public interface IOrderRepository
    {
        Response<List<ActualOrder>> GetActualOrdersFromDatabase();
        Response<DataTable> GetOrderDataByMonthYear(string month, string year);
        Task<Response<bool>> SaveOrdersAndUpdateStock(List<ActualOrder> orders);
        Task<Response<ActualOrder>> OrderFileExists(string fileName, string requiredMonth, string requiredYear);
        Response<Tuple<DataTable, List<ActualOrder>>> GetExistingOrderData(string fileName, string month, string year);

        Response<bool> IsDocNoExists(string docNo, string docType);
        Task<Response<bool>> ExecuteOrderInsertion(OrderMaster master, List<OrderDetail> details);
    }
}
