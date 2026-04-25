using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WIPAT.Entities.BO;
using WIPAT.Entities.Entities;

namespace WIPAT.Entities.Dto
{
    public class OrderFileResponse
    {
        public DataTable DataTable { get; set; }
        public List<ActualOrder> ValidOrders { get; set; }
        public List<InvalidOrder> MissingOrders { get; set; }
        public List<ValidOrder> ValidOrderItems { get; set; } = new List<ValidOrder>();
        public List<InvalidOrder> InvalidOrderItems { get; set; } = new List<InvalidOrder>();


        public int ValidOrderCount { get; set; }
        public int InvalidOrderCount { get; set; }
        public int TotalOrderCount { get; set; }

    }
}
