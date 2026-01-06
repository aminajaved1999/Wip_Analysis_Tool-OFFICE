using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WIPAT.Entities.Dto
{
    public class ForecastCheckResult
    {
        public bool FileExists { get; set; }
        public ForecastFileData FileData { get; set; }
        public bool ProjectionExists { get; set; }
        public bool IsWipCalculated { get; set; }
    }
}
