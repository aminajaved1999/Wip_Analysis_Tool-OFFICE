using System;

namespace WIPAT.Entities.Dto
{
    public class ItemCatalogueDto
    {
        public int Id { get; set; }
        public string Casin { get; set; } // C-ASIN
        public string Model { get; set; }
        public string Description { get; set; }
        public string ColorName { get; set; }
        public string Size { get; set; }
        public string PCPK { get; set; }
        public int? MOQ { get; set; }
        public int? CasePackQty { get; set; }

        // REPLACED: isActive (bool) -> ItemStatus (int)
        public int ItemStatus { get; set; }
        public string Notes { get; set; }
        public int OpeningStock { get; set; }
    }
}