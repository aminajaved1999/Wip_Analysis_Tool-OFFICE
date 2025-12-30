using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WIPAT.Entities
{
    public abstract class BaseEntity
    {
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public int? CreatedById { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public int? UpdatedById { get; set; }

        public string Description { get; set; }
        public string Notes { get; set; }

        // Navigation properties for User
        [ForeignKey(nameof(CreatedById))]
        public virtual User CreatedBy { get; set; }

        [ForeignKey(nameof(UpdatedById))]
        public virtual User UpdatedBy { get; set; }
    }


}
