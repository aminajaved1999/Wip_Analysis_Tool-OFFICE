using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WIPAT.Entities.Dto
{
    public class SimulationInputData
    {
        public string Asin { get; set; }
        public int ItemId { get; set; }
        public int ActualOrderQty { get; set; }
        public int InitialStock { get; set; }
    }
}
