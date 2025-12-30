using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WIPAT.Entities
{
    public class ForecastMaster
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Month { get; set; }
        public string Year { get; set; }
        public string ForecastingFor { get; set; }
        public string FileName { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string CreatedBy { get; set; }
        public bool IsWipCalculated { get; set; }
        public bool IsWipModifiedByUser { get; set; }


        // Navigation
        public virtual ICollection<ForecastDetail> Details { get; set; }
    }
}
