using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WIPAT.Entities;
using WIPAT.Entities.Dto;

namespace WIPAT.DAL
{
    public class OrderRepository
    {
        #region actual order
        public async Task<bool> SaveActualOrdersToDatabase(List<ActualOrder> orders)
        {
            try
            {
                using (var context = new WIPATContext())
                {
                    // Delete existing records first
                    context.ActualOrders.RemoveRange(context.ActualOrders);
                    context.SaveChanges();

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
        #endregion actual order
    }
}
