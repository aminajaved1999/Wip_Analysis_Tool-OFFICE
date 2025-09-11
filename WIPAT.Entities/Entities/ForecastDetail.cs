using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WIPAT.Entities
{
    public class ForecastDetail
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        //excel column
        public string CASIN { get; set; }
        public string ModelNumber { get; set; }
        public int RequestedQuantity { get; set; }
        public int? Wip { get; set; }

        public string CommitmentPeriod { get; set; }

        public DateTime PODate { get; set; }

        //extracted from PO Date

        public string Month { get; set; }
        public string Year { get; set; }

        // Foreign Key
        public int POForecastMasterId { get; set; }

        [ForeignKey("POForecastMasterId")]
        public virtual ForecastMaster Master { get; set; }
    }
}
