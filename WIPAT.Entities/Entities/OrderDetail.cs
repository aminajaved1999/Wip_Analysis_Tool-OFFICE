using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WIPAT.Entities.Entities
{
    public class OrderDetail : BaseEntity
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int OrderMasterId { get; set; }  //FK
        public int ItemCatalogueId { get; set; }  // FK
        public string WipNo { get; set; }  
        public string Casin { get; set; }  
        public int Quantity { get; set; }
        public string DocType { get; set; } //S -> ship, A-actual order
        public string DocNo { get; set; }

        [ForeignKey(nameof(OrderMasterId))]
        public virtual OrderMaster Master { get; set; }

        [ForeignKey(nameof(ItemCatalogueId))]
        public ItemCatalogue ItemCatalogue { get; set; }

    }
}
