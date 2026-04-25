using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WIPAT.Entities;

namespace WIPAT.BLL.Strategies
{
    public class WipCalculationContext
    {
        public int CurrentPeriod { get; set; }
        public int TargetPeriod { get; set; }
        public int CurrentStock { get; set; }
        public int InitialStock { get; set; }
        public int Demand { get; set; } // qty2
        public List<ForecastDetail> ForecastData { get; set; }
        public int? Percentage { get; set; } // For medium capacity
    }
}
