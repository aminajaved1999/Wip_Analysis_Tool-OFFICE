using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WIPAT.Entities.BO;

namespace WIPAT.Entities.Dto
{
    public class StockFileResponse
    {
        public DataTable DataTable { get; set; }
        public List<InitialStock> ValidStocks { get; set; }
        public List<InvalidStock> MissingStocks { get; set; }
    }
}
