using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WIPAT.Entities.Entities
{
    public class OrderMaster
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Month { get; set; }
        public string Year { get; set; }
        public string DocType { get; set; } //S -> ship, A-actual order
        public string DocNo { get; set; }
        public string FileName { get; set; }

        public virtual ICollection<OrderDetail> Details { get; set; }


    }
}
