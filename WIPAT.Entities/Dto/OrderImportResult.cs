using System.Collections.Generic;
using WIPAT.Entities;    
using WIPAT.Entities.BO;
using WIPAT.Entities.Entities;

namespace WIPAT.Entities.Dto
{
    public class OrderImportResult
    {
        public List<InvalidOrder> InvalidItems { get; set; } = new List<InvalidOrder>();
        public List<OrderDetail> ValidItems { get; set; } = new List<OrderDetail>();

        public bool HasErrors => InvalidItems.Count > 0;
    }
}