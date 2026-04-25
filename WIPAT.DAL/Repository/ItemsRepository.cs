using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using WIPAT.DAL.Interfaces;
using WIPAT.Entities;
using WIPAT.Entities.Dto;

namespace WIPAT.DAL
{
    public class ItemsRepository : IItemsRepository
    {
        private readonly WIPATContext _context;

        public ItemsRepository(WIPATContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Response<List<ItemCatalogue>>> GetItemCatalogues()
        {
            try
            {
                var items = await _context.ItemCatalogues.Where(i=> i.isActive == true).ToListAsync();

                if (items != null && items.Count > 0)
                {
                    return new Response<List<ItemCatalogue>>
                    {
                        Success = true,
                        Data = items,
                        Message = "ItemCatalogues retrieved successfully"
                    };
                }

                return new Response<List<ItemCatalogue>>
                {
                    Success = false,
                    Data = null,
                    Message = "No ItemCatalogues found"
                };
            }
            catch (Exception ex)
            {
                return new Response<List<ItemCatalogue>> { Success = false, Message = ex.Message };
            }
        }

        public int GetItemIdByAsin(string asin)
        {
            var item = _context.ItemCatalogues
                               .AsNoTracking().Where(i => i.isActive == true)
                               .FirstOrDefault(i => i.Casin == asin);
            return item?.Id ?? 0;
        }

        public bool IsCAsinExistInCatalogue(string casin)
        {
            return _context.ItemCatalogues.Any(item => item.Casin == casin && item.isActive == true);
        }

        public ItemCatalogue GetItemByCAsin(string casin)
        {
            return _context.ItemCatalogues
                           .AsNoTracking()
                           .FirstOrDefault(item => item.Casin == casin && item.isActive == true);
        }

        public bool AddItemToCatalogue(string casin, string itemName = null, string description = null)
        {
            try
            {
                bool exists = _context.ItemCatalogues.Any(item => item.Casin == casin && item.isActive == true);
                if (exists)
                {
                    return true;
                }

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
            catch (Exception)
            {
                // In a real scenario, log the exception
                return false;
            }
        }

        public async Task<Response<bool>> IsCasinExistInCatalogueAndInitialStock(string casin)
        {
            try
            {
                // Check if the casin exists in ItemCatalogue
                bool existsInCatalogue = await _context.ItemCatalogues.AnyAsync(item => item.Casin == casin && item.isActive == true);

                // Check if the casin exists in InitialStock
                //bool existsInInitialStock = await _context.InitialStocks.AnyAsync(stock => stock.ItemCatalogue.Casin == casin);
                bool existsInInitialStock = await _context.InitialStocks.AnyAsync(stock => stock.ItemCatalogue.Casin == casin && stock.ItemCatalogue.isActive);


                if (existsInCatalogue || existsInInitialStock)
                {
                    string msg;
                    if (existsInCatalogue && existsInInitialStock)
                    {
                        msg = $"The Casin '{casin}' exists in both the Item Catalogue and the Initial Stock.";
                    }
                    else if (existsInCatalogue)
                    {
                        msg = $"The Casin '{casin}' exists in the Item Catalogue, but not in the Initial Stock.";
                    }
                    else
                    {
                        msg = $"The Casin '{casin}' exists in the Initial Stock, but not in the Item Catalogue.";
                    }

                    return new Response<bool> { Success = true, Data = true, Message = msg };
                }

                return new Response<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"The Casin '{casin}' was not found in either the Item Catalogue or the Initial Stock."
                };
            }
            catch (Exception ex)
            {
                return new Response<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"Error checking Casin '{casin}': {ex.Message}"
                };
            }
        }

        public async Task<Response<bool>> IsItemExistInCatalogue(string asin)
        {
            try
            {
                bool existsInCatalogue = await _context.ItemCatalogues.AnyAsync(item => item.Casin == asin && item.isActive == true );

                if (existsInCatalogue)
                {
                    return new Response<bool>
                    {
                        Success = true,
                        Data = true,
                        Message = $"The CASIN '{asin}' exists in the Item Catalogue."
                    };
                }

                return new Response<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"The CASIN '{asin}' was not found in the Item Catalogue."
                };
            }
            catch (Exception ex)
            {
                return new Response<bool>
                {
                    Success = false,
                    Data = false,
                    Message = $"Error checking ASIN '{asin}': {ex.Message}"
                };
            }
        }
    }
}