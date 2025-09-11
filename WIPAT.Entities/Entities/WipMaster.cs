using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WIPAT.Entities
{
    public class WipMaster
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string IssuedMonth { get; set; }
        public string TargetMonth { get; set; }
        public string FileName { get; set; }
        public string Type { get; set; }
        public string WipProcessingType { get; set; }
        public int? MOQ { get; set; }
        public bool IsCasePackChecked { get; set; }
        // Navigation
        public virtual ICollection<WipDetail> Details { get; set; }
    }
}
