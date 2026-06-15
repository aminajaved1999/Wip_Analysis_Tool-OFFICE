using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using WIPAT.DAL.Interfaces;
using WIPAT.Entities;
using WIPAT.Entities.Dto;
using WIPAT.Entities.Entities;
using WIPAT.Entities.Enum;

namespace WIPAT.DAL
{
    public class OrderRepository : IOrderRepository
    {
        private readonly WIPATContext _context;
        private readonly WipSession _session;

        public OrderRepository(WIPATContext context, WipSession session)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        #region Actual Order

        public Response<List<ActualOrder>> GetActualOrdersFromDatabase()
        {
            try
            {
                var orders = _context.ActualOrders.AsNoTracking().ToList();

                return new Response<List<ActualOrder>>
                {
                    Success = true,
                    Message = "Actual orders retrieved successfully.",
                    Data = orders
                };
            }
            catch (Exception ex)
            {
                return new Response<List<ActualOrder>>
                {
                    Success = false,
                    Message = $"Error retrieving actual orders: {ex.Message}"
                };
            }
        }

        public Response<DataTable> GetOrderDataByMonthYear(string month, string year)
        {
            try
            {
                var query = _context.ActualOrders
                    .AsNoTracking()
                    .Include(o => o.ItemCatalogue)
                    .Where(o => o.Month == month && o.Year == year)    
                    .ToList();

                if (!query.Any())
                {
                    return new Response<DataTable>
                    {
                        Success = false,
                        Message = $"No data found for month '{month}', and year '{year}'."
                    };
                }

                #region DataTable Construction
                DataTable table = new DataTable();
                table.Columns.Add("ItemCatalogueId", typeof(int));
                table.Columns.Add("CASIN", typeof(string));
                table.Columns.Add("Quantity", typeof(int));
                table.Columns.Add("Month", typeof(string));
                table.Columns.Add("Year", typeof(string));
                table.Columns.Add("FileName", typeof(string));

                // ---> ADDED: New Columns for UI <---
                table.Columns.Add("IsActive", typeof(bool));
                table.Columns.Add("ItemStatus", typeof(string));

                foreach (var o in query)
                {
                    bool isActive = o.ItemCatalogue?.isActive ?? false;
                    string itemStatus = o.ItemCatalogue?.ItemStatus;

                    table.Rows.Add(
                        o.ItemCatalogueId,
                        o.ItemCatalogue?.Casin,
                        o.Quantity,
                        o.Month,
                        o.Year,
                        o.FileName,
                        isActive,
                        itemStatus
                    );
                }
                #endregion

                return new Response<DataTable>
                {
                    Success = true,
                    Message = $"Order Data for '{month}-{year}' retrieved successfully.",
                    Data = table
                };
            }
            catch (Exception ex)
            {
                return new Response<DataTable>
                {
                    Success = false,
                    Message = $"Exception While Getting Orders from DB: {ex.Message}"
                };
            }
        }

        #endregion

        public async Task<Response<bool>> SaveOrdersAndUpdateStock(List<ActualOrder> orders)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    #region Save Orders
                    _context.ActualOrders.AddRange(orders);
                    var isOrdersSaved = await _context.SaveChangesAsync();

                    if (isOrdersSaved <= 0)
                    {
                        transaction.Rollback();
                        return new Response<bool> { Success = false, Data = false, Message = "Failed to save actual orders." };
                    }
                    #endregion

                    #region Update Stock Quantities

                    //// 2. Update the stock quantities in InitialStocks table
                    //foreach (var order in orders)
                    //{
                    //    var existingStock = await context.InitialStocks.FirstOrDefaultAsync(s => s.ItemCatalogueId == order.ItemCatalogueId);
                    //    if (existingStock != null)
                    //    {
                    //        existingStock.OrderQty += order.Quantity;
                    //        existingStock.OrderQtyUpdatedAt = DateTime.Now;
                    //        existingStock.OrderQtyUpdatedBy = _session.LoggedInUser.Id;
                    //    }
                    //    else
                    //    {
                    //        // Rollback If stock record doesn't exist
                    //        transaction.Rollback();
                    //        response.Success = false;
                    //        response.Data = false;
                    //        response.Message = $"ItemCatalogueId {order.ItemCatalogueId} not found.";
                    //        return response;
                    //    }
                    //}
                    //var isStockUpdated = await context.SaveChangesAsync();

                    //if (isStockUpdated <= 0)

                    //{
                    //    transaction.Rollback(); // Rollback if updating stock fails
                    //    response.Success = false;
                    //    response.Data = false;
                    //    response.Message = "Failed to update stock quantities.";
                    //    return response;
                    //}

                    #endregion

                    transaction.Commit();

