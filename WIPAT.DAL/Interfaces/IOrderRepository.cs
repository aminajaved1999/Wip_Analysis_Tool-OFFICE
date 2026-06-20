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
        #region Read Operations
        Response<List<ActualOrder>> GetActualOrdersFromDatabase();
        Response<DataTable> GetOrderDataByMonthYear(string month, string year);
        Response<Tuple<DataTable, List<ActualOrder>>> GetExistingOrderData(string fileName, string month, string year);
        #endregion Read Operations

        #region Existence Checks
        Task<Response<ActualOrder>> OrderFileExists(string fileName, string requiredMonth, string requiredYear);

        Response<bool> IsDocNoExists(string docNo, string docType);

        #endregion Existence Checks

        #region Write Operations 
        Task<Response<bool>> SaveOrders(List<ActualOrder> orders);

        Task<Response<bool>> BulkInsertOrders(DataTable bulkTable);

        Task<Response<bool>> ExecuteOrderInsertion(OrderMaster master, List<OrderDetail> details);
    }
    #endregion Write Operations 



}
