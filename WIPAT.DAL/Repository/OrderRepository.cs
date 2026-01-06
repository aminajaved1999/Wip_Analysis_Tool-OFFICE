using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Data.Entity.Core.Metadata.Edm;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WIPAT.Entities;
using WIPAT.Entities.Dto;
using WIPAT.Entities.Enum;
namespace WIPAT.DAL
{
    public class OrderRepository
    {


        private WipSession _session;
        public OrderRepository(WipSession session)
        {
            _session = session;
        }


        #region actual order
        public async Task<bool> SaveActualOrdersToDatabase(List<ActualOrder> orders)
        {
            try
            {
                using (var context = new WIPATContext())
                {
                    // Add the new records
                    context.ActualOrders.AddRange(orders);
                    context.SaveChanges();
                }
                return true;
            }
            catch (Exception)
            {
                return false; 
            }
        }

        public Response<List<ActualOrder>> GetActualOrdersFromDatabase()
        {
            try
            {
                using (var context = new WIPATContext())
                {
                    var orders = context.ActualOrders.ToList();
                    return new Response<List<ActualOrder>>()
                    {
                        Success = true,
                        Message = "Actual orders retrieved successfully.",
                        Data = orders
                    };
                }
            }
            catch (Exception ex)
            {
                return new Response<List<ActualOrder>>()
                {
                    Success = false,
                    Message = $"Error retrieving actual orders: {ex.Message}",
                    Data = null
                };
            }
        }

