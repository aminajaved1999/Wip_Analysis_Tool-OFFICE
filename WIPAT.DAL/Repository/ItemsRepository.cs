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

        public bool AddItemToCatalogue(string casin, string itemName = null, string description = null)
        {
            try
            {
                using (var _context = new WIPATContext())
                {
                    // First check if already exists
                    bool exists = _context.ItemCatalogues.Any(item => item.Casin == casin);
                    if (exists) return true; // Already in catalogue, treat as success

                    var newItem = new ItemCatalogue
                    {
                        Casin = casin,
                        Description = description ?? string.Empty,
                        CreatedAt = DateTime.Now,           
                    };

                    _context.ItemCatalogues.Add(newItem);
                    _context.SaveChanges();

                    return true;
                }
            }
            catch (Exception ex)
            {
                // log ex if you have logging
                return false;
            }
        }


        public async Task<Response<bool>> IsCasinExistInCatalogueAndInitialStock(string casin)
        {
            var res = new Response<bool>();
            try
            {
                using (var _context = new WIPATContext())
                {
                    // Check if the casin exists in ItemCatalogue
                    bool existsInCatalogue = await _context.ItemCatalogues.AnyAsync(item => item.Casin == casin);

                    // Check if the casin exists in InitialStock
                    bool existsInInitialStock = await _context.InitialStocks.AnyAsync(stock => stock.ItemCatalogue.Casin == casin);

                    // Combine both checks and set response
                    if (existsInCatalogue || existsInInitialStock)
                    {
                        res.Success = true;
                        res.Data = true;

                        // Provide detailed message based on which condition was true
                        if (existsInCatalogue && existsInInitialStock)
                        {
                            res.Message = $"The Casin '{casin}' exists in both the Item Catalogue and the Initial Stock.";
                        }
                        else if (existsInCatalogue)
                        {
                            res.Message = $"The Casin '{casin}' exists in the Item Catalogue, but not in the Initial Stock.";
                        }
                        else
                        {
                            res.Message = $"The Casin '{casin}' exists in the Initial Stock, but not in the Item Catalogue.";
                        }
                    }
                    else
                    {
                        res.Success = false;
                        res.Data = false;
                        res.Message = $"The Casin '{casin}' was not found in either the Item Catalogue or the Initial Stock. Please ensure the correct Casin code is entered.";
                    }

                    return res;
                }
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Data = false;
                res.Message = $"An error occurred while checking the Casin '{casin}': {ex.Message}. Please try again or contact support if the issue persists.";
                return res;
            }
        }
        public async Task<Response<bool>> IsItemExistInCatalogue(string asin)
        {
            var res = new Response<bool>();
            try
            {
                using (var _context = new WIPATContext())
                {
                    // Check if the ASIN exists in ItemCatalogue
                    bool existsInCatalogue = await _context.ItemCatalogues.AnyAsync(item => item.Casin == asin);

                    // Set response based on existence of ASIN in the catalogue
                    if (existsInCatalogue)
                    {
                        res.Success = true;
                        res.Data = true;
                        res.Message = $"The ASIN '{asin}' exists in the Item Catalogue.";
                    }
                    else
                    {
                        res.Success = false;
                        res.Data = false;
                        res.Message = $"The ASIN '{asin}' was not found in the Item Catalogue. Please ensure the correct ASIN is entered.";
                    }

                    return res;
                }
            }
            catch (Exception ex)
            {
                res.Success = false;
                res.Data = false;
                res.Message = $"An error occurred while checking the ASIN '{asin}': {ex.Message}. Please try again or contact support if the issue persists.";
                return res;
            }
        }

    }
}