                    return new Response<bool>
                    {
                        Success = true,
                        Data = true,
                        Message = "Orders saved successfully."
                    };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new Response<bool>
                    {
                        Success = false,
                        Data = false,
                        Message = $"An error occurred: {ex.Message}."
                    };
                }
            }
        }

        public async Task<Response<ActualOrder>> OrderFileExists(string fileName, string requiredMonth, string requiredYear)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return new Response<ActualOrder> { Success = false, Message = "File name is required." };
            }

            if (string.IsNullOrWhiteSpace(requiredMonth) || string.IsNullOrWhiteSpace(requiredYear))
            {
                return new Response<ActualOrder> { Success = false, Message = "Both month and year are required." };
            }

            try
            {
                var orderByFileName = await _context.ActualOrders.FirstOrDefaultAsync(f => f.FileName == fileName);

                var orderByMonthYear = await _context.ActualOrders.FirstOrDefaultAsync(f => f.Month == requiredMonth && f.Year == requiredYear);

                if (orderByFileName != null)
                {
                    return new Response<ActualOrder> { Success = true, Data = orderByFileName, Message = "Order found by file name." };
                }
                else if (orderByMonthYear != null)
                {
                    return new Response<ActualOrder> { Success = true, Data = orderByMonthYear, Message = $"Order found for {requiredMonth}/{requiredYear}." };
                }

                return new Response<ActualOrder> { Success = false, Message = "No order found" };
            }
            catch (Exception ex)
            {
                return new Response<ActualOrder> { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public Response<Tuple<DataTable, List<ActualOrder>>> GetExistingOrderData(string fileName, string month, string year)
        {
            try
            {
                var query = _context.ActualOrders
                    .AsNoTracking()
                    .Include(o => o.ItemCatalogue)
                    .Where(o => o.Month == month && o.Year == year)
                    .ToList();

                if (!query.Any())
                {
                    return new Response<Tuple<DataTable, List<ActualOrder>>>
                    {
                        Success = false,
                        Message = $"No data found for file '{fileName}', month '{month}', and year '{year}'."
                    };
                }

                // DataTable setup
                DataTable table = new DataTable();

                table.Columns.Add("ItemCatalogueId", typeof(int));
                table.Columns.Add("CASIN", typeof(string));
                table.Columns.Add("Quantity", typeof(int));
                table.Columns.Add("Month", typeof(string));
                table.Columns.Add("Year", typeof(string));
                table.Columns.Add("FileName", typeof(string));

                // ItemCatalogue raw DB columns
                table.Columns.Add("IsActive", typeof(bool));
                table.Columns.Add("ItemStatus", typeof(string));

                foreach (var o in query)
                {
                    var catalogue = o.ItemCatalogue;

                    table.Rows.Add(
                        o.ItemCatalogueId,
                        catalogue?.Casin,
                        o.Quantity,
                        o.Month,
                        o.Year,
                        o.FileName,
                        catalogue?.isActive,
                        catalogue?.ItemStatus
                    );
                }

                return new Response<Tuple<DataTable, List<ActualOrder>>>()
                {
                    Success = true,
                    Message = $"Order Data for '{month}-{year}' retrieved successfully.",
                    Data = new Tuple<DataTable, List<ActualOrder>>(table, query)
                };
            }
            catch (Exception ex)
            {
                return new Response<Tuple<DataTable, List<ActualOrder>>>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
        public Response<Tuple<DataTable, List<ActualOrder>>> _GetExistingOrderData(string fileName, string month, string year)
        {
            try
            {
                // 1. Filter by Month/Year AND ensure the related ItemCatalogue is Active
                var query = _context.ActualOrders
                    .AsNoTracking()
                    .Include(o => o.ItemCatalogue)
                    .Where(o => o.Month == month
                             && o.Year == year
                             && o.ItemCatalogue != null        // Safety check
                             && o.ItemCatalogue.isActive)      // Filter for active records only
                    .ToList();


                if (!query.Any())
                {
                    return new Response<Tuple<DataTable, List<ActualOrder>>>
                    {
                        Success = false,
                        Message = $"No active data found for file '{fileName}', month '{month}', and year '{year}'."
                    };
                }

                int count = query.Count;


                DataTable table = new DataTable();
                table.Columns.Add("ItemCatalogueId", typeof(int));
                table.Columns.Add("Quantity", typeof(int));
                table.Columns.Add("Month", typeof(string));
                table.Columns.Add("Year", typeof(string));
                table.Columns.Add("FileName", typeof(string));

                foreach (var o in query)
                {
                    table.Rows.Add(o.ItemCatalogueId, o.Quantity, o.Month, o.Year, o.FileName);
                }

                return new Response<Tuple<DataTable, List<ActualOrder>>>
                {
                    Success = true,
                    Message = $"Active Order Data for '{month}-{year}' retrieved successfully.",
                    Data = new Tuple<DataTable, List<ActualOrder>>(table, query)
                };
            }
            catch (Exception ex)
            {
                return new Response<Tuple<DataTable, List<ActualOrder>>>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        #region New Order Methods
        public Response<bool> IsDocNoExists(string docNo, string docType)
        {
            try
            {
                bool exists = _context.OrderMasters
                    .AsNoTracking()
                    .Any(o => o.DocNo == docNo && o.DocType == docType);

                return new Response<bool>
                {
                    Success = true,
                    Data = exists,
                    Message = exists ? "Document number exists." : "Document number does not exist."
                };
            }
            catch (Exception ex)
            {
                return new Response<bool>
                {
                    Success = false,
                    Message = $"Error checking document number existence: {ex.Message}. Please try again.",
                    Data = false
                };
            }
        }

        public async Task<Response<bool>> ExecuteOrderInsertion(OrderMaster master, List<OrderDetail> details)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // 1. Save Master
                    _context.OrderMasters.Add(master);
                    await _context.SaveChangesAsync();

                    // 2. Link Details
                    foreach (var d in details)
                    {
                        d.OrderMasterId = master.Id;
                    }

                    // 3. Save Details
                    _context.OrderDetails.AddRange(details);
                    await _context.SaveChangesAsync();

                    transaction.Commit();
                    return new Response<bool> { Success = true, Data = true, Message = "Order created successfully." };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new Response<bool> { Success = false, Data = false, Message = $"Database Error: {ex.Message}" };
                }
            }
        }
        #endregion



    }
}