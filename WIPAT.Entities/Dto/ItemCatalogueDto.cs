using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WIPAT.Entities.Dto
{
    public class ItemCatalogueDto
    {
        //public string Casin { get; set; }
        //public string Model { get; set; }
        //public string Description { get; set; }
        //public string ColorName { get; set; }
        //public string Size { get; set; }
        //public string PCPK { get; set; }
        //public int? CasePackQty { get; set; }

        public int Id { get; set; }
        public string Casin { get; set; } // C-ASIN
        public string Model { get; set; }
        public string Description { get; set; }
        public string ColorName { get; set; }
        public string Size { get; set; }
        public string PCPK { get; set; }
        public int? MOQ { get; set; }
        public int? CasePackQty { get; set; }
        public bool isActive { get; set; }
        public string ItemStatus { get; set; }
        public string Notes { get; set; }
        public int OpeningStock { get; set; }

    }
}
