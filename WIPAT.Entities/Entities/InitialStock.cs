using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WIPAT.Entities.Dto;
using WIPAT.Entities.Enum;

namespace WIPAT.Entities
{
    public class InitialStock : BaseEntity
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Index(IsUnique = true)]
        public int ItemCatalogueId { get; set; }  // FK
        public int OpeningStock { get; set; }
        public int OrderQty { get; set; }
        public int ProductionQty { get; set; }
        public DateTime? OrderQtyUpdatedAt { get; set; }
        public int? OrderQtyUpdatedBy { get; set; }
        public DateTime? ProductionQtyUpdatedAt { get; set; }
        public int? ProductionQtyUpdatedBy { get; set; }

        public string Description { get; set; }
        public string Notes { get; set; }

        [ForeignKey(nameof(ItemCatalogueId))]
        public ItemCatalogue ItemCatalogue { get; set; }

    }

    public class InvalidStock
    {
        public string Casin { get; set; }
        public string Quantity { get; set; }
        public string FileName { get; set; }
    }

    public enum StockOrderExcelColumns
    {
        CASIN,
        Quantity,
        Month,
        Year
    }

    public enum ForecastExcelColumns
    {
        [Description("C-ASIN")]
        CASIN,

        [Description("Requested Quantity")]
        Requested_Quantity,

        [Description("Commitment period")]
        Commitment_Period,

        [Description("PO date")]
        PO_Date,

        ProjectionMonth,
        ProjectionYear
    }

    public enum ExcelSheetNames
    {
        [Description("Vendor Central Excel Output")]
        Forecast,
        Stock,
        Order,
    }

    public class ForecastCheckResult
    {
        public bool FileExists { get; set; }
        public ForecastFileData FileData { get; set; }
        public bool ProjectionExists { get; set; }
        public bool IsWipCalculated { get; set; }
    }

}
