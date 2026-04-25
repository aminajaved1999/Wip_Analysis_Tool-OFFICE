using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WIPAT.Entities.Dto
{
    public class OrderMasterDto
    {
        public int Id { get; set; }
        public string Month { get; set; }
        public string Year { get; set; }
        public string DocType { get; set; } //S -> ship, A-actual order
        public string DocNo { get; set; }
        public string FileName { get; set; }
    }

    public class OrderDetailDto
    {
        public int Id { get; set; }
        public int OrderMasterId { get; set; }  //FK
        public int ItemCatalogueId { get; set; }  // FK
        public string WipNo { get; set; }
        public string Casin { get; set; }
        public int Quantity { get; set; }
        public DateTime CreatedAt { get; set; }

        public int? CreatedById { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public int? UpdatedById { get; set; }

        public string Description { get; set; }
        public string Notes { get; set; }

    }

}
