using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WIPAT.Entities.Dto;
using WIPAT.Entities.Enum;

namespace WIPAT.Entities
{
    public class InitialStock : BaseEntity
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Index(IsUnique = true)]
        public int ItemCatalogueId { get; set; }  // FK
        public int OpeningStock { get; set; }
        public int OrderQty { get; set; }
        public int ProductionQty { get; set; }
        public DateTime? OrderQtyUpdatedAt { get; set; }
        public int? OrderQtyUpdatedBy { get; set; }
        public DateTime? ProductionQtyUpdatedAt { get; set; }
        public int? ProductionQtyUpdatedBy { get; set; }

        public string Description { get; set; }
        public string Notes { get; set; }

        [ForeignKey(nameof(ItemCatalogueId))]
        public ItemCatalogue ItemCatalogue { get; set; }

    }
}
