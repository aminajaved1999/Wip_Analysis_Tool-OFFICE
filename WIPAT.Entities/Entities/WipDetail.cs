using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WIPAT.Entities
{
    public class WipDetail
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int? WipMaster_Id { get; set; }
        public string CASIN { get; set; }
        public string Month { get; set; }
        public string Year { get; set; }
        public int? WipQuantity { get; set; }
        public int? SystemWip { get; set; }
        public int? Stock { get; set; }
        public string CommitmentPeriod { get; set; }
        public DateTime PODate { get; set; }
        //wip
        public int? LaymanFormula { get; set; }
        public int? Layman { get; set; }
        public int? Analyst { get; set; }
        public int? Review_Wip { get; set; }
        public int? MOQ_Wip { get; set; }
        public int? CasePack_Wip { get; set; }
        public int? CasePack { get; set; }
        public int? UserWipQty { get; set; }

        [ForeignKey(nameof(WipMaster_Id))]
        public virtual WipMaster Master { get; set; }

    }
}
