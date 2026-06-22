using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using WIPAT.DAL.Interfaces;
using WIPAT.Entities;
using WIPAT.Entities.Dto;
using WIPAT.Entities.Entities;
using WIPAT.Entities.ExcelTemplateDefinitions;

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

        #region Read Operations

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

                var tableResponse = new DataTableFactory().BuildOrderUIDataTable(query);

                if (!tableResponse.Success)
                {
                    return new Response<DataTable>
                    {
                        Success = false,
                        Message = tableResponse.Message
                    };
                }

                return new Response<DataTable>
                {
                    Success = true,
                    Message = $"Order Data for '{month}-{year}' retrieved successfully.",
                    Data = tableResponse.Data
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

                var tableResponse = new DataTableFactory().BuildOrderUIDataTable(query);

                if (!tableResponse.Success)
                {
                    return new Response<Tuple<DataTable, List<ActualOrder>>>
                    {
                        Success = false,
                        Message = tableResponse.Message
                    };
                }

                return new Response<Tuple<DataTable, List<ActualOrder>>>
                {
                    Success = true,
                    Message = $"Order Data for '{month}-{year}' retrieved successfully.",
                    Data = new Tuple<DataTable, List<ActualOrder>>(tableResponse.Data, query)
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

        #endregion

        #region Existence Checks

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

        #endregion

        #region Write Operations 

        public async Task<Response<bool>> SaveOrders(List<ActualOrder> orders)
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    _context.ActualOrders.AddRange(orders);
                    var isOrdersSaved = await _context.SaveChangesAsync();

                    if (isOrdersSaved <= 0)
                    {
                        transaction.Rollback();
                        return new Response<bool> { Success = false, Data = false, Message = "Failed to save actual orders." };
                    }

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

        public async Task<Response<bool>> BulkInsertOrders(DataTable bulkTable)
        {
            #region Validation

            if (bulkTable == null)
            {
                return new Response<bool> { Success = false, Data = false, Message = "Bulk insert failed: DataTable is null." };
            }

            if (bulkTable.Rows.Count == 0)
            {
                return new Response<bool> { Success = false, Data = false, Message = "Bulk insert failed: DataTable has no rows." };
            }

            #endregion

            #region Database Connection

            var dbConnection = _context.Database.Connection;

            if (dbConnection.State != ConnectionState.Open)
            {
                dbConnection.Open();
            }

            #endregion

            #region Transaction

            using (var transaction = dbConnection.BeginTransaction())
            {
                try
                {
                    #region Bulk Copy Operation

                    using (var sqlBulkCopy = new SqlBulkCopy((SqlConnection)dbConnection, SqlBulkCopyOptions.Default, (SqlTransaction)transaction))
                    {
                        sqlBulkCopy.DestinationTableName = "dbo.ActualOrders";

                        sqlBulkCopy.ColumnMappings.Add(MasterColumnCatalogue.ItemCatalogueId.Name, "ItemCatalogueId");
                        sqlBulkCopy.ColumnMappings.Add(MasterColumnCatalogue.Quantity.Name, "Quantity");
                        sqlBulkCopy.ColumnMappings.Add(MasterColumnCatalogue.MonthInteger.Name, "Month");
                        sqlBulkCopy.ColumnMappings.Add(MasterColumnCatalogue.Year.Name, "Year");
                        sqlBulkCopy.ColumnMappings.Add(MasterColumnCatalogue.FileName.Name, "FileName");
                        sqlBulkCopy.ColumnMappings.Add(MasterColumnCatalogue.CreatedById.Name, "CreatedById");
                        sqlBulkCopy.ColumnMappings.Add(MasterColumnCatalogue.CreatedAt.Name, "CreatedAt");

                        await sqlBulkCopy.WriteToServerAsync(bulkTable);
                    }

                    #endregion

                    transaction.Commit();

                    return new Response<bool> { Success = true, Data = true, Message = "Bulk insert successful." };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    return new Response<bool> { Success = false, Data = false, Message = $"Bulk insert failed: {ex.Message}" };
                }
                finally
                {
                    #region Cleanup Connection

                    if (dbConnection.State == ConnectionState.Open)
                        dbConnection.Close();

                    #endregion
                }
            }

            #endregion
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