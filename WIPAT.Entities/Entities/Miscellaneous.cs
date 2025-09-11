using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WIPAT.Entities
{
    public class Miscellaneous
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Casin { get; set; }
        public string Type { get; set; } // "Order" or "Item"
        public string Year { get; set; }
        public string Month { get; set; }
        public string Quantity { get; set; }
        public string FileName { get; set; }
        public DateTime DetectedAt { get; set; }
    }

}
