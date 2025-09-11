using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WIPAT.Entities
{
    public class InitialStock
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int ItemCatalogueId { get; set; }  // FK
        public string Month { get; set; }
        public string Year { get; set; }
        public int Quantity { get; set; }

        [ForeignKey(nameof(ItemCatalogueId))]
        public ItemCatalogue ItemCatalogue { get; set; }

    }
}
