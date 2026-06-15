using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WIPAT.Entities
{
    public class ItemCatalogue : BaseEntity
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Index(IsUnique = true)]
        [MaxLength(100)]
        public string Casin { get; set; } // C-ASIN
        public string Model { get; set; }
        public string Description { get; set; }
        public string ColorName { get; set; }
        public string Size { get; set; }
        public string PCPK { get; set; }
        public int? MOQ { get; set; }
        public int CasePackQty { get; set; }

        // REPLACED: isActive (bool) -> ItemStatus (int)
        public int ItemStatus { get; set; }
        public string Notes { get; set; }

        public virtual ICollection<InitialStock> InitialStocks { get; set; } = new List<InitialStock>();
        public virtual ICollection<ActualOrder> ActualOrders { get; set; } = new List<ActualOrder>();
    }
}