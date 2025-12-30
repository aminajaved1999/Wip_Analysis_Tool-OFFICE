using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WIPAT.Entities
{
    public class ItemCatalogue: BaseEntity
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
        public int? CasePackQty { get; set; }
    }

    public class ItemCatalogueDto
    {
        public string Casin { get; set; }
        public string Model { get; set; }
        public string Description { get; set; }
        public string ColorName { get; set; }
        public string Size { get; set; }
        public string PCPK { get; set; }
        public int? CasePackQty { get; set; }
    }
}
