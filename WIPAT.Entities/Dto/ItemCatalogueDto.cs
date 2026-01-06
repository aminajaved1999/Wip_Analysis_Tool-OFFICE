using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WIPAT.Entities.Dto
{
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
