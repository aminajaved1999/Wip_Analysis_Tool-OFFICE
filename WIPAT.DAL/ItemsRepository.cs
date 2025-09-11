using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WIPAT.Entities;
using WIPAT.Entities.Dto;

namespace WIPAT.DAL
{
    public class ItemsRepository
    {
        public async Task<Response<List<ItemCatalogue>>> GetItemCatalogues()
        {
            var res = new Response<List<ItemCatalogue>>();
            try
            {

                using (var _context = new WIPATContext())
                {
                    var ItemCatalogues = await _context.ItemCatalogues.ToListAsync();

                    if (ItemCatalogues != null)
                    {
                        res.Success = true;
                        res.Data = ItemCatalogues;
                        res.Message = "ItemCatalogues retrieved successfully";
                    }
                    else
                    {
                        res.Success = false;
                        res.Data = null;
                        res.Message = "No ItemCatalogues found";
                    }
                    return res;
                }
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Message = ex.Message;
                return res;

            }
        }

        public int GetItemIdByAsin(string asin)
        {
            using (var context = new WIPATContext())
            {
                var item = context.ItemCatalogues.FirstOrDefault(i => i.Casin == asin);
                return item?.Id ?? 0;
            }
        }

        // Checks if a C-ASIN exists in the item catalogue
        public bool IsCAsinExistInCatalogue(string casin)
        {
            using (var _context = new WIPATContext())
            {
                return _context.ItemCatalogues.Any(item => item.Casin == casin);
            }
        }

        // Returns the item by C-ASIN, or null if not found
        public ItemCatalogue GetItemByCAsin(string casin)
        {
            using (var _context = new WIPATContext())
            {
                return _context.ItemCatalogues.FirstOrDefault(item => item.Casin == casin);
            }
        }
    }
}
