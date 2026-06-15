using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WIPAT.Entities;
using WIPAT.Entities.Dto;

namespace WIPAT.DAL.Interfaces
{
    public interface IItemsRepository
    {
        //Get Methods
        Task<Response<List<ItemCatalogueDto>>> GetItemCataloguesWithStock();
        Task<Response<List<ItemCatalogue>>> GetActiveItemCatalogues(bool includeInactive = false);
        int GetItemIdByAsin(string asin);
        ItemCatalogue GetItemByCAsin(string casin);
        Task<Dictionary<string, int>> GetCatalogueIdsByCasinsAsync(List<string> casins);

        //Check / Validation Methods
        bool IsCAsinExistInCatalogue(string casin);
        Task<bool?> CheckCAsinStatus(string casin);
        Task<Response<bool>> IsCasinExistInCatalogueAndInitialStock(string casin);
        Task<Response<bool>> IsItemExistInCatalogue(string asin);
        //Add / Update Methods
        bool AddItemToCatalogue(string casin, string itemName = null, string description = null);
        Response<bool> BulkUpdateCatalogueAndStatusImport(System.Data.DataTable catalogueTable, System.Data.DataTable stockTable);

        Response<bool> BulkInsertInvalidCatalogueImport(DataTable dtInvalidItems, DataTable dtInitialStock);

        Response<bool> BulkInsertInvalidCatalogue(DataTable dtInvalidItems, SqlConnection conn, SqlTransaction transaction);
    }
}
