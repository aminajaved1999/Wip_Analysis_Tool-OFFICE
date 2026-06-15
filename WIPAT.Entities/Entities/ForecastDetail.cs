using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WIPAT.Entities
{
    public class ForecastDetail : BaseEntity
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // excel column
        public int? ItemCatalogueId { get; set; } // FK
        public string CASIN { get; set; }

        public string ModelNumber { get; set; }
        public int RequestedQuantity { get; set; }
        public int? Wip { get; set; }

        public int CommitmentPeriod { get; set; }

        public DateTime PODate { get; set; }

        // extracted from PO Date
        public string Month { get; set; }
        public string Year { get; set; }
        public bool IsSystemGenerated { get; set; }

        // REPLACED: IsActive (bool) -> ItemStatus (int)
        public int ItemStatus { get; set; }

        // Foreign Key
        public int POForecastMasterId { get; set; }

        [ForeignKey("POForecastMasterId")]
        public virtual ForecastMaster Master { get; set; }

        [ForeignKey(nameof(ItemCatalogueId))]
        public ItemCatalogue ItemCatalogue { get; set; }
    }
}