        public Response<DataTable> GetOrderDataByMonthYear(string month, string year)
        {
            var response = new Response<DataTable>();

            try
            {
                using (var context = new WIPATContext())
                {
                    #region Data Retrieval (Optimized single query)

                    var query = context.ActualOrders
                        .AsNoTracking()
                        .Where(o => o.Month == month && o.Year == year)
                        .Include(o => o.ItemCatalogue)
                        .ToList();

                    if (!query.Any())
                    {
                        response.Success = false;
                        response.Message = $"No data found for month '{month}', and year '{year}'.";
                        return response;
                    }

                    var actualOrder = query.ToList();
                    #endregion

                    #region DataTable Construction
                    DataTable table = new DataTable();
                    //table.Columns.Add("Id", typeof(int));
                    table.Columns.Add("ItemCatalogueId", typeof(int));
                    table.Columns.Add("Casin", typeof(string));
                    table.Columns.Add("Quantity", typeof(int));
                    table.Columns.Add("Month", typeof(string));
                    table.Columns.Add("Year", typeof(string));
                    table.Columns.Add("FileName", typeof(string));

                    foreach (var o in query)
                    {
                        table.Rows.Add(o.ItemCatalogueId, o.ItemCatalogue.Casin, o.Quantity, o.Month, o.Year, o.FileName);
                    }
                    #endregion

                    #region Response Preparation
                    response.Success = true;
                    response.Message = $"Order Data for '{month}-{year}' retrieved successfully.";
                    //response.Data = new Tuple<DataTable, List<ActualOrder>>(table, actualOrder);  // Return ActualOrder
                    response.Data = table;  // Return ActualOrder
                    return response;
                    #endregion
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Exception While Getting Orders from DB: {ex.Message}" + (ex.InnerException != null ? $" | Inner Exception: {ex.InnerException.Message}" : "");
                return response;
            }
        }

        #endregion actual order

        public async Task<Response<bool>> SaveOrdersAndUpdateStock(List<ActualOrder> orders)
        {
            var response = new Response<bool>();
            try
            {
                using (var context = new WIPATContext())
                {
                    using (var transaction = context.Database.BeginTransaction()) // Start a transaction
                    {
                        try
                        {
                            #region Save Orders

                            // 1. Save the new orders to the ActualOrders table
                            context.ActualOrders.AddRange(orders);
                            var isOrdersSaved = await context.SaveChangesAsync();

                            if (isOrdersSaved <= 0)
                            {
                                transaction.Rollback(); // Rollback if saving orders fails
                                response.Success = false;
                                response.Data = false;
                                response.Message = "Failed to save actual orders.";
                                return response;
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

                            // Commit
                            transaction.Commit();

                            response.Success = true;
                            response.Data = true;
                            response.Message = "Orders saved and stock quantities updated successfully.";
                            return response;
                        }
                        catch (Exception ex)
                        {
                            // Rollback
                            transaction.Rollback();

                            response.Success = false;
                            response.Data = false;
                            response.Message = $"An error occurred while saving orders and updating stock quantities: {ex.Message}. Please try again or contact support if the issue persists.";
                            return response;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Data = false;
                response.Message = $"An error occurred while processing the request: {ex.Message}. Please try again or contact support if the issue persists.";
                return response;
            }
        }

        public async Task<Response<ActualOrder>> OrderFileExists(string fileName, string requiredMonth, string requiredYear)
        {
            var response = new Response<ActualOrder>();

            // Input validation
            if (string.IsNullOrWhiteSpace(fileName))
            {
                response.Success = false;
                response.Message = "File name is required.";
                return response;
            }

            if (string.IsNullOrWhiteSpace(requiredMonth) || string.IsNullOrWhiteSpace(requiredYear))
            {
                response.Success = false;
                response.Message = "Both month and year are required.";
                return response;
            }

            try
            {
                using (var context = new WIPATContext())
                {
                    // Check for file name match
                    var orderByFileName = await context.ActualOrders.FirstOrDefaultAsync(f => f.FileName == fileName);

                    // Check for month and year match
                    var orderByMonthYear = await context.ActualOrders.FirstOrDefaultAsync(f => f.Month == requiredMonth && f.Year == requiredYear);

                    if (orderByFileName != null)
                    {
                        response.Success = true;
                        response.Data = orderByFileName;
                        response.Message = "Order found by file name.";
                    }
                    else if (orderByMonthYear != null)
                    {
                        response.Success = true;
                        response.Data = orderByMonthYear;
                        response.Message = $"Order found for {requiredMonth}/{requiredYear}.";
                    }
                    else
                    {
                        response.Success = false;
                        response.Data = null;
                        response.Message = $"No order found";
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Data = null;
                response.Message = $"An error occurred while checking for the order: {ex.Message}";
            }

            return response;
        }

        public async Task<Response<ActualOrder>> OrderFileDetailsExists(string month, string year)
        {
            var response = new Response<ActualOrder>();

            try
            {
                using (var context = new WIPATContext())
                {
                    //if order with the same file name exists = true
                    var order = context.ActualOrders.Where(o => o.Month == month && o.Year == year ).FirstOrDefault();
                    if (order != null)
                    {
                        response.Success = true;
                        response.Data = order;
                        response.Message = "Order found.";
                        return response;
                    }
                    else
                    {
                        response.Success = false;
                        response.Data = null;
                        response.Message = $"No order found with month '{month}', year '{year}'.";
                        return response;

                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Data = null;
                response.Message = $"An error occurred while checking for the order: {ex.Message}. Please try again or contact support.";
                return response;

            }
        }

        public Response<Tuple<DataTable, List<ActualOrder>>> GetExstingOrderData(string fileName, string month, string year)
        {
            var response = new Response<Tuple<DataTable, List<ActualOrder>>>();

            try
            {
                using (var context = new WIPATContext())
                {
                    #region Data Retrieval (Optimized single query)

                    // Fetch ActualOrders and related ItemCatalogue in a single query
                    var query = context.ActualOrders
                        .AsNoTracking()
                        //.Where(o => o.FileName == fileName && o.Month == month && o.Year == year)
                        .Where(o => o.Month == month && o.Year == year)
                        .Include(o => o.ItemCatalogue)
                        .ToList();

                    // Check if no data is returned
                    if (!query.Any())
                    {
                        response.Success = false;
                        response.Message = $"No data found for file '{fileName}', month '{month}', and year '{year}'.";
                        return response;
                    }

                    var actualOrder = query.ToList();
                    #endregion

                    #region DataTable Construction
                    DataTable table = new DataTable();
                    //table.Columns.Add("Id", typeof(int));
                    table.Columns.Add("ItemCatalogueId", typeof(int));
                    table.Columns.Add("Quantity", typeof(int));
                    table.Columns.Add("Month", typeof(string));
                    table.Columns.Add("Year", typeof(string));
                    table.Columns.Add("FileName", typeof(string));

                    foreach (var o in query)
                    {
                        //table.Rows.Add(o.Id,o.ItemCatalogueId,o.Quantity,o.Month,  o.Year , o.FileName);
                        table.Rows.Add(o.ItemCatalogueId,o.Quantity,o.Month,  o.Year , o.FileName);
                    }
                    #endregion

                    

                    #region Response Preparation
                    response.Success = true;
                    response.Message = $"Order Data for '{month}-{year}' retrieved successfully.";
                    response.Data = new Tuple<DataTable, List<ActualOrder>>(table, actualOrder);  // Return ActualOrder
                    return response;
                    #endregion
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Exception While Getting Orders from DB: {ex.Message}" +(ex.InnerException != null ? $" | Inner Exception: {ex.InnerException.Message}" : "");
                return response;
            }
        }

        public Response<bool> DeleteExistingOrderData(string fileName, string month, string year)
        {
            var response = new Response<bool>();

            try
            {
                using (var context = new WIPATContext())
                {
                    // Find all ActualOrder records matching the criteria
                    var ordersToDelete = context.ActualOrders
                        .Where(o => o.FileName == fileName && o.Month == month && o.Year == year)
                        .ToList();

                    if (!ordersToDelete.Any())
                    {
                        response.Message = $"No data found for file '{fileName}', month '{month}', and year '{year}' to delete.";
                        response.Status = StatusType.Warning;
                        response.Data = false;
                        return response;
                    }

                    // Remove the records
                    context.ActualOrders.RemoveRange(ordersToDelete);
                    context.SaveChanges();

                    response.Success = true;
                    response.Message = $"Data for file '{fileName}', month '{month}', and year '{year}' deleted successfully.";
                    response.Data = true;
                    return response;
                }
            }
            catch (Exception ex)
            {
                response.Message = $"Exception occurred: {ex.Message}" +
                    (ex.InnerException != null ? $" | Inner Exception: {ex.InnerException.Message}" : "");
                response.Status = StatusType.Error;
                response.Data = false;
                return response;
            }
        }
        private DataTable ReadOrderFileToDataTable(string filePath)
        {
            var dt = new DataTable();
            dt.Columns.Add("Item Catalogue Id", typeof(string));
            dt.Columns.Add("Quantity", typeof(string));
            dt.Columns.Add("Month", typeof(string));
            dt.Columns.Add("Year", typeof(string));

            // Simple CSV parsing example
            var lines = File.ReadAllLines(filePath);
            foreach (var line in lines.Skip(1)) // skip header
            {
                var cols = line.Split(',');

                if (cols.Length >= 2)
                {
                    var row = dt.NewRow();
                    row["Item Catalogue Id"] = cols[0].Trim();
                    row["Quantity"] = cols[1].Trim();

                    if (cols.Length > 2)
                        row["Month"] = cols[2].Trim();

                    if (cols.Length > 3)
                        row["Year"] = cols[3].Trim();

                    dt.Rows.Add(row);
                }
            }

            return dt;
        }


    }
}