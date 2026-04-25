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
    public interface IStockRepository
    {
        bool SaveInitialStocksToDatabase(List<InitialStock> stocks);
        Task<Response<bool>> UpdateStockQuantitiesAsync(List<InitialStock> stocks);
        Task<Response<bool>> UpdateStockQtyInStockTable(DataTable stockDataTable, string wipColName, string month, string year);
        int GetInitialStockValue(int itemCatalogueId);

        Response<bool> BulkInsertCatalogueImport(DataTable dtItemCatalogues, DataTable dtInitialStock);
    }
}
