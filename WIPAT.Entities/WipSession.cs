using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WIPAT.Entities.Dto;

namespace WIPAT.Entities
{
    public sealed class WipSession
    {
        // Step 1 inputs
        public List<ItemCatalogue> ItemCatalogue { get; set; } = new List<ItemCatalogue>();
        public List<string> AsinList { get; set; } = new List<string>();
        public List<ForecastFileData> ForecastFiles { get; set; } = new List<ForecastFileData>();
        
        // Forecast masters (MUST be strongly typed)
        public ForecastMaster Prev { get; set; }
        public ForecastMaster Curr { get; set; }
        public string CurrentMonth { get; set; }           // e.g. "July"
        public string CurrentMonthWithYear { get; set; }   // e.g. "July 2025"
        public string TargetMonth { get; set; }            // e.g. "August"
        public string TargetMonthWithYear { get; set; }            // e.g. "August 2025"
        public int CommitmentPeriod { get; set; }          // e.g. 2
        public string WipType { get; set; }                // Analyst | Layman | LaymanFormula
        public DataTable Stock { get; set; }
        public DataTable Orders { get; set; }

        public DataTable FinalDataTable { get; set; }      // produced by Step 2 (Review)
        public string WipStatus { get; set; }              // "Reviewed" / "Approved"
        public string WipProcessingType { get; set; }      // MonthOfSupply / Percentage / System

        public User LoggedInUser { get; set; }


    }
}