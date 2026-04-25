using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WIPAT.Entities;
using WIPAT.Entities.Dto;

namespace WIPAT.DAL.Interfaces
{
    public interface IItemsRepository
    {
        Task<Response<List<ItemCatalogue>>> GetItemCatalogues();
        int GetItemIdByAsin(string asin);
        bool IsCAsinExistInCatalogue(string casin);
        ItemCatalogue GetItemByCAsin(string casin);
        bool AddItemToCatalogue(string casin, string itemName = null, string description = null);
        Task<Response<bool>> IsCasinExistInCatalogueAndInitialStock(string casin);
        Task<Response<bool>> IsItemExistInCatalogue(string asin);
    }
}
